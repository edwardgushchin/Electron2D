# VideoStream

Last updated: 2026-10-16

`public abstract class VideoStream : Resource` in [VideoStream.cs](../../src/Scene/Resources/VideoStream.cs).
Part of [video playback](../components/video-playback.md), governed by [ADR 0110](../decisions/video-playback.md#adr-0110).

A reusable borrowed video source. `File` is stored resource configuration, initially empty. Changing it invalidates the source's prepared compressed bytes and emits Changed; observing players prepare replacement state. Existing playbacks keep their original compressed snapshot. Ordinary Resource naming, duplication and archive identity apply.

`InstantiatePlayback()` returns independent caller-owned state through the required protected `OnInstantiatePlayback()` hook. Custom sources may delegate to another source factory. Null, disposed or player-owned results fail. Recursive factory calls, path changes and source disposal during factory execution fail before mutation. A source must remain live while its playbacks are used.

```csharp
using var source = new VideoStreamTheora { File = "res://intro.ogv" };
using var playback = source.InstantiatePlayback();
playback.Play();
playback.Update(1.0 / 60);
```

| Member | Contract |
| --- | --- |
| protected constructor | Initializes detached source configuration. |
| File | Stored string path; null rejected. |
| InstantiatePlayback / OnInstantiatePlayback | Validated public wrapper and required typed decoder factory. |
| CopyCustomStateTo | Copies the path and immutable compressed snapshot for resource duplication. |
| GetPropertyDescriptors | Stores File through the ordinary Resource graph. |
| ValidateDisposal / Dispose | Rejects disposal during factory execution; releases prepared compressed bytes. |

The concrete source and exact resource factory are documented by [VideoStreamTheora](VideoStreamTheora.md). Construction is cold work; the returned decoder reuses its prepared storage.
