using Plunderspell.Items;
using System.Collections.Generic;
using PurrNet;
using Plunderspell.Loot;
using UnityEngine;

namespace Plunderspell.Net
{
    /// <summary>
    /// Shows every player's grab beam on every machine (#144). The holder's machine sends where its
    /// beam starts, where it aims and which point of the item it holds, about 15 times a second;
    /// everyone else draws a <see cref="GrabBeam"/> from that, smoothed, ending on the item as it
    /// is replicated here. Only networked items have a beam elsewhere; weapons are networked loot,
    /// so they get one too.
    /// <para>
    /// The same message doubles as how a beam piece's holder sends its pull: the server, which
    /// controls a held piece's body while it has any holder, writes the sender's pull into the
    /// item's remote holds (<see cref="Item.SetRemotePull"/>) so <see cref="Item.FixedUpdate"/> can
    /// apply it, unless the item is held in hand (a weapon, posed and owned locally, not shared).
    /// </para>
    /// </summary>
    public sealed class CarryBeamRelay : NetworkBehaviour
    {
        private const float k_sendInterval = 1f / 15f;
        private const float k_forgetAfter = 0.5f;

        private sealed class RemoteBeam
        {
            public GrabBeam Beam;
            public Item Item;
            public Vector3 Hand, Aim, ShownHand, ShownAim, GrabLocal;
            public float Load, LastHeard;
            public bool HasShown;
        }

        private readonly Dictionary<NetworkIdentity, RemoteBeam> _remote = new Dictionary<NetworkIdentity, RemoteBeam>();
        private readonly List<NetworkIdentity> _expired = new List<NetworkIdentity>();
        private NetworkIdentity _sending;
        private float _nextSendAt;

        // A holder key (LootPickup.HolderKey) back to the PlayerID that sent it, filled in
        // BeamMoved: an opposite-pulls snap (Item.RemoteBeamSnapped) only knows the item and the
        // key, and has to tell that specific player's machine to let go.
        private readonly Dictionary<int, PlayerID> _holderPlayers = new Dictionary<int, PlayerID>();

        // A sender's player body (Item.SetRemotePull's holderBody, so a held piece ignores that
        // player's own colliders), looked up by PlayerNetworkOwnership.owner and cached: a
        // FindObjectsByType scan every message would be wasteful at 15 messages/second/holder.
        // Refreshed on a cache miss or if the cached object was destroyed (a player who disconnected
        // and rejoined).
        private readonly Dictionary<PlayerID, GameObject> _playerBodies = new Dictionary<PlayerID, GameObject>();

        private GameObject ResolvePlayerBody(PlayerID player)
        {
            if (_playerBodies.TryGetValue(player, out GameObject cached) && cached != null)
                return cached;
            foreach (PlayerNetworkOwnership body in FindObjectsByType<PlayerNetworkOwnership>(FindObjectsSortMode.None))
            {
                if (body.owner == player)
                {
                    _playerBodies[player] = body.gameObject;
                    return body.gameObject;
                }
            }
            return null;
        }

        protected override void OnSpawned()
        {
            base.OnSpawned();
            Item.RemoteBeamSnapped += HandleRemoteBeamSnapped;
        }

        private void Update()
        {
            if (!isSpawned)
                return;
            SendLocalBeam();
            DrawRemoteBeams();
        }

        /// <summary>A remote holder's beam snapped (opposite pulls, #169): their pull is already
        /// removed by the time this fires (<see cref="Item.RemoteBeamSnapped"/>), so this only has
        /// to tell that holder's own machine to let go, or it would keep sending a pull the item no
        /// longer has and re-add it next message.</summary>
        private void HandleRemoteBeamSnapped(Item item, int holderKey)
        {
            if (!isServer || item == null)
                return;
            NetworkIdentity id = item.GetComponentInParent<NetworkIdentity>();
            if (id != null && _holderPlayers.TryGetValue(holderKey, out PlayerID player))
                TellHolderToRelease(player, id);
        }

        [TargetRpc]
        private void TellHolderToRelease(PlayerID holder, NetworkIdentity item)
        {
            ItemManager items = ItemManager.Instance;
            if (items != null && item != null && items.CarriedItem != null
                && items.CarriedItem.GetComponentInParent<NetworkIdentity>() == item)
                items.ForceRelease();
        }

        private void SendLocalBeam()
        {
            ItemManager items = ItemManager.Instance;
            Item held = items != null ? items.CarriedItem : null;
            NetworkIdentity id = held != null ? held.GetComponentInParent<NetworkIdentity>() : null;
            if (id != null && !id.isSpawned)
                id = null;

            if (_sending != null && _sending != id)
            {
                BeamStopped(_sending);
                _sending = null;
            }
            if (id == null || (_sending == id && Time.time < _nextSendAt))
                return;

            _sending = id;
            _nextSendAt = Time.time + k_sendInterval;
            // In-hand items (weapons) are owned and posed locally, not shared, so there is no pull
            // to send for them; a beam piece's pull rides along with the beam it already sends.
            CarryPull pull = held.IsInHand ? default : held.LocalPull;
            BeamMoved(id, items.BeamHand, held.TargetPosition, held.HeldPointLocal, held.Load,
                pull.TargetVelocity, pull.WantedRotation, pull.IsTowing, pull.TowFeet, pull.TowVelocity,
                pull.TowRope, pull.UprightLocalUp, pull.GripStrength, pull.HaulStrength, pull.TurnStrength);
        }

        // PurrNet RPC parameters must be primitive/auto-packed types, so CarryPull's fields travel
        // as separate parameters rather than as the struct itself.
        [ServerRpc(requireOwnership: false)]
        private void BeamMoved(NetworkIdentity item, Vector3 hand, Vector3 aim, Vector3 grabLocal, float load,
            Vector3 targetVelocity, Quaternion wantedRotation, bool isTowing, Vector3 towFeet, Vector3 towVelocity,
            float towRope, Vector3 uprightLocalUp, float gripStrength, float haulStrength, float turnStrength,
            RPCInfo info = default)
        {
            // The server controls a held piece's body while it has any holder (LootPickup.RequestHostControl);
            // this writes the sender's pull into it, unless it is the host's own hold (applied
            // locally already) or the item is held in hand (a weapon, not shared).
            if (item != null && info.sender != localPlayerForced && item.TryGetComponent(out Item component) &&
                !component.IsInHand)
            {
                int holderKey = LootPickup.HolderKey(info.sender);
                _holderPlayers[holderKey] = info.sender;
                component.SetRemotePull(holderKey, new CarryPull
                {
                    GripLocal = grabLocal,
                    Target = aim,
                    TargetVelocity = targetVelocity,
                    WantedRotation = wantedRotation,
                    IsTowing = isTowing,
                    TowFeet = towFeet,
                    TowVelocity = towVelocity,
                    TowRope = towRope,
                    UprightLocalUp = uprightLocalUp,
                    GripStrength = gripStrength,
                    HaulStrength = haulStrength,
                    TurnStrength = turnStrength,
                }, ResolvePlayerBody(info.sender));
            }
            ShowBeam(item, hand, aim, grabLocal, load, info.sender);
        }

        [ObserversRpc]
        private void ShowBeam(NetworkIdentity item, Vector3 hand, Vector3 aim, Vector3 grabLocal, float load,
            PlayerID holder)
        {
            if (holder == localPlayerForced || item == null)
                return; // the holder draws its own beam, without the network's delay

            if (!_remote.TryGetValue(item, out RemoteBeam beam))
            {
                beam = new RemoteBeam
                {
                    Beam = GrabBeam.Create($"GrabBeam_{holder}"),
                    Item = item.GetComponent<Item>(),
                };
                _remote[item] = beam;
            }
            beam.Hand = hand;
            beam.Aim = aim;
            beam.GrabLocal = grabLocal;
            beam.Load = load;
            beam.LastHeard = Time.time;
        }

        [ServerRpc(requireOwnership: false)]
        private void BeamStopped(NetworkIdentity item, RPCInfo info = default)
        {
            if (item != null && item.TryGetComponent(out Item component))
                component.RemoveRemotePull(LootPickup.HolderKey(info.sender));
            HideBeam(item, info.sender);
        }

        [ObserversRpc]
        private void HideBeam(NetworkIdentity item, PlayerID holder)
        {
            if (holder == localPlayerForced || item == null)
                return;
            Forget(item);
        }

        private void DrawRemoteBeams()
        {
            _expired.Clear();
            foreach (KeyValuePair<NetworkIdentity, RemoteBeam> pair in _remote)
            {
                RemoteBeam beam = pair.Value;
                if (pair.Key == null || beam.Item == null || Time.time - beam.LastHeard > k_forgetAfter)
                {
                    _expired.Add(pair.Key);
                    continue;
                }

                // Updates arrive a few times a second; ease toward them so the beam does not step.
                float ease = beam.HasShown ? 1f - Mathf.Exp(-20f * Time.deltaTime) : 1f;
                beam.ShownHand = Vector3.Lerp(beam.ShownHand, beam.Hand, ease);
                beam.ShownAim = Vector3.Lerp(beam.ShownAim, beam.Aim, ease);
                beam.HasShown = true;
                beam.Beam.Show(beam.ShownHand, beam.ShownAim, beam.Item.LocalToWorldPoint(beam.GrabLocal), beam.Load);
            }

            foreach (NetworkIdentity key in _expired)
                Forget(key);
        }

        private void Forget(NetworkIdentity item)
        {
            if (!_remote.TryGetValue(item, out RemoteBeam beam))
                return;
            if (beam.Beam != null)
                Destroy(beam.Beam.gameObject);
            _remote.Remove(item);
        }

        protected override void OnDespawned()
        {
            base.OnDespawned();
            Item.RemoteBeamSnapped -= HandleRemoteBeamSnapped;
            foreach (RemoteBeam beam in _remote.Values)
            {
                if (beam.Beam != null)
                    Destroy(beam.Beam.gameObject);
            }
            _remote.Clear();
        }
    }
}
