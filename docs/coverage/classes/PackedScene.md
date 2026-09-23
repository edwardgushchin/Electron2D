# PackedScene API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/PackedScene.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PackedScene.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Resource](Resource.md). Electron2D type: [`public sealed class Electron2D.PackedScene`](../../classes/PackedScene.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class PackedScene`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PackedScene.xml) | [`public sealed class Electron2D.PackedScene`](../../classes/PackedScene.md) | Partial | Typed C# type exists; inheritance, signatures and behavior require row-level audit. |
| [`enum GenEditState`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PackedScene.xml) | [`public enum Electron2D.PackedSceneEditState`](../../classes/PackedSceneEditState.md) | Partial | Declaration mapping is structural; return/default/value and observable behavior require audit. |
| [`enum_value GEN_EDIT_STATE_DISABLED [GenEditState] = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PackedScene.xml) | [`public const Electron2D.PackedSceneEditState Disabled = 0`](../../classes/PackedSceneEditState.md) | Implemented | Numeric identity matches the compiled C# declaration.  |
| [`enum_value GEN_EDIT_STATE_INSTANCE [GenEditState] = 1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PackedScene.xml) | [`public const Electron2D.PackedSceneEditState Instance = 1`](../../classes/PackedSceneEditState.md) | Implemented | Numeric identity matches the compiled C# declaration.  |
| [`enum_value GEN_EDIT_STATE_MAIN [GenEditState] = 2`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PackedScene.xml) | [`public const Electron2D.PackedSceneEditState Main = 2`](../../classes/PackedSceneEditState.md) | Implemented | Numeric identity matches the compiled C# declaration.  |
| [`enum_value GEN_EDIT_STATE_MAIN_INHERITED [GenEditState] = 3`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PackedScene.xml) | [`public const Electron2D.PackedSceneEditState MainInherited = 3`](../../classes/PackedSceneEditState.md) | Implemented | Numeric identity matches the compiled C# declaration.  |
| [`method can_instantiate() -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PackedScene.xml) | [`public System.Boolean CanInstantiate()`](../../classes/PackedScene.md) | Partial | Declaration mapping is structural; return/default/value and observable behavior require audit. |
| [`method get_state() -> SceneState`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PackedScene.xml) | [`public Electron2D.SceneState GetState()`](../../classes/PackedScene.md) | Partial | Typed live SceneState for in-memory scenes; ADR 0023. |
| [`method instantiate(int edit_state [PackedScene.GenEditState] = 0) -> Node`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PackedScene.xml) | [`public Electron2D.Node Instantiate(Electron2D.PackedSceneEditState editState = Disabled)`](../../classes/PackedScene.md) | Partial | Declaration mapping is structural; return/default/value and observable behavior require audit. |
| [`method pack(Node path) -> int [Error]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/PackedScene.xml) | [`public System.Void Pack(Electron2D.Node root)`](../../classes/PackedScene.md) | Partial | Declaration mapping is structural; return/default/value and observable behavior require audit. Return type differs: int → System.Void; audit observable contract. |
