# AudioEffectLimiterInstance

Last updated: 2026-10-03

**Declaration:** `internal sealed class Electron2D.AudioEffectLimiterInstance` · **Source:** [AudioEffectLimiter.cs](../../src/Scene/Resources/AudioEffectLimiter.cs) · **Component:** [Audio playback](../components/audio-playback.md#legacy-soft-limiting).

**Inherits:** [AudioEffectInstance](AudioEffectInstance.md).

## Description

Internal stateless stereo processor borrowing [AudioEffectLimiter](AudioEffectLimiter.md). Each block reads coherent controls, compensates gain, applies the per-channel legacy branch and caps finite output samples. It supports aliased spans and owns no delay/history or output-rate-dependent storage. Silent processing is not requested. The inherited standalone lifecycle and attached bus ownership apply; no additional public instance API exists.

## Verification and limits

[AudioLimiterTests](../../tests/Electron2D.Tests/AudioLimiterTests.cs) compares pinned C++ PCM and checks numeric boundaries, split/alias behavior, live edits, native output and warmed allocation. See the resource page for actual verification scope and [ADR 0047](../decisions/audio.md#adr-0047).
