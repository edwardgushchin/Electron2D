# Electron2D documentation

Last updated: 2026-09-21

This directory describes the engine as it exists now. Planned features are listed only as explicit limitations or next boundaries; they are never presented as implemented.

## Current snapshot

- Product boundary: exclusively 2D; 3D is out of scope.
- Runtime target matrix: Linux, Windows, macOS, Android, and iOS. This is the required product boundary, not a claim of completed native delivery on every target.
- Game-object model: Node-based and scene-oriented. `Node` is the primary public game object, `SceneTree` owns the active hierarchy, and `PackedScene` packages any reusable Node hierarchy—from one composed object to a complete level—for independent instantiation. The current packing implementation is typed and in-memory; disk and editor workflows are not implemented.
- Public engine assembly: one managed `Electron2D.dll` class library. Accepted runtime dependencies may ship as separate assemblies; the current build has none.
- Product source boundary: runtime engine code lives in `src/`; the future self-hosted editor belongs to a separate executable project under `editor/Electron2D.Editor/`; first-party example games and templates belong under `examples/<Game>/`. Editor and games depend on the public runtime API, never the reverse.
- Production source root: `src/`, organized by engine module while retaining the flat public `Electron2D` namespace.
- Architecture context: `decisions/index.md` routes to bounded domain logs; read only the affected logs and explicit cross-domain dependencies. No decision log may exceed 500 lines.
- Target framework: .NET 8 (`net8.0`).
- Implemented domains: Core, Scene, Localization, and Resources.
- Implemented components: Object lifecycle, typed event connections, typed editor properties, color values, geometry values, configuration files, file and directory access, project settings, main loop, engine runtime, unified 2D node, scene tree, packed scenes, translation, and the resource base.
- Implemented production types: `ElectronObject`, `EventConnection`, `PropertyDescriptor`, `PropertyDescriptor<TOwner, TValue>`, `Color`, `Colors`, `Vector2`, `Vector2I`, `Vector4`, `Vector4I`, `Rect`, `Transform`, `Side`, `ConfigKey<T>`, `ConfigFile`, `FileAccess`, `DirAccess`, `FileAccessMode`, `FileCompressionMode`, `UnixPermissionFlags`, `ProjectSetting<T>`, `ProjectSettings`, `MainLoop`, `Engine`, `EngineVersionInfo`, `Node`, `NodeProcessMode`, `SceneTree`, `SceneTreeTimer`, `GroupCallFlags`, `PackedScene`, `SceneState`, `PackedSceneEditState`, `TranslationServer`, `Resource`, and `DeepDuplicateMode`.
- SDL3-CS integration: not implemented.
- Native SDL packaging: not designed or verified yet.
- Platform delivery status: the current `net8.0` project and executable harness are verified on Linux. There is no five-platform CI matrix, SDL application host, Android package, iOS bundle, signing workflow, or native-device verification for all targets yet.
- 2D physics backend: `Box2D.NET` is selected as a future external managed dependency, but neither its package nor a physics domain is integrated yet.
- Rendering architecture: the SDL3 GPU API is selected as the primary future backend with 2D shader support; SDL_Renderer is the reduced-capability fallback for baseline 2D drawing. The public API will expose backend capabilities and reject unsupported shader use explicitly. No rendering or shader code is implemented yet.
- Managed memory remains runtime-owned; `IDisposable` controls deterministic logical/native cleanup. Public manual reference counting is excluded, while internal asset leases are reserved for a future resource manager with concrete native-backed assets.
- Floating-point RGBA values, HSV and perceptual OKHSL conversion, straight-alpha blend, arithmetic/comparison, packed/HTML formats, strict finite configuration serialization, packed-scene value storage, and all 146 standard named colors: implemented without a renderer dependency.
- Engine-owned `Vector2`/`Vector2I` and `Vector4`/`Vector4I` families with complete float/integer value math, strict typed configuration schemas, packed-scene storage, documented IEEE/overflow behavior, and allocation-free warmed numeric paths: implemented. The four-component values are numeric tuples and do not introduce 3D/4D scene geometry.
- Floating-point `Rect` geometry with explicit negative-size normalization, half-open containment, enclosure/intersection/growth/merge/support and transform-bound operations, strict finite configuration serialization, packed-scene storage, and stable side identities: implemented without renderer, UI, or physics dependencies.
- Engine-owned `Transform` is implemented with complete affine math, rectangle operators, strict finite configuration persistence, direct packed-scene storage, allocation-free numeric hot paths, and direct `Node` local/global integration through `Vector2`.
- Process-wide typed project settings, feature overrides, directory-backed `res://`/`user://`, blocking typed file access with metadata/hashes/temporary files/cross-platform extended attributes/compression/authenticated encryption, scoped directory navigation/listing/mutations/links/temporary ownership/filesystem identity, host-driven bounded fixed-step scheduling, time scaling, frame metrics, named engine singletons, typed sectioned configuration files with atomic persistence and authenticated encryption, unified 2D nodes, local/global transforms, hierarchy paths and groups, visibility and Z state, pause-aware process/physics callbacks, exception-safe scene-tree lifecycle, typed group operations, one-shot frame timers, queued deletion, typed deferred work and event connections, in-memory typed packed scenes with per-instance local resources, translations, notifications including the future-facing `ScriptChanged` hook, typed editor-property descriptors, and the typed resource base with graph duplication: implemented.
- Rendering, SDL integration, input, audio, collision/rigid-body physics, concrete assets, asset loading/saving, scene file serialization, and an editor application: not implemented.
- The editor source root is reserved in this repository, but no editor project or source exists yet. Its future assembly is a consumer of `Electron2D.dll` and is not part of the one-runtime-DLL boundary.
- Persistent event connections: deferred until a typed stable endpoint schema exists.
- Packed/exported resource filesystems, import remapping, `uid://`, and `pipe://` are not implemented; current `res://`/`user://` resolution is directory-backed and lexically confined. FastLZ and Zstandard are explicit file-access gaps. Extended attributes and directory links are implemented for Linux, macOS, and Windows, with native-host verification currently limited to Linux. Android/iOS link and drive-enumeration integration is explicitly absent.

## Navigation

- [Inventory](inventory.md)
- Domain: [Core](domains/core.md)
- Domain: [Scene](domains/scene.md)
- Domain: [Localization](domains/localization.md)
- Domain: [Resources](domains/resources.md)
- Component: [Object lifecycle](components/object-lifecycle.md)
- Component: [Typed event connections](components/event-connections.md)
- Component: [Typed editor properties](components/editor-properties.md)
- Component: [Color values](components/color-values.md)
- Component: [Geometry values](components/geometry-values.md)
- Component: [Configuration files](components/config-files.md)
- Component: [File and directory access](components/file-access.md)
- Component: [Project settings](components/project-settings.md)
- Component: [Main loop](components/main-loop.md)
- Component: [Engine runtime](components/engine-runtime.md)
- Component: [Unified 2D node](components/unified-node.md)
- Component: [Scene tree](components/scene-tree.md)
- Component: [Packed scenes](components/packed-scenes.md)
- Component: [Translation](components/localization.md)
- Component: [Resource base](components/resources.md)
- Class: [ElectronObject](classes/ElectronObject.md)
- Class: [EventConnection](classes/EventConnection.md)
- Class: [PropertyDescriptor](classes/PropertyDescriptor.md)
- Class: [PropertyDescriptor&lt;TOwner, TValue&gt;](classes/PropertyDescriptor.Generic.md)
- Struct: [Color](classes/Color.md)
- Static class: [Colors](classes/Colors.md)
- Struct: [Vector2](classes/Vector2.md)
- Struct: [Vector2I](classes/Vector2I.md)
- Struct: [Vector4](classes/Vector4.md)
- Struct: [Vector4I](classes/Vector4I.md)
- Struct: [Rect](classes/Rect.md)
- Struct: [Transform](classes/Transform.md)
- Enum: [Side](classes/Side.md)
- Class: [ConfigKey&lt;T&gt;](classes/ConfigKey.Generic.md)
- Class: [ConfigFile](classes/ConfigFile.md)
- Class: [FileAccess](classes/FileAccess.md)
- Class: [DirAccess](classes/DirAccess.md)
- Enum: [FileAccessMode](classes/FileAccessMode.md)
- Enum: [FileCompressionMode](classes/FileCompressionMode.md)
- Enum: [UnixPermissionFlags](classes/UnixPermissionFlags.md)
- Class: [ProjectSetting&lt;T&gt;](classes/ProjectSetting.Generic.md)
- Class: [ProjectSettings](classes/ProjectSettings.md)
- Class: [MainLoop](classes/MainLoop.md)
- Class: [Engine](classes/Engine.md)
- Class: [EngineVersionInfo](classes/EngineVersionInfo.md)
- Class: [Node](classes/Node.md)
- Enum: [NodeProcessMode](classes/NodeProcessMode.md)
- Class: [SceneTree](classes/SceneTree.md)
- Class: [SceneTreeTimer](classes/SceneTreeTimer.md)
- Enum: [GroupCallFlags](classes/GroupCallFlags.md)
- Class: [PackedScene](classes/PackedScene.md)
- Class: [SceneState](classes/SceneState.md)
- Enum: [PackedSceneEditState](classes/PackedSceneEditState.md)
- Class: [TranslationServer](classes/TranslationServer.md)
- Class: [Resource](classes/Resource.md)
- Enum: [DeepDuplicateMode](classes/DeepDuplicateMode.md)
- Decisions: [routing index](decisions/index.md) with bounded logs for [Product architecture](decisions/product.md), [Core object/runtime](decisions/core-object-runtime.md), [Core data/I/O](decisions/core-data-io.md), [Core math](decisions/core-math.md), [Scene](decisions/scene.md), [Resources](decisions/resources.md), [Localization](decisions/localization.md), and [Rendering](decisions/rendering.md).

The maintenance rules for this documentation are mandatory and live in the repository root [AGENTS.md](../AGENTS.md).
