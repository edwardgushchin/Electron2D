# TextServerAdvanced API coverage

Last updated: 2026-09-23

Godot source: [modules/text_server_adv/doc_classes/TextServerAdvanced.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/text_server_adv/doc_classes/TextServerAdvanced.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [TextServerExtension](TextServerExtension.md). Electron2D type: [`public sealed class Electron2D.TextServerAdvanced`](../../classes/TextServerAdvanced.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class TextServerAdvanced`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/text_server_adv/doc_classes/TextServerAdvanced.xml) | [`public sealed class Electron2D.TextServerAdvanced`](../../classes/TextServerAdvanced.md) | Implemented | ADR 0107: independently constructible Advanced (ICU/HarfBuzz), the initial public primary; inherited native callbacks execute existing contextual, bidirectional, vertical, font/raster and Unicode operations. Registration, lifetime and real shared consumers are exercised. Declaration implementation is distinct from the outstanding complete package/host conformance matrix in ADR 0021. |
