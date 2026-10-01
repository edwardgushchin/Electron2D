# AudioStreamPlaybackResampled API coverage

Last updated: 2026-10-01

Godot source: [doc/classes/AudioStreamPlaybackResampled.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioStreamPlaybackResampled.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [AudioStreamPlayback](AudioStreamPlayback.md). Electron2D type: [`public abstract class Electron2D.AudioStreamPlaybackResampled`](../../classes/AudioStreamPlaybackResampled.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class AudioStreamPlaybackResampled`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioStreamPlaybackResampled.xml) | [`public abstract class Electron2D.AudioStreamPlaybackResampled`](../../classes/AudioStreamPlaybackResampled.md) | Implemented | ADR 0047: executable Linux FAudio stream/bus pipeline with prepared PCM, typed ownership and bounded mix buffers; CPU and actual native 2/4/6/8-channel PCM checks cover current behavior. Other platforms and physical listening remain unverified. |
| [`method _get_stream_sampling_rate() -> float`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioStreamPlaybackResampled.xml) | [`protected abstract System.Single OnGetStreamSamplingRate()`](../../classes/AudioStreamPlaybackResampled.md) | Implemented | ADR 0047: executable Linux FAudio stream/bus pipeline with prepared PCM, typed ownership and bounded mix buffers; CPU and actual native 2/4/6/8-channel PCM checks cover current behavior. Other platforms and physical listening remain unverified. |
| [`method _mix_resampled(AudioFrame* dst_buffer, int frame_count) -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioStreamPlaybackResampled.xml) | [`protected abstract System.Int32 OnMixResampled(System.Span<Electron2D.Vector2> buffer)`](../../classes/AudioStreamPlaybackResampled.md) | Implemented | ADR 0047: executable Linux FAudio stream/bus pipeline with prepared PCM, typed ownership and bounded mix buffers; CPU and actual native 2/4/6/8-channel PCM checks cover current behavior. Other platforms and physical listening remain unverified. |
| [`method begin_resample() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioStreamPlaybackResampled.xml) | [`public System.Void BeginResample()`](../../classes/AudioStreamPlaybackResampled.md) | Implemented | ADR 0047: executable Linux FAudio stream/bus pipeline with prepared PCM, typed ownership and bounded mix buffers; CPU and actual native 2/4/6/8-channel PCM checks cover current behavior. Other platforms and physical listening remain unverified. |
