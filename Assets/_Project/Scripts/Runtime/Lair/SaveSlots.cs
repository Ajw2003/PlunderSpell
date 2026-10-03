using UnityEngine;

namespace Plunderspell.Lair
{
    /// <summary>
    /// Which of the campaign saves is in use. Each slot keeps its own debt, gold and era under its own
    /// PlayerPrefs keys; <see cref="LairHubManager"/> reads and writes whichever slot is active.
    /// </summary>
    public static class SaveSlots
    {
        public const int Count = 3;

        private const string ActiveKey = "Save.ActiveSlot";

        /// <summary>The slot in use, 1 to <see cref="Count"/>.</summary>
        public static int Active
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(ActiveKey, 1), 1, Count);
            set
            {
                PlayerPrefs.SetInt(ActiveKey, Mathf.Clamp(value, 1, Count));
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// The PlayerPrefs key for <paramref name="baseKey"/> in <paramref name="slot"/>. Slot 1 keeps
        /// the keys saves used before slots existed, so an existing campaign is slot 1.
        /// </summary>
        public static string Key(string baseKey, int slot) => slot <= 1 ? baseKey : "Slot" + slot + "." + baseKey;
    }
}
