using System.Collections.Generic;
using Plunderspell.Alarm;
using Plunderspell.Guards;
using UnityEngine;

namespace Plunderspell.Audio
{
    /// <summary>Speaks each guard's lines from replicated state. Limits and reasoning: docs/4-systems/audio.md.</summary>
    public sealed class GuardVoiceDirector : MonoBehaviour
    {
        private const float LineGapSeconds = 2f;
        private const float MurmurQuietSeconds = 8f;
        private const float MurmurMin = 8f;
        private const float MurmurMax = 20f;
        private const float AsleepEvery = 6f;
        private const float HearingDistance = 45f;
        private const float PantDistance = 12f;

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
        }

        private AudioDirector _director;
        private readonly Dictionary<int, Record> _records = new Dictionary<int, Record>();
        private EnemyDirector _alarm;

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
                        NextMurmurAt = now + Random.Range(MurmurMin, MurmurMax),
                        NextAsleepAt = now + AsleepEvery
                    };
                    _records[id] = record;
                }

                if (!record.Profile.Valid)
                    continue;

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
                if (record.Gone || record.SeenFrame == Time.frameCount || record.Guard != null)
                    continue;

                record.Gone = true;
                if (!raiding || !record.Profile.Valid)
                    continue;
                if ((record.LastHead - listener).sqrMagnitude > HearingDistance * HearingDistance)
                    continue;

                string name = GuardVoices.LineName(record.Profile, GuardLine.Death);
                if (name != null)
                    _director.Play(name, record.LastHead, 1f, -1, SoundPoolKind.Voice);
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

            if (state == GuardAlertState.Incapacitated)
                record.NextMurmurAt = now + Random.Range(MurmurMin, MurmurMax);
            if (line == GuardLine.Murmur)
                record.NextMurmurAt = now + Random.Range(MurmurMin, MurmurMax);
            if (line == GuardLine.Asleep)
                record.NextAsleepAt = now + AsleepEvery;

            if (line == null || !audible)
                return;
            if (now - record.LastLineAt < LineGapSeconds)
                return;

            string name = GuardVoices.LineName(record.Profile, line.Value);
            if (name != null && _director.Play(name, head, 1f, -1, SoundPoolKind.Voice) != null)
                record.LastLineAt = now;
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
