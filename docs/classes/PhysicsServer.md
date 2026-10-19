# PhysicsServer

The [indexed slot operation audit](../components/physics-shapes.md#indexed-slot-operation-audit) completes registered replace, pose, disable, remove and clear accounting with public scene/raw failure and identity checks. Runtime behavior is unchanged; wider implementation acceptance remains open.

The [Body/Area append audit](../components/physics-shapes.md#bodyarea-append-contract-audit) closes both public append operations and their registered projections under the accepted rigid-pose contract. Pending/disabled slots, exact logical indices, canonical geometry, ownership, rollback/retry and zero warmed managed frames are covered; overall physics acceptance remains separate.

Last updated: 2026-10-19

## Failed initial attachment

Failed initial preparation retires the partial physical binding. Successful cleanup preserves the original exception; a cleanup failure is aggregated alongside it. The RID and authored shapes remain usable for a healthy later attachment, and unrelated live colliders remain available. This applies to server Body/Area and every scene PhysicsBody sibling plus Area. See [initial attachment checks](../components/physics-registered-adapter.md#initial-attachment-cleanup-errors) for executed scope and limits.

## Space creation and initialization

All SpaceCreate overloads publish inactive spaces with tick zero and retain captured selection/thread policy. Parameterless creation stays CPU; explicit enum selection ignores manager defaults. Named factories initialize against a registered context and can use readonly direct-space queries before completion; step, activation, policy writes, binding and release remain blocked. Failed initialization retires registrations/ownership and preserves cleanup errors. See [space creation verification](../components/physics-registered-adapter.md#space-creation-operation-audit) for real scene/raw, startup-fallback, lifecycle and allocation checks and their limits.

[Gravity publication and terminal parameter guards](../components/physics-registered-adapter.md#gravity-publication-and-terminal-parameter-guards) retry the latest effective native gravity/omission request after a selected error and discard it on attachment retirement. Scene damping, omission and inherited material setters reject failed worlds before changing authoring while healthy post-solver callbacks retain writes. Public CPU/software-GPU/registered JIT/NativeAOT profiles verify real motion, history boundaries and strict zero managed-byte frames; wider acceptance remains open.

[Mass profile publication recovery](../components/physics-registered-adapter.md#mass-profile-publication-recovery) preserves preceding authoring after selected mass/inertia/center/reset errors and prepares it before numerical use. Cached views, space/force/checkpoint/portable boundaries, attachment retirement, in-flight rejection and post-commit notification behavior execute across all scene/raw roles. Public JIT/strict NativeAOT CPU/software-GPU/registered full-frame checks retain strict zero managed allocation; wider native/platform/network/performance gates remain open.

[CCD policy operation audit](../components/physics-registered-adapter.md#ccd-policy-operation-audit) checks all three modes across five scene siblings and raw bodies, selected numerical input reads, wake/failure recovery, role/attachment lifetime and actual discrete/continuous motion with logical contact identity and impulse balance. Public JIT/strict NativeAOT CPU/software-GPU/registered full frames retain strict zero managed allocation; broader numerical/native/network/performance acceptance remains open.

[Body mode operation audit](../components/physics-registered-adapter.md#body-mode-operation-audit) now checks all four roles across five scene siblings and raw bodies, real changed-frame motion, captured numerical input reads, guarded writes and complete mode-publication recovery. Public JIT/strict NativeAOT CPU/software-GPU/registered companions retain strict zero managed-byte frames; broader numerical/native/network/performance acceptance remains open.

[Complete Area parameter operations](../components/physics-areas.md#complete-area-parameter-operations) now have a public-only component, lifetime, selected-preparation failure and warmed complete-frame audit across CPU/GPU/registered implementations. The engine-owned ten-field contract preserves independent defaults, prepared reads and retry without replaying Solve; broader numerical/native/network/performance acceptance remains open.

[Prepared Area pose inputs](../components/physics-areas.md#prepared-area-pose-inputs) let AreaGetTransform read scene/raw configured poses from selected numerical hooks, without live solver queries or general scene observation. Ordinary reads retain owner/failure guards and non-preparing presentation.

## Failed local activation

A failed selected activation leaves the prior public flag committed. Equal writes retry incomplete publication; live space queries, local/portable history and active positive intervals restore that committed policy before numerical use. Explicit writes may instead commit a different supplied choice. Metadata reads and skipped intervals remain non-preparing. Post-solver failure keeps its completed tick and callbacks; retirement discards pending activation. See [activation recovery](../components/physics-registered-adapter.md#failed-local-activation) for exercised scope and limits.

## Failed world policy publication

World sleep/contact profiles, solver iterations and default constraint bias retain accepted input when selected publication fails. An equal setter retries its complete current family; live queries, checkpoint/portable preparation and active positive steps finish incomplete families before numerical use. Completed families do not replay when a later family fails. Authored getters remain non-preparing; skipped steps and retirement do not publish pending settings. Local restore validates fixed configuration before this preparation. See [world policy recovery](../components/physics-registered-adapter.md#failed-world-policy-publication) for execution evidence and limits.

## Failed motion publication

Failed velocity/sleep/policy publication retains the complete requested write for retry before numerical access, body views, live queries or a nonzero step. Ordinary authored reads stay non-preparing. Linear/angular edits preserve the other selected motion channel, including impulse-derived values. Retirement overlays requested channels on retained selected motion without repairing the discarded attachment. See [motion publication](../components/physics-registered-adapter.md#failed-motion-publication) for executable scope and limits.

## Failed pose publication

Failed BodySetTransform and raw AreaSetTransform publication retains the committed pose. Live queries and nonzero steps retry current authored input; raw body scalar/batch reads and acquired or retained direct-body views also prepare incomplete publication. Scene presentation and Area pose getters remain non-preparing. Repeated errors retain their original exception and leave the tick unchanged. Detachment preserves authored pose instead of sampling a partially changed raw solver pose, discards retired attachment debt and invalidates old views. Later kinematic targets still wait for a nonzero active step. See [pose publication](../components/physics-registered-adapter.md#failed-pose-publication) for the executable scope and limits.

## Prepared body view acquisition

Body view acquisition now finishes pending direct-mode preparation before reading resolved fields, selecting a cached view or invoking its selected factory. The same boundary serves caller-created body-state extensions. A repeated preparation error creates no view, publishes no fields and does not advance the tick. Successful recovery preserves cached identity or creates one generation-qualified view; stale attachments remain rejected. See [the preparation contract](../components/physics-registered-adapter.md#body-view-acquisition-after-mode-failure).

## Failed body-mode publication

A failed direct BodySetMode publication retains the committed mode and motion policy. Numerical body access, including retained direct views and scene direct-mode velocity/sleep access, repairs the complete transition before reading or writing selected state. Live space queries and nonzero steps use the same preparation; repeated failure leaves the tick unchanged. Detachment preserves authored motion and discards retired attachment debt. Scene role takeover prepares the previous direct profile before changing its descriptors. See [mode preparation](../components/physics-registered-adapter.md#failed-body-mode-publication) for executable scope and limits.

**Inherits:** ElectronObject · **Source:** [PhysicsServer.cs](../../src/Servers/Physics/PhysicsServer.cs), [PhysicsServer.Resources.cs](../../src/Servers/Physics/PhysicsServer.Resources.cs), [PhysicsServer.Mass.cs](../../src/Servers/Physics/PhysicsServer.Mass.cs)

Public static declarations are in [`PhysicsServer.API.cs`](../../src/Servers/Physics/PhysicsServer.API.cs)
and [`PhysicsServer.Sleep.API.cs`](../../src/Servers/Physics/PhysicsServer.Sleep.API.cs).
Explicit CPU/GPU selection and startup diagnostics are in
[`PhysicsServer.Backends.cs`](../../src/Servers/Physics/PhysicsServer.Backends.cs).
The pose batch is in [`PhysicsServer.BodyTransforms.cs`](../../src/Servers/Physics/PhysicsServer.BodyTransforms.cs).

## Failed metadata publication

Object/canvas/priority authoring and reverse node membership commit before selected publication. Initial factory/attachment hooks already borrow the target world and node. Failed live publication queues complete metadata repair before live queries/nonzero steps; repeated preparation failure keeps the tick unchanged. Failed initial creation retires partial bindings. See [the contract and executable limits](../components/physics-registered-adapter.md#failed-collider-metadata-publication).

## Raw shape initialization

The eight built-in PhysicsServer shape factories return unconfigured RIDs. ShapeGetType works immediately. ShapeGetData and using that RID or a pending borrowed view as a collision-query source throw InvalidOperationException until ShapeSetData succeeds. Ordinary Shape constructors and CustomShapeCreate still supply initialized data.

Body/Area add operations accept pending shapes and keep their logical indices, transforms and disabled policy. Those slots supply no geometry, mass contribution, sensors or contacts until configuration. Disposed borrowed resources also produce inactive numerical descriptors while retaining logical slots and empty RID getters, including after disable/re-enable. Replacement operations require configured data. The first successful ShapeSetData activates enabled scene/raw users with stable RID/index/owner identity; a failed first configuration restores the pending state and can retry. Data copies, old-view retirement and shared-world borrowing keep their existing guarantees.

Private checkpoints preserve pending slots. Portable authoring compatibility distinguishes pending and configured data, so applying an old pending snapshot after configuration rejects before mutation. The encoded snapshot layout and hashes for previously configured shapes stay unchanged. This is headless authoring/simulation behavior; hardware GPU, rendered output, native allocator and full physics acceptance remain separate gates.


## Description

Public operations and events use static access to the retained object under [ADR 0095](../decisions/singleton-services.md#adr-0095). Object state, identity, property discovery and the owning domain lifetime rules remain intact.

The process-wide registry for typed 2D physics RIDs. It registers each SceneTree's selected CPU/GPU world and its scene CollisionObject identities, and can also create explicit spaces, bodies, Areas, joints and the eight implemented shape families. A server-created collider can join either kind of space; [World](World.md) and `SpaceGetDirectState` query that same solver state. RID values never expose Box2D IDs and never resolve to a later object after free. The server singleton cannot be disposed by consumers.

World sleep/contact parameters, solver iterations, default constraint bias and local activation reject writes while that world's numerical implementation hook is borrowed, before changing authoring or calling another hook. Equal writes follow the same guard. Authored getters and ordinary post-solver callback edits remain available; process-wide `SetActive` retains its separate atomic policy. [World policy checks](../components/physics-registered-adapter.md#borrowed-world-policies) exercise this boundary through a public-only consumer.

Body/Area world, layer/mask, object/canvas ID and body-priority getters expose common prepared authoring inside selected numerical/status hooks, including the exact borrowed numerical thread. They do not query solver state or change ownership. Ordinary owner/failure guards and mutation/query restrictions remain in force; see [the complete eleven-getter scope](../components/physics-registered-adapter.md#prepared-collider-metadata-reads).

Body/AreaGetSpace and Body/AreaSetSpace accept all live scene/raw collider RIDs. They report/change actual physics membership while preserving scene hierarchy and the World descriptor. Scene entry or viewport world replacement reclaims the scene-selected attachment. Source geometry and both world owners are validated before detachment; failed partial target additions are retired before restoring the old attachment, with cleanup/restoration errors aggregated. A resource world keeps its scene driver, while caller-owned spaces use SpaceStep. [Common space bindings](../components/physics-registered-adapter.md#common-space-bindings) records verification and failure limits.

## Example

```csharp
RID space = PhysicsServer.SpaceCreate();
PhysicsServer.SpaceSetActive(space, true);
RID body = PhysicsServer.BodyCreate();
RID shape = PhysicsServer.CircleShapeCreate();
using var circle = new CircleShape { Radius = 12 };
PhysicsServer.ShapeSetData(shape, circle);
PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static);
PhysicsServer.BodyAddShape(body, shape);
PhysicsServer.BodySetSpace(body, space);
using var ray = PhysicsRayQueryParameters.Create(new(0, -40), new(0, 40));
var hit = PhysicsServer.SpaceGetDirectState(space).IntersectRay(ray);
PhysicsServer.FreeRID(body);
PhysicsServer.FreeRID(shape);
PhysicsServer.FreeRID(space);
```

## API summary

`SpaceCreate(Backend backend, bool allowCPUFallback = false)` selects a CPU or GPU
space implementation independently of rendering. `SpaceGetRequestedBackend`,
`SpaceGetBackend` and `SpaceGetBackendFallbackReason` read startup metadata.
Parameterless creation remains CPU. See [backend lifetime and verified scope](../components/physics-backends.md).

| Member | Contract |
| --- | --- |
| `public enum BodyMode` | [Static, Kinematic, Rigid, RigidLinear](PhysicsServer.BodyMode.md). |
| `public static RID SpaceCreate()` | Caller-owned inactive independent physics space. |
| `public static void SpaceStep(RID space, double delta)` | Advance only an explicitly created space; zero delta is inert. |
| `public static PhysicsDirectSpaceState SpaceGetDirectState(RID space)` | Cached query view of any live server/scene space. |
| `public static bool BodyTestMotion(RID body, PhysicsTestMotionParameters parameters, PhysicsTestMotionResult? result = null)` | Test a scene or server body against its current space without moving it; optionally fill typed output. |
| `public static void BodyAddCollisionException(RID body, RID exceptedBody)` / `BodyRemoveCollisionException(RID body, RID exceptedBody)` | Change a one-sided body-owned RID exception affecting both solver contacts and motion tests. |
| `public static RID BodyCreate()` / `AreaCreate()` | Detached rigid body or sensor Area with default layer/mask one. |
| `public static RID CircleShapeCreate()` / `RectangleShapeCreate()` | Unconfigured typed shape RIDs; configure with ShapeSetData. |
| `public static RID CapsuleShapeCreate()` / `SegmentShapeCreate()` | Unconfigured typed shape RIDs; configure with ShapeSetData. |
| `public static RID SeparationRayShapeCreate()` | Unconfigured directed-ray RID. |
| `public static RID ConvexPolygonShapeCreate()` / `ConcavePolygonShapeCreate()` | Unconfigured polygon RIDs. |
| `public static void ShapeSetData(RID shape, Shape data)` | Copy same-kind caller geometry into a server shape and rebuild users. |
| `public static Shape ShapeGetData(RID shape)` | Caller-owned independent geometry copy; rejects an unconfigured RID. |
| `public static void BodyAddShape(RID body, RID shape, Transform? transform = null, bool disabled = false)` | Add one indexed body shape slot; null transform is identity. |
| `public static void AreaAddShape(RID area, RID shape, Transform? transform = null, bool disabled = false)` | Add one indexed sensor shape slot. |
| `public static int BodyGetShapeCount(RID body)` / `AreaGetShapeCount(RID area)` | Count slots, including disabled ones. |
| `public static void BodySetShapeDisabled(RID body, int index, bool disabled)` / `AreaSetShapeDisabled(RID area, int index, bool disabled)` | Rebuild one indexed slot's fixtures. |
| `public static void BodyRemoveShape(RID body, int index)` / `AreaRemoveShape(RID area, int index)` | Remove one slot and its fixtures. |
| `public static void BodySetSpace(RID body, RID space)` / `AreaSetSpace(RID area, RID space)` | Attach to a live space; empty RID detaches. |
| `public static RID BodyGetSpace(RID body)` / `AreaGetSpace(RID area)` | Current space RID, or empty while detached. |
| `public static void BodySetTransform(RID body, Transform transform)` / `AreaSetTransform(RID area, Transform transform)` | Set finite unit-scale, zero-skew pose. |
| `public static Transform BodyGetTransform(RID body)` | Current scene presentation or raw solver pose. |
| `public static void BodyGetTransform(RID space, ReadOnlySpan<RID> bodies, Span<Transform> transforms)` | Ordered caller-owned batch of the same poses; validates the complete sequence before writing, preserves duplicates and unused destination elements. |
| `public static void BodySetLinearVelocity(RID body, Vector2 velocity)` | Finite scene units per second. |
| `public static void BodySetContinuousCollisionDetectionMode(RID body, CCDMode mode)` / `CCDMode BodyGetContinuousCollisionDetectionMode(RID body)` | Store/read the same per-body policy as RigidBody.ContinuousCD, including detached/non-dynamic roles; validates live body identity, owner/phase and enum values. |
| `public static void BodySetMode(RID body, BodyMode mode)` / `BodyMode BodyGetMode(RID body)` | Change/read the solver motion mode. |
| `public static void BodySetCollisionLayer(RID body, uint layer)` / `BodySetCollisionMask(RID body, uint mask)` | Update existing scene/server/tile fixtures and wake contact neighbors. |
| `public static uint BodyGetCollisionLayer(RID body)` / `BodyGetCollisionMask(RID body)` | Read complete authored filter bits even while detached or geometry is disabled. |
| `public static void AreaSetCollisionLayer(RID area, uint layer)` | Update existing Area sensor metadata with 32-bit queryable layers. |
| `public static void FreeRID(RID rid)` | Free a caller-owned space, body, Area, joint or shape. |
| `protected override void ValidateDisposal()` | Reject consumer disposal of the singleton. |

## Method descriptions

<a id="spaces"></a>
### Space creation, access and stepping

`SpaceCreate` allocates one inactive selected physics world with the current sampled 2D project gravity default. `SpaceGetDirectState` returns a live view of any registered explicit or SceneTree space; disposing a view allows the next lookup to create another. `SpaceStep` accepts a finite nonnegative delta only for explicit spaces, so it cannot double-step a scene world. The SceneTree activates and advances its own registered space in its fixed physics lane. Caller-created worlds need SpaceSetActive(space, true) before SpaceStep can simulate; global/local suspension skips the solver interval without accumulating time. Space mutation and querying require the creating/owning thread and reject a world currently stepping. Explicit spaces with server-created bodies advance the real solver, not a parallel query-only representation. Scene and raw body/Area fields follow the shared selected implementation and common runtime policy.

<a id="colliders"></a>
### Collider creation, shapes and ownership

`BodyCreate` defaults to rigid mode; `AreaCreate` defaults to a stationary nonresponding sensor. Neither has a space until `BodySetSpace` or `AreaSetSpace`. The eight built-in shape factories allocate unconfigured circle, rectangle, capsule, segment, convex/concave polygon, directed separation ray and world boundary RIDs. Their type and logical slots exist immediately; enabled slots supply no numerical geometry until ShapeSetData succeeds. Ordinary resource constructors remain initialized. `ShapeSetData` rejects a disposed or different-kind Shape, copies represented geometry and rebuilds attached users; `ShapeGetData` returns an independent concrete resource. Procedural imports and borrowed-RID exports consume canonical geometry without requiring a subclass duplicate factory. Each `BodyAddShape` or `AreaAddShape` appends an indexed slot with a finite unit-scale, zero-skew local transform, default identity, and optional disabled state. Slot-count, disabled-toggle and removal methods make those indices executable after attachment; invalid indices throw before mutation. Compound fixtures from one slot share its public static query ShapeIndex. An explicit collider may be moved into the SceneTree's `World.Space`; direct queries then see it beside scene nodes. Queries return its RID with a null scene Collider.

<a id="body-state"></a>
### Body state and filters

`BodySetMode` accepts all four mode values for scene and raw body RIDs. Direct scene assignments retain a runtime profile independently of the concrete node type and stored Freeze/LockRotation flags. A later scene role edit restores configured control; MakeStatic temporarily overrides the effective mode. Static/Kinematic transitions clear linear/angular motion and RigidLinear clears angular motion. Identity and retained profile survive reentry; checkpoints and portable snapshots retain motion and kinematic targets. See [common mode verification](../components/physics-registered-adapter.md#common-server-mode-family).

Selected motion dispatch now borrows world lifetime for the whole query and validates its completed output before caller replacement or scene movement. Finite travel/contact values, ordered fractions, the requested moving RID, live enabled logical slots, same-space bodies, moving-mask/target-layer filtering, symmetric body/joint exceptions and explicit RID/object exclusions are checked. Collider object identity is sampled by the engine; supplied identity cannot retarget a result. Misses clear collision payload and must report completed motion. Same-space recursive motion rejects because the selected solver owns reusable query scratch; query failures retire borrowing without advancing or poisoning the world. Public registration remains open under ADR 0103.

`BodyTestMotion` resolves the current scene/raw body, prepares pending fixtures and delegates typed global pose/motion/exclusions to its selected world implementation. Native fixture lists remain inside the CPU owner. It tests the supplied body's own shapes from that pose. The moving body mask against target layers, RID and managed-instance exclusions, one-way surfaces, recovery margin and initial overlap are applied. It returns false on a miss and updates an optional [PhysicsTestMotionResult](PhysicsTestMotionResult.md) with full travel and cleared contact fields. On a hit it reports contact identity, point, normal, depth, velocity, local/collider shape-owner indices and safe/unsafe fractions. It never changes the actual body pose. A detached body or wrong RID rejects; off-owner and in-step calls reject. [PhysicsTestMotionParameters](PhysicsTestMotionParameters.md) names the input. `CollideSeparationRay` enables non-sliding ray sweeps; sliding rays and recovery obey [ADR 0068](../decisions/physics.md#adr-0068).

`BodyAddCollisionException` and `BodyRemoveCollisionException` edit only the owner's RID list. Either body's entry suppresses the pair in fixed-step solver contacts and `BodyTestMotion`, independent of reciprocal collision masks; Area monitoring is unaffected. Duplicates and absent removals preserve the list and still request dynamic-owner wakeup. The owner must be a live scene or server body; an arbitrary excepted RID, including an empty or later freed one, is retained but cannot match a live pair. An attached owner requires its space thread and cannot change exceptions while stepping. A changed list refreshes selected contact policy before the next body-motion query or step, including already touching pairs, while retaining prepared geometry. Freeing an owner removes its own entries; other bodies can retain its RID as an inert exception until explicitly removed.

<a id="bodygettransform-batch"></a>
### `BodyGetTransform(RID space, ReadOnlySpan<RID> bodies, Span<Transform> transforms)`

Copies one pose per input body, in input order, with the scalar getter's exact scene presentation, dynamic raw solver and static authored semantics. Repeated RIDs produce repeated values. Every body must be live and attached to the designated space. The destination must have at least the input length; its unused suffix stays unchanged. Empty input still validates the space and its owner/lifetime/solver/failure guards.

All identities, membership and access checks finish before output writes. Results are staged in space-owned scratch and copied only after successful reads, so invalid input or a device error leaves the destination untouched. Scratch grows on demand; fixed-capacity warmed batches allocate no managed memory and create no direct body views. GPU raw dynamic poses require one selected gather and one status fence: 16 bytes per requested pose plus eight status bytes, excluding pending edits/mass preparation. Scene and raw static bodies use their existing presentation/authored values without that read. Velocity, resolved fields and sleep state are not mirrored by this operation. Explicit pose caches expire on world-state edits or completed intervals and participate in local checkpoint restore.

Use this overload when a renderer needs many server-body poses in one interval. Reuse input/output arrays; call after stepping on the owning thread. [PhysicsBodyTransformTests](../../tests/Electron2D.Tests/PhysicsBodyTransformTests.cs) checks the CPU/GPU contract, compact byte counts, guards, duplicate requests larger than the resident population, authored edits, checkpoint restore, portable apply and zero warmed owner/all-thread allocation.

<a id="free"></a>
### `FreeRID`

Frees only caller-owned server resources. Joint free removes its native handle and pair contribution, then invalidates its RID; body free clears dependent joints. World free suspends caller-owned connections with local frames/settings retained. A shape free removes its slots from live users; a body/Area free detaches its backend object; a space free detaches its server colliders and invalidates retained query views. Collider RIDs remain live and detached after their space is freed, so they can be assigned to another space. SceneTree spaces, scene joint RIDs and scene CollisionObject RIDs must be released by their owning objects; attempting to free them here throws. Stale or wrong-kind RIDs reject without resolving to a later resource. The `RID` value itself remains nonzero after free.

Space release rejects while a physics step or any body/contact/Area callback is live, preserving its RID and implementation. Once cleanup starts, it attempts all participant and solver releases; AggregateException reports failures after the space RID/direct view are unregistered. An injected worker cleanup failure verifies native world destruction and registry removal.

All caller-owned kinds now preflight related access before mutation. Once cleanup
starts, every dependent/attachment/shape-user action is attempted and the freed RID
retires despite errors. A lone error retains its type/stack; multiple errors
aggregate. Motion capture or one joint failure cannot skip collider detach or
other joint cleanup. Shared shape retirement removes every common user slot and
retains dirty numerical preparation after an error. Reentry resets scene shape
revisions. A collider being freed rejects rebind/recursive free from departure
observers, while all queued observer deliveries retain their own errors. Retained
body views expire by attachment generation; an old direct-space RID query rejects
with ArgumentException after space free. See [resource retirement evidence](../components/physics-registered-adapter.md#resource-retirement).

## Verification and limits

[PhysicsQueryTests](../../tests/Electron2D.Tests/PhysicsQueryTests.cs) checks scene/server shared world, explicit stepping, six shape families, filters, modes, cross-space moves and RID lifecycle. [PhysicsShapeQueryTests](../../tests/Electron2D.Tests/PhysicsShapeQueryTests.cs) checks direct shape operations; [PhysicsMotionTests](../../tests/Electron2D.Tests/PhysicsMotionTests.cs) checks body motion; [PhysicsCollisionExceptionTests](../../tests/Electron2D.Tests/PhysicsCollisionExceptionTests.cs) checks unilateral scene/server lists, live solver and motion filtering, owner/target lifetime and warmed allocation. Full inherited and registered numerical conformance remains open; the [contract audit](../components/physics-contract-audit.md) tracks both backend obligations. Scene Area field/event delivery does not yet represent server-only colliders in typed object-level events. Queries use their selected CPU/GPU geometry pipelines; native allocator accounting, other platforms and large-world throughput remain unverified. See [ADR 0063](../decisions/physics.md#adr-0063).

`SeparationRayShapeCreate()` returns a caller-owned pending directed RID; ShapeSetData configures it. ShapeSetData copies length and slope policy; ShapeGetData returns an independent copy. Direct shape/body motion, sensing and ordinary directed solver impulses execute on the [resource class](SeparationRayShape.md), including raw body material, sleep and contact views.

## Body state and callbacks

| Signature | Contract |
| --- | --- |
| `public static PhysicsDirectBodyState? BodyGetDirectState(RID body)` | Cached attachment view, null while detached. Synchronizes only this body's pending pose/geometry; whole-space queries prepare their world separately. |
| `public static void BodySetForceIntegrationCallback(RID body, Action<PhysicsDirectBodyState>? callback)` | Set/clear a post-solver force callback. |
| `public static void BodySetForceIntegrationCallback<T>(RID body, Action<PhysicsDirectBodyState, T>? callback, T userData)` | Strongly typed data adapter allocated at registration. |
| `public static void BodySetStateSyncCallback(RID body, Action<PhysicsDirectBodyState>? callback)` | Set/clear the following sync observer. |
| `public static void BodySetOmitForceIntegration(RID body, bool enable)` | Control default gravity/damping/force omission. |
| `public static bool BodyIsOmittingForceIntegration(RID body)` | Read omission policy. |
| `public static void BodySetMaxContactsReported(RID body, int amount)` | Set a contact-point cap from zero through 4095 and clear the old point count. |
| `public static int BodyGetMaxContactsReported(RID body)` | Read the configured cap. |

Setters replace the previous user delegate; null clears it. Typed generic userdata replaces the dynamic callback boundary without invocation-time boxing. These APIs accept scene or server body identities and reject Area/wrong/stale RIDs. Attached access uses the owning space thread outside native stepping. Detached bodies retain callbacks, constants, omission and caps; they have no live view. Freeing a body clears that registry state.

Force callback runs before the RigidBody integration hook and user sync observer. Scene-owned synchronization remains mandatory under the typed host architecture; a user sync observer supplements it. Scene pose/cache synchronization brackets hooks. Callback failures are collected; body removal safely invalidates captured entries. Recursive stepping and world disposal reject during callback dispatch. [PhysicsBodyStateTests](../../tests/Electron2D.Tests/PhysicsBodyStateTests.cs) checks these contracts under [ADR 0070](../decisions/physics.md#adr-0070).

`public static void AreaSetMonitorable(RID area, bool monitorable)` controls whether scene monitoring Areas detect a server-created Area. Server Areas default false; the next nonzero overlap scan adopts the flag. Attached changes require the space owner thread outside native stepping. Shape events carry its RID with a null scene Area. Scene-object overlap arrays stay scene-only. Server free preserves exit values and finishes registry cleanup despite callback failure. ShapePairEventTests verifies body and Area nullable payloads under ADR 0055.

## Typed body mass parameters

| Signature | Contract |
| --- | --- |
| `void BodySetMass(RID body, float mass)` | Set positive finite kilograms; default one. |
| `float BodyGetMass(RID body)` | Configured kilograms, including static/kinematic/detached state. |
| `void BodySetInertia(RID body, float inertia)` | Nonnegative kg·scene-units²; zero selects automatic geometry. |
| `float BodyGetInertia(RID body)` | Explicit override or most recently resolved automatic moment, initially zero before first resolution by attachment or a geometry-dependent force call. |
| `void BodySetCenterOfMass(RID body, Vector2 center)` | Set local custom center relative to body origin. |
| `Vector2 BodyGetCenterOfMass(RID body)` | Configured custom center or most recently resolved automatic center, initially zero before first resolution by attachment or a geometry-dependent force call. |
| `void BodyResetMassProperties(RID body)` | Select automatic center and inertia while retaining mass. |

These methods accept every live scene/server body RID. A wrong-kind, empty or freed RID throws ArgumentException. Attached reads synchronize pending shape revisions, and all attached access checks owner thread and solver ownership. Invalid numeric profiles reject before configuration/native writes. Detached automatic values retain the last resolved cache until attachment; custom configuration survives removal. Explicit server body geometry now normalizes to stored kilograms rather than raw unit density. Static and kinematic profiles retain their resolved center with zero physical inverse mass/inertia.

A scene RigidBody shares its stored Mass/Inertia/CenterOfMassMode/CenterOfMass with the typed server profile. Setting its center selects Custom atomically; reset clears stored center/inertia and selects Auto. Property-list callback failures report after the complete commit. Shape geometry uses the same solid-area and segment rod policy as scene bodies. All remaining bounce/friction, gravity and damping parameter branches are now typed under ADR 0076; no Variant dispatcher or inert parameter enum is exposed. [PhysicsMassProfileTests](../../tests/Electron2D.Tests/PhysicsMassProfileTests.cs) and [ADR 0073](../decisions/physics-mass.md#adr-0073) define verification and the ownership adaptation.

## Body forces and impulses

**Source:** [PhysicsServer.Forces.cs](../../src/Servers/Physics/PhysicsServer.Forces.cs)

| Signature | Contract |
| --- | --- |
| `void BodyApplyCentralForce(RID body, Vector2 force)` | One eligible fixed-step force without torque. |
| `void BodyApplyForce(RID body, Vector2 force, Vector2 position = default)` | One-step force and captured moment. |
| `void BodyApplyTorque(RID body, float torque)` | One-step angular force. |
| `void BodyApplyCentralImpulse(RID body, Vector2 impulse)` | Instantaneous central impact. |
| `void BodyApplyImpulse(RID body, Vector2 impulse, Vector2 position = default)` | Instantaneous positioned impact. |
| `void BodyApplyTorqueImpulse(RID body, float impulse)` | Instantaneous angular impact. |
| `void BodyAddConstantCentralForce(RID body, Vector2 force)` | Add persistent force without changing torque. |
| `void BodyAddConstantForce(RID body, Vector2 force, Vector2 position = default)` | Add persistent force and moment. |
| `void BodyAddConstantTorque(RID body, float torque)` | Add persistent torque without changing force. |
| `void BodySetConstantForce(RID body, Vector2 force)` | Replace force; zero clears without waking. |
| `Vector2 BodyGetConstantForce(RID body)` | Stored global force. |
| `void BodySetConstantTorque(RID body, float torque)` | Replace torque; zero clears without waking. |
| `float BodyGetConstantTorque(RID body)` | Stored scalar torque. |

All calls accept live scene/server body RIDs, including detached bodies. Force units are kg·scene-units/s², impulse kg·scene-units/s, torque kg·scene-units²/s² and angular impulse kg·scene-units²/s. Position is a global-axis offset from body origin; its captured moment uses the current rotated center. Current normalized mass resolves from resource proxies while detached, without a temporary world/body. Impulses change retained velocity immediately, while static/kinematic inverse values and rotation lock suppress their respective responses.

Transient inputs persist through removal, static and dormant participation and apply once at the next eligible awake fixed step. Kinematic integration consumes them without dynamic response; omission clears eligible transient/native accumulators. A later explicit sleep assignment wins and retains input until wakeup. Persistent settings share scene RigidBody descriptors; constant getters/replacements require no ready geometry. Add operations wake even for zero input; zero replacement cannot wake by reassigning the other channel. Both totals and resulting velocity/moment validate before mutation. Wrong-kind/freed RID rejects; attached reads/writes enforce owner thread and solver ownership. Post-solver callbacks may queue the next step.

[PhysicsServerForceTests](../../tests/Electron2D.Tests/PhysicsServerForceTests.cs) verifies analytic units, every primitive projection, lifetime, mode/sleep/omission, failures and 64 warmed active attached/detached iterations with zero managed allocation. [ADR 0074](../decisions/physics-forces.md#adr-0074) records immediate detached profile resolution and shared lifetime adaptations. Native allocation, other platforms and owner acceptance remain unverified. Axis velocity and wider body-state methods retain their own coverage rows.

## Complete typed body material and field parameters

**Source:** [PhysicsServer.Parameters.cs](../../src/Servers/Physics/PhysicsServer.Parameters.cs)

| Getter / setter pair | Default | Contract |
| --- | --- | --- |
| `float BodyGetFriction(RID body)` / `void BodySetFriction(RID body, float friction)` | 1 | Finite signed coefficient; negative projects rough precedence. |
| `float BodyGetBounce(RID body)` / `void BodySetBounce(RID body, float bounce)` | 0 | Finite signed coefficient; negative projects absorbency. |
| `float BodyGetGravityScale(RID body)` / `void BodySetGravityScale(RID body, float scale)` | 1 | Signed multiplier of resolved Area/world gravity. |
| `float BodyGetLinearDamp(RID body)` / `void BodySetLinearDamp(RID body, float damp)` | 0 | Signed inverse-second damping. |
| `float BodyGetAngularDamp(RID body)` / `void BodySetAngularDamp(RID body, float damp)` | 0 | Signed inverse-second angular damping. |
| `RigidBody.DampMode BodyGetLinearDampMode(RID body)` / `void BodySetLinearDampMode(RID body, RigidBody.DampMode mode)` | Combine | Add selected damping or Replace it. |
| `RigidBody.DampMode BodyGetAngularDampMode(RID body)` / `void BodySetAngularDampMode(RID body, RigidBody.DampMode mode)` | Combine | Add selected angular damping or Replace it. |

Together with mass/center/inertia this covers all ten native body parameters. [ADR 0076](../decisions/physics-mass.md#adr-0076) replaces the numeric Variant discriminator/container/sentinel with these concrete typed capabilities; no unused selector is exposed. Server BodyDampMode uses the existing identical [RigidBody.DampMode](RigidBody.DampMode.md) values.

All live scene/server body RIDs accept detached configuration. Wrong kind/freed RID, off-owner/solver-owned access, nonfinite coefficients and unknown damping modes reject. RigidBody field policy is bidirectional with its scene properties; other roles retain configuration in their runtime record. Selected gravity scales before reporting/integration. Default integration applies max(0,1-delta*damp) to velocity before selected gravity and forces; negative damping is valid. Replace zero bypasses Area/world damping. Omission reports selected fields while skipping their automatic effect. Static/kinematic inverse response remains disabled.

Signed material values feed actual fixture coefficients and rough/absorbent combine callbacks. A per-body server material write leaves a shared borrowed PhysicsMaterial unchanged. A scene material assignment/revision/disposal reloads both overrides; polling recovers an earlier throwing resource subscriber at preparation. Reentry preserves raw server parameters. CharacterBody and its direct view expose the same scaled selected gravity without automatic modification of user Velocity. [PhysicsBodyParameterTests](../../tests/Electron2D.Tests/PhysicsBodyParameterTests.cs) verifies real material/field response, ownership/lifetime/failures and 64 warmed field/read/solver iterations with zero managed allocation. Native allocation, other platforms and owner acceptance remain unverified.

## Area receivers and filter/pose reads

**Source:** [PhysicsServer.Areas.cs](../../src/Servers/Physics/PhysicsServer.Areas.cs)

| Signature | Contract |
| --- | --- |
| `void AreaSetMonitorCallback(RID area, Action<AreaBodyStatus, RID, ulong, int, int>? callback)` | Body-pair observer; null clears. |
| `void AreaSetAreaMonitorCallback(RID area, Action<AreaBodyStatus, RID, ulong, int, int>? callback)` | Monitorable-Area pair observer; null clears. |
| `void AreaSetCollisionMask(RID area, uint mask)` | Directional receiver mask; default one, including bit 32. |
| `uint AreaGetCollisionLayer(RID area)` | Current category bits. |
| `uint AreaGetCollisionMask(RID area)` | Current accepted other-category bits. |
| `Transform AreaGetTransform(RID area)` | Current global scene/server pose. |

All live scene/server Area RIDs are accepted; pose/layer/monitorable setters use that same projection. [AreaBodyStatus](PhysicsServer.AreaBodyStatus.md) reports Added=0/Removed=1. Remaining payload is other RID, sampled assigned InstanceID or zero when unassigned, other global logical shape index and local index. Compound fixtures deduplicate per logical pair. The receiver mask tests other layer without reciprocal masks. Area callbacks include only monitorable peers; explicit server Areas default non-monitorable.

Successful registration resets both histories even for the same delegate; null clears without synthetic exits, and current overlaps enter again at the next nonzero scan. Scene-owned overlap arrays/events remain independent and mandatory; an external observer is not its replacement and can observe while scene Monitoring is false. Snapshot state commits before dispatch; failures continue later events and do not replay. Own receiver configuration mutation from a raw or scene overlap callback rejects, while reads are allowed. Epoch changes suppress stale queued callbacks. Other collider removal/free emits retained exits immediately; removal from an entry callback suppresses stale later entries and safely delivers departures.

Receiver detach clears history silently and retains configuration for reentry. Receiver free/world disposal clears history/event storage; cleanup of a freed other collider completes even if its departure callbacks throw. Wrong-kind/freed RID and off-owner/solver-owned access reject. [PhysicsAreaMonitorTests](../../tests/Electron2D.Tests/PhysicsAreaMonitorTests.cs) verifies payload/filter/lifetime/errors and 64 warmed active entry/exit cycles with zero managed allocation. [ADR 0077](../decisions/physics-monitoring.md#adr-0077) records the ownership adaptation. Typed fields and completed process statistics have their own verified profiles; full registered numerical/extension, native allocation, large-world performance, other platforms and owner acceptance remain open.

## Typed Area fields and space defaults

Twenty concrete methods share one field profile with scene Area properties. Each accepts a live scene/server Area RID; a live space RID instead addresses its unbounded default Area. Every attached access requires the owner thread outside solver ownership. Detached server configuration is retained through assignment/reentry. Receiver monitoring/monitorability do not gate fields, and changing fields does not reset raw observer history. Field writes are allowed inside post-step callbacks for the next nonzero reduction. Invalid RID/kind throws ArgumentException; finite/defined input failures throw ArgumentOutOfRangeException before assignment. Nonfinite computed fields or motion produce the existing aggregate physics-frame/SpaceStep failure before that body's velocity/cache changes; correct the configuration before continuing.

| Full getter signature | Full setter signature | Value/default |
| --- | --- | --- |
| `public static Area.SpaceOverride AreaGetGravitySpaceOverride(RID area)` | `public static void AreaSetGravitySpaceOverride(RID area, Area.SpaceOverride value)` | Disabled; one of five independent reduction modes. |
| `public static float AreaGetGravity(RID area)` | `public static void AreaSetGravity(RID area, float value)` | Signed scene units/s²: 9.80665 for server-only, 980 for scene. |
| `public static Vector2 AreaGetGravityVector(RID area)` | `public static void AreaSetGravityVector(RID area, Vector2 value)` | Unnormalized direction/local point: (0, -1) server, (0, 1) scene. |
| `public static bool AreaGetGravityPoint(RID area)` | `public static void AreaSetGravityPoint(RID area, bool value)` | False; point center shares scene GravityDirection/GravityPointCenter storage. |
| `public static float AreaGetGravityPointUnitDistance(RID area)` | `public static void AreaSetGravityPointUnitDistance(RID area, float value)` | Zero; positive selects inverse-square falloff, nonpositive constant strength. |
| `public static Area.SpaceOverride AreaGetLinearDampSpaceOverride(RID area)` | `public static void AreaSetLinearDampSpaceOverride(RID area, Area.SpaceOverride value)` | Disabled. |
| `public static float AreaGetLinearDamp(RID area)` | `public static void AreaSetLinearDamp(RID area, float value)` | Signed inverse seconds, 0.1. |
| `public static Area.SpaceOverride AreaGetAngularDampSpaceOverride(RID area)` | `public static void AreaSetAngularDampSpaceOverride(RID area, Area.SpaceOverride value)` | Disabled. |
| `public static float AreaGetAngularDamp(RID area)` | `public static void AreaSetAngularDamp(RID area, float value)` | Signed inverse seconds, 1. |
| `public static int AreaGetPriority(RID area)` | `public static void AreaSetPriority(RID area, int value)` | Zero; greater values run first. |

### Field method descriptions

<a id="areagetgravityspaceoverride"></a>
<a id="areasetgravityspaceoverride"></a>
**AreaGetGravitySpaceOverride / AreaSetGravitySpaceOverride:** Disabled; one of five independent reduction modes. Getter returns the stored value; setter updates that same profile for the next reduction.

<a id="areagetgravity"></a>
<a id="areasetgravity"></a>
**AreaGetGravity / AreaSetGravity:** Signed scene units/s²: 9.80665 for server-only, 980 for scene. Getter returns the stored value; setter updates that same profile for the next reduction.

<a id="areagetgravityvector"></a>
<a id="areasetgravityvector"></a>
**AreaGetGravityVector / AreaSetGravityVector:** Unnormalized direction/local point: (0, -1) server, (0, 1) scene. Getter returns the stored value; setter updates that same profile for the next reduction.

<a id="areagetgravitypoint"></a>
<a id="areasetgravitypoint"></a>
**AreaGetGravityPoint / AreaSetGravityPoint:** False; point center shares scene GravityDirection/GravityPointCenter storage. Getter returns the stored value; setter updates that same profile for the next reduction.

<a id="areagetgravitypointunitdistance"></a>
<a id="areasetgravitypointunitdistance"></a>
**AreaGetGravityPointUnitDistance / AreaSetGravityPointUnitDistance:** Zero; positive selects inverse-square falloff, nonpositive constant strength. Getter returns the stored value; setter updates that same profile for the next reduction.

<a id="areagetlineardampspaceoverride"></a>
<a id="areasetlineardampspaceoverride"></a>
**AreaGetLinearDampSpaceOverride / AreaSetLinearDampSpaceOverride:** Disabled. Getter returns the stored value; setter updates that same profile for the next reduction.

<a id="areagetlineardamp"></a>
<a id="areasetlineardamp"></a>
**AreaGetLinearDamp / AreaSetLinearDamp:** Signed inverse seconds, 0.1. Getter returns the stored value; setter updates that same profile for the next reduction.

<a id="areagetangulardampspaceoverride"></a>
<a id="areasetangulardampspaceoverride"></a>
**AreaGetAngularDampSpaceOverride / AreaSetAngularDampSpaceOverride:** Disabled. Getter returns the stored value; setter updates that same profile for the next reduction.

<a id="areagetangulardamp"></a>
<a id="areasetangulardamp"></a>
**AreaGetAngularDamp / AreaSetAngularDamp:** Signed inverse seconds, 1. Getter returns the stored value; setter updates that same profile for the next reduction.

<a id="areagetpriority"></a>
<a id="areasetpriority"></a>
**AreaGetPriority / AreaSetPriority:** Zero; greater values run first. Getter returns the stored value; setter updates that same profile for the next reduction.

Gravity, linear damping and angular damping stop independently under [Area.SpaceOverride](Area.SpaceOverride.md). Current exact shape overlap and receiver mask against body layer select each Area once; reciprocal body masks and duplicate fixture pairs do not multiply contributions. Scene/server Areas are sorted together in stable descending priority; equal priorities retain current membership order (scene entries then server entries). Disabled geometry, removal and free stop contribution; reentry preserves configuration. Directional gravity ignores receiver rotation; point gravity transforms its local vector through the global pose, normalizes the direction toward it, applies constant/inverse-square strength, and is zero at the exact center.

A space's default profile starts from sampled ProjectSettings strength/vector/damping, priority -1 and disabled modes. Its priority and modes remain stored but do not gate the final default fallback. The fallback contributes only channels not stopped by an Area; it has no bounded geometry and point gravity uses an identity transform. Changes apply to the existing space on its next nonzero step, wake affected sleeping dynamics, and do not edit ProjectSettings or other worlds. Body damping Combine/Replace and gravity scaling follow the existing parameter contract; CharacterBody reads selected gravity without automatic acceleration. Zero delta retains the last body totals.

Partial usage snippet (live `areaRID` in a stepped world):

```csharp
PhysicsServer.AreaSetGravitySpaceOverride(areaRID, Area.SpaceOverride.Replace);
PhysicsServer.AreaSetGravity(areaRID, 0);
PhysicsServer.AreaSetLinearDampSpaceOverride(areaRID, Area.SpaceOverride.Combine);
PhysicsServer.AreaSetLinearDamp(areaRID, 2);
```

[PhysicsServerAreaFieldTests](../../tests/Electron2D.Tests/PhysicsServerAreaFieldTests.cs) checks defaults, all ten branches, scene projection, mixed mode/priority/filter/lifecycle, actual server response, mutable space defaults/point fallback, failure recovery, callbacks/guards and zero managed bytes over 64 warmed active field frames on Linux/.NET 10. Native allocations, other platforms and owner visual acceptance are unverified. [ADR 0056](../decisions/physics-fields.md#adr-0056) owns the shared profile and typed selector adaptation.

<a id="joints"></a>
## Joint resources and typed settings

Source: [PhysicsServer.Joints.cs](../../src/Servers/Physics/PhysicsServer.Joints.cs). The [JointType enum](PhysicsServer.JointType.md) reports Pin, Groove, DampedSpring or Empty. Joint identities and scalar settings share the scene kernels under [ADR 0087](../decisions/physics-joints.md#adr-0087).

### Joint method summary

| Signature | Contract |
| --- | --- |
| `public static RID JointCreate()` | Caller-owned Empty identity, collision suppression true. |
| `public static void JointClear(RID joint)` | Remove connection, preserve RID and collision policy. |
| `public static JointType JointGetType(RID joint)` | Configured role, including pending connections and Empty. |
| `public static void JointDisableCollisionsBetweenBodies(RID joint, bool disable)` | Change pair suppression while preserving anchors. |
| `public static bool JointIsDisabledCollisionsBetweenBodies(RID joint)` | Stored policy, even for Empty. |
| `public static void JointMakePin(RID joint, Vector2 anchor, RID bodyA, RID bodyB = default)` | Global pivot; empty body B binds A to the fixed world. |
| `public static void JointMakeGroove(RID joint, Vector2 groove1A, Vector2 groove2A, Vector2 anchorB, RID bodyA = default, RID bodyB = default)` | Finite global guide on A; both body RIDs required. |
| `public static void JointMakeDampedSpring(RID joint, Vector2 anchorA, Vector2 anchorB, RID bodyA, RID bodyB = default)` | Two required bodies, global anchors, force and axial drag. |
| `public static float DampedSpringJointGetDamping(RID joint)` | [Shared concrete parameter](#dampedspringjointgetdamping). |
| `public static float DampedSpringJointGetRestLength(RID joint)` | [Shared concrete parameter](#dampedspringjointgetrestlength). |
| `public static float DampedSpringJointGetStiffness(RID joint)` | [Shared concrete parameter](#dampedspringjointgetstiffness). |
| `public static void DampedSpringJointSetDamping(RID joint, float value)` | [Shared concrete parameter](#dampedspringjointsetdamping). |
| `public static void DampedSpringJointSetRestLength(RID joint, float value)` | [Shared concrete parameter](#dampedspringjointsetrestlength). |
| `public static void DampedSpringJointSetStiffness(RID joint, float value)` | [Shared concrete parameter](#dampedspringjointsetstiffness). |
| `public static bool PinJointGetAngularLimitEnabled(RID joint)` | [Shared concrete parameter](#pinjointgetangularlimitenabled). |
| `public static float PinJointGetAngularLimitLower(RID joint)` | [Shared concrete parameter](#pinjointgetangularlimitlower). |
| `public static float PinJointGetAngularLimitUpper(RID joint)` | [Shared concrete parameter](#pinjointgetangularlimitupper). |
| `public static bool PinJointGetMotorEnabled(RID joint)` | [Shared concrete parameter](#pinjointgetmotorenabled). |
| `public static float PinJointGetMotorMaxTorque(RID joint)` | [Shared concrete parameter](#pinjointgetmotormaxtorque). |
| `public static float PinJointGetMotorTargetVelocity(RID joint)` | [Shared concrete parameter](#pinjointgetmotortargetvelocity). |
| `public static void PinJointSetAngularLimitEnabled(RID joint, bool value)` | [Shared concrete parameter](#pinjointsetangularlimitenabled). |
| `public static void PinJointSetAngularLimitLower(RID joint, float value)` | [Shared concrete parameter](#pinjointsetangularlimitlower). |
| `public static void PinJointSetAngularLimitUpper(RID joint, float value)` | [Shared concrete parameter](#pinjointsetangularlimitupper). |
| `public static void PinJointSetMotorEnabled(RID joint, bool value)` | [Shared concrete parameter](#pinjointsetmotorenabled). |
| `public static void PinJointSetMotorMaxTorque(RID joint, float value)` | [Shared concrete parameter](#pinjointsetmotormaxtorque). |
| `public static void PinJointSetMotorTargetVelocity(RID joint, float value)` | [Shared concrete parameter](#pinjointsetmotortargetvelocity). |

### Example

```csharp
var space = PhysicsServer.SpaceCreate();
PhysicsServer.SpaceSetActive(space, true);
var first = PhysicsServer.BodyCreate();
var second = PhysicsServer.BodyCreate();
var link = PhysicsServer.JointCreate();
try
{
    PhysicsServer.BodySetMode(first, PhysicsServer.BodyMode.Static);
    PhysicsServer.BodySetTransform(second, new Transform(0, Vector2.One, 0, new Vector2(0, 50)));
    PhysicsServer.BodySetGravityScale(second, 0);
    PhysicsServer.BodySetSpace(first, space);
    PhysicsServer.BodySetSpace(second, space);
    PhysicsServer.JointMakeDampedSpring(link, Vector2.Zero, new Vector2(0, 50), first, second);
    PhysicsServer.DampedSpringJointSetRestLength(link, 25);
    PhysicsServer.SpaceStep(space, 1d / 60);
}
finally
{
    PhysicsServer.FreeRID(link);
    PhysicsServer.FreeRID(first);
    PhysicsServer.FreeRID(second);
    PhysicsServer.FreeRID(space);
}
```

### Joint method descriptions

<a id="jointcreate"></a>
**JointCreate:** Allocates a nonempty RID for an Empty resource. It has no native constraint and may be configured before body attachment. The caller releases it through FreeRID.

<a id="jointclear"></a>
**JointClear:** Removes the native handle and active collision contribution and forgets endpoints, retaining identity, scalar records and collision policy. Concrete operations reject a caller-owned Empty role. A scene-owned Empty role retains access to its concrete node's settings. A raw scene clear persists until a path/geometry/name edit or reentry; it does not alter stored NodeA/NodeB text.

<a id="jointgettype"></a>
**JointGetType:** Returns the configured role, which remains concrete while a connection waits for body attachment or temporarily spans different worlds. It returns Empty after allocation, clear or final endpoint free. A disposed RID rejects.

Creation/type/clear/make share common identity with the selected numerical attachment.
Clear and replacement reject factory/initial/live/release hooks before changing
configuration. Both incoming body owners are checked before preparing either,
including an Empty joint with a detached first endpoint. Invalid kind, role or
geometry candidates preserve the old connection. A selected attachment failure
retains its validated role for explicit make retry; Attach cleanup attempts Detach
and preserves both errors if needed. A detach error retires the numerical object
and repeated clear completes common cleanup. Ordinary game callbacks may clear
and remake the next interval's connection. See [connection lifetime evidence](../components/physics-registered-adapter.md#joint-connection-lifetime).

<a id="jointdisablecollisionsbetweenbodies"></a>
<a id="jointisdisabledcollisionsbetweenbodies"></a>
**Collision policy pair:** True by default. Set/get the shared node/server policy. Active changes preserve local anchors and refresh contact fixtures. Each disabled connection independently contributes to both bodies' contact and motion-test exceptions; snapshots deduplicate explicit and joint targets. Clearing/freeing/toggling one joint cannot remove another joint or explicit exception's contribution. An Empty resource stores policy for its next make.

<a id="jointmakepin"></a>
**JointMakePin:** Samples the finite global scene-unit pivot into body-local frames. Distinct live bodies may be scene nodes or caller-owned colliders. An empty bodyB selects a hidden shape-free static world anchor; the first body remains a live required RID. Replaces the old connection while preserving identity/collision policy and resets pin limits/motor to disabled/zero, torque cap to 10 N·m. Relative angle is zero at sampling.

<a id="jointmakegroove"></a>
**JointMakeGroove:** Samples global endpoints into A's local finite guide and the global anchor into B's local frame. Equal endpoints form a point limit; endpoint order supplies axis direction. Rotation remains free. Despite the empty defaults in the signature, both RIDs must be distinct live bodies. Segment and anchor distances use the ten-million-scene-unit extent guard.

<a id="jointmakedampedspring"></a>
**JointMakeDampedSpring:** Samples two finite global anchors into distinct required bodies. Resets relaxed separation to their distance, stiffness to 20 kg/s² and damping to 1.5 kg/s. It executes the shared Hooke/effective-mass axial-drag rule, including pure damping. Anchor separation is bounded to ten million scene units. Length is not a stretch cap.

<a id="pinjointgetangularlimitenabled"></a>
<a id="pinjointsetangularlimitenabled"></a>
**PinJointGetAngularLimitEnabled / PinJointSetAngularLimitEnabled:** Enable finite ordered relative-angle limits. Enabling validates both stored endpoints. Default: false. The getter returns the shared value; the setter updates that same record and active solver response without resampling anchors. Wrong concrete roles reject.

<a id="pinjointgetangularlimitlower"></a>
<a id="pinjointsetangularlimitlower"></a>
**PinJointGetAngularLimitLower / PinJointSetAngularLimitLower:** Lower relative angle in radians. With limits enabled, bounds must be ordered inside ±0.99π. Default: 0. The getter returns the shared value; the setter updates that same record and active solver response without resampling anchors. Wrong concrete roles reject.

<a id="pinjointgetangularlimitupper"></a>
<a id="pinjointsetangularlimitupper"></a>
**PinJointGetAngularLimitUpper / PinJointSetAngularLimitUpper:** Upper relative angle in radians. With limits enabled, bounds must be ordered inside ±0.99π. Default: 0. The getter returns the shared value; the setter updates that same record and active solver response without resampling anchors. Wrong concrete roles reject.

<a id="pinjointgetmotorenabled"></a>
<a id="pinjointsetmotorenabled"></a>
**PinJointGetMotorEnabled / PinJointSetMotorEnabled:** Enable the angular motor with the configured target velocity and torque cap. Default: false. The getter returns the shared value; the setter updates that same record and active solver response without resampling anchors. Wrong concrete roles reject.

<a id="pinjointgetmotortargetvelocity"></a>
<a id="pinjointsetmotortargetvelocity"></a>
**PinJointGetMotorTargetVelocity / PinJointSetMotorTargetVelocity:** Finite relative radians per second, signed for direction. Default: 0. The getter returns the shared value; the setter updates that same record and active solver response without resampling anchors. Wrong concrete roles reject.

<a id="pinjointgetmotormaxtorque"></a>
<a id="pinjointsetmotormaxtorque"></a>
**PinJointGetMotorMaxTorque / PinJointSetMotorMaxTorque:** Finite nonnegative torque cap in newton-meters; required by the native motor and shared with PinJoint.MotorMaxTorque. Default: 10. The getter returns the shared value; the setter updates that same record and active solver response without resampling anchors. Wrong concrete roles reject.

<a id="dampedspringjointgetrestlength"></a>
<a id="dampedspringjointsetrestlength"></a>
**DampedSpringJointGetRestLength / DampedSpringJointSetRestLength:** Finite nonnegative relaxed separation in scene units, at most ten million. A raw server zero is literal. For a scene automatic zero, the getter reports abs(Length); a server write selects literal policy until the scene RestLength setter restores automatic policy. Default: initial anchor distance. The getter returns the shared value; the setter updates that same record and active solver response without resampling anchors. Wrong concrete roles reject.

<a id="dampedspringjointgetstiffness"></a>
<a id="dampedspringjointsetstiffness"></a>
**DampedSpringJointGetStiffness / DampedSpringJointSetStiffness:** Finite nonnegative Hooke coefficient in kg/s²; zero supports a pure damper. Default: 20. The getter returns the shared value; the setter updates that same record and active solver response without resampling anchors. Wrong concrete roles reject.

<a id="dampedspringjointgetdamping"></a>
<a id="dampedspringjointsetdamping"></a>
**DampedSpringJointGetDamping / DampedSpringJointSetDamping:** Finite nonnegative axial drag in kg/s; zero leaves elastic response undamped. Default: 1.5 raw / 1 scene. The getter returns the shared value; the setter updates that same record and active solver response without resampling anchors. Wrong concrete roles reject.

### Joint lifetime, errors and verification

Make validates geometry and every local anchor radius through ten million scene units, distinct live body identities and current world compatibility before removing the previous connection. Related active worlds require their owning thread and reject solver/synchronization phases. These checks also guard pending detached endpoints, membership changes and disposal; rejected operations preserve object lifetime. Wrong/stale/body/Area RIDs or wrong concrete setting roles throw ArgumentException; nonfinite or invalid scalar ranges throw ArgumentOutOfRangeException. A scene joint cannot be changed to another concrete node role or active foreign world (InvalidOperationException).

Detached caller-owned connections retain sampled local frames and connect once both bodies share an active world; temporary departure or cross-world membership suspends them. Reentry uses fresh native IDs. Endpoint free clears dependents; world free leaves live resources detached and configured. Scene GetRID is borrowed and stable until node disposal; raw same-role server make/clear persists until scene geometry/path/name edits or reentry reclaim it. Scalar edits are bidirectional without replacing the public static node. No joint owns or frees endpoint bodies.

[PhysicsServerJointTests](../../tests/Electron2D.Tests/PhysicsServerJointTests.cs) covers all three actual server responses, world pin, bidirectional settings, clear/replacement, defaults, lifecycle/failure/phase/thread rollback, independent exceptions and 64 warmed active typed-setting/spring frames with zero managed allocation on Linux/.NET 10. Native allocations, broad-scene stability/performance, other platforms and owner visual acceptance remain unverified. The [joint policy family](../components/physics-joint-policies.md) now executes positional bias/correction speed/force caps and linear pin softness. Scene physics debug drawing and independent GPU-world selection remain open.

<a id="shape-slots"></a>
## Indexed body and Area geometry

Source: [PhysicsServer.ShapeSlots.cs](../../src/Servers/Physics/PhysicsServer.ShapeSlots.cs). All indexed methods accept scene or caller-owned collider RIDs and use zero-based global logical indices. One compound shape can create several fixtures, each reporting its same slot index. Disabled/disposed slots still count; a retained disposed resource produces an empty shape RID. [ADR 0088](../decisions/physics-shape-slots.md#adr-0088) owns the shared projection.

| Signature | Contract |
| --- | --- |
| `public static RID BodyGetShape(RID body, int index)` | Borrowed shape identity, or empty for a disposed retained resource. |
| `public static RID AreaGetShape(RID area, int index)` | Same contract for an Area. |
| `public static Transform BodyGetShapeTransform(RID body, int index)` | Effective local slot pose. |
| `public static Transform AreaGetShapeTransform(RID area, int index)` | Effective local sensor pose. |
| `public static void BodySetShape(RID body, int index, RID shape)` | Replace geometry, retain index/pose/policies, borrow the shape. |
| `public static void AreaSetShape(RID area, int index, RID shape)` | Same for a sensor slot. |
| `public static void BodySetShapeTransform(RID body, int index, Transform transform)` | Finite unit-scale, zero-skew local pose for one slot. |
| `public static void AreaSetShapeTransform(RID area, int index, Transform transform)` | Same for a sensor slot. |
| `public static void BodyClearShapes(RID body)` | Remove all indexed shapes without freeing resources. |
| `public static void AreaClearShapes(RID area)` | Remove all sensor slots without freeing resources. |
| `public static void BodySetShapeAsOneWayCollision(RID body, int index, bool enable, float margin, Vector2? direction = null)` | One slot's directional body contacts/motion; null means down. |

### Example

Partial snippet: `bodyRID` is a live scene or server body. The caller owns `geometry` until FreeRID; its free removes any remaining slots that use it.

```csharp
var geometry = PhysicsServer.RectangleShapeCreate();
PhysicsServer.BodyAddShape(bodyRID, geometry);
int index = PhysicsServer.BodyGetShapeCount(bodyRID) - 1;
PhysicsServer.BodySetShapeTransform(bodyRID, index, new Transform(0, Vector2.One, 0, new Vector2(40, 0)));
RID sameGeometry = PhysicsServer.BodyGetShape(bodyRID, index);
PhysicsServer.BodyRemoveShape(bodyRID, index);
PhysicsServer.FreeRID(geometry);
```

<a id="bodygetshape"></a>
<a id="areagetshape"></a>
**BodyGetShape / AreaGetShape:** Return the effective slot's shape identity. Caller-created shape IDs remain caller-owned; managed Shape IDs remain resource-owned. Getters confer no release ownership. An absent index throws ArgumentOutOfRangeException; wrong/stale collider identities throw ArgumentException. Body/Area count, RID and local-pose getters read common prepared authoring without querying numerical state. A selected implementation may use them during its numerical hook on the owner or its exact borrowed solver thread. Ordinary access retains owner-thread and failed-world guards; unrelated threads, geometry writes and live solver queries retain their existing restrictions.

<a id="bodysetshape"></a>
<a id="areasetshape"></a>
**BodySetShape / AreaSetShape:** Replace the indexed geometry with a live owned or borrowed shape RID, preserving index, pose, disabled and one-way settings. The previous resource remains live. Scene owner lookup reports current geometry, while child Shape properties are unchanged. A later child resource edit rebuilds its group. Raw-server fixtures rebuild immediately; scene fixtures prepare before the next query/step. Invalid IDs/indices reject before slot mutation.

<a id="bodygetshapetransform"></a>
<a id="areagetshapetransform"></a>
<a id="bodysetshapetransform"></a>
<a id="areasetshapetransform"></a>
**Slot pose pairs:** Read/write translation and rotation relative to the collider. Numeric input must be finite, with unit scale and zero skew. Moving one slot never moves its body, siblings or stored child node. A later group/child transform assignment reclaims its corresponding raw pose overrides. The effective pose participates in query/contact geometry and automatic mass-center/inertia calculation.

<a id="bodyclearshapes"></a>
<a id="areaclearshapes"></a>
**Clear pairs:** Idempotently remove all slots and native fixtures. Shapes remain live. Scene groups and child configuration remain, with empty group shape lists; a subsequent child resource edit can repopulate its group. Per-index removal shifts all later indices and query tags. Retained contact results still describe their sampled indices.

<a id="bodysetshapeasonewaycollision"></a>
**BodySetShapeAsOneWayCollision:** Changes one slot's full directional policy. Margin is finite/nonnegative scene units. Direction is finite and normalized, preserving zero; null supplies local down. Slot/body rotation carries it into world coordinates. The existing pre-solve callback gates ordinary contacts and the shared motion kernel gates sweeps/recovery. An Area RID rejects. A later scene group one-way setting restores the full group policy.

### Shared geometry lifetime and verification

Existing add/count/disable/remove methods now address scene slots too. Raw scene addition creates a transient null-owner group with no hidden child. Structural edits do not free geometry or persist through PackedScene. Owned shape data replacement preserves the RID, refreshes RID-backed users and retires an old borrowed geometry view. Such a view cannot be disposed separately; obtain independent data through ShapeGetData. A retirement observer exception propagates after the new data commit. Shape free removes users and unregisters the RID even if its geometry's disposal observer throws. Every related active world's owner/phase is preflighted before shared data mutation/free.

[PhysicsServerShapeSlotTests](../../tests/Electron2D.Tests/PhysicsServerShapeSlotTests.cs) covers real queries, body/Area scene/server roles, slot/group/child independence, replacement/clear/reindex, borrowed/owned identity, callback/phase/thread/numeric failures, mass-center geometry and real one-way contacts/motion. Sixty-four warmed indexed reads, unchanged writes and active solver frames allocate zero managed bytes on Linux/.NET 10. Native allocation, structural-edit allocation budgets, other platforms and owner visual acceptance remain unverified.

<a id="activity"></a>
## World activation and suspension

Source: [PhysicsServer.Activity.cs](../../src/Servers/Physics/PhysicsServer.Activity.cs).

| Signature | Contract |
| --- | --- |
| `public static void SetActive(bool active)` | Atomic process-wide policy for subsequent world intervals; default true. |
| `public static void SpaceSetActive(RID space, bool active)` | Local activation for a live scene or caller-owned world. |
| `public static bool SpaceIsActive(RID space)` | Stored local policy, independent of global suspension. |
| `public static bool SpaceIsFlushingQueries(RID space)` | Owner-qualified completed-notification status, including body/state and contact/Area/sleep delivery. |

```csharp
RID space = PhysicsServer.SpaceCreate(); // Inactive.
PhysicsServer.SpaceSetActive(space, true);
PhysicsServer.SpaceStep(space, 1d / 60);
PhysicsServer.SpaceSetActive(space, false); // Retain this world's simulation state.
PhysicsServer.SpaceStep(space, 10);        // Skipped; no time backlog.
PhysicsServer.FreeRID(space);
```

<a id="setactive"></a>
**SetActive:** The server starts enabled. Any thread may atomically disable/enable later world steps. A running interval completes; the flag is sampled at each world's next boundary. Queries, explicit configuration and scene scheduling remain available. This does not change any world's local flag.

<a id="spacesetactive"></a>
**SpaceSetActive:** Set the local world gate. New caller-owned spaces default false; SceneTree activates its world. Body motion, forces, joints, integration callbacks and monitoring advance only when both local/global policies permit. Skipped intervals retain native state, direct views, previous Step and pending one-step forces; resume integrates only its current delta. World cleanup and queries/configuration still work while inactive. The owner thread is required; a solver-phase write rejects before mutation. Post-solver callbacks can change later policy.

<a id="spaceisactive"></a>
**SpaceIsActive:** Return the local flag, even while the global server is suspended. It does not report effective scene ProcessMode or SceneTree.Paused. Reads require owner-thread access outside solver stepping. Wrong/stale RID throws ArgumentException; off-owner/in-solver access throws InvalidOperationException.

[PhysicsActivityTests](../../tests/Electron2D.Tests/PhysicsActivityTests.cs) checks native motion/spring and pending-force behavior, inactive queries, defaults, callback/timer continuation, phase/thread/lifetime guards, callback failure and 64 warmed global/local cycles with skipped/active frames without managed allocation on Linux/.NET 10. Native allocations, other platforms and owner visual acceptance remain unverified. [ADR 0089](../decisions/physics-activity.md#adr-0089) defines this profile. [ProcessInfo counters](#statistics) now publish completed CPU/GPU steps.

<a id="body-state"></a>
## Typed body state

The five state capabilities have concrete typed getter/setter pairs for any live
scene or server body RID. They share validation and state with direct views.
Attached access requires the world's owner thread outside solver execution; a
failed GPU world rejects further state access. Wrong-kind, empty and released
RIDs throw `ArgumentException`; nonfinite velocity throws `ArgumentOutOfRangeException`
and an invalid pose throws `ArgumentException`, before mutation. Scene parent
transforms are respected through global coordinates. Detached bodies retain
configuration without allocating a physics world.

| Signature | Contract |
| --- | --- |
| `public static void BodySetTransform(RID body, Transform transform)` | Finite unit-scale, zero-skew global pose. |
| `public static Transform BodyGetTransform(RID body)` | Current scene presentation or raw solver pose. |
| `public static void BodyGetTransform(RID space, ReadOnlySpan<RID> bodies, Span<Transform> transforms)` | Ordered caller-owned batch of the same poses; validates the complete sequence before writing, preserves duplicates and unused destination elements. |
| `public static void BodySetLinearVelocity(RID body, Vector2 velocity)` | Global scene units/s. |
| `public static Vector2 BodyGetLinearVelocity(RID body)` | Live total velocity, or detached configuration. |
| `public static void BodySetAngularVelocity(RID body, float velocity)` | Radians/s. |
| `public static float BodyGetAngularVelocity(RID body)` | Live total angular velocity, or detached configuration. |
| `public static void BodySetSleeping(RID body, bool sleeping)` | Explicit dynamic sleep/wakeup. |
| `public static bool BodyGetSleeping(RID body)` | Current or detached sleep state. |
| `public static void BodySetCanSleep(RID body, bool canSleep)` | Automatic sleep permission. |
| `public static bool BodyGetCanSleep(RID body)` | Permission, true by default. |
| `public static void BodySetAxisVelocity(RID body, Vector2 axisVelocity)` | Replace the component along the supplied axis; retain perpendicular velocity. |

<a id="bodysettransform"></a><a id="bodygettransform"></a>
Raw kinematic bodies initialize their first assigned pose immediately. Later
assignments replace the pending target; a nonzero active step traverses its path
and consumes it. Queries/getters retain the old pose until then. Zero steps,
suspension and detach/reentry preserve an unconsumed target. Static and dynamic
transforms apply immediately; moving a static support wakes touching bodies. Scene bodies retain their existing presentation:
AnimatableBody can defer its displayed pose, while CharacterBody and frozen
kinematic bodies preserve immediate query targets. All roles keep their existing
unit-scale physics restriction.

<a id="bodysetlinearvelocity"></a><a id="bodygetlinearvelocity"></a>
<a id="bodysetangularvelocity"></a><a id="bodygetangularvelocity"></a>
Velocity assignments wake dynamics, including explicit zero writes. Static and
kinematic velocity supplies virtual surface motion; a kinematic target contributes
actual travel velocity during its step, without integrating the virtual component
into the pose. Getters and point queries include both. An assignment replaces the
current component as well as its configured surface value. StaticBody properties
are projected bidirectionally; RigidBody keeps its existing freeze/disable stored
configuration. CharacterBody's desired movement `Velocity` is separate from its
low-level contact velocity. Low-level character state and non-rigid sleep policy
survive reentry but are not additional scene packing properties.

<a id="bodysetsleeping"></a><a id="bodygetsleeping"></a>
Explicit sleep affects dynamic roles, clears their velocities and retains pending
forces until wakeup. Static and kinematic roles ignore sleep assignments; static
bodies report inactive. Scene explicit sleep assignments suppress the automatic
sleep-change event, as the scene property does. Automatic solver transitions keep
their existing event path.

<a id="bodysetcansleep"></a><a id="bodygetcansleep"></a>
Automatic sleep permission survives detach/reentry. Setting it false wakes a
dynamic body, including detached configuration. Explicit sleep remains possible
while automatic sleep is disabled, and this state survives reattachment.

<a id="bodysetaxisvelocity"></a>
The finite vector's direction selects an axis and its magnitude selects the new
component in scene units/s. Zero preserves velocity and follows the normal wakeup
rule. Projection uses wider intermediate arithmetic; nonfinite results reject
before state mutation. For example, `PhysicsServer.BodySetAxisVelocity(body.GetRID(),
new Vector2(0, -240));` changes vertical speed while preserving horizontal speed.

[PhysicsServerStateTests](../../tests/Electron2D.Tests/PhysicsServerStateTests.cs)
runs the complete family on explicitly selected independent CPU/GPU worlds:
scene/server/direct-state consistency, raw latest targets and zero/inactive steps,
sleep/wakeup, reentry, moving-support response and owner/numeric rejection.
Sixty-four warmed state/read/solver cycles require 0/0 owner/all-thread managed
bytes. Private native lock injection remains a CPU diagnostic. This does not
establish native allocation, other platforms, full GPU completion or owner visual acceptance.
[ADR 0070](../decisions/physics.md#adr-0070) owns the typed state adaptation.

Contact limits prepare bounded point storage before fixed stepping. Every touching pair is scanned directly, retaining the deepest points without copying raw manifolds; no early pair cap hides later candidates. Rigid monitoring prepares its bounded pair/change collections at configuration time. Solver array compaction keeps the removed reference in the unused slot rather than constructing a replacement; active slots remain distinct. PhysicsSandbox profiles check collision churn and debug contact reads after warmup; native allocations remain outside the managed counter.

## Viewport world integration

[Canvas and physics worlds](../components/worlds.md) documents World.Canvas, Viewport.World/FindWorld, nearest-viewport CanvasItem access, shared rendering, independent physics, membership changes and runtime lifetime. Existing server and native kernels remain the implementation path. [WorldTests](../../tests/Electron2D.Tests/WorldTests.cs) supplies direct behavior and actual target-pixel evidence.

After an internally enabled GPU backend fails, body/area/joint teardown preserves
owner/stepping guards but skips individual raw graph destruction and partial-motion
capture. Managed bindings/views are released; the failed space reclaims raw storage
in bulk. Queries and further simulation remain rejected. See the
[GPU island graph failure contract](../components/gpu-physics.md#gpu-contact-driven-island-graph-2026-10-08).


## Joint policy accessors

| Signature | Contract |
| --- | --- |
| `public static float JointGetBias(RID joint)` | Stored [0,1] fraction, default zero/inherit. |
| `public static void JointSetBias(RID joint, float value)` | Change positional recovery fraction; zero uses the space default. |
| `public static float JointGetMaxBias(RID joint)` | Stored correction-speed cap, default float.MaxValue. |
| `public static void JointSetMaxBias(RID joint, float value)` | Finite nonnegative vector linear speed / angular-stop speed cap. |
| `public static float JointGetMaxForce(RID joint)` | Stored per-second impulse budget, default float.MaxValue/unlimited. |
| `public static void JointSetMaxForce(RID joint, float value)` | Finite nonnegative force budget shared by the joint's linear axes, separately by pure-angular rows. |
| `public static float PinJointGetSoftness(RID joint)` | Linear anchor compliance in inverse-kilogram units, default zero. |
| `public static void PinJointSetSoftness(RID joint, float value)` | Finite nonnegative linear compliance; zero keeps the rigid anchor. |
| `public static float SpaceGetConstraintDefaultBias(RID space)` | Captured or overridden joint correction default. |
| `public static void SpaceSetConstraintDefaultBias(RID space, float value)` | Finite [0,1]; updates and wakes zero-bias joints in the space. |

### General joint policy pairs

Getters return the shared scene/server authored value. Setters preserve local frames and RID, wake connected bodies and clear old joint impulses. General values are valid on Empty resources and survive clear/replacement. Joint.Bias/MaxBias/MaxForce are serializable scene projections. Invalid input rejects before mutation. MaxBias limits linear vector scene-unit/s and angular rad/s, also bounded by the current world guard of 200; springs have no positional rows. MaxForce limits force times substep duration in kg·scene-unit/s and a separate kg·scene-unit²/s angular channel; the N·m motor cap remains additional. See [Joint](Joint.md#bias-maxbias-maxforce) and [the equations](../components/physics-joint-policies.md).

### Pin softness pair

Accepts a configured pin or an unconfigured scene PinJoint RID. Other roles reject. Softness is linear inverse-mass compliance, not angular spring tuning. Live edits preserve anchors and clear/wake like the general policy; raw role replacement resets the pin-specific value. PackedScene preserves PinJoint.Softness.

### Space correction default pair

The constructor captures ProjectSettings.Physics2DDefaultConstraintBias with feature overrides, initially 0.2. Replacing it changes only joints with zero authored Bias; explicit nonzero values remain effective. This is a joint recovery parameter, not contact-separation bias. Other applicable space parameters retain their separate pending coverage.

All access checks resource identity and related world ownership/phase, including an endpoint-free scene joint's attachment space. ArgumentException reports stale/wrong resources or concrete roles; ArgumentOutOfRangeException reports invalid numbers; InvalidOperationException reports solver-owned or off-owner access. PhysicsJointPolicyTests exercises both CPU and the internally enabled stage GPU path; independent GPU public-world integration remains open.

## Continuous collision

The two body CCD accessors use the shared CCDMode enum; scene and server bodies
share one runtime setting. Area/shape/freed RIDs reject. See [continuous collision](../components/cpu-continuous-collision.md)
for force/impulse cadence, geometry limits and actual backend acceptance.

## Logical shape identities and infinite boundaries

`public static RID WorldBoundaryShapeCreate()` creates an owned upward zero-distance
half-plane. `ShapeSetData`/`ShapeGetData` preserve its typed raw normal and distance.
`public static ShapeType ShapeGetType(RID shape)` validates a live shape and returns
its logical authored geometry, independent of compiled fixture decomposition or
short-shape fallbacks. See [ShapeType](PhysicsServer.ShapeType.md) for numeric values
and [WorldBoundaryShape](WorldBoundaryShape.md) for geometry/lifetime/verification.
The Custom identity does not provide an extension factory.

## World sleep settings

| Signature | Value and default |
| --- | --- |
| `public static float SpaceGetBodyLinearVelocitySleepThreshold(RID space)` | Scene units/s; project default 2. |
| `public static void SpaceSetBodyLinearVelocitySleepThreshold(RID space, float value)` | Sets the finite nonnegative linear threshold. |
| `public static float SpaceGetBodyAngularVelocitySleepThreshold(RID space)` | Radians/s; project default 0.13962634. |
| `public static void SpaceSetBodyAngularVelocitySleepThreshold(RID space, float value)` | Sets the finite nonnegative angular threshold. |
| `public static float SpaceGetBodyTimeToSleep(RID space)` | Seconds; project default 0.5. |
| `public static void SpaceSetBodyTimeToSleep(RID space, float value)` | Sets the finite nonnegative quiet duration. |

Each new world captures ProjectSettings feature overrides. Existing worlds retain
their settings. Both speeds must be strictly below the thresholds for longer than
the duration. Zero velocity thresholds disable automatic sleep; zero duration needs
one eligible positive interval. A changed value wakes dynamics and resets timers;
equal writes do neither. Explicit sleep and CanSleep remain independent. Positive
linear thresholds must survive backend unit conversion.

All six operations reject stale/wrong RIDs with ArgumentException and non-owner or
in-solver access with InvalidOperationException. Setters reject invalid values with
ArgumentOutOfRangeException before mutation. Inactive worlds allow configuration;
post-solver callbacks can change future intervals, retaining committed changes if
a later callback fails. Scene RIDs share exactly the same operations. See
[World sleep policy](../components/physics-sleep.md) for tests and measurements.

## World contact correction

| Signature | Contract |
| --- | --- |
| `public static float SpaceGetContactDefaultBias(RID space)` | Current fallback correction fraction. |
| `public static void SpaceSetContactDefaultBias(RID space, float value)` | Finite [0,1], default 0.8 from project settings. |
| `public static float SpaceGetContactMaxAllowedPenetration(RID space)` | Current correction slack in scene units. |
| `public static void SpaceSetContactMaxAllowedPenetration(RID space, float value)` | Finite nonnegative representable distance, project default 0.3. |

New worlds sample feature overrides; existing worlds retain their policy. Changed
settings wake dynamic bodies and reset quiet timers; equal writes do neither.
Slack leaves shallow contacts reportable and does not change collision filtering,
friction or bounce. Shape nonzero biases override the world as described in
[Shape](Shape.md#customsolverbias). Correction is distributed across solver intervals.

These typed operations require a live space RID, its owner thread and a non-solver
phase, like the sleep settings above. Invalid RID, phase/thread and value errors use
ArgumentException, InvalidOperationException and ArgumentOutOfRangeException respectively,
before mutation. Inactive worlds remain configurable. Source:
[ContactPolicy.API](../../src/Servers/Physics/PhysicsServer.ContactPolicy.API.cs).
See [contact policy](../components/physics-contact-policy.md) for backend limits.

## Solver iteration count

| Signature | Contract |
| --- | --- |
| `public static int SpaceGetSolverIterations(RID space)` | Current positive sweep count, initially the captured project value. |
| `public static void SpaceSetSolverIterations(RID space, int value)` | Sets contact and joint sweeps per scheduled substep. |

The project default is sixteen. This count controls convergence work independently
of time integration and substep count. CPU performs that many biased and relaxation
sweeps; resident GPU uses its physical/position iteration loop. Higher counts do not
multiply force or correction budgets. Positive integer requests are retained without
clamping; large requests can be expensive. Nonpositive values throw
ArgumentOutOfRangeException before mutation. A changed count wakes dynamics and
resets quiet timers; equal writes preserve them. These operations use the same
RID, owner-thread and solver-phase guards as world contact settings, including
inactive spaces and scene-owned RIDs. Source: [ContactPolicy.API](../../src/Servers/Physics/PhysicsServer.ContactPolicy.API.cs).
See [solver settings](../components/physics-contact-policy.md#solver-iterations).

## Contact history limits

| Signature | Contract |
| --- | --- |
| `public static float SpaceGetContactRecycleRadius(RID space)` | Current local-anchor reuse radius, initially one scene unit. |
| `public static void SpaceSetContactRecycleRadius(RID space, float value)` | Sets the strict distance bound on both cached body-local boundary anchors. |
| `public static float SpaceGetContactMaxSeparation(RID space)` | Current normal/tangential history-validation distance, initially 1.5 scene units. |
| `public static void SpaceSetContactMaxSeparation(RID space, float value)` | Discards history beyond either separation bound. |

New worlds capture typed project values with feature overrides. Values are finite
nonnegative scene distances, with representable squares in both backend and scene
units; positive squared backend values cannot underflow to zero. Invalid values throw
ArgumentOutOfRangeException before mutation. Live RID, owner-thread and solver-phase
validation follow the other world settings above. Changed values wake dynamics and
reset quiet timers; equal writes preserve sleep and an explicit later sleep wins.

Both local boundary anchors must move strictly less than the recycle radius; zero
radius disables reuse. Old anchors transformed into current world poses must remain
within the normal separation and tangential drift bounds (equality is accepted).
Zero separation allows stable penetrating contacts without drift. Fresh contact
geometry is always recomputed; query margins and body/shape enter/exit events do not
acquire hysteresis. Reused impulses cannot be duplicated across fresh points.
See [contact persistence](../components/physics-contact-policy.md#contact-history-limits)
for backend storage, tests and limits. All nine space-parameter capabilities now
have concrete typed operations; this does not complete the rest of PhysicsServer.

## Body collision priority

| Signature | Contract |
| --- | --- |
| `public static float BodyGetCollisionPriority(RID body)` | Read the retained relative recovery weight, default one. |
| `public static void BodySetCollisionPriority(RID body, float priority)` | Write a finite positive weight without rebuilding fixtures or waking bodies. |

Both operations accept live scene or server bodies, including detached bodies.
An Area/wrong/freed RID throws ArgumentException. Invalid priority throws
ArgumentOutOfRangeException before mutation; attached owner/step violations throw
InvalidOperationException. Scene properties and server operations project the same
value. Priority weights obstacle contacts during BodyTestMotion recovery and the
shared PhysicsBody/CharacterBody movement paths. It does not change rigid solver
mass, impulses or direct-space ray/shape query filtering. See [policy and verification](../components/physics-contact-policy.md#collision-priority).


## Canvas association

| Operation | Contract |
| --- | --- |
| `BodyAttachCanvasInstanceID(RID body, ulong id)` | Assign body point-query association without changing fixtures, sleep or pose. |
| `BodyGetCanvasInstanceID(RID body)` | Read the retained scene/server body association. |
| `AreaAttachCanvasInstanceID(RID area, ulong id)` | Assign Area point-query association without changing overlaps. |
| `AreaGetCanvasInstanceID(RID area)` | Read the retained scene/server Area association. |

Zero identifies the default canvas. Scene canvas entry selects the nearest
CanvasLayer.InstanceID and exit clears it; reentry replaces a manual override.
Raw server associations survive detach/reattach. Keys are opaque 64-bit values,
retain no object and are distinct from World.Canvas RIDs. The owning RID must be
live and of the correct kind; attached owner/solver guards also apply to reads.
Point queries compare exact equality before result caps. This filter does not
change physical collisions, Area overlaps, ray/shape casts or coordinates.
[Canvas verification](../components/physics-queries.md#canvas-association) records
CPU/GPU comparisons and the remaining public GPU-world boundary.


BodyAttachObject and AreaAttachObject borrow a live ElectronObject (null clears). BodyGetObjectInstanceID and AreaGetObjectInstanceID retain the assigned numeric identity. See the [object association contract](../components/physics-object-bindings.md) for scene/server ownership, guards, snapshots and events.

<a id="statistics"></a>
## Completed-step statistics

`public static int GetProcessInfo(ProcessInfo processInfo)` reads one of
[ActiveObjects, CollisionPairs or IslandCount](PhysicsServer.ProcessInfo.md) from
currently active registered worlds. CPU and GPU worlds contribute once each;
shared viewports do not multiply a world. Samples publish after a successful
positive step, before user callbacks. Local deactivation removes a contribution;
reactivation, global suspension and zero elapsed time retain its previous sample.
Freeing a world removes it. Reads are safe on any thread and issue no GPU work or
managed allocation. Invalid selectors throw ArgumentOutOfRangeException; summed
integer overflow throws OverflowException. Candidate/island totals reflect actual
backend work, including sensor candidates and deferred native island splitting.

[Performance](Performance.md) exposes the same samples. Both are permanent named
Engine services. See [implementation and verification](../components/physics-statistics.md).

## Tick-qualified local history

`SpaceGetTick(space)` reports the world-local simulation tick. Active positive
intervals, including empty ones, advance once; skipped intervals do not.
`SpaceCreateCheckpoint(space)` returns a captured [PhysicsCheckpoint](PhysicsCheckpoint.md)
that restores this tick together with local simulation/observer state. It is not a
portable network snapshot. See [local checkpoints](../components/physics-space-checkpoints.md)
for timing, error and ownership boundaries.

## Body collision filters

<a id="bodygetcollisionlayer"></a>
### `BodyGetCollisionLayer(RID body)`

Reads all 32 authored category bits, including zero and bit 32. Scene bodies, caller-owned bodies and generated tile bodies use the same operation. Attachment, disabled geometry and tree exit do not replace the metadata. The RID must identify a live body; wrong-kind, stale and freed identities throw ArgumentException. Attached off-owner, solver-owned and failed-world access throws InvalidOperationException.

<a id="bodygetcollisionmask"></a>
### `BodyGetCollisionMask(RID body)`

Reads all 32 authored accepted-category bits with the same identity and access rules as BodyGetCollisionLayer. For a PhysicalBone, these are configured bits; inactive-follower fixtures can still use effective zero filters.

<a id="bodysetcollisionlayer"></a>
### `BodySetCollisionLayer(RID body, uint layer)`

Stores any unsigned 32-bit layer value. Scene properties and server reads agree immediately. Attached fixture metadata updates without recompiling geometry or replacing public body/shape identity. Assignments wake the body and existing contact neighbors even for unchanged bits. Queries observe the edit immediately; contact/sensor snapshots publish on the next step. Generated tile-body edits are runtime state; a tile-authoring rebuild may reassert its TileSet policy. Errors use the getter's identity/access contract and reject before changing authoring.

<a id="bodysetcollisionmask"></a>
### `BodySetCollisionMask(RID body, uint mask)`

Uses the same mutation, wake, lifetime and failure rules. Body motion tests compare the moving mask with target layers, independently of target masks. A physical pair is admitted by either endpoint; each dynamic endpoint responds only when its own mask matches. See [collision filter execution and checks](../components/physics-filters.md).

[Report-only contacts](../components/physics-report-only.md) now include kinematic/static and kinematic/kinematic pairs on CPU/GPU when either endpoint enables reporting. Impulses and positional response are zero; two static bodies remain ineligible. Caps, captured owners/indices, frozen-body scene events and checkpoint metadata share the existing contact contract.

BodySetMaxContactsReported synchronizes live reporting eligibility for both scene
and caller-owned bodies. A positive cap immediately exposes a kinematic receiver
as active; contact membership still refreshes on the next completed step.

## Registered initialization

Explicit and lazy scene worlds now use PhysicsServer.CreateRegisteredSpace. It allocates and registers an immutable space RID before invoking the selected implementation factory outside the registry lock. Metadata/settings exist first; ordinary query access rejects until an implementation is retained. The initialization phase can then construct guarded direct-space extensions and query the real selected world. Activation, stepping, attachment changes, recursive initialization and release reject until it completes. Explicit spaces publish inactive; scene spaces become active after successful initialization.

Failure retires owned/direct-state/space registrations and releases any implementation retained by that space. A foreign implementation is rejected without disposing its original world. Initialization and cleanup errors are aggregated after cleanup attempts. [Ownership tests](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.Initialization.cs) exercise live RID lookup, registry-lock independence, all six query hooks, transfer rejection, real motion after publication and CPU/GPU native retirement. This is the internal construction transaction; public registered factories and their complete operation bridge remain open.

## Retained named selection

Existing CPU/GPU creation resolves built-in entries through the retained internal [PhysicsBackendRegistry](PhysicsBackendRegistry.md). A captured selection can retain its requested token and actual registration across lazy creation and shared World aliases. The common initialization transaction invokes the selected factory outside registry locks; internal name diagnostics distinguish a custom registration or built-in fallback. Public named registration and the space-creation operation are exercised; complete extension-operation acceptance remains open. See [selection behavior and checks](../components/physics-backends.md#named-implementation-selection).

## Selected shape-pair operations

The internal resource/resource and body-slot/resource bridge now executes through the retained implementation with complete logical indices, composed global poses and validated nested caller-span results. Detached bodies use the same exact resource kernel without a solver; borrowed collider release is preflighted before registry cleanup. GPU scalar pose demand gathers sixteen pose bytes plus eight status bytes instead of complete motion/field/sleep state when no current cache exists. [The pair contract and checks](../components/physics-shape-pairs.md) distinguish this internal prerequisite from the pending complete public server-extension family.

Internal `GetBodyShapePair(RID body, int index)` returns the logical shape RID and composed body/slot pose. `BodyCollideShapeCore(RID body, int index, RID shape, Transform pose, Vector2 motion, Span<Vector2> contacts, out int count)` selects an attached owner or executes a detached resource query, preserving complete caller point pairs and validated output. These native implementation capabilities are absent from the pinned bound public facade table; their eventual public/protected projection belongs to the complete backend/extension family.

## Factory contact publication

A factory-returned cached body extension now projects its contact count/values on the world owner after native capture and before scene/raw observer delivery. Complete validated snapshots commit atomically; cached getters retain immutable data and do not reinvoke contact hooks. Independent caller-created views keep ordinary guarded direct hook dispatch. Qualified publication permits immutable PhysicsBodyContact identity construction while ordinary solver queries/mutation stay blocked. Bad receivers retain their prior published state while other callbacks and committed ticks continue. [The executing contract and verification](../components/physics-body-extensions.md#selected-contact-publication) retain public registration/native/platform/performance limits.

[Registered numerical adapter](../components/physics-registered-adapter.md) now connects a public value/RID-only implementation to real scene/raw motion, fields/forces, joints, queries, contact/Area publication, private/portable history and terminal failure. Its independent circle/spring test solver owns its own state and does not wrap a built-in. Public registered factories and the typed family now execute in a separate consumer; arbitrary custom geometry and unexercised operation acceptance remain open.

Public [registered implementations](../components/physics-registered-adapter.md) now execute through PhysicsServerManager, World(string) and PhysicsServer.SpaceCreate(string). Requested/actual names retain the captured registration and startup fallback identity. The scoped PhysicsServerBackend/Extension family and immutable inputs/results have a public-only circle/spring consumer; custom geometry and full extension/performance/platform acceptance remain open.

[Custom collision geometry](../components/physics-custom-geometry.md) now provides copied canonical data, exact mass/bounds, diagnostic segments and standalone pair callbacks to registered implementations. Built-in compilation rejects unsupported custom geometry before mutation. Portable schema covers the custom authoring payload; detached force preparation resolves exact custom mass while automatic getters retain cached-resolution semantics.


Registered [initial collider metadata](../components/physics-registered-adapter.md#initial-collider-metadata) receives retained object/canvas IDs and priority before shapes. Body/Area live metadata writes and raw release/transfer reject a borrowed extension scope before changing bindings, including initial attachment before SpaceRID publication. Common getters and weak sampled query identities retain their existing contract.

Body/Area shape-slot and layer/mask writes, shared server-shape data replacement and release reject while their registered implementation is borrowed, before changing retained authoring. This also covers partial initial attachment; guarded getters and immutable result construction retain their existing access. See [borrowed shape authoring](../components/physics-registered-adapter.md#borrowed-shape-authoring).

Body force, mass, integration, motion, sleep, CCD and callback authoring now
reject before mutation during selected numerical hooks, including initial
attachment. Direct-body setters/forces use the same qualified numerical guard.
Guarded authored reads, ordinary post-solver callbacks and caller-created
body-state forwarding remain available; hierarchy/resource-lifetime guards still
cover every extension scope. See [body authoring](../components/physics-registered-adapter.md#borrowed-body-authoring).

[Registered Area fields](../components/physics-registered-adapter.md#registered-area-fields) now exercise the complete typed profile/default shortcut through the selected hooks, including independent override/point/mask response and zero warmed edited full frames. Numerical-hook writes reject before mutation; authored reads and ordinary Area callback edits retain their guards and overlap history. Generic solver/extension acceptance remains open.

[Parameter reads](../components/physics-registered-adapter.md#parameter-reads-and-preparation) project configured values without numerical preparation. Automatic inertia/center reads still synchronize current geometry outside numerical borrowing; preparation rejects inside a numerical hook. Initial/live scene/raw and zero-allocation public-only checks execute; full solver/extension acceptance remains open.

[Common server modes](../components/physics-registered-adapter.md#common-server-mode-family) now project all four modes through scene and raw RIDs, with independent scene descriptors, effective state/force response, pose publication, reentry and replay. CPU/GPU/registered checks share the public consumer; complete solver and final performance acceptance remain open.

Callback registration, force omission and contact caps share scene/raw guarded configuration. The public-only integration consumer checks the same numerical behavior on CPU/GPU and an independent implementation: pending/persistent force omission with active impulses/contacts, force-scene-sync ordering, strongly typed userdata, exception continuation and clearing delegates. Numerical implementation hooks cannot change these settings or inherited scene integration/contact properties; ordinary game callbacks retain access.

Collision-exception add/remove preserves insertion order and requests wakeup even for duplicate/absent entries. Numerical hooks cannot change body or joint exclusion authoring. Geometry remains reusable; [selected exception checks](../components/physics-registered-adapter.md#collision-exception-authoring-and-response) cover physical response, immediate motion, sensor independence and warmed memory.


All typed common/pin/spring parameter getter/setter pairs share retained scene/raw authoring. Initial/live selected numerical hooks reject writes before mutation, including unchanged assignments; ordinary state callbacks may edit the next interval.


[Selected Area pose/monitor conformance](../components/physics-registered-adapter.md#area-pose-and-monitor-publication) covers creation, transform access, both typed callbacks and monitorability through CPU/GPU/registered owners. Scene policy changes reject from the same Area's server observer before mutation; equal scene assignments and pose reads remain available during ordinary callbacks.


BodySetContactsReportedDepthThreshold(RID, float) and BodyGetContactsReportedDepthThreshold(RID) retain scene/raw reporting policy. Zero disables filtering; positive finite scene-unit thresholds include equal/deeper candidates on the next publication, preserving completed snapshots and physical response. [Shared conformance](../components/physics-registered-adapter.md#contact-reporting-depth) covers CPU/GPU/registered guards, lifetime, history and zero warm allocation.

[Notification delivery status](../components/physics-registered-adapter.md#notification-delivery-status)
now shares world-scoped lifecycle state across CPU/GPU and registered implementations.
Body integration/state callbacks, contacts, Areas and sleep events are included;
exceptions and nested calls preserve the existing delivery/order/cleanup guarantees.

### SpaceIsFlushingQueries

`public static bool SpaceIsFlushingQueries(RID space)` reads a live scene-owned
or explicitly created world's notification phase, requiring its owner thread.
It shares the backend property's value, including nested queries and terminal-error
cleanup, without numerical reads or allocations. Unknown/non-space/retired RIDs
throw ArgumentException; off-owner reads throw InvalidOperationException; disposed
service/space reads throw ObjectDisposedException. Ordinary false status does not
certify that a failed world can step again.


### Selected shape-pair queries

`ShapeCollide(RID space, RID first, Transform firstPose, Vector2 firstMotion,
RID second, Transform secondPose, Vector2 secondMotion, Span<Vector2> contacts,
out int count)` dispatches through the addressed live space. Resources need not
be attached. `BodyCollideShape(RID body, int index, RID shape, Transform pose,
Vector2 motion, Span<Vector2> contacts, out int count)` chooses the body's actual
attachment, or standalone resource geometry while detached. The logical slot pose
includes its body pose; disabled slots and zero filters remain explicit inputs.

Even-length caller spans contain alternating first/body and second-shape global
points. `count` is a pair count. A hit can return zero pairs at zero capacity;
misses and unused tails preserve caller storage. All returned counts and points
validate before publication. Invalid RID/slot/pose/capacity, off-owner/solver/failed
world access and hook failures reject without partially replacing output.
[Public-only conformance](../components/physics-registered-adapter.md#public-shape-pair-dispatch)
covers CPU/GPU and an independent numerical implementation.


[Motion result receiver identity](../components/physics-registered-adapter.md#motion-result-receiver-identity)
preserves the moving RID authored by SetCollision through selected-query validation.
A same-world result for another moving body rejects before caller publication;
no-result calls enforce the same rule. SetMotion misses retain normal requested-body
binding, and rejection restores scope for the next valid query. The numerical
motion/geometry acceptance remains broader than this identity guarantee.


[Shared mass-profile conformance](../components/physics-bodies.md#shared-mass-profile-conformance)
now executes the full scene/raw mass, center, inertia and lifecycle matrix on CPU
and GPU worlds. Both retain zero owner/all-thread allocation through 64 changing
active frames; native implementation diagnostics remain owner-specific.


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


[Registered factory-selected views](../components/physics-registered-adapter.md#factory-selected-direct-views) now execute through a separate public-only consumer: cache identity, an initially empty contact snapshot, synchronized immutable publication before callbacks, result/lifetime validation, recreation and zero warm managed allocations. Independent caller-created body views keep direct hook dispatch; wider numerical-extension acceptance stays open.

Shared Shape.CustomSolverBias edits preflight every scene/raw borrower before
mutation, including equal assignments, disabled slots and a scene-held server
geometry view. [Shape](Shape.md#customsolverbias) defines the owner/phase/borrow
guards, observer failure and retained geometry identity.

Shared ShapeSetData failure restores the original resource and attempts rollback
for every raw borrower even if an earlier rollback fails. Scene users are marked
dirty, rejected private geometry is retired, and apply/rollback/retirement errors
are retained together. Incomplete borrowers stay dirty so public query/step
preparation retries before observing them. An old-view retirement error after a
successful replacement does not undo the committed new geometry.

[Body construction and continuous policy](../components/physics-registered-adapter.md#body-construction-and-continuous-policy)
now share selected CPU/GPU/registered ownership, detached/live modes, automatic
mass reset and guarded typed configuration. Valid CCD writes avoid enum metadata
allocation after collection; the public-only consumer checks 128 edited full
steps at 0/0 managed bytes. Registered numerical CCD and full acceptance remain open.

The complete PhysicsAreaFieldTests, PhysicsServerAreaFieldTests and PhysicsAreaMonitorTests now execute on explicitly selected CPU/GPU worlds. They retain all five gravity/damping modes, priorities, point transforms/falloff, default-world shortcuts, signed damping, directional masks, logical pair identity, callback failure/removal and lifecycle assertions. Initial unchanged child registration preserves a sleeping body; changed fields still wake it. Sixty-four measured moving, sleeping and edited-field frames and 64 monitor transition cycles (128 frames) require zero owner/all-thread managed bytes. CPU field validation failure can be corrected; a started GPU field-reduction failure is terminal, preserves the selected backend and last published scene field snapshot, and permits resource retirement. Native allocations, other platforms, visual and final performance acceptance remain open.

Complete PhysicsBodyStateTests and PhysicsServerForceTests now execute on explicitly selected public CPU/GPU worlds, alongside the common PhysicsContactImpulseTests and report-only role suite. They retain state/field/force/center values, post-solver callback edits/errors/removal/reentry, borrowed-view lifetime, force omission and pending-input rules. Real scene/raw dynamic and kinematic fixtures expose six uncapped contacts and verify deepest 2/1/0 caps and the corresponding monitor objects without private list-order assumptions; static/static pairs remain ineligible. Momentum checks retain their .08/.1 scene-impulse bounds, equal/opposite and rotating-normal/transient/sleep assertions. Warm gates require 0/0 owner/all-thread bytes for contact/callback/force frames, zero-cap frames and live pose reads. CPU native scheduling/depth-injection checks remain separate diagnostics; legacy staged GPU controls do not establish independent GPU acceptance. Native/platform, visual and final network/performance gates remain open.

The complete shared PhysicsCollisionExceptionTests now executes scene/raw exceptions on explicitly selected public CPU/GPU worlds, retaining both motion directions, solved contact release/restoration, idempotent edits, opaque/freed targets and reentry. Sixty-four warmed changed raw-exception/step cycles require 0/0 owner/all-thread managed bytes. Internal GPU store probes remain diagnostics; broader inherited/extension, native/platform and final performance acceptance stays open.

New world/space creation captures ProjectSettings.Physics2DRunOnSeparateThread with feature overrides, retaining it across aliases and later project edits. True runs complete selected Solve on a retained numerical lane and joins before owner publication/callbacks; unsupported hosts reject startup. [Thread conformance](../components/physics-thread.md) records CPU/provider checks and the open GPU/native gate.


Point-field reduction avoids float squared-distance underflow/overflow and
retains signed constant/inverse-square behavior, exact-center zero and rejection
of truly unrepresentable forces. PhysicsPointGravityRangeTests covers scene, raw
Area and default-space sources with three body roles and CPU zero-allocation frames.
The device kernel now passes the point-range and terminal-failure contract on NVIDIA GeForce RTX 3090 Ti/Vulkan; native allocation, other drivers/platforms and performance acceptance remain open.
See [point-field verification](../components/physics-areas.md#point-gravity-numeric-range).


Point-gravity center transformation and coordinate subtraction retain widened CPU
intermediates before normalization. The shared test covers opposite finite 2e38
coordinates, signed direction and falloff, affine virtual centers, three body roles
and zero-allocation full frames. Collision precision at such coordinates and real
GPU performance remain separate gates. See [field numeric range](../components/physics-areas.md#point-gravity-numeric-range).

[Shared public contact correction](../components/physics-contact-policy.md#shared-public-correction-2026-10-14)
now runs the complete shape/world bias, slack, resource/lifetime and scene sleep/event
suite on explicitly selected CPU/GPU worlds in serial/separate numerical modes.
Seven forms include both bias-one endpoints; 128 measured edited full steps require
0/0 owner/all-thread managed bytes. Software Vulkan execution and historical direct
resident/stage controls remain distinct from hardware/performance acceptance.

[Shared public priority verification](../components/physics-contact-policy.md#shared-public-priority-2026-10-14)
now exercises weighted recovery, scene/server/CharacterBody motion, packing and
lifetime through explicit CPU/GPU worlds in both numerical-thread modes. Changing
priority preserves sleep and fixture identity over 128 measured full frames at
0/0 owner/all-thread managed bytes; query brackets retain the same zero budget.
Software Vulkan execution does not establish hardware or broader acceptance.

[Shared public contact history](../components/physics-contact-policy.md#shared-public-history-2026-10-14)
now checks seven geometries, live reuse limits, actual support/impulses and scene
sleep/event ordering through explicit CPU/GPU worlds in both numerical-thread modes.
All six public/resident/stage profiles retain 128 edited full-step samples at
0/0 owner/all-thread managed bytes. Software Vulkan evidence leaves hardware,
registered geometry and broader network/platform/performance acceptance open.

[Shared public iteration verification](../components/physics-contact-policy.md#shared-public-iterations-2026-10-14)
now exercises captured/live counts, contact/pin convergence, momentum, fixed-time
integration and sleep/phase guards on explicit CPU/GPU worlds in both numerical
thread modes. All six profiles preserve 256 active bodies and 1/16/32-sweep full
step brackets with 0/0 owner/all-thread managed bytes. Hardware and broader
registered/network/platform/performance acceptance remain open.

[Shared public sleep verification](../components/physics-sleep.md#shared-public-sleep-policy-2026-10-14)
now checks the complete strict threshold/duration, callback/lifetime and scene event
suite on explicit CPU/GPU worlds in both numerical-thread modes. All six profiles
retain 64 active or wake/sleep-cycling bodies at 0/0 warm owner/all-thread bytes.
The public-only CPU/GPU/registered consumer additionally verifies connected groups
and quiet-clock restoration. Hardware and broader acceptance remain open.

[Public angular CCD](../components/physics-registered-adapter.md#angular-compound-motion)
now checks asymmetric mass-center motion and intermediate rotational impact on
CPU/GPU/registered worlds in serial/separate modes. Exact slots, opposite frame
impulses, authored CCD modes and one callback/tick retain their contract; both
128-warm/128-measured impact brackets require 0/0 managed bytes. GPU executes on
CPU-only llvmpipe; hardware, broader geometry and final acceptance remain open.

[Mass roles](../components/physics-registered-adapter.md#mass-profiles-across-roles)
and [portable role correction](../components/physics-snapshots.md#portable-body-roles)
now execute explicit CPU/GPU/registered serial and separate numerical profiles.
Scene/raw automatic/custom centers, inverse dynamics, point velocity and exact
slot queries survive role changes; CPU/GPU round trips preserve pending force/
torque consumption and atomic authoring rejection. Original warm brackets retain
0/0 managed bytes. GPU uses llvmpipe; hardware and broader acceptance stay open.

The resident point-field reducer preserves guarded float operation order with GLSL precise intermediates and propagates reconstruction validity separately from the value. Values beyond the finite output range do not enter subsequent arithmetic as infinity sentinels. A later Replace/ReplaceCombine contribution resets validity with the channel; Combine retains an invalid earlier contribution. Final invalid fields fail before body-field publication or solver motion through the existing eight-byte status read and terminal GPU-world policy. Mantissa/exponent normalization avoids reciprocals outside the normal range for large/tiny coordinates. No CPU mirror, buffer growth, new readback or wait is introduced. See [hardware point-field guards](../components/physics-areas.md#hardware-point-field-guards-2026-10-14).


[Shape-pair operand borrowing](../components/physics-shape-pairs.md#operand-borrowing-2026-10-14)
now protects both resources through standalone and selected callbacks, including
inherited copying/reset and owned RID replacement/retirement. Nested reads and
error cleanup retain their existing contract; full numerical/foreign/hardware
acceptance remains open.


[Borrowed motion output](../components/physics-registered-adapter.md#borrowed-motion-output-2026-10-14) preserves selected scratch lifetime and owner scope while retaining caller-owned snapshots, error continuation and retirement failures. The inherited disposal preflight is a reviewed C# adaptation; full numerical/foreign/hardware acceptance stays open.

[Completed numerical statistics](../components/physics-registered-adapter.md#completed-numerical-statistics)
now exercise actual mixed scene/raw constraints, sensors, kinematic activity and
sleep/replay on CPU/software-GPU/registered serial/separate profiles. Selector
validation no longer rebuilds enum metadata after collection; complete moving
frames plus all process/Performance reads retain strict 0/0 managed bytes.
Registered output with Islands greater than Active fails before publication.


[Registered priority recovery](../components/physics-registered-adapter.md#registered-priority-recovery)
now exercises weighted motion recovery independently through the public API,
including finite/directed families, bounded contact planes, exclusions, one-way
controls, character movement and unchanged rigid momentum. The shared CPU/GPU/
registered profile covers five 128-warmup/128-measured full-frame brackets at zero
managed allocation. Wider registered contact-cache settings and arbitrary coupled correction and complete numerical,
native/network/platform/performance acceptance remain open.


[Registered contact numerics](../components/physics-registered-adapter.md#registered-contact-numerics)
now exercises actual world/shape bias and penetration slack, complete contact/joint
sweeps, full physical impulses and identity-preserving live policy through the
public-only provider. Seven source families, dense contacts, CCD durations and
contact/pin convergence use nineteen complete 128-warmup/128-measured brackets
at zero managed allocation. Registered contact-cache, arbitrary coupled/custom,
full inherited/extension/network and native/platform/performance acceptance remain
open.


[Moving one-way recovery](../components/gpu-resident-motion-queries.md#moving-one-way-recovery)
now includes kinematic/dynamic linear approach over the last physical step on
both production backends, including private/portable restoration. Static surface,
receding/tangent and angular-only velocities do not enlarge recovery depth.
PhysicsMovingOneWayTests checks raw/scene callers, roles, rotated slots, six finite
source families and eight 128-warmup/128-measured complete-frame brackets at zero
managed allocation. Native/platform/hardware/performance acceptance remains open.

### Failed raw shape edits

Attached raw Body/Area shape edits restore prior logical authoring and attempt
numerical restoration after an error. A failed restoration aggregates both errors
and leaves preparation pending before the next query/step. Scene slot edits keep
deferred preparation. See [the complete edit family and verification](../components/physics-queries.md#failed-raw-shape-edits-and-warmed-reuse).

### Observer preparation failure

Both Area callback setters prepare common/selected monitoring capacity before
publishing a replacement. Failure preserves the old delegates, both histories
and queued-event generation. Successful null/equal/new registrations retain
ordinary reset and next-nonzero-scan semantics. See [transactions and checks](../components/physics-queries.md#area-observer-registration-transactions).

### Filter update failures

Body/Area layer and mask setters commit configured bits after numerical success.
A failed update preserves authoring and queues current effective-filter repair
before the next numerical query or active positive-duration step. A failed retry
propagates without advancing the interval. See [the complete scene/raw family](../components/physics-filters.md#failed-updates-and-effective-sensing).

## Procedural shape data

[Typed server shape data](../components/physics-shape-data.md) now accepts a
procedural Shape by represented ShapeType and exports borrowed procedural RIDs
without a duplicate factory. Copies use concrete engine wrappers, retain common
resource metadata and solver bias, and keep geometry independent from the source.
Both operations borrow every using world while suppliers run; existing replacement
rollback and view-retirement semantics remain intact.

[Shared shape replacement](../components/physics-shape-data.md#shared-cpugpuregistered-replacement)
now holds every participating world through numerical rebuild, rollback and
rejected-copy retirement. One implementation hook cannot edit or retire another
participant. Old committed-view retirement occurs after those world borrows end,
preserving ordinary callback mutation and committed error semantics.

Server data operations freeze the addressed RID's consumer membership before status
or geometry hooks and keep it frozen through rebuild, rollback and rejected-copy
retirement. Incoming enabled/disabled Body/Area slots in unrelated worlds, direct
owner additions and CollisionShape assignments of borrowed views reject before
changing storage. Detached existing consumers also reject edits and release.
The guard is released on supplier/status failure and before committed old-view
retirement, so later ordinary bindings remain available.

[Shared owned-shape release](../components/physics-shape-data.md#shared-shape-release) now borrows resource membership and every
participating world through removal and private-view retirement. Status failure
preserves live data; committed cleanup attempts all users, retains exact errors
and unregisters the RID. Public-only checks retain unrelated slots, exact Area
entry/exit histories and zero warmed managed bytes. Wider acceptance stays open.

The [one-way slot operation audit](../components/physics-shapes.md#one-way-slot-operation-audit)
checks complete per-slot policy delivery, raw rollback and deferred scene retry,
full stored-group reclaim, normalized/default/zero direction and actual selected
motion/recovery. Warm unchanged-policy query/full-frame intervals require 0/0
managed bytes; changed immutable authoring remains a separate measured cost.
Complete registered numerical response and native/platform/network/performance
acceptance retain their independent gates.

[Body-pair publication recovery](../components/physics-shape-pairs.md#body-pair-publication-recovery-and-composed-pose-validation)
validates the slot/resources, then prepares that body's pending motion/mode/pose
before selected pair dispatch. The composed pose validates before that hook. Errors preserve caller points/count and the tick;
resource-only queries remain independent, and later kinematic targets retain their
step boundary. Expanded public profiles keep strict zero-allocation complete
frames and the existing GPU pose-only transfer boundary. Full acceptance is separate.

[Filter setter operation audit](../components/physics-filters.md#filter-setter-operation-audit)
completes shared Body/Area layer/mask authoring through selected UpdateFilter.
Argument/wake/thread checks, eight-role failure repair and real directional response
retain configured metadata, logical identity and zero managed allocation in full
moving frames. Geometry, numerical and platform/performance gates remain separate.

[Prepared Area field inputs](../components/physics-areas.md#prepared-area-field-inputs)
permit all ten configured getters for the owning scene/raw/default profile inside
selected numerical hooks, including the captured solver thread. Existing common
input guards preserve ordinary owner/failure/lifetime checks; writes remain guarded.
Readonly profiles match selected preparation and require no live field-state query.
The public 64-frame edited/acceleration fixture retains strict zero managed bytes.
