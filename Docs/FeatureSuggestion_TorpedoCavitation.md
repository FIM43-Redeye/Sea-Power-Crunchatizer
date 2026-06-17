# Feature Suggestion: Torpedo Cavitation System

## Current State

Torpedoes in Sea Power currently have:

- `_isCavitating` flag (inherited from `ObjectBase`) - but it's unclear where/if this is ever set
- `_cavitationNoise` parameter in `SensorData` - currently set to **zero** for all torpedoes examined
- Visual bubble trail effect (`_inFlightEffect`) that displays regardless of cavitation state

This means:
1. The cavitation flag has no acoustic effect on torpedoes (0 noise penalty)
2. The visual trail always appears, regardless of speed or depth
3. There's no distinction between quiet approach and high-speed terminal runs

## Suggested Implementation

### Physics-Based Torpedo Cavitation

Torpedoes should cavitate based on speed and depth, just like submarines. The game already has the infrastructure for this via `_cavitationParams` (depth/speed relationship).

**Proposed behavior:**

| Scenario | Speed | Cavitation | Noise | Visual |
|----------|-------|------------|-------|--------|
| Passive search | 10-20 kts | No | Base noise only | Minimal/no bubble trail |
| Active search | 25-35 kts | Marginal | Slight increase | Light bubble trail |
| Terminal attack | 40-55+ kts | Yes | Significant increase | Full bubble trail |

### Parameter Changes

1. **Set `_cavitationNoise` to realistic values** for all torpedoes
   - Currently zero, should be non-zero (e.g., 10-30 dB penalty when cavitating)

2. **Add `_cavitationParams`** to torpedo definitions (or use existing infrastructure)
   - Define the speed/depth curve at which cavitation begins
   - Deeper = higher speed before cavitating (same physics as submarines)

3. **Tie visual effects to cavitation state**
   - `_inFlightEffect` (bubble trail) should only display when `_isCavitating == true`
   - Or have two effect variants: quiet running vs cavitating

### Gameplay Impact

This would add meaningful tactical depth to torpedo employment:

- **Mk 48 ADCAP at 15 knots** sneaking up on a deep Alfa-class: Nearly invisible, minimal acoustic signature, no bubble trail. Target has little warning.

- **Same Mk 48 at 55 knots** on terminal approach: Loud cavitation noise, visible bubble trail. Target gets clear warning but less time to react.

- **Shallow water operations**: Torpedoes cavitate at lower speeds (same physics as submarines), making them more detectable.

Players would need to balance:
- Slow/quiet approach (harder to detect, but target may maneuver away)
- Fast/loud attack (easier to detect, but less reaction time)

### Technical Notes

The infrastructure largely exists:
- `ObjectBase._isCavitating` is already checked in `SonarCalculator`, `Sonar`, and noise calculations
- `Torpedo.getNoise()` already has the `if (_isCavitating)` check at line 728
- Visual effects are already spawned conditionally in torpedo states

What's missing:
- Actual assignment of `_isCavitating` based on speed/depth for torpedoes
- Non-zero `_cavitationNoise` values in torpedo INI files
- Conditional visual effect display

## Reference: Current Code

From `Torpedo.cs`:
```csharp
public override float getNoise()
{
    float baseNoise = _sensorData._baseNoise;
    baseNoise += getVelocityInKnots() * _sensorData._flowNoise;
    if (_isCavitating)
    {
        baseNoise += _sensorData._cavitationNoise;  // Currently 0 for all torpedoes
    }
    return baseNoise;
}
```

The calculation framework is there - it just needs meaningful parameter values and the cavitation state to actually be set based on physics.
