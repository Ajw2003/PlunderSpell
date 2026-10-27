using UnityEngine;

namespace Plunderspell.Core
{
    /// <summary>
    /// Every number of the Ophan's demand loop (#431), and the raid's length, edited in the Inspector:
    /// select <c>Assets/_Project/Resources/OphanDemandTuning.asset</c>. Read through <see cref="Default"/>.
    /// See docs/4-systems/raid.md.
    /// </summary>
    [CreateAssetMenu(menuName = "Plunderspell/Raid/Ophan Demand Tuning", fileName = "OphanDemandTuning")]
    public sealed class OphanDemandTuning : ScriptableObject
    {
        public const string ResourcePath = "OphanDemandTuning";

        [Header("Raid")]
        [Tooltip("How long a raid lasts, in seconds, from arriving to the portal closing.")]
        [Range(60f, 1800f)] public float RaidSeconds = 300f;

        [Tooltip("The last part of the raid, in seconds, in which the Ophan hunts while a demand is owed.")]
        [Range(0f, 300f)] public float HuntWindowSeconds = 60f;

        [Header("When the Ophan demands")]
        [Tooltip("Raids played before the first demand falls due.")]
        [Range(0, 10)] public int FirstDemandDueAfterRaids = 1;

        [Tooltip("Fewest raids between one demand and the next.")]
        [Range(1, 20)] public int RaidsBetweenDemandsMin = 3;

        [Tooltip("Most raids between one demand and the next. The gap is picked between the two.")]
        [Range(1, 20)] public int RaidsBetweenDemandsMax = 5;

        [Tooltip("While a demand is owed, the Ophan hunts in every raid's hunt window, not only the one it fell due in.")]
        public bool HuntEveryRaidWhileOwed = true;

        [Tooltip("Share of a raid's spoils the Ophan takes while a demand is owed: 1 takes everything brought home.")]
        [Range(0f, 1f)] public float SpoilsTakenWhileOwed = 1f;

        [Header("What it demands")]
        [Tooltip("Extra pieces added to the next demand for each demand missed.")]
        [Range(0, 5)] public int ExtraPiecesPerMissedDemand = 0;

        [Tooltip("Most pieces one demand can ask for, missed demands included.")]
        [Range(1, 10)] public int MaxPiecesPerDemand = 3;

        [Tooltip("Chance, per raid, that the Ophan comes with nothing owed.")]
        [Range(0f, 1f)] public float UnscheduledVisitChance = 0f;

        [Tooltip("Seconds the Ophan keeps hunting after a demand is paid in that raid.")]
        [Range(0f, 300f)] public float HuntSecondsAfterPaid = 0f;

        [Header("The marked piece")]
        [Tooltip("Radius of the mark around a demanded piece, in metres.")]
        [Range(1f, 30f)] public float PieceMarkRadius = 6f;

        [Tooltip("How many demands a piece's mark lasts before it fades.")]
        [Range(1, 10)] public int PieceMarkLastsForDemands = 3;

        [Tooltip("The first demanded piece lies at least this many rooms from the arrival portal.")]
        [Range(0, 30)] public int FirstPieceMinRoomsFromPortal = 6;

        private static OphanDemandTuning s_default;

        /// <summary>The shipped asset, loaded once from Resources. Missing, it logs an error and uses these field defaults.</summary>
        public static OphanDemandTuning Default
        {
            get
            {
                if (s_default != null)
                    return s_default;
                s_default = Resources.Load<OphanDemandTuning>(ResourcePath);
                if (s_default == null)
                {
                    Debug.LogError($"[Raid] No Resources/{ResourcePath}.asset; using the built-in defaults.");
                    s_default = CreateInstance<OphanDemandTuning>();
                }
                return s_default;
            }
        }
    }
}
