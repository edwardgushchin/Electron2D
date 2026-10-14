# Resource base component

Last updated: 2026-10-14

[Image-array resources](texture-arrays.md) execute copied homogeneous layers, typed samplers/defaults, shape-aware reload, source archives, actual GPU layer/mip upload and owned/proxy RIDs. Ordinary texture drawing retains its separate resource branch. Current native evidence is Linux Wayland GPU; compatibility rejects shader use. Compressed/integer formats and foreign/native-allocation acceptance retain exact dependencies.

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
4. Copying first validates the target engine resource authoring policy, then resets it, shallow-copies stored state, preserves target identity, and emits one coalesced change. Shape preflight checks geometry/access hooks and all live borrowers before any base-state, reset, ownership or Changed effects. Arbitrary failures after accepted preflight retain the documented non-transactional behavior.
5. Duplication creates and validates exact-type targets, registers them before recursive copying, and therefore preserves aliases and cycles.
6. Packed-scene instantiation uses the same graph machinery to duplicate local resource graphs. It assigns the new scene root before setup, invokes setup once per local duplicate, and transfers every created resource to root ownership.
7. Failure disposes every partial duplicate in reverse order; normal resource disposal unregisters its path, clears local-scene association, and clears subscribers.

## Dependencies

The component depends on Core object lifetime and typed property descriptors plus .NET collections, weak references, locking, and cryptographic random generation. `Resource.GetLocalScene()` introduces one narrow dependency on Scene's [`Node`](../classes/Node.md), while the [Packed scenes](packed-scenes.md) component consumes Resource duplication. ADR 0023 records this intentional in-assembly cycle.

The managed [Images](images.md) component derives from this base and implements concrete CPU buffer duplication without changing base lifetime. The [Resource loading](resource-loading.md) component now reads registered paths from this base cache and loads `ImageTexture` files without adding a Resource-to-loader dependency. Future asset serialization, rendering handles, and editor/import metadata may consume this component. The base still has no dependency on `PackedScene`, `SceneTree`, or file formats.

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

The component does not expose a public manual reference counter: managed memory remains owned by the runtime, while `IDisposable` performs deterministic logical cleanup. The first synchronous image-texture loader uses this weak path cache; it adds no manager-owned native payload or lease. A future shared-payload asset manager may use internal disposable leases when a concrete ownership transition requires them. Renderer IDs, further concrete payload schemas, imports, automatic reflection-based discovery and editor path IDs remain deferred. Runtime packed scenes implement per-instance local duplication and automatic setup; persistent endpoint storage remains absent; typed archive resource/scene persistence executes through the resource-file component.

## Verification

The executable checks in `tests/Electron2D.Tests/Program.cs` cover positive, negative, concurrent, cyclic, callback-failure, partial-copy, cleanup, and packed-scene local-resource paths. Release build and generated XML documentation validation are part of the repository-wide completion checks.

## TLS security resources

X509Certificate and CryptoKey implement copied Resource state, change/failure behavior and typed `.crt`/`.key` ResourceLoader integration. [TLS](tls.md) owns retention and backend boundaries; active sessions prevent payload replacement/disposal. These formats do not establish a general ResourceSaver or editor serialization.

## Typed file integration

See [resource-file contracts](resource-files.md) for registered typed schemas, cache/UID resolution, file-root and scene-instance ownership, public extension hooks and exercised verification. File operations allocate outside frame processing. UID paths resolve through the permanent catalog before directory-backed path resolution; unknown UIDs fail explicitly. The archive profile does not add an editor, arbitrary import/remap rules or every resource schema.

## Background loading

[Threaded resource loading](threaded-resource-loading.md) executes typed requests on WorkerThreadPool under ADR 0013/0104. Isolated preparation does not mutate registered cache objects; owner publication preserves cache modes and shared graph leases. Protected remapping handles stored/PackedScene/shader references. AsyncGallery verifies separately authored scenes with real background dependencies and native rendered output. Further import/pack/editor/schema work retains its own coverage dependencies.

Standalone Shape.ResetState uses the same preflight before OnResetState. The public-only regression rejects reset during numerical/access/geometry hooks, checks no custom-reset side effects, and preserves allowed quiescent reset without Changed. Copying invokes reset exactly once after its own accepted preflight.

[World copy preflight](../components/worlds.md#world-resource-copy-preflight-2026-10-14) now refuses bound/expired/release-restricted runtime replacement before Resource metadata, reset, ownership and Changed effects. Public-only CPU/GPU/registered serial/separate checks retain aliases, accepted errors, actual simulation/query identity and zero warmed allocations; broader inherited/numerical/native/performance gates remain open.

Scene-local physical resource graphs now preserve borrowed non-local identities for both built-in and external paths. Local shapes/resources retain aliases and cycles within each instance and isolate geometry between instances. Setup sees the complete detached scene once per local duplicate. GetLocalScene borrows immutable weak object identity, returns null when the root starts disposal or is collected, and transfers with ownership on Node.ReplaceBy. Ordinary deep duplication and explicit force/isolated-authoring policies retain separate rules. Public-only --local-scenes exercises CPU/GPU/registered ordinary/separate worlds, actual query identities, GC retirement, setup rollback and strict 0/0 managed bytes for 128 warm/128 measured two-scene intervals; the connected base/packed/replacement/authoring suite also passes. Evidence is retained under bin/physics-local-scenes/2026-10-14. Full inherited, native allocation, foreign-platform and hardware/window/performance acceptance remain open.

The final active-body cohort also passes strict current-source NativeAOT publication and six native CPU/GPU/registered ordinary/separate profiles. Seven managed profiles cover the same local scenes plus base/packed/replacement/isolated authoring; the existing complete CPU/GPU physical suites pass against the identical retained runtime. Each of twelve local brackets drives two worlds with active rigid integration at 3 units/s and dt 1/60, position-increment tolerance 0.00005 for float accumulation, live queries and borrowed root reads: 128 warm/128 measured intervals remain 0/0 owner/all-thread managed bytes. Four native full-callback/PackedScene profiles also pass at zero warm bytes. GPU uses lavapipe only; separate local profiles observe two real solver threads for the two worlds. Source/runtime/ELF manifests and exact limits are retained in bin/physics-local-scenes/2026-10-14/validation.json.

## Named scene assets

[ResourcePreloader](resource-preloading.md) supplies case-sensitive named resource slots with numbered duplicate/rename handling and copied name enumeration. Existing PackedScene and archive graphs retain aliases, local-scene copying and file ownership through exact typed reserved slot recovery. The holder borrows ordinary assets and exposes no loading task. Runtime/test details and verification boundaries are in the component.
