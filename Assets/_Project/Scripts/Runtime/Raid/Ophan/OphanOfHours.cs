using System.Collections;
using System.Collections.Generic;
using Code.Scripts.EventSystems;
using Interfaces;
using Plunderspell.Castle;
using Plunderspell.Core;
using Plunderspell.Extraction;
using Plunderspell.Fracture;
using Plunderspell.Loot;
using PurrNet;
using StateMachine;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// The Ophan of Hours in the sky (#418, approved 2026-10-27). On the host it comes
    /// <see cref="OphanDemandTuning.HuntWindowSeconds"/> before the portal closes, picks whoever carries the most worth,
    /// runs <see cref="OphanHunt"/> against the facts of the scene (is the target alive, is stone between its eye and
    /// them, do they hold an artifact) and acts on each step: slow-beam damage, the charged beam and the rooms it
    /// scorches, the take, leaving. Every machine is told each change and shows it through <see cref="OphanLook"/>.
    /// Lives on the raid scene's Network object. See docs/4-systems/fear.md, "The Ophan in the sky".
    /// </summary>
    public sealed class OphanOfHours : NetworkBehaviour
    {
        public const string PrefabResource = "Fear/TheOphanOfHours";
        private const float RepickSeconds = 1f;
        private const float TakeDespawnDelaySeconds = 1f;
        private const float CutRadius = 1.5f;
        private const float CutForce = 12f;
        private const float NavRefreshRadius = 2f;

        [SerializeField] private OphanTuning _tuning = new OphanTuning();

        [Tooltip("Test switch until the demand (#419) exists: every raid is hunted. Off, the Ophan never comes.")]
        [SerializeField] private bool _huntEveryRaid = true;

        /// <summary>Set by check scripts and tests to switch hunts on or off without touching the scene.</summary>
        public static bool? HuntOverride;

        private OphanHunt _hunt;
        private PlayerStateMachine _target;
        private float _repickIn;
        private Vector3 _eyePoint;
        private readonly List<LootValue> _loot = new List<LootValue>();
        private readonly RaycastHit[] _hits = new RaycastHit[64];
        private OphanState _shownState = OphanState.Absent;
        private PlayerStateMachine _shownTarget;
        private bool _shownBeaming, _shownSparing;
        private OphanLook _look;

        public OphanTuning Tuning => _tuning;
        public OphanState State => _hunt != null ? _hunt.State : OphanState.Absent;
        public PlayerStateMachine Target => _target;
        public OphanLook Look => _look;
        private bool Hunted => HuntOverride ?? _huntEveryRaid;

        private void OnEnable() => EventManager.Instance?.Subscribe(this, (RaidPhaseChanged e) => OnPhase(e.Phase));
        private void OnDisable() => EventManager.Instance?.UnsubscribeFromAllEvents(this);

        private void OnPhase(RaidPhase phase)
        {
            // Every machine: a new castle or the end of the raid clears the last raid's Ophan and scorched rooms.
            if (phase != RaidPhase.Raiding)
            {
                ScorchedRooms.Clear();
                RemoveLook();
            }
            if (!isServer)
                return;
            StopAllCoroutines();
            _hunt = null;
            _target = null;
            if (phase == RaidPhase.Raiding && Hunted)
                StartCoroutine(ArriveWhenTheHuntWindowOpens());
        }

        private IEnumerator ArriveWhenTheHuntWindowOpens()
        {
            var zone = FindFirstObjectByType<ExtractionZone>();
            float window = OphanDemandTuning.Default.HuntWindowSeconds;
            float wait = zone != null ? zone.TimeRemaining - window : 0f;
            if (wait > 0f)
                yield return new WaitForSeconds(wait);
            ArriveNow();
        }

        /// <summary>On the host: the Ophan appears now. Public for tests and check scripts.</summary>
        public void ArriveNow()
        {
            if (!isServer)
                return;
            var director = FindFirstObjectByType<RaidDirector>();
            Vector3 castle = CastleCentre();
            Vector3 arrival = director != null ? director.ArrivalPoint : castle + Vector3.back;
            _eyePoint = OphanSkyPlacement.Centre(castle, arrival, _tuning);
            _hunt = new OphanHunt(_tuning);
            _hunt.Arrive();
            _target = null;
            _repickIn = 0f;
            _loot.Clear();
            _loot.AddRange(FindObjectsByType<LootValue>(FindObjectsSortMode.None));
            ShowArrival(_eyePoint, OphanSkyPlacement.Facing(_eyePoint, castle), castle);
            Debug.Log($"[Ophan] arrives at {_eyePoint} over the castle at {castle}.");
        }

        // per-frame: while it hunts, its beams drain on game time and its target moves every frame
        private void Update()
        {
            if (!isServer || _hunt == null || _hunt.State == OphanState.Absent)
                return;

            _repickIn -= Time.deltaTime;
            bool targetLost = _target == null || _target.dead;
            // Re-weighed every second in every state (aj, 2026-10-28): a player who picks up more worth mid-drain or
            // mid-charge takes the eye at once, rather than after the current target escapes or dies.
            if (targetLost || (_repickIn <= 0f && _hunt.State != OphanState.Leaving))
            {
                PlayerStateMachine before = _target;
                Repick();
                _repickIn = RepickSeconds;
                if (_target != before)
                    _hunt.Retarget();
            }

            bool hasTarget = _target != null && !_target.dead;
            Vector3 aim = hasTarget ? AimPoint(_target) : Eye;
            bool inCover = hasTarget && StoneBetween(Eye, aim, out _);
            LootValue artifact = hasTarget ? HeldArtifact(_target) : null;

            OphanStep step = _hunt.Tick(Time.deltaTime, hasTarget, inCover, artifact != null);

            if (step.Drain > 0f && hasTarget)
                Damage.Apply(_target, step.Drain, gameObject, gameObject, aim, DamageKind.Spell);
            if (step.ChargedFire && hasTarget)
                FireCharged(aim);
            if (step.ChargedKill && hasTarget)
            {
                // The second shot re-aims at wherever the target is now, breaking any new stone on that line (#470).
                FireCharged(aim);
                Damage.Apply(_target, _tuning.ChargedBeamDamage, gameObject, gameObject, aim, DamageKind.Spell);
            }
            if (step.Take && artifact != null)
                Take(artifact);

            // A killing blow can end the raid on the spot (the last player down), which clears the hunt.
            if (_hunt == null)
                return;
            bool beaming = _hunt.State == OphanState.Draining && hasTarget && !inCover;
            bool sparing = _target != null && artifact != null;
            if (_hunt.State != _shownState || _target != _shownTarget || beaming != _shownBeaming || sparing != _shownSparing)
            {
                _shownState = _hunt.State;
                _shownTarget = _target;
                _shownBeaming = beaming;
                _shownSparing = sparing;
                ShowState((int)_hunt.State, _target != null ? _target.GetComponent<NetworkIdentity>() : null, beaming, sparing);
            }
            if (step.Gone)
            {
                _hunt = null;
                _shownState = OphanState.Absent;
            }
        }

        private void Repick()
        {
            // Loot spawned or dropped since it arrived (a torn page, a piece out of a chest) counts too.
            _loot.Clear();
            _loot.AddRange(FindObjectsByType<LootValue>(FindObjectsSortMode.None));
            var players = new List<OphanCandidate<PlayerStateMachine>>();
            foreach (PlayerStateMachine body in FindObjectsByType<PlayerStateMachine>(FindObjectsSortMode.InstanceID))
                players.Add(new OphanCandidate<PlayerStateMachine>(body, WorthCarried(body), !body.dead));
            OphanTargeting.TryPick(players, out _target);
        }

        /// <summary>The sell value of what <paramref name="body"/> holds (pick 3A): the worth the portal's tally counts.</summary>
        private float WorthCarried(PlayerStateMachine body)
        {
            float worth = 0f;
            foreach (LootValue piece in _loot)
                if (Holds(body, piece))
                    worth += piece.Worth;
            return worth;
        }

        private LootValue HeldArtifact(PlayerStateMachine body)
        {
            foreach (LootValue piece in _loot)
                if (piece != null && piece.Item != null && piece.Item.IsArtifact && Holds(body, piece))
                    return piece;
            return null;
        }

        private static bool Holds(PlayerStateMachine body, LootValue piece)
        {
            if (piece == null || !piece.TryGetComponent(out Item item))
                return false;
            var identity = body.GetComponent<NetworkIdentity>();
            int key = identity != null && identity.owner.HasValue ? LootPickup.HolderKey(identity.owner.Value) : int.MinValue;
            return item.IsHeldBy(body.gameObject, key);
        }

        /// <summary>Chest height above the body pivot; the pivot sits 0.9 m above the floor, so aiming higher overshoots the head.</summary>
        public const float ChestAboveBody = 0.3f;

        /// <summary>The eye bone when the look exists, so cover, charge and beam all start at the same point.</summary>
        private Vector3 Eye => _look != null ? _look.EyePoint : _eyePoint;

        private static Vector3 AimPoint(PlayerStateMachine body) => body.transform.position + Vector3.up * ChestAboveBody;

        /// <summary>Whether castle stone lies on the line from its eye to the target.</summary>
        private bool StoneBetween(Vector3 from, Vector3 to, out List<int> pieces, List<Vector3> entries = null)
        {
            pieces = null;
            Vector3 line = to - from;
            int count = Physics.RaycastNonAlloc(from, line.normalized, _hits, line.magnitude, ~0, QueryTriggerInteraction.Ignore);
            bool blocked = false;
            for (int i = 0; i < count; i++)
            {
                Collider hit = _hits[i].collider;
                // Any part of the built castle is stone, room or not (towers, the keep, the curtain wall's dressing).
                // A charged beam's hole is a gap in the collider itself, so nothing else is let through (#467).
                if (hit.GetComponentInParent<ProceduralCastleGenerator>() == null)
                    continue;
                int piece = ScorchedRooms.PieceIndexOf(hit);
                blocked = true;
                entries?.Add(_hits[i].point);
                if (piece >= 0)
                    (pieces ??= new List<int>()).Add(piece);
            }
            return blocked;
        }

        private void FireCharged(Vector3 aim)
        {
            // Where it enters the stone, and (cast back from the target) where it breaks out on the target's side.
            var holes = new List<Vector3>();
            StoneBetween(aim, Eye, out _, holes);
            StoneBetween(Eye, aim, out List<int> pieces, holes);
            var indices = new List<int>();
            if (pieces != null)
                foreach (int piece in pieces)
                    if (!indices.Contains(piece))
                        indices.Add(piece);
            ShowCharged(Eye, aim, indices.ToArray(), holes.ToArray());
            Debug.Log($"[Ophan] charged beam at {aim}; {indices.Count} piece(s) hit, {holes.Count} wall(s) burst.");
        }

        private void Take(LootValue artifact)
        {
            var identity = artifact.GetComponent<NetworkIdentity>();
            ShowTake(identity);
            Debug.Log($"[Ophan] takes {artifact.DisplayName} and leaves.");
            _loot.Remove(artifact);
            // On a spawned piece the server's destroy despawns it for every client (as SellCounter does). Not at once:
            // every machine first copies its look for the lift (and hides it), which needs it still there.
            Destroy(identity != null ? identity.gameObject : artifact.gameObject, TakeDespawnDelaySeconds);
        }

        private static Vector3 CastleCentre()
        {
            var bounds = new Bounds();
            bool any = false;
            foreach (CastleRoomModule room in FindObjectsByType<CastleRoomModule>(FindObjectsSortMode.None))
            {
                if (any) bounds.Encapsulate(room.transform.position);
                else { bounds = new Bounds(room.transform.position, Vector3.zero); any = true; }
            }
            return any ? new Vector3(bounds.center.x, 0f, bounds.center.z) : Vector3.zero;
        }

        // -----------------------------------------------------------------------------------------
        // What every machine is told, and shows.
        // -----------------------------------------------------------------------------------------

        [ObserversRpc]
        private void ShowArrival(Vector3 centre, Quaternion facing, Vector3 castle)
        {
            RemoveLook();
            var prefab = Resources.Load<GameObject>(PrefabResource);
            if (prefab == null)
            {
                Debug.LogError($"[Ophan] no Resources/{PrefabResource}.prefab; run Plunderspell > Fear > Build The Ophan.");
                return;
            }
            GameObject go = Instantiate(prefab, centre, facing);
            go.transform.localScale = Vector3.one * _tuning.Scale;
            // The model's pivot is at its feet: lift it so its middle sits on the centre.
            go.transform.position = centre - go.transform.up * (OphanLook.MiddleAboveFeet * _tuning.Scale);
            _look = go.AddComponent<OphanLook>();
            _look.Init(_tuning, castle);
            OphanHum.Play(go);
            EventManager.Instance?.Publish(new OphanArrived(OphanHum.Seconds));
        }

        [ObserversRpc]
        private void ShowState(int state, NetworkIdentity target, bool beaming, bool sparing)
        {
            if (_look == null)
                return;
            _look.Show((OphanState)state, target != null ? target.transform : null, beaming, sparing);
            if ((OphanState)state == OphanState.Absent)
                RemoveLook();
        }

        [ObserversRpc]
        private void ShowCharged(Vector3 eye, Vector3 at, int[] pieces, Vector3[] holes)
        {
            if (_look != null)
                _look.FlashCharged(at);
            foreach (Vector3 hole in holes)
                OphanWallBurst.At(hole, (at - eye).normalized);
            // Stops 2 m short of the aim so the floor under the target is never broken.
            Vector3 cutEnd = at - (at - eye).normalized * 2f;
            foreach (int index in pieces)
            {
                Transform piece = ScorchedRooms.PieceAt(index);
                if (piece == null)
                    continue;
                // Debris lands on this piece's floor slab (piece height when it has no floor part).
                Transform floor = piece.Find("Part_Floor");
                float groundY = floor != null && floor.TryGetComponent(out Renderer floorRenderer)
                    ? floorRenderer.bounds.max.y
                    : piece.position.y;
                foreach (FracturableObject part in piece.GetComponentsInChildren<FracturableObject>())
                {
                    part.groundY = groundY;
                    part.HitAlongLine(eye, cutEnd, CutRadius, CutForce);
                }
                if (piece.TryGetComponent(out CastleRoomModule room))
                    ScorchedRooms.Scorch(room);
            }
            // Guards walk on the host's nav map; only the host updates it, once the broken colliders are in physics.
            if (isServer)
                StartCoroutine(RefreshNavAfterBreak(eye, cutEnd));
        }

        // Two fixed updates, so a whole physics step has run since the colliders changed whichever side of the step
        // WaitForFixedUpdate resumes on; the map's casts then see the holes.
        private IEnumerator RefreshNavAfterBreak(Vector3 from, Vector3 to)
        {
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            ProceduralCastleGenerator generator = FindFirstObjectByType<ProceduralCastleGenerator>();
            generator?.LastGenerated?.NavGraph?.RefreshAfterBreak(from, to, NavRefreshRadius);
        }

        [ObserversRpc]
        private void ShowTake(NetworkIdentity piece)
        {
            if (_look != null && piece != null)
                _look.Lift(piece.gameObject);
        }

        private void RemoveLook()
        {
            if (_look != null)
                Destroy(_look.gameObject);
            _look = null;
        }
    }
}
