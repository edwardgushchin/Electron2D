# SceneTreeTimer API coverage

Last updated: 2026-09-22

Godot source: [doc/classes/SceneTreeTimer.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/SceneTreeTimer.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [RefCounted](RefCounted.md). Electron2D type: [`public sealed class Electron2D.SceneTreeTimer`](../../classes/SceneTreeTimer.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class SceneTreeTimer`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/SceneTreeTimer.xml) | [`public sealed class Electron2D.SceneTreeTimer`](../../classes/SceneTreeTimer.md) | Partial | Typed C# type exists; inheritance, signatures and behavior require row-level audit. |
| [`property float time_left`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/SceneTreeTimer.xml) | [`public System.Double TimeLeft { get; set; }`](../../classes/SceneTreeTimer.md) | Partial | Declaration mapping is structural; return/default/value and observable behavior require audit. |
| [`signal timeout() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/SceneTreeTimer.xml) | [`public event System.Action<Electron2D.SceneTreeTimer> Timeout`](../../classes/SceneTreeTimer.md) | Partial | Declaration mapping is structural; return/default/value and observable behavior require audit. |
