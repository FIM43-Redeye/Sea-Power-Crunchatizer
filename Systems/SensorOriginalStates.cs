// Original state classes for sensor systems
// Used for caching and restoring sensor parameters during side-swap

using System.Collections.Generic;
using SeaPower;
using UnityEngine;

namespace SeaPowerCrunchatizer.Systems
{
    /// <summary>
    /// Stores original SensorSystem base class values.
    /// </summary>
    public class SensorSystemOriginalState
    {
        public int TargetChannels { get; set; }
        public int WeaponChannels { get; set; }
        public Vector2 VerticalViewArc { get; set; }
        public Vector2 HorizontalViewArc { get; set; }
    }

    /// <summary>
    /// Stores original ECM sensor values.
    /// </summary>
    public class ECMOriginalState
    {
        public List<Globals.Frequency>? Frequencies { get; set; }
        public List<float>? WaveLengths { get; set; }
        public float JamConeFov { get; set; }
        public float JamChance { get; set; }
    }

    /// <summary>
    /// Stores original ESM sensor values.
    /// </summary>
    public class ESMOriginalState
    {
        public float Gain { get; set; }
        public float GainFactor { get; set; }
        public float AngularResolutionDegrees { get; set; }
        public List<Globals.Frequency>? Frequencies { get; set; }
        public bool HasDataLink { get; set; }
        public float IdentificationRate { get; set; }
    }

    /// <summary>
    /// Stores original radar sensor values.
    /// </summary>
    public class RadarOriginalState
    {

        public RadarParameters.Role Role { get; set; }
        public bool HasDataLink { get; set; }
        public bool CanDetectLandTargets { get; set; }
        public bool CanDetectPeriscope { get; set; }
        public float MinAltitude { get; set; }
        public float MaxAltitude { get; set; }
        public float MinRange { get; set; }
        public float MaxRange { get; set; }
        public float LookDownMultiplier { get; set; }
        public float LookDownRange { get; set; }
    }

    /// <summary>
    /// Stores original sonar sensor values.
    /// </summary>
    public class SonarOriginalState
    {
        public float Gain { get; set; }
        public float ActiveGain { get; set; }
        public bool HasDataLink { get; set; }
        public float ActiveRangeInKm { get; set; }
        public float AngularResolutionDegrees { get; set; }
    }

    /// <summary>
    /// Stores original visual sensor values.
    /// </summary>
    public class VisualOriginalState
    {
        public float VidRangeMultiplier { get; set; }
        public float LookDownMultiplier { get; set; }
        public float MaxRangeMultiplier { get; set; }
        public float NightVisionLevel { get; set; }
    }
}
