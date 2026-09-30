using System.Collections;
using System.Collections.Generic;
using System.Text;
using Plunderspell.Guards;
using Plunderspell.Voice;
using UnityEngine;

namespace Plunderspell.Audio.Mimic
{
    /// <summary>
    /// Guard mimic prototype. When this machine's player talks between casts ("Guards hear my voice"
    /// on), every word goes into a word bank cut from their recording; the nearest guard then answers
    /// with a sentence a small local language model builds only from banked words, spoken in the
    /// player's own clips at a random pitch per word. Only the speaker hears it (nothing is sent).
    /// Added by <see cref="AudioDirector"/>. Plan: docs/plans/guards-speak-overheard-words.md.
    /// </summary>
    public sealed class GuardMimic : MonoBehaviour
    {
        public const string SoundName = "mimic_guard";
        private const float ReplyRange = 25f;
        private const float MinSecondsBetweenReplies = 2f;
        private const int MinBankWords = 2;

        private readonly MimicWordBank _bank = new MimicWordBank();
        private readonly System.Random _random = new System.Random();
        private readonly Queue<ChatterReport> _heard = new Queue<ChatterReport>();
        private LocalLanguageModel _model;
        private IChatterSource _source;
        private AudioSource _voice;
        private bool _busy;
        private bool _warmed;
        private float _lastReplyAt = -100f;

        /// <summary>The word bank, for tests and the co-op check.</summary>
        public MimicWordBank Bank => _bank;

        /// <summary>The last line a guard said back, for the co-op check.</summary>
        public string LastReply { get; private set; }

        private void Awake()
        {
            _model = gameObject.AddComponent<LocalLanguageModel>();
            var go = new GameObject("GuardMimicVoice");
            go.transform.SetParent(transform, false);
            _voice = go.AddComponent<AudioSource>();
            _voice.playOnAwake = false;
            _voice.spatialBlend = 1f;
            _voice.minDistance = 2f;
            _voice.maxDistance = 30f;
            _voice.rolloffMode = AudioRolloffMode.Logarithmic;
        }

        private void Update()
        {
            // The voice service is registered after the audio layer starts; hook on once it exists.
            if (_source == null && VoiceServiceLocator.Current is IChatterSource source)
            {
                _source = source;
                _source.ChatterHeard += OnChatterHeard;
            }

            // Start the model once a raid has guards and this player lets them hear, not on the first line.
            if (!_warmed && _source != null && CastleGuard.Active.Count > 0 && Plunderspell.Core.AudioInputSettings.GuardsHearChatter)
            {
                _warmed = true;
                _model.Warm();
            }

            if (!_busy && _heard.Count > 0 && Time.unscaledTime - _lastReplyAt >= MinSecondsBetweenReplies)
                StartCoroutine(Reply(_heard.Dequeue()));
        }

        private void OnDestroy()
        {
            if (_source != null)
                _source.ChatterHeard -= OnChatterHeard;
        }

        private void OnChatterHeard(ChatterReport report)
        {
            int added = _bank.Add(report);
            Debug.Log($"[Mimic] Banked {added} of {report.Words.Length} word(s) from \"{report.Transcript}\"; bank holds {_bank.WordCount}.");
            if (_heard.Count < 2)
                _heard.Enqueue(report);
        }

        /// <summary>Feeds a report as if it had been heard (tests and the co-op check).</summary>
        public void Hear(ChatterReport report) => OnChatterHeard(report);

        private IEnumerator Reply(ChatterReport report)
        {
            _busy = true;
            _lastReplyAt = Time.unscaledTime;
            try
            {
                CastleGuard guard = NearestGuard();
                if (guard == null)
                {
                    Debug.Log($"[Mimic] No awake guard within {ReplyRange} m to answer.");
                    yield break;
                }
                if (_bank.WordCount < MinBankWords)
                {
                    Debug.Log($"[Mimic] Only {_bank.WordCount} word(s) banked; saying nothing yet.");
                    yield break;
                }

                string reply = null;
                float asked = Time.realtimeSinceStartup;
                yield return _model.Complete(Prompt(report.Transcript), Grammar(), text => reply = text);
                if (string.IsNullOrWhiteSpace(reply))
                    yield break;

                string[] words = reply.Trim().Split(' ');
                float[] samples = MimicStitcher.Stitch(words, _bank, ChatterReport.SampleRate, _random);
                if (samples.Length == 0)
                    yield break;
                if (!SoundFocus.Allows(SoundName))
                {
                    Debug.Log($"[Mimic] Muted by SoundFocusSettings: \"{reply}\".");
                    yield break;
                }

                LastReply = reply;
                Debug.Log($"[Mimic] {guard.name} says \"{reply}\" ({(Time.realtimeSinceStartup - asked) * 1000f:0} ms to think, " +
                          $"{samples.Length / (float)ChatterReport.SampleRate:0.00} s of speech).");
                var clip = AudioClip.Create(SoundName, samples.Length, 1, ChatterReport.SampleRate, false);
                clip.SetData(samples, 0);
                _voice.transform.position = guard.transform.position + Vector3.up * 1.7f;
                _voice.clip = clip;
                _voice.Play();
                yield return new WaitForSecondsRealtime(clip.length + 0.1f);
                _voice.clip = null;
                Destroy(clip);
            }
            finally
            {
                _busy = false;
            }
        }

        private CastleGuard NearestGuard()
        {
            Vector3 listener = AudioDirector.Instance != null ? AudioDirector.Instance.Listener : transform.position;
            CastleGuard best = null;
            float bestDistance = ReplyRange * ReplyRange;
            foreach (CastleGuard guard in CastleGuard.Active)
            {
                if (guard == null || guard.IsDead || guard.IsIncapacitated)
                    continue;
                float distance = (guard.transform.position - listener).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = guard;
                }
            }
            return best;
        }

        private static string Prompt(string heard) =>
            "<|im_start|>system\nYou are a castle guard in a dark medieval castle. You caught a thief and you answer them. " +
            "You can only use the words you are allowed. Reply with one short sentence.<|im_end|>\n" +
            $"<|im_start|>user\nThe thief said: \"{heard.Replace("\"", string.Empty)}\"<|im_end|>\n<|im_start|>assistant\n";

        /// <summary>A GBNF grammar that lets the model say 2 to 8 words, each one a banked word.</summary>
        private string Grammar()
        {
            var sb = new StringBuilder("root ::= w (\" \" w){1,7}\nw ::= ");
            bool first = true;
            foreach (string word in _bank.Words)
            {
                if (!first)
                    sb.Append(" | ");
                sb.Append('"').Append(word).Append('"');
                first = false;
            }
            return sb.ToString();
        }
    }
}
