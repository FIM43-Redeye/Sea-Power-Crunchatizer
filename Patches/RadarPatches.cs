// Radar horizon bypass patches
// Extracted from CrunchatizerCore.cs during refactoring

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using BepInEx;
using HarmonyLib;
using JetBrains.Annotations;
using Sea_Power_Crunchatizer;
using SeaPower;
using SeaPowerCrunchatizer.Config;
using SeaPowerCrunchatizer.Utilities;

namespace SeaPowerCrunchatizer.Patches
{
    /// <summary>
    /// A dynamic, global patcher that finds all calls to RadarCalculator.getRadarHorizon
    /// and replaces them with a patched version. It uses a strategy pattern to handle
    /// different calling contexts, caches scan results for fast startup, and generates a clean summary report.
    /// </summary>
    public static class RadarHorizonGlobalPatcher
    {
        /// <summary>
        /// The method we want to find calls to and replace.
        /// </summary>
        private static readonly MethodInfo OriginalHorizonMethod =
            AccessTools.Method(typeof(RadarCalculator), "getRadarHorizon");

        /// <summary>
        /// Our replacement method that adds player detection check.
        /// </summary>
        private static readonly MethodInfo PatchedHorizonMethod =
            AccessTools.Method(typeof(RadarHorizonGlobalPatcher), nameof(GetPatchedRadarHorizon));

        /// <summary>
        /// The generic transpiler we will apply to all callers.
        /// </summary>
        private static readonly HarmonyMethod Transpiler =
            new HarmonyMethod(typeof(RadarHorizonGlobalPatcher), nameof(UniversalHorizonTranspiler));

        /// <summary>
        /// A dictionary mapping a caller method's full name to a function that provides
        /// the IL instructions needed to load the 'detector' ObjectBase onto the stack.
        /// Returns null on error to signal the transpiler should abort.
        /// </summary>
        private static readonly Dictionary<string, Func<MethodBase, List<CodeInstruction>?>> PatchingStrategies =
            new Dictionary<string, Func<MethodBase, List<CodeInstruction>?>>();

        /// <summary>
        /// A set of methods that we have reviewed and decided not to patch.
        /// </summary>
        private static readonly HashSet<string> IgnoredCallers = new HashSet<string>();

        /// <summary>
        /// Static constructor to initialize patching strategies.
        /// </summary>
        static RadarHorizonGlobalPatcher()
        {
            // Strategy 1: The 'detector' is the 2nd argument (index 1) of the method.
            Func<MethodBase, List<CodeInstruction>> loadDetectorFromArg1 = _ => new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_1)
            };

            // Strategy 2: The 'detector' is the '_baseObject' field of the 'this' instance.
            Func<MethodBase, List<CodeInstruction>?> loadDetectorFromThisBaseObject = m =>
            {
                if (m.DeclaringType == null)
                {
                    CrunchatizerCore.Log.LogError(
                        $"[RadarHorizonPatcher] Strategy Error: Method '{m.Name}' has no declaring type.");
                    return null;
                }

                var baseObjectField = AccessTools.Field(m.DeclaringType, "_baseObject");
                if (baseObjectField == null)
                {
                    CrunchatizerCore.Log.LogError(
                        $"[RadarHorizonPatcher] Strategy Error: Could not find '_baseObject' field on type '{m.DeclaringType.FullName}' for method '{m.Name}'.");
                    return null;
                }

                return new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, baseObjectField)
                };
            };

            // Strategy 3: The 'detector' is the '_homeObject' field of the 'this' instance.
            Func<MethodBase, List<CodeInstruction>?> loadDetectorFromThisHomeObject = m =>
            {
                if (m.DeclaringType == null)
                {
                    CrunchatizerCore.Log.LogError(
                        $"[RadarHorizonPatcher] Strategy Error: Method '{m.Name}' has no declaring type.");
                    return null;
                }

                var baseObjectField = AccessTools.Field(m.DeclaringType, "_homeObject");
                if (baseObjectField == null)
                {
                    CrunchatizerCore.Log.LogError(
                        $"[RadarHorizonPatcher] Strategy Error: Could not find '_homeObject' field on type '{m.DeclaringType.FullName}' for method '{m.Name}'.");
                    return null;
                }

                return new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, baseObjectField)
                };
            };

            // Strategy 4: The 'detector' is the 'this' instance itself (inherits from ObjectBase).
            Func<MethodBase, List<CodeInstruction>> loadDetectorFromThis = _ => new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0)
            };

            // Register all strategies
            PatchingStrategies.Add("SeaPower.RadarCalculator.getSignalStrength", loadDetectorFromArg1);
            PatchingStrategies.Add("SeaPower.UnitSensorCache.UnitsHaveRadarlLOS", loadDetectorFromArg1);
            PatchingStrategies.Add("SeaPower.SensorSystemECM.OnFixedUpdate", loadDetectorFromThisBaseObject);
            // GetRange clamps a radar's detection distance to the horizon (Mathf.Min(..., radarHorizon, ...));
            // the detector is the sensor's owning unit (_baseObject), same as the other sensor-side callers.
            PatchingStrategies.Add("SeaPower.SensorSystemRadar.GetRange", loadDetectorFromThisBaseObject);
            PatchingStrategies.Add("SeaPower.Radar.IsDetectableTarget_Conditional", loadDetectorFromThisHomeObject);
            PatchingStrategies.Add("SeaPower.WeaponSystemHardpoint.OnUpdate", loadDetectorFromThisBaseObject);
            PatchingStrategies.Add("SeaPower.WeaponSystemLauncher.CalculateHorizon", loadDetectorFromThisBaseObject);
            PatchingStrategies.Add("SeaPower.Missile.CheckRadioConnection", loadDetectorFromThis);
            PatchingStrategies.Add("SeaPower.ObjectBase.CalculateAmmunitionHorizons", loadDetectorFromThis);
            PatchingStrategies.Add("SeaPower.ObjectBase.GetAmmunitionHorizons", loadDetectorFromThis);
            PatchingStrategies.Add("SeaPower.Missile.RadioCommandTargetIllumination", loadDetectorFromThis);

            // Define methods to explicitly ignore
            IgnoredCallers.Add("SeaPower.RadarCalculator.getSignalStrengthFromWeapon");
            IgnoredCallers.Add("SeaPower.SensorSystemVisual.runVisualScan");
            IgnoredCallers.Add("SeaPower.UnitSensorCache.UnitsHaveVisualLOS");
            IgnoredCallers.Add("SeaPower.WeaponSystem.CheckTargetInRangeOfTVSeeker");
            IgnoredCallers.Add("SeaPower.SensorSystemLaserDesignator.rangeTargetWithLaser");
            IgnoredCallers.Add("SeaPower.SensorSystemLaserDesignator.rangeTargetWithLaser_bkp");
            IgnoredCallers.Add("SeaPower.TVSeeker.isInRange");
            IgnoredCallers.Add("SeapowerUI.MapUnitViewModel.CommunicationResult");
            // These GetRange overrides query the VISUAL horizon (getRadarHorizon with visual: true).
            // The Ignore Radar Horizon cheat intentionally leaves visual detection alone, matching the
            // other visual/laser exclusions above. Only SensorSystemRadar.GetRange is patched.
            IgnoredCallers.Add("SeaPower.SensorSystemVisual.GetRange");
            IgnoredCallers.Add("SeaPower.SensorSystemLaserDesignator.GetRange");
        }

        /// <summary>
        /// Custom helper method that replaces the game's original horizon calculation.
        /// Returns max value for player units to bypass horizon checks.
        /// </summary>
        /// <param name="radarHeight">Height of the radar/sensor.</param>
        /// <param name="targetHeight">Height of the target.</param>
        /// <param name="visual">Whether this is a visual sensor check.</param>
        /// <param name="terrainHeight">Height of terrain.</param>
        /// <param name="detector">The detecting unit.</param>
        /// <returns>Maximum range for player units, original calculation otherwise.</returns>
        public static float GetPatchedRadarHorizon(float radarHeight, float targetHeight, bool visual,
            float terrainHeight, ObjectBase detector)
        {
            if (CheatConfig.IgnoreRadarHorizon.Value && PlayerUtils.IsPlayerUnit(detector))
            {
                PlayerUtils.LogIfExtremeSpam(
                    $"IgnoreRadarHorizon: Bypassing horizon check for player unit '{detector?.name ?? "Unknown"}'.");
                return float.MaxValue;
            }

            return RadarCalculator.getRadarHorizon(radarHeight, targetHeight, visual, terrainHeight);
        }

        /// <summary>
        /// Scans the game's assembly for all calls to getRadarHorizon, applies the universal transpiler,
        /// and prints a final summary report. Call this from the plugin's Awake() method.
        /// </summary>
        /// <param name="harmony">The Harmony instance to use for patching.</param>
        public static void Apply(Harmony harmony)
        {
            if (OriginalHorizonMethod == null || PatchedHorizonMethod == null)
            {
                CrunchatizerCore.Log.LogError(
                    "[RadarHorizonPatcher] Critical error: Could not reflect original or patched horizon methods. Aborting.");
                return;
            }

            // Caching setup
            const string cacheFileName = "Crunchatizer.RadarHorizon.cache";
            string cacheFilePath = Path.Combine(Paths.CachePath, cacheFileName);
            var gameAssembly = typeof(RadarCalculator).Assembly;
            // The leading token is a cache-format version: bump it whenever the way we
            // key/store callers changes, so stale caches from older mod versions are
            // discarded instead of silently masking the change. (v2: caller keys now
            // include the declaring type, so virtual overrides no longer collide.)
            string cacheKey = $"v2|{gameAssembly.FullName}";

            var callers = new Dictionary<string, MethodBase>();

            // Try to load from cache first
            if (File.Exists(cacheFilePath))
            {
                var lines = File.ReadAllLines(cacheFilePath);
                if (lines.Length > 1 && lines[0] == cacheKey)
                {
                    CrunchatizerCore.Log.LogInfo(
                        "[RadarHorizonPatcher] Valid cache found. Loading callers from cache...");
                    for (int i = 1; i < lines.Length; i++)
                    {
                        var method = AccessTools.Method(lines[i]);
                        if (method != null)
                        {
                            // Key by the declaring-type-qualified signature so virtual
                            // overrides (e.g. each SensorSystem*.GetRange) stay distinct.
                            callers[GetParsableMethodSignature(method)] = method;
                        }
                        else
                        {
                            CrunchatizerCore.Log.LogWarning(
                                $"[RadarHorizonPatcher] Cache entry '{lines[i]}' could not be resolved. Invalidating cache.");
                            callers.Clear();
                            break;
                        }
                    }

                    if (callers.Any())
                    {
                        CrunchatizerCore.Log.LogInfo(
                            $"[RadarHorizonPatcher] Successfully loaded {callers.Count} callers from cache.");
                    }
                }
                else
                {
                    CrunchatizerCore.Log.LogInfo(
                        "[RadarHorizonPatcher] Cache is stale (game update?) or invalid. Re-scanning...");
                }
            }

            // If cache failed, perform full scan
            if (!callers.Any())
            {
                CrunchatizerCore.Log.LogInfo("[RadarHorizonPatcher] Starting scan for getRadarHorizon callers...");
                CrunchatizerCore.Log.LogInfo(
                    $"[RadarHorizonPatcher] Scanning assembly '{gameAssembly.FullName}'. This may take a moment...");

                try
                {
                    var types = gameAssembly.GetTypes();
                    foreach (var type in types)
                    {
                        var methodsToScan = type.GetMethods(AccessTools.all)
                            .Cast<MethodBase>()
                            .Concat(type.GetConstructors(AccessTools.all));

                        foreach (var method in methodsToScan)
                        {
                            if (method.GetMethodBody() == null)
                            {
                                continue;
                            }

                            try
                            {
                                var instructions = PatchProcessor.GetOriginalInstructions(method);
                                foreach (var instruction in instructions)
                                {
                                    if (instruction.Calls(OriginalHorizonMethod))
                                    {
                                        // Declaring-type-qualified so virtual overrides that share
                                        // a signature (the SensorSystem*.GetRange family) don't
                                        // collapse into a single entry and get dropped.
                                        var signature = GetParsableMethodSignature(method);
                                        if (!callers.ContainsKey(signature))
                                        {
                                            callers.Add(signature, method);
                                        }
                                        break;
                                    }
                                }
                            }
                            catch
                            {
                                // Some methods can't be read, safe to ignore
                            }
                        }
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    CrunchatizerCore.Log.LogError(
                        $"[RadarHorizonPatcher] ReflectionTypeLoadException during scan. Errors: {string.Join(", ", ex.LoaderExceptions.Select(e => e.Message))}");
                }
                catch (Exception ex)
                {
                    CrunchatizerCore.Log.LogError(
                        $"[RadarHorizonPatcher] Unexpected error during assembly scan: {ex}");
                }

                // Save scan results to cache
                if (callers.Any())
                {
                    CrunchatizerCore.Log.LogInfo(
                        $"[RadarHorizonPatcher] Scan complete. Saving {callers.Count} found callers to cache...");
                    try
                    {
                        var linesToSave = new List<string> { cacheKey };
                        linesToSave.AddRange(callers.Values.Select(GetParsableMethodSignature));

                        Directory.CreateDirectory(Paths.CachePath);
                        File.WriteAllLines(cacheFilePath, linesToSave);
                        CrunchatizerCore.Log.LogInfo($"[RadarHorizonPatcher] Cache saved to '{cacheFilePath}'.");
                    }
                    catch (Exception ex)
                    {
                        CrunchatizerCore.Log.LogError($"[RadarHorizonPatcher] Failed to write cache file: {ex}");
                    }
                }
            }

            // Apply patches and generate report
            var patchedCallers = new List<string>();
            var failedCallers = new List<string>();
            var unhandledCallers = new HashSet<string>();
            var ignoredCallersList = new List<string>();

            foreach (var caller in callers.Values)
            {
                var callerName = caller.DeclaringType != null
                    ? $"{caller.DeclaringType.FullName}.{caller.Name}"
                    : caller.Name;
                var callerSignature = caller.ToString();

                if (IgnoredCallers.Contains(callerName))
                {
                    ignoredCallersList.Add(callerSignature);
                    continue;
                }

                if (PatchingStrategies.ContainsKey(callerName))
                {
                    try
                    {
                        harmony.Patch(caller, transpiler: Transpiler);
                        patchedCallers.Add(callerSignature);
                    }
                    catch (Exception ex)
                    {
                        failedCallers.Add($"{callerSignature} (Reason: {ex.Message})");
                    }
                }
                else
                {
                    unhandledCallers.Add(callerSignature);
                }
            }

            // Generate summary report
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("========== Radar Horizon Patcher Summary ==========");
            sb.AppendLine($"Scan found {callers.Count} unique methods calling getRadarHorizon.");
            sb.AppendLine();

            sb.AppendLine($"--- Patched ({patchedCallers.Count}) ---");
            patchedCallers.OrderBy(s => s).ToList().ForEach(s => sb.AppendLine($"[+] {s}"));
            sb.AppendLine();

            sb.AppendLine($"--- Unhandled ({unhandledCallers.Count}) ---");
            if (unhandledCallers.Any())
            {
                unhandledCallers.OrderBy(s => s).ToList().ForEach(s => sb.AppendLine($"[?] {s}"));
            }
            else
            {
                sb.AppendLine("(None)");
            }
            sb.AppendLine();

            sb.AppendLine($"--- Failed ({failedCallers.Count}) ---");
            if (failedCallers.Any())
            {
                failedCallers.OrderBy(s => s).ToList().ForEach(s => sb.AppendLine($"[!] {s}"));
            }
            else
            {
                sb.AppendLine("(None)");
            }
            sb.AppendLine();

            sb.AppendLine($"--- Ignored ({ignoredCallersList.Count}) ---");
            if (ignoredCallersList.Any())
            {
                ignoredCallersList.OrderBy(s => s).ToList().ForEach(s => sb.AppendLine($"[-] {s}"));
            }
            else
            {
                sb.AppendLine("(None)");
            }

            sb.AppendLine("=================================================");

            if (unhandledCallers.Any() || failedCallers.Any())
            {
                CrunchatizerCore.Log.LogWarning(sb.ToString());
                CrunchatizerCore.Log.LogWarning(
                    "[RadarHorizonPatcher] Found unhandled or failed patches. Please review the summary above.");
            }
            else
            {
                CrunchatizerCore.Log.LogInfo(sb.ToString());
                CrunchatizerCore.Log.LogInfo("[RadarHorizonPatcher] All found callers were handled successfully.");
            }
        }

        /// <summary>
        /// Creates a string representation of a method that is reliably parsable by AccessTools.Method(string).
        /// </summary>
        private static string GetParsableMethodSignature(MethodBase method)
        {
            var typeName = method.DeclaringType?.FullName ?? "UnknownType";
            var methodName = method.Name;

            if (method.IsConstructor)
            {
                methodName = method.IsStatic ? ".cctor" : ".ctor";
            }

            var parameters = method.GetParameters()
                .Select(p => p.ParameterType.FullName)
                .ToArray();

            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i] != null && parameters[i].EndsWith("&"))
                {
                    parameters[i] = parameters[i].TrimEnd('&') + "@";
                }
            }

            return $"{typeName}:{methodName}({string.Join(",", parameters)})";
        }

        /// <summary>
        /// Universal transpiler applied to every method we want to patch.
        /// Looks up the correct strategy and injects code to load the detector ObjectBase.
        /// </summary>
        [UsedImplicitly]
        private static IEnumerable<CodeInstruction> UniversalHorizonTranspiler(
            IEnumerable<CodeInstruction> instructions,
            MethodBase originalMethod)
        {
            var callerName = originalMethod.DeclaringType != null
                ? $"{originalMethod.DeclaringType.FullName}.{originalMethod.Name}"
                : originalMethod.Name;

            if (!PatchingStrategies.TryGetValue(callerName, out var strategyBuilder))
            {
                CrunchatizerCore.Log.LogError(
                    $"[RadarHorizonPatcher] Transpiler called on un-configured method '{callerName}'. Returning original code.");
                return instructions;
            }

            var loadDetectorInstructions = strategyBuilder(originalMethod);
            if (loadDetectorInstructions == null)
            {
                CrunchatizerCore.Log.LogError(
                    $"[RadarHorizonPatcher] Strategy for '{callerName}' failed to generate instructions. Aborting patch.");
                return instructions;
            }

            var code = new List<CodeInstruction>(instructions);

            // Iterate backwards to safely insert without messing up indices
            for (var i = code.Count - 1; i >= 0; i--)
            {
                if (code[i].Calls(OriginalHorizonMethod))
                {
                    // Insert instructions to load 'detector' before the call
                    code.InsertRange(i, loadDetectorInstructions);

                    // Update call target
                    var callInstructionIndex = i + loadDetectorInstructions.Count;
                    code[callInstructionIndex].operand = PatchedHorizonMethod;
                }
            }

            return code.AsEnumerable();
        }
    }
}
