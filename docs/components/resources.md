# Resource base component

Last updated: 2026-10-28

[Image-array resources](texture-arrays.md) execute copied homogeneous layers, typed samplers/defaults, shape-aware reload, source archives, actual GPU layer/mip upload and owned/proxy RIDs. Ordinary texture drawing retains its separate resource branch. Current native evidence is Linux Wayland GPU; compatibility rejects shader use. Copied byte, normalized/float, block and unsigned-integer images execute; foreign/native-allocation acceptance remains separate.

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
5. Duplication checks each returned identity against its invocation construction scope, including nested, Task and engine-worker creation. An unrelated object created concurrently rejects before copying or ownership acquisition. Fresh exact-type targets register before recursive copying, preserving aliases and cycles; distinct sources cannot reuse an earlier created target.
   Both ordinary copying and duplication retain inherited translation enablement/domain alongside resource name/locality. These settings apply before custom copying and the final coalesced change notification; scene-local setup sees the completed inherited state.
6. Packed-scene instantiation uses the same graph machinery to duplicate local resource graphs. It assigns the new scene root before setup, invokes setup once per local duplicate, and transfers every created resource to root ownership.
7. Failure disposes every partial duplicate in reverse order; normal resource disposal unregisters its path, clears local-scene association, and clears subscribers.

## Live script copy and cache behavior

[Typed Resource bindings](scripting.md#resource-bindings) install independent runtime state on newly constructed duplicates and archive owners before stored restoration. Public CopyFromResource retains target code. File replacement and reused scene-local targets switch to the prepared binding before restoring values, preserving identity and retaining partially committed graph ownership on failure. Source notifications follow ownership commitment and precede the final coalesced Changed. Prepared Script reference remapping retains the already initialized binding/state. Source assets remain scalar-only and cannot own script bindings.

## Per-referrer path IDs

`Resource.GetIDForPath(path)` and `SetIDForPath(path, id)` expose the shared
process authoring cache. Its key is the exact ordinal pair of referring file path
and the resource's current `ResourcePath`. No normalization is performed; empty
paths remain keys. Missing entries return empty; an empty ID removes an entry.
IDs are opaque strings and do not use `ResourceSceneUniqueID` validation.

Raw aliases, path takeover and newly loaded objects at the same path see the same
entry. Changing a resource path selects another key; clearing/disposal does not
erase previous keys. The cache retains only strings, which remain until explicitly
cleared or process exit. It does not retain resources, change ownership, emit
`Changed`, or participate in resource copying/reset and geometry invalidation.
Operations serialize with path assignment, so each call samples one current path.

[ADR 0013](../decisions/resources.md#adr-0013) makes this authoring metadata
available without editor UI under ADR 0090. Custom file formats explicitly persist
and restore their labels. The cache itself is process-local; built-in archive
bindings, embedded scene IDs and numeric resource UIDs retain their own contracts.
This API does not add an editor-mode setting or change the built-in file schema.

The public-only `--resource-path-ids` consumer exercises the base resource, every
implemented concrete shape, an application Shape, PhysicsMaterial, World and
PackedScene. It checks exact pairs, raw/registered aliases, takeover, copy/duplicate,
null/disposed input, concurrent callers and weak resource retirement. A real custom
ResourceFormatSaver/Loader writes a scene referencing two external shape files,
keeps byte-identical dependency labels across saves, leaves file/cache unchanged
on injected encoding failure and restores aliased resources in a fresh process.
Loaded scenes execute actual motion and exact query identities. Each parent profile
warms 128 full frames and measures another 128 including ID reads/writes at zero
owner/all-thread managed bytes; file work is outside this bracket. The default
resource-base suite also runs the inherited cache checks. Evidence and precise
platform/build limits: `bin/physics-resource-path-ids/2026-10-16/`.

Twelve Linux x64 profiles pass on the final source: CPU/software-GPU/registered
selection in both numerical thread policies, under Release CoreCLR and strict
NativeAOT. All twelve fresh child processes reconstruct and run the physical
scene; process maps show lavapipe only for GPU cases, no NVIDIA driver and no
CoreCLR in native executables. The related resource-base, archive and local-scene
suites pass. Release/API, three format checks and wiki generation/check pass;
six PDB/source sets have no missing or mismatched declared sources. These are
bounded file/identity/frame checks; complete native memory, foreign platforms,
real windows and target-hardware performance remain open.

## Dependencies

The component depends on Core object lifetime and typed property descriptors plus .NET collections, weak references, locking, and cryptographic random generation. `Resource.GetLocalScene()` introduces one narrow dependency on Scene's [`Node`](../classes/Node.md), while the [Packed scenes](packed-scenes.md) component consumes Resource duplication. ADR 0023 records this intentional in-assembly cycle. The base references `Script` to preserve ordinary script edges during duplication; concrete typed hooks still own their payload copying.

The managed [Images](images.md) component derives from this base and implements concrete CPU buffer duplication without changing base lifetime. The [Resource loading](resource-loading.md) component now reads registered paths from this base cache and loads `ImageTexture` files without adding a Resource-to-loader dependency. Future asset serialization, rendering handles, and editor/import metadata may consume this component. The base still has no dependency on `PackedScene`, `SceneTree`, or file formats.

## Invariants and error behavior

- Registered nonempty paths have at most one live owner.
- Registered equal path writes are silent; equal raw writes invoke hooks and stay unregistered. Raw assignment unregisters only the caller's prior claim. Takeover commits displaced/new metadata before attempting both hooks; callback failure does not roll back ownership. Synchronous content events use subscription order and the publishing thread, with independent subscription snapshots for reentrant delivery.
- A duplicate has the exact runtime type and no copied path or scene ID.
- Failed factories preserve rejected pre-existing resources, including active physical shapes, materials and worlds. Newly acquired invalid/partial targets retain reverse-order rollback cleanup.
- Derived custom stored state is never silently assumed copyable; duplication support is explicit.
- Deep graphs preserve identity, aliases and cycles across selected duplicated edges. Sharing/duplication is selected before cache lookup; forced/root acquisitions do not redirect ordinary shared edges, including external source-root references under Internal. Scene non-local edges follow the same ordering rule.
- Ordinary Script edges remain shared across deep modes, independently of cached forced copies. Local script flags and isolated authoring keep their distinct scene policies; root/force wrapper copies retain the compiled registration.
- No failed duplication leaves a live partial graph owned by the caller.
- A scene-local duplicate knows its owning root before setup; every created duplicate is disposed with that root or during failed instantiation rollback.
- Change and setup callbacks are synchronous and may fail after documented state commitment.
- Copying derived state is not transactional, so failed copies still publish one final change indication.

Base state supports concurrent access, and copy batches on one target serialize without absorbing another thread's change publication. Derived state and callbacks have no automatic synchronization or thread affinity.

## Current implementation status

The typed base operations, deep policies and packed-scene lifecycle execute. The generic copy/duplicate family is audited below; each concrete resource owns copying its stored fields through the explicit hooks in ADR 0013. Full inherited live-script binding and whole-class acceptance remain open on their declaring rows.

## Copy and duplicate contract

The three base operations `CopyFromResource`, `Duplicate` and `DuplicateDeep`
implement ADR 0013's typed adaptation. The pinned [copy loop](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/core/io/resource.cpp#L218-L243),
[edge and construction rules](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/core/io/resource.cpp#L260-L405)
and [entry points](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/core/io/resource.cpp#L486-L525)
define the reference behavior. Explicit factories and custom-state hooks replace
runtime property discovery; typed exceptions replace error codes. This audit
changes no runtime API or architectural decision.

| Contract | Executable evidence |
| --- | --- |
| Fresh exact-type root, inherited stored metadata, preserved target identity and silent self-copy | `VerifyResources` and `ResourceStoredState`: base, nine production shapes, an application Shape, PhysicsMaterial and World; 130 mode/translation combinations per host |
| Shallow containers; deep None/Internal/All; forced/never edges; aliases and cycles selected per edge | `ResourceEdgePolicy`: 390 mode/path/order cases and 30 root cycles; `ScriptGraphs` preserves ordinary Script references and compiled registrations |
| Local-scene selection, independent sessions and correct ownership | `ResourceEdgeScenes`, packed/replacement/isolated-authoring suites and actual fresh-process resource-identity scene loads |
| Rejected borrowed, wrong-type, reused or malformed factory targets | `ResourceFactoryOwnership`, `FactoryConstructionChecks` and base negative cases; caller-owned sources remain live |
| Preflight before reset/metadata/events; accepted partial failures and recovery | `RunShapeGeometry`, `RunWorldCopies`, `ResourceCopyFailures`; real selected frames and queries retain accepted body/shape/space identities |
| Coalesced, nested and reentrant changes; all rollback disposers attempted in reverse order | `VerifyResources` and `ResourceCopyFailures`; original, observer and cleanup exceptions retain their identities/order |

Live native Script schemas add typed stored fields after the ordinary custom copy hook. Matching target names and exact value types receive those fields; public shallow copying retains target code and container aliases. Deep duplication uses the existing per-edge resource policy and independent containers. These injected fields do not require duplicate writes in derived hooks; ordinary CLR fields still do. See [the schema contract](scripting.md#native-property-schemas).

Each derived type must supply its complete stored-field copy and appropriate
container handling. An unsupported application type fails explicitly. A missing
field in a concrete implementation belongs to that type's audit; arbitrary caller
hooks cannot become an unbounded prerequisite for the base session. The base
class still inherits the open Object script-binding/initialization requirements.

Current qualification is retained in
`bin/physics-resource-contract/2026-10-25/qualification.json`: 38 profiles pass
(20 CLR and 18 NativeAOT). The existing
`--resource-identity`, `--shape-geometry` and `--world-copies` consumers execute
CPU, registered and NVIDIA Vulkan GPU worlds in owner/separate-thread modes,
both CLR and strict NativeAOT. Related managed base and local-scene suites cover
the generic graph contract. Resource-identity hosts include real child-process
scene loads and 128 warm/128 measured active frames with queries at strict 0/0
owner/all-thread managed bytes; all 36 physical frame brackets pass at 0/0.
These are functional/allocation results; the
concurrent ordinary suite and GPU timings are separate evidence.

Scene-local live Resource callbacks use [ResourceScriptCallbacks](../classes/ResourceScriptCallbacks.md) after native setup and stored restoration. They share common object callback installation and cleanup; an acquired graph whose setup fails remains owned by the existing rollback. See [common native callbacks](scripting.md#common-native-object-callbacks) for qualification and remaining resource hooks.

## Exclusions and deferred integration

The component does not expose a public manual reference counter: managed memory remains owned by the runtime, while `IDisposable` performs deterministic logical cleanup. The first synchronous image-texture loader uses this weak path cache; it adds no manager-owned native payload or lease. A future shared-payload asset manager may use internal disposable leases when a concrete ownership transition requires them. Renderer IDs, further concrete payload schemas, imports, automatic reflection-based discovery and wider editor/import workflows remain deferred. Runtime packed scenes implement per-instance local duplication and automatic setup; persistent endpoint storage remains absent; typed archive resource/scene persistence executes through the resource-file component.

## Verification

The executable checks in `tests/Electron2D.Tests/Program.cs` cover positive, negative, concurrent, cyclic, callback-failure, partial-copy, cleanup, and packed-scene local-resource paths. Release build and generated XML documentation validation are part of the repository-wide completion checks.

[Public physical consumers](../../tests/PhysicsBackendExtension.Consumer/BackendChecks.ResourceFactories.cs) reject pre-existing exact/wrong-type targets, nested borrowed targets and reuse of an acquired target for a distinct source. They preserve attached custom shape geometry/RID, material and world identities, then execute active simulation and a physical query. The resource-identity host runs this fixture on CPU, software GPU and registered backends in serial/separate-thread modes; broader platform and hardware performance acceptance remain separate.

The current cohort also passes all six profiles as strict NativeAOT consumers, twelve real child-process physical scene loads, the common sixteen CPU/GPU physical suites, resource archives and packed-scene/subtree authoring regressions. Twelve resource-identity brackets warm 128 and measure 128 full active frames with queries at 0/0 owner/all-thread managed bytes. Manifests and results are retained in `bin/physics-resource-factories/2026-10-14/validation.json`; GPU execution uses explicit lavapipe. Full derived-resource duplication coverage and native/hardware performance remain open.

[Inherited stored-state consumers](../../tests/PhysicsBackendExtension.Consumer/BackendChecks.ResourceStoredState.cs) audit thirteen resources: the base, all nine production shape types, an application Shape, PhysicsMaterial and a shared World. Five shallow/deep modes with translation enabled/disabled give 130 combinations per host; each also checks CopyFromResource identity, complete event state, silent self-copy, exact geometry/material values and actual translated messages. Polygon edits preserve independent copied storage. Scene-local setup and real fresh-process physical files retain the inherited policy. The current cohort passes 26 managed profiles and twelve strict NativeAOT profiles, with twelve physical child processes and 36 zero-byte owner/all-thread brackets after 128 warm/128 measured active frames. Shape/world callback, geometry-query and lifetime preflight reject without changing inherited state. Evidence is retained in `bin/physics-resource-stored-state/2026-10-14/validation.json`; software GPU is functional evidence, and wider derived-resource/native/hardware/platform acceptance remains open.

[Script graph consumers](../../tests/PhysicsBackendExtension.Consumer/BackendChecks.ScriptGraphs.cs) preserve ordinary shared script identity in scalar, array and dictionary fields under all five copy modes and both force orders. Explicit root/force wrappers remain independent, scene-local script instances share one acquired wrapper, non-local direct/nested references remain borrowed, and the real RESET authoring path isolates snapshots. Actual selected physical scene/query execution and retirement preserve those lifetimes. The cohort passes 28 managed and twelve strict NativeAOT profiles, including a real registered compiled project class through ScriptTests and full RESET save regression, with 36 warmed physical brackets at 0/0 owner/all-thread bytes. The existing twelve fresh-process physical file consumers also pass. NativeAOT source registration still requires its unavailable file-backed assembly/PDB; these native graph checks use actual unbound Script assets. Exact evidence and pinned comparison hashes are retained in `bin/physics-script-graphs/2026-10-14/validation.json`. Wider inherited/native/hardware/platform acceptance remains open.

[General edge-policy consumers](../../tests/PhysicsBackendExtension.Consumer/BackendChecks.ResourceEdgePolicy.cs) audit thirteen base/physical families across 390 mode/path/force-order combinations and thirty source-root cycle cases. Ordinary, forced and never-copy scalar/array/dictionary edges keep their policy before remap lookup. The old external-root Internal-cycle expectation was corrected to retain its original root; All and built-in Internal selected cycles still remap. Twelve scene combinations cover shape/material/world, both local flags and force orders, direct/nested references, actual body material/shape/world identities, selected physics/query and acquired-target retirement. Current 28 managed plus twelve strict NativeAOT profiles pass, including earlier Script, RESET, archive and source-authoring regressions, with 36 warmed 128-frame brackets at 0/0 owner/all-thread bytes. Evidence and pinned source hashes are retained in `bin/physics-resource-edges/2026-10-14/validation.json`; hardware/native/platform and full inherited acceptance remain open.

[Lifecycle contract consumers](../../tests/PhysicsBackendExtension.Consumer/BackendChecks.ResourceLifecycle.cs) audit reset and typed hooks, registered/raw/promoted/taken-over paths, weak ordinal cache ownership, empty/equal/conflicting/reentrant/concurrent/error cases, collection/disposal, built-in path classification and synchronous/reentrant/error/thread event delivery. Every base/physical family runs the public operations before actual selected full physical frames; existing shape/world phase rejection and copy batching regressions also pass. Current 28 managed and twelve strict NativeAOT profiles complete with 36 zero-byte warmed frame/query brackets. The ten matching reference rows are Implemented under ADR 0013's managed lifetime, explicit error, cache and typed-event adaptations. Duplicate/copy, remaining renderer RID, editor path-ID producers and the full Resource inheritance audit remain open. Evidence and exact pinned hashes are retained in `bin/physics-resource-lifecycle/2026-10-14/validation.json`.

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

Internal resource graph IDs are staged before persistence: first authored IDs remain stable, missing and duplicate IDs are generated without colliding with reserved authored values. IDs commit only after atomic writing, while source path replacement follows its explicit flag. Post-write path-hook failures attempt later paths and retain the committed IDs/file. Read validation rejects duplicate resolved embedded paths before factories or cache replacement, including legacy fallback indices. Public-only physical scene identity checks exercise actual saved aliases and backend simulation; exact executable evidence and platform limits are recorded with the current validation cohort.

## Named scene assets

[ResourcePreloader](resource-preloading.md) supplies case-sensitive named resource slots with numbered duplicate/rename handling and copied name enumeration. Existing PackedScene and archive graphs retain aliases, local-scene copying and file ownership through exact typed reserved slot recovery. The holder borrows ordinary assets and exposes no loading task. Runtime/test details and verification boundaries are in the component.

[Copy-failure consumers](../../tests/PhysicsBackendExtension.Consumer/BackendChecks.ResourceCopyFailures.cs) exercise a live application Shape: reset failure before stored writes, partial shallow state, combined operation/observer failures, skipped later subscribers, nested and final-observer reentrant batches, and recovery through actual selected frames/queries with stable body/shape/cache identity. A failed deep graph retires both acquired query shapes in reverse order despite two throwing cleanup hooks, preserves each exception and leaves sources live. Subsequent duplication uses an independent session. Exact enum values and invalid policies are checked alongside the existing full per-edge matrix. CPU/software-Vulkan/registered serial/separate managed and NativeAOT evidence is retained in `bin/physics-resource-copy-failures/2026-10-15/validation.json`; hardware timings, native allocator totals and complete derived-resource storage remain separate.

## Concurrent factory ownership

[FactoryConstructionChecks](../../tests/PhysicsBackendExtension.Consumer/FactoryConstructionChecks.cs) exercises unrelated concurrent resource/file/node/PackedScene results, preserved weak path-cache ownership, cross-returning independent factories, legitimate nested/Task/engine-worker creation, the worker scene-activation barrier, and release of object references from retained closed execution contexts. The former global construction-number cutoff admitted unrelated concurrently created objects. The common [construction scope](../classes/ElectronObject.md#factory-construction-provenance) replaces that timing assumption; broader derived resource storage coverage remains open.

Linux x64 verification on 2026-10-20 passes twelve managed profiles (worker, Resource base/local/archive, compiled Script, CPU script factories and six public physical resource profiles) plus six strict NativeAOT public profiles. The twelve public resource profiles cover CPU without an available GPU, hardware RTX 3090 Ti and a registered backend, each with owner/separate numerical execution; each checks a real fresh-process scene and 128 measured active frames after 128 warm frames at 0/0 owner/all-thread managed bytes. Runtime/test/consumer portable-PDB checks match all 2,419/909/259 expected source documents. Receipts: `bin/resource-factory-provenance/2026-10-20/managed-profiles.json`, `native-profiles.json` and `source-proof.json`. This establishes the construction ownership boundary on this host; cold construction cost, native allocations, foreign platforms, full derived storage and overall physical performance acceptance remain separate.

The full ordinary Linux test runner also passes in 675.771 seconds from the exercised working tree based on `0add63c6f0b825ed76f9fce7e20b44966c00dfb7` with this construction change. The preserved Release payload uses source-built native dependencies. All recorded payload/source hashes remain unchanged; 333 successful GPU telemetry samples, actual NVIDIA mappings and no software Vulkan mappings/new Xid accompany the run. This includes scene duplication, compiled script factories, worker allocation/context tests and threaded archive loading. Exact source/PDB fingerprints and the full result are in `bin/resource-factory-provenance/2026-10-20/validation.json` and `full/check.json`. The earlier full-run receipt remains historical evidence for its own frozen revision.
