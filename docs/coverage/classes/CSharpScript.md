# CSharpScript API coverage

Last updated: 2026-10-01

Godot source: [modules/mono/doc_classes/CSharpScript.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/mono/doc_classes/CSharpScript.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Script](Script.md). Electron2D type: [`public sealed class Electron2D.Script`](../../classes/Script.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

[ADR 0091](../../decisions/scripting.md#adr-0091) maps both reference classes to one future concrete Electron2D `Script : Resource` for C#. No separate CSharpScript type is planned. All source declarations remain accounted for; the resource and its applicable API are not implemented.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class CSharpScript`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/mono/doc_classes/CSharpScript.xml) | [`public sealed class Electron2D.Script`](../../classes/Script.md) | Partial | Compiled source/type assets, metadata, typed construction, ordinary callbacks and fresh-process file integration execute under ADR 0091. Implementation Reload/keep-state requires a compiled generation/lifetime/restart or hot-migration contract; RPC metadata requires the high-level scene RPC contract. Live object GetScript/SetScript obligations remain separate; no type-changing identity claim is made. |
| [`method new() -> Variant`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/mono/doc_classes/CSharpScript.xml) | [`public T New<T>()`](../../classes/Script.md)<br>[`public T New<T>(Func<T> constructor)`](../../classes/Script.md) | Implemented | ADR 0091: compiled project Node/Resource source assets use explicit exact factories/typed schemas, matching portable-PDB documents/checksums, typed CLR/default/constant metadata, .cs loader/atomic saver and registered archive identities. ScriptTests constructs actual user classes, restores state/references in a fresh process and executes ordinary callbacks; RichTextLabel consumes compatible compiled resource effects. No live reassignment, Reload/keep-state or RPC completion is claimed. |
