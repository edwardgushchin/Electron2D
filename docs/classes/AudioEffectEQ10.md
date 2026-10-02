# AudioEffectEQ10

Last updated: 2026-10-02

**Declaration:** `public sealed class Electron2D.AudioEffectEQ10` · **Source:** [AudioEffectEQ.cs](../../src/Scene/Resources/AudioEffectEQ.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [AudioEffectEQ](AudioEffectEQ.md).

## Description

Ten-band preset with centers 31.25, 62.5, 125, 250, 500, 1000, 2000, 4000, 8000 and 16000 Hz. Its inherited indexed gain, bus ownership, error and processing contracts follow AudioEffectEQ. All gains start at zero DB. Copies preserve the concrete type and every authored gain.

## Example

Partial snippet: `using var eq = new AudioEffectEQ10(); eq.SetBandGainDB(8, -6);`.

## API summary

| Signature | Contract |
| --- | --- |
| `public AudioEffectEQ10()` | Creates zero-DB ten-band configuration. |
| `protected override Resource CreateDuplicateInstance()` | Returns a new EQ10 copy target. |

## Method Descriptions

### CreateDuplicateInstance

Used by Resource duplication and scene-local copying. It constructs a new ten-band resource; the inherited state-copy hook transfers committed DB gains. Processing histories and native bus instances remain independent.

## Lifecycle, errors and verification

AudioEQTests checks the concrete count, indexed descriptors, Resource/scene-local copying, frequency-selective PCM and warmed allocations. Physical listening and other platforms remain unverified. See the [base API](AudioEffectEQ.md) and [coverage](../coverage/classes/AudioEffectEQ10.md).
