# AudioStreamInteractive.TransitionFromTime

Last updated: 2026-10-03

**Namespace:** `Electron2D` · **Declaration:** `public enum Electron2D.AudioStreamInteractive.TransitionFromTime` · **Source:** [AudioStreamInteractive.cs](../../src/Scene/Resources/AudioStreamInteractive.cs).

## Description

Chooses the source moment of a requested transition.

See [AudioStreamInteractive](AudioStreamInteractive.md) for timing and ownership.

## Enum values

| Complete signature | Contract |
| --- | --- |
| `public const Electron2D.AudioStreamInteractive.TransitionFromTime End = 3` | Wait for the declared musical or sample duration, otherwise begin immediately. |
| `public const Electron2D.AudioStreamInteractive.TransitionFromTime Immediate = 0` | Begin without waiting. |
| `public const Electron2D.AudioStreamInteractive.TransitionFromTime NextBar = 2` | Wait for the next bar when tempo/bar metadata exists, otherwise begin immediately. |
| `public const Electron2D.AudioStreamInteractive.TransitionFromTime NextBeat = 1` | Wait for the next beat when tempo exists, otherwise begin immediately. |

## Enum values descriptions

<a id="member-2ad82ae09211"></a>
### End

`public const Electron2D.AudioStreamInteractive.TransitionFromTime End = 3`

Wait for the declared musical or sample duration, otherwise begin immediately.

<a id="member-7687d4ceb8fe"></a>
### Immediate

`public const Electron2D.AudioStreamInteractive.TransitionFromTime Immediate = 0`

Begin without waiting.

<a id="member-38d409a63f84"></a>
### NextBar

`public const Electron2D.AudioStreamInteractive.TransitionFromTime NextBar = 2`

Wait for the next bar when tempo/bar metadata exists, otherwise begin immediately.

<a id="member-2e780a1f0001"></a>
### NextBeat

`public const Electron2D.AudioStreamInteractive.TransitionFromTime NextBeat = 1`

Wait for the next beat when tempo exists, otherwise begin immediately.
