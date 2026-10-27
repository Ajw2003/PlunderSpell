using Code.Scripts.EventSystems;
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
            EventManager.Instance?.Publish(new PlayerStatsChanged());
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
            EventManager.Instance?.Publish(new PlayerStatsChanged());
        }

        public void Heal(int amount)
        {
            Health = Mathf.Clamp(Health + amount, 0, MaxHealth);
            EventManager.Instance?.Publish(new PlayerStatsChanged());
        }

        public bool SpendMana(int amount)
        {
            if (amount > Mana)
            {
                return false;
            }

            Mana -= amount;
            EventManager.Instance?.Publish(new PlayerStatsChanged());
            return true;
        }

        public void RestoreMana(int amount)
        {
            Mana = Mathf.Clamp(Mana + amount, 0, MaxMana);
            EventManager.Instance?.Publish(new PlayerStatsChanged());
        }

        /// <summary>Fills mana to the maximum, as a fresh body arriving in a raid does.</summary>
        public void RefillMana() => RestoreMana(MaxMana);

        public void AddGold(int amount)
        {
            Gold += amount;
            EventManager.Instance?.Publish(new PlayerStatsChanged());
        }
    }
}
