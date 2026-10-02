# AudioEffectPhaserInstance

Last updated: 2026-10-03

**Declaration:** `internal sealed class Electron2D.AudioEffectPhaserInstance` · **Source:** [AudioEffectPhaser.cs](../../src/Scene/Resources/AudioEffectPhaser.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [AudioEffectInstance](AudioEffectInstance.md).

## Description

Prepared six-stage all-pass and feedback state for one [AudioEffectPhaser](AudioEffectPhaser.md) and stereo pair. Left and right have independent six-sample histories and feedback; the sinusoidal oscillator phase is shared. Each block reads a coherent resource snapshot, then processes frames in input order without allocating. Silent input continues through the chain to produce the remaining tail. Arithmetic overflow clears both histories, feedback and phase before reporting failure. The instance adds no public declarations.

## Verification and limits

[AudioPhaserTests](../../tests/Electron2D.Tests/AudioPhaserTests.cs) compares pinned C++ PCM, analytic six-stage impulse, aliasing, tails, overflow recovery, native output and warmed allocation. See [ADR 0047](../decisions/audio.md#adr-0047).
