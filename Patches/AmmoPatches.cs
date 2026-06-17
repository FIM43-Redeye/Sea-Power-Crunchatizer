// Ammunition and magazine system patches
// Extracted from CrunchatizerCore.cs during refactoring

using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using SeaPower;
using SeaPowerCrunchatizer.Config;
using SeaPowerCrunchatizer.Utilities;

namespace SeaPowerCrunchatizer.Patches
{
    /// <summary>
    /// Contains Harmony patches for ammunition management cheats.
    /// Includes bottomless magazines and container auto-refresh functionality.
    /// </summary>
    internal static class AmmoPatches
    {
        // Patch classes are defined below as nested types
    }

    /// <summary>
    /// Prevents ammunition decrease for WeaponMagazineSystem (overload 1: string, bool).
    /// </summary>
    [HarmonyPatch(typeof(WeaponMagazineSystem), "decreaseAmmunitionCount", typeof(string), typeof(bool))]
    internal static class BottomlessMagsPatch1
    {
        /// <summary>
        /// Prefix that blocks ammo decrease for player units.
        /// </summary>
        [HarmonyPrefix]
        [UsedImplicitly]
        private static bool Prefix(WeaponMagazineSystem __instance)
        {
            if (!CheatConfig.BottomlessMags.Value)
            {
                return true;
            }

            if (PlayerUtils.IsPlayerSystem(__instance))
            {
                PlayerUtils.LogIfSpam(
                    $"BottomlessMags: Preventing ammo decrease for player weapon '{__instance._moduleName ?? "Unknown"}'.");
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Prevents ammunition decrease for WeaponMagazineSystem (overload 2: string, int, bool).
    /// </summary>
    [HarmonyPatch(typeof(WeaponMagazineSystem), "decreaseAmmunitionCount", typeof(string), typeof(int), typeof(bool))]
    internal static class BottomlessMagsPatch2
    {
        /// <summary>
        /// Prefix that blocks ammo decrease for player units.
        /// </summary>
        [HarmonyPrefix]
        [UsedImplicitly]
        private static bool Prefix(WeaponMagazineSystem __instance)
        {
            if (!CheatConfig.BottomlessMags.Value)
            {
                return true;
            }

            if (PlayerUtils.IsPlayerSystem(__instance))
            {
                PlayerUtils.LogIfSpam(
                    $"BottomlessMags: Preventing ammo decrease for player weapon '{__instance._moduleName ?? "Unknown"}'.");
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Prevents magazine ammo count decrease for WeaponSystem.
    /// </summary>
    [HarmonyPatch(typeof(WeaponSystem), "decreaseMagazineAmmoCount", typeof(string), typeof(int), typeof(bool))]
    internal static class BottomlessMagsPatch3
    {
        /// <summary>
        /// Prefix that blocks magazine ammo decrease for player units.
        /// </summary>
        [HarmonyPrefix]
        [UsedImplicitly]
        private static bool Prefix(WeaponSystem __instance)
        {
            if (!CheatConfig.BottomlessMags.Value)
            {
                return true;
            }

            if (PlayerUtils.IsPlayerWeapon(__instance))
            {
                PlayerUtils.LogIfSpam(
                    $"BottomlessMags: Preventing magazine decrease for player weapon system '{__instance._name ?? "Unknown"}'.");
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Prevents ammunition count increase for player units.
    /// This prevents ammo inflation when BottomlessMags is active.
    /// </summary>
    [HarmonyPatch(typeof(WeaponMagazineSystem), "increaseAmmunitionCount", typeof(string))]
    internal static class BottomlessMagsIncrement
    {
        /// <summary>
        /// Prefix that blocks ammo increase for player units to prevent inflation.
        /// </summary>
        [HarmonyPrefix]
        [UsedImplicitly]
        private static bool Prefix(WeaponMagazineSystem __instance)
        {
            if (!CheatConfig.BottomlessMags.Value)
            {
                return true;
            }

            if (PlayerUtils.IsPlayerSystem(__instance))
            {
                PlayerUtils.LogIfSpam(
                    $"BottomlessMags: Preventing ammo increase for player weapon '{__instance._moduleName ?? "Unknown"}'.");
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Automatically refills weapon containers when they become empty.
    /// Applies to launcher-type weapons that use containers rather than magazines.
    /// </summary>
    [HarmonyPatch(typeof(WeaponContainer), "launch")]
    internal static class ContainerAutoRefresh
    {
        /// <summary>
        /// Postfix that refills empty containers after a launch for player units.
        /// </summary>
        [HarmonyPostfix]
        [UsedImplicitly]
        private static void Postfix(ref WeaponContainer __instance)
        {
            if (!CheatConfig.ContainerAutoRefresh.Value)
            {
                PlayerUtils.LogIfSpam("Not doing anything, setting is off for container replen");
                return;
            }

            if (__instance._weaponSystem._baseObject._taskforce.Side != Taskforce.TfType.Player)
            {
                return;
            }

            var weaponsToInit = new List<WeaponSystem>();

            // Find all ammunition types that are now empty
            foreach (var ammoPair in __instance._weaponSystem._baseObject.AmmunitionAmountDictionary
                         .Where(ammoPair => ammoPair.Value == 0))
            {
                PlayerUtils.LogIfSpam($"{ammoPair.Key} has ammunition number {ammoPair.Value}");

                // Get all weapon systems that use this ammunition type
                foreach (var weaponTarget in __instance._weaponSystem._baseObject
                             .GetWeaponSystemsForAmmunition(ammoPair.Key))
                {
                    weaponsToInit.Add(weaponTarget);
                    PlayerUtils.LogIfSpam($"Adding {weaponTarget._name} to the list");
                }
            }

            // Reinitialize containers for all affected weapon systems
            foreach (var current in weaponsToInit)
            {
                foreach (WeaponContainer container in current._containers)
                {
                    container.init(current._baseObject._obp._launcherAmmunitionGroups, current._sectionName);
                }

                PlayerUtils.LogIfSpam($"Initialized {current._name}");
            }
        }
    }
}
