# AudioStreamInteractive.FadeMode

Last updated: 2026-10-03

**Namespace:** `Electron2D` · **Declaration:** `public enum Electron2D.AudioStreamInteractive.FadeMode` · **Source:** [AudioStreamInteractive.cs](../../src/Scene/Resources/AudioStreamInteractive.cs).

## Description

Chooses which clips fade during a transition.

See [AudioStreamInteractive](AudioStreamInteractive.md) for timing and ownership.

## Enum values

| Complete signature | Contract |
| --- | --- |
| `public const Electron2D.AudioStreamInteractive.FadeMode Automatic = 4` | Use Out for destination Start, otherwise Cross. |
| `public const Electron2D.AudioStreamInteractive.FadeMode Cross = 3` | Fade both clips. |
| `public const Electron2D.AudioStreamInteractive.FadeMode Disabled = 0` | Start at full gain; a looping source receives a one-millisecond outgoing fade. |
| `public const Electron2D.AudioStreamInteractive.FadeMode In = 1` | Fade the destination in; a looping source receives a one-millisecond outgoing fade. |
| `public const Electron2D.AudioStreamInteractive.FadeMode Out = 2` | Fade the source out and start the destination at full gain. |

## Enum values descriptions

<a id="member-d0df9865bbce"></a>
### Automatic

`public const Electron2D.AudioStreamInteractive.FadeMode Automatic = 4`

Use Out for destination Start, otherwise Cross.

<a id="member-d994fd745e8b"></a>
### Cross

`public const Electron2D.AudioStreamInteractive.FadeMode Cross = 3`

Fade both clips.

<a id="member-5d29335208d5"></a>
### Disabled

`public const Electron2D.AudioStreamInteractive.FadeMode Disabled = 0`

Start at full gain; a looping source receives a one-millisecond outgoing fade.

<a id="member-89e50f4e6127"></a>
### In

`public const Electron2D.AudioStreamInteractive.FadeMode In = 1`

Fade the destination in; a looping source receives a one-millisecond outgoing fade.

<a id="member-b21700c3f254"></a>
### Out

`public const Electron2D.AudioStreamInteractive.FadeMode Out = 2`

Fade the source out and start the destination at full gain.
