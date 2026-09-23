# Coverage roadmap

Last updated: 2026-09-23

The order follows concrete dependencies. `Partial` rows need either a semantic audit or resolution of a documented behavior gap; `Unmapped` Electron2D rows need an exact upstream link or a documented typed-C# rationale. The 3D/GDScript exclusions are not delivery work.

1. Review 1745 partially implemented rows and 0 unmapped Electron2D declarations, beginning with the existing core, input, scene, resource and image domains.
2. Complete 1392 missing declarations in already represented type families; split each type by its documented dependency trigger. Start with the independent [FastNoiseLite](classes/FastNoiseLite.md), [Geometry2D](classes/Geometry2D.md), [JSON](classes/JSON.md), [Line2D](classes/Line2D.md), [Marker2D](classes/Marker2D.md), [Noise](classes/Noise.md), [OptimizedTranslation](classes/OptimizedTranslation.md), [Parallax2D](classes/Parallax2D.md), [ParallaxBackground](classes/ParallaxBackground.md), [ParallaxLayer](classes/ParallaxLayer.md), [Polygon2D](classes/Polygon2D.md), [RegEx](classes/RegEx.md), [RegExMatch](classes/RegExMatch.md), [Translation](classes/Translation.md), [TranslationDomain](classes/TranslationDomain.md), [XMLParser](classes/XMLParser.md) class slices.
3. Complete the missing 2D renderer integrations, then GUI/theme and tiles; Box2D.NET physics; audio/navigation/animation; asset loaders and networking; and the self-hosted editor. Finish specific display/input host gaps at their documented triggers. The first executable GL/EGL/GLX fallback slice must audit each of the five blocked `DisplayServer.HandleType` identities against its actual driver and window-associated context under ADR 0042.

## Existing type backlog

These classes already have an Electron2D type. Sort by missing member count, then unaudited mapped count; this is workload order, not a claim that dependencies can be skipped.

| Godot class | Unimplemented members | Partial members |
| --- | ---: | ---: |
| [RenderingServer](classes/RenderingServer.md) | 565 | 7 |
| [Control](classes/Control.md) | 183 | 24 |
| [Node](classes/Node.md) | 97 | 66 |
| [Input](classes/Input.md) | 61 | 26 |
| [Window](classes/Window.md) | 44 | 34 |
| [Object](classes/Object.md) | 34 | 22 |
| [TranslationServer](classes/TranslationServer.md) | 27 | 6 |
| [ProjectSettings](classes/ProjectSettings.md) | 22 | 27 |
| [SceneTree](classes/SceneTree.md) | 16 | 21 |
| [Engine](classes/Engine.md) | 16 | 18 |
| [Image](classes/Image.md) | 13 | 69 |
| [SceneState](classes/SceneState.md) | 12 | 16 |
| [Viewport](classes/Viewport.md) | 5 | 12 |
| [FileAccess](classes/FileAccess.md) | 4 | 66 |
| [Resource](classes/Resource.md) | 4 | 21 |
| [Color](classes/Color.md) | 3 | 203 |
| [Material](classes/Material.md) | 3 | 0 |
| [Vector4](classes/Vector4.md) | 2 | 59 |
| [InputMap](classes/InputMap.md) | 2 | 13 |
| [DirAccess](classes/DirAccess.md) | 1 | 39 |
| [Texture2D](classes/Texture.md#godot-texture2d) | 1 | 22 |
| [InputEventWithModifiers](classes/InputEventWithModifiers.md) | 1 | 7 |
| [InputEventMouse](classes/InputEventMouse.md) | 1 | 3 |
| [InputEventGesture](classes/InputEventGesture.md) | 1 | 1 |
| [Vector3](classes/Vector3.md) | 0 | 81 |
| [Vector3i](classes/Vector3i.md) | 0 | 53 |
| [Vector4i](classes/Vector4i.md) | 0 | 44 |
| [CanvasItem](classes/CanvasItem.md) | 0 | 36 |
| [Tween](classes/Tween.md) | 0 | 35 |
| [Rect2](classes/Rect2.md) | 0 | 27 |
| [Node2D](classes/Node2D.md) | 0 | 23 |
| [Rect2i](classes/Rect2i.md) | 0 | 23 |
| [ConfigFile](classes/ConfigFile.md) | 0 | 17 |
| [InputEvent](classes/InputEvent.md) | 0 | 14 |
| [InputEventKey](classes/InputEventKey.md) | 0 | 14 |
| [Timer](classes/Timer.md) | 0 | 12 |
| [InputEventScreenDrag](classes/InputEventScreenDrag.md) | 0 | 9 |
| [DisplayServer](classes/DisplayServer.md) | 0 | 7 |
| [InputEventMouseMotion](classes/InputEventMouseMotion.md) | 0 | 7 |
| [PropertyTweener](classes/PropertyTweener.md) | 0 | 7 |
| [Shader](classes/Shader.md) | 0 | 6 |
| [ImageTexture](classes/ImageTexture.md) | 0 | 5 |
| [InputEventMouseButton](classes/InputEventMouseButton.md) | 0 | 5 |
| [InputEventScreenTouch](classes/InputEventScreenTouch.md) | 0 | 5 |
| [MainLoop](classes/MainLoop.md) | 0 | 5 |
| [PackedScene](classes/PackedScene.md) | 0 | 5 |
| [InputEventAction](classes/InputEventAction.md) | 0 | 4 |
| [InputEventJoypadButton](classes/InputEventJoypadButton.md) | 0 | 3 |
| [MethodTweener](classes/MethodTweener.md) | 0 | 3 |
| [ShaderMaterial](classes/ShaderMaterial.md) | 0 | 3 |
| [Camera2D](classes/Camera2D.md) | 0 | 2 |
| [InputEventJoypadMotion](classes/InputEventJoypadMotion.md) | 0 | 2 |
| [SceneTreeTimer](classes/SceneTreeTimer.md) | 0 | 2 |
| [AwaitTweener](classes/AwaitTweener.md) | 0 | 1 |
| [CallbackTweener](classes/CallbackTweener.md) | 0 | 1 |
| [CanvasLayer](classes/CanvasLayer.md) | 0 | 1 |
| [InputEventFromWindow](classes/InputEventFromWindow.md) | 0 | 1 |
| [InputEventMagnifyGesture](classes/InputEventMagnifyGesture.md) | 0 | 1 |
| [InputEventPanGesture](classes/InputEventPanGesture.md) | 0 | 1 |
| [SubtweenTweener](classes/SubtweenTweener.md) | 0 | 1 |
| [Tweener](classes/Tweener.md) | 0 | 1 |

## Blocked type families

| Exact trigger | Classes |
| --- | ---: |
| Trigger: first typed 2D visual-shader graph translation and shader-import slice (ADR 0028). | 92 |
| GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). | 88 |
| Trigger: first self-hosted editor executable slice under ADR 0027. | 65 |
| Audio: trigger is the first audio mixing and playback slice. | 56 |
| Networking: trigger is the first networking and multiplayer slice. | 41 |
| Physics2D: trigger is the first Box2D.NET-backed 2D physics slice (ADR 0012). | 39 |
| Animation: trigger is the first scene animation slice. | 28 |
| Assets: trigger is the first concrete loader and native-backed asset slice (ADR 0013/0023). | 13 |
| Navigation2D: trigger is the first 2D navigation slice. | 12 |
| Trigger: first typed asset loader, scene-file format and import slice after a concrete format is selected (ADRs 0013 and 0023). | 11 |
| Trigger: an accepted typed scripting or extension-host contract and its first executable slice (ADR 0001). | 10 |
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
| Trigger: first 2D light/mesh texture renderer integration (ADR 0028). | 2 |
| Trigger: first 2D offscreen composition and framebuffer-copy slice (ADR 0028). | 2 |
| Trigger: first 2D world/render-environment integration slice after SDL3 GPU rendering (ADRs 0008 and 0028). | 2 |
| Trigger: first audio decoding and playback slice. | 2 |
| Trigger: first independent offscreen viewport lifecycle and texture-output slice (ADRs 0008 and 0028). | 2 |
| Trigger: first missing 2D material, canvas-modulation and shader-global renderer integration (ADR 0028). | 2 |
| Trigger: first retained-canvas visibility tracking and notification slice (ADR 0028). | 2 |
| Trigger: first shader include import and dependency-tracking slice (ADR 0028). | 2 |
| Trigger: first typed GUI/editor Shortcut ownership and focus-routing slice (ADR 0038). | 2 |
| Trigger: first typed missing-asset placeholder and loader slice (ADRs 0013 and 0023). | 2 |
| Trigger: first typed networking, address-resolution and RPC slice. | 2 |
| Trigger: first typed packed-asset container and loader slice (ADRs 0013 and 0023). | 2 |
| Trigger: first writable GPU texture and blit-command lifetime slice (ADR 0028). | 2 |
| Trigger: accepted MIDI-domain and native host-API decision, then the first MIDI device/event slice (ADR 0038). | 1 |
| Trigger: an accepted public weak-reference contract beyond System.WeakReference<T>; Resource currently uses only an internal weak path cache (ADR 0013). | 1 |
| Trigger: complete the Noise resource and first noise-texture rebaking slice (ADR 0013). | 1 |
| Trigger: first backend-neutral 2D renderer resource-identity and lifetime slice (ADR 0028). | 1 |
| Trigger: first native-menu service slice with ownership, callbacks and target checks (ADR 0041). | 1 |
| Trigger: first portable external-image ownership and native texture-import decision (ADRs 0021 and 0028). | 1 |
| Trigger: first semantic accessibility-tree, focus and native screen-reader bridge slice (ADR 0041). | 1 |
| Trigger: first typed 2D navigation and pathfinding slice. | 1 |
| Trigger: first typed GUI DPI-scale and theme-texture slice (ADR 0028). | 1 |
| Trigger: first typed multiplayer replication slice after scene persistence (ADR 0023). | 1 |
| Trigger: first typed rich-text effect slice after 2D GUI and text rendering (ADR 0028). | 1 |
| Separate product-scope decision for each of 1 currently unassigned families; see their catalog pages for exact names. | 1 |

Each [catalog entry](catalog.md) opens the complete member table. Excluded rows have an accepted product reason and no implementation task.
