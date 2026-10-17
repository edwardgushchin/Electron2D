# PhysicsServerBackend

Last updated: 2026-10-17

## Prepared body view acquisition

Body view acquisition now finishes pending direct-mode preparation before reading resolved fields, selecting a cached view or invoking its selected factory. The same boundary serves caller-created body-state extensions. A repeated preparation error creates no view, publishes no fields and does not advance the tick. Successful recovery preserves cached identity or creates one generation-qualified view; stale attachments remain rejected. See [the preparation contract](../components/physics-registered-adapter.md#body-view-acquisition-after-mode-failure).

[Curved custom contacts](../components/physics-registered-adapter.md#curved-custom-contacts)
verify the independent implementation's response and complete frame publication
when positional recovery retires a transient overlap. Private restore/replay,
motor/counterpart motion and strict warm frame bytes pass in both numerical modes;
arbitrary geometry and full extension acceptance remain open.

**Declaration:** `public abstract class PhysicsServerBackend : ElectronObject` · **Source:** [PhysicsServerBackend.cs](../../src/Servers/Physics/PhysicsServerBackend.cs) · **Component:** [Registered solver adapter](../components/physics-registered-adapter.md)

## Initial collider ownership

Object/canvas/priority authoring and reverse node membership commit before selected publication. Initial factory/attachment hooks already borrow the target world and node. Failed live publication queues complete metadata repair before live queries/nonzero steps; repeated preparation failure keeps the tick unchanged. Failed initial creation retires partial bindings. See [the contract and executable limits](../components/physics-registered-adapter.md#failed-collider-metadata-publication).

## Prepared collider inputs

The eleven Body/Area world/filter/object/canvas/priority getters read prepared common authoring in selected numerical/status hooks, including the borrowed numerical thread. Weak target disposal preserves assigned numeric IDs; transfer reports the actual current world. See [the executable contract and limits](../components/physics-registered-adapter.md#prepared-collider-metadata-reads).

## Prepared slot inputs

Body/Area GetShapeCount, GetShape and GetShapeTransform expose common prepared logical slots during numerical hooks, including the exact borrowed solver thread. These reads do not enter the solver or change resource ownership. Pending/disabled and disposed slots keep their logical meaning. See [the executable scope and guards](../components/physics-registered-adapter.md#prepared-logical-shape-reads).

## Pending server geometry

GetShapeGeometry rejects a pending raw shape before invoking its geometry supplier. After ShapeSetData commits, the helper returns the configured immutable canonical data. Backend RebuildShapes receives pending slots as Enabled=false; implementation algorithms must honor that flag.

## Description

Defines every world, collider/joint creation, query, lifecycle and history operation consumed by the registered adapter. All operations use logical RIDs and typed engine-unit values. This is the public scoped implementation role. Derive PhysicsServerExtension and register a fresh implementation factory with PhysicsServerManager. The world owns binding and lifetime; the implementation owns numerical state.

The engine retains authoring, logical identity, owner-thread/solver guards and observer timing. Backend-private simulation state stays with the implementation. Borrowed spans expire on return. Factories and hooks cannot transfer/release their current world or attachments. Solver/history objects reject repeated claims; retiring attachments preserve matching replacements.

Numerical hooks cannot change their world's sleep/contact policy, iteration count, default joint bias or local active flag through PhysicsServer. These writes reject before mutation or nested hook dispatch, including equal values. Authored getters stay readable outside solver-owned phases; ordinary post-solver game callbacks can edit the next interval's policies.

## Public properties

| Signature | Contract |
| --- | --- |
| `public bool IsFlushingQueries { get; }` | The current world's completed-notification delivery phase, shared with PhysicsServer.SpaceIsFlushingQueries. |

### IsFlushingQueries

True during force/integration/state/sleep callbacks, selected FlushQueries and
contact/Area observers. False when this world has no active delivery phase;
nested ordinary queries keep that phase and another world's phase is independent.
The read observes lifecycle only, allocates nothing and remains available during
terminal-error cleanup. Unbound/off-owner reads throw InvalidOperationException;
disposed implementation/world reads throw ObjectDisposedException. It does not
replace the existing structural, authoring, reentry or failed-world guards.

## Authored geometry access

`protected ShapeGeometry GetShapeGeometry(RID shape)` returns immutable current
local geometry for any live shape RID, without cloning the resource. Cached data
survives later edits/disposal. Every built-in family and custom canonical data are
available; this does not impose a particular collision algorithm on the backend.

Bound/live authorized-lane and failed-world guards apply. Numerical hooks may read
this authored input without solver reentry. Procedural suppliers execute under
world borrowing, reject authoring/lifetime writes, and restore the borrow after
errors. First-demand supplier allocations remain caller-controlled. Invalid or
retired shape RIDs reject; unbound/disposed/unauthorized implementations reject.
[Public pair conformance](../components/physics-registered-adapter.md#public-shape-pair-dispatch)
exercises the read, its guards and geometry revision lifetime.

## Public methods

| Signature | Contract |
| --- | --- |
| `public bool IsBodyExcludedFromMotionTest(RID body)` | Exact membership in this world's active motion-test body exclusions. |
| `public bool IsObjectExcludedFromMotionTest(ulong objectID)` | Exact membership in the active motion-test object exclusions. |

### IsBodyExcludedFromMotionTest

Reads the already supplied exclusions during `TestBodyMotion`, without resolving
the candidate RID or copying the list. Inert and foreign RIDs remain ordinary
membership values. Returns false outside a motion test. The world keeps the
original input lists if a caller edits its parameter resource during the hook;
those edits affect the next invocation. Nested ordinary queries and motion tests
in another world preserve this scope; same-world recursive motion tests reject.
Normal completion, hook failure and result-validation failure clear the scope.

### IsObjectExcludedFromMotionTest

Uses the same scope and guards, with a 64-bit object identity. It does not resolve
objects or treat object identity as a body RID; one object may represent several
raw bodies. Both methods reject unbound implementations, unauthorized-lane calls and
failed-world ordinary access with `InvalidOperationException`; disposed
implementations/worlds reject with `ObjectDisposedException`. These authored-input
reads remain available during numerical hooks, with false outside motion testing.

Inside a backend's candidate loop, the following partial snippet skips both
identity families; the complete compiled example is
[BackendChecks.Solver.cs](../../tests/PhysicsBackendExtension.Consumer/BackendChecks.Solver.cs).

```csharp
if (IsBodyExcludedFromMotionTest(candidateRID) ||
    IsObjectExcludedFromMotionTest(candidateObjectID))
    continue;
```

## Protected implementation hooks

| Declaration | Contract |
| --- | --- |
| `protected internal abstract bool HasFailure { get` | Owner status except the immediate Solve result, which shares the captured numerical lane. |
| `protected internal abstract bool ResultsReady { get` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract int PreparedBodyCapacity { get` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void Initialize()` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void SetActive(bool active)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void PrepareCheckpoint()` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void PreparePortableCapture()` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void ReadPortableBodies(ReadOnlySpan<RID> colliders, bool capture)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void InvalidateCollisionExceptions(RID collider)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void SetCollisionExceptions(ReadOnlySpan<(RID From, RID To)> pairs)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void SynchronizeCollisionExceptions()` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract PhysicsBackendCheckpoint CreateCheckpoint()` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void BeginPortableRestore()` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void CompletePortableRestore(ReadOnlySpan<PhysicsBackendOneWay> pairs, ulong tick)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract int GetPortableOneWayCount()` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract int CapturePortableOneWays(Span<PhysicsBackendOneWay> destination)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract float ReadPortableSleepTime(RID collider)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void ApplyPortableSleep(RID collider, float time, bool canSleep, bool sleeping)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void SetDebugContacts(int maximum)` | Prepares the nonnegative point cap before solving; zero disables capture and supports failed-world cleanup. Preserve the previous limit if preparation throws. |
| `protected internal abstract int ReadDebugContacts(Span<Vector2> output)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void SetSleepSettings(in PhysicsSleepSettings settings)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void SetContactSettings(in PhysicsContactSettings settings)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void SetSolverIterations(int value)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void SetConstraintDefaultBias(float value)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void PrepareSolverCapacity(int bodies, int shapes, int joints)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void PrepareMonitoringCapacity(int bodies, int shapes)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract PhysicsBackendStatistics ReadStatistics()` | Returns completed nonnegative active-body/candidate/island counts. Islands cannot exceed Active. Invalid samples fail the interval before common publication; cached readers retain the prior completed sample. |
| `protected internal abstract PhysicsBackendCollider CreateCollider(RID collider, bool area)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract PhysicsBackendJoint CreateJoint(RID joint)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal virtual PhysicsDirectSpaceState CreateDirectSpaceState(RID space)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal virtual PhysicsDirectBodyState CreateDirectBodyState(RID body)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void BeginStep(double delta)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void PrepareWorld(double delta, in PhysicsAreaFieldConfiguration fields)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void PrepareBodies(double delta)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void Solve(double delta)` | Complete numerical interval on the captured owner/separate lane; prepared inputs are read-only and publication waits for completion. |
| `protected internal abstract void SyncResults()` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void CompleteBody(RID body)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void CollectContacts()` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void ScanAreas()` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract int GetAreaOverlapCount(RID area)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract int ReadAreaOverlaps(RID area, Span<PhysicsBackendOverlap> output)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract bool AreaContainsPoint(RID area, Vector2 point, uint mask)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void EndStep(Exception? failure)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void FlushQueries()` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void EndSync(Exception? failure)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract PhysicsRayResult? IntersectRay(Vector2 from, Vector2 to, uint mask, ReadOnlySpan<RID> excluded, bool collideWithAreas, bool collideWithBodies, bool hitFromInside)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract List<PhysicsPointResult> CollectPointHits(PhysicsPointQueryParameters parameters, ReadOnlySpan<RID> excluded)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract List<PhysicsShapeResult> CollectShapeHits(PhysicsShapeQueryParameters parameters, ReadOnlySpan<RID> excluded)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract (float SafeFraction, float UnsafeFraction) CastMotion(PhysicsShapeQueryParameters parameters, ReadOnlySpan<RID> excluded)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract List<PhysicsBackendShapeContact> CollectShapeContacts(PhysicsShapeQueryParameters parameters, ReadOnlySpan<RID> excluded, int limit)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract PhysicsRestInfo? GetRestInfo(PhysicsShapeQueryParameters parameters, ReadOnlySpan<RID> excluded)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract bool CollideShapes(RID first, Transform firstPose, Vector2 firstMotion, RID second, Transform secondPose, Vector2 secondMotion, Span<Vector2> contacts, out int count)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract bool BodyCollideShape(RID body, int index, RID shape, Transform pose, Vector2 motion, Span<Vector2> contacts, out int count)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract bool TestBodyMotion(RID body, Transform from, Vector2 motion, float margin, bool recoveryAsCollision, ReadOnlySpan<RID> excludedBodies, ReadOnlySpan<ulong> excludedObjects, bool collideSeparationRay, PhysicsTestMotionResult result)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void Finish()` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |

## Verification and limits

[Independent solver probe](../../tests/Electron2D.Tests/PhysicsRegisteredBackendTests.cs) exercises scene/raw integration, ordinary views, queries, Area events, contacts, spring policy, local/portable restore, guarded lifetime and terminal solve failure. The separate public-only consumer implements a bounded circle/constraint model; it is a test solver, not a production backend. Arbitrary custom geometry and unexercised operation acceptance remain open under [ADR 0103](../decisions/physics-extensions.md#adr-0103).

`GetPortableOneWayCount` bounds the next `CapturePortableOneWays` span, even after retained storage has grown. Returned count cannot exceed that current span. Valid continuation requires distinct bound colliders, enabled logical shapes, implementation-validated nonnegative pieces and no duplicate unordered pair. `CompletePortableRestore` receives the translated local identities; the external consumer checks actual pass-through and contact impulses after restore.

Body force, mass, integration, motion, sleep, CCD and callback authoring now
reject before mutation during selected numerical hooks, including initial
attachment. Direct-body setters/forces use the same qualified numerical guard.
Guarded authored reads, ordinary post-solver callbacks and caller-created
body-state forwarding remain available; hierarchy/resource-lifetime guards still
cover every extension scope. See [body authoring](../components/physics-registered-adapter.md#borrowed-body-authoring).

Protected `RID[] GetCollisionExceptions(RID body)` copies guarded common authoring: ordered explicit entries followed by deduplicated active-joint targets, including inert values. It is readable in the owning numerical hook without observing solver state; snapshots belong to the caller and allocate. See [collision-exception response](../components/physics-registered-adapter.md#collision-exception-authoring-and-response) for full lifetime and verification limits.


Joint parameter authoring follows the same borrowed numerical boundary as body authoring. Typed facade reads retain common values; immutable PhysicsJointConfiguration and concrete joint hooks carry the full profile. The guard applies during initial attachment and live hooks.


The common joint lifecycle retains RID/type/frame authoring while CreateJoint returns fresh selected numerical state. Both incoming body owners preflight before make preparation; current scene/raw connection writes reject factory/attachment/release hooks. [Connection lifetime](../components/physics-registered-adapter.md#joint-connection-lifetime) records error/retry and allocation evidence.


[Motion result receiver identity](../components/physics-registered-adapter.md#motion-result-receiver-identity)
preserves the moving RID authored by SetCollision through selected-query validation.
A same-world result for another moving body rejects before caller publication;
no-result calls enforce the same rule. SetMotion misses retain normal requested-body
binding, and rejection restores scope for the next valid query. The numerical
motion/geometry acceptance remains broader than this identity guarantee.


[Configured body reads during numerical hooks](../components/physics-registered-adapter.md#parameter-reads-and-preparation)
use common retained authoring for all eighteen parameter/policy getters, including
actual selected Solve. Writes and numerical queries retain their rejection guards;
automatic geometry-dependent reads still need ordinary query access. The public-only
consumer verifies scene/raw ownership, failures and 128 full active frames at zero
owner/all-thread managed allocation. Complete numerical-extension acceptance stays open.


[Registered lifecycle census](../components/physics-registered-adapter.md#registered-lifecycle-census)
now verifies initialization, local/global activation, complete interval order,
completed statistics, failure/cleanup paths and 128 moving full frames per scene/raw
role at zero managed allocation through a public-only independent implementation.
Eight former unmapped rows have their actual typed projections; complete numerical
extension and platform/performance acceptance remain open.


[Selected diagnostic preparation](../components/physics-registered-adapter.md#diagnostic-capacity-and-publication) passes the contact-point cap to the numerical owner before solving, including registered implementations. Zero disables capture and remains available for failed-world cleanup; synchronized point/count reads retain the bounded shared snapshot. Full numerical and platform acceptance remain separate.


[Registered factory-selected views](../components/physics-registered-adapter.md#factory-selected-direct-views) now execute through a separate public-only consumer: cache identity, an initially empty contact snapshot, synchronized immutable publication before callbacks, result/lifetime validation, recreation and zero warm managed allocations. Independent caller-created body views keep direct hook dispatch; wider numerical-extension acceptance stays open.


[Independent constraint checks](../components/physics-registered-adapter.md#independent-pin-and-groove-response)
exercise real pin/groove response through immutable settings and selected
attachments on registered/CPU/GPU worlds, including strict warmed allocation and
private next-step replay. Complete numerical-extension acceptance remains open.


[Independent numerical CCD](../components/physics-registered-adapter.md#independent-numerical-ccd)
now verifies actual registered/CPU/GPU ray/shape motion, relative impact, remaining
time, full impulse and force/callback/tick budgets through common public checks.
128 warmed complete replay cycles require 0/0 owner/all-thread bytes. Registered
explicit-space controls check custom hollow geometry and terminal numerical
budget failure. This bounded fixture does not establish general geometry/rotation,
full extension, network or native/performance acceptance.


[Completed contact frame checks](../components/physics-registered-adapter.md#independent-completed-contact-frames)
share registered/CPU/GPU assertions for separated multi-peer CCD impacts, full
signed impulse sums, immediate private restoration, deepest caps, depth filters
and encounter-order ties. The independent fixture retains bounded per-peer
numerical rows and frozen velocities; 128 replay cycles and 768 changed-policy
frames each measure 0/0 owner/all-thread bytes. Arbitrary shape features, coupled
geometry and final network/native/performance/platform acceptance remain open.

[Compound slot numerics](../components/physics-registered-adapter.md#compound-slot-numerics) now verify independent multiple-circle/annulus placements and logical indices through the typed hook family, including Area/CCD/contact/private-history use. General geometry and coupled angular acceptance remain open.

With the captured separate-thread policy, Solve and its immediate HasFailure check execute on the retained numerical lane. Prepared readonly geometry, mass and exclusion inputs remain available in that exact borrowed scope; authoring, live queries and scene/lifetime operations reject. All other hooks, factories, synchronization and game callbacks remain on the owner. See [thread conformance](../components/physics-thread.md).


[Borrowed motion output](../components/physics-registered-adapter.md#borrowed-motion-output-2026-10-14) preserves selected scratch lifetime and owner scope while retaining caller-owned snapshots, error continuation and retirement failures. The inherited disposal preflight is a reviewed C# adaptation; full numerical/foreign/hardware acceptance stays open.


[Registered contact numerics](../components/physics-registered-adapter.md#registered-contact-numerics)
now exercises actual world/shape bias and penetration slack, complete contact/joint
sweeps, full physical impulses and identity-preserving live policy through the
public-only provider. Seven source families, dense contacts, CCD durations and
contact/pin convergence use nineteen complete 128-warmup/128-measured brackets
at zero managed allocation. Registered contact-cache, arbitrary coupled/custom,
full inherited/extension/network and native/platform/performance acceptance remain
open.
