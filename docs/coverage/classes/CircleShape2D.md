# CircleShape2D API coverage

Last updated: 2026-09-24

Godot source: [doc/classes/CircleShape2D.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CircleShape2D.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Shape2D](Shape2D.md). Electron2D type: [`public sealed class Electron2D.CircleShape`](../../classes/CircleShape.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class CircleShape2D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CircleShape2D.xml) | [`public sealed class Electron2D.CircleShape`](../../classes/CircleShape.md) | Implemented | Complete own shape dimensions and bounds execute; inherited Shape queries retain separate rows. Shared --rounded-cores checks add coincident circle/capsule/segment and collapsed/point physical response, transformed scene/raw slot identity, unequal-mass center/momentum and strict zero warmed managed bytes; native primitive unit normals, unequal-radius surface midpoints and both anchors execute. See docs/components/physics-contact-policy.md#coincident-rounded-cores for bounds and remaining gates. |
| [`property float radius = 10.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CircleShape2D.xml) | [`public System.Single Radius { get; set; }`](../../classes/CircleShape.md) | Implemented | Pinned defaults and typed behavior execute on explicitly selected public CPU/GPU scene worlds; PhysicsBodyTests covers shape geometry, gravity, contact resolution, impulse, collision masks, live shape edits, scene packing, failure recovery and disposal. The complete common suite now runs both variants in the ordinary full entry, retaining numerical bounds and 64-frame warm resting/active/moving gates at 0/0 owner/all-thread bytes. SIMD/worker internals remain CPU diagnostics; wider inherited/extension/platform/performance acceptance stays open. |
