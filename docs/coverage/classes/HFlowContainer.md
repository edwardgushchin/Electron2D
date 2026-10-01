# HFlowContainer API coverage

Last updated: 2026-10-01

Godot source: [doc/classes/HFlowContainer.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/HFlowContainer.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [FlowContainer](FlowContainer.md). Electron2D type: [`public class Electron2D.HFlowContainer`](../../classes/HFlowContainer.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class HFlowContainer`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/HFlowContainer.xml) | [`public class Electron2D.HFlowContainer`](../../classes/HFlowContainer.md) | Implemented | ADR 0081: actual row/column wrapping, integer bound minima/maxima, capped positive-weight extras with per-child truncation, all relative last-wrap alignment choices, RTL/reverse, visibility/top-level/order/resize, typed theme gaps, owner/capture/disposal guards and exact factories/packing execute. FlowContainerTests verifies state/geometry/callbacks/membership/maximum/overflow and zero warmed managed allocations; FlowContainerRenderingTests verifies six pixel states and 64 active plus 64 idle warmed layout/record/render frames on Linux GPU/compatibility/dummy. Nonpositive stored ratios receive no extra space and an oversized first child creates no artificial empty wrap under the pre-release geometry correction. TextureRect fit-mode multi-wrap stabilization enters its first own control slice; inherited accessibility remains separately tracked. |
