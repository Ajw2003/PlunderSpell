using System.Collections.Generic;
using PurrNet;
using UnityEngine;

namespace RogueAi.Net
{
    /// <summary>
    /// Shows every player's grab beam on every machine (#144). The holder's machine sends where its
    /// beam starts, where it aims and which point of the item it holds, about 15 times a second;
    /// everyone else draws a <see cref="GrabBeam"/> from that, smoothed, ending on the item as it
    /// is replicated here. Only networked items (loot) have a beam elsewhere: weapons are a local
    /// copy on each machine.
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

        private void Update()
        {
            if (!isSpawned)
                return;
            SendLocalBeam();
            DrawRemoteBeams();
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
            BeamMoved(id, items.BeamHand, held.TargetPosition, held.HeldPointLocal, held.Load);
        }

        [ServerRpc(requireOwnership: false)]
        private void BeamMoved(NetworkIdentity item, Vector3 hand, Vector3 aim, Vector3 grabLocal, float load,
            RPCInfo info = default) =>
            ShowBeam(item, hand, aim, grabLocal, load, info.sender);

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
        private void BeamStopped(NetworkIdentity item, RPCInfo info = default) => HideBeam(item, info.sender);

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
            foreach (RemoteBeam beam in _remote.Values)
            {
                if (beam.Beam != null)
                    Destroy(beam.Beam.gameObject);
            }
            _remote.Clear();
        }
    }
}
