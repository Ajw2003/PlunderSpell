using Interfaces;
using PurrNet;
using UnityEngine;

namespace Plunderspell.Loot
{
    /// <summary>
    /// The player's hands. Looks at what is in front of the camera and, on the interact key, picks it
    /// up, drops it, or helps a teammate lift something too heavy for one person.
    ///
    /// The dual-carry rule is what makes this co-op rather than four solo raids: anything over
    /// <see cref="LootItem.DualCarryBulkThreshold"/> stone simply will not move until a second player
    /// takes the other end. This component makes that legible — <see cref="FocusRequiresHelp"/> is
    /// true while you are looking at something you cannot lift alone.
    /// </summary>
    public class LootInteractor : NetworkBehaviour
    {
        [Header("Reach")]
        [Tooltip("How far the player can reach to grab something, in metres.")]
        [SerializeField] private float _reach = 6f;

        [Tooltip("Layers that can be interacted with.")]
        [SerializeField] private LayerMask _interactableLayers = ~0;

        [Header("Input")]
        [SerializeField] private KeyCode _interactKey = KeyCode.E;
        [SerializeField] private KeyCode _dropKey = KeyCode.Q;

        [Header("Wiring")]
        [Tooltip("Camera the reach ray is cast from. Falls back to this transform.")]
        [SerializeField] private Transform _eye;

        /// <summary>What the player is currently looking at within reach, or null.</summary>
        public LootPickup Focus { get; private set; }

        /// <summary>What the player is currently carrying, or null.</summary>
        public LootPickup Carried { get; private set; }

        /// <summary>A door in reach, or null. Doors are interacted with by the same key.</summary>
        public CastleDoorHandle FocusDoor { get; private set; }

        /// <summary>True while looking at something too heavy to lift alone.</summary>
        public bool FocusRequiresHelp =>
            Focus != null && Focus.Data != null && Focus.Data.RequiresDualCarry;

        /// <summary>Raised when what the player is looking at changes. The HUD prompt reads this.</summary>
        public event System.Action<LootPickup> FocusChanged;

        private void Awake()
        {
            if (_eye == null)
                _eye = transform;
        }

        private void Update()
        {
            // A remote player's copy must not read this machine's keyboard.
            if (isSpawned && !isOwner)
                return;



            if (Input.GetKeyDown(_interactKey))
            {
                UpdateFocus();
                Interact();
            }

            else if (Input.GetKeyDown(_dropKey))
                Drop();
        }

        /// <summary>
        /// Re-resolves what is in reach. Public so a test can drive it a frame at a time without a
        /// running game loop.
        /// </summary>
        public void UpdateFocus()
        {
            LootPickup previous = Focus;
            Focus = null;
            FocusDoor = null;

            Vector3 origin = _eye != null ? _eye.position : transform.position;
            Vector3 direction = _eye != null ? _eye.forward : transform.forward;

            // A ray, not a sphere cast: reach is a distance, and passing it as a sweep radius casts a
            // 6-metre ball that starts already overlapping whatever the player is standing next to,
            // which resolves as a zero-distance hit on an arbitrary collider.
            if (Physics.Raycast(origin, direction, out RaycastHit hit, _reach, _interactableLayers,
                    QueryTriggerInteraction.Collide))
            {
                Focus = hit.collider.GetComponentInParent<LootPickup>();
                FocusDoor = hit.collider.GetComponentInParent<CastleDoorHandle>();
            }

            if (Focus != previous)
            {
                SetHighlight(previous, false);
                SetHighlight(Focus, true);
                FocusChanged?.Invoke(Focus);
            }
        }

        /// <summary>
        /// The interact key. In priority order: open a door in reach, take the other end of a heavy
        /// item someone is already holding, or pick up what you are looking at.
        /// </summary>
        public void Interact()
        {
            if (FocusDoor != null)
            {
                FocusDoor.Interact();
                return;
            }

            if (Focus == null || Focus.IsBroken)
                return;

            // Someone already has the primary end of this: take the other one.
            if (Focus.IsBeingCarried && Focus.CurrentCarryMode == CarryMode.Dual &&
                Focus.SecondaryCarrierNetId == null && Focus.PrimaryCarrierNetId != this)
            {
                if (isSpawned)
                    Focus.RequestSecondaryPickup(this);
                else
                    Focus.PerformSecondaryPickup(this);
                return;
            }

            if (Focus.IsBeingCarried)
                return;

            // Spawned, the server decides; offline the RPC wrapper would run nothing at all.
            if (isSpawned)
                Focus.RequestPickup(this);
            else
                Focus.PerformPickup(this);

            Carried = Focus;
        }

        /// <summary>Puts down whatever is being carried.</summary>
        public void Drop()
        {
            if (Carried == null)
                return;

            if (isSpawned)
                Carried.RequestDrop();
            else
                Carried.PerformDrop();

            Carried = null;
        }

        /// <summary>Test/tooling seam: point the reach ray at a specific transform.</summary>
        public void SetEye(Transform eye) => _eye = eye;

        /// <summary>
        /// Turns the focused item's glow on or off, attaching the glow the first time an item is
        /// looked at. Added here rather than authored onto each prefab so conjured and test-built
        /// loot highlights too, and so no piece of loot can be shipped without it.
        /// </summary>
        private static void SetHighlight(LootPickup pickup, bool highlighted)
        {
            if (pickup == null)
                return;

            var highlight = pickup.GetComponent<LootHighlight>();
            if (highlight == null)
            {
                if (!highlighted)
                    return;
                highlight = pickup.gameObject.AddComponent<LootHighlight>();
            }

            highlight.SetHighlighted(highlighted);
        }

        private void OnDisable() => SetHighlight(Focus, false);
    }

    /// <summary>
    /// Put on a door's collider so <see cref="LootInteractor"/> can find it without the Loot assembly
    /// referencing Castle (which would cycle back through Core's interfaces). Opening by hand fails
    /// on a locked door — which is the moment the player decides whether to spend a word on Porta or
    /// make a noise forcing it.
    /// </summary>
    public class CastleDoorHandle : MonoBehaviour
    {
        [Tooltip("The door this handle opens. Any component implementing IOpenable.")]
        [SerializeField] private MonoBehaviour _door;

        [Tooltip("If true, a failed hand-open forces the door instead — loudly.")]
        [SerializeField] private bool _forceWhenLocked;

        private IOpenable Door => _door as IOpenable;

        /// <summary>Opens the door if it can be opened by hand. Returns true if it opened.</summary>
        public bool Interact()
        {
            IOpenable door = Door;
            if (door == null || door.IsOpen)
                return false;

            // Reflection-free: the door exposes hand/force opening through its own component API,
            // which the handle discovers via the optional interface below.
            if (_door is IHandOpenable byHand)
            {
                if (byHand.TryOpenByHand())
                    return true;
                if (_forceWhenLocked)
                    return byHand.ForceOpen();
                return false;
            }

            door.Open();
            return true;
        }

        /// <summary>Assigns the door at runtime (used by tooling-built scenes and tests).</summary>
        public void SetDoor(MonoBehaviour door) => _door = door;
    }

}
