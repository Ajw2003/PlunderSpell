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

        /// <summary>
        /// Lifts <paramref name="piece"/> <paramref name="height"/> metres, lets it fall, and logs per frame
        /// where its rigidbody and its visible mesh are, when the collision fires, and when its sound
        /// reaches the listener.
        /// </summary>
        public void Drop(Item piece, float height) => StartCoroutine(MeasureDrop(piece, height));

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
            yield return new WaitForSecondsRealtime(0.5f);

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
