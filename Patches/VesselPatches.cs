// Surface vessel-specific patches
// Extracted from CrunchatizerCore.cs during refactoring

using HarmonyLib;
using JetBrains.Annotations;
using SeaPower;
using SeaPowerCrunchatizer.Systems;
using SeaPowerCrunchatizer.Utilities;

namespace SeaPowerCrunchatizer.Patches
{
    /// <summary>
    /// Modifies vessel parameters at initialization for player vessels.
    /// Uses CheatApplicator to ensure state tracking for side-swap support.
    /// </summary>
    [HarmonyPatch(typeof(Vessel), "init", typeof(VesselParameters))]
    internal static class EditVesselAtInit
    {
        /// <summary>
        /// Postfix that applies vessel cheats after initialization.
        /// </summary>
        [HarmonyPostfix]
        [UsedImplicitly]
        private static void Postfix(Vessel __instance)
        {
            if (!PlayerUtils.IsPlayerUnit(__instance))
            {
                return;
            }

            CheatApplicator.ApplyVesselCheats(__instance);
        }
    }
}
