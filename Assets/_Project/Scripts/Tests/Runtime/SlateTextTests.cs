using NUnit.Framework;
using Plunderspell.Market;
using Plunderspell.Raid;

namespace Plunderspell.Tests
{
    /// <summary>The words on a Market counter's chalk slate (#360).</summary>
    public class SlateTextTests
    {
        [Test]
        public void NoHaggleShowsHisNameAndWhatHeWants()
        {
            Assert.AreEqual("The Goldsmith - buys metal dearly", SlateText.For(Vendor.Goldsmith, false, ""));
            Assert.AreEqual("The Fence - buys anything", SlateText.For(Vendor.Fence, false, ""));
        }

        [Test]
        public void AnOpenHaggleShowsNameReplyAndTheKeys()
        {
            string line = VendorLines.Raised(Vendor.Goldsmith, 138);
            Assert.AreEqual("The Goldsmith\nVery well. 138 coin.\n<size=60%>1 Plus · 2 Satis · 3 Vale</size>",
                SlateText.For(Vendor.Goldsmith, true, line));
        }

        [Test]
        public void AClosedHaggleKeepsTheLastWordWithoutTheKeys()
        {
            Assert.AreEqual("The Pardoner\nFarewell.", SlateText.For(Vendor.Pardoner, false, VendorLines.Farewell(Vendor.Pardoner)));
        }
    }
}
