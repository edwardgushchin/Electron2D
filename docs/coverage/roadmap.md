# Coverage roadmap

Last updated: 2026-10-08

Choose each next executable vertical slice by user API value, dependent work unlocked and current-backend feasibility. Resolve its applicable Partial rows with behavior evidence; do not treat easy isolated audits as the roadmap. `Unmapped` Electron2D rows need an exact upstream link or documented typed-C# rationale. The 3D/GDScript exclusions are not delivery work.

1. Close 1387 partially implemented rows and 0 unmapped Electron2D declarations within connected executable slices, including core, input, scene, resource and image domains.
2. Complete 1335 missing declarations in already represented type families; split each type by its documented dependency trigger.
3. Complete the missing 2D renderer integrations, then GUI/theme and tiles; complete CPU and independent GPU physics under ADR 0054; audio/navigation/animation; asset loaders and networking; and the self-hosted editor. Finish specific display/input host gaps at their documented triggers. The first executable GL/EGL/GLX fallback slice must audit each of the five blocked `DisplayServer.HandleType` identities against its actual driver and window-associated context under ADR 0042.

## Existing type backlog

These classes already have an Electron2D type. The counts scope work; they do not rank the next slice or authorize skipping a dependency.

| Godot class | Unimplemented members | Partial members |
| --- | ---: | ---: |
| [RenderingDevice](classes/RenderingDevice.md) | 527 | 7 |
| [RenderingServer](classes/RenderingServer.md) | 428 | 30 |
| [OS](classes/OS.md) | 94 | 6 |
| [Node](classes/Node.md) | 58 | 63 |
| [Window](classes/Window.md) | 35 | 57 |
| [Object](classes/Object.md) | 32 | 22 |
| [Performance](classes/Performance.md) | 24 | 2 |
| [RDShaderSPIRV](classes/RDShaderSPIRV.md) | 18 | 4 |
| [Engine](classes/Engine.md) | 16 | 18 |
| [FontFile](classes/FontFile.md) | 15 | 1 |
| [Input](classes/Input.md) | 12 | 31 |
| [SceneState](classes/SceneState.md) | 12 | 16 |
| [Image](classes/Image.md) | 11 | 71 |
| [TranslationServer](classes/TranslationServer.md) | 9 | 21 |
| [Control](classes/Control.md) | 8 | 60 |
| [ProjectSettings](classes/ProjectSettings.md) | 8 | 45 |
| [SceneTree](classes/SceneTree.md) | 6 | 20 |
| [AnimationPlayer](classes/AnimationPlayer.md) | 5 | 0 |
| [Font](classes/Font.md) | 4 | 0 |
| [FileAccess](classes/FileAccess.md) | 2 | 66 |
| [Resource](classes/Resource.md) | 2 | 20 |
| [Viewport](classes/Viewport.md) | 2 | 14 |
| [DirAccess](classes/DirAccess.md) | 1 | 39 |
| [Texture2D](classes/Texture.md#godot-texture2d) | 1 | 22 |
| [Polygon2D](classes/Polygon2D.md) | 1 | 13 |
| [PhysicsServer2D](classes/PhysicsServer2D.md) | 1 | 5 |
| [ResourceLoader](classes/ResourceLoader.md) | 1 | 3 |
| [Animation](classes/Animation.md) | 1 | 2 |
| [Material](classes/Material.md) | 1 | 0 |
| [CanvasItem](classes/CanvasItem.md) | 0 | 26 |
| [ItemList](classes/ItemList.md) | 0 | 19 |
| [Line2D](classes/Line2D.md) | 0 | 14 |
| [InputEventKey](classes/InputEventKey.md) | 0 | 10 |
| [Translation](classes/Translation.md) | 0 | 9 |
| [TranslationDomain](classes/TranslationDomain.md) | 0 | 9 |
| [DisplayServer](classes/DisplayServer.md) | 0 | 8 |
| [Area2D](classes/Area2D.md) | 0 | 7 |
| [ParallaxBackground](classes/ParallaxBackground.md) | 0 | 7 |
| [RegEx](classes/RegEx.md) | 0 | 7 |
| [InputEventScreenDrag](classes/InputEventScreenDrag.md) | 0 | 6 |
| [NavigationServer2D](classes/NavigationServer2D.md) | 0 | 6 |
| [RegExMatch](classes/RegExMatch.md) | 0 | 6 |
| [Shader](classes/Shader.md) | 0 | 6 |
| [ImageTexture](classes/ImageTexture.md) | 0 | 5 |
| [InputEventMouseMotion](classes/InputEventMouseMotion.md) | 0 | 5 |
| [MainLoop](classes/MainLoop.md) | 0 | 5 |
| [PackedScene](classes/PackedScene.md) | 0 | 5 |
| [RigidBody2D](classes/RigidBody2D.md) | 0 | 5 |
| [AudioStreamOggVorbis](classes/AudioStreamOggVorbis.md) | 0 | 3 |
| [ParallaxLayer](classes/ParallaxLayer.md) | 0 | 3 |
| [ShaderMaterial](classes/ShaderMaterial.md) | 0 | 3 |
| [AudioStreamPlayback](classes/AudioStreamPlayback.md) | 0 | 2 |
| [Camera2D](classes/Camera2D.md) | 0 | 2 |
| [GraphNode](classes/GraphNode.md) | 0 | 2 |
| [ImageTextureLayered](classes/ImageTextureLayered.md) | 0 | 2 |
| [InputEventMouse](classes/InputEventMouse.md) | 0 | 2 |
| [InputEventScreenTouch](classes/InputEventScreenTouch.md) | 0 | 2 |
| [KinematicCollision2D](classes/KinematicCollision2D.md) | 0 | 2 |
| [LineEdit](classes/LineEdit.md) | 0 | 2 |
| [ThemeDB](classes/ThemeDB.md) | 0 | 2 |
| [AnimationNode](classes/AnimationNode.md) | 0 | 1 |
| [ArrayMesh](classes/ArrayMesh.md) | 0 | 1 |
| [AudioStream](classes/AudioStream.md) | 0 | 1 |
| [AudioStreamPlayer](classes/AudioStreamPlayer.md) | 0 | 1 |
| [AudioStreamPlayer2D](classes/AudioStreamPlayer2D.md) | 0 | 1 |
| [CanvasLayer](classes/CanvasLayer.md) | 0 | 1 |
| [FileDialog](classes/FileDialog.md) | 0 | 1 |
| [OptimizedTranslation](classes/OptimizedTranslation.md) | 0 | 1 |
| [PhysicsDirectSpaceState2D](classes/PhysicsDirectSpaceState2D.md) | 0 | 1 |
| [PhysicsTestMotionResult2D](classes/PhysicsTestMotionResult2D.md) | 0 | 1 |
| [RayCast2D](classes/RayCast2D.md) | 0 | 1 |
| [RichTextLabel](classes/RichTextLabel.md) | 0 | 1 |
| [ShapeCast2D](classes/ShapeCast2D.md) | 0 | 1 |
| [TileSetAtlasSource](classes/TileSetAtlasSource.md) | 0 | 1 |
| [Timer](classes/Timer.md) | 0 | 1 |

## Blocked type families

| Exact trigger | Classes |
| --- | ---: |
| Trigger: first typed 2D visual-shader graph translation and shader-import slice (ADR 0028). | 92 |
| Trigger: first self-hosted editor executable slice under ADR 0027. | 65 |
| Trigger: the owning portable RenderingDevice pipeline/resource integration under revised ADR 0028. Local compute buffers and dispatch are the first connected slice; device graphics/texture pipelines, additional descriptor kinds, source import and their caches require their executing consumers. Three-dimensional and ray-tracing operations remain outside the 2D product boundary. | 20 |
| GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). | 19 |
| Trigger: an accepted typed scripting or extension-host contract and its first executable slice (ADR 0001). | 8 |
| Trigger: first typed asset loader, scene-file format and import slice after a concrete format is selected (ADRs 0013 and 0023). | 8 |
| Trigger: first Android or Web host-interoperability slice after the portable SDL host (ADR 0021). | 6 |
| Trigger: first type-specific OS, clock, diagnostics, logging, capture or tray-service integration beyond the existing SDL host, with target capability reporting (ADRs 0015, 0016 and 0021). | 6 |
| ADR 0094: Requires a WebRTC backend with ICE/STUN/TURN, SDP, DTLS/SCTP and data-channel state/ownership, plus native/browser packaging. Managed extension hooks follow the executable typed owner. | 4 |
| Trigger: first native camera-capture host slice with device lifetime and 2D texture delivery (ADR 0021). | 3 |
| Trigger: first video decoding, timed texture playback and audio synchronization slice. | 3 |
| Trigger: typed portable synchronization/explicit thread ownership over the accepted worker foundation (ADRs 0021 and 0104). | 3 |
| Trigger: actual GPU particle simulation, process-material shader/data integration, native storage and compute/collision/attractor ownership under ADR 0028. CPUParticles baseline does not provide GPU compute or process-material conversion. | 2 |
| Trigger: compressed array-file parsing/decoding or direct compressed native upload, source format/mipmap ownership and reproducible sampling over the executable TextureArray pipeline (ADR 0028). | 2 |
| Trigger: first compressed-texture import, decoder and verified GPU sampling slice (ADRs 0028 and 0039). | 2 |
| Trigger: first missing 2D material, canvas-modulation and shader-global renderer integration (ADR 0028). | 2 |
| Trigger: first public image-decoder plugin and format-discovery slice beyond the internal six-codec Image path (ADR 0039). | 2 |
| Trigger: first self-hosted editor and typed GUI authoring slice (ADRs 0027 and 0028). | 2 |
| Trigger: first shader include import and dependency-tracking slice (ADR 0028). | 2 |
| Trigger: first typed missing-asset placeholder and loader slice (ADRs 0013 and 0023). | 2 |
| Trigger: first typed networking, address-resolution and RPC slice. | 2 |
| Trigger: first typed packed-asset container and loader slice (ADRs 0013 and 0023). | 2 |
| Trigger: first writable GPU texture and blit-command lifetime slice (ADR 0028). | 2 |
| Trigger: typed mesh topology/adjacency and incremental geometry editing, attribute conversion and transactional commit to the now executable ArrayMesh; missing advanced channels enter their own shader/skeleton producer slices (ADR 0092). | 2 |
| ADR 0094: Typed MultiplayerPeer now executes. Requires executable WebRTCPeerConnection/DataChannel SDP/ICE/DTLS/SCTP host integration and an owned WebRTC multiplayer cohort. | 1 |
| ADR 0103 requires the complete executing typed implementation family and public registration. Internal space RID registration precedes owner initialization; selected factories now supply validated direct-state/query views to real scene/raw consumers and callbacks, preserving contact snapshots, failure/reentry guards and zero warmed allocation on CPU/GPU/no-device. CPU native collider storage now stays in its owner, with real wrapped collider policy/force/query/Area/CCD and portable state paths; common scene/server identities expose no native CPU handles. GPU attachments now remain in their resident store; wrapped CPU/GPU implementations retain identity/priority hooks, queries/Areas/joints and local/portable restore without facade native aliases. Selected initialization/activation/flush/end-sync now run with guarded lifecycle, mandatory observer delivery, nested phase retirement and empty-world intervals. Selected collider shape creation now receives one neutral scene/raw body/Area input span, preserving disabled logical indices, transforms/one-way policy and guarded scratch/preparation. Selected joint creation/policy now consumes immutable engine-unit configurations; CPU spring scheduling resolves native attachments from their actual owner and matching retirement preserves replacements, including selected wrappers. Selected checkpoint preparation and history operations now borrow lifetime; GPU reports/epochs/collider caches stay with the selected GPU history, while common replay/portable schema uses neutral field values. Selected resolved field values, exception invalidation/synchronization and portable preparation/batch reads now execute through actual owners with borrowing guards; common server/replay/map code retains no GPU handles or state batches. Internal named factories/default priority now retain immutable lazy selections and execute fresh scene/explicit owners with real wrapped response and selected views, execution-context recursion rejection and failed-owner recovery. Public named factories and the complete operation bridge remain open. Numerical default-force, CPU field and GPU parameter preparation now consumes value-only engine-unit inputs; shared bindings retain queue/dirty flag ownership, with real transformed scene/raw force response and guarded pre-solve recovery. Selected cached contact hooks now publish complete immutable scene/raw snapshots on the owner after native worker capture, reaching real monitor events and callbacks; cached contact reads preserve those values. Invalid/count/stale/user failures isolate receivers while later callbacks and committed ticks continue. | 1 |
| Animation: trigger is the first missing type-specific animation resource utility or persistence slice on the executable graph/state-machine/BlendSpace/action foundation (ADR 0093); applicable event tracks already execute. | 1 |
| Navigation2D: trigger is the next operation-specific navigation link, agent/obstacle avoidance, source-geometry bake or typed query slice beyond the authored map/region backend (ADRs 0052/0097). | 1 |
| The public Electron2D name is Marker : Entity under ADR 0004. A runtime-only anchor without the pinned editor cross would be an inert compatibility shell. Trigger: implement editor canvas gizmo drawing in the self-hosted editor, including configurable gizmo extents, then add Marker and verify the inherited spatial API; no runtime type exists yet. | 1 |
| Tiles: trigger is the first tile and atlas resource slice after 2D rendering. | 1 |
| Trigger: accepted MIDI-domain and native host-API decision, then the first MIDI device/event slice (ADR 0038). | 1 |
| Trigger: complete typed backend registration/factory and world-scoped implementation dispatch, shared RID lifecycle, callbacks, direct state/query result construction and CPU/GPU scene integration (ADRs 0054 and 0103); current facade is static/sealed, step/direct-space queries, fresh body/Area/joint attachments, world policy/capacity, completed statistics, full-step phases and body-motion dispatch now use retained built-in owners; platform velocity uses indexed current RID membership and selected collider motion; caller-created direct-space hooks now execute; scene/raw Area overlap scans, CPU field/body preparation and audio containment now use selected owners; native CPU interval/contact/CCD history plus selected private replay, portable one-way mapping and debug points now use owners; default-force preparation now uses selected collider owners and CPU field integration is concrete; internal named factories/default priority now retain immutable lazy selections and execute fresh scene/explicit owners with real wrapped response and selected views; other server operations, remaining concrete integration helpers, public registered factories and custom-geometry extension contexts remain open; caller-created body-state hooks now execute. | 1 |
| Trigger: concrete applicable 2D mesh import/library entry model, owned resource graphs and loader/authoring format integration over the implemented mesh resources (ADRs 0013/0092); audit 3D-only entry fields separately. | 1 |
| Trigger: first 2D light/mesh texture renderer integration (ADR 0028). | 1 |
| Trigger: first 2D world/render-environment integration slice after SDL3 GPU rendering (ADRs 0008 and 0028). | 1 |
| Trigger: first applicable typed 2D procedural geometry producer with concrete generation parameters and visible mesh output; static surface rendering already executes (ADR 0092). | 1 |
| Trigger: first native-menu service slice with ownership, callbacks and target checks (ADR 0041). | 1 |
| Trigger: first portable external-image ownership and native texture-import decision (ADRs 0021 and 0028). | 1 |
| Trigger: first semantic accessibility-tree, focus and native screen-reader bridge slice (ADR 0041). | 1 |
| Trigger: first typed 2D mesh-data and MeshInstance2D rendering slice; audit 3D-only members individually (ADR 0028). | 1 |
| Trigger: first typed 2D navigation and pathfinding slice. | 1 |
| Trigger: first typed GUI DPI-scale and theme-texture slice (ADR 0028). | 1 |
| Trigger: typed missing-asset mesh placeholder producer/loader and its 2D drawing/bounds policy over the implemented Mesh resource (ADR 0092). | 1 |
| Separate product-scope decision for each of 1 currently unassigned families; see their catalog pages for exact names. | 1 |

Each [catalog entry](catalog.md) opens the complete member table. Excluded rows have an accepted product reason and no implementation task.
