using UnityEngine;
using UnityEngine.UI;

namespace Plunderspell.UI
{
    /// <summary>The handle to a control built by <see cref="UIFactory.CreateSegmented"/>: shows which segment is chosen.</summary>
    public class UISegmentedControl : MonoBehaviour
    {
        private GameObject[] _selectedFills;
        private GameObject[] _underlines;
        private Text[] _labels;

        public int Selected { get; private set; } = -1;

        public int Count => _labels == null ? 0 : _labels.Length;

        public void Setup(GameObject[] selectedFills, GameObject[] underlines, Text[] labels)
        {
            _selectedFills = selectedFills;
            _underlines = underlines;
            _labels = labels;
        }

        /// <summary>Marks a segment as chosen. Does not raise the control's select callback.</summary>
        public void SetSelected(int index)
        {
            if (_labels == null)
                return;

            Selected = index;
            for (int i = 0; i < _labels.Length; i++)
            {
                bool on = i == index;
                _selectedFills[i].SetActive(on);
                _underlines[i].SetActive(on);
                _labels[i].color = on ? UITheme.Text : UITheme.TextDim;
            }
        }
    }
}
