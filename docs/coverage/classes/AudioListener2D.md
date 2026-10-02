# AudioListener2D API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/AudioListener2D.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioListener2D.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Node2D](Node2D.md). Electron2D type: [`public sealed class Electron2D.AudioListener`](../../classes/AudioListener.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class AudioListener2D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioListener2D.xml) | [`public sealed class Electron2D.AudioListener`](../../classes/AudioListener.md) | Implemented | ADR 0047: spatial FAudio source matrices, viewport listener selection, distance/pan, Area point-query bus routing and typed scene state execute. AudioSpatialTests checks Linux Wayland GPU/compatibility native PCM and warmed allocation; other platforms and physical listening remain unverified. |
| [`method clear_current() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioListener2D.xml) | [`public System.Void ClearCurrent()`](../../classes/AudioListener.md) | Implemented | ADR 0047: spatial FAudio source matrices, viewport listener selection, distance/pan, Area point-query bus routing and typed scene state execute. AudioSpatialTests checks Linux Wayland GPU/compatibility native PCM and warmed allocation; other platforms and physical listening remain unverified. |
| [`method is_current() -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioListener2D.xml) | [`public System.Boolean IsCurrent()`](../../classes/AudioListener.md) | Implemented | ADR 0047: spatial FAudio source matrices, viewport listener selection, distance/pan, Area point-query bus routing and typed scene state execute. AudioSpatialTests checks Linux Wayland GPU/compatibility native PCM and warmed allocation; other platforms and physical listening remain unverified. |
| [`method make_current() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AudioListener2D.xml) | [`public System.Void MakeCurrent()`](../../classes/AudioListener.md) | Implemented | ADR 0047: spatial FAudio source matrices, viewport listener selection, distance/pan, Area point-query bus routing and typed scene state execute. AudioSpatialTests checks Linux Wayland GPU/compatibility native PCM and warmed allocation; other platforms and physical listening remain unverified. |
