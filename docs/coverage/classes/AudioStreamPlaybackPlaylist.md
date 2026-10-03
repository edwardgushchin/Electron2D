# AudioStreamPlaybackPlaylist API coverage

Last updated: 2026-09-23

Godot source: [modules/interactive_music/doc_classes/AudioStreamPlaybackPlaylist.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/interactive_music/doc_classes/AudioStreamPlaybackPlaylist.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [AudioStreamPlayback](AudioStreamPlayback.md). Electron2D type: [`public sealed class Electron2D.AudioStreamPlaybackPlaylist`](../../classes/AudioStreamPlaybackPlaylist.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class AudioStreamPlaybackPlaylist`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/interactive_music/doc_classes/AudioStreamPlaybackPlaylist.xml) | [`public sealed class Electron2D.AudioStreamPlaybackPlaylist`](../../classes/AudioStreamPlaybackPlaylist.md) | Implemented | ADR 0047: borrowed fixed-64 list, active-prefix Resource metadata/authoring/copies, fresh order, output-clock sequence/seek/loops, outgoing fade and prepared queued child controls execute. Cohort factories/ownership rollback and callback/cleanup containment are verified by AudioPlaylistTests with exact CPU boundaries, actual FAudio/public hosts and warmed allocation. Empty modulo, cursor reset/precision, stale block transition/padding and final cleanup defects are corrected explicitly; physical/hardware/other-platform gates remain separate. |
