// Common utility methods for player unit detection and logging
// Extracted from CrunchatizerCore.cs during refactoring

using Sea_Power_Crunchatizer;
using SeaPower;
using SeaPowerCrunchatizer.Config;

namespace SeaPowerCrunchatizer.Utilities
{
    /// <summary>
    /// Common utility methods for player unit detection and logging.
    /// These helpers eliminate duplicate boilerplate code across patches.
    /// </summary>
    public static class PlayerUtils
    {
        /// <summary>
        /// Checks if the given ObjectBase belongs to the player's taskforce.
        /// </summary>
        /// <param name="obj">The object to check (can be null).</param>
        /// <returns>True if the object belongs to the player's side, false otherwise.</returns>
        /// <remarks>
        /// This method is null-safe and will return false if any part of the chain is null.
        /// Use this instead of repeating the pattern:
        /// <code>obj?._taskforce?.Side == Taskforce.TfType.Player</code>
        /// </remarks>
        public static bool IsPlayerUnit(ObjectBase? obj)
        {
            return obj?._taskforce?.Side == Taskforce.TfType.Player;
        }

        /// <summary>
        /// Checks if the given BaseSystem belongs to a player unit.
        /// </summary>
        /// <param name="system">The system to check (can be null).</param>
        /// <returns>True if the system's parent object belongs to the player's side, false otherwise.</returns>
        public static bool IsPlayerSystem(BaseSystem? system)
        {
            return IsPlayerUnit(system?._baseObject);
        }

        /// <summary>
        /// Checks if the given SensorSystem belongs to a player unit.
        /// </summary>
        /// <param name="sensor">The sensor to check (can be null).</param>
        /// <returns>True if the sensor's parent object belongs to the player's side, false otherwise.</returns>
        public static bool IsPlayerSensor(SensorSystem? sensor)
        {
            return IsPlayerUnit(sensor?._baseObject);
        }

        /// <summary>
        /// Checks if the given WeaponSystem belongs to a player unit.
        /// </summary>
        /// <param name="weapon">The weapon system to check (can be null).</param>
        /// <returns>True if the weapon's parent object belongs to the player's side, false otherwise.</returns>
        public static bool IsPlayerWeapon(WeaponSystem? weapon)
        {
            return IsPlayerUnit(weapon?._baseObject);
        }

        /// <summary>
        /// Logs a message if LogSpam is enabled in configuration.
        /// Use this for informational debugging messages that may be frequent.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void LogIfSpam(string message)
        {
            if (CheatConfig.LogSpam.Value)
            {
                CrunchatizerCore.Log.LogInfo(message);
            }
        }

        /// <summary>
        /// Logs a message if ExtremeLogSpam is enabled in configuration.
        /// Use this for per-frame or very high-frequency logging that would normally
        /// cause severe performance issues.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void LogIfExtremeSpam(string message)
        {
            if (CheatConfig.ExtremeLogSpam.Value)
            {
                CrunchatizerCore.Log.LogInfo(message);
            }
        }

        /// <summary>
        /// Logs a warning message if LogSpam is enabled in configuration.
        /// </summary>
        /// <param name="message">The warning message to log.</param>
        public static void WarnIfSpam(string message)
        {
            if (CheatConfig.LogSpam.Value)
            {
                CrunchatizerCore.Log.LogWarning(message);
            }
        }
    }
}
