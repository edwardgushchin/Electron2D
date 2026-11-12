# WorldEnvironment API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/WorldEnvironment.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/WorldEnvironment.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Node](Node.md). Electron2D type: [`public sealed class Electron2D.WorldEnvironment`](../../classes/WorldEnvironment.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class WorldEnvironment`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/WorldEnvironment.xml) | [`public sealed class Electron2D.WorldEnvironment`](../../classes/WorldEnvironment.md) | Partial | ADRs 0004/0028: World-selected GPU glow executes in half-float passes with seven levels, borrowed maps and pre-effect history. RGBA8 viewports decode before filtering and encode after composition. Windows native pixels verify byte and half-float output. Canvas mode applies GPU glow before drawing foreground layers. Compatibility needs a native low-end filtering and composition pass. Pinned numeric parity and the complete target matrix remain open. See docs/components/canvas-glow.md. |
| [`property CameraAttributes camera_attributes`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/WorldEnvironment.xml) | — | Excluded | The pinned WorldEnvironment property supplies a default Camera3D attribute resource. Camera3D and its exposure/DOF inputs are outside ADR 0004. |
| [`property Compositor compositor`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/WorldEnvironment.xml) | — | Blocked | Needs a typed canvas callback stage and borrowed render-data lifetime that execute on a native canvas target. The current glow path has built-in passes. The pinned compositor's scene depth, sky, and spatial material producers remain outside ADR 0004. |
| [`property Environment environment`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/WorldEnvironment.xml) | [`public Electron2D.CanvasEnvironment Environment { get; set; }`](../../classes/WorldEnvironment.md) | Partial | ADRs 0004/0028: World-selected GPU glow executes in half-float passes with seven levels, borrowed maps and pre-effect history. RGBA8 viewports decode before filtering and encode after composition. Windows native pixels verify byte and half-float output. Canvas mode applies GPU glow before drawing foreground layers. Compatibility needs a native low-end filtering and composition pass. Pinned numeric parity and the complete target matrix remain open. See docs/components/canvas-glow.md. |
