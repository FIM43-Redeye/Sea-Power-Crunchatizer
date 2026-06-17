// Sea Power Crunchatizer - BepInEx Plugin Entry Point
// This is the minimal entry point that initializes configuration and applies Harmony patches.
// All actual functionality is organized in separate files under Config/, Patches/, Utilities/, etc.

using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SeaPowerCrunchatizer.Config;
using SeaPowerCrunchatizer.Patches;

// ReSharper disable InconsistentNaming

namespace Sea_Power_Crunchatizer
{
    /// <summary>
    /// Main entry point for the Sea Power Crunchatizer BepInEx plugin.
    /// Handles plugin initialization, configuration loading, and Harmony patch application.
    /// </summary>
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Sea Power.exe")]
    public class CrunchatizerCore : BaseUnityPlugin
    {
        /// <summary>
        /// Plugin metadata constants used by BepInEx for plugin identification.
        /// </summary>
        public static class PluginInfo
        {
            /// <summary>Unique identifier for this plugin.</summary>
            public const string PLUGIN_GUID = "net.particle.sea_power_crunchatizer";

            /// <summary>Display name of the plugin.</summary>
            public const string PLUGIN_NAME = "Sea Power Crunchatizer";

            /// <summary>Current version of the plugin.</summary>
            public const string PLUGIN_VERSION = "2.2.2";
        }

        /// <summary>
        /// Static logger instance for use throughout the mod.
        /// Initialized in Awake() before any other code runs.
        /// </summary>
        internal static ManualLogSource Log = null!;

        /// <summary>
        /// BepInEx entry point. Called when the plugin is loaded.
        /// Initializes logging, loads configuration, and applies all Harmony patches.
        /// </summary>
        private void Awake()
        {
            // Initialize logger first so all other code can use it
            Log = Logger;
            Log.LogInfo($"Loading {PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION}...");

            // Load configuration from BepInEx config file
            CheatConfig.Initialize(Config);
            Log.LogInfo("Configuration loaded.");

            // Apply all Harmony patches
            try
            {
                var harmony = new Harmony(PluginInfo.PLUGIN_GUID);

                // Apply all [HarmonyPatch] annotated classes in this assembly
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                Log.LogInfo("Harmony attribute-based patches applied successfully.");

                // Apply dynamic radar horizon patches (requires special handling)
                RadarHorizonGlobalPatcher.Apply(harmony);
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to apply Harmony patches: {ex}");
            }

            Log.LogInfo($"{PluginInfo.PLUGIN_NAME} loaded successfully!");
        }
    }
}
