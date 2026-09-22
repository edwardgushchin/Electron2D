# Electron2D rendering decisions

Last updated: 2026-09-22

This bounded log owns the complete architectural records for rendering. Use [the decision index](index.md) to route other work; read only the affected logs and explicitly linked dependencies.

Decisions in this log: [0028](#adr-0028).

<a id="adr-0028"></a>
## ADR 0028: GPU-first 2D rendering, shaders, and SDL_Renderer fallback

Last updated: 2026-09-22

### Status

Accepted.

### Context

Electron2D needs one portable 2D rendering contract for Windows, macOS, Linux (X11/Wayland), Android, iOS, and Web. That contract must support user-authored shaders without making simple 2D output depend on every target successfully creating a programmable graphics pipeline.

SDL exposes two relevant layers. The [SDL GPU API](https://wiki.libsdl.org/SDL3/CategoryGPU) provides cross-platform graphics devices, shaders, pipelines, command buffers, and render passes. The [SDL Render API](https://wiki.libsdl.org/SDL3/CategoryRender) accelerates a smaller set of simple 2D operations but does not expose arbitrary user shader pipelines. Treating both as equivalent would either remove shader support from the engine or make the fallback claim behavior it cannot provide.

SDL3-CS supplies managed bindings to the DisplayServer. The self-contained Linux x64 example packages native SDL and runs a window, input, and scene-frame host through the public Electron2D API. Other target packages remain unverified, and the rendering domain is not implemented yet. This decision fixes the rendering boundary without introducing placeholder renderer or shader types.

### Decision

- The primary Electron2D rendering backend will use the SDL3 GPU API through the vendored SDL3-CS binding when the rendering domain is implemented.
- Electron2D will support engine-provided and user-authored graphics shaders for 2D rendering on the GPU backend. Three-dimensional pipelines and shader functionality are outside the product boundary.
- SDL_Renderer will be the fallback backend for the portable baseline of simple 2D drawing when the GPU backend is unavailable, cannot be initialized, or a host explicitly selects fallback mode.
- The SDL_Renderer fallback may select an OpenGL or OpenGL ES driver. This does not itself expose a window-associated GL/EGL/GLX context through the public API; the first executable fallback slice must audit those identities individually under [ADR 0042](display.md#adr-0042).
- SDL_Renderer fallback will not pretend to support arbitrary shaders. Shader-dependent resources or operations must be rejected explicitly before drawing when the active backend lacks the required capability; ignoring a shader, silently changing the effect, or reporting false success is prohibited.
- The public Electron2D rendering API will be backend-neutral and typed. It must expose the active backend and relevant capabilities without exposing SDL-owned handles or types. A project or host that requires shaders must be able to reject fallback during startup.
- Shared baseline operations must retain their documented visible semantics on both backends, subject to explicit capability and precision limits. Exact output, performance, advanced blend behavior, and shader support must not be claimed equivalent without backend-specific verification.
- Fallback is a rendering-initialization policy, not a promise of live backend migration. Runtime switching and recovery after graphics-device loss are deferred until their real lifecycle can be implemented and tested.
- Native resources remain internal and are deterministically released through `SafeHandle`-based ownership. Render-frame hot paths must avoid steady-state managed allocations.
- The concrete public types, baseline operation set, threading contract, shader source language, accepted binary formats, offline compilation and cross-compilation tooling, cache/import pipeline, material model, backend-selection setting, and device-loss policy will be specified by the first complete rendering vertical slice. None is represented now by an empty API.

### Consequences

- The GPU path is the normal rendering path and the only path that guarantees programmable shader behavior.
- The fallback keeps basic 2D games and editor recovery UI possible on systems where the GPU path cannot start, while its reduced capability remains visible to callers.
- Games that use custom shaders must declare or check that requirement instead of assuming fallback visual equivalence.
- Rendering code and vendored SDL3-CS managed bindings belong to `Electron2D.dll`; native SDL packaging remains a separate platform-specific integration boundary.
- Every implemented rendering feature must be tested against each backend that claims it. Shader compilation and visual correctness additionally require backend- and platform-specific executable or image-based verification.

### Rejected alternatives

- **Use SDL_Renderer as the only or primary backend:** rejected because its simple 2D API cannot provide the required arbitrary shader pipeline.
- **Require the GPU backend with no fallback:** rejected because a reduced but explicit baseline renderer improves portability and startup recovery.
- **Emulate arbitrary shaders on SDL_Renderer:** rejected because it would create incompatible behavior, high maintenance cost, and misleading capability claims.
- **Silently drop shader effects on fallback:** rejected because output would be incorrect while appearing successful.
- **Expose SDL handles in the public API:** rejected because it would couple games and the editor to one backend and prevent controlled backend evolution.
- **Add renderer interfaces and shader resources immediately:** rejected because no executable rendering behavior exists yet.

### Current implementation boundary

This ADR selects a future renderer architecture only. [ADR 0040](display.md#adr-0040) now provides a native SDL window and input pump; the repository still contains no rendering domain, backend, graphics device, texture, material, shader, pipeline, canvas, draw command, or render test. Existing visibility, Z-order, color, geometry, timing, and node state do not produce pixels. No cross-platform graphics behavior or shader support is claimed as implemented or verified.

The Web target has no browser graphics host or verified mapping to these SDL backends. Its first rendering/host slice must establish that mapping and explicit capability behavior before claiming browser output.

### Related decisions

- [0004: 2D API in one Electron2D-owned assembly](product.md#adr-0004)
- [0012: Vendored SDL3-CS and Box2D.NET](product.md#adr-0012)
- [0014: Managed Resource lifetime and realtime allocation](resources.md#adr-0014)
- [0015: Main-loop lifecycle and host boundary](core-object-runtime.md#adr-0015)
- [0016: Process-wide Engine runtime and host-driven scheduling](core-object-runtime.md#adr-0016)
- [0021: Runtime and editor target platforms](product.md#adr-0021)
- [0027: Self-hosted editor and game project boundary](product.md#adr-0027)
