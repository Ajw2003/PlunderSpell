using System.Collections.Generic;
using Plunderspell.Alarm;
using Plunderspell.Audio.VoiceBank;
using Plunderspell.Guards;
using UnityEngine;

namespace Plunderspell.Audio
{
    /// <summary>Speaks each guard's lines from replicated state. Limits and reasoning: docs/4-systems/audio.md.</summary>
    public sealed class GuardVoiceDirector : MonoBehaviour
    {
        private const float LineGapSeconds = 2f;
        // How long a line waits for its clip to finish rendering before it is dropped.
        private const float PendingSeconds = 1f;
        private const float MurmurQuietSeconds = 8f;
        private const float MurmurMin = 8f;
        private const float MurmurMax = 20f;
        private const float AsleepEvery = 6f;
        private const float HearingDistance = 45f;
        private const float PantDistance = 12f;
        private const float QuietGain = 0.5f;
        private const float NormalGain = 0.75f;
        private const float LoudGain = 1f;
        private const string SpeechGroupPrefix = "guardspeech_";

        private sealed class Record
        {
            public GuardVoiceProfile Profile;
            public GuardAlertState State;
            public int AttackCount;
            public float Health;
            public Guard Guard;
            public Vector3 LastHead;
            public int SeenFrame;
            public bool Gone;
            public float LastLineAt = -100f;
            public float NextMurmurAt;
            public float NextAsleepAt;
            public float SpeakingUntil;
            public GuardLine? PendingLine;
            public AudioClip PendingSource;
            public float PendingAt;
            public string Name;
            public int Seed;
            public Vector3 FirstPosition;
            public bool HasNetworkSeed;
            public bool HasDisguise;
            public DisguiseProfile Disguise;
        }

        private AudioDirector _director;
        private readonly Dictionary<int, Record> _records = new Dictionary<int, Record>();
        private EnemyDirector _alarm;
        private readonly System.Random _rng = new System.Random();
        private readonly GuardSpeechRenderer _renderer = new GuardSpeechRenderer();

        public void Initialize(AudioDirector director) => _director = director;

        private void OnDisable()
        {
            if (_alarm != null)
                _alarm.AlarmStateChanged -= OnAlarmStateChanged;
        }

        private void Update()
        {
            if (_director == null)
                return;

            _renderer.Pump();

            if (_alarm == null && _director.Alarm != null)
            {
                _alarm = _director.Alarm;
                _alarm.AlarmStateChanged += OnAlarmStateChanged;
            }

            float now = Time.time;
            Vector3 listener = _director.Listener;
            IReadOnlyList<Component> guards = EnemyDirector.GuardsOf(_director.Alarm);
            for (int i = 0; i < guards.Count; i++)
            {
                Guard guard = guards[i] as Guard;
                if (guard == null)
                    continue;

                int id = guard.GetInstanceID();
                if (!_records.TryGetValue(id, out Record record))
                {
                    record = new Record
                    {
                        Profile = GuardVoices.Resolve(guard.name),
                        State = guard.State,
                        AttackCount = guard.AttackSignal.Count,
                        Health = guard.CurrentHealth,
                        Guard = guard,
                        Name = guard.name,
                        FirstPosition = guard.transform.position,
                        NextMurmurAt = now + Random.Range(MurmurMin, MurmurMax),
                        NextAsleepAt = now + AsleepEvery
                    };
                    _records[id] = record;
                }

                if (!record.Profile.Valid)
                    continue;

                // The network id only exists once the guard has spawned; from then on it is the seed.
                if (!record.HasNetworkSeed && guard.objectId != 0)
                {
                    record.HasNetworkSeed = true;
                    record.HasDisguise = false;
                }

                Vector3 head = guard.transform.position + Vector3.up * 1.6f;
                record.LastHead = head;
                record.SeenFrame = Time.frameCount;
                bool audible = (head - listener).sqrMagnitude < HearingDistance * HearingDistance;
                Listen(guard, record, head, audible, now);

                if (record.Profile.Hound)
                    Pant(id, record, guard, head, listener);
            }

            SpeakDeaths(now, listener);
            _director.Loops.Tick(Time.unscaledDeltaTime);
        }

        // A dead guard topples and fades before it is destroyed (the Dead state), and the death line plays
        // when the body is removed. A guard that vanishes while the raid is running has died, and says so
        // from where it stood.
        private void SpeakDeaths(float now, Vector3 listener)
        {
            bool raiding = _director.Raid != null && _director.Raid.Phase == Plunderspell.Raid.RaidPhase.Raiding;
            foreach (KeyValuePair<int, Record> pair in _records)
            {
                Record record = pair.Value;
                if (record.Gone && record.PendingLine == GuardLine.Death)
                {
                    RetryPending(record, record.LastHead, now);
                    continue;
                }
                if (record.Gone || record.SeenFrame == Time.frameCount || record.Guard != null)
                    continue;

                record.Gone = true;
                if (!raiding || !record.Profile.Valid)
                    continue;
                if ((record.LastHead - listener).sqrMagnitude > HearingDistance * HearingDistance)
                    continue;

                if (record.Profile.Hound)
                {
                    string name = GuardVoices.LineName(record.Profile, GuardLine.Death);
                    if (name != null)
                        _director.Play(name, record.LastHead, 1f, -1, SoundPoolKind.Voice);
                }
                else
                {
                    Speak(record, GuardLine.Death, record.LastHead, now);
                }
            }
        }

        private void Listen(Guard guard, Record record, Vector3 head, bool audible, float now)
        {
            GuardAlertState state = guard.State;
            int attacks = guard.AttackSignal.Count;
            float health = guard.CurrentHealth;
            bool dead = guard.IsDead;

            GuardLine? line = null;
            if (!dead && health < record.Health - 0.01f)
                line = GuardLine.Hurt;
            else if (attacks != record.AttackCount)
                line = GuardLine.Attack;
            else if (state != record.State)
                line = GuardVoices.LineForStateChange(record.State, state);
            else if (!dead && state == GuardAlertState.Incapacitated && now >= record.NextAsleepAt)
                line = GuardLine.Asleep;
            else if (!dead && state == GuardAlertState.Patrolling && now >= record.NextMurmurAt
                     && now - record.LastLineAt >= MurmurQuietSeconds)
                line = GuardLine.Murmur;

            record.State = state;
            record.AttackCount = attacks;
            record.Health = health;

            // A line whose clip was still being rendered last frame is tried again, unless something newer came up.
            if (line == null && record.PendingLine.HasValue && record.PendingLine != GuardLine.Death)
            {
                if (now - record.PendingAt > PendingSeconds)
                    ClearPending(record);
                else
                    line = record.PendingLine;
            }

            if (state == GuardAlertState.Incapacitated)
                record.NextMurmurAt = now + Random.Range(MurmurMin, MurmurMax);
            if (line == GuardLine.Murmur)
                record.NextMurmurAt = now + Random.Range(MurmurMin, MurmurMax);
            if (line == GuardLine.Asleep)
                record.NextAsleepAt = now + AsleepEvery;

            if (line == null || !audible)
                return;
            if (now - record.LastLineAt < LineGapSeconds || now < record.SpeakingUntil)
                return;

            if (record.Profile.Hound)
            {
                string name = GuardVoices.LineName(record.Profile, line.Value);
                if (name != null && _director.Play(name, head, 1f, -1, SoundPoolKind.Voice) != null)
                    record.LastLineAt = now;
            }
            else if (Speak(record, line.Value, head, now))
            {
                record.LastLineAt = now;
            }
        }

        // Picks a recorded line for the guard's Age, re-voices it with the guard's own disguise and plays it
        // from its head. The guard is silent for the clip's length so its own lines do not overlap.
        private bool Speak(Record record, GuardLine line, Vector3 head, float now)
        {
            string situation = GuardSpeechBank.Situation(line);
            if (!SoundFocus.Allows(SpeechGroupPrefix + situation))
                return false;

            // The same clip is asked for again while it renders, so the wait ends once it is ready.
            AudioClip source = record.PendingLine == line ? record.PendingSource : null;
            if (source == null && !GuardSpeechBank.Shared.TryPick(record.Profile.Age, line, _rng, out source))
                return false;

            DisguiseProfile disguise = DisguiseOf(record);
            string key = record.Profile.Age + "/" + record.Profile.Voice + "/" + record.Seed;
            if (!_renderer.TryGet(source, disguise, key, out AudioClip clip))
            {
                if (record.PendingLine != line)
                {
                    record.PendingLine = line;
                    record.PendingAt = now;
                }
                record.PendingSource = source;
                return false;
            }
            ClearPending(record);

            AudioSource playing = _director.PlayClip(clip, head, SituationGain(line), SoundPoolKind.Voice);
            if (playing == null)
                return false;

            record.SpeakingUntil = now + clip.length;
            string who = record.Guard != null ? record.Guard.name : "(gone)";
            Debug.Log("[GuardSpeech] " + who + " (" + record.Profile.Age + "/" + record.Profile.Voice + ") " + situation + " "
                      + source.name + " pitch x" + disguise.PitchRatio.ToString("F2")
                      + " speed x" + disguise.Speed.ToString("F2"));
            return true;
        }

        private void RetryPending(Record record, Vector3 head, float now)
        {
            if (now - record.PendingAt > PendingSeconds)
                ClearPending(record);
            else
                Speak(record, record.PendingLine.Value, head, now);
        }

        private static void ClearPending(Record record)
        {
            record.PendingLine = null;
            record.PendingSource = null;
        }

        private DisguiseProfile DisguiseOf(Record record)
        {
            if (!record.HasDisguise)
            {
                ulong id = record.Guard != null ? record.Guard.objectId : 0;
                record.Seed = GuardVoiceProfiles.SeedFor(id, record.Name.Replace("(Clone)", string.Empty), record.FirstPosition);
                record.Disguise = GuardVoiceProfiles.For(record.Seed, record.Profile.Voice);
                record.HasDisguise = true;
            }
            return record.Disguise;
        }

        private static float SituationGain(GuardLine line)
        {
            switch (line)
            {
                case GuardLine.Murmur:
                case GuardLine.Lost:
                case GuardLine.Asleep: return QuietGain;
                case GuardLine.Chase:
                case GuardLine.Attack: return LoudGain;
                default: return NormalGain;
            }
        }

        // A hound pants while it is calm and near enough to hear, as a loop that fades in and out.
        private void Pant(int id, Record record, Guard guard, Vector3 head, Vector3 listener)
        {
            bool calm = guard.State == GuardAlertState.Patrolling && !guard.IsDead;
            if (!calm || (head - listener).sqrMagnitude > PantDistance * PantDistance)
                return;
            if (_director.Bank.TryGet("vo_hound_pant_loop", out SoundEntry entry))
                _director.Loops.Drive(id, entry, 0.6f, head);
        }

        // The howl doubles as a Roused call (audio.md 3.9): the nearest hound answers once.
        private void OnAlarmStateChanged(AlarmState state)
        {
            if (state < AlarmState.Roused)
                return;

            IReadOnlyList<Component> guards = EnemyDirector.GuardsOf(_director.Alarm);
            Guard nearest = null;
            float best = float.MaxValue;
            Vector3 listener = _director.Listener;
            for (int i = 0; i < guards.Count; i++)
            {
                Guard guard = guards[i] as Guard;
                if (guard == null || guard.IsDead || !GuardVoices.Resolve(guard.name).Hound)
                    continue;
                float distance = (guard.transform.position - listener).sqrMagnitude;
                if (distance < best)
                {
                    best = distance;
                    nearest = guard;
                }
            }

            if (nearest != null)
                _director.Play("vo_hound_howl", nearest.transform.position + Vector3.up, 1f, -1, SoundPoolKind.Voice);
        }
    }
}
