# ShaderInclude API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/ShaderInclude.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ShaderInclude.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Resource](Resource.md). Electron2D type: [`public sealed class Electron2D.ShaderInclude`](../../classes/ShaderInclude.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class ShaderInclude`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ShaderInclude.xml) | [`public sealed class Electron2D.ShaderInclude`](../../classes/ShaderInclude.md) | Implemented | ADR 0028 uses bounded typed ShaderInclude.Code authoring, source loader/saver and typed archive persistence; source edits are saved and dependent HLSL/GLSL compilation occurs at build/import, preserving the compiled runtime boundary. ShaderIncludeTests and check_includes.py verify edit notifications, rollback, copy/cache/lifetime, actual nested dependency compiler behavior and changed GPU pixels; foreign toolchain/host gates remain separate. |
| [`property String code = ""`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ShaderInclude.xml) | [`public System.String Code { get; set; }`](../../classes/ShaderInclude.md) | Implemented | ADR 0028 uses bounded typed ShaderInclude.Code authoring, source loader/saver and typed archive persistence; source edits are saved and dependent HLSL/GLSL compilation occurs at build/import, preserving the compiled runtime boundary. ShaderIncludeTests and check_includes.py verify edit notifications, rollback, copy/cache/lifetime, actual nested dependency compiler behavior and changed GPU pixels; foreign toolchain/host gates remain separate. |
