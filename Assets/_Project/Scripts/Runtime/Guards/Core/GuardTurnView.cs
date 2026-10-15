using System.Collections;
using Code.Scripts.EventSystems;
using Plunderspell.Alarm;
using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// Shows the Lantern Warden turning into the Wick on every machine (#417). The host decides the turn;
    /// every peer hears it as <see cref="Guard.TurnBegan"/> and plays the same sequence: raise the "guard
    /// turned" event, play <c>warden_turn</c>, drop the lantern at its <c>PropDetach</c> event, swap the model
    /// for the Wick at <see cref="TrueFormTuning.SwapSeconds"/>, then let the Wick's Animator follow how fast
    /// the guard moves. A peer that joins after the turn shows the Wick at once.
    /// </summary>
    [RequireComponent(typeof(Guard), typeof(FearAnimationEvents))]
    public sealed class GuardTurnView : MonoBehaviour
    {
        private static readonly int TurnTrigger = Animator.StringToHash("Turn");
        private static readonly int SpeedParameter = Animator.StringToHash("Speed");
        private static readonly int CatchTrigger = Animator.StringToHash("Catch");

        [SerializeField] private Animator _wardenAnimator;
        [SerializeField] private GameObject _wick;
        [SerializeField] private Transform _lanternRing;
        [SerializeField] private GameObject _looseLanternPrefab;

        private Guard _guard;
        private Animator _wickAnimator;
        private Renderer[] _wardenRenderers;
        private WardenLanternDrop _lantern;
        private Vector3 _lastPosition;

        private void Awake()
        {
            _guard = GetComponent<Guard>();
            _wardenRenderers = GetComponentsInChildren<Renderer>();
            _wickAnimator = _wick.GetComponent<Animator>();
            _lantern = new WardenLanternDrop(_guard, _lanternRing, _looseLanternPrefab);
            _guard.TurnBegan += OnTurnBegan;
            GetComponent<FearAnimationEvents>().PropDetached += _ => _lantern.Drop();
        }

        private void Start()
        {
            _guard.AttackSignal.Attacked += _ => { if (_wick.activeSelf) _wickAnimator.SetTrigger(CatchTrigger); };
            _lastPosition = transform.position;
            if (_guard.IsTurned && !_wick.activeSelf)
                ShowWick();
        }

        private void OnDestroy() => _guard.TurnBegan -= OnTurnBegan;

        private void OnTurnBegan()
        {
            EventManager.Instance?.Publish(new GuardTurned(_guard, transform.position));
            _wardenAnimator.SetTrigger(TurnTrigger);
            StartCoroutine(SwapModelAfter(_guard.Tuning.TrueForm.SwapSeconds));
        }

        private IEnumerator SwapModelAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            ShowWick();
        }

        private void ShowWick()
        {
            _lantern.Drop();
            foreach (Renderer part in _wardenRenderers)
                part.enabled = false;
            _wick.SetActive(true);
        }

        // per-frame: the Wick's walk and run follow how fast it really moves, on every machine.
        private void Update()
        {
            if (!_wick.activeSelf)
                return;

            Vector3 position = transform.position;
            Vector3 step = position - _lastPosition;
            step.y = 0f;
            _lastPosition = position;
            if (Time.deltaTime > 0f)
                _wickAnimator.SetFloat(SpeedParameter, step.magnitude / Time.deltaTime, 0.1f, Time.deltaTime);
        }
    }
}
