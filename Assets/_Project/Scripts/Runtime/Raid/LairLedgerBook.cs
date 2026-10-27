using Code.Scripts.EventSystems;
using Plunderspell.Lair;
using TMPro;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// Writes the Lair ledger onto the open book on the table (#357). The left page is the demanded piece's drawing
    /// (<see cref="LedgerDemandSketch"/>, #419); the right page names it, then the last raid and the purses (wording in
    /// <see cref="LedgerPageText"/>). Prototype: the piece is set by LairRoomForge until the demand itself chooses it.
    /// Numbers come from <see cref="LairHubManager"/>, which on a client has been given the host's ledger, so
    /// both players' books agree. Redrawn when the ledger's events fire; the last raid has no event, so each
    /// frame only compares its two values.
    /// </summary>
    public class LairLedgerBook : MonoBehaviour
    {
        [SerializeField] private TextMeshPro _rightPage;
        [SerializeField] private Plunderspell.Loot.LootItem _demanded;

        private LairHubManager _lair;
        private float _shownWorth = float.NaN;
        private int _shownLeftBehind = -1;
        private bool _dirty = true;

        public string RightText => _rightPage.text;

        private void OnEnable()
        {
            EventManager manager = EventManager.Instance;
            if (manager == null)
                return;
            manager.Subscribe(this, (DebtChanged e) => _dirty = true);
            manager.Subscribe(this, (PurseChanged e) => _dirty = true);
            manager.Subscribe(this, (PresentChanged e) => _dirty = true);
            manager.Subscribe(this, (CollectorPaid e) => _dirty = true);
            manager.Subscribe(this, (CollectorSpoke e) => _dirty = true);
            manager.Subscribe(this, (SaveSlotLoaded e) => _dirty = true);
            _dirty = true;
        }

        private void OnDisable() => EventManager.Instance?.UnsubscribeFromAllEvents(this);

        private void Update()
        {
            if (_lair == null)
                _lair = FindFirstObjectByType<LairHubManager>();
            if (_lair == null)
                return;

            if (_lair.LastRaidWorth != _shownWorth || _lair.LastRaidLeftBehind != _shownLeftBehind)
                _dirty = true;
            if (_dirty)
                Redraw();
        }

        private void Redraw()
        {
            _dirty = false;
            _shownWorth = _lair.LastRaidWorth;
            _shownLeftBehind = _lair.LastRaidLeftBehind;

            var purses = new int[LairHubManager.Seats];
            var paid = new int[LairHubManager.Seats];
            var present = new bool[LairHubManager.Seats];
            for (int seat = 0; seat < purses.Length; seat++)
            {
                purses[seat] = _lair.Purse(seat);
                paid[seat] = _lair.PaidLast(seat);
                present[seat] = _lair.IsPresent(seat);
            }

            string piece = _demanded != null ? _demanded.DisplayName : "nothing yet";
            _rightPage.text = LedgerPageText.DemandLines(piece, _shownWorth, _shownLeftBehind)
                + LedgerPageText.RightPage(purses, paid, present, _lair.ShareDue(), string.Empty);
        }
    }
}
