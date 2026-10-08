using Player;
using Plunderspell.Core;
using Plunderspell.Inventory;
using Plunderspell.Lair;
using TMPro;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// The century dial in the Lair room chooses the Age to set out for (#358). Looking at the stand and pressing E turns
    /// it to the next Age; only the host (the session authority) chooses, a client's E does nothing. The chosen Age is
    /// <see cref="LairHubManager"/>'s selected era, the same one the Lair screen's cards set and a raid starts in, and the
    /// client learns it through <c>RaidDirector</c>. The rings ease to that Age's pose and the plaque eases to its words
    /// (<see cref="AgeNames.Plaque"/>) over <see cref="TurnSeconds"/>, so every player's dial follows.
    /// </summary>
    public class LairCenturyDial : MonoBehaviour
    {
        public const float TurnSeconds = 1f;

        [Tooltip("How far from the camera the dial can be turned, in metres.")]
        [SerializeField] private float _reach = 3f;
        [SerializeField] private Transform[] _rings;
        [SerializeField] private TextMeshPro _plaque;
        [Tooltip("Degrees each Age adds to ring n's turn about the vertical: ring n turns by this x (n + 1), alternating direction.")]
        [SerializeField] private float _degreesPerAge = 25f;

        private LairHubManager _lair;
        private Quaternion[] _restPose;
        private Quaternion[] _from;
        private HistoricalEra? _shown;
        private float _turn = 1f;
        private bool _textSwapped = true;

        /// <summary>The Age the dial is turning to or has reached.</summary>
        public HistoricalEra? ShownEra => _shown;

        /// <summary>The plaque's current words, for the co-op check.</summary>
        public string PlaqueText => _plaque.text;

        private void Awake()
        {
            _restPose = new Quaternion[_rings.Length];
            _from = new Quaternion[_rings.Length];
            for (int n = 0; n < _rings.Length; n++)
                _restPose[n] = _rings[n].localRotation;
        }

        private void Update()
        {
            if (_lair == null)
                _lair = FindFirstObjectByType<LairHubManager>();
            if (_lair == null)
                return;

            if (GameInput.Actions.PlayerActions.Interact.WasPressedThisFrame() && CanTurn())
                _lair.SelectEra(AgeNames.Next(_lair.GetLairState().SelectedEra));

            HistoricalEra chosen = _lair.GetLairState().SelectedEra;
            if (_shown != chosen)
                Show(chosen);
            Ease();
        }

        private bool CanTurn() =>
            GameServices.GameState.CurrentState == GameState.LairRoom
            && GameServices.IsSessionAuthority()
            && LookTarget.IsLookedAt(Camera.main, transform, _reach);

        // The first sight of the dial sets it at once; every later change eases.
        private void Show(HistoricalEra era)
        {
            bool first = !_shown.HasValue;
            _shown = era;
            for (int n = 0; n < _rings.Length; n++)
                _from[n] = _rings[n].localRotation;
            _turn = first ? 1f : 0f;
            _textSwapped = first;
            if (first)
                _plaque.text = AgeNames.Plaque(era);
        }

        private void Ease()
        {
            if (_turn >= 1f && _textSwapped)
                return;

            _turn = Mathf.Min(1f, _turn + Time.deltaTime / TurnSeconds);
            float eased = Mathf.SmoothStep(0f, 1f, _turn);
            int age = AgeNames.IndexOf(_shown.Value);
            for (int n = 0; n < _rings.Length; n++)
            {
                float degrees = age * _degreesPerAge * (n + 1) * (n % 2 == 0 ? 1f : -1f);
                Quaternion target = Quaternion.AngleAxis(degrees, Vector3.up) * _restPose[n];
                _rings[n].localRotation = Quaternion.Slerp(_from[n], target, eased);
            }

            // The plaque fades out, swaps its words at the half way mark, and fades back in.
            if (!_textSwapped && _turn >= 0.5f)
            {
                _plaque.text = AgeNames.Plaque(_shown.Value);
                _textSwapped = true;
            }
            _plaque.alpha = Mathf.Abs(2f * eased - 1f);
        }
    }
}
