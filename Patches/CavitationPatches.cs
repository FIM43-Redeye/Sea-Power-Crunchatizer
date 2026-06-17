// Cavitation suppression patches for player units
// Unified approach: patch the propulsion system update methods to clear cavitation
// after the game's normal calculation, preserving all original parameters for UI.

using HarmonyLib;
using JetBrains.Annotations;
using SeaPower;
using SeaPowerCrunchatizer.Config;
using SeaPowerCrunchatizer.Utilities;
using UnityEngine;

namespace SeaPowerCrunchatizer.Patches
{
    /// <summary>
    /// Prevents submarine cavitation by clearing the flag after the game sets it.
    /// This approach preserves the original cavitation parameters, which are used
    /// for calculating depth presets and speed settings in the UI.
    /// </summary>
    [HarmonyPatch(typeof(SubmarinePropulsionSystem), "updateParticles")]
    internal static class NoCavitationSubmarine_Patch
    {
        /// <summary>
        /// Postfix that clears cavitation state for player submarines.
        /// Runs after the vanilla check sets _isCavitating, then overrides it.
        /// </summary>
        [HarmonyPostfix]
        [UsedImplicitly]
        private static void Postfix(SubmarinePropulsionSystem __instance)
        {
            if (!CheatConfig.NoCavSubmarine.Value)
            {
                return;
            }

            // Access private _submarine field via Traverse
            var submarine = Traverse.Create(__instance).Field<Submarine>("_submarine").Value;
            if (submarine == null || !PlayerUtils.IsPlayerUnit(submarine))
            {
                return;
            }

            // Override the cavitation state after the vanilla check
            if (submarine._isCavitating)
            {
                submarine._isCavitating = false;

                // Stop the particle effect if it's playing
                var subParams = Traverse.Create(__instance).Field<SubmarineParameters>("_submarineParameters").Value;
                subParams?._cavitation?.Stop();
            }
        }
    }

    /// <summary>
    /// Prevents vessel cavitation by clearing the flag after the game sets it.
    /// This approach preserves the original cavitation parameters and particle
    /// system references for proper side-swap restoration.
    /// </summary>
    [HarmonyPatch(typeof(VesselPropulsionSystem), "updateParticles")]
    internal static class NoCavitationVessel_Patch
    {
        /// <summary>
        /// Postfix that clears cavitation state for player vessels.
        /// Runs after the vanilla check sets _isCavitating, then overrides it.
        /// </summary>
        [HarmonyPostfix]
        [UsedImplicitly]
        private static void Postfix(VesselPropulsionSystem __instance)
        {
            if (!CheatConfig.NoCavVessel.Value)
            {
                return;
            }

            // Access private _vessel field via Traverse
            var vessel = Traverse.Create(__instance).Field<Vessel>("_vessel").Value;
            if (vessel == null || !PlayerUtils.IsPlayerUnit(vessel))
            {
                return;
            }

            // Override the cavitation state after the vanilla check
            if (vessel._isCavitating)
            {
                vessel._isCavitating = false;

                // Stop the particle effect if it's playing
                var vesselParams = Traverse.Create(__instance).Field<VesselParameters>("_vesselParameters").Value;
                vesselParams?._cavitation?.Stop();
            }
        }
    }

    /// <summary>
    /// Prevents torpedo cavitation by clearing the flag at initialization.
    /// Torpedoes inherit _isCavitating from ObjectBase but it's not visibly set
    /// anywhere in the decompiled code, yet still ends up true. This clears it
    /// for player-launched torpedoes so they run silent.
    /// </summary>
    [HarmonyPatch(typeof(Torpedo), "init", typeof(ObjectBase), typeof(Vector3), typeof(AmmunitionParameters))]
    internal static class NoCavitationTorpedo_Patch
    {
        /// <summary>
        /// Postfix that clears cavitation state for player-launched torpedoes.
        /// </summary>
        [HarmonyPostfix]
        [UsedImplicitly]
        private static void Postfix(Torpedo __instance, ObjectBase launchPlatform)
        {
            if (!CheatConfig.NoCavTorpedo.Value)
            {
                return;
            }

            // Only for torpedoes launched by player units
            if (!PlayerUtils.IsPlayerUnit(launchPlatform))
            {
                return;
            }

            __instance._isCavitating = false;
            PlayerUtils.LogIfSpam($"NoCavTorpedo: Cleared cavitation for torpedo launched by '{launchPlatform.name}'");
        }
    }
}
