// MonoBehaviour for storing hardpoint loadout data on flying units
// Used by the AutoRearm system to track and restore loadouts for aircraft and helicopters

using System.Collections.Generic;
using SeaPowerCrunchatizer.Data;
using UnityEngine;

namespace Sea_Power_Crunchatizer
{
    /// <summary>
    /// Unity MonoBehaviour component attached to flying units (aircraft, helicopters) to store
    /// their original loadout configuration. Both Aircraft and Helicopter inherit from ObjectBase,
    /// and both can have WeaponSystemHardpoint weapons that this component tracks.
    /// This allows the AutoRearm system to restore a unit's weapons and stores after rearming.
    /// </summary>
    public class HardpointLoadoutData : MonoBehaviour
    {
        /// <summary>
        /// List of loadout information for each hardpoint station on the unit.
        /// Captured when the unit is first encountered by the AutoRearm system.
        /// Works for any ObjectBase with hardpoint weapons (aircraft, helicopters, etc.).
        /// </summary>
        public List<StationLoadoutInfo> LoadoutInfos = new List<StationLoadoutInfo>();
    }
}
