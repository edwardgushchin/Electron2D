# ExternalTexture API coverage

Last updated: 2026-09-22

Godot source: [doc/classes/ExternalTexture.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ExternalTexture.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Texture2D](Texture2D.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class ExternalTexture`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ExternalTexture.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`method get_external_texture_id() -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ExternalTexture.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`method set_external_buffer_id(int external_buffer_id) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ExternalTexture.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`property bool resource_local_to_scene = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ExternalTexture.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`property Vector2 size = Vector2(256, 256)`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ExternalTexture.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
