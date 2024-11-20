using HarmonyLib;
using MelonLoader;
using SeaPower;
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
    [HarmonyPatch(typeof(WeaponContainer), "closeHatches")]
    public static class ContainerAutoRefresh
    {
        private static void Postfix(ref WeaponContainer __instance)
        {
            switch (CrunchatizerCore.ContainerAutoRefresh.Value)
            {
                // If the setting's off, don't do anything!
                case false:
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
    
    // Weapon modification code. See about splitting this up to apply to different weapon types in the future, maybe making reload speed apply to RBUs too? It doesn't right now.
    [HarmonyPatch(typeof(ObjectBaseLoader), "initWeaponParameters")]
    public static class AlterWeaponProperties
    {
        private static void Postfix(ref WeaponParameters __result)
        {
            switch (__result._weaponSystem._baseObject._taskforce.Side)
            {
                case Taskforce.TfType.Player:
                    // LEAVE THESE AT ONE if you don't want to do anything with them!
                    MelonLogger.Msg("Multiplying fire rate of " + __result._fireRatePerMinute + " by " + CrunchatizerCore.FireRateMult);
                    __result._fireRatePerMinute *= CrunchatizerCore.FireRateMult.Value;
                    MelonLogger.Msg("Dividing reaction time of " + __result._maxReactiontime + " by " + CrunchatizerCore.ReactionTimeDiv);
                    __result._maxReactiontime /= CrunchatizerCore.ReactionTimeDiv.Value;
                    MelonLogger.Msg("Dividing target acquisition time of " + __result._targetAcquisitionTime + " by " + CrunchatizerCore.TargetAcqTimeDiv);
                    __result._targetAcquisitionTime /= CrunchatizerCore.TargetAcqTimeDiv.Value;
                    MelonLogger.Msg("Dividing pre-launch delay of " + __result._preLaunchDelay + " by " + CrunchatizerCore.PreLaunchDelayDiv);
                    __result._preLaunchDelay /= CrunchatizerCore.PreLaunchDelayDiv.Value;
                    MelonLogger.Msg("Dividing mag reload time of " + __result._magazineReloadTime + " by " + CrunchatizerCore.MagReloadTimeDiv);
                    __result._magazineReloadTime /= CrunchatizerCore.MagReloadTimeDiv.Value;
                    MelonLogger.Msg("Multiplying vertical traverse rate of " + __result._verticalDegreesPerSecond + " by " + CrunchatizerCore.TraverseSpeedMult);
                    __result._verticalDegreesPerSecond *= CrunchatizerCore.TraverseSpeedMult.Value;
                    MelonLogger.Msg("Multiplying horizontal traverse rate of " + __result._horizontalDegreesPerSecond + " by " + CrunchatizerCore.TraverseSpeedMult);
                    __result._horizontalDegreesPerSecond *= CrunchatizerCore.TraverseSpeedMult.Value;
                    break;
            }
        }
    }
    
    // I couldn't figure out any better way to stop the barrel from RECEDING INTO THE SHIP so instead you all get this.
    // Better way would be to intercept the readIni functions to do arbitrary modifications but that seems EXTREMELY difficult
    [HarmonyPatch(typeof(GunBarrel), MethodType.Constructor, typeof(GameObject), typeof(WeaponParameters), typeof(WeaponSystemGun), typeof(GameObject), typeof(Vector3), typeof(Vector3), typeof(float), typeof(float))]
    public static class InterceptRecoilData
    {
        private static void Prefix(ref float recoilStrength)
        {
            MelonLogger.Msg("I hate doing this. Our recoil strength is " + recoilStrength);
            recoilStrength /= CrunchatizerCore.FireRateMult.Value;
            MelonLogger.Msg("Now it is " + recoilStrength);
        }
    }
    
    // Now for aircraft and helos. They have different inits, so need different modifiers.
    // Would be easier to peg range flown at zero/fuel quantity at max but I'm too dumb for that
    [HarmonyPatch(typeof(Aircraft), "init")]
    public static class HijackAircraftInit
    {
        private static void Prefix(ref AircraftParameters aircraftParameters)
        {
            MelonLogger.Msg("Our base fixed-wing range is " + aircraftParameters._maxRangeInKm + " kilometers");
            aircraftParameters._maxRangeInKm = CrunchatizerCore.AircraftRangeMult.Value;
            MelonLogger.Msg("Post-modification it is now " + aircraftParameters._maxRangeInKm + " kilometers");
                
        }
    }
    
    // And helos
    [HarmonyPatch(typeof(Helicopter), "init")]
    public static class HijackHelicopterInit
    {
        private static void Prefix(ref HelicopterParameters helicopterParameters)
        {
            
            MelonLogger.Msg("Our base rotary-wing range is " + helicopterParameters._maxRangeInKm + " kilometers");
            helicopterParameters._maxRangeInKm = CrunchatizerCore.AircraftRangeMult.Value;
            MelonLogger.Msg("Post-modification it is now " + helicopterParameters._maxRangeInKm + " kilometers");
        }
    }
}