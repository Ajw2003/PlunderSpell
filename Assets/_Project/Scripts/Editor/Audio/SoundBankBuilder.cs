using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Plunderspell.Audio;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// Regenerates <c>Assets/_Project/Audio/SoundBank.asset</c> from <c>Tools/AudioForge/manifest.csv</c> and
    /// the clips on disk, and registers it as a preloaded asset so the game finds it with no Resources
    /// folder. It never edits the manifest or a clip. See docs/4-systems/audio.md.
    /// </summary>
    public static class SoundBankBuilder
    {
        public const string BankPath = "Assets/_Project/Audio/SoundBank.asset";
        public const string ManifestPath = "Tools/AudioForge/manifest.csv";
        public const string AudioRoot = "Assets/_Project/Audio";

        public readonly struct ManifestRow
        {
            public readonly string Name;
            public readonly int Variants;
            public readonly string Folder;
            public readonly string Bus;
            public readonly bool ThreeD;
            public readonly bool Loop;
            public readonly SoundNoise Noise;

            public ManifestRow(string name, int variants, string folder, string bus, bool threeD, bool loop, SoundNoise noise)
            {
                Name = name;
                Variants = variants;
                Folder = folder;
                Bus = bus;
                ThreeD = threeD;
                Loop = loop;
                Noise = noise;
            }
        }

        [MenuItem("Plunderspell/Audio/Rebuild SoundBank")]
        public static void RebuildMenu() => Rebuild();

        public static SoundBank Rebuild()
        {
            AudioMixer mixer = AudioMixerBuilder.LoadOrBuild();
            List<ManifestRow> rows = ReadManifest(File.ReadAllText(ManifestPath));

            var entries = new List<SoundEntry>(rows.Count);
            int missingClips = 0;
            foreach (ManifestRow row in rows)
            {
                var clips = new List<AudioClip>(row.Variants);
                for (int v = 1; v <= row.Variants; v++)
                {
                    string path = AudioRoot + "/" + row.Folder + "/" + row.Name + "_" + v.ToString("00") + ".ogg";
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    if (clip != null)
                        clips.Add(clip);
                    else
                        missingClips++;
                }

                entries.Add(new SoundEntry
                {
                    Name = row.Name,
                    Clips = clips.ToArray(),
                    Group = FindGroup(mixer, row.Bus),
                    ThreeD = row.ThreeD,
                    Loop = row.Loop,
                    Noise = row.Noise
                });
            }

            var bank = AssetDatabase.LoadAssetAtPath<SoundBank>(BankPath);
            if (bank == null)
            {
                bank = ScriptableObject.CreateInstance<SoundBank>();
                AssetDatabase.CreateAsset(bank, BankPath);
            }

            bank.Assign(mixer, entries);
            EditorUtility.SetDirty(bank);
            AssetDatabase.SaveAssets();
            RegisterPreloaded(bank);
            Debug.Log("[Audio] SoundBank rebuilt: " + entries.Count + " entries, " + missingClips + " clip files missing.");
            return bank;
        }

        private static AudioMixerGroup FindGroup(AudioMixer mixer, string bus)
        {
            AudioMixerGroup[] groups = mixer.FindMatchingGroups("Master/" + bus);
            string leaf = bus.Substring(bus.LastIndexOf('/') + 1);
            foreach (AudioMixerGroup group in groups)
            {
                if (group.name == leaf)
                    return group;
            }
            throw new InvalidOperationException("The mixer has no group for bus '" + bus + "'.");
        }

        private static void RegisterPreloaded(SoundBank bank)
        {
            var preloaded = new List<UnityEngine.Object>(PlayerSettings.GetPreloadedAssets());
            if (preloaded.Contains(bank))
                return;
            preloaded.Add(bank);
            PlayerSettings.SetPreloadedAssets(preloaded.ToArray());
            AssetDatabase.SaveAssets();
        }

        /// <summary>Parses the manifest: a header row, then one sound per row; quoted fields may hold commas.</summary>
        public static List<ManifestRow> ReadManifest(string csv)
        {
            var rows = new List<ManifestRow>();
            string[] lines = csv.Split('\n');
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].TrimEnd('\r');
                if (line.Length == 0)
                    continue;
                List<string> f = SplitCsv(line);
                rows.Add(new ManifestRow(f[0], int.Parse(f[1]), f[2], f[3], f[4] == "3d", f[5] == "1", ParseNoise(f[7])));
            }
            return rows;
        }

        private static SoundNoise ParseNoise(string value)
        {
            switch (value)
            {
                case "low": return SoundNoise.Low;
                case "mid": return SoundNoise.Mid;
                case "high": return SoundNoise.High;
                case "max": return SoundNoise.Max;
                default: return SoundNoise.None;
            }
        }

        private static List<string> SplitCsv(string line)
        {
            var fields = new List<string>();
            var current = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (quoted && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        quoted = !quoted;
                    }
                }
                else if (c == ',' && !quoted)
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            fields.Add(current.ToString());
            return fields;
        }
    }
}
