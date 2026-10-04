using System;
using Interfaces;
using PurrNet;
using Plunderspell.Acoustics;
using UnityEngine;

namespace Plunderspell.Castle
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

        // Locked: needs Porta (or a key). Barred: cannot be opened by hand at all, only forced or spelled open.
        // All three are synced so every peer sees the same door; only the server (or an offline door) writes them.
        private readonly SyncVar<bool> _isOpen = new SyncVar<bool>(false);
        private readonly SyncVar<bool> _locked = new SyncVar<bool>(false);
        private readonly SyncVar<bool> _barred = new SyncVar<bool>(false);
        private Quaternion _closedRotation;

        public bool IsOpen => _isOpen.value;
        public bool IsLocked => _locked.value;
        public bool IsBarred => _barred.value;

        // Offline (tests, solo) there is no server to ask, so the door decides for itself.
        private bool DecidesLocally => !isSpawned || isServer;

        /// <summary>Raised on state change so audio and UI can react. True when it just opened.</summary>
        public event Action<bool> OpenStateChanged;

        private void Awake()
        {
            if (_hinge == null)
                _hinge = transform;
            _closedRotation = _hinge.localRotation;
            // The hinge and the event follow the synced state, so they run on every peer, not only the one that asked.
            _isOpen.onChanged += ApplyOpenState;
        }

        /// <summary>
        /// <see cref="IOpenable"/>: open regardless of lock or bar. This is Porta's entry point —
        /// the spell's whole value is that it ignores both, silently.
        /// </summary>
        public void Open()
        {
            if (IsOpen)
                return;
            if (DecidesLocally)
                _isOpen.value = true;
            else
                OpenOnServer();
        }

        public void Close()
        {
            if (!IsOpen)
                return;
            if (DecidesLocally)
                _isOpen.value = false;
            else
                CloseOnServer();
        }

        [ServerRpc(requireOwnership: false)]
        private void OpenOnServer() => _isOpen.value = true;

        [ServerRpc(requireOwnership: false)]
        private void CloseOnServer() => _isOpen.value = false;

        /// <summary>
        /// A player pushing the door by hand. Fails on a locked or barred door — which is the moment
        /// the player has to decide whether to spend a word or make a noise.
        /// </summary>
        public bool TryOpenByHand()
        {
            if (IsOpen)
                return true;
            if (IsLocked || IsBarred)
                return false;

            // A client answers from the synced lock state; the server re-checks it before opening.
            if (DecidesLocally)
                _isOpen.value = true;
            else
                TryOpenByHandOnServer();
            return true;
        }

        [ServerRpc(requireOwnership: false)]
        private void TryOpenByHandOnServer() => TryOpenByHand();

        /// <summary>
        /// Shoulder it open. Always works, always loud: the noise is emitted through the normal
        /// acoustic path, so it reaches the alarm exactly like any other sound.
        /// </summary>
        public bool ForceOpen()
        {
            if (IsOpen)
                return true;

            if (!DecidesLocally)
            {
                ForceOpenOnServer();
                return true;
            }

            _isOpen.value = true;
            NoiseBroadcaster.Broadcast(transform.position, _forceNoiseRadius, _forceNoiseStrength,
                NoiseType.ItemDrop, ~0, _geometryLayers);
            return true;
        }

        [ServerRpc(requireOwnership: false)]
        private void ForceOpenOnServer() => ForceOpen();

        /// <summary>Locks the door. The alarm calls this castle-wide when it reaches Roused.</summary>
        public void Lock() => SetLocked(true);

        public void Unlock() => SetLocked(false);

        /// <summary>Bars the door — cannot be opened by hand at all, only forced or spelled open.</summary>
        public void Bar() => SetBarred(true);

        private void SetLocked(bool locked)
        {
            if (DecidesLocally)
                _locked.value = locked;
            else
                SetLockedOnServer(locked);
        }

        private void SetBarred(bool barred)
        {
            if (DecidesLocally)
                _barred.value = barred;
            else
                SetBarredOnServer(barred);
        }

        [ServerRpc(requireOwnership: false)]
        private void SetLockedOnServer(bool locked) => _locked.value = locked;

        [ServerRpc(requireOwnership: false)]
        private void SetBarredOnServer(bool barred) => _barred.value = barred;

        private void ApplyOpenState(bool open)
        {
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
