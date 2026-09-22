# Resource base component

Last updated: 2026-09-23

## Scope

The resource base component provides reusable managed engine data with change publication, display and path identity, scene-instancing configuration, typed copying, and graph-preserving duplication.

## Owned types

- [`Resource`](../classes/Resource.md)
- [`DeepDuplicateMode`](../classes/DeepDuplicateMode.md)

Both types are production members of `Electron2D.dll`.

## Runtime flow

1. A resource starts with empty name/path/scene ID and scene-local behavior disabled.
2. Ordinary path assignment claims one process-wide ordinal path; takeover is explicit, while raw cache assignment bypasses ownership.
3. Custom setters call `EmitChanged` only after committing meaningful content.
4. Copying resets the target, shallow-copies stored state, preserves target identity, and emits one coalesced change.
5. Duplication creates and validates exact-type targets, registers them before recursive copying, and therefore preserves aliases and cycles.
6. Packed-scene instantiation uses the same graph machinery to duplicate local resource graphs. It assigns the new scene root before setup, invokes setup once per local duplicate, and transfers every created resource to root ownership.
7. Failure disposes every partial duplicate in reverse order; normal resource disposal unregisters its path, clears local-scene association, and clears subscribers.

## Dependencies

The component depends on Core object lifetime and typed property descriptors plus .NET collections, weak references, locking, and cryptographic random generation. `Resource.GetLocalScene()` introduces one narrow dependency on Scene's [`SceneNode`](../classes/SceneNode.md), while the [Packed scenes](packed-scenes.md) component consumes Resource duplication. ADR 0023 records this intentional in-assembly cycle.

The managed [Images](images.md) component derives from this base and implements concrete CPU buffer duplication without changing base lifetime. Future asset serialization, rendering handles, and editor/import metadata may consume this component but are not implemented dependencies today. The base still has no dependency on `PackedScene`, `SceneTree`, or file formats.

## Invariants and error behavior

- Registered nonempty paths have at most one live owner.
- A duplicate has the exact runtime type and no copied path or scene ID.
- Derived custom stored state is never silently assumed copyable; duplication support is explicit.
- Deep graphs preserve identity, repeated edges, cycles, and explicit typed always/never-duplicate fields.
- No failed duplication leaves a live partial graph owned by the caller.
- A scene-local duplicate knows its owning root before setup; every created duplicate is disposed with that root or during failed instantiation rollback.
- Change and setup callbacks are synchronous and may fail after documented state commitment.
- Copying derived state is not transactional, so failed copies still publish one final change indication.

Base state supports concurrent access, and copy batches on one target serialize without absorbing another thread's change publication. Derived state and callbacks have no automatic synchronization or thread affinity.

## Current implementation status

Name/path/scene configuration, path cache ownership, raw cache paths, changed/setup events, local-scene association, reset, shallow copy, all deep-duplicate modes, alias/cycle handling, rollback cleanup, typed property exposure, packed-scene local duplication/setup/ownership, and deterministic disposal are implemented and verified.

## Exclusions and deferred integration

The component does not expose a public manual reference counter: managed memory remains owned by the runtime, while `IDisposable` performs deterministic logical cleanup. A future asset manager may use internal disposable leases to retain shared native-backed payloads and release them when the last lease ends; that mechanism is deferred until the first concrete loader and native-backed asset establish real ownership transitions. Managed `Image` does not trigger native leases; ADR 0039 defines its codec/texture triggers. Renderer IDs, asset loading/saving, imports, automatic file-serialization discovery, and editor path IDs remain deferred. Runtime packed scenes implement per-instance local duplication and automatic setup; persistent endpoint storage and disk scene/resource formats remain absent.

## Verification

The executable checks in `tests/Electron2D.Tests/Program.cs` cover positive, negative, concurrent, cyclic, callback-failure, partial-copy, cleanup, and packed-scene local-resource paths. Release build and generated XML documentation validation are part of the repository-wide completion checks.
