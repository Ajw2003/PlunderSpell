// Headless shim of the PurrNet 1.15 surface Plunderspell uses.
//
// Fidelity note: PurrNet's RPCs are rewritten by IL codegen at build time, so a call to an
// [ObserversRpc] method leaves the caller and re-enters on each observer. Headlessly there is
// exactly one peer (a listen-server host), so an RPC call simply runs its body in place — which is
// the same observable behaviour a host sees. Tests that need to prove a *networked* round trip must
// use the real editor PlayMode suite; these tests prove the game logic the RPC bodies contain.
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PurrNet.Transports
{
    public enum Channel { ReliableOrdered, ReliableUnordered, Unreliable, UnreliableSequenced }

    /// <summary>PurrNet 1.15's connection lifecycle, as reported by NetworkManager.serverState/clientState.</summary>
    public enum ConnectionState { Connecting, Connected, Disconnecting, Disconnected }

    /// <summary>
    /// Base of every PurrNet transport. Headlessly a transport is only a field type CoopSession
    /// assigns to NetworkManager.transport: nothing is sent, the harness is a single host.
    /// </summary>
    public abstract class GenericTransport : UnityEngine.MonoBehaviour { }

    /// <summary>In-process transport, used for solo play.</summary>
    public class LocalTransport : GenericTransport { }

    /// <summary>LAN transport. Only its <c>address</c> is modelled.</summary>
    public class UDPTransport : GenericTransport
    {
        public string address = "127.0.0.1";
    }
}

namespace PurrNet.Modules
{
    public enum StripCodeModeOverride { Settings, Never, Always }
}

namespace PurrNet
{
    public enum CompressionLevel { None, Fast, Balanced, Best }

    public readonly struct PlayerID : IEquatable<PlayerID>
    {
        public readonly ushort id;
        public readonly bool isBot;

        public PlayerID(ushort id, bool isBot = false) { this.id = id; this.isBot = isBot; }

        public bool Equals(PlayerID other) => id == other.id && isBot == other.isBot;
        public override bool Equals(object obj) => obj is PlayerID p && Equals(p);
        public override int GetHashCode() => id.GetHashCode();
        public static bool operator ==(PlayerID a, PlayerID b) => a.Equals(b);
        public static bool operator !=(PlayerID a, PlayerID b) => !a.Equals(b);
        public override string ToString() => isBot ? $"Bot({id})" : $"Player({id})";
    }

    public struct RPCInfo
    {
        public PlayerID sender;
        public bool asServer;
    }

    public enum RPCType { ServerRPC, ObserversRPC, TargetRPC }

    [AttributeUsage(AttributeTargets.Method)]
    public class ServerRpcAttribute : Attribute
    {
        public ServerRpcAttribute(
            Transports.Channel channel = Transports.Channel.ReliableOrdered,
            bool runLocally = false,
            bool requireOwnership = true,
            CompressionLevel compressionLevel = CompressionLevel.None,
            float asyncTimeoutInSec = 5f,
            Modules.StripCodeModeOverride stripCode = Modules.StripCodeModeOverride.Settings)
        { }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ObserversRpcAttribute : Attribute
    {
        public ObserversRpcAttribute(
            Transports.Channel channel = Transports.Channel.ReliableOrdered,
            bool runLocally = false,
            bool bufferLast = false,
            bool requireServer = true,
            bool excludeOwner = false,
            bool excludeSender = false,
            CompressionLevel compressionLevel = CompressionLevel.None,
            float asyncTimeoutInSec = 5f)
        { }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class TargetRpcAttribute : Attribute
    {
        public TargetRpcAttribute(
            Transports.Channel channel = Transports.Channel.ReliableOrdered,
            bool runLocally = false,
            bool bufferLast = false,
            CompressionLevel compressionLevel = CompressionLevel.None,
            float asyncTimeoutInSec = 5f)
        { }
    }

    /// <summary>
    /// Headless network identity. <see cref="NetworkHarness"/> decides whether the peer counts as a
    /// spawned server/owner, so components can be exercised on both sides of their isServer branches.
    /// </summary>
    public class NetworkIdentity : MonoBehaviour
    {
        private PlayerID? _owner;
        private bool _spawned = true;

        public bool isSpawned => _spawned && NetworkHarness.IsRunning;
        public bool isServer => isSpawned && NetworkHarness.IsServer;
        public bool isClient => isSpawned && NetworkHarness.IsClient;
        public bool isHost => isServer && isClient;
        public bool isServerOnly => isServer && !isClient;
        public bool isOwner => isSpawned && _owner.HasValue && _owner == NetworkHarness.LocalPlayer;
        public bool isController => isSpawned && (_owner.HasValue ? isOwner : isServer);
        public bool hasConnectedOwner => _owner.HasValue;
        public PlayerID? owner => _owner;
        public PlayerID? localPlayer => NetworkHarness.LocalPlayer;
        /// <summary>PurrNet's non-null local player; the harness's, or player 0 when it has none.</summary>
        public PlayerID localPlayerForced => NetworkHarness.LocalPlayer ?? default;

        public bool IsController(bool ownerHasAuthority) => ownerHasAuthority ? isController : isServer;
        public bool IsSpawned(bool asServer) => isSpawned;

        public void GiveOwnership(PlayerID player, bool silent = false) => _owner = player;
        public void GiveOwnership(PlayerID? player, bool silent = false) => _owner = player;
        public void RemoveOwnership() => _owner = null;

        /// <summary>Test seam: mark this identity spawned/despawned and fire the lifecycle hooks.</summary>
        public void SetIsSpawned(bool spawned, bool asServer = true)
        {
            bool was = _spawned;
            _spawned = spawned;
            if (spawned && !was) InvokeMessage("OnSpawned");
            else if (!spawned && was) InvokeMessage("OnDespawned");
        }

        /// <summary>Test seam: run the OnSpawned lifecycle hook as PurrNet would after spawn.</summary>
        public void RaiseSpawned() => InvokeMessage("OnSpawned");
        public void RaiseDespawned() => InvokeMessage("OnDespawned");

        protected virtual void OnSpawned() { }
        protected virtual void OnDespawned() { }
        protected virtual void OnSpawned(bool asServer) { }
        protected virtual void OnDespawned(bool asServer) { }
        protected virtual void OnOwnerChanged(PlayerID? oldOwner, PlayerID? newOwner, bool asServer) { }
    }

    public abstract class NetworkBehaviour : NetworkIdentity { }

    /// <summary>PurrNet's per-connection player spawner. Only a scene test looks it up, by type.</summary>
    public class PlayerSpawner : MonoBehaviour { }

    /// <summary>PurrNet's transform sync. Only who controls it is modelled; nothing is sent.</summary>
    public class NetworkTransform : NetworkIdentity
    {
        public bool ownerAuth = true;
    }

    /// <summary>
    /// Global switch describing the simulated peer. Defaults to a listen-server host with local
    /// player 0, which is what a single-machine playtest looks like.
    /// </summary>
    public static class NetworkHarness
    {
        public static bool IsRunning { get; set; } = true;
        public static bool IsServer { get; set; } = true;
        public static bool IsClient { get; set; } = true;
        public static PlayerID? LocalPlayer { get; set; } = new PlayerID(0);

        /// <summary>Reset to the default host configuration (called by test setup).</summary>
        public static void ResetToHost()
        {
            IsRunning = true;
            IsServer = true;
            IsClient = true;
            LocalPlayer = new PlayerID(0);
        }

        /// <summary>Simulate a pure client peer: isServer is false, so server-only branches are skipped.</summary>
        public static void BecomeClientOnly(ushort localId = 1)
        {
            IsRunning = true;
            IsServer = false;
            IsClient = true;
            LocalPlayer = new PlayerID(localId);
        }

        /// <summary>Simulate an unspawned object (offline / single-player), as EditMode tests see it.</summary>
        public static void GoOffline()
        {
            IsRunning = false;
            IsServer = false;
            IsClient = false;
            LocalPlayer = null;
        }
    }

    public abstract class NetworkModule
    {
        public virtual void OnSpawn() { }
        public virtual void OnDespawned() { }
    }

    public class SyncVar<T> : NetworkModule
    {
        private T _value;

        public event Action<T> onChanged;
        public delegate void ActionWithOld(T oldValue, T newValue);
        public event ActionWithOld onChangedWithOld;

        public SyncVar(T initialValue = default, float sendIntervalInSeconds = 0f, bool ownerAuth = false)
        {
            _value = initialValue;
        }

        public T value
        {
            get => _value;
            set
            {
                if (EqualityComparer<T>.Default.Equals(_value, value)) return;
                T old = _value;
                _value = value;
                onChanged?.Invoke(value);
                onChangedWithOld?.Invoke(old, value);
            }
        }

        public bool ownerAuth => false;
        public void SetDirty() { }
        public void FlushImmediately() { }
        public static implicit operator T(SyncVar<T> syncVar) => syncVar._value;
        public override string ToString() => _value?.ToString() ?? "null";
    }

    public enum SyncListOperation { Added, Removed, Cleared, Set, Inserted }

    public readonly struct SyncListChange<T>
    {
        public readonly SyncListOperation operation;
        public readonly T value;
        public readonly T oldValue;
        public readonly int index;

        public SyncListChange(SyncListOperation operation, T value, T oldValue, int index)
        {
            this.operation = operation;
            this.value = value;
            this.oldValue = oldValue;
            this.index = index;
        }
    }

    public class SyncList<T> : NetworkModule, IList<T>, IReadOnlyList<T>
    {
        private readonly List<T> _list = new List<T>();

        public delegate void SyncListChanged<TYPE>(SyncListChange<TYPE> change);
        public event SyncListChanged<T> onChanged;

        public SyncList(bool ownerAuth = false) { }
        public SyncList(List<T> defaultValues, bool ownerAuth = false) => _list.AddRange(defaultValues);

        public List<T> list => _list;
        public List<T> ToList() => new List<T>(_list);
        public int Count => _list.Count;
        public bool IsReadOnly => false;

        public T this[int index]
        {
            get => _list[index];
            set
            {
                T old = _list[index];
                _list[index] = value;
                onChanged?.Invoke(new SyncListChange<T>(SyncListOperation.Set, value, old, index));
            }
        }

        public void Add(T item)
        {
            _list.Add(item);
            onChanged?.Invoke(new SyncListChange<T>(SyncListOperation.Added, item, default, _list.Count - 1));
        }

        public bool Remove(T item)
        {
            int index = _list.IndexOf(item);
            if (index < 0) return false;
            _list.RemoveAt(index);
            onChanged?.Invoke(new SyncListChange<T>(SyncListOperation.Removed, item, item, index));
            return true;
        }

        public void RemoveAt(int index)
        {
            T item = _list[index];
            _list.RemoveAt(index);
            onChanged?.Invoke(new SyncListChange<T>(SyncListOperation.Removed, item, item, index));
        }

        public void Insert(int index, T item)
        {
            _list.Insert(index, item);
            onChanged?.Invoke(new SyncListChange<T>(SyncListOperation.Inserted, item, default, index));
        }

        public void Clear()
        {
            _list.Clear();
            onChanged?.Invoke(new SyncListChange<T>(SyncListOperation.Cleared, default, default, -1));
        }

        public bool Contains(T item) => _list.Contains(item);
        public int IndexOf(T item) => _list.IndexOf(item);
        public void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);
        public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

namespace UnityEngine.Scripting
{
    [AttributeUsage(AttributeTargets.All)]
    public class PreserveAttribute : Attribute { }
}

namespace PurrNet
{
    /// <summary>
    /// Headless stand-in for PurrNet's NetworkManager. It owns no transport: starting a "host" just
    /// flips <see cref="NetworkHarness"/> into its server+client configuration, which is what the
    /// identity flags read.
    /// </summary>
    /// <summary>Only referenced by field type here; the real asset's rules are never evaluated headlessly.</summary>
    public class NetworkRules : UnityEngine.ScriptableObject { }

    public class NetworkManager : UnityEngine.MonoBehaviour
    {
        public static NetworkManager main { get; private set; }
        public NetworkRules networkRules;

        public bool isServer => NetworkHarness.IsServer;
        public bool isClient => NetworkHarness.IsClient;
        public bool isHost => isServer && isClient;
        public bool isServerOnly => isServer && !isClient;
        public bool isClientOnly => isClient && !isServer;
        public bool isOffline => !NetworkHarness.IsRunning;

        private void Awake() => main = this;

        /// <summary>The transport the next Start* call uses. Headlessly nothing reads it.</summary>
        public PurrNet.Transports.GenericTransport transport { get; set; }

        public PurrNet.Transports.ConnectionState serverState => NetworkHarness.IsServer
            ? PurrNet.Transports.ConnectionState.Connected : PurrNet.Transports.ConnectionState.Disconnected;
        public PurrNet.Transports.ConnectionState clientState => NetworkHarness.IsClient
            ? PurrNet.Transports.ConnectionState.Connected : PurrNet.Transports.ConnectionState.Disconnected;

        // Raised synchronously on each Start/Stop here; the real transport raises them a frame or
        // more later, after Connecting/Disconnecting, which a headless single host never sees.
        public event Action<PurrNet.Transports.ConnectionState> onServerConnectionState;
        public event Action<PurrNet.Transports.ConnectionState> onClientConnectionState;

        public void StartServer() { NetworkHarness.IsRunning = true; NetworkHarness.IsServer = true; onServerConnectionState?.Invoke(serverState); }
        public void StartClient() { NetworkHarness.IsRunning = true; NetworkHarness.IsClient = true; onClientConnectionState?.Invoke(clientState); }
        public void StartHost() { NetworkHarness.ResetToHost(); onServerConnectionState?.Invoke(serverState); onClientConnectionState?.Invoke(clientState); }
        public void StopServer() { NetworkHarness.IsServer = false; onServerConnectionState?.Invoke(serverState); }
        public void StopClient() { NetworkHarness.IsClient = false; onClientConnectionState?.Invoke(clientState); }
    }
}
