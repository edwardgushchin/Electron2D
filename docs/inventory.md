# Engine inventory

Last updated: 2026-09-21

This is the exhaustive inventory of implemented Electron2D engine domains, components, and production types. Test-only helpers are not engine types.

Electron2D is 2D-only, and its runtime targets Linux, Windows, macOS, Android, and iOS under ADR 0021. All Electron2D-owned production rows in this inventory belong to `Electron2D.dll`. Accepted external runtime dependencies may ship as separate assemblies under ADR 0012.

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
| [Core](domains/core.md) | [Color values](components/color-values.md) | [`Color`](classes/Color.md) | [`Color.cs`](../src/Core/Math/Color.cs) | Current | Implemented and verified |
| [Core](domains/core.md) | [Color values](components/color-values.md) | [`Colors`](classes/Colors.md) | [`Colors.cs`](../src/Core/Math/Colors.cs) | Current | Implemented and verified |
| [Core](domains/core.md) | [Geometry values](components/geometry-values.md) | [`Rect2`](classes/Rect2.md) | [`Rect2.cs`](../src/Core/Math/Rect2.cs) | Current | Implemented and verified |
| [Core](domains/core.md) | [Geometry values](components/geometry-values.md) | [`Side`](classes/Side.md) | [`Side.cs`](../src/Core/Math/Side.cs) | Current | Implemented and verified |
| [Scene](domains/scene.md) | [Unified 2D node](components/unified-node.md) | [`Node`](classes/Node.md) | [`Node.cs`](../src/Scene/Main/Node.cs) | Current | Implemented and verified |
| [Scene](domains/scene.md) | [Unified 2D node](components/unified-node.md) | [`NodeProcessMode`](classes/NodeProcessMode.md) | [`NodeProcessMode.cs`](../src/Scene/Main/NodeProcessMode.cs) | Current | Implemented and verified |
| [Scene](domains/scene.md) | [Scene tree](components/scene-tree.md) | [`SceneTree`](classes/SceneTree.md) | [`SceneTree.cs`](../src/Scene/Main/SceneTree.cs) | Current | Implemented and verified |
| [Scene](domains/scene.md) | [Scene tree](components/scene-tree.md) | [`SceneTreeTimer`](classes/SceneTreeTimer.md) | [`SceneTreeTimer.cs`](../src/Scene/Main/SceneTreeTimer.cs) | Current | Implemented and verified |
| [Scene](domains/scene.md) | [Scene tree](components/scene-tree.md) | [`GroupCallFlags`](classes/GroupCallFlags.md) | [`GroupCallFlags.cs`](../src/Scene/Main/GroupCallFlags.cs) | Current | Implemented and verified |
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

## Selected but not integrated dependencies

| Dependency | Intended role | Packaging | Current state |
| --- | --- | --- | --- |
| `Box2D.NET` | Future 2D collision and rigid-body backend | Separate managed runtime assembly referenced by `Electron2D.dll` | Approved by ADR 0012; no package reference, physics code, or shipping artifact exists yet |

## Explicitly absent

There are currently no production types for `Transform2D`, `Rect2I`, packed/exported virtual filesystems, import remapping, resource-UID resolution, platform pipes, 2D rendering, SDL resources/hosting, input, audio, collision/rigid-body physics, concrete assets, asset loading/saving, scene file loading/saving, scripting, logging, movie writing, an editor application, or networking. ADR 0026 requires a future standalone `Transform2D`; current `Node` members remain `Matrix3x2` and `Rect2` transform operators remain deferred. There are also no Android or iOS application hosts, mobile packages, signing workflows, or native-device tests. `FileAccess`, `DirAccess`, and `ProjectSettings` resolve directory-backed `res://`/`user://`; no archive mount exists, and `uid://`/`pipe://` fail explicitly. FastLZ and Zstandard remain explicit codec gaps. Linux extended attributes and directory behavior are verified locally; implemented macOS and Windows backends are not native-host verified, no extended-attribute backend is claimed for Android or iOS, and mobile directory links/drive enumeration await host integration. `ConfigFile` itself continues to accept ordinary operating-system paths. `PackedScene` provides only typed in-memory capture/instantiation; it does not add a scene file format, editor inheritance, placeholders, or persistent event endpoints. Selecting `Box2D.NET` does not make physics implemented. `Engine` supplies fixed-step callback scheduling but no collision simulation, hidden clock, native pump, frame wait, or draw count. Persistent event connections remain blocked by the absence of a typed stable endpoint schema. Typed property descriptors exist for storage and future tooling, but no editor UI exists. A separate `Node2D` and all three-dimensional engine functionality are intentionally outside the product boundary.
