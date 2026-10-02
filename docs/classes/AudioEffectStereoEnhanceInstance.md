# AudioEffectStereoEnhanceInstance

Last updated: 2026-10-03

**Declaration:** `internal sealed class Electron2D.AudioEffectStereoEnhanceInstance` · **Source:** [AudioEffectStereoEnhance.cs](../../src/Scene/Resources/AudioEffectStereoEnhance.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [AudioEffectInstance](AudioEffectInstance.md).

## Description

Independent stereo width and delay state for one [AudioEffectStereoEnhance](AudioEffectStereoEnhance.md) resource. It prepares one power-of-two float ring at the output mix rate and reads a coherent resource snapshot per block. Each input frame is read before the output is written, so aliasing is supported. The ring stores the widened right channel in ordinary mode and the widened center in surround mode; live edits retain that common history. Silent processing flushes delayed frames, while arithmetic overflow clears the ring before reporting failure. It adds no public declarations.

## Verification and limits

[AudioStereoEnhanceTests](../../tests/Electron2D.Tests/AudioStereoEnhanceTests.cs) checks analytic PCM, ring wrap, aliasing, live edits, native finite-source tails and warmed allocation. See [ADR 0047](../decisions/audio.md#adr-0047).
