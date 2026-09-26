using System.Collections.Generic;
using System.Reflection;
using PurrNet;
using UnityEngine;

namespace RogueAi.Tests.Integration
{
    /// <summary>
    /// Lightweight harness for the Milestone-3 integration suite.
    ///
    /// A truly headless CI runner cannot spin up four real PurrNet peers with a live socket transport,
    /// so this harness does two things:
    /// <list type="bullet">
    /// <item>Creates a real <see cref="NetworkManager"/> GameObject (host anchor) so any code that looks
    /// up a manager finds one.</item>
    /// <item>Provides a deterministic, in-process simulation of up to four <see cref="SimulatedClient"/>
    /// peers. Server-authoritative state (e.g. the castle seed) is pushed to every simulated client the
    /// same way PurrNet's <see cref="SyncVar{T}"/> would fan it out — this lets us assert cross-client
    /// replication invariants without a live transport.</item>
    /// </list>
    /// This mirrors PurrNet's own NetworkTestBed philosophy: exercise the replication contract, not the
    /// socket stack.
    /// </summary>
    public class NetworkTestHarness
    {
        /// <summary>Maximum number of simulated peers (host + 3 clients).</summary>
        public const int MaxClients = 4;

        /// <summary>A simulated in-process peer holding the state a real client would receive.</summary>
        public class SimulatedClient
        {
            public int Index;
            public bool Connected;
            public NetworkIdentity Identity;   // spawned player identity (null in pure-logic mode)
            public int CastleSeed;             // last replicated castle seed
            public GameObject PlayerObject;
        }

        public GameObject ManagerObject { get; private set; }
        public NetworkManager Manager { get; private set; }
        public bool HostStarted { get; private set; }

        private readonly List<SimulatedClient> _clients = new List<SimulatedClient>();
        public IReadOnlyList<SimulatedClient> Clients => _clients;

        /// <summary>Number of connected peers (host counts as index 0).</summary>
        public int ConnectedCount
        {
            get
            {
                int n = 0;
                foreach (var c in _clients)
                    if (c.Connected) n++;
                return n;
            }
        }

        /// <summary>Create the manager GameObject and start the host (peer index 0).</summary>
        public void StartHost()
        {
            ManagerObject = new GameObject("TestNetworkManager");

            // NetworkManager.Awake() throws if its NetworkRules field is unset, and AddComponent()
            // runs Awake synchronously — so the GameObject must stay inactive until the field is
            // assigned via reflection (NetworkRules has no public setter).
            ManagerObject.SetActive(false);
            // A real NetworkManager component so lookups succeed; we do not open a socket.
            Manager = ManagerObject.AddComponent<NetworkManager>();
            AssignNetworkRules(Manager);
            ManagerObject.SetActive(true);

            HostStarted = true;

            var host = new SimulatedClient { Index = 0, Connected = true };
            host.PlayerObject = new GameObject("Host_Player");
            _clients.Add(host);
        }

        /// <summary>
        /// Assigns the same NetworkRules asset the real TestScene's NetworkManager uses, so the
        /// harness matches production configuration instead of inventing test-only rules.
        /// </summary>
        private static void AssignNetworkRules(NetworkManager manager)
        {
            if (manager.networkRules != null)
                return;

            NetworkRules rules = null;
#if UNITY_EDITOR
            rules = UnityEditor.AssetDatabase.LoadAssetAtPath<NetworkRules>("Assets/_Project/Net/NetworkRules.asset");
#endif
            if (rules == null)
                return;

            typeof(NetworkManager)
                .GetField("_networkRules", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(manager, rules);
        }

        /// <summary>Connect a simulated client at the given index (1..3). Returns the created peer.</summary>
        public SimulatedClient ConnectClient(int index)
        {
            var client = new SimulatedClient { Index = index, Connected = true };
            client.PlayerObject = new GameObject($"Client_{index}_Player");
            _clients.Add(client);
            return client;
        }

        /// <summary>
        /// Simulate the server replicating a castle seed to every connected peer, exactly as a PurrNet
        /// <see cref="SyncVar{T}"/> onChanged callback would deliver it.
        /// </summary>
        public void ReplicateCastleSeed(int seed)
        {
            foreach (var c in _clients)
                if (c.Connected)
                    c.CastleSeed = seed;
        }

        /// <summary>Tear down all spawned GameObjects.</summary>
        public void Teardown()
        {
            foreach (var c in _clients)
                if (c.PlayerObject != null)
                    Object.Destroy(c.PlayerObject);
            _clients.Clear();

            if (ManagerObject != null)
                Object.Destroy(ManagerObject);
            HostStarted = false;
        }
    }
}
