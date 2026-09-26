# VBoxContainer API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/VBoxContainer.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/VBoxContainer.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [BoxContainer](BoxContainer.md). Electron2D type: [`public class Electron2D.VBoxContainer`](../../classes/VBoxContainer.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class VBoxContainer`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/VBoxContainer.xml) | [`public class Electron2D.VBoxContainer`](../../classes/VBoxContainer.md) | Implemented | ADR 0081: Control size bits/stretch ratios and deferred Container fit/phase callbacks execute through Box/HBox/VBox primary allocation, min/max weighted refit, alignment/RTL, child eligibility and actual spacers. BoxContainerTests verifies state/lifecycle/packing/callbacks, captured batch reuse and zero warmed allocations; native GPU/compatibility tests verify rendered geometry. Accessibility semantic service remains separately blocked. |
