# PhysicsServer

Last updated: 2026-09-26

**Inherits:** ElectronObject · **Source:** [PhysicsServer.cs](../../src/Servers/Physics/PhysicsServer.cs), [PhysicsServer.Resources.cs](../../src/Servers/Physics/PhysicsServer.Resources.cs), [PhysicsServer.Mass.cs](../../src/Servers/Physics/PhysicsServer.Mass.cs)

## Description

The process-wide registry for typed 2D physics RIDs. It registers each SceneTree's existing Box2D world and its scene CollisionObject identities, and can also create explicit spaces, bodies, Areas and the six implemented shape families. A server-created collider can join either kind of space; [World2D](World2D.md) and `SpaceGetDirectState` query that same solver state. RID values never expose Box2D IDs and never resolve to a later object after free. The server singleton cannot be disposed by consumers.

## Example

```csharp
var server = PhysicsServer.Instance;
RID space = server.SpaceCreate();
RID body = server.BodyCreate();
RID shape = server.CircleShapeCreate();
using var circle = new CircleShape { Radius = 12 };
server.ShapeSetData(shape, circle);
server.BodySetMode(body, PhysicsServer.BodyMode.Static);
server.BodyAddShape(body, shape);
server.BodySetSpace(body, space);
using var ray = PhysicsRayQueryParameters2D.Create(new(0, -40), new(0, 40));
var hit = server.SpaceGetDirectState(space).IntersectRay(ray);
server.FreeRID(body);
server.FreeRID(shape);
server.FreeRID(space);
```

## API summary

| Member | Contract |
| --- | --- |
| `public static PhysicsServer Instance { get; }` | Shared process server. |
| `public enum BodyMode` | [Static, Kinematic, Rigid, RigidLinear](PhysicsServer.BodyMode.md). |
| `public RID SpaceCreate()` | Caller-owned independent physics space. |
| `public void SpaceStep(RID space, double delta)` | Advance only an explicitly created space; zero delta is inert. |
| `public PhysicsDirectSpaceState SpaceGetDirectState(RID space)` | Cached query view of any live server/scene space. |
| `public bool BodyTestMotion(RID body, PhysicsTestMotionParameters2D parameters, PhysicsTestMotionResult2D? result = null)` | Test a scene or server body against its current space without moving it; optionally fill typed output. |
| `public void BodyAddCollisionException(RID body, RID exceptedBody)` / `BodyRemoveCollisionException(RID body, RID exceptedBody)` | Change a one-sided body-owned RID exception affecting both solver contacts and motion tests. |
| `public RID BodyCreate()` / `AreaCreate()` | Detached rigid body or sensor Area with default layer/mask one. |
| `public RID CircleShapeCreate()` / `RectangleShapeCreate()` | Default concrete geometry RIDs. |
| `public RID CapsuleShapeCreate()` / `SegmentShapeCreate()` | Default concrete geometry RIDs. |
| `public RID SeparationRayShapeCreate()` | Twenty-unit downward directed ray, SlideOnSlope false. |
| `public RID ConvexPolygonShapeCreate()` / `ConcavePolygonShapeCreate()` | Empty polygon geometry RIDs. |
| `public void ShapeSetData(RID shape, Shape data)` | Copy same-kind caller geometry into a server shape and rebuild users. |
| `public Shape ShapeGetData(RID shape)` | Caller-owned independent geometry copy. |
| `public void BodyAddShape(RID body, RID shape, Transform? transform = null, bool disabled = false)` | Add one indexed body shape slot; null transform is identity. |
| `public void AreaAddShape(RID area, RID shape, Transform? transform = null, bool disabled = false)` | Add one indexed sensor shape slot. |
| `public int BodyGetShapeCount(RID body)` / `AreaGetShapeCount(RID area)` | Count slots, including disabled ones. |
| `public void BodySetShapeDisabled(RID body, int index, bool disabled)` / `AreaSetShapeDisabled(RID area, int index, bool disabled)` | Rebuild one indexed slot's fixtures. |
| `public void BodyRemoveShape(RID body, int index)` / `AreaRemoveShape(RID area, int index)` | Remove one slot and its fixtures. |
| `public void BodySetSpace(RID body, RID space)` / `AreaSetSpace(RID area, RID space)` | Attach to a live space; empty RID detaches. |
| `public RID BodyGetSpace(RID body)` / `AreaGetSpace(RID area)` | Current space RID, or empty while detached. |
| `public void BodySetTransform(RID body, Transform transform)` / `AreaSetTransform(RID area, Transform transform)` | Set finite unit-scale, zero-skew pose. |
| `public Transform BodyGetTransform(RID body)` | Current solver pose, including dynamic movement. |
| `public void BodySetLinearVelocity(RID body, Vector2 velocity)` | Finite scene units per second. |
| `public void BodySetMode(RID body, BodyMode mode)` / `BodyMode BodyGetMode(RID body)` | Change/read the solver motion mode. |
| `public void BodySetCollisionLayer(RID body, uint layer)` / `BodySetCollisionMask(RID body, uint mask)` | Rebuild body fixtures with 32-bit filters. |
| `public void AreaSetCollisionLayer(RID area, uint layer)` | Rebuild Area sensor fixtures with 32-bit queryable layers. |
| `public void FreeRID(RID rid)` | Free a caller-owned space, body, Area or shape. |
| `protected override void ValidateDisposal()` | Reject consumer disposal of the singleton. |

## Method descriptions

<a id="spaces"></a>
### Space creation, access and stepping

`SpaceCreate` allocates one real Box2D world with the current sampled 2D project gravity default. `SpaceGetDirectState` returns a live view of any registered explicit or SceneTree space; disposing a view allows the next lookup to create another. `SpaceStep` accepts a finite nonnegative delta only for explicit spaces, so it cannot double-step a scene world. The SceneTree advances its own registered space in its fixed physics lane. Space mutation and querying require the creating/owning thread and reject a world currently stepping. Explicit spaces with server-created bodies advance the real solver, not a parallel query-only representation. Scene Area and RigidBody damping reduction is separate and does not yet apply to server-only bodies.

<a id="colliders"></a>
### Collider creation, shapes and ownership

`BodyCreate` defaults to rigid mode; `AreaCreate` defaults to a stationary nonresponding sensor. Neither has a space until `BodySetSpace` or `AreaSetSpace`. The six shape constructors correspond to the already implemented circle, rectangle, capsule, segment, convex and concave resource families. `ShapeSetData` rejects a disposed or different-kind Shape, duplicates caller data and rebuilds attached users; `ShapeGetData` returns another independent duplicate. Each `BodyAddShape` or `AreaAddShape` appends an indexed slot with a finite unit-scale, zero-skew local transform, default identity, and optional disabled state. Slot-count, disabled-toggle and removal methods make those indices executable after attachment; invalid indices throw before mutation. Compound fixtures from one slot share its public query ShapeIndex. An explicit collider may be moved into the SceneTree's `World2D.Space`; direct queries then see it beside scene nodes. Queries return its RID with a null scene Collider.

<a id="body-state"></a>
### Body state and filters

`BodySetMode` supports all four numeric mode values and rejects undefined input. Static, kinematic and rigid bodies share the Box2D world; RigidLinear locks rotation and clears angular velocity. Switching to Static or Kinematic clears linear and angular velocity while retaining the solved pose. The typed `BodySetTransform`, `BodySetLinearVelocity` and `BodyGetTransform` methods cover the corresponding transform/linear-velocity branches of the dynamic reference state API. A moving body's solved pose and velocity are captured before space detachment, preserving state when reattached. Body layer/mask and Area layer setters accept all 32 bits; direct queries match the layer independently of the collider's mask. Server-only Area mask writes remain [Blocked](../coverage/classes/PhysicsServer2D.md) until Area overlap monitoring or fields consume them. Other body states, forces, parameters, callbacks and Area field parameters retain separate coverage gaps.

`BodyTestMotion` prepares pending scene and server fixtures, then tests the supplied body's own shapes from a typed global pose. Reciprocal body filters, RID and managed-instance exclusions, one-way surfaces, recovery margin and initial overlap are applied. It returns false on a miss and updates an optional [PhysicsTestMotionResult2D](PhysicsTestMotionResult2D.md) with full travel and cleared contact fields. On a hit it reports contact identity, point, normal, depth, velocity, local/collider shape-owner indices and safe/unsafe fractions. It never changes the actual body pose. A detached body or wrong RID rejects; off-owner and in-step calls reject. [PhysicsTestMotionParameters2D](PhysicsTestMotionParameters2D.md) names the input. `CollideSeparationRay` enables non-sliding ray sweeps; sliding rays and recovery obey [ADR 0068](../decisions/physics.md#adr-0068).

`BodyAddCollisionException` and `BodyRemoveCollisionException` edit only the owner's RID list. Either body's entry suppresses the pair in fixed-step solver contacts and `BodyTestMotion`, independent of reciprocal collision masks; Area monitoring is unaffected. Duplicates and absent removals do nothing. The owner must be a live scene or server body; an arbitrary excepted RID, including an empty or later freed one, is retained but cannot match a live pair. An attached owner requires its space thread and cannot change exceptions while stepping. A list change marks that owner's fixtures for rebuilding before the next query or step, including when the pair is already touching. Freeing an owner removes its own entries; other bodies can retain its RID as an inert exception until explicitly removed.

<a id="free"></a>
### `FreeRID`

Frees only caller-owned server resources. A shape free removes its slots from live users; a body/Area free detaches its backend object; a space free detaches its server colliders and invalidates retained query views. Collider RIDs remain live and detached after their space is freed, so they can be assigned to another space. SceneTree spaces and scene CollisionObject RIDs must be released by their owning objects; attempting to free them here throws. Stale or wrong-kind RIDs reject without resolving to a later resource. The `RID` value itself remains nonzero after free.

## Verification and limits

[PhysicsQueryTests](../../tests/Electron2D.Tests/PhysicsQueryTests.cs) checks scene/server shared world, explicit stepping, six shape families, filters, modes, cross-space moves and RID lifecycle. [PhysicsShapeQueryTests](../../tests/Electron2D.Tests/PhysicsShapeQueryTests.cs) checks direct shape operations; [PhysicsMotionTests](../../tests/Electron2D.Tests/PhysicsMotionTests.cs) checks body motion; [PhysicsCollisionExceptionTests](../../tests/Electron2D.Tests/PhysicsCollisionExceptionTests.cs) checks unilateral scene/server lists, live solver and motion filtering, owner/target lifetime and warmed allocation. Remaining server methods, world-boundary/separation-ray/custom shapes, joints and direct body state remain separate slices. Scene Area field/event delivery does not yet represent server-only colliders in typed object-level events. Query paths scan fixtures linearly; native allocator accounting, other platforms and large-world throughput remain unverified. See [ADR 0063](../decisions/physics.md#adr-0063).

`SeparationRayShapeCreate()` returns caller-owned default directed geometry. ShapeSetData copies length and slope policy; ShapeGetData returns an independent copy. Direct shape/body motion and sensing execute; ordinary ray solver impulses remain incomplete on the [resource class](SeparationRayShape.md).

## Body state and callbacks

| Signature | Contract |
| --- | --- |
| `public PhysicsDirectBodyState? BodyGetDirectState(RID body)` | Cached attachment view, null while detached. |
| `public void BodySetForceIntegrationCallback(RID body, Action<PhysicsDirectBodyState>? callback)` | Set/clear a post-solver force callback. |
| `public void BodySetForceIntegrationCallback<T>(RID body, Action<PhysicsDirectBodyState, T>? callback, T userData)` | Strongly typed data adapter allocated at registration. |
| `public void BodySetStateSyncCallback(RID body, Action<PhysicsDirectBodyState>? callback)` | Set/clear the following sync observer. |
| `public void BodySetOmitForceIntegration(RID body, bool enable)` | Control default gravity/damping/force omission. |
| `public bool BodyIsOmittingForceIntegration(RID body)` | Read omission policy. |
| `public void BodySetMaxContactsReported(RID body, int amount)` | Set a nonnegative contact-point cap. |
| `public int BodyGetMaxContactsReported(RID body)` | Read the configured cap. |

Setters replace the previous user delegate; null clears it. Typed generic userdata replaces the dynamic callback boundary without invocation-time boxing. These APIs accept scene or server body identities and reject Area/wrong/stale RIDs. Attached access uses the owning space thread outside native stepping. Detached bodies retain callbacks, constants, omission and caps; they have no live view. Freeing a body clears that registry state.

Force callback runs before the RigidBody integration hook and user sync observer. Scene-owned synchronization remains mandatory under the typed host architecture; a user sync observer supplements it. Scene pose/cache synchronization brackets hooks. Callback failures are collected; body removal safely invalidates captured entries. Recursive stepping and world disposal reject during callback dispatch. [PhysicsBodyStateTests](../../tests/Electron2D.Tests/PhysicsBodyStateTests.cs) checks these contracts under [ADR 0070](../decisions/physics.md#adr-0070).

`public void AreaSetMonitorable(RID area, bool monitorable)` controls whether scene monitoring Areas detect a server-created Area. Server Areas default false; the next nonzero overlap scan adopts the flag. Attached changes require the space owner thread outside native stepping. Shape events carry its RID with a null scene Area. Scene-object overlap arrays stay scene-only. Server free preserves exit values and finishes registry cleanup despite callback failure. ShapePairEventTests verifies body and Area nullable payloads under ADR 0055.

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

All live scene/server Area RIDs are accepted; pose/layer/monitorable setters use that same projection. [AreaBodyStatus](PhysicsServer.AreaBodyStatus.md) reports Added=0/Removed=1. Remaining payload is other RID, stable scene InstanceID or zero for server-only, other global logical shape index and local index. Compound fixtures deduplicate per logical pair. The receiver mask tests other layer without reciprocal masks. Area callbacks include only monitorable peers; explicit server Areas default non-monitorable.

Registration resets both histories even for the same delegate; null clears without synthetic exits, and current overlaps enter again at the next nonzero scan. Scene-owned overlap arrays/events remain independent and mandatory; an external observer is not its replacement and can observe while scene Monitoring is false. Snapshot state commits before dispatch; failures continue later events and do not replay. Own receiver configuration mutation from a raw or scene overlap callback rejects, while reads are allowed. Epoch changes suppress stale queued callbacks. Other collider removal/free emits retained exits immediately; removal from an entry callback suppresses stale later entries and safely delivers departures.

Receiver detach clears history silently and retains configuration for reentry. Receiver free/world disposal clears history/event storage; cleanup of a freed other collider completes even if its departure callbacks throw. Wrong-kind/freed RID and off-owner/solver-owned access reject. [PhysicsAreaMonitorTests](../../tests/Electron2D.Tests/PhysicsAreaMonitorTests.cs) verifies payload/filter/lifetime/errors and 64 warmed active entry/exit cycles with zero managed allocation. [ADR 0077](../decisions/physics-monitoring.md#adr-0077) records the ownership adaptation. Server field parameters and remaining shape/instance/canvas operations retain their own coverage gaps; native allocation, large-world performance, other platforms and owner acceptance remain unverified.
