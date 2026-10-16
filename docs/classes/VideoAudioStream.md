# VideoAudioStream

Last updated: 2026-10-16

`internal sealed class VideoAudioStream : AudioStream` in [VideoAudioStream.cs](../../src/Scene/Resources/VideoAudioStream.cs).
Part of [video playback](../components/video-playback.md). Unavailable to engine consumers.

A private one-owner transport over retained QueuePlayback : AudioStreamPlaybackResampled. Construction reserves a power-of-two Vector2 ring strictly larger than the requested source-frame count, bounded to 2^24 frames. A positive source rate is required. InstantiatePlayback returns that one queue once; ordinary audio players own its native voice lifetime.

Push accepts as many interleaved source frames as fit and commits the write cursor after finite downmix validation. Vorbis center/side gains are 1/sqrt(2), rear gains 1/2, mono duplicates, LFE is omitted. QueuePlayback serializes producer, reset, gain and mixer access through the shared resampler gate. Underruns output silence while remaining active. Reset clears cursors and shared interpolation history without reallocating. The internal shared MixPrepared entry takes explicit global speed 1, avoiding division overflow and keeping video independent of ordinary-audio speed. Gain applies after resampling to all routed channels.

Dispose clears the ring and base resampler state. VideoAudioSink, declared internally in [VideoStreamPlayback.cs](../../src/Scene/Resources/VideoStreamPlayback.cs), is a synchronous borrowed-span delegate returning accepted frame count. It connects the scene decoder to this queue and is cleared before playback retirement.
