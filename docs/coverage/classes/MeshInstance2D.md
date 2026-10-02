# MeshInstance2D API coverage

Last updated: 2026-10-02

Godot source: [doc/classes/MeshInstance2D.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MeshInstance2D.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Node2D](Node2D.md). Electron2D type: [`public class Electron2D.MeshInstance`](../../classes/MeshInstance.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class MeshInstance2D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MeshInstance2D.xml) | [`public class Electron2D.MeshInstance`](../../classes/MeshInstance.md) | Implemented | ADR 0092: executable copied typed 2D surfaces, all five topologies, live packed vertex/RGBA8/UV updates, surface materials, borrowed/owned RID lifetime, node/canvas/server integration. Managed and native GPU/compatibility pixel/warm replay checks distinguish platform/native-allocation limits. |
| [`property Mesh mesh`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MeshInstance2D.xml) | [`public Electron2D.Mesh Mesh { get; set; }`](../../classes/MeshInstance.md) | Implemented | ADR 0092: executable copied typed 2D surfaces, all five topologies, live packed vertex/RGBA8/UV updates, surface materials, borrowed/owned RID lifetime, node/canvas/server integration. Managed and native GPU/compatibility pixel/warm replay checks distinguish platform/native-allocation limits. |
| [`property Texture2D texture`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MeshInstance2D.xml) | [`public Electron2D.Texture Texture { get; set; }`](../../classes/MeshInstance.md) | Implemented | ADR 0092: executable copied typed 2D surfaces, all five topologies, live packed vertex/RGBA8/UV updates, surface materials, borrowed/owned RID lifetime, node/canvas/server integration. Managed and native GPU/compatibility pixel/warm replay checks distinguish platform/native-allocation limits. |
| [`signal texture_changed() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/MeshInstance2D.xml) | [`public event System.Action TextureChanged`](../../classes/MeshInstance.md) | Implemented | ADR 0092: executable copied typed 2D surfaces, all five topologies, live packed vertex/RGBA8/UV updates, surface materials, borrowed/owned RID lifetime, node/canvas/server integration. Managed and native GPU/compatibility pixel/warm replay checks distinguish platform/native-allocation limits. |
