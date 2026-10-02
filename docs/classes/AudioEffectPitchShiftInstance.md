# AudioEffectPitchShiftInstance

Last updated: 2026-10-03

**Declaration:** `internal sealed class Electron2D.AudioEffectPitchShiftInstance` · **Source:** [AudioEffectPitchShift.cs](../../src/Scene/Resources/AudioEffectPitchShift.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [AudioEffectInstance](AudioEffectInstance.md).

## Description

Internal prepared stereo phase-vocoder state for [AudioEffectPitchShift](AudioEffectPitchShift.md). Each channel owns input/output FIFO, interleaved float FFT work, phase, spectral and overlap-add arrays. The instance fixes FFT size at creation and reads live pitch/overlap controls once per processing block. Both channels reset together when overlap changes, unit-pitch bypass toggles or arithmetic overflow is detected. Silent input keeps processing to flush delayed output. No separate public instance API is exposed.

## Verification and limits

[AudioPitchShiftTests](../../tests/Electron2D.Tests/AudioPitchShiftTests.cs) compares pinned C++ PCM, output frequency and duration, in-place/split behavior, preparation-time size, live controls, failure recovery, native output and warmed allocation. See [ADR 0047](../decisions/audio.md#adr-0047).
