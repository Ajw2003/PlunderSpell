using System.IO;
using UnityEditor;
using UnityEngine;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// Puts every castle model's materials on Plunderspell/Surface as they import, choosing the
    /// detail each one gets from its pigment (docs/plans/night-atmosphere.md, section 3). The asset
    /// pipeline gives every mesh one material per pigment, named "Module_pigment", so no module needs
    /// touching by hand. Also sets the detail texture's import settings: linear, tiling, alpha kept.
    /// </summary>
    public class CastleSurfaceMaterials : AssetPostprocessor
    {
        public const string CastleModelRoot = "Assets/_Project/Art/Models/Castle/";
        public const string DetailTexturePath = "Assets/_Project/Art/Textures/SurfaceDetail.png";
        public const string SurfaceShaderName = "Plunderspell/Surface";

        /// <summary>Which channel of the detail texture a pigment takes.</summary>
        public enum Detail
        {
            Stone = 0,
            Wood = 1,
            Iron = 2,
            Cloth = 3,
        }

        // The palette's sixteen pigments (Tools/AssetPipeline/palette.py) and what each is built as in
        // the castle kits: stone for walls, floors and trim; oak for timber; bronze and orpiment for
        // metalwork; the saturated colours for banners, rugs and hangings.
        private static readonly (string Pigment, Detail Detail, bool IsStone)[] s_pigments =
        {
            ("verdigris_lo", Detail.Stone, true),
            ("vellum_faint", Detail.Stone, true),
            ("vellum_dim", Detail.Stone, true),
            ("bone_black", Detail.Stone, true),
            ("ash_hi", Detail.Stone, true),
            ("vellum", Detail.Stone, true),
            ("iron", Detail.Stone, true),
            ("line", Detail.Stone, true),
            ("ash", Detail.Stone, true),
            ("oak", Detail.Wood, false),
            ("bronze", Detail.Iron, false),
            ("orpiment", Detail.Iron, false),
            ("leather", Detail.Cloth, false),
            ("verdigris", Detail.Cloth, false),
            ("madder", Detail.Cloth, false),
            ("lapis", Detail.Cloth, false),
        };

        // Where an era's kit builds with a pigment differently (Tools/AssetPipeline/castle_builders_<era>.py):
        // the Bronze Age plasters its walls in "bronze" ochre and lays mudbrick in "leather"; the later
        // Ages line with brick in "leather"; "line" is blackened iron in the Late kit, walnut in Powder's.
        private static readonly (string Folder, string Pigment, Detail Detail, bool IsStone)[] s_eraOverrides =
        {
            ("BronzeAge", "bronze", Detail.Stone, true),
            ("BronzeAge", "leather", Detail.Stone, true),
            ("BronzeAge", "madder", Detail.Stone, false),
            ("LateMedieval", "leather", Detail.Stone, true),
            ("LateMedieval", "line", Detail.Iron, false),
            ("AgeOfPowder", "leather", Detail.Stone, true),
            ("AgeOfPowder", "line", Detail.Wood, false),
        };

        private void OnPreprocessTexture()
        {
            if (assetPath != DetailTexturePath)
                return;
            var importer = (TextureImporter)assetImporter;
            importer.sRGBTexture = false;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 4;
        }

// After URP's own FBX material preprocessor (order 1), which fills in the palette texture.
        public override int GetPostprocessOrder() => 100;

        public override uint GetVersion() => 1;

        // Models import their materials through a MaterialDescription, which is the hook that sees
        // embedded materials; OnPostprocessMaterial is not called for them.
        private void OnPreprocessMaterialDescription(UnityEditor.AssetImporters.MaterialDescription description,
            Material material, AnimationClip[] clips)
        {
            if (!assetPath.StartsWith(CastleModelRoot, System.StringComparison.OrdinalIgnoreCase))
                return;
            context.DependsOnSourceAsset(DetailTexturePath);
            Apply(material, EraFolderOf(assetPath));
        }

        /// <summary>The pigment a "Module_pigment" material is painted in, or null.</summary>
        public static string PigmentOf(string materialName)
        {
            foreach ((string pigment, Detail _, bool _) in s_pigments)
            {
                if (materialName.EndsWith("_" + pigment, System.StringComparison.Ordinal))
                    return pigment;
            }
            return null;
        }

        /// <summary>The era subfolder a castle model sits in ("BronzeAge"), or empty for High Medieval.</summary>
        public static string EraFolderOf(string modelPath)
        {
            string rest = modelPath.Substring(CastleModelRoot.Length);
            int slash = rest.IndexOf('/');
            return slash > 0 ? rest.Substring(0, slash) : string.Empty;
        }

        /// <summary>Moves a castle material onto the surface shader, keeping its palette texture.</summary>
        public static void Apply(Material material, string eraFolder = "")
        {
            Shader shader = Shader.Find(SurfaceShaderName);
            if (shader == null)
            {
                Debug.LogWarning($"[CastleSurface] {SurfaceShaderName} not found; {material.name} keeps {material.shader.name}.");
                return;
            }

            Texture palette = material.HasProperty("_BaseMap") ? material.GetTexture("_BaseMap") : null;
            material.shader = shader;
            if (palette != null)
                material.SetTexture("_BaseMap", palette);
            material.SetColor("_BaseColor", Color.white);

            string pigment = PigmentOf(material.name);
            Detail detail = Detail.Stone;
            bool isStone = false;
            foreach ((string name, Detail d, bool stone) in s_pigments)
            {
                if (name == pigment)
                {
                    detail = d;
                    isStone = stone;
                }
            }
            foreach ((string folder, string name, Detail d, bool stone) in s_eraOverrides)
            {
                if (folder == eraFolder && name == pigment)
                {
                    detail = d;
                    isStone = stone;
                }
            }

            material.SetTexture("_DetailMap", AssetDatabase.LoadAssetAtPath<Texture2D>(DetailTexturePath));
            material.SetVector("_DetailMask", new Vector4(
                detail == Detail.Stone ? 1 : 0, detail == Detail.Wood ? 1 : 0,
                detail == Detail.Iron ? 1 : 0, detail == Detail.Cloth ? 1 : 0));
            material.SetFloat("_DetailStrength", detail == Detail.Cloth ? 0.35f : pigment == null ? 0f : 0.6f);
            material.SetFloat("_IsStone", isStone ? 1f : 0f);
        }

        /// <summary>Reimports every castle model so its materials pass through <see cref="Apply"/>.</summary>
        [MenuItem("Tools/Plunderspell/Night Atmosphere/Reimport Castle Models Onto Surface Shader")]
        public static void ReimportMenu() => Debug.Log(ReimportCastleModels());

        public static string ReimportCastleModels()
        {
            AssetDatabase.ImportAsset(DetailTexturePath, ImportAssetOptions.ForceUpdate);
            string[] models = Directory.GetFiles(CastleModelRoot, "*.fbx", SearchOption.AllDirectories);
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string model in models)
                    AssetDatabase.ImportAsset(model.Replace('\\', '/'), ImportAssetOptions.ForceUpdate);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            return $"[CastleSurface] Reimported {models.Length} castle models onto {SurfaceShaderName}.";
        }
    }
}
