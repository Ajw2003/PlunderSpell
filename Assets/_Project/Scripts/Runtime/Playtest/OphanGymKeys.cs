using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Plunderspell.Playtest
{
    /// <summary>
    /// The Ophan Gym's test keys (#454): F7 summons the Ophan, F8 no damage to you, F9 removes the guards, F10 cycles
    /// time. Work from either machine; <see cref="OphanGym"/> asks the host where the host decides. F1, F5 and F6 are
    /// taken by the game's own input actions, so the gym's start at F7.
    /// </summary>
    [RequireComponent(typeof(OphanGym))]
    public sealed class OphanGymKeys : MonoBehaviour
    {
        /// <summary>The keys and what each does, in the order the panel lists them.</summary>
        public static readonly (string Key, string Does)[] Legend =
        {
            ("F7", "Summon the Ophan now"),
            ("F8", "No damage to you"),
            ("F9", "Remove the guards"),
            ("F10", "Time: full, half, quarter"),
        };

        private readonly List<InputAction> _actions = new List<InputAction>();

        private void OnEnable()
        {
            var gym = GetComponent<OphanGym>();
            Bind("<Keyboard>/f7", gym.Summon);
            Bind("<Keyboard>/f8", gym.ToggleLocalPlayerUntouchable);
            Bind("<Keyboard>/f9", gym.RemoveGuards);
            Bind("<Keyboard>/f10", gym.CycleTimeScale);
        }

        private void OnDisable()
        {
            foreach (InputAction action in _actions)
                action.Dispose();
            _actions.Clear();
        }

        private void Bind(string binding, Action does)
        {
            var action = new InputAction(binding: binding);
            action.performed += _ => does();
            action.Enable();
            _actions.Add(action);
        }
    }
}
