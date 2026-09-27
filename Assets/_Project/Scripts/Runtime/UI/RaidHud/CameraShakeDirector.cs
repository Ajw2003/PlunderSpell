using Interfaces;
using Plunderspell.Spells;
using Plunderspell.Voice;
using StateMachine;
using UnityEngine;

namespace Plunderspell.UI
{
    /// <summary>
    /// Shakes this machine's view when something lands: a hit on you, a hit you dealt, a spell cast
    /// near you (a shout more than a whisper), and your own Saltus slam. Only the local camera moves,
    /// never time or physics, because the raid is shared. Creates itself.
    /// See docs/4-systems/damage.md, "Camera shake".
    /// </summary>
    [DefaultExecutionOrder(-50)] // before PlayerInputController, which applies the offset in Look
    public class CameraShakeDirector : MonoBehaviour
    {
        /// <summary>A cast farther away than this does not shake you.</summary>
        public const float CastReachMetres = 20f;

        private readonly ShakeTrauma _trauma = new ShakeTrauma();
        private PlayerStateMachine _slamSource;

        public static CameraShakeDirector Instance { get; private set; }

        /// <summary>Current trauma, 0..1, for tests.</summary>
        public float Trauma => _trauma.Trauma;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null)
                return;
            var go = new GameObject("~CameraShake");
            DontDestroyOnLoad(go);
            go.AddComponent<CameraShakeDirector>();
        }

        private void Awake() => Instance = this;

        private void OnEnable()
        {
            Damage.Dealt += OnDamage;
            SpellCastingSystem.CastResolved += OnCast;
        }

        private void OnDisable()
        {
            Damage.Dealt -= OnDamage;
            SpellCastingSystem.CastResolved -= OnCast;
            WatchSlams(null);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>Trauma for a hit on the local player: sized by the share of health it took,
        /// so a sword blow jolts and a fire tick barely registers.</summary>
        public static float ForHitTaken(float amount, float maxHealth, DamageKind kind)
        {
            if (kind == DamageKind.Burn || kind == DamageKind.Choke)
                return 0.1f;
            return 0.4f + 1.5f * amount / Mathf.Max(1f, maxHealth);
        }

        /// <summary>Trauma for a hit the local player dealt: a small knock, more for a kill.</summary>
        public static float ForHitDealt(bool killed) => killed ? 0.55f : 0.35f;

        /// <summary>Trauma for a cast <paramref name="metres"/> away: louder is stronger, and it
        /// fades to nothing at <see cref="CastReachMetres"/>.</summary>
        public static float ForCast(CastVolume volume, float metres)
        {
            float loudness = volume switch
            {
                CastVolume.Shout => 0.7f,
                CastVolume.Normal => 0.45f,
                _ => 0.25f,
            };
            return loudness * Mathf.Clamp01(1f - metres / CastReachMetres);
        }

        /// <summary>Trauma for a Saltus slam landing at <paramref name="speed"/> metres a second.</summary>
        public static float ForSlam(float speed) => 0.45f + 0.55f * Mathf.Clamp01(speed / 25f);

        private void Update()
        {
            PlayerStateMachine local = PlayerStateMachine.Local;
            WatchSlams(local);
            if (local == null)
            {
                _trauma.Clear();
                return;
            }
            local.ViewShake = _trauma.Step(Time.deltaTime);
        }

        private void WatchSlams(PlayerStateMachine local)
        {
            if (_slamSource == local)
                return;
            if (_slamSource != null)
                _slamSource.SlamLanded -= OnSlam;
            _slamSource = local;
            if (_slamSource != null)
                _slamSource.SlamLanded += OnSlam;
        }

        private void OnSlam(Vector3 where, float speed) => _trauma.Add(ForSlam(speed));

        private void OnDamage(DamageReport report)
        {
            PlayerStateMachine local = PlayerStateMachine.Local;
            if (local == null || report.Target == null)
                return;

            Transform you = local.transform.root;
            if (report.Target.transform.root == you)
                _trauma.Add(ForHitTaken(report.Amount, report.MaxHealth, report.Kind));
            else if (report.Instigator != null && report.Instigator.transform.root == you)
                _trauma.Add(ForHitDealt(report.Killed));
        }

        private void OnCast(SpellCastingSystem.CastReport report)
        {
            PlayerStateMachine local = PlayerStateMachine.Local;
            if (local == null)
                return;
            _trauma.Add(ForCast(report.Volume, Vector3.Distance(local.transform.position, report.Origin)));
        }
    }
}
