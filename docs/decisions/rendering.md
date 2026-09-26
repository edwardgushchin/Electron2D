# Electron2D rendering decisions

Last updated: 2026-09-24

This bounded log owns the complete architectural records for rendering. Use [the decision index](index.md) to route other work; read only the affected logs and explicitly linked dependencies.

Decisions in this log: [0028](#adr-0028), [0046](#adr-0046).

<a id="adr-0028"></a>
## ADR 0028: GPU-first 2D rendering, shared SPIR-V shaders, browser WebGPU, and SDL_Renderer fallback

Last updated: 2026-09-25

### Status

Accepted.

### Context

Electron2D needs one portable 2D rendering contract for Windows, macOS, Linux (X11/Wayland), Android, iOS, Android TV, tvOS, and Web. That contract must support user-authored shaders without making simple 2D output depend on every target successfully creating a programmable graphics pipeline.

SDL exposes two relevant layers. The [SDL GPU API](https://wiki.libsdl.org/SDL3/CategoryGPU) provides cross-platform graphics devices, shaders, pipelines, command buffers, and render passes. The [SDL Render API](https://wiki.libsdl.org/SDL3/CategoryRender) accelerates a smaller set of simple 2D operations but does not expose arbitrary user shader pipelines. Treating both as equivalent would either remove shader support from the engine or make the fallback claim behavior it cannot provide.

[SDL_shadercross](https://github.com/libsdl-org/SDL_shadercross) accepts HLSL source and SPIR-V bytecode and translates them to formats used by SDL GPU backends. SDL3-CS already provides its managed ShaderCross binding. This supplies the common SPIR-V reflection and backend-translation layer. GLSL compilation needs its own import/build frontend; shadercross does not itself compile GLSL source.

SDL3-CS supplies managed bindings to the DisplayServer. The self-contained Linux x64 example packages native SDL and runs a window, input, and scene-frame host through the public Electron2D API. The rendering integration now contains executable GPU/fallback canvas paths and partial shader materials. Other target packages remain unverified; the current integration boundary is detailed below.

The pinned SDL GPU API has no browser backend. A standalone Chrome probe rendered a WGSL shader through WebGPU, but that probe did not use Electron2D's canvas or materials. Browser shader support therefore needs an engine-owned backend with the same public material contract.

### Decision

- The primary native Electron2D rendering backend uses the SDL3 GPU API through the vendored SDL3-CS binding.
- On Web, use a separate engine-owned WebGPU canvas backend through browser interop. It submits the same retained canvas batches and applies the same typed ShaderMaterial parameters and textures. Keep browser WebGPU and JavaScript objects behind the existing backend-neutral public API. A persistent, nonblocking browser host owns WebGPU initialization, frame submission and disposal; it must not rely on SDL_GPU becoming available in the browser.
- Electron2D will support engine-provided and user-authored graphics shaders for 2D rendering on the GPU backend. Three-dimensional pipelines and shader functionality are outside the product boundary.
- Directly support exactly two shader source languages: HLSL and GLSL. Compile both to SPIR-V during project import or build. Source compilation is part of the project toolchain, not a required operation when a shipped game loads a material or draws a frame.
- SPIR-V is the common intermediate representation and accepted precompiled input format. Every module, whether produced by the HLSL/GLSL importers or supplied externally, follows the same path: payload/capability validation, parameter and resource reflection, shader-interface validation, backend translation where needed, and GPU-program creation. No input origin bypasses this contract.
- The WebGPU backend translates validated SPIR-V to WGSL before browser shader-module and pipeline creation, preserving the reflected entry point, interface, bindings, material values and texture semantics. This applies to built-in shaders and to modules loaded or replaced at runtime through the public Shader API; an import-only conversion cannot be the sole path. Pin and package a Web-compatible translator when implementing this backend, and propagate translation, WGSL compilation, pipeline and device errors. WGSL remains an internal backend representation, not a third directly supported source language.
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
- Native resources remain internal and are deterministically released through `SafeHandle`-based ownership. Engine-owned render-frame hot paths have zero managed and engine-owned native allocations after preparation under ADR 0014; backend-internal allocations require separate measurement.
- The first complete rendering vertical slice must specify the concrete public types, baseline operation set, threading contract, supported HLSL and GLSL profiles, SPIR-V capabilities, shader interfaces, pinned import/build compiler integrations, cache/import pipeline, material model, backend-selection setting and device-loss policy. The two source languages, import/build compilation phase and common SPIR-V runtime path are already decided above. None is represented now by an empty API.

### Consequences

- The GPU path is the normal rendering path and the only path that guarantees programmable shader behavior.
- The fallback keeps basic 2D games and editor recovery UI possible on systems where the GPU path cannot start, while its reduced capability remains visible to callers.
- Games that use custom shaders must declare or check that requirement instead of assuming fallback visual equivalence.
- Rendering code and vendored SDL3-CS managed bindings belong to `Electron2D.dll`; native SDL packaging remains a separate platform-specific integration boundary.
- Projects can supply HLSL, GLSL or externally compiled compatible SPIR-V without depending on SDL types. Shipped shader artifacts enter one validated SPIR-V path. The same validation and reflection requirements apply to imported and external modules.
- Browser WebGPU consumes that same contract through an internal SPIR-V-to-WGSL translation path; separate browser shader source or public WebGPU handles are not required.
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
- **Wait for SDL_GPU to gain a Web backend:** rejected as the browser implementation plan because the pinned SDL build exposes no such driver while WebGPU is available in the tested browser.

### Current implementation boundary

The initial canvas integration is executable. Engine.Run creates a renderer for the root Window; retained rectangles, lines and texture drawing use visibility, transforms, Z order and modulation. The GPU and compatibility paths have native Linux Wayland pixel checks. This is not complete rendering API coverage or cross-platform acceptance.

HLSL/GLSL import/build compilation and the common SPIR-V path have an executable initial integration through the complete vendored ShaderCross module and Linux native package. Typed scalar/vector/array parameters (including bool and boolean component masks), sampled Texture/ImageTexture resources and shader reload execute on Linux Wayland/Vulkan. [Shader materials](../components/shader-materials.md) records the exact input profile, compiler checks, ownership and tested behavior. Compressed/integer textures, named material sampler configuration, further uniform mappings and a shader compilation cache remain incomplete. Full language-support criteria remain accepted requirements; shader support may be reported as implemented only for the verified operations, input paths and platforms.

The Linux x64 import tool packages glslang 16.4.0 and SPIRV-Tools v2026.3, built with the matching SPIRV-Headers from the source revisions and SHA-256 archive hashes in [the toolchain lock](../../tools/shaders/toolchain.lock.json). The glslang dependency revisions match its upstream [known-good manifest](https://github.com/KhronosGroup/glslang/blob/16.4.0/known_good.json). These CLI tools and their licenses belong to the separate ShaderImport publish. The runtime consumes SPIR-V through SDL3-CS/Shadercross; it does not invoke source compilers or include the import toolchain. The published importer passes its HLSL, GLSL and external-SPIR-V suite with no compiler lookup through `PATH`. Its `Electron2D.Shaders.targets` integrates those inputs with SDK C# build/publish and Clean. The initial cache/import policy reuses artifact bytes and timestamps: normal builds compile and validate every input, replacing output only when bytes change; publish with `--no-build` reuses the last build artifacts. This supplies the first-slice cache/import contract without skipping source validation. A later compilation-skipping cache must account for compiler versions/options and actual include resolution, including new shadowing files and macro-only changes; it remains a toolchain optimization, not claimed behavior of this integration. Logical boolean types are recovered from the source compilers during import and stored as validated standard OpString records in the common SPIR-V artifact. External producers use the same metadata schema and physical-layout checks; runtime does not infer booleans from unsigned storage. Scalar bool maps to bool, boolean vectors to int component masks, and arrays use one such value per logical element under ADR 0005. [The component contract](../components/shader-materials.md#boolean-type-information) records the schema and verified limits. Additional build hosts remain unverified.

CanvasItem/Viewport sampling policies now cover texel filtering, ordinary/mirrored repeat, uploaded mipmaps and GPU anisotropy. [Canvas rendering](../components/canvas-rendering.md#texture-sampling) records native backend restrictions; unsupported fallback modes fail explicitly under this ADR. Named material samplers currently implement linear/base-level/clamp defaults independently of canvas policies; their configuration remains pending.

The tested Android arm64 phone now runs the GPU canvas and HLSL/SPIR-V material when four optional Vulkan features are disabled; the pipeline enables depth clipping because depth clamping is then unavailable. The tested Android TV exposes OpenGL ES 2 but no Vulkan hardware feature, so SDL_GPU remains unavailable and only the shaderless SDL_Renderer fallback works. The isolated Chrome probe has WebGL2 and WebGPU devices and a direct SDL_Renderer frame, but SDL 3.4.16/Emscripten exposes no SDL_GPU driver. [The platform matrix](../platform-verification.md) records the exact checks and limits. Standalone probes confirmed a GLES2 fragment shader and red readback on that TV and a WGSL/WebGPU shader frame in Chrome. The selected browser WebGPU backend and a shader-capable TV path remain unimplemented; both require shader translation, canvas/material integration, lifecycle and pixel verification before claiming the common shader contract there.

### Related decisions

- [0004: 2D API in one Electron2D-owned assembly](product.md#adr-0004)
- [0012: Vendored SDL3-CS and Box2D.NET](product.md#adr-0012)
- [0014: Managed Resource lifetime and realtime allocation](resources.md#adr-0014)
- [0015: Main-loop lifecycle and host boundary](core-object-runtime.md#adr-0015)
- [0016: Process-wide Engine runtime and host-driven scheduling](core-object-runtime.md#adr-0016)
- [0021: Runtime and editor target platforms](product.md#adr-0021)
- [0027: Self-hosted editor and game project boundary](product.md#adr-0027)

<a id="adr-0046"></a>
## ADR 0046: Render text with SDL_ttf 3 and HarfBuzz

Last updated: 2026-09-23

### Status

Accepted. Text rendering and the font domain are not yet implemented.

### Context

The existing canvas has GPU and SDL_Renderer paths. The future public font, text and GUI APIs must preserve the applicable reference behavior without exposing a second renderer or native backend types. Font rasterization alone does not supply every text-layout feature.

### Decision

- Use SDL_ttf 3 with HarfBuzz enabled for font loading, text shaping and glyph rasterization. Integrate its output into the existing Electron2D canvas paths; SDL_ttf does not define a separate public rendering API.
- Keep text layout, fallback, measurement, selection, direction and drawing behavior behind Electron2D-owned, typed APIs. Audit the applicable `TextServer`, font and GUI contracts in the first text slice. Mixed-direction paragraphs, line breaking and other capabilities not supplied by the selected library require engine-owned integration; do not mark them implemented on the basis of SDL_ttf availability.
- Vendor the complete SDL3-CS TTF managed binding module into `Electron2D.dll` when the executable text slice begins, following [ADR 0012](product.md#adr-0012). Pin and package native SDL_ttf with FreeType and HarfBuzz, whether linked or bundled, for each target. Verify HarfBuzz is enabled in the shipped native build; a build without it does not satisfy this decision.
- Verify text output on both claimed canvas backends and on each claimed runtime platform under [ADR 0021](product.md#adr-0021), including browser packaging. Until those checks pass, keep unimplemented API and platform behavior blocked or explicitly unsupported in coverage.

### Consequences

- SDL_ttf supplies the native font and shaping foundation while the public contract and canvas integration remain owned by Electron2D.
- This decision selects a backend, not a completed `TextServer`, font resource or GUI implementation. Native packages and managed bindings are added only with an executable text slice.

### Rejected alternatives

- Introduce SkiaSharp or another complete 2D renderer for text: rejected because Electron2D already owns a canvas renderer and would have to package and reconcile a second graphics stack.
- Use glyph rasterization without HarfBuzz shaping: rejected because it cannot satisfy the multilingual text contract.

<a id="adr-0078"></a>
## ADR 0078: Retained screen visibility and processing enablers

Last updated: 2026-09-26

- Status: Accepted
- Scope: VisibleOnScreenNotifier/VisibleOnScreenEnabler runtime regions in the current retained canvases
- Depends on: [0028](#adr-0028), [0008](scene.md#adr-0008), [0014](resources.md#adr-0014), [0072](physics.md#adr-0072), [0027](product.md#adr-0027)

### Decision

- Map VisibleOnScreenNotifier2D and its enabler subclass to VisibleOnScreenNotifier : Entity and VisibleOnScreenEnabler : VisibleOnScreenNotifier. Defaults are Rect(-10,-10,20,20), on-screen false, EnableNodePath ".." and enabled mode Inherit. Namespace ScreenEnableMode supplies Inherit=0, Always=1, WhenPaused=2 because C# cannot give a nested enum and property the same name.
- Evaluate visibility during actual retained frame construction with the same effective transforms, layer membership, viewport masks, interpolation, clipping and repeated copies as submitted geometry. Use conservative transformed bounds including the local origin and any inherited retained draw geometry, inclusive border/line/point intersection and the inherited modulation alpha threshold 0.007; SelfModulate does not gate detection. This follows pinned render culling and does not test occlusion by other items. Negative finite rectangle extents normalize; reject nonfinite rectangles/endpoints and transformed overflow. State remains false until the first submitted frame; skipped/hidden rendering retains the last sample.
- Commit all current notifier states after backend submission before callbacks, then deliver ScreenEntered/ScreenExited on the owner thread before FramePostDraw. Reuse transition storage and generation stamps; in-callback remove/reentry invalidates stale queued delivery. Continue later notifiers after user failures, report aggregate errors and never replay committed transitions. Tree entry/exit reset state silently through actual membership hooks, not manually delivered notifications.
- Enabler weakly caches its relative path target on entry or a changed nonempty path. Entry disables it before render. A screen entry writes the selected enabled ProcessMode, exit writes Disabled; apply this after the event even when a handler fails. Every enabled-mode assignment updates the current target; equal paths do not rebind. Empty/changed paths and departure drop the cache without restoring old modes. An invalid attached path commits the path, clears the cache and raises a typed error rather than only logging the source diagnostic. Disposed targets are ignored; ordinary target owner/physics/capture guards remain authoritative.
- Node.ProcessMode captures disabled-state transitions in cached depth-specific lists instead of a LINQ dictionary. Preserve preorder, pre-mutation physics validation, callback failure continuation and nested callback edits. Warm each used subtree capacity/reentrancy depth outside the measured interval. Node lifecycle transitions may still perform explicit physics resource attachment/removal; the zero-allocation screen check targets neutral processing nodes, while separate native assertions verify RigidBody remove/reentry behavior.
- ShowRect is editor-only magenta gizmo drawing in the pinned source. Keep that member Blocked until the first self-hosted editor canvas gizmo drawing slice and add/test its flag and fill there. Do not ship an inert bool. Public independent offscreen viewport rendering remains subject to its existing lifecycle gate; this slice integrates the current root-window renderer and CanvasLayer groups on both backends.

### Verification

ScreenVisibilityTests covers defaults, enums, target cache identity/path changes/errors, every process policy, weak disposal, silent departure/reentry, event failure with policy application, ownership, packing and 64 warmed nested ProcessMode transitions with zero managed bytes. ScreenVisibilityRenderingTests covers twelve native region/mask/layer/default-canvas/alpha/border/degenerate stages, black readback confirming no runtime gizmo, actual RigidBody participation, committed peer state, stale event suppression, failure continuation/native release and 64 warmed active enabler transitions with zero managed bytes per compatibility/GPU Linux Wayland path. Native allocator counts, other platforms, broad-scene cost and owner visual acceptance are unverified; no vendor source changes.

Primary pinned sources: [node behavior](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/scene/2d/visible_on_screen_notifier_2d.cpp), [render culling](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/servers/rendering/renderer_canvas_cull.cpp).

<a id="adr-0079"></a>
## ADR 0079: Retained nine-patch controls

Last updated: 2026-09-26

- Status: Accepted
- Scope: NinePatchRect control, borrowed texture regions and all axis modes on both current canvas backends
- Depends on: [0028](#adr-0028), [0008](scene.md#adr-0008), [0013](resources.md#adr-0013), [0014](resources.md#adr-0014)

### Decision

- Implement NinePatchRect : Control with Texture=null, RegionRect=zero, DrawCenter=true, four signed integer margins=zero and horizontal/vertical Stretch=0. Reuse nested AxisStretchMode Stretch=0, Tile=1, TileFit=2 and Side Left/Top/Right/Bottom. Constructor and typed inherited descriptor default MouseFilter to Ignore=2. Intrinsic minimum is the floating-point sum of opposing margins, including signed values; Control combines it with custom minimum and zero.
- Record one typed nine-patch canvas command, then expand independent axis pieces using the pinned pixel mapping. Corners retain native source size; center axes stretch once, tile at source-center pixel period, or round the repeat count with floor(destination/source+0.5) and scale whole tiles to fit. Both-center pieces are omitted only when DrawCenter=false. A collapsed source center in Stretch uses constant UV. Oversized borders keep begin-side priority; signed margin policy remains stored rather than editor-hint clamping.
- Use the existing texture/material/transform/sampling/blend/clip pipeline on both GPU and compatibility. Live dimensions and atlas geometry resolve at every submission, so a missed earlier Changed subscriber cannot strand old geometry. Source regions with negative extent use the accepted texture-region flip convention. All-zero size selects the whole base texture. Atlas region/margin clipping maps destination and source before splitting; nested views resolve through each layer to common root storage. Reuse the same atlas region resolver for ordinary DrawRectRegion.
- CPU tessellation is proportional to repeat count; reject more than 1,048,574 axis tiles or 1,048,576 piece pairs before submission. This is the current finite geometry budget, not a blanket performance claim. A common nine-patch fragment path on a shader-capable fallback is the upgrade when dense panels make this ceiling or cost relevant. Successful warmed frames within prepared capacity allocate no managed bytes; first capacity growth and errors are outside the measured interval.
- Compensate interpolation rounding at normalized source starts with a 0.00001-texel inward offset; native per-pixel oracle checks exact fractional TileFit seams on both backends. This numerical adjustment does not replace the source axis mapping. Texture filter/repeat still use inherited current canvas policy; native allocator counts remain unmeasured.
- Borrow textures without disposing them. Assignment unsubscribes old events, subscribes the new binding, commits redraw/minimum invalidation and emits TextureChanged; equal references are silent. Content changes redraw without replacement signal. Disposed borrowed resources clear the binding. Notification errors leave committed state and are aggregated; disposal releases subscriptions. Store all configuration in typed scene descriptors and preserve exact class reconstruction.

### Verification and dependency triggers

NinePatchTests checks defaults, signed margins/minimum sums, defined enum/side and finite-region guards, borrowed replacement/update/disposal/errors, packing and an independent pixel-axis oracle for all nine mode combinations. Native NinePatchRenderingTests checks every pixel of the nine combinations, DrawCenter=false, signed-region flips, atlas margins/clipping and constant center, plus 64 warmup/64 active resized frames with zero managed bytes on Linux Wayland GPU and compatibility. Native readback images were inspected. Other platforms, native allocation, dense-panel cost and owner visual acceptance are unverified; vendor source is unchanged.

RenderingServer.CanvasItemAddNinePatch remains Blocked until public retained canvas/item RID ownership and attachment exist; this control uses the real current internal command consumer. TextureProgressBar.NinePatchStretch now depends on its complete Range/progress/radial-fill control slice, with this geometry reusable then. Theme/StyleBox and first editor skin authoring remain separate complete slices.

Primary pinned sources: [control contract](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/scene/gui/nine_patch_rect.cpp), [axis shader oracle](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/servers/rendering/renderer_rd/shaders/canvas.glsl), [atlas mapping](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/scene/resources/atlas_texture.cpp).

<a id="adr-0080"></a>
## ADR 0080: Shared ranges and texture progress controls

Last updated: 2026-09-27

- Status: Accepted
- Scope: Range value policy and TextureProgressBar retained fill rendering
- Depends on: [0028](#adr-0028), [0079](#adr-0079), [0001](product.md#adr-0001), [0008](scene.md#adr-0008), [0013](resources.md#adr-0013), [0014](resources.md#adr-0014)

### Decision

- Implement abstract Range : Control with double MinValue=0, MaxValue=100, Value=0, Step=0.01, Page=0 and false allow/exponential/local Rounded policies. Double follows the pinned range storage. Concrete TextureProgressBar uses Step=1 and MouseFilter.Pass. Bounds/step/page must be finite under the typed host contract; Value retains NaN and clamps infinities according to allow flags. Rendering rejects nonfinite ratio explicitly.
- Snap positive steps relative to minimum unless its magnitude is more than step*1e14; then round locally to integer with midpoint away from zero and clamp upper to MaxValue-Page before lower to MinValue. Nonpositive step disables snapping. Use allocation-free decimal arithmetic for representable user decimal steps and binary fallback outside its documented window, projecting the pinned decimal-intent snapping without per-update strings/R128 dependency. Min raises max as needed; min/max clamp page and resnap value before Changed. Page clamps to [0,max-min] and resnaps; Step emits Changed without resnapping. Allow/Rounded/ExpEdit edits do not resnap or emit Changed.
- Ratio maps clamped Value linearly or with base-two logarithms for ExpEdit with nonnegative min; min zero uses log-min zero. Approximately equal bounds return one. Ratio assignment performs its source-specific step rounding before normal value calculation and final Page cap. Preserve repeated-NaN signal suppression and SetValueNoSignal's protected hook/redraw without ValueChanged.
- Share accepts a typed Range target, moves only that target to the caller's shared state and emits target Changed then value hook/ValueChanged even when detached. This follows pinned implementation; other peers of the old target group remain there. Rounded stays local to the writer. Unshare copies configuration without notifications. Ordinary shared events/hooks target attached owners in host insertion order. Cache weak owners and depth-specific dispatch snapshots; generation changes suppress stale deliveries after reentrant group moves/disposal. Validate all peers before mutation, commit values before callbacks, continue required hooks/signals/later owners after errors and aggregate them. Packed scenes store configuration, not sharing links; dependency descriptors precede restored Value.
- TextureProgressBar borrows under/progress/over textures and independent tints, drawing them in that order. Source texture dimensions govern ordinary fills; nine-patch uses Control.Size and signed margin minimum sums. Implement six linear/centered fills and three radial policies. Progress offset, radial original-texture center offset (clamped normalized center), initial degree wrap and fill-degree clamp execute. Full/zero radial cases are explicit; partial sectors reuse existing textured polygon construction and source rectangle clipping. Radial nine-patch mode stretches polygon size without nine-patch border UV remapping.
- Reuse ADR 0079 geometry for progress-dependent nine-patch source/destination borders, with tint recorded on the same command. Preserve source texture identity offset behavior and bounded warm reusable canvas storage. Singular bilateral drift uses a finite fallback instead of sending undefined/nonfinite geometry to a backend; this host adaptation is explicit. Source enum becomes namespace TextureProgressFillMode because C# cannot reuse a nested FillMode name as the property.
- Control.SizeFlags and Container/Box layout now consume inherited vertical flags under ADR 0081. Range defaults to ShrinkBegin (0), TextureProgressBar to Fill (1), with matching typed descriptors and real fit/expand behavior. Their class/default rows are Implemented in this bounded scope; native semantic accessibility follows the existing separate service gate.

### Verification

RangeProgressTests checks source defaults, decimal min-offset snap, signed/local rounding, Page/allow/ratio timing, share target identity/unshare, NaN suppression, owner/errors, failure continuation/reentrant unshare, borrowed slots/disposal and dependency-ordered packing. Sixty-four warmed shared value updates allocate zero managed bytes. TextureProgressRenderingTests checks every pixel of nine default fill masks at half/empty/full, linear centered/reverse nine-patch masks, readback images and 64 warmed active radial frames without managed allocation on Linux Wayland GPU/compatibility. Native allocator counts, broad GUI performance, other platforms and owner acceptance are unverified; no vendor source changes.

Primary pinned sources: [range policy](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/scene/gui/range.cpp), [texture progress geometry](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/scene/gui/texture_progress_bar.cpp).

<a id="adr-0081"></a>
## ADR 0081: Typed control size flags and deferred box and grid containers

Last updated: 2026-09-27

- Status: Accepted
- Scope: Control.SizeFlags, Container and Box/HBox/VBox/Grid runtime layout
- Depends on: [0028](#adr-0028), [0080](#adr-0080), [0008](scene.md#adr-0008), [0021](product.md#adr-0021), [0045](product.md#adr-0045)

### Decision

- Implement the real inheritance chain Container : Control, BoxContainer : Container, HBoxContainer/VBoxContainer : BoxContainer and GridContainer : Container. SizeFlags is a nested flagged enum: ShrinkBegin=0, Fill=1, Expand=2, ExpandFill=3, ShrinkCenter=4, ShrinkEnd=8. Both Control axes default Fill, ratio one; Range vertical defaults ShrinkBegin, texture progress Fill. Keep unknown bits and finite signed/zero ratios; equal flag/ratio writes are silent and actual changes emit SizeFlagsChanged. Reject nonfinite ratio inputs as a typed finite-geometry adaptation.
- Container defaults MouseFilter.Pass and PropagateMaximumSize=true. Listen to direct child flags/minimum/maximum/visibility/order and own entry/resize/maximum/direction/visible changes. Coalesce attached QueueSort through a cached deferred action. Requests made during a running pass schedule one follow-up after it completes, preserving reentrant layout changes for the next captured batch. Deliver pre-sort notification 50, PreSortChildren, sort notification 51 and SortChildren in order; attempt required phases after failures and report aggregate errors. Detached requests do nothing. Entry caches a membership-generation action; stale deferred actions from prior memberships cannot consume the current sort. Exit/disposal clears membership/subscriptions/cache. A direct Control child changing TopLevel clears its allocation cache and requests parent minimum refresh/sort through its canvas-entry notification. Allowed-size hooks remain executable advisory inspector queries, not an editor claim.
- Fit live direct children to finite nonnegative allocations. Fill and non-fill are independent of primary Expand; non-fill uses bound minimum with End priority over Center, floored centering and horizontal RTL. Reset anchors and commit one rectangle reflow before resetting rotation/scale. Current desired-size virtual default is zero and all shipped controls have no larger desired size; a future desired-size resource/control consumer must add the internal calculation/listener dependency with executable evidence.
- Box minimum uses local-visible non-top-level direct controls; runtime arrangement uses visible-in-tree controls. Ceil minima, truncate maxima, use pixel-floored available size, iterative weighted min/max refit and accumulated fractional error; reverse horizontal child order under RTL and align residual space. Preserve signed separation and actual Control spacers (Pass, primary ExpandFill, ratio one). Fixed HBox/VBox reject all Vertical assignments and omit that descriptor. A local typed Separation property projects the existing theme constant default four; global Theme resources and inheritance remain separately blocked. Reentrant immediate arrangement reuses cleared snapshots and repeats; nonsettling callbacks fail after 64 passes rather than recursing indefinitely. Checked pixel arithmetic rejects overflow.
- GridContainer fills direct eligible controls in row-major order with Columns >= 1 (default one). Local-visible children determine minimum size; visible-in-tree children participate in arrangement. Truncate bound minima and maxima to integer pixels, aggregate the largest minimum and effective maximum per occupied column/row, and refit selected expanding columns/rows uniformly on each axis without stretch ratios. Treat empty declared columns as expanding by count, without storage proportional to Columns; only occupied columns contribute separation. Assign integer remainder to the first expanding occupied columns/rows and reverse horizontal placement under RTL. Local signed HSeparation/VSeparation default four and project existing theme constants; global Theme resource lookup/inheritance remains separate. Reuse sparse layout buffers and the existing Container final fit/notification path. Cache each Control minimum/maximum deferred callback after first use so post-sort minimum refresh does not create per-pass delegates.
- Correct the pinned grid capped-row offset defect: after maximum refit, advance by the preceding row's final allocated height plus separation, rather than its minimum. For two expanding rows in 100 pixels with zero separation, with first minimum 10/maximum 20 and second minimum 10, the second begins at 20 with height 80, not at 10 overlapping the first row. Preserve the remaining min/max aggregation and ordering contract; this is a bounded geometry correction under the pre-release correctness rule.
- Maximum propagation supplies remaining primary bounds through internal per-child caches without a public cache API. Grid propagation supplies the combined bounds for constraint collection, then independently subtracts each cell's column/row offset for final fitting. Control.UpdateMaximumSize clears direct non-top-level child allocation caches before recursively refreshing bounds; Container requests sorting on its own MaximumSizeChanged even if its rectangle stays unchanged. Raising a maximum therefore releases an obsolete smaller allocation without manual resize/sort. Indexed child traversal and cached deferred callbacks keep prepared updates free of per-call enumerator/delegate allocation. Store flags/ratio, generic orientation, alignment, box separation and grid columns/separations with exact typed factories and correct inherited defaults. Preserve ordinary node ownership and resource lifetime contracts.
- Reuse two SceneTree action queues under the existing enqueue/finalization lock: swap enqueue/captured roles, drain captured callbacks outside the lock and recycle capacity. Enqueues made during capture wait for a later flush; cross-thread acceptance remains atomic with lifetime. This removes the observed 832 managed bytes per active layout frame from fresh ConcurrentQueue batches. Growth and user callbacks may allocate. Queued deletion ownership remains its existing separate path.
- Container.AccessibilityRegion remains Blocked until a native semantic service can publish/update/remove a region through viewport/control semantic identity. The class stays Partial for that exact member; do not expose an inert property. Other container types retain their own coverage triggers. Global Theme resources/lookup need their complete resource and inheritance slice; Slider/HSlider/VSlider need three StyleBox skins, texture state plus input/repeat integration; text needs the approved SDL_ttf/HarfBuzz font/shaping and canvas slice.

### Verification and limits

BoxContainerTests checks weights, min/max redistribution, alignment/RTL, shrink/reset anchors/transforms, visible child minimums, fixed orientation, unknown bits/ratio guards, actual spacer naming, event coalescing/failure continuation/membership, packing, reentrant arrangement, captured-next-batch/cross-thread acceptance and zero managed bytes for 64 warmed resize/deferred cycles and captured batch cycles. BoxContainerRenderingTests checks real retained panel geometry, horizontal RTL, hidden-child redistribution and cross-axis shrink plus 64 warmed vertical resize/immediate-layout/render frames on Linux Wayland GPU/compatibility. Native allocator counts, large-GUI performance, other platforms and owner visual acceptance are unverified; no vendor source changes.

[GridContainerTests](../../tests/Electron2D.Tests/GridContainerTests.cs) verifies defaults, integer minima, eligibility, row-major/RTL placement, independent expansion, min/max bounds, sparse columns, typed packing, guards, callback continuation/reentrancy, independent two-axis maximum propagation, cache clearing after removal, increasing maximum from 20 to 80, and overflow rejection before child fitting. It measures zero managed bytes for 64 warmed layout cycles and 64 alternating maximum-update/refit cycles with four deferred frames per cycle. [GridContainerRenderingTests](../../tests/Electron2D.Tests/GridContainerRenderingTests.cs) verifies seven geometry/pixel phases covering remainder placement, RTL, visibility, reordering, resize, column changes and capped rows on Linux Wayland GPU and compatibility. Each backend also passes 64 warmed resize/layout/render frames with zero managed bytes measured from ProcessFrameStarted through FramePostDraw. Native allocator counts, large-GUI performance, other platforms and owner acceptance remain unverified.

Primary pinned sources: [container](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/scene/gui/container.cpp), [box layout](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/scene/gui/box_container.cpp), [grid layout](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/scene/gui/grid_container.cpp).
