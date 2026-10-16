# ShaderIncludeDB

Last updated: 2026-10-16

`ShaderIncludeDB : ElectronObject` is the static discovery service for the portable shader fragments embedded in Electron2D.dll. Lookup works before renderer startup and on worker threads. There is no public constructor or instance accessor.

```csharp
var names = ShaderIncludeDB.ListBuiltInIncludeFiles();
var source = ShaderIncludeDB.GetBuiltInIncludeFile("electron2d/color.hlslinc");
using var editableCopy = new ShaderInclude { Code = source };
```

## Members

| Member | Behavior |
| --- | --- |
| `GetBuiltInIncludeFile(string filename)` | Returns immutable source, or an empty string for a missing name. |
| `HasBuiltInIncludeFile(string filename)` | Tests the exact registered name. |
| `ListBuiltInIncludeFiles()` | Returns a new array of names in ordinal order. |

Names are literal and case-sensitive, with forward slashes. Lookup does not normalize paths, read project files or invoke compilers. Null names throw `ArgumentNullException`. Repeated prepared source/existence lookup allocates no managed bytes; listing intentionally copies its result. The service retains its internal object and immutable source for process lifetime.

## Portable color library

The library contains `electron2d/color.hlslinc` and `electron2d/color.glslinc`. Include the matching file from HLSL or GLSL; GLSL also requires `#extension GL_GOOGLE_include_directive : require`. The import tool supplies these files to native preprocessing, including nested includes, guards and inactive branches.

Each language provides `Electron2D_SRGBToLinear` and `Electron2D_LinearToSRGB` overloads for scalar, RGB and RGBA values. RGBA overloads preserve alpha. The transfer functions retain negative and HDR component values rather than clamping them. HLSL uses `float`, `float3`, `float4`; GLSL uses `float`, `vec3`, `vec4`. Internal canvas color processing consumes the same HLSL fragment. Project files follow normal compiler include precedence; avoid shadowing these names.

These portable files are independent of the selected graphics driver. They expose no native buffer layouts or driver-specific shader internals. Source discovery does not establish backend execution support. Games consume compiled SPIR-V through the ordinary shader API and require no source compiler.

## Verification

`ShaderIncludeDBTests.RunManaged` exercises startup-independent discovery, exact names, missing/null cases, isolated results, ordinary source-resource authoring, worker lookup and 1024 prepared zero-byte lookups. `tools/shaders/check_builtin_includes.py` imports nested guarded HLSL/GLSL fragments and device vertex/compute stages, preserves unchanged bytecode/timestamps and retains prior output on missing includes. `RunNative` compares scalar/RGB/RGBA conversions, negative/HDR values, transfer thresholds, round trips and alpha against CPU results in actual rendered pixels.

Linux x64 Wayland and X11 software-Vulkan runs pass both-language pixels plus HDR color/modulation/source regressions. A separate public-only JIT/NativeAOT consumer verifies embedded source delivery, fresh author/read processes, worker lookup and 1024 zero-byte lookups; the AOT process loads no CoreCLR and ships no shader compiler. Other operating systems, physical graphics hardware, native-allocation and owner acceptance remain separate gates.

## Decisions

- [Rendering and shader import](../decisions/rendering.md#adr-0028)
- [Static service access](../decisions/singleton-services.md#adr-0095)
