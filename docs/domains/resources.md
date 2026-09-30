# Resources domain

Last updated: 2026-10-01

## Responsibility

Shader import retains logical bool and boolean vectors/arrays in validated SPIR-V metadata. Materials expose bool scalars and int vector masks; raw unsigned fields retain their numeric types. Both source languages and compatible external artifacts share reflection and backend checks. See [the boolean contract](../components/shader-materials.md#boolean-type-information).

The Resources domain defines reusable typed data, portable CPU image buffers and binary masks used by textures, atlases, importers, and other assets across the runtime targets. It contains the common resource contract, managed `Image` and `BitMap`, and the partial shader/material integration described below. Texture resources, ordinary Node/Texture drawing and sampled shader bindings are executable; PNG/JPEG/WebP/BMP/TGA/SVG file/buffer decoding, PNG/JPEG saving and synchronous ImageTexture/FontFile loading are executable. General asset loading remains incomplete. SDL_image is an approved internal dependency for the codec integration.

Resource base and image sources live under `src/Core/IO/`; shader/material/texture/frame-library/curve/font resources live under `src/Scene/Resources/`. The public namespace remains `Electron2D`.

## Component inventory

| Component | Types | State |
| --- | --- | --- |
| [Canvas rendering](../components/canvas-rendering.md) | [`SpriteFrames`](../classes/SpriteFrames.md), [`SpriteFrames.LoopMode`](../classes/SpriteFrames.LoopMode.md), [`AnimatedTexture`](../classes/AnimatedTexture.md), [StyleBox](../classes/StyleBox.md), [StyleBoxTexture](../classes/StyleBoxTexture.md), [StyleBoxLine](../classes/StyleBoxLine.md), [StyleBoxEmpty](../classes/StyleBoxEmpty.md), [StyleBoxFlat](../classes/StyleBoxFlat.md) | Retained decoration and named animation data for AnimatedSprite and independent timed texture playback, with graph copying and borrowed sources |
| [Gradients](../components/gradients.md) | Gradient and its mode/space enums, GradientRampTexture, GradientTexture and its fill/repeat enums | Managed interpolation and lazy RGBA8/RGBAF texture generation, typed copies/local ownership and native canvas/material sampling |
| [Curves](../components/curves.md) | [`Curve`](../classes/Curve.md), [`Curve.TangentMode`](../classes/Curve.TangentMode.md), [`Curve2D`](../classes/Curve2D.md), [`CurveTexture`](../classes/CurveTexture.md), [`CurveXYZTexture`](../classes/CurveXYZTexture.md), [`CurveTexture.TextureModeEnum`](../classes/CurveTexture.TextureModeEnum.md) | Managed scalar/spatial sampling, tangents, baking, tessellation and nearest queries with typed copying and scene-local ownership |
| [Typed themes](../components/themes.md) | [Theme](../classes/Theme.md), [Theme.DataType](../classes/Theme.DataType.md) | Six typed data categories including borrowed Font resources, exact copies and scene lookup integration |
| [Resource base](../components/resources.md) | [`Resource`](../classes/Resource.md), [`DeepDuplicateMode`](../classes/DeepDuplicateMode.md) | Implemented and verified |
| [Text](../components/text.md) | [Font](../classes/Font.md), [FontFile](../classes/FontFile.md), [LabelSettings](../classes/LabelSettings.md) | Validated encoded bytes, fractional metrics/shaping, borrowed fallbacks, cached glyph textures and shared label effects |
| [Resource loading](../components/resource-loading.md) | [`ResourceLoader`](../classes/ResourceLoader.md), [`ResourceLoader.CacheMode`](../classes/ResourceLoader.CacheMode.md) | Synchronous image-texture and dynamic font files use the weak path cache; general loader remains Partial |
| [Images](../components/images.md) | [`Image`](../classes/Image.md), [`BitMap`](../classes/BitMap.md), Image's seven nested enums, [`ImageMetrics`](../classes/ImageMetrics.md), [`ClockDirection`](../classes/ClockDirection.md) | Managed image and mask processing implemented and verified; six native load formats and PNG/JPEG saving; further codec semantics pending; copied pixels feed textures |
| [Noise](../components/noise.md) | [`Noise`](../classes/Noise.md), [`FastNoiseLite`](../classes/FastNoiseLite.md), [`NoiseTexture`](../classes/NoiseTexture.md) | Abstract and built-in 1D/2D samplers, plus generated texture with gradient, normal-map and mipmap processing |
| [Shader materials](../components/shader-materials.md) | [`Shader`](../classes/Shader.md), [`Shader.Mode`](../classes/Shader.Mode.md), [`Material`](../classes/Material.md), [`ShaderMaterial`](../classes/ShaderMaterial.md), [`CanvasItemMaterial`](../classes/CanvasItemMaterial.md) and its [`BlendMode`](../classes/BlendMode.md), [`Texture`](../classes/Texture.md), [`ImageTexture`](../classes/ImageTexture.md), [`AtlasTexture`](../classes/AtlasTexture.md) | SPIR-V fragment programs and typed uniforms execute on Linux Wayland/Vulkan; fixed canvas blending executes on Wayland GPU and compatibility hardware, with software limited to Mix; further mappings remain pending |

## Public surface

The domain exposes resource name/path/scene configuration, built-in classification, synchronous change/setup events, local-scene association, reset and raw-cache hooks, copy and graph-preserving duplication, explicit deep-copy policy, scene ID generation, path takeover, typed property descriptors, deterministic disposal, and typed CPU image storage/processing across uncompressed and raw GPU-compressed formats.

ResourceLoader uses the existing process-wide weak path cache to load ImageTexture and dynamic FontFile resources. Typed generic load, existence and extension discovery cover both integrated format families. Reuse retains a live cached wrapper, Ignore returns an unregistered independent wrapper, and Replace decodes before updating the same cached texture or font; deep modes coincide with their ordinary modes for leaf image/font files. Callers own loaded resources. Live Wayland compatibility/GPU Sprite pixels pass before and after a file replacement.

BitMap adds a packed boolean grid with Image alpha import, L8 export, nearest resize, region mutation, circular morphology and marching-squares polygon extraction. Its operations are managed and independent of a physics or rendering backend.

Noise adds an abstract scalar sampling contract and managed grayscale/seamless image generation. NoiseTexture borrows a concrete sampler and optional Gradient, rebakes lazily on settings or source changes, and exposes its image through Texture. Applications can derive a sampler or use the built-in FastNoiseLite resource.

Shader resources add copied binary loading and reflected typed parameter discovery. ShaderMaterial adds borrowed Shader assignment and typed scalar/vector/matrix/array values (including RGB Color and Rect2 aliases, Vector3 float3 values, Vector3i signed/unsigned triples, unsigned vectors with preserved component bits and Transform float2x2 basis values) and borrowed Texture bindings with independent resource copies and migration across shader reload. ImageTexture adds copied pixel snapshots, Update, logical size overrides, mip metadata and independent image readback; Texture is its direct abstract parent. AtlasTexture adds borrowed rectangular views with margins, nested drawing, CPU crop/opacity queries, shared source GPU storage and graph-aware resource duplication.

Curve and Curve2D supply independently editable scalar and spatial Bézier resources with lazy caches, typed indexed properties and exact-state duplication. Their managed checks include an executing Entity consumer and PackedScene ownership. Scene Path/PathFollow now consume Curve2D; CurveTexture/CurveXYZTexture generate float snapshots with live subscriptions and the same resource-copy ownership. Texture dimension queries are GetWidth/GetHeight/GetSize; generated textures add writable Width. Native GPU sampling and explicit unsupported-float fallback rejection are verified in the curve component.

Gradient adds typed color points and three interpolation modes/spaces. GradientRampTexture and GradientTexture supply inclusive ramp samples and planar fills with lazy source updates. CPU generation and copying need no renderer; native sampling and explicit HDR backend limits are verified in [Gradients](../components/gradients.md).

AnimatedTexture stores up to 256 borrowed texture slots with per-slot duration, scene-aware duplication and source change forwarding. Active sources set the smallest logical dimensions; larger current frames are cropped for image queries and native sampling, including live source resize. The renderer advances it on unscaled monotonic time; CPU configuration and copying need no renderer. AtlasTexture slots and dependency cycles are rejected. Its behavior and native verification are recorded in [the class](../classes/AnimatedTexture.md).

## Dependency direction

Resources depends on Core and, narrowly, Scene's `Node` type for `Resource.GetLocalScene()`. Image and BitMap processing use Core `Color`, `Vector2`, `Vector2i`, and `Rect2i`. Scene's packed-scene component in turn depends on Resources for typed resource duplication, so ADR 0023 accepts a contained Resources↔Scene type cycle inside the single `Electron2D.dll`. Resource base and managed image/mask processing remain independent of rendering/importing/editor. Image file/buffer codecs use internal SDL3-CS and FileAccess without exposing native handles.

Concrete Shader/ShaderMaterial resources use the internal rendering reflection and uniform-upload path. SDL3-CS and the already packaged SPIRV-Cross native library remain internal; public material APIs expose engine value types and typed descriptors. ResourceLoader consumes `Image.LoadFromFile`, `ImageTexture.CreateFromImage`/`SetImage`, transactional `FontFile` decoding, `FileAccess` path policy and Resource's weak path registry. Resource base and Image retain their independent managed behavior.

## Domain-wide invariants

- Electron2D-owned code stays in `Electron2D.dll`.
- Resource state is typed; there is no dynamic property bag or untyped reflection-based copier.
- Registered paths are ordinal, process-wide, weakly held, and single-owner.
- Synchronous loader cache decisions serialize; decoding failures preserve existing cached image pixels or font bytes and resource identity. Loaded resources remain caller-owned.
- Derived resources explicitly define construction and stored-state copying.
- Successful duplication preserves graph topology and clears external identity.
- Scene-local duplicates preserve graph topology, receive their owning scene root before setup, run setup once, and are disposed by that root.
- Failed duplication disposes its complete partial result.
- Base state is concurrently safe; derived state must define any stronger contract.
- Managed object memory is reclaimed by the runtime; deterministic disposal controls logical and native-resource lifetime, not managed memory reclamation.
- Public resources do not expose manual reference counting. Font layout internally leases immutable source snapshots so native retirement waits for active reads; this does not add a public asset-manager or lease API.
- Serialized resource state and ownership semantics must remain portable across all five runtime targets; platform-native payloads require explicit internal backends.
- Image buffers own copied bytes, use canonical little-endian multi-byte fields, publish complete states atomically, and never expose mutable backing storage.
- BitMap keeps packed bits private, snapshots before morphology, and publishes changes outside its state lock.
- Raw GPU-compressed storage is not a claim of compressor, decompressor, texture, or renderer support.

## Current limitations

`Image`, `BitMap`, and `FastNoiseLite` are concrete managed assets; `Noise` is their abstract sampling contract. NoiseTexture has managed and Linux Wayland native pixel checks but no other-platform verification. The synchronous loader covers ImageTexture and dynamic FontFile sources with a weak cache: there is no general loader/saver, complete image-codec family, public format-loader plugin, UID/dependency graph, threaded loading, importer, renderer RID, editor resource-ID map, script resource, automatic file-serialization discovery, resource manager, or asset lease type. BitMap has no physics collision consumer yet. In-memory packed scenes perform automatic per-instance local duplication, association, setup, and root ownership, but there is no disk format or cross-platform asset import/package verification. Exact triggers for codec, compression, texture, and lease work are recorded in ADR 0039 and ResourceLoader coverage.

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

Resource, Image, and packed-scene checks live in `tests/Electron2D.Tests/Program.cs`. They exercise base duplication, managed image processing and per-instance local graph association/setup/ownership. Native `ImageCodecTests` covers the six public decoders; `ResourceLoaderTests` covers the first synchronous file loader and weak cache. Focused `SpriteRenderingTests` uses the loader for a PNG texture and verifies live file replacement pixels on Linux Wayland compatibility/GPU and dummy compatibility. Other targets remain unverified.

[CurveTests](../../tests/Electron2D.Tests/CurveTests.cs) verifies CPU curve samples, geometric edge cases, event timing, cache/copy isolation, scene ownership, failure recovery, concurrent state access and allocation-free warm queries without native dependencies.

[NoiseTests](../../tests/Electron2D.Tests/NoiseTests.cs) verifies managed 1D/2D dispatch, L8 image conversion, seamless overlap and boundary errors through a concrete test sampler. It does not verify a built-in generator or native texture sampling.

[FastNoiseLiteTests](../../tests/Electron2D.Tests/FastNoiseLiteTests.cs) checks pinned algorithm vectors, copying and generated-texture invalidation. [NoiseTextureTests](../../tests/Electron2D.Tests/NoiseTextureTests.cs) verifies source invalidation, luminance mapping, normal conversion, mipmaps, seamless selection, graph copies, failures and cleanup through a concrete test sampler. [NoiseTextureRenderingTests](../../tests/Electron2D.Tests/NoiseTextureRenderingTests.cs) verifies retained Sprite pixels after source changes on Linux Wayland GPU and compatibility backends.

Shader reflection separates the reserved float32 TIME input from material values and stored descriptors. Reload and duplication preserve user parameters while the renderer fills current time per draw. The shared HLSL/GLSL/SPIR-V validation and native checks are recorded in [shader render time](../components/shader-materials.md#render-time).

## Style resources and canvas decoration

[StyleBox](../classes/StyleBox.md) supplies typed content margins, minimum size, mask and draw-bound queries plus protected custom drawing hooks. [StyleBoxTexture](../classes/StyleBoxTexture.md) borrows a texture and records nine-patch decoration with atlas resolution before expansion; [StyleBoxLine](../classes/StyleBoxLine.md) records a signed, integer-aligned rectangle; [StyleBoxEmpty](../classes/StyleBoxEmpty.md) supplies margins without decoration. CanvasItem.DrawStyleBox executes these resources during the target's normal recording scope. The current-item query spans all three recording stages and restores context after failures. Stored state uses the existing exact resource graph duplication; consumers request redraw/layout when their styles change, and texture Changed is not forwarded by the style.

[Managed tests](../../tests/Electron2D.Tests/StyleBoxTests.cs) verify margins/defaults, hook dispatch, Changed timing, integer strip geometry, recording context and failure cleanup, exact copies, scene-local resource policy, lifetime and lock boundaries. Sixty-four warmed line mutation/recording/replay cycles allocate zero managed bytes. [Native tests](../../tests/Electron2D.Tests/StyleBoxRenderingTests.cs) verify all nine axis combinations, fractional borders/expansion, center suppression, source regions, modulation, atlas ordering, line and empty drawing on Linux Wayland GPU/compatibility; each backend also passes 64 warmed style mutation/recording/render frames with zero managed bytes measured from ProcessFrameStarted through FramePostDraw. Native allocator counts, broad GUI performance, other platforms and owner acceptance remain unverified. [StyleBoxFlat](../classes/StyleBoxFlat.md) adds retained rounded fill, borders, blended edges, shadows, skew and anti-aliasing through untextured triangles; [Corner](../classes/Corner.md) supplies its stable radius index. It shares the style margin/copy/draw lifecycle. [StyleBoxFlatTests](../../tests/Electron2D.Tests/StyleBoxFlatTests.cs) verifies defaults, Corner identities, equal-write event ordering, clamps/guards, content/draw bounds, hooks, exact resource copies, scene-local behavior, callback failures and concurrency. The [independent C++ fixture](../../tests/Electron2D.Tests/Fixtures/StyleBoxFlatGeometry.json) covers 15 sharp/rounded/unequal/oversized/blended/hollow/AA/shadow/skew/signed/degenerate profiles; triangle ordering, vertex positions, colors and UVs match within 0.00005, and draw rectangles match exactly. Sixty-four warmed mutation/geometry-recording/replay cycles allocate zero managed bytes. [StyleBoxFlatRenderingTests](../../tests/Electron2D.Tests/StyleBoxFlatRenderingTests.cs) verifies three visible mutation states covering rounded corners, borders, center suppression, border blend, offset shadow, skew, AA and expansion on Linux Wayland GPU/compatibility. Each backend also passes 64 warmed mutation/recording/render frames with zero managed bytes measured from ProcessFrameStarted through FramePostDraw. Native allocator counts, broad GUI performance, nonunit viewport recording scale, other platforms and owner acceptance remain unverified. The current viewport path has identity content stretch and no canvas recording oversampling override, so feather width equals AntiAliasingSize in local units (recording factor one). Font draw oversampling and FontFile.Oversampling control glyph rasterization; they do not change the viewport recording scale or StyleBoxFlat feathers. A future viewport recording-scale integration must divide the feather by the active factor and invalidate retained recordings when it changes. Panel/PanelContainer consume actual Theme/default-skin lookup and invalidation under [ADR 0083](../decisions/rendering.md#adr-0083); Font and Label execute through the FreeType/HarfBuzz and private ICU [text component](../components/text.md). See [ADR 0082](../decisions/rendering.md#adr-0082).

[Theme](../classes/Theme.md) supplies six typed categories—Color, Constant, Font, FontSize, Icon and StyleBox—through [theme lookup](../components/themes.md). Control/Window inherit themes and own local overrides; Panel/PanelContainer, box/grid and Label consume the values in drawing and layout. Default and named Font slots retain borrowed identities, alias subscriptions and graph-copy semantics. ThemeDB shares an embedded Open Sans SemiBold font between its initial built-in default and fallback without loading a native face before text use. Defaults for remaining GUI families and project Theme file loading retain their exact separate triggers under [ADR 0083](../decisions/rendering.md#adr-0083).

## Font resources and text effects

[Font](../classes/Font.md) owns ordered borrowed fallbacks and reusable line/paragraph caches. [FontFile](../classes/FontFile.md) owns copied encoded data, native metadata, hinting/subpixel/rounding policy, OpenType feature overrides and glyph rasters. Public data and file replacement validate a complete new font before committing, preserve the old resource on decode failure, and invalidate dependent layouts on success. The loader supports dynamic font files through the existing Ignore/Reuse/Replace cache policy. Exact resource duplication preserves fallback aliases and scene-local ownership.

FreeType 2.13.3 and HarfBuzz remain internal; shaping retains 26.6 precision, contextual features and scalar clusters. Private ICU 78.3 supplies dictionary word/line boundaries alongside the managed Unicode 17 layout helpers. Serialized native operations run on the font worker; active-reader leases protect native source retirement. Immutable glyph-image snapshots already recorded by a canvas remain usable after font replacement or disposal. The snapshots hold CPU pixels, and retirement ends their forced backend residency without keeping the owning font alive.

[LabelSettings](../classes/LabelSettings.md) borrows a Font and stores base font/outline/shadow values plus ordered stacked effects, with typed indexed descriptors and ordinary resource graph copying. [Label](../classes/Label.md) is the scene consumer for these effects, inherited font themes and text layout. [FontFileTests](../../tests/Electron2D.Tests/FontFileTests.cs), [FontResourceLoaderTests](../../tests/Electron2D.Tests/FontResourceLoaderTests.cs), [FontLifetimeTests](../../tests/Electron2D.Tests/FontLifetimeTests.cs), [LabelSettingsTests](../../tests/Electron2D.Tests/LabelSettingsTests.cs) and [ThemeFontTests](../../tests/Electron2D.Tests/ThemeFontTests.cs) cover the implemented resource contracts. Font discovery, variable/palette editing, bitmap/cache authoring, MSDF, public TextServer RID operations and Theme file import remain separate capabilities; see [Text](../components/text.md) and [ADR 0046](../decisions/rendering.md#adr-0046) for precise verification and platform limits.

The [texture identity slice](../components/canvas-rendering.md#texture-resource-identities) links borrowed resource RID lifetime to actual server-owned texture creation/update/replacement/free and retained CanvasItem drawing. Owned identities expire with the active renderer; borrowed resources survive. It executes on both native baseline backends with explicit format/platform limits.
