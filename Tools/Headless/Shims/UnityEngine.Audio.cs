// Headless shim for the AudioMixer surface the audio layer uses. A mixer is a bag of exposed
// parameters: SetFloat/GetFloat round-trip, nothing is mixed.
using System.Collections.Generic;

namespace UnityEngine.Audio
{
    public class AudioMixerGroup : Object { }

    public class AudioMixer : Object
    {
        private readonly Dictionary<string, float> _parameters = new Dictionary<string, float>();

        public bool SetFloat(string name, float value) { _parameters[name] = value; return true; }
        public bool GetFloat(string name, out float value) => _parameters.TryGetValue(name, out value);
    }
}
