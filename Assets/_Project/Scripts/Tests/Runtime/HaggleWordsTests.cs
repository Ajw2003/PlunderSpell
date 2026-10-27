using System.Collections.Generic;
using NUnit.Framework;
using Plunderspell.Market;

namespace Plunderspell.Tests
{
    /// <summary>The haggling words as the speech recogniser hears them (#312).</summary>
    public class HaggleWordsTests
    {
        [TestCase("PLUS", HaggleWord.Plus)]
        [TestCase("SATIS", HaggleWord.Satis)]
        [TestCase("VALE", HaggleWord.Vale)]
        [TestCase("plus", HaggleWord.Plus)]
        [TestCase("SAT IS", HaggleWord.Satis)]
        [TestCase("SAD IS", HaggleWord.Satis)]
        [TestCase("VALLEY", HaggleWord.Vale)]
        [TestCase("VEIL", HaggleWord.Vale)]
        public void ARecognisedWordMapsToTheHaggleWord(string text, HaggleWord expected)
        {
            Assert.IsTrue(HaggleWords.TryParse(text, out HaggleWord word));
            Assert.AreEqual(expected, word);
        }

        [TestCase("IGNIS")]
        [TestCase("FRANGO")]
        [TestCase("")]
        [TestCase(null)]
        [TestCase("PLUS ULTRA")]
        public void ASpellPhraseIsNotAHaggleWord(string text)
        {
            Assert.IsFalse(HaggleWords.TryParse(text, out _));
        }

        [Test]
        public void TheVocabularyGainsTheWordsAndKeepsASpellsSpelling()
        {
            var vocabulary = new Dictionary<string, string> { { "veil", "LEVO" }, { "igneous", "IGNIS" } };
            HaggleWords.AddTo(vocabulary);

            Assert.AreEqual("PLUS", vocabulary["plus"]);
            Assert.AreEqual("SATIS", vocabulary["satis"]);
            Assert.AreEqual("VALE", vocabulary["vale"]);
            Assert.AreEqual("LEVO", vocabulary["veil"], "a spell's spelling is never replaced");
            Assert.AreEqual("IGNIS", vocabulary["igneous"]);
        }
    }
}
