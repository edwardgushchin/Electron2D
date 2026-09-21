# Material API coverage

Last updated: 2026-09-22

Godot source: [doc/classes/Material.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Material.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Resource](Resource.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class Material`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Material.xml) | — | Blocked | Rendering2D: trigger is the first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`constant RENDER_PRIORITY_MAX = 127`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Material.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`constant RENDER_PRIORITY_MIN = -128`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Material.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`method _can_do_next_pass() -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Material.xml) | — | Blocked | Rendering2D: trigger is the first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`method _can_use_render_priority() -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Material.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`method _get_shader_mode() -> int [Shader.Mode]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Material.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`method _get_shader_rid() -> RID`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Material.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`method create_placeholder() -> Resource`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Material.xml) | — | Blocked | Rendering2D: trigger is the first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`method inspect_native_shader_code() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Material.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`property Material next_pass`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Material.xml) | — | Blocked | Rendering2D: trigger is the first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`property int render_priority`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Material.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
