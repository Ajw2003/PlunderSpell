using Plunderspell.Core;
using PurrNet;
using Plunderspell.Voice;
using UnityEngine;

namespace Plunderspell.Spells
{
    /// <summary>
    /// Bridges the voice pipeline to networked spell casting.
    ///
    /// On the owning client it listens for recognised phrases, resolves them through the
    /// <see cref="MisfireEngine"/> against the <see cref="SpellLexicon"/>, then asks the server to
    /// resolve the cast. The server — and only the server — runs the effect through
    /// <see cref="SpellEffectRegistry"/>, so consequence is authoritative; the
    /// <c>[ObserversRpc]</c> that follows carries presentation to every peer.
    ///
    /// Every word costs mana (<see cref="SpellWord.ManaCost"/>), spent on the caster's machine from
    /// <see cref="GameServices.PlayerStats"/> and regained over time. A number-key cast is chanted
    /// for <see cref="SpellTuning.KeyboardCastSeconds"/> before it fires, so keys never out-pace
    /// speech (#116). See docs/4-systems/spells.md, "Mana and the keyboard chant".
    /// </summary>
    public class SpellCastingSystem : NetworkBehaviour
    {
        [Tooltip("The spellbook used to resolve spoken phrases into spells/misfires.")]
        [SerializeField] private SpellLexicon _lexicon;

        [Header("Cast origin")]
        [Tooltip("The caster's look camera; a spell fires from here, along its forward. Self-wires " +
                 "to the first child Camera if left empty. See docs/4-systems/spells.md, \"A cast " +
                 "follows the camera, not the body\".")]
        [SerializeField] private Transform _aimSource;
        [Tooltip("How far in front of the aim source a spell originates, in metres.")]
        [SerializeField] private float _castOriginForwardOffset = 1.0f;

        [Header("Layers")]
        [Tooltip("Layers a spell effect may affect.")]
        [SerializeField] private LayerMask _targetLayers = ~0;
        [Tooltip("Layers treated as sound-blocking walls when a cast makes noise.")]
        [SerializeField] private LayerMask _geometryLayers;

        private IVoiceInputService _voice;
        private bool _subscribed;

        // The keyboard chant in progress, if any. Owner-side only, like the rest of the input half.
        private SpellId _chantSpell = SpellId.None;
        private CastVolume _chantVolume;
        private float _chantStartedAt;
        private float _chantEndsAt;

        // Fractional mana regained but not yet whole: PlayerStats counts in whole points.
        private float _manaRegenCarry;

        /// <summary>This machine's own caster: the one listening to this machine's microphone and
        /// keys. Null until the local player exists. The HUD reads the chant and costs from it.</summary>
        public static SpellCastingSystem Local { get; private set; }

        /// <summary>True while a number-key cast is being chanted and has not fired yet.</summary>
        public bool IsChanting => _chantSpell != SpellId.None;

        /// <summary>The spell being chanted, or <see cref="SpellId.None"/>.</summary>
        public SpellId ChantingSpell => _chantSpell;

        /// <summary>The word being chanted, as keyed ("IGNIS"), or empty.</summary>
        public string ChantingWord { get; private set; } = string.Empty;

        /// <summary>How far through the chant, 0..1; 0 when not chanting.</summary>
        public float ChantProgress =>
            IsChanting ? Mathf.InverseLerp(_chantStartedAt, _chantEndsAt, Time.time) : 0f;

        /// <summary>The spellbook in use, for displays that list words and costs.</summary>
        public SpellLexicon Lexicon => _lexicon;

        /// <summary>Mana a resolved cast costs from this caster's spellbook.</summary>
        public int ManaCostOf(SpellId resolved) => _lexicon != null ? _lexicon.ManaCostOf(resolved) : 0;

        protected override void OnSpawned()
        {
            base.OnSpawned();

            // Only the local owner captures voice; remote copies just receive broadcasts.
            if (!isOwner)
                return;

            Subscribe();
        }

        /// <summary>
        /// Offline there is no spawn event, so without this a single-player scene would never listen
        /// for voice and casting — the entire point of the game — would silently do nothing.
        /// </summary>
        private void Start()
        {
            if (!isSpawned)
                Subscribe();
        }

        private void Subscribe()
        {
            if (_subscribed)
                return;

            if (_lexicon == null)
                Debug.LogWarning("[SpellCast] No SpellLexicon assigned — every phrase will fizzle (None).");

            // Self-wires like SpellBook does, so dropping this on a player rig is enough on its own.
            if (_aimSource == null)
                _aimSource = GetComponentInChildren<Camera>()?.transform;

            _voice = VoiceServiceLocator.Current;
            if (_voice != null)
            {
                // Real speech can only hear English, so it needs told which spellings mean which word.
                if (_voice is IPhraseVocabularyTarget speech && _lexicon != null)
                    speech.SetVocabulary(_lexicon.BuildHeardVocabulary());

                _voice.OnPhraseRecognized += HandlePhrase;
                _subscribed = true;
                Local = this;

                // A new body arrives in the raid with a full pool.
                GameServices.PlayerStats?.RefillMana();
                _manaRegenCarry = 0f;
            }
            else
            {
                Debug.LogWarning("[SpellCast] No voice service available.");
            }
        }

        /// <summary>Assigns the spellbook at runtime, for tooling-built scenes and tests.</summary>
        public void SetLexicon(SpellLexicon lexicon) => _lexicon = lexicon;

        /// <summary>Assigns the aim camera at runtime, for tooling-built scenes and tests.</summary>
        public void SetAimSource(Transform aimSource) => _aimSource = aimSource;

        protected override void OnDespawned()
        {
            base.OnDespawned();
            Unsubscribe();
        }

        private void OnDisable() => Unsubscribe();

        private void Unsubscribe()
        {
            if (_subscribed && _voice != null)
                _voice.OnPhraseRecognized -= HandlePhrase;
            _subscribed = false;
            _chantSpell = SpellId.None;
            if (Local == this)
                Local = null;
        }

        /// <summary>Owner-side: mana comes back over time, and a finished chant fires.</summary>
        private void Update()
        {
            if (!_subscribed)
                return;

            RegenerateMana(Time.deltaTime);

            if (IsChanting && Time.time >= _chantEndsAt)
            {
                SpellId spell = _chantSpell;
                _chantSpell = SpellId.None;
                Cast(spell, _chantVolume);
            }
        }

        private void RegenerateMana(float deltaTime)
        {
            PlayerStats stats = GameServices.PlayerStats;
            if (stats == null || stats.Mana >= stats.MaxMana)
            {
                _manaRegenCarry = 0f;
                return;
            }

            _manaRegenCarry += SpellTuning.ManaRegenPerSecond * deltaTime;
            int whole = (int)_manaRegenCarry;
            if (whole <= 0)
                return;

            _manaRegenCarry -= whole;
            stats.RestoreMana(whole);
        }

        /// <summary>True when the local pool covers <paramref name="cost"/>. With no stats service
        /// (a bare test scene) mana is not enforced.</summary>
        private static bool CanAfford(int cost)
        {
            PlayerStats stats = GameServices.PlayerStats;
            return stats == null || stats.Mana >= cost;
        }

        /// <summary>Owner-side handler: resolve the phrase and request a networked cast.</summary>
        private void HandlePhrase(VoiceRecognitionResult result)
        {
            SpellId resolved = MisfireEngine.Resolve(result, _lexicon);
            if (resolved != SpellId.None && result.FromKeyboard && IsChanting)
            {
                Debug.Log($"[SpellCast] Already chanting {_chantSpell}; \"{result.NormalizedText}\" ignored.");
                return;
            }

            bool notEnoughMana = resolved != SpellId.None && !CanAfford(ManaCostOf(resolved));
            bool chant = resolved != SpellId.None && !notEnoughMana && result.FromKeyboard &&
                         SpellTuning.KeyboardCastSeconds > 0f;

            PhraseResolved?.Invoke(new PhraseReport(result.RawText, result.NormalizedText, resolved,
                result.Volume, notEnoughMana, chant));
            if (resolved == SpellId.None)
            {
                Debug.Log($"[SpellCast] Phrase \"{result.NormalizedText}\" fizzled (no match).");
                return;
            }

            if (notEnoughMana)
            {
                Debug.Log($"[SpellCast] Not enough mana for {resolved} ({ManaCostOf(resolved)} needed).");
                return;
            }

            if (chant)
            {
                _chantSpell = resolved;
                _chantVolume = result.Volume;
                ChantingWord = result.NormalizedText;
                _chantStartedAt = Time.time;
                _chantEndsAt = Time.time + SpellTuning.KeyboardCastSeconds;
                return;
            }

            Cast(resolved, result.Volume);
        }

        /// <summary>Owner-side: pay for the cast, then resolve it (offline) or ask the server to.</summary>
        private void Cast(SpellId resolved, CastVolume volume)
        {
            GameServices.PlayerStats?.SpendMana(ManaCostOf(resolved));

            bool isMisfire = IsMisfire(resolved);
            if (isMisfire)
                Debug.Log($"[Misfire] Local cast misfired \u2192 {resolved} (Volume: {volume})");
            else
                Debug.Log($"[SpellCast] Local cast {resolved} (Volume: {volume})");

            // Offline there is no server to ask — and the [ServerRpc]/[ObserversRpc] wrappers would
            // send nothing and run nothing on an unspawned object — so resolve and present here.
            if (!isSpawned)
            {
                int affected = ExecuteEffect(resolved, volume, this);
                PresentCast(resolved, volume, this, default, affected,
                    CastOrigin(this), CastDirection(this));
                return;
            }

            // Aim is read here, on the caster's machine: the server never sees a remote player's
            // camera pitch, so its own reading of their aim would point along the horizon.
            ServerCast(resolved, (byte)volume, this, CastOrigin(this), CastDirection(this));
        }

        /// <summary>
        /// Server entry point: runs the effect authoritatively, then fans the outcome out to every
        /// observer for presentation. Running the effect here (not in the observers RPC) is what
        /// stops four clients each applying the same damage.
        /// </summary>
        // The volume crosses the network as a byte: CastVolume lives in the Voice assembly, which
        // PurrNet's code generation never registers, so sending the enum itself failed to pack and
        // every networked cast was lost (caught by RaidSceneCastingTests once solo became a host).
        [ServerRpc(requireOwnership: true)]
        private void ServerCast(SpellId spellId, byte volumeByte, NetworkIdentity caster, Vector3 origin,
            Vector3 direction, RPCInfo info = default)
        {
            var volume = (CastVolume)volumeByte;
            int affected = ExecuteEffect(spellId, volume, caster, origin, direction);

            // info.sender is the player that requested the cast.
            BroadcastCast(spellId, volumeByte, caster, info.sender, affected, origin, direction);
        }

        /// <summary>
        /// Builds the effect context from the caster's transform and runs the registered effect.
        /// Public and network-free so the whole voice → misfire → consequence chain is testable
        /// without a transport.
        /// </summary>
        public int ExecuteEffect(SpellId spellId, CastVolume volume, NetworkIdentity caster) =>
            ExecuteEffect(spellId, volume, caster, CastOrigin(caster), CastDirection(caster));

        /// <summary>Runs the effect from an aim measured on the caster's own machine.</summary>
        public int ExecuteEffect(SpellId spellId, CastVolume volume, NetworkIdentity caster, Vector3 origin,
            Vector3 direction)
        {
            var ctx = new SpellEffectContext(
                spellId, volume,
                origin, direction,
                caster,
                _targetLayers,
                _geometryLayers);

            return SpellEffectRegistry.Execute(ctx);
        }

        /// <summary>Where a cast leaves the caster's hands. Shared by the effect and its visual, so
        /// the two cannot disagree about where the spell came from.</summary>
        private Vector3 CastOrigin(NetworkIdentity caster)
        {
            Transform aim = AimTransform(caster);
            return aim.position + aim.forward * _castOriginForwardOffset;
        }

        private Vector3 CastDirection(NetworkIdentity caster) => AimTransform(caster).forward;

        /// <summary>The transform a cast aims along: the caster's own camera when it has one (this
        /// covers a remote caster too, since every player's SpellCastingSystem self-wires its own),
        /// falling back to the caster's body transform for a caster with no camera at all (tests).</summary>
        private Transform AimTransform(NetworkIdentity caster)
        {
            if (caster is SpellCastingSystem casterSystem && casterSystem._aimSource != null)
                return casterSystem._aimSource;

            return caster != null ? caster.transform : transform;
        }

        /// <summary>
        /// Runs on every client (and host): presentation only. The effect already happened on the
        /// server, so this must stay side-effect-free apart from logging and the local event.
        /// </summary>
        [ObserversRpc(bufferLast: false)]
        private void BroadcastCast(SpellId spellId, byte volumeByte, NetworkIdentity caster,
            PlayerID sender, int affected, Vector3 origin, Vector3 direction) =>
            PresentCast(spellId, (CastVolume)volumeByte, caster, sender, affected, origin, direction);

        /// <summary>
        /// Presentation half, callable without an RPC. Must stay side-effect-free apart from logging
        /// and the local event: the effect has already happened on the server.
        /// </summary>
        private static void PresentCast(SpellId spellId, CastVolume volume, NetworkIdentity caster,
            PlayerID sender, int affected, Vector3 origin, Vector3 direction)
        {
            string who = caster != null ? caster.name : sender.ToString();
            if (IsMisfire(spellId))
                Debug.Log($"[Misfire] Player {who} misfired \u2192 {spellId} (Volume: {volume}, affected: {affected})");
            else
                Debug.Log($"[SpellCast] Player {who} cast {spellId} (Volume: {volume}, affected: {affected})");

            CastResolved?.Invoke(new CastReport(spellId, volume, affected, who, origin, direction));
        }

        /// <summary>What a resolved cast did. The HUD's cast feed reads these.</summary>
        public readonly struct CastReport
        {
            public readonly SpellId Spell;
            public readonly CastVolume Volume;
            public readonly int Affected;
            public readonly string CasterName;

            /// <summary>Where the cast left the caster's hands, so a visual can be put there.</summary>
            public readonly Vector3 Origin;

            /// <summary>Which way the caster was facing, for directional visuals.</summary>
            public readonly Vector3 Direction;

            public CastReport(SpellId spell, CastVolume volume, int affected, string casterName,
                Vector3 origin = default, Vector3 direction = default)
            {
                Spell = spell;
                Volume = volume;
                Affected = affected;
                CasterName = casterName;
                Origin = origin;
                Direction = direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector3.forward;
            }

            public bool IsMisfire => SpellCatalogue.IsMisfire(Spell);
        }

        /// <summary>What the local player said and what it became — including a fizzle, which casts
        /// nothing and so never reaches <see cref="CastResolved"/>. The phrase caption reads this.</summary>
        public readonly struct PhraseReport
        {
            /// <summary>Exactly what the recogniser output ("igneous"), or the key's word.</summary>
            public readonly string Heard;

            /// <summary>The lexicon word it was taken as ("IGNIS").</summary>
            public readonly string Word;

            public readonly SpellId Result;
            public readonly CastVolume Volume;

            /// <summary>A real word, refused because the caster's mana did not cover it.</summary>
            public readonly bool NotEnoughMana;

            /// <summary>A keyed word that will fire once its chant finishes.</summary>
            public readonly bool Chanting;

            public PhraseReport(string heard, string word, SpellId result, CastVolume volume,
                bool notEnoughMana = false, bool chanting = false)
            {
                Heard = heard ?? string.Empty;
                Word = word ?? string.Empty;
                Result = result;
                Volume = volume;
                NotEnoughMana = notEnoughMana;
                Chanting = chanting;
            }

            public bool Fizzled => Result == SpellId.None;
            public bool IsMisfire => SpellCatalogue.IsMisfire(Result);
        }

        /// <summary>Raised on the caster's machine for every phrase, cast or not.</summary>
        public static event System.Action<PhraseReport> PhraseResolved;

        /// <summary>Raised on every peer when a cast resolves. UI and audio subscribe.</summary>
        public static event System.Action<CastReport> CastResolved;

        /// <summary>
        /// Announces a cast to the presentation layer without routing a real one through the voice
        /// pipeline and a transport. An event cannot be raised from outside its declaring class, so
        /// without this the HUD's cast feed and the spell visuals are both untestable.
        /// </summary>
        public static void AnnounceForTesting(CastReport report) => CastResolved?.Invoke(report);

        /// <summary>True if the resolved id is one of the misfire outcomes.</summary>
        public static bool IsMisfire(SpellId id) => SpellCatalogue.IsMisfire(id);
    }
}
