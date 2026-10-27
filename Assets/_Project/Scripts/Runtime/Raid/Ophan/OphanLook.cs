using StateMachine;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// How the Ophan of Hours looks on this machine (#440, approved 2026-10-27): its eye follows its target, its three
    /// rings spin at a speed set by its state, its eye pulses through the lock-on and swells and brightens through the
    /// charge, its beams are drawn from the eye, and it spins down and rises away when it leaves. It only shows what
    /// <see cref="OphanOfHours"/> tells it; nothing here decides anything.
    /// </summary>
    public sealed class OphanLook : MonoBehaviour
    {
        // Ring speeds as multiples of the hover clip's own spin (pick 4A): fastest charging, half that locking on, a
        // slow turn watching, stopped as it leaves. The clip turns each ring about its own axis at its own rate, measured
        // from ophan_hover_turn (ophan_spread, the held sky pose, copies its wheels): GreatWheel about -Z at 60 deg/s,
        // InnerWheel about +X at 80.5, TiltWheel about a tilted axis at 75.5.
        private const float WatchSpin = 0.25f, LockSpin = 1.5f, ChargeSpin = 3f;
        private static readonly Vector3 GreatAxis = new Vector3(0f, 0f, -1f), InnerAxis = new Vector3(1f, 0f, 0f),
            TiltAxis = new Vector3(0.10f, 0.95f, 0.29f).normalized;
        private const float GreatRate = 60f, InnerRate = 80.5f, TiltRate = 75.5f;
        // At a kilometre a player is a few degrees off its centre; the eye's turn is magnified so it can be seen to follow.
        private const float EyeFollowGain = 8f, EyeFollowMaxDegrees = 35f;
        private const float BeamWidthAtEye = 40f, BeamWidthAtTarget = 0.8f, ChargedWidthAtEye = 90f, ChargedWidthAtTarget = 8f;
        // The beam fades out over its last metres. Seen from the target's own eyes it runs from around the camera to the
        // sky, and a shaft that close fills the view, so on the target's machine it fades out further away.
        private const float FadeMetresOnTarget = 25f, FadeMetresSeenByOthers = 2f;
        private const float LiftGrowthPerMetre = 0.03f;
        private const float ChargedFlashSeconds = 0.6f;

        private OphanTuning _tuning;
        private Transform _eye, _great, _inner, _tilt;
        private BonePose _eyePose, _greatPose, _innerPose, _tiltPose;
        private float _greatAngle, _innerAngle, _tiltAngle, _spin;
        private Renderer[] _renderers;
        private MaterialPropertyBlock _block;
        private Color _baseEmission;
        private OphanBeamShaft _beam;
        private Vector3 _flashTarget;
        private float _flashUntil = float.NegativeInfinity;
        private Vector3 _restScale;
        private Vector3 _restPosition;
        private Transform _lifted;
        private Vector3 _liftFrom;
        private float _liftStart;

        public OphanState State { get; private set; } = OphanState.Arriving;
        public Transform Target { get; private set; }
        public bool Beaming { get; private set; }
        private float _stateSince;

        /// <summary>The eye's world position, where every beam starts.</summary>
        public Vector3 EyePoint => _eye != null ? _eye.position : transform.position;

        public void Init(OphanTuning tuning)
        {
            _tuning = tuning;
            _eye = Find("Eye");
            _great = Find("GreatWheel");
            _inner = Find("InnerWheel");
            _tilt = Find("TiltWheel");
            _eyePose = new BonePose(_eye);
            _greatPose = new BonePose(_great);
            _innerPose = new BonePose(_inner);
            _tiltPose = new BonePose(_tilt);
            _renderers = GetComponentsInChildren<Renderer>();
            _block = new MaterialPropertyBlock();
            Material shared = _renderers.Length > 0 ? _renderers[0].sharedMaterial : null;
            _baseEmission = shared != null && shared.HasProperty("_EmissionColor") ? shared.GetColor("_EmissionColor") : Color.white;
            _restScale = transform.localScale;
            _restPosition = transform.position;
            _beam = new OphanBeamShaft();
            _stateSince = Time.time;
            _spin = 1f; // arriving, the rings turn as the clip turns them
        }

        public void Show(OphanState state, Transform target, bool beaming)
        {
            if (state != State)
                _stateSince = Time.time;
            State = state;
            Target = target;
            Beaming = beaming;
        }

        /// <summary>The charged beam's flash to where it struck.</summary>
        public void FlashCharged(Vector3 at)
        {
            _flashTarget = at;
            _flashUntil = Time.time + ChargedFlashSeconds;
        }

        /// <summary>
        /// The take: a beam flashes to the piece and a copy of it rises into the sky toward the eye as the Ophan leaves.
        /// Only a copy, made on this machine; the host despawns the real piece for everyone.
        /// </summary>
        public void Lift(GameObject piece)
        {
            FlashCharged(piece.transform.position);
            if (_lifted != null)
                Destroy(_lifted.gameObject);
            _lifted = CopyLook(piece).transform;
            foreach (Renderer shown in piece.GetComponentsInChildren<Renderer>())
                shown.enabled = false;
            _liftFrom = _lifted.position;
            _liftStart = Time.time;
        }

        // per-frame: the eye follows a moving player and the rings turn every frame, over the clip's own pose
        private void LateUpdate()
        {
            if (_tuning == null)
                return;
            float inState = Time.time - _stateSince;
            SpinRings(inState);
            ShowTells(inState);
            DrawBeam();
            DriftAway(inState);
            RaiseLifted();
        }

        private void OnDestroy()
        {
            if (_lifted != null)
                Destroy(_lifted.gameObject);
            _beam?.Destroy();
        }

        private void RaiseLifted()
        {
            if (_lifted == null)
                return;
            float t = (Time.time - _liftStart) / Mathf.Max(0.01f, _tuning.LeaveSeconds);
            if (t >= 1f)
            {
                Destroy(_lifted.gameObject);
                return;
            }
            // Slow off the floor, then fast: it is drawn up, not thrown.
            _lifted.position = Vector3.Lerp(_liftFrom, EyePoint, t * t * t);
            _lifted.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);
            // It grows as it rises so it still reads hundreds of metres up, as a thing being taken in.
            _lifted.localScale = Vector3.one * (1f + LiftGrowthPerMetre * Vector3.Distance(_liftFrom, _lifted.position));
        }

        private static GameObject CopyLook(GameObject piece)
        {
            var copy = new GameObject("TakenPiece");
            copy.transform.SetPositionAndRotation(piece.transform.position, piece.transform.rotation);
            foreach (MeshRenderer source in piece.GetComponentsInChildren<MeshRenderer>())
            {
                var filter = source.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                    continue;
                var part = new GameObject(source.name);
                part.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
                part.transform.localScale = source.transform.lossyScale;
                part.transform.SetParent(copy.transform, true);
                part.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                part.AddComponent<MeshRenderer>().sharedMaterials = source.sharedMaterials;
            }
            return copy;
        }

        private void SpinRings(float inState)
        {
            float wanted = State switch
            {
                OphanState.Charging => ChargeSpin * Mathf.Lerp(0.5f, 1f, Mathf.Clamp01(inState / _tuning.ChargeSeconds)),
                OphanState.Locking => LockSpin,
                OphanState.Draining => LockSpin,
                OphanState.Leaving => 0f,
                _ => WatchSpin,
            };
            // Ease toward the wanted speed so the rings wind up and spin down rather than jump.
            _spin = Mathf.MoveTowards(_spin, wanted, 1.5f * Time.deltaTime);
            // The clip already turns each ring at 1x; add the rest (negative slows it, -1x stops it).
            float extra = (_spin - 1f) * Time.deltaTime;
            _greatAngle += extra * GreatRate;
            _innerAngle += extra * InnerRate;
            _tiltAngle += extra * TiltRate;
            _greatPose.Set(Quaternion.AngleAxis(_greatAngle, GreatAxis), Vector3.one);
            _innerPose.Set(Quaternion.AngleAxis(_innerAngle, InnerAxis), Vector3.one);
            _tiltPose.Set(Quaternion.AngleAxis(_tiltAngle, TiltAxis), Vector3.one);
        }

        /// <summary>The eye's turn toward its target, in the eye's parent space, magnified so it shows from a kilometre.</summary>
        private Quaternion EyeTurn()
        {
            if (_eye == null || Target == null || State == OphanState.Leaving)
                return Quaternion.identity;
            Vector3 toTarget = (Target.position - _eye.position).normalized;
            Quaternion turn = Quaternion.FromToRotation(transform.forward, toTarget);
            turn.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f)
                angle -= 360f;
            float shown = Mathf.Clamp(angle * EyeFollowGain, -EyeFollowMaxDegrees, EyeFollowMaxDegrees);
            Vector3 localAxis = _eye.parent != null ? _eye.parent.InverseTransformDirection(axis) : axis;
            return Quaternion.AngleAxis(shown, localAxis);
        }

        private void ShowTells(float inState)
        {
            if (_eye == null)
                return;
            float glow = 1f;
            Vector3 eyeScale = Vector3.one;
            if (State == OphanState.Locking)
            {
                // Lock-on (pick 5A): the eye narrows and pulses, the pulse quickening over the lock.
                float progress = Mathf.Clamp01(inState / _tuning.LockSeconds);
                float hz = Mathf.Lerp(1f, 6f, progress);
                float pulse = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * hz * inState);
                eyeScale = new Vector3(1f, Mathf.Lerp(1f, 0.55f, progress), 1f);
                glow = 1f + 2f * pulse * progress;
            }
            else if (State == OphanState.Charging)
            {
                // Charge: the eye swells to half again its size and brightens steadily to full blast.
                float progress = Mathf.Clamp01(inState / _tuning.ChargeSeconds);
                eyeScale = Vector3.one * Mathf.Lerp(1f, 1.5f, progress);
                glow = Mathf.Lerp(1f, 6f, progress);
            }
            else if (State == OphanState.Draining && Beaming)
            {
                glow = 2.5f;
            }
            _eyePose.Set(EyeTurn(), eyeScale, turnFirst: true);
            foreach (Renderer r in _renderers)
            {
                r.GetPropertyBlock(_block);
                _block.SetColor("_EmissionColor", _baseEmission * glow);
                r.SetPropertyBlock(_block);
            }
        }

        private void DrawBeam()
        {
            bool flashing = Time.time < _flashUntil;
            bool slow = Beaming && Target != null && State == OphanState.Draining;
            // The take: after its flash, the slow beam's look holds on the piece the whole way up.
            bool lifting = !flashing && _lifted != null;
            _beam.Visible = flashing || slow || lifting;
            if (!_beam.Visible)
                return;
            Vector3 end = flashing ? _flashTarget : lifting ? _lifted.position : Target.position + Vector3.up * 1.2f;
            // Drawn on both faces and added onto the scene, so it is half the old ribbon's strength.
            Color colour = flashing ? new Color(1f, 1f, 1f, 1f) : new Color(1f, 0.85f, 0.55f, 0.45f);
            // The charged flash fades near the player it strikes too, or its 8 m end whites out the wall bursting.
            bool onLocalPlayer = PlayerStateMachine.Local != null && (flashing
                ? Vector3.Distance(end, PlayerStateMachine.Local.transform.position) < 3f
                : Target == PlayerStateMachine.Local.transform);
            _beam.Draw(EyePoint, end,
                flashing ? ChargedWidthAtEye : BeamWidthAtEye,
                flashing ? ChargedWidthAtTarget : BeamWidthAtTarget,
                colour, onLocalPlayer ? FadeMetresOnTarget : FadeMetresSeenByOthers);
        }

        private void DriftAway(float inState)
        {
            if (State != OphanState.Leaving)
                return;
            float t = Mathf.Clamp01(inState / Mathf.Max(0.01f, _tuning.LeaveSeconds));
            transform.localScale = _restScale * Mathf.Lerp(1f, 0.05f, t * t);
            transform.position = _restPosition + Vector3.up * (t * _tuning.Distance * 0.5f);
        }

        /// <summary>
        /// Lays an offset over a bone's pose without compounding: if the Animator wrote the bone this frame its pose is
        /// the base, otherwise the base kept from before the last offset is.
        /// </summary>
        private sealed class BonePose
        {
            private readonly Transform _bone;
            private Quaternion _baseRotation, _written;
            private Vector3 _baseScale, _writtenScale;

            public BonePose(Transform bone)
            {
                _bone = bone;
                if (bone == null)
                    return;
                _baseRotation = _written = bone.localRotation;
                _baseScale = _writtenScale = bone.localScale;
            }

            public void Set(Quaternion offset, Vector3 scale, bool turnFirst = false)
            {
                if (_bone == null)
                    return;
                if (_bone.localRotation != _written)
                    _baseRotation = _bone.localRotation;
                if (_bone.localScale != _writtenScale)
                    _baseScale = _bone.localScale;
                _written = turnFirst ? offset * _baseRotation : _baseRotation * offset;
                _writtenScale = Vector3.Scale(_baseScale, scale);
                _bone.localRotation = _written;
                _bone.localScale = _writtenScale;
            }
        }

        private Transform Find(string bone)
        {
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
                if (t.name == bone)
                    return t;
            Debug.LogError($"[Ophan] no {bone} bone under {name}; that part will not move.", this);
            return null;
        }
    }
}
