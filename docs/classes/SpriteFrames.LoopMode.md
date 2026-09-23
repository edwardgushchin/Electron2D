# SpriteFrames.LoopMode

Last updated: 2026-09-23

- **Declaration:** `public enum SpriteFrames.LoopMode`
- **Source:** [SpriteFrames.cs](../../src/Scene/Resources/SpriteFrames.cs)
- **Owner:** [SpriteFrames](SpriteFrames.md)
- **Consumer:** [AnimatedSprite](AnimatedSprite.md)

## Description

Endpoint policy stored per animation. These are mutually exclusive values, not flags. A new animation defaults to Linear. SetAnimationLoopMode rejects undefined values and does not emit Changed; playback reads the current policy at its endpoint.

| Value | Integer | Behavior |
| --- | --- | --- |
| `None` | 0 | Pause at the endpoint and emit AnimationFinished. |
| `Linear` | 1 | Wrap to the opposite endpoint and emit AnimationLooped before FrameChanged. |
| `PingPong` | 2 | Retain the endpoint and reverse custom speed. Emit AnimationLooped before FrameChanged. |

PingPong timing follows the [source behavior audit](AnimatedSprite.md#timing-contract-and-source-audit), including endpoint dwell and the old-direction progress reset. It does not promise a single endpoint display. The obsolete GetAnimationLoop returns true only for Linear, and SetAnimationLoop(true/false) selects Linear/None.

## Example

```csharp
using var frames = new SpriteFrames();
frames.SetAnimationLoopMode("default", SpriteFrames.LoopMode.PingPong);
```

Enum values, legacy mapping and both playback directions are covered by [AnimatedSpriteTests](../../tests/Electron2D.Tests/AnimatedSpriteTests.cs).
