// Submarine-specific patches
// Refactored to use targeted patches instead of parameter modifications
// to avoid breaking depth preset UI calculations

using HarmonyLib;
using JetBrains.Annotations;
using SeaPower;
using SeaPowerCrunchatizer.Config;
using SeaPowerCrunchatizer.Systems;
using SeaPowerCrunchatizer.Utilities;

namespace SeaPowerCrunchatizer.Patches
{
    /// <summary>
    /// Modifies submarine parameters at initialization for player submarines.
    /// Uses CheatApplicator to ensure state tracking for side-swap support.
    /// Note: Cavitation and crush depth cheats are now handled by separate patches
    /// that target the checks rather than modifying parameters (to preserve UI calculations).
    /// </summary>
    [HarmonyPatch(typeof(Submarine), "init", typeof(SubmarineParameters))]
    internal static class EditSubmarineAtInit
    {
        /// <summary>
        /// Postfix that applies submarine cheats after initialization.
        /// </summary>
        [HarmonyPostfix]
        [UsedImplicitly]
        private static void Postfix(Submarine __instance)
        {
            if (!PlayerUtils.IsPlayerUnit(__instance))
            {
                return;
            }

            CheatApplicator.ApplySubmarineCheats(__instance);
        }
    }

    // Note: Infinite battery removed - use game's built-in _unlimitedFuel option.
    // Cavitation patch moved to CavitationPatches.cs for unified handling.

    /// <summary>
    /// Prevents submarines from being crushed at depth by skipping the crush check.
    /// This approach preserves the original crush depth parameter, which is also
    /// used for calculating the "Very Deep" preset and clamping depth in the UI.
    /// </summary>
    [HarmonyPatch(typeof(Submarine), "crushSubmarine")]
    internal static class InfiniteDepthSubmarine_Patch
    {
        /// <summary>
        /// Prefix that skips crushing for player submarines.
        /// </summary>
        [HarmonyPrefix]
        [UsedImplicitly]
        private static bool Prefix(Submarine __instance)
        {
            if (!CheatConfig.InfiniteSubDepth.Value)
            {
                return true; // Run original
            }

            if (!PlayerUtils.IsPlayerUnit(__instance))
            {
                return true; // Run original
            }

            // Skip the crush - submarine survives
            return false;
        }
    }
}
