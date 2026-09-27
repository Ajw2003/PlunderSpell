using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Plunderspell.EditorTools
{
    // doc-ref d1ef docs/4-systems/raid-scene-assembly.md
    /// <summary>
    /// Owns the import settings of everything under <c>Assets/Models/ArtBible/</c>: rig per model,
    /// URP Lit materials from the baked maps, texture settings. Checked by
    /// <see cref="ArtAssetImportValidator.ValidateArtBible"/>.
    /// </summary>
    public class ArtBibleModelImporter : AssetPostprocessor
    {
        public const string Root = "Assets/Models/ArtBible/";
        public const string EnemyRoot = Root + "Enemies/";
        public const string ItemRoot = Root + "Items/";

        /// <summary>
        /// The baked emission map is 8-bit and clamps at 1.0; the shipping material scales it back up.
        /// Must equal <c>EMISSION_STRENGTH</c> in Tools/EnemyForge/enemy_forge/materials.py.
        /// </summary>
        public const float EmissionStrength = 9f;

        /// <summary>
        /// Largest texture size imported. The bake is 1024 and the user chose to keep it there until
        /// profiling says otherwise (plan, "Decisions recorded"): raising it is this one constant.
        /// </summary>
        public const int TextureSize = 1024;

        /// <summary>The root bone of the hound's Generic rig.</summary>
        public const string GenericRootBone = "Root";

        public const string LitShaderName = "Universal Render Pipeline/Lit";

        /// <summary>
        /// Unity's HumanBodyBones name → ArtForge's bone name. A copy of <c>UNITY_HUMANOID</c> in
        /// Tools/ArtForge/art_forge/figures.py; the humanoid avatar is built from exactly this, so a
        /// bone renamed on one side and not the other makes the avatar invalid, which
        /// <c>ArtBibleImportTests</c> reports.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> HumanBoneMap = new Dictionary<string, string>
        {
            ["Hips"] = "Hips", ["Spine"] = "Spine", ["Chest"] = "Chest", ["Neck"] = "Neck", ["Head"] = "Head",
            ["LeftShoulder"] = "Shoulder.L", ["LeftUpperArm"] = "UpperArm.L",
            ["LeftLowerArm"] = "LowerArm.L", ["LeftHand"] = "Hand.L",
            ["RightShoulder"] = "Shoulder.R", ["RightUpperArm"] = "UpperArm.R",
            ["RightLowerArm"] = "LowerArm.R", ["RightHand"] = "Hand.R",
            ["LeftUpperLeg"] = "UpperLeg.L", ["LeftLowerLeg"] = "LowerLeg.L", ["LeftFoot"] = "Foot.L",
            ["RightUpperLeg"] = "UpperLeg.R", ["RightLowerLeg"] = "LowerLeg.R", ["RightFoot"] = "Foot.R",
        };

        /// <summary>Texture suffixes that hold numbers, not colour, and must import linear.</summary>
        public static readonly string[] LinearTextureSuffixes = { "_ORM", "_MetallicGloss", "_Metallic", "_Roughness" };

        /// <summary>What kind of ArtBible asset a path is, or null for anything outside it.</summary>
        public enum Kind
        {
            HumanoidEnemy,
            GenericEnemy,
            Item
        }

        /// <summary>Classifies a model path. Pure, so the rules are tested without an import.</summary>
        public static Kind? KindOf(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) ||
                !assetPath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                return null;
            if (assetPath.StartsWith(ItemRoot, StringComparison.OrdinalIgnoreCase))
                return Kind.Item;
            if (!assetPath.StartsWith(EnemyRoot, StringComparison.OrdinalIgnoreCase))
                return null;
            string name = Path.GetFileNameWithoutExtension(assetPath);
            return ArtBibleEnemyCatalog.GenericRigModels.Contains(name) ? Kind.GenericEnemy : Kind.HumanoidEnemy;
        }

        /// <summary>True for a texture that holds data (ORM, metallic, roughness) rather than colour.</summary>
        public static bool IsLinearTexture(string assetPath)
        {
            string name = Path.GetFileNameWithoutExtension(assetPath);
            return LinearTextureSuffixes.Any(suffix => name.EndsWith(suffix, StringComparison.Ordinal));
        }

        /// <summary>The human bone list for the avatar, one entry per <see cref="HumanBoneMap"/> row.</summary>
        public static HumanBone[] BuildHumanBones()
        {
            return HumanBoneMap.Select(pair =>
            {
                var bone = new HumanBone { humanName = pair.Key, boneName = pair.Value };
                bone.limit.useDefaultValues = true;
                return bone;
            }).ToArray();
        }

        // -----------------------------------------------------------------------------------------
        // Models
        // -----------------------------------------------------------------------------------------

        private void OnPreprocessModel()
        {
            Kind? kind = KindOf(assetPath);
            if (!kind.HasValue)
                return;

            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.isReadable = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;

            switch (kind.Value)
            {
                case Kind.Item:
                    importer.animationType = ModelImporterAnimationType.None;
                    importer.importAnimation = false;
                    break;

                case Kind.GenericEnemy:
                    importer.animationType = ModelImporterAnimationType.Generic;
                    importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    importer.motionNodeName = GenericRootBone;
                    break;

                case Kind.HumanoidEnemy:
                    importer.animationType = ModelImporterAnimationType.Human;
                    importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    importer.humanDescription = HumanDescriptionFor(importer.humanDescription);
                    break;
            }
        }

        /// <summary>
        /// The explicit bone map, keeping any skeleton (rest pose) Unity already recorded for the
        /// model. Twist and stretch are Unity's own defaults, written out because a description
        /// carried over from the Generic import has them zeroed.
        /// </summary>
        private static HumanDescription HumanDescriptionFor(HumanDescription current)
        {
            current.human = BuildHumanBones();
            current.upperArmTwist = 0.5f;
            current.lowerArmTwist = 0.5f;
            current.upperLegTwist = 0.5f;
            current.lowerLegTwist = 0.5f;
            current.armStretch = 0.05f;
            current.legStretch = 0.05f;
            current.feetSpacing = 0f;
            current.hasTranslationDoF = false;
            return current;
        }

        /// <summary>
        /// Records the rest pose a Humanoid avatar needs and imports once more when the model has
        /// none yet (raid-scene-assembly.md, Traps). A second pass finds it and returns: no loop.
        /// </summary>
        private void OnPostprocessModel(GameObject root)
        {
            if (KindOf(assetPath) != Kind.HumanoidEnemy)
                return;

            var importer = (ModelImporter)assetImporter;
            HumanDescription description = importer.humanDescription;
            if (description.skeleton != null && description.skeleton.Length > 0)
                return;

            description.skeleton = SkeletonOf(root);
            importer.humanDescription = HumanDescriptionFor(description);
            string path = assetPath;
            Debug.Log($"[ArtBible] {path}: recorded the rest pose for its Humanoid avatar; reimporting once.");
            EditorApplication.delayCall += () =>
            {
                if (AssetImporter.GetAtPath(path) is ModelImporter again)
                    again.SaveAndReimport();
            };
        }

        /// <summary>Every transform under the model, with its rest-pose local transform.</summary>
        private static SkeletonBone[] SkeletonOf(GameObject root)
        {
            var bones = new List<SkeletonBone>();
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                bones.Add(new SkeletonBone
                {
                    name = t.name,
                    position = t.localPosition,
                    rotation = t.localRotation,
                    scale = t.localScale
                });
            }
            return bones.ToArray();
        }

        // -----------------------------------------------------------------------------------------
        // Materials
        // -----------------------------------------------------------------------------------------

        private void OnPostprocessMaterial(Material material)
        {
            if (!KindOf(assetPath).HasValue)
                return;

            Shader lit = Shader.Find(LitShaderName);
            if (lit == null)
            {
                Debug.LogError($"[ArtBible] {assetPath}: shader '{LitShaderName}' not found; material " +
                               $"'{material.name}' left as imported.");
                return;
            }

            string name = Path.GetFileNameWithoutExtension(assetPath);
            string textures = $"{Path.GetDirectoryName(assetPath).Replace('\\', '/')}/Textures/{name}";

            material.shader = lit;
            material.SetColor("_BaseColor", Color.white);

            Texture2D baseMap = LoadTexture(textures + "_BaseMap.png");
            if (baseMap != null)
                material.SetTexture("_BaseMap", baseMap);

            // URP Lit reads metallic from RGB and smoothness from alpha of _MetallicGlossMap, which is
            // exactly how EnemyForge packs <Name>_MetallicGloss.png. The ORM map is not bound: URP's
            // _OcclusionMap samples green, and ORM's green is roughness.
            Texture2D metallicGloss = LoadTexture(textures + "_MetallicGloss.png");
            if (metallicGloss != null)
            {
                material.SetTexture("_MetallicGlossMap", metallicGloss);
                material.SetFloat("_Smoothness", 1f);
                material.SetFloat("_SmoothnessTextureChannel", 0f);
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
            }

            bool emissive = ArtBibleEnemyCatalog.EmissiveModels.Contains(name);
            Texture2D emission = emissive ? LoadTexture(textures + "_Emission.png") : null;
            if (emission != null)
            {
                material.SetTexture("_EmissionMap", emission);
                material.SetColor("_EmissionColor", Color.white * EmissionStrength);
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                material.SetColor("_EmissionColor", Color.black);
                material.DisableKeyword("_EMISSION");
            }

            if (baseMap == null || metallicGloss == null || (emissive && emission == null))
                Debug.LogWarning($"[ArtBible] {assetPath}: missing baked map(s) beside the model " +
                                 $"(base {baseMap != null}, metallic-gloss {metallicGloss != null}, " +
                                 $"emission {(emissive ? (emission != null).ToString() : "n/a")}). " +
                                 "Reimport the model once its Textures folder has imported.");
        }

        private Texture2D LoadTexture(string path)
        {
            // Re-import the model when a baked map changes, so the material never points at stale art.
            context?.DependsOnArtifact(path);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // -----------------------------------------------------------------------------------------
        // Textures
        // -----------------------------------------------------------------------------------------

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root, StringComparison.OrdinalIgnoreCase))
                return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = !IsLinearTexture(assetPath);
            importer.mipmapEnabled = true;
            importer.isReadable = false;
            importer.maxTextureSize = TextureSize;
            importer.textureCompression = TextureImporterCompression.Compressed;
        }
    }
}
