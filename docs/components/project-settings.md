# Project settings component

Last updated: 2026-09-23

## Scope

This Core component provides the process-wide typed settings registry, isolated registries, defaults/validation/revert metadata, feature overrides, unsaved/change-notification tracking, project persistence, project discovery, and directory-backed `res://`/`user://` resolution.

## Owned types

| Type | Role |
| --- | --- |
| [`ProjectSetting<T>`](../classes/ProjectSetting.Generic.md) | Immutable typed name/default/validator identity |
| [`ProjectSettings`](../classes/ProjectSettings.md) | Registry, metadata, override, persistence, event, and virtual-path owner |

Both types are implemented in [`src/Core/Config/ProjectSettings.cs`](../../src/Core/Config/ProjectSettings.cs).

## Runtime flow

1. The component creates built-in application, timing and rendering definitions and registers them in each registry.
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

Implemented and covered by the executable harness. The process singleton is registered in Engine; Engine timing properties read active project-setting overrides and write their typed base definitions. Built-in features include build configuration, managed runtime, current OS when recognized, and process architecture. Custom features are runtime-managed.

## Exclusions and deferred integration

- Global script-class discovery requires a scripting domain.
- Resource pack loading, exported archive mounts, and non-directory `res://` require file-access/resource-pack domains.
- Editor-specific hints, override layers, hidden-prefix UI, and settings dialogs require an editor.
- Rendering registers backend selection, startup fallback, clear color and the implemented canvas mip/anisotropy defaults with its executable canvas integration. Remaining settings enter with their owning input, audio, networking, physics or other domain capabilities; three-dimensional settings will never be added.
- Symbolic-link resolution and hostile-filesystem confinement are outside the current lexical resolver contract.

## Verification

`tests/Electron2D.Tests/Program.cs` verifies registration, values, snapshots, validation, metadata, feature selection, changes/events, persistence/ordering, virtual paths, root-pair consumption by directory operations, failures, concurrency, disposal, and Engine integration. `CanvasSamplingTests` checks the new sampling keys and viewport construction; native sampling checks belong to [canvas rendering](canvas-rendering.md). Tests use temporary directory-backed projects and do not exercise resource packs, editor UI, SDL, or crash-time filesystem behavior.

## Decisions

- [0019: Typed project settings and virtual paths](../decisions/core-data-io.md#adr-0019)
- [0018: Typed configuration files](../decisions/core-data-io.md#adr-0018)
- [0016: Process-wide Engine runtime](../decisions/core-object-runtime.md#adr-0016)
- [0001: Typed C# without Variant](../decisions/product.md#adr-0001)
