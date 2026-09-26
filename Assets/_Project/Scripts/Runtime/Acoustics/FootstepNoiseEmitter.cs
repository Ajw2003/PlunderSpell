using Plunderspell.Voice;
using UnityEngine;

namespace Plunderspell.Acoustics
{
    /// <summary>Movement stance, drives footstep loudness.</summary>
    public enum MoveStance
    {
        Crouch,
        Walk,
        Run
    }

    /// <summary>
    /// Attach to the player root. Turns locomotion and voice casting into <see cref="NoiseEvent"/>s via
    /// an <see cref="AcousticEmitter"/>:
    /// <list type="bullet">
    /// <item>Footsteps — radius 1.5 (crouch) / 4.0 (walk) / 8.0 (run). Call <see cref="OnFootstep"/>
    /// from an animation event, or let the component poll a footstep <see cref="AudioSource"/>.</item>
    /// <item>Voice — on each recognised phrase, emits a VoiceCast noise scaled by the spoken
    /// <see cref="CastVolume"/> (Whisper 0.5 / Normal 5.0 / Shout 12.0).</item>
    /// </list>
    ///
    /// PurrNet/Voice note: <c>PushToCastController</c> exposes only a boolean casting-state event and
    /// does not carry loudness, so voice noise is driven off
    /// <see cref="IVoiceInputService.OnPhraseRecognized"/> (whose <c>VoiceRecognitionResult.Volume</c>
    /// carries the <see cref="CastVolume"/>) rather than a nonexistent
    /// <c>PushToCastController.OnCastStarted</c>.
    /// </summary>
    [RequireComponent(typeof(AcousticEmitter))]
    public class FootstepNoiseEmitter : MonoBehaviour
    {
        [Header("Footstep radii (metres)")]
        [SerializeField] private float _crouchRadius = 1.5f;
        [SerializeField] private float _walkRadius = 4.0f;
        [SerializeField] private float _runRadius = 8.0f;

        [Header("Footstep strength")]
        [Range(0f, 1f)]
        [SerializeField] private float _footstepStrength = 0.4f;

        [Header("Voice radii (metres)")]
        [SerializeField] private float _whisperRadius = 0.5f;
        [SerializeField] private float _normalRadius = 5.0f;
        [SerializeField] private float _shoutRadius = 12.0f;

        [Header("Voice strength")]
        [Range(0f, 1f)]
        [SerializeField] private float _voiceStrength = 0.8f;

        [Header("Optional audio-driven footsteps")]
        [Tooltip("If set, a footstep is emitted each time this source starts playing a clip.")]
        [SerializeField] private AudioSource _footstepAudio;

        private AcousticEmitter _emitter;
        private IVoiceInputService _voiceService;
        private bool _wasAudioPlaying;

        private void Awake()
        {
            _emitter = GetComponent<AcousticEmitter>();
        }

        private void OnEnable()
        {
            _voiceService = VoiceServiceLocator.Current;
            if (_voiceService != null)
                _voiceService.OnPhraseRecognized += HandlePhraseRecognized;
        }

        private void OnDisable()
        {
            if (_voiceService != null)
                _voiceService.OnPhraseRecognized -= HandlePhraseRecognized;
        }

        private void Update()
        {
            // Fallback footstep source: fire once each time the audio source begins playing.
            if (_footstepAudio == null)
                return;

            bool playing = _footstepAudio.isPlaying;
            if (playing && !_wasAudioPlaying)
                OnFootstep(MoveStance.Walk);
            _wasAudioPlaying = playing;
        }

        /// <summary>Emit a footstep noise for the given stance. Wire to animation events.</summary>
        public void OnFootstep(MoveStance stance)
        {
            _emitter.NoiseType = NoiseType.Footstep;
            _emitter.EmitNoise(RadiusForStance(stance), _footstepStrength);
        }

        private float RadiusForStance(MoveStance stance)
        {
            switch (stance)
            {
                case MoveStance.Crouch: return _crouchRadius;
                case MoveStance.Run: return _runRadius;
                default: return _walkRadius;
            }
        }

        private void HandlePhraseRecognized(VoiceRecognitionResult result)
        {
            EmitVoiceCast(result.Volume);
        }

        /// <summary>Emit a VoiceCast noise scaled by the spoken volume.</summary>
        public void EmitVoiceCast(CastVolume volume)
        {
            _emitter.NoiseType = NoiseType.VoiceCast;
            _emitter.EmitNoise(RadiusForVolume(volume), _voiceStrength);
        }

        private float RadiusForVolume(CastVolume volume)
        {
            switch (volume)
            {
                case CastVolume.Whisper: return _whisperRadius;
                case CastVolume.Shout: return _shoutRadius;
                default: return _normalRadius;
            }
        }
    }
}
