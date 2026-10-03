# AudioStreamInteractive.TransitionToTime

Last updated: 2026-10-03

**Namespace:** `Electron2D` · **Declaration:** `public enum Electron2D.AudioStreamInteractive.TransitionToTime` · **Source:** [AudioStreamInteractive.cs](../../src/Scene/Resources/AudioStreamInteractive.cs).

## Description

Chooses the destination cursor.

See [AudioStreamInteractive](AudioStreamInteractive.md) for timing and ownership.

## Enum values

| Complete signature | Contract |
| --- | --- |
| `public const Electron2D.AudioStreamInteractive.TransitionToTime PreviousPosition = 2` | Resume the destination's remembered mixed cursor when it has a duration. |
| `public const Electron2D.AudioStreamInteractive.TransitionToTime SamePosition = 0` | Use the source cursor plus its wait when the destination has a finite duration. |
| `public const Electron2D.AudioStreamInteractive.TransitionToTime Start = 1` | Start the destination at zero. |

## Enum values descriptions

<a id="member-ecc09ea2b5fa"></a>
### PreviousPosition

`public const Electron2D.AudioStreamInteractive.TransitionToTime PreviousPosition = 2`

Resume the destination's remembered mixed cursor when it has a duration.

<a id="member-ebf133852d5a"></a>
### SamePosition

`public const Electron2D.AudioStreamInteractive.TransitionToTime SamePosition = 0`

Use the source cursor plus its wait when the destination has a finite duration.

<a id="member-e5a91a5117fd"></a>
### Start

`public const Electron2D.AudioStreamInteractive.TransitionToTime Start = 1`

Start the destination at zero.
