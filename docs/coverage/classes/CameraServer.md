# CameraServer API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/CameraServer.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CameraServer.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Object](Object.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class CameraServer`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CameraServer.xml) | — | Blocked | Trigger: first native camera-capture host slice with device lifetime and 2D texture delivery (ADR 0021). |
| [`enum FeedImage`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CameraServer.xml) | — | Blocked | Trigger: first native camera-capture host slice with device lifetime and 2D texture delivery (ADR 0021). |
| [`enum_value FEED_CBCR_IMAGE [FeedImage] = 1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CameraServer.xml) | — | Blocked | Trigger: first native camera-capture host slice with device lifetime and 2D texture delivery (ADR 0021). |
| [`enum_value FEED_RGBA_IMAGE [FeedImage] = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CameraServer.xml) | — | Blocked | Trigger: first native camera-capture host slice with device lifetime and 2D texture delivery (ADR 0021). |
| [`enum_value FEED_YCBCR_IMAGE [FeedImage] = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CameraServer.xml) | — | Blocked | Trigger: first native camera-capture host slice with device lifetime and 2D texture delivery (ADR 0021). |
| [`enum_value FEED_Y_IMAGE [FeedImage] = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CameraServer.xml) | — | Blocked | Trigger: first native camera-capture host slice with device lifetime and 2D texture delivery (ADR 0021). |
| [`method add_feed(CameraFeed feed) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CameraServer.xml) | — | Blocked | Trigger: first native camera-capture host slice with device lifetime and 2D texture delivery (ADR 0021). |
| [`method feeds() -> CameraFeed[]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CameraServer.xml) | — | Blocked | Trigger: first native camera-capture host slice with device lifetime and 2D texture delivery (ADR 0021). |
| [`method get_feed(int index) -> CameraFeed`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CameraServer.xml) | — | Blocked | Trigger: first native camera-capture host slice with device lifetime and 2D texture delivery (ADR 0021). |
| [`method get_feed_count() -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CameraServer.xml) | — | Blocked | Trigger: first native camera-capture host slice with device lifetime and 2D texture delivery (ADR 0021). |
| [`method remove_feed(CameraFeed feed) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CameraServer.xml) | — | Blocked | Trigger: first native camera-capture host slice with device lifetime and 2D texture delivery (ADR 0021). |
| [`property bool monitoring_feeds = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CameraServer.xml) | — | Blocked | Trigger: first native camera-capture host slice with device lifetime and 2D texture delivery (ADR 0021). |
| [`signal camera_feed_added(int id) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CameraServer.xml) | — | Blocked | Trigger: first native camera-capture host slice with device lifetime and 2D texture delivery (ADR 0021). |
| [`signal camera_feed_removed(int id) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CameraServer.xml) | — | Blocked | Trigger: first native camera-capture host slice with device lifetime and 2D texture delivery (ADR 0021). |
| [`signal camera_feeds_updated() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CameraServer.xml) | — | Blocked | Trigger: first native camera-capture host slice with device lifetime and 2D texture delivery (ADR 0021). |
