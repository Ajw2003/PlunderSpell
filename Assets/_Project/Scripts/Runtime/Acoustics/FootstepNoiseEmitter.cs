using Code.Scripts.EventSystems;
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

        [Header("Landing (metres, strength)")]
        [SerializeField] private float _landingRadius = 6.0f;
        [SerializeField] private float _heavyLandingRadius = 12.0f;
        [Range(0f, 1f)]
        [SerializeField] private float _landingStrength = 0.5f;
        [Range(0f, 1f)]
        [SerializeField] private float _heavyLandingStrength = 0.8f;

        /// <summary>Paces under this are a crouch, from <see cref="RunFromSpeed"/> a run, between a walk (m/s).</summary>
        public const float CrouchBelowSpeed = 2.2f;
        public const float RunFromSpeed = 4.5f;

        /// <summary>Falls slower than this (m/s) make no noise; from <see cref="HeavyLandingFallSpeed"/> it is a crash.</summary>
        public const float LandingMinFallSpeed = 2.5f;
        public const float HeavyLandingFallSpeed = 10f;

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
                EventManager.Instance?.Subscribe(this, (PhraseRecognized e) => HandlePhraseRecognized(e.Result));
        }

        private void OnDisable()
        {
            EventManager.Instance?.Unsubscribe<PhraseRecognized>(this);
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

        /// <summary>The stance a pace in m/s amounts to: slow is a crouch, fast is a run. The same breaks as the footstep sounds (StepMath).</summary>
        public static MoveStance StanceForSpeed(float metresPerSecond)
        {
            if (metresPerSecond < CrouchBelowSpeed)
                return MoveStance.Crouch;
            return metresPerSecond < RunFromSpeed ? MoveStance.Walk : MoveStance.Run;
        }

        /// <summary>
        /// Emit the noise of landing from a fall at <paramref name="fallSpeed"/> m/s: nothing for a small
        /// step down, a thud for a jump, a crash for a long drop (#238).
        /// </summary>
        public void OnLanding(float fallSpeed)
        {
            if (fallSpeed < LandingMinFallSpeed)
                return;

            bool heavy = fallSpeed >= HeavyLandingFallSpeed;
            _emitter.NoiseType = NoiseType.Footstep;
            _emitter.EmitNoise(heavy ? _heavyLandingRadius : _landingRadius, heavy ? _heavyLandingStrength : _landingStrength);
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
