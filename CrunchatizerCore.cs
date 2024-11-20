using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MelonLoader;
using NodeCanvas.Tasks.Actions;
using SeaPower;
using System.Reflection;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Sea_Power_Crunchatizer
{
    public class CrunchatizerCore : MelonMod
    {
        public static MelonPreferences_Category Config;
        public static MelonPreferences_Entry<bool> BottomlessMags;
        public static MelonPreferences_Entry<bool> ContainerAutoRefresh;
        public static MelonPreferences_Entry<int> FireRateMult;
        public static MelonPreferences_Entry<int> ReactionTimeDiv;
        public static MelonPreferences_Entry<int> TargetAcqTimeDiv;
        public static MelonPreferences_Entry<int> PreLaunchDelayDiv;
        public static MelonPreferences_Entry<int> MagReloadTimeDiv;
        public static MelonPreferences_Entry<int> TraverseSpeedMult;
        public static MelonPreferences_Entry<int> AircraftRangeMult;

        public static void PrintObjectFields(object obj)
        {
            if (obj == null)
            {
                MelonLogger.Msg("Thing itself is null");
                return;
            }

            MelonLogger.Msg($"Fields of object of type {obj.GetType().Name}:");
        
            // Get all instance fields (public and non-public)
            FieldInfo[] fields = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            foreach (var field in fields)
            {
                // Get the field value
                var value = field.GetValue(obj);
            
                // Safely convert the value to string to handle potential nulls or unprintable types
                string valueString = value != null ? value.ToString() : "null";
            
                // Print field name and its value
               MelonLogger.Msg($"{field.Name}: {valueString}");
            }
        }
        public override void OnInitializeMelon()
        {
            var harmony = new HarmonyLib.Harmony("net.particle.sea_power_crunchatizer");
            MelonLogger.Msg("Here we go again.");
            Config = MelonPreferences.CreateCategory("CrunchatizerConfig");
            BottomlessMags = Config.CreateEntry("BottomlessMags", true);
            BottomlessMags.Description =
                "Magazines infinitely provide ammo. Returning ammo to magazines will not add to them.";
            ContainerAutoRefresh = Config.CreateEntry("ContainerAutoRefresh", true);
            ContainerAutoRefresh.Description =
                "Weapon containers that do not reload will automatically self-refresh at the conclusion of firing.";
            FireRateMult = Config.CreateEntry("FireRateMult", 1);
            FireRateMult.Description = "Flat multiplier for fire rate.";
            ReactionTimeDiv = Config.CreateEntry("ReactionTimeDiv", 1);
            ReactionTimeDiv.Description = "Flat divisor for reaction time.";
            TargetAcqTimeDiv = Config.CreateEntry("TargetAcqTimeDiv", 1);
            TargetAcqTimeDiv.Description = "Flat divisor for target acquisition time.";
            PreLaunchDelayDiv = Config.CreateEntry("PreLaunchDelayDiv", 1);
            PreLaunchDelayDiv.Description = "Flat divisor for pre-launch delay.";
            MagReloadTimeDiv = Config.CreateEntry("MagReloadTimeDiv", 1);
            MagReloadTimeDiv.Description = "Flat divisor for magazine reload time.";
            TraverseSpeedMult = Config.CreateEntry("TraverseSpeedMult", 1);
            TraverseSpeedMult.Description = "Flat multiplier for traverse speed.";
            AircraftRangeMult = Config.CreateEntry("AircraftRangeMult", 1);
            AircraftRangeMult.Description = "Flat multiplier for aircraft range.";
            
        }
    }

    // First copy of decreaseAmmunitionCount (there are two)
    [HarmonyPatch(typeof(WeaponMagazineSystem), "decreaseAmmunitionCount", typeof(string))]
    public static class BottomlessMagsDecrementSingle
    {
        private static bool Prefix(ref WeaponMagazineSystem __instance)
        {
            switch (CrunchatizerCore.BottomlessMags.Value)
            {
                // If the setting's off, don't do anything!
                case false:
                    return true;
            }
            switch (__instance._baseObject._taskforce.Side)
            {
                case Taskforce.TfType.Player:
                    // Abort the function and skip removing ammo
                    MelonLogger.Msg("Caught a player ship trying to remove ammo, skipping!");
                    return false;
                default:
                    return true;
            }
        }
    }
    
    // Second copy
    [HarmonyPatch(typeof(WeaponMagazineSystem), "decreaseAmmunitionCount", typeof(string), typeof(int))]
    public static class BottomlessMagsDecrementMultiple
    {
        private static bool Prefix(ref WeaponMagazineSystem __instance)
        {
            switch (CrunchatizerCore.BottomlessMags.Value)
            {
                // If the setting's off, don't do anything!
                case false:
                    return true;
            }
            switch (__instance._baseObject._taskforce.Side)
            {
                case Taskforce.TfType.Player:
                    // Abort the function and skip removing ammo
                    MelonLogger.Msg("Caught a player ship trying to remove ammo, skipping!");
                    return false;
                default:
                    return true;
            }
        }
    }
    
    //Magazine count control for base WeaponSystem
    [HarmonyPatch(typeof(WeaponSystem), "decreaseMagazineAmmoCount", typeof(string), typeof(int))]
    public static class BottomlessMagsDecrementMags
    {
        private static bool Prefix(ref WeaponMagazineSystem __instance)
        {
            switch (CrunchatizerCore.BottomlessMags.Value)
            {
                // If the setting's off, don't do anything!
                case false:
                    return true;
            }
            switch (__instance._baseObject._taskforce.Side)
            {
                case Taskforce.TfType.Player:
                    // Abort the function and skip removing ammo
                    MelonLogger.Msg("Caught a player ship trying to remove ammo from a mag, skipping!");
                    return false;
                default:
                    return true;
            }
        }
    }
    
    // Only copy of increaseAmmunitionCount, to prevent returning rounds from eating up 
    [HarmonyPatch(typeof(WeaponMagazineSystem), "increaseAmmunitionCount", typeof(string))]
    public static class BottomlessMagsIncrement
    {
        private static bool Prefix(ref WeaponMagazineSystem __instance)
        {
            switch (CrunchatizerCore.BottomlessMags.Value)
            {
                // If the setting's off, don't do anything!
                case false:
                    return true;
            }
            switch (__instance._baseObject._taskforce.Side)
            {
                case Taskforce.TfType.Player:
                    // Abort the function and skip inserting ammo
                    MelonLogger.Msg("Caught a player ship trying to add ammo, skipping!");
                    return false;
                default:
                    return true;
            }
        }
    }
    
    // Container auto-refresh code. This should really be fixed up for later, but for now it replenishes single containers just so I can publish this. Ideally, it would check the entire ship to see if it has no ammunition of the type remaining and no magazines containing the type.
    [HarmonyPatch(typeof(WeaponContainer), "launch")]
    public static class ContainerAutoRefresh
    {
        private static void Postfix(ref WeaponContainer __instance)
        {
            switch (CrunchatizerCore.ContainerAutoRefresh.Value)
            {
                // If the setting's off, don't do anything!
                case false:
                    MelonLogger.Msg("Not doing anything, setting is off for container replen");
                    return;
            }

            // Check ammo in all containers, whether the weapon has a mag (in which case we let the mag system work), and whether the weapon holder is owned by the player
            if (__instance._weaponSystem.getNumberOfAmmunitionInAllContainers() != 0 ||
                __instance._weaponSystem.hasAMagazine() ||
                __instance._weaponSystem._baseObject._taskforce.Side != Taskforce.TfType.Player) return;
            // Don't know any better ways to replenish the ammo in the container right now so we just reinitialize the whole thing
            MelonLogger.Msg("Player ship's weapon system is out of ammo, resetting it!");
            __instance._weaponSystem.init();
        }
    }
    
    // The Really Real Weapon Modification Code.
    [HarmonyPatch(typeof(WeaponSystem), "LoadFromInI")]
    public static class MunchWeaponProperties
    {
        private static void Postfix(ref WeaponSystem __instance)
        {
            switch (__instance._baseObject._taskforce.Side)
            {
                case Taskforce.TfType.Player:
                    // LEAVE THESE AT ONE if you don't want to do anything with them!
                    MelonLogger.Msg("We are now operating on:");
                    CrunchatizerCore.PrintObjectFields(__instance._vwp);
                    MelonLogger.Msg("Multiplying fire rate/dividing launch delay+burst time+salvo time+burst time by " +
                                    CrunchatizerCore.FireRateMult.Value);
                    __instance._vwp._fireRatePerMinute *= CrunchatizerCore.FireRateMult.Value;
                    __instance._vwp._delayBetweenLaunches /= CrunchatizerCore.FireRateMult.Value;
                    __instance._vwp._burstTime /= CrunchatizerCore.FireRateMult.Value;
                    __instance._vwp._salvoFireTime /= CrunchatizerCore.FireRateMult.Value;
                    MelonLogger.Msg("Dividing reaction time of " + __instance._vwp._maxReactiontime + " by " + CrunchatizerCore.ReactionTimeDiv.Value);
                    __instance._vwp._maxReactiontime /= CrunchatizerCore.ReactionTimeDiv.Value;
                    MelonLogger.Msg("Dividing target acquisition time of " + __instance._vwp._targetAcquisitionTime + " by " + CrunchatizerCore.TargetAcqTimeDiv.Value);
                    __instance._vwp._targetAcquisitionTime /= CrunchatizerCore.TargetAcqTimeDiv.Value;
                    MelonLogger.Msg("Dividing pre-launch delay of " + __instance._vwp._preLaunchDelay + " by " + CrunchatizerCore.PreLaunchDelayDiv.Value);
                    __instance._vwp._preLaunchDelay /= CrunchatizerCore.PreLaunchDelayDiv.Value;
                    MelonLogger.Msg("Dividing mag reload time of " + __instance._vwp._magazineReloadTime + " by " + CrunchatizerCore.MagReloadTimeDiv.Value);
                    __instance._vwp._magazineReloadTime /= CrunchatizerCore.MagReloadTimeDiv.Value;
                    MelonLogger.Msg("Multiplying vertical traverse rate of " + __instance._vwp._verticalDegreesPerSecond + " by " + CrunchatizerCore.TraverseSpeedMult.Value);
                    __instance._vwp._verticalDegreesPerSecond *= CrunchatizerCore.TraverseSpeedMult.Value;
                    MelonLogger.Msg("Multiplying horizontal traverse rate of " + __instance._vwp._horizontalDegreesPerSecond + " by " + CrunchatizerCore.TraverseSpeedMult.Value);
                    __instance._vwp._horizontalDegreesPerSecond *= CrunchatizerCore.TraverseSpeedMult.Value;
                    break;
            }
        }
    }
    
    // We need special code to alter properties inherent solely to WeaponSystemGun.
    [HarmonyPatch(typeof(WeaponSystemGun), "LoadFromInI", typeof(IniHandler), typeof(string),
        typeof(ObjectBaseParameters), typeof(string))]
    public static class AlterGunFireRate
    {
        private static void Postfix(ref WeaponSystemGun __instance, ref float ____burstDelay, ref float ____delayBetweenShotsinBurst, ref float ____shellReloadTime)
        {
            if (__instance._baseObject._taskforce.Side == Taskforce.TfType.Player)
            {
                MelonLogger.Msg("trying guns!");
                ____burstDelay /= CrunchatizerCore.FireRateMult.Value;
                ____shellReloadTime /= CrunchatizerCore.FireRateMult.Value;
                ____delayBetweenShotsinBurst /= CrunchatizerCore.FireRateMult.Value;
            }
        }
    }

    // To change shared launch delays.
    [HarmonyPatch(typeof(ObjectBaseLoader), "LoadWeaponSystems", typeof(IniHandler), typeof(ObjectBaseParameters), typeof(ObjectBase))]
    public static class AlterWeaponSystemsAtObjectLoad
    {
        private static void Postfix(ref ObjectBaseParameters obp)
        {
            if (obp._baseObject._taskforce.Side == Taskforce.TfType.Player)
            {
                foreach (var pair in obp._baseObject._sharedLaunchIntervals.ToList())
                {
                    obp._baseObject._sharedLaunchIntervals[pair.Key] /= CrunchatizerCore.FireRateMult.Value;
                }
            }
        }
    }
    
    // I couldn't figure out any better way to stop the barrel from RECEDING INTO THE SHIP so instead you all get this.
    // Better way would be to intercept the readIni functions to do arbitrary modifications but that seems EXTREMELY difficult
    [HarmonyPatch(typeof(GunBarrel), MethodType.Constructor, typeof(GameObject), typeof(WeaponParameters), typeof(WeaponSystemGun), typeof(GameObject), typeof(Vector3), typeof(Vector3), typeof(float), typeof(float))]
    public static class InterceptRecoilData
    {
        private static void Prefix(ref float recoilTime, ref WeaponSystemGun vwsg)
        {
            if (vwsg._baseObject._taskforce.Side == Taskforce.TfType.Player)
            {
                MelonLogger.Msg("I hate doing this. Our recoil time is " + recoilTime);
                recoilTime /= CrunchatizerCore.FireRateMult.Value;
                MelonLogger.Msg("Now it is " + recoilTime);
            }
        }
    }
    
    
    // Now for aircraft and helos. They have different inits, so need different modifiers.
    // Would be easier to peg range flown at zero/fuel quantity at max but I'm too dumb for that
    [HarmonyPatch(typeof(Aircraft), "init")]
    public static class HijackAircraftInit
    {
        private static void Prefix(ref AircraftParameters aircraftParameters)
        {
            if (aircraftParameters._baseObject._taskforce.Side == Taskforce.TfType.Player)
            {
                MelonLogger.Msg("Our base fixed-wing range is " + aircraftParameters._maxRangeInKm + " kilometers");
                aircraftParameters._maxRangeInKm *= CrunchatizerCore.AircraftRangeMult.Value;
                MelonLogger.Msg("Post-modification it is now " + aircraftParameters._maxRangeInKm + " kilometers");
            }

        }
    }
    
    // And helos
    [HarmonyPatch(typeof(Helicopter), "init")]
    public static class HijackHelicopterInit
    {
        private static void Prefix(ref Helicopter __instance)
        {
            if (__instance._taskforce.Side == Taskforce.TfType.Player)
            {
                MelonLogger.Msg("Our base rotary-wing range is " + __instance._hp._maxRangeInKm + " kilometers");
                __instance._hp._maxRangeInKm *= CrunchatizerCore.AircraftRangeMult.Value;
                MelonLogger.Msg("Post-modification it is now " + __instance._hp._maxRangeInKm + " kilometers");
            }
        }
    }
}