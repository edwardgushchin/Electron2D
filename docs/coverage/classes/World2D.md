# World2D API coverage

Last updated: 2026-09-25

Godot source: [doc/classes/World2D.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/World2D.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Resource](Resource.md). Electron2D type: [`public sealed class Electron2D.World2D`](../../classes/World2D.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class World2D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/World2D.xml) | [`public sealed class Electron2D.World2D`](../../classes/World2D.md) | Partial | SceneTree-shared Space and DirectSpaceState execute; Canvas and NavigationMap still require renderer/navigation resource ownership (ADR 0063). |
| [`property RID canvas`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/World2D.xml) | — | Blocked | Trigger: renderer canvas RID and navigation-map RID lifetimes must be attached to the same World2D; physics space identity alone does not implement those server domains (ADRs 0028, 0052 and 0063). |
| [`property PhysicsDirectSpaceState2D direct_space_state`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/World2D.xml) | [`public Electron2D.PhysicsDirectSpaceState2D DirectSpaceState { get;  }`](../../classes/World2D.md) | Implemented | The live SceneTree world exposes one stable physics-space RID and the same typed direct query view returned by PhysicsServer2D (ADR 0063, PhysicsQueryTests). |
| [`property RID navigation_map`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/World2D.xml) | — | Blocked | Trigger: renderer canvas RID and navigation-map RID lifetimes must be attached to the same World2D; physics space identity alone does not implement those server domains (ADRs 0028, 0052 and 0063). |
| [`property RID space`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/World2D.xml) | [`public Electron2D.RID Space { get;  }`](../../classes/World2D.md) | Implemented | The live SceneTree world exposes one stable physics-space RID and the same typed direct query view returned by PhysicsServer2D (ADR 0063, PhysicsQueryTests). |
