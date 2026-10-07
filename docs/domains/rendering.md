# Rendering domain

Last updated: 2026-10-07

Private native binaries come from versioned Linux/macOS packages in ordinary desktop builds. Only `Electron2DBuildNativeFromSource=true` invokes native compilers. [Native delivery](../native-packaging.md) separates audited packages, executable consumer checks and public publication; macOS runtime integration verification is pending.

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
| [Typed themes](../components/themes.md) | [ThemeDB](../classes/ThemeDB.md), Control/Window owner lookup and overrides, [Panel](../classes/Panel.md), [PanelContainer](../classes/PanelContainer.md), box/grid constants and Label fonts/effects | Six executable typed data categories and current GUI consumers; project-theme/default-catalog gaps remain explicit |
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

[Typed themes](../components/themes.md) connect six categories—Color, Constant, Font, FontSize, Icon and StyleBox—to Control/Window owner traversal, variation/native-type order, local overrides and change delivery. Panel drawing, PanelContainer content layout, box/grid gaps and Label fonts/effects consume the resolved values. ThemeDB supplies a shared embedded Open Sans SemiBold fallback whose native font loads on first text use. [ADR 0083](../decisions/rendering.md#adr-0083) keeps project Theme loading and defaults for remaining GUI families separate. [ThemeResourceTests](../../tests/Electron2D.Tests/ThemeResourceTests.cs) verifies typed values, placeholders, alias subscriptions, variations, merge/copy, guards and concurrency; [ThemeLookupTests](../../tests/Electron2D.Tests/ThemeLookupTests.cs) verifies owner priority, deferred/detached caches, batching, reentry, fallback policy and typed override packing. [PanelContainerTests](../../tests/Electron2D.Tests/PanelContainerTests.cs) verifies defaults, background draw order, content bounds, eligibility, failure continuation and sorting after failed theme callbacks. Resource updates and active lookup pass 64 warmed cycles with zero managed bytes. [ThemePanelRenderingTests](../../tests/Electron2D.Tests/ThemePanelRenderingTests.cs) verifies seven visual phases and 64 warmed notification/layout/recording/render frames with zero managed bytes from ProcessFrameStarted through FramePostDraw on Linux Wayland GPU and compatibility. Native allocator counts, large-GUI performance, nonunit default-icon scaling, other platforms and owner acceptance remain unverified.

[Slider](../classes/Slider.md), [HSlider](../classes/HSlider.md) and [VSlider](../classes/VSlider.md) connect existing Range, typed themes, GUI input and internal processing: complete track/fill/grabber/tick drawing, RTL/vertical placement, drag lifecycle, wheel/direction/Home/End and delayed joypad repeat. Font is not required; native semantic Slider accessibility remains an exact shared-service dependency. Managed SliderTests and eight native pixel/input phases pass on Linux Wayland GPU and compatibility. Sixty-four measured reused-input cycles and active redraw frames each allocate zero managed bytes after their respective warmup, within the bounded scopes documented on the Slider class page. See [ADR 0080](../decisions/rendering.md#adr-0080).

## Font and Label rendering

The [text component](../components/text.md) connects Font/FontFile resources, six CanvasItem text entrypoints and [Label](../classes/Label.md) to ordinary retained glyph textures. FreeType supplies fractional metrics and phase-aware glyph/outline rasters; HarfBuzz shapes contextual runs with full paragraph context. Managed Unicode 17 bidi/grapheme/script/casing algorithms and a private ICU 78.3 word/line backend supply layout, dictionary segmentation and locale tailoring. The native font worker owns font operations, while the renderer remains on the scene owner thread. [ADR 0046](../decisions/rendering.md#adr-0046) records the integrated backend and remaining font capabilities.

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

[Repeated mesh resources](../components/meshes.md#repeated-instance-resources) now connect MultiMesh/MultiMeshInstance, DrawMultiMesh and owned server identities to native pose/color/UV/material drawing, visible-prefix Rect2 culling and physics interpolation. Raw instance fragment data executes through the GPU shader interface. CPU expansion and compatible batching are verified; hardware instance buffers/indirect commands and other-platform acceptance remain dependencies.

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

[CPUParticles](../classes/CPUParticles.md) and [CPU particles](../components/cpu-particles.md) connect ordinary scene internal processing, scalar curves/gradients, borrowed textures/materials, typed file graphs and shared canvas output. Configured CPU simulation and sprite-sheet replay execute on current GPU/compatibility. World emission follows complete physics poses and uses a separate canvas basis for visible world quads. GPU compute/process materials, their conversion, foreign/native allocator and owner acceptance remain separate exact dependencies.
