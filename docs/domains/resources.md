# Resources domain

Last updated: 2026-09-23

## Responsibility

Shader import retains logical bool and boolean vectors/arrays in validated SPIR-V metadata. Materials expose bool scalars and int vector masks; raw unsigned fields retain their numeric types. Both source languages and compatible external artifacts share reflection and backend checks. See [the boolean contract](../components/shader-materials.md#boolean-type-information).

The Resources domain defines reusable typed data, portable CPU image buffers and binary masks used by textures, atlases, importers, and other assets across the runtime targets. It contains the common resource contract, managed `Image` and `BitMap`, and the partial shader/material integration described below. Texture resources, ordinary Node/Texture drawing and sampled shader bindings are executable; PNG/JPEG/WebP/BMP/TGA/SVG file/buffer decoding and PNG/JPEG saving are executable, while general asset loading remains absent. SDL_image is an approved internal dependency for the codec integration.

Resource base and image sources live under `src/Core/IO/`; shader/material/texture/frame-library/curve resources live under `src/Scene/Resources/`. The public namespace remains `Electron2D`.

## Component inventory

| Component | Types | State |
| --- | --- | --- |
| [Canvas rendering](../components/canvas-rendering.md) | [`SpriteFrames`](../classes/SpriteFrames.md), [`SpriteFrames.LoopMode`](../classes/SpriteFrames.LoopMode.md), [`AnimatedTexture`](../classes/AnimatedTexture.md) | Named animation data for AnimatedSprite and independent timed texture playback, with graph copying and borrowed sources |
| [Gradients](../components/gradients.md) | Gradient and its mode/space enums, GradientRampTexture, GradientTexture and its fill/repeat enums | Managed interpolation and lazy RGBA8/RGBAF texture generation, typed copies/local ownership and native canvas/material sampling |
| [Curves](../components/curves.md) | [`Curve`](../classes/Curve.md), [`Curve.TangentMode`](../classes/Curve.TangentMode.md), [`PathCurve`](../classes/PathCurve.md), [`CurveTexture`](../classes/CurveTexture.md), [`CurveXYZTexture`](../classes/CurveXYZTexture.md), [`CurveTexture.TextureModeEnum`](../classes/CurveTexture.TextureModeEnum.md) | Managed scalar/spatial sampling, tangents, baking, tessellation and nearest queries with typed copying and scene-local ownership |
| [Resource base](../components/resources.md) | [`Resource`](../classes/Resource.md), [`DeepDuplicateMode`](../classes/DeepDuplicateMode.md) | Implemented and verified |
| [Images](../components/images.md) | [`Image`](../classes/Image.md), [`BitMap`](../classes/BitMap.md), Image's seven nested enums, [`ImageMetrics`](../classes/ImageMetrics.md), [`ClockDirection`](../classes/ClockDirection.md) | Managed image and mask processing implemented and verified; six native load formats and PNG/JPEG saving; further codec semantics pending; copied pixels feed textures |
| [Noise](../components/noise.md) | [`Noise`](../classes/Noise.md) | Executable abstract 1D/2D sampler and managed L8/seamless image generation; concrete FastNoiseLite and noise textures pending |
| [Shader materials](../components/shader-materials.md) | [`Shader`](../classes/Shader.md), [`Shader.Mode`](../classes/Shader.Mode.md), [`Material`](../classes/Material.md), [`ShaderMaterial`](../classes/ShaderMaterial.md), [`CanvasItemMaterial`](../classes/CanvasItemMaterial.md) and its [`BlendModeEnum`](../classes/CanvasItemMaterial.BlendModeEnum.md), [`Texture`](../classes/Texture.md), [`ImageTexture`](../classes/ImageTexture.md), [`AtlasTexture`](../classes/AtlasTexture.md) | SPIR-V fragment programs and typed uniforms execute on Linux Wayland/Vulkan; fixed canvas blending executes on Wayland GPU and compatibility hardware, with software limited to Mix; further mappings remain pending |

## Public surface

The domain exposes resource name/path/scene configuration, built-in classification, synchronous change/setup events, local-scene association, reset and raw-cache hooks, copy and graph-preserving duplication, explicit deep-copy policy, scene ID generation, path takeover, typed property descriptors, deterministic disposal, and typed CPU image storage/processing across uncompressed and raw GPU-compressed formats.

BitMap adds a packed boolean grid with Image alpha import, L8 export, nearest resize, region mutation, circular morphology and marching-squares polygon extraction. Its operations are managed and independent of a physics or rendering backend.

Noise adds an abstract scalar sampling contract and managed grayscale/seamless image generation. Applications can derive a concrete sampler; a built-in FastNoiseLite resource and noise textures remain separate work.

Shader resources add copied binary loading and reflected typed parameter discovery. ShaderMaterial adds borrowed Shader assignment and typed scalar/vector/matrix/array values (including RGB Color and Rect aliases, Vector3 float3 values, Vector3I signed/unsigned triples, unsigned vectors with preserved component bits and Transform float2x2 basis values) and borrowed Texture bindings with independent resource copies and migration across shader reload. ImageTexture adds copied pixel snapshots, Update, logical size overrides, mip metadata and independent image readback; Texture is its direct abstract parent. AtlasTexture adds borrowed rectangular views with margins, nested drawing, CPU crop/opacity queries, shared source GPU storage and graph-aware resource duplication.

Curve and PathCurve supply independently editable scalar and spatial Bézier resources with lazy caches, typed indexed properties and exact-state duplication. Their managed checks include an executing Entity consumer and PackedScene ownership. Scene Path/PathFollow now consume PathCurve; CurveTexture/CurveXYZTexture generate float snapshots with live subscriptions and the same resource-copy ownership. Texture dimension queries are GetWidth/GetHeight/GetSize; generated textures add writable Width. Native GPU sampling and explicit unsupported-float fallback rejection are verified in the curve component.

Gradient adds typed color points and three interpolation modes/spaces. GradientRampTexture and GradientTexture supply inclusive ramp samples and planar fills with lazy source updates. CPU generation and copying need no renderer; native sampling and explicit HDR backend limits are verified in [Gradients](../components/gradients.md).

AnimatedTexture stores up to 256 borrowed texture slots with per-slot duration, scene-aware duplication and source change forwarding. Active sources set the smallest logical dimensions; larger current frames are cropped for image queries and native sampling, including live source resize. The renderer advances it on unscaled monotonic time; CPU configuration and copying need no renderer. AtlasTexture slots and dependency cycles are rejected. Its behavior and native verification are recorded in [the class](../classes/AnimatedTexture.md).

## Dependency direction

Resources depends on Core and, narrowly, Scene's `Node` type for `Resource.GetLocalScene()`. Image and BitMap processing use Core `Color`, `Vector2`, `Vector2I`, and `RectI`. Scene's packed-scene component in turn depends on Resources for typed resource duplication, so ADR 0023 accepts a contained Resources↔Scene type cycle inside the single `Electron2D.dll`. Resource base and managed image/mask processing remain independent of rendering/importing/editor. Image file/buffer codecs use internal SDL3-CS and FileAccess without exposing native handles.

Concrete Shader/ShaderMaterial resources use the internal rendering reflection and uniform-upload path. SDL3-CS and the already packaged SPIRV-Cross native library remain internal; public material APIs expose engine value types and typed descriptors. Resource base and Image retain their independent managed behavior.

## Domain-wide invariants

- Electron2D-owned code stays in `Electron2D.dll`.
- Resource state is typed; there is no dynamic property bag or untyped reflection-based copier.
- Registered paths are ordinal, process-wide, weakly held, and single-owner.
- Derived resources explicitly define construction and stored-state copying.
- Successful duplication preserves graph topology and clears external identity.
- Scene-local duplicates preserve graph topology, receive their owning scene root before setup, run setup once, and are disposed by that root.
- Failed duplication disposes its complete partial result.
- Base state is concurrently safe; derived state must define any stronger contract.
- Managed object memory is reclaimed by the runtime; deterministic disposal controls logical and native-resource lifetime, not managed memory reclamation.
- Public resources do not expose manual reference counting. A future asset manager may count internal disposable leases solely to retain shared native-backed payloads.
- Serialized resource state and ownership semantics must remain portable across all five runtime targets; platform-native payloads require explicit internal backends.
- Image buffers own copied bytes, use canonical little-endian multi-byte fields, publish complete states atomically, and never expose mutable backing storage.
- BitMap keeps packed bits private, snapshots before morphology, and publishes changes outside its state lock.
- Raw GPU-compressed storage is not a claim of compressor, decompressor, texture, or renderer support.

## Current limitations

`Image` and `BitMap` are concrete managed assets; `Noise` is an executable abstract resource without a built-in generator yet. There is no asset loader/saver, complete image-codec family, cache mode, importer, renderer RID, editor resource-ID map, script resource, automatic file-serialization discovery, resource manager, or asset lease type. BitMap has no physics collision consumer yet. In-memory packed scenes perform automatic per-instance local duplication, association, setup, and root ownership, but there is no disk format, UID/import integration, or cross-platform asset import/package verification. Exact triggers for codec, compression, texture, and lease work are recorded in ADR 0039.

## Decisions

- [ADR 0001: Typed C# without Variant](../decisions/product.md#adr-0001)
- [ADR 0002: C# events for signals](../decisions/product.md#adr-0002)
- [ADR 0003: ElectronObject lifetime](../decisions/core-object-runtime.md#adr-0003)
- [ADR 0004: 2D API in one Electron2D-owned assembly](../decisions/product.md#adr-0004)
- [ADR 0013: Managed typed Resource contract](../decisions/resources.md#adr-0013)
- [ADR 0014: Managed Resource lifetime and realtime allocation](../decisions/resources.md#adr-0014)
- [ADR 0017: Source-tree module layout](../decisions/product.md#adr-0017)
- [ADR 0021: Runtime and editor target platforms](../decisions/product.md#adr-0021)
- [ADR 0023: Typed in-memory packed scenes](../decisions/scene.md#adr-0023)
- [ADR 0039: Managed image buffers and codec boundaries](../decisions/resources.md#adr-0039)

## Verification

Resource, Image, and packed-scene checks live in `tests/Electron2D.Tests/Program.cs`. They exercise base duplication, image formats and processing, and per-instance local graph association/setup/ownership without requiring a codec, asset loader/saver, editor, or renderer. RenderingTextureTests separately verifies texture resources, native sampling, and SDL_image package loading/PNG decoding; that backend probe does not establish a public codec API.

[CurveTests](../../tests/Electron2D.Tests/CurveTests.cs) verifies CPU curve samples, geometric edge cases, event timing, cache/copy isolation, scene ownership, failure recovery, concurrent state access and allocation-free warm queries without native dependencies.

[NoiseTests](../../tests/Electron2D.Tests/NoiseTests.cs) verifies managed 1D/2D dispatch, L8 image conversion, seamless overlap and boundary errors through a concrete test sampler. It does not verify a built-in generator or native texture sampling.

Shader reflection separates the reserved float32 TIME input from material values and stored descriptors. Reload and duplication preserve user parameters while the renderer fills current time per draw. The shared HLSL/GLSL/SPIR-V validation and native checks are recorded in [shader render time](../components/shader-materials.md#render-time).
