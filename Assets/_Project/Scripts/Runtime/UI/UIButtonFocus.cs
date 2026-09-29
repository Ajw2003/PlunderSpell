using Plunderspell.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Plunderspell.UI
{
    /// <summary>
    /// Makes a hovered button and a keyboard- or controller-selected one look the same: a marker on
    /// the left edge, and optionally a brighter border and label. Selectable already tints the fill for
    /// both, but a tint alone is too faint to find the focused button from across the room.
    /// </summary>
    public class UIButtonFocus : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        private Image _border;
        private Color _borderNormal;
        private Color _borderFocused;
        private Text _label;
        private Color _labelNormal;
        private Color _labelFocused;
        private GameObject _marker;
        private bool _hovered;
        private bool _selected;

        /// <summary>Any of the parts can be null: a card has no label to recolour, a Primary button no border change.</summary>
        public void Setup(GameObject marker, Image border, Color borderNormal, Color borderFocused,
            Text label, Color labelNormal, Color labelFocused)
        {
            _marker = marker;
            _border = border;
            _borderNormal = borderNormal;
            _borderFocused = borderFocused;
            _label = label;
            _labelNormal = labelNormal;
            _labelFocused = labelFocused;
            Apply();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            AudioDirector.PlayUi(SoundNames.UiHover);
            _hovered = true;
            Apply();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            Apply();
        }

        public void OnSelect(BaseEventData eventData)
        {
            _selected = true;
            Apply();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _selected = false;
            Apply();
        }

        /// <summary>A button hidden while hovered never gets its exit event, so it would come back looking focused.</summary>
        private void OnDisable()
        {
            _hovered = false;
            _selected = false;
            Apply();
        }

        private void Apply()
        {
            bool focused = _hovered || _selected;
            if (_marker != null)
                _marker.SetActive(focused);
            if (_border != null)
                _border.color = focused ? _borderFocused : _borderNormal;
            if (_label != null)
                _label.color = focused ? _labelFocused : _labelNormal;
        }
    }
}
