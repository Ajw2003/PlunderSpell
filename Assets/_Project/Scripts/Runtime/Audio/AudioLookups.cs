using System;
using Plunderspell.Guards;
using Plunderspell.Inventory;
using UnityEngine;

namespace Plunderspell.Audio
{
    /// <summary>What a character is walking on. The names match the <c>foley_step_*</c> sounds.</summary>
    public enum Surface
    {
        Stone,
        Wood,
        Earth,
        Rushes,
        Tile,
        Metal,
        Water
    }

    /// <summary>
    /// Pure lookups from a name to a sound category. They are plain string matches so each is tested
    /// without a scene. The reasoning behind the keyword lists is in docs/4-systems/audio.md.
    /// </summary>
    public static class SurfaceLookup
    {
        // A castle room is one floor collider named after its room ("BronzeLevyBarracks(Clone)") with
        // a material per pigment, so the room name decides; the one material that says more than the
        // room is the ground outside ("BaileyEarth").
        private static readonly (string Keyword, Surface Surface)[] Rules =
        {
            ("water", Surface.Water),
            ("cistern", Surface.Water),
            ("earth", Surface.Earth),
            ("courtyard", Surface.Earth),
            ("garden", Surface.Earth),
            ("yard", Surface.Earth),
            ("midden", Surface.Earth),
            ("pens", Surface.Earth),
            ("tiltyard", Surface.Earth),
            ("woodpile", Surface.Earth),
            ("leanto", Surface.Earth),
            ("shed", Surface.Rushes),
            ("stable", Surface.Rushes),
            ("barn", Surface.Rushes),
            ("hayloft", Surface.Rushes),
            ("barracks", Surface.Wood),
            ("bunk", Surface.Wood),
            ("archive", Surface.Wood),
            ("library", Surface.Wood),
            ("solar", Surface.Wood),
            ("bedchamber", Surface.Wood),
            ("brewhouse", Surface.Wood),
            ("countinghouse", Surface.Wood),
            ("drawbridge", Surface.Wood),
            ("bath", Surface.Tile),
            ("kitchen", Surface.Tile),
            ("chapel", Surface.Tile),
            ("chantry", Surface.Tile),
            ("fresco", Surface.Tile),
            ("treasury", Surface.Tile),
            ("greathall", Surface.Tile),
            ("megaron", Surface.Tile),
            ("foundry", Surface.Metal),
            ("blacksmith", Surface.Metal),
            ("armoury", Surface.Metal),
            ("portcullis", Surface.Metal),
            ("grate", Surface.Metal)
        };

        /// <summary>The surface for a floor collider and one of its material names; stone when nothing matches.</summary>
        public static Surface FromNames(string colliderName, string materialName)
        {
            Surface fromMaterial = Match(materialName, out bool materialMatched);
            if (materialMatched && fromMaterial == Surface.Earth)
                return Surface.Earth;
            Surface fromCollider = Match(colliderName, out bool colliderMatched);
            if (colliderMatched)
                return fromCollider;
            return materialMatched ? fromMaterial : Surface.Stone;
        }

        private static Surface Match(string name, out bool matched)
        {
            matched = false;
            if (string.IsNullOrEmpty(name))
                return Surface.Stone;
            for (int i = 0; i < Rules.Length; i++)
            {
                if (name.IndexOf(Rules[i].Keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    matched = true;
                    return Rules[i].Surface;
                }
            }
            return Surface.Stone;
        }

        public static string StepSound(Surface surface)
        {
            switch (surface)
            {
                case Surface.Wood: return "foley_step_wood";
                case Surface.Earth: return "foley_step_earth";
                case Surface.Rushes: return "foley_step_rushes";
                case Surface.Tile: return "foley_step_tile";
                case Surface.Metal: return "foley_step_metal";
                case Surface.Water: return "foley_step_water";
                default: return "foley_step_stone";
            }
        }
    }

    /// <summary>Stride, loudness and landing rules for footsteps.</summary>
    public static class StepMath
    {
        public const float MinLandFallSpeed = 2.5f;
        public const float HeavyLandFallSpeed = 7f;

        /// <summary>Metres between steps: short when slow, longer when fast, so a sprint steps faster but not frantically.</summary>
        public static float Stride(float speed) => Mathf.Clamp(0.7f + 0.22f * speed, 0.9f, 2f);

        /// <summary>Volume by pace, in step with FootstepNoiseEmitter's crouch, walk and run reach (1.5, 4 and 8 m).</summary>
        public static float Loudness(float speed)
        {
            if (speed < 2.2f)
                return 0.4f;
            return speed < 4.5f ? 0.7f : 1f;
        }

        /// <summary>The landing sound for a fall speed in m/s, or null for a step down too small to hear.</summary>
        public static string Land(float fallSpeed)
        {
            if (fallSpeed < MinLandFallSpeed)
                return null;
            return fallSpeed < HeavyLandFallSpeed ? "foley_player_land" : "foley_player_land_heavy";
        }
    }

    /// <summary>What a loose item is made of, as far as it sounds.</summary>
    public enum LootMaterial
    {
        Stone,
        Wood,
        Metal,
        Bronze,
        Gold,
        Ceramic,
        Glass,
        Cloth,
        Book,
        Coins
    }

    /// <summary>
    /// Loot has no material field, and the data assets are not edited for audio, so the material is read
    /// from the piece's name. First keyword that matches wins; the order below is the priority.
    /// </summary>
    public static class LootMaterials
    {
        private static readonly (string Keyword, LootMaterial Material)[] Rules =
        {
            ("glass", LootMaterial.Glass),
            ("mirror", LootMaterial.Glass),
            ("psalter", LootMaterial.Book),
            ("ledger", LootMaterial.Book),
            ("book", LootMaterial.Book),
            ("coin", LootMaterial.Coins),
            ("tapestry", LootMaterial.Cloth),
            ("cloth", LootMaterial.Cloth),
            ("hippopotamus", LootMaterial.Ceramic),
            ("amphora", LootMaterial.Ceramic),
            ("nautilus", LootMaterial.Ceramic),
            ("faience", LootMaterial.Ceramic),
            ("gold", LootMaterial.Gold),
            ("gilded", LootMaterial.Gold),
            ("crown", LootMaterial.Gold),
            ("badge", LootMaterial.Gold),
            ("mask", LootMaterial.Gold),
            ("goblet", LootMaterial.Gold),
            ("reliquary", LootMaterial.Gold),
            ("nef", LootMaterial.Gold),
            ("copper", LootMaterial.Bronze),
            ("bronze", LootMaterial.Bronze),
            ("cauldron", LootMaterial.Bronze),
            ("tripod", LootMaterial.Bronze),
            ("oxhide", LootMaterial.Bronze),
            ("astrolabe", LootMaterial.Bronze),
            ("silver", LootMaterial.Metal),
            ("tureen", LootMaterial.Metal),
            ("ewer", LootMaterial.Metal),
            ("plate", LootMaterial.Metal),
            ("tin", LootMaterial.Metal),
            ("cup", LootMaterial.Metal),
            ("sword", LootMaterial.Metal),
            ("mace", LootMaterial.Metal),
            ("axe", LootMaterial.Metal),
            ("helm", LootMaterial.Metal),
            ("armour", LootMaterial.Metal),
            ("crossbow", LootMaterial.Metal),
            ("matchlock", LootMaterial.Metal),
            ("pistol", LootMaterial.Metal),
            ("grenade", LootMaterial.Metal),
            ("shield", LootMaterial.Wood),
            ("chest", LootMaterial.Wood),
            ("coffer", LootMaterial.Wood),
            ("cabinet", LootMaterial.Wood),
            ("relic", LootMaterial.Stone)
        };

        /// <summary>The material for a piece's name (spaces, underscores and case are ignored), stone when nothing matches.</summary>
        public static LootMaterial FromName(string pieceName) => FromName(pieceName, out _);

        /// <summary>As above, and says whether a keyword actually matched.</summary>
        public static LootMaterial FromName(string pieceName, out bool known)
        {
            known = false;
            if (string.IsNullOrEmpty(pieceName))
                return LootMaterial.Stone;
            for (int i = 0; i < Rules.Length; i++)
            {
                if (ContainsLoose(pieceName, Rules[i].Keyword))
                {
                    known = true;
                    return Rules[i].Material;
                }
            }
            return LootMaterial.Stone;
        }

        // Matches "Golden_Goblet", "GoldenGoblet" and "golden goblet" alike, without allocating.
        private static bool ContainsLoose(string name, string keyword)
        {
            int k = 0;
            int matchStart = -1;
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (c == ' ' || c == '_' || c == '-')
                    continue;
                if (char.ToLowerInvariant(c) == keyword[k])
                {
                    if (k == 0)
                        matchStart = i;
                    k++;
                    if (k == keyword.Length)
                        return true;
                }
                else if (k > 0)
                {
                    k = 0;
                    i = matchStart;
                }
            }
            return false;
        }

        /// <summary>The impact sound for a material; only some materials have a light and a heavy version.</summary>
        public static string Impact(LootMaterial material, bool heavy)
        {
            switch (material)
            {
                case LootMaterial.Wood: return heavy ? "phys_impact_wood_heavy" : "phys_impact_wood_light";
                case LootMaterial.Metal: return heavy ? "phys_impact_metal_heavy" : "phys_impact_metal_light";
                case LootMaterial.Bronze: return heavy ? "phys_impact_bronze_heavy" : "phys_impact_bronze_light";
                case LootMaterial.Gold: return heavy ? "phys_impact_gold_heavy" : "phys_impact_gold_light";
                case LootMaterial.Ceramic: return "phys_impact_ceramic_light";
                case LootMaterial.Glass: return "phys_impact_glass_light";
                case LootMaterial.Cloth: return "phys_impact_cloth";
                case LootMaterial.Book: return "phys_impact_book";
                case LootMaterial.Coins: return "phys_impact_coins";
                default: return heavy ? "phys_impact_stone_heavy" : "phys_impact_stone_light";
            }
        }

        /// <summary>The sound of a piece of this material breaking; solid metal and stone have no break of their own and use the ceramic crack.</summary>
        public static string Break(LootMaterial material, string pieceName)
        {
            switch (material)
            {
                case LootMaterial.Glass:
                    return ContainsLoose(pieceName ?? string.Empty, "mirror") ? "phys_break_glass_large" : "phys_break_glass";
                case LootMaterial.Wood: return "phys_break_wood";
                case LootMaterial.Book: return "phys_break_book";
                case LootMaterial.Coins: return "phys_coin_spill";
                case LootMaterial.Ceramic:
                    return ContainsLoose(pieceName ?? string.Empty, "amphora") ? "phys_break_liquid" : "phys_break_ceramic";
                default: return "phys_break_ceramic";
            }
        }

        /// <summary>The dragging loop for a material.</summary>
        public static string Scrape(LootMaterial material)
        {
            switch (material)
            {
                case LootMaterial.Wood: return "phys_scrape_wood_loop";
                case LootMaterial.Metal:
                case LootMaterial.Bronze:
                case LootMaterial.Gold: return "phys_scrape_metal_loop";
                default: return "phys_scrape_stone_loop";
            }
        }

        public const string Roll = "phys_roll_loop";
        public const string Body = "phys_impact_body";
    }

    /// <summary>Which voice a guard has, from the name of the prefab it was made from.</summary>
    public readonly struct GuardVoiceProfile
    {
        public readonly bool Valid;
        public readonly bool Hound;

        /// <summary>The Age key in the sound names: bronze, high, late or powder.</summary>
        public readonly string Age;

        /// <summary>The guard's voice in the sound names: levy, knight, guard and so on.</summary>
        public readonly string Voice;

        /// <summary>The armour layer under the steps: linen, leather, mail, plate or bronze_plate. Null for the hound.</summary>
        public readonly string Gear;

        public GuardVoiceProfile(string age, string voice, string gear, bool hound)
        {
            Valid = true;
            Age = age;
            Voice = voice;
            Gear = gear;
            Hound = hound;
        }
    }

    /// <summary>The voice lines a guard can speak, and the names they play.</summary>
    public enum GuardLine
    {
        Murmur,
        Alert,
        Chase,
        Search,
        Lost,
        Attack,
        Hurt,
        Death,
        Asleep
    }

    public static class GuardVoices
    {
        /// <summary>
        /// The profile for a prefab name ("PalaceLevy" or "PalaceLevy(Clone)"). The four prototype enemies
        /// (ArcRevenant, HexTurret, GildedColossus, CryptRisen, and the older ManAtArms, Sergeant,
        /// Watchman, VaultWarden, SigilWisp, WarHound) get no voice, as audio.md section 3.9 decided.
        /// </summary>
        public static GuardVoiceProfile Resolve(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName))
                return default;
            int clone = prefabName.IndexOf("(Clone)", StringComparison.Ordinal);
            string id = clone >= 0 ? prefabName.Substring(0, clone) : prefabName;
            switch (id)
            {
                case "PalaceLevy": return new GuardVoiceProfile("bronze", "levy", "linen", false);
                case "WallSlinger": return new GuardVoiceProfile("bronze", "slinger", "linen", false);
                case "DendraChampion": return new GuardVoiceProfile("bronze", "champion", "bronze_plate", false);
                case "KeeperOfTheFlame": return new GuardVoiceProfile("bronze", "keeper", "linen", false);
                case "CastleCrossbowman": return new GuardVoiceProfile("high", "crossbowman", "leather", false);
                case "HouseholdKnight": return new GuardVoiceProfile("high", "knight", "mail", false);
                case "LanternWarden": return new GuardVoiceProfile("high", "warden", "mail", false);
                case "AlauntWarHound": return new GuardVoiceProfile("high", "hound", null, true);
                case "SalletHalberdier": return new GuardVoiceProfile("late", "halberdier", "mail", false);
                case "Handgunner": return new GuardVoiceProfile("late", "handgunner", "leather", false);
                case "GothicManAtArms": return new GuardVoiceProfile("late", "manatarms", "plate", false);
                case "Pavisier": return new GuardVoiceProfile("late", "pavisier", "mail", false);
                case "PalaceGuard": return new GuardVoiceProfile("powder", "guard", "plate", false);
                case "Musketeer": return new GuardVoiceProfile("powder", "musketeer", "leather", false);
                case "Cuirassier": return new GuardVoiceProfile("powder", "cuirassier", "plate", false);
                case "Petardier": return new GuardVoiceProfile("powder", "petardier", "leather", false);
                default: return default;
            }
        }

        public static string LineName(GuardVoiceProfile profile, GuardLine line)
        {
            if (!profile.Valid)
                return null;
            if (profile.Hound)
            {
                switch (line)
                {
                    case GuardLine.Alert:
                    case GuardLine.Search: return "vo_hound_growl";
                    case GuardLine.Chase: return "vo_hound_bark";
                    case GuardLine.Attack: return "vo_hound_bite";
                    case GuardLine.Hurt:
                    case GuardLine.Death: return "vo_hound_yelp";
                    default: return null;
                }
            }

            string suffix;
            switch (line)
            {
                case GuardLine.Murmur: suffix = "murmur"; break;
                case GuardLine.Alert: suffix = "alert"; break;
                case GuardLine.Chase: suffix = "chase"; break;
                case GuardLine.Search: suffix = "search"; break;
                case GuardLine.Lost: suffix = "lost"; break;
                case GuardLine.Attack: suffix = "attack"; break;
                case GuardLine.Hurt: suffix = "hurt"; break;
                case GuardLine.Death: suffix = "death"; break;
                default: suffix = "asleep"; break;
            }
            return "vo_" + profile.Age + "_" + profile.Voice + "_" + suffix;
        }

        /// <summary>The line a change of alert state calls for, or null. Lost is a return to patrol from any alerted state.</summary>
        public static GuardLine? LineForStateChange(GuardAlertState previous, GuardAlertState next)
        {
            switch (next)
            {
                case GuardAlertState.Investigating: return GuardLine.Alert;
                case GuardAlertState.Chasing: return GuardLine.Chase;
                case GuardAlertState.Searching: return GuardLine.Search;
                case GuardAlertState.Incapacitated: return GuardLine.Asleep;
                case GuardAlertState.Patrolling:
                    return previous == GuardAlertState.Patrolling || previous == GuardAlertState.Incapacitated
                        ? (GuardLine?)null
                        : GuardLine.Lost;
                default: return null;
            }
        }

        public static string GearSound(GuardVoiceProfile profile) =>
            profile.Valid && profile.Gear != null ? "foley_gear_" + profile.Gear + "_move" : null;
    }
}
