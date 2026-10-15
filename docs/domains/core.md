# Core domain

Last updated: 2026-10-15

[Movie writing](../components/movie-writing.md) adds typed recording hooks and fixed-cadence Engine entry points over the ordinary window lifecycle. Built-in outputs are a real PNG sequence with synchronous WAV and indexed AVI/MJPEG with PCM16/32 and OGV/Theora/Vorbis with native muxing.

## Skeletal schema policy

The existing PropertyDescriptor/Resource scene-copy machinery now supports an internal force-subresource policy, used by Skeleton stacks to prevent shared mutable scene bindings. Existing DuplicationSession tracking retains aliases, rollback and instance-owned lifetime; no new public selector or reflection path is added.

The [OS environment and directory component](../components/os-environment.md) now executes process-local variables, native child inheritance and global/project/user-directory queries through the retained static OS service.

The [standard-input component](../components/standard-input.md) executes actual native standard-handle classification and serialized raw/UTF-8 input, including pipe-driven child applications.

## Responsibility

Process-wide service operations and events use static access to retained objects under [ADR 0095](../decisions/singleton-services.md#adr-0095). Native availability remains explicit through DisplayServer.IsAvailable and RenderingServer.IsAvailable. Independent project registries use ProjectSettingsRegistry; static ProjectSettings operations address only the runtime registry.

Core owns behavior shared by engine objects independently of scene, rendering, audio, physics, asset, or platform backends, plus the MainLoop/Engine integration points used by the separate typed Input domain.

The domain is part of the 2D-only runtime for Windows, macOS, Linux (X11/Wayland), Android, iOS, Android TV, tvOS, and Web and is compiled into the single production assembly `Electron2D.dll`.

[ADR 0090](../decisions/agent-native.md#adr-0090) makes programmatic authoring, batch execution and observable verification an accepted product requirement. Core supplies the existing typed object/property, lifecycle, scheduling and I/O foundations; shared editor/CLI operations remain outside the runtime. Manual scheduling does not by itself implement a headless project runner, persistent scene format or public rendered capture. Those missing capabilities remain explicit implementation work.

Its production sources are grouped by upstream module under `src/Core/`: `Config`, `IO`, `Math`, `Object`, `OS`, and `String`. These directories do not alter the flat public `Electron2D` namespace.

## Current state

The domain contains the following active components:

| Component | Responsibility | State |
| --- | --- | --- |
| [Worker execution](../components/worker-pool.md) | Typed background regular/group jobs, priority, progress, nested/concurrent waits and completion ownership | Applicable own API Implemented; Linux Wayland GPU/compatibility scene workflow verified; browser/foreign/native allocation gates remain |
| [Object lifecycle](../components/object-lifecycle.md) | Process-local identity, runtime type diagnostics, deterministic disposal, typed weak references, and disposed-state protection | Implemented and verified |
| [Typed event connections](../components/event-connections.md) | Disposable typed subscriptions with one-shot and deferred delivery | Implemented and verified |
| [Typed editor properties](../components/editor-properties.md) | Variant-free property discovery, typed access, validation, and revert behavior | Implemented and verified |
| [Configuration files](../components/config-files.md) | Strongly typed sectioned values, transactional parsing, atomic persistence, and authenticated encryption | Applicable own API Implemented and verified under ADR 0018 |
| [JSON documents](../components/json.md) | Document-scoped JSON parsing, diagnostics, formatting, and typed native conversion | Applicable own API Implemented and verified under ADR 0048 |
| [File and directory access](../components/file-access.md) | Blocking file/directory I/O, scoped navigation, virtual paths, metadata, links, hashes, temporary ownership, compression, and encryption | Implemented and verified; documented codec/platform gaps |
| [XML parsing](../components/xml-parsing.md) | Permissive UTF-8 token reading, byte offsets, attributes and section navigation | Applicable own API Implemented and verified under ADR 0049 |
| [Project settings](../components/project-settings.md) | Typed global/default values, feature overrides, dirty events, project persistence, and virtual paths | Implemented and verified |
| [Main loop](../components/main-loop.md) | Owner-thread application lifecycle, frame hooks, stop requests, and platform-notification endpoints | Implemented and verified |
| [Engine runtime](../components/engine-runtime.md) | Process-wide loop coordination, fixed-step scheduling, time scaling, metrics, build information, and named singletons | Implemented and verified |
| [Scalar math](../components/scalar-math.md) | Stateless constants, transcendental functions, angles, interpolation, approximation, rounding, wrapping, and audio conversion | Implemented and verified |
| [Random generation](../components/random-generation.md) | Independent PCG32 streams, seed/state restoration, bounded, weighted and normal sampling | Implemented and managed-verified |
| [Text patterns](../components/text-patterns.md) | Managed expression compilation, capture results, bounded searches and replacement | Partial parity with the pinned PCRE2 dialect |
| [Color values](../components/color-values.md) | Floating-point RGBA math, HSV/OKHSL conversion, packing/parsing, and the standard named catalog | Implemented and verified |
| [Geometry values](../components/geometry-values.md) | Engine-owned vectors, rectangles, affine transforms, side identities, and pure 2D geometry queries | Values and Geometry class implemented |

The common [physics clock audit](../components/engine-runtime.md#verification) now connects host scheduling to real CPU world ticks and motion with graphics denied. It verifies effective/live/feature settings, per-frame capture, interpolation re-baselining, zero-scale cadence, catch-up limits and 0/0 warmed active-frame managed allocation. The global fixed-phase snapshot grants no owner-thread access. Separate-thread simulation and native/GPU cadence remain separate acceptance requirements.

Production types are [`ElectronObject`](../classes/ElectronObject.md), [`WeakRef<T>`](../classes/WeakRef.Generic.md), [`EventConnection`](../classes/EventConnection.md), [`PropertyDescriptor`](../classes/PropertyDescriptor.md), [`PropertyDescriptor<TOwner, TValue>`](../classes/PropertyDescriptor.Generic.md), [`ConfigKey<T>`](../classes/ConfigKey.Generic.md), [`ConfigFile`](../classes/ConfigFile.md), [`JSON`](../classes/JSON.md), [`FileAccess`](../classes/FileAccess.md), [`DirAccess`](../classes/DirAccess.md), [`XMLParser`](../classes/XMLParser.md), [`FileAccessModeFlags`](../classes/FileAccessModeFlags.md), [`FileCompressionMode`](../classes/FileCompressionMode.md), [`UnixPermissionFlags`](../classes/UnixPermissionFlags.md), [`ProjectSetting<T>`](../classes/ProjectSetting.Generic.md), [`ProjectSettings`](../classes/ProjectSettings.md), [`ProjectSettingsRegistry`](../classes/ProjectSettingsRegistry.md), [`MainLoop`](../classes/MainLoop.md), [`Engine`](../classes/Engine.md), [`EngineVersionInfo`](../classes/EngineVersionInfo.md), [`Mathf`](../classes/Mathf.md), [`RandomNumberGenerator`](../classes/RandomNumberGenerator.md), [`RegEx`](../classes/RegEx.md), [`RegExMatch`](../classes/RegExMatch.md), [`Color`](../classes/Color.md), [`Colors`](../classes/Colors.md), [`Geometry`](../classes/Geometry.md), [`Vector2`](../classes/Vector2.md), [`Vector2i`](../classes/Vector2i.md), [`Vector3`](../classes/Vector3.md), [`Vector3i`](../classes/Vector3i.md), [`Vector4`](../classes/Vector4.md), [`Vector4i`](../classes/Vector4i.md), [`Rect2`](../classes/Rect2.md), [`Rect2i`](../classes/Rect2i.md), [`Transform`](../classes/Transform.md), [`Side`](../classes/Side.md), and [`Corner`](../classes/Corner.md).

## Public surface

- `ElectronObject`: identity, disposal, notifications including property-list/script-change hooks, typed property discovery, and per-object translation access.
- `WeakRef<T>`: non-owning typed access to a live engine object, invalidated by disposal or collection.
- `EventConnection`: owned typed event subscription with optional one-shot and deferred delivery.
- `PropertyDescriptor`: heterogeneous property metadata and compatibility checks.
- `PropertyDescriptor<TOwner, TValue>`: typed access, validation, and revert behavior.
- `ConfigKey<T>`: immutable typed identity for one sectioned configuration entry.
- `ConfigFile`: concurrent in-memory configuration, transactional parse/merge, deterministic text encoding, atomic file replacement, and raw-key/password authenticated encryption.
- `JSON`: document-scoped JSON tree parsing, formatting, diagnostics, source retention and explicitly typed model conversion.
- `FileAccess`: seekable raw and transformed file ownership, typed binary/text I/O, mounted or directory-backed virtual paths, metadata/hashes, native attributes, temporary files, compression, and authenticated encryption.
- `DirAccess`: scoped current-directory state, streaming/sorted enumeration, file/directory mutation, links, temporary-directory ownership, drive/capacity/type/case/identity queries, and static absolute helpers.
- `FileAccessModeFlags`, `FileCompressionMode`, and `UnixPermissionFlags`: exact typed mode, codec, and Unix mode-bit identities.
- `ProjectSetting<T>`: immutable typed setting identity, default snapshot, and optional validator.
- `ProjectSettings`: process and isolated registries, feature overrides, metadata, dirty/event state, project persistence/discovery, directory-backed virtual paths, typed input-action defaults, and typed 2D gravity/damping world defaults consumed by SceneTree physics.
- `MainLoop`: explicit initialization, variable/fixed frame callbacks, host-stop results, finalization, system notification IDs, typed permission results, internal original-delta context, and Input transition/dispatch integration.
- `Engine`: singleton runtime configuration, bounded host-driven scheduling, scaled/original delta delivery, callback metrics, architecture/version data, and typed named-singleton lookup including permanent Input/InputMap services.
- `EngineVersionInfo`: immutable typed assembly version metadata.
- `Mathf`: seven scalar constants and 127 integer/float/double/decimal operations covering transcendental math, angles, interpolation, approximation, rounding, periodic values, and audio conversion.
- `RandomNumberGenerator`: independent managed PCG32 stream with restorable 64-bit seed/state and integer, float, normal and weighted sampling; it is not a cryptographic random source.
- `RegEx` and `RegExMatch`: compiled managed text search, substitution and immutable capture inspection with documented PCRE2 compatibility gaps.
- `XMLParser`: copied UTF-8 XML tokens, ordered attributes, byte-offset seeking, and nested section skipping with permissive malformed-input behavior.
- `Color`: sequential floating-point RGBA value with color-space conversion, math, composition, packing, text, and comparison behavior.
- `Colors`: immutable 146-entry named color surface and lookup catalog.
- `Geometry`: twenty-four stateless grid-line, nearest-point, polygon, hull, decomposition, triangulation, atlas, intersection, clipping and offset operations.
- `Vector2` and `Vector2i`: complete two-component floating-point/integer values for 2D spatial, grid, and numeric behavior.
- `Vector3` and `Vector3i`: three-component numeric tuples without 3D scene semantics. Vector3 has 84 applicable members and its type row audited, with ten model-orientation/3D transform exclusions under ADR 0004; all 56 Vector3i members and its type row now have managed behavioral audits under ADR 0033.
- `Vector4` and `Vector4i`: four-component numeric tuples without 3D scene semantics. Vector4 has all 65 applicable members and its type row audited; the Projection operator is Excluded by ADR 0004. Vector4i has all 52 declared members and its type row audited with accepted checked-integer boundaries under ADR 0033.
- `Rect2` and `Rect2i`: sequential floating-point/integer axis-aligned rectangles with complete backend-independent geometry, typed conversions, strict persistence, and packed-scene storage; `Rect2` additionally provides transform bounds operators. All 27 Rect2 members and all 23 Rect2i members, plus both type rows, have pinned semantic audits, including IEEE growth, ordered edge rejection, integer wrap and conversion failures.
- `Transform`: sequential affine 2D value with basis/origin decomposition, composition, inversion, interpolation, local/global operations, and typed point/vector/rectangle transforms.
- `Side`: stable identity of the four rectangle edges.
- `Corner`: stable clockwise identity of the four rectangle corners, consumed by StyleBoxFlat radius access.

## Dependency direction

- Core depends on the .NET Base Class Library and internally compiled Clipper2 2.0.1 for polygon clipping and offsets; it calls the static Localization-domain `TranslationServer` from `ElectronObject.Tr`/`TrN`.
- JSON documents use `System.Text.Json` and the existing typed configuration converters; this does not make JSON nodes valid configuration values.
- Configuration files use `System.Text.Json`, operating-system file APIs, PBKDF2-HMAC-SHA-256, and AES-256-GCM; they do not depend on an asset loader or platform host.
- File access uses `ProjectSettings` path resolution/root snapshots, .NET file/directory/drive/compression/hash/cryptography primitives, native filesystem identity/case/capacity and volume metadata, native Linux/macOS xattrs, and Windows alternate data streams. It shares the internal atomic replacement helper with `ConfigFile` and does not depend on a pack/resource loader.
- Project settings build on `ConfigFile`, typed properties, runtime platform/architecture detection, and ordinary directory paths. Engine reads its fixed-step settings from the process registry and flushes its coalesced event.
- `EventConnection` accepts a scheduler delegate rather than depending on Scene; callers may supply `SceneTree.Defer`.
- `MainLoop` does not depend on Scene or SDL; it has a narrow in-assembly dependency on Input for transition completion/event forwarding. `Engine` depends on both as the runtime coordinator, `SceneTree` derives from MainLoop, and the current SDL `DisplayServer` supplies native events to Input while a consumer supplies time to Engine.
- `TranslationServer` reads typed Core project settings at startup and explicit reload; Core calls its static API without taking a construction-time dependency on a translation instance.
- Core does not depend on SDL3-CS.
- Scalar math depends only on .NET numeric primitives and has no mutable state, native backend, or higher-domain dependency.
- Random generation uses .NET numeric and wall/monotonic clock primitives and inherited object lifetime; encryption and temporary-name code explicitly uses the separate .NET cryptographic generator.
- Text patterns use .NET regular expressions with a finite timeout and no native or higher-domain dependency.
- XML parsing uses managed byte/token processing and the existing `FileAccess` path resolver; it does not require a native parser or resource loader.
- Color math depends on `Mathf`, .NET primitives, and the bundled MIT-licensed managed OKHSL formulas; it has no native or rendering dependency. `ConfigFile` provides its strict finite JSON schema, while typed scene property storage consumes the reference-free value without a dependency back from Core Math to Scene.
- Vector, rectangle, and transform math depends on `Mathf` plus .NET layout/formatting primitives. `ConfigFile` provides strict vector, `Position`/`Size`, and `X`/`Y`/`Origin` schemas, while typed scene storage consumes reference-free values without a dependency back from Core Math to Scene.
- `Rect2`, `Transform`, and Scene's `Entity` use `Electron2D.Vector2`; public external numerics types and old compatibility names are absent.
- Future engine domains may depend on Core.
- Core must not acquire dependencies on scene, rendering, or other higher-level domains. The accepted MainLoop/Engine-to-Input integration is the narrow exception recorded by ADR 0038; native backends still depend inward rather than reversing ownership.
- Core must not introduce 3D concepts or require a second production assembly.
- Core public semantics must remain portable across Windows, macOS, Linux (X11/Wayland), Android, iOS, Android TV, tvOS, and Web; platform-specific work stays behind explicit backend or host boundaries.

## Domain invariants

- Public APIs are statically typed C# APIs.
- Event payloads remain statically typed; connection disposal and one-shot acceptance are thread-safe.
- Engine object identity is process-local and not persistent or network-stable.
- Disposal is explicit and idempotent.
- Starting disposal makes the object unavailable to other threads immediately; the winning disposal thread may inspect guarded state while running teardown callbacks.
- Dynamic Godot facilities are not recreated with `dynamic` or broad `object` containers.
- Main-loop lifecycle and frames are one-shot/non-reentrant owner-thread operations; effective/original deltas are finite, non-negative, and frame-scoped; Engine.Run measures monotonic time on the calling thread, while RunAsync yields on the application thread or Android's required native video thread.
- Engine is process-wide and non-disposable; it schedules from host-supplied or owned monotonic elapsed time, bounds catch-up, carries original time independently of scaling, permanently registers Input/InputMap, and never takes disposal ownership of the active loop or user-registered singletons.
- JSON documents expose mutable syntax trees only within the JSON API; caller edits need their own synchronization and native conversion rejects untyped or engine-object members.
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
- User C# Node subclasses execute ordinary callbacks and registered source assets reconstruct actual project classes through typed scene files. [ADR 0091](../decisions/scripting.md#adr-0091) uses one concrete Script resource for C# source/type/schema metadata and typed factories. Live script attachment/replacement, editor application and implementation reload remain absent; ScriptChanged retains its reserved binding notification role.
- No persistent event connections; in-memory packed scenes intentionally omit subscribers, and persistence requires a typed stable endpoint identity/binding schema.
- Engine.Run now owns windowed application startup, event pumping, monotonic MaxFPS pacing, SceneTree.Quit exit codes and cleanup. Root-window canvas rendering runs after scene processing. Permission requests and remaining mobile/browser integrations are absent.
- The repository has an 18-RID automated build/package/test matrix and RID-selected CharacterMovement desktop, Android/Android TV, UIKit and browser hosts. CharacterMovement was executed on Linux Wayland, one Android phone, one Android TV and Chrome; Windows/macOS/iOS/tvOS execution and store signing remain unverified for this example. Browser storage and complete production lifecycle acceptance remain separate work. See the platform report for exact scope.
- Sparse/delta pack exporters, native executable section discovery and import remapping remain dependencies. PCK/ZIP mounting now provides read-only resource overlays; remaining exported virtual filesystem, platform-pipe backend exists. ResourceUID supplies registered identity resolution. `FileAccess`, `DirAccess`, and `ProjectSettings` resolve only configured `res://`/`user://` directories; `uid://` resolves registered file identities and unknown identities fail; `pipe://` still fails explicitly, and `ConfigFile` still accepts only ordinary operating-system paths. FastLZ and Zstandard execute with the matching native bridge; native file-codec verification covers Linux x64. The macOS and Windows extended-attribute/directory backends are implemented but not verified on native hosts. Android/iOS directory links and drive enumeration await host/storage integration.
- No renderer draw count, logging-output controls, generated author/license manifest, script backtrace/language registry, movie writer, or editor hints; the Engine coverage inventory records each dependency boundary.

## Verification

The shared [portability contract tests](../../tests/Portability/ContractChecks.cs) execute compiled JSON models, ConfigFile parse/reload, ProjectSettings base/feature-override validation and headless scene physics without JSON reflection. Dedicated desktop, Android, Apple and browser consumers share that source. Linux x64 NativeAOT and Chromium execution are checked locally; target host/device results remain individually scoped in [platform verification](../platform-verification.md#automated-rid-checks).

`tests/Electron2D.Tests/Program.cs` verifies concurrent identity allocation, notifications, the `ElectronObject` lifetime contract, typed property discovery/access/validation/revert, property-list/script-change events, event-connection lifecycle/concurrency, the exact `Mathf` constant/overload surface and numeric boundaries, complete color behavior, all six vector surfaces and numeric boundaries, floating-point and integer rectangle layout/geometry/conversions/boundaries, transform decomposition/composition/inversion/interpolation/rectangle operations, Entity vector/transform integration, strict persistence, packed-scene storage, allocation behavior, configuration parsing/encoding/persistence/encryption/concurrency, file and directory access, MainLoop state/error/thread/Input-transition behavior, and Engine scaled/original scheduling/service-registry integration. SDL dummy-driver checks belong to the separate Display domain; this Core verification does not prove renderer or native-host behavior.

[RandomNumberGeneratorTests](../../tests/Electron2D.Tests/RandomNumberGeneratorTests.cs) verifies fixed PCG32 vectors, state replay, integer range boundaries, weighted and normal sampling, typed descriptors and disposal on managed Linux/.NET.

Built-in rendering settings now include canvas mip interpolation and viewport anisotropy defaults; their consumption and native evidence are documented in [project settings](../components/project-settings.md) and [canvas rendering](../components/canvas-rendering.md).

The typed `physics/common/physics_interpolation` setting initializes each new SceneTree's 2D presentation policy. Scene and Rendering own its execution; Core continues to own only the typed setting and Engine's fixed-step fraction.

Four typed `physics/2d/default_*` keys define finite gravity strength/vector and linear/angular damping. The Physics domain samples them for each new world and owns priority reduction with Area fields; Core owns validation, persistence and feature overrides. [PhysicsAreaFieldTests](../../tests/Electron2D.Tests/PhysicsAreaFieldTests.cs) verifies default identities and world sampling.

[`JsonTests`](../../tests/Electron2D.Tests/JsonTests.cs) verifies document parsing, formatting, diagnostics, typed conversion, duplicate ownership and disposal; exact reference parity remains Partial.

The same harness verifies project-setting registration, value snapshots, validators, metadata, overrides, changes/events, persistence, virtual paths, transaction rollback, concurrency, disposal, and Engine integration.

## Decisions

- [0001: Typed C# without Variant](../decisions/product.md#adr-0001)
- [0048: Dedicated JSON documents](../decisions/core-data-io.md#adr-0048)
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

Engine.Run(Window) and yielding Engine.RunAsync(Window) are the ordinary application entry points, with MaxFPS, monotonic timing, event pumping and deterministic scene/native teardown. This adds a narrow in-assembly dependency on SceneTree and Window; manual Start/AdvanceFrame/Stop remain available for embedding.

Pixel-snapping integration is described by [the canvas component](../components/canvas-rendering.md#pixel-snapping). Viewport owns independent transform/vertex policies; rendering preserves logical node transforms, while Sprite local queries honor attached transform snapping. Project defaults initialize the explicit root Window at construction.

The typed ProjectSettings.DebugPathsColor definition supplies the construction-time color for optional SceneTree path diagnostics; the runtime consumer is documented in [Scene paths](../components/scene-paths.md).

## Native networking foundation

[Networking](networking.md) owns StreamPeer/PacketPeer codecs and native TCP/UDP/UDS listener/peer behavior under [ADR 0094](../decisions/networking.md#adr-0094). The core object identity/disposal and typed project settings remain shared infrastructure; transport handles stay private. This is the first executable network layer, with protocol-specific and other-platform gates recorded separately.

## Typed file integration

See [resource-file contracts](../components/resource-files.md) for registered typed schemas, cache/UID resolution, file-root and scene-instance ownership, public extension hooks and exercised verification. File operations allocate outside frame processing. UID paths resolve through the permanent catalog before directory-backed path resolution; unknown UIDs fail explicitly. The archive profile does not add an editor, arbitrary import/remap rules or every resource schema.

[File dialogs](../components/file-dialogs.md) now execute scoped browsing, five selection modes, typed custom options, filters, menus, overwrite/folder workflows and recoverable desktop Linux trash. The shared FileDialogMode identity spans the custom browser and DisplayServer; native-file-extra and foreign platform gates remain explicit. Six permanent ui_filedialog actions use the existing InputMap settings loader.

[Compiled C# source assets](../components/scripting.md) register exact project Node/Resource factories and typed schemas, validate portable-PDB source/type association, load/save Script resources, and reconstruct actual user classes through scene archives. Project callbacks retain ordinary lifecycle and source-resource disposal does not unload their compiled class. RichTextLabel consumes owned compiled effect instances. Live reassignment, implementation reload/migration, scene RPC and foreign target source-symbol prerequisites remain explicit dependencies.

## System font integration

[System font matching](../components/system-fonts.md) adds installed families/styles/logical collection faces and owned automatic text fallback over the shared native owner and canvas path. FontFile.AllowSystemFallback defaults to true; explicit resources retain precedence and explicit support queries remain distinct from automatic rendered coverage. Active parent readers retain retired fallback faces through policy changes. SystemFont archives store preferences and rematch the host. The current Linux catalog and both canvas consumers are exercised; CoreText/DirectWrite, extra raster/MSDF, native allocator and foreign acceptance gates remain explicit.

NativeLibraries resolves the packaged SPIRV-Cross shared library directly for cold shader reflection before renderer startup. The current Linux particle shader host exercises this path; foreign native execution remains unverified.

## Physics diagnostic service

[Performance](../classes/Performance.md) is a permanent retained Engine service with
static GetMonitor access. Its three current producers read completed CPU/GPU
[physics statistics](../components/physics-statistics.md) on any thread without
allocating or synchronizing devices. Other built-in producers and custom monitors
remain open coverage obligations.

WorkerThreadPool schedules synchronized or independently owned data computations. It does not grant scene/render thread access: the executable WorkerRoutes consumer publishes through SceneTree.Defer. Threaded ResourceLoader, asynchronous baking and their ownership/cancellation contracts remain separate consumers under ADR 0104.

The [independent-process component](../components/processes.md) adds static launch/capture/current-app identity and argv, retained child exit/kill observations and owned typed native pipe results. It supplies runtime primitives for external tools; project orchestration, deadline/cancellation policy and full agent acceptance remain consumer/tooling work.

[Runtime diagnostics](../components/runtime-diagnostics.md) now includes typed application custom monitors on the retained Performance service: exact callback values, unit/name snapshots, elapsed mutation ticks, explicit removal and captured-provider lifetime. Calls are available without a native runtime; provider execution stays on the caller thread outside the registry lock. Existing physical producers retain their completed-snapshot contract.

Engine.GetFramesDrawn now accounts for completed [native canvas intervals](../components/manual-render-frames.md), independently of process and physics callbacks.

[Compiled indexed properties](../components/editor-properties.md#compiled-indexed-properties) now execute typed object/component/array/dictionary projections through the existing descriptor schema, including Tween and saved-scene consumers.

[Implicit physics world selection](../components/physics-backends.md#project-physics-selection) now captures typed project engine/fallback settings and manager DEFAULT priority independently of rendering. Caller parameterless and explicit creation retain their policies. CPU/GPU/registered motion, no-device startup and native pixels have scoped checks; separate-thread scheduling and complete physics/performance acceptance remain open.

OS restart-on-exit configuration is a process-wide, desktop-only request with a copied literal argument list. SetRestartOnExit atomically replaces both values; IsRestartOnExitSet reads the flag and GetRestartOnExitArguments returns an independent array, including while disabled. Invalid Unicode, NUL or null elements fail before publication. A single normal managed process-shutdown handler captures the final request and uses CreateInstance. A standalone host must finish its loop cleanup and return; stopping a borrowed loop or returning from Engine.Run alone does not exit its host. The replacement process starts with disabled restart state. Failed native launch reports to standard error and preserves the original exit code; crashes and forced termination do not guarantee shutdown callbacks.

## Managed synchronization

[Mutex](../classes/Mutex.md) and [Semaphore](../classes/Semaphore.md) are caller-owned synchronization primitives over the worker foundation. Their recursive critical sections, counted signals, typed failures and joined cleanup support actual worker-to-scene publication; they grant no thread-affine scene or renderer access. See [synchronization](../components/synchronization.md) and ADR 0104.

The fixed locale catalog exposes language/script/country names, aliases, script/regional defaults and gettext definitions through TranslationServer. Translation resources without a caller PluralSelector use these prepared integer rules; copying and archive loading restore the automatic locale policy. Unknown multi-form rules require a caller selector. Runtime Culture remains CultureInfo. Decimal CLDR category selection and general PO/MO import are separate capabilities.

Typed property inspection uses the exact teardown-thread exception from ADR 0009: GetValue, TryGetRevertValue and CanRevert remain usable in pre-delete/cleanup on that thread; foreign-thread and terminal access fail. SetValue and Revert reject once disposal starts. TryGetRevertValue reports factory presence even for a null or unchanged default, while CanRevert and PropertyCanRevert retain the current value-difference policy. Factories run on each request; their results follow delegate ownership with no implicit copy. Validators reject before the setter, and an exception after a setter commits does not undo that write. Ordinary owner setters and getters retain thread, capture and numerical-phase rules.

[ObjectPropertyTests](../../tests/Electron2D.Tests/ObjectPropertyTests.cs) executes discovery snapshots, compatible replacement/filtering and invalid lists, owner policy, live/default/null/array values, readonly/validation/delegate errors and teardown inspection. Explicit CPU/GPU serial and separate-thread profiles use real RigidBody properties, permitted post-solver mass edits, owner/capture rejection and callback-error recovery. Each retains 128 warm/128 measured full frames with typed inspection, velocity writes and reversion at strict zero owner/all-thread managed bytes. GPU runs use CPU-only Vulkan; this does not establish hardware performance, editor automation or native/platform acceptance.

Prepared reusable synchronous subscriptions with zero through four arguments create no per-emission capture. Prepared standard-scene delivery uses typed invocation slots; arbitrary custom schedulers retain their independent argument capture paths. No zero-allocation claim is made for subscription/accessor work or allocating user handlers. This preserves argument values, lock boundaries, detach-before-handler ordering, cancellation and exception policy under ADR 0010.

[EventConnectionPhysicsTests](../../tests/Electron2D.Tests/EventConnectionPhysicsTests.cs) verifies all five prepared immediate arities and full selected CPU/GPU frames: 128 warm/128 measured frames, strict zero owner/all-thread managed bytes, 128 emissions of each arity and 64 Area body enters/exits. Identity, disposed subscriptions and collected callback-error recovery remain observable. Cold two-argument deferred/one-shot checks cover value capture, ordering, cancellation and recursion; the existing EventConnection suite retains concurrency, accessor rollback/removal/scheduler errors and all overloads. GPU execution uses CPU-only Vulkan; hardware/native/network/performance acceptance stays open.

[Extended event arities](../classes/EventConnection.md) cover current 3-argument picking and 4-argument body/Area shape signatures under ADR 0010 without altering event payloads. The shared physics fixture now retains 128 measured emissions of each arity, 64 body and 64 shape enters/exits and exact RID/owner/disabled-first shape indices at strict 0/0 warm bytes on selected CPU/GPU serial/separate profiles. Deferred/one-shot calls retain their separate capture allocation paths.

Prepared reusable deferred zero-argument subscriptions now retain one immutable callback in the token and submit it for every emission. Each submission remains a separate queue entry; multiplicity, cancellation, scheduler errors and handler state checks are unchanged. There is no shared mutable argument slot or coalescing. With prepared SceneTree queue capacity and nonallocating handlers, the full physical frame retains zero owner/all-thread managed bytes. Custom-scheduler payload capture, subscription and first capacity growth remain separate allocating paths.

The shared EventConnectionPhysicsTests now verifies a three-emission deferred batch, 128 warm/128 measured prepared emit/drain cycles, pending cancellation and 128 real SceneTree.Defer callbacks inside measured CPU/GPU physical frames, alongside all five immediate arities, Area body/shape transitions and picking/lifetime/error cases. GPU profiles use CPU-only Vulkan; no hardware performance/native/platform acceptance is inferred.

The exact SceneTree.Defer scheduler and SceneTree.SetDeferred now use a Scene-owned typed invocation provider. Each accepted call retains independent typed values in a slot and submits its immutable callback into the existing single FIFO queue. Slots recycle only after callback completion or discard, clearing argument/callback references even after failure. Work emitted during a captured batch waits for the next flush. Pool operations share the lifetime/work gate; user handlers run outside it. A weak-owner Core registry recognizes only the exact registered delegate, so Core has no Scene runtime dependency. Public signatures, borrowed reference ownership, event multiplicity and cancellation remain unchanged.

Prepared standard-scene delivery and typed setters have zero invocation allocation, including prepared one-shot first delivery. First type/capacity growth, subscription and allocations performed by handlers/accessors remain separate; native multicast removal with multiple listeners can allocate. Arbitrary custom schedulers preserve their independent captured Actions and may allocate. Deferred delivery never extends a borrowed input event lifetime.

EventConnectionPhysicsTests verifies mixed typed/untyped FIFO order, next-batch work, failures followed by later work, closed-scheduler subscription/emission policy, 256 concurrent producers, reference release on execution/failure/finalization/activation rollback, and 640 first one-shot callbacks for immediate and scene-deferred profiles with singleton accessors. Selected CPU/GPU full frames retain 128 callbacks per immediate and deferred arity plus body/shape transitions at strict zero owner/all-thread bytes. Real picking and legacy custom-scheduler/concurrency/error checks remain included. Software Vulkan is functional evidence; hardware/native/foreign/network/performance acceptance stays open.

## Prepared physical event delivery

Each emission leases an ordered immutable handler snapshot. Duplicate handlers and last-matching multicast subsequence removal retain ordinary delegate semantics. Reentrant/concurrent changes affect later emissions; user callbacks run outside the storage lock. Subscription prepares a spare buffer for one-shot removal. Capacity or overlapping-snapshot growth can allocate; arbitrary consumer event accessors and user callbacks keep their own allocation policy. Disposal clears these publisher-owned lists after teardown. Pointer events collect per-handler errors and retain eligibility checks; overlap/contact/sleep handlers stop the current event on failure.

EventHandlerTests compares 1,024 mixed delegate add/remove operations with native delegate lists, checks nested snapshot changes/errors, 256 concurrent producers and weak-reference release from disposed publishers. All 18 physical accessors each deliver 128 prepared first one-shot callbacks at strict zero owner/all-thread managed bytes. EventConnectionPhysicsTests adds a real full selected CPU/GPU Area transition with 128 one-shot listeners and a permanent listener. Serial/separate numerical profiles use CPU-only Vulkan for GPU kernels; native allocations, hardware performance, real-window FPS and foreign acceptance remain open.

See [typed handler storage](../classes/EventHandlers.Generic.md) and [ADR 0010](../decisions/core-object-runtime.md#adr-0010).

## ZIP asset preparation

[ZIP archives](../components/zip-archives.md) add owned ZIPReader/ZIPPacker sessions, streamed full-level deflate, ZIP64/prefix/append preservation, portable permission/time metadata and complete length/CRC validation. Bundled scene/resource ZIP round trips execute real GPU/compatibility rendering; they do not introduce automatic extraction or pack mounts.

[Typed undo history](../components/undo-history.md) adds thread-owned reversible Action/property authoring, named cursor/version/merge policies and conditional non-Resource ownership. It is public runtime infrastructure for game/tool/editor consumers; it does not introduce an editor-only dependency or dynamic property lookup. See ADR 0105 and the component for executed behavior and verification boundaries.

- [PCKPacker](../classes/PCKPacker.md) and static ProjectSettings.LoadResourcePack execute the [resource pack](../components/resource-packs.md) writer, mounting and shared FileAccess/DirAccess/ResourceLoader path.

- [Runtime diagnostics](../components/diagnostics.md): retained Logger registration, typed OS message/error ingress, Engine output flags and immutable C# ScriptBacktrace metadata integrate lifecycle failures and lazily loaded native logs. CLR live-variable debugging remains a separate backend.

Configured movie startup is available through Engine.RunMovie/RunMovieAsync, with typed file/FPS settings, feature overrides, explicit Run argument precedence and a per-recording presentation override. It shares the existing frame/PCM/cleanup path for every built-in codec. See [project movie startup](../components/movie-writing.md#project-movie-startup) for executable checks and platform limits.
