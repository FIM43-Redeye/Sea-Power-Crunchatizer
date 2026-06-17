// Developer-only diagnostics.
//
// These patches only add information to the game's existing dev menu; they never
// change gameplay. They exist so we can SEE why a missile is or isn't holding speed
// (thrust vs drag) instead of guessing.

using System.Reflection;
using System.Text;
using HarmonyLib;
using JetBrains.Annotations;
using Sea_Power_Crunchatizer;
using SeaPower;
using SeaPowerCrunchatizer.Config;
using SeaPowerCrunchatizer.Utilities;

namespace SeaPowerCrunchatizer.Patches
{
    /// <summary>
    /// Appends a thrust/drag/burn breakdown to the dev menu's missile readout
    /// (DevModeUtils.GetMissileData), so the motor and drag forces can be compared
    /// directly. Only runs while the dev menu is open with a missile selected, and
    /// only when the Debug Missile Readout option is enabled.
    /// </summary>
    [HarmonyPatch(typeof(DevModeUtils), "GetMissileData")]
    internal static class MissileBurnReadoutPatch
    {
        // Missile.CalculateThrustOverTime(AmmunitionParameters ap, bool isAirLaunched,
        //   float timeSinceLaunch, float timeWindow) is private static - everything else
        // we need (IsMotorBurning, GetAccelerationTimes, CalculateDrag, the _ap burn
        // fields, the booster timing fields on WeaponBase) is public.
        private static readonly MethodInfo? _calculateThrustOverTime = Reflect.Method(
            typeof(Missile), "CalculateThrustOverTime",
            new[] { typeof(AmmunitionParameters), typeof(bool), typeof(float), typeof(float) });

        // Knots <-> Unity-units-per-fixed-step conversion used throughout Missile.cs.
        private const float KnotsToUnity = 0.0076554087f;

        [HarmonyPostfix]
        [UsedImplicitly]
        private static void Postfix(Missile missile, StringBuilder sb)
        {
            if (!CheatConfig.DebugMissileReadout.Value || missile == null || missile._ap == null)
            {
                return;
            }

            var ap = missile._ap;
            float timeSinceLaunch = GameTime.time - missile._boosterEffectStartTime;

            // Burn phases: Item1 = boost duration @ _acceleration, Item2 = sustainer @ _sustainerBurnAcceleration.
            var (boostTime, sustainerTime) = Missile.GetAccelerationTimes(ap, missile._airLaunched);

            // Thrust this second (timeWindow = 1f so it reads in knots/sec, matching the drag terms).
            string thrust = "n/a";
            if (_calculateThrustOverTime != null && missile._isBoosterEffectStarted)
            {
                var raw = (float)_calculateThrustOverTime.Invoke(
                    null, new object[] { ap, missile._airLaunched, timeSinceLaunch, 1f })!;
                thrust = $"{raw * missile._motorPerformance:F1} kn/s";
            }

            // Drag breakdown this second (time = 1f -> knots/sec). Induced drag is only
            // non-zero while coasting (motorBurning == false), which is exactly the point.
            float velocityUnity = missile.getVelocityInKnots() * KnotsToUnity;
            float altitude = missile.transform.position.y;
            Missile.CalculateDrag(
                altitude, velocityUnity, 1f, missile._currentPitch,
                ap.GetDragFactor(missile._airLaunched), ap.LiftFactor, missile.IsMotorBurning, altitude,
                out float aeroDragKnots, out float inducedDragKnots, out float parallelGKnots);

            sb.Append("\n<color=#8cffc6>[Crunchatizer]</color> Motor: ")
                .Append(missile.IsMotorBurning ? "<color=lime>BURNING</color>" : "<color=red>OUT</color>")
                .Append($" | Thrust: {thrust}");
            sb.Append($"\n  Drag/s: aero(v^2) {aeroDragKnots:F1} | induced {inducedDragKnots:F1} | gravity {parallelGKnots:F1}");
            sb.Append($"\n  Burn: t+{timeSinceLaunch:F1}s | boost {boostTime:F1}s @{ap._acceleration:F2} | sustainer {sustainerTime:F1}s @{ap._sustainerBurnAcceleration:F2}");
        }
    }
}
