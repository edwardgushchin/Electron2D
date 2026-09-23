# OptimizedTranslation API coverage

Last updated: 2026-09-24

Godot source: [doc/classes/OptimizedTranslation.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/OptimizedTranslation.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Translation](Translation.md). Electron2D type: [`public sealed class Electron2D.OptimizedTranslation`](../../classes/OptimizedTranslation.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class OptimizedTranslation`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/OptimizedTranslation.xml) | [`public sealed class Electron2D.OptimizedTranslation`](../../classes/OptimizedTranslation.md) | Partial | Generate creates an in-memory hash-keyed Brotli catalog, discards source text and contextual entries, resolves singular compressed values and duplicates independently. VerifyTranslations covers lookup, registration, copying, empty generation and disposal. The pinned Smaz binary representation, editor-only generation gate and asset-file persistence are absent; the shared runtime/editor assembly currently permits explicit generation at runtime. |
| [`method generate(Translation from) -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/OptimizedTranslation.xml) | [`public System.Boolean Generate(Electron2D.Translation source)`](../../classes/OptimizedTranslation.md) | Partial | Generate creates an in-memory hash-keyed Brotli catalog, discards source text and contextual entries, resolves singular compressed values and duplicates independently. VerifyTranslations covers lookup, registration, copying, empty generation and disposal. The pinned Smaz binary representation, editor-only generation gate and asset-file persistence are absent; the shared runtime/editor assembly currently permits explicit generation at runtime. |
