# Coverage roadmap

Last updated: 2026-09-22

The order follows concrete dependencies. `Partial` rows need either a semantic audit or resolution of a documented behavior gap; `Unmapped` Electron2D rows need an exact upstream link or a documented typed-C# rationale. The 3D/GDScript exclusions are not delivery work.

1. Review 1553 partially implemented rows and 0 unmapped Electron2D declarations, beginning with the existing core, input, scene, resource and image domains.
2. Complete 676 missing declarations in already represented type families; split each type by its documented dependency trigger. Start with the independent [BitMap](classes/BitMap.md), [Curve](classes/Curve.md), [Curve2D](classes/Curve2D.md), [FastNoiseLite](classes/FastNoiseLite.md), [Geometry2D](classes/Geometry2D.md), [JSON](classes/JSON.md), [Noise](classes/Noise.md), [OptimizedTranslation](classes/OptimizedTranslation.md), [RandomNumberGenerator](classes/RandomNumberGenerator.md), [RegEx](classes/RegEx.md), [RegExMatch](classes/RegExMatch.md), [Translation](classes/Translation.md), [TranslationDomain](classes/TranslationDomain.md), [XMLParser](classes/XMLParser.md) class slices.
3. Implement the blocked domains in dependency order: SDL host/input and display; SDL3 GPU 2D rendering; GUI/theme and tiles; Box2D.NET physics; audio/navigation/animation; asset loaders and networking; self-hosted editor.

## Existing type backlog

These classes already have an Electron2D type. Sort by missing member count, then unaudited mapped count; this is workload order, not a claim that dependencies can be skipped.

| Godot class | Unimplemented members | Partial members |
| --- | ---: | ---: |
| [Node](classes/Node.md) | 101 | 64 |
| [Input](classes/Input.md) | 65 | 22 |
| [CanvasItem](classes/CanvasItem.md) | 43 | 15 |
| [Object](classes/Object.md) | 34 | 22 |
| [TranslationServer](classes/TranslationServer.md) | 27 | 6 |
| [Image](classes/Image.md) | 26 | 58 |
| [ProjectSettings](classes/ProjectSettings.md) | 22 | 24 |
| [SceneTree](classes/SceneTree.md) | 21 | 21 |
| [Engine](classes/Engine.md) | 17 | 17 |
| [SceneState](classes/SceneState.md) | 12 | 16 |
| [FileAccess](classes/FileAccess.md) | 4 | 66 |
| [Resource](classes/Resource.md) | 4 | 21 |
| [Color](classes/Color.md) | 3 | 203 |
| [Transform2D](classes/Transform2D.md) | 3 | 40 |
| [Vector2](classes/Vector2.md) | 2 | 78 |
| [Vector4](classes/Vector4.md) | 2 | 59 |
| [InputMap](classes/InputMap.md) | 2 | 13 |
| [DirAccess](classes/DirAccess.md) | 1 | 39 |
| [InputEventWithModifiers](classes/InputEventWithModifiers.md) | 1 | 7 |
| [InputEventMouse](classes/InputEventMouse.md) | 1 | 3 |
| [InputEventGesture](classes/InputEventGesture.md) | 1 | 1 |
| [Vector4i](classes/Vector4i.md) | 0 | 44 |
| [Tween](classes/Tween.md) | 0 | 35 |
| [Rect2](classes/Rect2.md) | 0 | 27 |
| [Node2D](classes/Node2D.md) | 0 | 23 |
| [Rect2i](classes/Rect2i.md) | 0 | 23 |
| [ConfigFile](classes/ConfigFile.md) | 0 | 17 |
| [InputEvent](classes/InputEvent.md) | 0 | 14 |
| [InputEventKey](classes/InputEventKey.md) | 0 | 14 |
| [Timer](classes/Timer.md) | 0 | 12 |
| [InputEventScreenDrag](classes/InputEventScreenDrag.md) | 0 | 9 |
| [InputEventMouseMotion](classes/InputEventMouseMotion.md) | 0 | 7 |
| [PropertyTweener](classes/PropertyTweener.md) | 0 | 7 |
| [InputEventMouseButton](classes/InputEventMouseButton.md) | 0 | 5 |
| [InputEventScreenTouch](classes/InputEventScreenTouch.md) | 0 | 5 |
| [MainLoop](classes/MainLoop.md) | 0 | 5 |
| [PackedScene](classes/PackedScene.md) | 0 | 5 |
| [InputEventAction](classes/InputEventAction.md) | 0 | 4 |
| [InputEventJoypadButton](classes/InputEventJoypadButton.md) | 0 | 3 |
| [MethodTweener](classes/MethodTweener.md) | 0 | 3 |
| [InputEventJoypadMotion](classes/InputEventJoypadMotion.md) | 0 | 2 |
| [SceneTreeTimer](classes/SceneTreeTimer.md) | 0 | 2 |
| [AwaitTweener](classes/AwaitTweener.md) | 0 | 1 |
| [CallbackTweener](classes/CallbackTweener.md) | 0 | 1 |
| [InputEventFromWindow](classes/InputEventFromWindow.md) | 0 | 1 |
| [InputEventMagnifyGesture](classes/InputEventMagnifyGesture.md) | 0 | 1 |
| [InputEventPanGesture](classes/InputEventPanGesture.md) | 0 | 1 |
| [SubtweenTweener](classes/SubtweenTweener.md) | 0 | 1 |
| [Tweener](classes/Tweener.md) | 0 | 1 |

## Blocked type families

| Exact trigger | Classes |
| --- | ---: |
| Rendering2D: trigger is the first SDL3 GPU 2D rendering slice (ADR 0028). | 156 |
| GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). | 150 |
| Trigger: first self-hosted editor executable slice under ADR 0027. | 79 |
| Audio: trigger is the first audio mixing and playback slice. | 56 |
| Networking: trigger is the first networking and multiplayer slice. | 41 |
| Physics2D: trigger is the first Box2D.NET-backed 2D physics slice (ADR 0012). | 41 |
| Animation: trigger is the first scene animation slice. | 26 |
| Navigation2D: trigger is the first 2D navigation slice. | 15 |
| Trigger: a concrete backend-neutral 2D compositing contract in the SDL3 GPU renderer; exclude direct RD members under ADR 0028. | 12 |
| Assets: trigger is the first concrete loader and native-backed asset slice (ADR 0013/0023). | 11 |
| Trigger: first typed asset loader, scene-file format and import slice after a concrete format is selected (ADRs 0013 and 0023). | 11 |
| Trigger: an accepted typed scripting or extension-host contract and its first executable slice (ADR 0001). | 9 |
| Trigger: first 2D skeletal animation and inverse-kinematics slice. | 9 |
| Tiles: trigger is the first tile and atlas resource slice after 2D rendering. | 8 |
| Trigger: first SDL-backed host, profiling, logging or capture integration slice with target capability reporting (ADRs 0015, 0016 and 0021). | 8 |
| Trigger: first typed 2D mesh-data and MeshInstance2D rendering slice; audit 3D-only members individually (ADR 0028). | 8 |
| Trigger: first Android or Web host-interoperability slice after the portable SDL host (ADR 0021). | 6 |
| Host: trigger is the first SDL-backed platform host and display slice (ADR 0021/0038). | 5 |
| Trigger: first self-hosted editor and typed GUI authoring slice (ADRs 0027 and 0028). | 5 |
| Trigger: a typed engine job-system decision with ownership, cancellation and target threading guarantees (ADRs 0001 and 0021). | 4 |
| InputHost: trigger is the first SDL input-host integration slice (ADR 0038). | 3 |
| Trigger: first typed networking-security integration slice with a portable crypto backend (ADR 0021). | 3 |
| Trigger: first 2D world/render-environment integration slice after SDL3 GPU rendering (ADRs 0008 and 0028). | 2 |
| Trigger: first audio decoding and playback slice. | 2 |
| Trigger: first typed networking, address-resolution and RPC slice. | 2 |
| Trigger: an accepted public weak-reference contract beyond System.WeakReference<T>; Resource currently uses only an internal weak path cache (ADR 0013). | 1 |
| Trigger: first backend-neutral 2D renderer resource-identity and lifetime slice (ADR 0028). | 1 |
| Trigger: first typed 2D navigation and pathfinding slice. | 1 |
| Trigger: first typed multiplayer replication slice after scene persistence (ADR 0023). | 1 |
| Trigger: first typed rich-text effect slice after 2D GUI and text rendering (ADR 0028). | 1 |
| Separate product-scope decision for each of 2 currently unassigned families; see their catalog pages for exact names. | 2 |

Each [catalog entry](catalog.md) opens the complete member table. Excluded rows have an accepted product reason and no implementation task.
