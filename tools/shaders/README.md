# Shader import tool

This build tool imports HLSL, GLSL or compatible SPIR-V into the engine's common
SPIR-V material interface. Games load the resulting bytes through
`Shader.CreateFromSPIRV`; source compilation runs before the game starts.

## Build and publish

The packaged compiler toolchain currently targets Linux x64. Building it requires
Python 3.12 or later, CMake, Ninja, a C++17 compiler and network access for the
first download. Source revisions and archive SHA-256 hashes are pinned in
`toolchain.lock.json`: glslang 16.4.0, SPIRV-Tools v2026.3 and their matching
SPIRV-Headers. These are build tools, outside the runtime project.

```sh
python3 tools/shaders/build_toolchain.py
dotnet publish tools/shaders/ShaderImport.csproj -c Release -r linux-x64 --self-contained true -o /tmp/electron2d-shader-import
/tmp/electron2d-shader-import/Electron2D.ShaderImport shader.frag.glsl fragment shader.spv
```

The published tool carries glslang, `spirv-val`, their source lock and licenses in
`toolchain/`. It invokes those exact paths without searching `PATH`. HLSL uses
SDL3-CS ShaderCross/DXC from the engine's pinned native package. GLSL uses glslang;
all three inputs pass `spirv-val` and the shared runtime interface check before
an atomic output replacement. Identical output bytes preserve the existing file
and its timestamp. Compiler failures preserve the previous artifact.
The GLSL executable has its compiler and optimizer libraries linked statically;
the standard Linux C/C++ runtime libraries remain system dependencies. The build
is pinned at source level; binary reproducibility across C++ toolchains and Linux
distributions is not asserted.

Development invocation uses the same bundled binaries:

```sh
dotnet run --project tools/shaders/ShaderImport.csproj -c Release -- shader.frag.hlsl fragment shader.spv
python3 -B tools/shaders/check.py
```

Arguments are input path, `vertex` or `fragment`, and output path. Vertex import is
used for the engine's built-in canvas program; public Shader resources currently
accept the canvas fragment interface. Compiler diagnostics include source and
stage, with source locations where supplied by the compiler. Relative includes
resolve through the source compiler's normal file rules.

ShaderImport is an engine build tool with internal access to the shared interface
validator and selected native compiler bindings. Games, examples and the future
editor have no such access; they consume the public runtime API and imported
artifacts. The runtime never invokes this tool or starts source compilers.

## Verified source and bytecode profiles

- HLSL uses the pinned `SDL3-CS.Linux.Shadercross` 3.0.0.11 package: DXC's effective
  language version is HLSL 2021, with `ps_6_0` for fragments and `vs_6_0` for the
  internal vertex program, entry point `main`. The pinned native implementation
  enables SPIR-V output, flattened resource arrays, preserved bindings and
  preserved interfaces. The importer does not override the language version;
  `check.py` verifies the compiler macros and emitted fixture bytes.
- GLSL uses explicit `#version 450` in the verified inputs. glslang runs with
  `-V --target-env vulkan1.0 -S frag|vert -e main`. Its preprocessor version and
  Vulkan target are checked by the same suite.
- Every source or external SPIR-V import passes `spirv-val --target-env vulkan1.0`.
  The recorded artifacts use SPIR-V 1.0 and the baseline `Shader` capability.
  A language version or shader-model target does not imply support for every
  feature of that language: the shared [canvas material interface](../../docs/components/shader-materials.md)
  limits uniforms, textures, resource layouts and stage interfaces.

The runtime's structural header check accepts SPIR-V 1.0 through 1.6, followed by
the same resource/interface and backend checks for all input origins. This wider
header range is not a claim that every module or instruction version is valid or
supported. The packaged importer verifies the narrower Vulkan 1.0 baseline;
runtime reflection is not a replacement for instruction-level validation.

## Boolean parameter types

Import preserves bool and bool2/3/4 (GLSL bvec2/3/4), including fixed arrays.
HLSL uses a second compilation through ShaderCross to reflect original DXIL types
with the already packaged DXC library; GLSL uses a second glslang `-gV` pass.
This work occurs only during import. HLSL reflection selects the same Vulkan 1.0
preprocessor macros and matches buffer names to actual SPIR-V bindings, including
`vk::binding` and `ConstantBuffer<T>`. No additional package is required.

The resulting SPIR-V contains standard OpString records of the form
`Electron2D:bool:1:<width>:<array-length>:<base64-utf8-member-name>`.
Width is 1..4; length zero denotes an individual value, 1..1024 a fixed array.
Every record must name a unique reflected member of an active buffer and match
its unsigned 32-bit physical components and array shape. Unknown versions,
invalid names/encoding, duplicate records or layout mismatches reject the import
without replacing prior output. External producers may supply the same records.
Unannotated unsigned storage remains numeric; stripping records loses logical
boolean types. Final output passes `spirv-val` and the ordinary runtime validator.

C# scalar bool uses bool. Boolean vectors use an int mask whose low bits map to
X/Y/Z/W; each array element uses the same mapping. Defaults are false/zero.
See [the complete contract](../../docs/components/shader-materials.md#boolean-type-information).

## Render time input

Canvas fragment programs may declare a scalar `float TIME` in a normal uniform
buffer (HLSL `register(b0, space3)` or GLSL `layout(set = 3, binding = 0, std140)`).
Bindings remain contiguous; TIME can share a buffer with material parameters.
The renderer fills it with scaled, wrapped render seconds. It is excluded from
material parameter setters/descriptors. Wrong types, arrays, texture resources
named TIME and vertex TIME fail the common interface check, including for
externally supplied SPIR-V. See [the complete contract and examples](../../docs/components/shader-materials.md#render-time).

## Project builds

Import the `.targets` file next to the published executable in an SDK C# project:

```xml
<ItemGroup>
  <Electron2DShader Include="Shaders/Glow.hlsl" Stage="fragment" />
  <Electron2DShader Include="Shaders/Outline.glsl" Stage="fragment" />
  <Electron2DShader Include="Shaders/External.spv" Stage="fragment" TargetPath="Shaders/External.spv" />
</ItemGroup>
<Import Project="/path/to/published/importer/Electron2D.Shaders.targets" />
```

Use `dotnet build` or `dotnet publish`. The targets invoke the adjacent importer;
`Electron2DShaderImportTool` can override its executable path. Import runs before
C# compilation and failures stop the build. Paths go through process arguments,
without a shell. Current host verification is Linux x64 using dotnet MSBuild.

Each item requires `Stage="vertex"` or `Stage="fragment"`; the public canvas
Shader accepts fragment programs. The default asset path is the item's `Link`
metadata, or its relative `Include` path, with `.spv` appended. Thus sources with
the same stem retain distinct `.hlsl.spv` and `.glsl.spv` outputs. Use `TargetPath`
for a custom relative `.spv` asset name. Absolute source paths and sources outside
the project need a relative `Link` or `TargetPath`. Target paths cannot contain
empty, `.` or `..` components; duplicate shader target paths fail before import.

Generated files live under `$(IntermediateOutputPath)Electron2D/shaders/` and
copy to the normal build/publish directory with their asset paths. Only compiled
SPIR-V is copied by this integration; source files and compiler tools are not game
dependencies. Games can read these files and call `Shader.CreateFromSPIRV`.
`dotnet clean` removes generated outputs. `dotnet publish --no-build` reuses the
last build's artifacts; it intentionally does not import changed sources.
With SDK 10.0.101, an apostrophe in an absolute publish directory breaks the
SDK's own item transform even in a project without this integration. Use a
relative `-p:PublishDir=publish/` for such project paths.

Compilation and validation run on every build, including when C# is unchanged.
This keeps the native compilers authoritative for nested includes, macros and
include guards, even if a dependency changes without a timestamp change.
Unchanged bytecode avoids file replacement and downstream copies. This is output
reuse, not a cache that skips compilation: a complete dependency cache remains
unfinished, and large shader projects currently pay compilation cost each build.

Verify a published importer and its build integration with:

```sh
python3 -B tools/shaders/check.py --tool /path/to/published/importer/Electron2D.ShaderImport
python3 -B tools/shaders/check_build.py --tool /path/to/published/importer/Electron2D.ShaderImport
```
