using System.Collections.Generic;
using StateMachine;
using UnityEngine;

namespace Plunderspell.Audio
{
    /// <summary>
    /// Footsteps, jumps and landings for one walking character. Added at run time by AudioDirector to
    /// every player and guard, so no prefab is edited. A step is one stride of distance covered while
    /// on the ground, so a sprint steps faster and standing still is silent; nothing here depends on an
    /// animation event. See docs/4-systems/audio.md.
    /// </summary>
    public sealed class StepAudio : MonoBehaviour
    {
        private const float HearingDistance = 45f;
        private const float MinMovingSpeed = 0.4f;
        private const float TeleportDistance = 3f;
        private const float LocalLevel = 0.35f;
        private const float GearLevel = 0.4f;
        private const float JumpRiseSpeed = 2f;
        private const float IdleResetSeconds = 0.4f;

        private static readonly Dictionary<int, Surface> SurfaceCache = new Dictionary<int, Surface>();

        private AudioDirector _director;
        private GuardVoiceProfile _profile;
        private PlayerStateMachine _player;
        private Vector3 _last;
        private float _travelled;
        private float _windowSeconds;
        private float _idleSeconds;
        private bool _wasGrounded = true;
        private float _fallSpeed;
        private Surface _surface;

        public Surface CurrentSurface => _surface;

        public void Initialize(AudioDirector director, GuardVoiceProfile profile, PlayerStateMachine player)
        {
            _director = director;
            _profile = profile;
            _player = player;
            _last = transform.position;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || _director == null)
                return;

            Vector3 position = transform.position;
            Vector3 delta = position - _last;
            _last = position;
            float vertical = delta.y / dt;
            delta.y = 0f;
            float travelled = delta.magnitude;
            if (travelled > TeleportDistance)
                travelled = 0f;

            if (_player != null && _player.dead)
                return;

            bool near = (position - _director.Listener).sqrMagnitude < HearingDistance * HearingDistance
                        || (_player != null && _player.IsLocal);
            bool grounded = near ? Probe(position) : _wasGrounded;

            if (_player != null)
                TrackAir(grounded, vertical, position);

            _wasGrounded = grounded;
            if (!grounded)
                return;

            if (travelled / dt < MinMovingSpeed)
            {
                _idleSeconds += dt;
                if (_idleSeconds > IdleResetSeconds)
                {
                    _travelled = 0f;
                    _windowSeconds = 0f;
                }
                return;
            }

            _idleSeconds = 0f;
            _travelled += travelled;
            _windowSeconds += dt;
            float speed = _travelled / _windowSeconds;
            if (_travelled >= StepMath.Stride(speed))
            {
                _travelled = 0f;
                _windowSeconds = 0f;
                if (near)
                    Step(position, speed);
            }
        }

        private bool Probe(Vector3 position)
        {
            bool hit = Physics.Raycast(position + Vector3.up * 0.3f, Vector3.down, out RaycastHit info, 2.5f,
                Physics.AllLayers, QueryTriggerInteraction.Ignore);
            if (hit)
                _surface = SurfaceOf(info.collider);

            if (_player != null)
                return _player.IsGrounded;
            return hit && info.distance < 1.2f;
        }

        private void TrackAir(bool grounded, float vertical, Vector3 position)
        {
            bool local = _player.IsLocal;
            if (!grounded)
            {
                if (_wasGrounded && vertical > JumpRiseSpeed)
                    _director.Play(SoundNames.Jump, position, local ? LocalLevel + 0.3f : 1f, -1, SoundPoolKind.Step, local);
                if (vertical < 0f && -vertical > _fallSpeed)
                    _fallSpeed = -vertical;
                return;
            }

            if (!_wasGrounded)
            {
                string land = StepMath.Land(_fallSpeed);
                if (land != null)
                    _director.Play(land, position, local ? LocalLevel + 0.3f : 1f, -1, SoundPoolKind.Step, local);
            }
            _fallSpeed = 0f;
        }

        private void Step(Vector3 position, float speed)
        {
            bool local = _player != null && _player.IsLocal;
            float level = StepMath.Loudness(speed) * (local ? LocalLevel : 1f);
            string step = _profile.Hound ? "foley_step_hound" : SurfaceLookup.StepSound(_surface);
            _director.Play(step, position, level, -1, SoundPoolKind.Step, local);

            string gear = GuardVoices.GearSound(_profile);
            if (gear != null)
                _director.Play(gear, position, level * GearLevel, -1, SoundPoolKind.Step, false);
        }

        private static Surface SurfaceOf(Collider collider)
        {
            int id = collider.GetInstanceID();
            if (SurfaceCache.TryGetValue(id, out Surface known))
                return known;

            Surface surface = SurfaceLookup.FromNames(collider.name, null);
            Renderer renderer = collider.GetComponentInParent<Renderer>();
            if (renderer != null)
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null)
                        continue;
                    Surface fromMaterial = SurfaceLookup.FromNames(collider.name, materials[i].name);
                    if (fromMaterial == Surface.Earth)
                    {
                        surface = Surface.Earth;
                        break;
                    }
                }
            }

            SurfaceCache[id] = surface;
            return surface;
        }
    }
}
