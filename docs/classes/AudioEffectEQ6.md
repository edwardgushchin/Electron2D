# AudioEffectEQ6

Last updated: 2026-10-02

**Declaration:** `public sealed class Electron2D.AudioEffectEQ6` · **Source:** [AudioEffectEQ.cs](../../src/Scene/Resources/AudioEffectEQ.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [AudioEffectEQ](AudioEffectEQ.md).

## Description

Six-band preset with centers 32, 100, 320, 1000, 3200 and 10000 Hz. Its inherited indexed gain, bus ownership, error and processing contracts are the same as directly constructed AudioEffectEQ. All gains start at zero DB. The protected copy hook preserves the concrete type; inherited copying retains every authored gain.

## Example

Partial snippet: `using var eq = new AudioEffectEQ6(); eq.SetBandGainDB(1, 6);`.

## API summary

| Signature | Contract |
| --- | --- |
| `public AudioEffectEQ6()` | Creates zero-DB six-band configuration. |
| `protected override Resource CreateDuplicateInstance()` | Returns a new EQ6 copy target. |

## Method Descriptions

### CreateDuplicateInstance

Used by Resource duplication and scene-local copying. It constructs a new six-band resource; the inherited state-copy hook transfers committed DB gains. Processing histories and native bus instances remain independent.

## Lifecycle, errors and verification

AudioEQTests checks model, Resource/scene-local copying, actual FAudio output, host cleanup and warmed allocations. Physical listening and other platforms remain unverified. See the [base API](AudioEffectEQ.md) and [coverage](../coverage/classes/AudioEffectEQ6.md).
