// Data class for storing aircraft hardpoint loadout information
// Extracted from CrunchatizerCore.cs during refactoring

using SeaPower;
using UnityEngine;

namespace SeaPowerCrunchatizer.Data
{
    /// <summary>
    /// Stores information about an aircraft hardpoint's loadout configuration.
    /// Used by the auto-rearm system to restore ammunition to its original state.
    /// </summary>
    /// <remarks>
    /// This class captures the essential data needed to recreate ammunition objects
    /// on aircraft hardpoints after they have been expended. The data is captured
    /// when ammunition is first loaded and stored for later restoration.
    /// </remarks>
    public class StationLoadoutInfo
    {
        /// <summary>
        /// The filename of the ammunition definition used to spawn new instances.
        /// This corresponds to the game's data file system.
        /// </summary>
        public string? AmmoFileName;

        /// <summary>
        /// The local position where the ammunition object should be spawned
        /// relative to the hardpoint's parent transform.
        /// </summary>
        public Vector3 LocalSpawnPosition;

        /// <summary>
        /// The local rotation of the station/pylon where ammunition is mounted.
        /// </summary>
        public Quaternion LocalStationRotation;

        /// <summary>
        /// The parent GameObject for the station, used as the transform parent
        /// when spawning new ammunition.
        /// </summary>
        public GameObject? StationParent;

        /// <summary>
        /// The ObjectBaseParameters of the host aircraft, needed for creating
        /// ammunition instances that are properly associated with the parent unit.
        /// </summary>
        public ObjectBaseParameters? HostObjectParameters;

        /// <summary>
        /// Whether this ammunition is a fuel tank (should not be reloaded).
        /// Determined from AmmunitionParameters._subType at capture time.
        /// </summary>
        /// <remarks>
        /// Fuel tanks have _subType == Ammunition.Type.Fueltank in the game's type system.
        /// We store this directly rather than relying on name heuristics for reliability.
        /// </remarks>
        public bool IsFuelTank;

        /// <summary>
        /// The fuel mass in kilograms, if this ammunition provides fuel.
        /// </summary>
        public float FuelMassKg;

        /// <summary>
        /// The ammunition type (Missile, Bomb, etc.).
        /// </summary>
        public Ammunition.Type AmmoType;

        /// <summary>
        /// The ammunition subtype (Fueltank, Sonobuoy, etc.), if any.
        /// </summary>
        public Ammunition.Type AmmoSubType;
    }
}
