using System;
using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// Receives the animation events the fear clips carry (<c>PropDetach</c>, <c>Footstep</c>,
    /// <c>AttackHit</c>, #413) and re-raises each as a C# event. Unity delivers a clip event by message to
    /// the components on the Animator's own object; with no receiver it logs an error every time. Sits on
    /// the Wick and on the Lantern Warden, which plays the first half of the turn.
    /// </summary>
    public sealed class FearAnimationEvents : MonoBehaviour
    {
        /// <summary>Raised with the prop's name ("LanternRing") when the warden's turn lets go of it.</summary>
        public event Action<string> PropDetached;

        /// <summary>Raised with "L" or "R" as a foot lands.</summary>
        public event Action<string> FootPlanted;

        /// <summary>Raised at the instant of a catch.</summary>
        public event Action HitLanded;

        // Called by name from the clips' events.
        public void PropDetach(string prop) => PropDetached?.Invoke(prop);

        public void Footstep(string foot) => FootPlanted?.Invoke(foot);

        public void AttackHit() => HitLanded?.Invoke();
    }
}
