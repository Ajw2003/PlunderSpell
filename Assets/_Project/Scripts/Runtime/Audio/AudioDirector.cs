using System.Collections.Generic;
using Interfaces;
using Plunderspell.Alarm;
using Plunderspell.Castle;
using Plunderspell.Extraction;
using Plunderspell.Guards;
using Plunderspell.Inventory;
using Plunderspell.Loot;
using Plunderspell.Raid;
using Plunderspell.Spells;
using Plunderspell.Voice;
using StateMachine;
using UnityEngine;

namespace Plunderspell.Audio
{
    /// <summary>
    /// Plays sounds from the SoundBank out of a fixed pool of AudioSources, driven by events the game
    /// already raises. Nothing here reaches into gameplay code and nothing goes over the network: each
    /// machine plays from what it already hears about. How it works, and what is not built yet:
    /// docs/4-systems/audio.md.
    /// </summary>
    public sealed class AudioDirector : MonoBehaviour
    {
        private const int PoolSize = 32;
        private const float PollSeconds = 1f;
        private const float DoorScanSeconds = 5f;
        private const float MinRepeatSeconds = 0.04f;

        private static readonly float[] NoiseMaxDistance = { 15f, 20f, 30f, 45f, 70f };

        public static AudioDirector Instance { get; private set; }

        public RaidDirector Raid { get; private set; }
        public AlarmFSMManager Alarm { get; private set; }
        public ExtractionZone Zone { get; private set; }
        public AudioSourcePool Pool => _pool;
        public SoundBank Bank => _bank;
        public MusicDirector Music => _music;

        private SoundBank _bank;
        private AudioSourcePool _pool;
        private MusicDirector _music;
        private PushToCastController _pushToCast;
        private Transform _listener;

        private readonly HashSet<string> _reported = new HashSet<string>();
        private readonly Dictionary<string, float> _lastPlayed = new Dictionary<string, float>();
        private readonly HashSet<int> _subscribedDoors = new HashSet<int>();
        private readonly HashSet<int> _subscribedGuards = new HashSet<int>();
        private float _pollAt;
        private float _doorScanAt;
        private int _lastHaulCount;

        /// <summary>Wires the director to a bank. Runs once, from <see cref="AudioBootstrapper"/> or a test.</summary>
        public void Initialize(SoundBank bank, bool withMusic = true)
        {
            Instance = this;
            _bank = bank;
            _pool = new AudioSourcePool(transform, PoolSize);

            AudioLevels.Bind(bank.Mixer);

            if (withMusic)
            {
                _music = gameObject.AddComponent<MusicDirector>();
                _music.Initialize(this);
            }
        }

        private void OnEnable()
        {
            SpellCastingSystem.PhraseResolved += OnPhraseResolved;
            SpellCastingSystem.CastResolved += OnCastResolved;
            Damage.Dealt += OnDamageDealt;
            LootValue.Ruined += OnLootRuined;
        }

        private void OnDisable()
        {
            SpellCastingSystem.PhraseResolved -= OnPhraseResolved;
            SpellCastingSystem.CastResolved -= OnCastResolved;
            Damage.Dealt -= OnDamageDealt;
            LootValue.Ruined -= OnLootRuined;

            if (Alarm != null)
                Alarm.AlarmStateChanged -= OnAlarmStateChanged;
            if (Zone != null)
            {
                Zone.HaulInZoneChanged -= OnHaulChanged;
                Zone.ExtractionResolved -= OnExtractionResolved;
            }
            if (Raid != null)
                Raid.PortalOpened -= OnPortalOpened;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            AudioLevels.TickCasting(_pushToCast != null && _pushToCast.IsCasting, Time.unscaledDeltaTime);

            if (Time.unscaledTime < _pollAt)
                return;
            _pollAt = Time.unscaledTime + PollSeconds;
            Discover();
        }

        // --- Playing -----------------------------------------------------------------------------------

        /// <summary>Plays a UI sound if the audio layer is running; safe to call from anywhere.</summary>
        public static void PlayUi(string soundName)
        {
            if (Instance != null)
                Instance.Play(soundName, Vector3.zero);
        }

        /// <summary>
        /// Plays one variant of <paramref name="soundName"/> at <paramref name="position"/> and returns the
        /// source, or null when the name is not in the bank (logged once, never thrown).
        /// </summary>
        public AudioSource Play(string soundName, Vector3 position, float volumeScale = 1f, int variant = -1)
        {
            if (_bank == null || soundName == null)
                return null;

            if (!_bank.TryGet(soundName, out SoundEntry entry) || entry.Clips == null || entry.Clips.Length == 0)
            {
                if (_reported.Add(soundName))
                    Debug.LogWarning("[Audio] No sound named '" + soundName + "' in the SoundBank.");
                return null;
            }

            float now = Time.unscaledTime;
            if (_lastPlayed.TryGetValue(soundName, out float last) && now - last < MinRepeatSeconds)
                return null;
            _lastPlayed[soundName] = now;

            int index = variant >= 0 ? variant % entry.Clips.Length : Random.Range(0, entry.Clips.Length);
            AudioClip clip = entry.Clips[index];
            if (clip == null)
                return null;

            AudioSource source = _pool.Acquire();
            source.Stop();
            source.clip = clip;
            source.outputAudioMixerGroup = entry.Group;
            source.loop = false;
            source.volume = entry.Volume * volumeScale;
            source.pitch = 1f + Random.Range(-entry.PitchRange, entry.PitchRange);
            source.spatialBlend = entry.ThreeD ? 1f : 0f;
            source.minDistance = 2f;
            source.maxDistance = NoiseMaxDistance[(int)entry.Noise];
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.transform.position = position;
            source.Play();
            return source;
        }

        private Vector3 ListenerPosition => _listener != null ? _listener.position : Vector3.zero;

        private HistoricalEra CurrentEra => Raid != null ? Raid.Era : HistoricalEra.BronzeAge;

        // --- Events ------------------------------------------------------------------------------------

        private void OnPhraseResolved(SpellCastingSystem.PhraseReport report)
        {
            if (report.NotEnoughMana)
                Play(SoundNames.NoMana, ListenerPosition);
            else if (report.Fizzled && !report.Chanting)
                Play(SoundNames.Fizzle, ListenerPosition);
        }

        private void OnCastResolved(SpellCastingSystem.CastReport report)
        {
            string name = report.IsMisfire
                ? SoundNames.SpellMisfire(report.Spell)
                : SoundNames.SpellCast(report.Spell, report.Volume);
            Play(name, report.Origin);
            if (report.IsMisfire)
                Play(SoundNames.MisfireSting, ListenerPosition);
        }

        private void OnDamageDealt(DamageReport report)
        {
            var body = report.Target as PlayerStateMachine;
            if (body != null && body.IsLocal)
            {
                bool heavy = report.MaxHealth > 0f && report.Amount >= 0.3f * report.MaxHealth;
                Play(heavy ? SoundNames.PlayerHurtHeavy : SoundNames.PlayerHurt, report.Point);
            }
            else
            {
                Play(SoundNames.Hit(report.Kind), report.Point);
            }

            if (report.Killed && body != null)
                Play(SoundNames.PlayerDeath, report.Point);
        }

        private void OnLootRuined(LootValue piece, float worthLost)
        {
            if (piece != null)
                Play(SoundNames.LootBreak, piece.transform.position);
        }

        private void OnAlarmStateChanged(AlarmState state)
        {
            string sting = SoundNames.AlarmSting(state, CurrentEra);
            if (sting != null)
                Play(sting, Vector3.zero);
        }

        private void OnPortalOpened(Vector3 point) => Play(SoundNames.PortalOpened, Vector3.zero);

        private void OnHaulChanged(float worth, int pieces)
        {
            if (pieces > _lastHaulCount)
                Play(SoundNames.ItemCrossed, Vector3.zero);
            _lastHaulCount = pieces;
        }

        private void OnExtractionResolved(float worth, int playersSaved)
        {
            if (playersSaved > 0)
                Play(SoundNames.ExtractSuccess, Vector3.zero);
        }

        private void OnDoorChanged(CastleDoor door, bool open)
        {
            if (door != null)
                Play(open ? SoundNames.DoorOpen : SoundNames.DoorClose, door.transform.position);
        }

        private void OnGuardAttacked(CastleGuard guard, GuardAttackKind kind)
        {
            if (guard != null)
                Play(SoundNames.GuardAttack(kind, CurrentEra), guard.transform.position);
        }

        // --- Finding the things that raise events -------------------------------------------------------

        /// <summary>
        /// Once a second: find the scene objects that raise events and subscribe to any not yet heard.
        /// Guards come from their static list; doors have none, so they are searched every few seconds
        /// while a raid runs (a client builds its castle after the host).
        /// </summary>
        private void Discover()
        {
            if (_listener == null)
            {
                AudioListener listener = FindFirstObjectByType<AudioListener>();
                if (listener != null)
                    _listener = listener.transform;
            }

            if (Raid == null)
            {
                Raid = FindFirstObjectByType<RaidDirector>();
                if (Raid != null)
                    Raid.PortalOpened += OnPortalOpened;
            }

            if (Alarm == null)
            {
                Alarm = FindFirstObjectByType<AlarmFSMManager>();
                if (Alarm != null)
                    Alarm.AlarmStateChanged += OnAlarmStateChanged;
            }

            if (Zone == null)
            {
                Zone = FindFirstObjectByType<ExtractionZone>();
                if (Zone != null)
                {
                    _lastHaulCount = Zone.PiecesInZone;
                    Zone.HaulInZoneChanged += OnHaulChanged;
                    Zone.ExtractionResolved += OnExtractionResolved;
                }
            }

            if (_pushToCast == null)
                _pushToCast = FindFirstObjectByType<PushToCastController>();

            IReadOnlyList<CastleGuard> guards = CastleGuard.Active;
            for (int i = 0; i < guards.Count; i++)
            {
                CastleGuard guard = guards[i];
                if (guard != null && _subscribedGuards.Add(guard.GetInstanceID()))
                    guard.Attacked += kind => OnGuardAttacked(guard, kind);
            }

            bool raiding = Raid != null && Raid.Phase == RaidPhase.Raiding;
            if (raiding && Time.unscaledTime >= _doorScanAt)
            {
                _doorScanAt = Time.unscaledTime + DoorScanSeconds;
                CastleDoor[] doors = FindObjectsByType<CastleDoor>(FindObjectsSortMode.None);
                for (int i = 0; i < doors.Length; i++)
                {
                    CastleDoor door = doors[i];
                    if (_subscribedDoors.Add(door.GetInstanceID()))
                        door.OpenStateChanged += open => OnDoorChanged(door, open);
                }
            }
        }
    }
}
