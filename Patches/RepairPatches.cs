// Repair and Damage Control system patches
// Refactored to use minimal, targeted patches instead of method replacements

using System;
using System.Text;
using HarmonyLib;
using JetBrains.Annotations;
using Sea_Power_Crunchatizer;
using SeaPower;
using SeaPowerCrunchatizer.Config;
using SeaPowerCrunchatizer.Systems;
using SeaPowerCrunchatizer.Utilities;

namespace SeaPowerCrunchatizer.Patches
{
    /// <summary>
    /// Bypasses the repair damage cap by overriding BiggestDamageType for player units.
    ///
    /// The game tracks the worst damage a system ever received (BiggestDamageType) and
    /// prevents repair beyond that threshold. The game's repair logic checks:
    ///   if (BiggestDamageType &lt;= CurrentDamageType) { repair(); return true; }
    ///   Inoperable.Value = false; // Only reached when repair is "complete"
    ///
    /// Strategy:
    /// - While integrity &lt; max: return No, so repair continues past normal damage cap
    /// - At full integrity (100%): return Light, so the vanilla repair-complete logic
    ///   triggers and DC crews move on to other compartments
    ///
    /// Note: Systems come back online at &gt;90% integrity via the separate
    /// UnlimitedRepair_RestoreSystemOnFullRepair patch, but repair continues to 100%.
    /// The existing DC allocation logic prioritizes lower-integrity systems, so "cleanup"
    /// work (91% to 100%) naturally happens after critical repairs are done.
    /// </summary>
    [HarmonyPatch(typeof(BaseSystem), "BiggestDamageType", MethodType.Getter)]
    internal static class UnlimitedRepair_BypassDamageCapPatch
    {
        /// <summary>
        /// Postfix that overrides BiggestDamageType to allow full repair for player units.
        /// Returns No while integrity is below max (allows repair to continue).
        /// Returns Light at full integrity (triggers repair-complete logic).
        /// </summary>
        [HarmonyPostfix]
        [UsedImplicitly]
        private static void Postfix(BaseSystem __instance, ref DamageValues.DamageType __result)
        {
            if (!CheatConfig.UnlimitedRepair.Value)
            {
                return;
            }

            if (!PlayerUtils.IsPlayerUnit(__instance._baseObject))
            {
                return;
            }

            // If the system still needs repair, return No so repair can continue past normal cap
            // The check BiggestDamageType <= CurrentDamageType becomes: 0 <= X, always true
            if (__instance.CurrentIntegrity < __instance._maxIntegrity)
            {
                __result = DamageValues.DamageType.No;
                return;
            }

            // System is at full integrity (100%)
            // Return Light so the repair-complete branch triggers:
            //   if (Light <= No) is FALSE, so it skips repair and DC crews move on
            // The OnFixedUpdate patch handles bringing systems online at >90%
            __result = DamageValues.DamageType.Light;
        }
    }

    /// <summary>
    /// Makes all systems always repairable for player units.
    /// This handles the IsDestroyed check in repair logic.
    /// Records original state for side-swap restoration.
    /// </summary>
    [HarmonyPatch(typeof(BaseSystem), "init")]
    internal static class UnlimitedRepair_PatchBaseSystem
    {
        /// <summary>
        /// Postfix that sets _alwaysRepairable flag for player unit systems.
        /// </summary>
        [HarmonyPostfix]
        [UsedImplicitly]
        private static void Postfix(BaseSystem __instance)
        {
            if (!CheatConfig.UnlimitedRepair.Value)
            {
                return;
            }

            if (!PlayerUtils.IsPlayerUnit(__instance._baseObject))
            {
                return;
            }

            // Aircraft don't have the same repair system
            if (__instance._baseObject._type == ObjectBase.ObjectType.Aircraft)
            {
                return;
            }

            // Record original state before modification (for side-swap restoration)
            CheatStateTracker.RecordSystemState(
                __instance._baseObject,
                __instance,
                __instance._alwaysRepairable);

            __instance._alwaysRepairable = true;

            PlayerUtils.LogIfSpam(
                $"UnlimitedRepair: Set _alwaysRepairable=true for '{__instance._systemName ?? __instance.GetType().Name}' on '{__instance._baseObject.name}'");
        }
    }

    /// <summary>
    /// Forces maximum allowed integrity for flooding compartments on player vessels.
    /// Without this, hull breaches would have a repair cap based on damage received.
    /// </summary>
    [HarmonyPatch(typeof(FloodingCompartment), "MaxAllowedIntegrity", MethodType.Getter)]
    internal static class UnlimitedRepair_FloodingCompartment_MaxAllowedIntegrityPatch
    {
        /// <summary>
        /// Postfix that overrides maximum integrity calculation for player units.
        /// </summary>
        [HarmonyPostfix]
        [UsedImplicitly]
        private static void Postfix(FloodingCompartment __instance, ref float __result)
        {
            if (!CheatConfig.UnlimitedRepair.Value)
            {
                return;
            }

            var baseObject = __instance._compartments?._baseObject;
            if (baseObject == null)
            {
                return;
            }

            if (baseObject._taskforce?.Side != Taskforce.TfType.Player)
            {
                return;
            }

            if (baseObject._type != ObjectBase.ObjectType.Vessel &&
                baseObject._type != ObjectBase.ObjectType.Submarine)
            {
                return;
            }

            __result = __instance._maxIntegrity;

            PlayerUtils.LogIfExtremeSpam(
                $"UnlimitedRepair: Forced MaxAllowedIntegrity to {__result} for flooding compartment on '{baseObject.name}'");
        }
    }

    /// <summary>
    /// Fixes a side-effect of the BiggestDamageType patch where systems never come back online.
    ///
    /// The vanilla game's RepairSystems() method only clears Inoperable when:
    ///   BiggestDamageType > CurrentDamageType
    /// But our BiggestDamageType patch returns No (0), so this condition is never true.
    ///
    /// This patch monitors systems during OnFixedUpdate and brings them back online
    /// when they've been fully repaired (CurrentDamageType == No).
    /// </summary>
    [HarmonyPatch(typeof(BaseSystem), nameof(BaseSystem.OnFixedUpdate))]
    internal static class UnlimitedRepair_RestoreSystemOnFullRepair
    {
        /// <summary>
        /// Postfix that brings systems back online once they're fully repaired.
        /// </summary>
        [HarmonyPostfix]
        [UsedImplicitly]
        private static void Postfix(BaseSystem __instance)
        {
            if (!CheatConfig.UnlimitedRepair.Value)
            {
                return;
            }

            if (!PlayerUtils.IsPlayerUnit(__instance._baseObject))
            {
                return;
            }

            // Only act if the system is currently inoperable but fully repaired
            if (!__instance.Inoperable.Value)
            {
                return;
            }

            // Check if the system has been repaired to operational status
            // CurrentDamageType.No means > 90% integrity
            if (__instance.CurrentDamageType != DamageValues.DamageType.No)
            {
                return;
            }

            // Bring the system back online
            __instance.Inoperable.Value = false;

            PlayerUtils.LogIfSpam(
                $"UnlimitedRepair: Restored '{__instance._systemName ?? __instance.GetType().Name}' to operational status on '{__instance._baseObject.name}'");
        }
    }

    /// <summary>
    /// Multiplies the number of damage control teams on player ships and submarines.
    /// Hooks the Compartments constructor to apply the multiplier after the game
    /// calculates the default number based on displacement.
    /// </summary>
    [HarmonyPatch(typeof(Compartments), MethodType.Constructor, typeof(ObjectBase))]
    internal static class DCTeamMultiplier_Patch
    {
        /// <summary>
        /// Postfix that multiplies DC teams for player units after initialization.
        /// </summary>
        [HarmonyPostfix]
        [UsedImplicitly]
        private static void Postfix(Compartments __instance)
        {
            var multiplier = CheatConfig.DCTeamMultiplier.Value;
            if (multiplier <= 1)
            {
                return;
            }

            var baseObject = __instance._baseObject;
            if (baseObject == null)
            {
                return;
            }

            if (!PlayerUtils.IsPlayerUnit(baseObject))
            {
                return;
            }

            // Only apply to vessels and submarines
            if (baseObject._type != ObjectBase.ObjectType.Vessel &&
                baseObject._type != ObjectBase.ObjectType.Submarine)
            {
                return;
            }

            // Multiply both the actual pool count and the display "default" value
            var currentTeams = __instance.DamageControlTeamsNumbers[5];
            var newTeams = currentTeams * multiplier;

            __instance.SetDamageControlTeamsNumbers(5, newTeams);

            // Also update the default number used for UI display (the "X/Y" denominator)
            __instance.DamageControlTeamDefaultsNumber = newTeams;

            PlayerUtils.LogIfSpam(
                $"DCTeamMultiplier: Increased DC teams from {currentTeams} to {newTeams} on '{baseObject.name}'");
        }
    }

    /// <summary>
    /// Debug logging patch for system repair priority setter.
    /// Only active when LogSpam is enabled.
    /// </summary>
    [HarmonyPatch(typeof(SystemCompartment), "SystemPrioritisedForRepair", MethodType.Setter)]
    internal static class SystemCompartment_SystemPrioritisedForRepair_Setter_LogPatch
    {
        /// <summary>
        /// Postfix that logs repair priority changes for debugging.
        /// </summary>
        [HarmonyPostfix]
        [UsedImplicitly]
        private static void Postfix(SystemCompartment __instance, BaseSystem value)
        {
            if (!CheatConfig.LogSpam.Value)
            {
                return;
            }

            try
            {
                var sb = new StringBuilder();
                var objName = __instance._object?.name ?? "Unknown Object";
                sb.Append($"[PriorityPatch] Setter called on SystemCompartment of '{objName}'.");

                var incomingValueStr =
                    $"'{value._systemName ?? value.GetType().Name}' (Repairable={value.Repairable}, Destroyed={value.IsDestroyed})";
                sb.Append($" Incoming value: {incomingValueStr}.");

                var currentPriority =
                    Traverse.Create(__instance).Field<BaseSystem>("_systemPrioritisedForRepair").Value;
                var resultingValueStr = currentPriority == null
                    ? "null"
                    : $"'{currentPriority._systemName ?? currentPriority.GetType().Name}' (IsPriority={currentPriority.damageSummary?.IsPriority ?? false})";
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
}
