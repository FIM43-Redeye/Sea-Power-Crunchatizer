// Ammunition type detection utilities
// Uses the game's actual data types for reliable detection

using Sea_Power_Crunchatizer;
using SeaPower;
using SeaPowerCrunchatizer.Config;

namespace SeaPowerCrunchatizer.Utilities
{
    /// <summary>
    /// Provides methods for identifying ammunition types using the game's type system.
    /// Used primarily by the auto-rearm system to distinguish between weapons and fuel tanks.
    /// </summary>
    /// <remarks>
    /// Sea Power's ammunition type system:
    /// - _type: Primary type (Missile, Bomb, Torpedo, etc.) - used for physics/behavior
    /// - _subType: Secondary type (Fueltank, Sonobuoy, etc.) - used for special handling
    ///
    /// Fuel tanks have _type = Bomb (for physics) and _subType = Fueltank (for identification).
    /// This is checked in the game's own code at WeaponSystemHardpoint.init().
    /// </remarks>
    public static class AmmoHeuristics
    {
        /// <summary>
        /// Determines if an ammunition is a fuel tank using the game's type system.
        /// </summary>
        /// <param name="ammoParams">The ammunition parameters to check.</param>
        /// <returns>True if the ammunition is typed as a fuel tank.</returns>
        /// <remarks>
        /// This checks _subType == Ammunition.Type.Fueltank, which is how the game
        /// itself identifies fuel tanks (see WeaponSystemHardpoint.init() line 648).
        /// </remarks>
        public static bool IsFuelTank(AmmunitionParameters? ammoParams)
        {
            if (ammoParams == null)
            {
                return false;
            }

            bool isFuelTank = ammoParams._subType == Ammunition.Type.Fueltank;

            if (isFuelTank && CheatConfig.LogSpam?.Value == true)
            {
                CrunchatizerCore.Log?.LogInfo(
                    $"[AmmoHeuristics] '{ammoParams._ammunitionFileName}' identified as fuel tank via _subType.");
            }

            return isFuelTank;
        }

        /// <summary>
        /// Determines if an ammunition is a fuel tank using the Ammunition wrapper class.
        /// </summary>
        /// <param name="ammo">The ammunition to check.</param>
        /// <returns>True if the ammunition is typed as a fuel tank.</returns>
        public static bool IsFuelTank(Ammunition? ammo)
        {
            return IsFuelTank(ammo?._ap);
        }

        /// <summary>
        /// Determines if an ammunition carries fuel (regardless of whether it's a dedicated fuel tank).
        /// </summary>
        /// <param name="ammoParams">The ammunition parameters to check.</param>
        /// <returns>True if the ammunition has any fuel mass.</returns>
        /// <remarks>
        /// Some missiles or bombs may carry fuel for extended range.
        /// This checks the _fuelMassInKg field which is loaded from the INI [General] Fuel key.
        /// </remarks>
        public static bool HasFuel(AmmunitionParameters? ammoParams)
        {
            return ammoParams != null && ammoParams._fuelMassInKg > 0f;
        }

        /// <summary>
        /// Determines if an ammunition carries fuel using the Ammunition wrapper class.
        /// </summary>
        /// <param name="ammo">The ammunition to check.</param>
        /// <returns>True if the ammunition has any fuel mass.</returns>
        public static bool HasFuel(Ammunition? ammo)
        {
            return HasFuel(ammo?._ap);
        }

        /// <summary>
        /// Gets the fuel mass in kilograms for an ammunition.
        /// </summary>
        /// <param name="ammoParams">The ammunition parameters to check.</param>
        /// <returns>The fuel mass in kg, or 0 if none.</returns>
        public static float GetFuelMassKg(AmmunitionParameters? ammoParams)
        {
            return ammoParams?._fuelMassInKg ?? 0f;
        }

        /// <summary>
        /// Gets the fuel mass in kilograms using the Ammunition wrapper class.
        /// </summary>
        /// <param name="ammo">The ammunition to check.</param>
        /// <returns>The fuel mass in kg, or 0 if none.</returns>
        public static float GetFuelMassKg(Ammunition? ammo)
        {
            return GetFuelMassKg(ammo?._ap);
        }

        /// <summary>
        /// Gets the ammunition type (Missile, Bomb, Torpedo, etc.).
        /// </summary>
        /// <param name="ammoParams">The ammunition parameters to check.</param>
        /// <returns>The ammunition type, or Unknown if null.</returns>
        public static Ammunition.Type GetAmmoType(AmmunitionParameters? ammoParams)
        {
            return ammoParams?._type ?? Ammunition.Type.Unknown;
        }

        /// <summary>
        /// Gets the ammunition subtype (Fueltank, Sonobuoy, AirDepthCharge, etc.).
        /// </summary>
        /// <param name="ammoParams">The ammunition parameters to check.</param>
        /// <returns>The ammunition subtype, or None if null.</returns>
        public static Ammunition.Type GetAmmoSubType(AmmunitionParameters? ammoParams)
        {
            return ammoParams?._subType ?? Ammunition.Type.None;
        }
    }
}
