# AudioEffectChorusInstance

Last updated: 2026-10-02

**Declaration:** `internal sealed class Electron2D.AudioEffectChorusInstance` · **Source:** [AudioEffectChorus.cs](../../src/Scene/Resources/AudioEffectChorus.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [AudioEffectInstance](AudioEffectInstance.md).

## Description

Internal independent stereo processor borrowing one [AudioEffectChorus](AudioEffectChorus.md). It owns a power-of-two ring at the actual output rate, four phase accumulators and four stereo filter histories. Processing uses the resource's coherent block snapshot, divides larger spans into at most 256-frame chunks and supports in-place input/output. Active and silent blocks reuse prepared storage. The instance requests silence processing for delayed tails. Resource copies omit all transient state.

## Lifecycle and verification

`OnProcess(ReadOnlySpan<Vector2>, Span<Vector2>)` performs interpolated modulated reads, one-pole filtering and wet/dry mixing. `OnProcessSilence()` returns true. On nonfinite output, histories reset before reporting an arithmetic error. The inherited public processing/disposal contract is documented on [AudioEffectInstance](AudioEffectInstance.md). [AudioChorusTests](../../tests/Electron2D.Tests/AudioChorusTests.cs) checks CPU, native and public host behavior; physical listening and other platforms remain unverified.
