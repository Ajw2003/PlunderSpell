using System.Collections.Generic;
using RogueAi.Castle;
using RogueAi.Raid;
using UnityEngine;

namespace RogueAi.Atmosphere
{
    /// <summary>
    /// Lights the castle: whenever the raid builds a castle, one <see cref="FireSource"/> per fire
    /// anchor of every placed module (<see cref="CastleRoomModuleData.FireAnchors"/>), set straight
    /// to the alarm's current state. Every peer builds the same castle from the seed, so every peer
    /// spawns the same fires locally; none are networked.
    /// </summary>
    public class CastleFireSpawner : MonoBehaviour
    {
        [SerializeField] private RaidDirector _director;
        [SerializeField] private ProceduralCastleGenerator _generator;

        [Header("One prefab per kind")]
        [SerializeField] private FireSource _sconce;
        [SerializeField] private FireSource _brazier;
        [SerializeField] private FireSource _hearth;
        [SerializeField] private FireSource _beacon;

        private readonly List<FireSource> _spawned = new List<FireSource>();
        private ProceduralCastleData _builtFor;
        private Transform _root;

        /// <summary>Fires spawned into the current castle.</summary>
        public IReadOnlyList<FireSource> Spawned => _spawned;

        private void Awake()
        {
            if (_director == null)
                _director = FindFirstObjectByType<RaidDirector>();
            if (_generator == null)
                _generator = FindFirstObjectByType<ProceduralCastleGenerator>();
        }

        private void LateUpdate()
        {
            // The director builds the castle on the host, on each client from the replicated seed,
            // and in tools; watching its result catches every path without hooking each one.
            ProceduralCastleData castle = _director != null ? _director.Castle : null;
            if (ReferenceEquals(castle, _builtFor))
                return;
            _builtFor = castle;
            Clear();
            if (castle != null && _generator != null)
                SpawnFor(castle, _generator.Registry);
        }

        /// <summary>Spawns the fires for a castle. Public so tools and tests can drive it without a raid.</summary>
        public void SpawnFor(ProceduralCastleData castle, CastleRoomRegistry registry)
        {
            Clear();
            if (castle?.PlacedModules == null || registry == null)
                return;

            _root = new GameObject("CastleFires").transform;
            _root.SetParent(transform, false);

            foreach (ProceduralCastleData.PlacedModule module in castle.PlacedModules)
            {
                CastleRoomModuleData entry = registry.GetById(module.RoomId);
                if (entry?.FireAnchors == null)
                    continue;
                foreach (CastleFireAnchor anchor in entry.FireAnchors)
                    Spawn(anchor, module.Position, module.Rotation);
            }
        }

        /// <summary>Spawns one fire at an anchor of a module placed at <paramref name="position"/>.</summary>
        public FireSource Spawn(CastleFireAnchor anchor, Vector3 position, Quaternion rotation)
        {
            FireSource prefab = PrefabFor(anchor.Kind);
            if (prefab == null)
                return null;
            if (_root == null)
            {
                _root = new GameObject("CastleFires").transform;
                _root.SetParent(transform, false);
            }

            Vector3 at = position + rotation * anchor.Position;
            Quaternion facing = rotation * Quaternion.Euler(0f, anchor.Yaw, 0f);

            // The portal needs clear ground round it; a brazier standing in it would block the way home.
            if (_director != null && _director.ArrivalModuleIndex >= 0 && anchor.Kind != FireKind.Sconce
                && anchor.BringsHolder && FlatDistance(at, _director.ArrivalPoint) < k_PortalClearance)
                return null;

            FireSource fire = Instantiate(prefab, at, facing, _root);
            if (!anchor.BringsHolder)
                fire.DropHolder();
            fire.Configure(anchor.Kind, anchor.LitFrom);
            fire.SetAlarmState(CastleAtmosphere.CurrentState, immediate: true);
            _spawned.Add(fire);
            return fire;
        }

        /// <summary>Puts out and removes every fire this spawned.</summary>
        public void Clear()
        {
            _spawned.Clear();
            if (_root != null)
            {
                _root.gameObject.SetActive(false);
                Destroy(_root.gameObject);
                _root = null;
            }
        }

        private const float k_PortalClearance = 4f;

        private static float FlatDistance(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).magnitude;

        private FireSource PrefabFor(FireKind kind)
        {
            switch (kind)
            {
                case FireKind.Sconce: return _sconce;
                case FireKind.Hearth: return _hearth;
                case FireKind.Beacon: return _beacon;
                default: return _brazier;
            }
        }
    }
}
