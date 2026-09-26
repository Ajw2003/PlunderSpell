using NUnit.Framework;

namespace RogueAi.Tests.Editor
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
    }
}
