# AudioStreamRandomizer.PlaybackMode

Last updated: 2026-10-02

**Declaration:** `public enum AudioStreamRandomizer.PlaybackMode` · **Source:** [AudioStreamRandomizer.cs](../../src/Scene/Resources/AudioStreamRandomizer.cs).

## Description

Selects the existing random pool policy on [AudioStreamRandomizer.Mode](AudioStreamRandomizer.md#mode). The default is RandomNoRepeats. Undefined values reject before mutation.

## Values

| Value | Identity | Behavior |
| --- | ---: | --- |
| Random | 1 | Use positive weights and allow repeated identities. |
| RandomNoRepeats | 0 | Use positive weights and avoid the preceding stream identity when another is eligible. |
| Sequential | 2 | Traverse distinct non-null stream identities in pool order, ignoring weights. |

## Example

```csharp
using var pool = new AudioStreamRandomizer { Mode = AudioStreamRandomizer.PlaybackMode.Sequential };
```

## Verification

AudioRandomizerTests exercises all three policies, literal weights, duplicate identities, shared history and boundary failures. See the owning class for callback/lifetime/platform limits.
