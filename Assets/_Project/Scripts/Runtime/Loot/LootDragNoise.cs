using UnityEngine;

namespace Plunderspell.Loot
{
    /// <summary>
    /// The scrape of a piece too heavy to lift being dragged across the floor (#238): while it is held
    /// and moving it makes a <see cref="LootNoise"/> every <see cref="LootNoise.DragScrapeSeconds"/>.
    /// One per piece, ticked from its fixed update on the machine that decides what guards hear.
    /// </summary>
    public sealed class LootDragNoise
    {
        private float _secondsToNext;

        /// <summary>Advances by <paramref name="deltaTime"/>; returns how many listeners heard a scrape this tick.</summary>
        public int Tick(float deltaTime, Item item, Rigidbody body)
        {
            if (item == null || body == null || !IsDragging(item, body))
            {
                _secondsToNext = 0f;
                return 0;
            }

            _secondsToNext -= deltaTime;
            if (_secondsToNext > 0f)
                return 0;

            _secondsToNext = LootNoise.DragScrapeSeconds;
            return LootNoise.BroadcastDrag(body.position, item.Mass);
        }

        private static bool IsDragging(Item item, Rigidbody body) =>
            item.IsTooHeavyToLift && item.HolderCount > 0 && body.linearVelocity.sqrMagnitude >= LootNoise.MinDragSpeed * LootNoise.MinDragSpeed;
    }
}
