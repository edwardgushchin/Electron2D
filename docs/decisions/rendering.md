# Electron2D rendering decisions

Last updated: 2026-09-23

This bounded log owns the complete architectural records for rendering. Use [the decision index](index.md) to route other work; read only the affected logs and explicitly linked dependencies.

Decisions in this log: [0028](#adr-0028).

<a id="adr-0028"></a>
## ADR 0028: GPU-first 2D rendering, HLSL/GLSL import and a shared SPIR-V shader path, and SDL_Renderer fallback

Last updated: 2026-09-22

### Status

Accepted.

### Context

Electron2D needs one portable 2D rendering contract for Windows, macOS, Linux (X11/Wayland), Android, iOS, and Web. That contract must support user-authored shaders without making simple 2D output depend on every target successfully creating a programmable graphics pipeline.

SDL exposes two relevant layers. The [SDL GPU API](https://wiki.libsdl.org/SDL3/CategoryGPU) provides cross-platform graphics devices, shaders, pipelines, command buffers, and render passes. The [SDL Render API](https://wiki.libsdl.org/SDL3/CategoryRender) accelerates a smaller set of simple 2D operations but does not expose arbitrary user shader pipelines. Treating both as equivalent would either remove shader support from the engine or make the fallback claim behavior it cannot provide.

[SDL_shadercross](https://github.com/libsdl-org/SDL_shadercross) accepts HLSL source and SPIR-V bytecode and translates them to formats used by SDL GPU backends. SDL3-CS already provides its managed ShaderCross binding. This supplies the common SPIR-V reflection and backend-translation layer. GLSL compilation needs its own import/build frontend; shadercross does not itself compile GLSL source.

SDL3-CS supplies managed bindings to the DisplayServer. The self-contained Linux x64 example packages native SDL and runs a window, input, and scene-frame host through the public Electron2D API. The rendering integration now contains executable GPU/fallback canvas paths and partial shader materials. Other target packages remain unverified; the current integration boundary is detailed below.

### Decision

- The primary Electron2D rendering backend will use the SDL3 GPU API through the vendored SDL3-CS binding when the rendering domain is implemented.
- Electron2D will support engine-provided and user-authored graphics shaders for 2D rendering on the GPU backend. Three-dimensional pipelines and shader functionality are outside the product boundary.
- Directly support exactly two shader source languages: HLSL and GLSL. Compile both to SPIR-V during project import or build. Source compilation is part of the project toolchain, not a required operation when a shipped game loads a material or draws a frame.
- SPIR-V is the common intermediate representation and accepted precompiled input format. Every module, whether produced by the HLSL/GLSL importers or supplied externally, follows the same path: payload/capability validation, parameter and resource reflection, shader-interface validation, backend translation where needed, and GPU-program creation. No input origin bypasses this contract.
- Accept compatible SPIR-V produced by third-party compilers, including tools for Slang and WGSL. This is the extension boundary; Slang and WGSL are not additional directly supported source languages. Their output must meet the same stage, entry-point, SPIR-V version/capability, resource-binding and backend requirements as the built-in import paths.
- Use SDL_shadercross through the SDL3-CS ShaderCross binding for reflection and translation of SPIR-V to the active SDL GPU backend. HLSL import can use its HLSL-to-SPIR-V compiler; GLSL import uses a separately specified build-time compiler. The intended backend representations are SPIR-V for Vulkan, DXIL for Direct3D 12 and MSL for Metal. Translation or compilation of a backend representation does not add another public source language or guarantee support for every instruction on every target.
- The common runtime checks cover payload bounds/structure, the supported capabilities and stages, reflected resource layouts, bindings and shader interfaces; native translation/program-creation failures must propagate. This contract does not require adding SPIRV-Tools through a separate runtime C API. Additional instruction-level validation in import/build tools does not make that validator a runtime dependency. Document the checks actually performed; successful reflection alone is not proof of arbitrary SPIR-V validity.
- A directly supported language includes usable diagnostics, material parameters, textures, consistent resource bindings and backend verification. Successful source compilation alone is insufficient. Diagnostics must identify the source and stage, preserve compiler location information when available, and explain import, interface and backend failures. Reflected parameters and textures must connect to executable typed material APIs, with validation of types, layouts and bindings.
- HLSL and GLSL must share one documented material/resource interface contract. Equivalent shaders must receive the same parameter values and texture resources through the same engine API. Verify compilation errors, invalid bindings, material-parameter updates, texture sampling and rendered output for each language on every backend/platform that claims support. Missing material, texture or backend behavior keeps that language path incomplete.
- Follow [ADR 0012](product.md#adr-0012) for the SDL3-CS/SDL_shadercross dependency path. This decision does not authorize restoring Silk.NET.Shaderc.Native or a separate runtime shaderc P/Invoke layer. Specify and pin the import/build compilers and account for their delivery separately from the native libraries actually required by the runtime's SPIR-V backend path. Compiler selection and native packaging require executable verification during integration.
- SDL_Renderer will be the fallback backend for the portable baseline of simple 2D drawing when the GPU backend is unavailable, cannot be initialized, or a host explicitly selects fallback mode.
- The SDL_Renderer fallback may select an OpenGL or OpenGL ES driver. This does not itself expose a window-associated GL/EGL/GLX context through the public API; the first executable fallback slice must audit those identities individually under [ADR 0042](display.md#adr-0042).
- SDL_Renderer fallback will not pretend to support arbitrary shaders. Shader-dependent resources or operations must be rejected explicitly before drawing when the active backend lacks the required capability; ignoring a shader, silently changing the effect, or reporting false success is prohibited.
- The public Electron2D rendering API will be backend-neutral and typed. It must expose the active backend and relevant capabilities without exposing SDL-owned handles or types. A project or host that requires shaders must be able to reject fallback during startup.
- Shared baseline operations must retain their documented visible semantics on both backends, subject to explicit capability and precision limits. Exact output, performance, advanced blend behavior, and shader support must not be claimed equivalent without backend-specific verification.
- Fallback is a rendering-initialization policy, not a promise of live backend migration. Runtime switching and recovery after graphics-device loss are deferred until their real lifecycle can be implemented and tested.
- Native resources remain internal and are deterministically released through `SafeHandle`-based ownership. Render-frame hot paths must avoid steady-state managed allocations.
- The first complete rendering vertical slice must specify the concrete public types, baseline operation set, threading contract, supported HLSL and GLSL profiles, SPIR-V capabilities, shader interfaces, pinned import/build compiler integrations, cache/import pipeline, material model, backend-selection setting and device-loss policy. The two source languages, import/build compilation phase and common SPIR-V runtime path are already decided above. None is represented now by an empty API.

### Consequences

- The GPU path is the normal rendering path and the only path that guarantees programmable shader behavior.
- The fallback keeps basic 2D games and editor recovery UI possible on systems where the GPU path cannot start, while its reduced capability remains visible to callers.
- Games that use custom shaders must declare or check that requirement instead of assuming fallback visual equivalence.
- Rendering code and vendored SDL3-CS managed bindings belong to `Electron2D.dll`; native SDL packaging remains a separate platform-specific integration boundary.
- Projects can supply HLSL, GLSL or externally compiled compatible SPIR-V without depending on SDL types. Shipped shader artifacts enter one validated SPIR-V path. The same validation and reflection requirements apply to imported and external modules.
- Every implemented rendering feature must be tested against each backend that claims it. Shader compilation and visual correctness additionally require backend- and platform-specific executable or image-based verification.

### Rejected alternatives

- **Use SDL_Renderer as the only or primary backend:** rejected because its simple 2D API cannot provide the required arbitrary shader pipeline.
- **Require the GPU backend with no fallback:** rejected because a reduced but explicit baseline renderer improves portability and startup recovery.
- **Emulate arbitrary shaders on SDL_Renderer:** rejected because it would create incompatible behavior, high maintenance cost, and misleading capability claims.
- **Silently drop shader effects on fallback:** rejected because output would be incorrect while appearing successful.
- **Expose SDL handles in the public API:** rejected because it would couple games and the editor to one backend and prevent controlled backend evolution.
- **Support HLSL only and leave GLSL entirely to external tools:** rejected because HLSL and GLSL are both directly supported project languages.
- **Add more directly supported languages for each available compiler:** rejected because each language entails diagnostics, material and texture integration, binding rules and backend verification. Compatible SPIR-V preserves extensibility without multiplying maintained source frontends.
- **Require source compilation when loading or drawing game materials:** rejected because source compilation belongs to import/build, while the runtime consumes the common shader artifact contract.
- **Treat compiler success as complete language support:** rejected because parameters, textures, resource interfaces, diagnostics and backend behavior must also work.
- **Add renderer interfaces and shader resources without executable behavior:** rejected because declarations alone do not satisfy the rendering contract.

### Current implementation boundary

The initial canvas integration is executable. Engine.Run creates a renderer for the root Window; retained rectangles, lines and texture drawing use visibility, transforms, Z order and modulation. The GPU and compatibility paths have native Linux Wayland pixel checks. This is not complete rendering API coverage or cross-platform acceptance.

HLSL/GLSL import/build compilation and the common SPIR-V path have an executable initial integration through the complete vendored ShaderCross module and Linux native package. Typed scalar/vector/array parameters, sampled Texture/ImageTexture resources and shader reload execute on Linux Wayland/Vulkan. [Shader materials](../components/shader-materials.md) records the exact input profile, compiler checks, ownership and tested behavior. Compressed/integer textures, named material sampler configuration, further uniform mappings and a shader compilation cache remain incomplete. Full language-support criteria remain accepted requirements; shader support may be reported as implemented only for the verified operations, input paths and platforms.

The Linux x64 import tool packages glslang 16.4.0 and SPIRV-Tools v2026.3, built with the matching SPIRV-Headers from the source revisions and SHA-256 archive hashes in [the toolchain lock](../../tools/shaders/toolchain.lock.json). The glslang dependency revisions match its upstream [known-good manifest](https://github.com/KhronosGroup/glslang/blob/16.4.0/known_good.json). These CLI tools and their licenses belong to the separate ShaderImport publish. The runtime consumes SPIR-V through SDL3-CS/Shadercross; it does not invoke source compilers or include the import toolchain. The published importer passes its HLSL, GLSL and external-SPIR-V suite with no compiler lookup through `PATH`. Its `Electron2D.Shaders.targets` integrates those inputs with SDK C# build/publish and Clean. The initial cache/import policy reuses artifact bytes and timestamps: normal builds compile and validate every input, replacing output only when bytes change; publish with `--no-build` reuses the last build artifacts. This supplies the first-slice cache/import contract without skipping source validation. A later compilation-skipping cache must account for compiler versions/options and actual include resolution, including new shadowing files and macro-only changes; it remains a toolchain optimization, not claimed behavior of this integration. Additional build hosts remain unverified.

CanvasItem/Viewport sampling policies now cover texel filtering, ordinary/mirrored repeat, uploaded mipmaps and GPU anisotropy. [Canvas rendering](../components/canvas-rendering.md#texture-sampling) records native backend restrictions; unsupported fallback modes fail explicitly under this ADR. Named material samplers retain their fixed profile.

The Web target has no browser graphics host or verified mapping to these SDL backends. Its first rendering/host slice must establish that mapping and explicit capability behavior before claiming browser output.

### Related decisions

- [0004: 2D API in one Electron2D-owned assembly](product.md#adr-0004)
- [0012: Vendored SDL3-CS and Box2D.NET](product.md#adr-0012)
- [0014: Managed Resource lifetime and realtime allocation](resources.md#adr-0014)
- [0015: Main-loop lifecycle and host boundary](core-object-runtime.md#adr-0015)
- [0016: Process-wide Engine runtime and host-driven scheduling](core-object-runtime.md#adr-0016)
- [0021: Runtime and editor target platforms](product.md#adr-0021)
- [0027: Self-hosted editor and game project boundary](product.md#adr-0027)
