using System;
using Interfaces;
using PurrNet;
using RogueAi.Acoustics;
using UnityEngine;

namespace RogueAi.Castle
{
    /// <summary>
    /// A door in the castle. Three ways through it, and the choice between them is the stealth game:
    /// <list type="bullet">
    /// <item>Unlocked — open it by hand, quietly.</item>
    /// <item>Locked — Porta opens it silently, if you can say the word correctly.</item>
    /// <item>Barred — forced open, which is loud enough to be heard across the ward.</item>
    /// </list>
    ///
    /// The alarm raises the stakes directly: at <see cref="AlarmState.Roused"/> and above the castle
    /// locks its doors, so a raid that got loud on the way in has to spend words getting out.
    /// </summary>
    public class CastleDoor : NetworkBehaviour, IHandOpenable
    {
        [Header("State")]
        [Tooltip("Locked doors need Porta (or a key); an unlocked door opens by hand.")]
        [SerializeField] private bool _locked;

        [Tooltip("Barred doors cannot be opened by hand at all — only forced, or opened by Porta.")]
        [SerializeField] private bool _barred;

        [Header("Noise")]
        [Tooltip("Radius of the noise made by forcing this door, in metres.")]
        [SerializeField] private float _forceNoiseRadius = 12f;

        [Range(0f, 1f)]
        [Tooltip("Loudness of forcing this door.")]
        [SerializeField] private float _forceNoiseStrength = 0.7f;

        [Tooltip("Layers treated as sound-blocking walls.")]
        [SerializeField] private LayerMask _geometryLayers;

        [Header("Presentation")]
        [Tooltip("Transform rotated when the door opens. Defaults to this transform.")]
        [SerializeField] private Transform _hinge;

        [Tooltip("Degrees the hinge swings when open.")]
        [SerializeField] private float _openAngle = 90f;

        private readonly SyncVar<bool> _isOpen = new SyncVar<bool>(false);
        private Quaternion _closedRotation;

        public bool IsOpen => _isOpen.value;
        public bool IsLocked => _locked;
        public bool IsBarred => _barred;

        /// <summary>Raised on state change so audio and UI can react. True when it just opened.</summary>
        public event Action<bool> OpenStateChanged;

        private void Awake()
        {
            if (_hinge == null)
                _hinge = transform;
            _closedRotation = _hinge.localRotation;
        }

        /// <summary>
        /// <see cref="IOpenable"/>: open regardless of lock or bar. This is Porta's entry point —
        /// the spell's whole value is that it ignores both, silently.
        /// </summary>
        public void Open()
        {
            if (IsOpen)
                return;
            SetOpen(true);
        }

        public void Close()
        {
            if (!IsOpen)
                return;
            SetOpen(false);
        }

        /// <summary>
        /// A player pushing the door by hand. Fails on a locked or barred door — which is the moment
        /// the player has to decide whether to spend a word or make a noise.
        /// </summary>
        public bool TryOpenByHand()
        {
            if (IsOpen)
                return true;
            if (_locked || _barred)
                return false;

            SetOpen(true);
            return true;
        }

        /// <summary>
        /// Shoulder it open. Always works, always loud: the noise is emitted through the normal
        /// acoustic path, so it reaches the alarm exactly like any other sound.
        /// </summary>
        public bool ForceOpen()
        {
            if (IsOpen)
                return true;

            SetOpen(true);
            NoiseBroadcaster.Broadcast(transform.position, _forceNoiseRadius, _forceNoiseStrength,
                NoiseType.ItemDrop, ~0, _geometryLayers);
            return true;
        }

        /// <summary>Locks the door. The alarm calls this castle-wide when it reaches Roused.</summary>
        public void Lock() => _locked = true;

        public void Unlock() => _locked = false;

        /// <summary>Bars the door — cannot be opened by hand at all, only forced or spelled open.</summary>
        public void Bar() => _barred = true;

        private void SetOpen(bool open)
        {
            _isOpen.value = open;
            if (_hinge != null)
            {
                _hinge.localRotation = open
                    ? _closedRotation * Quaternion.AngleAxis(_openAngle, Vector3.up)
                    : _closedRotation;
            }
            OpenStateChanged?.Invoke(open);
        }
    }
}
