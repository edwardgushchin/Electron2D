# FogMaterial API coverage

Last updated: 2026-09-22

Godot source: [doc/classes/FogMaterial.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/FogMaterial.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Material](Material.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class FogMaterial`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/FogMaterial.xml) | — | Blocked | Rendering2D: trigger is the first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`property Color albedo = Color(1, 1, 1, 1)`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/FogMaterial.xml) | — | Blocked | Rendering2D: trigger is the first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`property float density = 1.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/FogMaterial.xml) | — | Blocked | Rendering2D: trigger is the first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`property Texture3D density_texture`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/FogMaterial.xml) | — | Excluded | 3D-only signature is outside ADR 0004; no implementation trigger. |
| [`property float edge_fade = 0.1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/FogMaterial.xml) | — | Blocked | Rendering2D: trigger is the first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`property Color emission = Color(0, 0, 0, 1)`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/FogMaterial.xml) | — | Blocked | Rendering2D: trigger is the first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`property float height_falloff = 0.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/FogMaterial.xml) | — | Blocked | Rendering2D: trigger is the first SDL3 GPU 2D rendering slice (ADR 0028). |
