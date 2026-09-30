using System.Collections.Generic;
using Interfaces;
using Plunderspell.Alarm;
using Plunderspell.Castle;
using Plunderspell.Extraction;
using Plunderspell.Guards;
using Plunderspell.Inventory;
using Plunderspell.Raid;
using Plunderspell.Spells;
using Plunderspell.Voice;
using StateMachine;
using UnityEngine;

namespace Plunderspell.Audio
{
    /// <summary>Which set of sources a sound plays from, so a crowd of steps or voices cannot use up the general pool.</summary>
    public enum SoundPoolKind
    {
        General,
        Step,
        Voice
    }

    /// <summary>
    /// Plays sounds from the SoundBank out of a fixed pool of AudioSources, driven by events the game
    /// already raises. Nothing here reaches into gameplay code and nothing goes over the network: each
    /// machine plays from what it already hears about. How it works, and what is not built yet:
    /// docs/4-systems/audio.md.
    /// </summary>
    public sealed class AudioDirector : MonoBehaviour
    {
        private const int PoolSize = 32;
        private const int StepPoolSize = 12;
        private const int VoicePoolSize = 6;
        private const int LoopSlots = 6;
        private const float PlayerScanSeconds = 5f;
        private const float PollSeconds = 1f;
        private const float DoorScanSeconds = 5f;
        private const float MinRepeatSeconds = 0.04f;
        private const string GuardFoleyGroupName = "guard_foley";

        private static readonly float[] NoiseMaxDistance = { 15f, 20f, 30f, 45f, 70f };

        public static AudioDirector Instance { get; private set; }

        public RaidDirector Raid { get; private set; }
        public AlarmFSMManager Alarm { get; private set; }
        public ExtractionZone Zone { get; private set; }
        public AudioSourcePool Pool => _pool;
        public LoopBus Loops => _loops;
        public SoundBank Bank => _bank;
        public MusicDirector Music => _music;

        private SoundBank _bank;
        private AudioSourcePool _pool;
        private AudioSourcePool _stepPool;
        private AudioSourcePool _voicePool;
        private LoopBus _loops;
        private MusicDirector _music;
        private PushToCastController _pushToCast;
        private AudioListener _listener;
        private SoundEntry _voiceEntry;

        private readonly HashSet<string> _reported = new HashSet<string>();
        private readonly Dictionary<string, float> _lastPlayed = new Dictionary<string, float>();
        private readonly HashSet<int> _subscribedDoors = new HashSet<int>();
        private readonly HashSet<int> _subscribedGuards = new HashSet<int>();
        private readonly HashSet<int> _steppers = new HashSet<int>();
        private float _playerScanAt;
        private float _pollAt;
        private float _doorScanAt;
        private int _lastHaulCount;

        /// <summary>Wires the director to a bank. Runs once, from <see cref="AudioBootstrapper"/> or a test.</summary>
        public void Initialize(SoundBank bank, bool withSceneLayers = true)
        {
            Instance = this;
            _bank = bank;
            _pool = new AudioSourcePool(transform, PoolSize);
            _stepPool = new AudioSourcePool(transform, StepPoolSize);
            _voicePool = new AudioSourcePool(transform, VoicePoolSize);
            _loops = new LoopBus(transform, LoopSlots);

            AudioLevels.Bind(bank.Mixer);

            if (withSceneLayers)
            {
                _music = gameObject.AddComponent<MusicDirector>();
                _music.Initialize(this);
                gameObject.AddComponent<ImpactAudio>().Initialize(this);
                gameObject.AddComponent<GuardVoiceDirector>().Initialize(this);
            }
        }

        private void OnEnable()
        {
            SpellCastingSystem.PhraseResolved += OnPhraseResolved;
            SpellCastingSystem.CastResolved += OnCastResolved;
            Damage.Dealt += OnDamageDealt;
        }

        private void OnDisable()
        {
            SpellCastingSystem.PhraseResolved -= OnPhraseResolved;
            SpellCastingSystem.CastResolved -= OnCastResolved;
            Damage.Dealt -= OnDamageDealt;

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
            AudioLevels.Settle(Time.unscaledDeltaTime);

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
        public AudioSource Play(string soundName, Vector3 position, float volumeScale = 1f, int variant = -1,
            SoundPoolKind pool = SoundPoolKind.General, bool flat = false)
        {
            if (_bank == null || soundName == null)
                return null;

            if (!_bank.TryGet(soundName, out SoundEntry entry) || entry.Clips == null || entry.Clips.Length == 0)
            {
                if (_reported.Add(soundName))
                    Debug.LogWarning("[Audio] No sound named '" + soundName + "' in the SoundBank.");
                return null;
            }

            // Checked after the lookup so a misspelt name is still reported while its group is muted.
            if (!SoundFocus.Allows(soundName))
                return null;

            // Only the general pool drops a repeat within 40 ms: two guards taking a step in the same frame are two sounds.
            if (pool == SoundPoolKind.General)
            {
                float now = Time.unscaledTime;
                if (_lastPlayed.TryGetValue(soundName, out float last) && now - last < MinRepeatSeconds)
                    return null;
                _lastPlayed[soundName] = now;
            }

            int index = variant >= 0 ? variant % entry.Clips.Length : Random.Range(0, entry.Clips.Length);
            AudioClip clip = entry.Clips[index];
            if (clip == null)
                return null;

            AudioSource source = AcquireFor(pool, position);
            if (source == null)
                return null;

            source.Stop();
            source.clip = clip;
            source.outputAudioMixerGroup = entry.Group;
            source.loop = false;
            source.volume = entry.Volume * volumeScale;
            source.pitch = 1f + Random.Range(-entry.PitchRange, entry.PitchRange);
            source.spatialBlend = entry.ThreeD && !flat ? 1f : 0f;
            source.minDistance = 2f;
            source.maxDistance = NoiseMaxDistance[(int)entry.Noise];
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.transform.position = position;
            source.Play();
            return source;
        }

        /// <summary>
        /// Plays a clip made at run time (a re-voiced guard line) through the voice pool and the mixer group the
        /// guard voices use, so the volume sliders reach it. Returns null when no voice source is free.
        /// </summary>
        public AudioSource PlayClip(AudioClip clip, Vector3 position, float volumeScale, SoundPoolKind pool)
        {
            if (_bank == null || clip == null || !TryFindVoiceEntry(out SoundEntry reference))
                return null;

            AudioSource source = AcquireFor(pool, position);
            if (source == null)
                return null;

            source.Stop();
            source.clip = clip;
            source.outputAudioMixerGroup = reference.Group;
            source.loop = false;
            source.volume = volumeScale;
            source.pitch = 1f;
            source.spatialBlend = 1f;
            source.minDistance = 2f;
            source.maxDistance = NoiseMaxDistance[(int)reference.Noise];
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.transform.position = position;
            source.Play();
            return source;
        }

        // The recorded guard lines are not in the SoundBank, so they borrow the group and reach of the
        // old guard voices, the bank's first "vo_" entry.
        private bool TryFindVoiceEntry(out SoundEntry entry)
        {
            if (_voiceEntry == null)
            {
                foreach (SoundEntry candidate in _bank.Entries)
                {
                    if (candidate.Name.StartsWith("vo_", System.StringComparison.Ordinal) && candidate.Group != null)
                    {
                        _voiceEntry = candidate;
                        break;
                    }
                }
            }
            entry = _voiceEntry;
            return entry != null;
        }

        /// <summary>Voices are capped at six at once and the nearest win: a new line takes a free source, else the farthest one playing if it is farther than the new line.</summary>
        private AudioSource AcquireFor(SoundPoolKind kind, Vector3 position)
        {
            switch (kind)
            {
                case SoundPoolKind.Step:
                    return _stepPool.Acquire();
                case SoundPoolKind.Voice:
                    return AcquireVoice(position);
                default:
                    return _pool.Acquire();
            }
        }

        private AudioSource AcquireVoice(Vector3 position)
        {
            Vector3 listener = ListenerPosition;
            float newDistance = (position - listener).sqrMagnitude;
            AudioSource farthest = null;
            float farthestDistance = -1f;
            for (int i = 0; i < _voicePool.Size; i++)
            {
                AudioSource source = _voicePool[i];
                if (!source.isPlaying)
                    return source;
                float distance = (source.transform.position - listener).sqrMagnitude;
                if (distance > farthestDistance)
                {
                    farthestDistance = distance;
                    farthest = source;
                }
            }
            return farthestDistance > newDistance ? farthest : null;
        }

        public int VoicesPlaying
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _voicePool.Size; i++)
                {
                    if (_voicePool[i].isPlaying)
                        count++;
                }
                return count;
            }
        }

        /// <summary>Where the listener is, for the systems that decide by distance.</summary>
        public Vector3 Listener => ListenerPosition;

        private Vector3 ListenerPosition => _listener != null ? _listener.transform.position : Vector3.zero;

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
            if (report.Spell == SpellId.Velox)
                Play(SoundNames.Dodge, report.Origin);
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
            // The raid hears through a fallback listener until this machine's player spawns, then
            // through the player's camera, so a listener that has been switched off is dropped.
            if (_listener == null || !_listener.isActiveAndEnabled)
                _listener = FindEnabledListener();

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

            bool guardsStep = SoundFocus.Allows(GuardFoleyGroupName);
            for (int i = 0; i < guards.Count && guardsStep; i++)
            {
                CastleGuard guard = guards[i];
                if (guard != null && _steppers.Add(guard.GetInstanceID()))
                    guard.gameObject.AddComponent<StepAudio>().Initialize(this, GuardVoices.Resolve(guard.name), null);
            }

            if (Time.unscaledTime >= _playerScanAt)
            {
                _playerScanAt = Time.unscaledTime + PlayerScanSeconds;
                PlayerStateMachine[] players = FindObjectsByType<PlayerStateMachine>(FindObjectsSortMode.None);
                for (int i = 0; i < players.Length; i++)
                {
                    if (_steppers.Add(players[i].GetInstanceID()))
                        players[i].gameObject.AddComponent<StepAudio>().Initialize(this, default, players[i]);
                }
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

        private static AudioListener FindEnabledListener()
        {
            foreach (AudioListener listener in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
                if (listener.isActiveAndEnabled)
                    return listener;
            return null;
        }
    }
}
