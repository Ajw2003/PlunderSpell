using UnityEngine;

namespace Interfaces
{
    // Spell effects must reach loot, guards and players without the Spells assembly referencing any
    // of them (Loot and Player both sit downstream of Spells, so a direct reference would cycle).
    // Each of these is implemented by the thing that can be affected; a spell only ever sees the
    // interface, found by an overlap query.

    /// <summary>Something a shatter spell (Frango) can break. Implemented by LootPickup.</summary>
    public interface IBreakable
    {
        bool IsBroken { get; }

        /// <summary>Break this object. Must be idempotent: breaking an already-broken thing is a no-op.</summary>
        void Break();
    }

    /// <summary>Something Levo can lift. Implemented by loot and by creature ragdolls.</summary>
    public interface ILevitatable
    {
        /// <summary>Lift with the given upward impulse for <paramref name="duration"/> seconds.</summary>
        void Levitate(Vector3 impulse, float duration, UnityEngine.GameObject instigator = null);
    }

    /// <summary>Something Somnus can put to sleep. Implemented by guards.</summary>
    public interface ISleepable
    {
        bool IsAsleep { get; }
        void Sleep(float duration);
        void WakeUp();
    }

    /// <summary>Something Tonitrus can stun, and Caecus can blind. Implemented by guards and players.</summary>
    public interface IStunnable
    {
        bool IsStunned { get; }
        void Stun(float duration);
    }

    /// <summary>Something Ignis can set alight — damage over time rather than a single hit.</summary>
    public interface IIgnitable
    {
        bool IsBurning { get; }
        void Ignite(float damagePerSecond, float duration, UnityEngine.GameObject instigator = null);
    }

    /// <summary>A door or portcullis Porta can open (and a misfired Porta can open by mistake).</summary>
    public interface IOpenable
    {
        bool IsOpen { get; }
        void Open();
        void Close();
    }

    /// <summary>
    /// A door that tells being opened by hand apart from being forced. Porta ignores both and just
    /// calls <see cref="IOpenable.Open"/>; a player's hands do not, which is what makes a locked door
    /// a decision — spend a word, or make a noise.
    /// </summary>
    public interface IHandOpenable : IOpenable
    {
        /// <summary>Open it by hand. False when locked or barred.</summary>
        bool TryOpenByHand();

        /// <summary>Shoulder it open. Always works, always loud.</summary>
        bool ForceOpen();
    }
}
