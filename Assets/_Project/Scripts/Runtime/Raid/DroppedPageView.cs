using Code.Scripts.EventSystems;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// The torn page lying on the floor where it was dropped (#450), on every machine: a small flat sheet of vellum showing
    /// this machine's own drawing, with a thin collider so a player looking at it can pick it up (<see cref="PagePicker"/>).
    /// Placed from the keeper's state on <see cref="DemandPageChanged"/>; gone when the page is held or back in the book.
    /// </summary>
    public class DroppedPageView : MonoBehaviour
    {
        [SerializeField] private DemandPageKeeper _keeper;

        private const float Height = 0.3f; // the sheet's length on the floor, in metres
        private const float Thickness = 0.01f;
        private const float Lift = 0.02f; // above the floor, so it does not shimmer against it
        private static readonly Color Vellum = new Color(0.863f, 0.824f, 0.729f, 1f);

        private Transform _sheet;

        /// <summary>The sheet while the page lies on the floor, else null.</summary>
        public Transform Sheet => _sheet != null && _sheet.gameObject.activeSelf ? _sheet : null;

        private void OnEnable()
        {
            EventManager.Instance?.Subscribe(this, (DemandPageChanged e) => Show(e.IsLying));
            Show(_keeper.IsLying); // a late joiner meets a page already lying
        }

        private void OnDisable() => EventManager.Instance?.UnsubscribeFromAllEvents(this);

        private void Show(bool lying)
        {
            Texture2D drawing = _keeper.Drawing;
            if (!lying || drawing == null)
            {
                if (_sheet != null)
                    _sheet.gameObject.SetActive(false);
                return;
            }
            if (_sheet == null)
                _sheet = MakeSheet();
            _sheet.gameObject.SetActive(true);
            _sheet.GetComponent<Renderer>().sharedMaterial.mainTexture = drawing;
            _sheet.position = _keeper.DropPoint + Vector3.up * Lift;
            _sheet.rotation = Quaternion.Euler(90f, 0f, 0f); // a quad faces -Z; turned to face up
            _sheet.localScale = new Vector3(Height * drawing.width / drawing.height, Height, 1f);
        }

        private static Transform MakeSheet()
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "DroppedDemandPage";
            Destroy(quad.GetComponent<Collider>());
            var box = quad.AddComponent<BoxCollider>();
            box.size = new Vector3(1f, 1f, Thickness); // a thin slab; the quad's scale is 1 in Z
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = Vellum };
            quad.GetComponent<Renderer>().sharedMaterial = material;
            return quad.transform;
        }

        private void OnDestroy()
        {
            if (_sheet != null)
                Destroy(_sheet.gameObject);
        }
    }
}
