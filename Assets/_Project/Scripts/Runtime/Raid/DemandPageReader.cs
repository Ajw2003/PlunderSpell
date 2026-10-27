using Code.Scripts.EventSystems;
using Player;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// The player holding the torn page holds R to raise it in front of their view, and may walk while it is up (#449).
    /// A quad on the local camera shows the drawing on vellum, unlit so it reads in the dark; it eases up and down
    /// (<see cref="PageRaise"/>). Only this machine sees it; nothing is sent. Lives with <see cref="DemandPageKeeper"/>, so
    /// it works in the Lair and in a raid. It uses no hand, so carrying loot is unaffected.
    /// </summary>
    public class DemandPageReader : MonoBehaviour
    {
        [SerializeField] private DemandPageKeeper _keeper;

        // The page, as a share of the screen's height, and where it sits (its centre, as a share of that height from the middle).
        private const float Distance = 0.5f;
        private const float HeightShare = 0.66f;
        private const float RaisedCentre = -0.17f;
        private const float LoweredCentre = -1.0f;
        private static readonly Color Vellum = new Color(0.863f, 0.824f, 0.729f, 1f);

        private Transform _page;
        private Texture2D _torn;     // the drawing with the ragged edge on its spine side
        private Texture2D _tornFrom; // the drawing it was cut from
        private float _raise;
        private bool _holds;

        public float Raise => _raise;

        private void OnEnable()
        {
            EventManager.Instance?.Subscribe(this, (DemandPageChanged _) => _holds = _keeper.LocalPlayerHolds);
            _holds = _keeper.LocalPlayerHolds;
        }

        private void OnDisable() => EventManager.Instance?.UnsubscribeFromAllEvents(this);

        // per-frame: reads the R key, which has no event, and eases the page's position
        private void Update()
        {
            float target = PageRaise.Target(_holds, GameInput.Actions.PlayerActions.ReadPage.IsPressed());
            _raise = PageRaise.Step(_raise, target, Time.deltaTime);
            if (_raise <= 0f)
            {
                if (_page != null)
                    _page.gameObject.SetActive(false);
                return;
            }
            Place(Camera.main);
        }

        private void Place(Camera eye)
        {
            Texture2D drawing = _keeper.Drawing;
            if (eye == null || drawing == null)
                return;
            if (_page == null)
                _page = MakePage();
            if (_page.parent != eye.transform)
                _page.SetParent(eye.transform, false);

            float seen = 2f * Distance * Mathf.Tan(eye.fieldOfView * 0.5f * Mathf.Deg2Rad); // the screen's height at the page
            float height = seen * HeightShare;
            float centre = Mathf.Lerp(LoweredCentre, RaisedCentre, Mathf.SmoothStep(0f, 1f, _raise)) * seen;
            _page.gameObject.SetActive(true);
            if (_torn == null || _tornFrom != drawing)
            {
                if (_torn != null)
                    Destroy(_torn);
                _torn = RaggedEdge.Cut(drawing);
                _tornFrom = drawing;
            }
            _page.GetComponent<Renderer>().sharedMaterial.mainTexture = _torn;
            _page.localPosition = new Vector3(0f, centre, Distance);
            _page.localRotation = Quaternion.Euler(-12f, 0f, 3f); // tipped back a little, as a page held in hands
            _page.localScale = new Vector3(height * drawing.width / drawing.height, height, 1f);
        }

        private static Transform MakePage()
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "RaisedDemandPage";
            Destroy(quad.GetComponent<Collider>());
            var material = RaggedEdge.PageMaterial("Universal Render Pipeline/Unlit", Vellum);
            quad.GetComponent<Renderer>().sharedMaterial = material;
            return quad.transform;
        }

        private void OnDestroy()
        {
            if (_page != null)
                Destroy(_page.gameObject);
            if (_torn != null)
                Destroy(_torn);
        }
    }
}
