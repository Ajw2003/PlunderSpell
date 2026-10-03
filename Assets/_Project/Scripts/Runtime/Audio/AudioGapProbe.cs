using System.Text;
using Plunderspell.Core;
using UnityEngine;

namespace Plunderspell.Audio
{
    /// <summary>
    /// Diagnostic: logs every stretch where the listener's output is silent, every main-thread hitch,
    /// and every game state change, each with what the music layers were doing. Not added by the game;
    /// attach it from an eval (<c>AudioDirector.Instance.gameObject.AddComponent&lt;AudioGapProbe&gt;()</c>).
    /// </summary>
    public sealed class AudioGapProbe : MonoBehaviour
    {
        private const float SilenceLevel = 0.0005f;
        private const float MinGapSeconds = 0.25f;
        private const float HitchSeconds = 0.1f;

        private readonly float[] _samples = new float[1024];
        private readonly StringBuilder _text = new StringBuilder(512);
        private float _silentSince = -1f;
        private GameState _lastState;
        private MusicDirector _music;

        private void Start()
        {
            _music = GetComponent<MusicDirector>();
            _lastState = GameServices.GameState.CurrentState;
            Debug.Log("[AudioProbe] started. " + Describe());
        }

        private void Update()
        {
            float now = Time.realtimeSinceStartup;
            if (Time.unscaledDeltaTime > HitchSeconds)
                Debug.Log($"[AudioProbe] hitch {Time.unscaledDeltaTime:F2}s at {now:F2}. {Describe()}");

            GameState state = GameServices.GameState.CurrentState;
            if (state != _lastState)
            {
                Debug.Log($"[AudioProbe] state {_lastState} -> {state} at {now:F2}. {Describe()}");
                _lastState = state;
            }

            AudioListener.GetOutputData(_samples, 0);
            float peak = 0f;
            foreach (float sample in _samples)
                peak = Mathf.Max(peak, Mathf.Abs(sample));

            if (peak < SilenceLevel)
            {
                if (_silentSince < 0f)
                    _silentSince = now;
            }
            else if (_silentSince >= 0f)
            {
                float gap = now - _silentSince;
                if (gap >= MinGapSeconds)
                    Debug.Log($"[AudioProbe] silent {gap:F2}s from {_silentSince:F2} to {now:F2}. {Describe()}");
                _silentSince = -1f;
            }
        }

        private string Describe()
        {
            _text.Clear();
            _text.Append("state ").Append(GameServices.GameState.CurrentState);
            _text.Append(" listenerVol ").Append(AudioListener.volume.ToString("F2"));
            _text.Append(" paused ").Append(AudioListener.pause);
            _text.Append(" dsp ").Append(AudioSettings.dspTime.ToString("F2"));
            if (_music != null && _music.Layers != null)
            {
                _text.Append(" bed ").Append(_music.CurrentBed ?? "-").Append(" stems ").Append(_music.StemsPlaying);
                foreach (AudioSource layer in _music.Layers)
                {
                    if (layer.clip == null)
                        continue;
                    _text.Append(" | ").Append(layer.name).Append(' ').Append(layer.clip.name)
                        .Append(" load ").Append(layer.clip.loadState)
                        .Append(" playing ").Append(layer.isPlaying)
                        .Append(" vol ").Append(layer.volume.ToString("F2"))
                        .Append(" t ").Append(layer.time.ToString("F2"));
                }
            }
            return _text.ToString();
        }
    }
}
