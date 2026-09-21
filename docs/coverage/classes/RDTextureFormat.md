# RDTextureFormat API coverage

Last updated: 2026-09-22

Godot source: [doc/classes/RDTextureFormat.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RDTextureFormat.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [RefCounted](RefCounted.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class RDTextureFormat`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RDTextureFormat.xml) | — | Excluded | Direct rendering-device public types conflict with the backend-neutral 2D API decision (ADR 0028). |
| [`method add_shareable_format(int format [RenderingDevice.DataFormat]) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RDTextureFormat.xml) | — | Excluded | Direct rendering-device public types conflict with the backend-neutral 2D API decision (ADR 0028). |
| [`method remove_shareable_format(int format [RenderingDevice.DataFormat]) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RDTextureFormat.xml) | — | Excluded | Direct rendering-device public types conflict with the backend-neutral 2D API decision (ADR 0028). |
| [`property int array_layers = 1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RDTextureFormat.xml) | — | Excluded | Direct rendering-device public types conflict with the backend-neutral 2D API decision (ADR 0028). |
| [`property int depth = 1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RDTextureFormat.xml) | — | Excluded | Direct rendering-device public types conflict with the backend-neutral 2D API decision (ADR 0028). |
| [`property int format [RenderingDevice.DataFormat] = 8`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RDTextureFormat.xml) | — | Excluded | Direct rendering-device public types conflict with the backend-neutral 2D API decision (ADR 0028). |
| [`property int height = 1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RDTextureFormat.xml) | — | Excluded | Direct rendering-device public types conflict with the backend-neutral 2D API decision (ADR 0028). |
| [`property bool is_discardable = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RDTextureFormat.xml) | — | Excluded | Direct rendering-device public types conflict with the backend-neutral 2D API decision (ADR 0028). |
| [`property bool is_resolve_buffer = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RDTextureFormat.xml) | — | Excluded | Direct rendering-device public types conflict with the backend-neutral 2D API decision (ADR 0028). |
| [`property int mipmaps = 1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RDTextureFormat.xml) | — | Excluded | Direct rendering-device public types conflict with the backend-neutral 2D API decision (ADR 0028). |
| [`property int samples [RenderingDevice.TextureSamples] = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RDTextureFormat.xml) | — | Excluded | Direct rendering-device public types conflict with the backend-neutral 2D API decision (ADR 0028). |
| [`property int texture_type [RenderingDevice.TextureType] = 1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RDTextureFormat.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`property int usage_bits [RenderingDevice.TextureUsageBits] = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RDTextureFormat.xml) | — | Excluded | Direct rendering-device public types conflict with the backend-neutral 2D API decision (ADR 0028). |
| [`property int width = 1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RDTextureFormat.xml) | — | Excluded | Direct rendering-device public types conflict with the backend-neutral 2D API decision (ADR 0028). |
