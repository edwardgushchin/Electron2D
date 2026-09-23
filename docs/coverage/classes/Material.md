# Material API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/Material.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Material.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Resource](Resource.md). Electron2D type: [`public abstract class Electron2D.Material`](../../classes/Material.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class Material`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Material.xml) | [`public abstract class Electron2D.Material`](../../classes/Material.md) | Partial | Typed C# type exists; inheritance, signatures and behavior require row-level audit. |
| [`constant RENDER_PRIORITY_MAX = 127`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Material.xml) | — | Blocked | Trigger: first missing operation-specific retained-canvas, texture or shader integration in the existing 2D renderer (ADR 0028). |
| [`constant RENDER_PRIORITY_MIN = -128`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Material.xml) | — | Blocked | Trigger: first missing operation-specific retained-canvas, texture or shader integration in the existing 2D renderer (ADR 0028). |
| [`method _can_do_next_pass() -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Material.xml) | — | Unimplemented | No mapped C# declaration; trigger: next complete Material API slice. |
| [`method _can_use_render_priority() -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Material.xml) | — | Blocked | Trigger: first missing operation-specific retained-canvas, texture or shader integration in the existing 2D renderer (ADR 0028). |
| [`method _get_shader_mode() -> int [Shader.Mode]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Material.xml) | — | Blocked | Trigger: first missing operation-specific retained-canvas, texture or shader integration in the existing 2D renderer (ADR 0028). |
| [`method _get_shader_rid() -> RID`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Material.xml) | — | Blocked | Trigger: first missing operation-specific retained-canvas, texture or shader integration in the existing 2D renderer (ADR 0028). |
| [`method create_placeholder() -> Resource`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Material.xml) | — | Unimplemented | No mapped C# declaration; trigger: next complete Material API slice. |
| [`method inspect_native_shader_code() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Material.xml) | — | Blocked | Trigger: the first editor shader-inspection slice must show actual generated backend code and variants through the public engine API; compiling/reflection alone does not implement this editor UI (ADRs 0027/0028). |
| [`property Material next_pass`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Material.xml) | — | Unimplemented | No mapped C# declaration; trigger: next complete Material API slice. |
| [`property int render_priority`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Material.xml) | — | Blocked | Trigger: first missing operation-specific retained-canvas, texture or shader integration in the existing 2D renderer (ADR 0028). |
