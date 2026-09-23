# Resources domain

Last updated: 2026-09-23

## Responsibility

The Resources domain defines reusable typed data and portable CPU image buffers used by textures, atlases, importers, and other assets across the runtime targets. It contains the common resource contract, managed `Image`, and the partial shader/material integration described below. Texture resources, ordinary Node/Texture drawing and sampled shader bindings are executable; PNG/JPEG/WebP/BMP/TGA file/buffer decoding and PNG/JPEG saving are executable, while general asset loading remains absent. SDL_image is an approved internal dependency for the codec integration.

Resource base and image sources live under `src/Core/IO/`; shader/material/texture/frame-library/curve resources live under `src/Scene/Resources/`. The public namespace remains `Electron2D`.

## Component inventory

| Component | Types | State |
| --- | --- | --- |
| [Canvas rendering](../components/canvas-rendering.md) | [`SpriteFrames`](../classes/SpriteFrames.md), [`SpriteFrames.LoopMode`](../classes/SpriteFrames.LoopMode.md) | Named animation data, duration/loop policies and resource graph copying consumed by AnimatedSprite |
| [Curves](../components/curves.md) | [`Curve`](../classes/Curve.md), [`Curve.TangentMode`](../classes/Curve.TangentMode.md), [`PathCurve`](../classes/PathCurve.md), [`CurveTexture`](../classes/CurveTexture.md), [`CurveXYZTexture`](../classes/CurveXYZTexture.md), [`CurveTexture.TextureModeEnum`](../classes/CurveTexture.TextureModeEnum.md) | Managed scalar/spatial sampling, tangents, baking, tessellation and nearest queries with typed copying and scene-local ownership |
| [Resource base](../components/resources.md) | [`Resource`](../classes/Resource.md), [`DeepDuplicateMode`](../classes/DeepDuplicateMode.md) | Implemented and verified |
| [Images](../components/images.md) | [`Image`](../classes/Image.md), its seven nested enums, [`ImageMetrics`](../classes/ImageMetrics.md), [`ClockDirection`](../classes/ClockDirection.md) | Managed buffer and processing contract implemented and verified; five native load formats and PNG/JPEG saving; further codec semantics pending; copied pixels feed textures |
| [Shader materials](../components/shader-materials.md) | [`Shader`](../classes/Shader.md), [`Shader.Mode`](../classes/Shader.Mode.md), [`Material`](../classes/Material.md), [`ShaderMaterial`](../classes/ShaderMaterial.md), [`Texture`](../classes/Texture.md), [`ImageTexture`](../classes/ImageTexture.md), [`AtlasTexture`](../classes/AtlasTexture.md) | SPIR-V fragment programs, typed uniform buffers and sampled textures execute on Linux Wayland/Vulkan; ordinary texture drawing and canvas sampling policies are integrated; further mappings remain pending |

## Public surface

The domain exposes resource name/path/scene configuration, built-in classification, synchronous change/setup events, local-scene association, reset and raw-cache hooks, copy and graph-preserving duplication, explicit deep-copy policy, scene ID generation, path takeover, typed property descriptors, deterministic disposal, and typed CPU image storage/processing across uncompressed and raw GPU-compressed formats.

Shader resources add copied binary loading and reflected typed parameter discovery. ShaderMaterial adds borrowed Shader assignment and typed scalar/vector/array values and borrowed Texture bindings with independent resource copies and migration across shader reload. ImageTexture adds copied pixel snapshots, Update, logical size overrides, mip metadata and independent image readback; Texture is its direct abstract parent. AtlasTexture adds borrowed rectangular views with margins, nested drawing, CPU crop/opacity queries, shared source GPU storage and graph-aware resource duplication.

Curve and PathCurve supply independently editable scalar and spatial Bézier resources with lazy caches, typed indexed properties and exact-state duplication. Their managed checks include an executing Entity consumer and PackedScene ownership. Scene Path/PathFollow now consume PathCurve; CurveTexture/CurveXYZTexture generate float snapshots with live subscriptions and the same resource-copy ownership. Texture dimension queries are GetWidth/GetHeight/GetSize; generated textures add writable Width. Native GPU sampling and explicit unsupported-float fallback rejection are verified in the curve component.

## Dependency direction

Resources depends on Core and, narrowly, Scene's `Node` type for `Resource.GetLocalScene()`. Image processing uses Core `Color`, `Vector2I`, and `RectI`. Scene's packed-scene component in turn depends on Resources for typed resource duplication, so ADR 0023 accepts a contained Resources↔Scene type cycle inside the single `Electron2D.dll`. Resource base and managed Image processing remain independent of rendering/importing/editor. Image file/buffer codecs use internal SDL3-CS and FileAccess without exposing native handles.

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
- Raw GPU-compressed storage is not a claim of compressor, decompressor, texture, or renderer support.

## Current limitations

`Image` is the first concrete asset type, but there is no asset loader/saver, complete image-codec family, cache mode, importer, renderer RID, editor resource-ID map, script resource, automatic file-serialization discovery, resource manager, or asset lease type. In-memory packed scenes perform automatic per-instance local duplication, association, setup, and root ownership, but there is no disk format, UID/import integration, or cross-platform asset import/package verification. Exact triggers for codec, compression, texture, and lease work are recorded in ADR 0039.

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
