using System;
using UnityEngine;

namespace Plunderspell.Core
{
    /// <summary>Health, mana and gold for the player. Plain C# so it is testable without a scene.</summary>
    public class PlayerStats
    {
        public int MaxHealth { get; }
        public int Health { get; private set; }
        public int MaxMana { get; }
        public int Mana { get; private set; }
        public int Gold { get; private set; }

        public event Action StatsChanged;

        public PlayerStats(int maxHealth = 100, int maxMana = 50)
        {
            MaxHealth = maxHealth;
            Health = maxHealth;
            MaxMana = maxMana;
            Mana = maxMana;
        }

        public void ApplyDamage(int amount)
        {
            Health = Mathf.Clamp(Health - amount, 0, MaxHealth);
            StatsChanged?.Invoke();
        }

        /// <summary>Mirrors the player body's real health, which owns the number. The HUD reads this.</summary>
        public void SetHealth(int health)
        {
            int clamped = Mathf.Clamp(health, 0, MaxHealth);
            if (clamped == Health)
            {
                return;
            }

            Health = clamped;
            StatsChanged?.Invoke();
        }

        public void Heal(int amount)
        {
            Health = Mathf.Clamp(Health + amount, 0, MaxHealth);
            StatsChanged?.Invoke();
        }

        public bool SpendMana(int amount)
        {
            if (amount > Mana)
            {
                return false;
            }

            Mana -= amount;
            StatsChanged?.Invoke();
            return true;
        }

        public void RestoreMana(int amount)
        {
            Mana = Mathf.Clamp(Mana + amount, 0, MaxMana);
            StatsChanged?.Invoke();
        }

        public void AddGold(int amount)
        {
            Gold += amount;
            StatsChanged?.Invoke();
        }
    }
}
