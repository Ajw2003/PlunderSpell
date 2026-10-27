using System;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// The Ophan of Hours' own numbers (#418), as the owner approved them on 2026-10-27. When it comes and how long it
    /// stays after being paid live in <see cref="Plunderspell.Core.OphanDemandTuning"/>; these are how it looks and how
    /// its beams behave. See docs/4-systems/fear.md, "The Ophan in the sky".
    /// </summary>
    [Serializable]
    public sealed class OphanTuning
    {
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
        [Range(0.5f, 10f)] public float LockSeconds = 3f;

        [Tooltip("Health a second the slow beam drains while no stone stands between its eye and the target.")]
        [Range(1f, 200f)] public float SlowBeamDamagePerSecond = 25f;

        [Tooltip("Damage is dealt in steps this many seconds apart, so a client is not sent a hit every frame.")]
        [Range(0.05f, 1f)] public float DamageStepSeconds = 0.2f;

        [Header("The charged beam")]
        [Tooltip("Seconds its target must be behind cover before it starts charging.")]
        [Range(0.5f, 20f)] public float CoverSecondsBeforeCharge = 3f;

        [Tooltip("Seconds it charges, its eye swelling to full blast, before the charged beam fires.")]
        [Range(0.5f, 10f)] public float ChargeSeconds = 3f;

        [Tooltip("Seconds after a charged beam before it can charge again.")]
        [Range(0f, 60f)] public float ChargeCooldownSeconds = 10f;

        [Tooltip("A charged hit takes this much health: more than any player has.")]
        [Range(100f, 100000f)] public float ChargedBeamDamage = 100000f;
    }
}
