# AudioEffectCompressorInstance

Last updated: 2026-10-03

**Declaration:** `internal sealed class Electron2D.AudioEffectCompressorInstance` · **Source:** [AudioEffectCompressor.cs](../../src/Scene/Resources/AudioEffectCompressor.cs) · **Component:** [Audio playback](../components/audio-playback.md#linked-compression-and-sidechain).

**Inherits:** [AudioEffectInstance](AudioEffectInstance.md).

## Description

Internal linked peak envelope borrowing [AudioEffectCompressor](AudioEffectCompressor.md). It fixes output rate at creation and begins with zero decibel excess. Attachment records a stereo-pair index. The processing hook selects local or prepared named detector PCM and delegates to the same block kernel used by the C++ conformance check. Only the envelope affects output; unused internal followers/meters from the comparison processor are not exposed or recreated. Silent processing is not requested. Arithmetic failure clears the block and envelope; the existing native hook contains and reports it. There is no additional public instance API.

## Verification and limits

[AudioCompressorTests](../../tests/Electron2D.Tests/AudioCompressorTests.cs) checks pinned PCM, timing, linked channel ratios, live controls, native named-bus semantics, ownership and warmed allocations. See the resource page and [ADR 0047](../decisions/audio.md#adr-0047) for current limits.
