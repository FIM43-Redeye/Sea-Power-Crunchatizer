// using System;
// using System.Collections.Generic;
// using System.Linq;

using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SeaPower;
using UnityEngine;

// Keep if needed by any indirect dependencies, though not used directly here
// BepInEx Core
// BepInEx Logging
// BepInEx Configuration
// Keep if you use Rider/ReSharper features

// ReSharper disable InconsistentNaming

namespace Sea_Power_Crunchatizer
{
    // --- BepInEx Plugin Metadata ---
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Sea Power.exe")] // Adjust "Sea Power.exe" if the executable name differs
    public class CrunchatizerCore : BaseUnityPlugin // Inherit from BaseUnityPlugin
    {
        // --- Plugin Constants ---
        public static class PluginInfo
        {
            public const string PLUGIN_GUID = "net.particle.sea_power_crunchatizer";
            public const string PLUGIN_NAME = "Sea Power Crunchatizer";
            public const string PLUGIN_VERSION = "2.0.0"; // PORTED BABY, HELL YEAH
        }

        // --- Logger Instance ---
        // Use this for logging throughout the mod
        internal static ManualLogSource Log = null!;

        // --- Configuration Entries (using BepInEx's ConfigEntry) ---
        public static ConfigEntry<bool> LogSpam = null!;
        public static ConfigEntry<bool> UnlimitedRepair = null!;
        public static ConfigEntry<bool> BottomlessMags = null!;
        public static ConfigEntry<bool> ContainerAutoRefresh = null!;
        public static ConfigEntry<bool> AircraftInfiniteAmmo = null!; // Note: Still unused in patches
        public static ConfigEntry<bool> ForceTerrainFollowing = null!;
        public static ConfigEntry<bool> EnhanceMissileFeatures = null!;
        public static ConfigEntry<bool> BrokenFireControl = null!;
        public static ConfigEntry<bool> BrokenSensorParams = null!;
        public static ConfigEntry<bool> UltraCrew = null!;
        public static ConfigEntry<bool> AircraftInfRange = null!;
        public static ConfigEntry<int> FireRateMult = null!;
        public static ConfigEntry<int> ReactionTimeDiv = null!;
        public static ConfigEntry<int> TargetAcqTimeDiv = null!;
        public static ConfigEntry<int> PreLaunchDelayDiv = null!;
        public static ConfigEntry<int> MagReloadTimeDiv = null!;
        public static ConfigEntry<int> TraverseSpeedMult = null!;
        public static ConfigEntry<int> WeaponRangeMult = null!;

        // --- BepInEx Entry Point ---
        private void Awake()
        {
            // 1. Assign Logger
            Log = Logger; // Use the logger provided by BaseUnityPlugin
            Log.LogInfo($"Loading {PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION}...");

            // 2. Load Configuration
            SetupConfiguration();
            Log.LogInfo("Configuration loaded.");

            // 3. Apply Harmony Patches
            try
            {
                // Use the static helper to patch all annotated methods in this assembly
                Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginInfo.PLUGIN_GUID);
                Log.LogInfo("Harmony patches applied successfully.");
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to apply Harmony patches: {ex}");
            }

            Log.LogInfo($"{PluginInfo.PLUGIN_NAME} loaded successfully!");
        }

        // --- Configuration Setup Method ---
        private void SetupConfiguration()
        {
            // Define configuration sections for organization
            const string generalSection = "1. General Cheats";
            const string weaponSection = "2. Weapon Modifiers";
            const string aircraftSection = "3. Aircraft Modifiers";
            const string miscSection = "4. Miscellaneous";

            // Bind configuration entries using BepInEx's ConfigFile instance (inherited 'Config')
            // Format: Config.Bind<Type>(Section, Key, DefaultValue, Description)

            LogSpam = Config.Bind(miscSection, "Enable Debug Logging", false, // Defaulting spam to false now
                "Enables potentially excessive logging messages for debugging purposes.");

            UnlimitedRepair = Config.Bind(generalSection, "Unlimited Repair", true,
                "Player units can repair systems/compartments indefinitely, ignoring max repair limits and damage types.");

            BottomlessMags = Config.Bind(generalSection, "Bottomless Magazines", true,
                "Player weapon magazines (WeaponMagazineSystem) never deplete. Returning ammo does nothing.");

            ContainerAutoRefresh = Config.Bind(generalSection, "Container Auto-Refresh", true,
                "For player weapon systems with no magazine (typically launchers like WeaponContainer), instantly refills ammo when empty.");

            AircraftInfiniteAmmo = Config.Bind(aircraftSection, "Aircraft Infinite Ammo", true,
                "Aircraft will instantly refill hardpoints when they're out of ammo."); // Finally implemented
            
            AircraftInfRange = Config.Bind(aircraftSection, "Infinite Aircraft Range", true, "Planes that go forever.");

            ForceTerrainFollowing = Config.Bind(weaponSection, "Force Terrain Following", true,
                "ALL missile/torpedo/rocket weapons assigned to the player's taskforce become terrain/sea-bed following (where applicable).");

            EnhanceMissileFeatures = Config.Bind(weaponSection, "Enhance Missile Features", true,
                "ALL missile/torpedo/rocket weapons assigned to the player's taskforce receive various guidance, targeting, and performance improvements.");

            BrokenFireControl = Config.Bind(generalSection, "Broken Fire Control Sensors", true,
                "FCRs and whatnot have... infinite guidance and target channels? Sweet.");

            BrokenSensorParams = Config.Bind(generalSection, "Broken Sensor Parameters", true,
                "Makes all the sensors wacky and fun.");

            UltraCrew = Config.Bind(generalSection, "Ultra Crew", true, "Makes all crew for player units ultra.");

            // --- Multipliers/Divisors ---
            // Consider adding AcceptableValueRange using ConfigDescription for these
            FireRateMult = Config.Bind(weaponSection, "Fire Rate Multiplier", 1,
                new ConfigDescription(
                    "Multiplier for fire rate (higher = faster). Affects RoF, various delays. Min=0, sets to infinite, other values multiply",
                    new AcceptableValueRange<int>(0, 100))); // Example range

            ReactionTimeDiv = Config.Bind(weaponSection, "Reaction Time Divisor", 1,
                new ConfigDescription(
                    "Divisor for weapon reaction time (higher = faster reaction). Min=0, sets to infinite, other values multiply",
                    new AcceptableValueRange<int>(0, 100)));

            TargetAcqTimeDiv = Config.Bind(weaponSection, "Target Acquisition Time Divisor", 1,
                new ConfigDescription(
                    "Divisor for weapon target acquisition time (higher = faster acquisition). Min=0, sets to infinite, other values multiply",
                    new AcceptableValueRange<int>(0, 100)));

            PreLaunchDelayDiv = Config.Bind(weaponSection, "Pre-Launch Delay Divisor", 1,
                new ConfigDescription(
                    "Divisor for weapon pre-launch delay (higher = faster launch). Min=0, sets to infinite, other values multiply",
                    new AcceptableValueRange<int>(0, 100)));

            MagReloadTimeDiv = Config.Bind(weaponSection, "Magazine Reload Time Divisor", 1,
                new ConfigDescription(
                    "Divisor for weapon magazine reload time (higher = faster reload). Min=0, sets to infinite, other values multiply",
                    new AcceptableValueRange<int>(0, 100)));

            TraverseSpeedMult = Config.Bind(weaponSection, "Traverse Speed Multiplier", 1,
                new ConfigDescription("Multiplier for weapon traverse speed (higher = faster traverse). Min=1",
                    new AcceptableValueRange<int>(1, 100)));

            WeaponRangeMult = Config.Bind(weaponSection, "Weapon Range Multiplier", 1,
                new ConfigDescription(
                    "Multiplier for weapon range (applied as sqrt to range values like max range, lifetime). Min=1",
                    new AcceptableValueRange<int>(1, 100)));
            

            // Example of reacting to a config change (optional)
            FireRateMult.SettingChanged += (sender, args) =>
            {
                Log.LogInfo($"Config changed: Fire Rate Multiplier set to {FireRateMult.Value}");
                // You might want to re-apply settings dynamically here if feasible,
                // but often patches applied at startup are sufficient.
            };
        }

        // --- Utility Function (using BepInEx Logger) ---
        // ReSharper disable once UnusedMember.Global
        public static void PrintObjectFields(object obj)
        {
            if (obj == null)
            {
                Log.LogWarning("PrintObjectFields requested for a null object.");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"--- Fields of object type {obj.GetType().FullName} ---"); // Use FullName for clarity

            try
            {
                var fields = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic |
                                                     BindingFlags.Public |
                                                     BindingFlags.Static); // Include static if needed

                if (!fields.Any())
                    sb.AppendLine("(No fields found)");
                else
                    foreach (var field in fields)
                        try
                        {
                            var value = field.GetValue(obj);
                            var valueString = value?.ToString() ?? "<null>";
                            var staticMarker = field.IsStatic ? "[Static] " : "";
                            sb.AppendLine($"{staticMarker}{field.FieldType.Name} {field.Name} = {valueString}");
                        }
                        catch (Exception ex)
                        {
                            sb.AppendLine($"{field.Name}: Error getting value - {ex.Message}");
                        }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Error retrieving fields: {ex.Message}");
            }

            sb.AppendLine("--- End of fields ---");
            Log.LogInfo(sb.ToString()); // Log as Debug level
        }
    }


    // =========================================================================
    // ========================== HARMONY PATCHES ============================
    // =========================================================================
    // Notes:
    // - Replaced MelonLogger with CrunchatizerCore.Log
    // - Wrapped spammy logs with `if (CrunchatizerCore.LogSpam.Value)`
    // - Added null checks `?.` for safety where appropriate
    // - Removed unnecessary `ref` keywords from prefix parameters (e.g., __instance)
    // - Accessing config values (`CrunchatizerCore.ConfigEntry.Value`) remains the same syntactically.
    // =========================================================================

    [HarmonyPatch(typeof(Compartments))]
    [HarmonyPatch("CheckDCTeamsAllocation")]
    public static class Compartments_CheckDCTeamsAllocation_RerunPatch
    {
        // Using a prefix to completely replace the method for the player.
        [HarmonyPrefix]
        public static bool Prefix(Compartments __instance)
        {
            // 1. Condition Check: Only run this custom logic for the player.
            // If not the player, return true to run the original game method.
            if (!CrunchatizerCore.UnlimitedRepair.Value ||
                __instance?._baseObject?._taskforce?.Side != Taskforce.TfType.Player)
            {
                return true;
            }

            // --- Start of Replicated Original Method Logic ---
            // All 'this' is replaced with '__instance'.

            // Early exit conditions from the original method.
            if (__instance._isSinking)
            {
                return false; // Skips original, does nothing.
            }

            // Defensive check for the array, then the original logic.
            if (__instance.DamageControlTeamsNumbers == null || __instance.DamageControlTeamsNumbers.Length <= 5 ||
                __instance.DamageControlTeamsNumbers[5] < 1)
            {
                return false; // Skips original, no teams available.
            }

            // Defensive null checks for compartment arrays. Good practice!
            if (__instance._portCompartments == null || __instance._starboardCompartments == null ||
                __instance._systemCompartments == null)
            {
                // Log error if you have a logger.
                // CrunchatizerCore.Log.LogError("Compartment arrays are null in CheckDCTeamsAllocation patch!");
                return false; // Can't proceed, skip original.
            }

            int num;
            float num2;

            // --- Priority 1: Highest Flooding Rate ---
            num = -1;
            num2 = 0f;
            for (int i = 0; i < __instance._compartmentsCount; i++)
            {
                // Optional: Add bounds checks if _compartmentsCount can be unreliable.
                if (i >= __instance._portCompartments.Length || i >= __instance._starboardCompartments.Length) continue;

                float num3 =
                    (__instance._portCompartments[i]._floodingRate > __instance._starboardCompartments[i]._floodingRate)
                        ? __instance._portCompartments[i]._floodingRate
                        : __instance._starboardCompartments[i]._floodingRate;
                if (num2 < num3)
                {
                    num2 = num3;
                    num = i;
                }
            }

            if (num > -1)
            {
                __instance.SendDCTeamToCompartment(num);
                // CRITICAL: We found the highest priority task. Stop here and skip the original method.
                return false;
            }

            // --- Priority 2: Highest Fire Severity ---
            num = -1;
            num2 = 0f;
            for (int j = 0; j < __instance._compartmentsCount; j++)
            {
                if (j >= __instance._systemCompartments.Length) continue;

                float num3 = __instance._systemCompartments[j].FireSeverity;
                if (num2 < num3)
                {
                    num2 = num3;
                    num = j;
                }
            }

            if (num > -1)
            {
                __instance.SendDCTeamToCompartment(num);
                return false; // Stop and skip original.
            }

            // --- Priority 3: Highest Fire Grow Rate ---
            num = -1;
            num2 = 0f;
            for (int k = 0; k < __instance._compartmentsCount; k++)
            {
                if (k >= __instance._systemCompartments.Length) continue;

                float num3 = __instance._systemCompartments[k]._fireGrowRate;
                if (num2 < num3)
                {
                    num2 = num3;
                    num = k;
                }
            }

            if (num > -1)
            {
                __instance.SendDCTeamToCompartment(num);
                return false; // Stop and skip original.
            }

            // ... repeat this pattern for FloodingDelta and IntegrityDelta ...

            // --- Priority 6: Most Damaged Repairable System ---
            num = -1;
            num2 = 100f; // Finding the minimum value, so start high.
            for (int n = 0; n < __instance._compartmentsCount; n++)
            {
                if (n >= __instance._systemCompartments.Length ||
                    __instance._systemCompartments[n]?._listOfSystems == null) continue;

                foreach (BaseSystem baseSystem in __instance._systemCompartments[n]._listOfSystems)
                {
                    if (baseSystem == null) continue;

                    // **UPDATED LOGIC**: Use the complete, up-to-date conditions from the original method.
                    if (baseSystem.Repairable)
                    {
                        float num3 = baseSystem.CurrentIntegrityPercent;
                        if (num2 > num3) // We are looking for the LOWEST integrity here.
                        {
                            num2 = num3;
                            num = n;
                        }
                    }
                }
            }

            if (num > -1)
            {
                __instance.SendDCTeamToCompartment(num);
                return false; // Stop and skip original.
            }

            // If we get here, no work was found. Skip the original method.
            return false;
        }
    }

    [HarmonyPatch(typeof(Compartments))]
    [HarmonyPatch("RepairSystems", new Type[] { typeof(int) })] // Explicitly define parameter types for robustness
    public static class Compartments_RepairSystems_PlayerCheatPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Compartments __instance, int index, ref bool __result)
        {
            // 1. Run original method if the cheat is off OR if the object is not a player vessel.
            // Using null-conditional (?.) and null-coalescing (??) operators for safety and conciseness.
            bool isPlayer = __instance?._baseObject?._taskforce?.Side == Taskforce.TfType.Player;
            if (!CrunchatizerCore.UnlimitedRepair.Value || !isPlayer || __instance._baseObject == null)
            {
                return true; // Proceed to original method
            }

            // --- CHEAT LOGIC ACTIVE ---

            // Default to no repair having happened yet.
            __result = false;

            var compartment = __instance._systemCompartments[index];
            if (compartment == null)
            {
                return false; // Skip original method.
            }

            // 2. Check and attempt to repair the prioritized system.
            var prioritizedSystem = compartment.SystemPrioritisedForRepair;
            if (prioritizedSystem != null)
            {
                // The point of the cheat is to repair anything, regardless of its state.
                // We only check if it's already fully repaired.
                if (prioritizedSystem.CurrentIntegrity < prioritizedSystem._maxIntegrity)
                {
                    if (__instance._baseObject == null) return false;
                    if (__instance._baseObject?._crew == null) return false;
                    if (__instance._baseObject?._crew == null) return false;
                    // Calculate repair amount once.
                    float repairAmount = (float)__instance.DamageControlTeamsNumbers[index] *
                                         __instance._baseObject._crew._systemRepair * GameTime.fixedDeltaTime;

                    if (repairAmount > 0f)
                    {
                        prioritizedSystem.CurrentIntegrity += repairAmount;
                        // Clamp integrity to max value.
                        if (prioritizedSystem.CurrentIntegrity > prioritizedSystem._maxIntegrity)
                        {
                            prioritizedSystem.CurrentIntegrity = prioritizedSystem._maxIntegrity;
                        }

                        // A key part of the original logic: make the system operable once repairs start.
                        // We check for 'Destroyed' as some systems may have special logic for that state.
                        if (prioritizedSystem.CurrentDamageType != DamageValues.DamageType.Destroyed)
                        {
                            prioritizedSystem.Inoperable.Value = false;
                        }

                        __result = true; // A repair was made.
                        return false; // Skip original method, our work here is done for this frame.
                    }
                }

                // If the prioritized system is fully repaired or cannot be repaired (no crew/rate),
                // clear the priority so other systems can be repaired in the future. This is critical.
                compartment.SystemPrioritisedForRepair = null;
            }

            // 3. If no prioritized system was repaired, iterate through all systems in the compartment.
            // This block only runs if __result is still false.
            if (compartment._listOfSystems != null)
            {
                foreach (BaseSystem system in compartment._listOfSystems)
                {
                    // Cheat logic: ignore 'Repairable' and other checks. Just see if it needs health.
                    if (system != null && system.CurrentIntegrity < system._maxIntegrity)
                    {
                        if (__instance._baseObject?._crew == null) return false;
                        float repairAmount = (float)__instance.DamageControlTeamsNumbers[index] *
                                             __instance._baseObject._crew._systemRepair * GameTime.fixedDeltaTime;

                        if (repairAmount > 0f)
                        {
                            system.CurrentIntegrity += repairAmount;
                            if (system.CurrentIntegrity > system._maxIntegrity)
                            {
                                system.CurrentIntegrity = system._maxIntegrity;
                            }

                            // Also make this system operable.
                            if (system.CurrentDamageType != DamageValues.DamageType.Destroyed)
                            {
                                system.Inoperable.Value = false;
                            }

                            __result = true; // A repair was made.
                            return false; // Skip original, only repair one system per call.
                        }
                    }
                    // The original function also sets `Inoperable.Value = false` for systems that fail the damage type check.
                    // Our cheat bypasses that check, so we make systems operable AS we repair them, which is a cleaner implementation for a cheat.
                }
            }

            // 4. If we reach here, no repair was possible this frame. Skip the original method.
            return false;
        }
    }


    [HarmonyPatch(typeof(SystemCompartment))]
    [HarmonyPatch("SystemPrioritisedForRepair", MethodType.Setter)]
    public static class SystemCompartment_SystemPrioritisedForRepair_Setter_LogPatch
    {
        public static void Postfix(SystemCompartment __instance, BaseSystem value) // `value` is the system being set
        {
            if (!CrunchatizerCore.LogSpam.Value) return; // Check spam flag

            try
            {
                var sb = new StringBuilder();
                var objName = __instance?._object?.name ?? "Unknown Object";
                sb.Append($"[PriorityPatch] Setter called on SystemCompartment of '{objName}'.");

                var incomingValueStr = value == null
                    ? "null"
                    : $"'{value._systemName ?? value.GetType().Name}' (Repairable={value?.Repairable}, Destroyed={value?.IsDestroyed})";
                sb.Append($" Incoming value: {incomingValueStr}.");

                // Access the actual value *after* setter using Traverse (Harmony utility) or reflection
                var currentPriority =
                    Traverse.Create(__instance).Field<BaseSystem>("_systemPrioritisedForRepair").Value;
                var resultingValueStr = currentPriority == null
                    ? "null"
                    : $"'{currentPriority._systemName ?? currentPriority.GetType().Name}' (IsPriority={currentPriority?.damageSummary?.IsPriority ?? false})";
                sb.Append($" Resulting _systemPrioritisedForRepair: {resultingValueStr}.");

                CrunchatizerCore.Log.LogInfo(sb.ToString());
            }
            catch (Exception ex)
            {
                CrunchatizerCore.Log.LogError(
                    $"[PriorityPatch] Error in SystemPrioritisedForRepair setter Postfix: {ex}");
            }
        }
    }


    [HarmonyPatch(typeof(BaseSystem), "init")]
    public static class UnlimitedRepair_PatchBaseSystem
    {
        public static void Postfix(BaseSystem __instance)
        {
            // Check config first, then conditions with null safety
            if (CrunchatizerCore.UnlimitedRepair.Value &&
                __instance?._baseObject?._taskforce?.Side == Taskforce.TfType.Player &&
                __instance._baseObject._type !=
                ObjectBase.ObjectType.Aircraft) // Aircraft handle repair differently (if at all)
            {
                // Apply the cheat
                __instance._alwaysRepairable = true;

                if (CrunchatizerCore.LogSpam.Value)
                {
                    var sysName = __instance._systemName ?? __instance.GetType().Name;
                    var objName = __instance._baseObject.name ?? "Unknown Object";
                    CrunchatizerCore.Log.LogInfo(
                        $"UnlimitedRepair: Set _alwaysRepairable=true for '{sysName}' on '{objName}'");
                }
            }
        }
    }


    [HarmonyPatch(typeof(FloodingCompartment), "get_MaxAllowedIntegrity")]
    public static class UnlimitedRepair_FloodingCompartment_MaxAllowedIntegrityPatch
    {
        // Prefix to potentially override the return value (__result) and skip original (return false)
        public static bool Prefix(FloodingCompartment __instance, ref float __result)
        {
            try
            {
                // Check config first, then conditions with null safety
                if (CrunchatizerCore.UnlimitedRepair.Value &&
                    __instance?._compartments?._baseObject?._taskforce?.Side == Taskforce.TfType.Player &&
                    (__instance._compartments._baseObject._type == ObjectBase.ObjectType.Vessel ||
                     __instance._compartments._baseObject._type == ObjectBase.ObjectType.Submarine))
                {
                    // Cheat active: Force result to be the absolute maximum integrity
                    __result = __instance._maxIntegrity;

                    // The lag for the following code is so bad that it is COMMENTED OUT BY DEFAULT
                    // if (CrunchatizerCore.LogSpam.Value) CrunchatizerCore.Log.LogInfo($"UnlimitedRepair: Forced MaxAllowedIntegrity to {__result} for flooding compartment on '{__instance._compartments._baseObject.name}'");

                    return false; // Skip the original getter logic
                }
            }
            catch (Exception ex)
            {
                CrunchatizerCore.Log.LogError($"Error in UnlimitedRepair_MaxAllowedIntegrityPatch Prefix: {ex}");
                return true; // Let original run if error occurs
            }

            // Cheat off or not applicable: Let the original getter logic run
            return true;
        }
    }

    [HarmonyPatch]
    public static class BottomlessMagsPatch
    {
        [HarmonyPatch(typeof(WeaponMagazineSystem), "decreaseAmmunitionCount", typeof(string), typeof(bool))]
        [HarmonyPrefix]
        private static bool Prefix(WeaponMagazineSystem __instance)
        {
            if (!CrunchatizerCore.BottomlessMags.Value) return true;

            if (__instance?._baseObject?._taskforce?.Side == Taskforce.TfType.Player)
            {
                if (CrunchatizerCore.LogSpam.Value)
                    CrunchatizerCore.Log.LogInfo(
                        $"BottomlessMags: Preventing ammo decrease for player weapon '{__instance?._moduleName ?? "Unknown"}'.");
                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(WeaponMagazineSystem), "decreaseAmmunitionCount", typeof(string), typeof(int), typeof(bool))]
        [HarmonyPrefix]
        private static bool Prefix2(WeaponMagazineSystem __instance)
        {
            if (!CrunchatizerCore.BottomlessMags.Value) return true;

            if (__instance?._baseObject?._taskforce?.Side == Taskforce.TfType.Player)
            {
                if (CrunchatizerCore.LogSpam.Value)
                    CrunchatizerCore.Log.LogInfo(
                        $"BottomlessMags: Preventing ammo decrease for player weapon '{__instance?._moduleName ?? "Unknown"}'.");
                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(WeaponSystem), "decreaseMagazineAmmoCount", typeof(string), typeof(int), typeof(bool))]
        [HarmonyPrefix]
        private static bool Prefix(WeaponSystem __instance)
        {
            if (!CrunchatizerCore.BottomlessMags.Value) return true;

            if (__instance?._baseObject?._taskforce?.Side == Taskforce.TfType.Player)
            {
                if (CrunchatizerCore.LogSpam.Value)
                    CrunchatizerCore.Log.LogInfo(
                        $"BottomlessMags: Preventing magazine decrease for player weapon system '{__instance?._name ?? "Unknown"}'.");
                return false;
            }

            return true;
        }
    }


    // Prevent adding ammo back to prevent inflation with BottomlessMags
    [HarmonyPatch(typeof(WeaponMagazineSystem), "increaseAmmunitionCount", typeof(string))]
    public static class BottomlessMagsIncrement
    {
        [HarmonyPrefix]
        private static bool Prefix(WeaponMagazineSystem __instance) // No ref needed
        {
            if (!CrunchatizerCore.BottomlessMags.Value) return true; // Cheat off

            if (__instance?._baseObject?._taskforce?.Side == Taskforce.TfType.Player)
            {
                if (CrunchatizerCore.LogSpam.Value)
                    CrunchatizerCore.Log.LogInfo(
                        $"BottomlessMags: Preventing ammo increase for player weapon '{__instance?._moduleName ?? "Unknown"}'.");
                return false; // Skip original increment for player
            }

            return true; // Not player, allow original
        }
    }

    // Container auto-refresh code. Finally works to my satisfaction.
    [HarmonyPatch(typeof(WeaponContainer), "launch")]
    public static class ContainerAutoRefresh
    {
        private static void Postfix(ref WeaponContainer __instance)
        {
            switch (CrunchatizerCore.ContainerAutoRefresh.Value)
            {
                // If the setting's off, don't do anything!
                case false:
                    if (CrunchatizerCore.LogSpam.Value)
                        CrunchatizerCore.Log.LogInfo("Not doing anything, setting is off for container replen");
                    return;
            }

            if (__instance._weaponSystem._baseObject._taskforce.Side != Taskforce.TfType.Player) return;

            var weaponsToInit = new List<WeaponSystem>();
            foreach (var ammoPair in __instance._weaponSystem._baseObject.AmmunitionAmountDictionary.Where(ammoPair =>
                         ammoPair.Value == 0))
            {
                if (CrunchatizerCore.LogSpam.Value)
                    CrunchatizerCore.Log.LogInfo(ammoPair.Key + " has ammunition number " + ammoPair.Value);

                foreach (var weaponTarget in __instance._weaponSystem._baseObject.GetWeaponSystemsForAmmunition(
                             ammoPair.Key))
                {
                    weaponsToInit.Add(weaponTarget);
                    if (CrunchatizerCore.LogSpam.Value)
                        CrunchatizerCore.Log.LogInfo("Adding " + weaponTarget._name + " to the list");
                }
            }

            foreach (var current in weaponsToInit)
            {
                current.init();
                if (CrunchatizerCore.LogSpam.Value) CrunchatizerCore.Log.LogInfo("Initialized " + current._name);
            }
        }
    }

    // BEGIN AIRCRAFT REARM CODE!!!
    // THIS TOOK FOREVER!!!
    // STILL NEEDS WORK!!!
    // THIS BLOCK IS TO GET MY ATTENTION!!!
    public class AutoRearmController : MonoBehaviour
    {
        private static AutoRearmController? _instance;

        public static AutoRearmController Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Create a new GameObject and add this component to it.
                    // This ensures the controller exists in the scene to run coroutines.
                    _instance = new GameObject("AutoRearmController").AddComponent<AutoRearmController>();
                    DontDestroyOnLoad(_instance.gameObject);
                }

                return _instance;
            }
        }

        public void ScheduleRearm(WeaponSystemHardpoint hardpoint)
        {
            StartCoroutine(RearmCoroutine(hardpoint));
        }

        private IEnumerator RearmCoroutine(WeaponSystemHardpoint hardpoint)
        {
            // Wait for the next game update cycle. This is a safer way to ensure the game
            // has finished all its logic for the current frame before we intervene.
            yield return null;

            // Now that it's safe, execute the rearm logic.
            AutoRearmPatch.RearmHardpoint(hardpoint);
        }
    }

    static class AmmoHeuristics
    {
        private static readonly string[] TankTokens =
        {
            "fueltank", "fuel_tank", "fuel-tank", "fuel tank",
            "droptank", "drop_tank", "drop-tank", "drop tank",
            "tank" // keep last, catch-all
        };

        public static bool LooksLikeFuelTank(string ammoName)
        {
            if (string.IsNullOrEmpty(ammoName)) return false;
            var s = ammoName.Trim().ToLowerInvariant();

            // Fast path exact/obvious matches
            foreach (var token in TankTokens)
            {
                if (s.Contains(token))
                    return true;
            }

            // Optional: stricter “tank” word boundary check to reduce false positives
            // e.g., match "tank" when adjacent chars are non-letters/digits or string edges.
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] != 't') continue;
                if (i + 3 >= s.Length) continue; // need room for "tank"
                if (s[i + 1] != 'a' || s[i + 2] != 'n' || s[i + 3] != 'k') continue;

                bool leftOk = i == 0 || !char.IsLetterOrDigit(s[i - 1]);
                bool rightOk = i + 4 >= s.Length || !char.IsLetterOrDigit(s[i + 4]);
                if (leftOk && rightOk) return true;
            }

            return false;
        }
    }

    [HarmonyPatch]
    public static class AutoRearmPatch
    {
        // --- CACHE FIELDS START ---
        // We will store the results of our expensive lookups here.
        private static readonly FieldInfo _weaponsField;
        private static readonly FieldInfo _loadedAmmunitionField;
        private static readonly FieldInfo _loadedAmmunitionCountField;
        private static readonly FieldInfo _baseObjectField;
        private static readonly FieldInfo _systemNameField;
        private static readonly MethodInfo _createAmmunitionObjectInstanceMethod;
        // --- CACHE FIELDS END ---

        #region State Flag and Helper Class

        [ThreadStatic] private static bool _isRearming;
        [ThreadStatic] public static WeaponSystemHardpoint? _justRearmedHardpoint;

        public class StationLoadoutInfo
        {
            public string? AmmoFileName;
            public Vector3 LocalSpawnPosition;
            public Quaternion LocalStationRotation;
            public GameObject? StationParent;
            public ObjectBaseParameters? HostObjectParameters;
        }

        #endregion

        #region Static Constructor for Caching

        static AutoRearmPatch()
        {
            // This code runs only ONCE when the mod loads.
            // It finds all the methods/fields we need and saves them to our cache fields.
            CrunchatizerCore.Log.LogInfo("[AutoRearm] Caching reflection data...");

            _weaponsField = AccessTools.Field(typeof(WeaponSystemHardpoint), "_weapons");
            _loadedAmmunitionField = AccessTools.Field(typeof(WeaponSystemHardpoint), "_loadedAmmunition");
            _loadedAmmunitionCountField = AccessTools.Field(typeof(WeaponSystem), "_loadedAmmunitionCount");
            _baseObjectField = AccessTools.Field(typeof(WeaponSystem), "_baseObject");
            _systemNameField = AccessTools.Field(typeof(WeaponSystem), "_systemName");

            _createAmmunitionObjectInstanceMethod =
                AccessTools.Method(typeof(WeaponSystemHardpoint), "createAmmunitionObjectInstance");

            CrunchatizerCore.Log.LogInfo("[AutoRearm] Caching complete.");
        }

        #endregion

        #region Core Rearm Logic

        public static void RearmHardpoint(WeaponSystemHardpoint hardpoint)
        {
            if (CrunchatizerCore.LogSpam.Value) CrunchatizerCore.Log.LogInfo("Rearm func called");
            _isRearming = true;

            // Precompute identifiers for consistent logging
            string aircraftId = "null";
            string systemName = "null";
            ObjectBase? baseObject = null;

            try
            {
                var loadoutData = hardpoint._baseObject.GetComponent<HardpointLoadoutData>();
                if (loadoutData == null)
                {
                    if (CrunchatizerCore.LogSpam.Value)
                        CrunchatizerCore.Log.LogWarning($"[Rearm] No HardpointLoadoutData found for hardpoint {hardpoint._systemName}. Cannot rearm.");
                    return;
                }

                var weaponsList = (List<WeaponBase>?)_weaponsField.GetValue(hardpoint);
                var loadedAmmunition = (Dictionary<Ammunition, int>?)_loadedAmmunitionField.GetValue(hardpoint);
                var loadedAmmunitionCount = (Dictionary<string, int>?)_loadedAmmunitionCountField.GetValue(hardpoint);
                baseObject = (ObjectBase?)_baseObjectField.GetValue(hardpoint);
                systemName = (string?)_systemNameField.GetValue(hardpoint);

                if (baseObject != null)
                    aircraftId = baseObject.getUIDAndName();

                // --- NEW LOGIC TO GET SWING WING DATA ---
                AircraftParameters? aircraftParameters = null;
                if (baseObject is Aircraft aircraft) // Use pattern matching
                {
                    // We need the animation controller to find the pylon lists
                    var animController = aircraft.AircraftAnimation;
                    if (animController != null)
                    {
                        aircraftParameters = (AircraftParameters?)AccessTools
                            .Field(typeof(AircraftAnimation), "_aircraftParameters").GetValue(animController);
                    }
                }

                // Snapshot current state for debugging
                if (CrunchatizerCore.LogSpam.Value)
                    CrunchatizerCore.Log.LogInfo($"[Rearm] Begin rearm: {aircraftId} hardpoint {systemName}");
                if (loadedAmmunitionCount != null)
                {
                    foreach (var kvp in loadedAmmunitionCount)
                        if (CrunchatizerCore.LogSpam.Value)
                            CrunchatizerCore.Log.LogInfo($"[Rearm] Pre-clear ammo '{kvp.Key}' = {kvp.Value}");
                }
                else
                {
                    if (CrunchatizerCore.LogSpam.Value)
                        CrunchatizerCore.Log.LogInfo("[Rearm] Pre-clear ammo dictionary is null");
                }

                if (weaponsList != null)
                {
                    if (CrunchatizerCore.LogSpam.Value)
                        CrunchatizerCore.Log.LogInfo($"[Rearm] Weapons to destroy: {weaponsList.Count}");
                    if (loadedAmmunition != null)
                        if (CrunchatizerCore.LogSpam.Value)
                            CrunchatizerCore.Log.LogInfo($"[Rearm] LoadedAmmunition entries: {loadedAmmunition.Count}");

                    // 1. Destroy the old weapon objects.
                    foreach (WeaponBase weapon in new List<WeaponBase>(weaponsList))
                    {
                        if (weapon)
                        {
                            string? file = weapon._ap?._ammunitionFileName ?? "null";
                            if (CrunchatizerCore.LogSpam.Value)
                                CrunchatizerCore.Log.LogInfo(
                                    $"[Rearm] Freeing weapon object: file={file}, go={weapon.gameObject?.name ?? "null"}");
                            Singleton<PoolManager>.Instance.freeObject(file, weapon.gameObject, weapon);
                        }
                    }

                    weaponsList.Clear();
                    if (loadedAmmunition != null) loadedAmmunition.Clear();
                }

                // 2. Reset ammunition counts (decrement from baseObject)
                if (loadedAmmunitionCount != null && baseObject != null)
                {
                    foreach (var ammoCount in new Dictionary<string, int>(loadedAmmunitionCount))
                    {
                        if (CrunchatizerCore.LogSpam.Value)
                            CrunchatizerCore.Log.LogInfo(
                                $"[Rearm] Decrement base ammo '{ammoCount.Key}' by {ammoCount.Value}");
                        baseObject.changeAmmunitionAmount(ammoCount.Key, -ammoCount.Value);
                    }

                    loadedAmmunitionCount.Clear();
                    if (CrunchatizerCore.LogSpam.Value)
                        CrunchatizerCore.Log.LogInfo("[Rearm] Cleared loadedAmmunitionCount");
                }

                // 3. Re-create all weapons using the stored initial loadout data.
                if (loadoutData.LoadoutInfos != null)
                {
                    if (CrunchatizerCore.LogSpam.Value)
                        CrunchatizerCore.Log.LogInfo($"[Rearm] Restoring {loadoutData.LoadoutInfos.Count} station entries");

                    foreach (StationLoadoutInfo loadoutInfo in loadoutData.LoadoutInfos)
                    {
                        if (loadoutInfo.AmmoFileName != null && AmmoHeuristics.LooksLikeFuelTank(loadoutInfo.AmmoFileName)) continue;
                        if (!loadoutInfo.StationParent || baseObject == null) continue;

                        // Calculate the original coordinate like how the game does it
                        Vector3 originalVector = loadoutInfo.StationParent.transform.position - baseObject.transform.position;

                        // Adjust the vector to put it in the right place
                        Vector3 adjustedSpawnPosition = loadoutInfo.LocalSpawnPosition + originalVector;
                        
                        if (CrunchatizerCore.LogSpam.Value) CrunchatizerCore.Log.LogInfo($"[Rearm] Creating ammo '{loadoutInfo.AmmoFileName}'. Adjusted LocalPos:{loadoutInfo.LocalSpawnPosition}. Passing position {adjustedSpawnPosition}.");
                        _createAmmunitionObjectInstanceMethod.Invoke(hardpoint, new object[]
                        {
                            loadoutInfo.AmmoFileName,
                            adjustedSpawnPosition,
                            loadoutInfo.LocalStationRotation.eulerAngles,
                            loadoutInfo.StationParent,
                            loadoutInfo.HostObjectParameters
                        });
                    }
                }

                // 4. Finalize state.
                hardpoint._isEmpty = false;
                hardpoint.HideWeapons();

                if (CrunchatizerCore.LogSpam.Value)
                    CrunchatizerCore.Log.LogInfo($"[Rearm] Completed rearm: {aircraftId} hardpoint {systemName}");
            }
            catch (Exception ex)
            {
                if (CrunchatizerCore.LogSpam.Value)
                    CrunchatizerCore.Log.LogInfo(
                        $"[Rearm] ERROR during rearm: {aircraftId} hardpoint {systemName} => {ex}");
                throw;
            }
            finally
            {
                _isRearming = false;
                _justRearmedHardpoint = hardpoint;
            }
        }

        #endregion

        #region Harmony Patches

        [HarmonyPatch(typeof(WeaponSystemHardpoint), "createAmmunitionObjectInstance")]
        private static class CaptureLoadout_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(WeaponSystemHardpoint __instance, string ammoFileName,
                /* The 'stationParent' parameter is no longer needed here, but can be left in */
                ObjectBaseParameters obp)
            {
                if (_isRearming || __instance._baseObject._taskforce.Side != Taskforce.TfType.Player) return;
                if (CrunchatizerCore.LogSpam.Value) CrunchatizerCore.Log.LogInfo("Loadout capture called");

                try
                {
                    var loadoutData = __instance._baseObject.GetComponent<HardpointLoadoutData>();
                    if (loadoutData == null)
                    {
                        loadoutData = __instance._baseObject.gameObject.AddComponent<HardpointLoadoutData>();
                    }

                    var weaponsList = (List<WeaponBase>?)AccessTools.Field(typeof(WeaponSystemHardpoint), "_weapons")
                        .GetValue(__instance);
                    if (weaponsList == null || weaponsList.Count == 0)
                    {
                        if (CrunchatizerCore.LogSpam.Value)
                            CrunchatizerCore.Log.LogWarning("[Capture] Weapons list is null or empty. Cannot capture.");
                        return;
                    }

                    WeaponBase newWeapon = weaponsList[weaponsList.Count - 1];
                    if (newWeapon == null || newWeapon.gameObject == null)
                    {
                        if (CrunchatizerCore.LogSpam.Value)
                            CrunchatizerCore.Log.LogWarning(
                                "[Capture] The newly created weapon or its GameObject is null.");
                        return;
                    }

                    // --- THIS IS THE KEY CHANGE ---
                    // Get the parent directly from the created weapon object itself. This is the source of truth.
                    Transform actualParentTransform = newWeapon.transform.parent;
                    if (actualParentTransform == null)
                    {
                        if (CrunchatizerCore.LogSpam.Value)
                            CrunchatizerCore.Log.LogWarning(
                                $"[Capture] Weapon '{ammoFileName}' was created with a null parent. Cannot capture local coordinates.");
                        return;
                    }
                    // --- END OF KEY CHANGE ---
                    
                    var loadoutInfo = new StationLoadoutInfo
                    {
                        AmmoFileName = ammoFileName,
                        LocalSpawnPosition = newWeapon.transform.localPosition,
                        LocalStationRotation = newWeapon.transform.localRotation,
                        // Store the parent GameObject we just safely retrieved.
                        StationParent = actualParentTransform.gameObject,
                        HostObjectParameters = obp
                    };
                    loadoutData.LoadoutInfos.Add(loadoutInfo);

                    if (CrunchatizerCore.LogSpam.Value)
                        CrunchatizerCore.Log.LogInfo(
                            $"[Capture] SUCCESS. Added ammo: '{loadoutInfo.AmmoFileName}' localPos={loadoutInfo.LocalSpawnPosition} localRot={loadoutInfo.LocalStationRotation.eulerAngles} parent={loadoutInfo.StationParent?.name ?? "null"}");
                }
                catch (Exception ex)
                {
                    if (CrunchatizerCore.LogSpam.Value)
                        CrunchatizerCore.Log.LogInfo($"[Capture] UNEXPECTED ERROR capturing loadout: {ex}");
                }
            }
        }

        [HarmonyPatch(typeof(WeaponSystemHardpoint), "launch")]
        private static class RearmTrigger_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(WeaponSystemHardpoint __instance)
            {
                if (CrunchatizerCore.LogSpam.Value) CrunchatizerCore.Log.LogInfo("Post-launch rearm trigger called");
                try
                {
                    if (__instance._baseObject._taskforce.Side != Taskforce.TfType.Player ||
                        !CrunchatizerCore.AircraftInfiniteAmmo.Value)
                        return;

                    if (_isRearming)
                    {
                        if (CrunchatizerCore.LogSpam.Value)
                            CrunchatizerCore.Log.LogInfo("[Trigger] Skipped: currently rearming");
                        return;
                    }

                    var loadedAmmoCount = (Dictionary<string, int>)AccessTools
                        .Field(typeof(WeaponSystem), "_loadedAmmunitionCount").GetValue(__instance);

                    if (loadedAmmoCount == null)
                    {
                        if (CrunchatizerCore.LogSpam.Value)
                            CrunchatizerCore.Log.LogInfo("[Trigger] loadedAmmunitionCount is null");
                        return;
                    }

                    // Log snapshot of counts
                    foreach (var kvp in loadedAmmoCount)
                    {
                        if (CrunchatizerCore.LogSpam.Value)
                            CrunchatizerCore.Log.LogInfo($"[Trigger] Ammo '{kvp.Key}' = {kvp.Value}");
                    }

                    bool shouldRearm = false;
                    string? triggeringAmmo = null;

                    foreach (var ammo in loadedAmmoCount)
                    {
                    string? name = ammo.Key ?? "(null)";
                        var value = ammo.Value;

                        // Heuristic: skip tanks
                        if (AmmoHeuristics.LooksLikeFuelTank(name))
                        {
                            if (CrunchatizerCore.LogSpam.Value)
                                CrunchatizerCore.Log.LogInfo($"[Trigger] Skipping tank-like ammo '{name}'");
                            continue;
                        }

                        if (value <= 0)
                        {
                            shouldRearm = true;
                            triggeringAmmo = name;
                            if (CrunchatizerCore.LogSpam.Value)
                                CrunchatizerCore.Log.LogInfo(
                                    $"[Trigger] Depleted non-tank ammo detected: '{name}' (<=0)");
                            break;
                        }
                    }

                    if (!shouldRearm && __instance._isEmpty)
                    {
                        bool onlyTanksOrEmpty = true;
                        foreach (var kvp in loadedAmmoCount)
                        {
                            if (!AmmoHeuristics.LooksLikeFuelTank(kvp.Key))
                            {
                                onlyTanksOrEmpty = false;
                                break;
                            }
                        }

                        if (CrunchatizerCore.LogSpam.Value)
                            CrunchatizerCore.Log.LogInfo(
                                $"[Trigger] Hardpoint _isEmpty={__instance._isEmpty}, onlyTanksOrEmpty={onlyTanksOrEmpty}");
                        shouldRearm = !onlyTanksOrEmpty;
                    }

                    if (shouldRearm)
                    {
                        if (CrunchatizerCore.LogSpam.Value)
                            CrunchatizerCore.Log.LogInfo(
                                $"[Trigger] Rearm scheduled. Triggering ammo='{triggeringAmmo ?? "unknown"}'");
                        RearmHardpoint(__instance);
                    }
                    else
                    {
                        if (CrunchatizerCore.LogSpam.Value) CrunchatizerCore.Log.LogInfo("[Trigger] No rearm needed");
                    }
                }
                catch (Exception ex)
                {
                    if (CrunchatizerCore.LogSpam.Value)
                        CrunchatizerCore.Log.LogInfo($"[Trigger] ERROR in postfix: {ex}");
                }
            }
        }

        #endregion
    }

    // END AIRCRAFT REARM CODE!!!
    // THIS TOOK FOREVER!!!
    // STILL NEEDS WORK!!!
    // THIS BLOCK IS TO GET MY ATTENTION!!!

    // Salvo fixer
    [HarmonyPatch(typeof(WeaponSystemHardpoint))]
    public static class ContinuousBombingPatch
    {
        /// <summary>
        /// This is a Harmony Prefix for the EndEngageTask method.
        /// Its purpose is to intercept the call that ends a weapon engagement.
        /// A prefix that returns 'false' will PREVENT the original method from running.
        /// </summary>
        /// <param name="__instance">A reference to the WeaponSystemHardpoint instance this method is being called on.</param>
        /// <returns>
        /// Returns 'false' to block the task from ending if it's a bomb salvo.
        /// Returns 'true' to allow the task to end normally in all other cases.
        /// </returns>
        [HarmonyPatch("EndEngageTask", new Type[] { typeof(bool) })]
        public static bool EndEngageTask_Prefix(WeaponSystemHardpoint __instance, bool ____isLaunchingSalvo,
            ref int ____currentShotInSalvo)
        {
            // Safety check: If for some reason there's no ammo selected for engagement,
            // let the task end cleanly to prevent errors.
            if (__instance._ammoForEngage == null || __instance._baseObject._taskforce.Side != Taskforce.TfType.Player)
            {
                return true;
            }

            // This is the core logic. We check for the specific conditions of a bomb salvo.
            // 1. Is the hardpoint currently in the middle of launching a salvo?
            // 2. Is the type of ammunition being used a Bomb?
            bool isBombSalvo = ____isLaunchingSalvo &&
                               CrunchatizerCore.AircraftInfiniteAmmo.Value &&
                               __instance._ammoForEngage._ap._type is Ammunition.Type.Bomb or Ammunition.Type.Cluster or Ammunition.Type.AerialRocket;

            if (isBombSalvo)
            {
                // We've detected the end of a bomb salvo.
                // Instead of letting the engagement task end, we will trick it into starting over.
                if(CrunchatizerCore.LogSpam.Value) CrunchatizerCore.Log.LogInfo($"Preventing the end of an engage task for {__instance._name}");
                // By resetting the salvo counter back to zero, the OnUpdate() loop will
                // think it's at the beginning of the salvo again.
                ____currentShotInSalvo = 0;

                // By returning 'false', we stop the original EndEngageTask() method from
                // ever being called. The attack run will continue indefinitely.
                return false;
            }

            // If it's not a bomb salvo (e.g., a missile, a gun, a single bomb drop),
            // we return 'true' to allow the original EndEngageTask() to run as normal.
            return true;
        }
    }


    [HarmonyPatch(typeof(Ammunition), MethodType.Constructor, typeof(string), typeof(int), typeof(WeaponSystem))]
    public static class ModifyAmmunitionAtLoadConstructor
    {
        [HarmonyPostfix]
        private static void Postfix(Ammunition __instance, WeaponSystem associatedWeaponSystem)
        {
            if (associatedWeaponSystem?._baseObject?._taskforce?.Side != Taskforce.TfType.Player) return;

            if (__instance._ap == null)
            {
                if (CrunchatizerCore.LogSpam.Value)
                    CrunchatizerCore.Log.LogWarning(
                        $"AmmoMod: AmmunitionParameters (_ap) is null for ammo created by '{associatedWeaponSystem?._name ?? "Unknown"}'. Cannot modify.");
                return;
            }

            if (CrunchatizerCore.ForceTerrainFollowing.Value)
            {
                ApplyTerrainFollowing(__instance);
            }

            if (CrunchatizerCore.EnhanceMissileFeatures.Value)
            {
                EnhanceWeapon(__instance);
            }

            ApplyRangeMultiplier(__instance, CrunchatizerCore.WeaponRangeMult.Value);
        }

        private static void ApplyTerrainFollowing(Ammunition ammo)
        {
            switch (ammo._ap._type)
            {
                case Ammunition.Type.Missile:
                case Ammunition.Type.Torpedo:
                    if (!ammo._ap._terrainFollowFlight)
                    {
                        ammo._ap._terrainFollowFlight = true;
                        if (CrunchatizerCore.LogSpam.Value)
                            CrunchatizerCore.Log.LogInfo(
                                $"AmmoMod: Forced Terrain Following for '{ammo._ap._displayedName}'.");
                    }
                    break;
            }
        }

        private static void EnhanceWeapon(Ammunition ammo)
        {
            switch (ammo._ap._type)
            {
                case Ammunition.Type.Missile:
                    EnhanceMissile(ammo);
                    break;
                case Ammunition.Type.Torpedo:
                    EnhanceTorpedo(ammo);
                    break;
                case Ammunition.Type.Bomb:
                    EnhanceBomb(ammo);
                    break;
            }
        }

        private static void EnhanceMissile(Ammunition ammo)
        {
            ammo._ap._requiresWarmUp = false;
            ammo._ap._targetMemory = true;
            ammo._ap._inertialGuidance = true;
            ammo._ap._hasESM = true;
            ammo._ap._isRearAspectOnly = false;
            ammo._ap._sharedSensorLink = true;
            ammo._ap._nightVisionLevel = 1f;
            ammo._ap._maxDepthUnity *= 100f;
            ammo._ap._minDepthUnity = 0f;
            ammo._ap._launchAltitudesInUnity.x = 0f;
            ammo._ap._launchAltitudesInUnity.y *= 10f;
            ammo._ap._seekerGimbalFOV = 180f;
            ammo._ap._secondaryPassiveRadarGuidanceType =
                AmmunitionParameters.SecondaryPassiveRadarGuidanceType.Full;
            foreach (var freq in (Globals.Frequency[])Enum.GetValues(typeof(Globals.Frequency)))
                if (freq != Globals.Frequency.Undefined &&
                    !ammo._ap._passiveRadarHomingFrequencies.Contains(freq))
                    ammo._ap._passiveRadarHomingFrequencies.Add(freq);
        }

        private static void EnhanceTorpedo(Ammunition ammo)
        {
            ammo._ap._targetMemory = true;
            ammo._ap._inertialGuidance = true;
            ammo._ap._maxDepthUnity *= 100f;
            ammo._ap._minDepthUnity = 0f;
            ammo._ap._launchAltitudesInUnity.x = 0f;
            ammo._ap._launchAltitudesInUnity.y *= 10f;
        }

        private static void EnhanceBomb(Ammunition ammo)
        {
            ammo._ap._requiresWarmUp = false;
            ammo._ap._targetMemory = true;
            ammo._ap._inertialGuidance = true;
            ammo._ap._hasESM = true;
            ammo._ap._isRearAspectOnly = false;
            ammo._ap._sharedSensorLink = true;
            ammo._ap._nightVisionLevel = 1f;
            ammo._ap._maxDepthUnity *= 100f;
            ammo._ap._minDepthUnity = 0f;
            ammo._ap._launchAltitudesInUnity.x = 0f;
            ammo._ap._launchAltitudesInUnity.y *= 10f;
            ammo._ap._seekerGimbalFOV = 180f;
            ammo._ap._secondaryPassiveRadarGuidanceType =
                AmmunitionParameters.SecondaryPassiveRadarGuidanceType.Full;
            foreach (var freq in (Globals.Frequency[])Enum.GetValues(typeof(Globals.Frequency)))
                if (freq != Globals.Frequency.Undefined &&
                    !ammo._ap._passiveRadarHomingFrequencies.Contains(freq))
                    ammo._ap._passiveRadarHomingFrequencies.Add(freq);
        }

        private static void ApplyRangeMultiplier(Ammunition ammo, float multiplier)
        {
            ammo._ap._lifeTime *= multiplier;
            ammo._ap._maxLaunchRangeInMiles *= multiplier;
            ammo._ap._launchRangesInUnity.y *= multiplier;
            ammo._ap._horizonRangesInUnity.y *= multiplier;
            ammo._ap._seekerPassiveRange *= multiplier;
            if (CrunchatizerCore.LogSpam.Value)
                CrunchatizerCore.Log.LogInfo(
                    $"AmmoMod: Applied Range Multiplier ({multiplier:F2}x sqrt) to '{ammo._ap._displayedName}'.");
        }
    }

// Modify base WeaponSystem properties after loading from INI
    [HarmonyPatch(typeof(WeaponSystem), "LoadFromInI")]
    public static class MunchWeaponProperties
    {
        [HarmonyPostfix]
        private static void Postfix(WeaponSystem __instance)
        {
            if (__instance?._baseObject?._taskforce?.Side != Taskforce.TfType.Player) return;

            if (__instance._vwp == null)
            {
                if (CrunchatizerCore.LogSpam.Value)
                    CrunchatizerCore.Log.LogWarning(
                        $"WeaponMod: WeaponParameters (_vwp) is null for '{__instance._name}' on '{__instance._baseObject.name}'. Cannot modify.");
                return;
            }

            ApplyFireRateMultiplier(__instance, CrunchatizerCore.FireRateMult.Value);
            ApplyDivisor(__instance._vwp, "_maxReactiontime", CrunchatizerCore.ReactionTimeDiv.Value);
            ApplyDivisor(__instance._vwp, "_targetAcquisitionTime", CrunchatizerCore.TargetAcqTimeDiv.Value);
            ApplyDivisor(__instance._vwp, "_preLaunchDelay", CrunchatizerCore.PreLaunchDelayDiv.Value);
            ApplyDivisor(__instance._vwp, "_magazineReloadTime", CrunchatizerCore.MagReloadTimeDiv.Value);
            ApplyMultiplier(__instance._vwp, "_verticalDegreesPerSecond", CrunchatizerCore.TraverseSpeedMult.Value);
            ApplyMultiplier(__instance._vwp, "_horizontalDegreesPerSecond", CrunchatizerCore.TraverseSpeedMult.Value);

            if (CrunchatizerCore.LogSpam.Value)
                CrunchatizerCore.Log.LogInfo(
                    $"WeaponMod: Applied modifiers to player weapon '{__instance._name}' on '{__instance._baseObject.name}'.");
        }

        private static void ApplyFireRateMultiplier(WeaponSystem weaponSystem, float multiplier)
        {
            if (multiplier == 0f)
            {
                weaponSystem._vwp._fireRatePerMinute = int.MaxValue;
                weaponSystem._vwp._delayBetweenLaunches = 0f;
                weaponSystem._vwp._burstTime = 0f;
                weaponSystem._vwp._salvoFireTime = 0f;
            }
            else if (multiplier > 0f && !Mathf.Approximately(multiplier, 1.0f))
            {
                var calculatedRate = weaponSystem._vwp._fireRatePerMinute * multiplier;
                var newRate = Math.Max(1, (int)Math.Round(calculatedRate));

                if (newRate != weaponSystem._vwp._fireRatePerMinute)
                {
                    weaponSystem._vwp._fireRatePerMinute = newRate;
                }

                if (weaponSystem._vwp._delayBetweenLaunches > 0) weaponSystem._vwp._delayBetweenLaunches /= multiplier;
                if (weaponSystem._vwp._burstTime > 0) weaponSystem._vwp._burstTime /= multiplier;
                if (weaponSystem._vwp._salvoFireTime > 0) weaponSystem._vwp._salvoFireTime /= multiplier;
            }
        }

        private static void ApplyMultiplier(object obj, string fieldName, float multiplier)
        {
            var field = AccessTools.Field(obj.GetType(), fieldName);
            if (field != null && field.FieldType == typeof(float))
            {
                var value = (float)field.GetValue(obj);
                if (value > 0)
                {
                    field.SetValue(obj, value * multiplier);
                }
            }
        }

        private static void ApplyDivisor(object obj, string fieldName, float divisor)
        {
            if (divisor == 0f)
            {
                var field = AccessTools.Field(obj.GetType(), fieldName);
                if (field != null && field.FieldType == typeof(float))
                {
                    field.SetValue(obj, 0f);
                }
            }
            else if (divisor > 0f && !Mathf.Approximately(divisor, 1.0f))
            {
                var field = AccessTools.Field(obj.GetType(), fieldName);
                if (field != null && field.FieldType == typeof(float))
                {
                    var value = (float)field.GetValue(obj);
                    if (value > 0)
                    {
                        field.SetValue(obj, value / divisor);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(SensorSystem), "LoadFromInI", typeof(IniHandler), typeof(string), typeof(ObjectBaseParameters),
        typeof(string))]
    public static class ModifySensorSystemAtLoad
    {
        [HarmonyPostfix]
        private static void Postfix(SensorSystem __instance)
        {
            if (CrunchatizerCore.BrokenFireControl.Value &&
                __instance._baseObject._taskforce.Side == Taskforce.TfType.Player)
            {
                __instance._targetChannels *= 128;
                __instance._weaponChannels *= 128;
            }
        }
    }

    /* Not relevant, nothing happens to ECM, but I wanted to have it here
    [HarmonyPatch(typeof(SensorSystemECM), "LoadFromInI", typeof(IniHandler), typeof(string),
        typeof(ObjectBaseParameters), typeof(string))]
    public static class ModifySensorECMAtLoad
    {
        [HarmonyPostfix]
        private static void Postfix(SensorSystemECM __instance)
        {

        }
    } */

    [HarmonyPatch(typeof(SensorSystemESM), "LoadFromInI", typeof(IniHandler), typeof(string),
        typeof(ObjectBaseParameters), typeof(string))]
    public static class ModifySensorESMAtLoad
    {
        [HarmonyPostfix]
        private static void Postfix(SensorSystemESM __instance)
        {
            if (CrunchatizerCore.BrokenSensorParams.Value &&
                __instance._baseObject._taskforce.Side == Taskforce.TfType.Player)
            {
                __instance._esm._ep._gain += 20f;
                __instance._esm._ep._gainFactor = Mathf.Pow(10f, __instance._esm._ep._gain / 10f);
                __instance._esm._ep._angularResolutionDegrees /= 10f;
                __instance._esm._ep._frequencies =
                    new List<Globals.Frequency>((Globals.Frequency[])Enum.GetValues(typeof(Globals.Frequency)));
                __instance._esm._ep._hasDataLink = true;
                __instance._esm._ep._identificationRate *= 2f;
            }
        }
    }

    [HarmonyPatch(typeof(SensorSystemRadar), "LoadFromInI", typeof(IniHandler), typeof(string),
        typeof(ObjectBaseParameters), typeof(string))]
    public static class ModifySensorRadarAtLoad
    {
        [HarmonyPostfix]
        private static void Postfix(SensorSystemRadar __instance)
        {
            if (CrunchatizerCore.BrokenSensorParams.Value &&
                __instance._baseObject._taskforce.Side == Taskforce.TfType.Player)
            {
                __instance._radar._rp._gain += 20f;
                __instance._radar._rp._gainFactor = Mathf.Pow(10f, __instance._radar._rp._gain / 10f);
                __instance._radar._rp._hasDataLink = true;
                __instance._radar._rp._canDetectLandTargets = true;
                __instance._radar._rp._canDetectPeriscope = true;
                __instance._radar._rp._minAltitude /= 10;
                __instance._radar._rp._maxAltitude *= 10;
                __instance._radar._rp._minRange /= 10;
                __instance._radar._rp._maxRange *= 10;
            }
        }
    }

    [HarmonyPatch(typeof(SensorSystemSonar), "LoadFromInI", typeof(IniHandler), typeof(string),
        typeof(ObjectBaseParameters), typeof(string))]
    public static class ModifySensorSonarAtLoad
    {
        [HarmonyPostfix]
        private static void Postfix(SensorSystemSonar __instance)
        {
            if (CrunchatizerCore.BrokenSensorParams.Value &&
                __instance._baseObject._taskforce.Side == Taskforce.TfType.Player)
            {
                __instance._sonar._sp._gain += 20f;
                __instance._sonar._sp._activeGain += 20f;
                __instance._sonar._sp._hasDataLink = true;
                __instance._sonar._sp._activeRangeInKm *= 20f;
                __instance._sonar._sp._angularResolutionDegrees /= 10f;
            }
        }
    }

    [HarmonyPatch(typeof(SensorSystemVisual), "LoadFromInI", typeof(IniHandler), typeof(string),
        typeof(ObjectBaseParameters), typeof(string))]
    public static class ModifySensorVisualAtLoad
    {
        [HarmonyPostfix]
        private static void Postfix(SensorSystemVisual __instance)
        {
            if (CrunchatizerCore.BrokenSensorParams.Value &&
                __instance._baseObject._taskforce.Side == Taskforce.TfType.Player)
            {
                __instance._vidRangeMultiplier *= 10f;
                __instance._lookDownMultiplier = 1f;
                __instance._maxRangeMultiplier *= 10f;
                __instance._nightVisionLevel = 1f;
            }
        }
    }

    [HarmonyPatch(typeof(SceneCreator), "SetAdditionalParameters", typeof(IniHandler), typeof(ObjectBase),
        typeof(SharedUnitData))]
    public static class ModifyUnitWhenSettingAdditionalParameters
    {
        [HarmonyPostfix]
        private static void Postfix(ref ObjectBase unit, ref SharedUnitData unitData)
        {
            if (CrunchatizerCore.UltraCrew.Value && unit._taskforce.Side == Taskforce.TfType.Player)
            {
                unitData._crewlSkill = ObjectBase.CrewSkill.Ultra;
                unit._crew.SetCrewSkill(unitData._crewlSkill);
            }
        }
    }

    // Modify WeaponSystemGun specific fire rate properties
    [HarmonyPatch(typeof(WeaponSystemGun), "LoadFromInI", typeof(IniHandler), typeof(string),
        typeof(ObjectBaseParameters), typeof(string))]
    public static class AlterGunFireRate
    {
        [HarmonyPostfix]
        // Use ref for private fields passed by Harmony (__instance fields need ref)
        private static void Postfix(WeaponSystemGun __instance, ref float ____burstDelay,
            ref float ____delayBetweenShotsinBurst, ref float ____shellReloadTime, ref float ____burstCount)
        {
            // Check player status safely
            if (__instance?._baseObject?._taskforce?.Side != Taskforce.TfType.Player) return;

            var fireRateMult = CrunchatizerCore.FireRateMult.Value;
            if (fireRateMult == 0f)
            {
                ____burstCount = 0;
                ____burstDelay = 0;
                ____delayBetweenShotsinBurst = 0;
                ____shellReloadTime = 0;
            }
            else
            {
                if (____burstDelay > 0)
                {
                    ____burstDelay /= fireRateMult;
                }

                if (____delayBetweenShotsinBurst > 0)
                {
                    ____delayBetweenShotsinBurst /= fireRateMult;
                }

                if (____shellReloadTime > 0)
                {
                    ____shellReloadTime /= fireRateMult;
                }
            }

            if (CrunchatizerCore.LogSpam.Value)
                CrunchatizerCore.Log.LogInfo(
                    $"GunMod: Applied fire rate multiplier ({fireRateMult}x) to gun-specific delays for '{__instance._name}' on '{__instance._baseObject.name}'.");
            // Log resulting values if needed: $" -> BurstDelay={____burstDelay:F3}, ShotDelay={____delayBetweenShotsinBurst:F3}, ShellReload={____shellReloadTime:F3}"
        }
    }


    // Nullify RBU reload animations for speed
    [HarmonyPatch(typeof(WeaponSystemLauncher), "init")]
    public static class FixRBUReloadAnimations
    {
        [HarmonyPostfix]
        private static void Postfix(WeaponSystemLauncher __instance)
        {
            // Check player status safely and weapon type
            if (__instance?._baseObject?._taskforce?.Side == Taskforce.TfType.Player &&
                __instance._weaponType == WeaponSystem.WeaponType.RBU)
            {
                var changed = false;
                if (__instance._loadAmmunitionAnimation != null)
                {
                    __instance._loadAmmunitionAnimation = null;
                    changed = true;
                }

                if (__instance._unloadAmmunitionAnimation != null)
                {
                    __instance._unloadAmmunitionAnimation = null;
                    changed = true;
                }

                if (__instance._forceMoveToLoadPosition)
                {
                    __instance._forceMoveToLoadPosition = false;
                    changed = true;
                }

                if (__instance._openSystemAnimation != null)
                {
                    __instance._openSystemAnimation = null;
                    changed = true;
                }

                if (__instance._closeSystemAnimation != null)
                {
                    __instance._closeSystemAnimation = null;
                    changed = true;
                }

                if (changed && CrunchatizerCore.LogSpam.Value)
                    CrunchatizerCore.Log.LogInfo(
                        $"RBUAnimFix: Nullified reload animations for player RBU '{__instance._name}' on '{__instance._baseObject.name}'.");
            }
        }
    }


    // Alter shared launch delays (set to zero)
    [HarmonyPatch(typeof(ObjectBaseLoader), "LoadWeaponSystems", typeof(IniHandler), typeof(ObjectBaseParameters),
        typeof(ObjectBase))]
    public static class AlterWeaponSystemsAtObjectLoad
    {
        [HarmonyPostfix]
        // Parameter `obp` contains the BaseObject being loaded. No ref needed if only reading/modifying its members.
        private static void Postfix(ObjectBaseParameters obp)
        {
            // Check player status safely
            if (obp?._baseObject?._taskforce?.Side != Taskforce.TfType.Player) return;

            // Skip aircraft
            if (obp._baseObject._type == ObjectBase.ObjectType.Aircraft ||
                obp._baseObject._type == ObjectBase.ObjectType.Helicopter) return;

            // Check if the dictionary exists and has entries
            if (obp._baseObject._sharedLaunchIntervals == null || !obp._baseObject._sharedLaunchIntervals.Any()) return;

            // Need ToList() to modify dictionary while iterating keys
            var keys = obp._baseObject._sharedLaunchIntervals.Keys.ToList();
            foreach (var key in keys)
                if (obp._baseObject._sharedLaunchIntervals[key] > 0) // Only modify if not already zero
                {
                    if (CrunchatizerCore.LogSpam.Value)
                        CrunchatizerCore.Log.LogInfo(
                            $"SharedLaunchDelay: Modifying interval for '{key}' from {obp._baseObject._sharedLaunchIntervals[key]}, dividing by multiplier {CrunchatizerCore.FireRateMult.Value} on '{obp._baseObject.name}'.");

                    if (CrunchatizerCore.FireRateMult.Value == 0)
                    {
                        obp._baseObject._sharedLaunchIntervals[key] = 0f; // Set to zero
                    }
                    else
                    {
                        obp._baseObject._sharedLaunchIntervals[key] /=
                            CrunchatizerCore.FireRateMult.Value; // Divide by fire rate multw
                    }
                }

            // if (modified && CrunchatizerCore.LogSpam.Value)
            //     CrunchatizerCore.Log.LogInfo($"SharedLaunchDelay: Finished modifying shared launch intervals for player vessel '{obp._baseObject.name}'.");
        }
    }


    // I couldn't figure out any better way to stop the barrel from RECEDING INTO THE SHIP so instead you all get this.
    // Better way would be to intercept the readIni functions to do arbitrary modifications but that seems EXTREMELY difficult
    [HarmonyPatch(typeof(GunBarrel), MethodType.Constructor, typeof(GameObject), typeof(WeaponParameters),
        typeof(WeaponSystemGun), typeof(GameObject), typeof(Vector3), typeof(Vector3), typeof(float),
        typeof(float))]
    public static class InterceptRecoilData
    {
        private static void Prefix(ref float recoilTime, ref float recoilStrength, ref WeaponSystemGun vwsg)
        {
            if (vwsg._baseObject._type == ObjectBase.ObjectType.Aircraft) return;
            if (vwsg._baseObject._taskforce.Side != Taskforce.TfType.Player) return;
            if (CrunchatizerCore.LogSpam.Value)
                CrunchatizerCore.Log.LogInfo("Our recoil time for system " + vwsg._name + " is " + recoilTime);
            if (CrunchatizerCore.FireRateMult.Value == 0 || CrunchatizerCore.MagReloadTimeDiv.Value == 0)
            {
                recoilTime = 0f;
                recoilStrength = 0f;
            }
            else
            {
                recoilTime /= CrunchatizerCore.FireRateMult.Value;
                recoilStrength /= CrunchatizerCore.FireRateMult.Value;
            }

            if (CrunchatizerCore.LogSpam.Value) CrunchatizerCore.Log.LogInfo(("Now it is " + recoilTime));
        }
    }


    [HarmonyPatch]
    public static class DisableFuelConsumptionPatch
    {
        [HarmonyPatch(typeof(Aircraft), "UpdateFuelConsumption")]
        [HarmonyPrefix]
        private static bool Prefix(Aircraft __instance)
        {
            if (CrunchatizerCore.AircraftInfRange.Value && __instance?._taskforce?.Side == Taskforce.TfType.Player)
            {
                return false; // Skip the original method entirely
            }
            return true; // Run normally for AI aircraft
        }

        [HarmonyPatch(typeof(Helicopter), "UpdateFuelConsumption")]
        [HarmonyPrefix]
        private static bool Prefix(Helicopter __instance)
        {
            if (CrunchatizerCore.AircraftInfRange.Value && __instance?._taskforce?.Side == Taskforce.TfType.Player)
            {
                return false; // Skip the original method entirely
            }
            return true; // Run normally for AI aircraft
        }
    }
} // End of namespace