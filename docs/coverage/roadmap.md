# Coverage roadmap

Last updated: 2026-10-08

Choose each next executable vertical slice by user API value, dependent work unlocked and current-backend feasibility. Resolve its applicable Partial rows with behavior evidence; do not treat easy isolated audits as the roadmap. `Unmapped` Electron2D rows need an exact upstream link or documented typed-C# rationale. The 3D/GDScript exclusions are not delivery work.

1. Close 1545 partially implemented rows and 0 unmapped Electron2D declarations within connected executable slices, including core, input, scene, resource and image domains.
2. Complete 725 missing declarations in already represented type families; split each type by its documented dependency trigger.
3. Complete the missing 2D renderer integrations, then GUI/theme and tiles; complete CPU and independent GPU physics under ADR 0054; audio/navigation/animation; asset loaders and networking; and the self-hosted editor. Finish specific display/input host gaps at their documented triggers. The first executable GL/EGL/GLX fallback slice must audit each of the five blocked `DisplayServer.HandleType` identities against its actual driver and window-associated context under ADR 0042.

## Existing type backlog

These classes already have an Electron2D type. The counts scope work; they do not rank the next slice or authorize skipping a dependency.

| Godot class | Unimplemented members | Partial members |
| --- | ---: | ---: |
| [RenderingServer](classes/RenderingServer.md) | 386 | 34 |
| [RenderingDevice](classes/RenderingDevice.md) | 154 | 39 |
| [OS](classes/OS.md) | 48 | 10 |
| [Window](classes/Window.md) | 32 | 57 |
| [Node](classes/Node.md) | 22 | 60 |
| [Object](classes/Object.md) | 21 | 16 |
| [Input](classes/Input.md) | 12 | 30 |
| [Performance](classes/Performance.md) | 12 | 2 |
| [Engine](classes/Engine.md) | 11 | 11 |
| [SceneTree](classes/SceneTree.md) | 6 | 18 |
| [ProjectSettings](classes/ProjectSettings.md) | 5 | 44 |
| [SceneState](classes/SceneState.md) | 5 | 15 |
| [Control](classes/Control.md) | 3 | 60 |
| [Image](classes/Image.md) | 2 | 73 |
| [FileAccess](classes/FileAccess.md) | 2 | 65 |
| [DirAccess](classes/DirAccess.md) | 1 | 39 |
| [Texture2D](classes/Texture.md#godot-texture2d) | 1 | 22 |
| [Polygon2D](classes/Polygon2D.md) | 1 | 13 |
| [Material](classes/Material.md) | 1 | 0 |
| [PhysicsServer2DExtension](classes/PhysicsServer2DExtension.md) | 0 | 136 |
| [CanvasItem](classes/CanvasItem.md) | 0 | 26 |
| [TranslationServer](classes/TranslationServer.md) | 0 | 20 |
| [ItemList](classes/ItemList.md) | 0 | 19 |
| [Line2D](classes/Line2D.md) | 0 | 14 |
| [Viewport](classes/Viewport.md) | 0 | 14 |
| [InputEventKey](classes/InputEventKey.md) | 0 | 10 |
| [RDSamplerState](classes/RDSamplerState.md) | 0 | 10 |
| [RDTextureFormat](classes/RDTextureFormat.md) | 0 | 9 |
| [TranslationDomain](classes/TranslationDomain.md) | 0 | 9 |
| [DisplayServer](classes/DisplayServer.md) | 0 | 8 |
| [Translation](classes/Translation.md) | 0 | 8 |
| [ParallaxBackground](classes/ParallaxBackground.md) | 0 | 7 |
| [RegEx](classes/RegEx.md) | 0 | 7 |
| [InputEventScreenDrag](classes/InputEventScreenDrag.md) | 0 | 6 |
| [NavigationServer2D](classes/NavigationServer2D.md) | 0 | 6 |
| [RDPipelineDepthStencilState](classes/RDPipelineDepthStencilState.md) | 0 | 6 |
| [RegExMatch](classes/RegExMatch.md) | 0 | 6 |
| [Shader](classes/Shader.md) | 0 | 6 |
| [ImageTexture](classes/ImageTexture.md) | 0 | 5 |
| [InputEventMouseMotion](classes/InputEventMouseMotion.md) | 0 | 5 |
| [MainLoop](classes/MainLoop.md) | 0 | 5 |
| [PackedScene](classes/PackedScene.md) | 0 | 5 |
| [PhysicsServer2D](classes/PhysicsServer2D.md) | 0 | 5 |
| [RDTextureView](classes/RDTextureView.md) | 0 | 5 |
| [Resource](classes/Resource.md) | 0 | 4 |
| [ResourceLoader](classes/ResourceLoader.md) | 0 | 4 |
| [AudioStreamOggVorbis](classes/AudioStreamOggVorbis.md) | 0 | 3 |
| [ParallaxLayer](classes/ParallaxLayer.md) | 0 | 3 |
| [ShaderMaterial](classes/ShaderMaterial.md) | 0 | 3 |
| [Animation](classes/Animation.md) | 0 | 2 |
| [AudioStreamPlayback](classes/AudioStreamPlayback.md) | 0 | 2 |
| [Camera2D](classes/Camera2D.md) | 0 | 2 |
| [GraphNode](classes/GraphNode.md) | 0 | 2 |
| [ImageTextureLayered](classes/ImageTextureLayered.md) | 0 | 2 |
| [InputEventMouse](classes/InputEventMouse.md) | 0 | 2 |
| [InputEventScreenTouch](classes/InputEventScreenTouch.md) | 0 | 2 |
| [LineEdit](classes/LineEdit.md) | 0 | 2 |
| [RDShaderSource](classes/RDShaderSource.md) | 0 | 2 |
| [ThemeDB](classes/ThemeDB.md) | 0 | 2 |
| [AnimationNode](classes/AnimationNode.md) | 0 | 1 |
| [ArrayMesh](classes/ArrayMesh.md) | 0 | 1 |
| [AudioStream](classes/AudioStream.md) | 0 | 1 |
| [AudioStreamPlayer](classes/AudioStreamPlayer.md) | 0 | 1 |
| [AudioStreamPlayer2D](classes/AudioStreamPlayer2D.md) | 0 | 1 |
| [CanvasLayer](classes/CanvasLayer.md) | 0 | 1 |
| [FileDialog](classes/FileDialog.md) | 0 | 1 |
| [ImporterMesh](classes/ImporterMesh.md) | 0 | 1 |
| [OptimizedTranslation](classes/OptimizedTranslation.md) | 0 | 1 |
| [RDFramebufferPass](classes/RDFramebufferPass.md) | 0 | 1 |
| [RichTextLabel](classes/RichTextLabel.md) | 0 | 1 |
| [Texture2DRD](classes/TextureRD.md) | 0 | 1 |
| [TileSetAtlasSource](classes/TileSetAtlasSource.md) | 0 | 1 |
| [Timer](classes/Timer.md) | 0 | 1 |

## Blocked type families

| Exact trigger | Classes |
| --- | ---: |
| Trigger: first typed 2D visual-shader graph translation and shader-import slice (ADR 0028). | 92 |
| Trigger: first self-hosted editor executable slice under ADR 0027. | 65 |
| GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). | 7 |
| Trigger: an accepted typed scripting or extension-host contract and its first executable slice (ADR 0001). | 7 |
| Trigger: first Android or Web host-interoperability slice after the portable SDL host (ADR 0021). | 6 |
| ADR 0094: Requires a WebRTC backend with ICE/STUN/TURN, SDP, DTLS/SCTP and data-channel state/ownership, plus native/browser packaging. Managed extension hooks follow the executable typed owner. | 4 |
| Trigger: first type-specific OS, clock, diagnostics, logging, capture or tray-service integration beyond the existing SDL host, with target capability reporting (ADRs 0015, 0016 and 0021). | 4 |
| Trigger: first native camera-capture host slice with device lifetime and 2D texture delivery (ADR 0021). | 3 |
| Trigger: first video decoding, timed texture playback and audio synchronization slice. | 3 |
| Trigger: the owning portable RenderingDevice pipeline/resource integration under revised ADR 0028. Local compute buffers and dispatch are the first connected slice; device graphics/texture pipelines, additional descriptor kinds, source import and their caches require their executing consumers. Three-dimensional and ray-tracing operations remain outside the 2D product boundary. | 3 |
| Trigger: actual GPU particle simulation, process-material shader/data integration, native storage and compute/collision/attractor ownership under ADR 0028. CPUParticles baseline does not provide GPU compute or process-material conversion. | 2 |
| Trigger: compressed array-file parsing/decoding or direct compressed native upload, source format/mipmap ownership and reproducible sampling over the executable TextureArray pipeline (ADR 0028). | 2 |
| Trigger: first compressed-texture import, decoder and verified GPU sampling slice (ADRs 0028 and 0039). | 2 |
| Trigger: first typed asset loader, scene-file format and import slice after a concrete format is selected (ADRs 0013 and 0023). | 2 |
| Trigger: first typed missing-asset placeholder and loader slice (ADRs 0013 and 0023). | 2 |
| Trigger: first typed packed-asset container and loader slice (ADRs 0013 and 0023). | 2 |
| Trigger: first writable GPU texture and blit-command lifetime slice (ADR 0028). | 2 |
| ADR 0094: Typed MultiplayerPeer now executes. Requires executable WebRTCPeerConnection/DataChannel SDP/ICE/DTLS/SCTP host integration and an owned WebRTC multiplayer cohort. | 1 |
| Exact prerequisite: typed persistent scene signal/endpoint storage and binding, plus missing-node factory/reference projection. Version-2 resource field roles do not implement node signal recording or missing-node hierarchy restoration (ADRs 0013/0023). No inert recording switch. | 1 |
| Navigation2D: trigger is the next operation-specific navigation link, agent/obstacle avoidance, source-geometry bake or typed query slice beyond the authored map/region backend (ADRs 0052/0097). | 1 |
| The public Electron2D name is Marker : Entity under ADR 0004. A runtime-only anchor without the pinned editor cross would be an inert compatibility shell. Trigger: implement editor canvas gizmo drawing in the self-hosted editor, including configurable gizmo extents, then add Marker and verify the inherited spatial API; no runtime type exists yet. | 1 |
| Tiles: trigger is the first tile and atlas resource slice after 2D rendering. | 1 |
| Trigger: accepted MIDI-domain and native host-API decision, then the first MIDI device/event slice (ADR 0038). | 1 |
| Trigger: actual editor execution-context reporting through Engine.IsEditorHint and editor/runtime border drawing under ADRs 0015/0027/0090. The current runtime has no editor-context producer; do not introduce a constant hint or inert EditorOnly switch. | 1 |
| Trigger: an installed compiler-consumed built-in include library with stable names, source lookup and all three retained static discovery operations (ADR 0028). ShaderInclude user-source files and native compiler preprocessing now execute; they do not supply a built-in library or invent empty lookup results. | 1 |
| Trigger: complete typed thread start/result, priority, main-owner identity and join/retirement, plus a resolved executable safety-check override contract: the pinned set_thread_safety_checks_enabled(bool) must not be an inert flag and current ADRs 0006/0011/0104 require owner-affine scene mutation. | 1 |
| Trigger: first 2D light/mesh texture renderer integration (ADR 0028). | 1 |
| Trigger: first 2D world/render-environment integration slice after SDL3 GPU rendering (ADRs 0008 and 0028). | 1 |
| Trigger: first applicable typed 2D procedural geometry producer with concrete generation parameters and visible mesh output; static surface rendering already executes (ADR 0092). | 1 |
| Trigger: first missing 2D material, canvas-modulation and shader-global renderer integration (ADR 0028). | 1 |
| Trigger: first native-menu service slice with ownership, callbacks and target checks (ADR 0041). | 1 |
| Trigger: first portable external-image ownership and native texture-import decision (ADRs 0021 and 0028). | 1 |
| Trigger: first semantic accessibility-tree, focus and native screen-reader bridge slice (ADR 0041). | 1 |
| Trigger: first typed 2D mesh-data and MeshInstance2D rendering slice; audit 3D-only members individually (ADR 0028). | 1 |
| Separate product-scope decision for each of 1 currently unassigned families; see their catalog pages for exact names. | 1 |

Each [catalog entry](catalog.md) opens the complete member table. Excluded rows have an accepted product reason and no implementation task.
