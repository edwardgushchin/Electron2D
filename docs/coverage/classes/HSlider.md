# HSlider API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/HSlider.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/HSlider.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Slider](Slider.md). Electron2D type: [`public class Electron2D.HSlider`](../../classes/HSlider.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class HSlider`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/HSlider.xml) | [`public class Electron2D.HSlider`](../../classes/HSlider.md) | Implemented | ADR 0080/0083: concrete horizontal/vertical sliders consume shared Range values, complete themed state drawing/ticks, pointer drag/wheel, directional/Home/End actions and joypad repeat, with exact default descriptors and scene factories. Font and inherited native semantic accessibility retain separate dependencies; built-in nonunit icon scaling remains a ThemeDB default-asset integration. Managed SliderTests cover nested peer gestures, lifecycle, repeat and packing; warmed reused input cycles allocate zero managed bytes. SliderRenderingTests verifies eight queued-SDL pixel/input phases plus 64 measured active frames after 64 warmup frames with zero managed bytes on Linux Wayland GPU and compatibility. |
