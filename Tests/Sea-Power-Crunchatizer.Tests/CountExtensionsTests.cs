// Tests for CountExtensions - the pure counting helper that replaces the
// repeated "if (!dict.ContainsKey(k)) dict[k] = 0; dict[k]++" idiom in the patches.
// The source file is linked into this test project (see the .csproj) so we can
// exercise it without pulling in the mod's Unity/BepInEx dependencies.

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SeaPowerCrunchatizer.Utilities;

namespace SeaPowerCrunchatizer.Tests
{
    [TestClass]
    public class CountExtensionsTests
    {
        [TestMethod]
        public void Increment_OnAbsentKey_StartsAtOne()
        {
            var counts = new Dictionary<string, int>();

            counts.Increment("harpoon");

            Assert.AreEqual(1, counts["harpoon"]);
        }

        [TestMethod]
        public void Increment_OnExistingKey_AddsOne()
        {
            var counts = new Dictionary<string, int> { ["harpoon"] = 2 };

            counts.Increment("harpoon");

            Assert.AreEqual(3, counts["harpoon"]);
        }

        [TestMethod]
        public void Increment_RepeatedCalls_Accumulate()
        {
            var counts = new Dictionary<string, int>();

            counts.Increment("sm2");
            counts.Increment("sm2");
            counts.Increment("sm2");

            Assert.AreEqual(3, counts["sm2"]);
        }

        [TestMethod]
        public void Increment_WithExplicitAmount_AddsThatAmount()
        {
            var counts = new Dictionary<string, int>();

            counts.Increment("torpedo", 5);

            Assert.AreEqual(5, counts["torpedo"]);
        }

        [TestMethod]
        public void Increment_WithNullKey_IsIgnoredAndReturnsFalse()
        {
            var counts = new Dictionary<string, int>();

            // A null ammo filename must not crash (Dictionary would throw on a null key)
            // and must not create a phantom entry.
            bool counted = counts.Increment(null);

            Assert.IsFalse(counted);
            Assert.AreEqual(0, counts.Count);
        }

        [TestMethod]
        public void Increment_WithEmptyKey_IsIgnoredAndReturnsFalse()
        {
            var counts = new Dictionary<string, int>();

            bool counted = counts.Increment("");

            Assert.IsFalse(counted);
            Assert.AreEqual(0, counts.Count);
        }

        [TestMethod]
        public void Increment_WithRealKey_ReturnsTrue()
        {
            var counts = new Dictionary<string, int>();

            bool counted = counts.Increment("exocet");

            Assert.IsTrue(counted);
        }
    }
}
