using System.Collections.Generic;
using Interfaces;
using UnityEngine;

namespace RogueAi.UI
{
    // doc-ref 7f7c docs/systems/damage.md
    /// <summary>
    /// Draws every <see cref="Damage.Dealt"/> hit: numbers, flashes, enemy health bars, a hit marker,
    /// and a red screen edge plus a "what hurt you" line for the local player. Creates itself.
    /// </summary>
    public class DamageFeedbackView : MonoBehaviour
    {
        private const float k_numberSeconds = 1.1f;
        private const float k_barSeconds = 4f;
        private const float k_flashSeconds = 0.12f;
        private const float k_hurtLineSeconds = 3f;
        private const float k_hitMarkerSeconds = 0.25f;
        private const int k_maxHurtLines = 4;

        private static readonly Color k_youHurtColour = new Color(1f, 0.25f, 0.2f);
        private static readonly Color k_youDealtColour = new Color(1f, 0.9f, 0.35f);
        private static readonly Color k_friendlyFireColour = new Color(1f, 0.55f, 0.1f);
        private static readonly Color k_otherColour = new Color(0.85f, 0.85f, 0.85f);
        private static readonly Color k_enemyBarColour = new Color(0.85f, 0.15f, 0.12f);
        private static readonly Color k_friendBarColour = new Color(0.25f, 0.8f, 0.3f);
        private static readonly int k_baseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int k_colorId = Shader.PropertyToID("_Color");

        private struct FloatingNumber
        {
            public Vector3 World;
            public string Text;
            public Color Colour;
            public float Born;
            public bool Big;
        }

        private struct HurtLine
        {
            public string Who;
            public float Amount;
            public float Born;

            public string Text => $"-{Mathf.Max(1, Mathf.RoundToInt(Amount))}  {Who}";
        }

        /// <summary>A hit from the same thing within this many seconds adds to its line instead of
        /// starting a new one, so a fire reads as one growing number, not a column of ones.</summary>
        private const float k_mergeSeconds = 1.5f;

        private sealed class Flash
        {
            public Renderer[] Renderers;
            public MaterialPropertyBlock[] Saved;
            public float Until;
        }

        private readonly List<FloatingNumber> _numbers = new List<FloatingNumber>();
        private readonly List<HurtLine> _hurtLines = new List<HurtLine>();
        private readonly Dictionary<Component, float> _recentlyHurt = new Dictionary<Component, float>();
        private readonly Dictionary<Component, Flash> _flashes = new Dictionary<Component, Flash>();
        private readonly List<Component> _scratch = new List<Component>();
        private MaterialPropertyBlock _flashBlock;

        private float _vignette;
        private float _hitMarkerUntil = float.NegativeInfinity;
        private bool _hitMarkerKill;
        private Texture2D _vignetteTexture;
        private Texture2D _white;
        private GUIStyle _numberStyle;
        private GUIStyle _bigNumberStyle;
        private GUIStyle _lineStyle;

        /// <summary>The hits drawn right now, for tests: how many floating numbers are alive.</summary>
        public int LiveNumberCount => _numbers.Count;

        /// <summary>How red the screen edge is (0..1), for tests.</summary>
        public float Vignette => _vignette;

        /// <summary>The lines naming what hurt the local player, newest last, for tests.</summary>
        public IEnumerable<string> HurtLines
        {
            get { foreach (HurtLine line in _hurtLines) yield return line.Text; }
        }

        public static DamageFeedbackView Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null)
                return;
            var go = new GameObject("~DamageFeedback");
            DontDestroyOnLoad(go);
            go.AddComponent<DamageFeedbackView>();
        }

        private void Awake()
        {
            Instance = this;
            _flashBlock = new MaterialPropertyBlock();
        }

        private void OnEnable() => Damage.Dealt += OnDamage;

        private void OnDisable()
        {
            Damage.Dealt -= OnDamage;
            foreach (Flash flash in _flashes.Values)
                Restore(flash);
            _flashes.Clear();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>The local player's root: whoever owns the camera the game renders through.</summary>
        private static Transform LocalPlayerRoot()
        {
            Camera cam = Camera.main;
            return cam != null ? cam.transform.root : null;
        }

        private static bool IsPlayer(Component c) =>
            c != null && c.GetComponentInParent<IPlayerBody>() != null;

        private void OnDamage(DamageReport report)
        {
            if (report.Target == null)
                return;

            Transform local = LocalPlayerRoot();
            bool targetIsYou = local != null && report.Target.transform.root == local;
            bool youDidIt = local != null && report.Instigator != null && report.Instigator.transform.root == local;
            bool targetIsFriend = !targetIsYou && IsPlayer(report.Target);

            Color colour = targetIsYou ? k_youHurtColour
                : youDidIt && targetIsFriend ? k_friendlyFireColour
                : youDidIt ? k_youDealtColour
                : k_otherColour;

            string amount = Mathf.Max(1, Mathf.RoundToInt(report.Amount)).ToString();
            _numbers.Add(new FloatingNumber
            {
                World = report.Point,
                Text = report.Killed ? $"{amount}  KO" : amount,
                Colour = colour,
                Born = Time.time,
                Big = report.Killed || report.Amount >= 25f,
            });

            StartFlash(report.Target, targetIsYou);

            if (targetIsYou)
            {
                // Sized by the hit: a sword blow flashes hard, a fire tick barely tints the edge.
                float share = report.Amount / Mathf.Max(1f, report.MaxHealth);
                float bump = report.Kind == DamageKind.Burn ? 0.06f : 0.2f + share * 2.5f;
                _vignette = Mathf.Min(0.75f, _vignette + bump);
                AddHurtLine(Describe(report, local), report.Amount);
            }
            else
            {
                _recentlyHurt[report.Target] = Time.time;
            }

            if (youDidIt && !targetIsYou)
            {
                _hitMarkerUntil = Time.time + k_hitMarkerSeconds;
                _hitMarkerKill = report.Killed;
            }
        }

        /// <summary>"Watchman", "yourself (fire)", "a thrown Golden Goblet".</summary>
        private static string Describe(DamageReport report, Transform local)
        {
            string how = report.Kind == DamageKind.Burn ? " (fire)"
                : report.Kind == DamageKind.Impact ? " (impact)"
                : string.Empty;

            if (report.SelfInflicted || (report.Instigator != null && report.Instigator.transform.root == local))
                return "yourself" + how;
            if (report.Instigator != null)
                return CleanName(report.Instigator) + how;
            if (report.Source != null)
                return CleanName(report.Source) + how;
            return "something" + how;
        }

        private static string CleanName(GameObject go) => go.name.Replace("(Clone)", string.Empty).Trim();

        private void AddHurtLine(string who, float amount)
        {
            int last = _hurtLines.Count - 1;
            if (last >= 0 && _hurtLines[last].Who == who && Time.time - _hurtLines[last].Born <= k_mergeSeconds)
            {
                HurtLine merged = _hurtLines[last];
                merged.Amount += amount;
                merged.Born = Time.time;
                _hurtLines[last] = merged;
                return;
            }

            _hurtLines.Add(new HurtLine { Who = who, Amount = amount, Born = Time.time });
            if (_hurtLines.Count > k_maxHurtLines)
                _hurtLines.RemoveAt(0);
        }

        private void StartFlash(Component target, bool isYou)
        {
            // Your own body is mostly off-screen; the screen edge is your flash.
            if (isYou)
                return;

            if (_flashes.TryGetValue(target, out Flash existing))
            {
                existing.Until = Time.time + k_flashSeconds;
                return;
            }

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            var flash = new Flash
            {
                Renderers = renderers,
                Saved = new MaterialPropertyBlock[renderers.Length],
                Until = Time.time + k_flashSeconds,
            };

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null || renderers[i] is ParticleSystemRenderer)
                    continue;
                var saved = new MaterialPropertyBlock();
                renderers[i].GetPropertyBlock(saved);
                flash.Saved[i] = saved;

                renderers[i].GetPropertyBlock(_flashBlock);
                _flashBlock.SetColor(k_baseColorId, new Color(1f, 0.35f, 0.3f));
                _flashBlock.SetColor(k_colorId, new Color(1f, 0.35f, 0.3f));
                renderers[i].SetPropertyBlock(_flashBlock);
            }

            _flashes[target] = flash;
        }

        private static void Restore(Flash flash)
        {
            for (int i = 0; i < flash.Renderers.Length; i++)
            {
                if (flash.Renderers[i] != null && flash.Saved[i] != null)
                    flash.Renderers[i].SetPropertyBlock(flash.Saved[i]);
            }
        }

        private void Update()
        {
            float now = Time.time;
            _numbers.RemoveAll(n => now - n.Born > k_numberSeconds);
            _hurtLines.RemoveAll(l => now - l.Born > k_hurtLineSeconds);
            _vignette = Mathf.MoveTowards(_vignette, 0f, Time.deltaTime * 0.55f);

            _scratch.Clear();
            foreach (var pair in _flashes)
            {
                if (pair.Key == null || now > pair.Value.Until)
                    _scratch.Add(pair.Key);
            }
            foreach (Component key in _scratch)
            {
                Restore(_flashes[key]);
                _flashes.Remove(key);
            }

            _scratch.Clear();
            foreach (var pair in _recentlyHurt)
            {
                if (pair.Key == null || now - pair.Value > k_barSeconds)
                    _scratch.Add(pair.Key);
            }
            foreach (Component key in _scratch)
                _recentlyHurt.Remove(key);
        }

        private void OnGUI()
        {
            if (!IsInWorld())
                return;

            EnsureStyles();
            Camera cam = Camera.main;

            DrawVignette();
            if (cam != null)
            {
                DrawHealthBars(cam);
                DrawNumbers(cam);
            }
            DrawHitMarker();
            DrawHurtLines();
        }

        private static bool IsInWorld()
        {
            var gameState = Plunderspell.Core.GameServices.GameState;
            return gameState == null || gameState.CurrentState == Plunderspell.Core.GameState.Playing
                                     || gameState.CurrentState == Plunderspell.Core.GameState.Paused;
        }

        private void DrawVignette()
        {
            float lowHealth = 0f;
            Transform local = LocalPlayerRoot();
            IHealth you = local != null ? local.GetComponentInChildren<IHealth>() : null;
            if (you != null && (you as Component) != null && you.MaxHealth > 0f)
            {
                float fraction = you.CurrentHealth / you.MaxHealth;
                if (fraction < 0.3f && fraction > 0f)
                    lowHealth = (0.3f - fraction) / 0.3f * (0.35f + 0.15f * Mathf.Sin(Time.time * 6f));
            }

            float alpha = Mathf.Max(_vignette, lowHealth);
            if (alpha <= 0.01f)
                return;

            Color previous = GUI.color;
            GUI.color = new Color(1f, 0f, 0f, Mathf.Clamp01(alpha));
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), _vignetteTexture, ScaleMode.StretchToFill);
            GUI.color = previous;
        }

        private void DrawHealthBars(Camera cam)
        {
            foreach (var pair in _recentlyHurt)
            {
                Component target = pair.Key;
                if (target == null || !(target is IHealth health) || health.MaxHealth <= 0f)
                    continue;

                Vector3 top = TopOf(target) + Vector3.up * 0.35f;
                Vector3 screen = cam.WorldToScreenPoint(top);
                if (screen.z <= 0f)
                    continue;

                float age = Time.time - pair.Value;
                float alpha = age > k_barSeconds - 0.5f ? (k_barSeconds - age) / 0.5f : 1f;
                float width = Mathf.Clamp(900f / screen.z, 40f, 110f);
                var bar = new Rect(screen.x - width * 0.5f, Screen.height - screen.y - 6f, width, 7f);

                Color previous = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, 0.7f * alpha);
                GUI.DrawTexture(new Rect(bar.x - 1f, bar.y - 1f, bar.width + 2f, bar.height + 2f), _white);
                Color fill = IsPlayer(target) ? k_friendBarColour : k_enemyBarColour;
                GUI.color = new Color(fill.r, fill.g, fill.b, alpha);
                float fraction = Mathf.Clamp01(health.CurrentHealth / health.MaxHealth);
                GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * fraction, bar.height), _white);
                GUI.color = previous;
            }
        }

        private void DrawNumbers(Camera cam)
        {
            foreach (FloatingNumber number in _numbers)
            {
                float age = (Time.time - number.Born) / k_numberSeconds;
                Vector3 world = number.World + Vector3.up * (0.2f + age * 0.9f);
                Vector3 screen = cam.WorldToScreenPoint(world);
                if (screen.z <= 0f)
                    continue;

                GUIStyle style = number.Big ? _bigNumberStyle : _numberStyle;
                float alpha = age < 0.7f ? 1f : 1f - (age - 0.7f) / 0.3f;
                var rect = new Rect(screen.x - 60f, Screen.height - screen.y - 16f, 120f, 32f);
                Shadowed(rect, number.Text, style, new Color(number.Colour.r, number.Colour.g, number.Colour.b, alpha));
            }
        }

        private void DrawHitMarker()
        {
            if (Time.time > _hitMarkerUntil)
                return;

            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;
            float size = _hitMarkerKill ? 13f : 9f;
            Color previous = GUI.color;
            GUI.color = _hitMarkerKill ? k_youHurtColour : Color.white;
            Matrix4x4 matrix = GUI.matrix;
            foreach (float angle in new[] { 45f, 135f, 225f, 315f })
            {
                GUIUtility.RotateAroundPivot(angle, new Vector2(cx, cy));
                GUI.DrawTexture(new Rect(cx + 5f, cy - 1f, size, 2.5f), _white);
                GUI.matrix = matrix;
            }
            GUI.color = previous;
        }

        private void DrawHurtLines()
        {
            float y = Screen.height * 0.62f;
            for (int i = _hurtLines.Count - 1; i >= 0; i--)
            {
                HurtLine line = _hurtLines[i];
                float age = (Time.time - line.Born) / k_hurtLineSeconds;
                float alpha = age < 0.75f ? 1f : 1f - (age - 0.75f) / 0.25f;
                var rect = new Rect(Screen.width * 0.5f - 200f, y, 400f, 26f);
                Shadowed(rect, line.Text, _lineStyle, new Color(k_youHurtColour.r, k_youHurtColour.g, k_youHurtColour.b, alpha));
                y += 24f;
            }
        }

        private static Vector3 TopOf(Component target)
        {
            var renderer = target.GetComponentInChildren<Renderer>();
            if (renderer != null)
                return new Vector3(target.transform.position.x, renderer.bounds.max.y, target.transform.position.z);
            return target.transform.position + Vector3.up * 2f;
        }

        private static void Shadowed(Rect rect, string text, GUIStyle style, Color colour)
        {
            Color previous = style.normal.textColor;
            style.normal.textColor = new Color(0f, 0f, 0f, colour.a * 0.8f);
            GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), text, style);
            style.normal.textColor = colour;
            GUI.Label(rect, text, style);
            style.normal.textColor = previous;
        }

        private void EnsureStyles()
        {
            if (_numberStyle != null)
                return;

            _numberStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                fontStyle = FontStyle.Bold,
            };
            _bigNumberStyle = new GUIStyle(_numberStyle) { fontSize = 28 };
            _lineStyle = new GUIStyle(_numberStyle) { fontSize = 18 };

            _white = Texture2D.whiteTexture;
            _vignetteTexture = BuildVignette(128);
        }

        /// <summary>A white texture that is clear in the middle and opaque at the edges.</summary>
        private static Texture2D BuildVignette(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f;
                    float dy = (y + 0.5f) / size * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / 1.414f;
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1f, d));
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
