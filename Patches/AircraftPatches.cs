// Aircraft, helicopter, and flight deck patches
// Extracted from CrunchatizerCore.cs during refactoring

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using Sea_Power_Crunchatizer;
using SeaPower;
using SeaPowerCrunchatizer.Config;
using SeaPowerCrunchatizer.Data;
using SeaPowerCrunchatizer.Utilities;
using UnityEngine;

namespace SeaPowerCrunchatizer.Patches
{
    /// <summary>
    /// Contains Harmony patches for flying unit systems including auto-rearm,
    /// flight deck modifications, and fuel consumption.
    /// Auto-rearm works for both Aircraft and Helicopter types via WeaponSystemHardpoint patches.
    /// </summary>
    public static class AircraftPatches
    {
        // --- CACHED REFLECTION (private game members only) ---
        // Most hardpoint/weapon members we touch (_weapons, _loadedAmmunitionCount,
        // _baseObject, _systemName) are public in the game and accessed directly below.
        // Only these two are private, so we reach them via reflection - resolved once
        // here and loudly logged by Reflect if a game update ever renames them.
        private static readonly FieldInfo? _loadedAmmunitionField;
        private static readonly MethodInfo? _createAmmunitionObjectInstanceMethod;

        /// <summary>
        /// Thread-local flag indicating a rearm operation is in progress.
        /// Prevents recursive triggering of rearm logic.
        /// </summary>
        [ThreadStatic]
        private static bool _isRearming;

        /// <summary>
        /// Thread-local reference to the hardpoint that was just rearmed.
        /// </summary>
        [ThreadStatic]
        public static WeaponSystemHardpoint? _justRearmedHardpoint;

        /// <summary>
        /// Static constructor that caches reflection data for performance.
        /// Runs once when the type is first accessed.
        /// </summary>
        static AircraftPatches()
        {
            CrunchatizerCore.Log.LogInfo("[AutoRearm] Caching reflection data...");

            // _loadedAmmunition is a private Dictionary<Ammunition, int> holding the actual
            // Ammunition objects with type data (the public _loadedAmmunitionCount only has filenames).
            _loadedAmmunitionField = Reflect.Field(typeof(WeaponSystemHardpoint), "_loadedAmmunition");
            _createAmmunitionObjectInstanceMethod =
                Reflect.Method(typeof(WeaponSystemHardpoint), "createAmmunitionObjectInstance");

            CrunchatizerCore.Log.LogInfo("[AutoRearm] Caching complete.");
        }

        /// <summary>
        /// Core rearm logic that restores a hardpoint's ammunition to its original loadout.
        /// Works for any ObjectBase with hardpoint weapons (aircraft, helicopters, etc.).
        /// </summary>
        /// <param name="hardpoint">The hardpoint to rearm.</param>
        public static void RearmHardpoint(WeaponSystemHardpoint hardpoint)
        {
            var objectType = hardpoint._baseObject._type;
            PlayerUtils.LogIfSpam($"Rearm func called for {objectType}");
            _isRearming = true;

            string unitId = "null";
            string? systemName = "null";

            try
            {
                var loadoutData = hardpoint._baseObject.GetComponent<HardpointLoadoutData>();
                if (loadoutData == null)
                {
                    PlayerUtils.WarnIfSpam(
                        $"[Rearm] No HardpointLoadoutData found for {objectType} hardpoint {hardpoint._systemName}. Cannot rearm.");
                    return;
                }

                var weaponsList = hardpoint._weapons;
                var loadedAmmunition = (Dictionary<Ammunition, int>?)_loadedAmmunitionField?.GetValue(hardpoint);
                var loadedAmmunitionCount = hardpoint._loadedAmmunitionCount;
                var baseObject = hardpoint._baseObject;
                systemName = hardpoint._systemName;

                if (baseObject)
                {
                    unitId = baseObject.getUIDAndName();
                }

                // Debug logging
                PlayerUtils.LogIfSpam($"[Rearm] Begin rearm for {objectType}: {unitId} hardpoint {systemName}");

                if (loadedAmmunitionCount != null)
                {
                    foreach (var kvp in loadedAmmunitionCount)
                    {
                        PlayerUtils.LogIfSpam($"[Rearm] Pre-clear ammo '{kvp.Key}' = {kvp.Value}");
                    }
                }
                else
                {
                    PlayerUtils.LogIfSpam("[Rearm] Pre-clear ammo dictionary is null");
                }

                if (weaponsList != null)
                {
                    PlayerUtils.LogIfSpam($"[Rearm] Weapons to destroy: {weaponsList.Count}");
                    if (loadedAmmunition != null)
                    {
                        PlayerUtils.LogIfSpam($"[Rearm] LoadedAmmunition entries: {loadedAmmunition.Count}");
                    }

                    // 1. Destroy the old weapon objects
                    foreach (WeaponBase weapon in new List<WeaponBase>(weaponsList))
                    {
                        if (weapon)
                        {
                            string file = weapon._ap?._ammunitionFileName ?? "null";
                            PlayerUtils.LogIfSpam(
                                $"[Rearm] Freeing weapon object: file={file}, go={weapon.gameObject.name}");
                            Singleton<PoolManager>.Instance.freeObject(file, weapon.gameObject, weapon);
                        }
                    }

                    weaponsList.Clear();
                    loadedAmmunition?.Clear();
                }

                // 2. Reset ammunition counts
                if (loadedAmmunitionCount != null && baseObject)
                {
                    foreach (var ammoCount in new Dictionary<string, int>(loadedAmmunitionCount))
                    {
                        PlayerUtils.LogIfSpam($"[Rearm] Decrement base ammo '{ammoCount.Key}' by {ammoCount.Value}");
                        baseObject.changeAmmunitionAmount(ammoCount.Key, -ammoCount.Value);
                    }

                    loadedAmmunitionCount.Clear();
                    PlayerUtils.LogIfSpam("[Rearm] Cleared loadedAmmunitionCount");
                }

                // 3. Re-create all weapons using the stored initial loadout data
                PlayerUtils.LogIfSpam($"[Rearm] Restoring {loadoutData.LoadoutInfos.Count} station entries");

                foreach (StationLoadoutInfo loadoutInfo in loadoutData.LoadoutInfos)
                {
                    // Skip fuel tanks - use game type data for reliable detection
                    if (loadoutInfo.IsFuelTank)
                    {
                        PlayerUtils.LogIfSpam($"[Rearm] Skipping fuel tank: '{loadoutInfo.AmmoFileName}'");
                        continue;
                    }

                    if (!loadoutInfo.StationParent || baseObject == null)
                    {
                        continue;
                    }

                    // Calculate the original coordinate
                    Vector3 originalVector =
                        loadoutInfo.StationParent.transform.position - baseObject.transform.position;
                    Vector3 adjustedSpawnPosition = loadoutInfo.LocalSpawnPosition + originalVector;

                    PlayerUtils.LogIfSpam(
                        $"[Rearm] Creating ammo '{loadoutInfo.AmmoFileName}'. Adjusted LocalPos:{loadoutInfo.LocalSpawnPosition}. Passing position {adjustedSpawnPosition}.");

                    _createAmmunitionObjectInstanceMethod?.Invoke(hardpoint, new object[]
                    {
                        loadoutInfo.AmmoFileName ?? string.Empty,
                        adjustedSpawnPosition,
                        loadoutInfo.LocalStationRotation.eulerAngles,
                        loadoutInfo.StationParent,
                        loadoutInfo.HostObjectParameters ?? throw new InvalidOperationException()
                    });
                }

                // 4. Finalize state
                hardpoint._isEmpty = false;
                hardpoint.HideWeapons();

                PlayerUtils.LogIfSpam($"[Rearm] Completed rearm for {objectType}: {unitId} hardpoint {systemName}");
            }
            catch (Exception ex)
            {
                PlayerUtils.LogIfSpam($"[Rearm] ERROR during rearm for {objectType}: {unitId} hardpoint {systemName} => {ex}");
                throw;
            }
            finally
            {
                _isRearming = false;
                _justRearmedHardpoint = hardpoint;
            }
        }

        /// <summary>
        /// Checks if a rearm operation is currently in progress.
        /// </summary>
        internal static bool IsRearming => _isRearming;
    }

    /// <summary>
    /// Captures initial loadout data when ammunition is created on hardpoints.
    /// Works for both aircraft and helicopters - any ObjectBase with WeaponSystemHardpoint.
    /// </summary>
    [HarmonyPatch(typeof(WeaponSystemHardpoint), "createAmmunitionObjectInstance")]
    internal static class CaptureLoadout_Patch
    {
        /// <summary>
        /// Postfix that records loadout information for later rearm operations.
        /// Applies to all player-owned units with hardpoint weapons (aircraft, helicopters, etc.).
        /// </summary>
        [HarmonyPostfix]
        [UsedImplicitly]
        private static void Postfix(WeaponSystemHardpoint __instance, string ammoFileName, ObjectBaseParameters obp)
        {
            if (AircraftPatches.IsRearming || __instance._baseObject._taskforce.Side != Taskforce.TfType.Player)
            {
                return;
            }

            var objectType = __instance._baseObject._type;
            PlayerUtils.LogIfSpam($"Loadout capture called for {objectType}: {__instance._baseObject.getUIDAndName()}");

            try
            {
                var loadoutData = __instance._baseObject.GetComponent<HardpointLoadoutData>();
                if (loadoutData == null)
                {
                    loadoutData = __instance._baseObject.gameObject.AddComponent<HardpointLoadoutData>();
                }

                var weaponsList = __instance._weapons;

                if (weaponsList == null || weaponsList.Count == 0)
                {
                    PlayerUtils.WarnIfSpam("[Capture] Weapons list is null or empty. Cannot capture.");
                    return;
                }

                WeaponBase newWeapon = weaponsList[weaponsList.Count - 1];
                if (newWeapon == null || newWeapon.gameObject == null)
                {
                    PlayerUtils.WarnIfSpam("[Capture] The newly created weapon or its GameObject is null.");
                    return;
                }

                Transform actualParentTransform = newWeapon.transform.parent;
                if (actualParentTransform == null)
                {
                    PlayerUtils.WarnIfSpam(
                        $"[Capture] Weapon '{ammoFileName}' was created with a null parent. Cannot capture local coordinates.");
                    return;
                }

                // Get ammunition parameters for reliable type detection
                var ammoParams = newWeapon._ap;
                bool isFuelTank = ammoParams?._subType == Ammunition.Type.Fueltank;
                float fuelMass = ammoParams?._fuelMassInKg ?? 0f;

                var loadoutInfo = new StationLoadoutInfo
                {
                    AmmoFileName = ammoFileName,
                    LocalSpawnPosition = newWeapon.transform.localPosition,
                    LocalStationRotation = newWeapon.transform.localRotation,
                    StationParent = actualParentTransform.gameObject,
                    HostObjectParameters = obp,
                    // Store ammunition type data for reliable fuel tank detection
                    IsFuelTank = isFuelTank,
                    FuelMassKg = fuelMass,
                    AmmoType = ammoParams?._type ?? Ammunition.Type.Unknown,
                    AmmoSubType = ammoParams?._subType ?? Ammunition.Type.None
                };
                loadoutData.LoadoutInfos.Add(loadoutInfo);

                PlayerUtils.LogIfSpam(
                    $"[Capture] SUCCESS for {objectType}. Added ammo: '{loadoutInfo.AmmoFileName}' localPos={loadoutInfo.LocalSpawnPosition} localRot={loadoutInfo.LocalStationRotation.eulerAngles} parent={loadoutInfo.StationParent?.name ?? "null"}");
            }
            catch (Exception ex)
            {
                PlayerUtils.LogIfSpam($"[Capture] UNEXPECTED ERROR capturing loadout: {ex}");
            }
        }
    }

    /// <summary>
    /// Triggers rearm operation when any weapon type is exhausted from a hardpoint.
    /// Works for both aircraft and helicopters - any ObjectBase with WeaponSystemHardpoint.
    /// Compares original loadout (from HardpointLoadoutData) with current _weapons list.
    /// </summary>
    [HarmonyPatch(typeof(WeaponSystemHardpoint), "launch")]
    internal static class RearmTrigger_Patch
    {
        /// <summary>
        /// Postfix that checks if rearm is needed after a weapon launch.
        /// Compares the original loadout with current remaining weapons.
        /// Triggers rearm when ANY non-fuel-tank weapon type is exhausted, not just when all are gone.
        /// </summary>
        [HarmonyPostfix]
        [UsedImplicitly]
        private static void Postfix(WeaponSystemHardpoint __instance)
        {
            var objectType = __instance._baseObject._type;
            PlayerUtils.LogIfSpam($"Post-launch rearm trigger called for {objectType}: {__instance._baseObject.getUIDAndName()}");

            try
            {
                if (__instance._baseObject._taskforce.Side != Taskforce.TfType.Player ||
                    !CheatConfig.AircraftInfiniteAmmo.Value)
                {
                    return;
                }

                if (AircraftPatches.IsRearming)
                {
                    PlayerUtils.LogIfSpam("[Trigger] Skipped: currently rearming");
                    return;
                }

                // Get original loadout from our captured data
                var loadoutData = __instance._baseObject.GetComponent<HardpointLoadoutData>();
                if (loadoutData == null || loadoutData.LoadoutInfos.Count == 0)
                {
                    PlayerUtils.LogIfSpam("[Trigger] No loadout data captured for this unit");
                    return;
                }

                // Get current remaining weapons
                var currentWeapons = __instance._weapons;

                // Count original non-fuel-tank weapons by type
                var originalCounts = new Dictionary<string, int>();
                foreach (var info in loadoutData.LoadoutInfos)
                {
                    if (info.IsFuelTank || info.AmmoSubType == Ammunition.Type.Fueltank)
                    {
                        continue;
                    }

                    originalCounts.Increment(info.AmmoFileName);
                }

                // Count current remaining weapons by type
                var currentCounts = new Dictionary<string, int>();
                foreach (var weapon in currentWeapons)
                {
                    // Skip empty hardpoints and fuel tanks; Increment ignores unnamed ammo.
                    var ap = weapon?._ap;
                    if (ap == null || ap._subType == Ammunition.Type.Fueltank)
                    {
                        continue;
                    }

                    currentCounts.Increment(ap._ammunitionFileName);
                }

                // Log the comparison
                PlayerUtils.LogIfSpam($"[Trigger] Original types: {string.Join(", ", originalCounts.Select(kv => $"{kv.Key}={kv.Value}"))}");
                PlayerUtils.LogIfSpam($"[Trigger] Current types: {string.Join(", ", currentCounts.Select(kv => $"{kv.Key}={kv.Value}"))}");

                // Check if any originally-loaded type is now exhausted
                bool shouldRearm = false;
                string? triggeringAmmo = null;

                foreach (var kvp in originalCounts)
                {
                    string ammoType = kvp.Key;
                    int originalCount = kvp.Value;
                    int currentCount = currentCounts.ContainsKey(ammoType) ? currentCounts[ammoType] : 0;

                    if (currentCount == 0 && originalCount > 0)
                    {
                        shouldRearm = true;
                        triggeringAmmo = ammoType;
                        PlayerUtils.LogIfSpam($"[Trigger] Ammo type '{ammoType}' exhausted (was {originalCount}, now {currentCount})");
                        break;
                    }
                }

                // Also trigger if hardpoint is marked empty (fallback)
                if (!shouldRearm && __instance._isEmpty && originalCounts.Count > 0)
                {
                    shouldRearm = true;
                    triggeringAmmo = "hardpoint empty";
                    PlayerUtils.LogIfSpam("[Trigger] Hardpoint marked empty, triggering rearm");
                }

                if (shouldRearm)
                {
                    PlayerUtils.LogIfSpam($"[Trigger] Rearm scheduled. Triggering ammo='{triggeringAmmo ?? "unknown"}'");
                    AircraftPatches.RearmHardpoint(__instance);
                }
                else
                {
                    PlayerUtils.LogIfSpam("[Trigger] No rearm needed yet");
                }
            }
            catch (Exception ex)
            {
                PlayerUtils.LogIfSpam($"[Trigger] ERROR in postfix: {ex}");
            }
        }
    }

    /// <summary>
    /// Replenishes flight deck ammunition pools when aircraft launch.
    /// </summary>
    [HarmonyPatch(typeof(FlightDeck), "createLaunchTask")]
    internal static class FlightDeckInfiniteAmmo
    {
        /// <summary>
        /// Postfix that refills flight deck ammunition after launch task creation.
        /// </summary>
        [HarmonyPostfix]
        [UsedImplicitly]
        public static void Postfix(FlightDeck __instance)
        {
            if (__instance._baseObject._taskforce.Side != Taskforce.TfType.Player ||
                !CheatConfig.AircraftInfiniteAmmo.Value)
            {
                return;
            }

            // Replenish general ammo pool
            __instance._currentAmmo = __instance._ammoCapacity;

            // Replenish specific accountable ammunition
            List<string> ammoKeys = new List<string>(__instance._accountableAmmunition.Keys);

            foreach (string key in ammoKeys)
            {
                if (__instance._accountableAmmunitionCapacity.ContainsKey(key))
                {
                    __instance._accountableAmmunition[key] = __instance._accountableAmmunitionCapacity[key];
                }
            }
        }
    }

    /// <summary>
    /// Sets flight deck to have infinite aircraft slots.
    /// </summary>
    [HarmonyPatch(typeof(FlightDeck), "LoadFromInI")]
    internal static class FlightDeckInfiniteSlots
    {
        /// <summary>
        /// Postfix that maximizes flight deck capacity for player units.
        /// </summary>
        [HarmonyPostfix]
        [UsedImplicitly]
        public static void Postfix(FlightDeck __instance)
        {
            if (__instance._baseObject._taskforce.Side == Taskforce.TfType.Player &&
                CheatConfig.FlightDeckInfiniteSlots.Value)
            {
                __instance.MaxAircraftOnBoard = int.MaxValue;
                __instance.GroundCrewCount = int.MaxValue;
            }
        }
    }

    // Note: Unlimited fuel patches removed - now handled centrally by CheatApplicator
    // and SideSwapPatches for all unit types.
}
