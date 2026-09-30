using System;
using Plunderspell.Core;
using Plunderspell.Voice;
using PurrNet;
using UnityEngine;

namespace Plunderspell.Acoustics
{
    /// <summary>What a spoken line did, told back to the speaker for a caption.</summary>
    public readonly struct ChatterOutcome
    {
        public readonly string Transcript;
        public readonly CastVolume Volume;
        public readonly int GuardsWhoUnderstood;

        public ChatterOutcome(string transcript, CastVolume volume, int guardsWhoUnderstood)
        {
            Transcript = transcript;
            Volume = volume;
            GuardsWhoUnderstood = guardsWhoUnderstood;
        }
    }

    /// <summary>
    /// Carries ordinary talk (opt-in, see <see cref="AudioInputSettings.GuardsHearChatter"/>) from the
    /// local player's microphone to the host, where it becomes a <see cref="NoiseType.Speech"/> noise.
    /// Only the written words travel; audio never leaves the machine. Mirrors SpellCastingSystem's
    /// owner-only subscribe and owner → [ServerRpc] → server-resolves route.
    /// </summary>
    public class PlayerChatterRelay : NetworkBehaviour
    {
        /// <summary>Transcripts longer than this are cut on the server; a client is not trusted.</summary>
        public const int MaxTranscriptLength = 200;

        [Header("Radius (metres) by how loudly it was said")]
        [SerializeField] private float _whisperRadius = 1.5f;
        [SerializeField] private float _normalRadius = 6f;
        [SerializeField] private float _shoutRadius = 14f;

        [Header("Strength by how loudly it was said")]
        [SerializeField] private float _whisperStrength = 0.3f;
        [SerializeField] private float _normalStrength = 0.5f;
        [SerializeField] private float _shoutStrength = 0.9f;

        [Header("Layers")]
        [SerializeField] private LayerMask _listenerLayers = ~0;
        [SerializeField] private LayerMask _geometryLayers;

        /// <summary>Raised on the speaker's machine when a line has been resolved.</summary>
        public static event Action<ChatterOutcome> ChatterResolved;

        private IChatterSource _source;
        private bool _subscribed;
        private bool _warnedNoSource;

        public static float RadiusFor(CastVolume volume, float whisper = 1.5f, float normal = 6f, float shout = 14f) =>
            volume == CastVolume.Whisper ? whisper : volume == CastVolume.Shout ? shout : normal;

        public static float StrengthFor(CastVolume volume, float whisper = 0.3f, float normal = 0.5f, float shout = 0.9f) =>
            volume == CastVolume.Whisper ? whisper : volume == CastVolume.Shout ? shout : normal;

        protected override void OnSpawned()
        {
            base.OnSpawned();
            if (isOwner)
                Subscribe();
        }

        // Offline there is no spawn event.
        private void Start()
        {
            if (!isSpawned)
                Subscribe();
        }

        protected override void OnDespawned()
        {
            base.OnDespawned();
            Unsubscribe();
        }

        private void OnDisable() => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed)
                return;

            _source = VoiceServiceLocator.Current as IChatterSource;
            if (_source == null)
            {
                if (!_warnedNoSource && AudioInputSettings.GuardsHearChatter)
                {
                    _warnedNoSource = true;
                    Debug.LogWarning("[Chatter] No speech recognizer on this machine; guards cannot hear speech.");
                }
                return;
            }

            _source.ChatterHeard += HandleChatter;
            AudioInputSettings.GuardsHearChatterChanged += HandleSettingChanged;
            _source.ChatterEnabled = AudioInputSettings.GuardsHearChatter;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
                return;
            _subscribed = false;
            _source.ChatterHeard -= HandleChatter;
            AudioInputSettings.GuardsHearChatterChanged -= HandleSettingChanged;
            _source.ChatterEnabled = false;
        }

        private void HandleSettingChanged(bool on)
        {
            if (_source != null)
                _source.ChatterEnabled = on;
        }

        private void HandleChatter(ChatterReport report)
        {
            if (!AudioInputSettings.GuardsHearChatter)
                return;

            if (!isSpawned)
            {
                int understood = Resolve(report.Transcript, report.Volume, MouthPosition);
                ChatterResolved?.Invoke(new ChatterOutcome(report.Transcript, report.Volume, understood));
                return;
            }

            // The volume crosses the network as a byte; see SpellCastingSystem.ServerCast.
            ReportChatter(report.Transcript, (byte)report.Volume, MouthPosition);
        }

        [ServerRpc(requireOwnership: true)]
        private void ReportChatter(string transcript, byte volumeByte, Vector3 mouth, RPCInfo info = default)
        {
            if (string.IsNullOrWhiteSpace(transcript))
                return;
            if (transcript.Length > MaxTranscriptLength)
                transcript = transcript.Substring(0, MaxTranscriptLength);

            var volume = (CastVolume)volumeByte;
            int understood = Resolve(transcript, volume, mouth);
            TellSpeaker(info.sender, transcript, volumeByte, understood);
        }

        [TargetRpc]
        private void TellSpeaker(PlayerID speaker, string transcript, byte volumeByte, int guardsWhoUnderstood)
        {
            ChatterResolved?.Invoke(new ChatterOutcome(transcript, (CastVolume)volumeByte, guardsWhoUnderstood));
        }

        /// <summary>
        /// Turns a spoken line into a noise at <paramref name="origin"/> and returns how many guards took
        /// in the words. Public and network-free so tests need no transport.
        /// </summary>
        public int Resolve(string transcript, CastVolume volume, Vector3 origin) =>
            NoiseBroadcaster.BroadcastSpeech(origin, RadiusFor(volume, _whisperRadius, _normalRadius, _shoutRadius),
                StrengthFor(volume, _whisperStrength, _normalStrength, _shoutStrength), transcript,
                _listenerLayers, _geometryLayers);

        private Vector3 MouthPosition
        {
            get
            {
                Camera eye = GetComponentInChildren<Camera>();
                return eye != null ? eye.transform.position : transform.position + Vector3.up * 1.6f;
            }
        }
    }
}
