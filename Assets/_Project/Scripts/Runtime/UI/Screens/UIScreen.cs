using UnityEngine;

namespace Plunderspell.UI.Screens
{
    /// <summary>Base for a full-screen UI panel that builds itself once and is then shown/hidden by state.</summary>
    [RequireComponent(typeof(RectTransform))]
    public abstract class UIScreen : MonoBehaviour
    {
        public void Build()
        {
            OnBuild();
            gameObject.SetActive(false);
        }

        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf == visible)
            {
                return;
            }

            gameObject.SetActive(visible);

            if (visible)
            {
                OnShown();
            }
        }

        protected abstract void OnBuild();

        protected virtual void OnShown()
        {
        }
    }
}
