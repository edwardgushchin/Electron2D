# AudioEffectDelayInstance

Last updated: 2026-10-02

**Declaration:** `internal sealed class Electron2D.AudioEffectDelayInstance` · **Source:** [AudioEffectDelay.cs](../../src/Scene/Resources/AudioEffectDelay.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [AudioEffectInstance](AudioEffectInstance.md).

## Description

Independent prepared stereo delay state for one [AudioEffectDelay](AudioEffectDelay.md) resource and one bus stereo pair or standalone caller. It owns two power-of-two Vector2 rings, input and feedback positions, and one low-pass history per channel. The instance borrows its resource. A process block snapshots current settings, writes each input before zero-delay tap reads, sums dry/taps/prior feedback, then computes filtered feedback. Silent input still processes so echoes survive source completion. A nonfinite output clears both rings and history before reporting an arithmetic failure.

## Example

Partial standalone processing snippet:

```csharp
using var delay = new AudioEffectDelay { Dry = 0, Tap1DelayMS = 100 };
using AudioEffectInstance instance = delay.Instantiate();
instance.Process(inputFrames, outputFrames); // Equal-length finite Vector2 spans.
```

Bus-owned instances are borrowed and cannot be directly processed or disposed.

## API and lifecycle

`AudioEffectDelayInstance` is internal and adds no public declarations. It implements `OnProcess(ReadOnlySpan<Vector2>, Span<Vector2>)` and `OnProcessSilence()`. The inherited public `Process`, `ProcessSilence` and disposal guards are documented on [AudioEffectInstance](AudioEffectInstance.md). Rings are allocated only during construction; repeated active and silent processing and live setting reads allocate no managed storage. A bus structural edit or output closure releases instances while retaining the borrowed resource. `AudioDelayTests` verifies CPU/native PCM, tail continuity, bypass, failure recovery and warmed allocation on Linux x64; physical listening and other platforms remain unverified.
