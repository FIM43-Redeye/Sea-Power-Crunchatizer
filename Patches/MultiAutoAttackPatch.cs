// Multi-target auto-attack transpiler patch
// Extracted from CrunchatizerCore.cs during refactoring

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Configuration;
using HarmonyLib;
using JetBrains.Annotations;
using Sea_Power_Crunchatizer;
using SeaPower;
using SeaPowerCrunchatizer.Config;

namespace SeaPowerCrunchatizer.Patches
{
    /// <summary>
    /// Uses a Harmony Transpiler to modify the AI's auto-attack logic.
    /// Finds the 'break;' statement that stops the AI from acquiring more than one target per frame,
    /// then injects code to skip this 'break;' for player units if the feature is enabled,
    /// allowing them to issue attack orders against all valid targets in range simultaneously.
    /// </summary>
    [HarmonyPatch]
    public static class MultiAutoAttackPatch
    {
        /// <summary>
        /// Preparer method called by Harmony before patching to determine the target method.
        /// This is the standard way to target private/internal types that can't be resolved at compile time.
        /// </summary>
        /// <returns>The MethodInfo of the target method, or null if not found.</returns>
        [UsedImplicitly]
        static MethodBase? TargetMethod()
        {
            var type = AccessTools.TypeByName("SeaPower.AI");
            if (type == null)
            {
                CrunchatizerCore.Log.LogError(
                    "MultiAutoAttackPatch: TargetMethod could not find type 'SeaPower.AI'. Patch will be skipped.");
                return null;
            }

            var method = AccessTools.Method(type, "AutoAttackOpponentInRange");
            if (method == null)
            {
                CrunchatizerCore.Log.LogError(
                    "MultiAutoAttackPatch: TargetMethod could not find method 'AutoAttackOpponentInRange'. Patch will be skipped.");
                return null;
            }

            CrunchatizerCore.Log.LogInfo(
                "MultiAutoAttackPatch: Successfully targeted method 'SeaPower.AI.AutoAttackOpponentInRange'.");
            return method;
        }

        /// <summary>
        /// Transpiler that modifies IL code to allow multi-target auto-attacks for player units.
        /// </summary>
        [HarmonyTranspiler]
        [UsedImplicitly]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
            ILGenerator ilGenerator, MethodBase original)
        {
            // --- Step 1: Get reflection info ---
            var aiType = original.DeclaringType;
            var baseObjectField = AccessTools.Field(aiType, "_baseObject");

            // Get ReactiveProperty<Taskforce> pattern accessors
            var unitTaskforcePropertyGetter = AccessTools.PropertyGetter(typeof(ObjectBase), "UnitTaskforce");
            var reactivePropertyTaskforceType = unitTaskforcePropertyGetter?.ReturnType;
            var reactivePropertyValueGetter = reactivePropertyTaskforceType != null
                ? AccessTools.PropertyGetter(reactivePropertyTaskforceType, "Value")
                : null;
            var sidePropertyGetter = AccessTools.PropertyGetter(typeof(Taskforce), "Side");

            // Get config field accessor - note: now uses CheatConfig
            var multiAutoAttackField = AccessTools.Field(typeof(CheatConfig), nameof(CheatConfig.MultiAutoAttack));
            var configEntryValueGetter = AccessTools.PropertyGetter(typeof(ConfigEntry<bool>), "Value");
            var insertEngageTaskMethod = AccessTools.Method(typeof(ObjectBase), "InsertEngageTask");

            // Validate all required reflection info
            if (baseObjectField == null || unitTaskforcePropertyGetter == null || reactivePropertyValueGetter == null ||
                sidePropertyGetter == null || multiAutoAttackField == null || configEntryValueGetter == null ||
                insertEngageTaskMethod == null)
            {
                CrunchatizerCore.Log.LogError(
                    "MultiAutoAttackPatch: Failed to get required reflection info. Patch failed.");

                if (unitTaskforcePropertyGetter == null)
                {
                    CrunchatizerCore.Log.LogWarning("-> 'UnitTaskforce' property getter not found on ObjectBase.");
                }
                if (reactivePropertyValueGetter == null)
                {
                    CrunchatizerCore.Log.LogWarning(
                        "-> 'Value' property getter not found on ReactiveProperty<Taskforce>.");
                }
                if (sidePropertyGetter == null)
                {
                    CrunchatizerCore.Log.LogWarning("-> 'Side' property getter not found on Taskforce.");
                }

                return instructions;
            }

            var codeInstructions = instructions.ToList();
            var code = new List<CodeInstruction>(codeInstructions);
            int breakInstructionIndex = -1;

            // --- Step 2: Find the target instruction ---
            int insertCallIndex = code.FindIndex(c => c.Calls(insertEngageTaskMethod));

            if (insertCallIndex != -1)
            {
                // Search forward for the 'leave' instruction that exits the loop's try block
                breakInstructionIndex = code.FindIndex(insertCallIndex,
                    c => c.opcode == OpCodes.Leave || c.opcode == OpCodes.Leave_S);
            }

            if (breakInstructionIndex == -1)
            {
                CrunchatizerCore.Log.LogError(
                    "MultiAutoAttackPatch: Could not find the target 'leave' instruction after InsertEngageTask. Patch failed.");
                return codeInstructions;
            }

            // --- Step 3: Define and attach labels ---
            object continueLabel = ilGenerator.DefineLabel();
            object originalBreakLabel = ilGenerator.DefineLabel();

            try
            {
                var labelsField = typeof(CodeInstruction).GetField("labels");
                if (labelsField == null)
                {
                    CrunchatizerCore.Log.LogError(
                        "MultiAutoAttackPatch: Critical error - Could not reflect the 'labels' property. Patch failed.");
                    return codeInstructions;
                }

                var labelsOfContinueInstruction = labelsField.GetValue(code[breakInstructionIndex + 1]);
                ((IList)labelsOfContinueInstruction).Add(continueLabel);

                var labelsOfBreakInstruction = labelsField.GetValue(code[breakInstructionIndex]);
                ((IList)labelsOfBreakInstruction).Add(originalBreakLabel);
            }
            catch (Exception ex)
            {
                CrunchatizerCore.Log.LogError(
                    $"MultiAutoAttackPatch: An exception occurred while adding labels via reflection: {ex}");
                return codeInstructions;
            }

            // --- Step 4: Create the new IL instructions to inject ---
            var instructionsToInsert = new List<CodeInstruction>
            {
                // if (!CheatConfig.MultiAutoAttack.Value) goto originalBreakLabel;
                new CodeInstruction(OpCodes.Ldsfld, multiAutoAttackField),
                new CodeInstruction(OpCodes.Callvirt, configEntryValueGetter),
                new CodeInstruction(OpCodes.Brfalse, originalBreakLabel),

                // if (this._baseObject.UnitTaskforce.Value.Side != Taskforce.TfType.Player) goto originalBreakLabel;
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, baseObjectField),
                new CodeInstruction(OpCodes.Callvirt, unitTaskforcePropertyGetter),
                new CodeInstruction(OpCodes.Callvirt, reactivePropertyValueGetter),
                new CodeInstruction(OpCodes.Callvirt, sidePropertyGetter),
                new CodeInstruction(OpCodes.Ldc_I4, (int)Taskforce.TfType.Player),
                new CodeInstruction(OpCodes.Ceq),
                new CodeInstruction(OpCodes.Brfalse, originalBreakLabel),

                // All checks passed - skip the break and continue the loop
                new CodeInstruction(OpCodes.Br, continueLabel),
            };

            // --- Step 5: Insert the new instructions ---
            code.InsertRange(breakInstructionIndex, instructionsToInsert);
            CrunchatizerCore.Log.LogInfo(
                "MultiAutoAttackPatch: Successfully applied transpiler to enable multi-target auto-attacks.");

            return code.AsEnumerable();
        }
    }
}
