using System;
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
        /// <summary>One enemy's health, as the view needs it to draw an in-world bar over its head.</summary>
        public readonly struct EnemyHealthBar
        {
            public readonly Vector3 WorldPosition;
            public readonly float HealthFraction;

            public EnemyHealthBar(Vector3 worldPosition, float healthFraction)
            {
                WorldPosition = worldPosition;
                HealthFraction = healthFraction;
            }
        }

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

        /// <summary>The player's current health. See issue #14 — the HUD must make this visible.</summary>
        public readonly float PlayerCurrentHealth;

        /// <summary>The player's maximum health. Zero means no player was wired to the presenter.</summary>
        public readonly float PlayerMaxHealth;

        /// <summary>Every living, in-view enemy's health, for the view to draw a bar over its head.</summary>
        public readonly EnemyHealthBar[] EnemyHealthBars;

        public RaidHudModel(RaidPhase phase, float timeRemaining, AlarmState alarm, float alarmLevel,
            string carriedLootName, bool carriedNeedsTwo, string interactPrompt,
            bool hasInteractTarget, float debt, float bankedGold, string lastCastLine,
            float haulWorth = 0f, int haulPieces = 0, float playerCurrentHealth = 0f,
            float playerMaxHealth = 0f, EnemyHealthBar[] enemyHealthBars = null)
        {
            HaulWorth = haulWorth;
            HaulPieces = haulPieces;
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
            PlayerCurrentHealth = playerCurrentHealth;
            PlayerMaxHealth = playerMaxHealth;
            EnemyHealthBars = enemyHealthBars ?? Array.Empty<EnemyHealthBar>();
        }

        /// <summary>
        /// Player health as a 0..1 bar fill. Reads full when no player is wired (max health zero)
        /// rather than empty, so a HUD built without a player never shows a dead-looking bar.
        /// </summary>
        public float PlayerHealthFill =>
            PlayerMaxHealth > 0f ? Mathf.Clamp01(PlayerCurrentHealth / PlayerMaxHealth) : 1f;

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
            ? "Haul: bring loot to the pad"
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
