# CenterContainer API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/CenterContainer.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CenterContainer.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Container](Container.md). Electron2D type: [`public class Electron2D.CenterContainer`](../../classes/CenterContainer.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class CenterContainer`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CenterContainer.xml) | [`public class Electron2D.CenterContainer`](../../classes/CenterContainer.md) | Implemented | ADRs 0081/0083: CenterContainer executes floor-centered minimum allocations or local-origin centering, desired/minimum policy, deferred child updates and typed scene capture. LayoutContainersTests checks odd sizes, top-left mode, callbacks, packing and 64 warmed active zero-allocation cycles; LayoutContainersRenderingTests checks pixels on Linux Wayland GPU and compatibility. Native allocation, other platforms and owner acceptance remain unverified. |
| [`property bool use_top_left = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/CenterContainer.xml) | [`public System.Boolean UseTopLeft { get; set; }`](../../classes/CenterContainer.md) | Implemented | ADRs 0081/0083: CenterContainer executes floor-centered minimum allocations or local-origin centering, desired/minimum policy, deferred child updates and typed scene capture. LayoutContainersTests checks odd sizes, top-left mode, callbacks, packing and 64 warmed active zero-allocation cycles; LayoutContainersRenderingTests checks pixels on Linux Wayland GPU and compatibility. Native allocation, other platforms and owner acceptance remain unverified. |
