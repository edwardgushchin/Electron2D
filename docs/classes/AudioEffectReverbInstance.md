# AudioEffectReverbInstance

Last updated: 2026-10-02

**Declaration:** `internal sealed class Electron2D.AudioEffectReverbInstance` · **Source:** [AudioEffectReverb.cs](../../src/Scene/Resources/AudioEffectReverb.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [AudioEffectInstance](AudioEffectInstance.md).

## Description

Independent prepared stereo processing state borrowing one [AudioEffectReverb](AudioEffectReverb.md). Each channel owns a 500 ms predelay line, eight parallel tuned comb buffers with damping history, four serial all-pass buffers and high-pass history. The second channel's base line lengths include a fixed tuning offset. One resource snapshot governs each block; live scalar edits change coefficients/effective lengths without allocating or clearing histories. The instance handles aliased source/destination frames and processes silence so finite-source tails continue. A nonfinite internal or final value clears both channel histories before throwing.

## Example

Partial standalone processing snippet:

```csharp
using var room = new AudioEffectReverb();
using AudioEffectInstance instance = room.Instantiate();
instance.Process(inputFrames, outputFrames); // Equal-length finite Vector2 spans.
```

Instances borrowed from AudioServer cannot be processed or disposed directly.

## API and lifecycle

`AudioEffectReverbInstance` is internal and introduces no public declarations. It implements `OnProcess(ReadOnlySpan<Vector2>, Span<Vector2>)` and `OnProcessSilence()`. The inherited public guards and lifetime are documented on [AudioEffectInstance](AudioEffectInstance.md). Prepared arrays are owned by the instance, released on disposal, and absent from Resource/scene copies. [AudioReverbTests](../../tests/Electron2D.Tests/AudioReverbTests.cs) verifies 24576 independently generated C++ channel samples, native tails, CPU/FAudio allocation boundaries and Linux Wayland host execution; physical listening and other platforms remain unverified.
