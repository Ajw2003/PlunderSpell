using UnityEngine;

namespace Plunderspell.Lair
{
    /// <summary>
    /// The unsold pieces lying on the Lair floor, saved per save slot as the asset names of their
    /// loot definitions (one PlayerPrefs string, names joined by '|'). Names, not objects, so this
    /// stays free of the loot assembly; HaulLanding turns them back into pieces.
    /// </summary>
    public static class HaulPileSave
    {
        private const string Key = "HaulPile";
        private const char Separator = '|';

        public static string[] Load(int slot)
        {
            string saved = PlayerPrefs.GetString(SaveSlots.Key(Key, slot), "");
            return saved.Length == 0 ? new string[0] : saved.Split(Separator);
        }

        public static void Save(int slot, string[] ids)
        {
            PlayerPrefs.SetString(SaveSlots.Key(Key, slot), string.Join(Separator.ToString(), ids));
            PlayerPrefs.Save();
        }

        public static void Clear(int slot) => PlayerPrefs.DeleteKey(SaveSlots.Key(Key, slot));
    }
}
