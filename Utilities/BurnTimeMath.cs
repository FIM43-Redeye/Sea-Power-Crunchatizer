// Pure logic for the "infinite burn time" cheat: given a missile's burn parameters,
// decide which burn phase to extend so the motor never cuts out.
//
// This file is intentionally dependency-free (no Unity/BepInEx) so it can be linked
// into the test project and unit-tested in isolation. The patch that actually mutates
// a missile's AmmunitionParameters lives in WeaponPatches and calls into this.
//
// Background (from the decompiled game): a Full-kinematics missile thrusts through a
// boost phase (_accelerationTime at _acceleration) then a sustainer phase
// (_sustainerBurnTime at _sustainerBurnAcceleration). Once total burn time elapses,
// CalculateThrustOverTime returns 0, the missile coasts, loses speed to drag, and
// once stalled it self-destructs. Extending the thrust-producing phase keeps the
// missile powered all the way to its lifetime limit (_maxFlightTime).

namespace SeaPowerCrunchatizer.Utilities
{
    /// <summary>
    /// Pure helpers for computing "infinite" burn parameters for a missile motor.
    /// </summary>
    public static class BurnTimeMath
    {
        /// <summary>
        /// A burn duration long enough to outlast any missile's flight time (which the
        /// game caps via _maxFlightTime, &lt;= 600s by default), making the motor
        /// effectively burn for the whole flight without using infinities that could
        /// upset downstream math.
        /// </summary>
        public const float InfiniteBurnSeconds = 100000f;

        /// <summary>
        /// Given a missile's current burn parameters, returns the burn times to apply so
        /// the motor burns effectively forever. The sustainer is extended when the missile
        /// has a real (thrust-producing) sustainer; otherwise the boost phase is extended.
        /// The unaffected phase is returned unchanged.
        /// </summary>
        /// <param name="accelerationTime">Current boost phase duration (_accelerationTime).</param>
        /// <param name="sustainerBurnTime">Current sustainer phase duration (_sustainerBurnTime).</param>
        /// <param name="sustainerBurnAcceleration">Sustainer acceleration; a sustainer only
        /// produces thrust when this is positive (_sustainerBurnAcceleration).</param>
        /// <returns>The (accelerationTime, sustainerBurnTime) to write back to the missile.</returns>
        public static (float accelerationTime, float sustainerBurnTime) ApplyInfiniteBurn(
            float accelerationTime, float sustainerBurnTime, float sustainerBurnAcceleration)
        {
            bool hasRealSustainer = sustainerBurnTime > 0f && sustainerBurnAcceleration > 0f;
            if (hasRealSustainer)
            {
                return (accelerationTime, InfiniteBurnSeconds);
            }

            return (InfiniteBurnSeconds, sustainerBurnTime);
        }

        /// <summary>
        /// How much the missile's flight-time/lifetime cap (_maxFlightTime) is extended when
        /// infinite burn is applied. The kinematic range simulation is bounded by _maxFlightTime,
        /// so a sustained motor only translates into longer range if the missile also lives longer.
        /// A multiplier (rather than a flat value) keeps the extension proportional: short-range
        /// weapons aren't over-simulated, long-range ones gain proportionally more reach.
        /// </summary>
        public const float FlightTimeMultiplier = 3f;

        /// <summary>
        /// Returns the extended flight-time cap for an infinite-burn missile. A non-positive input
        /// means "unset" (the game falls back to its own default), so it is left untouched rather
        /// than turned into a hard zero.
        /// </summary>
        public static float ExtendFlightTime(float maxFlightTime)
        {
            return maxFlightTime > 0f ? maxFlightTime * FlightTimeMultiplier : maxFlightTime;
        }
    }
}
