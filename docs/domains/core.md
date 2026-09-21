# Core domain

Last updated: 2026-09-21

## Responsibility

Core owns behavior shared by engine objects independently of scene, rendering, input, audio, physics, asset, or platform backends.

The domain is part of the 2D-only runtime for Linux, Windows, macOS, Android, and iOS and is compiled into the single production assembly `Electron2D.dll`.

Its production sources are grouped by upstream module under `src/Core/`: `Config`, `IO`, `Math`, `Object`, `OS`, and `String`. These directories do not alter the flat public `Electron2D` namespace.

## Current state

The domain currently contains ten implemented components:

| Component | Responsibility | State |
| --- | --- | --- |
| [Object lifecycle](../components/object-lifecycle.md) | Process-local identity, runtime type diagnostics, deterministic disposal, and disposed-state protection | Implemented and verified |
| [Typed event connections](../components/event-connections.md) | Disposable typed subscriptions with one-shot and deferred delivery | Implemented and verified |
| [Typed editor properties](../components/editor-properties.md) | Variant-free property discovery, typed access, validation, and revert behavior | Implemented and verified |
| [Configuration files](../components/config-files.md) | Strongly typed sectioned values, transactional parsing, atomic persistence, and authenticated encryption | Implemented and verified |
| [File and directory access](../components/file-access.md) | Blocking file/directory I/O, scoped navigation, virtual paths, metadata, links, hashes, temporary ownership, compression, and encryption | Implemented and verified; documented codec/platform gaps |
| [Project settings](../components/project-settings.md) | Typed global/default values, feature overrides, dirty events, project persistence, and virtual paths | Implemented and verified |
| [Main loop](../components/main-loop.md) | Owner-thread application lifecycle, frame hooks, stop requests, and platform-notification endpoints | Implemented and verified |
| [Engine runtime](../components/engine-runtime.md) | Process-wide loop coordination, fixed-step scheduling, time scaling, metrics, build information, and named singletons | Implemented and verified |
| [Color values](../components/color-values.md) | Floating-point RGBA math, HSV/OKHSL conversion, packing/parsing, and the standard named catalog | Implemented and verified |
| [Geometry values](../components/geometry-values.md) | Floating-point rectangles, affine transforms, side identities, spatial composition, inversion, containment, intersection, growth, and merge | Implemented and verified |

Production types are [`ElectronObject`](../classes/ElectronObject.md), [`EventConnection`](../classes/EventConnection.md), [`PropertyDescriptor`](../classes/PropertyDescriptor.md), [`PropertyDescriptor<TOwner, TValue>`](../classes/PropertyDescriptor.Generic.md), [`ConfigKey<T>`](../classes/ConfigKey.Generic.md), [`ConfigFile`](../classes/ConfigFile.md), [`FileAccess`](../classes/FileAccess.md), [`DirAccess`](../classes/DirAccess.md), [`FileAccessMode`](../classes/FileAccessMode.md), [`FileCompressionMode`](../classes/FileCompressionMode.md), [`UnixPermissionFlags`](../classes/UnixPermissionFlags.md), [`ProjectSetting<T>`](../classes/ProjectSetting.Generic.md), [`ProjectSettings`](../classes/ProjectSettings.md), [`MainLoop`](../classes/MainLoop.md), [`Engine`](../classes/Engine.md), [`EngineVersionInfo`](../classes/EngineVersionInfo.md), [`Color`](../classes/Color.md), [`Colors`](../classes/Colors.md), [`Rect2`](../classes/Rect2.md), [`Transform2D`](../classes/Transform2D.md), and [`Side`](../classes/Side.md).

## Public surface

- `ElectronObject`: identity, disposal, notifications including property-list/script-change hooks, typed property discovery, and per-object translation access.
- `EventConnection`: owned typed event subscription with optional one-shot and deferred delivery.
- `PropertyDescriptor`: heterogeneous property metadata and compatibility checks.
- `PropertyDescriptor<TOwner, TValue>`: typed access, validation, and revert behavior.
- `ConfigKey<T>`: immutable typed identity for one sectioned configuration entry.
- `ConfigFile`: concurrent in-memory configuration, transactional parse/merge, deterministic text encoding, atomic file replacement, and raw-key/password authenticated encryption.
- `FileAccess`: seekable raw and transformed file ownership, typed binary/text I/O, directory-backed virtual paths, metadata/hashes, native attributes, temporary files, compression, and authenticated encryption.
- `DirAccess`: scoped current-directory state, streaming/sorted enumeration, file/directory mutation, links, temporary-directory ownership, drive/capacity/type/case/identity queries, and static absolute helpers.
- `FileAccessMode`, `FileCompressionMode`, and `UnixPermissionFlags`: exact typed mode, codec, and Unix mode-bit identities.
- `ProjectSetting<T>`: immutable typed setting identity, default snapshot, and optional validator.
- `ProjectSettings`: process and isolated registries, feature overrides, metadata, dirty/event state, project persistence/discovery, and directory-backed virtual paths.
- `MainLoop`: explicit initialization, variable/fixed frame callbacks, host-stop results, finalization, system notification IDs, and typed permission results.
- `Engine`: singleton runtime configuration, bounded host-driven scheduling, time scaling, callback metrics, architecture/version data, and typed named-singleton lookup.
- `EngineVersionInfo`: immutable typed assembly version metadata.
- `Color`: sequential floating-point RGBA value with color-space conversion, math, composition, packing, text, and comparison behavior.
- `Colors`: immutable 146-entry named color surface and lookup catalog.
- `Rect2`: sequential floating-point axis-aligned rectangle with complete backend-independent geometry behavior.
- `Transform2D`: sequential affine 2D value with basis/origin decomposition, composition, inversion, interpolation, local/global operations, and typed point/vector transforms.
- `Side`: stable identity of the four rectangle edges.

## Dependency direction

- Core depends on the .NET Base Class Library and calls the static Localization-domain `TranslationServer` from `ElectronObject.Tr`/`TrN`.
- Configuration files use `System.Text.Json`, operating-system file APIs, PBKDF2-HMAC-SHA-256, and AES-256-GCM; they do not depend on an asset loader or platform host.
- File access uses `ProjectSettings` path resolution/root snapshots, .NET file/directory/drive/compression/hash/cryptography primitives, native filesystem identity/case/capacity and volume metadata, native Linux/macOS xattrs, and Windows alternate data streams. It shares the internal atomic replacement helper with `ConfigFile` and does not depend on a pack/resource loader.
- Project settings build on `ConfigFile`, typed properties, runtime platform/architecture detection, and ordinary directory paths. Engine reads its fixed-step settings from the process registry and flushes its coalesced event.
- `EventConnection` accepts a scheduler delegate rather than depending on Scene; callers may supply `SceneTree.Defer`.
- `MainLoop` does not depend on Scene or SDL; `Engine` depends on it as the runtime coordinator, `SceneTree` derives from it, and a future SDL host will supply time/events to Engine.
- `TranslationServer` has no dependency back on Core, so this direction does not form a cycle.
- Core does not depend on SDL3-CS.
- Color math depends only on .NET primitives and the bundled MIT-licensed managed OKHSL formulas; it has no native or rendering dependency. `ConfigFile` provides its strict finite JSON schema, while typed scene property storage consumes the reference-free value without a dependency back from Core Math to Scene.
- Rectangle geometry depends only on `System.Numerics.Vector2` and .NET primitives. `ConfigFile` provides its strict finite `Position`/`Size` schema, while typed scene property storage consumes the reference-free value without a dependency back from Core Math to Scene.
- Transform math depends only on `System.Numerics.Vector2` and .NET primitives. `ConfigFile` provides its strict finite `X`/`Y`/`Origin` schema, while typed scene property storage consumes the reference-free value without a dependency back from Core Math to Scene.
- Future engine domains may depend on Core.
- Core must not acquire dependencies on scene, rendering, input, or other higher-level domains.
- Core must not introduce 3D concepts or require a second production assembly.
- Core public semantics must remain portable across Linux, Windows, macOS, Android, and iOS; platform-specific work stays behind explicit backend or host boundaries.

## Domain invariants

- Public APIs are statically typed C# APIs.
- Event payloads remain statically typed; connection disposal and one-shot acceptance are thread-safe.
- Engine object identity is process-local and not persistent or network-stable.
- Disposal is explicit and idempotent.
- Starting disposal makes the object unavailable to other threads immediately; the winning disposal thread may inspect guarded state while running teardown callbacks.
- Dynamic Godot facilities are not recreated with `dynamic` or broad `object` containers.
- Main-loop lifecycle and frames are one-shot/non-reentrant owner-thread operations; no hidden thread or clock exists.
- Engine is process-wide and non-disposable; it schedules only from host-supplied elapsed time, bounds catch-up, and never takes disposal ownership of the active loop or registered singletons.
- Configuration keys reject universal-value and engine-object types. Parsing is transactional, mutation is lock-serialized, and saves replace through flushed same-directory temporary files. Encrypted files are authenticated before parsing.
- Project settings require exact typed definition identities, validate a complete candidate before load replacement, preserve unknown persisted entries, and lexically confine directory-backed virtual paths.
- File access enforces exact modes, complete scalar reads, strict UTF-8, authenticated-before-exposure encrypted reads, and lock-serialized instance calls. It is blocking/allocating and excluded from real-time hot paths.
- Directory access captures one immutable path scope, serializes instance state, never recursively deletes caller-selected content, and distinguishes unsorted listing snapshots from ordinally sorted content snapshots.
- Colors are sequential four-float values. Ordinary arithmetic retains HDR and IEEE 754 values; packed and HTML output are clamped/deterministic, and named lookup is immutable and thread-safe.
- Rectangles are sequential four-float values. Ordinary storage retains negative and IEEE 754 components, normalization is explicit, point containment is half-open, and numeric geometry is allocation-free after warmup.
- Transforms are sequential six-float column values. Ordinary math retains IEEE 754 components, left/right composition order is explicit, general and orthonormal inverse contracts are distinct, and numeric math is allocation-free after warmup.

## Not implemented

- No global object registry or lookup by `InstanceId`.
- No untyped metadata store.
- No reflection-based property or method invocation.
- No `Rect2I` production type. `Transform2D` exists, but migration of the current `Node` `Matrix3x2` surface and `Rect2` transform multiplication remains an explicit separate slice under ADR 0026 and ADR 0029.
- No script attachment, script runtime, editor application, or general file serialization. Only the typed `ScriptChanged` notification contract exists for the confirmed future scripting component.
- No persistent event connections; in-memory packed scenes intentionally omit subscribers, and persistence requires a typed stable endpoint identity/binding schema.
- No SDL application host, native system-event translation, permission request API, clock/wait-based maximum-FPS pacing, or exit-code service. `Engine` and `MainLoop` expose implemented integration endpoints without simulating those domains.
- No five-platform build/package/test matrix, Android host/package, iOS host/bundle, signing pipeline, or complete native-host verification exists yet. Current executable verification is Linux-only.
- No resource-pack mount, exported/archive-backed virtual filesystem, resource-UID resolver, or platform-pipe backend exists. `FileAccess`, `DirAccess`, and `ProjectSettings` resolve only configured `res://`/`user://` directories; `uid://` and `pipe://` fail explicitly, and `ConfigFile` still accepts only ordinary operating-system paths. FastLZ and Zstandard are not implemented. The macOS and Windows extended-attribute/directory backends are implemented but not verified on native hosts. Android/iOS directory links and drive enumeration await host/storage integration.
- No renderer draw count, logging-output controls, generated author/license manifest, script backtrace/language registry, movie writer, or editor hints; the Engine coverage inventory records each dependency boundary.

## Verification

`tests/Electron2D.Tests/Program.cs` verifies concurrent identity allocation, notifications, the `ElectronObject` lifetime contract, typed property discovery/access/validation/revert, property-list/script-change events, event-connection lifecycle/concurrency, complete color construction/math/conversion/parsing/named catalog/persistence/scene storage/allocation behavior, complete rectangle layout/geometry/boundaries/persistence/scene storage/allocation behavior, transform layout/construction/decomposition/composition/inversion/interpolation/persistence/scene storage/allocation behavior, configuration parsing/encoding/persistence/encryption/concurrency, file modes and typed data, directory scopes/listing/mutations/links/identity/case/temporary ownership, metadata/hashes, Linux xattrs/permissions and explicit unsupported attribute behavior, compression, encryption/tamper failures, virtual paths and commit cleanup, MainLoop state/error/thread behavior, and Engine configuration, scheduling, metrics, registry, failure safety, allocation, and SceneTree integration. It does not verify SDL or rendering behavior because those domains do not exist yet.

The same harness verifies project-setting registration, value snapshots, validators, metadata, overrides, changes/events, persistence, virtual paths, transaction rollback, concurrency, disposal, and Engine integration.

## Decisions

- [0001: Typed C# without Variant](../decisions/0001-typed-csharp-without-variant.md)
- [0002: C# events for signals](../decisions/0002-csharp-events-for-signals.md)
- [0003: ElectronObject lifetime](../decisions/0003-electron-object-lifetime.md)
- [0004: 2D scene-oriented API in one Electron2D-owned assembly](../decisions/0004-2d-api-single-assembly.md)
- [0012: External runtime dependencies and Box2D.NET](../decisions/0012-external-runtime-dependencies.md)
- [0005: Notifications and typed editor properties](../decisions/0005-notifications-and-typed-properties.md)
- [0009: Disposal-thread callback access](../decisions/0009-disposal-callback-access.md)
- [0010: Typed event connections](../decisions/0010-typed-event-connections.md)
- [0015: Main-loop lifecycle and host boundary](../decisions/0015-main-loop-contract.md)
- [0016: Process-wide Engine runtime and host-driven scheduling](../decisions/0016-engine-runtime.md)
- [0017: Source-tree module layout](../decisions/0017-source-tree-layout.md)
- [0018: Typed configuration files](../decisions/0018-typed-config-files.md)
- [0019: Typed project settings and directory-backed virtual paths](../decisions/0019-typed-project-settings.md)
- [0020: Typed file access and transformed-file containers](../decisions/0020-file-access.md)
- [0021: Cross-platform runtime target matrix](../decisions/0021-cross-platform-runtime-targets.md)
- [0022: Typed directory access](../decisions/0022-directory-access.md)
- [0024: Typed color values and portable quantization](../decisions/0024-typed-color-values.md)
- [0025: Typed axis-aligned rectangle geometry](../decisions/0025-typed-rectangle-geometry.md)
- [0026: Separate Transform2D foundational type](../decisions/0026-separate-transform2d-type.md)
- [0029: Typed Transform2D value and affine semantics](../decisions/0029-typed-transform2d-value.md)
