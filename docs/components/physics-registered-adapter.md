# Registered numerical solver adapter

Last updated: 2026-10-12

## Scope and ownership

The internal [RegisteredPhysicsWorldBackend](../classes/RegisteredPhysicsWorldBackend.md) connects the complete [PhysicsServerBackend](../classes/PhysicsServerBackend.md) to shared scene/raw identity, authoring, views, callbacks, monitors and history. The public contract has numerical [collider](../classes/PhysicsBackendCollider.md) and [joint](../classes/PhysicsBackendJoint.md) attachments and typed observation records. It carries no PhysicsSpace, PhysicsBodyRuntime, PhysicsColliderBackend or scene storage through implementation operation parameters.

## Runtime flow

A fresh claimed implementation initializes before activation. Attachment creation receives engine-assigned RIDs. Shared preparation supplies complete authored integration/force/Area policy and logical shape slots. The implementation solves its own state; common completion publishes scene/raw results, contact/Area observations, statistics and callbacks. Ordinary direct views dispatch through the same selected attachment. Motion output uses the reusable public result construction family and existing completed-output validation.

Each user implementation call borrows owner-thread lifetime. Factories cannot reuse one world, collider, joint or history object. Captured numerical output validates before consumption. Failed contact publication retains the receiver snapshot and aggregates errors after later deliveries. Started solve/synchronization failure retires the world without hidden CPU/GPU replay. Cleanup attempts Finish after all attachments, including common-space retirement and partial creation failures.

Same-backend private history stays with the implementation. Portable state transfers engine-authored bounded values; local one-way RIDs map to network IDs only in the common adapter. Explicit and joint exceptions are supplied as current-world typed RID pairs. Observer metadata and identities remain engine sampled.

## Verification boundary

The separate [public-only consumer](../../tests/PhysicsBackendExtension.Consumer/PhysicsBackendExtension.Consumer.csproj) implements a bounded circle/spring model, with actual integration, queries, overlap and contact output and spring response. It is not a production physics option. The fixture runs 64 warmup and 64 measured complete PhysicsFrame cycles, including raw state callbacks, and measures zero owner/all-thread managed bytes. Scene/raw mass2/4 and constant forces12/24 both yield .1 scene units/s after the first 1/60-second interval within .000001 float tolerance; retained checkpoint and portable poses compare exactly. Contact/Area events, real circle queries, bounded spring response, receiver-failure continuation, reuse rejection and terminal solve failure cover their named paths. The fixture proves these exercised adapter paths. All currently applicable built-in shape/body/Area/joint behavior still requires its existing shared CPU/GPU checks.

PhysicsServerManager.RegisterServer and SetDefaultServer now execute through a retained static facade. Factories return fresh PhysicsServerBackend objects, typically derived from PhysicsServerExtension. Duplicate/reserved names, null/disposed/reused implementations and recursive creation reject. DEFAULT uses strictly greater priorities; World(string) captures its selection before lazy startup, while SpaceCreate(string) initializes an owned inactive space immediately. Resource aliases retain one runtime. Requested and actual names accompany Backend.Extension; explicit CPU/GPU and parameterless CPU remain independent of the default. Factories execute outside the lock, including registration from another thread. World release calls Finish and disposes the implementation; direct caller disposal of a bound implementation rejects.

The public immutable configuration/result family includes body integration/forces, Area fields, joints, sleep/contact policy, logical shape slots, resolved fields, mass properties, overlap/shape contacts, statistics and one-way episodes. Collider and joint objects are fresh per attachment; checkpoint objects provide private history hooks. Query exclusion spans are passed explicitly and borrowed only during the synchronous call. The consumer compiles without internal access and executes both SceneTree frames and explicitly owned SpaceStep.

Every extension operation's acceptance remains open under [ADR 0103](../decisions/physics-extensions.md#adr-0103). Backend rows stay Partial or Blocked where their named path is absent; the bounded test solver does not certify the full production contract. Native allocator, cross-platform, real-rendered output and final performance acceptance are unverified here.

Initialization/Finish/disposal failure injection retains all three errors after retirement. Scene diagnostic capture executes the selected ReadDebugContacts hook after synchronization; invalid count or nonfinite points reject before diagnostic publication and fail the started interval.

Status and capacity hooks also borrow the owner context; recursive world inspection from the status hook rejects and a later normal query recovers. The separate consumer checks this guard.

[Custom geometry](physics-custom-geometry.md) now executes with an external annulus/circle implementation: canonical data, exact mass, standalone/selected queries, body contacts, Area observations, server mutation and private/portable replay. Generic custom format file codecs/editor authoring, unexercised extension operations, native/foreign acceptance and final performance remain open.

Portable one-way capture is now bounded by the current declared count, independently of retained array capacity. Shared validation rejects self/unbound/disabled/invalid-piece/duplicate episodes before encoding. The public-only consumer restores a rejected episode into a separately created explicit world with different local RIDs and verifies pass-through at 60 units/second versus a real impulse to 30 for the accepted state. Repeated restore reinstates the decision; 64 warmed capture/apply cycles allocate zero managed bytes.


## Initial collider metadata

The adapter publishes retained object/canvas IDs and collision priority immediately
after successful numerical Attach and before logical shapes are prepared. Earlier
code sent only live edits, so a registered solver's first query could miss a canvas
association authored while detached. Reattachment to another selected world now
receives current values through the same hooks. Body and Area identity share this
protocol; scene CanvasLayer entry/exit retains its ordinary live updates.

`BackendChecks.IdentityChecks` in the public-only consumer detects the prior bug
before any physics tick. It exercises body/Area detached/live/clear assignment,
weak sampled object identity, exact 64-bit point membership, two-world transfer,
scene body/Area hooks, failed initial identity/priority hooks with retry, owner and
borrow guards, and zero owner/all-thread managed allocation over 128 edited queries
after 64 warmup iterations. Allocation counters are initialized before warmup;
the first uninitialized-counter measurement reported 0/1,008 owner/all-thread
bytes, while initialized counters followed by warmup produced 0/0 with the same
queries. Zero-byte assertions remain exact. Queries use the external circle model,
not a built-in solver delegate.

Coverage pairs all ten reference object/canvas/priority operations with their typed
attachment/facade projections under ADR 0103. These rows remain Partial: identity
publication is verified, while complete extension-operation acceptance and general
third-party numerical priority response remain open. No runtime declaration or
public singleton instance accessor is added. Native allocator, other platforms,
final performance and the separately recorded sporadic GPU window failure remain
unverified here.


The initial-hook reentry check also exposed missing guards: an identity hook could
recursively assign canvas metadata, and a not-yet-committed raw attachment had a
world reference before its public SpaceRID was assigned. Shared collider metadata
writes now reject a borrowed extension scope before assignment; raw lifetime
preflight uses the live world reference to reject release/transfer during that
scope. Both body and Area initial hooks exercise these paths. Typed immutable
result construction and supported nested query scratch keep their existing paths.

## Borrowed shape authoring

Body/Area slot add, replace, transform, disable, remove and clear, body one-way
settings, collision layers/masks, shared server-shape data replacement and release
reject inside a registered hook before changing common authoring or numerical
state. Raw colliders use their live world reference even during initial attachment
before the public SpaceRID is committed. Qualified immutable result construction
and guarded logical getters retain their existing read access. This does not
claim a blanket ban on every engine operation from a hook.

The public-only ShapeMutationChecks exercises initial identity, live shape and
filter hooks, scene body/Area entry, owner-thread rejection and actual query
membership after ordinary replacement, disable, removal, clear and shared data
edits. It samples delivered transform/one-way values; the bounded circle solver
does not certify arbitrary transform or one-way numerical response. After 64
warmups, 128 layer-edit/point-query cycles require zero owner/all-thread managed
bytes. The old runtime failed the initial BodyClearShapes rejection before the
shared guard. The 27 corresponding extension shape/filter setter/getter rows
remain Partial; generic geometry and complete solver acceptance remain open.

## Borrowed body authoring

Body force/impulse, persistent force/torque, mass/center/inertia, material and
integration parameters, pose/velocities, sleep, CCD, contact capacity and callback
writes reject from a selected numerical hook before authoring changes. The raw
mode setter is guarded too. Direct-body setters and force/integration operations
cannot bypass this boundary. World numerical borrowing has a separate depth from
caller-created direct-state extension borrowing, so legitimate state-view
forwarding keeps working. Read paths and transform batches do not request writer
access. Existing owner, solve/failure and attachment checks remain in place.

The public-only BodyMutationChecks reproduces initial raw mutation and the direct
view sleep bypass, then verifies rejection on both scene/raw bodies, retained
authoring, real impulses, ordinary integration/sync callbacks and 64 full warmed
edited frames at zero owner/all-thread managed bytes. Exact impulse deltas use
0.001 velocity tolerance; the test accounts for the world's damping before the
impulse. A blanket all-extension write guard failed the separately rebuilt
PhysicsBodyExtension.Consumer forwarding test and was replaced by the numerical
scope guard. Generic solver response and the complete extension contract remain
open; these checks do not certify all parameter or mode semantics.

## Borrowed world policies

The three sleep values, four contact values, solver iterations, default constraint
bias and local activation share numerical borrowing guards before assignment or
an equal-value early return. This prevents a selected attachment or policy hook
from recursively changing the common profile or invoking another selected policy
hook. Reads retain owner/phase guards, and ordinary post-solver callbacks can edit
the next interval. Process-wide SetActive retains its independent atomic contract.

The public-only WorldPolicyChecks reproduces the initial attachment bypass, then
checks initial/live attachment and nested policy/activation hooks, every typed
getter/setter, retained values, no nested selected calls, off-owner rejection and
ordinary raw state-sync edits. With real scene and raw bodies, 64 warmup and 128
measured edited full frames retain zero owner/all-thread managed bytes. The
bounded consumer verifies dispatch and guards; it does not certify arbitrary
registered numerical sleep/contact response, native allocations or other platforms.

## Registered Area fields

The ten typed Area fields and the space-default shortcut retain one common
profile. Writes reject during a numerical hook before authoring changes,
including initial raw attachment. Guarded getters remain available. Default-field
query/status hooks run outside the RID registry lock. Ordinary post-step Area
callbacks can change the next interval's fields without resetting overlap
histories; monitor-configuration guards are not reused for these field writes.

The separate public-only consumer now implements bounded circle Area reduction
from the selected SetAreaFields/PrepareWorld inputs. It checks all five override
modes independently for gravity and both damping channels, descending priority,
exact ten-component delivery, point unit distance, directional magnitude, masks
and participation without monitoring/monitorability. Real point-field motion,
ordinary callback edits, borrowed rejection, off-owner guards and 64 warmed edited
full frames at zero owner/all-thread managed bytes are exercised. Zero-duration
frames attach objects but do not simulate or publish overlap events, so numerical
checks use nonzero intervals. Field tolerance is 0.00001 for the bounded fixtures;
the point-motion velocity interval 0.49..0.51 includes the changing field distance.

The two extension parameter rows remain Partial. Arbitrary geometry, full
numerical/extension acceptance and final platform/performance gates remain open.

## Parameter reads and preparation

Configured mass, explicit inertia/custom center, friction, bounce, gravity scale
and damping parameters project guarded common authoring without invoking
PrepareContactPolicy, RebuildShapes or other numerical preparation hooks.
Automatic inertia/center getters retain current-geometry preparation outside
numerical borrowing. They reject inside a numerical hook before preparation,
while configured values remain readable. Setter preparation is unchanged.

The public-only consumer reads the complete ten-parameter family during actual
initial scene/raw attachment and a live velocity hook and asserts no numerical
preparation. Automatic inertia for a 2 kg circle changes from 9 to 36 when radius
changes from 3 to 6, within 0.0001. Explicit values then remain unchanged. After
64 warmups, 128 paired scene/raw read cycles require zero owner/all-thread managed
bytes. The original runtime triggered numerical preparation from an initial
identity hook; this is a behavior regression, not an API-count change. General
numerical/extension and final performance acceptance remain open.

Material revision polling is retained in the authoring read path: friction/bounce
getters clear superseded overrides and mark later shape preparation dirty even
when an earlier resource subscriber prevented the scene callback. This performs
no numerical hook. The public-only consumer injects failures for both channels;
the general material regression remains part of acceptance.

## Exact scene motion roles

An unfrozen RigidBody with LockRotation supplies RigidLinear to the selected collider implementation at initial attachment and on live lock edits. Unlock supplies Rigid. The angular constraint travels with that exact mode; classification as dynamic still includes both values. Freeze overrides it with Static or Kinematic, and MakeStatic temporarily selects Static while retaining the configured lock for re-enable. Scene/raw locked bodies therefore supply the same numerical role.

The public-only MotionRoles consumer checks initial/live delivery, retained linear velocity, both freeze roles, lock edits during freeze and disable, restoration, owner/numerical guards, simulated pose publication, checkpoint restoration and 64 warmed full frames with zero owner/all-thread managed bytes. Built-in CPU/GPU motion suites retain their physical response and storage gates.

BodySetMode and BodyGetMode now accept all live scene/raw body RIDs. Direct scene assignments retain an internal motion profile without rewriting the scene descriptors. See the common server-mode family below; full numerical extension and performance acceptance remain open.

## Common server mode family

All four modes now apply to static, animatable, character, rigid and physical-bone scene RIDs as well as raw bodies. Direct assignments retain mode, motion and kinematic targets across reentry. A scene Freeze/FreezeMode/LockRotation or bone-role edit restores configured scene control; MakeStatic temporarily takes precedence. Scene descriptors and PackedScene identity stay unchanged. Common state/force operations use the effective mode and selected implementation, and direct dynamic poses are published back to the scene.

The public-only consumer exercises detached/initial authoring, all four live modes, kilograms/inverse mass, impulses, linear/angular state, static surface velocity, kinematic targets, sleep, reentry, owner/numerical rejection, checkpoints and portable application. CPU/GPU and independent implementation runs share the same checks. Snapshot format version 2 adds 48 encoded bytes of direct-profile motion/target state per scene body and includes the configured direct mode in the schema; version 1 input rejects before mutation. Performance and complete numerical-extension acceptance remain open.

## Common state family

Transform, linear/angular velocity, sleeping and can-sleep have typed common getter/setter pairs. Reads expose selected state without numerical preparation, including initial identity callbacks. Writes reject while a numerical implementation is borrowed; inherited scene pose helpers and RigidBody.CanSleep share that restriction. Ordinary force and state callbacks retain write access. Disabling sleep wakes detached/live direct dynamic profiles for all five scene body types as well as raw bodies, and reentry preserves the request.

The independent public-only consumer checks all five pairs, non-preparing reads, initial/live borrowing rejection and all scene siblings. Existing body checks exercise actual motion, direct-view writes and zero owner/all-thread allocation over 64 warm full frames. The two extension state rows remain Partial until the complete numerical-extension acceptance gate passes.

## Integration and contact publication policy

Common scene/raw callback registration, omission and contact caps are executable typed operations. Initial/live numerical calls cannot change callbacks, omission, caps or scene monitoring; inherited scene setters reject before mutation. Ordinary game callbacks retain writes. Force callbacks precede the RigidBody hook and sync observer; exceptions retain committed edits and do not skip the observer. Clearing delegates stops invocation. Generic userdata adapts once at registration.

The independent public-only `--integration` consumer also runs with `--cpu` and `--gpu`. It verifies omitted pending/persistent forces, impulse and actual collision response, capped contact identities and clearing caps, callback order/failure and zero owner/all-thread allocation across 64 full measured frames. Isolated force/impulse response uses a tolerance of 0.001 scene velocity units; identity/count/order checks are exact. Six extension rows remain Partial until full numerical-extension acceptance.

## Common space bindings

Body/AreaGetSpace and Body/AreaSetSpace now share scene/raw identity, current
attachment and selected lifecycle dispatch. All five scene body roles and Area
can be attached while detached from a tree, transferred between worlds, detached
explicitly and simulated in caller-owned spaces. The render-world descriptor and
hierarchy retain their values; scene entry and viewport world replacement reclaim
the scene-selected binding. Neither assignment nor importing a node creates a
new world driver. Owned-space release detaches nodes and expires cached body views.

Source geometry preflight rejects authoring/disposal/reparenting before mutation.
Node authoring and hierarchy guards inspect actual attachments even outside the
SceneTree world list. Engine pose publication remains permitted during the ordinary
step phase; a trial that applied the release-phase guard there was rejected.

Empty scans now use conservative process-wide attachment and scene-preflight
presence. Geometry hooks remain guarded before the first attachment; detach
cleanup and failed preflight balance their presence in finally. Any scene/raw
attachment retains the original checks. [Ownership regression
checks](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.GuardPresence.cs)
exercise first preflight reentry, failed geometry, detach and world retirement on
CPU/GPU. This reduces [cold scene lifecycle
cost](physics-scene-performance.md#empty-hierarchy-guard-scans), not solver work.

Initial or later target-addition failure retires any partial attachment before
restoring the source. Restoration failure attempts retirement and preserves all
errors in an AggregateException. Departure callbacks can report a committed
detachment; callers inspect the actual space getter after such an error.

The separate public-only SceneSpaceChecks executes queries and motion on CPU,
GPU and the registered implementation, including all body roles, Area/raw
siblings, view invalidation, scene reentry/world changes, owned-space free,
wrong-kind/owner guards and 64 warmed measured full frames at 0/0 owner/all-thread
managed bytes. Registered checks inject initial and post-attachment capacity
failures, double restoration failure, detached/imported numerical mutation and
procedural source-preflight reentry before a real transfer to CPU. Native allocator,
other platforms, rendered/FPS and complete numerical-extension acceptance remain open.

## Force, impulse and axis response

The common facade owns transient/persistent totals, finite candidate validation and
positioned moments about the current rotated center of mass. Selected
`PrepareDefaultForces`, `ApplyImpulse` and velocity hooks execute their numerical
response. A positioned constant force captures its moment once; subsequent body
rotation does not recalculate that stored torque. Impulses use current mass and
unlocked inertia. Pending forces survive detach and static participation, then
apply once at the next eligible dynamic tick. Invalid input or moment overflow
rejects before observable motion/constants change. Axis velocity preserves the
perpendicular component; a zero axis preserves motion.

The separate public-only [force consumer](../../tests/PhysicsBackendExtension.Consumer/BackendChecks.Forces.cs)
(`--forces`, optionally `--cpu` or `--gpu`) exercises all thirteen force operations
and axis velocity on scene and raw bodies, using server, scene and direct views.
[Lifetime checks](../../tests/PhysicsBackendExtension.Consumer/BackendChecks.ForcesLifetime.cs)
cover detach/reentry, static/dynamic transitions, expired views and invalid inputs.
Linear and angular response tolerances are 0.0001 scene units/s and radians/s:
they allow float integration and private unit conversions without accepting a
missing force, mass factor or positioned moment. Constant torque tolerance is
0.00001; vector totals, identities and rejected-write retention compare exactly.
CPU, resident GPU and the independent registered circle solver each pass 64
warmup and 128 measured edited full PhysicsFrame cycles at 0/0 owner/all-thread
managed bytes. Collection occurs after warmup, so the first following frame is
included. GPU CCD validation uses the three explicit valid modes rather than
reconstructing enum metadata after collection; an isolated old validation probe
reproduced the 248-byte allocation found in that first frame. These fourteen
extension rows remain Partial pending complete numerical-extension acceptance.

## Collision-exception authoring and response

Body add/remove owns one insertion-ordered explicit RID list; every valid call
requests dynamic wakeup, including duplicate additions and absent removals.
Either endpoint's entry suppresses solid response and body motion. Ordinary
space queries and Area sensing remain independent. Active collision-disabled
joints contribute separately to both endpoints; snapshots deduplicate explicit
entries before joint targets. Clearing one source preserves the other sources.
Empty list buffers remain attached to their live owner and retire with that body.
Terminal-world cleanup removes common contributions without requesting device
state or wake work from the failed implementation.
CPU refreshes fixture and cached-contact pre-solve flags without rebuilding
geometry. GPU keeps its resident exception edges and selected wake policy.

Numerical hooks cannot alter these lists through scene/server body methods or
joint collision-policy setters, including equal-value writes. The protected
`PhysicsServerBackend.GetCollisionExceptions(RID)` exposes retained authored
input in a bound owner-thread world, including detached/inert entries. It rejects
foreign attached owners, wrong/stale IDs, unbound/retired implementations and
ordinary reads after world failure. Reads during its own solve hook copy common
input without querying numerical state. Returned arrays intentionally allocate;
steady numerical work uses the supplied pair spans or retained solver storage.

The public-only [exception fixture](../../tests/PhysicsBackendExtension.Consumer/BackendChecks.Exceptions.cs)
(`--exceptions`, with optional `--cpu` or `--gpu`) verifies all five scene body
siblings and raw owners, initial/live borrowed-write rejection, actual
circle response/contact identity, immediate forward/reverse motion filtering,
Area sensing and live/duplicate/absent wakeup. It also verifies pin/groove/spring
and scene-joint contribution union/clear and guarded collision-policy changes.
[Scoped input reads](../../tests/PhysicsBackendExtension.Consumer/BackendChecks.ExceptionReads.cs)
cover ordered snapshots, caller-owned copies, solve-time reads, detached state,
foreign/off-thread/wrong/stale owners and failure/retirement. The resolved circle
stop interval is 23 to 25.1 scene units around the ideal 24, covering the configured
penetration/skin allowance; normal velocity is below 0.01 units/s. IDs, ordering,
source retention and wake state compare exactly. Body and joint edit fixtures each
run 64 warmed/64 measured cycles with two complete PhysicsFrame calls per cycle
and collection between brackets: 128 measured frames at 0/0 owner/all-thread B
on CPU/GPU/registered paths. Five extension rows remain Partial pending complete
numerical-extension acceptance. Snapshot-getter allocations are outside that
physical-frame bracket by their documented caller-owned contract.


## Joint parameter authoring

Common typed getters/setters retain Bias, MaxBias, MaxForce, pin positional softness,
limits, motor flags/speed/torque and spring rest/stiffness/damping. The selected joint
receives immutable complete configuration plus concrete live hooks. Every scalar
setter checks scene, joint and endpoint numerical borrowing before validation or
unchanged-value returns. Scene path and local groove/spring geometry check the same
boundary before field mutation, including pending initial attachment. Ordinary game
callbacks keep next-interval writes. Engine pre-solve automatic spring rest-length
preparation remains valid.

`BackendChecks.JointParameterChecks` exercises all three raw and scene roles on
explicit CPU, GPU and the independent registered fixture: exact typed/configuration
values, initial/live hook rejection, retained values after invalid candidates,
owner checks, endpoint detach/reentry and ordinary state callbacks. A two-body
mass-2 spring produces opposite velocities .1, .15, .1 and .05 scene units/s as
stiffness, full force cap, damping and literal rest length change. The .001
scene-unit/s tolerance bounds substep displacement differences for this short
low-speed test; it is not a general solver tolerance. After 64 warmup frames,
64 edited complete PhysicsFrame cycles allocate 0 owner/all-thread managed bytes
on each backend, including the first frame after collection.

The eight extension parameter/flag selector operations map to their complete typed
facade family under ADR 0087/0103 and remain Partial. Registered pin/groove numerical
response and complete numerical-extension acceptance remain open; built-in joint
response uses the existing shared CPU/GPU contract tests. Native allocations, other
platforms, networking and final window/performance acceptance are unverified here.


## Joint connection lifetime

RID creation and type retain common authoring. Selected CreateJoint/Attach/Detach
own each fresh numerical attachment. Clear and replacement check scene/current
joint/endpoints before any mutation, including pending initial attachment and
unchanged clear. Public make checks both incoming body owners before preparing
either; an Empty joint cannot bypass borrowing through a detached first endpoint.
Engine pre-solve scene preparation remains valid.

`BackendChecks.JointLifecycleChecks` covers all three raw and scene roles on CPU,
GPU and the independent fixture. Factory/Attach/live policy/Detach hooks reject
recursive clear/make/free, including unconfigured scene joints. Invalid kinds,
nonfinite anchors, duplicate endpoints and mismatched scene roles preserve the old
profile and attachment. Endpoint exit/reentry preserves sampled frames and values;
endpoint free clears the role while retaining the joint RID. Ordinary state
callbacks may clear/remake the next interval's connection.

Independent factory throws, null results and claimed-object reuse reject. Attach
failure attempts Detach; both errors remain if cleanup also throws. The matching
numerical attachment retires even on detach error. Validated common role may remain
pending after an attachment error, or until a failed clear is retried: explicit
make/clear retry completes the operation. These outside-step authoring errors
leave the world usable; started solve failures keep the existing terminal policy.

The test also exercises real capped spring response with the existing .001
scene-unit/s short-run tolerance. After 64 warmup frames, 64 complete frames with
six active joint records allocate 0 owner/all-thread managed bytes on each backend.
Cold creation/replacement intentionally creates fresh numerical objects and is
outside this allocation bracket. The six extension creation/type/clear/make rows
remain Partial: registered pin/groove numerical response and complete extension
acceptance are still open. Native allocations, other platforms, final networking
and real-window/performance acceptance are unverified here.


## Resource retirement

Common FreeRID owns caller-created Shape/Body/Area/Joint/Space lifetime; selected
objects own numerical cleanup. Kind/ownership/thread/solver/borrowing/geometry
preflight precedes any change. After retirement begins, all dependent joint,
motion capture, collider detach, shared shape-user and world Finish/disposal
actions are attempted. A single error keeps type/stack; several aggregate without
masking earlier failures. Registry entries retire even when cleanup reports an
error. Source body/Area resources retain their identities after a space is freed.

Earlier code removed body membership before a dependent joint error could abort
numerical detach. A retry then returned at the missing membership entry and dropped
the RID while the solver still owned a ghost body. Departure observers could also
rebind a body before final registry removal. Cleanup now continues through all
remaining stages, and a releasing collider rejects rebind/recursive free. Shared
shape removal marks dirty preparation before a failing numerical hook; the next
query/step retries retained authoring. Scene body/Area/joint departure resets
attachment/revision records in finally paths for fresh reentry.

The independent public-only `--resources` checks all five caller-owned RID kinds,
all five scene body siblings, scene Area/joints, borrowed identity rejection,
initial/live/release hooks, off-owner access, generation-expired body views,
stale direct-space queries, shared shape-user clearing, failed preparation retry
and explicit reentry. Its body-free injection retains exactly five errors: three
joints, motion capture and collider detach. World-free injection retains thirteen:
three joints, eight colliders, Finish and implementation disposal. Both real sensor
departure callbacks execute despite observer errors; rebind rejects and departures
are not repeated by the next interval. CPU/GPU share these named public checks.
After 64 warmup steps, 64 active steps after shape retirement/recovery allocate
0 owner/all-thread managed bytes, with six bodies and three constraints. Cold
resource creation/retirement intentionally allocates outside that bracket.

The extension FreeRID row remains Partial until complete numerical-extension
acceptance. Native allocation, other platforms, final network scenarios and
real-window/performance acceptance are unverified here.
