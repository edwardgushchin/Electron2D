# ColorPalette API coverage

Last updated: 2026-10-06

Godot source: [doc/classes/ColorPalette.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ColorPalette.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Resource](Resource.md). Electron2D type: [`public sealed class Electron2D.ColorPalette`](../../classes/ColorPalette.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class ColorPalette`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ColorPalette.xml) | [`public sealed class Electron2D.ColorPalette`](../../classes/ColorPalette.md) | Implemented | ADRs 0004/0013/0038/0046/0080/0083: actual color editing through typed Color math, shared numeric/sliding channels, six spatial surfaces, local swatches, palette files, owned embedded popup and completed application-viewport sampling. Defaults, input, scene files and current Linux GPU/compatibility pixels execute; documented cold/tessellation/platform gates remain distinct. |
| [`property PackedColorArray colors = PackedColorArray()`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ColorPalette.xml) | [`public Electron2D.Color[] Colors { get; set; }`](../../classes/ColorPalette.md) | Implemented | ADRs 0004/0013/0038/0046/0080/0083: actual color editing through typed Color math, shared numeric/sliding channels, six spatial surfaces, local swatches, palette files, owned embedded popup and completed application-viewport sampling. Defaults, input, scene files and current Linux GPU/compatibility pixels execute; documented cold/tessellation/platform gates remain distinct. |
