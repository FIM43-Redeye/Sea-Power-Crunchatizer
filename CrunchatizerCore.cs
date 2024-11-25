using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MelonLoader;
using SeaPower;
using System.Reflection;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using UnityEngine;
// ReSharper disable InconsistentNaming

namespace Sea_Power_Crunchatizer
{
    public class CrunchatizerCore : MelonMod
    {
        // ReSharper disable once MemberCanBePrivate.Global
        public static MelonPreferences_Category Config;
        public static MelonPreferences_Entry<bool> LogSpam;
        public static MelonPreferences_Entry<bool> BottomlessMags;
        public static MelonPreferences_Entry<bool> ContainerAutoRefresh;
        public static MelonPreferences_Entry<bool> ForceTerrainFollowing;
        public static MelonPreferences_Entry<int> FireRateMult;
        public static MelonPreferences_Entry<int> ReactionTimeDiv;
        public static MelonPreferences_Entry<int> TargetAcqTimeDiv;
        public static MelonPreferences_Entry<int> PreLaunchDelayDiv;
        public static MelonPreferences_Entry<int> MagReloadTimeDiv;
        public static MelonPreferences_Entry<int> TraverseSpeedMult;
        public static MelonPreferences_Entry<int> AircraftRangeMult;

        // ReSharper disable once UnusedMember.Global
        public static void PrintObjectFields(object obj)
        {
            if (obj == null)
            {
                MelonLogger.Msg("Thing itself is null");
                return;
            }

            MelonLogger.Msg($"Fields of object of type {obj.GetType().Name}:");
        
            // Get all instance fields (public and non-public)
            var fields = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            foreach (var field in fields)
            {
                // Get the field value
                var value = field.GetValue(obj);
            
                // Safely convert the value to string to handle potential nulls or unprintable types
                var valueString = value != null ? value.ToString() : "null";
            
                // Print field name and its value
               MelonLogger.Msg($"{field.Name}: {valueString}");
            }
        }
        public override void OnInitializeMelon()
        {
            // Set up all the garbage config stuff
            var harmony = new HarmonyLib.Harmony("net.particle.sea_power_crunchatizer");
            MelonLogger.Msg("Initializing CrunchatizerConfig");
            Config = MelonPreferences.CreateCategory("CrunchatizerConfig");
            LogSpam = Config.CreateEntry("LogSpam", true);
            LogSpam.Description =
                "Enables/disables ALL logging messages!";
            BottomlessMags = Config.CreateEntry("BottomlessMags", true);
            BottomlessMags.Description =
                "Magazines infinitely provide ammo. Returning ammo to magazines will not add to them.";
            ContainerAutoRefresh = Config.CreateEntry("ContainerAutoRefresh", true);
            ContainerAutoRefresh.Description =
                "For a weapon system with no magazine, when the last round is shot, the entire system will reinitialize and restore ammo.";
            ForceTerrainFollowing = Config.CreateEntry("ForceTerrainFollowing", true);
            ForceTerrainFollowing.Description =
                "ALL weapons assigned to the player's taskforce are now terrain following.";
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
            harmony.PatchAll();
        }
    }

    // First copy of decreaseAmmunitionCount (there are two)
    [HarmonyPatch(typeof(WeaponMagazineSystem), "decreaseAmmunitionCount", typeof(string))]
    public static class BottomlessMagsDecrementSingle
    {
        [UsedImplicitly]
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
                    if (CrunchatizerCore.LogSpam.Value)
                    {
                        MelonLogger.Msg("Caught a player ship trying to remove ammo, skipping!");
                    }
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
        [UsedImplicitly]
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
                    if (CrunchatizerCore.LogSpam.Value)
                    {
                        MelonLogger.Msg("Caught a player ship trying to remove ammo, skipping!");
                    }
                    return false;
                default:
                    return true;
            }
        }
    }
    
    // Magazine count control for base WeaponSystem
    [HarmonyPatch(typeof(WeaponSystem), "decreaseMagazineAmmoCount", typeof(string), typeof(int))]
    public static class BottomlessMagsDecrementMags
    {
        [UsedImplicitly]
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
                    if (CrunchatizerCore.LogSpam.Value)
                    {
                        MelonLogger.Msg("Caught a player ship trying to remove ammo from a mag, skipping!");
                    }
                    return false;
                default:
                    return true;
            }
        }
    }
    
    // Only copy of increaseAmmunitionCount, to prevent returning rounds from inflating the mag storage
    [HarmonyPatch(typeof(WeaponMagazineSystem), "increaseAmmunitionCount", typeof(string))]
    public static class BottomlessMagsIncrement
    {
        [UsedImplicitly]
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
                    if (CrunchatizerCore.LogSpam.Value)
                    {
                        MelonLogger.Msg("Caught a player ship trying to add ammo, skipping!");
                    }
                    return false;
                default:
                    return true;
            }
        }
    }
    
    // Container auto-refresh code. Finally works to my satisfaction.
    [HarmonyPatch(typeof(WeaponContainer), "launch")]
    public static class ContainerAutoRefresh
    {
        [UsedImplicitly]
        private static void Postfix(ref WeaponContainer __instance)
        {
            switch (CrunchatizerCore.ContainerAutoRefresh.Value)
            {
                // If the setting's off, don't do anything!
                case false:
                    if (CrunchatizerCore.LogSpam.Value)
                    {
                        MelonLogger.Msg("Not doing anything, setting is off for container replen");
                    }

                    return;
            }

            var weaponsToInit = new List<WeaponSystem>();
            foreach (var ammoPair in __instance._weaponSystem._baseObject.AmmunitionAmountDictionary.Where(ammoPair => ammoPair.Value == 0))
            {
                if (CrunchatizerCore.LogSpam.Value)
                {
                    MelonLogger.Msg(ammoPair.Key + " has ammunition number " + ammoPair.Value);
                }

                foreach (var weaponTarget in __instance._weaponSystem._baseObject.GetWeaponSystemsForAmmunition(
                             ammoPair.Key))
                {
                    weaponsToInit.Add(weaponTarget);
                    if (CrunchatizerCore.LogSpam.Value)
                    {
                        MelonLogger.Msg("Adding " + weaponTarget._name + " to the list");
                    }
                }
            }

            foreach (var current in weaponsToInit)
            {
                current.init();
                if (CrunchatizerCore.LogSpam.Value)
                {
                    MelonLogger.Msg("Initialized " + current._name);
                }
            }
            /* Old code that just replenishes a single container whenever it runs dry
            // Check ammo in all containers, whether the weapon has a mag (in which case we let the mag system work), and whether the weapon holder is owned by the player
            if (__instance._weaponSystem.getNumberOfAmmunitionInAllContainers() != 0 ||
                __instance._weaponSystem.hasAMagazine() ||
                __instance._weaponSystem._baseObject._taskforce.Side != Taskforce.TfType.Player) return;
            // Don't know any better ways to replenish the ammo in the container right now so we just reinitialize the whole thing
            MelonLogger.Msg("Player ship's weapon system is out of ammo, resetting it!");
            __instance._weaponSystem.init();
            */
        }
    }

    [HarmonyPatch(typeof(Ammunition), MethodType.Constructor,
        new Type[] { typeof(string), typeof(int), typeof(WeaponSystem) })]
    public static class ModifyAmmunitionAtLoad
    {
        private static void Postfix(ref Ammunition __instance, ref WeaponSystem associatedWeaponSystem)
        {
            if (__instance._ap._type == Ammunition.Type.Missile || associatedWeaponSystem._baseObject._taskforce.Side != Taskforce.TfType.Player) return;
            if (CrunchatizerCore.ForceTerrainFollowing.Value)
            {
                if (CrunchatizerCore.LogSpam.Value)
                {
                    MelonLogger.Msg("Forcing terrain following for weapon " + __instance._ap._displayedName);
                }

                __instance._ap._terrainFollowFlight = true;
            }
        }
    }
    
    // The Really Real Weapon Modification Code.
    [HarmonyPatch(typeof(WeaponSystem), "LoadFromInI")]
    public static class MunchWeaponProperties
    {
        [UsedImplicitly]
        private static void Postfix(ref WeaponSystem __instance)
        {
            switch (__instance._baseObject._taskforce.Side)
            {
                case Taskforce.TfType.Player:
                    // LEAVE THESE AT ONE if you don't want to do anything with them!
                    //MelonLogger.Msg("We are now operating on:");
                    //CrunchatizerCore.PrintObjectFields(__instance._vwp);
                    if (__instance._baseObject._type == ObjectBase.ObjectType.Aircraft) return;
                    if (CrunchatizerCore.LogSpam.Value)
                    {
                        MelonLogger.Msg(
                            "Multiplying fire rate/dividing launch delay+burst time+salvo time+burst time by " +
                            CrunchatizerCore.FireRateMult.Value);
                        MelonLogger.Msg("Dividing reaction time of " + __instance._vwp._maxReactiontime + " by " + CrunchatizerCore.ReactionTimeDiv.Value);
                        MelonLogger.Msg("Dividing target acquisition time of " + __instance._vwp._targetAcquisitionTime + " by " + CrunchatizerCore.TargetAcqTimeDiv.Value);
                        MelonLogger.Msg("Dividing pre-launch delay of " + __instance._vwp._preLaunchDelay + " by " + CrunchatizerCore.PreLaunchDelayDiv.Value);
                        MelonLogger.Msg("Dividing mag reload time of " + __instance._vwp._magazineReloadTime + " by " + CrunchatizerCore.MagReloadTimeDiv.Value);
                        MelonLogger.Msg("Multiplying vertical traverse rate of " + __instance._vwp._verticalDegreesPerSecond + " by " + CrunchatizerCore.TraverseSpeedMult.Value);
                        MelonLogger.Msg("Multiplying horizontal traverse rate of " + __instance._vwp._horizontalDegreesPerSecond + " by " + CrunchatizerCore.TraverseSpeedMult.Value);
                    }
                    // We just do some math, that's all
                    __instance._vwp._fireRatePerMinute *= CrunchatizerCore.FireRateMult.Value;
                    __instance._vwp._delayBetweenLaunches /= CrunchatizerCore.FireRateMult.Value;
                    __instance._vwp._burstTime /= CrunchatizerCore.FireRateMult.Value;
                    __instance._vwp._salvoFireTime /= CrunchatizerCore.FireRateMult.Value;
                    __instance._vwp._maxReactiontime /= CrunchatizerCore.ReactionTimeDiv.Value;
                    __instance._vwp._targetAcquisitionTime /= CrunchatizerCore.TargetAcqTimeDiv.Value;
                    __instance._vwp._preLaunchDelay /= CrunchatizerCore.PreLaunchDelayDiv.Value;
                    __instance._vwp._magazineReloadTime /= CrunchatizerCore.MagReloadTimeDiv.Value;
                    __instance._vwp._verticalDegreesPerSecond *= CrunchatizerCore.TraverseSpeedMult.Value;
                    __instance._vwp._horizontalDegreesPerSecond *= CrunchatizerCore.TraverseSpeedMult.Value;
                    break;
            }
        }
    }
    
    // We need special code to alter properties inherent solely to WeaponSystemGun, might also need to add this for WeaponSystemChaff/RBU/etc in the future
    [HarmonyPatch(typeof(WeaponSystemGun), "LoadFromInI", typeof(IniHandler), typeof(string),
        typeof(ObjectBaseParameters), typeof(string))]
    public static class AlterGunFireRate
    {
        [UsedImplicitly]
        private static void Postfix(ref WeaponSystemGun __instance, ref float ____burstDelay, ref float ____delayBetweenShotsinBurst, ref float ____shellReloadTime)
        {
            if (__instance._baseObject._taskforce.Side != Taskforce.TfType.Player) return;
            if (__instance._baseObject._type == ObjectBase.ObjectType.Aircraft) return;
            if (CrunchatizerCore.LogSpam.Value)
            {
                MelonLogger.Msg("For gun " + __instance._name + ", burst delay is " + ____burstDelay + ", shell reload time is " + ____shellReloadTime + ", delay between shots in burst is " + ____delayBetweenShotsinBurst);
            }
            ____burstDelay /= CrunchatizerCore.FireRateMult.Value;
            ____shellReloadTime /= CrunchatizerCore.FireRateMult.Value;
            ____delayBetweenShotsinBurst /= CrunchatizerCore.FireRateMult.Value;
        }
    }

    // To change shared launch delays.
    // NOTE: There seems to be a hard cap on one engage task per second for weapons free mode. Missile launching speed is limited as such.
    [HarmonyPatch(typeof(ObjectBaseLoader), "LoadWeaponSystems", typeof(IniHandler), typeof(ObjectBaseParameters), typeof(ObjectBase))]
    public static class AlterWeaponSystemsAtObjectLoad
    {
        [UsedImplicitly]
        private static void Postfix(ref ObjectBaseParameters obp)
        {
            if (obp._baseObject._taskforce.Side != Taskforce.TfType.Player) return;
            if (obp._baseObject._type is ObjectBase.ObjectType.Aircraft ||
                obp._baseObject._type is ObjectBase.ObjectType.Helicopter)
            {
                return;
            }
        if (CrunchatizerCore.LogSpam.Value)
            {
                MelonLogger.Msg("Ship " + obp._baseObject.name + " has shared launch intervals AND is player, we proceed to modify them!");
            }
            foreach (var pair in obp._baseObject._sharedLaunchIntervals.ToList())
            {
                if (CrunchatizerCore.LogSpam.Value)
                {
                    MelonLogger.Msg("The interval for system " + pair.Key + " is currently " + pair.Value);
                }
                obp._baseObject._sharedLaunchIntervals[pair.Key] /= CrunchatizerCore.FireRateMult.Value;
                if (CrunchatizerCore.LogSpam.Value)
                {
                    MelonLogger.Msg("The modified interval for system " + pair.Key + " is now " + obp._baseObject._sharedLaunchIntervals[pair.Key]);
                }
            }
        }
    }
    
    // I couldn't figure out any better way to stop the barrel from RECEDING INTO THE SHIP so instead you all get this.
    // Better way would be to intercept the readIni functions to do arbitrary modifications but that seems EXTREMELY difficult
    [HarmonyPatch(typeof(GunBarrel), MethodType.Constructor, typeof(GameObject), typeof(WeaponParameters), typeof(WeaponSystemGun), typeof(GameObject), typeof(Vector3), typeof(Vector3), typeof(float), typeof(float))]
    public static class InterceptRecoilData
    {
        [UsedImplicitly]
        private static void Prefix(ref float recoilTime, ref WeaponSystemGun vwsg)
        {
            
            if (vwsg._baseObject._type == ObjectBase.ObjectType.Aircraft) return;
            if (vwsg._baseObject._taskforce.Side != Taskforce.TfType.Player) return;
            if (CrunchatizerCore.LogSpam.Value)
            {
                MelonLogger.Msg("Our recoil time for system " + vwsg._name + " is " + recoilTime);
            }
            recoilTime /= CrunchatizerCore.FireRateMult.Value;
            if (CrunchatizerCore.LogSpam.Value)
            {
                MelonLogger.Msg("Now it is " + recoilTime);
            }
        }
    }
    
    
    // Now for aircraft and helicopters. They have different inits, so need different modifiers.
    // Would be easier to peg range flown at zero/fuel quantity at max, but I'm too dumb for that
    [HarmonyPatch(typeof(Aircraft), "init")]
    public static class HijackAircraftInit
    {
        [UsedImplicitly]
        private static void Postfix(ref AircraftParameters aircraftParameters, ref Aircraft __instance)
        {
            if (aircraftParameters._baseObject._taskforce.Side != Taskforce.TfType.Player) return;
            if (CrunchatizerCore.LogSpam.Value)
            {
                MelonLogger.Msg("Our base fixed-wing range is " + aircraftParameters._baseObject._obp._maxRangeInKm + " kilometers");
                MelonLogger.Msg("Our base default fixed-wing range is " + aircraftParameters._baseObject._obp._defaultMaxRangeInKm + " kilometers");
            }
            aircraftParameters._maxRangeInKm *= CrunchatizerCore.AircraftRangeMult.Value;
            aircraftParameters._defaultMaxRangeInKm *= CrunchatizerCore.AircraftRangeMult.Value;
            __instance.RangeInKm.Value *= CrunchatizerCore.AircraftRangeMult.Value;
            __instance.ActualRangeInKm.Value *= CrunchatizerCore.AircraftRangeMult.Value;
            __instance.RangeOnMap.Value *= CrunchatizerCore.AircraftRangeMult.Value;
            if (CrunchatizerCore.LogSpam.Value)
            {
                MelonLogger.Msg("Post-mod fixed-wing range is " + aircraftParameters._baseObject._obp._maxRangeInKm + " kilometers");
                MelonLogger.Msg("Post-mod default fixed-wing range is " + aircraftParameters._baseObject._obp._defaultMaxRangeInKm + " kilometers");
            }

        }
    }
    
    // And helicopters
    [HarmonyPatch(typeof(Helicopter), "init")]
    public static class HijackHelicopterInit
    {
        [UsedImplicitly]
        private static void Prefix(ref Helicopter __instance)
        {
            if (__instance._taskforce.Side != Taskforce.TfType.Player) return;
            if (__instance._obp == null)
            {
                MelonLogger.Msg("Base params not initialized!!");
                return;
            }
            if (CrunchatizerCore.LogSpam.Value)
            {
                MelonLogger.Msg("Our base rotary-wing range is " + __instance._obp._maxRangeInKm + " kilometers");
                MelonLogger.Msg("Our default base rotary-wing range is " + __instance._obp._defaultMaxRangeInKm + " kilometers");
            }
            __instance._obp._maxRangeInKm *= CrunchatizerCore.AircraftRangeMult.Value;
            __instance._obp._defaultMaxRangeInKm *= CrunchatizerCore.AircraftRangeMult.Value;
            if (CrunchatizerCore.LogSpam.Value)
            {
                MelonLogger.Msg("Post-modification base rotary range is now " + __instance._obp._maxRangeInKm + " kilometers");
                MelonLogger.Msg("Post-modification base rotary default range is now " + __instance._obp._defaultMaxRangeInKm + " kilometers");
            }
        }
    }
}