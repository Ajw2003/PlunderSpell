using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Plunderspell.Lair
{
    /// <summary>
    /// The unsold pieces lying on the Lair floor, saved per save slot as one PlayerPrefs string: entries
    /// joined by '|', each the asset name of the loot definition, optionally followed by '@' and the
    /// world pose "x,y,z,qx,qy,qz,qw". Names, not objects, so this stays free of the loot assembly;
    /// HaulLanding turns them back into pieces. A name-only entry is the older format and still loads.
    /// </summary>
    public static class HaulPileSave
    {
        private const string Key = "HaulPile";
        private const char Separator = '|';
        private const char PoseMark = '@';
        private const int PoseFloats = 7;

        public static HaulPileEntry[] LoadEntries(int slot)
        {
            string saved = PlayerPrefs.GetString(SaveSlots.Key(Key, slot), "");
            if (saved.Length == 0)
                return new HaulPileEntry[0];

            string[] parts = saved.Split(Separator);
            var entries = new HaulPileEntry[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                entries[i] = Parse(parts[i]);
            return entries;
        }

        public static void SaveEntries(int slot, IReadOnlyList<HaulPileEntry> entries)
        {
            var parts = new string[entries.Count];
            for (int i = 0; i < parts.Length; i++)
                parts[i] = Format(entries[i]);
            PlayerPrefs.SetString(SaveSlots.Key(Key, slot), string.Join(Separator.ToString(), parts));
            PlayerPrefs.Save();
        }

        /// <summary>The names alone, without poses.</summary>
        public static string[] Load(int slot)
        {
            HaulPileEntry[] entries = LoadEntries(slot);
            var names = new string[entries.Length];
            for (int i = 0; i < names.Length; i++)
                names[i] = entries[i].Name;
            return names;
        }

        /// <summary>Saves names with no poses, so the pieces will be placed by the grid.</summary>
        public static void Save(int slot, string[] ids)
        {
            var entries = new HaulPileEntry[ids.Length];
            for (int i = 0; i < ids.Length; i++)
                entries[i] = new HaulPileEntry(ids[i]);
            SaveEntries(slot, entries);
        }

        public static void Clear(int slot) => PlayerPrefs.DeleteKey(SaveSlots.Key(Key, slot));

        private static string Format(HaulPileEntry e)
        {
            if (!e.HasPose)
                return e.Name;
            Vector3 p = e.Position;
            Quaternion q = e.Rotation;
            return string.Format(CultureInfo.InvariantCulture, "{0}{1}{2:R},{3:R},{4:R},{5:R},{6:R},{7:R},{8:R}",
                e.Name, PoseMark, p.x, p.y, p.z, q.x, q.y, q.z, q.w);
        }

        // A pose that does not parse is dropped, not the piece: it falls back to the grid.
        private static HaulPileEntry Parse(string part)
        {
            int mark = part.IndexOf(PoseMark);
            if (mark < 0)
                return new HaulPileEntry(part);

            string name = part.Substring(0, mark);
            string[] f = part.Substring(mark + 1).Split(',');
            if (f.Length != PoseFloats)
                return new HaulPileEntry(name);
            var v = new float[PoseFloats];
            for (int i = 0; i < PoseFloats; i++)
            {
                if (!float.TryParse(f[i], NumberStyles.Float, CultureInfo.InvariantCulture, out v[i]))
                    return new HaulPileEntry(name);
            }
            return new HaulPileEntry(name, new Vector3(v[0], v[1], v[2]), new Quaternion(v[3], v[4], v[5], v[6]));
        }
    }
}
