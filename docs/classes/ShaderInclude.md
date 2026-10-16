# ShaderInclude

Last updated: 2026-10-16

`ShaderInclude : Resource` owns reusable HLSL or GLSL source for the shader build importer. Save it as `.hlslinc` or `.glslinc`, include that file from a shader, and rebuild to produce SPIR-V. Existing loaded shaders keep their compiled program until the caller explicitly replaces it with `Shader.SetSPIRV` or loads a new shader.

## Example

```csharp
using var include = new ShaderInclude { Code = "#define FACTOR 0.5\n" };
ResourceSaver.Save(include, "res://Shaders/factor.hlslinc");
```

Include the file with `#include "factor.hlslinc"` from HLSL or GLSL. GLSL uses `GL_GOOGLE_include_directive`; nested includes, guards, macros and inactive branches are handled by the compiler. The importer runs on every build, preserves unchanged output and reports errors without replacing the last valid SPIR-V.

[ShaderIncludeDB](ShaderIncludeDB.md) supplies the embedded portable color library. Its source can be copied into this resource or included directly by its registered HLSL/GLSL filename during project import.

## Members

| Member | Behavior |
| --- | --- |
| `ShaderInclude()` | Creates empty source. |
| `string Code { get; set; }` | Gets or replaces source. Every successful assignment emits Changed after commitment, including equal text. |

Code rejects null, invalid UTF-16 and strict UTF-8 source above eight MiB. Validation failures preserve the old source. Observer exceptions propagate after commitment. Access after disposal rejects the operation. Operations are serialized per resource; source authoring allocates and is unsuitable for a frame callback.

The stored Code descriptor supports independent Resource duplication, typed archive save/load and cache replacement. ResourceLoader and ResourceSaver recognize the two source extensions. Source loads validate strict UTF-8 before constructing the resource. Source saves use atomic file replacement.

## Verification

ShaderIncludeTests exercises edit/notifications, rollback, duplication, source/archive persistence, cache modes, malformed/oversized text, virtual paths, actual worker loads and lifetime. `tools/shaders/check_includes.py` saves source through the public API, imports nested guarded HLSL/GLSL sources, verifies changed native GPU pixels after a same-timestamp include edit and preserves compiled output on errors. Linux x64 is the exercised profile. Compatibility shader materials reject execution; foreign host/toolchain acceptance is separate.

## Decisions

- [Rendering and shader import](../decisions/rendering.md#adr-0028)
- [Resource and file formats](../decisions/resources.md#adr-0013)
