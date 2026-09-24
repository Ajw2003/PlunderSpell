using PurrNet;
using RogueAi.Voice;
using UnityEngine;

namespace RogueAi.Spells
{
    /// <summary>
    /// Bridges the voice pipeline to networked spell casting.
    ///
    /// On the owning client it listens for recognised phrases, resolves them through the
    /// <see cref="MisfireEngine"/> against the <see cref="SpellLexicon"/>, then asks the server to
    /// resolve the cast. The server — and only the server — runs the effect through
    /// <see cref="SpellEffectRegistry"/>, so consequence is authoritative; the
    /// <c>[ObserversRpc]</c> that follows carries presentation to every peer.
    /// </summary>
    public class SpellCastingSystem : NetworkBehaviour
    {
        [Tooltip("The spellbook used to resolve spoken phrases into spells/misfires.")]
        [SerializeField] private SpellLexicon _lexicon;

        [Header("Cast origin")]
        [Tooltip("The caster's look camera; a spell fires from here, along its forward. Self-wires " +
                 "to the first child Camera if left empty. See docs/systems/spells.md, \"A cast " +
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
        }

        /// <summary>Owner-side handler: resolve the phrase and request a networked cast.</summary>
        private void HandlePhrase(VoiceRecognitionResult result)
        {
            SpellId resolved = MisfireEngine.Resolve(result, _lexicon);
            PhraseResolved?.Invoke(new PhraseReport(result.RawText, result.NormalizedText, resolved, result.Volume));
            if (resolved == SpellId.None)
            {
                Debug.Log($"[SpellCast] Phrase \"{result.NormalizedText}\" fizzled (no match).");
                return;
            }

            bool isMisfire = IsMisfire(resolved);
            if (isMisfire)
                Debug.Log($"[Misfire] Local cast misfired \"{result.NormalizedText}\" \u2192 {resolved} (Volume: {result.Volume})");
            else
                Debug.Log($"[SpellCast] Local cast {resolved} (Volume: {result.Volume})");

            // Offline there is no server to ask — and the [ServerRpc]/[ObserversRpc] wrappers would
            // send nothing and run nothing on an unspawned object — so resolve and present here.
            if (!isSpawned)
            {
                int affected = ExecuteEffect(resolved, result.Volume, this);
                PresentCast(resolved, result.Volume, this, default, affected,
                    CastOrigin(this), CastDirection(this));
                return;
            }

            ServerCast(resolved, result.Volume, this);
        }

        /// <summary>
        /// Server entry point: runs the effect authoritatively, then fans the outcome out to every
        /// observer for presentation. Running the effect here (not in the observers RPC) is what
        /// stops four clients each applying the same damage.
        /// </summary>
        [ServerRpc(requireOwnership: true)]
        private void ServerCast(SpellId spellId, CastVolume volume, NetworkIdentity caster, RPCInfo info = default)
        {
            int affected = ExecuteEffect(spellId, volume, caster);

            // info.sender is the player that requested the cast.
            BroadcastCast(spellId, volume, caster, info.sender, affected,
                CastOrigin(caster), CastDirection(caster));
        }

        /// <summary>
        /// Builds the effect context from the caster's transform and runs the registered effect.
        /// Public and network-free so the whole voice → misfire → consequence chain is testable
        /// without a transport.
        /// </summary>
        public int ExecuteEffect(SpellId spellId, CastVolume volume, NetworkIdentity caster)
        {
            var ctx = new SpellEffectContext(
                spellId, volume,
                CastOrigin(caster), CastDirection(caster),
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
        private void BroadcastCast(SpellId spellId, CastVolume volume, NetworkIdentity caster,
            PlayerID sender, int affected, Vector3 origin, Vector3 direction) =>
            PresentCast(spellId, volume, caster, sender, affected, origin, direction);

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

            public PhraseReport(string heard, string word, SpellId result, CastVolume volume)
            {
                Heard = heard ?? string.Empty;
                Word = word ?? string.Empty;
                Result = result;
                Volume = volume;
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
