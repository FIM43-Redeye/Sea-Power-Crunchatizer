// Configuration management for Sea Power Crunchatizer
// Extracted from CrunchatizerCore.cs during refactoring

using BepInEx.Configuration;

namespace SeaPowerCrunchatizer.Config
{
    /// <summary>
    /// Defines the specific sensor types that can have "broken" parameters applied.
    /// The [Flags] attribute allows for multiple values to be selected in the config.
    /// </summary>
    [System.Flags]
    public enum BrokenSensorTypes
    {
        /// <summary>No sensors modified.</summary>
        None = 0,
        /// <summary>Electronic Support Measures - passive radar detection.</summary>
        ESM = 1 << 0,
        /// <summary>Electronic Counter Measures - radar jamming.</summary>
        ECM = 1 << 1,
        /// <summary>Active radar systems.</summary>
        Radar = 1 << 2,
        /// <summary>Sonar systems - both active and passive.</summary>
        Sonar = 1 << 3,
        /// <summary>Visual/optical detection systems.</summary>
        Visual = 1 << 4,
        /// <summary>All sensor types enabled.</summary>
        All = ESM | ECM | Radar | Sonar | Visual
    }

    /// <summary>
    /// Central configuration manager for all Crunchatizer mod settings.
    /// All settings are exposed as static ConfigEntry fields for easy access from patches.
    /// </summary>
    /// <remarks>
    /// Configuration is organized into sections:
    /// <list type="bullet">
    /// <item><description>General Cheats - Core gameplay modifications</description></item>
    /// <item><description>Weapon Modifiers - Fire rate, range, timing adjustments</description></item>
    /// <item><description>Aircraft Modifiers - Flight deck and aircraft cheats</description></item>
    /// <item><description>Sensor Modifiers - Detection and fire control</description></item>
    /// <item><description>Miscellaneous - Debugging and logging options</description></item>
    /// </list>
    /// </remarks>
    public static class CheatConfig
    {
        // Section name constants for organization in config file
        private const string GeneralSection = "1. General Cheats";
        private const string WeaponSection = "2. Weapon Modifiers";
        private const string AircraftSection = "3. Aircraft Modifiers";
        private const string SensorSection = "4. Sensor Modifiers";
        private const string MiscSection = "5. Miscellaneous";

        // =====================================================================
        // Miscellaneous / Debugging
        // =====================================================================

        /// <summary>
        /// Enables potentially excessive logging messages for debugging purposes.
        /// </summary>
        public static ConfigEntry<bool> LogSpam = null!;

        /// <summary>
        /// Enables logging for methods that run every frame - this WILL destroy your frames!
        /// </summary>
        public static ConfigEntry<bool> ExtremeLogSpam = null!;

        // =====================================================================
        // General Cheats
        // =====================================================================

        /// <summary>
        /// Player units can repair systems/compartments indefinitely, ignoring max repair limits and damage types.
        /// </summary>
        public static ConfigEntry<bool> UnlimitedRepair = null!;

        /// <summary>
        /// Player weapon magazines (WeaponMagazineSystem) never deplete. Returning ammo does nothing.
        /// </summary>
        public static ConfigEntry<bool> BottomlessMags = null!;

        /// <summary>
        /// For player weapon systems with no magazine (typically launchers), instantly refills ammo when empty.
        /// </summary>
        public static ConfigEntry<bool> ContainerAutoRefresh = null!;

        /// <summary>
        /// Allows player-controlled units to automatically engage all valid targets in range simultaneously.
        /// </summary>
        public static ConfigEntry<bool> MultiAutoAttack = null!;

        /// <summary>
        /// Makes all crew for player units ultra-skilled.
        /// </summary>
        public static ConfigEntry<bool> UltraCrew = null!;

        /// <summary>
        /// Disables cavitation on player vessels.
        /// </summary>
        public static ConfigEntry<bool> NoCavVessel = null!;

        /// <summary>
        /// Disables cavitation on player submarines.
        /// </summary>
        public static ConfigEntry<bool> NoCavSubmarine = null!;

        /// <summary>
        /// Disables cavitation on player-launched torpedoes.
        /// </summary>
        public static ConfigEntry<bool> NoCavTorpedo = null!;

        // Note: Infinite battery removed - use game's built-in unlimited fuel option.

        /// <summary>
        /// Player submarines can dive to any depth without hull damage.
        /// </summary>
        public static ConfigEntry<bool> InfiniteSubDepth = null!;

        /// <summary>
        /// Torpedoes launch instantly from all tubes - no more pesky delay.
        /// </summary>
        public static ConfigEntry<bool> NoTorpedoDelay = null!;

        /// <summary>
        /// Multiplier for the number of damage control teams on player ships and submarines.
        /// Applied after the game calculates the default based on displacement.
        /// </summary>
        public static ConfigEntry<int> DCTeamMultiplier = null!;

        /// <summary>
        /// All player units have unlimited fuel/battery, preventing fuel depletion.
        /// Applies to vessels, submarines, aircraft, and helicopters.
        /// </summary>
        public static ConfigEntry<bool> UnlimitedFuel = null!;

        // =====================================================================
        // Weapon Modifiers
        // =====================================================================

        /// <summary>
        /// All missile/torpedo/rocket weapons assigned to the player become terrain/sea-bed following.
        /// </summary>
        public static ConfigEntry<bool> ForceTerrainFollowing = null!;

        /// <summary>
        /// All player weapons receive various guidance, targeting, and performance improvements.
        /// </summary>
        public static ConfigEntry<bool> EnhanceMissileFeatures = null!;

        /// <summary>
        /// Prevents wire-guided weapons from breaking their connection due to launching vessel's speed.
        /// </summary>
        public static ConfigEntry<bool> UnbreakableWireGuidance = null!;

        /// <summary>
        /// Player missile motors burn for the whole flight instead of cutting out, so the
        /// missile powers to maximum range instead of coasting down and stall-destructing.
        /// Only affects full-kinematics missiles (the only ones that model motor burn).
        /// </summary>
        public static ConfigEntry<bool> InfiniteMissileBurnTime = null!;

        /// <summary>
        /// Multiplier for fire rate (higher = faster). Affects RoF, various delays.
        /// Value of 0 sets rate to effective infinity.
        /// </summary>
        public static ConfigEntry<int> FireRateMult = null!;

        /// <summary>
        /// Divisor for weapon reaction time (higher = faster reaction).
        /// Value of 0 sets time to near-zero.
        /// </summary>
        public static ConfigEntry<int> ReactionTimeDiv = null!;

        /// <summary>
        /// Divisor for weapon target acquisition time (higher = faster acquisition).
        /// Value of 0 sets time to near-zero.
        /// </summary>
        public static ConfigEntry<int> TargetAcqTimeDiv = null!;

        /// <summary>
        /// Divisor for weapon pre-launch delay (higher = faster launch).
        /// Value of 0 sets delay to near-zero.
        /// </summary>
        public static ConfigEntry<int> PreLaunchDelayDiv = null!;

        /// <summary>
        /// Divisor for weapon magazine reload time (higher = faster reload).
        /// Value of 0 sets time to near-zero.
        /// </summary>
        public static ConfigEntry<int> MagReloadTimeDiv = null!;

        /// <summary>
        /// Multiplier for weapon traverse speed (higher = faster traverse).
        /// </summary>
        public static ConfigEntry<int> TraverseSpeedMult = null!;

        /// <summary>
        /// Multiplier for weapon range (applied as sqrt to range values). Min=1.
        /// </summary>
        public static ConfigEntry<int> WeaponRangeMult = null!;

        // =====================================================================
        // Aircraft Modifiers
        // =====================================================================

        /// <summary>
        /// Aircraft will instantly refill hardpoints when they're out of ammo.
        /// </summary>
        /// <remarks>
        /// TODO: This config entry exists but implementation may be incomplete.
        /// </remarks>
        public static ConfigEntry<bool> AircraftInfiniteAmmo = null!;

        // Note: Unlimited fuel moved to General Cheats section as it applies to all units.

        /// <summary>
        /// Player flight decks can host infinite aircraft.
        /// </summary>
        public static ConfigEntry<bool> FlightDeckInfiniteSlots = null!;

        // =====================================================================
        // Sensor Modifiers
        // =====================================================================

        /// <summary>
        /// Allows player radar and ESM systems to detect targets over the horizon.
        /// </summary>
        public static ConfigEntry<bool> IgnoreRadarHorizon = null!;

        /// <summary>
        /// Fire control radars have infinite target and weapon channels.
        /// </summary>
        public static ConfigEntry<bool> BrokenFireControl = null!;

        /// <summary>
        /// Specifies which sensor types receive enhanced parameters.
        /// </summary>
        public static ConfigEntry<BrokenSensorTypes> BrokenSensorParams = null!;

        /// <summary>
        /// Allows towed arrays, VDS, dipping sonars, and other speed-restricted sensors to deploy at any speed.
        /// </summary>
        public static ConfigEntry<bool> TowedSensorAnySpeed = null!;

        /// <summary>
        /// Initialize all configuration entries from the given ConfigFile.
        /// Must be called during plugin Awake() before any patches access config values.
        /// </summary>
        /// <param name="config">The BepInEx ConfigFile instance from the plugin.</param>
        public static void Initialize(ConfigFile config)
        {
            // Miscellaneous / Debugging
            LogSpam = config.Bind(MiscSection, "Enable Debug Logging", false,
                "Enables potentially excessive logging messages for debugging purposes.");

            ExtremeLogSpam = config.Bind(MiscSection, "Enable Extreme Debug Logging", false,
                "Enables logging for methods that run every frame and such - this WILL destroy your frames!");

            // General Cheats
            UnlimitedRepair = config.Bind(GeneralSection, "Unlimited Repair", true,
                "Player units can repair systems/compartments indefinitely, ignoring max repair limits and damage types.");

            BottomlessMags = config.Bind(GeneralSection, "Bottomless Magazines", true,
                "Player weapon magazines (WeaponMagazineSystem) never deplete. Returning ammo does nothing.");

            ContainerAutoRefresh = config.Bind(GeneralSection, "Container Auto-Refresh", true,
                "For player weapon systems with no magazine (typically launchers like WeaponContainer), instantly refills ammo when empty.");

            MultiAutoAttack = config.Bind(GeneralSection, "Enable Multi-Target Auto-Attack", true,
                "Allows player-controlled units to automatically engage all valid targets in range simultaneously, instead of just the first one found.");

            UltraCrew = config.Bind(GeneralSection, "Ultra Crew", true,
                "Makes all crew for player units ultra.");

            NoCavVessel = config.Bind(GeneralSection, "No Cavitation Vessels", true,
                "Disables cavitation on player vessels.");

            NoCavSubmarine = config.Bind(GeneralSection, "No Cavitation Submarines", true,
                "Disables cavitation on player submarines.");

            NoCavTorpedo = config.Bind(GeneralSection, "No Cavitation Torpedoes", true,
                "Disables cavitation on player-launched torpedoes.");

            InfiniteSubDepth = config.Bind(GeneralSection, "Infinite Submarine Depth", true,
                "Just silly.");

            NoTorpedoDelay = config.Bind(GeneralSection, "No Torpedo Delay", true,
                "Torpedoes launch instantly from all tubes - no more pesky delay.");

            DCTeamMultiplier = config.Bind(GeneralSection, "DC Team Multiplier", 1,
                "Multiplier for the number of damage control teams on player ships/submarines. Set to 1 for normal, higher values for more teams.");

            UnlimitedFuel = config.Bind(GeneralSection, "Unlimited Fuel", true,
                "All player units have unlimited fuel/battery. Applies to vessels, submarines, aircraft, and helicopters.");

            // Weapon Modifiers
            ForceTerrainFollowing = config.Bind(WeaponSection, "Force Terrain Following", true,
                "ALL missile/torpedo/rocket weapons assigned to the player's taskforce become terrain/sea-bed following (where applicable).");

            EnhanceMissileFeatures = config.Bind(WeaponSection, "Enhance Missile Features", true,
                "ALL missile/torpedo/rocket weapons assigned to the player's taskforce receive various guidance, targeting, and performance improvements.");

            UnbreakableWireGuidance = config.Bind(WeaponSection, "Unbreakable Wire Guidance", true,
                "Prevents wire-guided weapons from breaking their connection due to the launching vessel's speed.");

            InfiniteMissileBurnTime = config.Bind(WeaponSection, "Infinite Missile Burn Time", true,
                "Player missile motors burn for the entire flight instead of cutting out, so missiles power to maximum range rather than coasting down and self-destructing. Only affects full-kinematics missiles.");

            FireRateMult = config.Bind(WeaponSection, "Fire Rate Multiplier", 1,
                "Multiplier for fire rate (higher = faster). Affects RoF, various delays. 0 = instant, other values multiply.");

            ReactionTimeDiv = config.Bind(WeaponSection, "Reaction Time Divisor", 1,
                "Divisor for weapon reaction time (higher = faster reaction). 0 = instant, other values divide.");

            TargetAcqTimeDiv = config.Bind(WeaponSection, "Target Acquisition Time Divisor", 1,
                "Divisor for weapon target acquisition time (higher = faster acquisition). 0 = instant, other values divide.");

            PreLaunchDelayDiv = config.Bind(WeaponSection, "Pre-Launch Delay Divisor", 1,
                "Divisor for weapon pre-launch delay (higher = faster launch). 0 = instant, other values divide.");

            MagReloadTimeDiv = config.Bind(WeaponSection, "Magazine Reload Time Divisor", 1,
                "Divisor for weapon magazine reload time (higher = faster reload). 0 = instant, other values divide.");

            TraverseSpeedMult = config.Bind(WeaponSection, "Traverse Speed Multiplier", 1,
                "Multiplier for weapon traverse speed (higher = faster traverse). Min 1.");

            WeaponRangeMult = config.Bind(WeaponSection, "Weapon Range Multiplier", 1,
                "Multiplier for weapon range (applied as sqrt to range values like max range, lifetime). Min 1.");

            // Aircraft Modifiers
            AircraftInfiniteAmmo = config.Bind(AircraftSection, "Aircraft Infinite Ammo", true,
                "Aircraft will instantly refill hardpoints when they're out of ammo.");

            FlightDeckInfiniteSlots = config.Bind(AircraftSection, "Infinite Flight Deck Slots", true,
                "Player flight decks can host infinite aircraft.");

            // Sensor Modifiers
            IgnoreRadarHorizon = config.Bind(SensorSection, "Ignore Radar Horizon", true,
                "Allows player radar and ESM systems to detect targets over the horizon, ignoring Earth's curvature.");

            BrokenFireControl = config.Bind(SensorSection, "Broken Fire Control Sensors", true,
                "FCRs and whatnot have INFINITE target and weapon channels.");

            BrokenSensorParams = config.Bind(SensorSection, "Broken Sensor Parameters", BrokenSensorTypes.All,
                "Makes all the sensors wacky and fun.");

            TowedSensorAnySpeed = config.Bind(SensorSection, "Towed Sensors at Any Speed", true,
                "Allows towed arrays, VDS, dipping sonars, and other speed-restricted sensors to deploy at any speed.");
        }
    }
}
