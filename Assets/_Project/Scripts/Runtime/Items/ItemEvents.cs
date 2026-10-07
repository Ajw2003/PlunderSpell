using EventSystems;
using UnityEngine;

// Global namespace, like Item and RangedWeapon.

/// <summary>An item started a collision (#301). Audio listens; see docs/4-systems/audio.md.</summary>
public readonly struct ItemImpacted : IEvent
{
    public readonly Item Item;
    public readonly Collision Collision;
    public ItemImpacted(Item item, Collision collision) { Item = item; Collision = collision; }
}

/// <summary>An impact another machine simulated: the piece, the contact's relative speed, the contact
/// point, and whether it struck a creature. Relayed by <c>LootPickup</c>.</summary>
public readonly struct ItemImpactedRemotely : IEvent
{
    public readonly Item Item;
    public readonly float Speed;
    public readonly Vector3 Point;
    public readonly bool StruckCreature;
    public ItemImpactedRemotely(Item item, float speed, Vector3 point, bool struckCreature)
    {
        Item = item; Speed = speed; Point = point; StruckCreature = struckCreature;
    }
}

/// <summary>A ranged weapon fired a shot from <see cref="SpawnPoint"/> along <see cref="Direction"/>.</summary>
public readonly struct RangedWeaponFired : IEvent
{
    public readonly RangedWeapon Weapon;
    public readonly Vector3 SpawnPoint;
    public readonly Vector3 Direction;
    public RangedWeaponFired(RangedWeapon weapon, Vector3 spawnPoint, Vector3 direction)
    {
        Weapon = weapon; SpawnPoint = spawnPoint; Direction = direction;
    }
}
