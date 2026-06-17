// Game-related constants used throughout patches
// Extracted from CrunchatizerCore.cs during refactoring

using UnityEngine;

namespace SeaPowerCrunchatizer.Constants
{
    /// <summary>
    /// Contains game-related constants used throughout the mod's patches.
    /// Centralizing these values makes them easier to maintain and document.
    /// </summary>
    public static class GameConstants
    {
        // =====================================================================
        // Wire Guidance Constants
        // =====================================================================

        /// <summary>
        /// Maximum vessel speed (knots) before wire guidance connection breaks.
        /// Set high to effectively disable the speed limit.
        /// </summary>
        public const float WireGuidanceMaxSpeed = 20.0f;

        // =====================================================================
        // Sensor Arc Constants
        // =====================================================================

        /// <summary>
        /// Full vertical sensor arc (-90 to +90 degrees plus small margin for floating point).
        /// Used to give sensors complete vertical coverage.
        /// </summary>
        public static readonly Vector2 FullVerticalArc = new(-91f, 91f);

        /// <summary>
        /// Full horizontal sensor arc (-180 to +180 degrees plus small margin for floating point).
        /// Used to give sensors complete horizontal coverage.
        /// </summary>
        public static readonly Vector2 FullHorizontalArc = new(-181f, 181f);

        /// <summary>
        /// 360-degree field of view for omnidirectional coverage.
        /// </summary>
        public const float FullCircleFov = 360f;

        // =====================================================================
        // Sensor Enhancement Constants
        // =====================================================================

        /// <summary>
        /// Standard gain boost applied to enhanced sensors (in dB).
        /// </summary>
        public const float GainBoostDb = 20f;

        // =====================================================================
        // Seeker/Missile Constants
        // =====================================================================

        /// <summary>
        /// Full gimbal field of view for missile seekers (180 degrees = hemisphere).
        /// </summary>
        public const float FullGimbalFov = 180f;

        /// <summary>
        /// Visual range multiplier for enhanced visual sensors.
        /// </summary>
        public const float EnhancedVisualRangeMultiplier = 10f;

        // =====================================================================
        // Fire Control Constants
        // =====================================================================

        /// <summary>
        /// Effectively infinite value for channel counts (target/weapon channels).
        /// Using int.MaxValue ensures any comparison will pass.
        /// </summary>
        public const int InfiniteChannels = int.MaxValue;

        // =====================================================================
        // Ammunition Enhancement Constants
        // =====================================================================

        /// <summary>
        /// Near-zero value used for timing fields when setting to "instant".
        /// Using a small epsilon rather than 0 to avoid potential division issues.
        /// </summary>
        public const float NearInstantTime = 0.001f;
    }
}
