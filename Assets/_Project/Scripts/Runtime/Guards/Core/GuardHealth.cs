using System;
using PurrNet;
using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// A guard's hit points. The replicated value is a SyncVar owned by the guard (PurrNet only finds
    /// SyncVars that are fields of the NetworkBehaviour), so this class holds a reference to it and
    /// owns the rules: damage never goes below zero, death is announced once, a bigger lobby scales it.
    /// </summary>
    public sealed class GuardHealth
    {
        private readonly SyncVar<float> _current;
        private float _max;

        /// <summary>Raised once, the moment hit points reach zero.</summary>
        public event Action Died;

        public GuardHealth(SyncVar<float> current, float max)
        {
            _current = current;
            _max = max;
            _current.value = max;
        }

        public float Current => _current.value;

        public float Max => _max;

        public bool IsDead => _current.value <= 0f;

        /// <summary>Takes hit points off. Ignored for zero or negative damage and for a guard already down.</summary>
        public void TakeDamage(float damage)
        {
            if (damage <= 0f || IsDead)
                return;

            _current.value = Mathf.Max(0f, _current.value - damage);
            if (IsDead)
                Died?.Invoke();
        }

        /// <summary>Multiplies full and current health, for a bigger lobby (#154). Applied once at spawn.</summary>
        public void Scale(float healthScale)
        {
            if (healthScale <= 0f)
                return;

            _max *= healthScale;
            _current.value = _max;
        }
    }
}
