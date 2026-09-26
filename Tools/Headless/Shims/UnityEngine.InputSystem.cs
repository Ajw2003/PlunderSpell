// Minimal shim of the Input System package surface used outside the generated PlayerInputs wrapper.
// Only the callback plumbing that gameplay code touches is modelled; the generated action asset
// (Player/Input/PlayerInputs.cs) is excluded from the headless build and stays editor-only.
using System;

namespace UnityEngine.InputSystem
{
    public class InputControl { public string name = string.Empty; }
    public class InputDevice : InputControl { }

    public class InputAction
    {
        public string name = string.Empty;
        public bool enabled { get; private set; } = true;

        public event Action<CallbackContext> started;
        public event Action<CallbackContext> performed;
        public event Action<CallbackContext> canceled;

        public void Enable() => enabled = true;
        public void Disable() => enabled = false;

        /// <summary>Test seam: raise `performed` with a value, as the real device pipeline would.</summary>
        public void Perform<T>(T value) where T : struct =>
            performed?.Invoke(new CallbackContext(this, value, ActionPhase.Performed));

        public void Cancel() => canceled?.Invoke(new CallbackContext(this, null, ActionPhase.Canceled));
        public void Start() => started?.Invoke(new CallbackContext(this, null, ActionPhase.Started));

        public enum ActionPhase { Disabled, Waiting, Started, Performed, Canceled }

        public readonly struct CallbackContext
        {
            private readonly object _value;
            public readonly InputAction action;
            public readonly ActionPhase phase;

            public CallbackContext(InputAction action, object value, ActionPhase phase)
            {
                this.action = action;
                _value = value;
                this.phase = phase;
            }

            public bool started => phase == ActionPhase.Started;
            public bool performed => phase == ActionPhase.Performed;
            public bool canceled => phase == ActionPhase.Canceled;
            public InputControl control => null;

            public T ReadValue<T>() where T : struct => _value is T typed ? typed : default;
            public object ReadValueAsObject() => _value;
            public float ReadValueAsButton() => _value is float f ? f : (_value is bool b && b ? 1f : 0f);
        }
    }

    public class ButtonControl
    {
        public bool isPressed;
        public bool wasPressedThisFrame;
        public bool wasReleasedThisFrame;
        public float ReadValue() => isPressed ? 1f : 0f;
    }

    public class Vector2Control
    {
        public Vector2 Value;
        public Vector2 ReadValue() => Value;
    }

    /// <summary>Scriptable mouse device: tests set the fields, gameplay reads them as usual.</summary>
    public class Mouse : InputDevice
    {
        public static Mouse current { get; set; } = new Mouse();
        public ButtonControl leftButton { get; } = new ButtonControl();
        public ButtonControl rightButton { get; } = new ButtonControl();
        public ButtonControl middleButton { get; } = new ButtonControl();
        public Vector2Control position { get; } = new Vector2Control();
        public Vector2Control scroll { get; } = new Vector2Control();
        public Vector2Control delta { get; } = new Vector2Control();

        /// <summary>Clears the one-frame press/release edges, as the input system does per frame.</summary>
        public static void NewFrame()
        {
            foreach (ButtonControl b in new[] { current.leftButton, current.rightButton, current.middleButton })
            {
                b.wasPressedThisFrame = false;
                b.wasReleasedThisFrame = false;
            }
        }
    }

    public class Keyboard : InputDevice
    {
        public static Keyboard current { get; set; } = new Keyboard();
    }

    public class InputActionMap
    {
        public void Enable() { }
        public void Disable() { }
    }

    public class InputActionAsset : ScriptableObject
    {
        public void Enable() { }
        public void Disable() { }
    }

    public class PlayerInput : MonoBehaviour
    {
        public InputActionAsset actions;
        public string currentControlScheme = "Keyboard&Mouse";
    }
}

namespace UnityEngine.InputSystem.Utilities
{
    public struct ReadOnlyArray<T>
    {
        private readonly T[] _array;
        public ReadOnlyArray(T[] array) => _array = array ?? Array.Empty<T>();
        public int Count => _array?.Length ?? 0;
        public T this[int index] => _array[index];
    }
}
