using Plunderspell.Acoustics;
using UnityEngine;

namespace Plunderspell.Loot
{
    /// <summary>
    /// The noise loose loot makes for the guards (#238), as opposed to the sound the player hears (ImpactAudio):
    /// a piece hitting the floor, and a piece too heavy to lift being dragged. The heavier it is, the
    /// further and louder; a small trinket put down gently is silent. The server calls this, as the
    /// guards are decided there.
    /// </summary>
    public static class LootNoise
    {
        /// <summary>Impacts slower than this (m/s) are a piece being set down: no noise.</summary>
        public const float MinImpactSpeed = 2f;

        /// <summary>Farthest an impact or a drag is heard, in metres.</summary>
        public const float MaxRadius = 14f;

        /// <summary>Seconds between the scrapes of a heavy piece being dragged.</summary>
        public const float DragScrapeSeconds = 0.8f;

        /// <summary>Slowest a dragged piece scrapes at (m/s).</summary>
        public const float MinDragSpeed = 0.5f;

        private const float DragStrength = 0.3f;

        /// <summary>How far an impact of <paramref name="massKg"/> at <paramref name="speed"/> m/s is heard. 2.4 m for a 1.5 kg ledger dropped, 8 m for a 16 kg chest.</summary>
        public static float ImpactRadius(float massKg, float speed)
        {
            if (speed < MinImpactSpeed)
                return 0f;
            float force = Mathf.Clamp(speed / 4f, 0.5f, 2f);
            return Mathf.Min(2f * Mathf.Sqrt(Mathf.Max(massKg, 0f)) * force, MaxRadius);
        }

        /// <summary>How loud an impact is, 0 to 1: a heavy piece is loud enough to carry through a wall.</summary>
        public static float ImpactStrength(float massKg) => Mathf.Clamp01(0.2f + 0.03f * massKg);

        /// <summary>How far a piece of <paramref name="massKg"/> being dragged is heard: 7.7 m for 15 kg.</summary>
        public static float DragRadius(float massKg) => Mathf.Min(2f * Mathf.Sqrt(Mathf.Max(massKg, 0f)), MaxRadius);

        /// <summary>Broadcasts an impact. Returns how many listeners heard it.</summary>
        public static int BroadcastImpact(Vector3 point, float massKg, float speed)
        {
            float radius = ImpactRadius(massKg, speed);
            if (radius <= 0f)
                return 0;
            return NoiseBroadcaster.Broadcast(point, radius, ImpactStrength(massKg), NoiseType.ItemDrop, ~0, 1);
        }

        /// <summary>Broadcasts the scrape of a heavy piece dragged along the floor. Returns how many listeners heard it.</summary>
        public static int BroadcastDrag(Vector3 point, float massKg) =>
            NoiseBroadcaster.Broadcast(point, DragRadius(massKg), DragStrength, NoiseType.ItemDrop, ~0, 1);
    }
}
