using System.Collections.Generic;
using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// The points one patrol round walks, and which one the guard is heading for. A plain list with a
    /// cursor: points are added when a round is planned, the cursor moves on at each arrival, and a point
    /// that cannot be reached is dropped. It never allocates once its list has grown to the round size.
    /// </summary>
    public sealed class GuardPatrolRoute
    {
        private readonly List<Vector3> _points = new List<Vector3>();
        private int _cursor;

        public int Count => _points.Count;

        /// <summary>True when every point has been visited (or there are none), so a fresh round is due.</summary>
        public bool IsFinished => _cursor >= _points.Count;

        public Vector3 Current => _points[_cursor];

        public Vector3 this[int index] => _points[index];

        /// <summary>Starts a new round: forgets the old points and puts the cursor back at the first.</summary>
        public void Clear()
        {
            _points.Clear();
            _cursor = 0;
        }

        public void Add(Vector3 point) => _points.Add(point);

        /// <summary>The guard reached the current point; head for the next.</summary>
        public void Advance() => _cursor++;

        /// <summary>Forgets the current point for this round. The next point slides into its place.</summary>
        public void DropCurrent() => _points.RemoveAt(_cursor);

        /// <summary>Swaps the current point for another, for a point that was blocked by something that may clear.</summary>
        public void ReplaceCurrent(Vector3 point) => _points[_cursor] = point;

        /// <summary>True when <paramref name="point"/> is within <paramref name="spacing"/> of a point already on the route.</summary>
        public bool HasPointNear(Vector3 point, float spacing)
        {
            float spacingSquared = spacing * spacing;
            for (int i = 0; i < _points.Count; i++)
            {
                if ((_points[i] - point).sqrMagnitude < spacingSquared)
                    return true;
            }
            return false;
        }
    }
}
