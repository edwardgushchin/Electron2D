# Electron2D documentation

Last updated: 2026-09-21

This directory describes the engine as it exists now. Planned features are listed only as explicit limitations or next boundaries; they are never presented as implemented.

## Current snapshot

- Product boundary: exclusively 2D; 3D is out of scope.
- Runtime target matrix: Linux, Windows, macOS, Android, and iOS. This is the required product boundary, not a claim of completed native delivery on every target.
- API direction: familiar scene-oriented 2D concepts expressed as typed C#.
- Public engine assembly: one managed `Electron2D.dll` class library. Accepted runtime dependencies may ship as separate assemblies; the current build has none.
- Production source root: `src/`, organized by engine module while retaining the flat public `Electron2D` namespace.
- Target framework: .NET 8 (`net8.0`).
- Implemented domains: Core, Scene, Localization, and Resources.
- Implemented components: Object lifecycle, typed event connections, typed editor properties, color values, geometry values, configuration files, file and directory access, project settings, main loop, engine runtime, unified 2D node, scene tree, packed scenes, translation, and the resource base.
- Implemented production types: `ElectronObject`, `EventConnection`, `PropertyDescriptor`, `PropertyDescriptor<TOwner, TValue>`, `Color`, `Colors`, `Rect2`, `Side`, `ConfigKey<T>`, `ConfigFile`, `FileAccess`, `DirAccess`, `FileAccessMode`, `FileCompressionMode`, `UnixPermissionFlags`, `ProjectSetting<T>`, `ProjectSettings`, `MainLoop`, `Engine`, `EngineVersionInfo`, `Node`, `NodeProcessMode`, `SceneTree`, `SceneTreeTimer`, `GroupCallFlags`, `PackedScene`, `SceneState`, `PackedSceneEditState`, `TranslationServer`, `Resource`, and `DeepDuplicateMode`.
- SDL3-CS integration: not implemented.
- Native SDL packaging: not designed or verified yet.
- Platform delivery status: the current `net8.0` project and executable harness are verified on Linux. There is no five-platform CI matrix, SDL application host, Android package, iOS bundle, signing workflow, or native-device verification for all targets yet.
- 2D physics backend: `Box2D.NET` is selected as a future external managed dependency, but neither its package nor a physics domain is integrated yet.
- Managed memory remains runtime-owned; `IDisposable` controls deterministic logical/native cleanup. Public manual reference counting is excluded, while internal asset leases are reserved for a future resource manager with concrete native-backed assets.
- Floating-point RGBA values, HSV and perceptual OKHSL conversion, straight-alpha blend, arithmetic/comparison, packed/HTML formats, strict finite configuration serialization, packed-scene value storage, and all 146 standard named colors: implemented without a renderer dependency.
- Floating-point axis-aligned rectangles with explicit negative-size normalization, half-open containment, enclosure/intersection/growth/merge/support operations, strict finite configuration serialization, packed-scene storage, and stable side identities: implemented without renderer, UI, or physics dependencies.
- A separate engine-owned `Transform2D` foundational type is required by ADR 0026 but is not implemented. `Node` truthfully retains its current `Matrix3x2` API until a complete transform slice and migration are delivered; `Rect2` transform operators are deferred to that dependency.
- Process-wide typed project settings, feature overrides, directory-backed `res://`/`user://`, blocking typed file access with metadata/hashes/temporary files/cross-platform extended attributes/compression/authenticated encryption, scoped directory navigation/listing/mutations/links/temporary ownership/filesystem identity, host-driven bounded fixed-step scheduling, time scaling, frame metrics, named engine singletons, typed sectioned configuration files with atomic persistence and authenticated encryption, unified 2D nodes, local/global transforms, hierarchy paths and groups, visibility and Z state, pause-aware process/physics callbacks, exception-safe scene-tree lifecycle, typed group operations, one-shot frame timers, queued deletion, typed deferred work and event connections, in-memory typed packed scenes with per-instance local resources, translations, notifications including the future-facing `ScriptChanged` hook, typed editor-property descriptors, and the typed resource base with graph duplication: implemented.
- Rendering, SDL integration, input, audio, collision/rigid-body physics, concrete assets, asset loading/saving, scene file serialization, and an editor application: not implemented.
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
- Struct: [Rect2](classes/Rect2.md)
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
- Decisions:
  - [0001: Typed C# without Variant](decisions/0001-typed-csharp-without-variant.md)
  - [0002: C# events for signals](decisions/0002-csharp-events-for-signals.md)
  - [0003: ElectronObject lifetime](decisions/0003-electron-object-lifetime.md)
  - [0004: 2D API in one Electron2D-owned assembly](decisions/0004-2d-api-single-assembly.md)
  - [0005: Notifications and typed editor properties](decisions/0005-notifications-and-typed-properties.md)
  - [0006: Scene-tree deferred work and queued deletion](decisions/0006-scene-tree-deferred-and-deletion.md)
  - [0007: Typed localization](decisions/0007-typed-localization.md)
  - [0008: Unified Node combines Node and Node2D](decisions/0008-unified-2d-node.md)
  - [0009: Disposal-thread callback access](decisions/0009-disposal-callback-access.md)
  - [0010: Typed event connections](decisions/0010-typed-event-connections.md)
  - [0011: SceneTree production contract](decisions/0011-scene-tree-production-contract.md)
  - [0012: External runtime dependencies and Box2D.NET](decisions/0012-external-runtime-dependencies.md)
  - [0013: Managed typed Resource contract](decisions/0013-managed-resource-contract.md)
  - [0014: Managed Resource lifetime and realtime allocation](decisions/0014-managed-resource-lifetime.md)
  - [0015: Main-loop lifecycle and host boundary](decisions/0015-main-loop-contract.md)
  - [0016: Process-wide Engine runtime and host-driven scheduling](decisions/0016-engine-runtime.md)
  - [0017: Source-tree module layout](decisions/0017-source-tree-layout.md)
  - [0018: Typed configuration files](decisions/0018-typed-config-files.md)
  - [0019: Typed project settings and directory-backed virtual paths](decisions/0019-typed-project-settings.md)
  - [0020: Typed file access and transformed-file containers](decisions/0020-file-access.md)
  - [0021: Cross-platform runtime target matrix](decisions/0021-cross-platform-runtime-targets.md)
  - [0022: Typed directory access](decisions/0022-directory-access.md)
  - [0023: Typed in-memory packed scenes](decisions/0023-typed-packed-scenes.md)
  - [0024: Typed color values and portable quantization](decisions/0024-typed-color-values.md)
  - [0025: Typed axis-aligned rectangle geometry](decisions/0025-typed-rectangle-geometry.md)
  - [0026: Separate Transform2D foundational type](decisions/0026-separate-transform2d-type.md)

The maintenance rules for this documentation are mandatory and live in the repository root [AGENTS.md](../AGENTS.md).
