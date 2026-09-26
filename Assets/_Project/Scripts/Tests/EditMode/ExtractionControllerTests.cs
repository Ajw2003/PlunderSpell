using NUnit.Framework;
using Plunderspell.Core;

namespace Plunderspell.Tests.EditMode
{
    public class ExtractionControllerTests
    {
        [Test]
        public void StartExtraction_SetsIsExtractingTrue()
        {
            var controller = new ExtractionController(extractionDurationSeconds: 5f);

            controller.StartExtraction();

            Assert.IsTrue(controller.IsExtracting);
        }

        [Test]
        public void Tick_CompletesExtractionAfterDuration()
        {
            var controller = new ExtractionController(extractionDurationSeconds: 2f);
            bool completed = false;
            controller.ExtractionCompleted += () => completed = true;

            controller.StartExtraction();
            controller.Tick(2.5f);

            Assert.IsTrue(completed);
            Assert.IsFalse(controller.IsExtracting);
        }

        [Test]
        public void CancelExtraction_StopsExtraction()
        {
            var controller = new ExtractionController(extractionDurationSeconds: 5f);
            controller.StartExtraction();

            controller.CancelExtraction();

            Assert.IsFalse(controller.IsExtracting);
        }
    }
}
