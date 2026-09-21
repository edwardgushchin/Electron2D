# Engine inventory

Last updated: 2026-09-21

This is the exhaustive inventory of implemented Electron2D engine domains, components, and production types. Test-only helpers are not engine types.

Electron2D is 2D-only, and its runtime targets Linux, Windows, macOS, Android, and iOS under ADR 0021. All currently implemented production rows in this inventory are runtime types belonging to `Electron2D.dll`. Accepted external runtime dependencies may ship as separate assemblies under ADR 0012. The future first-party editor is a separate executable consumer under ADR 0027 and has no implemented production rows yet.

| Domain | Component | Production type | Source | Documentation | State |
| --- | --- | --- | --- | --- | --- |
| [Core](domains/core.md) | [Object lifecycle](components/object-lifecycle.md) | [`ElectronObject`](classes/ElectronObject.md) | [`ElectronObject.cs`](../src/Core/Object/ElectronObject.cs) | Current | Implemented and verified |
| [Core](domains/core.md) | [Typed event connections](components/event-connections.md) | [`EventConnection`](classes/EventConnection.md) | [`EventConnection.cs`](../src/Core/Object/EventConnection.cs) | Current | Implemented and verified |
| [Core](domains/core.md) | [Typed editor properties](components/editor-properties.md) | [`PropertyDescriptor`](classes/PropertyDescriptor.md) | [`PropertyDescriptor.cs`](../src/Core/Object/PropertyDescriptor.cs) | Current | Implemented and verified |
| [Core](domains/core.md) | [Typed editor properties](components/editor-properties.md) | [`PropertyDescriptor<TOwner, TValue>`](classes/PropertyDescriptor.Generic.md) | [`PropertyDescriptor.cs`](../src/Core/Object/PropertyDescriptor.cs) | Current | Implemented and verified |
| [Core](domains/core.md) | [Configuration files](components/config-files.md) | [`ConfigKey<T>`](classes/ConfigKey.Generic.md) | [`ConfigFile.cs`](../src/Core/IO/ConfigFile.cs) | Current | Implemented and verified |
| [Core](domains/core.md) | [Configuration files](components/config-files.md) | [`ConfigFile`](classes/ConfigFile.md) | [`ConfigFile.cs`](../src/Core/IO/ConfigFile.cs) | Current | Implemented and verified |
| [Core](domains/core.md) | [File and directory access](components/file-access.md) | [`FileAccess`](classes/FileAccess.md) | [`FileAccess.cs`](../src/Core/IO/FileAccess.cs) | Current | Implemented and verified |
| [Core](domains/core.md) | [File and directory access](components/file-access.md) | [`DirAccess`](classes/DirAccess.md) | [`DirAccess.cs`](../src/Core/IO/DirAccess.cs) | Current | Implemented and verified on Linux; documented mobile host gaps |
| [Core](domains/core.md) | [File and directory access](components/file-access.md) | [`FileAccessMode`](classes/FileAccessMode.md) | [`FileAccessMode.cs`](../src/Core/IO/FileAccessMode.cs) | Current | Implemented and verified |
| [Core](domains/core.md) | [File and directory access](components/file-access.md) | [`FileCompressionMode`](classes/FileCompressionMode.md) | [`FileCompressionMode.cs`](../src/Core/IO/FileCompressionMode.cs) | Current | Implemented and verified, with two explicit codec gaps |
| [Core](domains/core.md) | [File and directory access](components/file-access.md) | [`UnixPermissionFlags`](classes/UnixPermissionFlags.md) | [`UnixPermissionFlags.cs`](../src/Core/IO/UnixPermissionFlags.cs) | Current | Implemented and verified on Linux |
| [Core](domains/core.md) | [Project settings](components/project-settings.md) | [`ProjectSetting<T>`](classes/ProjectSetting.Generic.md) | [`ProjectSettings.cs`](../src/Core/Config/ProjectSettings.cs) | Current | Implemented and verified |
| [Core](domains/core.md) | [Project settings](components/project-settings.md) | [`ProjectSettings`](classes/ProjectSettings.md) | [`ProjectSettings.cs`](../src/Core/Config/ProjectSettings.cs) | Current | Implemented and verified |
| [Core](domains/core.md) | [Main loop](components/main-loop.md) | [`MainLoop`](classes/MainLoop.md) | [`MainLoop.cs`](../src/Core/OS/MainLoop.cs) | Current | Implemented and verified |
| [Core](domains/core.md) | [Engine runtime](components/engine-runtime.md) | [`Engine`](classes/Engine.md) | [`Engine.cs`](../src/Core/Config/Engine.cs) | Current | Implemented and verified |
| [Core](domains/core.md) | [Engine runtime](components/engine-runtime.md) | [`EngineVersionInfo`](classes/EngineVersionInfo.md) | [`EngineVersionInfo.cs`](../src/Core/Config/EngineVersionInfo.cs) | Current | Implemented and verified |
| [Input](domains/input.md) | [Input runtime](components/input-runtime.md) | [`Input`](classes/Input.md) | [`Input.cs`](../src/Core/Input/Input.cs) | Current | Implemented and verified; native-host gaps have exact triggers |
| [Input](domains/input.md) | [Input runtime](components/input-runtime.md) | [`InputMap`](classes/InputMap.md) | [`InputMap.cs`](../src/Core/Input/InputMap.cs) | Current | Implemented and verified |
| [Input](domains/input.md) | [Input runtime](components/input-runtime.md) | [`InputEvent`](classes/InputEvent.md) | [`InputEvent.cs`](../src/Core/Input/InputEvent.cs) | Current | Implemented and verified |
| [Input](domains/input.md) | [Input runtime](components/input-runtime.md) | [`InputEventFromWindow`](classes/InputEventFromWindow.md) | [`InputEvent.cs`](../src/Core/Input/InputEvent.cs) | Current | Implemented and verified |
| [Input](domains/input.md) | [Input runtime](components/input-runtime.md) | [`InputEventWithModifiers`](classes/InputEventWithModifiers.md) | [`InputEvent.cs`](../src/Core/Input/InputEvent.cs) | Current | Implemented and verified |
| [Input](domains/input.md) | [Input runtime](components/input-runtime.md) | [`InputEventAction`](classes/InputEventAction.md) | [`InputEventAction.cs`](../src/Core/Input/InputEventAction.cs) | Current | Implemented and verified |
| [Input](domains/input.md) | [Input runtime](components/input-runtime.md) | [`InputEventKey`](classes/InputEventKey.md) | [`InputEventKey.cs`](../src/Core/Input/InputEventKey.cs) | Current | Implemented and verified |
| [Input](domains/input.md) | [Input runtime](components/input-runtime.md) | [`InputEventMouse`](classes/InputEventMouse.md) | [`InputEventMouse.cs`](../src/Core/Input/InputEventMouse.cs) | Current | Implemented and verified |
| [Input](domains/input.md) | [Input runtime](components/input-runtime.md) | [`InputEventMouseButton`](classes/InputEventMouseButton.md) | [`InputEventMouse.cs`](../src/Core/Input/InputEventMouse.cs) | Current | Implemented and verified |
| [Input](domains/input.md) | [Input runtime](components/input-runtime.md) | [`InputEventMouseMotion`](classes/InputEventMouseMotion.md) | [`InputEventMouse.cs`](../src/Core/Input/InputEventMouse.cs) | Current | Implemented and verified |
| [Input](domains/input.md) | [Input runtime](components/input-runtime.md) | [`InputEventJoypadButton`](classes/InputEventJoypadButton.md) | [`InputEventJoypad.cs`](../src/Core/Input/InputEventJoypad.cs) | Current | Implemented and verified |
| [Input](domains/input.md) | [Input runtime](components/input-runtime.md) | [`InputEventJoypadMotion`](classes/InputEventJoypadMotion.md) | [`InputEventJoypad.cs`](../src/Core/Input/InputEventJoypad.cs) | Current | Implemented and verified |
| [Input](domains/input.md) | [Input runtime](components/input-runtime.md) | [`InputEventScreenTouch`](classes/InputEventScreenTouch.md) | [`InputEventTouch.cs`](../src/Core/Input/InputEventTouch.cs) | Current | Implemented and verified |
| [Input](domains/input.md) | [Input runtime](components/input-runtime.md) | [`InputEventScreenDrag`](classes/InputEventScreenDrag.md) | [`InputEventTouch.cs`](../src/Core/Input/InputEventTouch.cs) | Current | Implemented and verified |
| [Input](domains/input.md) | [Input runtime](components/input-runtime.md) | [`InputEventGesture`](classes/InputEventGesture.md) | [`InputEventTouch.cs`](../src/Core/Input/InputEventTouch.cs) | Current | Implemented and verified |
| [Input](domains/input.md) | [Input runtime](components/input-runtime.md) | [`InputEventMagnifyGesture`](classes/InputEventMagnifyGesture.md) | [`InputEventTouch.cs`](../src/Core/Input/InputEventTouch.cs) | Current | Implemented and verified |
| [Input](domains/input.md) | [Input runtime](components/input-runtime.md) | [`InputEventPanGesture`](classes/InputEventPanGesture.md) | [`InputEventTouch.cs`](../src/Core/Input/InputEventTouch.cs) | Current | Implemented and verified |
| [Input](domains/input.md) | [Input runtime](components/input-runtime.md) | [`Key`](classes/Key.md) | [`InputEnums.cs`](../src/Core/Input/InputEnums.cs) | Current | Implemented and verified |
| [Input](domains/input.md) | [Input runtime](components/input-runtime.md) | [`KeyModifierMask`](classes/KeyModifierMask.md) | [`InputEnums.cs`](../src/Core/Input/InputEnums.cs) | Current | Implemented and verified |
| [Input](domains/input.md) | [Input runtime](components/input-runtime.md) | [`KeyLocation`](classes/KeyLocation.md) | [`InputEnums.cs`](../src/Core/Input/InputEnums.cs) | Current | Implemented and verified |
| [Input](domains/input.md) | [Input runtime](components/input-runtime.md) | [`MouseButton`](classes/MouseButton.md) | [`InputEnums.cs`](../src/Core/Input/InputEnums.cs) | Current | Implemented and verified |
| [Input](domains/input.md) | [Input runtime](components/input-runtime.md) | [`MouseButtonMask`](classes/MouseButtonMask.md) | [`InputEnums.cs`](../src/Core/Input/InputEnums.cs) | Current | Implemented and verified |
| [Input](domains/input.md) | [Input runtime](components/input-runtime.md) | [`JoyAxis`](classes/JoyAxis.md) | [`InputEnums.cs`](../src/Core/Input/InputEnums.cs) | Current | Implemented and verified |
| [Input](domains/input.md) | [Input runtime](components/input-runtime.md) | [`JoyButton`](classes/JoyButton.md) | [`InputEnums.cs`](../src/Core/Input/InputEnums.cs) | Current | Implemented and verified |
| [Core](domains/core.md) | [Scalar math](components/scalar-math.md) | [`Mathf`](classes/Mathf.md) | [`Mathf.cs`](../src/Core/Math/Mathf.cs) | Current | Implemented and verified |
| [Core](domains/core.md) | [Color values](components/color-values.md) | [`Color`](classes/Color.md) | [`Color.cs`](../src/Core/Math/Color.cs) | Current | Implemented and verified |
| [Core](domains/core.md) | [Color values](components/color-values.md) | [`Colors`](classes/Colors.md) | [`Colors.cs`](../src/Core/Math/Colors.cs) | Current | Implemented and verified |
| [Core](domains/core.md) | [Geometry values](components/geometry-values.md) | [`Vector2`](classes/Vector2.md) | [`Vector2.cs`](../src/Core/Math/Vector2.cs) | Current | Implemented and verified |
| [Core](domains/core.md) | [Geometry values](components/geometry-values.md) | [`Vector2I`](classes/Vector2I.md) | [`Vector2I.cs`](../src/Core/Math/Vector2I.cs) | Current | Implemented and verified |
| [Core](domains/core.md) | [Geometry values](components/geometry-values.md) | [`Vector4`](classes/Vector4.md) | [`Vector4.cs`](../src/Core/Math/Vector4.cs) | Current | Implemented and verified |
| [Core](domains/core.md) | [Geometry values](components/geometry-values.md) | [`Vector4I`](classes/Vector4I.md) | [`Vector4I.cs`](../src/Core/Math/Vector4I.cs) | Current | Implemented and verified |
| [Core](domains/core.md) | [Geometry values](components/geometry-values.md) | [`Rect`](classes/Rect.md) | [`Rect.cs`](../src/Core/Math/Rect.cs) | Current | Implemented and verified |
| [Core](domains/core.md) | [Geometry values](components/geometry-values.md) | [`RectI`](classes/RectI.md) | [`RectI.cs`](../src/Core/Math/RectI.cs) | Current | Implemented and verified |
| [Core](domains/core.md) | [Geometry values](components/geometry-values.md) | [`Transform`](classes/Transform.md) | [`Transform.cs`](../src/Core/Math/Transform.cs) | Current | Implemented and verified |
| [Core](domains/core.md) | [Geometry values](components/geometry-values.md) | [`Side`](classes/Side.md) | [`Side.cs`](../src/Core/Math/Side.cs) | Current | Implemented and verified |
| [Scene](domains/scene.md) | [Unified 2D node](components/unified-node.md) | [`Node`](classes/Node.md) | [`Node.cs`](../src/Scene/Main/Node.cs) | Current | Implemented and verified |
| [Scene](domains/scene.md) | [Unified 2D node](components/unified-node.md) | [`NodeProcessMode`](classes/NodeProcessMode.md) | [`NodeProcessMode.cs`](../src/Scene/Main/NodeProcessMode.cs) | Current | Implemented and verified |
| [Scene](domains/scene.md) | [Scene tree](components/scene-tree.md) | [`SceneTree`](classes/SceneTree.md) | [`SceneTree.cs`](../src/Scene/Main/SceneTree.cs) | Current | Implemented and verified |
| [Scene](domains/scene.md) | [Scene tree](components/scene-tree.md) | [`Timer`](classes/Timer.md) | [`Timer.cs`](../src/Scene/Main/Timer.cs) | Current | Implemented and verified |
| [Scene](domains/scene.md) | [Scene tree](components/scene-tree.md) | [`TimerProcessCallback`](classes/TimerProcessCallback.md) | [`TimerProcessCallback.cs`](../src/Scene/Main/TimerProcessCallback.cs) | Current | Implemented and verified |
| [Scene](domains/scene.md) | [Scene tree](components/scene-tree.md) | [`SceneTreeTimer`](classes/SceneTreeTimer.md) | [`SceneTreeTimer.cs`](../src/Scene/Main/SceneTreeTimer.cs) | Current | Implemented and verified |
| [Scene](domains/scene.md) | [Scene tree](components/scene-tree.md) | [`GroupCallFlags`](classes/GroupCallFlags.md) | [`GroupCallFlags.cs`](../src/Scene/Main/GroupCallFlags.cs) | Current | Implemented and verified |
| [Scene](domains/scene.md) | [Tweening](components/tweening.md) | [`Tween`](classes/Tween.md) | [`Tween.cs`](../src/Scene/Animation/Tween.cs) | Current | Implemented and verified |
| [Scene](domains/scene.md) | [Tweening](components/tweening.md) | [`Tween.TweenProcessMode`](classes/Tween.TweenProcessMode.md) | [`Tween.cs`](../src/Scene/Animation/Tween.cs) | Current | Implemented and verified |
| [Scene](domains/scene.md) | [Tweening](components/tweening.md) | [`Tween.TweenPauseMode`](classes/Tween.TweenPauseMode.md) | [`Tween.cs`](../src/Scene/Animation/Tween.cs) | Current | Implemented and verified |
| [Scene](domains/scene.md) | [Tweening](components/tweening.md) | [`Tween.TransitionType`](classes/Tween.TransitionType.md) | [`Tween.cs`](../src/Scene/Animation/Tween.cs) | Current | Implemented and verified |
| [Scene](domains/scene.md) | [Tweening](components/tweening.md) | [`Tween.EaseType`](classes/Tween.EaseType.md) | [`Tween.cs`](../src/Scene/Animation/Tween.cs) | Current | Implemented and verified |
| [Scene](domains/scene.md) | [Tweening](components/tweening.md) | [`Tweener`](classes/Tweener.md) | [`Tweeners.cs`](../src/Scene/Animation/Tweeners.cs) | Current | Implemented and verified |
| [Scene](domains/scene.md) | [Tweening](components/tweening.md) | [`PropertyTweener<TValue>`](classes/PropertyTweener.Generic.md) | [`Tweeners.cs`](../src/Scene/Animation/Tweeners.cs) | Current | Implemented and verified |
| [Scene](domains/scene.md) | [Tweening](components/tweening.md) | [`MethodTweener<TValue>`](classes/MethodTweener.Generic.md) | [`Tweeners.cs`](../src/Scene/Animation/Tweeners.cs) | Current | Implemented and verified |
| [Scene](domains/scene.md) | [Tweening](components/tweening.md) | [`CallbackTweener`](classes/CallbackTweener.md) | [`Tweeners.cs`](../src/Scene/Animation/Tweeners.cs) | Current | Implemented and verified |
| [Scene](domains/scene.md) | [Tweening](components/tweening.md) | [`IntervalTweener`](classes/IntervalTweener.md) | [`Tweeners.cs`](../src/Scene/Animation/Tweeners.cs) | Current | Implemented and verified |
| [Scene](domains/scene.md) | [Tweening](components/tweening.md) | [`SubtweenTweener`](classes/SubtweenTweener.md) | [`Tweeners.cs`](../src/Scene/Animation/Tweeners.cs) | Current | Implemented and verified |
| [Scene](domains/scene.md) | [Tweening](components/tweening.md) | [`AwaitTweener`](classes/AwaitTweener.md) | [`Tweeners.cs`](../src/Scene/Animation/Tweeners.cs) | Current | Implemented and verified |
| [Scene](domains/scene.md) | [Packed scenes](components/packed-scenes.md) | [`PackedScene`](classes/PackedScene.md) | [`PackedScene.cs`](../src/Scene/Resources/PackedScene.cs) | Current | Implemented and verified |
| [Scene](domains/scene.md) | [Packed scenes](components/packed-scenes.md) | [`SceneState`](classes/SceneState.md) | [`SceneState.cs`](../src/Scene/Resources/SceneState.cs) | Current | Implemented and verified |
| [Scene](domains/scene.md) | [Packed scenes](components/packed-scenes.md) | [`PackedSceneEditState`](classes/PackedSceneEditState.md) | [`PackedSceneEditState.cs`](../src/Scene/Resources/PackedSceneEditState.cs) | Current | Implemented and verified |
| [Localization](domains/localization.md) | [Translation](components/localization.md) | [`TranslationServer`](classes/TranslationServer.md) | [`TranslationServer.cs`](../src/Core/String/TranslationServer.cs) | Current | Implemented and verified |
| [Resources](domains/resources.md) | [Resource base](components/resources.md) | [`Resource`](classes/Resource.md) | [`Resource.cs`](../src/Core/IO/Resource.cs) | Current | Implemented and verified |
| [Resources](domains/resources.md) | [Resource base](components/resources.md) | [`DeepDuplicateMode`](classes/DeepDuplicateMode.md) | [`DeepDuplicateMode.cs`](../src/Core/IO/DeepDuplicateMode.cs) | Current | Implemented and verified |

## Assembly inventory

| Assembly | Project | Target | State |
| --- | --- | --- | --- |
| `Electron2D.dll` | [`Electron2D.csproj`](../Electron2D.csproj) | `net8.0` | The public engine assembly and only assembly produced by the current build; builds in Release with zero warnings |

This assembly row records the current build, not complete platform delivery. The executable harness is currently native-host verified on Linux; a five-platform build/package/test matrix and Android/iOS host projects do not exist yet.

## Repository product boundaries

| Product layer | Source root | Dependency direction | Current state |
| --- | --- | --- | --- |
| Runtime engine | [`src/`](../src/) and [`Electron2D.csproj`](../Electron2D.csproj) | May use only approved runtime dependencies | Implemented types compile into `Electron2D.dll` |
| Self-hosted editor | [`editor/Electron2D.Editor/`](../editor/Electron2D.Editor/) | Future executable references `Electron2D.dll`; runtime never references editor | Directory boundary reserved; no project or source implemented |
| First-party games/examples | [`examples/`](../examples/) | Each future executable references `Electron2D.dll`; runtime never references games | Directory boundary reserved; no game project implemented |

## Selected but not integrated dependencies

| Dependency | Intended role | Packaging | Current state |
| --- | --- | --- | --- |
| `Box2D.NET` | Future 2D collision and rigid-body backend | Separate managed runtime assembly referenced by `Electron2D.dll` | Approved by ADR 0012; no package reference, physics code, or shipping artifact exists yet |

## Explicitly absent

There are currently no production types for packed/exported virtual filesystems, import remapping, resource-UID resolution, platform pipes, 2D rendering, shaders, SDL resources/hosting, native input devices/pumping, audio, collision/rigid-body physics, concrete assets, asset loading/saving, scene file loading/saving, scripting, logging, movie writing, an editor application, or networking. Managed typed input events, action mapping/state, and SceneTree propagation are implemented; cursor/window control, buffering/emulation, controller discovery/effects, sensors, MIDI, shortcuts, action persistence, and GUI/viewport routing remain absent under the exact triggers in ADR 0038. The complete implemented engine-owned vector family is `Vector2`, `Vector2I`, `Vector4`, and `Vector4I`; the rectangle family is `Rect` and `RectI` with typed conversions and persistence; `Rect`, `Transform`, and `Node` use `Vector2`, and floating rectangle transform operators are implemented. `Vector3`, `Vector3I`, and every 3D scene type remain intentionally absent. Reusable `Timer` nodes, lightweight `SceneTreeTimer` delays, and typed `Tween` sequences all use the host-driven frame schedule; none owns a wall clock, background task, or second scheduler. Tween property and callback endpoints are typed delegates rather than serialized paths or callables. ADR 0027 reserves an editor source boundary but does not make an editor project, assembly, type, UI, or self-hosting behavior implemented. ADR 0028 selects the SDL3 GPU API as the future primary renderer with shader support and SDL_Renderer as the reduced-capability baseline fallback; it does not implement either backend or any rendering API. There are also no Android or iOS application hosts, mobile packages, signing workflows, or native-device tests. `FileAccess`, `DirAccess`, and `ProjectSettings` resolve directory-backed `res://`/`user://`; no archive mount exists, and `uid://`/`pipe://` fail explicitly. FastLZ and Zstandard remain explicit codec gaps. Linux extended attributes and directory behavior are verified locally; implemented macOS and Windows backends are not native-host verified, no extended-attribute backend is claimed for Android or iOS, and mobile directory links/drive enumeration await host integration. `ConfigFile` itself continues to accept ordinary operating-system paths. `PackedScene` provides only typed in-memory capture/instantiation; it does not add a scene file format, editor inheritance, placeholders, or persistent event endpoints. Selecting `Box2D.NET` does not make physics implemented. `Engine` supplies fixed-step callback scheduling but no collision simulation, hidden clock, native pump, frame wait, or draw count. Persistent event connections remain blocked by the absence of a typed stable endpoint schema. Typed property descriptors exist for storage and future tooling, but no editor UI exists. A separate spatial-node subclass and all three-dimensional engine functionality are intentionally outside the product boundary.
