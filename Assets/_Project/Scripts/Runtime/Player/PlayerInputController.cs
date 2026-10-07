using Code.Scripts.EventSystems;
using StateMachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class PlayerInputController : MonoBehaviour
    {
        private PlayerStateMachine _stateMachine;
        private PlayerInputs _input;
        private Vector2 _lookDelta;

        // Hold C to creep (#238). Ctrl is the whisper modifier and Shift the shout modifier, so C is free.
        // Local input only: the slower pace is the movement itself, so other machines hear it as a quiet step.
        private InputAction _creep;

        private void Awake()
        {
            _creep = new InputAction("Creep", InputActionType.Button, "<Keyboard>/c");
            _input ??= new PlayerInputs();
            _stateMachine = GetComponent<PlayerStateMachine>();
            EventManager.Instance?.Subscribe(this, (PlayerIdleEvent e) => EnableAllInputs());
        }

        private void Start()
        {
            EnableAllInputs();
        }

        /// <summary>
        /// Whether the world reacts to input in this state. Pure, so it can be asserted against
        /// <c>CursorLockPolicy.ShouldCapture</c>, which has to agree with it. See docs/6-decisions/Decisions.md,
        /// "Issue 9's gate belongs on the raid's player, not only on the playtest harness".
        /// </summary>
        public static bool AcceptsInputIn(Plunderspell.Core.GameState state) =>
            state == Plunderspell.Core.GameState.Playing || state == Plunderspell.Core.GameState.LairRoom;

        /// <summary>Whether the world should react to input right now.</summary>
        private bool AcceptsInput => Plunderspell.Core.GameServices.IsPlaying;

        private void Update()
        {
            // A menu is open: stop looking and stop walking. Movement is held in MovementDirection
            // between callbacks, so it has to be cleared here or the body keeps travelling on the
            // last value the Input System delivered before the menu opened.
            if (!AcceptsInput)
            {
                _stateMachine.Look(Vector2.zero);
                _stateMachine.Move(Vector2.zero);
                _stateMachine.Creeping = false;
                return;
            }

            _stateMachine.Creeping = _creep.IsPressed();

            // Lock player rotation while rotating a held item.
            if (ItemManager.Instance != null && ItemManager.Instance.IsRotatingObject)
            {
                _stateMachine.Look(Vector2.zero);
                return;
            }

            _stateMachine.Look(GetLookDelta());
        }

        private void WalkInputs(bool enable)
        {
            if (enable)
            {
                _input.PlayerActions.Move.started += OnMovePerformed;
                _input.PlayerActions.Move.performed += OnMovePerformed;
                _input.PlayerActions.Move.canceled += OnMoveCanceled;
            }
            else
            {
                _input.PlayerActions.Move.started -= OnMovePerformed;
                _input.PlayerActions.Move.performed -= OnMovePerformed;
                _input.PlayerActions.Move.canceled -= OnMoveCanceled;
            }
        }

        // Opening the inventory used to free the cursor from here. Cursor lock and visibility are
        // now owned solely by CursorLockPolicy, which follows GameState; a second writer is what left
        // the cursor stuck between a menu and the world.
        private void OpenInventoryInput(bool enable)
        {
        }

        private void ItemInteractionInputs(bool enable)
        {
            if (enable)
            {
                _input.Inventory.Enable();
                _input.Inventory.Clicked.started += OnItemClickedPerformed;
                _input.Inventory.Clicked.canceled += OnItemClickedPerformed;
            }
            else
            {
                _input.Inventory.Clicked.started -= OnItemClickedPerformed;
                _input.Inventory.Clicked.canceled -= OnItemClickedPerformed;
                _input.Inventory.Disable();
            }
        }

        private void OnItemClickedPerformed(InputAction.CallbackContext context)
        {
            if (!AcceptsInput)
                return;

            ItemManager.Instance.OnInventoryClicked(context);
        }

        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            if (!AcceptsInput)
                return;

            _stateMachine.ChangeState(_stateMachine.WalkState);
            _stateMachine.Move(context.ReadValue<Vector2>());
            EventManager.Instance?.Publish(new PlayerWalkEvent { enable = true });
        }

        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            _stateMachine.Move(Vector2.zero);
            EventManager.Instance?.Publish(new PlayerWalkEvent { enable = false });
        }

        private void AttackInputs(bool enable)
        {
            if (enable)
            {
                _input.PlayerActions.Attack.performed += OnAttackPerformed;
                _input.PlayerActions.Attack.canceled += OnAttackCanceled;
            }
            else
            {
                _input.PlayerActions.Attack.performed -= OnAttackPerformed;
                _input.PlayerActions.Attack.canceled -= OnAttackCanceled;
            }
        }

        private void OnAttackPerformed(InputAction.CallbackContext context)
        {
            if (!AcceptsInput)
                return;

            _stateMachine.Attack();
            EventManager.Instance?.Publish(new PlayerAttackEvent { enable = true });
        }

        private void OnAttackCanceled(InputAction.CallbackContext context) =>
            EventManager.Instance?.Publish(new PlayerAttackEvent { enable = false });

        private void JumpInputs(bool enable)
        {
            if (enable)
            {
                _input.PlayerActions.Jump.performed += OnJumpPerformed;
                _input.PlayerActions.Jump.canceled += OnJumpCanceled;
            }
            else
            {
                _input.PlayerActions.Jump.performed -= OnJumpPerformed;
                _input.PlayerActions.Jump.canceled -= OnJumpCanceled;
            }
        }

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            if (!AcceptsInput)
                return;

            _stateMachine.Jump();
            EventManager.Instance?.Publish(new PlayerJumpEvent { enable = true });
        }

        private void OnJumpCanceled(InputAction.CallbackContext context) =>
            EventManager.Instance?.Publish(new PlayerJumpEvent { enable = false });

        private void LookInputs(bool enable)
        {
            if (enable)
            {
                _input.PlayerActions.Look.performed += OnLookPerformed;
                _input.PlayerActions.Look.canceled += OnLookPerformed;
            }
            else
            {
                _input.PlayerActions.Look.performed -= OnLookPerformed;
                _input.PlayerActions.Look.canceled -= OnLookPerformed;
            }
        }

        private void OnLookPerformed(InputAction.CallbackContext context)
        {
            // Dropped rather than stored while a menu is open, so returning to play does not apply a
            // frame of mouse movement the player made over a menu button.
            _lookDelta = AcceptsInput ? context.ReadValue<Vector2>() : Vector2.zero;
        }

        public Vector2 GetLookDelta()
        {
            return _lookDelta;
        }

        /// <summary>
        /// Generated Input System actions are unmanaged and leak if they are only ever enabled.
        /// Unity asserts on the leak the second time a scene carrying a player is loaded, which is
        /// how this surfaced. See docs/4-systems/spells.md, "Two ways to cast".
        /// </summary>
        private void OnDestroy()
        {
            if (_input == null)
                return;

            DisableAllInputs();
            _creep.Dispose();
            _input.Disable();
            _input.Dispose();
            _input = null;
        }

        private void DisableAllInputs()
        {
            WalkInputs(false);
            JumpInputs(false);
            AttackInputs(false);
            LookInputs(false);
            OpenInventoryInput(false);
            ItemInteractionInputs(false);
        }

        private void EnableAllInputs()
        {
            if (_input == null) _input = new PlayerInputs();
            _input.Enable();
            _creep.Enable();

            OpenInventoryInput(true);
            WalkInputs(true);
            // No dodge key: dodging is the Velox spell (docs/4-systems/spells.md, "Velox and Saltus").
            JumpInputs(true);
            AttackInputs(true);
            LookInputs(true);
            ItemInteractionInputs(true);
        }
    }
}
