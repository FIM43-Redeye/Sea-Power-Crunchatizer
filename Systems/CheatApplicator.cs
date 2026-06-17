// Applies and removes cheats for units
// Part of the dynamic side-swap cheat system

using System.Collections.Generic;
using Sea_Power_Crunchatizer;
using SeaPower;
using SeaPowerCrunchatizer.Config;
using SeaPowerCrunchatizer.Utilities;

namespace SeaPowerCrunchatizer.Systems
{
    /// <summary>
    /// Centralized cheat application and removal for units.
    /// Used by init patches and side-swap patches to ensure consistent behavior.
    /// </summary>
    public static class CheatApplicator
    {
        /// <summary>
        /// Applies all applicable cheats to a vessel and tracks original state.
        /// </summary>
        public static void ApplyVesselCheats(Vessel vessel)
        {
            if (vessel == null)
            {
                return;
            }

            var state = CheatStateTracker.GetOrCreateState(vessel);

            // Store original values BEFORE modification
            state.IsCavitating = vessel._isCavitating;
            state.TorpedoLaunchDelay = vessel._torpedoLaunchDelay;
            state.VesselCavitation = vessel.VP._cavitation;
            state.OriginalVP = ParameterCloner.ShallowClone(vessel.VP);

            // Note: Cavitation is now handled by CavitationPatches.cs postfix on
            // VesselPropulsionSystem.updateParticles - no parameter modifications needed.

            if (CheatConfig.NoTorpedoDelay.Value)
            {
                vessel._torpedoLaunchDelay = 0;
                PlayerUtils.LogIfSpam($"CheatApplicator: Applied NoTorpedoDelay to '{vessel.name}'");
            }
        }

        /// <summary>
        /// Applies all applicable cheats to a submarine and tracks original state.
        /// </summary>
        public static void ApplySubmarineCheats(Submarine submarine)
        {
            if (submarine == null)
            {
                return;
            }

            var state = CheatStateTracker.GetOrCreateState(submarine);

            // Store original values BEFORE modification
            state.IsCavitating = submarine._isCavitating;
            state.TorpedoLaunchDelay = submarine._torpedoLaunchDelay;
            state.SubCavitation = submarine.SP._cavitation;
            state.SubCavitationParams = submarine.SP._cavitationParams;
            state.SubCrushDepth = submarine.SP._crushDepth;
            state.CavitationForbidden = submarine.CavitationForbidden.Value;
            state.OriginalSP = ParameterCloner.ShallowClone(submarine.SP);

            // Cavitation effect is suppressed by CavitationPatches.cs postfix.
            // Also disable the speed limiter that avoids cavitation.
            if (CheatConfig.NoCavSubmarine.Value)
            {
                submarine.CavitationForbidden.Value = false;
                PlayerUtils.LogIfSpam($"CheatApplicator: Set CavitationForbidden=false for '{submarine.name}'");
            }

            // Set crush depth very deep so "Very Deep" preset actually goes deep.
            // The crush check itself is skipped by InfiniteDepthSubmarine_Patch.
            if (CheatConfig.InfiniteSubDepth.Value)
            {
                submarine.SP._crushDepth = 65536f;
                PlayerUtils.LogIfSpam($"CheatApplicator: Set crush depth to 65536 (float) for '{submarine.name}'");
            }

            if (CheatConfig.NoTorpedoDelay.Value)
            {
                submarine._torpedoLaunchDelay = 0;
                PlayerUtils.LogIfSpam($"CheatApplicator: Applied NoTorpedoDelay to '{submarine.name}'");
            }
        }

        /// <summary>
        /// Removes vessel cheats and restores original state.
        /// </summary>
        public static void RemoveVesselCheats(Vessel vessel)
        {
            if (vessel == null)
            {
                return;
            }

            var state = CheatStateTracker.GetState(vessel);
            if (state == null)
            {
                CrunchatizerCore.Log.LogWarning(
                    $"CheatApplicator: Cannot remove cheats from '{vessel.name}' - no tracked state");
                return;
            }

            // Restore unit-level values
            vessel._isCavitating = state.IsCavitating;
            vessel._torpedoLaunchDelay = state.TorpedoLaunchDelay;
            if (state.VesselCavitation != null)
            {
                vessel.VP._cavitation = state.VesselCavitation;
            }

            // Restore full VesselParameters if we have them
            if (state.OriginalVP != null)
            {
                ParameterCloner.RestoreFrom(vessel.VP, state.OriginalVP);
            }

            // Restore all cached subsystem states
            RestoreWeaponParams(state);
            RestoreAmmoParams(state);
            RestoreSensorParams(state);
            RestoreSystemFlags(state);

            CheatStateTracker.StopTracking(vessel);
            CrunchatizerCore.Log.LogInfo($"CheatApplicator: Removed cheats from vessel '{vessel.name}'");
        }

        /// <summary>
        /// Removes submarine cheats and restores original state.
        /// </summary>
        public static void RemoveSubmarineCheats(Submarine submarine)
        {
            if (submarine == null)
            {
                return;
            }

            var state = CheatStateTracker.GetState(submarine);
            if (state == null)
            {
                CrunchatizerCore.Log.LogWarning(
                    $"CheatApplicator: Cannot remove cheats from '{submarine.name}' - no tracked state");
                return;
            }

            // Restore unit-level values
            submarine._isCavitating = state.IsCavitating;
            submarine._torpedoLaunchDelay = state.TorpedoLaunchDelay;
            submarine.CavitationForbidden.Value = state.CavitationForbidden;
            submarine.SP._cavitationParams = state.SubCavitationParams;
            submarine.SP._crushDepth = state.SubCrushDepth;
            if (state.SubCavitation != null)
            {
                submarine.SP._cavitation = state.SubCavitation;
            }

            // Restore full SubmarineParameters if we have them
            if (state.OriginalSP != null)
            {
                ParameterCloner.RestoreFrom(submarine.SP, state.OriginalSP);
            }

            // Restore all cached subsystem states
            RestoreWeaponParams(state);
            RestoreAmmoParams(state);
            RestoreSensorParams(state);
            RestoreSystemFlags(state);

            CheatStateTracker.StopTracking(submarine);
            CrunchatizerCore.Log.LogInfo($"CheatApplicator: Removed cheats from submarine '{submarine.name}'");
        }

        /// <summary>
        /// Restores weapon parameters from cached state.
        /// </summary>
        private static void RestoreWeaponParams(UnitOriginalState state)
        {
            foreach (var kvp in state.WeaponParams)
            {
                var weapon = kvp.Key;
                var originalParams = kvp.Value;

                if (weapon == null || weapon._vwp == null || originalParams == null)
                {
                    continue;
                }

                ParameterCloner.RestoreFrom(weapon._vwp, originalParams);
                PlayerUtils.LogIfSpam($"CheatApplicator: Restored weapon params for '{weapon._name}'");
            }
        }

        /// <summary>
        /// Restores ammunition parameters from cached state.
        /// </summary>
        private static void RestoreAmmoParams(UnitOriginalState state)
        {
            foreach (var kvp in state.AmmoParams)
            {
                var ammo = kvp.Key;
                var originalParams = kvp.Value;

                if (ammo == null || ammo._ap == null || originalParams == null)
                {
                    continue;
                }

                ParameterCloner.RestoreFrom(ammo._ap, originalParams);
                PlayerUtils.LogIfSpam($"CheatApplicator: Restored ammo params for '{ammo._ap._displayedName}'");
            }
        }

        /// <summary>
        /// Restores sensor parameters from cached state.
        /// </summary>
        private static void RestoreSensorParams(UnitOriginalState state)
        {
            foreach (var kvp in state.SensorParams)
            {
                var sensor = kvp.Key;
                var originalState = kvp.Value;

                if (sensor == null)
                {
                    continue;
                }

                switch (originalState)
                {
                    case SensorSystemOriginalState sensorState:
                        sensor._targetChannels = sensorState.TargetChannels;
                        sensor._weaponChannels = sensorState.WeaponChannels;
                        sensor._verticalViewArc = sensorState.VerticalViewArc;
                        sensor._horizontalViewArc = sensorState.HorizontalViewArc;
                        break;

                    case ECMOriginalState ecmState when sensor is SensorSystemECM ecmSensor:
                        if (ecmState.Frequencies != null)
                        {
                            ecmSensor._ecm._ep._frequencies = new List<Globals.Frequency>(ecmState.Frequencies);
                        }
                        if (ecmState.WaveLengths != null)
                        {
                            ecmSensor._ecm._ep._waveLengths = new List<float>(ecmState.WaveLengths);
                        }
                        ecmSensor._ecm._ep._jamConeFov = ecmState.JamConeFov;
                        ecmSensor._ecm._ep._jamChance = ecmState.JamChance;
                        break;

                    case ESMOriginalState esmState when sensor is SensorSystemESM esmSensor:
                        esmSensor._esm._ep._gain = esmState.Gain;
                        esmSensor._esm._ep._gainFactor = esmState.GainFactor;
                        esmSensor._esm._ep._angularResolutionDegrees = esmState.AngularResolutionDegrees;
                        if (esmState.Frequencies != null)
                        {
                            esmSensor._esm._ep._frequencies = new List<Globals.Frequency>(esmState.Frequencies);
                        }
                        esmSensor._esm._ep._hasDataLink = esmState.HasDataLink;
                        esmSensor._esm._ep._identificationRate = esmState.IdentificationRate;
                        break;

                    case RadarOriginalState radarState when sensor is SensorSystemRadar radarSensor:
                        radarSensor._radar._rp._role = radarState.Role;
                        radarSensor._radar._rp._hasDataLink = radarState.HasDataLink;
                        radarSensor._radar._rp._canDetectLandTargets = radarState.CanDetectLandTargets;
                        radarSensor._radar._rp._canDetectPeriscope = radarState.CanDetectPeriscope;
                        radarSensor._radar._rp._minAltitude = radarState.MinAltitude;
                        radarSensor._radar._rp._maxAltitude = radarState.MaxAltitude;
                        radarSensor._radar._rp._minRange = radarState.MinRange;
                        radarSensor._radar._rp._maxRange = radarState.MaxRange;
                        radarSensor._lookDownMultiplier = radarState.LookDownMultiplier;
                        radarSensor._lookDownRange = radarState.LookDownRange;
                        break;

                    case SonarOriginalState sonarState when sensor is SensorSystemSonar sonarSensor:
                        sonarSensor._sonar._sp._gain = sonarState.Gain;
                        sonarSensor._sonar._sp._activeGain = sonarState.ActiveGain;
                        sonarSensor._sonar._sp._hasDataLink = sonarState.HasDataLink;
                        sonarSensor._sonar._sp._activeRangeInKm = sonarState.ActiveRangeInKm;
                        sonarSensor._sonar._sp._angularResolutionDegrees = sonarState.AngularResolutionDegrees;
                        break;

                    case VisualOriginalState visualState when sensor is SensorSystemVisual visualSensor:
                        visualSensor._vidRangeMultiplier = visualState.VidRangeMultiplier;
                        visualSensor._lookDownMultiplier = visualState.LookDownMultiplier;
                        visualSensor._maxRangeMultiplier = visualState.MaxRangeMultiplier;
                        visualSensor._nightVisionLevel = visualState.NightVisionLevel;
                        break;
                }

                PlayerUtils.LogIfSpam($"CheatApplicator: Restored sensor params for '{sensor._systemName}'");
            }
        }

        /// <summary>
        /// Restores _alwaysRepairable flags for systems.
        /// </summary>
        private static void RestoreSystemFlags(UnitOriginalState state)
        {
            foreach (var kvp in state.SystemAlwaysRepairable)
            {
                if (kvp.Key != null)
                {
                    kvp.Key._alwaysRepairable = kvp.Value;
                }
            }
        }

        /// <summary>
        /// Applies cheats to any ObjectBase based on its type.
        /// </summary>
        public static void ApplyCheats(ObjectBase unit)
        {
            // Unlimited fuel applies to all unit types
            if (CheatConfig.UnlimitedFuel.Value && !unit._unlimitedFuel)
            {
                unit._unlimitedFuel = true;
                PlayerUtils.LogIfSpam($"CheatApplicator: Set unlimited fuel for '{unit.name}'");
            }

            // Type-specific cheats
            switch (unit)
            {
                case Vessel vessel:
                    ApplyVesselCheats(vessel);
                    break;
                case Submarine submarine:
                    ApplySubmarineCheats(submarine);
                    break;
            }
        }

        /// <summary>
        /// Removes cheats from any ObjectBase based on its type.
        /// </summary>
        public static void RemoveCheats(ObjectBase unit)
        {
            // Remove unlimited fuel from all unit types
            if (unit._unlimitedFuel)
            {
                unit._unlimitedFuel = false;
                PlayerUtils.LogIfSpam($"CheatApplicator: Removed unlimited fuel from '{unit.name}'");
            }

            // Type-specific cheat removal
            switch (unit)
            {
                case Vessel vessel:
                    RemoveVesselCheats(vessel);
                    break;
                case Submarine submarine:
                    RemoveSubmarineCheats(submarine);
                    break;
            }
        }
    }
}
