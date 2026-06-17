// Weapon system patches
// Extracted from CrunchatizerCore.cs during refactoring

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using JetBrains.Annotations;
using Sea_Power_Crunchatizer;
using SeaPower;
using SeaPowerCrunchatizer.Config;
using SeaPowerCrunchatizer.Constants;
using SeaPowerCrunchatizer.Systems;
using SeaPowerCrunchatizer.Utilities;
using UnityEngine;

namespace SeaPowerCrunchatizer.Patches
{
    /// <summary>
    /// Modifies ammunition properties when loaded via constructor.
    /// Applies terrain following, weapon enhancement, and range multipliers.
    /// </summary>
    [HarmonyPatch(typeof(Ammunition), MethodType.Constructor, typeof(string), typeof(int), typeof(WeaponSystem))]
    internal static class ModifyAmmunitionAtLoadConstructor
    {
        /// <summary>
        /// Postfix that applies various ammunition enhancements for player weapons.
        /// </summary>
        [HarmonyPostfix]
        [UsedImplicitly]
        private static void Postfix(Ammunition __instance, WeaponSystem? associatedWeaponSystem)
        {
            if (associatedWeaponSystem == null || !PlayerUtils.IsPlayerWeapon(associatedWeaponSystem))
            {
                return;
            }

            // Only process certain ammunition types
            switch (__instance._ap._type)
            {
                case Ammunition.Type.Missile:
                case Ammunition.Type.RBU:
                case Ammunition.Type.ASROC:
                case Ammunition.Type.Torpedo:
                case Ammunition.Type.AerialRocket:
                case Ammunition.Type.MOSS:
                    break;
                default:
                    return;
            }

            if (__instance._ap == null)
            {
                PlayerUtils.WarnIfSpam(
                    $"AmmoMod: AmmunitionParameters (_ap) is null for ammo created by '{associatedWeaponSystem._name ?? "Unknown"}'. Cannot modify.");
                return;
            }

            // Cache original parameters before modification (for side-swap restoration)
            var clonedParams = ParameterCloner.ShallowClone(__instance._ap);
            if (clonedParams != null)
            {
                CheatStateTracker.RecordAmmoParams(associatedWeaponSystem._baseObject, __instance, clonedParams);
            }

            if (CheatConfig.ForceTerrainFollowing.Value)
            {
                ApplyTerrainFollowing(__instance);
            }

            if (CheatConfig.EnhanceMissileFeatures.Value)
            {
                EnhanceWeapon(__instance);
            }

            ApplyRangeMultiplier(__instance, CheatConfig.WeaponRangeMult.Value);

            if (CheatConfig.InfiniteMissileBurnTime.Value != InfiniteBurnMode.Off &&
                __instance._ap._type == Ammunition.Type.Missile)
            {
                ApplyInfiniteBurnTime(__instance, CheatConfig.InfiniteMissileBurnTime.Value);
            }
        }

        /// <summary>
        /// Makes a missile's motor burn for effectively the whole flight, so it powers to
        /// maximum range instead of coasting down and stall-destructing once the motor cuts out,
        /// and extends its flight-time cap so that endless burn actually translates into longer
        /// kinematic range (range-to-engage). Only full-kinematics missiles model motor burn and
        /// have their range simulated; legacy/cruise (KinematicsLevel.None) missiles ignore thrust
        /// and use a static launch range, so they are skipped.
        /// </summary>
        private static void ApplyInfiniteBurnTime(Ammunition ammo, InfiniteBurnMode mode)
        {
            var ap = ammo._ap;
            if (ap.Kinematics != AmmunitionParameters.KinematicsLevel.Full)
            {
                return;
            }

            var (accelerationTime, sustainerBurnTime) = BurnTimeMath.ApplyInfiniteBurn(
                ap._accelerationTime, ap._sustainerBurnTime, ap._sustainerBurnAcceleration);

            ap._accelerationTime = accelerationTime;
            ap._sustainerBurnTime = sustainerBurnTime;

            // The kinematic range simulation only runs for the missile's lifetime, so a sustained
            // motor only buys longer range if the missile also lives longer. Proportional mode
            // multiplies each missile's flight time; Fixed mode raises it to a flat floor.
            ap._maxFlightTime = BurnTimeMath.ExtendFlightTime(
                ap._maxFlightTime, proportional: mode == InfiniteBurnMode.Proportional);

            PlayerUtils.LogIfSpam(
                $"AmmoMod: Infinite burn time ({mode}) for missile '{ap._displayedName}' " +
                $"(boost {accelerationTime}s, sustainer {sustainerBurnTime}s, maxFlight {ap._maxFlightTime}s).");
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
                        PlayerUtils.LogIfSpam($"AmmoMod: Forced Terrain Following for '{ammo._ap._displayedName}'.");
                    }
                    break;
            }
        }

        private static void EnhanceWeapon(Ammunition ammo)
        {
            PlayerUtils.LogIfSpam($"AmmoMod: Enhancing weapon '{ammo._ap._displayedName}' of type {ammo._ap._type}.");

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
                case Ammunition.Type.RBU:
                    EnhanceRBU(ammo);
                    break;
            }
        }

        private static void EnhanceMissile(Ammunition ammo)
        {
            PlayerUtils.LogIfSpam($"AmmoMod: Enhancing missile '{ammo._ap._displayedName}'.");

            ammo._ap._requiresWarmUp = false;
            ammo._ap._targetMemory = true;
            ammo._ap._inertialGuidance = true;
            ammo._ap._hasESM = true;
            ammo._ap._isRearAspectOnly = false;
            ammo._ap._nightVisionLevel = 1f;
            ammo._ap._maxDepthUnity = 65536f;
            ammo._ap._minDepthUnity = 0f;
            ammo._ap._launchAltitudesInUnity.x = 0f;
            ammo._ap._launchAltitudesInUnity.y = 65536f;

            if (ammo._ap._targetType != Ammunition.Target.AAW && ammo._ap._secondaryTargetType != Ammunition.Target.AAW)
            {
                ammo._ap._seekerGimbalFOV = GameConstants.FullGimbalFov;
                ammo._ap._smartFuse = true;
            }

            ammo._ap._canNotAttackTypes.Clear();

            if (ammo._ap._midCourseCorrection == AmmunitionParameters.MidCourseCorrection.None &&
                ammo._ap._guidanceType != AmmunitionParameters.GuidanceType.SemiActiveRadarHoming)
            {
                ammo._ap._midCourseCorrection = AmmunitionParameters.MidCourseCorrection.WireGuided;
            }

            ammo._ap._sharedSensorLink = true;

            if (ammo._ap._maxGroupSize > 1)
            {
                ammo._ap._maxGroupSize = int.MaxValue;
            }

            ammo._ap._secondaryPassiveRadarGuidanceType = AmmunitionParameters.SecondaryPassiveRadarGuidanceType.Full;

            foreach (var freq in (Globals.Frequency[])Enum.GetValues(typeof(Globals.Frequency)))
            {
                if (freq != Globals.Frequency.Undefined && !ammo._ap._passiveRadarHomingFrequencies.Contains(freq))
                {
                    ammo._ap._passiveRadarHomingFrequencies.Add(freq);
                }
            }

            if (ammo._ap._targetType == Ammunition.Target.ASuW || ammo._ap._secondaryTargetType == Ammunition.Target.ASuW)
            {
                ammo._ap._landAttack.Clear();
                ammo._ap._landAttack.Add(Ammunition.LandAttack.Installation);
                ammo._ap._landAttack.Add(Ammunition.LandAttack.Mobile);

                if (ammo._ap._landAttackGuidanceType == AmmunitionParameters.GuidanceType.None &&
                    ammo._ap._guidanceType != AmmunitionParameters.GuidanceType.None)
                {
                    ammo._ap._landAttackGuidanceType = ammo._ap._guidanceType;
                }

                var minCep = Mathf.Min(new[]
                {
                    ammo._ap._circularErrorRadius,
                    ammo._ap._circularErrorRadiusInstallation,
                    ammo._ap._circularErrorRadiusLarge,
                    ammo._ap._circularErrorRadiusMobileUnit
                });

                ammo._ap._circularErrorRadius = minCep;
                ammo._ap._circularErrorRadiusInstallation = minCep;
                ammo._ap._circularErrorRadiusLarge = minCep;
                ammo._ap._circularErrorRadiusMobileUnit = minCep;
            }
        }

        private static void EnhanceTorpedo(Ammunition ammo)
        {
            PlayerUtils.LogIfSpam($"AmmoMod: Enhancing torpedo '{ammo._ap._displayedName}'.");

            if (ammo._ap._guidanceType != AmmunitionParameters.GuidanceType.WakeHoming)
            {
                ammo._ap._midCourseCorrection = AmmunitionParameters.MidCourseCorrection.WireGuided;
            }

            ammo._ap._seekerGimbalFOV = GameConstants.FullGimbalFov;
            ammo._ap._targetMemory = true;
            ammo._ap._inertialGuidance = true;
            ammo._ap._maxDepthUnity = 65536f;
            ammo._ap._minDepthUnity = 0f;
            ammo._ap._launchAltitudesInUnity.x = 0f;
            ammo._ap._launchAltitudesInUnity.y = 65536f;
            ammo._ap._smartFuse = true;
        }

        private static void EnhanceBomb(Ammunition ammo)
        {
            PlayerUtils.LogIfSpam($"AmmoMod: Enhancing bomb '{ammo._ap._displayedName}'.");

            ammo._ap._requiresWarmUp = false;
            ammo._ap._targetMemory = true;
            ammo._ap._inertialGuidance = true;
            ammo._ap._hasESM = true;
            ammo._ap._isRearAspectOnly = false;
            ammo._ap._sharedSensorLink = true;
            ammo._ap._nightVisionLevel = 1f;
            ammo._ap._maxDepthUnity = 65536f;
            ammo._ap._minDepthUnity = 0f;
            ammo._ap._launchAltitudesInUnity.x = 0f;
            ammo._ap._launchAltitudesInUnity.y = 65536f;
            ammo._ap._seekerGimbalFOV = GameConstants.FullGimbalFov;
            ammo._ap._secondaryPassiveRadarGuidanceType = AmmunitionParameters.SecondaryPassiveRadarGuidanceType.Full;

            foreach (var freq in (Globals.Frequency[])Enum.GetValues(typeof(Globals.Frequency)))
            {
                if (freq != Globals.Frequency.Undefined && !ammo._ap._passiveRadarHomingFrequencies.Contains(freq))
                {
                    ammo._ap._passiveRadarHomingFrequencies.Add(freq);
                }
            }
        }

        private static void EnhanceRBU(Ammunition ammo)
        {
            PlayerUtils.LogIfSpam($"AmmoMod: Enhancing RBU '{ammo._ap._displayedName}'.");

            ammo._ap._antiTorpedo = true;

            if (ammo._ap._secondaryTargetType == Ammunition.Target.UNKNOWN)
            {
                switch (ammo._ap._targetType)
                {
                    case Ammunition.Target.ASuW:
                        ammo._ap._secondaryTargetType = Ammunition.Target.ASW;
                        break;
                    case Ammunition.Target.ASW:
                        ammo._ap._secondaryTargetType = Ammunition.Target.ASuW;
                        break;
                }
            }
        }

        private static void ApplyRangeMultiplier(Ammunition ammo, float multiplier)
        {
            if (multiplier <= 1)
            {
                return;
            }

            ammo._ap._lifeTime *= multiplier;
            ammo._ap._maxLaunchRangeInMiles *= multiplier;
            ammo._ap._launchRangesInUnity.y *= multiplier;
            ammo._ap._horizonRangesInUnity.y *= multiplier;
            ammo._ap._seekerPassiveRange *= multiplier;
            ammo._ap._transmitterRange *= multiplier;
            ammo._ap._wakeHomingSeekerRange *= multiplier;
            ammo._ap._reactionRange *= multiplier;
            ammo._ap._seekerActiveRange *= multiplier;
            ammo._ap._submunitionSeekerRange *= multiplier;
            ammo._ap._timeToMaxRange *= multiplier;
            ammo._ap._fractionOfRangeToActivateSeeker /= multiplier;

            PlayerUtils.LogIfSpam($"AmmoMod: Applied Range Multiplier ({multiplier:F2}x) to '{ammo._ap._displayedName}'.");
        }
    }

    /// <summary>
    /// Modifies base WeaponSystem properties after loading from INI.
    /// </summary>
    [HarmonyPatch(typeof(WeaponSystem), "LoadFromInI")]
    internal static class MunchWeaponProperties
    {
        /// <summary>
        /// Postfix that applies weapon property modifiers for player weapons.
        /// </summary>
        [HarmonyPostfix]
        [UsedImplicitly]
        private static void Postfix(WeaponSystem __instance)
        {
            if (!PlayerUtils.IsPlayerWeapon(__instance))
            {
                return;
            }

            if (__instance._vwp == null)
            {
                PlayerUtils.WarnIfSpam(
                    $"WeaponMod: WeaponParameters (_vwp) is null for '{__instance._name}' on '{__instance._baseObject.name}'. Cannot modify.");
                return;
            }

            // Cache original parameters before modification (for side-swap restoration)
            var clonedParams = ParameterCloner.ShallowClone(__instance._vwp);
            if (clonedParams != null)
            {
                CheatStateTracker.RecordWeaponParams(__instance._baseObject, __instance, clonedParams);
            }

            ApplyFireRateMultiplier(__instance, CheatConfig.FireRateMult.Value);
            ApplyDivisor(__instance._vwp, "_maxReactiontime", CheatConfig.ReactionTimeDiv.Value);
            ApplyDivisor(__instance._vwp, "_targetAcquisitionTime", CheatConfig.TargetAcqTimeDiv.Value);
            ApplyDivisor(__instance._vwp, "_preLaunchDelay", CheatConfig.PreLaunchDelayDiv.Value);
            ApplyDivisor(__instance._vwp, "_magazineReloadTime", CheatConfig.MagReloadTimeDiv.Value);
            ApplyMultiplier(__instance._vwp, "_verticalDegreesPerSecond", CheatConfig.TraverseSpeedMult.Value);
            ApplyMultiplier(__instance._vwp, "_horizontalDegreesPerSecond", CheatConfig.TraverseSpeedMult.Value);

            if (CheatConfig.BrokenFireControl.Value)
            {
                __instance._vwp._numberOfWires = GameConstants.InfiniteChannels;
            }

            PlayerUtils.LogIfSpam(
                $"WeaponMod: Applied modifiers to player weapon '{__instance._systemName}' on '{__instance._baseObject.name}'.");
        }

        private static void ApplyFireRateMultiplier(WeaponSystem weaponSystem, float multiplier)
        {
            if (multiplier == 0f)
            {
                PlayerUtils.LogIfSpam($"WeaponMod: Setting infinite fire rate for '{weaponSystem._name}'.");
                weaponSystem._vwp._fireRatePerMinute = int.MaxValue;
                weaponSystem._vwp._delayBetweenLaunches = 0f;
                weaponSystem._vwp._burstTime = 0f;
                weaponSystem._vwp._salvoFireTime = 0f;
            }
            else if (multiplier > 0f && !Mathf.Approximately(multiplier, 1.0f))
            {
                PlayerUtils.LogIfSpam($"WeaponMod: Applying fire rate multiplier {multiplier}x to '{weaponSystem._name}'.");
                var calculatedRate = weaponSystem._vwp._fireRatePerMinute * multiplier;
                var newRate = Math.Max(1, (int)Math.Round(calculatedRate));

                weaponSystem._vwp._fireRatePerMinute = newRate;
                weaponSystem._vwp._delayBetweenLaunches /= multiplier;
                weaponSystem._vwp._burstTime /= multiplier;
                weaponSystem._vwp._salvoFireTime /= multiplier;
            }
        }

        private static void ApplyMultiplier(object obj, string fieldName, float multiplier)
        {
            if (multiplier <= 1)
            {
                return;
            }

            var field = Reflect.Field(obj.GetType(), fieldName);
            if (field != null && field.FieldType == typeof(float))
            {
                var value = (float)field.GetValue(obj);
                if (value > 0)
                {
                    var newValue = value * multiplier;
                    field.SetValue(obj, newValue);
                    PlayerUtils.LogIfSpam(
                        $"WeaponMod: Applied multiplier {multiplier}x to '{fieldName}', value changed from {value} to {newValue}.");
                }
            }
        }

        private static void ApplyDivisor(object obj, string fieldName, float divisor)
        {
            if (divisor == 0f)
            {
                var field = Reflect.Field(obj.GetType(), fieldName);
                if (field != null && field.FieldType == typeof(float))
                {
                    field.SetValue(obj, 0f);
                    PlayerUtils.LogIfSpam($"WeaponMod: Set '{fieldName}' to 0 (infinite).");
                }
            }
            else if (divisor > 0f && !Mathf.Approximately(divisor, 1.0f))
            {
                var field = Reflect.Field(obj.GetType(), fieldName);
                if (field != null && field.FieldType == typeof(float))
                {
                    var value = (float)field.GetValue(obj);
                    if (value > 0)
                    {
                        var newValue = value / divisor;
                        field.SetValue(obj, newValue);
                        PlayerUtils.LogIfSpam(
                            $"WeaponMod: Applied divisor {divisor}x to '{fieldName}', value changed from {value} to {newValue}.");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Prevents wire-guided weapons from breaking connection due to vessel speed.
    /// Uses a Postfix to restore wire connection only when broken by the speed check.
    /// </summary>
    [HarmonyPatch(typeof(WeaponBase), "OnUpdateEveryFrame")]
    internal static class UnbreakableWireGuidancePatch
    {
        /// <summary>
        /// Postfix that restores wire connection for player weapons if broken by speed check.
        /// Only restores if all conditions match the speed-break scenario:
        /// - Wire is currently broken
        /// - Launch platform exists and is not destroyed
        /// - Platform is Vessel/Submarine going faster than wire guidance max speed
        /// </summary>
        [HarmonyPostfix]
        [UsedImplicitly]
        private static void Postfix(WeaponBase __instance)
        {
            if (!CheatConfig.UnbreakableWireGuidance.Value)
            {
                return;
            }

            // Only applies to wire-guided weapons that have lost connection
            if (__instance._ap?._midCourseCorrection != AmmunitionParameters.MidCourseCorrection.WireGuided)
            {
                return;
            }

            // Wire is still connected, nothing to restore
            if (__instance._onWire)
            {
                return;
            }

            // Only for player units
            if (!PlayerUtils.IsPlayerUnit(__instance._launchPlatform))
            {
                return;
            }

            // If launch platform is null or destroyed, wire break is legitimate
            if (__instance._launchPlatform == null || __instance._launchPlatform.IsDestroyed)
            {
                return;
            }

            // Speed check only applies to Vessels and Submarines
            if (__instance._launchPlatform is not Vessel && __instance._launchPlatform is not Submarine)
            {
                return;
            }

            // Only restore if platform is exceeding the speed limit (meaning speed broke the wire)
            if (__instance._launchPlatform._velocityInKnots <= GameConstants.WireGuidanceMaxSpeed)
            {
                return;
            }

            // All conditions match: wire was broken by speed check, restore it
            __instance._onWire = true;
            __instance.ConnectionLost.Value = false;
            __instance.ConnectionLostForever.Value = false;

            PlayerUtils.LogIfExtremeSpam(
                $"UnbreakableWire: Restored wire connection for weapon from '{__instance._launchPlatform.name}' (speed: {__instance._launchPlatform._velocityInKnots:F1} kts)");
        }
    }

    /// <summary>
    /// Modifies WeaponSystemGun specific fire rate properties.
    /// </summary>
    [HarmonyPatch(typeof(WeaponSystemGun), "LoadFromInI", typeof(IniHandler), typeof(string),
        typeof(ObjectBaseParameters), typeof(string))]
    internal static class AlterGunFireRate
    {
        /// <summary>
        /// Postfix that applies gun-specific fire rate modifications.
        /// </summary>
        [HarmonyPostfix]
        [UsedImplicitly]
        private static void Postfix(WeaponSystemGun __instance, ref float ____burstDelay,
            ref float ____delayBetweenShotsinBurst, ref float ____shellReloadTime, ref int ____burstCount)
        {
            if (!PlayerUtils.IsPlayerWeapon(__instance))
            {
                return;
            }

            var fireRateMult = CheatConfig.FireRateMult.Value;

            // Disable bursts for extreme fire rates
            if (fireRateMult == 0f)
            {
                ____burstCount = 1;
                ____burstDelay = 0;
                ____delayBetweenShotsinBurst = 0;
                ____shellReloadTime = 0;
            }
            else if (fireRateMult > 1f)
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

            PlayerUtils.LogIfSpam(
                $"GunMod: Applied fire rate multiplier ({fireRateMult}x) to '{__instance._name}'. Burst count: {____burstCount}.");
        }
    }

    /// <summary>
    /// Clamps gun solution to prevent negative aiming deviation with high fire rates.
    /// </summary>
    [HarmonyPatch(typeof(WeaponSystemGun), "OnUpdate")]
    internal static class ClampGunSolutionPatch
    {
        /// <summary>
        /// Postfix that clamps solution value to valid range.
        /// </summary>
        [HarmonyPostfix]
        [UsedImplicitly]
        private static void Postfix(WeaponSystemGun __instance, ref float ____solution)
        {
            if (!PlayerUtils.IsPlayerWeapon(__instance))
            {
                return;
            }

            if (____solution > 1.0f)
            {
                ____solution = 1.0f;
                PlayerUtils.LogIfSpam($"SolutionClamp: Clamped solution for '{__instance._name}' back to 1.0f.");
            }
        }
    }

    /// <summary>
    /// Modifies CIWS intercept chances for player units.
    /// </summary>
    [HarmonyPatch(typeof(WeaponSystemCIWS), "init")]
    internal static class ModifyCIWSProperties
    {
        /// <summary>
        /// Postfix that doubles CIWS intercept chances for player units.
        /// </summary>
        [HarmonyPostfix]
        [UsedImplicitly]
        public static void Postfix(WeaponSystemCIWS __instance)
        {
            if (!PlayerUtils.IsPlayerWeapon(__instance))
            {
                return;
            }

            PlayerUtils.LogIfSpam(
                $"CIWSMod: Doubling intercept chance for '{__instance._name}' on '{__instance._baseObject.name}'.");

            var missileField = Reflect.Field(typeof(WeaponSystemCIWS), "_missileInterceptChance");
            var aircraftField = Reflect.Field(typeof(WeaponSystemCIWS), "_aircraftInterceptChance");

            missileField?.SetValue(__instance, (float)missileField.GetValue(__instance) * 2f);
            aircraftField?.SetValue(__instance, (float)aircraftField.GetValue(__instance) * 2f);
        }
    }

    /// <summary>
    /// Fixes launcher loading to work from any angle when reload time is instant.
    /// </summary>
    [HarmonyPatch(typeof(WeaponSystemLauncher), "init")]
    internal static class FixLauncherLoadingAnywhere
    {
        /// <summary>
        /// Postfix that modifies loading parameters for instant reload.
        /// </summary>
        [HarmonyPostfix]
        [UsedImplicitly]
        private static void Postfix(WeaponSystemLauncher __instance)
        {
            if (!PlayerUtils.IsPlayerWeapon(__instance))
            {
                return;
            }

            if (CheatConfig.MagReloadTimeDiv.Value == 0 && __instance.hasAMagazine())
            {
                if (__instance._vwp._loadingType == WeaponSystem.LoadingType.ReloadsLowerDeck)
                {
                    __instance._vwp._loadingType = WeaponSystem.LoadingType.LoadWhenNeeded;
                }

                __instance._vwp._anyLoadAngle = true;

                PlayerUtils.LogIfSpam(
                    $"WeaponMod: Modified loading parameters for player weapon '{__instance._systemName}'.");
            }
        }
    }

    /// <summary>
    /// Removes RBU reload animations for faster reloading.
    /// </summary>
    [HarmonyPatch(typeof(WeaponSystemLauncher), "init")]
    internal static class FixRBUReloadAnimations
    {
        /// <summary>
        /// Postfix that nullifies RBU reload animations for player units.
        /// </summary>
        [HarmonyPostfix]
        [UsedImplicitly]
        private static void Postfix(WeaponSystemLauncher __instance)
        {
            if (!PlayerUtils.IsPlayerWeapon(__instance) || __instance._weaponType != WeaponSystem.WeaponType.RBU)
            {
                return;
            }

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

            if (changed)
            {
                PlayerUtils.LogIfSpam(
                    $"RBUAnimFix: Nullified reload animations for player RBU '{__instance._name}'.");
            }
        }
    }

    /// <summary>
    /// Modifies shared launch delays and torpedo delays.
    /// </summary>
    [HarmonyPatch(typeof(ObjectBaseLoader), "LoadWeaponSystems", typeof(IniHandler), typeof(ObjectBaseParameters),
        typeof(ObjectBase))]
    internal static class AlterWeaponSystemsAtObjectLoad
    {
        /// <summary>
        /// Postfix that applies shared launch interval modifications.
        /// </summary>
        [HarmonyPostfix]
        [UsedImplicitly]
        private static void Postfix(ObjectBaseParameters obp)
        {
            if (!PlayerUtils.IsPlayerUnit(obp._baseObject))
            {
                return;
            }

            // Torpedo delay
            if (CheatConfig.FireRateMult.Value == 0)
            {
                obp._baseObject._torpedoLaunchDelay = 0;
            }
            else
            {
                obp._baseObject._torpedoLaunchDelay /= CheatConfig.FireRateMult.Value;
            }

            // Shared launch intervals
            if (obp._baseObject._sharedLaunchIntervals == null || !obp._baseObject._sharedLaunchIntervals.Any())
            {
                return;
            }

            var keys = obp._baseObject._sharedLaunchIntervals.Keys.ToList();
            foreach (var key in keys)
            {
                if (obp._baseObject._sharedLaunchIntervals[key] > 0)
                {
                    PlayerUtils.LogIfSpam(
                        $"SharedLaunchDelay: Modifying interval for '{key}' from {obp._baseObject._sharedLaunchIntervals[key]}.");

                    if (CheatConfig.FireRateMult.Value == 0)
                    {
                        obp._baseObject._sharedLaunchIntervals[key] = 0f;
                    }
                    else
                    {
                        obp._baseObject._sharedLaunchIntervals[key] /= CheatConfig.FireRateMult.Value;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Modifies gun barrel recoil parameters for high fire rates.
    /// </summary>
    [HarmonyPatch(typeof(GunBarrel), MethodType.Constructor, typeof(GameObject), typeof(WeaponParameters),
        typeof(WeaponSystemGun), typeof(GameObject), typeof(Vector3), typeof(Vector3), typeof(float), typeof(float))]
    internal static class InterceptRecoilData
    {
        /// <summary>
        /// Prefix that adjusts recoil time and strength for high fire rates.
        /// </summary>
        [HarmonyPrefix]
        [UsedImplicitly]
        private static void Prefix(ref float recoilTime, ref float recoilStrength, ref WeaponSystemGun vwsg)
        {
            if (vwsg._baseObject._type == ObjectBase.ObjectType.Aircraft)
            {
                return;
            }
            if (!PlayerUtils.IsPlayerWeapon(vwsg))
            {
                return;
            }

            PlayerUtils.LogIfSpam($"RecoilMod: Original recoil time for {vwsg._name} is {recoilTime}");

            if (CheatConfig.FireRateMult.Value == 0 || CheatConfig.MagReloadTimeDiv.Value == 0)
            {
                recoilTime = 0f;
                recoilStrength = 0f;
            }
            else
            {
                recoilTime /= CheatConfig.FireRateMult.Value;
                recoilStrength /= CheatConfig.FireRateMult.Value;
            }

            PlayerUtils.LogIfSpam($"RecoilMod: New recoil time is {recoilTime}");
        }
    }

    /// <summary>
    /// Fixes gun barrel position reset before firing to prevent barrel displacement.
    /// </summary>
    [HarmonyPatch(typeof(WeaponSystemGun), "fireBarrel")]
    internal static class WeaponSystemGun_FireBarrel_InterruptionFixPatch
    {
        /// <summary>
        /// Prefix that resets barrel position before firing.
        /// </summary>
        [HarmonyPrefix]
        [UsedImplicitly]
        public static void Prefix(WeaponSystemGun __instance, int idx)
        {
            var barrels = Traverse.Create(__instance).Field<List<GunBarrel>>("_barrels").Value;

            if (barrels == null || idx < 0 || idx >= barrels.Count)
            {
                return;
            }

            var barrelToFire = barrels[idx];
            if (barrelToFire == null)
            {
                return;
            }

            var barrelTraverse = Traverse.Create(barrelToFire);
            var gunObject = barrelTraverse.Field<GameObject>("_gunObject").Value;
            var normalPosition = barrelTraverse.Field<Vector3>("_normalPosition").Value;

            if (gunObject != null)
            {
                gunObject.transform.localPosition = normalPosition;
                PlayerUtils.LogIfSpam(
                    $"GunBarrelFix: Resetting '{gunObject.name}' position before firing.");
            }
        }
    }

    /// <summary>
    /// Comprehensive logger and fixer for the WeaponSystemLauncher state machine.
    /// </summary>
    internal static class LauncherStateLogger
    {
        private static string GetStateReport(WeaponSystemLauncher instance, string eventName)
        {
            var sb = new StringBuilder();
            var t = Traverse.Create(instance);

            sb.AppendLine($"========== LAUNCHER DEBUGGER: {eventName} ==========");
            sb.AppendLine($" Unit: '{instance._baseObject?.name ?? "N/A"}' / System: '{instance._name ?? "N/A"}'");
            sb.AppendLine($" Current State: {instance._state}");
            sb.AppendLine($" IsReloading Flag: {instance._isReloading}");
            sb.AppendLine($" Loading Type: {instance._vwp?._loadingType.ToString() ?? "N/A"}");
            sb.AppendLine($" Has Magazine: {instance.hasAMagazine()}");
            sb.AppendLine($"=======================================================");

            return sb.ToString();
        }

        [HarmonyPatch(typeof(WeaponSystemLauncher), "updateLoadSystem")]
        internal static class Log_UpdateLoadSystem
        {
            [HarmonyPrefix]
            [UsedImplicitly]
            private static bool Prefix(WeaponSystemLauncher __instance, out WeaponSystemLauncher.State __state)
            {
                __state = __instance._state;

                var t = Traverse.Create(__instance);
                bool isShuttingDown = t.Field<bool>("_triggerShutDown").Value;

                if (isShuttingDown)
                {
                    PlayerUtils.LogIfSpam(
                        $"[LauncherFix] Skipped updateLoadSystem in state '{__instance._state}' - shutdown active.");
                    return false;
                }

                return true;
            }

            [HarmonyPostfix]
            [UsedImplicitly]
            private static void Postfix(WeaponSystemLauncher __instance, WeaponSystemLauncher.State __state)
            {
                if (!CheatConfig.LogSpam.Value)
                {
                    return;
                }

                if (__state != __instance._state || __state != WeaponSystemLauncher.State.Idle)
                {
                    CrunchatizerCore.Log.LogInfo(GetStateReport(__instance, $"TICK: updateLoadSystem (From {__state})"));
                }
            }
        }

        [HarmonyPatch(typeof(WeaponSystemLauncher), "destroyWeapons")]
        internal static class Log_DestroyWeapons
        {
            [HarmonyPostfix]
            [UsedImplicitly]
            private static void Postfix(WeaponSystemLauncher __instance)
            {
                var t = Traverse.Create(__instance);
                t.Field<int>("_spawnedAmmunitionCount").Value = 0;

                PlayerUtils.LogIfSpam("[LauncherFix] Postfix on destroyWeapons: _spawnedAmmunitionCount reset to 0.");
            }
        }
    }
}
