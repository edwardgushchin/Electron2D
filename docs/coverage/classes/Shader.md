# Shader API coverage

Last updated: 2026-09-22

Godot source: [doc/classes/Shader.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Shader.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Resource](Resource.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class Shader`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Shader.xml) | — | Blocked | Rendering2D: trigger is the first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`enum Mode`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Shader.xml) | — | Blocked | Rendering2D: trigger is the first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`enum_value MODE_CANVAS_ITEM [Mode] = 1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Shader.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`enum_value MODE_FOG [Mode] = 4`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Shader.xml) | — | Blocked | Rendering2D: trigger is the first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`enum_value MODE_PARTICLES [Mode] = 2`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Shader.xml) | — | Blocked | Rendering2D: trigger is the first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`enum_value MODE_SKY [Mode] = 3`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Shader.xml) | — | Blocked | Rendering2D: trigger is the first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`enum_value MODE_SPATIAL [Mode] = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Shader.xml) | — | Blocked | Rendering2D: trigger is the first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`enum_value MODE_TEXTURE_BLIT [Mode] = 5`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Shader.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`method get_default_texture_parameter(StringName name, int index = 0) -> Texture`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Shader.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`method get_mode() -> int [Shader.Mode]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Shader.xml) | — | Blocked | Rendering2D: trigger is the first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`method get_shader_uniform_list(bool get_groups = false) -> Array`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Shader.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`method inspect_native_shader_code() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Shader.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`method set_default_texture_parameter(StringName name, Texture texture, int index = 0) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Shader.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`property String code = ""`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Shader.xml) | — | Blocked | Rendering2D: trigger is the first SDL3 GPU 2D rendering slice (ADR 0028). |
