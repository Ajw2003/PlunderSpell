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
        [Tooltip("How far in front of the caster a spell originates, in metres.")]
        [SerializeField] private float _castOriginForwardOffset = 1.0f;
        [Tooltip("Height above the caster's pivot a spell originates at, in metres.")]
        [SerializeField] private float _castOriginHeight = 1.5f;

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

            _voice = VoiceServiceLocator.Current;
            if (_voice != null)
            {
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
                PresentCast(resolved, result.Volume, this, default, affected);
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
            BroadcastCast(spellId, volume, caster, info.sender, affected);
        }

        /// <summary>
        /// Builds the effect context from the caster's transform and runs the registered effect.
        /// Public and network-free so the whole voice → misfire → consequence chain is testable
        /// without a transport.
        /// </summary>
        public int ExecuteEffect(SpellId spellId, CastVolume volume, NetworkIdentity caster)
        {
            Transform origin = caster != null ? caster.transform : transform;
            var ctx = new SpellEffectContext(
                spellId, volume,
                origin.position + origin.forward * _castOriginForwardOffset + Vector3.up * _castOriginHeight,
                origin.forward,
                caster,
                _targetLayers,
                _geometryLayers);

            return SpellEffectRegistry.Execute(ctx);
        }

        /// <summary>
        /// Runs on every client (and host): presentation only. The effect already happened on the
        /// server, so this must stay side-effect-free apart from logging and the local event.
        /// </summary>
        [ObserversRpc(bufferLast: false)]
        private void BroadcastCast(SpellId spellId, CastVolume volume, NetworkIdentity caster,
            PlayerID sender, int affected) => PresentCast(spellId, volume, caster, sender, affected);

        /// <summary>
        /// Presentation half, callable without an RPC. Must stay side-effect-free apart from logging
        /// and the local event: the effect has already happened on the server.
        /// </summary>
        private static void PresentCast(SpellId spellId, CastVolume volume, NetworkIdentity caster,
            PlayerID sender, int affected)
        {
            string who = caster != null ? caster.name : sender.ToString();
            if (IsMisfire(spellId))
                Debug.Log($"[Misfire] Player {who} misfired \u2192 {spellId} (Volume: {volume}, affected: {affected})");
            else
                Debug.Log($"[SpellCast] Player {who} cast {spellId} (Volume: {volume}, affected: {affected})");

            CastResolved?.Invoke(new CastReport(spellId, volume, affected, who));
        }

        /// <summary>What a resolved cast did. The HUD's cast feed reads these.</summary>
        public readonly struct CastReport
        {
            public readonly SpellId Spell;
            public readonly CastVolume Volume;
            public readonly int Affected;
            public readonly string CasterName;

            public CastReport(SpellId spell, CastVolume volume, int affected, string casterName)
            {
                Spell = spell;
                Volume = volume;
                Affected = affected;
                CasterName = casterName;
            }

            public bool IsMisfire => SpellCatalogue.IsMisfire(Spell);
        }

        /// <summary>Raised on every peer when a cast resolves. UI and audio subscribe.</summary>
        public static event System.Action<CastReport> CastResolved;

        /// <summary>True if the resolved id is one of the misfire outcomes.</summary>
        public static bool IsMisfire(SpellId id) => SpellCatalogue.IsMisfire(id);
    }
}
