# Coverage roadmap

Last updated: 2026-10-26

Choose each next executable vertical slice by user API value, dependent work unlocked and current-backend feasibility. Resolve its applicable Partial rows with behavior evidence; do not treat easy isolated audits as the roadmap. `Unmapped` Electron2D rows need an exact upstream link or documented typed-C# rationale. The 3D/GDScript exclusions are not delivery work.

1. Close 1151 partially implemented rows and 0 unmapped Electron2D declarations within connected executable slices, including core, input, scene, resource and image domains.
2. Complete 493 missing declarations in already represented type families; split each type by its documented dependency trigger. Reassess dependencies for [VisualShaderNodeDistanceFade](classes/VisualShaderNodeDistanceFade.md), [VisualShaderNodeProximityFade](classes/VisualShaderNodeProximityFade.md) before selecting their slices.
3. Complete the missing 2D renderer integrations, then GUI/theme and tiles; complete CPU and independent GPU physics under ADR 0054; audio/navigation/animation; asset loaders and networking. Advance the self-hosted editor through the dependency stages below rather than waiting for the whole runtime backlog. Finish specific display/input host gaps at their documented triggers. The first executable GL/EGL/GLX fallback slice must audit each of the five blocked `DisplayServer.HandleType` identities against its actual driver and window-associated context under ADR 0042.

## Live project and agent delivery

[ADR 0090](../decisions/agent-native.md#delivery-and-acceptance) owns the complete live-session decision and acceptance gates. The [bounded no-UI session](../components/project-session.md) implements stage 1 for one typed scene and C# buffer; later stages remain unimplemented. This tooling slice changes no runtime declaration states or totals.

1. **Implemented bounded session foundation:** one existing typed scene and C# buffer, an exclusive owner lease, two local clients, typed operations, stable identities, unsaved documents, revisions/conflicts, events, persistence/recovery and guarded grouped history. Independent-process exclusion/crash recovery, failing mutations/saves, selective undo and fresh-process persistence execute in `tests/ProjectSession`. General project/resource operations and transport attach remain separate extensions.
2. **First functional editor project workflow:** bind minimal scene/inspector/code/preview views to that session and execute snapshot-backed build/run/capture jobs including unsaved inputs. Verify visible updates, stale results, real process/render output and cancellation/cleanup. Replace/remove the old Batch commands, SDK, formats and affected consumers/docs without a compatibility bridge.
3. **First agent-enabled editor milestone, before UI polish:** connect one real executor through actual local operations transport and an in-editor task/conversation panel; prefer ACP where supported. Exercise human/agent unsaved edits, stale conflicts, safe undo, snapshot jobs and actual artifacts. A manifest, fake executor or panel image is not this gate.
4. **After the first co-development slice:** expand semantic operations and scoped discovery, benchmark real tasks, then qualify installed delivery and additional executors/platforms. Concurrent agents and hot reload require their own justified and verified slices.

Linux/Wayland remains the executable gate under ADR 0021. Absent editor/import/export/plugin families retain exact dependencies; the historical broad trigger counts below do not mean the startup window alone unlocks those families.

## Existing type backlog

These classes already have an Electron2D type. The counts scope work; they do not rank the next slice or authorize skipping a dependency.

| Godot class | Unimplemented members | Partial members |
| --- | ---: | ---: |
| [RenderingServer](classes/RenderingServer.md) | 243 | 29 |
| [RenderingDevice](classes/RenderingDevice.md) | 151 | 37 |
| [OS](classes/OS.md) | 31 | 14 |
| [TextServer](classes/TextServer.md) | 18 | 0 |
| [Engine](classes/Engine.md) | 11 | 11 |
| [Window](classes/Window.md) | 7 | 54 |
| [SceneTree](classes/SceneTree.md) | 5 | 15 |
| [SceneState](classes/SceneState.md) | 5 | 10 |
| [ProjectSettings](classes/ProjectSettings.md) | 4 | 36 |
| [Performance](classes/Performance.md) | 4 | 2 |
| [Control](classes/Control.md) | 3 | 58 |
| [FileAccess](classes/FileAccess.md) | 2 | 65 |
| [Object](classes/Object.md) | 2 | 4 |
| [Image](classes/Image.md) | 1 | 70 |
| [DirAccess](classes/DirAccess.md) | 1 | 39 |
| [Input](classes/Input.md) | 1 | 34 |
| [Node](classes/Node.md) | 1 | 2 |
| [Material](classes/Material.md) | 1 | 0 |
| [Texture2D](classes/Texture.md#godot-texture2d) | 0 | 22 |
| [Polygon2D](classes/Polygon2D.md) | 0 | 13 |
| [TranslationServer](classes/TranslationServer.md) | 0 | 12 |
| [DisplayServer](classes/DisplayServer.md) | 0 | 11 |
| [InputEventKey](classes/InputEventKey.md) | 0 | 10 |
| [RDSamplerState](classes/RDSamplerState.md) | 0 | 10 |
| [RDTextureFormat](classes/RDTextureFormat.md) | 0 | 9 |
| [Translation](classes/Translation.md) | 0 | 8 |
| [Viewport](classes/Viewport.md) | 0 | 8 |
| [ParallaxBackground](classes/ParallaxBackground.md) | 0 | 7 |
| [InputEventScreenDrag](classes/InputEventScreenDrag.md) | 0 | 6 |
| [NavigationServer2D](classes/NavigationServer2D.md) | 0 | 6 |
| [RDPipelineDepthStencilState](classes/RDPipelineDepthStencilState.md) | 0 | 6 |
| [CanvasItem](classes/CanvasItem.md) | 0 | 5 |
| [ImageTexture](classes/ImageTexture.md) | 0 | 5 |
| [InputEventMouseMotion](classes/InputEventMouseMotion.md) | 0 | 5 |
| [PackedScene](classes/PackedScene.md) | 0 | 5 |
| [ResourceLoader](classes/ResourceLoader.md) | 0 | 4 |
| [TranslationDomain](classes/TranslationDomain.md) | 0 | 4 |
| [AudioStreamOggVorbis](classes/AudioStreamOggVorbis.md) | 0 | 3 |
| [ParallaxLayer](classes/ParallaxLayer.md) | 0 | 3 |
| [Shader](classes/Shader.md) | 0 | 3 |
| [ShaderMaterial](classes/ShaderMaterial.md) | 0 | 3 |
| [Animation](classes/Animation.md) | 0 | 2 |
| [AudioStreamPlayback](classes/AudioStreamPlayback.md) | 0 | 2 |
| [Camera2D](classes/Camera2D.md) | 0 | 2 |
| [GraphNode](classes/GraphNode.md) | 0 | 2 |
| [InputEventMouse](classes/InputEventMouse.md) | 0 | 2 |
| [InputEventScreenTouch](classes/InputEventScreenTouch.md) | 0 | 2 |
| [LineEdit](classes/LineEdit.md) | 0 | 2 |
| [RDShaderSource](classes/RDShaderSource.md) | 0 | 2 |
| [ThemeDB](classes/ThemeDB.md) | 0 | 2 |
| [AnimationNode](classes/AnimationNode.md) | 0 | 1 |
| [AudioStream](classes/AudioStream.md) | 0 | 1 |
| [AudioStreamPlayer](classes/AudioStreamPlayer.md) | 0 | 1 |
| [AudioStreamPlayer2D](classes/AudioStreamPlayer2D.md) | 0 | 1 |
| [CanvasLayer](classes/CanvasLayer.md) | 0 | 1 |
| [FileDialog](classes/FileDialog.md) | 0 | 1 |
| [MainLoop](classes/MainLoop.md) | 0 | 1 |
| [OptimizedTranslation](classes/OptimizedTranslation.md) | 0 | 1 |
| [RDTextureView](classes/RDTextureView.md) | 0 | 1 |
| [RichTextLabel](classes/RichTextLabel.md) | 0 | 1 |
| [Texture2DRD](classes/TextureRD.md) | 0 | 1 |
| [TileSetAtlasSource](classes/TileSetAtlasSource.md) | 0 | 1 |
| [Timer](classes/Timer.md) | 0 | 1 |

## Blocked type families

| Exact trigger | Classes |
| --- | ---: |
| Trigger: first self-hosted editor executable slice under ADR 0027. | 65 |
| Trigger: an accepted typed scripting or extension-host contract and its first executable slice (ADR 0001). | 7 |
| Trigger: first Android or Web host-interoperability slice after the portable SDL host (ADR 0021). | 6 |
| ADR 0046/0107: built-in buffers and service-owned fonts/caches now execute. Requires constructible external TextServerExtension virtual/callback dispatch for the complete family, provider-owned source/cache lifetimes, callback errors/reentry, retirement and public provider registration/replacement integration. This member enters that external implementation/dispatch slice. | 4 |
| Trigger: first native camera-capture host slice with device lifetime and 2D texture delivery (ADR 0021). | 3 |
| Trigger: first type-specific OS, clock, diagnostics, logging, capture or tray-service integration beyond the existing SDL host, with target capability reporting (ADRs 0015, 0016 and 0021). | 3 |
| Trigger: the owning portable RenderingDevice pipeline/resource integration under revised ADR 0028. Local compute buffers and dispatch are the first connected slice; device graphics/texture pipelines, additional descriptor kinds, source import and their caches require their executing consumers. Three-dimensional and ray-tracing operations remain outside the 2D product boundary. | 3 |
| Trigger: first typed packed-asset container and loader slice (ADRs 0013 and 0023). | 2 |
| Trigger: graph-frame/comment authoring with a real editor graph layout/preview consumer under ADRs 0027/0028; numeric canvas graph import already executes. | 2 |
| Navigation2D: trigger is the next operation-specific navigation link, agent/obstacle avoidance, source-geometry bake or typed query slice beyond the authored map/region backend (ADRs 0052/0097). | 1 |
| The public Electron2D name is Marker : Entity under ADR 0004. A runtime-only anchor without the pinned editor cross would be an inert compatibility shell. Trigger: implement editor canvas gizmo drawing in the self-hosted editor, including configurable gizmo extents, then add Marker and verify the inherited spatial API; no runtime type exists yet. | 1 |
| Tiles: trigger is the first tile and atlas resource slice after 2D rendering. | 1 |
| Trigger: accepted MIDI-domain and native host-API decision, then the first MIDI device/event slice (ADR 0038). | 1 |
| Trigger: actual editor execution-context reporting through Engine.IsEditorHint and editor/runtime border drawing under ADRs 0015/0027/0090. The current runtime has no editor-context producer; do not introduce a constant hint or inert EditorOnly switch. | 1 |
| Trigger: complete typed thread start/result, priority, main-owner identity and join/retirement, plus a resolved executable safety-check override contract: the pinned set_thread_safety_checks_enabled(bool) must not be an inert flag and current ADRs 0006/0011/0104 require owner-affine scene mutation. | 1 |
| Trigger: first 2D world/render-environment integration slice after SDL3 GPU rendering (ADRs 0008 and 0028). | 1 |
| Trigger: first native-menu service slice with ownership, callbacks and target checks (ADR 0041). | 1 |
| Trigger: first portable external-image ownership and native texture-import decision (ADRs 0021 and 0028). | 1 |
| Trigger: first semantic accessibility-tree, focus and native screen-reader bridge slice (ADR 0041). | 1 |
| Trigger: first typed 2D mesh-data and MeshInstance2D rendering slice; audit 3D-only members individually (ADR 0028). | 1 |
| Trigger: first typed asset loader, scene-file format and import slice after a concrete format is selected (ADRs 0013 and 0023). | 1 |
| Trigger: first typed missing-asset placeholder and loader slice (ADRs 0013 and 0023). | 1 |
| Separate product-scope decision for each of 1 currently unassigned families; see their catalog pages for exact names. | 1 |

Each [catalog entry](catalog.md) opens the complete member table. Excluded rows have an accepted product reason and no implementation task.
