// Minimal shim so UIBootstrapper (which plants an EventSystem once per scene) compiles headlessly.
// Nothing here processes real input; see the "Adding to the shims" note in Tools/Headless/README.md.
using UnityEngine;

namespace UnityEngine.EventSystems
{
    public class EventSystem : MonoBehaviour
    {
    }
}
