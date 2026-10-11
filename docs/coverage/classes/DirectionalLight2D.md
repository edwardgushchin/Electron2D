# DirectionalLight2D API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/DirectionalLight2D.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/DirectionalLight2D.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Light2D](Light2D.md). Electron2D type: [`public class Electron2D.DirectionalLight`](../../classes/DirectionalLight.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class DirectionalLight2D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/DirectionalLight2D.xml) | [`public class Electron2D.DirectionalLight`](../../classes/DirectionalLight.md) | Implemented | ADRs 0004/0028/0051/0095: executable GPU light, multichannel shading, ray/edge shadow and shader-requested distance-field consumers; static retained identities, scene/archive ownership and tile occlusion integrate the same behavior. LightingTests and native pixel/allocation checks establish the current Linux profile; compatibility explicitly rejects shader-dependent passes. See docs/components/canvas-lights.md for exact sampling/profile adaptations and remaining gates. |
| [`property float height = 0.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/DirectionalLight2D.xml) | [`public System.Single Height { get; set; }`](../../classes/DirectionalLight.md) | Implemented | ADRs 0004/0028/0051/0095: executable GPU light, multichannel shading, ray/edge shadow and shader-requested distance-field consumers; static retained identities, scene/archive ownership and tile occlusion integrate the same behavior. LightingTests and native pixel/allocation checks establish the current Linux profile; compatibility explicitly rejects shader-dependent passes. See docs/components/canvas-lights.md for exact sampling/profile adaptations and remaining gates. |
| [`property float max_distance = 10000.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/DirectionalLight2D.xml) | [`public System.Single MaxDistance { get; set; }`](../../classes/DirectionalLight.md) | Implemented | ADRs 0004/0028/0051/0095: executable GPU light, multichannel shading, ray/edge shadow and shader-requested distance-field consumers; static retained identities, scene/archive ownership and tile occlusion integrate the same behavior. LightingTests and native pixel/allocation checks establish the current Linux profile; compatibility explicitly rejects shader-dependent passes. See docs/components/canvas-lights.md for exact sampling/profile adaptations and remaining gates. |
