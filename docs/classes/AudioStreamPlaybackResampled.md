# AudioStreamPlaybackResampled

Last updated: 2026-10-01

**Declaration:** `public abstract class Electron2D.AudioStreamPlaybackResampled` · **Source:** [AudioStreamPlaybackResampled.cs](../../src/Scene/Resources/AudioStreamPlaybackResampled.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [AudioStreamPlayback](AudioStreamPlayback.md).

## Description

Abstract prepared cubic resampler with 128 source frames plus four history frames and an unsigned 16-bit fractional cursor. BeginResample clears history and prefills the source block. Decode position includes prefetch; ordinary concrete Seek does not implicitly reset interpolation history. Target frequency is AudioServer.GetMixRate; local rateScale and global PlaybackSpeedScale multiply source sampling rate. Zero rate retains a frozen cursor. Negative/nonfinite or overflowing ratios throw. The concrete mixed count marks the first silence; buffers/history are reused after preparation. Source callbacks use Span<Vector2> instead of pointer/count. This own class contract is implemented; broader AudioStreamPlayback sample/parameter hooks remain inherited gaps.

## API summary

| Full signature | Contract |
| --- | --- |
| `protected AudioStreamPlaybackResampled()` | Initializes the independent prepared resampling state. |
| `public System.Void BeginResample()` | Clears interpolation history and prefills the first source block. |
| System.ObjectDisposedException | The playback is disposed. |
| `protected abstract System.Single OnGetStreamSamplingRate()` | Supplies the source sampling frequency. Sampling rate in Hz. |
| `protected override System.Int32 OnMix(System.Span<Electron2D.Vector2> buffer, System.Single rateScale)` | Projects the inherited typed callback; see the concrete behavior above and base-class contract. |
| `protected abstract System.Int32 OnMixResampled(System.Span<Electron2D.Vector2> buffer)` | Supplies source stereo frames before resampling. Frames before the first silence, zero through buffer.Length. |
| buffer | Prepared writable output span. |

## Verification and limits

[Audio verification](../components/audio-playback.md#verification) distinguishes CPU behavior, actual native mixed PCM, public host lifecycle, packaging and physical listening. [ADR 0047](../decisions/audio.md#adr-0047) owns the backend/decoder boundary. Inherited members are documented on their declaring class.

[Own reference coverage](../coverage/classes/AudioStreamPlaybackResampled.md) retains missing and Partial members separately.
