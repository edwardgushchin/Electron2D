# ADR 0028: GPU-first 2D rendering, shaders, and SDL_Renderer fallback

Last updated: 2026-09-21

## Status

Accepted.

## Context

Electron2D needs one portable 2D rendering contract for Linux, Windows, macOS, Android, and iOS. That contract must support user-authored shaders without making simple 2D output depend on every target successfully creating a programmable graphics pipeline.

SDL exposes two relevant layers. The [SDL GPU API](https://wiki.libsdl.org/SDL3/CategoryGPU) provides cross-platform graphics devices, shaders, pipelines, command buffers, and render passes. The [SDL Render API](https://wiki.libsdl.org/SDL3/CategoryRender) accelerates a smaller set of simple 2D operations but does not expose arbitrary user shader pipelines. Treating both as equivalent would either remove shader support from the engine or make the fallback claim behavior it cannot provide.

SDL3-CS, native SDL packaging, an application host, and the Electron2D rendering domain are not implemented yet. This decision fixes the future boundary without introducing placeholder renderer or shader types.

## Decision

- The primary Electron2D rendering backend will use the SDL3 GPU API through the future SDL3-CS integration.
- Electron2D will support engine-provided and user-authored graphics shaders for 2D rendering on the GPU backend. Three-dimensional pipelines and shader functionality are outside the product boundary.
- SDL_Renderer will be the fallback backend for the portable baseline of simple 2D drawing when the GPU backend is unavailable, cannot be initialized, or a host explicitly selects fallback mode.
- SDL_Renderer fallback will not pretend to support arbitrary shaders. Shader-dependent resources or operations must be rejected explicitly before drawing when the active backend lacks the required capability; ignoring a shader, silently changing the effect, or reporting false success is prohibited.
- The public Electron2D rendering API will be backend-neutral and typed. It must expose the active backend and relevant capabilities without exposing SDL-owned handles or types. A project or host that requires shaders must be able to reject fallback during startup.
- Shared baseline operations must retain their documented visible semantics on both backends, subject to explicit capability and precision limits. Exact output, performance, advanced blend behavior, and shader support must not be claimed equivalent without backend-specific verification.
- Fallback is a rendering-initialization policy, not a promise of live backend migration. Runtime switching and recovery after graphics-device loss are deferred until their real lifecycle can be implemented and tested.
- Native resources remain internal and are deterministically released through `SafeHandle`-based ownership. Render-frame hot paths must avoid steady-state managed allocations.
- The concrete public types, baseline operation set, threading contract, shader source language, accepted binary formats, offline compilation and cross-compilation tooling, cache/import pipeline, material model, backend-selection setting, and device-loss policy will be specified by the first complete rendering vertical slice. None is represented now by an empty API.

## Consequences

- The GPU path is the normal rendering path and the only path that guarantees programmable shader behavior.
- The fallback keeps basic 2D games and editor recovery UI possible on systems where the GPU path cannot start, while its reduced capability remains visible to callers.
- Games that use custom shaders must declare or check that requirement instead of assuming fallback visual equivalence.
- Rendering code and resources belong to `Electron2D.dll`; SDL3-CS and native SDL remain deployment dependencies whose exact packaging is unresolved.
- Every implemented rendering feature must be tested against each backend that claims it. Shader compilation and visual correctness additionally require backend- and platform-specific executable or image-based verification.

## Rejected alternatives

- **Use SDL_Renderer as the only or primary backend:** rejected because its simple 2D API cannot provide the required arbitrary shader pipeline.
- **Require the GPU backend with no fallback:** rejected because a reduced but explicit baseline renderer improves portability and startup recovery.
- **Emulate arbitrary shaders on SDL_Renderer:** rejected because it would create incompatible behavior, high maintenance cost, and misleading capability claims.
- **Silently drop shader effects on fallback:** rejected because output would be incorrect while appearing successful.
- **Expose SDL handles in the public API:** rejected because it would couple games and the editor to one backend and prevent controlled backend evolution.
- **Add renderer interfaces and shader resources immediately:** rejected because no SDL integration or executable rendering behavior exists yet.

## Current implementation boundary

This ADR selects a future architecture only. The repository currently contains no rendering domain, backend, graphics device, window host, texture, material, shader, pipeline, canvas, draw command, or render test. Existing visibility, Z-order, color, geometry, timing, and node state do not produce pixels. No cross-platform graphics behavior or shader support is claimed as implemented or verified.

## Related decisions

- [0004: 2D API in one Electron2D-owned assembly](0004-2d-api-single-assembly.md)
- [0012: External runtime dependencies and Box2D.NET](0012-external-runtime-dependencies.md)
- [0014: Managed Resource lifetime and realtime allocation](0014-managed-resource-lifetime.md)
- [0015: Main-loop lifecycle and host boundary](0015-main-loop-contract.md)
- [0016: Process-wide Engine runtime and host-driven scheduling](0016-engine-runtime.md)
- [0021: Cross-platform runtime target matrix](0021-cross-platform-runtime-targets.md)
- [0027: Self-hosted editor and game project boundary](0027-self-hosted-editor-and-games.md)
