// Minimal shim so UIBootstrapper (which plants an EventSystem once per scene) and the UI focus
// component compile headlessly. Nothing here processes real input; see the "Adding to the shims"
// note in Tools/Headless/README.md.
using UnityEngine;

namespace UnityEngine.EventSystems
{
    public class EventSystem : MonoBehaviour
    {
    }

    public class BaseEventData
    {
    }

    public class PointerEventData : BaseEventData
    {
    }

    public interface IPointerEnterHandler
    {
        void OnPointerEnter(PointerEventData eventData);
    }

    public interface IPointerExitHandler
    {
        void OnPointerExit(PointerEventData eventData);
    }

    public interface ISelectHandler
    {
        void OnSelect(BaseEventData eventData);
    }

    public interface IDeselectHandler
    {
        void OnDeselect(BaseEventData eventData);
    }
}
