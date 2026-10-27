using Code.Scripts.EventSystems;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// The book's left sheet while its page is torn out (#448): the drawing is hidden and a narrow ragged strip of the same
    /// vellum is left by the spine, on every machine. Reacts to <see cref="DemandPageChanged"/>; no polling.
    /// </summary>
    public class LedgerTornStub : MonoBehaviour
    {
        [SerializeField] private DemandPageKeeper _keeper;
        [Tooltip("The drawing's quad, hidden while the page is torn.")]
        [SerializeField] private GameObject _drawing;
        [Tooltip("The strip of vellum left by the spine, shown while the page is torn.")]
        [SerializeField] private GameObject _stub;

        private const int StubWidth = 96;
        private const int StubHeight = 672;

        private Texture2D _edge;

        private void OnEnable()
        {
            if (_edge == null)
            {
                _edge = RaggedEdge.Stub(StubWidth, StubHeight);
                _stub.GetComponent<Renderer>().material.mainTexture = _edge;
            }
            EventManager.Instance?.Subscribe(this, (DemandPageChanged e) => Show(e.IsTorn));
            Show(_keeper.IsTorn); // a late joiner meets a book already torn
        }

        private void OnDisable() => EventManager.Instance?.UnsubscribeFromAllEvents(this);

        private void OnDestroy()
        {
            if (_edge != null)
                Destroy(_edge);
        }

        private void Show(bool torn)
        {
            _drawing.SetActive(!torn);
            _stub.SetActive(torn);
        }
    }
}
