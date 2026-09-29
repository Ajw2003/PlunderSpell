using System.Collections.Generic;
using Interfaces;
using Plunderspell.Loot;
using UnityEngine;

namespace Plunderspell.Audio
{
    /// <summary>
    /// Sounds for loose pieces: an impact when one hits something, its break, and a scrape or roll while
    /// it slides or rolls along the floor. The impact comes from <see cref="Item.Impacted"/>, the one
    /// hook added to gameplay code; the break from <see cref="LootValue.Ruined"/>. See
    /// docs/4-systems/audio.md.
    /// </summary>
    public sealed class ImpactAudio : MonoBehaviour
    {
        private const float MinImpactSpeed = 1.2f;
        private const float FullVolumeSpeed = 8f;
        private const float HeavyMass = 3f;
        private const float HeavySpeed = 6f;
        private const float RepeatSeconds = 0.12f;
        private const float MinSlideSpeed = 0.7f;
        private const float MinRollSpin = 2.5f;
        private const float TrackSeconds = 6f;
        private const int TrackedMax = 8;

        private struct Tracked
        {
            public Item Item;
            public Rigidbody Body;
            public Collider Collider;
            public LootMaterial Material;
            public float Until;
        }

        private AudioDirector _director;
        private readonly Tracked[] _tracked = new Tracked[TrackedMax];
        private readonly Dictionary<int, float> _lastImpact = new Dictionary<int, float>();
        private readonly Dictionary<int, LootMaterial> _materials = new Dictionary<int, LootMaterial>();

        public int TrackedCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < TrackedMax; i++)
                {
                    if (_tracked[i].Item != null)
                        count++;
                }
                return count;
            }
        }

        public void Initialize(AudioDirector director) => _director = director;

        private void OnEnable()
        {
            Item.Impacted += OnImpact;
            LootValue.Ruined += OnRuined;
        }

        private void OnDisable()
        {
            Item.Impacted -= OnImpact;
            LootValue.Ruined -= OnRuined;
        }

        private LootMaterial MaterialOf(Component piece)
        {
            int id = piece.GetInstanceID();
            if (_materials.TryGetValue(id, out LootMaterial known))
                return known;

            LootMaterial material = LootMaterials.FromName(NameOf(piece));
            _materials[id] = material;
            return material;
        }

        private static string NameOf(Component piece)
        {
            var value = piece.GetComponent<LootValue>();
            if (value != null && value.Item != null)
                return value.Item.name;
            return piece.name;
        }

        private void OnImpact(Item item, Collision collision)
        {
            float speed = collision.relativeVelocity.magnitude;
            if (speed < MinImpactSpeed)
                return;

            int id = item.GetInstanceID();
            float now = Time.unscaledTime;
            if (_lastImpact.TryGetValue(id, out float last) && now - last < RepeatSeconds)
                return;
            _lastImpact[id] = now;

            LootMaterial material = MaterialOf(item);
            Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : item.transform.position;

            bool body = collision.gameObject.GetComponentInParent<IHealth>() != null;
            string sound = body
                ? LootMaterials.Body
                : LootMaterials.Impact(material, item.Mass >= HeavyMass || speed >= HeavySpeed);
            _director.Play(sound, point, Mathf.Clamp(speed / FullVolumeSpeed, 0.15f, 1f), -1, SoundPoolKind.General, false);

            Track(item, material);
        }

        private void OnRuined(LootValue piece, float worthLost)
        {
            if (piece == null)
                return;
            string pieceName = piece.Item != null ? piece.Item.name : piece.name;
            LootMaterial material = LootMaterials.FromName(pieceName);
            _director.Play(LootMaterials.Break(material, pieceName), piece.transform.position);
        }

        private void Track(Item item, LootMaterial material)
        {
            int free = -1;
            for (int i = 0; i < TrackedMax; i++)
            {
                if (_tracked[i].Item == item)
                {
                    _tracked[i].Until = Time.unscaledTime + TrackSeconds;
                    return;
                }
                if (free < 0 && _tracked[i].Item == null)
                    free = i;
            }

            if (free < 0)
                return;
            _tracked[free] = new Tracked
            {
                Item = item,
                Body = item.GetComponent<Rigidbody>(),
                Collider = item.GetComponentInChildren<Collider>(),
                Material = material,
                Until = Time.unscaledTime + TrackSeconds
            };
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            LoopBus loops = _director.Loops;
            for (int i = 0; i < TrackedMax; i++)
            {
                Tracked t = _tracked[i];
                if (t.Item == null)
                    continue;
                if (now > t.Until || t.Body == null)
                {
                    _tracked[i] = default;
                    continue;
                }

                Vector3 velocity = t.Body.linearVelocity;
                float slide = new Vector2(velocity.x, velocity.z).magnitude;
                float spin = t.Body.angularVelocity.magnitude;
                if (t.Body.isKinematic || !OnFloor(t))
                    continue;

                string loop = null;
                float level = 0f;
                if (spin >= MinRollSpin && slide >= 0.3f)
                {
                    loop = LootMaterials.Roll;
                    level = Mathf.Clamp01(slide / 3f);
                }
                else if (slide >= MinSlideSpeed)
                {
                    loop = LootMaterials.Scrape(t.Material);
                    level = Mathf.Clamp01(slide / 3f);
                }

                if (loop != null && _director.Bank.TryGet(loop, out SoundEntry entry))
                {
                    loops.Drive(t.Item.GetInstanceID(), entry, level, t.Item.transform.position);
                    t.Until = now + TrackSeconds;
                    _tracked[i] = t;
                }
            }

            loops.Tick(Time.unscaledDeltaTime);
        }

        private static bool OnFloor(Tracked t)
        {
            float reach = (t.Collider != null ? t.Collider.bounds.extents.y : 0.3f) + 0.15f;
            return Physics.Raycast(t.Body.position, Vector3.down, reach, Physics.AllLayers, QueryTriggerInteraction.Ignore);
        }
    }
}
