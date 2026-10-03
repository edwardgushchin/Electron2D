# AudioStreamPlaybackMicrophone

Last updated: 2026-10-03

**Source:** [AudioStreamMicrophone.cs](../../src/Scene/Resources/AudioStreamMicrophone.cs). **Declaration:** `internal sealed class AudioStreamPlaybackMicrophone : AudioStreamPlaybackResampled`. **Inherits:** [AudioStreamPlaybackResampled](AudioStreamPlaybackResampled.md).

## Description and state

Internal caller/player-owned implementation returned as AudioStreamPlayback. Borrows AudioStreamMicrophone and the current AudioInputDevice. Start/Stop/Dispose require the audio owner; all cursor/history work uses ResampleGate. Start registers capture then primes cubic history, rolling back registration on failure. Source time and loops remain zero; Seek validates through the base and does not move input. Active repeated Start retains history. Stopped engine mixing clears output and returns zero; active source reads always report a full requested span including silence, so underrun never finishes a voice.

A device replacement is detected by identity at the next mix: cursor/generation/history reset and the new sampling rate feeds the shared resampler. Each reader begins at oldest available history after priming; overwritten frames cause catch-up, consumed input never replays. Server CloseInput marks playback inactive without disposing the borrowed resource. Cleanup releases requests even after source disposal. No additional public playback declarations are introduced.

## Verification

[AudioInputTests](../../tests/Electron2D.Tests/AudioInputTests.cs) checks independent stereo cursors against native conversion, public/server reader isolation, Start/Stop/Seek, multi-microphone and explicit recording lifetime, device changes, concurrent history/reset, warm active/stopped mixing, native FAudio output and engine teardown. See [microphone reference](AudioStreamMicrophone.md) and [audio component](../components/audio-playback.md#recording-input).

Interactive parents now prepare child controls on the audio owner, including paused microphone input and request capacity, then schedule selected child Start/Stop under the shared audio gate. Public microphone controls/disposal retain owner checks; preparation alone does not record. Mixed interactive/randomizer/synchronized graphs share cycle/owner validation. See [interactive streams](../components/audio-playback.md#interactive-streams) for timing, lifecycle, native evidence and limits.
