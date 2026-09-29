using Interfaces;
using Plunderspell.Alarm;
using Plunderspell.Guards;
using Plunderspell.Inventory;
using Plunderspell.Spells;
using Plunderspell.Voice;

namespace Plunderspell.Audio
{
    /// <summary>
    /// Which manifest name an in-game event plays. Pure, so a test can check every name it can return
    /// exists in the SoundBank. A null return means "no sound for this yet"; the gap list in
    /// docs/4-systems/audio.md names each one.
    /// </summary>
    public static class SoundNames
    {
        public static string EraKey(HistoricalEra era)
        {
            switch (era)
            {
                case HistoricalEra.BronzeAge: return "bronze";
                case HistoricalEra.HighMedieval: return "high";
                case HistoricalEra.LateMedieval: return "late";
                default: return "powder";
            }
        }

        public static string AlarmKey(AlarmState state)
        {
            switch (state)
            {
                case AlarmState.Stirred: return "stirred";
                case AlarmState.Roused: return "roused";
                case AlarmState.HueAndCry: return "huecry";
                default: return "calm";
            }
        }

        /// <summary>The raid stem for an alarm index 0..3 (calm, stirred, roused, hue and cry).</summary>
        public static string RaidStem(HistoricalEra era, int index)
        {
            string[] keys = { "calm", "stirred", "roused", "huecry" };
            return "mus_raid_" + EraKey(era) + "_" + keys[index];
        }

        /// <summary>The stinger for the alarm rising to <paramref name="state"/>; null for Calm.</summary>
        public static string AlarmSting(AlarmState state, HistoricalEra era) =>
            state == AlarmState.Calm ? null : "sting_alarm_" + AlarmKey(state) + "_" + EraKey(era);

        public static string SpellCast(SpellId id, CastVolume volume)
        {
            switch (id)
            {
                case SpellId.Ignis: return "sfx_spell_ignis_cast";
                case SpellId.Frango: return "sfx_spell_frango_cast";
                case SpellId.Levo: return "sfx_spell_levo_cast";
                case SpellId.AurumVoco: return "sfx_spell_aurumvoco_cast";
                case SpellId.Velox: return "sfx_spell_velox_cast";
                case SpellId.Saltus: return "sfx_spell_saltus_cast";
                case SpellId.Porta: return "sfx_spell_porta_cast";
                case SpellId.Somnus:
                    return volume == CastVolume.Shout ? "sfx_spell_somnus_cast_loud" : "sfx_spell_somnus_cast_soft";
                default: return null;
            }
        }

        public static string SpellMisfire(SpellId id)
        {
            switch (id)
            {
                case SpellId.MisfireIgnis: return "sfx_spell_ignis_misfire";
                case SpellId.MisFireFrango: return "sfx_spell_frango_misfire";
                case SpellId.MisfireLevo: return "sfx_spell_levo_misfire";
                case SpellId.MisFireSomnus: return "sfx_spell_somnus_misfire";
                case SpellId.MisfireAurumVoco: return "sfx_spell_aurumvoco_misfire";
                case SpellId.MisfirePorta: return "sfx_spell_porta_misfire";
                case SpellId.MisfireVelox: return "sfx_spell_velox_misfire";
                case SpellId.MisfireSaltus: return "sfx_spell_saltus_misfire";
                default: return null;
            }
        }

        public static string Hit(DamageKind kind)
        {
            switch (kind)
            {
                case DamageKind.Impact: return "phys_impact_body";
                case DamageKind.Melee: return "sfx_wpn_blade_hit_flesh";
                case DamageKind.Projectile: return "sfx_wpn_xbow_bolt_hit_flesh";
                case DamageKind.EnemyAttack: return "sfx_wpn_blunt_hit_flesh";
                default: return null;
            }
        }

        public static string GuardAttack(GuardAttackKind kind, HistoricalEra era)
        {
            if (kind == GuardAttackKind.Projectile)
                return "sfx_throw_whoosh_light";
            return era == HistoricalEra.BronzeAge ? "sfx_wpn_bronze_swing" : "sfx_wpn_blade_swing";
        }

        public const string DoorOpen = "sfx_door_wood_open";
        public const string DoorClose = "sfx_door_wood_close";
        public const string LootBreak = "phys_break_ceramic";
        public const string Fizzle = "sfx_spell_fizzle";
        public const string NoMana = "sfx_spell_no_mana";
        public const string MisfireSting = "sting_spell_misfire";
        public const string PlayerHurt = "sfx_player_hurt";
        public const string PlayerHurtHeavy = "sfx_player_hurt_heavy";
        public const string PlayerDeath = "sfx_player_death";
        public const string Dodge = "foley_player_dodge";
        public const string Jump = "foley_player_jump";
        public const string UiHover = "ui_button_hover";
        public const string UiClick = "ui_button_click";
        public const string UiBack = "ui_button_back";
        public const string ItemCrossed = "sfx_extract_item_cross";
        public const string ExtractSuccess = "sting_extract_success";
        public const string PlayerDownSting = "sting_player_down";
        public const string PortalOpened = "sting_portal_opened";
        public const string PortalWarning = "sting_portal_warning";
    }
}
