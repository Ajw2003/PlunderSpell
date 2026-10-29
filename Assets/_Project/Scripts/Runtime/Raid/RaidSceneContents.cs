using Plunderspell.Alarm;
using Plunderspell.Castle;
using Plunderspell.Extraction;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// The raid scene's own pieces, found by the <see cref="RaidDirector"/> in the Bootstrap scene once the raid scene is
    /// loaded (scene split, #538; docs/4-systems/scene-flow.md). One per raid scene; filled by the scene split tool.
    /// </summary>
    public sealed class RaidSceneContents : MonoBehaviour
    {
        [SerializeField] private ProceduralCastleGenerator _generator;
        [SerializeField] private CastleNetworkManager _castleNetwork;
        [SerializeField] private LootSpawner _lootSpawner;
        [SerializeField] private GuardSpawner _guardSpawner;
        [SerializeField] private CastleDoorSpawner _doorSpawner;
        [SerializeField] private ExtractionZone _extractionZone;
        [SerializeField] private EnemyDirector _alarm;

        /// <summary>The loaded raid scene's contents, or null while no raid scene is loaded.</summary>
        public static RaidSceneContents Current { get; private set; }

        public ProceduralCastleGenerator Generator => _generator;
        public CastleNetworkManager CastleNetwork => _castleNetwork;
        public LootSpawner LootSpawner => _lootSpawner;
        public GuardSpawner GuardSpawner => _guardSpawner;
        public CastleDoorSpawner DoorSpawner => _doorSpawner;
        public ExtractionZone ExtractionZone => _extractionZone;
        public EnemyDirector Alarm => _alarm;

        private void OnEnable() => Current = this;

        private void OnDisable()
        {
            if (Current == this)
                Current = null;
        }
    }
}
