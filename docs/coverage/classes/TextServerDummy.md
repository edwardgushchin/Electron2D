# TextServerDummy API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/TextServerDummy.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/TextServerDummy.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [TextServerExtension](TextServerExtension.md). Electron2D type: [`public sealed class Electron2D.TextServerDummy`](../../classes/TextServerDummy.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class TextServerDummy`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/TextServerDummy.xml) | [`public sealed class Electron2D.TextServerDummy`](../../classes/TextServerDummy.md) | Implemented | ADR 0107: independently constructible explicit portable disabled-text profile, with no capabilities/resources/shaping/output, false creation-work results, typed empty geometry and missing-position sentinels. Foreign resources cannot mutate/retire through Dummy. Shared consumers suppress text while graphics and logical editing remain usable. The sealed empty-factory exception preserves strict external-provider identity rules. Declaration implementation does not establish the full ADR 0021 package/host matrix. |
