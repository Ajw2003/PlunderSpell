using Plunderspell.Audio.VoiceBank;
using UnityEngine;

namespace Plunderspell.Audio
{
    /// <summary>How each guard archetype is re-voiced, and the seed that makes one guard sound the same on every machine.</summary>
    public static class GuardVoiceProfiles
    {
        public const float UnknownHz = 120f;

        public static float ArchetypeHz(string voiceKey)
        {
            switch (voiceKey)
            {
                case "levy": return 130f;
                case "slinger": return 150f;
                case "champion": return 95f;
                case "keeper": return 115f;
                case "warden": return 120f;
                case "crossbowman": return 140f;
                case "knight": return 100f;
                case "halberdier": return 125f;
                case "handgunner": return 145f;
                case "manatarms": return 98f;
                case "pavisier": return 135f;
                case "guard": return 128f;
                case "musketeer": return 150f;
                case "cuirassier": return 92f;
                case "petardier": return 140f;
                default: return UnknownHz;
            }
        }

        public static DisguiseProfile For(int seed, string voiceKey) => DisguiseProfile.For(seed, ArchetypeHz(voiceKey));

        /// <summary>
        /// A spawned guard's network object id is the same on the host and every client, so it is the seed.
        /// An unspawned guard (no id yet) falls back to its prefab name and where it first stood, which
        /// match across machines only while the castle spawns its guards in the same places.
        /// </summary>
        public static int SeedFor(ulong networkObjectId, string prefabName, Vector3 firstPosition)
        {
            unchecked
            {
                if (networkObjectId != 0)
                    return (int)networkObjectId * 31 + (int)(networkObjectId >> 32);

                uint hash = 2166136261u;
                foreach (char c in prefabName ?? string.Empty)
                    hash = (hash ^ c) * 16777619u;
                hash = (hash ^ (uint)Mathf.RoundToInt(firstPosition.x * 2f)) * 16777619u;
                hash = (hash ^ (uint)Mathf.RoundToInt(firstPosition.y * 2f)) * 16777619u;
                hash = (hash ^ (uint)Mathf.RoundToInt(firstPosition.z * 2f)) * 16777619u;
                return (int)hash;
            }
        }
    }
}
