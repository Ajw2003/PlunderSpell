using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Plunderspell.Voice
{
    /// <summary>
    /// Hold-to-talk driver. Holding the push-to-cast key opens the microphone
    /// (StartListening); releasing it closes the mic (StopListening), which is what
    /// triggers phrase recognition. The actual spell resolution happens in
    /// SpellCastingSystem, which subscribes to
    /// <see cref="IVoiceInputService.OnPhraseRecognized"/> — this controller is
    /// intentionally decoupled from the Spells assembly (Spells depends on Voice,
    /// not the other way around) so it only drives listening + visual feedback.
    /// </summary>
    public class PushToCastController : MonoBehaviour
    {
        [Header("Input")]
        [Tooltip("Hold to capture voice, release to cast.")]
        [SerializeField] private Key _pushToCastKey = Key.V;

        [Header("Visual feedback (optional)")]
        [Tooltip("Animator whose bool parameter is toggled while casting (e.g. hand raise).")]
        [SerializeField] private Animator _handAnimator;
        [SerializeField] private string _isCastingBool = "IsCasting";

        [Tooltip("UI GameObject (e.g. a 'listening' indicator) shown only while casting.")]
        [SerializeField] private GameObject _castingIndicator;

        [Tooltip("Raised whenever the casting/listening state changes. Wire UI or SFX here.")]
        public UnityEvent<bool> OnCastingStateChanged = new UnityEvent<bool>();

        /// <summary>True while the push-to-cast key is held and the mic is open.</summary>
        public bool IsCasting { get; private set; }

        /// <summary>The key the player holds to cast, so the HUD can name it instead of guessing.</summary>
        public Key PushToCastKey => _pushToCastKey;

        private int _isCastingHash;

        private void Awake()
        {
            _isCastingHash = Animator.StringToHash(_isCastingBool);
            SetCastingVisual(false);
        }

        private void Update()
        {
            // Gated on the state rather than by disabling this component, so the mic is never left
            // open across a pause and the animator hash survives the menu.
            if (!Plunderspell.Core.GameServices.IsPlaying)
            {
                if (IsCasting)
                    EndCasting();
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            KeyControl key = keyboard[_pushToCastKey];
            if (key.wasPressedThisFrame)
                BeginCasting();
            else if (key.wasReleasedThisFrame)
                EndCasting();
        }

        private void BeginCasting()
        {
            if (IsCasting)
                return;
            IsCasting = true;

            var service = VoiceServiceLocator.Current;
            service?.StartListening();

            SetCastingVisual(true);
            OnCastingStateChanged?.Invoke(true);
        }

        private void EndCasting()
        {
            if (!IsCasting)
                return;
            IsCasting = false;

            var service = VoiceServiceLocator.Current;
            service?.StopListening(); // <- this is what fires OnPhraseRecognized

            SetCastingVisual(false);
            OnCastingStateChanged?.Invoke(false);
        }

        private void SetCastingVisual(bool casting)
        {
            if (_handAnimator != null && !string.IsNullOrEmpty(_isCastingBool))
                _handAnimator.SetBool(_isCastingHash, casting);
            if (_castingIndicator != null)
                _castingIndicator.SetActive(casting);
        }

        private void OnDisable()
        {
            // Safety: never leave the mic open if the controller is torn down mid-cast.
            if (IsCasting)
                EndCasting();
        }
    }
}
