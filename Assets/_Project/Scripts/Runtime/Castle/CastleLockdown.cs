using RogueAi.Alarm;
using UnityEngine;

namespace RogueAi.Castle
{
    /// <summary>
    /// Closes the castle as the alarm rises. This is the consequence that gives the alarm teeth:
    /// without it, a loud raid is only a matter of more guards, and the players' route out stays
    /// exactly as open as it was on the way in.
    ///
    /// <list type="bullet">
    /// <item><see cref="AlarmState.Roused"/> — every door is locked. Hand-opening stops working, so
    /// the way out now costs a Porta or a very loud shoulder.</item>
    /// <item><see cref="AlarmState.HueAndCry"/> — every door is barred as well, so forcing is the only
    /// option left short of the word.</item>
    /// </list>
    ///
    /// Deliberately one-way. The alarm itself latches at Roused and never decays, and a castle that
    /// quietly unlocked itself would hand back a mistake the players already paid for.
    /// </summary>
    public class CastleLockdown : MonoBehaviour
    {
        [Tooltip("The alarm to follow. Found in the scene when left empty.")]
        [SerializeField] private AlarmFSMManager _alarm;

        /// <summary>True once the doors have been locked.</summary>
        public bool IsLockedDown { get; private set; }

        /// <summary>True once the doors have been barred as well.</summary>
        public bool IsBarred { get; private set; }

        /// <summary>Doors affected by the most recent lockdown. Surfaced for tests and tooling.</summary>
        public int DoorsAffected { get; private set; }

        private void Awake()
        {
            if (_alarm == null)
                _alarm = FindObjectOfType<AlarmFSMManager>();
        }

        private void OnEnable()
        {
            if (_alarm != null)
                _alarm.AlarmStateChanged += OnAlarmStateChanged;
        }

        private void OnDisable()
        {
            if (_alarm != null)
                _alarm.AlarmStateChanged -= OnAlarmStateChanged;
        }

        /// <summary>
        /// Applies the lockdown appropriate to an alarm state. Public and network-free so the rule is
        /// testable directly, and so a director can re-apply it to doors spawned after the alarm rose.
        /// </summary>
        public void ApplyAlarmState(AlarmState state)
        {
            if (state >= AlarmState.Roused && !IsLockedDown)
                LockAllDoors();

            if (state >= AlarmState.HueAndCry && !IsBarred)
                BarAllDoors();
        }

        private void OnAlarmStateChanged(AlarmState state) => ApplyAlarmState(state);

        /// <summary>Locks every door in the scene. Porta still opens them; hands no longer do.</summary>
        public void LockAllDoors()
        {
            IsLockedDown = true;
            CastleDoor[] doors = FindObjectsOfType<CastleDoor>();
            DoorsAffected = doors.Length;

            foreach (CastleDoor door in doors)
                door.Lock();

            Debug.Log($"[Lockdown] The castle locks its doors ({doors.Length}).");
        }

        /// <summary>Bars every door as well — forcing or a word, nothing else.</summary>
        public void BarAllDoors()
        {
            IsBarred = true;
            CastleDoor[] doors = FindObjectsOfType<CastleDoor>();
            DoorsAffected = doors.Length;

            foreach (CastleDoor door in doors)
            {
                door.Lock();
                door.Bar();
            }

            Debug.Log($"[Lockdown] The castle bars its doors ({doors.Length}).");
        }

        /// <summary>Reopens the castle. Called between raids, never during one.</summary>
        public void Reset()
        {
            IsLockedDown = false;
            IsBarred = false;
            DoorsAffected = 0;
        }

        /// <summary>Wires the lockdown from code, for tests and tooling-built scenes.</summary>
        public void Configure(AlarmFSMManager alarm)
        {
            if (_alarm != null)
                _alarm.AlarmStateChanged -= OnAlarmStateChanged;

            _alarm = alarm;

            if (_alarm != null && isActiveAndEnabled)
                _alarm.AlarmStateChanged += OnAlarmStateChanged;
        }
    }
}
