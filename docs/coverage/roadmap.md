# Coverage roadmap

Last updated: 2026-09-30

Choose each next executable vertical slice by user API value, dependent work unlocked and current-backend feasibility. Resolve its applicable Partial rows with behavior evidence; do not treat easy isolated audits as the roadmap. `Unmapped` Electron2D rows need an exact upstream link or documented typed-C# rationale. The 3D/GDScript exclusions are not delivery work.

1. Close 1268 partially implemented rows and 0 unmapped Electron2D declarations within connected executable slices, including core, input, scene, resource and image domains.
2. Complete 1051 missing declarations in already represented type families; split each type by its documented dependency trigger. Reassess dependencies for [AspectRatioContainer](classes/AspectRatioContainer.md), [CenterContainer](classes/CenterContainer.md), [DampedSpringJoint2D](classes/DampedSpringJoint2D.md), [GrooveJoint2D](classes/GrooveJoint2D.md), [Joint2D](classes/Joint2D.md), [MarginContainer](classes/MarginContainer.md), [PinJoint2D](classes/PinJoint2D.md), [WorldBoundaryShape2D](classes/WorldBoundaryShape2D.md) before selecting their slices.
3. Complete the missing 2D renderer integrations, then GUI/theme and tiles; remaining Box2D.NET physics; audio/navigation/animation; asset loaders and networking; and the self-hosted editor. Finish specific display/input host gaps at their documented triggers. The first executable GL/EGL/GLX fallback slice must audit each of the five blocked `DisplayServer.HandleType` identities against its actual driver and window-associated context under ADR 0042.

## Existing type backlog

These classes already have an Electron2D type. The counts scope work; they do not rank the next slice or authorize skipping a dependency.

| Godot class | Unimplemented members | Partial members |
| --- | ---: | ---: |
| [RenderingServer](classes/RenderingServer.md) | 565 | 7 |
| [PhysicsServer2D](classes/PhysicsServer2D.md) | 101 | 7 |
| [FontFile](classes/FontFile.md) | 76 | 0 |
| [Node](classes/Node.md) | 62 | 63 |
| [Window](classes/Window.md) | 42 | 34 |
| [Object](classes/Object.md) | 34 | 22 |
| [Control](classes/Control.md) | 16 | 60 |
| [Engine](classes/Engine.md) | 16 | 18 |
| [Input](classes/Input.md) | 12 | 31 |
| [TranslationServer](classes/TranslationServer.md) | 12 | 21 |
| [SceneState](classes/SceneState.md) | 12 | 16 |
| [Image](classes/Image.md) | 11 | 71 |
| [ProjectSettings](classes/ProjectSettings.md) | 9 | 44 |
| [Font](classes/Font.md) | 9 | 0 |
| [SceneTree](classes/SceneTree.md) | 8 | 19 |
| [Viewport](classes/Viewport.md) | 5 | 15 |
| [RigidBody2D](classes/RigidBody2D.md) | 5 | 7 |
| [Resource](classes/Resource.md) | 4 | 21 |
| [Material](classes/Material.md) | 3 | 0 |
| [FileAccess](classes/FileAccess.md) | 2 | 66 |
| [DirAccess](classes/DirAccess.md) | 1 | 39 |
| [Texture2D](classes/Texture.md#godot-texture2d) | 1 | 22 |
| [Polygon2D](classes/Polygon2D.md) | 1 | 13 |
| [ResourceLoader](classes/ResourceLoader.md) | 1 | 3 |
| [CollisionObject2D](classes/CollisionObject2D.md) | 1 | 0 |
| [CanvasItem](classes/CanvasItem.md) | 0 | 27 |
| [Line2D](classes/Line2D.md) | 0 | 14 |
| [NoiseTexture2D](classes/NoiseTexture2D.md) | 0 | 12 |
| [InputEventKey](classes/InputEventKey.md) | 0 | 10 |
| [Translation](classes/Translation.md) | 0 | 9 |
| [TranslationDomain](classes/TranslationDomain.md) | 0 | 9 |
| [Area2D](classes/Area2D.md) | 0 | 7 |
| [DisplayServer](classes/DisplayServer.md) | 0 | 7 |
| [ParallaxBackground](classes/ParallaxBackground.md) | 0 | 7 |
| [RegEx](classes/RegEx.md) | 0 | 7 |
| [InputEventScreenDrag](classes/InputEventScreenDrag.md) | 0 | 6 |
| [RegExMatch](classes/RegExMatch.md) | 0 | 6 |
| [Shader](classes/Shader.md) | 0 | 6 |
| [ImageTexture](classes/ImageTexture.md) | 0 | 5 |
| [InputEventMouseMotion](classes/InputEventMouseMotion.md) | 0 | 5 |
| [MainLoop](classes/MainLoop.md) | 0 | 5 |
| [PackedScene](classes/PackedScene.md) | 0 | 5 |
| [Noise](classes/Noise.md) | 0 | 4 |
| [ParallaxLayer](classes/ParallaxLayer.md) | 0 | 3 |
| [ShaderMaterial](classes/ShaderMaterial.md) | 0 | 3 |
| [Camera2D](classes/Camera2D.md) | 0 | 2 |
| [InputEventMouse](classes/InputEventMouse.md) | 0 | 2 |
| [InputEventScreenTouch](classes/InputEventScreenTouch.md) | 0 | 2 |
| [KinematicCollision2D](classes/KinematicCollision2D.md) | 0 | 2 |
| [PhysicsDirectBodyState2D](classes/PhysicsDirectBodyState2D.md) | 0 | 2 |
| [ThemeDB](classes/ThemeDB.md) | 0 | 2 |
| [CanvasLayer](classes/CanvasLayer.md) | 0 | 1 |
| [OptimizedTranslation](classes/OptimizedTranslation.md) | 0 | 1 |
| [PhysicsDirectSpaceState2D](classes/PhysicsDirectSpaceState2D.md) | 0 | 1 |
| [PhysicsTestMotionResult2D](classes/PhysicsTestMotionResult2D.md) | 0 | 1 |
| [RayCast2D](classes/RayCast2D.md) | 0 | 1 |
| [ShapeCast2D](classes/ShapeCast2D.md) | 0 | 1 |
| [Timer](classes/Timer.md) | 0 | 1 |

## Blocked type families

| Exact trigger | Classes |
| --- | ---: |
| Trigger: first typed 2D visual-shader graph translation and shader-import slice (ADR 0028). | 92 |
| Trigger: first self-hosted editor executable slice under ADR 0027. | 65 |
| Audio: trigger is the first audio mixing and playback slice. | 56 |
| GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). | 53 |
| Networking: trigger is the first networking and multiplayer slice. | 41 |
| Animation: trigger is the first scene animation slice. | 28 |
| Navigation2D: trigger is the first NavigationServer2D map, polygon, region and avoidance backend slice (ADR 0052). | 10 |
| Trigger: an accepted typed scripting or extension-host contract and its first executable slice (ADR 0001). | 10 |
| Trigger: first typed asset loader, scene-file format and import slice after a concrete format is selected (ADRs 0013 and 0023). | 10 |
| Trigger: first 2D skeletal animation and inverse-kinematics slice. | 9 |
| Tiles: trigger is the first tile and atlas resource slice after 2D rendering. | 8 |
| Trigger: first type-specific OS, clock, diagnostics, logging, capture or tray-service integration beyond the existing SDL host, with target capability reporting (ADRs 0015, 0016 and 0021). | 8 |
| Trigger: first layered/array texture storage, upload and sampling slice in the 2D renderer (ADR 0028). | 7 |
| Trigger: first Android or Web host-interoperability slice after the portable SDL host (ADR 0021). | 6 |
| Trigger: first typed 2D mesh-data and MeshInstance2D renderer slice (ADR 0028). | 6 |
| Trigger: first typed 2D mesh-data and MeshInstance2D rendering slice; audit 3D-only members individually (ADR 0028). | 6 |
| Trigger: first 2D light and occlusion renderer slice (ADR 0028). | 5 |
| Trigger: first self-hosted editor and typed GUI authoring slice (ADRs 0027 and 0028). | 5 |
| Trigger: a typed engine job-system decision with ownership, cancellation and target threading guarantees (ADRs 0001 and 0021). | 4 |
| Trigger: accepted typed cryptography utility contract and first portable crypto-service slice (ADR 0001). | 3 |
| Trigger: first 2D particle simulation, material and renderer integration slice (ADR 0028). | 3 |
| Trigger: first native camera-capture host slice with device lifetime and 2D texture delivery (ADR 0021). | 3 |
| Trigger: first typed networking-security integration slice with a portable crypto backend (ADR 0021). | 3 |
| Trigger: first video decoding, timed texture playback and audio synchronization slice. | 3 |
| Trigger: first 2D light/mesh texture renderer integration (ADR 0028). | 2 |
| Trigger: first 2D offscreen composition and framebuffer-copy slice (ADR 0028). | 2 |
| Trigger: first audio decoding and playback slice. | 2 |
| Trigger: first compressed-texture import, decoder and verified GPU sampling slice (ADRs 0028 and 0039). | 2 |
| Trigger: first independent offscreen viewport lifecycle and texture-output slice (ADRs 0008 and 0028). | 2 |
| Trigger: first missing 2D material, canvas-modulation and shader-global renderer integration (ADR 0028). | 2 |
| Trigger: first public image-decoder plugin and format-discovery slice beyond the internal six-codec Image path (ADR 0039). | 2 |
| Trigger: first shader include import and dependency-tracking slice (ADR 0028). | 2 |
| Trigger: first typed missing-asset placeholder and loader slice (ADRs 0013 and 0023). | 2 |
| Trigger: first typed networking, address-resolution and RPC slice. | 2 |
| Trigger: first typed packed-asset container and loader slice (ADRs 0013 and 0023). | 2 |
| Trigger: first writable GPU texture and blit-command lifetime slice (ADR 0028). | 2 |
| Trigger: typed physics resource-identity, shape/body/space lifetime and server extension contract beyond the first scene-body slice. | 2 |
| The public Electron2D name is Marker : Entity under ADR 0004. A runtime-only anchor without the pinned editor cross would be an inert compatibility shell. Trigger: implement editor canvas gizmo drawing in the self-hosted editor, including configurable gizmo extents, then add Marker and verify the inherited spatial API; no runtime type exists yet. | 1 |
| Trigger: accepted MIDI-domain and native host-API decision, then the first MIDI device/event slice (ADR 0038). | 1 |
| Trigger: first 2D skeleton bone and physics-body ownership integration. | 1 |
| Trigger: first 2D world/render-environment integration slice after SDL3 GPU rendering (ADRs 0008 and 0028). | 1 |
| Trigger: first concrete typed resource file format and serializer with ownership and rollback (ADRs 0013 and 0023). | 1 |
| Trigger: first native-menu service slice with ownership, callbacks and target checks (ADR 0041). | 1 |
| Trigger: first portable external-image ownership and native texture-import decision (ADRs 0021 and 0028). | 1 |
| Trigger: first public typed loader-plugin registration and callback slice after the concrete internal image-texture loader (ADR 0013). | 1 |
| Trigger: first semantic accessibility-tree, focus and native screen-reader bridge slice (ADR 0041). | 1 |
| Trigger: first typed 2D navigation and pathfinding slice. | 1 |
| Trigger: first typed GUI DPI-scale and theme-texture slice (ADR 0028). | 1 |
| Trigger: first typed multiplayer replication slice after scene persistence (ADR 0023). | 1 |
| Trigger: first typed rich-text effect slice after 2D GUI and text rendering (ADR 0028). | 1 |
| Trigger: platform font discovery, matching and owned fallback faces over the integrated FontFile backend (ADR 0046). | 1 |
| Trigger: typed direct-space sweep/ray/point query and result lifecycle over the PhysicsServer space. | 1 |
| Trigger: typed live body-state callback and solver ownership over the PhysicsServer space. | 1 |
| Trigger: variable-font instance coordinates, variation metadata and per-instance shaping/raster cache identity over the integrated FreeType/HarfBuzz backend (ADR 0046). | 1 |
| Separate product-scope decision for each of 1 currently unassigned families; see their catalog pages for exact names. | 1 |

Each [catalog entry](catalog.md) opens the complete member table. Excluded rows have an accepted product reason and no implementation task.
