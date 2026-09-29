using UnityEngine;

namespace Plunderspell.Audio
{
    /// <summary>A fixed set of AudioSources made up front. Nothing is created or destroyed while the game plays.</summary>
    public sealed class AudioSourcePool
    {
        private readonly AudioSource[] _sources;
        private int _cursor;

        public AudioSourcePool(Transform parent, int size)
        {
            _sources = new AudioSource[size];
            for (int i = 0; i < size; i++)
            {
                var go = new GameObject("Voice" + i);
                go.transform.SetParent(parent, false);
                AudioSource source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                _sources[i] = source;
            }
        }

        public int Size => _sources.Length;

        public AudioSource this[int index] => _sources[index];

        /// <summary>The next source that is not playing, going round the pool; when all are busy, the one that has waited longest.</summary>
        public AudioSource Acquire()
        {
            int size = _sources.Length;
            for (int step = 0; step < size; step++)
            {
                int index = (_cursor + step) % size;
                if (!_sources[index].isPlaying)
                {
                    _cursor = (index + 1) % size;
                    return _sources[index];
                }
            }

            AudioSource stolen = _sources[_cursor];
            _cursor = (_cursor + 1) % size;
            return stolen;
        }
    }
}
