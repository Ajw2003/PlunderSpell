using UnityEngine;

namespace Plunderspell.Audio
{
    /// <summary>
    /// A few pooled looping sources for sounds that last while something is true: a piece being dragged,
    /// a piece rolling, a hound panting. Owners call <see cref="Drive"/> every frame they want a loop
    /// heard; a slot nobody drove this frame fades out and is freed. Nothing is created after the
    /// constructor.
    /// </summary>
    public sealed class LoopBus
    {
        private const float FadeInSeconds = 0.1f;
        private const float FadeOutSeconds = 0.25f;

        private readonly AudioSource[] _sources;
        private readonly int[] _keys;
        private readonly float[] _target;
        private readonly bool[] _driven;
        private readonly bool[] _used;
        private readonly float[] _baseVolume;

        public LoopBus(Transform parent, int size)
        {
            _sources = new AudioSource[size];
            _keys = new int[size];
            _target = new float[size];
            _driven = new bool[size];
            _used = new bool[size];
            _baseVolume = new float[size];
            for (int i = 0; i < size; i++)
            {
                var go = new GameObject("Loop" + i);
                go.transform.SetParent(parent, false);
                AudioSource source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = true;
                _sources[i] = source;
            }
        }

        public int Size => _sources.Length;

        /// <summary>How many slots are in use, including ones fading out.</summary>
        public int Active
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _used.Length; i++)
                {
                    if (_used[i])
                        count++;
                }
                return count;
            }
        }

        public AudioSource SourceFor(int key)
        {
            for (int i = 0; i < _used.Length; i++)
            {
                if (_used[i] && _keys[i] == key)
                    return _sources[i];
            }
            return null;
        }

        /// <summary>Asks for the loop <paramref name="entry"/> to be heard for <paramref name="key"/> at <paramref name="level"/> (0 to 1) at <paramref name="position"/>.</summary>
        public void Drive(int key, SoundEntry entry, float level, Vector3 position)
        {
            if (entry == null || entry.Clips.Length == 0 || entry.Clips[0] == null || !SoundFocus.Allows(entry.Name))
                return;

            int slot = -1;
            for (int i = 0; i < _used.Length; i++)
            {
                if (_used[i] && _keys[i] == key)
                {
                    slot = i;
                    break;
                }
            }

            if (slot < 0)
            {
                for (int i = 0; i < _used.Length; i++)
                {
                    if (!_used[i])
                    {
                        slot = i;
                        break;
                    }
                }
                if (slot < 0)
                    return; // every slot busy: a new loop waits for one to free rather than cutting an old one

                AudioSource source = _sources[slot];
                source.clip = entry.Clips[0];
                source.outputAudioMixerGroup = entry.Group;
                source.spatialBlend = entry.ThreeD ? 1f : 0f;
                source.minDistance = 2f;
                source.maxDistance = 25f;
                source.volume = 0f;
                source.Play();
                _used[slot] = true;
                _keys[slot] = key;
                _baseVolume[slot] = entry.Volume;
            }

            _driven[slot] = true;
            _target[slot] = level * _baseVolume[slot];
            _sources[slot].transform.position = position;
        }

        /// <summary>Call once per frame after every owner has driven its loops.</summary>
        public void Tick(float deltaTime)
        {
            for (int i = 0; i < _used.Length; i++)
            {
                if (!_used[i])
                    continue;

                AudioSource source = _sources[i];
                float target = _driven[i] ? _target[i] : 0f;
                float rate = target > source.volume ? 1f / FadeInSeconds : 1f / FadeOutSeconds;
                source.volume = Mathf.MoveTowards(source.volume, target, rate * deltaTime);
                _driven[i] = false;

                if (source.volume <= 0f && target <= 0f)
                {
                    source.Stop();
                    _used[i] = false;
                }
            }
        }
    }
}
