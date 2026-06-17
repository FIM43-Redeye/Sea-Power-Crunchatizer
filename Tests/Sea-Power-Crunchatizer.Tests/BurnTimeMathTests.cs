// Tests for BurnTimeMath - the pure logic deciding how to make a missile's motor
// burn effectively forever. Linked into this test project (see the .csproj) so it
// runs without the mod's Unity/BepInEx dependencies.
//
// Game model (from the decompile): a Full-kinematics missile burns through a boost
// phase (_accelerationTime at _acceleration) then a sustainer phase (_sustainerBurnTime
// at _sustainerBurnAcceleration), after which thrust drops to zero and the missile
// coasts, bleeds speed to drag, and eventually stalls and self-destructs. To "burn
// forever" we extend the phase that actually produces thrust: the sustainer if the
// missile has one, otherwise the boost.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SeaPowerCrunchatizer.Utilities;

namespace SeaPowerCrunchatizer.Tests
{
    [TestClass]
    public class BurnTimeMathTests
    {
        [TestMethod]
        public void WithSustainer_ExtendsSustainerBurnTime()
        {
            // boost 4s, sustainer 10s @ accel 3 -> a real sustainer exists
            var (accelerationTime, sustainerBurnTime) =
                BurnTimeMath.ApplyInfiniteBurn(accelerationTime: 4f, sustainerBurnTime: 10f, sustainerBurnAcceleration: 3f);

            Assert.AreEqual(BurnTimeMath.InfiniteBurnSeconds, sustainerBurnTime, "sustainer should burn forever");
            Assert.AreEqual(4f, accelerationTime, "boost phase should be left untouched when a sustainer exists");
        }

        [TestMethod]
        public void BoostOnly_NoSustainerTime_ExtendsBoostInstead()
        {
            // sustainer time 0 -> boost-only missile; extend the boost
            var (accelerationTime, sustainerBurnTime) =
                BurnTimeMath.ApplyInfiniteBurn(accelerationTime: 6f, sustainerBurnTime: 0f, sustainerBurnAcceleration: 0f);

            Assert.AreEqual(BurnTimeMath.InfiniteBurnSeconds, accelerationTime, "boost should burn forever");
            Assert.AreEqual(0f, sustainerBurnTime, "absent sustainer should stay absent");
        }

        [TestMethod]
        public void SustainerTimeButNoAcceleration_TreatedAsBoostOnly()
        {
            // a sustainer time with zero acceleration produces no thrust, so it is not a
            // real sustainer - extend the boost instead.
            var (accelerationTime, sustainerBurnTime) =
                BurnTimeMath.ApplyInfiniteBurn(accelerationTime: 5f, sustainerBurnTime: 8f, sustainerBurnAcceleration: 0f);

            Assert.AreEqual(BurnTimeMath.InfiniteBurnSeconds, accelerationTime);
            Assert.AreEqual(8f, sustainerBurnTime, "sustainer time is left as-is since it produces no thrust");
        }

        [TestMethod]
        public void InfiniteBurnSeconds_OutlastsAnyRealisticFlight()
        {
            // The missile's own _maxFlightTime (<= 600s by default) caps its lifetime, so
            // the burn constant only needs to comfortably exceed that.
            Assert.IsTrue(BurnTimeMath.InfiniteBurnSeconds > 600f);
        }
    }
}
