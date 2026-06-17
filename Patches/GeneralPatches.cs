// General patches that apply to all unit types
// These handle cross-cutting concerns like unlimited fuel

using HarmonyLib;
using JetBrains.Annotations;
using SeaPower;
using SeaPowerCrunchatizer.Config;
using SeaPowerCrunchatizer.Utilities;

namespace SeaPowerCrunchatizer.Patches
{
    /// <summary>
    /// Re-applies unlimited fuel after SceneCreator.SetAdditionalParameters overwrites it from INI.
    /// SceneCreator reads _unlimitedFuel from scenario INI which defaults to false, undoing our setting.
    /// This postfix runs after that method completes to restore our setting for all player units.
    /// </summary>
    [HarmonyPatch(typeof(SceneCreator), "SetAdditionalParameters")]
    internal static class UnlimitedFuelSceneCreatorPatch
    {
        /// <summary>
        /// Postfix that re-applies unlimited fuel after SceneCreator finishes setting up the unit.
        /// </summary>
        [HarmonyPostfix]
        [UsedImplicitly]
        private static void Postfix(ObjectBase unit)
        {
            if (!CheatConfig.UnlimitedFuel.Value)
            {
                return;
            }

            // Only for player units
            if (!PlayerUtils.IsPlayerUnit(unit))
            {
                return;
            }

            if (!unit._unlimitedFuel)
            {
                unit._unlimitedFuel = true;
                PlayerUtils.LogIfSpam(
                    $"UnlimitedFuel: Re-applied for {unit._type} '{unit.name}' (after SceneCreator init)");
            }
        }
    }

    /// <summary>
    /// Sets unlimited fuel when units are assigned to a taskforce.
    /// This catches all unit spawns including encyclopedia, missions, and transfers.
    /// The _taskforce setter is called whenever a unit joins any taskforce.
    /// </summary>
    [HarmonyPatch(typeof(ObjectBase), "_taskforce", MethodType.Setter)]
    internal static class UnlimitedFuelTaskforcePatch
    {
        /// <summary>
        /// Postfix that sets _unlimitedFuel when a unit joins the player taskforce.
        /// Also removes it when leaving player control.
        /// </summary>
        [HarmonyPostfix]
        [UsedImplicitly]
        private static void Postfix(ObjectBase __instance, Taskforce value)
        {
            if (!CheatConfig.UnlimitedFuel.Value)
            {
                return;
            }

            bool isPlayerUnit = value?.Side == Taskforce.TfType.Player;

            if (isPlayerUnit && !__instance._unlimitedFuel)
            {
                __instance._unlimitedFuel = true;
                PlayerUtils.LogIfSpam(
                    $"UnlimitedFuel: Set for {__instance._type} '{__instance.name}' (joined taskforce)");
            }
            else if (!isPlayerUnit && __instance._unlimitedFuel)
            {
                __instance._unlimitedFuel = false;
                PlayerUtils.LogIfSpam(
                    $"UnlimitedFuel: Removed from {__instance._type} '{__instance.name}' (left player taskforce)");
            }
        }
    }
}
