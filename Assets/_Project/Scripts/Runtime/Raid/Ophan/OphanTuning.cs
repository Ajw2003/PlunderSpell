using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// The Ophan of Hours' own numbers (#418), as the owner approved them on 2026-10-27. When it comes and how long it
    /// stays after being paid live in <see cref="Plunderspell.Core.OphanDemandTuning"/>; these are how it looks and how
    /// its beams behave. See docs/4-systems/fear.md, "The Ophan in the sky".
    /// Edited in the Inspector: select <c>Assets/_Project/Resources/OphanTuning.asset</c>. One asset for every scene; it
    /// was a copy saved on each scene's Ophan, so a change in code never reached them (audit D2, #527). Read through
    /// <see cref="Default"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Plunderspell/Raid/Ophan Tuning", fileName = "OphanTuning")]
    public sealed class OphanTuning : ScriptableObject
    {
        public const string ResourcePath = "OphanTuning";

        [Header("In the sky")]
        [Tooltip("Times its modelled size (9.6 m tall, 11 m across). 150 is about 1.4 km tall: more than the whole view.")]
        [Range(1f, 300f)] public float Scale = 150f;

        [Tooltip("How far its centre hangs from the castle's centre, along the ground, in metres.")]
        [Range(100f, 2500f)] public float Distance = 900f;

        [Tooltip("How high its centre sits, as degrees above the horizon seen from the castle's centre.")]
        [Range(0f, 80f)] public float Elevation = 30f;

        [Tooltip("Seconds its arrival clip (ophan_arrive) plays before it starts hunting.")]
        [Range(0f, 10f)] public float ArriveSeconds = 4f;

        [Tooltip("Seconds it takes to leave once paid: its rings spin down and it fades.")]
        [Range(0f, 10f)] public float LeaveSeconds = 4f;

        [Header("The slow beam")]
        [Tooltip("Seconds its eye must hold a target in the open before the slow beam starts.")]
        [Range(0.5f, 10f)] public float LockSeconds = 2f;

        [Tooltip("Health a second the slow beam drains while no stone stands between its eye and the target.")]
        [Range(1f, 200f)] public float SlowBeamDamagePerSecond = 10f;

        [Tooltip("Damage is dealt in steps this many seconds apart, so a client is not sent a hit every frame.")]
        [Range(0.05f, 1f)] public float DamageStepSeconds = 0.2f;

        [Header("The charged beam")]
        [Tooltip("Seconds its target must be behind cover before it starts charging.")]
        [Range(0.5f, 20f)] public float CoverSecondsBeforeCharge = 3f;

        [Tooltip("Seconds it charges, its eye swelling to full blast, before the charged beam fires.")]
        [Range(0.5f, 10f)] public float ChargeSeconds = 3f;

        [Tooltip("Seconds between the charged beam's first shot, which breaks the stone, and its second, which re-aims and kills.")]
        [Range(0f, 5f)] public float SecondShotDelaySeconds = 4f;

        [Tooltip("Seconds after a charged beam before it can charge again.")]
        [Range(0f, 60f)] public float ChargeCooldownSeconds = 10f;

        [Tooltip("A charged hit takes this much health: more than any player has.")]
        [Range(25f, 100000f)] public float ChargedBeamDamage = 25f;

        [Tooltip("Radius of the charged beam, in metres: everything of the castle inside it breaks.")]
        [Range(0.25f, 5f)] public float CutRadius = 1.5f;

        [Tooltip("How hard the charged beam throws the pieces it breaks.")]
        [Range(0f, 50f)] public float CutForce = 12f;

        [Tooltip("Metres short of its target the charged beam stops breaking, so the floor under the target stays.")]
        [Range(0f, 5f)] public float CutStopShort = 2f;

        [Header("Targeting")]
        [Tooltip("Seconds between re-weighing who carries the most worth.")]
        [Range(0.25f, 10f)] public float RepickSeconds = 1f;

        private static OphanTuning s_default;

        /// <summary>The shipped asset, loaded once from Resources. Missing, it logs an error and uses these field defaults.</summary>
        public static OphanTuning Default
        {
            get
            {
                if (s_default != null)
                    return s_default;
                s_default = Resources.Load<OphanTuning>(ResourcePath);
                if (s_default == null)
                {
                    Debug.LogError($"[Raid] No Resources/{ResourcePath}.asset; using the built-in defaults.");
                    s_default = CreateInstance<OphanTuning>();
                }
                return s_default;
            }
        }
    }
}
