using Code.Scripts.EventSystems;
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

        /// <summary>
        /// Host: puts a piece for <paramref name="scenario"/> on the floor 1.5 m in front of the main
        /// camera and returns "x,y,z name" of where it rests. "impact" takes a piece that never breaks,
        /// "break" one a 2 m drop breaks.
        /// </summary>
        public static string StageScenario(string scenario)
        {
            const float DropHeight = 2f;
            float landingSpeed = Mathf.Sqrt(2f * -Physics.gravity.y * DropHeight);
            Transform cam = Camera.main.transform;
            Item piece = scenario == "break"
                ? Nearest(cam.position, freeOnly: true, breaksAt: landingSpeed * 0.8f)
                : Nearest(cam.position, freeOnly: true, sturdyOnly: true);
            if (piece == null)
                return "no piece for " + scenario;

            Vector3 forward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            Vector3 ahead = cam.position + forward * 1.5f;
            if (!Physics.Raycast(ahead, Vector3.down, out RaycastHit floor, 5f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
                return "no floor in front of the camera";

            var body = piece.GetComponent<Rigidbody>();
            var collider = piece.GetComponentInChildren<Collider>();
            Vector3 rest = floor.point + Vector3.up * ((collider != null ? collider.bounds.extents.y : 0.3f) + 0.02f);
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.position = rest;
            piece.transform.position = rest;
            return FormattableString.Invariant($"{rest.x:F2},{rest.y:F2},{rest.z:F2} {piece.name}");
        }

        /// <summary>Host: lifts the piece called <paramref name="name"/> nearest (x, y, z) and drops it.</summary>
        public static string DropAt(float x, float y, float z, string name, float height)
        {
            Item piece = Nearest(new Vector3(x, y, z), freeOnly: true, name);
            if (piece == null)
                return "no piece";
            Instance().Drop(piece, height);
            return "dropping " + piece.name;
        }

        /// <summary>Client: watches the piece called <paramref name="name"/> nearest (x, y, z) for <paramref name="seconds"/>.</summary>
        public static string WatchNearest(float x, float y, float z, float seconds, string name)
        {
            Item piece = Nearest(new Vector3(x, y, z), freeOnly: false, name);
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

        private static Item Nearest(Vector3 point, bool freeOnly, string name = null, float breaksAt = -1f, bool sturdyOnly = false)
        {
            Item best = null;
            float bestDistance = float.MaxValue;
            foreach (Item item in FindObjectsByType<Item>(FindObjectsSortMode.None))
            {
                var body = item.GetComponent<Rigidbody>();
                if (freeOnly && (body == null || body.isKinematic))
                    continue;
                if (name != null && item.name != name)
                    continue;
                if (breaksAt > 0f || sturdyOnly)
                {
                    if (!item.TryGetComponent(out Plunderspell.Loot.LootPickup loot))
                        continue;
                    if (breaksAt > 0f && !loot.WouldBreak(breaksAt))
                        continue;
                    if (sturdyOnly && loot.WouldBreak(50f))
                        continue;
                }
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
            EventManager.Instance?.Subscribe(this, (ItemImpacted e) => OnImpact(e.Item, e.Collision));
            float relayedAt = -1f;
            void OnRelayed(Item item, float speed, Vector3 point, bool struckCreature)
            {
                if (item == piece && relayedAt < 0f)
                    relayedAt = Time.realtimeSinceStartup;
            }
            EventManager.Instance?.Subscribe(this, (ItemImpactedRemotely e) => OnRelayed(e.Item, e.Speed, e.Point, e.StruckCreature));

            AudioSource[] sources = AudioDirector.Instance.GetComponentsInChildren<AudioSource>();
            float end = Time.realtimeSinceStartup + seconds;
            float top = restY, fallStart = -1f, landed = -1f, soundAt = -1f;
            string soundName = null;
            var heard = new System.Text.StringBuilder();
            var started = new System.Collections.Generic.HashSet<AudioSource>();
            while (Time.realtimeSinceStartup < end)
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
                if (fallStart > 0f)
                {
                    foreach (AudioSource source in sources)
                    {
                        if (source.isPlaying && source.clip != null && source.clip.name.StartsWith("phys_") && source.time < 0.1f
                            && started.Add(source))
                        {
                            if (soundAt < 0f)
                            {
                                soundAt = now;
                                soundName = source.clip.name;
                            }
                            heard.Append(' ').Append(source.clip.name).Append('@').Append(((now - fallStart) * 1000f).ToString("F0")).Append("ms");
                        }
                    }
                }
                if (landed > 0f && now - landed > 1.5f)
                    break;
                yield return null;
            }
            EventManager.Instance?.Unsubscribe<ItemImpacted>(this);
            EventManager.Instance?.Unsubscribe<ItemImpactedRemotely>(this);

            string Ms(float t) => t < 0f || fallStart < 0f ? "never" : $"{(t - fallStart) * 1000f:F0} ms";
            var body = piece.GetComponent<Rigidbody>();
            Debug.Log($"[AudioLatency] watch {piece.name}: from fall start, mesh down {Ms(landed)}, collision here {Ms(impactAt)}, relayed impact {Ms(relayedAt)}, " +
                      $"phys sound starts {Ms(soundAt)} ({soundName ?? "none"}), all:{heard}, kinematic {(body != null && body.isKinematic)}");
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

            EventManager.Instance?.Subscribe(this, (ItemImpacted e) => OnImpact(e.Item, e.Collision));
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
            EventManager.Instance?.Unsubscribe<ItemImpacted>(this);

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
