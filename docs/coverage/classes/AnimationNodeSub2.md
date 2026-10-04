# AnimationNodeSub2 API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/AnimationNodeSub2.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AnimationNodeSub2.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [AnimationNodeSync](AnimationNodeSync.md). Electron2D type: [`public sealed class Electron2D.AnimationNodeSub2`](../../classes/AnimationNodeSub2.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class AnimationNodeSub2`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AnimationNodeSub2.xml) | [`public sealed class Electron2D.AnimationNodeSub2`](../../classes/AnimationNodeSub2.md) | Implemented | ADR 0093 typed animation graph execution: reusable borrowed resources, per-tree/path typed parameter cells, clip timing, graph topology/filtering, signed Blend/Add/Sub, TimeScale/TimeSeek and provider integration. AnimationGraphTests verifies editing/cycles, two shared-tree instances, parameters/readonly/errors, arithmetic/filtering/clock edges, reentry/cleanup and zero warmed managed allocations. Two Linux Wayland GPU and two compatibility five-pose readbacks verify real rendered execution. Disk/editor and other-platform acceptance remain separate. |
