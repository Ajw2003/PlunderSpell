using Interfaces;
using UnityEngine;

namespace Plunderspell.Loot
{
    /// <summary>
    /// Put on a door's collider so <see cref="LootInteractor"/> can find it without the Loot assembly
    /// referencing Castle (which would cycle back through Core's interfaces). Opening by hand fails
    /// on a locked door — which is the moment the player decides whether to spend a word on Porta or
    /// make a noise forcing it. Its own file so a prefab can carry it.
    /// </summary>
    public class CastleDoorHandle : MonoBehaviour
    {
        [Tooltip("The door this handle opens. Any component implementing IOpenable.")]
        [SerializeField] private MonoBehaviour _door;

        [Tooltip("If true, a failed hand-open forces the door instead — loudly.")]
        [SerializeField] private bool _forceWhenLocked;

        private IOpenable Door => _door as IOpenable;

        /// <summary>Closes the door if it is open (#276), otherwise opens it if it can be opened by hand.
        /// Returns true if the door moved.</summary>
        public bool Interact()
        {
            IOpenable door = Door;
            if (door == null)
                return false;
            if (door.IsOpen)
            {
                door.Close();
                return true;
            }

            // Reflection-free: the door exposes hand/force opening through its own component API,
            // which the handle discovers via the optional interface below.
            if (_door is IHandOpenable byHand)
            {
                if (byHand.TryOpenByHand())
                    return true;
                if (_forceWhenLocked)
                    return byHand.ForceOpen();
                return false;
            }

            door.Open();
            return true;
        }

        /// <summary>Assigns the door at runtime (used by tooling-built scenes and tests).</summary>
        public void SetDoor(MonoBehaviour door) => _door = door;
    }
}
