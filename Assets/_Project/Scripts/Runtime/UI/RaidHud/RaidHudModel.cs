using RogueAi.Alarm;
using RogueAi.Raid;
using UnityEngine;

namespace RogueAi.UI
{
    /// <summary>
    /// Everything the HUD shows, as plain data. The presenter builds one of these each frame and the
    /// view only draws it — so what the player is told is testable without rendering anything.
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
        public readonly float Debt;
        public readonly float BankedGold;
        public readonly string LastCastLine;

        public RaidHudModel(RaidPhase phase, float timeRemaining, AlarmState alarm, float alarmLevel,
            string carriedLootName, bool carriedNeedsTwo, string interactPrompt,
            float debt, float bankedGold, string lastCastLine)
        {
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

        public static string FormatTime(float seconds)
        {
            if (seconds < 0f)
                seconds = 0f;
            int total = Mathf.FloorToInt(seconds);
            return $"{total / 60:00}:{total % 60:00}";
        }
    }
}
