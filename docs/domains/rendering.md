# Rendering domain

Last updated: 2026-09-23

## Responsibility

Rendering turns retained scene commands and typed resources into frames for the active root Window. Runtime source is in `src/Servers/Rendering/`; it compiles into Electron2D.dll. SDL3-CS and owned SDL handles remain internal. DisplayServer exposes supported borrowed operating-system context identities under ADR 0042.

## Components and public surface

| Component | Public types and integration | State |
| --- | --- | --- |
| [Canvas rendering](../components/canvas-rendering.md) | [RenderingServer](../classes/RenderingServer.md), CanvasItem drawing, [Sprite](../classes/Sprite.md) and [AnimatedSprite](../classes/AnimatedSprite.md) nodes and Texture drawing | Executable rectangle/line/stroke/curve/polygon/primitive/texture path; full API incomplete |
| [Shader materials](../components/shader-materials.md) | Shader, ShaderMaterial, Material, Texture, ImageTexture and AtlasTexture, owned by Resources | Executable HLSL/GLSL import, typed uniforms and sampled textures; broader language profile incomplete |

Games use Sprite for texture, sheet-frame and region drawing, AnimatedSprite with SpriteFrames for timed playback, or record custom commands from CanvasItem.OnDraw through CanvasItem and Texture. RenderingServer provides the active method/driver, frame events and clear/submission controls. Engine.Run starts and closes the renderer. CanvasItem and Viewport supply filtering/repeat policies and GPU mip/anisotropy settings, with explicit fallback limits. CanvasItem.ItemRectChanged reports local geometry changes synchronously; Sprite integrates the event with its setters while resource worker notifications remain atomic redraw requests.

AnimatedSprite uses the existing internal idle lane and canvas path. Its [timing audit](../classes/AnimatedSprite.md#timing-contract-and-source-audit) records duration-transition and ping-pong behavior. Native readback covers real timed completion, texture/atlas/blank frames and callback-failure cleanup.

## Dependencies and invariants

- Uses Scene for Window, tree membership, transforms, visibility, behind-parent and nested local Y ordering, with stable Z precedence.
- Uses Resources for copied image snapshots and typed materials; consumers borrow these resources.
- GPU is primary; compatibility is explicit startup fallback and rejects shaders.
- Frame submission and native resource lifetimes belong to the scene owner thread.
- HLSL and GLSL share the SPIR-V interface. Import/build compiles source; runtime consumes bytecode.
- Invalid resources or unsupported backend features fail explicitly. SDL types and owned graphics handles remain internal; DisplayServer may expose documented borrowed OS/context identities.

## Verification and limits

Current native verification covers Linux Wayland GPU/Vulkan and compatibility, plus the software renderer under the dummy video driver. Pixel checks cover ordinary drawing, both shader languages, canvas ordering and lifecycle-driven recording through notifications/events/overrides; other target backends, user visual acceptance, whole-frame performance, lights, clipping, meshes, GUI, offscreen/multiwindow rendering and device recovery remain unfinished. See the component pages for exact checks and limits.

[ADR 0028](../decisions/rendering.md#adr-0028) owns backend/shader decisions, [ADR 0004](../decisions/product.md#adr-0004) owns the 2D product boundary, and [ADR 0021](../decisions/product.md#adr-0021) owns the current platform gate.

Pixel-snapping integration is described by [the canvas component](../components/canvas-rendering.md#pixel-snapping). Viewport owns independent transform/vertex policies; rendering preserves logical node transforms, while Sprite local queries honor attached transform snapping. Project defaults initialize the explicit root Window at construction.

[Generated gradient textures](../components/gradients.md) feed the existing canvas and material paths. LDR live updates execute on GPU and compatibility; HDR preservation is verified on GPU and explicitly rejected by tested compatibility drivers lacking support.

Root viewport canvas/final transforms are connected to retained rendering, scene input localization and CanvasItem coordinate/pointer queries. Logical node transforms stay unchanged. [Canvas coordinate integration](../components/canvas-rendering.md#viewport-coordinates) records ownership, singular/overflow behavior, runtime-only properties and Linux Wayland/dummy verification. Camera and CanvasLayer are integrated; content scaling and nested/offscreen viewports remain absent.

[Camera tracking](../components/canvas-rendering.md#camera-tracking) connects Camera : Entity to viewport selection, idle/physics updates, zoom/rotation, drag/limit policies and smoothing. It reuses scene ownership and canvas/input transforms; editor preview and inherited physics interpolation remain absent.

[Canvas layers](../components/canvas-rendering.md#canvas-layers) provide independent drawing groups, transforms, visibility and viewport following through CanvasLayer : Node and CanvasItem.GetCanvasLayerNode. Opaque canvas identities and independent viewport rendering remain separate dependencies.

[Canvas visibility masks](../components/canvas-rendering.md#canvas-visibility-masks) connect stored CanvasItem.VisibilityLayer and Viewport.CanvasCullMask to retained submission, including parent pruning and nested Y sorting. Mask edits preserve logical visibility, callbacks and input. Native Wayland GPU/compatibility, HLSL/GLSL and dummy/software behavior is verified; independent/offscreen viewports remain absent.

[Transform invalidation and delivery](../components/scene-hierarchy.md#transform-invalidation-and-delivery) integrates cached canvas coordinates, coalesced global notifications, synchronous opted-in local notifications and ForceUpdateTransform. Camera and PathFollow follow the same timing; queues cancel on exit, disposal and activation rollback.

[Polygon commands](../components/canvas-rendering.md#polygon-commands) connect copied contours and attributes to the existing backend batches. Concave triangulation and short primitives have managed and native pixel checks; exact hardware subpixel point/line raster parity remains Partial.

[Stroke commands](../components/canvas-rendering.md#stroke-commands) provide joined polylines, independent pairs, dashes, circular/elliptical arcs and filled/outlined circles/ellipses with local antialias feathers. Thin widths use framebuffer triangle expansion; their exact hardware line coverage remains Partial.

[Canvas animation intervals and rectangle geometry](../components/canvas-rendering.md#animation-intervals-and-rectangles) execute in the retained command path. The render clock follows captured scaled process steps and the active wrap setting; the same clock feeds the optional GPU fragment [TIME built-in](../components/shader-materials.md#render-time).
