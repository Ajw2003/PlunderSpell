using Interfaces;
using PurrNet;
using UnityEngine;

namespace Plunderspell.Net
{
    /// <summary>
    /// Sends each hit to the machine whose copy of the target's health is the real one, by
    /// installing <see cref="Damage.Forward"/> while it is spawned. Guards, loot and everything else
    /// the server spawned keep their health on the server; a player's health lives on that player's
    /// own machine, because their body, HUD and death screen are there. The hitter gets the result
    /// back through <see cref="Damage.ReportRemote"/>, so damage numbers still appear for them.
    /// See docs/systems/net.md, "Damage".
    /// </summary>
    public sealed class DamageRelay : NetworkBehaviour
    {
        private static DamageRelay _live;

        protected override void OnSpawned()
        {
            base.OnSpawned();
            _live = this;
            Damage.Forward = TryForward;
        }

        protected override void OnDespawned()
        {
            base.OnDespawned();
            if (_live == this)
            {
                _live = null;
                Damage.Forward = null;
            }
        }

        private static bool TryForward(IHealth target, float amount, GameObject source, GameObject instigator,
            Vector3 point, DamageKind kind, float impactVelocity)
        {
            DamageRelay relay = _live;
            var component = target as Component;
            if (relay == null || !relay.isSpawned || component == null)
                return false;
            NetworkIdentity victim = component.GetComponentInParent<NetworkIdentity>();
            if (victim == null || !victim.isSpawned)
                return false;

            bool isPlayerBody = component.GetComponentInParent<PlayerNetworkOwnership>() != null;
            if (isPlayerBody)
            {
                if (victim.isOwner)
                    return false; // this machine's own body: its health lives here
                if (relay.isServer && victim.owner.HasValue)
                    relay.HitOwnedBody(victim.owner.Value, victim, amount, Id(source), Id(instigator), point, (int)kind, impactVelocity);
                else
                    relay.HitOnServer(victim, amount, Id(source), Id(instigator), point, (int)kind, impactVelocity);
                return true;
            }

            if (relay.isServer)
                return false; // the server's own object: apply here
            relay.HitOnServer(victim, amount, Id(source), Id(instigator), point, (int)kind, impactVelocity);
            return true;
        }

        private static NetworkIdentity Id(GameObject go)
        {
            if (go == null)
                return null;
            NetworkIdentity id = go.GetComponentInParent<NetworkIdentity>();
            return id != null && id.isSpawned ? id : null;
        }

        private static IHealth HealthOf(NetworkIdentity victim) =>
            victim != null ? victim.GetComponentInParent<IHealth>() ?? victim.GetComponentInChildren<IHealth>() : null;

        // DamageKind travels as an int: it lives in Plunderspell.Foundation, whose types PurrNet's code
        // generation does not register (the same trap as CastVolume in the cast RPC).
        [ServerRpc(requireOwnership: false)]
        private void HitOnServer(NetworkIdentity victim, float amount, NetworkIdentity source, NetworkIdentity instigator,
            Vector3 point, int kind, float impactVelocity, RPCInfo info = default)
        {
            IHealth health = HealthOf(victim);
            if (health == null)
                return;
            float lost = Damage.Apply(health, amount, source != null ? source.gameObject : null,
                instigator != null ? instigator.gameObject : null, point, (DamageKind)kind, impactVelocity);
            if (lost > 0f && info.sender != localPlayerForced)
                TellHitter(info.sender, victim, lost, instigator, point, kind, health.CurrentHealth, health.MaxHealth);
        }

        [TargetRpc]
        private void HitOwnedBody(PlayerID owner, NetworkIdentity victim, float amount, NetworkIdentity source,
            NetworkIdentity instigator, Vector3 point, int kind, float impactVelocity)
        {
            IHealth health = HealthOf(victim);
            if (health != null)
                Damage.Apply(health, amount, source != null ? source.gameObject : null,
                    instigator != null ? instigator.gameObject : null, point, (DamageKind)kind, impactVelocity);
        }

        [TargetRpc]
        private void TellHitter(PlayerID hitter, NetworkIdentity victim, float lost, NetworkIdentity instigator,
            Vector3 point, int kind, float healthAfter, float maxHealth)
        {
            if (victim == null)
                return;
            Damage.ReportRemote(new DamageReport(victim, null, instigator != null ? instigator.gameObject : null,
                lost, point, (DamageKind)kind, healthAfter, maxHealth));
        }
    }
}
