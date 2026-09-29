# Project settings component

Last updated: 2026-09-30

## Scope

This Core component provides the process-wide typed settings registry, isolated registries, defaults/validation/revert metadata, feature overrides, unsaved/change-notification tracking, project persistence, project discovery, and directory-backed `res://`/`user://` resolution.

## Owned types

| Type | Role |
| --- | --- |
| [`ProjectSetting<T>`](../classes/ProjectSetting.Generic.md) | Immutable typed name/default/validator identity |
| [`ProjectSettings`](../classes/ProjectSettings.md) | Registry, metadata, override, persistence, event, and virtual-path owner |

The input action records are defined by the [Input runtime](input-runtime.md); this component registers six typed defaults and provides the internal typed group snapshot used by explicit `InputMap` reload. Two locale and nine pseudolocalization settings have executable consumers in [Localization](localization.md). Both types are implemented in [`src/Core/Config/ProjectSettings.cs`](../../src/Core/Config/ProjectSettings.cs).

`DefaultScrollDeadzone` registers `gui/common/default_scroll_deadzone` as a signed integer defaulting to zero. Each new [ScrollContainer](../classes/ScrollContainer.md) samples its active feature override once during construction; an existing container keeps its own writable `ScrollDeadzone` value.

`InputUISelect`, `InputUIPageUp`, `InputUIPageDown` and `InputUIMenu` permanently register typed action defaults for list selection, paging and menu activation. `IncrementalSearchMaxIntervalMsec` registers `gui/timers/incremental_search_max_interval_msec` with a nonnegative 2000 ms default; [ItemList](../classes/ItemList.md) reads its active feature override for each typed-character search.

## Runtime flow

1. The component creates built-in application, timing, rendering, six GUI-focus input, two locale and nine pseudolocalization definitions and registers them in each registry.
2. Domains register additional `ProjectSetting<T>` definitions. A definition immediately claims and validates compatible values that may already exist in the loaded document.
3. Typed writes snapshot through the configuration serializer, validate, commit under the registry lock, mark the base name unsaved, increment `Version`, and queue one change notification.
4. Typed reads choose base/default or the first active feature override, then return a scalar cache or independent mutable snapshot.
5. The host may call `FlushChanges`; the process Engine does this after each successful process frame.
6. Loads build and validate a complete temporary document before replacement. Saves apply metadata order and delegate atomic file replacement to `ConfigFile`.
7. Virtual paths resolve lexically against immutable snapshots of the current project/user roots; `DirAccess` can capture both roots under one lock for a two-path operation.

## Dependencies

The root renderer samples `RenderingMethod`, `RenderingFallback` and `DefaultClearColor` from the process registry during Engine.Run startup. Their feature overrides use the same typed lookup as other settings. They do not switch an active backend or continuously update its clear color. GPU canvas sampler creation uses `UseNearestMipmapFilter` sampled at backend startup; newly constructed viewports read `AnisotropicFilteringLevel` (default 4×). New Windows also read SnapTransformsToPixel and SnapVerticesToPixel at construction; explicit window configuration then takes precedence. These reads apply active feature overrides and do not reconfigure existing consumers when the project setting changes. [ProjectSettings](../classes/ProjectSettings.md) documents keys, defaults and validation; [canvas rendering](canvas-rendering.md) documents startup fallback and runtime controls.

- Core `ElectronObject`, typed `PropertyDescriptor`, `ConfigFile`, `Engine`, `FileAccess`, and `DirAccess` integration.
- .NET path, filesystem, runtime-platform, architecture, collection, and synchronization primitives.
- No SDL, renderer, input, audio, collision physics, scripting, resource pack, asset loader, networking, or editor dependency.

## Invariants

- Public values remain generic and exact-definition keyed; no universal value API exists.
- Names are ordinal and case-sensitive; feature tags are normalized and case-insensitive.
- Built-in definitions cannot be removed; the process registry cannot be disposed.
- Failed serialization, validation, parse, file read, or re-entrant load mutation cannot partially replace registry state; user validators must be deterministic, thread-safe, and free of registry mutations.
- A registered base value equal to its current initial value is implicit and omitted from persistence; feature overrides remain explicit.
- Save failure does not clear unsaved-value tracking; main load/path reset/successful save establishes a clean persistence boundary, while custom merge preserves unsaved names that predated it.
- Notification batches are independent of persistence state. One flush exposes and then clears at most one batch; handler-created changes wait for a later flush.
- Unknown persisted entries survive until a domain registers a matching typed definition.
- Virtual paths reject lexical escape but do not claim symbolic-link sandboxing.
- No disk or serializer path is considered real-time safe; warmed Engine scalar reads remain allocation-free.

## Current implementation status

Implemented and covered by the executable harness. The process singleton is registered in Engine; Engine timing properties read active project-setting overrides and write their typed base definitions. Two locale and nine pseudolocalization settings are sampled at Engine startup and their eight transform options can be reapplied by TranslationServer during a run; the enabled switch remains a runtime choice until the next startup. Built-in features include build configuration, managed runtime, current OS when recognized, and process architecture. Custom features are runtime-managed.

The typed `physics/common/physics_interpolation` setting defaults false and initializes each new SceneTree. An active tree retains its own flag when the setting changes; callers can set `SceneTree.PhysicsInterpolation` immediately. Its renderer integration affects displayed 2D transforms and camera scroll without changing logical values.

Four typed `physics/2d/default_*` settings supply the new physics world's gravity strength (980), unnormalized direction (0, 1), linear damping (0.1/s) and angular damping (1/s). The world samples feature overrides when its first body or area attaches; existing worlds retain those values after later setting changes. The [physics bodies component](physics-bodies.md) owns their fixed-step use. Finite values are required; signed strength and damping are accepted.

## Exclusions and deferred integration

- Global script-class discovery requires a scripting domain.
- Resource pack loading, exported archive mounts, and non-directory `res://` require file-access/resource-pack domains.
- Editor-specific hints, override layers, hidden-prefix UI, and settings dialogs require an editor.
- Rendering registers backend selection, startup fallback, clear color and the implemented canvas mip/anisotropy defaults with its executable canvas integration. Four 2D physics world defaults are implemented; remaining settings enter with their owning input, audio, networking, physics or other domain capabilities. Three-dimensional settings will never be added.
- Symbolic-link resolution and hostile-filesystem confinement are outside the current lexical resolver contract.

## Verification

`tests/Electron2D.Tests/Program.cs` verifies registration, values, snapshots, validation, metadata, feature selection, changes/events, persistence/ordering, virtual paths, root-pair consumption by directory operations, failures, concurrency, disposal, and Engine integration. `LocalizationProjectSettingsTests` verifies typed locale and pseudolocalization defaults, project-file round trip, startup sampling, and reload; `WindowRuntimeTests` checks native startup sampling with the SDL dummy driver. `CanvasSamplingTests` checks the sampling keys and viewport construction; `PhysicsInterpolationTests` checks tree construction sampling and runtime changes, with native pixels in [canvas rendering](canvas-rendering.md#physics-interpolation). [PhysicsAreaFieldTests](../../tests/Electron2D.Tests/PhysicsAreaFieldTests.cs) checks 2D physics default identities and new-world sampling. Temporary directory-backed projects are used; resource packs, editor UI and crash-time filesystem behavior remain untested.

## Decisions

- [0019: Typed project settings and virtual paths](../decisions/core-data-io.md#adr-0019)
- [0018: Typed configuration files](../decisions/core-data-io.md#adr-0018)
- [0016: Process-wide Engine runtime](../decisions/core-object-runtime.md#adr-0016)
- [0001: Typed C# without Variant](../decisions/product.md#adr-0001)

DebugPathsColor defines debug/shapes/paths/geometry_color, defaults to finite Color(0.1, 1, 0.7, 0.4), and is registered as a built-in nonbasic setting. SceneTree samples active feature overrides at construction. Path debug drawing consumes this color through existing canvas commands; later setting changes affect later trees. SceneDiagnosticsTests checks nonfinite rejection without mutation; native PathRenderingTests verifies construction-time capture and actual pixels.

## Canvas render time

[Canvas animation intervals](canvas-rendering.md#animation-intervals-and-rectangles) use captured scaled process steps and a live typed rollover setting; ordered transform state is replayed alongside retained geometry. The clock is per Engine.Run and also supplies the optional GPU fragment [TIME built-in](shader-materials.md#render-time).
