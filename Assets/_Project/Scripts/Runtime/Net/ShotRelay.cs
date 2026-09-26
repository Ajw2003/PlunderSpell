using PurrNet;
using UnityEngine;

namespace Plunderspell.Net
{
    /// <summary>
    /// Shows a ranged weapon's shot on every machine. The shot is a local projectile on the machine
    /// that fired it, which is also where its hit is judged (and routed by <see cref="DamageRelay"/>);
    /// everyone else sees a copy with no damage, fired from the same muzzle in the same direction.
    /// </summary>
    public sealed class ShotRelay : NetworkBehaviour
    {
        protected override void OnSpawned()
        {
            base.OnSpawned();
            RangedWeapon.Fired += OnFiredHere;
        }

        protected override void OnDespawned()
        {
            base.OnDespawned();
            RangedWeapon.Fired -= OnFiredHere;
        }

        private void OnFiredHere(RangedWeapon weapon, Vector3 spawnPoint, Vector3 direction)
        {
            NetworkIdentity id = weapon != null ? weapon.GetComponentInParent<NetworkIdentity>() : null;
            if (id != null && id.isSpawned)
                ShotFired(id, spawnPoint, direction);
        }

        [ServerRpc(requireOwnership: false)]
        private void ShotFired(NetworkIdentity weapon, Vector3 spawnPoint, Vector3 direction, RPCInfo info = default) =>
            ShowShot(weapon, spawnPoint, direction, info.sender);

        [ObserversRpc]
        private void ShowShot(NetworkIdentity weapon, Vector3 spawnPoint, Vector3 direction, PlayerID shooter)
        {
            if (shooter == localPlayerForced || weapon == null)
                return; // the shooter already has the real projectile
            if (weapon.TryGetComponent(out RangedWeapon ranged))
                ranged.SpawnCosmeticShot(spawnPoint, direction);
        }
    }
}
