# Shader import tool

This build tool imports HLSL, GLSL or compatible SPIR-V into the engine's common
SPIR-V material interface. Games load the resulting bytes through
`Shader.CreateFromSPIRV`; source compilation runs before the game starts.

## Visual shader graph resources

The same tool imports a saved typed `VisualShader` resource into an editable
resource carrying compiled canvas vertex/fragment programs:

```text
Electron2D.ShaderImport source.e2dres canvas compiled.e2dres --visual-shader
```

Use `Electron2DShader` with `Profile="visual-shader"`, `Stage="canvas"` and an
`.e2dres` TargetPath in the existing SDK target. The default destination appends
`.compiled.e2dres`. Nested output directories and paths with spaces are supported.
Graph edits require reimport; games load the compiled resource through
`ResourceLoader.Load<VisualShader>` and ordinary `ShaderMaterial`. Invalid source
preserves the previous output, and identical results preserve output timestamps.
The [graph contract](../../docs/components/visual-shaders.md) lists supported
numeric nodes, ownership and remaining producers. `check_graphs.py` verifies
direct import and actual SDK edit/error/recovery/build/publish/Clean consumers.

## Build and publish

The compiler bootstrap selects the Linux, macOS or Windows x64/arm64 **host**, independently
of the game's target RID. Linux x64 execution is verified; other hosts require their CI checks.
Building it requires
Python 3.12 or later, CMake, Ninja, a C++17 compiler and network access for the
first download. Source revisions and archive SHA-256 hashes are pinned in
`toolchain.lock.json`: glslang 16.6.0, SPIRV-Tools v2026.4 and their matching
SPIRV-Headers. These are build tools, outside the runtime project.

## Built-in include library

The importer supplies `electron2d/color.hlslinc` and `electron2d/color.glslinc`
from the embedded runtime library. HLSL and GLSL expose
`Electron2D_SRGBToLinear` and `Electron2D_LinearToSRGB` for scalar, RGB and RGBA
values; RGBA preserves alpha, and RGB supports negative and HDR values.
GLSL sources require `#extension GL_GOOGLE_include_directive : require`.

Native compilers handle nested includes, guards, macros and inactive branches.
Project files retain normal native include precedence; avoid shadowing library
names. The tool materializes library source in a private temporary directory and
removes it after import, including failures. Original include roots and source
locations remain available. The library also works with device vertex/fragment/
compute import. Published games load compiled SPIR-V without the import tool.

Public `ShaderIncludeDB` static operations list names, test existence and return
that same source before renderer startup. Missing names return false/empty;
lookup is literal and case-sensitive. The library is portable across graphics
drivers. Internal canvas color processing consumes its HLSL fragment.

## Built-in runtime shaders

`dotnet build Electron2D.csproj` automatically compiles the HLSL rendering sources
and GLSL compute sources through `RuntimeShaders.targets`. The pinned glslang
compiler handles both built-in languages (`main`, Vulkan 1.0); `spirv-val` validates
each result. This bootstrap has no dependency on Electron2D.dll or SDL, so it can
run before the runtime exists and when cross-compiling the runtime.

The resident shape-query source compiles Intersect, Contacts, Rest and Cast into
separate operation modules; mixed requests retain their original result segments
and share one submission. These modules, CCD and body-motion queries use the pinned
compiler's `-O` performance optimization for Vulkan. Each also embeds a
`.comp.portable.spv` variant from the same source without that optimization for
Metal and Direct3D 12. This keeps the translator's generated source bounded while
preserving the measured Vulkan cold-compilation improvement. Both variants pass
`spirv-val` and keep the same resource layout, workgroup and numerical budgets.
Other built-in programs retain their existing compiler settings. See the
[cold pipeline investigation](../../docs/components/physics-network-example.md#cold-pipeline-compilation-2026-10-21)
and [backend verification limits](../../docs/components/gpu-resident-shape-queries.md#backend-compiler-profiles).

The first build prepares the host tools under `tools/shaders/obj/toolchain/`.
Later builds reuse that directory without downloads or native compilation while
the source lock and bootstrap recipe match. Parallel builds serialize tool preparation.
Generated shaders and their content manifest live in the runtime's
`$(IntermediateOutputPath)shaders/`, with the original resource names embedded in
Electron2D.dll. Build inputs include the compiler recipe, all local shader/include
files and output hashes. Unchanged bytes keep their timestamps; failed compilation
fails the build and preserves the last valid artifact. Deleted shaders are removed,
and `dotnet clean` removes generated resources. `publish --no-build` uses the already
built assembly. A game/package consumer needs no Python, CMake or shader compiler.

Generated runtime `.spv` files are excluded from Git. The fixed `.spv` files in
`tests/Electron2D.Tests/Shaders` are a separate reference corpus for external-bytecode
compatibility, malformed-input tests and DXC/GLSL importer regression checks;
they remain versioned so a compiler regression cannot silently rewrite the oracle. The fixed
Canvas HLSL source fixtures live beside this corpus and exercise the material
interface independently of private channels added to built-in renderer programs.
The material check also verifies rejection of the live private glyph interface;
`check_runtime.py` and `check_canvas_glyphs.py` validate built-in delivery and
composition.
The user-material importer below still uses ShaderCross/DXC for HLSL and emits
logical-type metadata. Its output need not be byte-identical to built-in glslang output.

```sh
python3 -B tools/shaders/check_runtime.py
```

This check verifies include invalidation, no-op timestamps, corrupt-output recovery,
failed compilation, stale-output removal, clean, and exact embedded bytes in a fresh
published assembly without shader sources or compiler tools in its output.

## Material importer

```sh
python3 tools/shaders/build_toolchain.py
dotnet publish tools/shaders/ShaderImport.csproj -c Release -r linux-x64 --self-contained true -o /tmp/electron2d-shader-import
/tmp/electron2d-shader-import/Electron2D.ShaderImport shader.frag.glsl fragment shader.spv
```

The published tool carries glslang, `spirv-val`, and their source lock in
`toolchain/`, with all license texts in the single top-level `licence/` directory. It invokes those exact paths without searching `PATH`. HLSL uses
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

Arguments are input path, `vertex` or `fragment`, and output path. Vertex import
validates the canvas program interface; public Shader resources accept matching
vertex/fragment pairs or a fragment using the built-in vertex channels. Compiler diagnostics include source and
stage, with source locations where supplied by the compiler. Relative includes
resolve through the source compiler's normal file rules.

ShaderImport is an engine build tool with internal access to the shared interface
validator and selected native compiler bindings. Games, examples and the future
editor have no such access; they consume the public runtime API and imported
artifacts. The runtime never invokes this tool or starts source compilers.

## Verified source and bytecode profiles

- HLSL uses the pinned `SDL3-CS.Linux.Shadercross` 3.0.0.13 package: DXC's effective
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

GLSL source-type reflection recognizes NonSemantic.Shader.DebugInfo 100, 101 and 102 emitted by the pinned glslang 16.6.0. The import checks cover boolean scalars, vectors and arrays as well as malformed artifact metadata and atomic replacement.

Application compute sources can use `Electron2DComputeShader` items with `ComputeShaders.targets`. GLSL and HLSL are compiled by the same pinned host toolchain, validated and embedded as `Electron2D.Compute.<filename>.spv` resources from the consumer obj directory. The water example and local-compute tests exercise this path. Runtime loading uses RDShaderSPIRV and RenderingDevice; generated bytecode remains untracked.

Built-in lighting generates the 27 diffuse/normal/specular float, signed and unsigned sampling variants from one source. Alpha clipping generates three scalar variants. Variant definitions participate in the build fingerprint; bytecode stays in intermediate output and is embedded in the runtime assembly.

Automatic material lighting embeds 32 internal function profiles and exact ID relocation resources derived with the pinned disassembler. Private vertex variants deliver storage-record indices. Runtime composition needs no compiler tools. `python3 -B tools/shaders/check_material_lighting.py` validates fresh composed GLSL/DXC/SPIR-V modules independently of native rendering.


## Application device graphics

Import `DeviceShaders.targets` and add `Electron2DDeviceShader` items ending in `.vert.glsl`, `.frag.glsl`, `.vert.hlsl` or `.frag.hlsl`. This low-level device profile uses the same pinned glslang frontend and independent `spirv-val` check as compute modules; it preserves shader source/stage diagnostics and atomically replaces completed bytecode. Results are embedded as `Electron2D.Device.<filename>.spv`. Load both stages into `RDShaderSPIRV` and use the public device pipeline/draw API. The device validates paired stage interfaces and logical resources at creation. Canvas material imports retain their separate ShaderImport profile. Source compilers remain build-time tools.

## Device source authoring

The same published tool can import a local-device module:

```sh
Electron2D.ShaderImport simulation.comp.glsl compute simulation.spv --device
Electron2D.ShaderImport geometry.vert.hlsl vertex geometry.spv --device
```

An optional argument after `--device` supplies the include root; otherwise file imports use the input directory. Device imports preserve original descriptors/push/specialization declarations, run the pinned validator and device structural/capability checks, and leave paired interfaces/native validation to pipeline creation. Canvas imports keep the existing default profile and logical-type metadata. Both profiles preserve atomic output replacement and unchanged bytes/timestamps.

Build consumers select `Profile="device"` on `Electron2DShader` items and set Stage to compute, vertex or fragment. The default Profile remains canvas. Invalid profiles/stages/paths reject before output replacement; arguments remain separated without shell interpretation. Ordinary build/publish consumers load the generated SPIR-V through public device APIs and receive no compiler executable or import-tool assembly.

Programmatic authoring uses RDShaderSource and RenderingDevice.ShaderCompileSPIRVFromSource. Select the published executable with ELECTRON2D_SHADER_IMPORT_TOOL, or install it beside the authoring host. This optional cold operation returns copied RDShaderSPIRV stage bytes/diagnostics; it creates no native program. Empty requests need no compiler, while missing prerequisites fail explicitly. Include/preprocessor sources are recompiled rather than cached. Shipped games that load compiled shaders require no authoring tool. [The runtime contract](../../docs/components/local-compute.md#source-authoring-and-build-imports) records bounds, lifetime, caching and native verification.

## Paired canvas programs

Import an authored vertex and matching fragment independently with the vertex/fragment stage arguments, then load both copied payloads through `Shader.CreateFromSPIRV(vertexBytes, fragmentBytes)`. HLSL uses `vs_6_0`/`ps_6_0`; GLSL uses the same Vulkan 1.0 profile. Vertices use a required Frame float2 client-size block at set one/binding zero, up to three following material/Instance blocks, and sampled images at set zero. Frame contains no other fields; renderer uniforms belong in material blocks. The [runtime canvas interface](../../docs/components/shader-materials.md#authored-vertex-stages) defines attributes, framebuffer coordinates, shared names and independent stage limits. `check.py` reproduces the fixed GLSL/HLSL pair and shared-texture/instance fixtures and verifies accepted material TIME versus rejected Frame TIME. Shader source compilation and full instruction validation remain build-time tools.

Use `--texture-blit` for a writable-texture fragment, or `Profile="texture-blit"` on an `Electron2DShader` item. Load its compiled module with `Shader.CreateFromSPIRV(bytes, Shader.Mode.TextureBlit)`. Its source inputs and target outputs follow the [writable texture contract](../../docs/components/drawable-textures.md).

## Particle compute programs

Use `--particles` for a compute module, or `Stage="compute" Profile="particles"` on an `Electron2DShader` item. The embedded `electron2d/particles.hlslinc` and `electron2d/particles.glslinc` fragments define the fixed state/frame ABI and optional bounded atomic subemission writer. Load with `Shader.CreateFromSPIRV(bytes, Shader.Mode.Particles)` and export its Compute stage to RenderingDevice for explicit execution. See [particle shaders](../../docs/components/particle-shaders.md) for the complete buffer contract and retained-emitter dependencies.

### Numeric graph parameters

Canvas graph imports resolve typed material, global and per-item declarations across both stages. Graph-local references, default words and advisory range/enum/Color metadata enter the ordinary source-type annotation, spirv-val, reflected-interface and atomic-resource path. Normal parameters use existing typed runtime setters; globals use the registry, and per-item scalar/vector fields have sixteen explicit/automatic slots. Numeric matrix parameters use material/global storage. Editing a definition requires reimport. See [the graph contract](../../docs/components/visual-shaders.md#parameters-and-graph-local-references).

Numeric derivative graphs emit fragment fwidth/dFdx/dFdy with their requested default/fine/coarse instruction. Reachable vertex derivatives reject import atomically. The ordinary common SPIR-V capability/stage validator applies, with no runtime source compiler or precision emulation.

## Compiled custom graph providers

Derive VisualShaderNodeCustom in an ordinary C# project and register its concrete
ResourceFileTypes factory in every authoring, import and runtime host. A compiled
import executable references ShaderImport.csproj, registers its factories and
returns the result of Electron2D.Tools.ShaderImportApplication.RunAsync(args).
The tool assembly supplies the same CLI validation, pinned compiler assets and
transactional publication. Concurrent/reentrant shared-entry calls reject.
See tests/VisualShader.CustomImport for an executable host and
[the provider contract](../../docs/components/visual-shaders.md#compiled-custom-nodes)
for callbacks, defaults, options, source scope and portable copy/storage rules.
Game programs reference only Electron2D and their compiled provider definitions.

Set Electron2DShaderImportTool to that host executable when importing graphs from
MSBuild. Electron2DShaderImportTimeoutMS defaults to 300000; positive values set
the per-import process deadline. A failed or timed-out producer preserves the
previous artifact; successful identical bytes preserve its timestamp. The SDK
stages output beside its destination before publication and removes failed staged
files. Native toolchain assets remain host-specific prerequisites.

Run python3 -B tools/shaders/check_host.py for SDK producer failure/deadline checks.
Run python3 -B tools/shaders/check_custom.py with --host, --stock-tool, --consumer
and optional --receipt for registered custom import, argument/concurrency/reentry
checks, source/ABI rejection and SDK build/publish/Clean. These Linux fixtures do
not establish editor UI, physical GPU, foreign-platform or owner acceptance.

Typed visual graph varyings lower through the existing paired canvas interface. Custom float32/int32/uint32 scalar/vector values occupy matching locations 12 through 15; location 11 remains private glyph transport. Graph matrices use four column vectors and Boolean data uses a private flat uint. Module import can validate stages independently; the paired runtime factory checks their producer/consumer agreement, and single-module factories/setters reject custom inputs without an authored vertex stage. See [typed varying transport](../../docs/components/visual-shaders.md#typed-varying-transport) for author/import/runtime commands, capacity and verified profiles.

Canvas matrix producer checks use the public consumer and existing importer:

```bash
python3 -B tools/shaders/check_canvas_matrices.py --tool /path/to/Electron2D.ShaderImport --consumer /path/to/VisualShader.Consumer --output bin/matrix-checks --render
```

The runner preserves logs and hashes, verifies 24 rejected ownership/shape/stage interfaces without replacing a valid module, and optionally reads real matrix component pixels. `--render` requires a configured GPU display session. The consumer covers original model, canvas and screen transforms, CPU/GPU instances, sorted particles, singular canvas, snapping and warmed active/idle allocation intervals.

Device HLSL imports select native 16-bit compilation after preprocessing explicit float16_t/int16_t/uint16_t tokens. Integer-only and Half modules retain the native16 DXC output; ordinary sources keep their established profile. See [native packed shader data](../../docs/components/local-compute.md#native-int16-and-uint16-arithmetic-and-packed-data) for feature gates and the direct narrow specialization frontend limit.
