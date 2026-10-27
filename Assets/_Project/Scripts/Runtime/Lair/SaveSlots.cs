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

        /// <summary>The slot the automated checks play in. Above <see cref="Count"/>, so the Main Menu never offers it.</summary>
        public const int TestSlot = 99;

        private const string ActiveKey = "Save.ActiveSlot";

        /// <summary>The slot in use, 1 to <see cref="Count"/>.</summary>
        public static int Active
        {
            get => Valid(PlayerPrefs.GetInt(ActiveKey, 1));
            set
            {
                PlayerPrefs.SetInt(ActiveKey, Valid(value));
                PlayerPrefs.Save();
            }
        }

        private static int Valid(int slot) => slot == TestSlot ? slot : Mathf.Clamp(slot, 1, Count);

        /// <summary>
        /// The PlayerPrefs key for <paramref name="baseKey"/> in <paramref name="slot"/>. Slot 1 keeps
        /// the keys saves used before slots existed, so an existing campaign is slot 1.
        /// </summary>
        public static string Key(string baseKey, int slot) => slot <= 1 ? baseKey : "Slot" + slot + "." + baseKey;
    }
}
