# RichTextEffect API coverage

Last updated: 2026-10-07

Godot source: [doc/classes/RichTextEffect.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RichTextEffect.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Resource](Resource.md). Electron2D type: [`public class Electron2D.RichTextEffect`](../../classes/RichTextEffect.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class RichTextEffect`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RichTextEffect.xml) | [`public class Electron2D.RichTextEffect`](../../classes/RichTextEffect.md) | Implemented | Executable rich document under ADRs 0004/0014/0038/0046/0051/0083: styled scalar paragraphs, borrowed fonts/images, tables/drop caps/lists, typed links and custom effect arguments/state, real canvas glyph effects, root queries, selection/scroll/input and registered scene/resource storage. See rich-text component for verified current backends and exact remaining dependencies. |
| [`method _process_custom_fx(CharFXTransform char_fx) -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RichTextEffect.xml) | [`protected virtual System.Boolean OnProcessCustomFX(Electron2D.CharFXTransform charFX)`](../../classes/RichTextEffect.md) | Implemented | Executable rich document under ADRs 0004/0014/0038/0046/0051/0083: styled scalar paragraphs, borrowed fonts/images, tables/drop caps/lists, typed links and custom effect arguments/state, real canvas glyph effects, root queries, selection/scroll/input and registered scene/resource storage. See rich-text component for verified current backends and exact remaining dependencies. |
