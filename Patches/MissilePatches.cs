// Missile behavior patches
// Extracted from CrunchatizerCore.cs during refactoring

using System;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using SeaPower;
using SeaPowerCrunchatizer.Utilities;

namespace SeaPowerCrunchatizer.Patches
{
    /// <summary>
    /// Modifies missile terminal approach to allow wider bearing to target.
    /// This enables missiles to re-attack targets more effectively.
    /// Patches the TerminalApproach constructor (a nested class inside Missile).
    /// </summary>
    [HarmonyPatch]
    internal static class MissileReattackPatch
    {
        /// <summary>
        /// Dynamically resolves the TerminalApproach nested class constructor.
        /// </summary>
        [HarmonyTargetMethod]
        [UsedImplicitly]
        private static MethodBase? TargetMethod()
        {
            // TerminalApproach is an internal nested class inside Missile
            var terminalApproachType = typeof(Missile).GetNestedType("TerminalApproach", BindingFlags.NonPublic);
            if (terminalApproachType == null)
            {
                throw new InvalidOperationException("Could not find Missile.TerminalApproach type");
            }
            return Reflect.Constructor(terminalApproachType, new[] { typeof(Missile) });
        }

        /// <summary>
        /// Postfix that maximizes bearing to target for player missiles after TerminalApproach state is created.
        /// </summary>
        [HarmonyPostfix]
        [UsedImplicitly]
        private static void Postfix(object __instance, Missile missile)
        {
            try
            {
                // Defensive null checks
                if (__instance == null || missile == null)
                {
                    return;
                }

                // Only apply to player missiles, and not to AAW missiles
                if (!PlayerUtils.IsPlayerUnit(missile))
                {
                    return;
                }

                if (missile._ap == null)
                {
                    return;
                }

                if (missile._ap._targetType == Ammunition.Target.AAW ||
                    missile._ap._secondaryTargetType == Ammunition.Target.AAW)
                {
                    return;
                }

                // Set _maxBearingToTarget on this TerminalApproach instance to allow wide re-attack angles
                var field = Traverse.Create(__instance).Field("_maxBearingToTarget");
                if (field.FieldExists())
                {
                    field.SetValue(float.MaxValue);
                }
            }
            catch (Exception ex)
            {
                // Log but don't crash
                BepInEx.Logging.Logger.CreateLogSource("MissileReattackPatch")
                    .LogError($"Exception in MissileReattackPatch: {ex}");
            }
        }
    }

    // TODO: Experimental SATCOM missile patches - disabled pending further testing
    // These patches would simulate a perfect radio link for player missiles without radar guidance,
    // bypassing LOS/range checks and avoiding NullReferenceExceptions.
    //
    // Original code preserved for future development:
    /*
    /// <summary>
    /// A prefix patch for CheckRadioConnection.
    /// If the missile is a player-controlled, radio-guided missile without a radar system,
    /// this patch simulates a perfect "SATCOM" link and skips the original method,
    /// avoiding all LOS/range checks and NullReferenceExceptions.
    /// </summary>
    [HarmonyPatch(typeof(Missile), "CheckRadioConnection")]
    public static class Missile_CheckRadioConnection_Prefix
    {
        [HarmonyPrefix]
        [UsedImplicitly]
        private static bool Prefix(Missile __instance)
        {
            // Define the conditions for our "cheat" missile.
            if (__instance._taskforce.Side != Taskforce.TfType.Player ||
                __instance._ap._midCourseCorrection == AmmunitionParameters.MidCourseCorrection.RadioCommand ||
                __instance._guidingRadarSystem != null) return true;

            // --- SATCOM CHEAT LOGIC ---
            // Force the connection to be considered perfect.
            __instance.ConnectionLost.Value = false;
            __instance._jammed = false;

            // Skip the original CheckRadioConnection method.
            return false;
        }
    }
    */
}
