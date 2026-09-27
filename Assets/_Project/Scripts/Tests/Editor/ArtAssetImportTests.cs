using NUnit.Framework;

namespace Plunderspell.Tests.Editor
{
    /// <summary>
    /// EditMode cover for the art pipeline's Unity half. The pipeline validates geometry in
    /// Blender before export; this asserts the exported FBX survive Unity's importer — indexed,
    /// non-empty, textured, and agreeing with Blender about the triangle count.
    /// </summary>
    public class ArtAssetImportTests
    {
        [Test]
        public void GeneratedPropsImportCleanly()
        {
            var report = ArtAssetImportValidator.Validate();
            Assert.That(report.Failures, Is.Empty, string.Join("\n", report.Lines));
        }

        /// <summary>
        /// The art-bible models and textures carry the settings ArtBibleModelImporter owns: rig type
        /// per model, a valid Humanoid avatar for every human, the hound's Generic root, URP Lit with
        /// the ×9 emission, linear data maps. docs/plans/artbible-enemies-in-engine.md, E0.
        /// </summary>
        [Test]
        public void ArtBibleModelsImportWithTheirOwnedSettings()
        {
            var report = ArtAssetImportValidator.ValidateArtBible();
            Assert.That(report.Failures, Is.Empty, string.Join("\n", report.Lines));
        }
    }
}
