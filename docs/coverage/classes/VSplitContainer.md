# VSplitContainer API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/VSplitContainer.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/VSplitContainer.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [SplitContainer](SplitContainer.md). Electron2D type: [`public class Electron2D.VSplitContainer`](../../classes/VSplitContainer.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class VSplitContainer`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/VSplitContainer.xml) | [`public class Electron2D.VSplitContainer`](../../classes/VSplitContainer.md) | Implemented | ADRs 0081/0083 with 0008/0023: real multi-panel layouts, relative copied offset arrays, weighted/capped defaults, bounds/active-priority overlap clamping, structural desired-size preservation, RTL/collapse, internal drag areas/custom children, touch images and nested orthogonal intersections execute. SplitContainerTests covers geometry, storage/int-array revert, lifecycle/guards, failed observers, overflow preflight and reentry/transform recovery. Native checks pass 15 pixel/input plus six simultaneous two-axis intersection phases on Linux Wayland GPU/compatibility; software checks six programmatic layout/touch-image phases because dummy has no system cursors. After 20 warmup frames, 64 active offset/resize and 64 idle intervals allocate zero managed bytes. Editor highlighting/native semantics, native allocations, other platforms and owner acceptance remain separate. |
