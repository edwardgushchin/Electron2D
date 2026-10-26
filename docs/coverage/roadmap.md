# Coverage roadmap

Last updated: 2026-10-26

Choose each next executable vertical slice by user API value, dependent work unlocked and current-backend feasibility. Resolve its applicable Partial rows with behavior evidence; do not treat easy isolated audits as the roadmap. `Unmapped` Electron2D rows need an exact upstream link or documented typed-C# rationale. The 3D/GDScript exclusions are not delivery work.

1. Close 1123 partially implemented rows and 0 unmapped Electron2D declarations within connected executable slices, including core, input, scene, resource and image domains.
2. Complete 498 missing declarations in already represented type families; split each type by its documented dependency trigger. Reassess dependencies for [VisualShaderNodeDistanceFade](classes/VisualShaderNodeDistanceFade.md), [VisualShaderNodeProximityFade](classes/VisualShaderNodeProximityFade.md) before selecting their slices.
3. Complete the missing 2D renderer integrations, then GUI/theme and tiles; complete CPU and independent GPU physics under ADR 0054; audio/navigation/animation; asset loaders and networking. Advance the self-hosted editor through the dependency stages below rather than waiting for the whole runtime backlog. Finish specific display/input host gaps at their documented triggers. The first executable GL/EGL/GLX fallback slice must audit each of the five blocked `DisplayServer.HandleType` identities against its actual driver and window-associated context under ADR 0042.

## Live project and agent delivery

[ADR 0090](../decisions/agent-native.md#delivery-and-acceptance) owns the complete live-session decision and acceptance gates. The [bounded no-UI session](../components/project-session.md) implements stage 1 for one typed scene and C# buffer. The [shared editor workflow](../components/project-jobs.md) implements bounded stage 2; [authenticated local operations and the real agent panel/executor](../components/project-agent.md) now exercise bounded stage 3. This tooling slice changes no runtime declaration states or totals.

1. **Implemented bounded session foundation:** one existing typed scene and C# buffer, an exclusive owner lease, two local clients, typed operations, stable identities, unsaved documents, revisions/conflicts, events, persistence/recovery and guarded grouped history. Independent-process exclusion/crash recovery, failing mutations/saves, selective undo and fresh-process persistence execute in `tests/ProjectSession`. General project/resource operations remain separate extensions; local authenticated attach now consumes this owner.
2. **Implemented bounded editor workflow:** create/open one binary-SDK project, bind scene/position/code/history/passive preview views to that session, and execute immutable unsaved build/run/capture jobs. Public/native tests exercise visible updates, stale captures, real output, failures, cancellation/deadline and owner-death descendant cleanup. The old controller/SDK/formats and consumers are removed. General authoring/dependency profiles and installed/foreign-platform/owner acceptance retain their gates.
3. **Implemented bounded agent-enabled editor:** authenticated Linux Unix ingress and MCP stdio connect codex-cli 0.163.0 through a replaceable app-server adapter to the native task/conversation/control panel. The real model scenario verifies unsaved edits, stale conflict repair, real snapshot PNG/assertions, cancellation and same-conversation reconnect. Deterministic checks retain safe undo, invalid input, missing executor, rights, compiler failure, disconnect/recovery and cleanup. Other executors/protocols/platforms remain unqualified.
4. **Stage 4a, next project-authoring slice:** [Git-backed creation and durable project memory](../decisions/agent-native.md#git-backed-project-memory), portable instructions, shared Markdown/six-section journal publication, validated Conventional Commits 1.0.0, bounded indexed context and an actually delivered/loaded development skill. Verify fresh-client continuation in editor/no-UI modes.
5. **Stage 4b, Git workspace:** use 4a's repository/document boundary for editor/no-UI review/index/commits, branches, remotes/fetch/pull/push, worktrees, integration/conflicts and explicit recovery. Preserve dirty buffers and coordinate shared refs; verify real failure/cancellation/uncertain-result cases.
6. **Stage 4c, provider and capability expansion:** qualify the named provider/backend plus executor profiles in the [support matrix](../components/project-agent.md#provider-and-client-support), including native Kimi Code CLI/Qwen Code and compatible-executor DeepSeek/GLM/MiniMax backends, plus Copilot CLI/Cursor external clients. Keep the common skill/context/journal contract and qualify each actual endpoint/model/executor combination. Expand semantic operations, spatial context and composition; benchmark real tasks and qualify installed distribution/additional platforms. Keep all README locales accurate. Concurrent agents and hot reload retain separate justified slices.

Linux/Wayland remains the executable gate under ADR 0021. Absent editor/import/export/plugin families retain exact dependencies; the historical broad trigger counts below do not mean the startup window alone unlocks those families.

## Existing type backlog

These classes already have an Electron2D type. The counts scope work; they do not rank the next slice or authorize skipping a dependency.

| Godot class | Unimplemented members | Partial members |
| --- | ---: | ---: |
| [RenderingServer](classes/RenderingServer.md) | 243 | 26 |
| [RenderingDevice](classes/RenderingDevice.md) | 151 | 37 |
| [OS](classes/OS.md) | 31 | 14 |
| [TextServer](classes/TextServer.md) | 18 | 0 |
| [Engine](classes/Engine.md) | 10 | 11 |
| [Window](classes/Window.md) | 6 | 53 |
| [ProjectSettings](classes/ProjectSettings.md) | 6 | 36 |
| [AccessibilityServer](classes/AccessibilityServer.md) | 6 | 2 |
| [SceneTree](classes/SceneTree.md) | 5 | 15 |
| [SceneState](classes/SceneState.md) | 5 | 10 |
| [Performance](classes/Performance.md) | 4 | 2 |
| [Control](classes/Control.md) | 3 | 58 |
| [FileAccess](classes/FileAccess.md) | 2 | 65 |
| [Object](classes/Object.md) | 2 | 2 |
| [Image](classes/Image.md) | 1 | 70 |
| [DirAccess](classes/DirAccess.md) | 1 | 39 |
| [Input](classes/Input.md) | 1 | 34 |
| [Node](classes/Node.md) | 1 | 0 |
| [Texture2D](classes/Texture.md#godot-texture2d) | 0 | 16 |
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
| [CanvasLayer](classes/CanvasLayer.md) | 0 | 1 |
| [FileDialog](classes/FileDialog.md) | 0 | 1 |
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
| Trigger: first typed 2D mesh-data and MeshInstance2D rendering slice; audit 3D-only members individually (ADR 0028). | 1 |
| Trigger: first typed asset loader, scene-file format and import slice after a concrete format is selected (ADRs 0013 and 0023). | 1 |
| Separate product-scope decision for each of 1 currently unassigned families; see their catalog pages for exact names. | 1 |

Each [catalog entry](catalog.md) opens the complete member table. Excluded rows have an accepted product reason and no implementation task.
