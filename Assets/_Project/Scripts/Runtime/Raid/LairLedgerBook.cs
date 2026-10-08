using Code.Scripts.EventSystems;
using Plunderspell.Lair;
using TMPro;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// Writes the Lair ledger onto the open book on the table (#357): the left page holds what is owed and the
    /// last raid, the right page the purses and the Collector's line (wording in <see cref="LedgerPageText"/>).
    /// Numbers come from <see cref="LairHubManager"/>, which on a client has been given the host's ledger, so
    /// both players' books agree. Redrawn when the ledger's events fire; the last raid has no event, so each
    /// frame only compares its two values.
    /// </summary>
    public class LairLedgerBook : MonoBehaviour
    {
        [SerializeField] private TextMeshPro _leftPage;
        [SerializeField] private TextMeshPro _rightPage;

        private LairHubManager _lair;
        private float _shownWorth = float.NaN;
        private int _shownLeftBehind = -1;
        private bool _dirty = true;

        public string LeftText => _leftPage.text;
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

            _leftPage.text = LedgerPageText.LeftPage(_lair.TotalDebt, _lair.DebtIncreasePerSession, _shownWorth, _shownLeftBehind);
            _rightPage.text = LedgerPageText.RightPage(purses, paid, present, _lair.ShareDue(), _lair.CollectorLine);
        }
    }
}
