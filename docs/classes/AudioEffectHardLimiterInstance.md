# AudioEffectHardLimiterInstance

Last updated: 2026-10-03

**Declaration:** `internal sealed class Electron2D.AudioEffectHardLimiterInstance` · **Source:** [AudioEffectHardLimiter.cs](../../src/Scene/Resources/AudioEffectHardLimiter.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [AudioEffectInstance](AudioEffectInstance.md).

## Description

Internal prepared stereo processor for [AudioEffectHardLimiter](AudioEffectHardLimiter.md). It owns a two-millisecond delay and fixed bucketed minimum-gain history at the output rate. One detector links both channels; each block reads one coherent settings snapshot. It processes silent blocks to flush pending PCM and resets histories after arithmetic overflow. The final output sample cap enforces the configured ceiling. There is no separate public instance API.

## Verification and limits

[AudioHardLimiterTests](../../tests/Electron2D.Tests/AudioHardLimiterTests.cs) compares pinned PCM, verifies strict sample bounds, finite-source delay, recovery and warmed CPU/native allocation. See [ADR 0047](../decisions/audio.md#adr-0047).
