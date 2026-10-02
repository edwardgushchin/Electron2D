# AudioEffectEQ21

Last updated: 2026-10-02

**Declaration:** `public sealed class Electron2D.AudioEffectEQ21` · **Source:** [AudioEffectEQ.cs](../../src/Scene/Resources/AudioEffectEQ.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [AudioEffectEQ](AudioEffectEQ.md).

## Description

Twenty-one-band preset with centers 22, 32, 44, 63, 90, 125, 175, 250, 350, 500, 700, 1000, 1400, 2000, 2800, 4000, 5600, 8000, 11000, 16000 and 22000 Hz. Its inherited indexed gain, bus ownership, error and processing contracts follow AudioEffectEQ. All gains start at zero DB. Copies preserve the concrete type and every authored gain.

## Example

Partial snippet: `using var eq = new AudioEffectEQ21(); eq.SetBandGainDB(17, -6);`.

## API summary

| Signature | Contract |
| --- | --- |
| `public AudioEffectEQ21()` | Creates zero-DB twenty-one-band configuration. |
| `protected override Resource CreateDuplicateInstance()` | Returns a new EQ21 copy target. |

## Method Descriptions

### CreateDuplicateInstance

Used by Resource duplication and scene-local copying. It constructs a new twenty-one-band resource; the inherited state-copy hook transfers committed DB gains. Processing histories and native bus instances remain independent.

## Lifecycle, errors and verification

AudioEQTests checks the concrete count, indexed descriptors, Resource/scene-local copying, frequency-selective PCM and warmed allocations. Physical listening and other platforms remain unverified. See the [base API](AudioEffectEQ.md) and [coverage](../coverage/classes/AudioEffectEQ21.md).
