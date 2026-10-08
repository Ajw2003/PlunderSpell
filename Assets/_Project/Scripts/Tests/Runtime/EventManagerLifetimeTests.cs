using Code.Scripts.EventSystems;
using NUnit.Framework;

namespace Plunderspell.Tests
{
    /// <summary>
    /// In Play there is always one event bus, created at start-up and kept across scene loads,
    /// so no scene has to hold one. See docs/4-systems/core.md, "Event bus".
    /// </summary>
    public class EventManagerLifetimeTests
    {
        [Test]
        public void TheBusExistsInPlayAndSurvivesSceneLoads()
        {
            Assert.IsNotNull(EventManager.Instance, "no event bus was created at start-up");
            Assert.AreEqual("DontDestroyOnLoad", EventManager.Instance.gameObject.scene.name,
                "the bus lives in a scene and would be lost when it unloads");
        }
    }
}
