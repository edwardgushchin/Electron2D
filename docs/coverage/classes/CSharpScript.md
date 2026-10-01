# CSharpScript API coverage

Last updated: 2026-10-01

Godot source: [modules/mono/doc_classes/CSharpScript.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/mono/doc_classes/CSharpScript.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Script](Script.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

[ADR 0091](../../decisions/scripting.md#adr-0091) maps both reference classes to one future concrete Electron2D `Script : Resource` for C#. No separate CSharpScript type is planned. All source declarations remain accounted for; the resource and its applicable API are not implemented.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class CSharpScript`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/mono/doc_classes/CSharpScript.xml) | — | Blocked | ADR 0091 maps Script and CSharpScript to one future concrete C# Script : Resource, preserving applicable inherited/own capabilities and typed creation without a provider subclass. Trigger: first real script-resource loader/editor/authoring slice with compiled-type registration, source/build association, usable typed metadata, factories and lifetime/failure verification. Ordinary Node callbacks do not implement this resource API; no production Script exists. |
| [`method new() -> Variant`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/mono/doc_classes/CSharpScript.xml) | — | Blocked | ADR 0091 maps Script and CSharpScript to one future concrete C# Script : Resource, preserving applicable inherited/own capabilities and typed creation without a provider subclass. Trigger: first real script-resource loader/editor/authoring slice with compiled-type registration, source/build association, usable typed metadata, factories and lifetime/failure verification. Ordinary Node callbacks do not implement this resource API; no production Script exists. |
