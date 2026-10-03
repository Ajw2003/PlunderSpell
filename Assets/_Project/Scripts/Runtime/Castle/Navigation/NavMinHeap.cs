using System;

namespace Plunderspell.Castle
{
    /// <summary>
    /// Binary min-heap of (priority, value) pairs for the nav searches. There is no decrease-key:
    /// a node is pushed again with its better priority and the caller skips the stale entry when it
    /// pops. It doubles its arrays when full, so it only allocates while a search is warming up.
    /// </summary>
    public sealed class NavMinHeap
    {
        private float[] _priorities = new float[8192];
        private int[] _values = new int[8192];

        /// <summary>Entries currently held, stale ones included.</summary>
        public int Count { get; private set; }

        /// <summary>Empties the heap without releasing its arrays.</summary>
        public void Clear()
        {
            Count = 0;
        }

        /// <summary>Adds a value; the lowest priority pops first.</summary>
        public void Push(float priority, int value)
        {
            if (Count == _priorities.Length)
            {
                Array.Resize(ref _priorities, Count * 2);
                Array.Resize(ref _values, Count * 2);
            }
            int hole = Count++;
            while (hole > 0)
            {
                int parent = (hole - 1) >> 1;
                if (_priorities[parent] <= priority)
                    break;
                _priorities[hole] = _priorities[parent];
                _values[hole] = _values[parent];
                hole = parent;
            }
            _priorities[hole] = priority;
            _values[hole] = value;
        }

        /// <summary>Removes and returns the value with the lowest priority.</summary>
        public int Pop()
        {
            int lowest = _values[0];
            Count--;
            if (Count > 0)
                SiftDown(_priorities[Count], _values[Count]);
            return lowest;
        }

        // Drops the last entry into the root's hole and lets it sink to where it belongs.
        private void SiftDown(float priority, int value)
        {
            int hole = 0;
            while (true)
            {
                int child = hole * 2 + 1;
                if (child >= Count)
                    break;
                if (child + 1 < Count && _priorities[child + 1] < _priorities[child])
                    child++;
                if (_priorities[child] >= priority)
                    break;
                _priorities[hole] = _priorities[child];
                _values[hole] = _values[child];
                hole = child;
            }
            _priorities[hole] = priority;
            _values[hole] = value;
        }
    }
}
