// Sensor system patches
// Extracted from CrunchatizerCore.cs during refactoring

using System;
using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using SeaPower;
using SeaPowerCrunchatizer.Config;
using SeaPowerCrunchatizer.Constants;
using SeaPowerCrunchatizer.Systems;
using SeaPowerCrunchatizer.Utilities;
using UnityEngine;

namespace SeaPowerCrunchatizer.Patches
{
    /// <summary>
    /// Contains Harmony patches for sensor system modifications.
    /// Includes broken sensor parameters for ESM, ECM, Radar, Sonar, and Visual systems,
    /// as well as fire control and crew skill modifications.
    /// </summary>
    internal static class SensorPatches
    {
        // Patch classes are defined below as nested types
    }

    /// <summary>
    /// Modifies base sensor system properties for broken fire control.
    /// </summary>
    [HarmonyPatch(typeof(SensorSystem), "LoadFromInI", typeof(IniHandler), typeof(string), typeof(ObjectBaseParameters),
        typeof(string))]
    internal static class ModifySensorSystemAtLoad
    {
        /// <summary>
        /// Postfix that applies broken fire control to sensor systems.
        /// </summary>
        [HarmonyPostfix]
        [UsedImplicitly]
        private static void Postfix(SensorSystem __instance)
        {
            if (!PlayerUtils.IsPlayerSensor(__instance))
            {
                return;
            }

            if (CheatConfig.BrokenFireControl.Value &&
                (__instance._targetChannels != 0 || __instance._weaponChannels != 0))
            {
                // Cache original values before modification
                CheatStateTracker.RecordSensorParams(
                    __instance._baseObject, __instance,
                    new SensorSystemOriginalState
                    {
                        TargetChannels = __instance._targetChannels,
                        WeaponChannels = __instance._weaponChannels,
                        VerticalViewArc = __instance._verticalViewArc,
                        HorizontalViewArc = __instance._horizontalViewArc
                    });

                PlayerUtils.LogIfSpam(
                    $"SensorMod: Applying BrokenFireControl to '{__instance._systemName}' on '{__instance._baseObject.name}'.");

                __instance._targetChannels = GameConstants.InfiniteChannels;
                __instance._weaponChannels = GameConstants.InfiniteChannels;
                __instance._verticalViewArc = GameConstants.FullVerticalArc;
                __instance._horizontalViewArc = GameConstants.FullHorizontalArc;
            }
        }
    }

    /// <summary>
    /// Modifies ECM sensor systems with enhanced parameters.
    /// </summary>
    [HarmonyPatch(typeof(SensorSystemECM), "LoadFromInI", typeof(IniHandler), typeof(string),
        typeof(ObjectBaseParameters), typeof(string))]
    internal static class ModifySensorECMAtLoad
    {
        /// <summary>
        /// Postfix that applies broken sensor parameters to ECM systems.
        /// </summary>
        [HarmonyPostfix]
        [UsedImplicitly]
        private static void Postfix(SensorSystemECM __instance)
        {
            if (!CheatConfig.BrokenSensorParams.Value.HasFlag(BrokenSensorTypes.ECM) ||
                !PlayerUtils.IsPlayerSensor(__instance))
            {
                return;
            }

            // Cache original values before modification
            CheatStateTracker.RecordSensorParams(
                __instance._baseObject, __instance,
                new ECMOriginalState
                {
                    Frequencies = new List<Globals.Frequency>(__instance._ecm._ep._frequencies),
                    WaveLengths = new List<float>(__instance._ecm._ep._waveLengths),
                    JamConeFov = __instance._ecm._ep._jamConeFov,
                    JamChance = __instance._ecm._ep._jamChance
                });

            PlayerUtils.LogIfSpam(
                $"SensorMod: Applying BrokenSensorParams (ECM) to '{__instance._systemName}' on '{__instance._baseObject.name}'.");

            // Reset frequencies for full coverage
            __instance._ecm._ep._frequencies = new List<Globals.Frequency>();
            __instance._ecm._ep._waveLengths = new List<float>();
            __instance._ecm._ep.loadFrequenciesForECM(new[] { "all" });

            __instance._ecm._ep._jamConeFov = GameConstants.FullCircleFov;
            __instance._ecm._ep._jamChance *= 2f;
        }
    }

    /// <summary>
    /// Modifies ESM sensor systems with enhanced parameters.
    /// </summary>
    [HarmonyPatch(typeof(SensorSystemESM), "LoadFromInI", typeof(IniHandler), typeof(string),
        typeof(ObjectBaseParameters), typeof(string))]
    internal static class ModifySensorESMAtLoad
    {
        /// <summary>
        /// Postfix that applies broken sensor parameters to ESM systems.
        /// </summary>
        [HarmonyPostfix]
        [UsedImplicitly]
        private static void Postfix(SensorSystemESM __instance)
        {
            if (!CheatConfig.BrokenSensorParams.Value.HasFlag(BrokenSensorTypes.ESM) ||
                !PlayerUtils.IsPlayerSensor(__instance))
            {
                return;
            }

            // Cache original values before modification
            CheatStateTracker.RecordSensorParams(
                __instance._baseObject, __instance,
                new ESMOriginalState
                {
                    Gain = __instance._esm._ep._gain,
                    GainFactor = __instance._esm._ep._gainFactor,
                    AngularResolutionDegrees = __instance._esm._ep._angularResolutionDegrees,
                    Frequencies = new List<Globals.Frequency>(__instance._esm._ep._frequencies),
                    HasDataLink = __instance._esm._ep._hasDataLink,
                    IdentificationRate = __instance._esm._ep._identificationRate
                });

            PlayerUtils.LogIfSpam(
                $"SensorMod: Applying BrokenSensorParams (ESM) to '{__instance._systemName}' on '{__instance._baseObject.name}'.");

            __instance._esm._ep._gain += GameConstants.GainBoostDb;
            __instance._esm._ep._gainFactor = Mathf.Pow(10f, __instance._esm._ep._gain / 10f);
            __instance._esm._ep._angularResolutionDegrees /= 10f;
            __instance._esm._ep._frequencies =
                new List<Globals.Frequency>((Globals.Frequency[])Enum.GetValues(typeof(Globals.Frequency)));
            __instance._esm._ep._hasDataLink = true;
            __instance._esm._ep._identificationRate *= 2f;
        }
    }

    /// <summary>
    /// Modifies radar sensor systems with enhanced parameters.
    /// </summary>
    [HarmonyPatch(typeof(SensorSystemRadar), "LoadFromInI", typeof(IniHandler), typeof(string),
        typeof(ObjectBaseParameters), typeof(string))]
    internal static class ModifySensorRadarAtLoad
    {
        /// <summary>
        /// Postfix that applies broken sensor parameters to radar systems.
        /// </summary>
        [HarmonyPostfix]
        [UsedImplicitly]
        private static void Postfix(SensorSystemRadar __instance)
        {
            if (!CheatConfig.BrokenSensorParams.Value.HasFlag(BrokenSensorTypes.Radar) ||
                !PlayerUtils.IsPlayerSensor(__instance))
            {
                return;
            }

            // Cache original values before modification
            CheatStateTracker.RecordSensorParams(
                __instance._baseObject, __instance,
                new RadarOriginalState
                {
                    Role = __instance._radar._rp._role,
                    HasDataLink = __instance._radar._rp._hasDataLink,
                    CanDetectLandTargets = __instance._radar._rp._canDetectLandTargets,
                    CanDetectPeriscope = __instance._radar._rp._canDetectPeriscope,
                    MinAltitude = __instance._radar._rp._minAltitude,
                    MaxAltitude = __instance._radar._rp._maxAltitude,
                    MinRange = __instance._radar._rp._minRange,
                    MaxRange = __instance._radar._rp._maxRange,
                    LookDownMultiplier = __instance._lookDownMultiplier,
                    LookDownRange = __instance._lookDownRange
                });

            PlayerUtils.LogIfSpam(
                $"SensorMod: Applying BrokenSensorParams (Radar) to '{__instance._systemName}' on '{__instance._baseObject.name}'.");

            // __instance._radar._rp._role = RadarParameters.Role.SurfaceAndAir;
            __instance._radar._rp._hasDataLink = true;
            __instance._radar._rp._canDetectLandTargets = true;
            __instance._radar._rp._canDetectPeriscope = true;
            __instance._radar._rp._minAltitude = 0;
            __instance._radar._rp._maxAltitude = float.MaxValue;
            __instance._radar._rp._minRange = 0;
            __instance._radar._rp._maxRange = float.MaxValue;
            __instance._lookDownMultiplier = 1f;
            __instance._lookDownRange = float.MaxValue;
        }
    }

    /// <summary>
    /// Modifies sonar sensor systems with enhanced parameters.
    /// </summary>
    [HarmonyPatch(typeof(SensorSystemSonar), "LoadFromInI", typeof(IniHandler), typeof(string),
        typeof(ObjectBaseParameters), typeof(string))]
    internal static class ModifySensorSonarAtLoad
    {
        /// <summary>
        /// Postfix that applies broken sensor parameters to sonar systems.
        /// </summary>
        [HarmonyPostfix]
        [UsedImplicitly]
        private static void Postfix(SensorSystemSonar __instance)
        {
            if (!CheatConfig.BrokenSensorParams.Value.HasFlag(BrokenSensorTypes.Sonar) ||
                !PlayerUtils.IsPlayerSensor(__instance))
            {
                return;
            }

            // Cache original values before modification
            CheatStateTracker.RecordSensorParams(
                __instance._baseObject, __instance,
                new SonarOriginalState
                {
                    Gain = __instance._sonar._sp._gain,
                    ActiveGain = __instance._sonar._sp._activeGain,
                    HasDataLink = __instance._sonar._sp._hasDataLink,
                    ActiveRangeInKm = __instance._sonar._sp._activeRangeInKm,
                    AngularResolutionDegrees = __instance._sonar._sp._angularResolutionDegrees
                });

            PlayerUtils.LogIfSpam(
                $"SensorMod: Applying BrokenSensorParams (Sonar) to '{__instance._systemName}' on '{__instance._baseObject.name}'.");

            __instance._sonar._sp._gain += GameConstants.GainBoostDb;
            __instance._sonar._sp._activeGain += GameConstants.GainBoostDb;
            __instance._sonar._sp._hasDataLink = true;
            __instance._sonar._sp._activeRangeInKm = float.MaxValue;
            __instance._sonar._sp._angularResolutionDegrees /= 10f;
        }
    }

    /// <summary>
    /// Modifies visual sensor systems with enhanced parameters.
    /// </summary>
    [HarmonyPatch(typeof(SensorSystemVisual), "LoadFromInI", typeof(IniHandler), typeof(string),
        typeof(ObjectBaseParameters), typeof(string))]
    internal static class ModifySensorVisualAtLoad
    {
        /// <summary>
        /// Postfix that applies broken sensor parameters to visual systems.
        /// </summary>
        [HarmonyPostfix]
        [UsedImplicitly]
        private static void Postfix(SensorSystemVisual __instance)
        {
            if (!CheatConfig.BrokenSensorParams.Value.HasFlag(BrokenSensorTypes.Visual) ||
                !PlayerUtils.IsPlayerSensor(__instance))
            {
                return;
            }

            // Cache original values before modification
            CheatStateTracker.RecordSensorParams(
                __instance._baseObject, __instance,
                new VisualOriginalState
                {
                    VidRangeMultiplier = __instance._vidRangeMultiplier,
                    LookDownMultiplier = __instance._lookDownMultiplier,
                    MaxRangeMultiplier = __instance._maxRangeMultiplier,
                    NightVisionLevel = __instance._nightVisionLevel
                });

            PlayerUtils.LogIfSpam(
                $"SensorMod: Applying BrokenSensorParams (Visual) to '{__instance._systemName}' on '{__instance._baseObject.name}'.");

            __instance._vidRangeMultiplier *= GameConstants.EnhancedVisualRangeMultiplier;
            __instance._lookDownMultiplier = 1f;
            __instance._maxRangeMultiplier *= GameConstants.EnhancedVisualRangeMultiplier;
            __instance._nightVisionLevel = 1f;
        }
    }

    /// <summary>
    /// Bypasses speed restrictions for towed sensors (towed arrays, VDS, dipping sonars, etc.)
    /// so they can deploy and operate at any speed.
    /// </summary>
    [HarmonyPatch(typeof(BaseSystem), nameof(BaseSystem.isTowedSystemSpeedWithinDeployConditions))]
    internal static class TowedSensorAnySpeedPatch
    {
        /// <summary>
        /// Prefix that returns true for player units, bypassing the speed check entirely.
        /// </summary>
        [HarmonyPrefix]
        [UsedImplicitly]
        private static bool Prefix(BaseSystem __instance, ObjectBase baseObject, ref bool __result)
        {
            // Only apply if enabled and this is a player unit
            if (!CheatConfig.TowedSensorAnySpeed.Value || !PlayerUtils.IsPlayerUnit(baseObject))
            {
                return true; // Run original method
            }

            // Bypass speed check - always return true for player towed sensors
            __result = true;
            return false; // Skip original method
        }
    }

    /// <summary>
    /// Sets crew skill to Ultra for player units.
    /// </summary>
    [HarmonyPatch(typeof(SceneCreator), "SetAdditionalParameters", typeof(IniHandler), typeof(ObjectBase),
        typeof(SharedUnitData))]
    internal static class ModifyUnitWhenSettingAdditionalParameters
    {
        /// <summary>
        /// Postfix that sets crew skill to Ultra for player units.
        /// </summary>
        [HarmonyPostfix]
        [UsedImplicitly]
        private static void Postfix(ref ObjectBase unit, ref SharedUnitData unitData)
        {
            if (CheatConfig.UltraCrew.Value && PlayerUtils.IsPlayerUnit(unit))
            {
                PlayerUtils.LogIfSpam($"CrewMod: Setting crew skill to Ultra for '{unit.name}'.");
                unitData._crewlSkill = ObjectBase.CrewSkill.Ultra;
                unit._crew.SetCrewSkill(unitData._crewlSkill);
            }
        }
    }
}
