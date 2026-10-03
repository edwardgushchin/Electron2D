# Animation.UpdateMode

Last updated: 2026-10-04

Continuous evaluation or discrete crossed-key writes; Capture (2) additionally admits snapshot fading.

Declared by [Animation](Animation.md). See [the component](../components/scene-animation.md) for execution and verification and [coverage](../coverage/classes/Animation.md) for exact numeric values and remaining applicable members.

| Value | Number |
| --- | --- |
| `Continuous` | 0 |
| `Discrete` | 1 |

`Capture = 2` evaluates continuously and permits AnimationMixer.Capture to snapshot its target. CaptureIncluded caches whether any track uses this mode.
