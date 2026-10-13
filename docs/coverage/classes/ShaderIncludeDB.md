# ShaderIncludeDB API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/ShaderIncludeDB.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ShaderIncludeDB.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Object](Object.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class ShaderIncludeDB`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ShaderIncludeDB.xml) | — | Blocked | Trigger: an installed compiler-consumed built-in include library with stable names, source lookup and all three retained static discovery operations (ADR 0028). ShaderInclude user-source files and native compiler preprocessing now execute; they do not supply a built-in library or invent empty lookup results. |
| [`method get_built_in_include_file(String filename) -> String`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ShaderIncludeDB.xml) | — | Blocked | Trigger: an installed compiler-consumed built-in include library with stable names, source lookup and all three retained static discovery operations (ADR 0028). ShaderInclude user-source files and native compiler preprocessing now execute; they do not supply a built-in library or invent empty lookup results. |
| [`method has_built_in_include_file(String filename) -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ShaderIncludeDB.xml) | — | Blocked | Trigger: an installed compiler-consumed built-in include library with stable names, source lookup and all three retained static discovery operations (ADR 0028). ShaderInclude user-source files and native compiler preprocessing now execute; they do not supply a built-in library or invent empty lookup results. |
| [`method list_built_in_include_files() -> PackedStringArray`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ShaderIncludeDB.xml) | — | Blocked | Trigger: an installed compiler-consumed built-in include library with stable names, source lookup and all three retained static discovery operations (ADR 0028). ShaderInclude user-source files and native compiler preprocessing now execute; they do not supply a built-in library or invent empty lookup results. |
