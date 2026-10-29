# TextServerFallback API coverage

Last updated: 2026-09-23

Godot source: [modules/text_server_fb/doc_classes/TextServerFallback.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/text_server_fb/doc_classes/TextServerFallback.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [TextServerExtension](TextServerExtension.md). Electron2D type: [`public sealed class Electron2D.TextServerFallback`](../../classes/TextServerFallback.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class TextServerFallback`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/text_server_fb/doc_classes/TextServerFallback.xml) | [`public sealed class Electron2D.TextServerFallback`](../../classes/TextServerFallback.md) | Implemented | ADR 0107: independently constructible Fallback (Built-in), with exact simple/dynamic/bitmap/MSDF features and a real nominal scalar font-worker path. Logical LTR glyphs, native/authored kerning, scalar boundaries, nonbreaking/CRLF wrapping, objects, formatting/editing, shared GUI/raster consumers and simple Unicode case utilities execute. Explicit RTL direction/ranges reject before mutation; Auto/LTR contexts are validated and retained. No contextual shaping, dictionary iteration, security/data lifecycle or synthetic elongation is claimed. Full package/host conformance under ADR 0021 remains open. |
