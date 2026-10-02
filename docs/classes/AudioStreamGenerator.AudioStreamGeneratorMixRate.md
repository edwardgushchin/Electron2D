# AudioStreamGenerator.AudioStreamGeneratorMixRate

Last updated: 2026-10-02

**Declaration:** `public enum AudioStreamGenerator.AudioStreamGeneratorMixRate` · **Source:** [AudioStreamGenerator.cs](../../src/Scene/Resources/AudioStreamGenerator.cs) · **Owner:** [AudioStreamGenerator](AudioStreamGenerator.md).

| Value | Numeric identity | Contract |
| --- | ---: | --- |
| [Output](#output) | 0 | Live AudioServer output frequency. |
| [Input](#input) | 1 | Prepared recording-device rate. |
| [Custom](#custom) | 2 | Resource MixRate; default. |
| [Max](#max) | 3 | Exclusive bound, rejected as a mode. |

## Value descriptions

### Output

Uses AudioServer.GetMixRate, including its documented pre-native 44100 Hz profile and actual native frequency after preparation. Generator capacity captures the creation-time rate; later mixing uses the current rate without resizing.

### Input

MixRateMode selection and playback creation/start prepare AudioServer.GetInputMixRate from a paused native recording device. Actual input frequency determines new queue capacity; live switching changes the source resampling rate without resizing existing queues. Mode configuration never activates recording; AudioDriverEnableInput gates capture requests separately. Device/permission or off-owner preparation failures preserve the old selection. Mixing reads only prepared frequency. [Coverage](../coverage/classes/AudioStreamGenerator.md) records this executable input integration.

### Custom

Uses the positive finite MixRate resource property. This is the initial selector, with 44100 Hz default.

### Max

Defines the exclusive validation bound. Selecting it throws ArgumentOutOfRangeException, as do values outside the defined range.

## Example and verification

```csharp
using var stream = new AudioStreamGenerator
{ MixRateMode = AudioStreamGenerator.AudioStreamGeneratorMixRate.Output };
```

[AudioGeneratorTests](../../tests/Electron2D.Tests/AudioGeneratorTests.cs) checks numeric/rate behavior and rejected mode rollback. [ADR 0047](../decisions/audio.md#adr-0047) records the exact boundary.
