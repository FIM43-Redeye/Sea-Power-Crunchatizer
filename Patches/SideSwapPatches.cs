// Side-swap detection patches
// Applies/removes cheats when units transfer to/from player control

using HarmonyLib;
using JetBrains.Annotations;
using Sea_Power_Crunchatizer;
using SeaPower;
using SeaPowerCrunchatizer.Systems;
using SeaPowerCrunchatizer.Utilities;

namespace SeaPowerCrunchatizer.Patches
{
    /// <summary>
    /// Detects when units transfer between taskforces (sides) and applies/removes cheats accordingly.
    /// Uses Prefix to capture pre-transfer state and Postfix to compare and act.
    /// </summary>
    [HarmonyPatch(typeof(ObjectBase), "TransferToTaskforce",
        typeof(Taskforce), typeof(bool), typeof(bool), typeof(bool))]
    internal static class TransferToTaskforcePatch
    {
        /// <summary>
        /// Captures whether the unit was player-owned before transfer.
        /// </summary>
        [HarmonyPrefix]
        [UsedImplicitly]
        private static void Prefix(ObjectBase __instance, Taskforce newTaskforce, out bool __state)
        {
            // __state = was this unit player-owned before transfer?
            __state = PlayerUtils.IsPlayerUnit(__instance);

            PlayerUtils.LogIfSpam(
                $"SideSwap: '{__instance.name}' transferring. " +
                $"Was player: {__state}, " +
                $"New taskforce: {newTaskforce?.Side.ToString() ?? "null"}");
        }

        /// <summary>
        /// Compares pre/post transfer state and applies or removes cheats.
        /// </summary>
        [HarmonyPostfix]
        [UsedImplicitly]
        private static void Postfix(ObjectBase __instance, bool __state)
        {
            var wasPlayer = __state;
            var isNowPlayer = PlayerUtils.IsPlayerUnit(__instance);

            // No change in player ownership
            if (wasPlayer == isNowPlayer)
            {
                return;
            }

            if (isNowPlayer)
            {
                // Joined player side - apply cheats
                CrunchatizerCore.Log.LogInfo(
                    $"SideSwap: '{__instance.name}' joined player side - applying cheats");
                CheatApplicator.ApplyCheats(__instance);
            }
            else
            {
                // Left player side - remove cheats
                CrunchatizerCore.Log.LogInfo(
                    $"SideSwap: '{__instance.name}' left player side - removing cheats");
                CheatApplicator.RemoveCheats(__instance);
            }
        }
    }
}
