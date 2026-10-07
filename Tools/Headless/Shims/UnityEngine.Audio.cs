// Headless shim for the AudioMixer surface the audio layer uses. A mixer is a bag of exposed
// parameters: SetFloat/GetFloat round-trip, nothing is mixed.
using System.Collections.Generic;

namespace UnityEngine.Audio
{
    public class AudioMixerGroup : Object { public AudioMixer audioMixer; }
    public class AudioMixerSnapshot : Object { }

    public class AudioMixer : Object
    {
        private readonly Dictionary<string, float> _parameters = new Dictionary<string, float>();

        /// <summary>No mixer asset is ever loaded headlessly, so there are no groups to match.</summary>
        public AudioMixerGroup[] FindMatchingGroups(string subPath) => new AudioMixerGroup[0];
        /// <summary>Approximation: snapshots are not modelled; any name resolves to a placeholder.</summary>
        public AudioMixerSnapshot FindSnapshot(string name) => new AudioMixerSnapshot { name = name };
        public bool ClearFloat(string name) => _parameters.Remove(name);
        public bool SetFloat(string name, float value) { _parameters[name] = value; return true; }
        public bool GetFloat(string name, out float value) => _parameters.TryGetValue(name, out value);
    }
}
