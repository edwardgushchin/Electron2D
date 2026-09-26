# StyleBoxEmpty API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/StyleBoxEmpty.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/StyleBoxEmpty.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [StyleBox](StyleBox.md). Electron2D type: [`public class Electron2D.StyleBoxEmpty`](../../classes/StyleBoxEmpty.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class StyleBoxEmpty`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/StyleBoxEmpty.xml) | [`public class Electron2D.StyleBoxEmpty`](../../classes/StyleBoxEmpty.md) | Implemented | ADR 0082: typed StyleBox margins/minimum/mask/draw hooks and current-item context execute through retained CanvasItem drawing. Texture style preserves axis policies, atlas-before-expand geometry, borrowed texture and exact resource copies; Empty and Line implement their real margin/draw semantics. Global Theme lookup and StyleBoxFlat/control skins remain separate capabilities. StyleBoxTests verifies defaults/notifications/hooks/scope/failure/copy/lifetime/concurrency and 64 warmed recording/replay cycles without managed allocation. StyleBoxRenderingTests verifies nine axis combinations, fractional/atlas/line/empty pixels and 64 warmed mutation/recording/render frames without managed allocation on Linux Wayland GPU/compatibility; native allocator and other-platform acceptance are not claimed. |
