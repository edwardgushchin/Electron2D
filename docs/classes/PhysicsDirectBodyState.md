# PhysicsDirectBodyState

Last updated: 2026-10-19

[Gravity publication and terminal parameter guards](../components/physics-registered-adapter.md#gravity-publication-and-terminal-parameter-guards) retry the latest effective native gravity/omission request after a selected error and discard it on attachment retirement. Scene damping, omission and inherited material setters reject failed worlds before changing authoring while healthy post-solver callbacks retain writes. Public CPU/software-GPU/registered JIT/NativeAOT profiles verify real motion, history boundaries and strict zero managed-byte frames; wider acceptance remains open.

[Mass profile publication recovery](../components/physics-registered-adapter.md#mass-profile-publication-recovery) preserves preceding authoring after selected mass/inertia/center/reset errors and prepares it before numerical use. Cached views, space/force/checkpoint/portable boundaries, attachment retirement, in-flight rejection and post-commit notification behavior execute across all scene/raw roles. Public JIT/strict NativeAOT CPU/software-GPU/registered full-frame checks retain strict zero managed allocation; wider native/platform/network/performance gates remain open.

## Failed motion publication

Failed velocity/sleep/policy publication retains the complete requested write for retry before numerical access, body views, live queries or a nonzero step. Ordinary authored reads stay non-preparing. Linear/angular edits preserve the other selected motion channel, including impulse-derived values. Retirement overlays requested channels on retained selected motion without repairing the discarded attachment. See [motion publication](../components/physics-registered-adapter.md#failed-motion-publication) for executable scope and limits.

## Failed pose publication

Failed BodySetTransform and raw AreaSetTransform publication retains the committed pose. Live queries and nonzero steps retry current authored input; raw body scalar/batch reads and acquired or retained direct-body views also prepare incomplete publication. Scene presentation and Area pose getters remain non-preparing. Repeated errors retain their original exception and leave the tick unchanged. Detachment preserves authored pose instead of sampling a partially changed raw solver pose, discards retired attachment debt and invalidates old views. Later kinematic targets still wait for a nonzero active step. See [pose publication](../components/physics-registered-adapter.md#failed-pose-publication) for the executable scope and limits.

## Prepared body view acquisition

Body view acquisition now finishes pending direct-mode preparation before reading resolved fields, selecting a cached view or invoking its selected factory. The same boundary serves caller-created body-state extensions. A repeated preparation error creates no view, publishes no fields and does not advance the tick. Successful recovery preserves cached identity or creates one generation-qualified view; stale attachments remain rejected. See [the preparation contract](../components/physics-registered-adapter.md#body-view-acquisition-after-mode-failure).

## Failed body-mode publication

A failed direct BodySetMode publication retains the committed mode and motion policy. Numerical body access, including retained direct views and scene direct-mode velocity/sleep access, repairs the complete transition before reading or writing selected state. Live space queries and nonzero steps use the same preparation; repeated failure leaves the tick unchanged. Detachment preserves authored motion and discards retired attachment debt. Scene role takeover prepares the previous direct profile before changing its descriptors. See [mode preparation](../components/physics-registered-adapter.md#failed-body-mode-publication) for executable scope and limits.

[Retained slot history](../components/physics-snapshots.md#capture-after-topology-edits)
keeps sampled public values while rejecting portable capture of retired/reindexed
slots before replacing packet bytes. In-slot replacement and disabled history stay
valid; portable apply remaps private slot lifetimes to destination authoring.

[Angular compound verification](../components/physics-registered-adapter.md#angular-compound-motion)
checks global-axis mass-center offsets and origin-relative point velocity on CPU
and the independent registered factory while an asymmetric body translates and
rotates. CPU/provider serial and separate numerical modes pass; GPU is unverified
for this new fixture.

**Inherits:** [ElectronObject](ElectronObject.md) · **Inherited By:** [PhysicsDirectBodyStateExtension](PhysicsDirectBodyStateExtension.md)

- **Source:** [PhysicsDirectBodyState.cs](../../src/Servers/Physics/PhysicsDirectBodyState.cs)
- **Declaration:** `public class PhysicsDirectBodyState : ElectronObject`
- **Component:** [Physics server and direct queries](../components/physics-queries.md)

[GPU publication for current consumers](../components/gpu-demand-publication.md) now keeps unobserved raw bodies on device. Explicit getters select one current snapshot; scene consumers, live raw views, contact receivers and kinematic completion retain fresh data. Portable preparation batches its explicit state demand. Cached values are qualified by the completed interval epoch; callback ordering and failure/lifetime guards remain unchanged.

## Description

An owner-thread view of one backend attachment of a scene or server body. Obtain it from PhysicsServer.BodyGetDirectState or the post-solver RigidBody integration hook. It has no public constructor and owns no body or space. The server caches one view per attachment. Access rejects native stepping, wrong threads, disposal, detachment, body release and a replaced attachment; reattachment never revives an old view. Consumer disposal allows a fresh cached view to be created, but disposal during a borrowed callback rejects.

Acquiring a view synchronizes its requested body's pending pose/geometry without scanning other bodies. Direct-space queries synchronize their world independently. Scene runtime ownership retains the existing weak registration and never extends a body's lifetime. Server runtimes retain their registered collider owner and mark it released when its RID is freed, avoiding registry lookups on each live field read while retaining release and attachment guards. Frequently polled getters retain a validated attachment adapter; its backend body/world references remain private. The common view validates the adapter version before reading current solver state.

Fields read the live native body. A zero-contact view does not itself require a per-step contact snapshot; reducing the contact cap to zero clears the prior snapshot on the next solved step. Contact methods read an immutable value snapshot from the last solved step, capped by MaxContactsReported independently of object ContactMonitor. The word local identifies this body; contact positions, normals and velocities use global coordinates. Collision layer/mask changes and direct-space queries may rebuild fixtures without invalidating those stored contact values.

The selected world owner creates the cached view before solver/worker borrowing;
its identity and attachment generation are checked before publication. The internal
[factory route](../components/physics-backends.md#selected-direct-state-construction)
also supplies executing extensions to scene/server callbacks. Public named backend
registration and complete server-operation dispatch remain open.

## Example

Partial subclass snippet; keep all borrowed shape resources alive while attached:

```csharp
class ControlledBody : RigidBody
{
    protected override void IntegrateForces(PhysicsDirectBodyState state)
    {
        if (CustomIntegrator) state.IntegrateForces();
        state.LinearVelocity = new Vector2(120, state.LinearVelocity.Y);
    }
}
```

## Property summary

| Signature | Contract |
| --- | --- |
| `public Single AngularVelocity { get; set; }` | Live owner-thread field; see descriptions below. |
| `public Vector2 CenterOfMass { get;  }` | Live owner-thread field; see descriptions below. |
| `public Vector2 CenterOfMassLocal { get;  }` | Live owner-thread field; see descriptions below. |
| `public UInt32 CollisionLayer { get; set; }` | Live owner-thread field; see descriptions below. |
| `public UInt32 CollisionMask { get; set; }` | Live owner-thread field; see descriptions below. |
| `public Single InverseInertia { get;  }` | Live owner-thread field; see descriptions below. |
| `public Single InverseMass { get;  }` | Live owner-thread field; see descriptions below. |
| `public Vector2 LinearVelocity { get; set; }` | Live owner-thread field; see descriptions below. |
| `public Boolean Sleeping { get; set; }` | Live owner-thread field; see descriptions below. |
| `public Single Step { get;  }` | Live owner-thread field; see descriptions below. |
| `public Single TotalAngularDamp { get;  }` | Live owner-thread field; see descriptions below. |
| `public Vector2 TotalGravity { get;  }` | Live owner-thread field; see descriptions below. |
| `public Single TotalLinearDamp { get;  }` | Live owner-thread field; see descriptions below. |
| `public Transform Transform { get; set; }` | Live owner-thread field; see descriptions below. |

## Method summary

| Signature | Contract |
| --- | --- |
| `public Void AddConstantCentralForce(Vector2 force = default)` | Guarded live operation or retained contact lookup. |
| `public Void AddConstantForce(Vector2 force, Vector2 position = default)` | Guarded live operation or retained contact lookup. |
| `public Void AddConstantTorque(Single torque)` | Guarded live operation or retained contact lookup. |
| `public Void ApplyCentralForce(Vector2 force = default)` | Guarded live operation or retained contact lookup. |
| `public Void ApplyCentralImpulse(Vector2 impulse)` | Guarded live operation or retained contact lookup. |
| `public Void ApplyForce(Vector2 force, Vector2 position = default)` | Guarded live operation or retained contact lookup. |
| `public Void ApplyImpulse(Vector2 impulse, Vector2 position = default)` | Guarded live operation or retained contact lookup. |
| `public Void ApplyTorque(Single torque)` | Guarded live operation or retained contact lookup. |
| `public Void ApplyTorqueImpulse(Single impulse)` | Guarded live operation or retained contact lookup. |
| `public Vector2 GetConstantForce()` | Guarded live operation or retained contact lookup. |
| `public Single GetConstantTorque()` | Guarded live operation or retained contact lookup. |
| `public RID GetContactCollider(Int32 contactIndex)` | Guarded live operation or retained contact lookup. |
| `public UInt64 GetContactColliderID(Int32 contactIndex)` | Guarded live operation or retained contact lookup. |
| `public CollisionObject GetContactColliderObject(Int32 contactIndex)` | Guarded live operation or retained contact lookup. |
| `public Vector2 GetContactColliderPosition(Int32 contactIndex)` | Guarded live operation or retained contact lookup. |
| `public Int32 GetContactColliderShape(Int32 contactIndex)` | Guarded live operation or retained contact lookup. |
| `public Vector2 GetContactColliderVelocityAtPosition(Int32 contactIndex)` | Guarded live operation or retained contact lookup. |
| `public Int32 GetContactCount()` | Guarded live operation or retained contact lookup. |
| `public Vector2 GetContactImpulse(Int32 contactIndex)` | Guarded live operation or retained contact lookup. |
| `public Vector2 GetContactLocalNormal(Int32 contactIndex)` | Guarded live operation or retained contact lookup. |
| `public Vector2 GetContactLocalPosition(Int32 contactIndex)` | Guarded live operation or retained contact lookup. |
| `public Int32 GetContactLocalShape(Int32 contactIndex)` | Guarded live operation or retained contact lookup. |
| `public Vector2 GetContactLocalVelocityAtPosition(Int32 contactIndex)` | Guarded live operation or retained contact lookup. |
| `public PhysicsDirectSpaceState GetSpaceState()` | Guarded live operation or retained contact lookup. |
| `public Vector2 GetVelocityAtLocalPosition(Vector2 localPosition)` | Guarded live operation or retained contact lookup. |
| `public Void IntegrateForces()` | Guarded live operation or retained contact lookup. |
| `public Void SetConstantForce(Vector2 force)` | Guarded live operation or retained contact lookup. |
| `public Void SetConstantTorque(Single torque)` | Guarded live operation or retained contact lookup. |
| `protected override Void ValidateDisposal()` | Guarded live operation or retained contact lookup. |

The public view contains no vendor body/world IDs or fixture tags. `PhysicsBodyRuntime`
validates the view/space/attachment version; `PhysicsColliderBackend` owns concrete
backend state access, unit conversion and contact traversal. The view retains
engine contact values and a weak collider owner, with lifetime checked against the
runtime's current view, space and attachment version. Detach invalidates old readers/writers even after
the same RID rejoins the same space; disposing a view permits a fresh view without
changing the attached body. Existing native storage remains behind that adapter;
this extraction does not supply an independent GPU world.

## Property descriptions

<a id="velocity"></a>
### Velocity, pose and sleep

LinearVelocity uses global scene units/s; AngularVelocity uses radians/s. Assignments require finite values and wake a dynamic body. Sleeping changes native awake state. Transform is a global translated/rotated unit-scale, zero-skew pose; invalid input rejects before applying the pose. A scene body's pose is applied to scene and backend immediately. Node velocity caches are synchronized around integration callbacks.

<a id="mass"></a>
### CenterOfMass, CenterOfMassLocal, InverseMass and InverseInertia

CenterOfMass is the offset from body origin to center along global axes; CenterOfMassLocal is that offset before body rotation. Both use scene units. InverseMass uses reciprocal kilograms; InverseInertia uses reciprocal kg times squared scene units and reports zero for a rotation lock. Static/kinematic native inverse mass and inertia are zero. These are actual native solver mass values, including child offsets and the existing shape/mass policy.

[Role-transition verification](../components/physics-registered-adapter.md#mass-profiles-across-roles)
checks retained automatic/custom centers and origin-relative point velocity on
scene/raw CPU and independent provider bodies before/after all four roles. Static
and kinematic inverse values remain zero while center metadata survives. Serial
and separate numerical modes retain strict zero warm allocation; GPU is unverified
for this new fixture.

<a id="fields"></a>
### Step, TotalGravity, TotalLinearDamp and TotalAngularDamp

Step is the last nonzero space step in seconds, zero before the first step. TotalGravity is the last selected world/Area gravity including scene body gravity scale. Damping includes current field and body combine/replace policy. Server bodies now participate in scene Area gravity/damping reduction and field-change wakeup using the same masks and shape-overlap kernel; server-only Area field configuration remains a separate gap.

<a id="filters"></a>
### CollisionLayer and CollisionMask

All 32 category bits are preserved through uint values. Scene filtering uses the existing body properties; server filtering rebuilds its indexed fixtures. The contact snapshot remains from the solved step until the next capture.

## Method descriptions

<a id="forces"></a>
### Force, impulse and persistent-force operations

Position arguments are offsets from the body origin along global axes, not rotated local positions. Force uses scene units times kg/s²; impulse uses scene units times kg/s; torque and angular impulse use kg times squared scene units per s² and s respectively. Central operations act at the actual mass center. Positioned operations include the center-relative moment. Nonfinite inputs/totals or resulting impulse velocities reject before native mutation. Static/kinematic native response follows their motion mode.

Apply*Force and ApplyTorque accumulate for the next native solver step. Impulses change velocity immediately. SetConstantForce/Torque replaces the persistent value; AddConstant* accumulates it. Positioned constant-force torque is recorded when added. Scene RigidBody constants share its public stored state; other bodies retain constants in the server runtime record across attachments. Constant writes wake a dynamic body.

<a id="integrateforces"></a>
### IntegrateForces()

Adds TotalGravity * Step to linear velocity, then applies max(0, 1 - Step * damping) to linear/angular velocity. Every call applies another equivalent tick and wakes a dynamic body. It includes no force/torque accumulators. This explicit gravity-before-damping order differs from the ordinary native-step force integration order. With CustomIntegrator, velocities written in the post-solver hook affect the next solved motion. Impulses and native contact response remain active.

<a id="contacts"></a>
### Contact methods

GetContactCount is capped by the body's configured contact limit, zero by default. Each zero-based index yields both direct shape indices, collider RID/instance identity, global contact points, global outward normal and global-axis point velocities. GetContactColliderObject returns the live scene CollisionObject or null for server-only or released objects. GetContactColliderObject<TileMapLayer> returns a live generated tile owner, while GetContactCollider retains its distinct physical body RID; the CollisionObject convenience overload remains null for that role. Contact snapshots survive fixture edits and queries during callbacks.

GetContactImpulse uses scene units times kg/s and reports impulse applied to this
body. Both normal and signed friction components sum warm starting and every solver
substep. Internal kinematic intervals accumulate vectors in world axes per canonical
fixture pair and contact feature. A shared contact is counted once even when both
ends report it. Sleeping manifolds with an older solve epoch report zero. Totals
belong to contacts observed during the completed outer frame and stay unchanged
after pose/fixture callback mutations. Short contacts that separate before the last
internal interval are retained for that frame; a later frame without the contact
clears it. Each feature uses its last observed geometry and greatest observed depth.
Per-body encounter lists avoid scanning all world contacts for every reporter.

Limits accept zero through 4095. A setter clears the old point count. Selection
fills slots in backend encounter order, then replaces the first shallowest retained
slot only for a strictly deeper candidate. Equal depths keep existing slots. Rigid
object/shape monitoring derives from exactly this selected snapshot. Depth and a
weak fixture-owner tag are private selection metadata, not new public contact fields.

[PhysicsContactImpulseTests](../../tests/Electron2D.Tests/PhysicsContactImpulseTests.cs)
compares reported impulses against actual linear momentum changes for scene/raw
bodies, checks both participants, multiple native intervals, transient bounce contacts, rotating normals, sleep reset, cap bounds
and ties, and measures 64 warmed active subdivided contact frames at zero all-thread
managed bytes on CPU and Linux/Vulkan GPU. Raw constraint checks also compare the
new tangent totals between scalar/SIMD CPU and GPU without changing tolerance.
Native allocation, foreign devices, broad-scene performance and visual acceptance
remain unverified. Large reporting limits cost O(candidates * limit). The optional
multi-interval feature map retains its observed peak; larger unseen contact/feature
sets can grow it outside the checked warm interval.

<a id="getspacestate"></a>
### GetSpaceState() and GetVelocityAtLocalPosition(...)

GetSpaceState returns the same cached direct-query view as PhysicsServer.SpaceGetDirectState. Queries are legal in the post-solver callback window. GetVelocityAtLocalPosition treats its argument as a finite global-axis offset from the body origin and includes rotation about the actual center. Malformed offsets or unrepresentable point velocity reject.

## Lifecycle, errors and verification

The server creates views on demand or during step preparation. Borrowed callback disposal is rejected; off-callback disposal invalidates only that wrapper. Detachment clears the cached wrapper while external old wrappers remain invalid. Old contacts contain values and RIDs, not owning scene references. Native world execution and recursive SpaceStep/world disposal remain forbidden; body/fixture mutations are legal after solver completion. Callback failures are aggregated after remaining callbacks and committed edits are retained.

Invalid contact indices throw ArgumentOutOfRangeException. Disposed or ended attachments throw ObjectDisposedException; thread/phase violations throw InvalidOperationException. Pose/motion/force validation uses ArgumentException/ArgumentOutOfRangeException as applicable. Public fields and methods do not expose native backend types.

[PhysicsBodyStateTests](../../tests/Electron2D.Tests/PhysicsBodyStateTests.cs) covers scene/server fields, center offsets, inertia units, force/impulse paths, Area reduction/wakeup, custom integration, callback ordering/userdata, actual solved contacts, fixture-query edits, hierarchy mutation, exceptions, stale/disposed/off-thread views, packing and 64 warmed active callback frames without managed allocations on Linux/.NET 10. Native allocation, other platforms and owner visual acceptance remain unverified. See [ADR 0070](../decisions/physics.md#adr-0070).

The [CollisionObject owner registry](CollisionObject.md#createshapeowner) now supplies logical shape slots for both child and manual groups. Query/contact indices identify global slots, while ShapeFindOwner returns the distinct group ID; removal shifts later indices. Motion owner accessors resolve weak configured objects as well as child nodes. See [ADR 0071](../decisions/physics.md#adr-0071).

## Disabled processing and physics

A CollisionObject using `DisableMode.Remove` invalidates this attachment view when its effective inherited ProcessMode becomes Disabled. Reattachment provides a new view with the same collider RID. MakeStatic retains the view and exposes zero inverse mass/inertia until the requested body type is restored. See [CollisionObject.DisableMode](CollisionObject.md#disablemode) and [ADR 0072](../decisions/physics.md#adr-0072).

## Configured versus physical mass

RigidBody and typed PhysicsServer mass parameters share configured kilograms, selected local center and automatic/explicit scene-unit polar moment. This view exposes actual inverse solver values: static/kinematic participation has zero inverse mass/inertia while preserving the resolved center; rotation lock has zero inverse inertia. Use PhysicsServer.BodyGetMass/BodyGetInertia for retained configuration/resolved geometry across detached or temporarily static states. [ADR 0073](../decisions/physics-mass.md#adr-0073) and [PhysicsMassProfileTests](../../tests/Electron2D.Tests/PhysicsMassProfileTests.cs) cover custom-center force response and restoration.

## Shared action state

ApplyForce/ApplyCentralForce/ApplyTorque queue one-step input in the shared body runtime; impulses remain immediate. Borrowed-view access validates the attachment before forwarding, while post-solver callbacks can queue the next step. Omission, static/dormant retention and numeric/rotation-lock validation match [PhysicsServer force methods](PhysicsServer.md). [ADR 0074](../decisions/physics-forces.md#adr-0074) and [PhysicsServerForceTests](../../tests/Electron2D.Tests/PhysicsServerForceTests.cs) define current behavior and allocation limits.

## Internal kinematic integration intervals

Fast kinematic travel may divide one fixed frame into several native world calls under [ADR 0075](../decisions/physics.md#adr-0075). Step, callback timing and contact impulse totals retain the complete outer frame, including all internal native intervals. [RigidFreezeModeTests](../../tests/Electron2D.Tests/RigidFreezeModeTests.cs) verifies full outer force duration and one integration callback.

## Selected field parameters

Typed PhysicsServer gravity/damping parameters feed the same reported TotalGravity/TotalLinearDamp/TotalAngularDamp and automatic integration policy for server/non-rigid bodies. CharacterBody.GetGravity reports the same scaled selected field. Custom integration still omits automatic effects while retaining selected-field reporting. [ADR 0076](../decisions/physics-mass.md#adr-0076) and [PhysicsBodyParameterTests](../../tests/Electron2D.Tests/PhysicsBodyParameterTests.cs) cover the shared policy.

Contact limits prepare bounded point storage before fixed stepping. Every touching pair is scanned directly, retaining the deepest points without copying raw manifolds; no early pair cap hides later candidates. Rigid monitoring prepares its bounded pair/change collections at configuration time. Solver array compaction keeps the removed reference in the unused slot rather than constructing a replacement; active slots remain distinct. PhysicsSandbox profiles check collision churn and debug contact reads after warmup; native allocations remain outside the managed counter.

Contact monitoring and direct-state values use the same solved-pair traversal. Snapshot collection may run on internal world workers; contact, integration and sync callbacks remain on the scene owner thread. The owner waits for every collector before any game callback can edit bodies or fixtures.

Static/kinematic `LinearVelocity`, `AngularVelocity` and point-velocity queries
include configured virtual surface motion. StaticBody/AnimatableBody velocity
writes project to their stored surface properties; a direct write replaces the
corresponding total velocity component immediately. Contact collider velocities
remain snapshots of the completed solve, including stationary surface motion.

Typed PhysicsServer state operations now share the view's velocity/sleep mutation
path. Explicit sleep clears dynamic velocity and suppresses the scene's automatic
sleep event; native transitions still emit normally. Raw kinematic Transform writes
queue the latest target after the initial pose, matching BodySetTransform; getter
reads remain the current native pose. Numeric and solver-lock guards reject before
changes. See [typed body state](PhysicsServer.md#body-state).


GetContactColliderObject<T>(int contactIndex), where T : ElectronObject, returns the live assigned object sampled with that contact. The existing nongeneric method retains physical scene-collider convenience. The sampled instance ID survives target disposal and rebind. See [object associations](../components/physics-object-bindings.md).

[Report-only contacts](../components/physics-report-only.md) now include kinematic/static and kinematic/kinematic pairs on CPU/GPU when either endpoint enables reporting. Impulses and positional response are zero; two static bodies remain ineligible. Caps, captured owners/indices, frozen-body scene events and checkpoint metadata share the existing contact contract.

## Caller-created extension views

[PhysicsDirectBodyStateExtension](PhysicsDirectBodyStateExtension.md) supplies the complete inherited state/force/contact/space family through guarded typed hooks. The constructor qualifies a borrowed scene/raw body by its current attachment generation, without acquiring a cached built-in view and independent of its disposal. Every operation validates owner, lifetime, failed-world and solver access before user code; finally releases nested hook context. Inputs and output fields are finite/rigid where required, and contact counts retain the authored cap.

Contact getters project one immutable [PhysicsBodyContact](PhysicsBodyContact.md) result with engine-sampled weak identity. Snapshot values/indices/IDs survive later collider release and edits; the observed-body attachment generation must still match the bound view. Scene/caller-owned body/world release, attachment transfer, hierarchy changes, active recursive steps and checkpoint capture/restore reject while a hook is borrowed. Built-in post-solver callbacks retain their existing semantics. Internal selected factories provide real scene/server callback views; the public registered factory family now executes; full custom numerical/geometry acceptance remains open.

## Factory contact publication

A factory-returned cached body extension now projects its contact count/values on the world owner after native capture and before scene/raw observer delivery. Complete validated snapshots commit atomically; cached getters retain immutable data and do not reinvoke contact hooks. Independent caller-created views keep ordinary guarded direct hook dispatch. Qualified publication permits immutable PhysicsBodyContact identity construction while ordinary solver queries/mutation stay blocked. Bad receivers retain their prior published state while other callbacks and committed ticks continue. [The executing contract and verification](../components/physics-body-extensions.md#selected-contact-publication) retain public registration/native/platform/performance limits.

[Registered numerical adapter](../components/physics-registered-adapter.md) now connects a public value/RID-only implementation to real scene/raw motion, fields/forces, joints, queries, contact/Area publication, private/portable history and terminal failure. Its independent circle/spring test solver owns its own state and does not wrap a built-in. Public registered factories and the typed family now execute in a separate consumer; arbitrary custom geometry and unexercised operation acceptance remain open.

Body force, mass, integration, motion, sleep, CCD and callback authoring now
reject before mutation during selected numerical hooks, including initial
attachment. Direct-body setters/forces use the same qualified numerical guard.
Guarded authored reads, ordinary post-solver callbacks and caller-created
body-state forwarding remain available; hierarchy/resource-lifetime guards still
cover every extension scope. See [body authoring](../components/physics-registered-adapter.md#borrowed-body-authoring).


[Contact reporting depth](../components/physics-registered-adapter.md#contact-reporting-depth) filters positive thresholds before deepest-point slot selection. CPU candidates use converted scene-unit depth, GPU candidates retain scene units. Zero disables this filter; the next nonzero publication applies edits without changing the completed snapshot or physical response.


[Registered factory-selected views](../components/physics-registered-adapter.md#factory-selected-direct-views) now execute through a separate public-only consumer: cache identity, an initially empty contact snapshot, synchronized immutable publication before callbacks, result/lifetime validation, recreation and zero warm managed allocations. Independent caller-created body views keep direct hook dispatch; wider numerical-extension acceptance stays open.

Complete PhysicsBodyStateTests and PhysicsServerForceTests now execute on explicitly selected public CPU/GPU worlds, alongside the common PhysicsContactImpulseTests and report-only role suite. They retain state/field/force/center values, post-solver callback edits/errors/removal/reentry, borrowed-view lifetime, force omission and pending-input rules. Real scene/raw dynamic and kinematic fixtures expose six uncapped contacts and verify deepest 2/1/0 caps and the corresponding monitor objects without private list-order assumptions; static/static pairs remain ineligible. Momentum checks retain their .08/.1 scene-impulse bounds, equal/opposite and rotating-normal/transient/sleep assertions. Warm gates require 0/0 owner/all-thread bytes for contact/callback/force frames, zero-cap frames and live pose reads. CPU native scheduling/depth-injection checks remain separate diagnostics; legacy staged GPU controls do not establish independent GPU acceptance. Native/platform, visual and final network/performance gates remain open.

[Mass roles](../components/physics-registered-adapter.md#mass-profiles-across-roles)
and [portable role correction](../components/physics-snapshots.md#portable-body-roles)
now execute explicit CPU/GPU/registered serial and separate numerical profiles.
Scene/raw automatic/custom centers, inverse dynamics, point velocity and exact
slot queries survive role changes; CPU/GPU round trips preserve pending force/
torque consumption and atomic authoring rejection. Original warm brackets retain
0/0 managed bytes. GPU uses llvmpipe; hardware and broader acceptance stay open.

[Portable contact and motion history](../components/physics-snapshots.md#portable-contact-and-motion-history)
now passes CPU/CPU, CPU/GPU and CPU/registered serial/separate public-only profiles.
Imported points/impulses and character slides retain destination peer/slot identity;
raw Area/contact history stays silent during correction and avoids repeated enters.
Malformed indices/platforms preserve state and events, and real movement retains
platform carry. Warm contact cycles require 0/0 bytes; cold motion validation has
no allocation claim. Hardware and wider geometry/joint/network gates remain open.

[Capture topology](../components/physics-snapshots.md#capture-after-topology-edits)
and [same-count authoring validation](../components/physics-snapshots.md#same-count-authoring-validation)
now pass CPU/CPU, CPU/GPU and CPU/registered serial/separate public-only profiles.
Each capture profile retains 24 removal/reindex/readdition cases; each geometry
profile retains 14 incompatible/restored cases across both directions. Rejection
preserves complete packets, sampled values and events; real refresh/continuation
recovers. These cold structural checks have no allocation claim. Hardware and
broader geometry/joint/network acceptance remain open.


[Joint and contact support](../components/physics-joints.md#joint-and-contact-support-2026-10-14) verifies spring-loaded pin/groove constraints, real floor friction, contact identity/normal/surface-velocity/impulse, monitored checkpoint continuation and post-clear motion on CPU/GPU. Warm full frames allocate no managed bytes; broader numerical/native/foreign/network/performance acceptance remains open and declaration states are unchanged.
