using Plunderspell.Loot;
using Plunderspell.Spells;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// Turns Aurum Voco into actual coin on the floor.
    ///
    /// The spell layer cannot spawn loot itself — Spells sits upstream of Loot — so the effect raises
    /// an event and this listens for it. That indirection is also what makes the spell testable: the
    /// effect's job is to announce that gold was conjured, and this one's is to put it somewhere.
    ///
    /// Both outcomes are handled. Spoken correctly, one pile appears where you aimed. Misfired, the
    /// same value arrives in four scattered pieces — the gold is not lost, but you are now picking it
    /// up off the floor of a castle that just heard you do it.
    /// </summary>
    [RequireComponent(typeof(LootSpawner))]
    public class ConjuredGoldSpawner : MonoBehaviour
    {
        [Tooltip("Loot asset used for a conjured pile. Its Worth is overwritten per cast.")]
        [SerializeField] private LootItem _goldTemplate;

        [Tooltip("Prefab spawned for conjured gold. Optional.")]
        [SerializeField] private GameObject _goldPrefab;

        private LootSpawner _spawner;

        /// <summary>Total value conjured this raid. Surfaced for tests and the raid summary.</summary>
        public float TotalConjured { get; private set; }

        private void Awake() => _spawner = GetComponent<LootSpawner>();

        private void OnEnable()
        {
            AurumVocoEffect.GoldConjured += OnGoldConjured;
            MisfireAurumVocoEffect.GoldScattered += OnGoldConjured;
        }

        private void OnDisable()
        {
            AurumVocoEffect.GoldConjured -= OnGoldConjured;
            MisfireAurumVocoEffect.GoldScattered -= OnGoldConjured;
        }

        private void OnGoldConjured(float worth, Vector3 position) => Conjure(worth, position);

        /// <summary>
        /// Spawns one pile of conjured coin. Public so a test can call it without routing through the
        /// spell layer. Returns the object, or null when no template is configured.
        /// </summary>
        public GameObject Conjure(float worth, Vector3 position)
        {
            if (_spawner == null)
                _spawner = GetComponent<LootSpawner>();

            if (_goldTemplate == null)
            {
                Debug.LogWarning("[Gold] Aurum Voco conjured coin but no gold template is configured.");
                return null;
            }

            // A per-cast instance: the conjured pile's worth varies with how loudly it was called,
            // and writing that onto the shared asset would rewrite every other pile in the raid.
            var item = Instantiate(_goldTemplate);
            item.Worth = worth;
            item.DisplayName = _goldTemplate.DisplayName;

            TotalConjured += worth;
            return _spawner.SpawnLoose(item, _goldPrefab, position);
        }

        /// <summary>Wires the spawner from code, for tests and tooling-built scenes.</summary>
        public void Configure(LootItem goldTemplate, GameObject goldPrefab)
        {
            _goldTemplate = goldTemplate;
            _goldPrefab = goldPrefab;
        }
    }
}
