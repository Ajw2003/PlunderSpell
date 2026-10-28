using System.Collections.Generic;
using Plunderspell.Alarm;
using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// Where a guard last knew each player to be, and which way they were going (#487). Once a guard loses the
    /// player it hunts, it keeps searching from that spot along that heading and goes back to patrol only when
    /// 8 seconds have passed with nothing new and the spot is 3 or more rooms from the guard. Investigate asks
    /// the questions; this class holds the facts and makes no moves.
    /// </summary>
    public sealed class GuardLastKnown
    {
        public const float GiveUpSeconds = 8f;
        public const int GiveUpRooms = 3;

        /// <summary>How far along the heading one search leg goes.</summary>
        public const float SearchLegMetres = 6f;

        // A player must move this far from the anchor before the heading is worked out again.
        private const float HeadingMinMetres = 0.5f;

        private struct Record
        {
            public Vector3 Spot;
            public Vector3 Anchor;
            public Vector3 Heading;
        }

        private readonly Dictionary<Transform, Record> _records = new Dictionary<Transform, Record>();
        private Transform _hunted;
        private Vector3 _lastHeardSpot;
        private bool _heardLast;
        private float _quietSeconds;

        public bool IsHunting { get; private set; }

        /// <summary>Seconds since anything new about the hunted player. Only meaningful while <see cref="IsHunting"/>.</summary>
        public float QuietSeconds => _quietSeconds;

        /// <summary>The last-known spot of the hunted player. Only meaningful while <see cref="IsHunting"/>.</summary>
        public Vector3 Spot => _heardLast ? _lastHeardSpot : (_hunted != null && _records.TryGetValue(_hunted, out Record record) ? record.Spot : Vector3.zero);

        /// <summary>The flat direction the hunted player was last going, or zero when it is not known.</summary>
        public Vector3 Heading => _hunted != null && _records.TryGetValue(_hunted, out Record record) ? record.Heading : Vector3.zero;

        /// <summary>The guard saw <paramref name="player"/> at <paramref name="spot"/>.</summary>
        public void Sighted(Transform player, Vector3 spot)
        {
            bool known = _records.TryGetValue(player, out Record record);
            record.Spot = spot;
            if (!known)
                record.Anchor = spot;
            Vector3 moved = spot - record.Anchor;
            moved.y = 0f;
            if (moved.magnitude >= HeadingMinMetres)
            {
                record.Heading = moved.normalized;
                record.Anchor = spot;
            }
            _records[player] = record;
            _hunted = player;
            _heardLast = false;
            _quietSeconds = 0f;
        }

        /// <summary>The guard lost sight of <paramref name="player"/> and starts hunting them.</summary>
        public void Hunt(Transform player)
        {
            _hunted = player;
            IsHunting = player != null;
            _quietSeconds = 0f;
        }

        /// <summary>A noise or cry pointed somewhere new: the hunt restarts its quiet count and searches from there.</summary>
        public void Heard(Vector3 spot, bool hasHeading, Vector3 heading)
        {
            if (!IsHunting)
                return;

            _lastHeardSpot = spot;
            _heardLast = true;
            _quietSeconds = 0f;
            if (hasHeading && _hunted != null && _records.TryGetValue(_hunted, out Record record))
            {
                record.Heading = heading;
                _records[_hunted] = record;
            }
        }

        public void Tick(float deltaTime)
        {
            if (IsHunting)
                _quietSeconds += deltaTime;
        }

        /// <summary>True when 8 s have passed with nothing new and the spot is 3 or more rooms from the guard (no map or no route counts as far).</summary>
        public bool MayGiveUp(IGuardNavigationMap map, Vector3 guardPosition)
        {
            if (_quietSeconds < GiveUpSeconds)
                return false;
            // shortcut: a guard whose legs dead-end near the spot would hunt for ever; 40 quiet seconds ends it. Revisit after aj's QA.
            if (_quietSeconds >= GiveUpSeconds * 5f)
                return true;

            int rooms = map != null ? map.RoomsBetween(guardPosition, Spot) : -1;
            return rooms < 0 || rooms >= GiveUpRooms;
        }

        /// <summary>The next point to search, one leg along the heading from <paramref name="from"/>. False when no heading is known.</summary>
        public bool TryNextLeg(Vector3 from, out Vector3 point)
        {
            Vector3 heading = Heading;
            point = from + heading * SearchLegMetres;
            return heading.sqrMagnitude > 0.0001f;
        }

        public void Forget() => IsHunting = false;
    }
}
