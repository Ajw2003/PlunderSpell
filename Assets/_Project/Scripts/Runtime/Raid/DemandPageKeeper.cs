using Code.Scripts.EventSystems;
using PurrNet;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// Holds the one fact about the Lair ledger's left page that every player must agree on (#448): whether it is torn out
    /// and which seat holds it (<see cref="DemandPageRules"/>). The server decides; a client's tear goes to it by
    /// <see cref="TearToServer"/> and the holder replicates as a SyncVar. Every change publishes <see cref="DemandPageChanged"/>
    /// on each machine. Unspawned (a scene without a session) it is its own authority. Coming home from a raid (the Resolved phase) puts the
    /// page back in the book. The drawing is never sent: each machine draws it from the same model (<see cref="LedgerDemandSketch"/>).
    /// </summary>
    public class DemandPageKeeper : NetworkBehaviour
    {
        private readonly SyncVar<int> _holder = new SyncVar<int>(DemandPageRules.InBook);

        // Where a dropped page lies (#450); set before the holder becomes Lying.
        private readonly SyncVar<Vector3> _dropPoint = new SyncVar<Vector3>();

        /// <summary>The seat holding the page, -1 while it is in the book, -2 while it lies on the floor.</summary>
        public int HolderSeat => _holder.value;
        public bool IsLying => _holder.value == DemandPageRules.Lying;
        public Vector3 DropPoint => _dropPoint.value;
        public bool IsTorn => DemandPageRules.IsTorn(_holder.value);
        public bool LocalPlayerHolds => IsTorn && _holder.value == LocalSeat;

        /// <summary>The drawing on the page, for the checks and for the views of later steps.</summary>
        public Texture2D Drawing => GetComponentInChildren<LedgerDemandSketch>(true)?.Drawing; // true: the quad is inactive while torn

        // Unspawned is its own authority; PurrNet's [ServerRpc] does nothing on an unspawned object.
        private bool Decides => !isSpawned || isServer;

        // Owner id n is seat n - 1, as the spawns and strongboxes have it; a lone offline player is seat 0.
        private int LocalSeat => isSpawned ? SeatOf(localPlayerForced) : 0;
        private static int SeatOf(PlayerID player) => Mathf.Max(0, (int)player.id.value - 1);

        protected override void OnSpawned()
        {
            base.OnSpawned();
            _holder.onChanged += OnReplicated;
            _dropPoint.onChanged += OnPointReplicated;
        }

        protected override void OnDespawned()
        {
            base.OnDespawned();
            _holder.onChanged -= OnReplicated;
            _dropPoint.onChanged -= OnPointReplicated;
        }

        private void OnEnable() =>
            EventManager.Instance?.Subscribe(this, (RaidPhaseChanged e) =>
            {
                // Resolved is coming home (RaidDirector.ApplyResult, on an extraction or a lost raid); InLair only comes when setting out again.
                if (e.Phase == RaidPhase.Resolved && Decides)
                    SetHolder(DemandPageRules.InBook); // the page goes back in the book
            });

        private void OnDisable() => EventManager.Instance?.UnsubscribeFromAllEvents(this);

        /// <summary>The local player tears the page out: decided here when this side decides, else sent to the server.</summary>
        public void RequestTear()
        {
            if (Decides)
                Tear(LocalSeat);
            else
                TearToServer();
        }

        [ServerRpc(requireOwnership: false)]
        private void TearToServer(RPCInfo info = default) => Tear(SeatOf(info.sender));

        /// <summary>The local player drops the page at <paramref name="point"/>: decided here when this side decides, else sent to the server.</summary>
        public void RequestDrop(Vector3 point)
        {
            if (Decides)
                Drop(LocalSeat, point);
            else
                DropToServer(point);
        }

        [ServerRpc(requireOwnership: false)]
        private void DropToServer(Vector3 point, RPCInfo info = default) => Drop(SeatOf(info.sender), point);

        /// <summary>The local player picks up the page lying on the floor.</summary>
        public void RequestPickUp()
        {
            if (Decides)
                PickUp(LocalSeat);
            else
                PickUpToServer();
        }

        [ServerRpc(requireOwnership: false)]
        private void PickUpToServer(RPCInfo info = default) => PickUp(SeatOf(info.sender));

        private void Drop(int seat, Vector3 point)
        {
            if (DemandPageRules.Drop(_holder.value, seat) == _holder.value)
                return;
            _dropPoint.value = point;
            SetHolder(DemandPageRules.Lying);
        }

        private void PickUp(int seat) => SetHolder(DemandPageRules.PickUp(_holder.value, seat));

        private void Tear(int seat) => SetHolder(DemandPageRules.Tear(_holder.value, seat));

        private void SetHolder(int holder)
        {
            if (_holder.value == holder)
                return;
            _holder.value = holder;
            Announce(holder);
        }

        // A client learns of a change by the SyncVar; the side that decided announced it in SetHolder.
        private void OnReplicated(int holder)
        {
            if (!Decides)
                Announce(holder);
        }

        // The point may arrive after the holder; the views place the page again.
        private void OnPointReplicated(Vector3 point)
        {
            if (!Decides)
                Announce(_holder.value);
        }

        private static void Announce(int holder) => EventManager.Instance?.Publish(new DemandPageChanged(holder));
    }
}
