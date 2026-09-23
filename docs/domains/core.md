# Core domain

Last updated: 2026-09-23

## Responsibility

Core owns behavior shared by engine objects independently of scene, rendering, audio, physics, asset, or platform backends, plus the MainLoop/Engine integration points used by the separate typed Input domain.

The domain is part of the 2D-only runtime for Windows, macOS, Linux (X11/Wayland), Android, iOS, and Web and is compiled into the single production assembly `Electron2D.dll`.

Its production sources are grouped by upstream module under `src/Core/`: `Config`, `IO`, `Math`, `Object`, `OS`, and `String`. These directories do not alter the flat public `Electron2D` namespace.

## Current state

The domain currently contains twelve implemented components:

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
| [Scalar math](../components/scalar-math.md) | Stateless constants, transcendental functions, angles, interpolation, approximation, rounding, wrapping, and audio conversion | Implemented and verified |
| [Random generation](../components/random-generation.md) | Independent PCG32 streams, seed/state restoration, bounded, weighted and normal sampling | Implemented and managed-verified |
| [Color values](../components/color-values.md) | Floating-point RGBA math, HSV/OKHSL conversion, packing/parsing, and the standard named catalog | Implemented and verified |
| [Geometry values](../components/geometry-values.md) | Engine-owned vectors, rectangles, affine transforms, side identities, and pure 2D geometry queries | Values and Geometry class implemented |

Production types are [`ElectronObject`](../classes/ElectronObject.md), [`EventConnection`](../classes/EventConnection.md), [`PropertyDescriptor`](../classes/PropertyDescriptor.md), [`PropertyDescriptor<TOwner, TValue>`](../classes/PropertyDescriptor.Generic.md), [`ConfigKey<T>`](../classes/ConfigKey.Generic.md), [`ConfigFile`](../classes/ConfigFile.md), [`FileAccess`](../classes/FileAccess.md), [`DirAccess`](../classes/DirAccess.md), [`FileAccessMode`](../classes/FileAccessMode.md), [`FileCompressionMode`](../classes/FileCompressionMode.md), [`UnixPermissionFlags`](../classes/UnixPermissionFlags.md), [`ProjectSetting<T>`](../classes/ProjectSetting.Generic.md), [`ProjectSettings`](../classes/ProjectSettings.md), [`MainLoop`](../classes/MainLoop.md), [`Engine`](../classes/Engine.md), [`EngineVersionInfo`](../classes/EngineVersionInfo.md), [`Mathf`](../classes/Mathf.md), [`RandomNumberGenerator`](../classes/RandomNumberGenerator.md), [`Color`](../classes/Color.md), [`Colors`](../classes/Colors.md), [`Geometry`](../classes/Geometry.md), [`Vector2`](../classes/Vector2.md), [`Vector2I`](../classes/Vector2I.md), [`Vector4`](../classes/Vector4.md), [`Vector4I`](../classes/Vector4I.md), [`Rect`](../classes/Rect.md), [`RectI`](../classes/RectI.md), [`Transform`](../classes/Transform.md), and [`Side`](../classes/Side.md).

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
- `MainLoop`: explicit initialization, variable/fixed frame callbacks, host-stop results, finalization, system notification IDs, typed permission results, internal original-delta context, and Input transition/dispatch integration.
- `Engine`: singleton runtime configuration, bounded host-driven scheduling, scaled/original delta delivery, callback metrics, architecture/version data, and typed named-singleton lookup including permanent Input/InputMap services.
- `EngineVersionInfo`: immutable typed assembly version metadata.
- `Mathf`: seven scalar constants and 127 integer/float/double/decimal operations covering transcendental math, angles, interpolation, approximation, rounding, periodic values, and audio conversion.
- `RandomNumberGenerator`: independent managed PCG32 stream with restorable 64-bit seed/state and integer, float, normal and weighted sampling; it is not a cryptographic random source.
- `Color`: sequential floating-point RGBA value with color-space conversion, math, composition, packing, text, and comparison behavior.
- `Colors`: immutable 146-entry named color surface and lookup catalog.
- `Geometry`: twenty-four stateless grid-line, nearest-point, polygon, hull, decomposition, triangulation, atlas, intersection, clipping and offset operations.
- `Vector2` and `Vector2I`: complete two-component floating-point/integer values for 2D spatial, grid, and numeric behavior.
- `Vector4` and `Vector4I`: complete four-component floating-point/integer numeric tuples without 3D scene semantics.
- `Rect` and `RectI`: sequential floating-point/integer axis-aligned rectangles with complete backend-independent geometry, typed conversions, strict persistence, and packed-scene storage; `Rect` additionally provides transform bounds operators.
- `Transform`: sequential affine 2D value with basis/origin decomposition, composition, inversion, interpolation, local/global operations, and typed point/vector/rectangle transforms.
- `Side`: stable identity of the four rectangle edges.

## Dependency direction

- Core depends on the .NET Base Class Library and internally compiled Clipper2 for polygon clipping and offsets; it calls the static Localization-domain `TranslationServer` from `ElectronObject.Tr`/`TrN`.
- Configuration files use `System.Text.Json`, operating-system file APIs, PBKDF2-HMAC-SHA-256, and AES-256-GCM; they do not depend on an asset loader or platform host.
- File access uses `ProjectSettings` path resolution/root snapshots, .NET file/directory/drive/compression/hash/cryptography primitives, native filesystem identity/case/capacity and volume metadata, native Linux/macOS xattrs, and Windows alternate data streams. It shares the internal atomic replacement helper with `ConfigFile` and does not depend on a pack/resource loader.
- Project settings build on `ConfigFile`, typed properties, runtime platform/architecture detection, and ordinary directory paths. Engine reads its fixed-step settings from the process registry and flushes its coalesced event.
- `EventConnection` accepts a scheduler delegate rather than depending on Scene; callers may supply `SceneTree.Defer`.
- `MainLoop` does not depend on Scene or SDL; it has a narrow in-assembly dependency on Input for transition completion/event forwarding. `Engine` depends on both as the runtime coordinator, `SceneTree` derives from MainLoop, and the current SDL `DisplayServer` supplies native events to Input while a consumer supplies time to Engine.
- `TranslationServer` has no dependency back on Core, so this direction does not form a cycle.
- Core does not depend on SDL3-CS.
- Scalar math depends only on .NET numeric primitives and has no mutable state, native backend, or higher-domain dependency.
- Random generation uses .NET numeric and wall/monotonic clock primitives and inherited object lifetime; encryption and temporary-name code explicitly uses the separate .NET cryptographic generator.
- Color math depends on `Mathf`, .NET primitives, and the bundled MIT-licensed managed OKHSL formulas; it has no native or rendering dependency. `ConfigFile` provides its strict finite JSON schema, while typed scene property storage consumes the reference-free value without a dependency back from Core Math to Scene.
- Vector, rectangle, and transform math depends on `Mathf` plus .NET layout/formatting primitives. `ConfigFile` provides strict vector, `Position`/`Size`, and `X`/`Y`/`Origin` schemas, while typed scene storage consumes reference-free values without a dependency back from Core Math to Scene.
- `Rect`, `Transform`, and Scene's `Entity` use `Electron2D.Vector2`; public external numerics types and old compatibility names are absent.
- Future engine domains may depend on Core.
- Core must not acquire dependencies on scene, rendering, or other higher-level domains. The accepted MainLoop/Engine-to-Input integration is the narrow exception recorded by ADR 0038; native backends still depend inward rather than reversing ownership.
- Core must not introduce 3D concepts or require a second production assembly.
- Core public semantics must remain portable across Windows, macOS, Linux (X11/Wayland), Android, iOS, and Web; platform-specific work stays behind explicit backend or host boundaries.

## Domain invariants

- Public APIs are statically typed C# APIs.
- Event payloads remain statically typed; connection disposal and one-shot acceptance are thread-safe.
- Engine object identity is process-local and not persistent or network-stable.
- Disposal is explicit and idempotent.
- Starting disposal makes the object unavailable to other threads immediately; the winning disposal thread may inspect guarded state while running teardown callbacks.
- Dynamic Godot facilities are not recreated with `dynamic` or broad `object` containers.
- Main-loop lifecycle and frames are one-shot/non-reentrant owner-thread operations; effective/original deltas are finite, non-negative, and frame-scoped; no hidden game thread exists; Engine.Run measures monotonic time on the calling thread.
- Engine is process-wide and non-disposable; it schedules only from host-supplied elapsed time, bounds catch-up, carries original time independently of scaling, permanently registers Input/InputMap, and never takes disposal ownership of the active loop or user-registered singletons.
- Configuration keys reject universal-value and engine-object types. Parsing is transactional, mutation is lock-serialized, and saves replace through flushed same-directory temporary files. Encrypted files are authenticated before parsing.
- Project settings require exact typed definition identities, validate a complete candidate before load replacement, preserve unknown persisted entries, and lexically confine directory-backed virtual paths.
- File access enforces exact modes, complete scalar reads, strict UTF-8, authenticated-before-exposure encrypted reads, and lock-serialized instance calls. It is blocking/allocating and excluded from real-time hot paths.
- Directory access captures one immutable path scope, serializes instance state, never recursively deletes caller-selected content, and distinguishes unsorted listing snapshots from ordinally sorted content snapshots.
- `Mathf` is stateless, and normal nonthrowing calls are allocation-free after warmup. Single-precision approximate comparison uses strict `1e-6f`; double precision uses strict `1e-14`; documented managed exceptions remain visible.
- Each `RandomNumberGenerator` instance serializes its draws and seed/state changes. Fixed seeds reproduce a stream; bounded integers use unbiased rejection, and disposal closes the stream.
- Colors are sequential four-float values. Ordinary arithmetic retains HDR and IEEE 754 values; packed and HTML output are clamped/deterministic, and named lookup is immutable and thread-safe.
- Vectors are sequential two-, three-, or four-component float/int values. Floating math retains IEEE behavior; ordinary integer component arithmetic wraps except for documented managed failures. Integer-vector squared norms return checked signed 64-bit values, while lengths and distances remain finite across all 32-bit coordinates; numeric hot paths allocate no managed memory after warmup.
- Rectangles are sequential four-component values. Ordinary storage retains signed components and floating IEEE 754 values, normalization is explicit, point containment is half-open, integer math wraps except for documented managed failures, and numeric geometry is allocation-free after warmup.
- Transforms are sequential six-float column values. Ordinary math retains IEEE 754 components, left/right composition order is explicit, general and orthonormal inverse contracts are distinct, and numeric math is allocation-free after warmup.

## Not implemented

- No global object registry or lookup by `InstanceID`.
- No untyped metadata store.
- No reflection-based property or method invocation.
- No 3D rectangle, transform, node, renderer, or physics type. Three- and four-component vectors are numeric values, not scene types.
- No script attachment, script runtime, editor application, or general file serialization. Only the typed `ScriptChanged` notification contract exists for the confirmed future scripting component.
- No persistent event connections; in-memory packed scenes intentionally omit subscribers, and persistence requires a typed stable endpoint identity/binding schema.
- Engine.Run now owns windowed application startup, event pumping, monotonic MaxFPS pacing, SceneTree.Quit exit codes and cleanup. Root-window canvas rendering runs after scene processing. Permission requests and remaining mobile/browser integrations are absent.
- No six-target build/package/test matrix, Android host/package, iOS host/bundle, Web browser host/build/storage integration, signing pipeline, or complete native/browser verification exists yet. Current native verification is Linux-only: the root host and canvas have Wayland checks, with narrower XWayland display/context probes. This does not establish complete X11 or other-target acceptance.
- No resource-pack mount, exported/archive-backed virtual filesystem, resource-UID resolver, or platform-pipe backend exists. `FileAccess`, `DirAccess`, and `ProjectSettings` resolve only configured `res://`/`user://` directories; `uid://` and `pipe://` fail explicitly, and `ConfigFile` still accepts only ordinary operating-system paths. FastLZ and Zstandard are not implemented. The macOS and Windows extended-attribute/directory backends are implemented but not verified on native hosts. Android/iOS directory links and drive enumeration await host/storage integration.
- No renderer draw count, logging-output controls, generated author/license manifest, script backtrace/language registry, movie writer, or editor hints; the Engine coverage inventory records each dependency boundary.

## Verification

`tests/Electron2D.Tests/Program.cs` verifies concurrent identity allocation, notifications, the `ElectronObject` lifetime contract, typed property discovery/access/validation/revert, property-list/script-change events, event-connection lifecycle/concurrency, the exact `Mathf` constant/overload surface and numeric boundaries, complete color behavior, all six vector surfaces and numeric boundaries, floating-point and integer rectangle layout/geometry/conversions/boundaries, transform decomposition/composition/inversion/interpolation/rectangle operations, Entity vector/transform integration, strict persistence, packed-scene storage, allocation behavior, configuration parsing/encoding/persistence/encryption/concurrency, file and directory access, MainLoop state/error/thread/Input-transition behavior, and Engine scaled/original scheduling/service-registry integration. SDL dummy-driver checks belong to the separate Display domain; this Core verification does not prove renderer or native-host behavior.

[RandomNumberGeneratorTests](../../tests/Electron2D.Tests/RandomNumberGeneratorTests.cs) verifies fixed PCG32 vectors, state replay, integer range boundaries, weighted and normal sampling, typed descriptors and disposal on managed Linux/.NET.

Built-in rendering settings now include canvas mip interpolation and viewport anisotropy defaults; their consumption and native evidence are documented in [project settings](../components/project-settings.md) and [canvas rendering](../components/canvas-rendering.md).

The same harness verifies project-setting registration, value snapshots, validators, metadata, overrides, changes/events, persistence, virtual paths, transaction rollback, concurrency, disposal, and Engine integration.

## Decisions

- [0001: Typed C# without Variant](../decisions/product.md#adr-0001)
- [0002: C# events for signals](../decisions/product.md#adr-0002)
- [0003: ElectronObject lifetime](../decisions/core-object-runtime.md#adr-0003)
- [0004: 2D scene-oriented API in one Electron2D-owned assembly](../decisions/product.md#adr-0004)
- [0012: Vendored SDL3-CS and Box2D.NET](../decisions/product.md#adr-0012)
- [0005: Notifications and typed editor properties](../decisions/core-object-runtime.md#adr-0005)
- [0009: Disposal-thread callback access](../decisions/core-object-runtime.md#adr-0009)
- [0010: Typed event connections](../decisions/core-object-runtime.md#adr-0010)
- [0015: Main-loop lifecycle and host boundary](../decisions/core-object-runtime.md#adr-0015)
- [0016: Process-wide Engine runtime and host-driven scheduling](../decisions/core-object-runtime.md#adr-0016)
- [0017: Source-tree module layout](../decisions/product.md#adr-0017)
- [0018: Typed configuration files](../decisions/core-data-io.md#adr-0018)
- [0019: Typed project settings and directory-backed virtual paths](../decisions/core-data-io.md#adr-0019)
- [0020: Typed file access and transformed-file containers](../decisions/core-data-io.md#adr-0020)
- [0021: Runtime and editor target platforms](../decisions/product.md#adr-0021)
- [0022: Typed directory access](../decisions/core-data-io.md#adr-0022)
- [0024: Typed color values and portable quantization](../decisions/core-math.md#adr-0024)
- [0025: Typed axis-aligned rectangle geometry](../decisions/core-math.md#adr-0025)
- [0026: Separate Transform2D foundational type](../decisions/core-math.md#adr-0026)
- [0029: Typed Transform2D value and affine semantics](../decisions/core-math.md#adr-0029)
- [0032: Engine-owned unsuffixed 2D math vocabulary](../decisions/core-math.md#adr-0032)
- [0033: Dimensioned engine-owned vector family](../decisions/core-math.md#adr-0033)
- [0034: Canonical scalar mathematics and pre-release correction](../decisions/core-math.md#adr-0034)
- [0035: Foreseeable public type-family completeness](../decisions/core-math.md#adr-0035)
- [0036: Reusable Node timer and dual-delta frame delivery](../decisions/scene.md#adr-0036)
- [0038: Typed input events, action state, and scene propagation](../decisions/input.md#adr-0038)

## Windowed lifecycle

Engine.Run(Window) is the ordinary application entry point, with MaxFPS, monotonic timing, event pumping and deterministic scene/native teardown. This adds a narrow in-assembly dependency on SceneTree and Window; manual Start/AdvanceFrame/Stop remain available for embedding.

Pixel-snapping integration is described by [the canvas component](../components/canvas-rendering.md#pixel-snapping). Viewport owns independent transform/vertex policies; rendering preserves logical node transforms, while Sprite local queries honor attached transform snapping. Project defaults initialize the explicit root Window at construction.

The typed ProjectSettings.DebugPathsColor definition supplies the construction-time color for optional SceneTree path diagnostics; the runtime consumer is documented in [Scene paths](../components/scene-paths.md).
