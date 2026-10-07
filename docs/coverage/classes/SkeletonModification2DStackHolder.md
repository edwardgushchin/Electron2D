# SkeletonModification2DStackHolder API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/SkeletonModification2DStackHolder.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/SkeletonModification2DStackHolder.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [SkeletonModification2D](SkeletonModification2D.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class SkeletonModification2DStackHolder`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/SkeletonModification2DStackHolder.xml) | — | Blocked | Trigger: borrowed child-stack binding, phase/strength composition, nested execution/cycle guards, copied scene ownership and actual modified pose output over the implemented stack (ADRs 0014/0028/0092). |
| [`method get_held_modification_stack() -> SkeletonModificationStack2D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/SkeletonModification2DStackHolder.xml) | — | Blocked | Trigger: borrowed child-stack binding, phase/strength composition, nested execution/cycle guards, copied scene ownership and actual modified pose output over the implemented stack (ADRs 0014/0028/0092). |
| [`method set_held_modification_stack(SkeletonModificationStack2D held_modification_stack) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/SkeletonModification2DStackHolder.xml) | — | Blocked | Trigger: borrowed child-stack binding, phase/strength composition, nested execution/cycle guards, copied scene ownership and actual modified pose output over the implemented stack (ADRs 0014/0028/0092). |
