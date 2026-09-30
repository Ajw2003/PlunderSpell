using System;
using System.Collections;
using UnityEngine;

namespace Plunderspell.Audio
{
    /// <summary>
    /// Diagnostic: plays each named sound through <see cref="AudioDirector"/> in turn and logs how long
    /// until the listener's output first carries it. Not added by the game; attach it from an eval with
    /// the names to test, e.g. <c>AudioDirector.Instance.gameObject.AddComponent&lt;AudioLatencyProbe&gt;().Run(names)</c>.
    /// </summary>
    public sealed class AudioLatencyProbe : MonoBehaviour
    {
        private const float OnsetLevel = 0.001f;
        private const float TimeoutSeconds = 2f;
        private const float GapSeconds = 1.5f;

        private readonly float[] _samples = new float[256];

        public void Run(string[] soundNames) => StartCoroutine(Measure(soundNames));

        // Static entry points for Tools/Unity/coop_drop_latency.sh: a Development build's eval reaches
        // game code only through reflection, so each call is one method with plain arguments.

        /// <summary>Host: lifts the free piece nearest the main camera and drops it; returns where it was.</summary>
        public static string DropNearestToCamera(float height)
        {
            Item piece = Nearest(Camera.main.transform.position, freeOnly: true);
            if (piece == null)
                return "no piece";
            Vector3 at = piece.transform.position;
            Instance().Drop(piece, height);
            return FormattableString.Invariant($"{at.x:F2},{at.y:F2},{at.z:F2} {piece.name}");
        }

        /// <summary>Client: watches the piece nearest (x, y, z) for <paramref name="seconds"/>.</summary>
        public static string WatchNearest(float x, float y, float z, float seconds)
        {
            Item piece = Nearest(new Vector3(x, y, z), freeOnly: false);
            if (piece == null)
                return "no piece";
            Instance().Watch(piece, y, seconds);
            return "watching " + piece.name;
        }

        /// <summary>Turns the reduced sound set on or off, so a muted group can be measured.</summary>
        public static string SetSoundFocus(bool enabled)
        {
            SoundFocus.Enabled = enabled;
            return "focus " + enabled;
        }

        private static AudioLatencyProbe Instance()
        {
            GameObject root = AudioDirector.Instance.gameObject;
            return root.GetComponent<AudioLatencyProbe>() ?? root.AddComponent<AudioLatencyProbe>();
        }

        private static Item Nearest(Vector3 point, bool freeOnly)
        {
            Item best = null;
            float bestDistance = float.MaxValue;
            foreach (Item item in FindObjectsByType<Item>(FindObjectsSortMode.None))
            {
                var body = item.GetComponent<Rigidbody>();
                if (freeOnly && (body == null || body.isKinematic))
                    continue;
                float distance = (item.transform.position - point).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = item;
                }
            }
            return best;
        }

        /// <summary>
        /// Lifts <paramref name="piece"/> <paramref name="height"/> metres, lets it fall, and logs per frame
        /// where its rigidbody and its visible mesh are, when the collision fires, and when its sound
        /// reaches the listener.
        /// </summary>
        public void Drop(Item piece, float height) => StartCoroutine(MeasureDrop(piece, height));

        /// <summary>
        /// For a machine that does not own the piece (a co-op client): watches <paramref name="piece"/>
        /// for <paramref name="seconds"/> and logs, relative to the moment its visible mesh starts falling
        /// back from a lift, when the mesh lands, when this machine raises the collision, and when a
        /// phys_ sound starts on this machine.
        /// </summary>
        public void Watch(Item piece, float restY, float seconds) => StartCoroutine(WatchDrop(piece, restY, seconds));

        // Heights are the piece's transform, which carries its visible mesh; restY is where it rested
        // before the host lifted it (the lift may already be under way when the watch starts).
        private IEnumerator WatchDrop(Item piece, float restY, float seconds)
        {
            Transform mesh = piece.transform;
            float impactAt = -1f;
            void OnImpact(Item item, Collision collision)
            {
                if (item == piece && impactAt < 0f)
                    impactAt = Time.realtimeSinceStartup;
            }
            Item.Impacted += OnImpact;

            AudioSource[] sources = AudioDirector.Instance.GetComponentsInChildren<AudioSource>();
            float end = Time.realtimeSinceStartup + seconds;
            float top = restY, fallStart = -1f, landed = -1f, soundAt = -1f;
            string soundName = null;
            while (Time.realtimeSinceStartup < end && (landed < 0f || soundAt < 0f))
            {
                float now = Time.realtimeSinceStartup;
                float y = mesh.position.y;
                if (fallStart < 0f)
                {
                    if (y > top) top = y;
                    else if (top > restY + 0.5f && y < top - 0.02f) fallStart = now;
                }
                else if (landed < 0f && y <= restY + 0.05f)
                {
                    landed = now;
                }
                if (fallStart > 0f && soundAt < 0f)
                {
                    foreach (AudioSource source in sources)
                    {
                        if (source.isPlaying && source.clip != null && source.clip.name.StartsWith("phys_") && source.time < 0.1f)
                        {
                            soundAt = now;
                            soundName = source.clip.name;
                            break;
                        }
                    }
                }
                yield return null;
            }
            Item.Impacted -= OnImpact;

            string Ms(float t) => t < 0f || fallStart < 0f ? "never" : $"{(t - fallStart) * 1000f:F0} ms";
            var body = piece.GetComponent<Rigidbody>();
            Debug.Log($"[AudioLatency] watch {piece.name}: from fall start, mesh down {Ms(landed)}, collision here {Ms(impactAt)}, " +
                      $"phys sound starts {Ms(soundAt)} ({soundName ?? "none"}), kinematic {(body != null && body.isKinematic)}");
        }

        private IEnumerator MeasureDrop(Item piece, float height)
        {
            var body = piece.GetComponent<Rigidbody>();
            var mesh = piece.GetComponentInChildren<Renderer>();
            float impactAt = -1f;
            void OnImpact(Item item, Collision collision)
            {
                if (item == piece && impactAt < 0f)
                    impactAt = Time.realtimeSinceStartup;
            }

            float restY = mesh != null ? mesh.bounds.center.y : body.position.y;
            body.isKinematic = true;
            body.position += Vector3.up * height;
            // Long enough for a co-op client's watch to start while the piece is still held up.
            yield return new WaitForSecondsRealtime(3f);

            Item.Impacted += OnImpact;
            body.isKinematic = false;
            body.linearVelocity = Vector3.zero;
            float start = Time.realtimeSinceStartup;
            float heard = -1f;
            float meshLanded = -1f;
            while (Time.realtimeSinceStartup - start < 3f)
            {
                AudioListener.GetOutputData(_samples, 0);
                float peak = 0f;
                foreach (float sample in _samples)
                    peak = Mathf.Max(peak, Mathf.Abs(sample));
                float now = Time.realtimeSinceStartup;
                if (heard < 0f && impactAt > 0f && peak > OnsetLevel)
                    heard = now;
                if (meshLanded < 0f && mesh != null && mesh.bounds.center.y <= restY + 0.05f)
                    meshLanded = now;
                if (heard > 0f && meshLanded > 0f)
                    break;
                yield return null;
            }
            Item.Impacted -= OnImpact;

            string Ms(float t) => t < 0f ? "never" : $"{(t - start) * 1000f:F0} ms";
            Debug.Log($"[AudioLatency] drop {piece.name} from {height} m: collision {Ms(impactAt)}, sound heard {Ms(heard)}, " +
                      $"visible mesh down {Ms(meshLanded)}, interpolation {body.interpolation}, kinematic {body.isKinematic}");
        }

        private IEnumerator Measure(string[] soundNames)
        {
            foreach (string soundName in soundNames)
            {
                yield return new WaitForSecondsRealtime(GapSeconds);
                AudioSource source = AudioDirector.Instance.Play(soundName, Vector3.zero, 1f, 0, SoundPoolKind.General, true);
                if (source == null)
                {
                    Debug.Log($"[AudioLatency] {soundName}: no source (muted or missing)");
                    continue;
                }

                string loadType = source.clip.loadType.ToString();
                float start = Time.realtimeSinceStartup;
                float heard = -1f;
                while (Time.realtimeSinceStartup - start < TimeoutSeconds)
                {
                    AudioListener.GetOutputData(_samples, 0);
                    float peak = 0f;
                    foreach (float sample in _samples)
                        peak = Mathf.Max(peak, Mathf.Abs(sample));
                    if (peak > OnsetLevel)
                    {
                        heard = Time.realtimeSinceStartup - start;
                        break;
                    }
                    yield return null;
                }

                Debug.Log(heard < 0f
                    ? $"[AudioLatency] {soundName} ({source.clip.name}, {loadType}): not heard within {TimeoutSeconds}s"
                    : $"[AudioLatency] {soundName} ({source.clip.name}, {loadType}): {heard * 1000f:F0} ms");
            }
            Debug.Log("[AudioLatency] done");
        }
    }
}
