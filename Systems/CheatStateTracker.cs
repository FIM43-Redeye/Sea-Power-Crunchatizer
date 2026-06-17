// Tracks original unit state for cheat removal when units leave player control
// Part of the dynamic side-swap cheat system

using System.Collections.Generic;
using SeaPower;
using UnityEngine;

namespace SeaPowerCrunchatizer.Systems
{
    /// <summary>
    /// Stores original parameter objects for a unit before cheats are applied.
    /// Uses shallow clones to preserve all original values.
    /// </summary>
    public class UnitOriginalState
    {
        // Unit-level state (Vessel/Submarine shared)
        public bool IsCavitating { get; set; }
        public float TorpedoLaunchDelay { get; set; }

        // Vessel-specific
        public ParticleSystem? VesselCavitation { get; set; }
        public VesselParameters? OriginalVP { get; set; }

        // Submarine-specific
        public ParticleSystem? SubCavitation { get; set; }
        public Vector2 SubCavitationParams { get; set; }
        public float SubCrushDepth { get; set; }
        public bool CavitationForbidden { get; set; }
        public SubmarineParameters? OriginalSP { get; set; }

        // Per-weapon: stores cloned WeaponParameters before modification
        public Dictionary<WeaponSystem, WeaponParameters> WeaponParams { get; } = new();

        // Per-ammunition: stores cloned AmmunitionParameters before modification
        public Dictionary<Ammunition, AmmunitionParameters> AmmoParams { get; } = new();

        // Per-sensor: stores cloned sensor parameters before modification
        // Key is the sensor system, value is the cloned parameter object (type varies by sensor type)
        public Dictionary<SensorSystem, object> SensorParams { get; } = new();

        // Per-system: stores original _alwaysRepairable flag
        public Dictionary<BaseSystem, bool> SystemAlwaysRepairable { get; } = new();
    }

    /// <summary>
    /// Tracks which units have had cheats applied and stores their original state.
    /// Enables clean removal of cheats when units leave player control.
    /// </summary>
    public static class CheatStateTracker
    {
        private static readonly Dictionary<ObjectBase, UnitOriginalState> TrackedUnits = new();

        /// <summary>
        /// Checks if a unit is currently being tracked.
        /// </summary>
        public static bool IsTracked(ObjectBase unit)
        {
            return unit != null && TrackedUnits.ContainsKey(unit);
        }

        /// <summary>
        /// Gets the original state for a unit, or null if not tracked.
        /// </summary>
        public static UnitOriginalState? GetState(ObjectBase unit)
        {
            return unit != null && TrackedUnits.TryGetValue(unit, out var state) ? state : null;
        }

        /// <summary>
        /// Gets or creates tracking state for a unit.
        /// Call this BEFORE applying any cheats to ensure state is captured.
        /// </summary>
        public static UnitOriginalState GetOrCreateState(ObjectBase unit)
        {
            if (TrackedUnits.TryGetValue(unit, out var existingState))
            {
                return existingState;
            }

            var state = new UnitOriginalState();
            TrackedUnits[unit] = state;
            return state;
        }

        /// <summary>
        /// Stops tracking a unit and removes it from the tracker.
        /// Call this after restoring original state.
        /// </summary>
        public static void StopTracking(ObjectBase unit)
        {
            if (unit != null)
            {
                TrackedUnits.Remove(unit);
            }
        }

        /// <summary>
        /// Records the original state for a system's _alwaysRepairable flag.
        /// </summary>
        public static void RecordSystemState(ObjectBase unit, BaseSystem system, bool originalValue)
        {
            if (unit == null || system == null)
            {
                return;
            }

            var state = GetOrCreateState(unit);
            if (!state.SystemAlwaysRepairable.ContainsKey(system))
            {
                state.SystemAlwaysRepairable[system] = originalValue;
            }
        }

        /// <summary>
        /// Records original weapon parameters before modification.
        /// </summary>
        public static void RecordWeaponParams(ObjectBase unit, WeaponSystem weapon, WeaponParameters clonedParams)
        {
            if (unit == null || weapon == null || clonedParams == null)
            {
                return;
            }

            var state = GetOrCreateState(unit);
            if (!state.WeaponParams.ContainsKey(weapon))
            {
                state.WeaponParams[weapon] = clonedParams;
            }
        }

        /// <summary>
        /// Records original ammunition parameters before modification.
        /// </summary>
        public static void RecordAmmoParams(ObjectBase unit, Ammunition ammo, AmmunitionParameters clonedParams)
        {
            if (unit == null || ammo == null || clonedParams == null)
            {
                return;
            }

            var state = GetOrCreateState(unit);
            if (!state.AmmoParams.ContainsKey(ammo))
            {
                state.AmmoParams[ammo] = clonedParams;
            }
        }

        /// <summary>
        /// Records original sensor parameters before modification.
        /// </summary>
        public static void RecordSensorParams(ObjectBase unit, SensorSystem sensor, object clonedParams)
        {
            if (unit == null || sensor == null || clonedParams == null)
            {
                return;
            }

            var state = GetOrCreateState(unit);
            if (!state.SensorParams.ContainsKey(sensor))
            {
                state.SensorParams[sensor] = clonedParams;
            }
        }

        /// <summary>
        /// Clears all tracked units. Use for cleanup on plugin unload.
        /// </summary>
        public static void ClearAll()
        {
            TrackedUnits.Clear();
        }

        /// <summary>
        /// Gets the count of currently tracked units.
        /// </summary>
        public static int TrackedCount => TrackedUnits.Count;
    }
}
