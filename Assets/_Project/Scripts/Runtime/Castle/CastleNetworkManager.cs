using PurrNet;
using UnityEngine;

namespace Plunderspell.Castle
{
    /// <summary>
    /// Replicates the castle layout across the network using nothing but the generation seed.
    ///
    /// PurrNet's <see cref="SyncVar{T}"/> is a field-based network module (there is no Mirror-style
    /// <c>[SyncVar(hook=...)]</c> attribute in PurrNet 1.15), so the seed is a
    /// <c>SyncVar&lt;int&gt;</c> and we subscribe to its <see cref="SyncVar{T}.onChangedWithOld"/>
    /// event to run local generation on every peer — host included. Because
    /// <see cref="ProceduralCastleGenerator"/> is fully deterministic, every client rebuilds an
    /// identical castle from the shared seed; no mesh or transform data is ever sent.
    /// </summary>
    [RequireComponent(typeof(ProceduralCastleGenerator))]
    public class CastleNetworkManager : NetworkBehaviour
    {
        [Tooltip("Local deterministic generator driven by the replicated seed.")]
        [SerializeField] private ProceduralCastleGenerator generator;

        [Tooltip("Maximum regeneration attempts before giving up on an unwalkable layout.")]
        [SerializeField] private int maxRetries = 10;

        // Server-authoritative seed. Replicated to every observer; change fires OnSeedChanged.
        // (PurrNet discovers SyncVar module fields via codegen; inline-initialised so it is never null.)
        private SyncVar<int> _castleSeed = new SyncVar<int>(0);

        private int _serverRetryCount;

        private void Awake()
        {
            if (generator == null)
                generator = GetComponent<ProceduralCastleGenerator>();
        }

        protected override void OnSpawned()
        {
            base.OnSpawned();
            _castleSeed.onChangedWithOld += OnSeedChanged;

            // If a seed was already assigned before we observed the object, generate immediately.
            if (_castleSeed.value != 0)
                GenerateLocal(_castleSeed.value);

            // The server kicks off the first generation once spawned.
            if (isServer)
                RequestGeneration();
        }

        protected override void OnDespawned()
        {
            base.OnDespawned();
            _castleSeed.onChangedWithOld -= OnSeedChanged;
        }

        /// <summary>
        /// Client → server request to (re)generate the castle. The server rolls a fresh seed and
        /// writes it to the SyncVar, which fans out to every observer.
        /// </summary>
        [ServerRpc(requireOwnership: false)]
        public void RequestGeneration()
        {
            int seed = Random.Range(1, int.MaxValue);
            Debug.Log($"[CastleNet] Server assigning castle seed {seed} (attempt {_serverRetryCount + 1}).");
            _castleSeed.value = seed;
        }

        /// <summary>
        /// Server-side: publish a specific seed. Writing the SyncVar fans it out to every observer,
        /// each of which regenerates locally — the same path <see cref="RequestGeneration"/> takes,
        /// but with the seed chosen by the caller. <see cref="RaidDirector"/> uses this so one seed
        /// drives both the castle and its loot.
        /// </summary>
        public void SetSeed(int seed)
        {
            if (isSpawned && !isServer)
            {
                Debug.LogWarning("[CastleNet] SetSeed ignored on a client; the server owns the seed.");
                return;
            }
            _castleSeed.value = seed;
        }

        /// <summary>The seed the castle is currently built from. Zero means nothing generated yet.</summary>
        public int CurrentSeed => _castleSeed.value;

        /// <summary>
        /// Fires on ALL peers (including the host) whenever the seed changes. Triggers local
        /// deterministic generation and, on the server, validates the result — retrying with a new
        /// seed up to <see cref="maxRetries"/> times if no crypt→extraction path exists.
        /// </summary>
        private void OnSeedChanged(int oldSeed, int newSeed)
        {
            if (newSeed == 0)
                return;

            ProceduralCastleData data = GenerateLocal(newSeed);

            if (!isServer)
                return;

            bool walkable = CastlePathValidator.ValidatePath(data, out _);
            if (walkable)
            {
                _serverRetryCount = 0;
                NotifyClientsGenerationComplete();
                return;
            }

            _serverRetryCount++;
            if (_serverRetryCount >= maxRetries)
            {
                Debug.LogError($"[CastleNet] Failed to generate a walkable castle after " +
                               $"{maxRetries} attempts (last seed {newSeed}).");
                _serverRetryCount = 0;
                return;
            }

            Debug.LogWarning($"[CastleNet] Seed {newSeed} produced no valid path; retrying " +
                             $"({_serverRetryCount}/{maxRetries}).");
            RequestGeneration();
        }

        /// <summary>Runs the deterministic generator locally and returns the resulting layout.</summary>
        private ProceduralCastleData GenerateLocal(int seed)
        {
            if (generator == null)
            {
                Debug.LogError("[CastleNet] No ProceduralCastleGenerator assigned.");
                return null;
            }
            Debug.Log($"[CastleNet] Generating castle locally from seed {seed}.");
            return generator.Generate(seed);
        }

        /// <summary>Broadcast once the server has confirmed a walkable layout.</summary>
        [ObserversRpc(bufferLast: true)]
        private void NotifyClientsGenerationComplete()
        {
            Debug.Log($"[CastleNet] Castle generation complete and validated (seed {_castleSeed.value}).");
        }
    }
}
