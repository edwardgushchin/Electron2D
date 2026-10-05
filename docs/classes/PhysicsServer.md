# PhysicsServer

Last updated: 2026-10-05

**Inherits:** ElectronObject · **Source:** [PhysicsServer.cs](../../src/Servers/Physics/PhysicsServer.cs), [PhysicsServer.Resources.cs](../../src/Servers/Physics/PhysicsServer.Resources.cs), [PhysicsServer.Mass.cs](../../src/Servers/Physics/PhysicsServer.Mass.cs)

Public static declarations are in [`PhysicsServer.API.cs`](../../src/Servers/Physics/PhysicsServer.API.cs).

## Description

Public operations and events use static access to the retained object under [ADR 0095](../decisions/singleton-services.md#adr-0095). Object state, identity, property discovery and the owning domain lifetime rules remain intact.

The process-wide registry for typed 2D physics RIDs. It registers each SceneTree's existing Box2D world and its scene CollisionObject identities, and can also create explicit spaces, bodies, Areas, joints and the six implemented shape families. A server-created collider can join either kind of space; [World](World.md) and `SpaceGetDirectState` query that same solver state. RID values never expose Box2D IDs and never resolve to a later object after free. The server singleton cannot be disposed by consumers.

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

| Member | Contract |
| --- | --- |
| `public enum BodyMode` | [Static, Kinematic, Rigid, RigidLinear](PhysicsServer.BodyMode.md). |
| `public static RID SpaceCreate()` | Caller-owned inactive independent physics space. |
| `public static void SpaceStep(RID space, double delta)` | Advance only an explicitly created space; zero delta is inert. |
| `public static PhysicsDirectSpaceState SpaceGetDirectState(RID space)` | Cached query view of any live server/scene space. |
| `public static bool BodyTestMotion(RID body, PhysicsTestMotionParameters parameters, PhysicsTestMotionResult? result = null)` | Test a scene or server body against its current space without moving it; optionally fill typed output. |
| `public static void BodyAddCollisionException(RID body, RID exceptedBody)` / `BodyRemoveCollisionException(RID body, RID exceptedBody)` | Change a one-sided body-owned RID exception affecting both solver contacts and motion tests. |
| `public static RID BodyCreate()` / `AreaCreate()` | Detached rigid body or sensor Area with default layer/mask one. |
| `public static RID CircleShapeCreate()` / `RectangleShapeCreate()` | Default concrete geometry RIDs. |
| `public static RID CapsuleShapeCreate()` / `SegmentShapeCreate()` | Default concrete geometry RIDs. |
| `public static RID SeparationRayShapeCreate()` | Twenty-unit downward directed ray, SlideOnSlope false. |
| `public static RID ConvexPolygonShapeCreate()` / `ConcavePolygonShapeCreate()` | Empty polygon geometry RIDs. |
| `public static void ShapeSetData(RID shape, Shape data)` | Copy same-kind caller geometry into a server shape and rebuild users. |
| `public static Shape ShapeGetData(RID shape)` | Caller-owned independent geometry copy. |
| `public static void BodyAddShape(RID body, RID shape, Transform? transform = null, bool disabled = false)` | Add one indexed body shape slot; null transform is identity. |
| `public static void AreaAddShape(RID area, RID shape, Transform? transform = null, bool disabled = false)` | Add one indexed sensor shape slot. |
| `public static int BodyGetShapeCount(RID body)` / `AreaGetShapeCount(RID area)` | Count slots, including disabled ones. |
| `public static void BodySetShapeDisabled(RID body, int index, bool disabled)` / `AreaSetShapeDisabled(RID area, int index, bool disabled)` | Rebuild one indexed slot's fixtures. |
| `public static void BodyRemoveShape(RID body, int index)` / `AreaRemoveShape(RID area, int index)` | Remove one slot and its fixtures. |
| `public static void BodySetSpace(RID body, RID space)` / `AreaSetSpace(RID area, RID space)` | Attach to a live space; empty RID detaches. |
| `public static RID BodyGetSpace(RID body)` / `AreaGetSpace(RID area)` | Current space RID, or empty while detached. |
| `public static void BodySetTransform(RID body, Transform transform)` / `AreaSetTransform(RID area, Transform transform)` | Set finite unit-scale, zero-skew pose. |
| `public static Transform BodyGetTransform(RID body)` | Current solver pose, including dynamic movement. |
| `public static void BodySetLinearVelocity(RID body, Vector2 velocity)` | Finite scene units per second. |
| `public static void BodySetMode(RID body, BodyMode mode)` / `BodyMode BodyGetMode(RID body)` | Change/read the solver motion mode. |
| `public static void BodySetCollisionLayer(RID body, uint layer)` / `BodySetCollisionMask(RID body, uint mask)` | Rebuild body fixtures with 32-bit filters. |
| `public static void AreaSetCollisionLayer(RID area, uint layer)` | Rebuild Area sensor fixtures with 32-bit queryable layers. |
| `public static void FreeRID(RID rid)` | Free a caller-owned space, body, Area, joint or shape. |
| `protected override void ValidateDisposal()` | Reject consumer disposal of the singleton. |

## Method descriptions

<a id="spaces"></a>
### Space creation, access and stepping

`SpaceCreate` allocates one inactive real Box2D world with the current sampled 2D project gravity default. `SpaceGetDirectState` returns a live view of any registered explicit or SceneTree space; disposing a view allows the next lookup to create another. `SpaceStep` accepts a finite nonnegative delta only for explicit spaces, so it cannot double-step a scene world. The SceneTree activates and advances its own registered space in its fixed physics lane. Caller-created worlds need SpaceSetActive(space, true) before SpaceStep can simulate; global/local suspension skips the solver interval without accumulating time. Space mutation and querying require the creating/owning thread and reject a world currently stepping. Explicit spaces with server-created bodies advance the real solver, not a parallel query-only representation. Scene Area and RigidBody damping reduction is separate and does not yet apply to server-only bodies.

<a id="colliders"></a>
### Collider creation, shapes and ownership

`BodyCreate` defaults to rigid mode; `AreaCreate` defaults to a stationary nonresponding sensor. Neither has a space until `BodySetSpace` or `AreaSetSpace`. The six shape constructors correspond to the already implemented circle, rectangle, capsule, segment, convex and concave resource families. `ShapeSetData` rejects a disposed or different-kind Shape, duplicates caller data and rebuilds attached users; `ShapeGetData` returns another independent duplicate. Each `BodyAddShape` or `AreaAddShape` appends an indexed slot with a finite unit-scale, zero-skew local transform, default identity, and optional disabled state. Slot-count, disabled-toggle and removal methods make those indices executable after attachment; invalid indices throw before mutation. Compound fixtures from one slot share its public static query ShapeIndex. An explicit collider may be moved into the SceneTree's `World.Space`; direct queries then see it beside scene nodes. Queries return its RID with a null scene Collider.

<a id="body-state"></a>
### Body state and filters

`BodySetMode` supports all four numeric mode values and rejects undefined input. Static, kinematic and rigid bodies share the Box2D world; RigidLinear locks rotation and clears angular velocity. Switching to Static or Kinematic clears linear and angular velocity while retaining the solved pose. The typed `BodySetTransform`, `BodySetLinearVelocity` and `BodyGetTransform` methods cover the corresponding transform/linear-velocity branches of the dynamic reference state API. A moving body's solved pose and velocity are captured before space detachment, preserving state when reattached. Body layer/mask and Area layer setters accept all 32 bits; direct queries match the layer independently of the collider's mask. Server-only Area mask writes remain [Blocked](../coverage/classes/PhysicsServer2D.md) until Area overlap monitoring or fields consume them. Other body states, forces, parameters, callbacks and Area field parameters retain separate coverage gaps.

`BodyTestMotion` prepares pending scene and server fixtures, then tests the supplied body's own shapes from a typed global pose. Reciprocal body filters, RID and managed-instance exclusions, one-way surfaces, recovery margin and initial overlap are applied. It returns false on a miss and updates an optional [PhysicsTestMotionResult](PhysicsTestMotionResult.md) with full travel and cleared contact fields. On a hit it reports contact identity, point, normal, depth, velocity, local/collider shape-owner indices and safe/unsafe fractions. It never changes the actual body pose. A detached body or wrong RID rejects; off-owner and in-step calls reject. [PhysicsTestMotionParameters](PhysicsTestMotionParameters.md) names the input. `CollideSeparationRay` enables non-sliding ray sweeps; sliding rays and recovery obey [ADR 0068](../decisions/physics.md#adr-0068).

`BodyAddCollisionException` and `BodyRemoveCollisionException` edit only the owner's RID list. Either body's entry suppresses the pair in fixed-step solver contacts and `BodyTestMotion`, independent of reciprocal collision masks; Area monitoring is unaffected. Duplicates and absent removals do nothing. The owner must be a live scene or server body; an arbitrary excepted RID, including an empty or later freed one, is retained but cannot match a live pair. An attached owner requires its space thread and cannot change exceptions while stepping. A list change marks that owner's fixtures for rebuilding before the next query or step, including when the pair is already touching. Freeing an owner removes its own entries; other bodies can retain its RID as an inert exception until explicitly removed.

<a id="free"></a>
### `FreeRID`

Frees only caller-owned server resources. Joint free removes its native handle and pair contribution, then invalidates its RID; body free clears dependent joints. World free suspends caller-owned connections with local frames/settings retained. A shape free removes its slots from live users; a body/Area free detaches its backend object; a space free detaches its server colliders and invalidates retained query views. Collider RIDs remain live and detached after their space is freed, so they can be assigned to another space. SceneTree spaces, scene joint RIDs and scene CollisionObject RIDs must be released by their owning objects; attempting to free them here throws. Stale or wrong-kind RIDs reject without resolving to a later resource. The `RID` value itself remains nonzero after free.

## Verification and limits

[PhysicsQueryTests](../../tests/Electron2D.Tests/PhysicsQueryTests.cs) checks scene/server shared world, explicit stepping, six shape families, filters, modes, cross-space moves and RID lifecycle. [PhysicsShapeQueryTests](../../tests/Electron2D.Tests/PhysicsShapeQueryTests.cs) checks direct shape operations; [PhysicsMotionTests](../../tests/Electron2D.Tests/PhysicsMotionTests.cs) checks body motion; [PhysicsCollisionExceptionTests](../../tests/Electron2D.Tests/PhysicsCollisionExceptionTests.cs) checks unilateral scene/server lists, live solver and motion filtering, owner/target lifetime and warmed allocation. Remaining server methods, world-boundary/separation-ray/custom shapes, joint tuning/debug drawing and wider direct body state remain separate slices. Scene Area field/event delivery does not yet represent server-only colliders in typed object-level events. Query paths scan fixtures linearly; native allocator accounting, other platforms and large-world throughput remain unverified. See [ADR 0063](../decisions/physics.md#adr-0063).

`SeparationRayShapeCreate()` returns caller-owned default directed geometry. ShapeSetData copies length and slope policy; ShapeGetData returns an independent copy. Direct shape/body motion and sensing execute; ordinary ray solver impulses remain incomplete on the [resource class](SeparationRayShape.md).

## Body state and callbacks

| Signature | Contract |
| --- | --- |
| `public static PhysicsDirectBodyState? BodyGetDirectState(RID body)` | Cached attachment view, null while detached. |
| `public static void BodySetForceIntegrationCallback(RID body, Action<PhysicsDirectBodyState>? callback)` | Set/clear a post-solver force callback. |
| `public static void BodySetForceIntegrationCallback<T>(RID body, Action<PhysicsDirectBodyState, T>? callback, T userData)` | Strongly typed data adapter allocated at registration. |
| `public static void BodySetStateSyncCallback(RID body, Action<PhysicsDirectBodyState>? callback)` | Set/clear the following sync observer. |
| `public static void BodySetOmitForceIntegration(RID body, bool enable)` | Control default gravity/damping/force omission. |
| `public static bool BodyIsOmittingForceIntegration(RID body)` | Read omission policy. |
| `public static void BodySetMaxContactsReported(RID body, int amount)` | Set a nonnegative contact-point cap. |
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

All live scene/server Area RIDs are accepted; pose/layer/monitorable setters use that same projection. [AreaBodyStatus](PhysicsServer.AreaBodyStatus.md) reports Added=0/Removed=1. Remaining payload is other RID, stable scene InstanceID or zero for server-only, other global logical shape index and local index. Compound fixtures deduplicate per logical pair. The receiver mask tests other layer without reciprocal masks. Area callbacks include only monitorable peers; explicit server Areas default non-monitorable.

Registration resets both histories even for the same delegate; null clears without synthetic exits, and current overlaps enter again at the next nonzero scan. Scene-owned overlap arrays/events remain independent and mandatory; an external observer is not its replacement and can observe while scene Monitoring is false. Snapshot state commits before dispatch; failures continue later events and do not replay. Own receiver configuration mutation from a raw or scene overlap callback rejects, while reads are allowed. Epoch changes suppress stale queued callbacks. Other collider removal/free emits retained exits immediately; removal from an entry callback suppresses stale later entries and safely delivers departures.

Receiver detach clears history silently and retains configuration for reentry. Receiver free/world disposal clears history/event storage; cleanup of a freed other collider completes even if its departure callbacks throw. Wrong-kind/freed RID and off-owner/solver-owned access reject. [PhysicsAreaMonitorTests](../../tests/Electron2D.Tests/PhysicsAreaMonitorTests.cs) verifies payload/filter/lifetime/errors and 64 warmed active entry/exit cycles with zero managed allocation. [ADR 0077](../decisions/physics-monitoring.md#adr-0077) records the ownership adaptation. Server field parameters and remaining shape/instance/canvas operations retain their own coverage gaps; native allocation, large-world performance, other platforms and owner acceptance remain unverified.

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

[PhysicsServerJointTests](../../tests/Electron2D.Tests/PhysicsServerJointTests.cs) covers all three actual server responses, world pin, bidirectional settings, clear/replacement, defaults, lifecycle/failure/phase/thread rollback, independent exceptions and 64 warmed active typed-setting/spring frames with zero managed allocation on Linux/.NET 10. Native allocations, broad-scene stability/performance, other platforms and owner visual acceptance remain unverified. General joint positional bias/correction speed/force caps, linear pin softness and scene debug drawing remain exact coverage gaps; angular spring tuning does not supply them.

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
**BodyGetShape / AreaGetShape:** Return the effective slot's shape identity. Caller-created shape IDs remain caller-owned; managed Shape IDs remain resource-owned. Getters confer no release ownership. An absent index throws ArgumentOutOfRangeException; wrong/stale collider identities throw ArgumentException. An active world requires its owner thread outside solver/sync phases.

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

[PhysicsActivityTests](../../tests/Electron2D.Tests/PhysicsActivityTests.cs) checks native motion/spring and pending-force behavior, inactive queries, defaults, callback/timer continuation, phase/thread/lifetime guards, callback failure and 64 warmed global/local cycles with skipped/active frames without managed allocation on Linux/.NET 10. Native allocations, other platforms and owner visual acceptance remain unverified. [ADR 0089](../decisions/physics-activity.md#adr-0089) defines this profile. ProcessInfo counters remain a separate verification/integration slice.

## Body-state completion dependency

The transform/linear-velocity state facade is still server-only and Partial. Full scene/server state parity needs typed angular velocity, sleep and can-sleep access, deferred kinematic transform targets and exact static surface velocity. The latter requires an actual normal/tangential contact-point velocity channel while pose stays fixed; native static bodies use zero dummy solver state, so storing values or tangentSpeed alone is insufficient. It enters the first stationary-contact solver integration under [ADR 0075](../decisions/physics.md#adr-0075), together with StaticBody constant surface velocities; no feature patch was added to vendored code.
