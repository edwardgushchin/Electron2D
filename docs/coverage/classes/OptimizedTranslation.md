# OptimizedTranslation API coverage

Last updated: 2026-09-24

Godot source: [doc/classes/OptimizedTranslation.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/OptimizedTranslation.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Translation](Translation.md). Electron2D type: [`public sealed class Electron2D.OptimizedTranslation`](../../classes/OptimizedTranslation.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class OptimizedTranslation`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/OptimizedTranslation.xml) | [`public sealed class Electron2D.OptimizedTranslation`](../../classes/OptimizedTranslation.md) | Implemented | ADR 0007 adapts the pinned tool-only generation gate to explicit authoring in the shared assembly and replaces the Smaz tables with a versioned hash-keyed raw/Brotli payload under ADR 0013. Null and context-only inputs preserve the previous catalog. Lookup ignores context and uses the first form. Source keys stay hidden. The public resource locale consumer verifies fresh-process persistence, copied and borrowed lifetime, validation and locale selection. |
| [`method generate(Translation from) -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/OptimizedTranslation.xml) | [`public System.Boolean Generate(Electron2D.Translation source)`](../../classes/OptimizedTranslation.md) | Implemented | ADR 0007 adapts the pinned tool-only generation gate to explicit authoring in the shared assembly and replaces the Smaz tables with a versioned hash-keyed raw/Brotli payload under ADR 0013. Null and context-only inputs preserve the previous catalog. Lookup ignores context and uses the first form. Source keys stay hidden. The public resource locale consumer verifies fresh-process persistence, copied and borrowed lifetime, validation and locale selection. |
