# AudioStreamInteractive.AutoAdvanceMode

Last updated: 2026-10-03

**Namespace:** `Electron2D` · **Declaration:** `public enum Electron2D.AudioStreamInteractive.AutoAdvanceMode` · **Source:** [AudioStreamInteractive.cs](../../src/Scene/Resources/AudioStreamInteractive.cs).

## Description

Chooses automatic clip progression.

See [AudioStreamInteractive](AudioStreamInteractive.md) for timing and ownership.

## Enum values

| Complete signature | Contract |
| --- | --- |
| `public const Electron2D.AudioStreamInteractive.AutoAdvanceMode Disabled = 0` | Stay on this clip until an explicit switch. |
| `public const Electron2D.AudioStreamInteractive.AutoAdvanceMode Enabled = 1` | Schedule the configured next clip at this clip's declared end. |
| `public const Electron2D.AudioStreamInteractive.AutoAdvanceMode ReturnToHold = 2` | Schedule the remembered held clip, if any. |

## Enum values descriptions

<a id="member-7483aaefbaca"></a>
### Disabled

`public const Electron2D.AudioStreamInteractive.AutoAdvanceMode Disabled = 0`

Stay on this clip until an explicit switch.

<a id="member-e800f619318d"></a>
### Enabled

`public const Electron2D.AudioStreamInteractive.AutoAdvanceMode Enabled = 1`

Schedule the configured next clip at this clip's declared end.

<a id="member-8868d4d8e4b6"></a>
### ReturnToHold

`public const Electron2D.AudioStreamInteractive.AutoAdvanceMode ReturnToHold = 2`

Schedule the remembered held clip, if any.
