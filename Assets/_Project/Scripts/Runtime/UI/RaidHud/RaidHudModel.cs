using Plunderspell.Alarm;
using Plunderspell.Raid;
using UnityEngine;

namespace Plunderspell.UI
{
    /// <summary>
    /// Everything the HUD shows, as plain data. The presenter builds one of these when a source publishes
    /// a change, and the view only draws it — so what the player is told is testable without rendering anything.
    /// </summary>
    public readonly struct RaidHudModel
    {
        public readonly RaidPhase Phase;
        public readonly float TimeRemaining;
        public readonly AlarmState Alarm;
        public readonly float AlarmLevel;
        public readonly string CarriedLootName;
        public readonly bool CarriedNeedsTwo;
        public readonly string InteractPrompt;

        /// <summary>
        /// True while the player is aiming at something they can act on. The crosshair swaps to its
        /// interaction form on this, which is what makes "you can touch that" readable at a glance
        /// before the player has read the prompt.
        /// </summary>
        public readonly bool HasInteractTarget;

        public readonly float Debt;
        public readonly float BankedGold;
        public readonly string LastCastLine;

        /// <summary>Worth of the loot currently standing in the extraction zone.</summary>
        public readonly float HaulWorth;

        /// <summary>How many pieces are standing in the extraction zone.</summary>
        public readonly int HaulPieces;

        /// <summary>"" while not holding a ranged weapon, "Loaded" while ready, or a reload
        /// countdown otherwise. See Issue 39's completion check that ammo/reload state be visible.</summary>
        public readonly string RangedWeaponStatus;

        /// <summary>True while the cast key is held and the microphone (or keyboard) is taking a word.</summary>
        public readonly bool IsCasting;

        /// <summary>The microphone being listened on while casting by voice, or null when casting by keyboard or not at all.</summary>
        public readonly string ListenDevice;

        /// <summary>True while a keyed cast is being chanted, with the word and how far along it is (0..1).</summary>
        public readonly bool Chanting;
        public readonly string ChantWord;
        public readonly float ChantProgress;

        /// <summary>The player's mana now; the spell list strikes through words that cost more.</summary>
        public readonly int Mana;

        /// <summary>The game mode now. The HUD draws only while playing or paused.</summary>
        public readonly Plunderspell.Core.GameState State;

        /// <summary>True while the player holds <c>T</c> to raise the pocket watch.</summary>
        public readonly bool WatchUp;

        /// <summary>The raid's full length in seconds, so the watch can show what share is left.</summary>
        public readonly float TimeTotal;

        /// <summary>True while the grimoire is open (the player holds <c>Tab</c>, or it opened itself for a first raid).</summary>
        public readonly bool GrimoireOpen;

        /// <summary>The last few casts, newest first, for the grimoire's margin. Null before the first.</summary>
        public readonly string[] RecentCasts;

        public RaidHudModel(RaidPhase phase, float timeRemaining, AlarmState alarm, float alarmLevel,
            string carriedLootName, bool carriedNeedsTwo, string interactPrompt,
            bool hasInteractTarget, float debt, float bankedGold, string lastCastLine,
            float haulWorth = 0f, int haulPieces = 0, string rangedWeaponStatus = "",
            bool isCasting = false, string listenDevice = null, bool chanting = false, string chantWord = "",
            float chantProgress = 0f, int mana = int.MaxValue,
            Plunderspell.Core.GameState state = Plunderspell.Core.GameState.Playing,
            bool watchUp = false, float timeTotal = 0f, bool grimoireOpen = false, string[] recentCasts = null)
        {
            GrimoireOpen = grimoireOpen;
            RecentCasts = recentCasts;
            WatchUp = watchUp;
            TimeTotal = timeTotal;
            IsCasting = isCasting;
            ListenDevice = listenDevice;
            Chanting = chanting;
            ChantWord = chantWord;
            ChantProgress = chantProgress;
            Mana = mana;
            State = state;
            HaulWorth = haulWorth;
            HaulPieces = haulPieces;
            RangedWeaponStatus = rangedWeaponStatus;
            HasInteractTarget = hasInteractTarget;
            Phase = phase;
            TimeRemaining = timeRemaining;
            Alarm = alarm;
            AlarmLevel = alarmLevel;
            CarriedLootName = carriedLootName;
            CarriedNeedsTwo = carriedNeedsTwo;
            InteractPrompt = interactPrompt;
            Debt = debt;
            BankedGold = bankedGold;
            LastCastLine = lastCastLine;
        }

        /// <summary>The raid clock as mm:ss. Negative time reads 00:00 rather than going backwards.</summary>
        public string TimerText => FormatTime(TimeRemaining);

        /// <summary>
        /// True inside the last minute. The view uses this to shout about it — a raid is lost by not
        /// noticing the clock, and the clock is the only thing the players cannot negotiate with.
        /// </summary>
        public bool TimerIsCritical => TimeRemaining > 0f && TimeRemaining <= 60f;

        /// <summary>What the alarm bar should read.</summary>
        public string AlarmText
        {
            get
            {
                switch (Alarm)
                {
                    case AlarmState.Stirred: return "STIRRED — they heard something";
                    case AlarmState.Roused: return "ROUSED — doors locking";
                    case AlarmState.HueAndCry: return "HUE AND CRY — get out";
                    default: return "Calm";
                }
            }
        }

        /// <summary>Alarm level as a 0..1 bar fill.</summary>
        public float AlarmFill => Mathf.Clamp01(AlarmLevel / 100f);

        /// <summary>
        /// What the haul readout says. Nothing on the pad reads as an instruction rather than a
        /// zero, because "0 gold" looks like a broken counter and "carry it here" does not.
        /// </summary>
        public string HaulText => HaulPieces == 0
            ? "Haul: bring loot to the portal"
            : $"Haul: {HaulWorth:N0} gold ({HaulPieces} piece{(HaulPieces == 1 ? string.Empty : "s")})";

        public static string FormatTime(float seconds)
        {
            if (seconds < 0f)
                seconds = 0f;
            int total = Mathf.FloorToInt(seconds);
            return $"{total / 60:00}:{total % 60:00}";
        }
    }
}
