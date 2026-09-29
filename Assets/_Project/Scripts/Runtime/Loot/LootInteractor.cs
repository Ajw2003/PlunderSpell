using Interfaces;
using PurrNet;
using UnityEngine;

namespace Plunderspell.Loot
{
    /// <summary>
    /// The player's eyes for loot: looks at what is in front of the camera so the HUD can show a
    /// focus prompt and doors can be opened by the interact key. Grabbing and carrying itself goes
    /// through <see cref="ItemManager"/> (left-click), not this component — see docs/plans/GitIssues/
    /// Issue_169_Plan.md, "Remove the old two-person carry".
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
        [Tooltip("Door-only now — grabbing loot is left-click, through ItemManager.")]
        [SerializeField] private KeyCode _interactKey = KeyCode.E;

        [Header("Wiring")]
        [Tooltip("Camera the reach ray is cast from. Falls back to this transform.")]
        [SerializeField] private Transform _eye;

        /// <summary>What the player is currently looking at within reach, or null.</summary>
        public LootPickup Focus { get; private set; }

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

        /// <summary>The interact key: opens a door in reach. Grabbing and dropping loot is
        /// left-click, through <see cref="ItemManager"/>.</summary>
        public void Interact()
        {
            if (FocusDoor != null)
                FocusDoor.Interact();
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
