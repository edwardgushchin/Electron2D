# SceneTreeTimer API coverage

Last updated: 2026-09-24

Godot source: [doc/classes/SceneTreeTimer.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/SceneTreeTimer.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [RefCounted](RefCounted.md). Electron2D type: [`public sealed class Electron2D.SceneTreeTimer`](../../classes/SceneTreeTimer.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class SceneTreeTimer`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/SceneTreeTimer.xml) | [`public sealed class Electron2D.SceneTreeTimer`](../../classes/SceneTreeTimer.md) | Implemented | One-shot tree-owned RefCounted role maps to managed ElectronObject lifetime under ADRs 0003/0014; SceneTreeTimerTests and VerifySceneTreeGroupsEventsAndTimers cover both frame lanes, pause, expiry and disposal. |
| [`property float time_left`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/SceneTreeTimer.xml) | [`public System.Double TimeLeft { get; set; }`](../../classes/SceneTreeTimer.md) | Implemented | Mutable finite non-negative seconds, frame decrement and zero-before-timeout match the timer contract; invalid assignment preserves state. SceneTreeTimerTests covers scale-zero and direct frames. |
| [`signal timeout() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/SceneTreeTimer.xml) | [`public event System.Action<Electron2D.SceneTreeTimer> Timeout`](../../classes/SceneTreeTimer.md) | Implemented | Typed synchronous event fires once at zero after node callbacks; disposal follows even on handler failure and later timers continue. VerifySceneTreeGroupsEventsAndTimers and SceneTreeTimerTests cover ordering and lifetime. |
