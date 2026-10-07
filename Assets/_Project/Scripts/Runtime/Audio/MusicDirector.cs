using Code.Scripts.EventSystems;
using Plunderspell.Alarm;
using Plunderspell.Core;
using Plunderspell.Extraction;
using Plunderspell.Inventory;
using Plunderspell.Raid;
using UnityEngine;

namespace Plunderspell.Audio
{
    /// <summary>What the music should be doing right now.</summary>
    public readonly struct MusicPlan
    {
        /// <summary>A single looping bed, or null when the raid stems play or nothing should change.</summary>
        public readonly string Bed;

        /// <summary>True when the current Age's stems should play, faded by alarm state.</summary>
        public readonly bool RaidStems;

        /// <summary>Leave whatever is playing alone (paused, settings, or a raid still being built).</summary>
        public readonly bool Hold;

        public MusicPlan(string bed, bool raidStems, bool hold)
        {
            Bed = bed;
            RaidStems = raidStems;
            Hold = hold;
        }
    }

    /// <summary>
    /// Title, Lair and results beds, and in a raid the current Age's four stems faded in by alarm state.
    /// Cross-fades on a timer, not on bar lines (bar-line quantising is Phase E of the audio plan).
    /// </summary>
    public sealed class MusicDirector : MonoBehaviour
    {
        private const int StemCount = 4;
        private const int BedA = StemCount;
        private const int BedB = StemCount + 1;
        private const float BedFadeSeconds = 1.5f;
        private const float StemFadeSeconds = 2f;
        private const float StemStartDelay = 0.1f;

        private static readonly float[] PortalWarningSeconds = { 120f, 60f, 30f };

        private AudioDirector _director;
        private AudioSource[] _layers;
        private float[] _target;
        private float[] _gain;
        private float[] _speed;
        private string _bed;
        private int _activeBed = BedA;
        private bool _stemsOn;
        private HistoricalEra _stemEra;
        private int _nextWarning;
        private bool _warningsArmed;

        // What decides the music, kept current by the events that change it (#304).
        private GameState _state = GameState.MainMenu;
        private bool _raidRunning;
        private int _alarmIndex;
        private HistoricalEra _raidEra;

        public string CurrentBed => _bed;
        public bool StemsPlaying => _stemsOn;
        public AudioSource[] Layers => _layers;

        /// <summary>The highest stem audible for an alarm state: 0 calm only, up to 3 with all four.</summary>
        public static int StemIndex(AlarmState state) => (int)state;

        /// <summary>Pure: what music a game state calls for.</summary>
        public static MusicPlan Choose(GameState state, bool raidRunning)
        {
            switch (state)
            {
                case GameState.MainMenu: return new MusicPlan("mus_title_loop", false, false);
                case GameState.Lair: return new MusicPlan("mus_lair_loop", false, false);
                case GameState.GameOver: return new MusicPlan("mus_results_failure_loop", false, false);
                case GameState.Victory: return new MusicPlan("mus_results_success_loop", false, false);
                case GameState.Playing:
                    return raidRunning ? new MusicPlan(null, true, false) : new MusicPlan(null, false, true);
                default:
                    return new MusicPlan(null, false, true);
            }
        }

        public void Initialize(AudioDirector director)
        {
            _director = director;
            int count = StemCount + 2;
            _layers = new AudioSource[count];
            _target = new float[count];
            _gain = new float[count];
            _speed = new float[count];
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject(i < StemCount ? "Stem" + i : "Bed" + (i - StemCount));
                go.transform.SetParent(transform, false);
                AudioSource source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = true;
                source.spatialBlend = 0f;
                source.volume = 0f;
                _layers[i] = source;
            }

            ReadCurrentState();
            Apply();
        }

        private void OnEnable()
        {
            EventManager bus = EventManager.Instance;
            if (bus == null)
                return;

            bus.Subscribe(this, (GameStateChanged e) => { _state = e.Current; Apply(); });
            bus.Subscribe(this, (RaidPhaseChanged e) =>
            {
                _raidRunning = e.Phase == RaidPhase.Raiding || e.Phase == RaidPhase.Extracting;
                Apply();
            });
            bus.Subscribe(this, (RaidContextPublished e) => { _raidEra = e.Context.Era; });
            bus.Subscribe(this, (AlarmChanged e) => { _alarmIndex = StemIndex(e.State); Apply(); });
            bus.Subscribe(this, (ExtractionTimerChanged e) => UpdatePortalWarning(e.SecondsRemaining));
        }

        private void OnDisable() => EventManager.Instance?.UnsubscribeFromAllEvents(this);

        // Only the crossfade runs every frame; what to play is decided when something changes.
        private void Update()
        {
            if (_layers == null)
                return;
            Fade(Time.unscaledDeltaTime);
        }

        /// <summary>Reads the game state, raid and alarm once, for when the director starts after the raid has.</summary>
        private void ReadCurrentState()
        {
            if (GameServices.GameState != null)
                _state = GameServices.GameState.CurrentState;
            RaidDirector raid = _director.Raid;
            if (raid != null)
            {
                _raidRunning = raid.Phase == RaidPhase.Raiding || raid.Phase == RaidPhase.Extracting;
                _raidEra = raid.Era;
            }
            EnemyDirector alarm = _director.Alarm;
            _alarmIndex = alarm != null ? StemIndex(alarm.State) : 0;
        }

        private void Apply()
        {
            if (_layers == null)
                return;

            MusicPlan plan = Choose(_state, _raidRunning);
            if (!plan.Hold)
            {
                if (plan.RaidStems)
                {
                    SetBed(null);
                    SetStems(true, _raidEra);
                }
                else
                {
                    SetStems(false, _stemEra);
                    SetBed(plan.Bed);
                }
            }

            if (_stemsOn)
            {
                for (int i = 0; i < StemCount; i++)
                    _target[i] = i <= _alarmIndex ? _gain[i] : 0f;
            }

            if (!_raidRunning)
                DisarmPortalWarning();
        }

        private void SetBed(string bedName)
        {
            if (bedName == _bed)
                return;

            _target[_activeBed] = 0f;
            _speed[_activeBed] = 1f / BedFadeSeconds;
            _bed = bedName;
            if (bedName == null)
                return;

            int next = _activeBed == BedA ? BedB : BedA;
            if (StartLayer(next, bedName, BedFadeSeconds, false))
                _activeBed = next;
            else
                _bed = null;
        }

        private void SetStems(bool on, HistoricalEra era)
        {
            if (on)
            {
                if (_stemsOn && _stemEra == era)
                    return;
                double start = AudioSettings.dspTime + StemStartDelay;
                for (int i = 0; i < StemCount; i++)
                    StartLayer(i, SoundNames.RaidStem(era, i), StemFadeSeconds, true, start);
                _stemEra = era;
                _stemsOn = true;
            }
            else if (_stemsOn)
            {
                for (int i = 0; i < StemCount; i++)
                {
                    _target[i] = 0f;
                    _speed[i] = 1f / StemFadeSeconds;
                }
                _stemsOn = false;
            }
        }

        private bool StartLayer(int layer, string soundName, float fadeSeconds, bool stem, double scheduled = 0d)
        {
            if (!SoundFocus.Allows(soundName))
                return false;
            if (!_director.Bank.TryGet(soundName, out SoundEntry entry) || entry.Clips.Length == 0 || entry.Clips[0] == null)
            {
                Debug.LogWarning("[Audio] No music named '" + soundName + "' in the SoundBank.");
                return false;
            }

            AudioSource source = _layers[layer];
            source.Stop();
            source.clip = entry.Clips[0];
            source.outputAudioMixerGroup = entry.Group;
            source.volume = 0f;
            source.loop = true;
            _gain[layer] = entry.Volume;
            _target[layer] = stem ? 0f : entry.Volume;
            _speed[layer] = 1f / fadeSeconds;
            if (scheduled > 0d)
                source.PlayScheduled(scheduled);
            else
                source.Play();
            return true;
        }

        private void Fade(float dt)
        {
            for (int i = 0; i < _layers.Length; i++)
            {
                AudioSource source = _layers[i];
                if (!source.isPlaying && _target[i] <= 0f)
                    continue;

                source.volume = Mathf.MoveTowards(source.volume, _target[i], _speed[i] * dt);

                // A silent stem keeps playing while the stems are on, so it is still in step when its alarm state arrives.
                bool keepRunning = i < StemCount && _stemsOn;
                if (source.volume <= 0f && _target[i] <= 0f && !keepRunning)
                    source.Stop();
            }
        }

        private void DisarmPortalWarning()
        {
            _warningsArmed = false;
            _nextWarning = 0;
        }

        /// <summary>The portal warning bell at 2 minutes, 1 minute and 30 seconds left, more urgent each time.</summary>
        private void UpdatePortalWarning(float left)
        {
            if (!_raidRunning)
            {
                DisarmPortalWarning();
                return;
            }

            if (!_warningsArmed)
            {
                // Arm only once the timer has been seen above the first warning, so a stale zero never rings it.
                _warningsArmed = left > PortalWarningSeconds[0];
                return;
            }

            if (_nextWarning < PortalWarningSeconds.Length && left > 0f && left <= PortalWarningSeconds[_nextWarning])
            {
                _director.Play(SoundNames.PortalWarning, Vector3.zero, 1f, _nextWarning);
                _nextWarning++;
            }
        }
    }
}
