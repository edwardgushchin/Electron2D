# Noise API coverage

Last updated: 2026-09-22

Godot source: [modules/noise/doc_classes/Noise.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/noise/doc_classes/Noise.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Resource](Resource.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class Noise`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/noise/doc_classes/Noise.xml) | — | Unimplemented | Accepted 2D capability; trigger: first procedural 2D curve or noise resource slice after typed resource storage (ADR 0013). |
| [`method get_image(int width, int height, bool invert = false, bool in_3d_space = false, bool normalize = true) -> Image`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/noise/doc_classes/Noise.xml) | — | Unimplemented | Accepted 2D capability; trigger: first procedural 2D curve or noise resource slice after typed resource storage (ADR 0013). |
| [`method get_image_3d(int width, int height, int depth, bool invert = false, bool normalize = true) -> Image[]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/noise/doc_classes/Noise.xml) | — | Unimplemented | Accepted 2D capability; trigger: first procedural 2D curve or noise resource slice after typed resource storage (ADR 0013). |
| [`method get_noise_1d(float x) -> float`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/noise/doc_classes/Noise.xml) | — | Unimplemented | Accepted 2D capability; trigger: first procedural 2D curve or noise resource slice after typed resource storage (ADR 0013). |
| [`method get_noise_2d(float x, float y) -> float`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/noise/doc_classes/Noise.xml) | — | Unimplemented | Accepted 2D capability; trigger: first procedural 2D curve or noise resource slice after typed resource storage (ADR 0013). |
| [`method get_noise_2dv(Vector2 v) -> float`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/noise/doc_classes/Noise.xml) | — | Unimplemented | Accepted 2D capability; trigger: first procedural 2D curve or noise resource slice after typed resource storage (ADR 0013). |
| [`method get_noise_3d(float x, float y, float z) -> float`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/noise/doc_classes/Noise.xml) | — | Unimplemented | Accepted 2D capability; trigger: first procedural 2D curve or noise resource slice after typed resource storage (ADR 0013). |
| [`method get_noise_3dv(Vector3 v) -> float`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/noise/doc_classes/Noise.xml) | — | Excluded | 3D-only signature is outside ADR 0004; no implementation trigger. |
| [`method get_seamless_image(int width, int height, bool invert = false, bool in_3d_space = false, float skirt = 0.1, bool normalize = true) -> Image`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/noise/doc_classes/Noise.xml) | — | Unimplemented | Accepted 2D capability; trigger: first procedural 2D curve or noise resource slice after typed resource storage (ADR 0013). |
| [`method get_seamless_image_3d(int width, int height, int depth, bool invert = false, float skirt = 0.1, bool normalize = true) -> Image[]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/noise/doc_classes/Noise.xml) | — | Unimplemented | Accepted 2D capability; trigger: first procedural 2D curve or noise resource slice after typed resource storage (ADR 0013). |
