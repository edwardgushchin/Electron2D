# HScrollBar API coverage

Last updated: 2026-09-30

Godot source: [doc/classes/HScrollBar.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/HScrollBar.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [ScrollBar](ScrollBar.md). Electron2D type: [`public class Electron2D.HScrollBar`](../../classes/HScrollBar.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class HScrollBar`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/HScrollBar.xml) | [`public class Electron2D.HScrollBar`](../../classes/HScrollBar.md) | Implemented | ADRs 0008/0038/0083: ScrollBar and ScrollContainer execute themed range and clipped content with typed wheel/pan/touch, internal bars/hints, focus and scene packing. ScrollBarTests, ScrollContainerTests, ScrollThemeTests and Linux Wayland GPU/compatibility ScrollRenderingTests verify managed and pixel behavior; 64 warmed active frames allocate zero managed bytes. Native allocation, other platforms and owner acceptance remain unverified. |
| [`theme_item int padding_bottom = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/HScrollBar.xml) | [`public System.Int32 GetThemeConstant(System.String name, System.String themeType = "")`](../../classes/Control.md) | Implemented | ADRs 0008/0038/0083: ScrollBar and ScrollContainer execute themed range and clipped content with typed wheel/pan/touch, internal bars/hints, focus and scene packing. ScrollBarTests, ScrollContainerTests, ScrollThemeTests and Linux Wayland GPU/compatibility ScrollRenderingTests verify managed and pixel behavior; 64 warmed active frames allocate zero managed bytes. Native allocation, other platforms and owner acceptance remain unverified. |
| [`theme_item int padding_top = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/HScrollBar.xml) | [`public System.Int32 GetThemeConstant(System.String name, System.String themeType = "")`](../../classes/Control.md) | Implemented | ADRs 0008/0038/0083: ScrollBar and ScrollContainer execute themed range and clipped content with typed wheel/pan/touch, internal bars/hints, focus and scene packing. ScrollBarTests, ScrollContainerTests, ScrollThemeTests and Linux Wayland GPU/compatibility ScrollRenderingTests verify managed and pixel behavior; 64 warmed active frames allocate zero managed bytes. Native allocation, other platforms and owner acceptance remain unverified. |
