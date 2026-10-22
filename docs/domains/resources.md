# Resources domain

Last updated: 2026-10-22

[Live-scene authoring saves](../components/resource-files.md#live-scene-authoring-save) now share pre/post notification, RESET-aware packing, atomic persistence and cleanup through ResourceSaver.SaveScene. The programmatic operation retains explicit owner/failure/fresh-process evidence and separate visual-editor/tooling gates.

[Per-referrer resource IDs](../components/resources.md#per-referrer-path-ids)
now provide exact shared path-pair authoring metadata without editor UI. Public
format extensions persist stable opaque labels and reconstruct physical aliases
in a fresh process; embedded scene IDs and UIDs remain separate.

[Image-array resources](../components/texture-arrays.md) execute copied homogeneous layers, typed samplers/defaults, shape-aware reload, source archives, actual GPU layer/mip upload and owned/proxy RIDs. Ordinary texture drawing retains its separate resource branch. Current native evidence is Linux Wayland GPU; compatibility rejects shader use. Copied byte, normalized/float, block and unsigned-integer images execute; foreign/native-allocation acceptance remains separate.

[Imported image textures](../components/compressed-textures.md) add ordinary `.ctex` loading, independent logical/physical dimensions, exact source mips and retained Texture consumers. Atomic replacement, typed cache/worker loading and embedded resource/scene archives run through the shared resource pipeline.

[File-backed arrays](../components/compressed-arrays.md) load raw/PNG/WebP and Basis LDR/HDR layers with complete mips, atomic source replacement, typed caching/worker loading and embedded archives. They reuse the existing GPU array consumer and borrowed RenderingDevice lifetime; Compatibility keeps its explicit custom-shader gate.

[Portable compressed textures](../components/portable-textures.md) add seven runtime image-encoding modes, complete mip storage, ordinary texture sampling, independent duplication and typed resource/scene archives with explicit buffer-retention policy. The same approved native encoder supplies the remaining ETC/EAC/ASTC Image.Compress capabilities.

## Tile grid and terrain integration

[Tile resources and layers](../components/tiles.md) now connect square, isometric, half-offset and hexagonal atlas authoring and typed storage to actual merged static/kinematic bodies in CPU/GPU worlds, with tile-owner queries/events, runtime data callbacks and real retained-canvas output. All layouts, animated atlas storage/drawing and navigation now execute; terrain definitions, modes, weighted connect/path painting and fresh file graphs now execute; reusable pattern capture/storage/placement now execute; explicit typed proxy mapping now executes through placed cell consumers; typed custom data now controls executable runtime callbacks and persists actual values independently of configured schemas; occlusion retains its prerequisite under [ADR 0101](../decisions/tiles.md#adr-0101).

Weak ResourcePath lookup preserves a temporarily closed resource entry and path reservation until disposal commits; a veto restores ordinary lookup of the same identity.

## Executable mesh skin integration

ArrayMesh persists copied typed geometry/name/skin arrays and a separate Material graph through exact built-in file registration. Fresh scenes restore aliases and actual four/eight skin consumers; renderer identities and palette attachments are transient.

## Physical skeletal integration

PhysicalBone and SkeletonModificationPhysicalBones have exact node/resource factories and copied bounded consumer paths. Fresh scenes restore independent modification graphs and run actual physics-driven poses; pending named commands and live bindings are transient.

## Jiggle controller integration

Jiggle owns copied bounded configuration and transient numeric history; StackHolder borrows a force-copied child graph with weak scene leases. Nested aliases retain independent scene copies, clear transient binding on final release and reject active disposal before terminal changes. Existing typed resource/factory/copy machinery supplies fresh-process persistence.

## Executable IK integration

Concrete TwoBoneIK/CCDIK/FABRIK resources retain copied bounded joint configuration and weak scene binding under the existing stack ownership policy. Their exact factories/archives support fresh-process files and forced independent scene copies; prepared scratch is transient and scene-node references remain borrowed.

## Skeletal graph resources

[Modification stacks](../components/skeletal-animation.md) own copied slot storage and borrow modifications. Scene binding is exclusive and transient. PackedScene force-copies the complete stack graph per rig through existing alias-preserving sessions; typed built-in schemas persist concrete LookAt settings and copied Polygon bone records. Arbitrary custom extensions require explicit registration/copy contracts.

## Responsibility

Ordinary [texture placeholders](../components/texture-placeholders.md) execute explicit pixel stripping through `Texture.CreatePlaceholder`, typed dimension archives and scene-local instances. They retain layout size and use diagnostic GPU/Compatibility pixels. Version-4 tolerant archives preserve unknown texture fields and missing dependency metadata, retaining missing-payload provenance across saves.

Shader import retains logical bool and boolean vectors/arrays in validated SPIR-V metadata. Materials expose bool scalars and int vector masks; raw unsigned fields retain their numeric types. Both source languages and compatible external artifacts share reflection and backend checks. See [the boolean contract](../components/shader-materials.md#boolean-type-information).

The Resources domain defines reusable typed data, portable CPU image buffers and binary masks used by textures, atlases, importers, and other assets across the runtime targets. It contains the common resource contract, managed `Image` and `BitMap`, and the partial shader/material integration described below. Texture resources, ordinary Node/Texture drawing and sampled shader bindings are executable; PNG/JPEG/WebP/BMP/TGA/SVG file/buffer decoding, PNG/JPEG/WebP saving and synchronous ImageTexture/FontFile/WAV/MP3/Ogg loading are executable. General asset loading remains incomplete. SDL_image is an approved internal dependency for the codec integration.

Resource base and image sources live under `src/Core/IO/`; shader/material/texture/frame-library/curve/font resources live under `src/Scene/Resources/`. The public namespace remains `Electron2D`.

[ADR 0091](../decisions/scripting.md#adr-0091) owns the implemented concrete `Script : Resource` for compiled C# source/type metadata, typed construction and registered source/archive loading. It merges the reference Script/CSharpScript roles and keeps source assets separate from instance gameplay state. [The scripting component](../components/scripting.md) records real registration/file prerequisites, consumer checks and remaining editor/tool limits. Ordinary nested scripts stay shared under all deep-copy modes; force/root and scene-local copies retain their own policies.

## Component inventory

| Component | Types | State |
| --- | --- | --- |
| [Canvas rendering](../components/canvas-rendering.md) | [`SpriteFrames`](../classes/SpriteFrames.md), [`SpriteFrames.LoopMode`](../classes/SpriteFrames.LoopMode.md), [`AnimatedTexture`](../classes/AnimatedTexture.md), [StyleBox](../classes/StyleBox.md), [StyleBoxTexture](../classes/StyleBoxTexture.md), [StyleBoxLine](../classes/StyleBoxLine.md), [StyleBoxEmpty](../classes/StyleBoxEmpty.md), [StyleBoxFlat](../classes/StyleBoxFlat.md) | Retained decoration and named animation data for AnimatedSprite and independent timed texture playback, with graph copying and borrowed sources |
| [Gradients](../components/gradients.md) | Gradient and its mode/space enums, GradientRampTexture, GradientTexture and its fill/repeat enums | Managed interpolation and lazy RGBA8/RGBAF texture generation, typed copies/local ownership and native canvas/material sampling |
| [Curves](../components/curves.md) | [`Curve`](../classes/Curve.md), [`Curve.TangentMode`](../classes/Curve.TangentMode.md), [`Curve2D`](../classes/Curve2D.md), [`CurveTexture`](../classes/CurveTexture.md), [`CurveXYZTexture`](../classes/CurveXYZTexture.md), [`CurveTexture.TextureModeEnum`](../classes/CurveTexture.TextureModeEnum.md) | Managed scalar/spatial sampling, tangents, baking, tessellation and nearest queries with typed copying and scene-local ownership |
| [Typed themes](../components/themes.md) | [Theme](../classes/Theme.md), [Theme.DataType](../classes/Theme.DataType.md) | Six typed data categories including borrowed Font resources, exact copies and scene lookup integration |
| [Resource base](../components/resources.md) | [`Resource`](../classes/Resource.md), [`DeepDuplicateMode`](../classes/DeepDuplicateMode.md) | Typed base operations and physical consumers execute; wider derived storage/RID/editor coverage remains open |
| [Text](../components/text.md) | [Font](../classes/Font.md), [FontFile](../classes/FontFile.md), [LabelSettings](../classes/LabelSettings.md) | Validated encoded bytes, fractional metrics/shaping, borrowed fallbacks, cached glyph textures and shared label effects |
| [Resource loading](../components/resource-loading.md) | [`ResourceLoader`](../classes/ResourceLoader.md), [`ResourceLoader.CacheMode`](../classes/ResourceLoader.CacheMode.md) | Synchronous image-texture, dynamic font and audio files use the weak path cache; general loader remains Partial |
| [Images](../components/images.md) | [`Image`](../classes/Image.md), [`BitMap`](../classes/BitMap.md), Image's seven nested enums, [`ImageMetrics`](../classes/ImageMetrics.md), [`ClockDirection`](../classes/ClockDirection.md) | Managed image and mask processing implemented and verified; nine file extensions including DDS and EXR; PNG/JPEG/WebP/EXR saving; further codec semantics pending; copied byte/BCn/HDR pixels feed textures |
| [Noise](../components/noise.md) | [`Noise`](../classes/Noise.md), [`FastNoiseLite`](../classes/FastNoiseLite.md), [`NoiseTexture`](../classes/NoiseTexture.md) | Abstract and built-in 1D/2D samplers, plus generated texture with gradient, normal-map and mipmap processing |
| [Shader materials](../components/shader-materials.md) | [`Shader`](../classes/Shader.md), [`Shader.Mode`](../classes/Shader.Mode.md), [`Material`](../classes/Material.md), [`ShaderMaterial`](../classes/ShaderMaterial.md), [`CanvasItemMaterial`](../classes/CanvasItemMaterial.md) and its [`BlendMode`](../classes/BlendMode.md), [`Texture`](../classes/Texture.md), [`ImageTexture`](../classes/ImageTexture.md), [`AtlasTexture`](../classes/AtlasTexture.md) | SPIR-V fragment programs and typed uniforms execute on Linux Wayland/Vulkan; fixed canvas blending executes on Wayland GPU and compatibility hardware, with software limited to Mix; further mappings remain pending |

## Public surface

The domain exposes resource name/path/scene configuration, built-in classification, synchronous change/setup events, local-scene association, reset and raw-cache hooks, copy and graph-preserving duplication, explicit deep-copy policy, scene ID generation, path takeover, typed property descriptors, deterministic disposal, and typed CPU image storage/processing across uncompressed and raw GPU-compressed formats.

ResourceLoader uses the existing process-wide weak path cache to load ImageTexture, dynamic FontFile and WAV/MP3/Ogg AudioStream resources. Typed generic load, existence and extension discovery cover the integrated image/font/audio format families. Reuse retains a live cached wrapper, Ignore returns an unregistered independent wrapper, and Replace decodes before updating the same cached texture, font or audio wrapper; deep modes coincide with their ordinary modes for leaf image/font/audio files. Callers own loaded resources. Live Wayland compatibility/GPU Sprite pixels pass before and after a file replacement.

BitMap adds a packed boolean grid with Image alpha import, L8 export, nearest resize, region mutation, circular morphology and marching-squares polygon extraction. Its operations are managed and independent of a physics or rendering backend.

Noise adds an abstract scalar sampling contract and managed grayscale/seamless image generation. NoiseTexture borrows a concrete sampler and optional Gradient, publishes deferred first and later background images on the scene owner, retains completed pixels during work, and uses synchronous reads for library hosts. FastNoiseLite/NoiseTexture authored settings have compiled portable schemas and independent local scene graphs. Applications can derive a sampler or use the built-in FastNoiseLite resource.

Shader resources add copied binary loading and reflected typed parameter discovery. ShaderMaterial adds borrowed Shader assignment and typed scalar/vector/matrix/array values (including RGB Color and Rect2 aliases, Vector3 float3 values, Vector3i signed/unsigned triples, unsigned vectors with preserved component bits and Transform float2x2 basis values) and borrowed Texture bindings with independent resource copies and migration across shader reload. ImageTexture adds copied pixel snapshots, Update, logical size overrides, mip metadata and independent image readback; Texture is its direct abstract parent. AtlasTexture adds borrowed rectangular views with margins, nested drawing, CPU crop/opacity queries, shared source GPU storage and graph-aware resource duplication.

Curve and Curve2D supply independently editable scalar and spatial Bézier resources with lazy caches, typed indexed properties and exact-state duplication. Their managed checks include an executing Entity consumer and PackedScene ownership. Scene Path/PathFollow now consume Curve2D; CurveTexture/CurveXYZTexture generate float snapshots with live subscriptions and the same resource-copy ownership. Texture dimension queries are GetWidth/GetHeight/GetSize; generated textures add writable Width. Native GPU sampling and explicit unsupported-float fallback rejection are verified in the curve component.

Gradient adds typed color points and three interpolation modes/spaces. GradientRampTexture and GradientTexture supply inclusive ramp samples and planar fills with lazy source updates. CPU generation and copying need no renderer; native sampling and explicit HDR backend limits are verified in [Gradients](../components/gradients.md).

AnimatedTexture stores up to 256 borrowed texture slots with per-slot duration, scene-aware duplication and source change forwarding. Active sources set the smallest logical dimensions; larger current frames are cropped for image queries and native sampling, including live source resize. The renderer advances it on unscaled monotonic time; CPU configuration and copying need no renderer. AtlasTexture slots and dependency cycles are rejected. Its behavior and native verification are recorded in [the class](../classes/AnimatedTexture.md).

## Dependency direction

Resources depends on Core and, narrowly, Scene's `Node` type for `Resource.GetLocalScene()`. Image and BitMap processing use Core `Color`, `Vector2`, `Vector2i`, and `Rect2i`. Scene's packed-scene component in turn depends on Resources for typed resource duplication, so ADR 0023 accepts a contained Resources↔Scene type cycle inside the single `Electron2D.dll`. Resource base and managed image/mask processing remain independent of rendering/importing/editor. Image file/buffer codecs use internal SDL3-CS and FileAccess without exposing native handles.

Concrete Shader/ShaderMaterial resources use the internal rendering reflection and uniform-upload path. SDL3-CS and the already packaged SPIRV-Cross native library remain internal; public material APIs expose engine value types and typed descriptors. ResourceLoader consumes `Image.LoadFromFile`, `ImageTexture.CreateFromImage`/`SetImage`, transactional `FontFile` and compressed audio decoding, `FileAccess` path policy and Resource's weak path registry. Resource base and Image retain their independent managed behavior.

## Domain-wide invariants

- Electron2D-owned code stays in `Electron2D.dll`.
- Resource state is typed; there is no dynamic property bag or untyped reflection-based copier.
- Registered paths are ordinal, process-wide, weakly held, and single-owner.
- Reset reaches the typed transient hook without manufacturing Changed. Path callbacks observe committed cache state; ordinary content events are synchronous, including explicit reset-hook publication and reentrant delivery.
- Synchronous loader cache decisions serialize; decoding failures preserve existing cached image pixels or font bytes and resource identity. Loaded resources remain caller-owned.
- Derived resources explicitly define construction and stored-state copying.
- Common resource copying includes inherited translation policy/domain as stored state, including scene-local duplicates; successful coalesced notifications observe the completed state.
- Successful duplication preserves graph topology across selected duplicated edges and clears their external identity.
- Per-edge policy is selected before cached targets; only duplicated edges remap aliases/cycles. Shared root/external references remain original, and a forced copy never redirects ordinary shared edges.
- Ordinary nested Script assets remain shared under deep policy; explicit root/force wrapper copies and scene-local/isolated authoring retain their separate lifetime and source rules.
- Scene-local duplicates preserve graph topology, receive their owning scene root before setup, run setup once, and are disposed by that root.
- Failed duplication disposes its complete partial result.
- Factory results created before their invocation are rejected before copying or acquiring ownership; rollback never releases those caller-owned resources. Previously acquired targets cannot be returned for a distinct source.
- Base state is concurrently safe; derived state must define any stronger contract.
- Managed object memory is reclaimed by the runtime; deterministic disposal controls logical and native-resource lifetime, not managed memory reclamation.
- Public resources do not expose manual reference counting. Font layout internally leases immutable source snapshots so native retirement waits for active reads; this does not add a public asset-manager or lease API.
- Serialized resource state and ownership semantics must remain portable across all five runtime targets; platform-native payloads require explicit internal backends.
- Image buffers own copied bytes, use canonical little-endian multi-byte fields, publish complete states atomically, and never expose mutable backing storage.
- BitMap keeps packed bits private, snapshots before morphology, and publishes changes outside its state lock.
- BC1–BC7 compression/decompression, DDS raw/HDR/mip storage and prepared texture sampling execute; [ETC/EAC/ASTC decoding](../components/image-block-decoding.md) now supplies complete channel/HDR/mip roles and shared texture consumers; their encoding remains separate. See [BC/DDS integration](../components/image-bcn.md).

## Current limitations

`Image`, `BitMap` and `FastNoiseLite` are concrete managed assets; Noise is the abstract sampling contract. ResourceLoader/Saver execute explicit typed resource/scene archives and compiled format extensions, with UID/dependency graphs, cache reconciliation, threaded preparation and owner publication. Loaded roots/instances retain internal file-graph ownership while reused external sources stay borrowed. There is no automatic file-schema discovery or public manual reference counting. NoiseTexture's owner/worker/file/local graph and Linux rendering profile is verified below; foreign execution/import/package, editor tooling and native allocator totals remain separate gates.

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

The Linux package's FreeType 2.13.3, private producer's FreeType 2.14.3 and HarfBuzz remain internal; shaping retains 26.6 precision, contextual features and scalar clusters. Private ICU 78.3 supplies dictionary word/line boundaries alongside the managed Unicode 17 layout helpers. Serialized native operations run on the font worker; active-reader leases protect native source retirement. Immutable glyph-image snapshots already recorded by a canvas remain usable after font replacement or disposal. The snapshots hold CPU pixels, and retirement ends their forced backend residency without keeping the owning font alive.

[LabelSettings](../classes/LabelSettings.md) borrows a Font and stores base font/outline/shadow values plus ordered stacked effects, with typed indexed descriptors and ordinary resource graph copying. [Label](../classes/Label.md) is the scene consumer for these effects, inherited font themes and text layout. [FontFileTests](../../tests/Electron2D.Tests/FontFileTests.cs), [FontResourceLoaderTests](../../tests/Electron2D.Tests/FontResourceLoaderTests.cs), [FontLifetimeTests](../../tests/Electron2D.Tests/FontLifetimeTests.cs), [LabelSettingsTests](../../tests/Electron2D.Tests/LabelSettingsTests.cs) and [ThemeFontTests](../../tests/Electron2D.Tests/ThemeFontTests.cs) cover the implemented resource contracts. Font discovery, bitmap/cache authoring, MSDF, public TextServer RID operations and Theme file import remain separate capabilities; see [Text](../components/text.md) and [ADR 0046](../decisions/rendering.md#adr-0046) for precise verification and platform limits.

The [texture identity slice](../components/canvas-rendering.md#texture-resource-identities) links borrowed resource RID lifetime to actual server-owned texture creation/update/replacement/free and retained CanvasItem drawing. Owned identities expire with the active renderer; borrowed resources survive. It executes on both native baseline backends with explicit format/platform limits.

[Texture proxies](../components/canvas-rendering.md#texture-proxies) now provide real borrowed-source retargeting and shared native sampling through stable owned aliases. Source/intermediate release leaves reconnectable empty aliases; owned source allocations remain until free/shutdown.

## Audio resources

The [audio component](../components/audio-playback.md) adds reusable AudioStream, independent AudioStreamPlayback/Resampled and copied AudioStreamWAV with typed AudioWAVImportOptions. Static WAV import/save and independent Resource duplication execute; player PackedScene state borrows the source. Generic ResourceLoader audio registration and file scene authoring remain separate absent integration. Internal PCM/IMA/QOA preparation feeds the native server through bounded buffers; no public backend type is exposed.

Audio import/playback ownership and codec verification are documented in the [audio component](../components/audio-playback.md). AudioCompressedTests verifies generic cache identity, ignored resources, validated replacement, retained old playback and malformed rollback; multichannel Vorbis retains its pinned decoder dependency.

[AudioStreamRandomizer](../classes/AudioStreamRandomizer.md) uses Resource graph copying and scene-local ownership for borrowed child pools. Its typed indexed descriptors provide authoring; the audio component owns selection, child playback lifetime and native execution.

[Typed 2D mesh surfaces](../components/meshes.md) now execute Mesh/ArrayMesh/MeshInstance, CanvasItem.DrawMesh and logical server-owned mesh production on the shared canvas path. The owning [ADR 0092](../decisions/mesh.md#adr-0092) records copied typed channels, packed region updates, material/lifetime rules and exact deferred deformation/channel consumers. Native pixel and warmed frame evidence is separate from other-platform or owner acceptance.

MultiMesh owns copied finite packed 2D instance records and transient previous/current presentation pairs while borrowing Mesh. Duplicate storage is independent and follows inherited shallow/deep/scene-local resource graph rules. Resource edits commit before Changed callbacks; mesh consumers invalidate canvas safely on worker notifications. [The mesh component](../components/meshes.md#repeated-instance-resources) records cold allocation and warmed replay limits.

AudioStreamSynchronized stores only active-prefix child/volume configuration in copies and scene graphs while retaining hidden slot assignments on the original resource. Child resources are borrowed; each playback owns independent child playback and scratch state. Structural factory/cleanup failures preserve or commit ownership at the documented boundary, and microphone composites preflight owner-thread cleanup. See [synchronized streams](../components/audio-playback.md#synchronized-streams).

## TLS resources

[Certificate and key resources](../components/tls.md) provide ordered chains, RSA/EC public/private material, PEM/DER files and typed weak ResourceLoader cache modes. They use ordinary Resource duplication/lifetime; active TLS use retains payload state until disconnect. General saver/serialization/editor workflows remain separate.

## Typed file integration

See [resource-file contracts](../components/resource-files.md) for registered typed schemas, cache/UID resolution, file-root and scene-instance ownership, public extension hooks and exercised verification. File operations allocate outside frame processing. UID paths resolve through the permanent catalog before directory-backed path resolution; unknown UIDs fail explicitly. The archive profile does not add an editor, arbitrary import/remap rules or every resource schema.

AudioBusLayout and all 27 concrete audio-effect schemas now use the existing typed archive producer/loader. Ordered controls and effect aliases persist; private file graph retention protects applied bus configuration and derived snapshots. See [saved bus layouts](../components/audio-playback.md#saved-bus-layouts).

[Popup menus](../components/popup-menus.md) add the typed item/submenu consumer atop embedded Window presentation, shaped themed drawing, shared scroll/search controls and scene/file factories. Native/system menu services and dependent MenuButton/OptionButton/TabContainer/context-action consumers retain separate coverage.

[Tab panels](../components/tab-panels.md) combine TabContainer/TabBar selection with actual child Control visibility/layout, typed themes, popup input, group page drags and fresh scene factories. Warm selection/layout/visibility and native render checks pass on current Linux Wayland GPU/compatibility; inherited semantic-service/editor and other-platform acceptance remain separate.

[Dropdown choices](../components/dropdown-choices.md) integrate OptionButton selection, caption/radio state, real menu/search/shortcut input and fresh-process factories through current Button/PopupMenu and text/theme backends. Native keyboard/readback and warm selection checks pass on Linux Wayland GPU/compatibility; inherited semantic/editor/native-popup and broader platform limits remain distinct.

[File dialogs](../components/file-dialogs.md) now execute scoped browsing, five selection modes, typed custom options, filters, menus, overwrite/folder workflows and recoverable desktop Linux trash. The shared FileDialogMode identity spans the custom browser and DisplayServer; native-file-extra and foreign platform gates remain explicit. Six permanent ui_filedialog actions use the existing InputMap settings loader.

[Numeric input](../components/numeric-input.md) adds SpinBox formula/text/arrow/repeat/relative-drag authoring through shared Range and LineEdit, fresh scene factories and generated numeral localization. Current Wayland GPU/compatibility capture/input/pixels and prepared active rendering are exercised; precise pointer warp, inherited semantic/editor and foreign target gates remain separate.

[Color authoring](../components/color-authoring.md) connects spatial/numeric color editing, local swatches, typed palette files, owned popup buttons and completed application-viewport sampling. Native external capture and semantic/foreign-target gates remain separate.

[FontVariation](../classes/FontVariation.md) and Font.FindVariation supply independent native font instances: copied axis coordinates, collection faces, synthetic outlines, spacing, baseline and selected/custom palettes. Font metadata exposes validated axis bounds and predefined palettes. RichTextLabel consumes these instances through its full font-tag options. [Text](../components/text.md) records ownership, storage and the local verification boundary.

[Compiled C# source assets](../components/scripting.md) register exact project Node/Resource factories and typed schemas, validate portable-PDB source/type association, load/save Script resources, and reconstruct actual user classes through scene archives. Project callbacks retain ordinary lifecycle and source-resource disposal does not unload their compiled class. RichTextLabel consumes owned compiled effect instances. Live reassignment, implementation reload/migration, scene RPC and foreign target source-symbol prerequisites remain explicit dependencies.

## Bitmap/indexed font integration

[Bitmap font authoring](../components/bitmap-fonts.md) connects FontFile indexed image/glyph/kerning/metric records and matching configured FontVariation resources to the existing HarfBuzz and common canvas/control path. Copied pixel UV regions preserve clipping and recorded image snapshots; authored publication retires native data after active readers finish. Text/binary v3 import, typed archive/fresh-process restoration and current Linux GPU/compatibility prepared output are exercised. Source policies and other platform/native-allocator gates remain explicit.

## System font integration

[System font matching](../components/system-fonts.md) adds installed families/styles/logical collection faces and owned automatic text fallback over the shared native owner and canvas path. FontFile.AllowSystemFallback defaults to true; explicit resources retain precedence and explicit support queries remain distinct from automatic rendered coverage. Active parent readers retain retired fallback faces through policy changes. SystemFont archives store preferences and rematch the host. The current Linux catalog and both canvas consumers are exercised; CoreText/DirectWrite, extra raster/MSDF, native allocator and foreign acceptance gates remain explicit.

## CPU particle integration

[CPUParticles](../classes/CPUParticles.md) and [CPU particles](../components/cpu-particles.md) connect ordinary scene internal processing, scalar curves/gradients, borrowed textures/materials, typed file graphs and shared canvas output. Configured CPU simulation and sprite-sheet replay execute on current GPU/compatibility. World emission follows complete physics poses and uses a separate canvas basis for visible world quads. GPU compute/process materials, their conversion, foreign/native allocator and owner acceptance remain separate exact dependencies.

[Threaded resource loading](../components/threaded-resource-loading.md) now stages isolated graphs on workers and publishes cache state on the scene/consuming owner. Request/status/get, true dependency parallelism, duplicate collection, cancellation, deep modes and graph cleanup are executable; shared-payload resource management, remaining import/pack/schema/editor capabilities retain separate scope.

[Tile navigation layers](../components/tiles.md#navigation-layers) connect typed TileSet masks and borrowed TileData polygons to scene-owned NavigationServer regions, runtime cell data, root-local baking and retained diagnostic drawing. Global navigation diagnostics remain separate work.

[Atlas frame animation and padding](../components/tiles.md#atlas-frame-animation-and-padding) provide complete frame reservations/configuration, source-owned transient padded textures and retained timed TileMapLayer drawing. Physics/navigation geometry remains independent of visual frames.

[NoiseGenerationTests](../../tests/Electron2D.Tests/NoiseGenerationTests.cs), the 142 pinned image/kernel cases and [NoiseGenerationRenderingTests](../../tests/Electron2D.Tests/NoiseGenerationRenderingTests.cs) verify deferred/background image production, source snapshots, failures/lifetime, complete portable/local graphs and actual queued old/new pixels. See [Noise](../components/noise.md) for exact measurements and platform limits.

[Per-tile drawing](../components/tiles.md#per-tile-materials-and-draw-order) now connects material resources, Z/Y offsets, UV clipping and stored layer grouping/order to actual retained canvas output.

[Scene collection tiles](../components/tiles.md#scene-collections) retain shared PackedScene graphs and compiled factory root validation, with deferred ordinary child placement and explicit editor/physics prerequisites.

## Native device image consumers

The active GPU renderer now retains a RenderingDevice and directly samples its ordinary/array images through TextureRD, TextureLayeredRD and TextureArrayRD. Native allocations stay on the device timeline; canvas frames require no CPU snapshot/upload. Borrowed/owned canvas wrapper lifetimes, role validation, transient archive bindings, readback conversion, alias replacement and automatic global submission are described in [local compute](../components/local-compute.md#device-images-in-canvas).

Integer device image consumers now validate float/signed/unsigned sampled scalar types and array roles, migrate reload bindings and use nearest native integer sampling. Cold snapshots expose raw-valued float32 RGBA; byte device readback preserves all integer bits. See [the integer canvas checkpoint](../components/local-compute.md#integer-canvas-sampling-checkpoint) for executable evidence and the connected scalar-aware lighting and alpha-mask behavior.

[Missing-resource recovery](../components/resource-files.md#missing-resource-recovery) executes typed opaque field storage, version-4 identity relocation and typed ordinary-texture fallback and strict/tolerant synchronous/threaded policy. Unknown mesh factories retain bounded surface-free placeholders and can recover actual geometry in a later compiled process. Legacy unknown schemas, missing Node/signal schemas and other exact asset-family substitutions remain explicit dependencies.

## Font support policy

FontFile owns exact language/script override dictionaries with copied query arrays and bounded typed archive storage. Font primary-source support queries combine explicit/wildcard policy with native script metadata, independently of fallback glyph coverage. Source reload, indexed authoring, duplication and FontVariation preserve policy. Changes invalidate cached text, and the existing canvas/Label/RichTextLabel pipeline executes stable support-aware fallback choice. The [text component](../components/text.md#support-policy-and-fallback-priority) records executable native and storage evidence.

## Native font raster preferences

FontFile/SystemFont native strike and autohinter flags execute through the retained FreeType owner; generated glyph textures and indexed pages publish complete MIP chains. Replacement snapshots preserve active old readers, authored bitmap records, configured variations, copied preferences and bounded fresh-process storage. [Text verification](../components/text.md#native-raster-policy-verification) records actual pixel/sampling and allocation boundaries.

Animation graph resources and AnimationTree scene controllers now persist authored topology, compiled writable parameter IDs and typed values through the existing archive. See [graph file persistence](../components/scene-animation.md#graph-file-persistence). Resource validation runs after all embedded payloads and before publication.

RESET save preparation now executes through AnimationMixer.ResetOnSave and PackedScene.PackForSave. Enabled RESET value/Bézier tracks run at zero on detached nodes and isolated copied resources, capturing coupled effects while preserving source preview values/clocks/queues. Ordinary Pack captures the actual pose. Preparation failures preserve prior packed data; temporary resources are cleaned or transferred to the prepared scene. Compiled factories/setters/postprocessing execute, while lifecycle, local-resource setup and method/audio/nested event tracks are suppressed. Custom authoring state must be stored and resource duplication supported. AnimationResetSaveTests exercises policies, all controller families, coupled values/resources, errors, lifetime and portable saved scenes.

Resource directory discovery connects current format registration and path policy to actual fresh-process scene/resource loading; see [resource loading](../components/resource-loading.md#resource-directory-discovery).

The primary realized face supplies OpenType metadata through `GetSupportedFeatureList()` and `GetOTNameStrings()`. The feature snapshot contains numeric tags and typed optional labels, boolean/integer selection and hidden flags. GSUB is read before GPOS, whose duplicate descriptions win. Name strings are copied by native language spelling and standard role; unknown IDs retain `unknown_N` keys. These cold snapshots allocate independently and omit fallback metadata or authored family overrides. Feature labels retain the tool locale sampled during native realization; source replacement or cache retirement rebuilds metadata.


## Custom image decoder integration

[ImageFormatLoader](../classes/ImageFormatLoader.md) and [ImageFormatLoaderExtension](../classes/ImageFormatLoaderExtension.md) provide caller-owned file decoder registration. Custom suffixes participate in Image loading, resource extension/existence/directory discovery, synchronous/threaded ImageTexture loading and cache replacement. Ordered callbacks execute outside the registry lock with borrowed temporary Image/FileAccess objects; false means unrecognized content, exceptions preserve the destination, and success copies pixels before one Changed notification. Encoded input retains the 64 MiB limit. Registration is process-local and must precede fresh-host file loading. Keep decoders alive through pending worker calls; removal affects subsequent snapshots. Built-in buffer codecs and their channel/color metadata limits remain separate.

[Inherited shape copying](../components/physics-registered-adapter.md#inherited-shape-copying-2026-10-14) now preflights every target borrower before Resource reset, name/locality, ownership and Changed effects. Public-only CPU/GPU/registered serial/separate checks cover nine shape families plus an application Shape, ordinary callback/query continuation and strict zero warmed frame allocations. Remaining inherited and complete numerical/native/performance gates stay open.

Standalone Shape.ResetState uses the same preflight before OnResetState. The public-only regression rejects reset during numerical/access/geometry hooks, checks no custom-reset side effects, and preserves allowed quiescent reset without Changed. Copying invokes reset exactly once after its own accepted preflight.

WebP lossless/lossy saving, live codec settings, compressed-source conversion, atomic files and texture/capture consumers are described in [the executable codec contract](../components/image-webp.md). Foreign-target native execution and prebuilt bundle publication remain unverified.

The [EXR I/O slice](../components/image-exr.md) supplies precision-preserving HDR file/buffer load and lossless save. Image and texture resource discovery includes DDS and EXR alongside PNG/JPEG/WebP/BMP/TGA/SVG; typed archives also expose their registered extensions. GPU float sampling, explicit Compatibility byte conversion and float-driver rejection are separate exercised profiles.

Typed tile array defaults use an existing exact array type without dynamic element-type construction or an archive codec. Script source association still requires a file-backed assembly, matching PDB and complete project metadata; its registration APIs now declare those prerequisites, and metadata queries copy the registration-time declarations. See the tiles and scripting components for executable scope.

Scene-local physical resource graphs now preserve borrowed non-local identities for both built-in and external paths. Local shapes/resources retain aliases and cycles within each instance and isolate geometry between instances. Setup sees the complete detached scene once per local duplicate. GetLocalScene borrows immutable weak object identity, returns null when the root starts disposal or is collected, and transfers with ownership on Node.ReplaceBy. Ordinary deep duplication and explicit force/isolated-authoring policies retain separate rules. Public-only --local-scenes exercises CPU/GPU/registered ordinary/separate worlds, actual query identities, GC retirement, setup rollback and strict 0/0 managed bytes for 128 warm/128 measured two-scene intervals; the connected base/packed/replacement/authoring suite also passes. Evidence is retained under bin/physics-local-scenes/2026-10-14. Full inherited, native allocation, foreign-platform and hardware/window/performance acceptance remain open.

Internal resource graph IDs are staged before persistence: first authored IDs remain stable, missing and duplicate IDs are generated without colliding with reserved authored values. IDs commit only after atomic writing, while source path replacement follows its explicit flag. Post-write path-hook failures attempt later paths and retain the committed IDs/file. Read validation rejects duplicate resolved embedded paths before factories or cache replacement, including legacy fallback indices. Public-only physical scene identity checks exercise actual saved aliases and backend simulation; exact executable evidence and platform limits are recorded with the current validation cohort.

## Named scene assets

[ResourcePreloader](../components/resource-preloading.md) supplies case-sensitive named resource slots with numbered duplicate/rename handling and copied name enumeration. Existing PackedScene and archive graphs retain aliases, local-scene copying and file ownership through exact typed reserved slot recovery. The holder borrows ordinary assets and exposes no loading task. Runtime/test details and verification boundaries are in the component.

Physical copy-failure consumers now verify partial-state publication, nested/reentrant batches and throwing graph cleanup against actual selected simulation/query identities. The precise enum policy excludes ordinary Script references and allows explicit force/never fields. These results extend the exercised base/physical contract; the component and coverage retain wider Resource obligations instead of a blanket completion claim.


Mounted res:// resources now share [resource pack resolution](../components/resource-packs.md). PCK/ZIP overlays, synthetic directories, read-only packed metadata, relative scene dependencies and bitmap-font pages use the mounted bytes. Ordinary absolute paths and user:// retain physical access. Cached resources keep their normal cache-mode policy.


[DPITexture](../components/scalable-svg.md) stores SVG source and typed paint maps as resource data, with independent scene-local copies and transient viewport raster caches. Existing Image SVG codec limits remain separate from this retained texture/resource integration.

[Video resources](../components/video-playback.md) now load bounded Ogg sources, instantiate independent prepared decoders and persist exact source/player configuration through resource and scene archives. Fresh-process authoring is exercised separately from rendered playback.

The [KTX image slice](../components/image-ktx.md) adds flat KTX1/KTX2 file/buffer and resource loading, raw precision/mips, portable Basis LDR/HDR transcoding, bounded metadata/DFD validation and independent rendered-pixel checks. Compressed array assets remain dependent on their container, legacy Basis channel roles and resource integration; the codec prerequisite alone does not implement those types.

[Linked packed scenes](../components/linked-scenes.md) reuse compatible local resources when applying nested overrides, preserve aliases/cycles and transfer created resources to the appropriate nested owner. Version-5 archives persist template references while retaining version-4 missing-payload roles.

[Packed mesh surfaces](../components/meshes.md#packed-server-surfaces) now connect typed binary descriptions to owned RenderingServer creation/append and copied export from ordinary/custom resources. Position/direction/custom/skin/morph/LOD/material data and authored visibility bounds reach the existing renderer. Real software GPU/GLSL, GPU/HLSL and Compatibility profiles verify output and prepared allocation limits; procedural position-free formats retain their exact shader producer prerequisite.

[Manual mesh visibility bounds](../components/meshes.md#manual-mesh-visibility-bounds) connect stored ArrayMesh policy and static server access to fitted groups, morph output and both instance drawing paths, independently of source geometry bounds.

## Fixed sampled descriptor arrays

[Canvas sampled descriptor arrays](../components/shader-materials.md#fixed-sampled-descriptor-arrays) now execute through GLSL/HLSL, typed resource and static RID access, indexed shader defaults, complete validation, graph storage and derived scalar native bindings. Paired stages and existing lighting/glyph/instance/HDR/MSAA consumers share the same physical capacity and resource lifetime. See the component for measured software-Vulkan checks and the separate hardware/platform gates.

Prepared skeletal stacks now expose logical metadata to active processing groups and execute pose callbacks in their skeleton's current group. Binding/setup and resource configuration stay with the owner; callbacks release the stack metadata gate while topology/reentry guards remain active. See [grouped skeletal execution](../components/skeletal-animation.md#prepared-modifications-in-processing-groups).

DrawableTexture archives and graph duplication preserve authored setup with independent cleared GPU storage; transient blit results are not serialized. BlitMaterial preserves blend mode, and Shader archives retain TextureBlit domain metadata. See [writable textures](../components/drawable-textures.md).

Factory construction provenance is shared by resource, script, file and scene creation through [ElectronObject](../classes/ElectronObject.md#factory-construction-provenance). It rejects unrelated concurrently created results while preserving nested, Task and engine worker construction; [ADR 0013](../decisions/resources.md#adr-0013) owns this boundary.

## Localized resource files

ResourceLoader owns localized file selection and weak live-resource refresh, using the locale score supplied by Localization. Logical cache/path/UID identity survives physical file changes. The existing graph/cache/format pipeline handles archives, images, fonts, WAV, shader includes and mounted ZIP input; relative dependencies and stale threaded publication are exercised. Refresh is explicit cold work before application translation notification. See [localized resources](../components/localized-resources.md).

Shader resource storage now includes a bounded typed sampler-state table. [Sampler metadata](../components/shader-materials.md#explicit-image-sampler-state) copies with the resource, survives compatible program replacement and real fresh-process file loading, and rejects malformed tables before publication. The built-in Shader persists by a versioned identity rather than its renderer-private module interface.

Compiled [particle and writable-texture visual graphs](../components/visual-shaders.md#particle-and-writable-texture-graph-domains) use the existing offline importer, immutable program/material ownership and native compute/blit consumers. Lifecycle graph roots, exact device birth numbers, borrowed planar mesh emission and typed supplied-field masks execute through those consumers; light/varying/editor transport retains its own gate.
