# Rendering domain

[Layered device multiview](../components/local-compute.md#layered-multiview) executes ordered view-indexed draws into array color/depth targets, including per-view input passes and MSAA resolve, without changing extra layers.

[Mesh textures](../components/meshes.md#mesh-textures) connect real retained mesh geometry to Texture consumers and static texture draw identities, with borrowed source lifetime and typed graph/scene storage.

[Particle shaders](../components/particle-shaders.md) now execute a validated HLSL/GLSL compute profile through public RenderingDevice, with typed material/archive state and portable bounded-subemission includes. [Retained GPU particles](../components/gpu-particles.md) now execute server-owned simulation, mesh canvas output, trails and subemitters. [ParticleProcessMaterial](../classes/ParticleProcessMaterial.md) now authors the native built-in process. [GPUParticles](../classes/GPUParticles.md) now executes scene emission and CPU conversion.

[Numeric material matrices](../components/shader-materials.md#numeric-material-matrices) support copied mat3/mat4 values and fixed arrays through resources and static server identities, with typed storage, identity defaults and layout-aware paired-stage reload.

[Original mesh vertex IDs](../components/shader-materials.md#authored-vertex-stages) now reach authored GLSL/HLSL vertex programs across indexed/expanded geometry, LODs, deformation and native instances. The verified profile is Linux Wayland software Vulkan; procedural position-free storage and generated-position transforms now execute.

[TopLevel presentation resets](../components/canvas-rendering.md#toplevel-transition-resets-2026-10-18) now discard descendant interpolation history after a canvas-boundary change, preserving physical position/velocity, identity and tick. Public CPU/software-GPU/registered consumers retain strict zero warm managed bytes; actual dummy and software Wayland pixels cover the reset.

[Global shader parameters](../components/global-shader-parameters.md) now connect static typed session values and overrides to both authored stages, numeric matrices and ordinary/array image bindings shared by separate canvas materials. Registered project definitions prepare typed bases before scene readiness and own loaded images through session shutdown. The registry has explicit owner/session lifetime and no material-property aliases.

Last updated: 2026-10-20

[Movie writing](../components/movie-writing.md) consumes completed root-viewport pixels through the actual renderer and fixes capture dimensions across later resizes. It does not create a headless renderer or claim readback/encoding is allocation-free.

Private native runtime binaries come from versioned Linux/macOS packages in ordinary desktop builds; `Electron2DBuildNativeFromSource=true` rebuilds those libraries. Source builds separately prepare pinned host shader tools on first use and generate built-in shaders under `obj/`. [Shader delivery](../components/shader-materials.md#built-in-shader-delivery) keeps these tools out of game packages. [Native delivery](../native-packaging.md) separates audited packages, executable consumer checks and public publication; macOS runtime integration verification is pending.

[Image-array resources](../components/texture-arrays.md) execute copied homogeneous layers, typed samplers/defaults, shape-aware reload, source archives, actual GPU layer/mip upload and owned/proxy RIDs. Ordinary texture drawing retains its separate resource branch. Current native evidence is Linux Wayland GPU; compatibility rejects shader use. Copied byte, normalized/float, block and unsigned-integer images execute; foreign/native-allocation acceptance remains separate.



[Imported image textures](../components/compressed-textures.md) add ordinary `.ctex` loading, independent logical/physical dimensions, exact source mips and retained Texture consumers. Atomic replacement, typed cache/worker loading and embedded resource/scene archives run through the shared resource pipeline.

[File-backed arrays](../components/compressed-arrays.md) load raw/PNG/WebP and Basis LDR/HDR layers with complete mips, atomic source replacement, typed caching/worker loading and embedded archives. They reuse the existing GPU array consumer and borrowed RenderingDevice lifetime; Compatibility keeps its explicit custom-shader gate.

## Native text coverage modes

FontFile/SystemFont now generate real Mono/Gray/LCD native glyphs and share a captured project subpixel layout. Scalable glyph regions consume the existing GPU LCD path, while compatibility retains ordinary coverage and explicitly rejects component glyphs. [The complete font contract](../components/text.md#native-font-antialiasing-and-lcd-layout) records variation/fallback/storage, native oracle and rendered/allocation evidence. MSDF generation remains separate.

Weak logical rendering identities distinguish temporary pre-delete closure from committed cleanup. Canvas, skeleton, viewport, custom mesh and texture lookups/sweeps preserve RIDs through a veto while keeping closed targets unavailable until notification returns.

## LCD and MSDF regions

The retained canvas now draws caller-supplied LCD and MSDF textures through scene and static server commands. GPU executes coverage decoding, component blend, custom materials, lights, masks/groups and MSAA; compatibility rejects the unsupported profile. [The glyph contract and verification](../components/canvas-rendering.md#lcd-and-msdf-texture-regions) distinguish texture consumption from the separate FontFile generation prerequisite.

## Executable shader and material identities

Caller-owned compiled canvas programs and typed materials now bind directly to retained canvas items. Borrowed Shader/Material RIDs reuse the same parameter, texture, reload and native pipeline behavior. [The program contract](../components/shader-materials.md#shader-and-material-identities) records ownership, actual rendering and remaining initializer/sampler/profile dependencies.

## Executable low-level primitive producers

Seventeen copied primitive/texture/indexed-triangle and command/order/culling operations now feed the existing owned/borrowed canvas graph. [The primitive contract](../components/canvas-rendering.md#low-level-primitive-commands) distinguishes source state, real palette/native replay, limits and explicit remaining shader/material/emitter prerequisites.

## Caller-owned canvas integration

Caller-owned canvases/items and borrowed scene canvas identities now support independent native parent/property state, retained Mesh/MultiMesh/nine-patch commands and simultaneous viewport attachments on the existing rendering backends. [The canvas contract](../components/canvas-rendering.md#caller-owned-canvases-and-items) separates scene authoring, real rendering, lifetime and verification limits.

## Executable mesh skin integration

Owned/scene palettes now attach to stable scene canvas identities and deform four/eight-slot Mesh/MultiMesh skin records into real triangles on both backends. Rest-bound culling cannot discard skinned instances moved into view; prepared storage remains backend-neutral.

## Physical skeletal integration

[Physics-driven bones](../components/skeletal-animation.md#physics-driven-bones) feed solved rigid-body poses into the existing retained weighted Polygon palette. The renderer continues using ordinary triangles; server-owned palettes and generic Mesh skin storage/consumers now execute.

## Jiggle controller integration

Jiggle/held controller output now reaches the existing palette/weighted Polygon consumer on GPU and compatibility. Dynamic-point collision uses the real shared physics world and no extra render/native dependency. Whole-bone collisions, generic Mesh skin storage and server-owned palette attachment are not implied.

## Executable IK integration

Three concrete IK resources now exercise the existing skeletal palette-to-Polygon triangle path on both Linux native backend. The solvers do not add native dependencies, generic Mesh skin storage or caller-owned palette attachment. Those producer/consumer families retain exact coverage triggers.

## Skeletal canvas consumer

The [skeletal component](../components/skeletal-animation.md) prepares inverse-rest/current presentation palettes and retained strongest-four Polygon deformation for both native triangle backends. Weak scene palette identity remains borrowed; same viewport/CanvasLayer space and TopLevel/interpolation apply. General Mesh skin channels and server-owned palette attachment are still absent with exact triggers.

## Responsibility

Process-wide service operations and events use static access to retained objects under [ADR 0095](../decisions/singleton-services.md#adr-0095). Native availability remains explicit through DisplayServer.IsAvailable and RenderingServer.IsAvailable. Independent project registries use ProjectSettingsRegistry; static ProjectSettings operations address only the runtime registry.

Shader import retains logical bool and boolean vectors/arrays in validated SPIR-V metadata. Materials expose bool scalars and int vector masks; raw unsigned fields retain their numeric types. Both source languages and compatible external artifacts share reflection and backend checks. See [the boolean contract](../components/shader-materials.md#boolean-type-information).

Rendering turns retained scene commands and typed resources into frames for the active root Window and independent offscreen canvases. Runtime source is in `src/Servers/Rendering/`; it compiles into Electron2D.dll. SDL3-CS and owned SDL handles remain internal. DisplayServer exposes supported borrowed operating-system context identities under ADR 0042.

The text boundary library uses `runtimes/<RID>/native` with engine-owned resolution under [ADR 0012](../decisions/product.md#adr-0012); ICU data remains embedded in `Electron2D.dll`. macOS native source/package audits passed and the runtime now selects its dylib. First public native execution remains pending; neither this package nor the full headless suite proves macOS rendering.

## Components and public surface

| Component | Public types and integration | State |
| --- | --- | --- |
| [Canvas rendering](../components/canvas-rendering.md) | [RenderingServer](../classes/RenderingServer.md), CanvasItem drawing, [Line](../classes/Line.md), [Parallax](../classes/Parallax.md), [Sprite](../classes/Sprite.md) and [AnimatedSprite](../classes/AnimatedSprite.md) nodes, [AnimatedTexture](../classes/AnimatedTexture.md), [MarginContainer](../classes/MarginContainer.md), [CenterContainer](../classes/CenterContainer.md), [AspectRatioContainer](../classes/AspectRatioContainer.md) and [TextureRect](../classes/TextureRect.md)/Texture drawing | Executable 2D drawing and deferred margin/center/aspect GUI layout on current backends; full API incomplete |
| [Typed themes](../components/themes.md) | [ThemeDB](../classes/ThemeDB.md), Control/Window owner lookup and overrides, [Panel](../classes/Panel.md), [PanelContainer](../classes/PanelContainer.md), box/grid constants and Label fonts/effects | Six executable typed data categories and current GUI consumers; portable project themes execute; default-catalog gaps remain explicit |
| [Scrolling](../components/scrolling.md) | [ScrollBar](../classes/ScrollBar.md), [HScrollBar](../classes/HScrollBar.md), [VScrollBar](../classes/VScrollBar.md), [ScrollContainer](../classes/ScrollContainer.md), [ItemList](../classes/ItemList.md) | Clipped content and selectable text/icon rows, themed bars/hints, focus and pointer/action input on both current backends |
| [Text](../components/text.md) | [Font](../classes/Font.md), [FontFile](../classes/FontFile.md), [LabelSettings](../classes/LabelSettings.md), canvas text and [Label](../classes/Label.md) | FreeType/HarfBuzz fractional shaping, private ICU dictionary boundaries, Unicode layout, fallbacks and glyph textures on both current Linux backends |
| [Shader materials](../components/shader-materials.md) | Shader, ShaderMaterial, CanvasItemMaterial, Material, Texture, ImageTexture and AtlasTexture, owned by Resources | Executable HLSL/GLSL import, typed uniforms and sampled textures; five fixed blend modes on Wayland GPU/compatibility hardware; broader language profile incomplete |
| [2D mesh surfaces](../components/meshes.md) | [Mesh](../classes/Mesh.md), [ArrayMesh](../classes/ArrayMesh.md), [ImmediateMesh](../classes/ImmediateMesh.md), [MeshInstance](../classes/MeshInstance.md), typed channels and canvas/server RIDs | Copied static surfaces and incremental drafts draw through both current backends; advanced attributes/deformations retain exact dependencies |

Games use Sprite for texture, sheet-frame and region drawing, AnimatedSprite with SpriteFrames for timed playback, or record custom commands from CanvasItem.OnDraw through CanvasItem and Texture. RenderingServer provides the active method/driver, frame events and clear/submission controls. Engine.Run starts and closes the renderer. CanvasItem and Viewport supply filtering/repeat policies and GPU mip/anisotropy settings, with explicit fallback limits. CanvasItem.ItemRectChanged reports local geometry changes synchronously; Sprite integrates the event with its setters while resource worker notifications remain atomic redraw requests.

AnimatedSprite uses the existing internal idle lane and canvas path. Its [timing audit](../classes/AnimatedSprite.md#timing-contract-and-source-audit) records duration-transition and ping-pong behavior. Native readback covers real timed completion, texture/atlas/blank frames and callback-failure cleanup.

AnimatedTexture is a node-independent Texture that selects borrowed frame sources. RenderingServer advances it before FramePreDraw using unscaled monotonic time, then the existing texture batch path samples the selected image. Managed state and timed native readback are covered in [the canvas component](../components/canvas-rendering.md#animated-frame-playback).

## Dependencies and invariants

- Uses Scene for Window, tree membership, transforms, visibility, behind-parent and nested local Y ordering, with stable Z precedence.
- Canvas traversal uses the internal visual transform when a Control enables a visual-only offset; logical global coordinates and GUI hit testing continue to use the ordinary transform. Both normal and Y-sorted descendants inherit the visual matrix.
- Uses Resources for copied image snapshots and typed materials; consumers borrow these resources.
- GPU is primary; compatibility is explicit startup fallback and rejects shaders.
- Frame submission and native resource lifetimes belong to the scene owner thread.
- HLSL and GLSL share the SPIR-V interface. Import/build compiles source; runtime consumes bytecode. Named material samplers have linear/base-level/clamp defaults independently of canvas policies; configurable named sampler state remains pending.
- Invalid resources or unsupported backend features fail explicitly. SDL types and owned graphics handles remain internal; DisplayServer may expose documented borrowed OS/context identities.

## Verification and limits

Apple static text imports and NativeFontPrecisionTests are connected to all six iOS/tvOS test-app profiles. MSBuild selection checks and local preprocessing-branch compilation are separate from pending simulator/native raster execution; no Apple rendered or physical-device acceptance is inferred.

Current native verification covers Linux Wayland GPU/Vulkan and compatibility, plus the software renderer under the dummy video driver. Pixel checks cover ordinary drawing, both shader languages, canvas ordering and lifecycle-driven recording through notifications/events/overrides; other target backends, user visual acceptance, broad-scene performance, lights, nested alpha-mask composition, meshes, remaining GUI families, multi-view/multiwindow rendering and device recovery remain unfinished. See the component pages for exact checks and limits.

[ADR 0028](../decisions/rendering.md#adr-0028) owns backend/shader decisions, [ADR 0004](../decisions/product.md#adr-0004) owns the 2D product boundary, and [ADR 0021](../decisions/product.md#adr-0021) owns the current platform gate.

Pixel-snapping integration is described by [the canvas component](../components/canvas-rendering.md#pixel-snapping). Viewport owns independent transform/vertex policies; rendering preserves logical node transforms, while Sprite local queries honor attached transform snapping. Project defaults initialize the explicit root Window at construction.

[Generated gradient textures](../components/gradients.md) feed the existing canvas and material paths. LDR live updates execute on GPU and compatibility; HDR preservation is verified on GPU and explicitly rejected by tested compatibility drivers lacking support.

Root viewport canvas/final transforms are connected to retained rendering, scene input localization and CanvasItem coordinate/pointer queries. Logical node transforms stay unchanged. [Canvas coordinate integration](../components/canvas-rendering.md#viewport-coordinates) records ownership, singular/overflow behavior, runtime-only properties and Linux Wayland/dummy verification. Camera and CanvasLayer are integrated; root content scaling, embedded containers and nested native windows remain absent; single-layer SubViewport canvases execute.

[Camera tracking](../components/canvas-rendering.md#camera-tracking) connects Camera : Entity to viewport selection, idle/physics updates, zoom/rotation, drag/limit policies and smoothing. It reuses scene ownership and canvas/input transforms; inherited physics interpolation now presents its viewport history on both backends, while editor preview remains absent.

[Parallax scrolling](../components/canvas-rendering.md#parallax-scrolling) uses camera-published screen origins and repeats retained canvas entries by a local basis offset. It keeps draw callbacks and scene nodes single-instance; the combined parallax/interpolation path has not had a dedicated native pixel audit.

[Canvas layers](../components/canvas-rendering.md#canvas-layers) provide independent drawing groups, transforms, visibility and viewport following through CanvasLayer : Node and CanvasItem.GetCanvasLayerNode. Opaque canvas identities and independent viewport rendering remain separate dependencies.

[Canvas visibility masks](../components/canvas-rendering.md#canvas-visibility-masks) connect stored CanvasItem.VisibilityLayer and Viewport.CanvasCullMask to retained submission, including parent pruning and nested Y sorting. Mask edits preserve logical visibility, callbacks and input. Native Wayland GPU/compatibility, HLSL/GLSL and dummy/software behavior is verified; independent single-layer SubViewport canvases now share these policies; layered targets retain their prerequisite.

[Transform invalidation and delivery](../components/scene-hierarchy.md#transform-invalidation-and-delivery) integrates cached canvas coordinates, coalesced global notifications, synchronous opted-in local notifications and ForceUpdateTransform. Camera and PathFollow follow the same timing; queues cancel on exit, disposal and activation rollback.

[Polygon commands](../components/canvas-rendering.md#polygon-commands) connect copied contours and attributes to the existing backend batches. Concave triangulation and short primitives have managed and native pixel checks; exact hardware subpixel point/line raster parity remains Partial.

[Stroke commands](../components/canvas-rendering.md#stroke-commands) provide joined polylines, independent pairs, dashes, circular/elliptical arcs and filled/outlined circles/ellipses with local antialias feathers. Thin widths use framebuffer triangle expansion; their exact hardware line coverage remains Partial.

[Canvas animation intervals and rectangle geometry](../components/canvas-rendering.md#animation-intervals-and-rectangles) execute in the retained command path. The render clock follows captured scaled process steps and the active wrap setting; the same clock feeds the optional GPU fragment [TIME built-in](../components/shader-materials.md#render-time).

Retained screen regions now sample the same actual render transforms, layer/mask/clip/repetition and inherited alpha as submitted canvases. All states commit before queued screen events; failures continue later nodes and membership epochs reject stale delivery. [VisibleOnScreenNotifier](../classes/VisibleOnScreenNotifier.md) and [VisibleOnScreenEnabler](../classes/VisibleOnScreenEnabler.md) provide the current runtime API. Both Linux Wayland backends and 64 warmed active neutral-target transitions are verified by [ScreenVisibilityRenderingTests](../../tests/Electron2D.Tests/ScreenVisibilityRenderingTests.cs), under [ADR 0078](../decisions/rendering.md#adr-0078). Native allocations, other platforms, layered offscreen targets and editor gizmo drawing remain outside this verification; single-layer offscreen integration has separate SubViewportTests evidence.

[NinePatchRect](../classes/NinePatchRect.md) now records one retained panel command with fixed borders, independent Stretch/Tile/TileFit axes and optional center. Live base/atlas dimensions resolve before splitting; ordinary atlas region drawing reuses that resolver. Signed margins drive Control intrinsic minimum size and inherited pointer filtering defaults to Ignore. All nine native axis combinations, center/flip/atlas/constant UV and 64 warmed resized frames are checked by [NinePatchRenderingTests](../../tests/Electron2D.Tests/NinePatchRenderingTests.cs), under [ADR 0079](../decisions/rendering.md#adr-0079). Dense CPU geometry limits, native allocator counts, other platforms and owner acceptance remain explicit.

[Range](../classes/Range.md) and [TextureProgressBar](../classes/TextureProgressBar.md) now execute shared double value policy and textured linear/centered/radial fills. Nine-patch partial progress reuses the real retained geometry/tint path. Their inherited vertical size flags now execute through [Container](../classes/Container.md) and [BoxContainer](../classes/BoxContainer.md), with Range ShrinkBegin and progress Fill defaults under [ADR 0081](../decisions/rendering.md#adr-0081). [RangeProgressTests](../../tests/Electron2D.Tests/RangeProgressTests.cs) and [native tests](../../tests/Electron2D.Tests/TextureProgressRenderingTests.cs) verify the current scope and allocation/platform limits under [ADR 0080](../decisions/rendering.md#adr-0080).

## Box and grid container layout

Container alignment uses separate nested [BoxContainer.AlignmentMode](../classes/BoxContainer.AlignmentMode.md), [AspectRatioContainer.AlignmentMode](../classes/AspectRatioContainer.AlignmentMode.md) and [FlowContainer.AlignmentMode](../classes/FlowContainer.AlignmentMode.md). Each retains Begin=0, Center=1 and End=2; properties and scene storage retain the declaring owner under ADR 0051.

[Container](../classes/Container.md) owns direct-control listeners and deferred pre/sort phases; [BoxContainer](../classes/BoxContainer.md), [HBoxContainer](../classes/HBoxContainer.md) and [VBoxContainer](../classes/VBoxContainer.md) arrange weighted primary allocations and cross-axis fill/shrink before retained drawing. Min/max refit, RTL, local signed separation and actual spacers consume [Control.SizeFlags](../classes/Control.SizeFlags.md). Typed storage preserves exact defaults/factories. Two synchronized SceneTree action queues recycle prepared captured-batch capacity; per-box scratch slots are reused. Managed and native tests verify small warmed layouts with zero managed allocation; larger GUI/native allocator/platform/owner guarantees remain unverified. Container semantic accessibility retains its precise separate dependency in [ADR 0081](../decisions/rendering.md#adr-0081).

[GridContainer](../classes/GridContainer.md) uses the same deferred phases and final child fitting for row-major cells. It truncates bound minima to integer pixels, aggregates row/column constraints and expands each selected row or column equally, independent of child stretch ratios. Empty declared columns participate in width sharing without allocating a slot for each empty column. Remainder pixels go to the first expanding occupied columns/rows; RTL reverses horizontal placement. Capped-row advancement uses the final allocated height, preventing overlap after maximum refit. Signed local HSeparation/VSeparation project the existing theme constants, and typed storage preserves exact GridContainer identity. [Managed tests](../../tests/Electron2D.Tests/GridContainerTests.cs) verify state, bounds, sparse layout, storage, callbacks and 64 warmed zero-allocation layout cycles. [Native tests](../../tests/Electron2D.Tests/GridContainerRenderingTests.cs) verify seven geometry/pixel phases on Linux Wayland GPU/compatibility and zero managed bytes for 64 warmed frames measured from ProcessFrameStarted through FramePostDraw, including resize/layout/render. Native allocator counts, large-GUI performance, other platforms and owner acceptance remain unverified; see [ADR 0081](../decisions/rendering.md#adr-0081).

## Style resources and canvas decoration

[StyleBox](../classes/StyleBox.md) supplies typed content margins, minimum size, mask and draw-bound queries plus protected custom drawing hooks. [StyleBoxTexture](../classes/StyleBoxTexture.md) borrows a texture and records nine-patch decoration with atlas resolution before expansion; [StyleBoxLine](../classes/StyleBoxLine.md) records a signed, integer-aligned rectangle; [StyleBoxEmpty](../classes/StyleBoxEmpty.md) supplies margins without decoration. CanvasItem.DrawStyleBox executes these resources during the target's normal recording scope. The current-item query spans all three recording stages and restores context after failures. Stored state uses the existing exact resource graph duplication; consumers request redraw/layout when their styles change, and texture Changed is not forwarded by the style.

[Managed tests](../../tests/Electron2D.Tests/StyleBoxTests.cs) verify margins/defaults, hook dispatch, Changed timing, integer strip geometry, recording context and failure cleanup, exact copies, scene-local resource policy, lifetime and lock boundaries. Sixty-four warmed line mutation/recording/replay cycles allocate zero managed bytes. [Native tests](../../tests/Electron2D.Tests/StyleBoxRenderingTests.cs) verify all nine axis combinations, fractional borders/expansion, center suppression, source regions, modulation, atlas ordering, line and empty drawing on Linux Wayland GPU/compatibility; each backend also passes 64 warmed style mutation/recording/render frames with zero managed bytes measured from ProcessFrameStarted through FramePostDraw. Native allocator counts, broad GUI performance, other platforms and owner acceptance remain unverified. [StyleBoxFlat](../classes/StyleBoxFlat.md) adds retained rounded fill, borders, blended edges, shadows, skew and anti-aliasing through untextured triangles; [Corner](../classes/Corner.md) supplies its stable radius index. It shares the style margin/copy/draw lifecycle. [StyleBoxFlatTests](../../tests/Electron2D.Tests/StyleBoxFlatTests.cs) verifies defaults, Corner identities, equal-write event ordering, clamps/guards, content/draw bounds, hooks, exact resource copies, scene-local behavior, callback failures and concurrency. The [independent C++ fixture](../../tests/Electron2D.Tests/Fixtures/StyleBoxFlatGeometry.json) covers 15 sharp/rounded/unequal/oversized/blended/hollow/AA/shadow/skew/signed/degenerate profiles; triangle ordering, vertex positions, colors and UVs match within 0.00005, and draw rectangles match exactly. Sixty-four warmed mutation/geometry-recording/replay cycles allocate zero managed bytes. [StyleBoxFlatRenderingTests](../../tests/Electron2D.Tests/StyleBoxFlatRenderingTests.cs) verifies three visible mutation states covering rounded corners, borders, center suppression, border blend, offset shadow, skew, AA and expansion on Linux Wayland GPU/compatibility. Each backend also passes 64 warmed mutation/recording/render frames with zero managed bytes measured from ProcessFrameStarted through FramePostDraw. Native allocator counts, broad GUI performance, nonunit viewport recording scale, other platforms and owner acceptance remain unverified. The current viewport path has identity content stretch and no canvas recording oversampling override, so feather width equals AntiAliasingSize in local units (recording factor one). Font draw oversampling and FontFile.Oversampling control glyph rasterization; they do not change the viewport recording scale or StyleBoxFlat feathers. A future viewport recording-scale integration must divide the feather by the active factor and invalidate retained recordings when it changes. Panel/PanelContainer consume actual Theme/default-skin lookup and invalidation under [ADR 0083](../decisions/rendering.md#adr-0083); Font and Label execute through the FreeType/HarfBuzz and private ICU [text component](../components/text.md). See [ADR 0082](../decisions/rendering.md#adr-0082).

[Typed themes](../components/themes.md) connect six categories—Color, Constant, Font, FontSize, Icon and StyleBox—to Control/Window owner traversal, variation/native-type order, local overrides and change delivery. Panel drawing, PanelContainer content layout, box/grid gaps and Label fonts/effects consume the resolved values. ThemeDB supplies a shared embedded Open Sans SemiBold fallback whose native font loads on first text use. [ADR 0083](../decisions/rendering.md#adr-0083) defines executable project Theme loading and keeps defaults for remaining GUI families separate. [ThemeResourceTests](../../tests/Electron2D.Tests/ThemeResourceTests.cs) verifies typed values, placeholders, alias subscriptions, variations, merge/copy, guards and concurrency; [ThemeLookupTests](../../tests/Electron2D.Tests/ThemeLookupTests.cs) verifies owner priority, deferred/detached caches, batching, reentry, fallback policy and typed override packing. [PanelContainerTests](../../tests/Electron2D.Tests/PanelContainerTests.cs) verifies defaults, background draw order, content bounds, eligibility, failure continuation and sorting after failed theme callbacks. Resource updates and active lookup pass 64 warmed cycles with zero managed bytes. [ThemePanelRenderingTests](../../tests/Electron2D.Tests/ThemePanelRenderingTests.cs) verifies seven visual phases and 64 warmed notification/layout/recording/render frames with zero managed bytes from ProcessFrameStarted through FramePostDraw on Linux Wayland GPU and compatibility. Native allocator counts, large-GUI performance, nonunit default-icon scaling, other platforms and owner acceptance remain unverified.

[Slider](../classes/Slider.md), [HSlider](../classes/HSlider.md) and [VSlider](../classes/VSlider.md) connect existing Range, typed themes, GUI input and internal processing: complete track/fill/grabber/tick drawing, RTL/vertical placement, drag lifecycle, wheel/direction/Home/End and delayed joypad repeat. Font is not required; native semantic Slider accessibility remains an exact shared-service dependency. Managed SliderTests and eight native pixel/input phases pass on Linux Wayland GPU and compatibility. Sixty-four measured reused-input cycles and active redraw frames each allocate zero managed bytes after their respective warmup, within the bounded scopes documented on the Slider class page. See [ADR 0080](../decisions/rendering.md#adr-0080).

## Font and Label rendering

The [text component](../components/text.md) connects Font/FontFile resources, six CanvasItem text entrypoints and [Label](../classes/Label.md) to ordinary retained glyph textures. FreeType supplies fractional metrics and phase-aware glyph/outline rasters; HarfBuzz shapes contextual runs with full paragraph context. Managed Unicode 17 bidi/grapheme/script/casing algorithms and a private ICU 78.3 word/line backend supply layout, dictionary segmentation, locale tailoring and UTS 39 confusable/spoof checks. TextServer exports independent exact support-database snapshots and flushed files; its embedded-data initialization retains immutable process ownership. See the [security/data contract](../components/text-server.md#unicode-security-and-embedded-support-data). The native font worker owns font operations, while the renderer remains on the scene owner thread. [ADR 0046](../decisions/rendering.md#adr-0046) records the integrated backend and remaining font capabilities.

macOS now selects private FreeType with WOFF2/PNG/zlib decoding and HarfBuzz auto-hinting support. The original target consumer failed WOFF2 loading; its replacement decoder passes locally, while the matching macOS consumer/full-suite run remains an explicit gate in [native delivery](../native-packaging.md).

Label consumes inherited theme fonts or borrowed LabelSettings, performs wrapping, justification, ellipsis, alignment and cluster-bound queries, and records ordered shadow/outline/text effects. Translation, full Unicode uppercase and visible-character policies feed the same cached layout. ClipText intersects an internal logical rectangle with inherited canvas clips and also clips descendants. Font retirement waits for active native reads; recorded glyph-image snapshots remain usable, while unused backend residency is released after retirement.

[FontRenderingTests](../../tests/Electron2D.Tests/FontRenderingTests.cs) and [LabelRenderingTests](../../tests/Electron2D.Tests/LabelRenderingTests.cs) pass focused Linux Wayland GPU/compatibility pixel and lifetime checks. Each test's warmed active-frame scope passes 64 measured frames with zero managed bytes. These results do not measure native allocations or establish broad-scene performance, other-platform text execution or owner acceptance. The private ICU build is integrated for Linux x64/ARM64 profiles; only Linux x64 execution is verified.

The [GUI buttons and shortcuts component](../components/gui-buttons.md) connects the existing theme/text canvas with button actions, groups, texture masks, shortcut resources and tooltip presentation. Its verification section records the measured input-copy allocation boundary and current native gates.

The [texture identity slice](../components/canvas-rendering.md#texture-resource-identities) links borrowed resource RID lifetime to actual server-owned texture creation/update/replacement/free and retained CanvasItem drawing. Owned identities expire with the active renderer; borrowed resources survive. It executes on both native baseline backends with explicit format/platform limits.

[Texture proxies](../components/canvas-rendering.md#texture-proxies) now provide real borrowed-source retargeting and shared native sampling through stable owned aliases. Source/intermediate release leaves reconnectable empty aliases; owned source allocations remain until free/shutdown.

[Wrapping flow layout](../components/canvas-rendering.md#flow-layout) now arranges ordinary controls across rows/columns with relative last-wrap alignment, weighted caps and RTL/reverse. It uses the same theme, deferred lifecycle and backend-neutral retained canvas.

[TextureRect](../classes/TextureRect.md) now provides actual image placement, texture-derived minimums, nested-atlas tiling/reflection, stored scenes and flow-fit stabilization through the common canvas. Its managed and 13-state native GPU/compatibility/dummy checks include 64 active plus 64 idle warmed zero-managed-allocation frames; native allocations and other targets remain unverified.

[SplitContainer](../classes/SplitContainer.md) and fixed H/V variants now supply resizable multi-panel layouts, touch targets and nested two-axis dragging through the existing Container/Control/canvas/theme pipeline. Native Wayland GPU/compatibility input/pixel checks and software layout checks pass, with prepared active/idle frame allocation evidence. Editor highlights, native semantics, other platforms and native allocator totals retain exact limits.

[Typed 2D mesh surfaces](../components/meshes.md) now execute Mesh/ArrayMesh/MeshInstance, CanvasItem.DrawMesh and logical server-owned mesh production on the shared canvas path. The owning [ADR 0092](../decisions/mesh.md#adr-0092) records copied typed channels, packed region updates, material/lifetime rules and exact deferred deformation/channel consumers. Native pixel and warmed frame evidence is separate from other-platform or owner acceptance.

[Repeated mesh resources](../components/meshes.md#repeated-instance-resources) now connect MultiMesh/MultiMeshInstance, DrawMultiMesh and owned server identities to native pose/color/UV/material drawing, visible-prefix Rect2 culling and physics interpolation. Raw instance fragment data executes through the GPU shader interface. Eligible unskinned triangle instances use renderer-owned hardware streams; compatibility, skinning, snapping and compositing retain CPU expansion. [Writable instance buffers](../components/meshes.md#writable-instance-device-buffers) now supply real global RenderingDevice identities, native compute mutation and indexed/nonindexed indirect drawing with per-instance LOD and interpolation. Other-platform and target-hardware acceptance remain separate gates.

[Offscreen canvases](../components/canvas-rendering.md#offscreen-canvas-targets) add SubViewport, ViewportTexture and shared update/clear enums, stable logical RID queries, native dependency ordering and feedback. Multi-view/layered targets and non-root GUI context remain exact separate gates.

On the current Linux Wayland Vulkan profile, remapping a previously presented root surface rejects with NotSupportedException before native/managed visibility changes. Wayland protocol traces show buffer state retained at xdg_surface recreation even after GPU idle, swapchain release and SDL.SyncWindow; trigger: an SDL-owned native presentation-completion/unmap acknowledgement bridge or verified backend/compositor correction. Hiding succeeds, offscreen targets continue, compatibility remapping executes. This is an explicit platform capability gap, not a completed Show path.

[CanvasGroup and BackBufferCopy](../components/canvas-rendering.md#group-composition-and-screen-snapshots) now execute native same-Z group composition and ordered screen snapshots. GPU screen-reading HLSL/GLSL and generated group mipmaps share existing materials; compatibility retains explicit shader/mipmap/software blend gates. [Canvas alpha masks](../components/canvas-rendering.md#canvas-alpha-masks) now execute through CanvasItem.ClipChildren; nested captures, writable screen reads and editor inspector integration retain explicit boundaries.

[Embedded viewport containers and GUI](../components/canvas-rendering.md#embedded-viewport-containers-and-gui) now execute native SubViewportContainer composition, stretch/shrink, independent GUI state and connected input/drag routing. Public input reentry remains rejected; native subwindows, multiview, editor/file workflows and other-platform acceptance remain separate.

## Single-line text fields

[LineEdit](../classes/LineEdit.md) executes themed shaped text editing, scalar limits, BiDi carets/selection, IME, clipboard/history, Unicode-control command dispatch and text drag/drop through existing font and canvas backends. LineEditTests verifies Linux x64 X11 GPU/compatibility pixels and native text input plus 64 warmed caret/selection render-mutation frames with zero managed allocation. Popup/native keyboard/picker integration and other platform/owner acceptance remain separate.

[TabBar](../classes/TabBar.md) now adds real themed tab strips on the existing retained pipeline, with intrinsic/clipped shaped text, logical RTL alignment, icon/button resources, clipped overflow, navigation and drop markers. Current GPU/compatibility pixel and visual checks pass, with 64 warmed active selection/layout/record/render frames at zero measured managed bytes. See [tab strips](../components/tab-strips.md); native accessibility/editor, external allocations and other platforms remain separate.

The [embedded popup window slice](../components/popup-windows.md) composes Window/Popup/PopupPanel targets through the existing GPU/compatibility renderer, routes independent window GUI/input and preserves typed scene/file factories. Independent native child windows and live embedding-policy migration remain explicit dependencies.

[Popup menus](../components/popup-menus.md) add the typed item/submenu consumer atop embedded Window presentation, shaped themed drawing, shared scroll/search controls and scene/file factories. Native/system menu services and dependent MenuButton/OptionButton/TabContainer/context-action consumers retain separate coverage.

[Tab panels](../components/tab-panels.md) combine TabContainer/TabBar selection with actual child Control visibility/layout, typed themes, popup input, group page drags and fresh scene factories. Warm selection/layout/visibility and native render checks pass on current Linux Wayland GPU/compatibility; inherited semantic-service/editor and other-platform acceptance remain separate.

[Dropdown choices](../components/dropdown-choices.md) integrate OptionButton selection, caption/radio state, real menu/search/shortcut input and fresh-process factories through current Button/PopupMenu and text/theme backends. Native keyboard/readback and warm selection checks pass on Linux Wayland GPU/compatibility; inherited semantic/editor/native-popup and broader platform limits remain distinct.

[Embedded acceptance and confirmation dialogs](../components/dialogs.md) compose Window, Label, Button and HBoxContainer with registered LineEdit input. Own public operations, event/hook order, deferred cancel/reopen protection, panel/button themes and fresh scenes execute. Independent native children and inherited accessibility/scaling retain separate prerequisites.

[Command menu buttons](../components/command-menu-buttons.md) reuse Button and PopupMenu text/icon/theme rendering. Current GPU/compatibility SDL interaction/pixel checks and warmed caption/focus/render measurements establish this embedded profile; inherited native services and other-platform acceptance remain separate.

[File dialogs](../components/file-dialogs.md) now execute scoped browsing, five selection modes, typed custom options, filters, menus, overwrite/folder workflows and recoverable desktop Linux trash. The shared FileDialogMode identity spans the custom browser and DisplayServer; native-file-extra and foreign platform gates remain explicit. Six permanent ui_filedialog actions use the existing InputMap settings loader.

[Numeric input](../components/numeric-input.md) adds SpinBox formula/text/arrow/repeat/relative-drag authoring through shared Range and LineEdit, fresh scene factories and generated numeral localization. Current Wayland GPU/compatibility capture/input/pixels and prepared active rendering are exercised; precise pointer warp, inherited semantic/editor and foreign target gates remain separate.

[Color authoring](../components/color-authoring.md) connects spatial/numeric color editing, local swatches, typed palette files, owned popup buttons and completed application-viewport sampling. Native external capture and semantic/foreign-target gates remain separate.

The executable [multiline editing component](../components/multiline-editing.md) connects TextEdit documents, typed syntax resources, existing font/Control rendering, scene storage and input. Its cold/warm and target limits are recorded with the exercised workflow.

[Graph authoring](../components/graph-authoring.md) connects GraphElement, GraphNode, GraphFrame and GraphEdit through current canvas, font, input and scene storage. Typed ports, request-driven wiring, keyboard/remapped commands, nested frames and graph-scaled popup input execute; semantic accessibility, editor authoring and foreign/native allocation gates remain explicit.

[FontVariation](../classes/FontVariation.md) and Font.FindVariation supply independent native font instances: copied axis coordinates, collection faces, synthetic outlines, spacing, baseline and selected/custom palettes. Font metadata exposes validated axis bounds and predefined palettes. RichTextLabel consumes these instances through its full font-tag options. [Text](../components/text.md) records ownership, storage and the local verification boundary.

## Bitmap/indexed font integration

[Bitmap font authoring](../components/bitmap-fonts.md) connects FontFile indexed image/glyph/kerning/metric records and matching configured FontVariation resources to the existing HarfBuzz and common canvas/control path. Copied pixel UV regions preserve clipping and recorded image snapshots; authored publication retires native data after active readers finish. Text/binary v3 import, typed archive/fresh-process restoration and current Linux GPU/compatibility prepared output are exercised. Source policies and other platform/native-allocator gates remain explicit.

## System font integration

[System font matching](../components/system-fonts.md) adds installed families/styles/logical collection faces and owned automatic text fallback over the shared native owner and canvas path. FontFile.AllowSystemFallback defaults to true; explicit resources retain precedence and explicit support queries remain distinct from automatic rendered coverage. Active parent readers retain retired fallback faces through policy changes. SystemFont archives store preferences and rematch the host. The current Linux catalog and both canvas consumers are exercised; CoreText/DirectWrite, extra raster/MSDF, native allocator and foreign acceptance gates remain explicit.

## CPU particle integration

[CPUParticles](../classes/CPUParticles.md) and [CPU particles](../components/cpu-particles.md) connect ordinary scene internal processing, scalar curves/gradients, borrowed textures/materials, typed file graphs and shared canvas output. Configured CPU simulation and sprite-sheet replay execute on current GPU/compatibility. World emission follows complete physics poses and uses a separate canvas basis for visible world quads. The [GPU particle server](../components/gpu-particles.md) executes compute simulation and canvas output; built-in process materials now execute, and GPUParticles now connects scene emission and CPU conversion. Foreign/native allocator and owner acceptance remain separate gates.

## Executable viewport worlds

[The world contract](../components/worlds.md) combines canvas and physics ownership, default/shared/explicit viewport binding, transition notification and real native pixel verification. NavigationMap retains its separate navigation-backend trigger.

## Local application compute

[Local compute](../components/local-compute.md) now supplies the applicable RenderingDevice buffer/compute-list API, independent of canvas selection. Ordered buffer copies/clears and GPU-produced indirect compute commands execute through the same device, with validated ranges and roles. Queued readbacks capture request-time data and deliver owner-thread callbacks at local completion, with caller-memory reuse, FIFO/reentrancy, failure and cancellation rules. Runtime-owned native handles stay internal; consumers load compiled modules through RDShaderSPIRV and bind typed RDUniform descriptors. The water playground executes the same application liquid model on CPU or through this device API. Local color images, mip chains, sampled arrays, samplers and writable image descriptors now execute through the same device, including queued per-layer snapshots, raw region copies and selected mip/layer clears. Device graphics, native texture views, multisample/depth lifetimes and canvas sharing remain separate gaps.

[Menu strips](../components/menu-strips.md) add shaped translated RTL headers, all ordinary/mirrored state styles and flat/focus text behavior to current embedded PopupMenu presentation and typed theme lookup.

[Canvas lights and occlusion](../components/canvas-lights.md) integrate scene/retained sources, multichannel textures, shadow contours and tile distance-field policies. Compatibility and programmable-material limitations remain explicit.

## Native device image consumers

The active GPU renderer now retains a RenderingDevice and directly samples its ordinary/array images through TextureRD, TextureLayeredRD and TextureArrayRD. Native allocations stay on the device timeline; canvas frames require no CPU snapshot/upload. Borrowed/owned canvas wrapper lifetimes, role validation, transient archive bindings, readback conversion, alias replacement and automatic global submission are described in [local compute](../components/local-compute.md#device-images-in-canvas).

Integer device image consumers now validate float/signed/unsigned sampled scalar types and array roles, migrate reload bindings and use nearest native integer sampling. Cold snapshots expose raw-valued float32 RGBA; byte device readback preserves all integer bits. See [the integer canvas checkpoint](../components/local-compute.md#integer-canvas-sampling-checkpoint) for executable evidence and the connected scalar-aware lighting and alpha-mask behavior.

[Shader material lighting](../components/shader-materials.md#automatic-scene-lighting) now composes user fragment colors with the existing light/contour consumers, preserving original parameters, discard, returns, instancing and blend order. Capacity overflow fails explicitly.

[Per-item shader parameters](../components/shader-materials.md#per-item-shader-parameters) now provide independent typed overrides over shared materials, including scene/server, inherited/surface mesh, hardware-instance and group consumers. Layout/type validation, source boolean metadata and scene archives use the same compiled profile.


## Device color graphics

Ordered framebuffer passes now execute color/depth/resolve outputs, readonly single/multisample input attachments and preservation through inactive intermediate passes. Pipelines select their pass; transitions reset bindings while retaining list identity. The [ordered pass contract](../components/local-compute.md#ordered-framebuffer-passes-and-inputs) records clear ordering, GPU snapshots for sampled inputs, exact integer loads, native pixel checks and remaining multiview prerequisites.

The [local device component](../components/local-compute.md#color-graphics-and-compute-integration) executes paired vertex/fragment shaders, borrowed vertex/index arrays and color/depth/stencil framebuffer passes with exact MSAA and color resolve, including procedural/instanced/indexed/indirect draws. Compute-generated geometry and commands remain on the GPU timeline. Retained device textures feed ordinary canvas consumers. Depth bounds, independent visible-face stencil masks/reference, raw aspect transfers/depth resolve, multiview and additional shader interfaces retain explicit prerequisites in coverage.

The local and retained device pipelines now accept compiled push blocks and copied per-draw/per-dispatch binary parameters. Original compact 32-bit layouts, paired graphics stages and public uniform bindings remain coherent; [the push contract](../components/local-compute.md#binary-push-parameters) defines the portable 128-byte capacity, initialization, native uniform quota and verified limits.

Compute and paired graphics pipeline factories now accept copied typed specialization values. One shader identity yields independent native variants with resolved workgroups and uniform/push array extents, preserving public uniform sets and retained texture sharing. [The specialization contract](../components/local-compute.md#typed-pipeline-specialization) defines the typed family, validation, lifetime and verified limits.

Explicit [device source authoring](../components/local-compute.md#source-authoring-and-build-imports) now compiles typed GLSL/HLSL stage descriptions through the separately installed import tool, then executes the common compute/graphics pipeline path. Per-stage diagnostics, copied artifacts, cache bypass and changed includes are integrated; shipped games require only compiled modules.

The [secondary mesh UV channel](../components/meshes.md#secondary-uv-storage-and-shader-consumer) now executes typed storage, immediate construction, packed updates, archives and GPU shader inputs across ordinary, instanced and skinned meshes. Compatibility preserves primary drawing with explicit programmable-material rejection.

## Typed surface authoring

[SurfaceTool](../classes/SurfaceTool.md) now supplies finite typed 2D vertex/fan construction, four/eight skin selection, copied source/array imports, transformed append, exact indexing/deindexing and actual index-cache reordering. Commit atomically publishes copied geometry and borrowed material into ArrayMesh. Completed output executes existing canvas and indexed device rendering; saved output loads and redraws in a fresh process. Advanced custom/morph prerequisites remain precise coverage gaps. Cold construction may allocate; warmed retained and device playback measures zero managed bytes on Linux Wayland. Physical cache speed, native allocation totals and wider platforms remain unverified.

## Copied triangle topology editing

[MeshDataTool](../classes/MeshDataTool.md) now edits copied 2D triangle vertices and current supported channels with explicit vertex-edge-face connectivity, exact typed transient annotations and atomic copied geometry/material commit. Boundary, degenerate, nonmanifold and eight-influence data execute on the existing consumers. Fresh-process output and native indexed/retained pixels are checked. Native allocation totals, wider platforms remain open.

Numerical mesh normals/tangents now execute through [shared authoring, exact packed storage, archives and real shader consumers](../components/meshes.md#numerical-normals-and-tangents). SurfaceTool generates smooth/flat planar normals and MikkTSpace tangents; ArrayMesh regeneration is transactional, and MeshDataTool edits all applicable vertex channels. Optional GPU fragment inputs at locations five/six receive mesh-local values independently of UV2 and instance data. Compatibility retains its ordinary rendering policy. Native device/retained/fresh-process/reference-parity and warmed managed-allocation checks pass on the current Linux/Wayland gate.

Four numerical custom mesh channels now execute through [typed packed storage, transactional authoring/updates, AMS4 archives and real shader consumers](../components/meshes.md#numerical-custom-channels). Eight independent layouts reach ordinary, hardware-instanced and skinned rendering; Linux GLSL/HLSL pixels and warmed complete frames are checked.

[Named mesh targets and independent instance deformation](../components/meshes.md#named-targets-and-instance-deformation) now connect typed authoring, raw pose extraction, stored scene weights and actual pre-skinning 2D drawing. Normalized/relative equations, callback boundaries, AMS1–AMS5 storage and Linux backend pixels are verified; complete native allocations and wider platforms remain open.

Authored mesh LODs now execute from copied threshold/index storage through full framebuffer scale selection in the retained canvas. Each MultiMesh presentation instance chooses independently; morph output, skin, group drawing, live packed updates, resource duplication and AMS6/fresh scene files preserve ordinary channels and ownership. The one-pixel policy uses the greatest singular value so anisotropy and shear retain detail. Base arrays/faces/bounds keep full authored geometry; automatic simplification and attribute compression remain their exact separate integrations. See [authored LOD indices](../components/meshes.md#authored-lod-indices-and-scale-selection).

[Named compiled shader versions](../components/local-compute.md#named-compiled-shader-versions) now provide borrowed RDShaderSPIRV selection, Unicode-sorted copied names and independent import diagnostics. Resource graphs preserve aliases and diagnostics through saved files and duplication; selected loaded versions execute existing compute/graphics factories without runtime source compilation.


Compressed mesh attribute import now executes through copied canonical UV/frame storage, decoded retained drawing, partial updates, morph/LOD/skin/instance/group integration and AMS8 saved scenes. [The mesh component](../components/meshes.md#compressed-attributes) records exact strides, precision and Linux pixel/zero-managed-allocation evidence. GPU vertex uploads keep the existing canvas layout. Procedural shader-generated empty-position geometry executes with independent counts and native topology submission.


[ImporterMesh](../classes/ImporterMesh.md) now turns raw resource/scene authoring into drawable ArrayMesh snapshots and actual automatic LODs. The cold native simplifier is independent of SDL/GPU initialization; ordinary render replay retains existing allocation contracts. [The component](../components/meshes.md#importer-mesh) records raw precision, resource graph/storage, transformed merge and tested native/package boundaries.

Renderer-managed texture uploads, image arrays and completed viewport targets now export borrowed retained-device identities for native read/copy/update/compute consumers. Allocation expiry and queued read lifetime execute on the shared renderer timeline; see [renderer texture export](../components/local-compute.md#renderer-texture-export-checkpoint). Distinct sRGB views remain a native format-view dependency.

The [explicit-frame component](../components/manual-render-frames.md) adds owner-thread manual native drawing, real GPU synchronization, one-shot completion actions and Engine frame accounting on the existing retained canvas path.

Server-owned [offscreen viewport targets](../components/canvas-rendering.md#server-owned-offscreen-viewport-targets) now execute canvas output through static RID APIs without attaching scene nodes, sharing native image ownership, dependency submission and explicit texture readback. Single-layer output is implemented; layered targets remain a precise separate dependency.

Retained canvas-item visibility callbacks aggregate culling samples across all submitted viewports, commit once after backend frame completion and run before FramePostDraw. Callback errors do not prevent later transitions/readbacks/completion; disable/free/disposal clear ownership silently. See [canvas rendering](../components/canvas-rendering.md).

The primary realized face supplies OpenType metadata through `GetSupportedFeatureList()` and `GetOTNameStrings()`. The feature snapshot contains numeric tags and typed optional labels, boolean/integer selection and hidden flags. GSUB is read before GPOS, whose duplicate descriptions win. Name strings are copied by native language spelling and standard role; unknown IDs retain `unknown_N` keys. These cold snapshots allocate independently and omit fallback metadata or authored family overrides. Feature labels retain the tool locale sampled during native realization; source replacement or cache retirement rebuilds metadata.

[ShaderInclude](../classes/ShaderInclude.md) authors and persists reusable HLSL/GLSL source consumed by the existing build importer; runtime source edits require save/rebuild and explicit compiled Shader replacement.

[ShaderIncludeDB](../classes/ShaderIncludeDB.md) provides static discovery of the embedded portable color fragments consumed by both the project importer and built-in canvas color processing. Its source is available before renderer startup; consuming shaders retain normal backend requirements.

Retained canvas items now support local finite repetition through CanvasSetItemRepeat, sharing one state with direct-child CanvasSetItemMirroring. Arbitrary item subtrees repeat on both backends; nested sources override inherited repetitions, and viewport culling avoids expanding distant ordinary copies. See the [canvas component](../components/canvas-rendering.md#retained-item-repetition) for state lifetime and verification limits.

Retained canvas-group configuration now executes through RenderingServer.CanvasItemSetCanvasGroupMode for arbitrary live items, with the shared masking/transparent composition path. See [retained configuration](../components/canvas-rendering.md#retained-canvas-group-configuration) for fitting, margins, mipmaps, source republishing, lifetime and shared-backbuffer limits.

## Viewport canvas configuration

`RenderingServer.ViewportSetCanvasCullMask`, `ViewportSetGlobalCanvasTransform`, `ViewportSetDefaultCanvasItemTextureFilter` and `ViewportSetDefaultCanvasItemTextureRepeat` configure the next submitted image of a live scene or caller-owned viewport. One shared canvas can therefore render with independent transforms, visibility masks and samplers in each attached viewport. The outer matrix applies after the canvas attachment matrix and before window stretch/pixel density, including default scene canvases and CanvasLayer canvases. Finite singular matrices are valid; zero masks hide all canvas items and bit 31 remains usable.

Renderer-session overrides preserve authored properties and input coordinates. Matching scene setters republish, including equal sampling values. Invalid sampler/matrix/identity values reject; unsupported native sampling retains explicit backend gates. See [canvas configuration behavior and verification](../components/canvas-rendering.md#viewport-canvas-configuration).

## Linear HDR canvas output

Stored Viewport.UseHDR2D, a static RenderingServer override and the typed root project setting select actual linear half-float targets. The same format flows through composition, MSAA, device exports and explicit readback; byte color sampling converts before filtering and native writes remain visible. Root presentation and movie output apply the SDR transfer. [HDR canvas targets](../components/hdr-canvas.md) records role-specific colors, verified pixels, warm allocation intervals and backend/platform limits.

## Canvas multisample antialiasing

The shared Viewport.MSAA enum, stored MSAA2D scene property, static server selection and typed root project setting execute 2/4/8-sample GPU geometry coverage. Per-sample history survives Never-clear frames and pass restarts; screen/group/clip consumers read resolved pixels. Compatibility rejects enabled MSAA. [Canvas rendering](../components/canvas-rendering.md#canvas-multisample-antialiasing) records target ownership, typed identity, native coverage and warmed allocation evidence.

Retained canvas item interpolation now executes through scene physics boundaries, native hierarchy composition, typed reset/rebase and source policy publication. Shared history logic also preserves canonical light/occluder rebases. See [canvas interpolation](../components/canvas-rendering.md#retained-canvas-item-interpolation) for pixel, lifetime and warmed allocation evidence.

Retained canvas items can copy screen snapshots independently of scene node type through the static server API. [Copy settings](../components/canvas-rendering.md#retained-backbuffer-copy-settings) documents source publication, backend execution and verification limits.

## Native font distance generation

[MSDF font generation](../components/text.md#native-msdf-font-generation) completes six FontFile/SystemFont source settings through the serialized native outline owner, canonical indexed image cache and common GPU text path. Current Linux Wayland pixel, source-oracle, managed lifetime and prepared allocation checks are exercised; Compatibility rejects actual encoded submissions. Native package ABI/source/license checks accompany the closure; old native bundles, foreign execution and native allocator totals remain separate.

Type 1 PFB/PFA outlines and paired PFM/AFM native metrics now execute through FontFile, FontVariation, Linux installed/automatic matching, existing text measurement and ordinary/LCD/MSDF canvas consumers. [Source rules and verification](../components/text.md#type-1-outline-and-auxiliary-metric-sources) distinguish byte storage, native images, actual hosts and foreign-platform limits.

[BC/DDS images](../components/image-bcn.md) now execute S3TC/BPTC CPU authoring, exact DDS/raw/full-mip source archives, native BC1–BC7 GPU textures/arrays and decoded Compatibility sampling. Imported RenderingDevice texture descriptors use the actual native BC format, with exact block readback and existing ownership. Compatibility BC6 retains its driver-dependent HDR precision gate; the exercised driver rejects it. [ETC/EAC/ASTC decoding](../components/image-block-decoding.md) now supplies prepared ordinary/array sampling, preserving signed/HDR precision. UInt16 sources execute raw unsigned GPU sampling; foreign-platform/native allocator acceptance remains separate.

## Themed numeric progress

[ProgressBar](../classes/ProgressBar.md) connects shared Range state with actual background/fill styles, centered localized percentage text and outline, four physical/RTL directions and clipped indeterminate animation. The existing EditedSceneRoot selection scopes static or animated authoring preview. Exact scene factories restore settings in a fresh process. GPU/Compatibility pixel masks and 64 prepared active frames verify current Linux rendering and zero managed owner-thread allocation; authored Theme archive, native semantics and foreign platforms retain their separate dependencies. See [ADR 0080](../decisions/rendering.md#adr-0080).

## Project themes and portable authoring

Theme resource archives and scenes now persist complete typed theme data, resource aliases and structural groups/variations. ThemeDB.GetProjectTheme returns the real resource prepared at startup from typed GUIThemeCustom/GUIThemeCustomFont settings. Branch overrides precede project items; built-in defaults remain behind them. Headless startup and fresh-process authoring checks are separate from GPU/Compatibility rendered verification. Project file failure preserves the prior selection, while successful replacement retires owned project snapshots. See ADR 0083 and the Theme/ThemeDB class pages.

[CanvasModulate](../classes/CanvasModulate.md) now applies scene-authored tint to a complete logical canvas, with visible-node selection, weak registration, layer/World isolation, fresh scene storage and current GPU/compatibility consumers. See [scene canvas modulation](../components/canvas-rendering.md#scene-canvas-modulation).

[FoldableContainer](../classes/FoldableContainer.md) and [FoldableGroup](../classes/FoldableGroup.md) implement collapsible title/body panels, mutually exclusive expansion, shared BarPosition, owner-safe input/layout/themes and typed fresh scene/title-control storage. FoldableTests/FoldableNativeTests cover the connected workflow under ADR 0081/0083.

## Touch game consumers

[Touch game controls](../components/touch-game-controls.md) connects TouchScreenButton scene-space masks/textures with VirtualJoystick themed analog gestures. Independent action contributions, queued button event delivery, lifecycle cancellation, typed fresh-process scenes and Linux rendered input checks are documented in the component. Physical mobile, foreign backend, inherited semantic accessibility and native allocation acceptance remain separate.

[LinkButton](../classes/LinkButton.md) adds executable text links through the existing font/theme/canvas and BaseButton input paths. Typed BiDi, scalar ellipsis, state underline and scene-file authoring are integrated; URI activation delegates the existing OS opener. Its exact inherited accessibility-focus value does not claim an accessibility service. See GUI buttons for actual rendered/allocation checks and remaining host gates.

[GUI backgrounds and separators](../components/gui-primitives.md) add solid ColorRect fills and the abstract Separator/HSeparator/VSeparator theme family. They use the existing Control/canvas and scene-file paths; integer placement, live resource edits and input policy retain their documented contracts. Editor reference frames, SVG DPI textures and public text-object builders keep explicit separate prerequisites.

[TextLine](../classes/TextLine.md) is a retained standalone typed line with borrowed font spans/metadata and inline tokens, full-buffer caret/object geometry and a separately trimmed display. Its public drawing uses CanvasItem and existing native glyph kernels; [text objects](../components/text-objects.md) and [ADR 0107](../decisions/text-objects.md#adr-0107) describe ownership and separate gates.

[TextParagraph](../classes/TextParagraph.md) extends standalone retained text to complete wrapped paragraphs, independent dropcap and indexed line buffers, sharing typed spans, native fonts and canvas drawing with TextLine. Visibility retains hidden line queries/drawing; reflow retires line identities. See [text objects](../components/text-objects.md) for ownership, signed metrics and verification limits.


[Scalable SVG textures](../components/scalable-svg.md) integrate DPITexture paint maps, native image processing, shared viewport/font oversampling, retained scale identities, default theme icons and typed resource/scene storage. Native Linux warmed replay and foreign-host acceptance remain separately measured gates.

[Video playback](../components/video-playback.md) publishes stable ImageTexture frames through both real canvas backends, including a second texture consumer. Revision-aware uploads retain prepared storage; duplicated/saved images remain independent snapshots.

[UInt16 image texture sources](../components/integer-textures.md) now retain all four original layouts and mip chains, use raw unsigned RGBA16 GPU storage, validate ordinary/layered sampler scalars and reject unsupported integer drawing on Compatibility.

[Viewport screen attachment](../components/canvas-rendering.md#viewport-screen-attachment) now presents scene or caller-owned targets directly in the main native window, with one final GPU/Compatibility composition, clipping, update scheduling and HDR/MSAA source handling. Additional native-window identities depend on the native multiwindow ownership/routing slice.

## Authored canvas vertices

Paired compiled Shader stages now execute vertex deformation through existing CanvasItem, Mesh/MultiMesh, glyph, lighting and viewport GPU consumers. Typed material/instance/default-texture state merges across stages while native bindings remain stage-specific. Engine-owned wrappers preserve hardware instancing, canvas modulation and private glyph/light data. The [shader ABI](../components/shader-materials.md#authored-vertex-stages) records framebuffer coordinates, exact capacities, retained-bounds limitations, storage and Linux native evidence. Compatibility continues its explicit shader rejection; foreign platforms remain unverified.

Scene-authored global shader values now use [ShaderGlobalsOverride](../classes/ShaderGlobalsOverride.md). Its typed portable values, tree-order arbitration and renderer-session restoration are described in the [global parameter component](../components/global-shader-parameters.md#scene-owned-overrides).

Window placement and ResetSize use the existing viewport/native resize lifecycle. Embedded centering remains independent of unavailable Wayland global positions; native root placement is subject to compositor constraints. See [the window runtime](../components/window-runtime.md#initial-placement-and-constrained-reset).

## Rendering changes and low-processor scheduling

[Low-processor rendering](../components/low-processor-rendering.md) connects retained change producers, static RenderingServer.HasChanged, the two OS policies and typed startup settings to Engine.Run/RunAsync. Static output skips submissions while processing and input continue; time-driven drawing and capture retain their executing consumers. Verification limits remain explicit in the component.

[Live index-buffer edits](../components/meshes.md#live-index-buffer-updates) now let caller-owned meshes change topology through partial 16/32-bit packed updates. Typed channel-offset/index-stride queries share the existing storage contract; retained mesh and repeated-instance drawing observe edited indices while preserving surface counts, RID, material and independent LOD storage. Current evidence separates Linux/Wayland software rendering and prepared managed allocations from native allocation totals and foreign/hardware acceptance.

[Packed mesh surfaces](../components/meshes.md#packed-server-surfaces) now connect typed binary descriptions to owned RenderingServer creation/append and copied export from ordinary/custom resources. Position/direction/custom/skin/morph/LOD/material data and authored visibility bounds reach the existing renderer. Real software GPU/GLSL, GPU/HLSL and Compatibility profiles verify output and prepared allocation limits; procedural position-free formats execute through the authored vertex producer with explicit visibility bounds.

[Manual mesh visibility bounds](../components/meshes.md#manual-mesh-visibility-bounds) connect stored ArrayMesh policy and static server access to fitted groups, morph output and both instance drawing paths, independently of source geometry bounds.

[Logical transform observation](../components/scene-hierarchy.md#logical-transform-observation-from-processing-groups) now permits active groups to read local/global Entity and Control chains, including inherited physical nodes. Shared dirty matrices publish under the retained canvas-item gate after virtual getters return; notification coalescing, ordinary-worker rejection and owner mutation guards remain distinct. Public parallel ordinary/pool-host physics consumers verify the bounded read contract. General worker mutation remains Partial.

[Procedural mesh surfaces](../components/meshes.md#procedural-surfaces-without-positions) connect independent typed/packed counts and absent position buffers to authored vertex programs, native topology, full affine skin/instances, masks and fitted groups. CPU geometry operations retain their explicit missing-position boundary; Compatibility rejects programmable output.

## Fixed sampled descriptor arrays

[Canvas sampled descriptor arrays](../components/shader-materials.md#fixed-sampled-descriptor-arrays) now execute through GLSL/HLSL, typed resource and static RID access, indexed shader defaults, complete validation, graph storage and derived scalar native bindings. Paired stages and existing lighting/glyph/instance/HDR/MSAA consumers share the same physical capacity and resource lifetime. See the component for measured software-Vulkan checks and the separate hardware/platform gates.

[Writable textures](../components/drawable-textures.md) connect DrawableTexture, BlitMaterial, the TextureBlit shader domain and static RenderingServer drawable operations to ordered GPU passes and ordinary native sampling.

[Sparse framebuffer outputs](../components/local-compute.md#sparse-framebuffer-outputs) now preserve logical output/blend/resolve slots while packing native targets and private fragment output locations. Exact shader masks, typed MRT, MSAA input/resolve, depth-only holes and retained canvas consumers execute without dummy images or a runtime source compiler.

[GUI control pixel snapping](../components/canvas-rendering.md#gui-control-pixel-snapping) is independent of canvas transform/vertex snapping. The viewport-owned flag acts on eligible Control local visual translations before visual-only offsets; Entity/layer/canvas matrices keep their separate stages. Both current renderers consume retained commands without additional redraw callbacks.

Ordinary desktop Window children now execute [independent native hosts](../components/window-runtime.md#independent-native-windows), sharing one display/event service and scene renderer. GPU device targets are shared; compatibility contexts replay frozen commands with cold history transfer. Native popup surfaces and platform-specific decoration controls retain separate prerequisites.

## Low-level shaped text

[TextServer buffers](../components/text-server.md) connect retained provider discovery, owned buffer creation/release and borrowed TextLine/TextParagraph identities to typed authoring, native shaping, ceiled dimensions, fractional baseline metrics and baseline canvas drawing. Paragraph line edits detach atomically and retire at reflow. The same FreeType/HarfBuzz and canvas owners remain in use; raw font-cache and extension families retain separate coverage prerequisites.

## Shaped-buffer editing

[TextServer editing](../components/text-server.md#caret-selection-and-navigation) supplies typed baseline-relative BIDI caret shapes, logical selections, Unicode character boundaries and native shaping-cluster navigation. Existing canvas primitives consume the returned geometry on both renderers; paragraph line views retain global scalar origins and line-local geometry. TextCaretInfo owns no native resources. Snapshot arrays remain cold; prepared value queries retain the existing owner/generation/reentry checks.

[Shaped-buffer wrapping and substrings](../components/text-server.md#line-wrapping-and-owned-substrings) expose variable-width multiline flow and independently owned cluster-preserving draws, including shared source leases, global scalar offsets, BIDI/inline context and explicit parent retirement.

[Shaped-buffer formatting](../components/text-server.md#fitting-spacing-and-overrun) connects ten TextServer methods and TextOverrunFlags to actual fitting, signed spacing, punctuation and independent trim/ellipsis display. Owned/borrowed lines share real geometry, rendering and transactional font reads; raw editing ranges remain complete. Verification and remaining platform/allocation/owner limits are recorded with the component.

[TextServer Unicode utilities](../components/text-server.md#standalone-unicode-utilities) now turn locale-sensitive case, normalization and word ranges into actual shaped text and canvas drawing. They share pinned Unicode data and existing uppercase/font tag helpers. Identifier properties include the required Indic layout data; scalar boundary snapshots remain distinct from shaped-cluster wrapping. Linux software parent/fresh-process rendering and independent normalization/data/package checks retain their separate hardware, native allocator, foreign-platform and package-consumer limits.

[Span font replacement and shaped words](../components/text-server.md#span-font-replacement-and-shaped-word-ranges) preserve source/metadata while replacing actual native font, size and feature dependencies. Retained buffer word/Kashida opportunities are prepared during coherent shaping, and typed cluster properties also reach rich callbacks. Managed and native/fresh-process evidence retain their separate allocation, package, platform and owner limits.

[Font-free hexadecimal replacements](../components/text-server.md#font-free-hexadecimal-replacement) expose shared missing-glyph measurement and baseline rectangle recording directly through TextServer. They use ordinary CanvasItem transforms, colors and clipping without a Font source; two/four/six-digit boundaries, live rendering and prepared managed allocation scopes have dedicated checks.

[Owned TextServer fonts](../components/text-server.md#owned-font-sources-and-caches) connect stable font creation/free, linked shared caches and independent spacing/baseline to existing typed Font consumers and real native glyph/outline drawing. Typed cache and Bézier contour snapshots use actual backend data; ordinary-resource glyph identity and external provider dispatch retain separate coverage obligations.

## Actual text source metadata

The text service exposes borrowed resource source caches and complete copied glyph/run metadata through [TextGlyph](../classes/TextGlyph.md) and [the source contract](../components/text-server.md#resource-sources-glyph-snapshots-and-visual-runs). Custom canvas consumers can use actual font source/index/size/offset/advance without native pointers. Resource data edits, linked state, automatic fallback retirement and immutable recorded pixels retain their existing owners. External provider dispatch and platform/package/hardware/allocator/owner acceptance remain separate gates.

[Independent device sampled bindings](../components/local-compute.md#independent-sampled-resource-bindings) connect reusable Texture/Sampler resources across logical sets and shader stages, including real compute, layered graphics and retained canvas consumers.

[Item scale oversampling](../components/scalable-svg.md#item-scale-oversampling) connects the stored CanvasItem policy, inherited by spatial/UI/physics descendants, to native font and SVG raster factors and shared last-owner cache retirement. Logical dimensions and explicit font/MSDF policies remain intact.
