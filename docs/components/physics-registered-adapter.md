# Registered numerical solver adapter

Last updated: 2026-10-17

## Body view acquisition after mode failure

Body view acquisition now finishes pending direct-mode preparation before reading resolved fields, selecting a cached view or invoking its selected factory. The same boundary serves caller-created body-state extensions. A repeated preparation error creates no view, publishes no fields and does not advance the tick. Successful recovery preserves cached identity or creates one generation-qualified view; stale attachments remain rejected.

The public-only --mode-transactions fixture now includes cached acquisition and caller-created construction among six preparation routes: 1440 partial-failure/retry cases across six body roles, four modes and five failure stages. Twenty-four cold-cache cases require preparation to finish before a selected view factory runs; 220 retirement cases and six scene-role takeovers remain covered. All profiles retain actual logical-slot queries, inverse mass, velocity/wake policy and 128 warmed changed-mode frames at exact zero owner/all-thread managed bytes. The existing --factory-views consumer independently covers typed selected views, immutable contact publication, callbacks, all query families, null/foreign/disposed/recursive factories, retry and attachment retirement. All 20 focused/related profiles pass on managed/NativeAOT CPU/software-Vulkan/registered paths. Portable PDB audits match the exact 2264 runtime, 158 consumer and 747 root-test sources with no missing/mismatched inputs or DEBUG. Evidence: bin/physics-direct-state-preparation/2026-10-17/validation.json. Complete numerical/custom/network/native/foreign and hardware/performance acceptance remains open.

## Failed body-mode publication

Direct BodySetMode commits common mode and its velocity policy before calling the selected owner. Incomplete mode, rotation, linear/angular or surface velocity, shape and mass publication stays pending. Numerical server/direct-view access, scene direct-mode velocity/sleep access, live space queries and nonzero steps retry the whole transition. Recursive preparation rejects. Original errors propagate, repeated preparation failure leaves the tick unchanged, and no failed numerical Solve is replayed. Detachment skips incomplete numerical motion capture and retires only that attachment's preparation debt. Reattachment uses retained mode/motion; old direct views remain invalid. Scene role takeover prepares the previous direct profile before changing configured descriptors.

The public-only --mode-transactions fixture exercises six body roles and all 16 ordinary role transitions per body. Registered serial/separate profiles cover 960 partial-failure/retry cases at mode, rotation, velocity, surface and shape stages through four preparation routes, 220 attachment/scene retirement cases and six configured role takeovers. It checks original/repeated errors, tick boundaries, velocity/wake policy, inverse mass, cached-view identity, logical disabled gaps and stale-view rejection. CPU/private software Vulkan/registered profiles warm 128 and measure 128 changed-mode full frames with exact zero owner/all-thread managed bytes after an explicit full collection. Reflection-based enum validation in the resident mode/integration path previously recreated 488 bytes of enum metadata in the first measured transition; explicit valid-value patterns retain validation without that allocation. Native/foreign allocation, complete numerical/network and hardware/window/performance gates remain open. Managed and NativeAOT profiles both execute; all 42 final focused/related profiles pass after the concurrent texture integration. Portable-PDB audits match 2264 runtime, 158 consumer and 747 root-test sources, including the frozen final-main payload, without missing/mismatched files or DEBUG. Evidence: bin/physics-mode-transactions/2026-10-17/validation.json. The frozen full software/offscreen regression for 5c1a0f3afeeeca8c0dbda905dba6a95310b5e733 passed with exit 0 in 1395.157364 seconds. Frozen test host SHA-256: 72863fb9a0046662c318080aa5d6277a47b71dccafa417201cb52bf24940eaa4. Across 41027 map samples, private lavapipe was observed and no hardware driver was observed. Evidence: bin/physics-mode-transactions/2026-10-17/full-runs.json and bin/physics-direct-state-preparation/2026-10-17/full-baseline-completion.json. This verifies the preceding mode-recovery revision; the later direct-view acquisition repair has its separate 20-profile proof, not a claimed new full-suite pass.

## Failed collider metadata publication

Object/canvas/priority setters retain committed common authoring when the selected hook fails. Reverse node membership commits before publication, so the new target remains protected during failing hooks and the former target loses that binding. Per-attachment dirty identity/priority flags share the existing filter-repair preflight. A live query or nonzero step retries complete current metadata before numerical use, without rebuilding shapes or replaying a failed Solve. Failed preparation preserves its original error and leaves the tick unchanged. Getters remain pure authored reads; detach clears retired attachment debt.

Target-world ownership and the external-node reverse binding now precede selected collider creation as well as Attach/identity/priority callbacks. Failed factory/attachment cleanup removes reverse membership and releases partial implementation state, allowing target disposal and later attachment retry. The public-only --metadata-transactions fixture covers 28 ordinary scene/API operations, 112 injected live failure cases with both repair routes, six initial failure cases and 128 edited full frames with actual motion/queries at zero owner/all-thread managed bytes. CPU/software-Vulkan/registered owner/separate managed and NativeAOT evidence is retained under bin/physics-metadata-transactions/2026-10-17. Complete numerical/custom/native/foreign/network and hardware/performance acceptance remains open.

## Prepared collider metadata reads

Body/Area GetSpace, GetCollisionLayer, GetCollisionMask, GetObjectInstanceID and GetCanvasInstanceID, plus BodyGetCollisionPriority, read common prepared inputs in selected numerical and status hooks. Owner/separate numerical lanes use the same authored values. These getters do not enter live solver state or transfer GPU data. Other threads and failed worlds remain rejected; metadata writes, world transfers and live queries retain their existing guards.

The public-only --collider-inputs fixture covers all six scene collider roles and raw Body/Area, detached defaults, all 32 filter bits and 64 canvas bits, fractional body priority, weak object disposal with retained numeric identity, and transfers with actual steps through two SceneTree-owned worlds. Repeated filter/canvas/priority edits, actual motion and point-query identity/filter checks warm 128 and measure 128 full frames at strict zero owner/all-thread managed bytes. CPU, private software Vulkan and registered implementations exercise both numerical policies; managed and NativeAOT evidence stays under bin/physics-collider-inputs/2026-10-17. Complete mutations, numerical/network, native/foreign and hardware/performance gates remain open.

## Prepared logical shape reads

The Body/Area GetShapeCount, GetShape and GetShapeTransform family reads the engine-owned logical slots. Registered numerical hooks can inspect those prepared inputs on their owner or exact borrowed solver thread; no solver query, GPU transfer or second geometry store is involved. Pending/disabled slots retain count, RID and local pose; a disposed borrowed resource retains its slot and pose with an empty RID. Owned shape release removes and reindexes slots. Ordinary reads keep owner/failure guards, while queries, mutation and unrelated threads remain guarded.

The public-only --shape-inputs fixture covers all six scene collider roles and raw Body/Area, status-hook reads, both numerical thread policies, retirement/reindexing, invalid indices and all six getter guards after failure. Managed and NativeAOT CPU, private software Vulkan and registered profiles each warm 128 and measure 128 actual motion/query/full frames at zero owner/all-thread managed bytes. Native/foreign, complete numerical/network and target hardware/performance gates remain open. Evidence: bin/physics-shape-inputs/2026-10-17.

## Raw shape initialization

The eight built-in PhysicsServer shape factories return unconfigured RIDs. ShapeGetType works immediately. ShapeGetData and using that RID or a pending borrowed view as a collision-query source throw InvalidOperationException until ShapeSetData succeeds. Ordinary Shape constructors and CustomShapeCreate still supply initialized data.

Body/Area add operations accept pending shapes and keep their logical indices, transforms and disabled policy. Those slots supply no geometry, mass contribution, sensors or contacts until configuration. Disposed borrowed resources also produce inactive numerical descriptors while retaining logical slots and empty RID getters, including after disable/re-enable. Replacement operations require configured data. Selected query-result publication rejects disabled or unconfigured scene/raw slots before changing caller output. The first successful ShapeSetData activates enabled scene/raw users with stable RID/index/owner identity; a failed first configuration restores the pending state and can retry. Data copies, old-view retirement and shared-world borrowing keep their existing guarantees.

Private checkpoints preserve pending slots. Portable authoring compatibility distinguishes pending and configured data, so applying an old pending snapshot after configuration rejects before mutation. The encoded snapshot layout and hashes for live configured shapes stay unchanged. This is headless authoring/simulation behavior; hardware GPU, rendered output, native allocator and full physics acceptance remain separate gates.


## Scope and ownership

The internal [RegisteredPhysicsWorldBackend](../classes/RegisteredPhysicsWorldBackend.md) connects the complete [PhysicsServerBackend](../classes/PhysicsServerBackend.md) to shared scene/raw identity, authoring, views, callbacks, monitors and history. The public contract has numerical [collider](../classes/PhysicsBackendCollider.md) and [joint](../classes/PhysicsBackendJoint.md) attachments and typed observation records. It carries no PhysicsSpace, PhysicsBodyRuntime, PhysicsColliderBackend or scene storage through implementation operation parameters.

## Runtime flow

A fresh claimed implementation initializes before activation. Attachment creation receives engine-assigned RIDs. Shared preparation supplies complete authored integration/force/Area policy and logical shape slots. The implementation solves its own state; common completion publishes scene/raw results, contact/Area observations, statistics and callbacks. Ordinary direct views dispatch through the same selected attachment. Motion output uses the reusable public result construction family and existing completed-output validation.

Each user implementation call borrows owner-thread lifetime. Factories cannot reuse one world, collider, joint or history object. Captured numerical output validates before consumption. Failed contact publication retains the receiver snapshot and aggregates errors after later deliveries. Started solve/synchronization failure retires the world without hidden CPU/GPU replay. Cleanup attempts Finish after all attachments, including common-space retirement and partial creation failures.

Same-backend private history stays with the implementation. Portable state transfers engine-authored bounded values; local one-way RIDs map to network IDs only in the common adapter. Explicit and joint exceptions are supplied as current-world typed RID pairs. Observer metadata and identities remain engine sampled.

## Verification boundary

The separate [public-only consumer](../../tests/PhysicsBackendExtension.Consumer/PhysicsBackendExtension.Consumer.csproj) implements a bounded numerical model with circle/annulus, rectangle, capsule, segment, convex/concave polygon, infinite-boundary and directed-ray geometry. The dedicated geometry sections below record actual integration, queries, overlap/contact output, constraints, automatic sleep and private replay, with their own limits and tolerances. It is not a production physics option. The fixture runs 64 warmup and 64 measured complete PhysicsFrame cycles, including raw state callbacks, and measures zero owner/all-thread managed bytes. Scene/raw mass2/4 and constant forces12/24 both yield .1 scene units/s after the first 1/60-second interval within .000001 float tolerance; retained checkpoint and portable poses compare exactly. Contact/Area events, real circle queries, bounded spring response, receiver-failure continuation, reuse rejection and terminal solve failure cover their named paths. The fixture proves these exercised adapter paths. All currently applicable built-in shape/body/Area/joint behavior still requires its existing shared CPU/GPU checks.

PhysicsServerManager.RegisterServer and SetDefaultServer now execute through a retained static facade. Factories return fresh PhysicsServerBackend objects, typically derived from PhysicsServerExtension. Duplicate/reserved names, null/disposed/reused implementations and recursive creation reject. DEFAULT uses strictly greater priorities; World(string) captures its selection before lazy startup, while SpaceCreate(string) initializes an owned inactive space immediately. Resource aliases retain one runtime. Requested and actual names accompany Backend.Extension; explicit CPU/GPU and parameterless CPU remain independent of the default. Factories execute outside the lock, including registration from another thread. World release calls Finish and disposes the implementation; direct caller disposal of a bound implementation rejects.

The public immutable configuration/result family includes body integration/forces, Area fields, joints, sleep/contact policy, logical shape slots, resolved fields, mass properties, overlap/shape contacts, statistics and one-way episodes. Collider and joint objects are fresh per attachment; checkpoint objects provide private history hooks. Query exclusion spans are passed explicitly and borrowed only during the synchronous call. The consumer compiles without internal access and executes both SceneTree frames and explicitly owned SpaceStep.

Complete extension-family acceptance remains open under [ADR 0103](../decisions/physics-extensions.md#adr-0103). The coverage ledger retains each row's actual Implemented, Partial or Blocked status and its named remaining trigger; existing public registration and exercised geometry are not missing prerequisites. The bounded test solver does not certify arbitrary external algorithms or the full production contract. Native allocator, cross-platform, real-rendered output and final performance acceptance are unverified here.

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

Configured mass, explicit inertia/custom center, friction, bounce, gravity scale,
damping, effective body mode, CCD mode, can-sleep, omission, persistent force/torque,
contact cap and reporting depth use the same guarded authored-input read path.
The path permits actual selected numerical hooks, including Solve, without querying
solver state or recursively inspecting its failure hook. Ordinary reads retain
owner/lifetime/failed-world guards. Numerical writes, including equal-value
assignments, still reject before mutation; velocity/direct-state queries remain
unavailable inside a solve.

Automatic inertia/center getters still require current-geometry preparation and
reject numerical borrowing before entering preparation. The public-only
--parameter-reads consumer covers all eighteen getters on scene/raw identities,
initial/live attachment, actual solving, automatic profile rejection, invalid-kind/
stale IDs, off-owner access and terminal-world reads. It keeps the original circle
inertia check (9 to 36 kg-scene-unit-squared after radius 3 to 6), within 0.0001.
Configured values and discrete policy reads are exact. After 64 warmup frames,
128 full active 1/120-s frames with paired ordinary and solver-hook reads require
zero owner/all-thread managed bytes. The same pre-fix consumer fails inside Solve;
the fixed runtime permits the unchanged consumer and keeps write/query guards.

Material revision polling remains in the authored read path: friction/bounce
getters clear superseded overrides and mark later shape preparation dirty even
when an earlier resource subscriber prevented the scene callback. This invokes
no numerical preparation. The consumer retains its failure cases for both channels.
Native allocations, other platforms and complete registered numerical acceptance
remain open; this closes an input-read rejection, not the complete solver contract.

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
facade family under ADR 0087/0103 and remain Partial. Bounded registered pin/groove
response now has the shared checks below; complete numerical-extension acceptance
remains open. Built-in joint response uses the existing shared CPU/GPU contract tests. Native allocations, other
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
remain Partial: broader coupled registered constraint behavior and complete
extension acceptance are still open. Native allocations, other platforms, final networking
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


## Area pose and monitor publication

AreaCreate retains a fresh engine RID independently of selection. Common scene/raw
pose and monitoring authoring projects through selected collider creation, SetPose,
SetAreaMonitoring and overlap output. AreaGetTransform returns authored engine
pose, without exposing numerical storage. Invalid finite/scale/skew input rejects
before replacing authoring. Numerical borrowing rejects configuration writes,
including equal scene assignments; reads retain their existing access guards.

Scene Monitoring/Monitorable changes also reject from the same Area's raw server
observer, using the shared dispatch state. Equal assignments remain harmless during
ordinary overlap callbacks. Typed scene events/snapshots and external server
observers keep independent histories; disabling scene Monitoring leaves raw
observers active. Re-registering either callback resets both raw histories, including
the same delegate. Receiver detach silently clears its histories; reentry preserves
callbacks and policy. Observer errors continue committed deliveries without replay.

The public-only consumer's `--area-monitors` runs on explicit CPU, GPU and an
independent registered circle solver. It checks scene/assigned/unassigned RID and
instance identities, logical slot zero, directional body masks, default/live raw
monitorability, invalid poses, initial numerical borrowing, own callback mutation,
equal assignments, zero steps, reset/detach/reentry, four continued failing
deliveries, off-owner access and wrong/stale RID kinds. After 64 warmup frames and
completed finalization, 64 pose-edited full PhysicsFrame cycles deliver 128 entries
and 128 exits and allocate 0 owner/all-thread managed bytes on each implementation.
Raw handler installation and intentional errors occur outside that bracket.

The registered fixture uses one circle slot per collider; arbitrary registered
geometry/compound logical indices and complete extension numerical acceptance
remain open. Existing PhysicsAreaMonitorTests/ShapePairEventTests cover built-in
compound slots and event epochs. Native allocation, other platforms, final network
scenarios and real-window/performance acceptance are unverified here.


## Contact reporting depth

BodySet/GetContactsReportedDepthThreshold retains common scene/raw policy in scene
units. Zero leaves all signed candidate depths eligible; a positive value includes
only equal/deeper penetration. The policy filters contact reports and RigidBody's
point/object/shape snapshots, without changing physical response, waking, geometry
or the already completed snapshot. Changes apply at the next nonzero publication
and survive detach/reentry. Checkpoints require unchanged configuration; portable
schema matching includes the threshold, so incompatible policy rejects before apply.

PhysicsBodyContact exposes immutable signed Depth and a finite explicit-depth
constructor. The existing constructor derives depth from supplied boundary anchors.
The registered adapter now stores the implementation's depth rather than zero;
ReadContacts must return the deepest candidates fitting its supplied span, with
encounter-order ties. Common filtering is monotonic, so removing shallow records
from an already deepest capped set cannot omit an eligible deeper candidate. CPU
converts native metre depths; GPU retains resident reports in scene units. No new
GPU readback or CPU mirror is introduced. Factory contact projection uses its own
explicit sampled depth and applies the same common threshold before committing.

The public-only `--contact-threshold` runs on CPU/GPU/registered owners. Two
independent circle contacts exercise scene/raw defaults, all five scene body sibling
roles, raw static reporting, invalid input, numerical
borrow reads/rejected writes, positive .5 and high 100 thresholds, monitor exits,
unmodified same-backend response (.00001 scene-unit tolerance), stale/kind/thread
guards, retained edits after failing ordinary callbacks, detach/reentry, checkpoint
and portable-schema rejection/recovery. The
registered two-unit sample verifies equality eligibility exactly. After 64 warmup,
64 threshold-edited full frames deliver 32 scene entries/exits and allocate zero
owner/all-thread managed bytes. Cold errors/history setup are outside the bracket.
The independent fixture retains one circle contact per body; arbitrary registered
contact topology/overflow remains part of complete numerical acceptance.

## Motion-test exclusions

The inherited public helpers IsBodyExcludedFromMotionTest(RID) and
IsObjectExcludedFromMotionTest(ulong) read the current world's synchronous motion
inputs without copying or resolving identities. The common space borrows the
already supplied arrays until its finally block, so parameter-resource edits affect
the next call. Helpers return false outside motion, retain owner/lifetime/failure
guards, and remain authored-input reads inside numerical hooks. A nested ray query
or another world's motion query preserves the outer scope; same-world recursive
motion rejects before replacing it. Hook and invalid-result failures clear the
scope while preserving the caller's prior result.

The independent circle/annulus consumer now filters object exclusions as well as
body RIDs. One associated object can represent both raw obstacles; excluding that
object suppresses both, while a body exclusion removes only the specified body.
The engine still rejects an excluded collider returned by a faulty implementation.

[BackendChecks.MotionExclusions.cs](../../tests/PhysicsBackendExtension.Consumer/BackendChecks.MotionExclusions.cs)
uses only public API and executes the same scene/raw numerical cases on CPU, GPU
and the registered implementation, also running the latter with unavailable GPU
drivers. Eight cases per mover check nearest/farther hits, mixed/all exclusions,
shared object association, inert/default RIDs and unknown/zero object values.
Registered probes execute during actual hooks, including nested ray/other-world
queries, parameter edits, off-owner access inside/outside motion, hook/result
failures, unbound/disposed implementations and terminal failed spaces.

Each final process exits zero. After 128 warmup frames, 64 measured full frames
include 1024 outer motion tests and zero owner/all-thread managed bytes. Registered
samples additionally execute 1024 nested motion tests and ray queries; total
outer hooks are 3104 including warmup and recovery controls. Cold error/thread
probes and GC/finalizer draining stay outside the measured bracket. A 1e-5
scene-unit pose allowance covers float unit conversion at the authored 60-unit
coordinate (CPU observed 59.999996); identities/exclusion membership are exact.
The fixture asserts a positive actual hook count to reject skipped checks caused
by lazy World initialization. Original rejected runs remain separate.

Reproduce with the source-native Release consumer and --motion-exclusions,
adding --cpu or --gpu for a built-in owner. Ignored outcomes, copied-runtime
fingerprints and logs are under bin/physics-motion-exclusions/2026-10-12/final-main.
This implements the two membership helpers; arbitrary registered geometry and
complete motion-operation numerical acceptance remain open.

## Notification delivery status

PhysicsServerBackend.IsFlushingQueries and
PhysicsServer.SpaceIsFlushingQueries(RID) read the same common-world phase.
It includes the previously separate force/integration/state callback interval,
selected FlushQueries, sleep changes and contact/Area event delivery. Nested
ordinary queries retain the ongoing phase; other worlds have independent status.
No callback reordering, numerical policy, mutation guard or error aggregation
changes. Status remains an owner/lifetime-qualified lifecycle read during
terminal-error cleanup, observing no solver state and allocating no storage.

The pinned implementation wraps body state and Area call_queries under its flush
flag; see the [server phase](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/godot_physics_2d/godot_physics_server_2d.cpp)
and [space delivery](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/godot_physics_2d/godot_space_2d.cpp).
The C# implementation-object projection keeps shared observer ownership under
ADR 0103 and static service access under ADR 0095.

[BackendChecks.NotificationPhase.cs](../../tests/PhysicsBackendExtension.Consumer/BackendChecks.NotificationPhase.cs)
executes real scene/raw force and state callbacks, a RigidBody subclass integration
hook, contact enter/exit, scene/raw Area observers and sleep transitions on CPU,
GPU and a registered solver. It asserts true inside delivery and false outside;
registered solve/end-sync hooks, nested query/other-world step, callback/flush
exceptions, reentry rejection, invalid/stale RIDs, unbound/disposed/off-owner
reads and terminal failure cleanup are separate controls. The registered sleep
control changes implementation-owned awake state; CPU/GPU use automatic sleep and
policy wake. Explicit user Sleeping assignment suppresses that event by contract
and was rejected as a test trigger.

After 128 warmup frames, 64 full measured frames retain real observer transitions
and zero owner/all-thread managed allocation. Cold errors, threads, sleep controls
and GC/finalizer draining are outside the sampled bracket. Status/membership and
callback counts are exact; this test does not compare backend trajectories or
certify complete registered geometry/numerical behavior. CPU, GPU, registered and
registered-with-GPU-unavailable processes all exit zero, including cold controls
after the printed sample counters. Reproduce with --notification-phase, optionally
--cpu or --gpu. The unavailable-GPU control removes display/runtime variables,
selects invalid GPU/Vulkan drivers and a dummy video driver. Logs, outcomes and
the fingerprinted independent runtime remain ignored under
bin/physics-notification-phase/2026-10-12/final.


## Public shape-pair dispatch

PhysicsServer.ShapeCollide selects an explicit live space for CollideShapes;
BodyCollideShape selects the body's actual attachment or detached resource path.
Previously the selected resource-pair hook had only an internal test entry point.
Both public calls retain logical shape-slot poses, independent swept regions,
zero-capacity collision truth, pair counts and atomic staged global-point output.
Disabled slots, filters and other objects do not suppress an explicit pair.

Protected GetShapeGeometry borrows immutable current authored data without the
resource clones formerly used by the public-only test solver's shape queries.
It supports all built-in data families and custom data, numerical input reads,
revision retention and owner/lifetime/failure guards. Procedural suppliers execute
under world borrowing; exceptions restore that scope. User supplier allocation
on a cache miss remains outside the engine's cached-read guarantee.

[BackendChecks.ShapePairs.cs](../../tests/PhysicsBackendExtension.Consumer/BackendChecks.ShapePairs.cs)
uses public API only. CPU/GPU checks cover every built-in pair family; registered
checks execute circle swept regions and static annulus/circle contacts with a real
hole and reversed ownership. Scene/raw/detached transformed slots, disabled slots,
zero filters/capacity, untouched output tails, five invalid/throwing hook modes,
same/other-world nested staging, supplier errors, retirement, failed worlds and
owner/solver guards execute. Boundary distances use a 0.0001 scene-unit tolerance
for finite float geometry; discrete counts/identity and untouched tails are exact.

After 128 warmup frames, 64 full frames each execute five outer public queries
(320 total) at zero owner/all-thread managed bytes on CPU/GPU/registered owners.
Registered samples include 512 selected hooks from outer and same-world nested
calls plus independent other-world queries. Cold GC/finalizers, thread, error and
supplier controls are outside the bracket. Run --shape-pairs, optionally --cpu or
--gpu; ignored process logs and copied-runtime fingerprints are under
bin/physics-public-shape-pairs/2026-10-12/final.

The built-in resource-pair evaluator remains managed geometry; this change does
not claim device acceleration for standalone resource pairs. The bounded external
fixture does not certify arbitrary geometry, moving-annulus contact boundaries
or full extension numerical acceptance. The two pair rows and typed shape-data
read are Partial; broader motion/solver acceptance remains open.


## Motion result receiver identity

Registered TestBodyMotion retains the moving RID supplied by SetCollision until
common result validation compares it with the requested body. The adapter formerly
retagged every result before validation, so a same-world surrogate with compatible
shape indices could hide an invalid receiver. Only an unobstructed SetMotion value,
which has no authored receiver, receives the requested RID from the adapter.

The existing common validator rejects a mismatched receiver before publishing any
caller output, including queries with no caller result. Error cleanup restores
motion/exclusion scopes; the next valid query succeeds without failing the world.
No new API, duplicate validator, callback ordering or numerical policy is added.

[BackendChecks.MotionExclusions.cs](../../tests/PhysicsBackendExtension.Consumer/BackendChecks.MotionExclusions.cs)
adds both scene-to-raw and raw-to-scene same-world receiver substitutions. The
surrogate constructor is otherwise valid; changed point/normal/travel expose any
accidental publication. All thirteen public result values/identities remain exact
on rejection, and no-result calls reject too. The original runtime failed this
public-only check (process -6, Expected InvalidOperationException); the fixed
runtime passes. Cold substitution/errors follow the unchanged 1024-query and
64-full-frame warm bracket, retaining 0/0 owner/all-thread bytes on CPU/GPU and
registered owners, including the GPU-unavailable registered control.

Ignored red/final copied-runtime outcomes and fingerprints are under
bin/physics-motion-result-identity/2026-10-12. The actual typed motion hook now maps
as Partial. General custom geometry, complete recovery/separation-ray behavior in
an arbitrary external solver and full numerical-extension acceptance remain open.


The first broader full-suite run failed the CPU resting-material owner-allocation
assertion; a fresh full run of the same unchanged copied runtime passed. Both logs
are retained as final/full-suite.log and final/full-suite-repeat.log. The isolated
failure's cause is unproven and remains a verification limit; the repeat does not
establish that material-allocation intermittency is fixed.


## Registered lifecycle census

The public-only --lifecycle consumer drives an independent registered implementation
through both an explicitly stepped raw body and a SceneTree-owned rigid body.
Initialize runs once before attachment/activation; a World activates after initialization,
while SpaceCreate stays inactive. Changed local activation calls SetActive before
commit; equal values skip it and failure preserves the old policy. Shared global
suspension skips the interval without rewriting that local flag.

A positive active interval calls BeginStep, Solve, SyncResults and ReadStatistics,
then delivers the completed body observer before FlushQueries and EndSync. An empty
active world keeps the same interval stages without body callbacks. Zero-time,
inactive and globally suspended calls perform no selected interval. Public scene
reentry and SceneTree release guards are exercised through their own roles; explicit
spaces use SpaceStep and FreeRID.

ReadStatistics supplies the complete typed Active/Pairs/Islands value; each field
must be nonnegative, and Islands cannot exceed Active. Common publication keeps the last completed sample and
GetProcessInfo aggregates active spaces without reentering the implementation,
including on another thread. Failed numerical intervals retain the previous sample.
Solve, synchronization or statistics failure makes that selected world terminal;
flush/end-sync errors preserve the committed interval and allow a subsequent tick.
End-sync still receives earlier failures. Init/finish/disposal errors retain all
three failures after cleanup; ordinary and failed owners finish and dispose.

Discrete order, mode/count/identity and rejection checks are exact. One 1/120-s
unit-velocity step permits 0.0001 scene-unit absolute displacement error. After
64 warmups, each ownership role measures 128 full moving intervals with cached
statistics reads and requires zero owner/all-thread managed bytes. The fixture's
phase instrumentation reuses a bounded list; errors and tracing setup are cold.

Eight previously unmapped lifecycle/statistics extension rows now map to eleven
existing typed declarations under ADR 0103. BeginStep plus Solve project the step
operation, EndSync accepts the prior failure, and the three-value statistics record
projects the selector output. The global static facade remains separate from the
owned implementation. The lifecycle rows remain Partial; the exercised completed-statistics selector is Implemented, with concrete executed paths;
complete numerical-extension, native/foreign and final performance acceptance
remain open. No additional lifecycle API or numerical algorithm is introduced.


## Diagnostic capacity and publication

`SetDebugContacts(int maximum)` is a required selected implementation hook.
SceneTree sends its construction-sampled project point cap before the next solve;
zero disables capture immediately and is also allowed during terminal-error cleanup.
Preparation runs outside stepping on the owner thread with the world borrowed.
Equal limits skip the hook. Implementations prepare reusable storage before returning
and preserve their prior limit if preparation throws. The common cap commits only
after successful preparation. This gives external GPU implementations the policy
needed to omit disabled work and retain buffers before stepping.

`ReadDebugContacts(Span<Vector2>)` combines the reference count and point reads.
After synchronization, it writes a finite engine-unit prefix bounded by the prepared
cap; negative/excess counts and nonfinite points fail the started interval. These
samples follow ADR 0100, independently of gameplay contact-report caps. Zero-time
and inactive intervals retain the latest sample; active empty output replaces it.
Disable/tree detach relinquishes capture even if a caller retains the World resource.
No game-facing static debug-space wrappers are introduced. Backend authors must
implement the newly required preparation hook alongside their existing read hook.

Tree FinalizeLoop and Dispose preflight borrowed implementation ownership before
teardown starts. This also covers preparation outside a scene frame; rejecting a
nested release keeps the tree running and its hierarchy intact. Normal failed-world
cleanup retains the existing release path.


The public-only `--debug-contacts` consumer exercises cap 3 and zero, two actual
circle pairs with gameplay reporting disabled, construction-sampled settings,
zero/inactive retention, active empty replacement, disable/retained-World detach,
preparation failure/retry, owner/borrow guards and both tree finalization entries.
Negative/excess counts and nonfinite points fail the interval and still allow zero-cap
cleanup. Each cap runs 64 warmup plus 128 measured complete active 1/120-second frames,
including public transform/velocity edits, with zero owner/all-thread managed bytes.
These are allocation and lifecycle checks, not native-memory or full numerical acceptance.

`--debug-drawing` uses a continuously forced circle contact and cap 1 to inspect a
single diagnostic marker. Both gpu and compatibility renderers pass actual pixel
checks, disable/re-enable, separation and native retirement. Outlines are disabled
in this fixture so opaque shape outlines do not alter the marker's background.
Channel tolerances are .05 green/.04 red and blue for render-target rounding.
The captured GPU image was also visually inspected. Shared built-in CPU/GPU contact
and native four-combination diagnostic checks retain their independent coverage.
No new overall FPS, network or mass-scene performance acceptance is claimed here.


## Factory-selected direct views

The registered backend's existing CreateDirectBodyState and CreateDirectSpaceState
hooks now have a public-only executable fixture. The common engine caches one live
space view and one current-generation body view, validates factory results outside
registry locks, and retains the existing null/disposed/foreign/recursive rejection
and retry rules. Disposing a view releases no body/world. Detach/reentry permanently
retires a body view; another attachment receives a new one.

A newly cached body extension starts with an empty contact snapshot. Preparing
contact storage marks that empty snapshot as published; getters do not call
implementation contact hooks before the first completed interval or after view
recreation. The earlier code initialized only the count, leaving cached extensions
in caller-created dispatch mode until the first solve. The public regression fails
on that unexpected hook invocation; the same consumer DLL passes against the fixed
library. Independent caller-created extensions still dispatch their own contact
hooks because they are not the runtime's cached view.

After solving, the body extension supplies actual independent contact values during
qualified publication. Count/identity/geometry/impulse validation completes before
raw force/state, scene force and scene virtual integration callbacks receive the
cached view. Later getters preserve that immutable snapshot through pose and object
association edits; the next interval samples the replacement owner. Equal contact
cap assignment clears the snapshot explicitly. One bad receiver keeps its previous
snapshot while later callbacks and the committed tick continue.

`--factory-views` reuses the existing public CircleQueries fixture by linked source,
without another query algorithm or backend dependency. It exercises all six query
families, analytic circle cast entry within .0001 fraction tolerance, excluded and
nested calls, malformed counts/fractions/points, duplicate hits and hook failures.
The state fixture supplies the complete typed hook family and checks real mass-scaled
impulses, state/filter/sleep/force operations and callback identity. Null/disposed,
foreign-body/world, recursive and earlier-attachment factory results reject without
retiring someone else's view. A pre-solve factory error publishes no tick and retry
recovers. After 64 warmup cycles, 128 active complete scene/raw frames with contact
publication, four callbacks and public point-span queries allocate zero owner/all-
thread managed bytes. This is a bounded circle implementation; full numerical,
native allocator, foreign-platform and final performance acceptance remain open.


Factory state-write checks also exposed an asymmetric guard: raw layer/mask setters used the geometry-lifetime path and rejected ordinary body-state forwarding, while scene setters were parameter writes. Both raw setter siblings now use the common body write guard and retain separate release/geometry-borrow validation. Numerical filter hooks still reject nested layer/mask writes, shape addition and RID release before mutation. Scene and raw view writes preserve the same filter values and real wake/query path.

## Borrowed shape correction policy

The common Shape.CustomSolverBias resource property supplies selected collider
Shape descriptors with authored policy. Its getter remains available during
numerical borrowing. Valid writes, including equal values, preflight all scene/raw
users before changing the value or policy epoch. Disabled slots and a scene-held
server geometry view participate. Owner-thread, numerical/step, own geometry-supplier
and recursive access violations reject without Changed or geometry mutation.
Access hooks run outside the resource registry and geometry locks and cannot dispose the shape.
Retained borrower lists are cleared after success or failure. Ordinary body-state
callbacks can edit policy; a throwing Changed observer preserves an already
committed edit. Immutable geometry data and resource RID remain identical.

The public-only `--shape-policy` profile executes on registered, CPU and independent
GPU worlds. It checks initial attachment and live state/shape/filter hooks, solve
and status reentry, raw Body/Area sharing, borrowed server geometry through a scene
owner, foreign-thread equal/changed writes, invalid values and recovery. After 256
warmup cycles, 128 active complete scene/raw frames with two changing resource
biases allocate zero owner/all-thread managed bytes. Existing contact-policy tests
verify physical CPU/GPU correction. The two extension bias rows map to the actual
common resource projection as Partial; arbitrary registered numerical correction,
full extension acceptance remain open; geometric writers now use the preflight protocol below.

Geometry edits now preflight all scene/raw borrowers before private data or convex
compilation-cache mutation. This includes disabled slots and equal assignments.
The protected Shape.EnsureGeometryChangeAccess authoring helper runs selected
status hooks on the owner before commit without resource locks. Coordinated worker
geometry writes remain valid in quiescent worlds and never call owner-bound status
hooks; numerical/step borrowing rejects worker writes before mutation. EmitGeometryChanged publishes
the edit with owner/phase checks and no repeated fallible status hook. Procedural
subclasses must preflight before changing private fields; publication cannot undo
such private writes. Ordinary post-solver edits remain available. The public-only
--shape-geometry profile exercises 16 authoring paths across all eight built-ins,
CustomShape and a procedural resource, with immutable data/RID preservation on
rejection, actual point-query changes and observer failure. Warm frames measure
stable geometry plus guard calls; array copies and accepted immutable geometry
creation are authoring allocations. Full numerical-extension acceptance remains open.

## Shared shape construction and data lifetime

The common facade owns the eight built-in shape factories, exact ShapeGetType and
typed ShapeSetData/ShapeGetData. A shape RID is shared metadata; selected collider
attachments consume its actual current Shape resource. Copying data preserves the
full typed geometry and inherited CustomSolverBias. Returned data is independent.
A successful replacement retains RID and retires the old private resource view;
immutable data already retained from that view remains valid. FreeRID removes
scene/raw slots and retires the current view; empty scene shape owners retain
identity until explicitly removed.

On replacement failure, original authoring is restored first. Every raw borrower
is marked dirty and its rollback attempted; an error cannot skip later borrowers
or retirement of rejected private geometry. Scene borrowers are marked dirty too.
Original apply, rollback and retirement failures are retained. A failed rollback
stays dirty so the next guarded public preparation retries before a query or step.
If old-view retirement throws after successful replacement, new geometry remains
committed. This path uses the existing resource cleanup/error machinery.

The public-only --shape-resources registered/CPU/GPU profile checks all eight
factories, exact typed data/policy, copies, private view identity/lifetime, disabled
scene/raw slots, wrong-owner/type refusal and release. The registered failure
fixture additionally checks mixed apply/rollback/retirement errors, later borrower
continuation, source view preservation, real query recovery and subsequent success.
These ten extension rows now map as Partial to the actual common resource role;
full arbitrary registered numerical geometry and extension acceptance remain open.

## Body construction and continuous policy

The common facade allocates independent detached body identities and inactive
spaces; explicit CPU/GPU or registered-name selection binds each space owner.
Four body modes and three per-body CCD values survive detached authoring and
attachment. All five scene body siblings retain their existing direct mode
response. Mass reset preserves configured kilograms and recomputes automatic
center/inertia from current geometry through the selected owner. The complete
typed BodyParameter family reaches mass, material and integration preparation.
Numerical borrowing rejects equal mode/CCD writes and mass resets before mutation;
retained policy reads stay available.

The public-only `--construction` consumer checks these paths on registered, CPU
and GPU worlds, including sleep preservation for equal CCD, wake on changes,
invalid positive/negative values and retired views after detachment. Its circle
uses mass 4 and radius 3: automatic inertia is 18 within 0.0001; center, mode and
CCD values are exact. After 256 warmups and full garbage collection, 128 complete
1/120-s steps change CCD, set velocity and solve with 0/0 owner/all-thread managed
bytes. Direct valid-value checking avoids enum metadata reinitialization: the
unchanged pre-fix measurement allocated 248/248 B in the first CCD setter after
collection. The registered fixture records delivered CCD configuration; it does
not implement or establish independent numerical CCD.

The eight creation/mode/CCD/mass/typed-parameter extension rows are Partial.
`ELECTRON2D_TEST_GPU_CCD_CONTRACT=1` separately verifies actual CCD, boundaries and
directed-ray dynamics on CPU and resident GPU. Frozen runner evidence is under
`bin/physics-construction/2026-10-12/final/`. Full registered numerical, physical,
network, platform and final performance acceptance remain open.

## Registered pointer delivery

Scene Body/Area pickability is retained by CollisionObject.InputPickable under
ADR 0099. The selected point-query owner supplies physical hit geometry, canvas
identity and logical indices; common scene eligibility then chooses callbacks.
Ordinary point queries include unpickable colliders. Raw RIDs do not become scene
callback sources. The two extension pickability rows project this existing scene
role as Partial; no inert raw flags or separate hit-test world are introduced.

The public-only `--picking` consumer runs unchanged on CPU/GPU/registered worlds.
It verifies false Body/true Area defaults, root/transformed-layer queries, exact
viewport event coordinates, virtual-before-signal ordering, owned event-copy
lifetime, object/shape enter/exit, off-owner writes and equal policy writes during
selected point queries and solving. Ordinary pointer callbacks can edit policy.
After 128 warmups and complete garbage collection/finalizer draining, 128 complete
1/120-s frames change both flags and produce 64 real passive entries/inputs and
64 exits, with zero owner/all-thread managed bytes. Policy/identity values are
exact; input geometry is a radius-8 circle at local (20,20).

The native SDL picking suite now also creates named registered worlds through
the existing public consumer factory. GPU/compatibility renderers each run CPU,
GPU and registered physics, checking localized native clicks, capture suppression,
exit-before-window-signal and teardown. Pixel assertions at the transformed click
coordinate (55,50) compare RGB to Pink within 0.02. Native injection uses one
physics tick per rendered frame so catch-up ticks cannot replace the synthetic
hover with the actual desktop pointer before the next pump; the previous limit is
restored. This fixture is not a real-time FPS benchmark.

Frozen evidence and six PNGs are under
`bin/physics-picking/2026-10-12/final/`. Full registered arbitrary geometry,
physics/network/performance and platform acceptance remain open.

## Shape-owner metadata access

The complete common owner registry read family now qualifies an attached
CollisionObject through its actual PhysicsSpace as well as any SceneTree. A
scene object explicitly bound with BodySetSpace/AreaSetSpace has no Tree, but
still rejects off-space-owner reads. Detached objects retain ordinary authoring
access. Attached ordinary getters reject terminal world failure; pure retained
owner inputs remain readable within selected numerical hooks without preparing
geometry or recursively querying the backend failure hook.

CreateShapeOwner uses the same failure preflight before allocating a group.
Other owner setters already resolve their group before mutation. Internal logical
slot inspection retains its existing guarded caller boundary: contact publication
and failed-world shape retirement must still inspect slots while ordinary public
queries are unavailable. Lifecycle cleanup remains available after failure.

The public-only `--shape-owners` consumer runs on CPU/GPU/registered spaces with
StaticBody, AnimatableBody, CharacterBody, RigidBody, PhysicalBone and Area. It
checks all eleven authored getter families from a worker on tree-less attachments,
retained reads/equal-write rejection inside numerical point queries, owner/index
reindexing and highest-ID reuse, normalized one-way authoring and Area's accepted
no-effect one-way setters. Registered failure checks exercise every read family,
set/create rejection and full disposal. The pre-fix consumer rejects the accepted
worker read as RED; the unchanged checks pass after the common preflight change.

After 128 warmups and full GC/finalizer draining, 128 complete 1/120-s intervals
read scalar registry metadata on all six roles with 0/0 owner/all-thread managed
bytes. Caller-owned GetShapeOwners arrays allocate intentionally and are outside
that scalar/step bracket. Identities and policy values are exact. Frozen evidence
is under `bin/physics-shape-owners/2026-10-12/final/`. This closes the confirmed
access defect; full shape-owner geometry/disable, numerical-extension and final
physics/network/performance acceptance remain open.


## Independent pin and groove response

The public-only consumer's Solver now solves pin anchor constraints and finite
grooves using body-local frames, center-of-mass lever arms and inverse mass/inertia.
Coupled linear rows share a vector force-times-duration cap. Pin compliance,
world-inherited bias, correction-speed limits and independently capped motor/stop
angular impulses use the immutable supplied configuration. Groove translation
remains free inside its finite interval, and unilateral endpoints permit retreat
back into it; zero/reversed guides and relative rotation
retain their roles. Iterations and constraint default bias are actual retained
solver inputs. Body integration honors Replace damping without adding world drag.
This is a bounded external test implementation, not a new production backend.

--joint-response, optionally --cpu or --gpu, exercises the same public assertions
on registered/CPU/GPU worlds without fallback. Both server-owned and scene-owned
joint RIDs drive a scene body and a raw body. Cases cover physical momentum and
coupled force caps, positional recovery, compliance, world default edits,
off-center and single-body pins, motor/general angular caps, simultaneous motor
and stops, axial/perpendicular/endpoint groove response, retreat at both endpoints and
reversed/zero guides.
For mass 2, force 6 and dt 1/60, the maximum physical velocity change is .05;
checks bound it to (.045, .051) and pair momentum error below .001 scene units/s.
Motor torque .0006 N m produces .05 rad/s at inertia 2; general angular force .3
produces .0025 rad/s. Off-center point speed has a .5 scene-unit/s short-interval
bound. The driven stop allowance .06 rad bounds one 3 rad/s outer tick (.05 rad)
plus numerical slack. Position and compliance checks require recovery/ordering,
not identical internal velocity corrections or substep counts.

Each of the four owner/role combinations warms 128 full edited frames and measures
128 more at strict 0/0 owner/all-thread managed bytes, then restores a private
checkpoint and verifies the next interval within .001 scene units and units/s.
The final contract is RED with the former missing pin/groove solve, and separately
with the former ignored Replace modes. Existing spring/registration/lifetime
checks remain in the complete consumer. Logs are retained under
bin/physics-registered-joints/2026-10-13/. Pose integration in this fixture remains
one outer interval; arbitrary coupled geometry, CCD/sleep/contact history,
portable/network correction and final native/performance acceptance stay open.


Final linux-x64 Release/native-source validation on main a0063147 plus this change
passed registered and CPU no-GPU response, explicit GPU response, the complete
registered consumer and the full executable suite (five profiles, all exit 0).
Source/runtime fingerprints remained identical; final-current/provenance-before.json,
provenance-after.json and outcomes.json under bin/physics-registered-joints/2026-10-13/
retain the exact binding. The earlier final/ executable suite passed but its source
guard rejected the subsequent unilateral-endpoint correction; it is not final proof.


## Independent private history

The public-only consumer's private History now captures typed numerical body state,
including motion, pending numerical forces, sleep/can-sleep/time, integration flags,
surface motion, resolved fields, saved motion poses and implementation-local
contact observations. It also preserves one-way decisions, the last numerical
step, pair statistics, readiness and diagnostic points. Common authoring,
scene/server observations and joint configuration keep their existing shared
ownership; the independent constraints reset interval scratch at BeginSolve and
have no additional persistent warm-start state. No vendor data is exported.

Capture reuses growable arrays and a current body prefix, clears retired body
references on shrink, and validates attachment identity before restoring. One-way
capacity is prepared before mutation. Dispose releases arrays and the solver
reference. This replaces the former pose/velocity-only fixed-size snapshot.

--history, optionally --cpu or --gpu, runs the same public sleep, topology
recapture, force/torque replay, contact restoration and warmed checks on the
registered and built-in paths with explicit actual backend and no fallback.
Mass 2 with force 12 and torque 6 over 1/60 s gives .1 scene-unit/s and .05 rad/s;
replayed pose/velocity/angular/contact values use .001 bounds, identities and
sleep flags are exact. The registered case separately reads restored numerical
contacts and 1/60 step through an uncached caller-created view. A retained rejected
one-way decision keeps 60 scene-unit/s pass-through after rollback; explicitly
allowing the same pair gives 30 and one contact. This is a bounded episode fixture,
not a new general one-way/CCD algorithm.

Each path warms 128 then measures 128 full-step/capture/restore cycles at strict
0/0 owner/all-thread managed bytes. The registered episode case separately
measures 128 capture/changed-decision/contact-step/restore cycles at the same zero
budget. Cold topology growth and setup are outside those intervals. Evidence is
under bin/physics-registered-history/2026-10-13/. Full coupled geometry/CCD, portable network replay, native allocations and final performance
acceptance remain open.


Final linux-x64 Release/native-source checks on main 4911a033 plus this change
passed registered/CPU no-GPU history, explicit GPU history, the full consumer and
the complete executable suite (five profiles, all exit 0). Source and all frozen
runtime hashes remained identical before/after. Exact commands, environment and
provenance are retained under bin/physics-registered-history/2026-10-13/final/.


## Independent automatic sleep

The bounded public-only solver now retains PhysicsSleepSettings and applies actual
numerical automatic sleep. A dynamic contact/joint group must remain strictly below
both speed thresholds for strictly longer than the quiet duration. CanSleep=false
vetoes group sleep. Zero thresholds disable automatic sleep; zero duration allows
the first positive quiet interval. Static supports are excluded from dynamic group
traversal, so separate bodies resting on one support keep independent clocks.

Changed settings reset quiet clocks and wake bodies immediately. Explicit impulses,
wake requests and disabling sleep propagate through connected dynamic members.
Dormant contacts retain observations without pose correction; dormant joints skip
numerical corrections. Zero internal impulses no longer wake a resting group or
reset quiet time. Group lists are retained scratch; private history already owns
sleep flags and quiet clocks, while visitation is rebuilt on demand.

--sleep, optionally --cpu or --gpu, exercises the same public contract on scene/raw
bodies without fallback. Exact binary 1/32 s intervals verify equality at .125 s
remains awake, threshold equality prevents sleep, pause does not age clocks, and
restored .5 s history reaches the same later sleep boundary. Pin/contact groups
verify actual sleep and impulse wake, CanSleep veto removal, and independent groups
on one static support. One group candidate failed because residual pin bias woke
an already sleeping group; skipping dormant correction fixes that behavior.

Each path warms then measures 128 replay cycles (15 full frames each, 1920 measured
frames) and 128 actual joint/contact sleep/wake cycles (7 frames each, 896 measured
frames). Both brackets require strict 0/0 owner/all-thread managed bytes, including
state assertions on every measured group cycle. Cold setup and scratch growth are
outside measurement. This closes the former no-op sleep policy in this fixture;
its one-interval primitive geometry, generic CCD, portable/network replay, native
allocation totals and final performance acceptance remain open. Evidence is retained
under bin/physics-registered-sleep/2026-10-13/.


Final linux-x64 Release/native-source checks on main dd0aba2f plus this change
passed registered/CPU no-GPU sleep, explicit GPU sleep, the complete consumer and
the full executable suite (five profiles, all exit 0). Source and every frozen
runtime hash remained identical before/after. Commands, environment and hashes
are retained under bin/physics-registered-sleep/2026-10-13/final-current/; mandatory
build/API/wiki logs are under current-checks/. The earlier final/ suite also
passed, but its source/revision guard rejected concurrent monitor changes; that
run does not confirm the resulting main state.


## Independent contact response

The bounded circle/annulus consumer now retains signed friction/bounce in its
numerical collider and private history. Contacts use endpoint point velocities,
including virtual static/kinematic surface motion. A normal restitution impulse
and Coulomb-limited tangent impulse use both masses and angular lever arms; the
reported world-axis impulse contains both components. Surface edits wake touching
dynamics. AnimatableBody adds inherited virtual motion to its actual target motion
without integrating virtual motion into its pose.

--contact-response, optionally --cpu or --gpu, checks the same public scene/raw
invariants without fallback. Equal mass-2 bodies at +/-30 scene units/s exercise
ordinary summed/clamped bounce, absorbent and negative coefficients, with .5
scene-unit/s velocity tolerance and .02 kg-scene-unit/s momentum-sum bound. Signed/rough friction
must produce the expected stronger response. Static normal, tangent and +/-angular
surface cases use .2 scene-unit/s bounds; an unlocked mass/inertia-2 circle checks
8 scene-unit/s and 4 rad/s within .5. Reported complete contact impulse must match
mass times the observed full-step velocity change within .1 kg-scene-unit/s.
The velocity bounds allow one-pass/substep and contact-point differences while
remaining small against the 20/30-unit incoming speed; momentum and peer identity
are separate stricter checks. Radius 2 -> 3 changes the angular surface result from 9 -> 11 scene units/s and
therefore checks that a material optimization cannot hide changed geometry.

Private next-step response uses .001 velocity and .01 impulse replay bounds.
After 128 warmups, 128 changed-resource/typed-material/surface/step/capture/restore
cycles require strict 0/0 owner/all-thread managed bytes. Each cycle checks actual
surface transfer inside the measured interval. Shared indexed shape traversals
remove the observed 400 B/cycle enumerator cost. CPU additionally updates materials
in place for unchanged logical geometry, removing 600 B/cycle fixture metadata
recreation; geometry/transform/filter/density/one-way/bias changes still take the
full shape path. These are numerical and managed allocation checks, not native
allocation totals, full custom geometry/CCD, networking or final GPU performance.
Evidence is retained under bin/physics-registered-contact-response/2026-10-13/.


Final linux-x64 Release/native-source verification on main 0b7dc576 plus this
change passed registered/CPU no-GPU contact response, explicit GPU response,
the full consumer and the complete executable suite (five profiles, all exit 0).
Source and all frozen runtime hashes were identical before/after. Exact commands,
environment and hashes are under bin/physics-registered-contact-response/2026-10-13/final-current/;
mandatory build/API/wiki logs are under current-checks/. The earlier final/ suite
passed executable checks, but its source/revision guard rejected concurrent OS
changes; it is not evidence for the resulting main state.


## Independent numerical CCD

The public-only bounded circle/annulus solver now consumes its per-body CCD policy
in actual motion. Discrete-only worlds retain the existing one-interval path.
Continuous worlds prepare force eligibility and joint impulse budgets once, then
choose bounded intervals from current solved relative motion and swept circular
geometry. Analytic double-precision entry/exit roots shorten a near-hit interval,
including grazing contact that uniform sampling missed; a .0001-feature advance
ensures a representable contact without losing the remaining tick. Late-woken bodies receive their still-unapplied forces for remaining
time. Contacts/joints resolve again as positions change, while joint accumulated
budgets stay qualified by the full outer interval. Sleep ages by elapsed intervals.
CastRay sweeps the leading radial support point without its own radius; CastShape
sweeps the complete circular/ring geometry. The explicit grazing control preserves
that leading-point/full-geometry distinction.

Contact impulses accumulate for repeated observations of the same peer, including
impacts that separate before final publication. A retained frame-pair set avoids
counting internal intervals as new collision pairs. Private history already owns
completed numerical motion/forces/sleep/contact state; force eligibility and the
pair set are regenerated interval scratch. Completed numerical contacts now retain
all observed peers in this single-shape fixture; arbitrary coupled geometry and
multiple features per shape remain outside its bounded acceptance.

--continuous-response, optionally --cpu or --gpu, exercises the same public
scene/raw assertions without fallback. Mass-2 circles at 3000 scene units/s and
.02 s cross a circle at x=20 when disabled, stop in either continuous mode, and
exchange equal-mass momentum with a -3000-unit/s peer. Elastic response returns
-3000 within 2 units/s and x < -15, requiring actual remaining-time integration.
The stopped/full-bounce impulses are -6000/-12000 kg-scene-unit/s within 10;
this small relative allowance covers native contact skin and interval correction.
Momentum velocity-sum uses .1 units/s; private same-backend replay uses .001
position/velocity and .01 impulse. No bitwise or substep-count equality is required.

An unrelated mass-2 body receives force 6 for the full .02 s and changes speed by
.06 within .001. Exactly one state callback and public tick occur per outer frame.
A circle at y=1.99 relative to the moving radius-1 circle must deflect below
-10 scene units/s in Y for CastShape. CastRay may miss that off-leading-point
contact and must retain its approximate policy; this preserves the accepted
mode distinction rather than equating ray detection to full shape detection. Masks and explicit exceptions allow real pass-through. Each path warms 128 then
measures 128 reset/step/capture/restore cycles with an actual impact assertion at
strict 0/0 owner/all-thread managed bytes.

Registered explicit SpaceCreate/SpaceStep controls exercise the custom annulus
hole: stopping at x=4 and repeated opposite elastic rebounds within [-4,4], with
signed impulse matching final momentum within .01. They use true explicitly owned
spaces; a scene-owned World space cannot be stepped through SpaceStep. More than
4096 fixture intervals fails explicitly. A distant obstacle at 1,000,000 and speed
100,000,000 reaches that numerical budget, publishes no sync result, retains the
selected implementation with no fallback, and rejects later queries/steps. The
control checks the actual aggregate's numerical cause, not an authoring rejection.
Evidence is retained under bin/physics-registered-continuous-response/2026-10-13/.
General geometry/rotation/coupled joints, portable/network replay, native totals and
final performance acceptance remain open.


The shared grazing case also exposed a CPU production defect: general TOI's
linear-slop radius reduction discarded that shallow circle intersection. The
CPU world owner now supplies exact linear disk/point roots for fixed rotations,
then retains its normal contact/remaining-time coordinator. The mode-qualified
CPU/GPU/registered suite passes; this closes the observed disk regression without
claiming complete arbitrary-geometry/coupled extension acceptance.


Final linux-x64 Release/native-source checks on main 3101b954 plus this change
passed registered/CPU no-GPU numerical CCD, explicit GPU CCD, the full consumer
and the complete executable suite (five profiles, all exit 0). Source and every
frozen runtime hash remained identical before/after. Exact commands, environment
and hashes are under bin/physics-registered-continuous-response/2026-10-13/final/;
mandatory format/build/API/wiki logs are under final-checks/. Earlier controls
and RED runs remain separate; none substitutes for this final source binding.


## Independent completed contact frames

The bounded public-only solver retains one numerical row per observed peer for
the complete outer frame, including CCD impacts that have separated by its end.
Repeated observations sum the full signed world-axis impulse, keep the last
geometry/normal and greatest depth, and freeze final point velocities before
callbacks. Publication scans all eligible rows: fill in encounter order, then
replace the first shallowest slot only for strictly greater depth. Equal-depth
ties keep earlier rows. Positive depth thresholds filter before the cap; zero
cap publishes nothing. Kinematic/static contacts can report without physical
response; static/static pairs stay excluded. Lists retain peak capacity.

Private history stores flat numerical rows plus each body's prefix and frozen
reporting policy, prepares restore capacity before mutation and clears retired
prefixes. The ordinary selected view and a caller-created implementation view
both restore multi-peer observations immediately. Public contact records are
constructed in qualified capture, after numeric point velocities freeze.

--contact-frames, optionally --cpu or --gpu, shares the same public assertions
without fallback. A mass-2 circle at 3000 units/s rebounds from x=20 and x=-20
within .028 s: both separated peers retain impulses -12000/+12000 within 20,
and their sum matches full-frame momentum change within 20. Contact-free future
frames clear observations; private restore recovers both peers and motion
within .001. Three report-only peers at depths .25/.5/1 exercise caps 3/2/1/0,
thresholds .49/1.1 and encounter-order ties at equal .5 depth. A scaled case has depths near
25/50/100 and keeps the middle peer at its selected observed depth (within .001
of 50), using published boundary points/normal for built-ins and known explicit
fixture depth. This checks equality without requiring identical geometry bits
across numerical implementations. A raw kinematic
receiver reports a scene peer with zero cap, then filters that contact at .51. Each backend
measures 128 transient step/capture/restore cycles and 128 changed cap/depth
cycles (768 full frames), each at strict 0/0 owner/all-thread managed bytes.
Evidence: bin/physics-registered-contact-frames/2026-10-13/. General shape/feature
and coupled extension, portable network, native totals, real rendering and final
performance/platform acceptance remain open.

## Implicit project selection

The public-only `--project-selection` consumer registers a fresh named numerical owner and selects it through `ProjectSettings.Physics2DEngine`. It verifies actual scene travel, lazily captured aliases, priority changes after DEFAULT capture, strict provider error propagation even with project fallback enabled, and 128 complete warm frames at 0/0 managed bytes. [The project selection contract](physics-backends.md#project-physics-selection) defines creation/reset and explicit-choice boundaries. General coupled geometry, other-platform and final performance acceptance remain open.

## Compound slot numerics

The public-only `--compound` fixture now retains every enabled circle/annulus slot, its local placement and original logical index; disabled slots do not renumber siblings. Common CPU/GPU/registered assertions exercise rotated scene shapes, empty gaps, disable/re-enable, ray/point/shape/contact/rest/cast and body-motion queries, scene/raw body and Area indices, automatic compound inertia, per-feature momentum/contact reporting, deepest same-peer caps and depth filtering, both CCD modes, Area effects and private restore. After 128 warmups, 128 full-frame/query/capture/restore cycles require 0/0 owner/all-thread managed bytes. The same-peer contact key includes both logical shape indices; private flat contact history preserves them. The numerical fixture remains bounded to circles/analytic annulus-circle pairs with uniform local scale; general polygon/capsule/compound-piece and coupled angular CCD acceptance remains open.

Built-in shape casts now target geometric boundaries in CPU conservative advancement and independent resident GPU query/motion sweeps, rather than a penetration-reduced solver target. Disk queries and linear-disk CCD share the CPU analytic fraction kernel; other represented proxies use geometric distance with 1e-5 scene-unit convergence tolerance. The internal CPU cast explicitly selects this query policy while native solver callers retain their existing policy. Sharp zero-radius contact retains the preceding finite normal. CastMotion keeps eight refinements: analytic first touch must lie between safe/unsafe with maximum width 1/256. The rejected disk 10/12 case returned .87109375/.875; the rejected rectangle .4 case returned .421875/.42578125. Ten circle/segment query combinations against rectangle, capsule, segment, convex and concave targets now verify rounded and sharp boundaries; each runs 128 warm queries at strict 0/0 managed bytes. Bit equality across backends is not required.

The final 2026-10-13 verification on main at 55429120 plus this change passes all ten frozen profiles: registered, actual no-GPU CPU/registered, GPU, complete consumer, six native physics/renderer combinations, ordinary full suite and both separate-process network renderer paths. Source/build/runtime fingerprints remain unchanged through build and execution. Local evidence is retained in `bin/physics-compound/2026-10-13/final-geometric/` (outcomes, bindings, runtime manifest, allocation/native logs, pixels and network reports). Release/API/wiki, formatting and production identity checks pass separately. This is scoped query/compound acceptance; general independent polygon/piece numerics, foreign platforms and final massive-scene performance remain open.

## Separate numerical scheduling

The [public-only threaded consumer](physics-thread.md) now exercises Solve and immediate status on a retained numerical lane, authorized readonly inputs, owner factories/publication/lifecycle, terminal failure/no-replay and 0/0 warm full frames. General geometry, GPU/native thread execution and final performance remain open.

## Angular compound motion

The public-only --angular-compound fixture checks the registered solver, CPU and
GPU through the same scene/raw API, in serial and --threaded modes. An asymmetric
two-circle body has a local mass center at (3, 0). Over 32 full frames at .01 s,
its world mass center follows linear velocity while the origin follows rotation.
Direct CenterOfMass returns a global-axis offset from that origin, not an absolute
position. Point velocity likewise interprets its argument as a global-axis origin
offset. The independent collider and direct-state factory now project these
contracts consistently; its dynamic integration advances the mass center.

Two radius-.25 circles at local x=40 and x=-40, with a disabled slot before them,
rotate at 30 rad/s for .02 s. A target on the intermediate .225-radian arc has clear
initial/final and quarter-step poses. The closest discrete surface gap is 2.4993
units, beyond the ordinary two-unit speculative contact margin. CastShape must
resolve the contact at logical slot 1 and
publish its full momentum-changing impulse. The discrete control misses that
arc; CastRay remains approximate and ordinary substeps may still encounter it.
A second case gives the raw target a translated, rotating offset circle while
its CCD remains Disabled. Both bodies must receive opposite momentum changes,
including the target's angular response, without changing that authored policy.
Each outer frame publishes one tick and one scene callback.

The independent coordinator now includes offset-center angular speed in candidate
intervals. Circular/ring trajectories use the same mass-center-relative rotation
as integration. Fixed-rotation casts retain their analytic entry/exit roots.
Curved casts advance against a current separating axis with a bounded angular
acceleration; an inner-hole bound also includes the linear distance curvature.
This avoids exhausting tiny speed-only advances after a tangent/separating
contact. Curved searches are bounded at 256 advances; the outer coordinator retains
its 4096-interval bound. Exhaustion fails explicitly;
remaining motion is not discarded or replayed through a built-in solver.

Mass-center/point-velocity tolerances are .002 scene unit or unit/s. Full frame
linear impulse must match momentum change within .02 kg-unit/s, and angular
contact must reduce the scene body's speed by at least 20 percent. These are
physical observations, not matching internal traversal or exact poses. Both
active contact cases run 128 warmup and 128 measured reset/full-frame cycles at
strict 0/0 owner/all-thread managed bytes. Logs are under
bin/physics-angular-public/2026-10-14/final3. All six profiles pass, with actual
backend/no-fallback checks and observed retained threads. The GPU implementation
executes on CPU-only llvmpipe Vulkan without NVIDIA. Its separated convex CCD
advance now bounds projected vertex velocity and angular acceleration against
the current separating axis, avoiding tiny global-speed-only progress at grazing
or separating arcs. The original .2-radian/20-unit fixture hit an ordinary GPU
speculative contact; clearing only discrete poses still did not clear that margin.
The corrected control preserves the strict no-contact assertion. It then exposed
a real GPU 256-advance exhaustion, fixed without raising either numerical budget.
The two GPU contact cases retain final angular speeds 15.000301 and 19.244148 rad/s,
exact logical slots and momentum tolerance. Native allocations, physical hardware,
arbitrary polygon/capsule/multiple-piece provider geometry and long coupled/network/
performance acceptance remain open. Curved custom-annulus checks follow below.

## Curved custom contacts

The public-only --curved-annulus fixture runs the independent registered solver in
serial and --threaded modes. A radius-.25 compound circle rotates from -.4 to .4
radians through an annulus with inner radius 4 and outer radius 5. Both endpoint
poses are clear. Discrete motion misses the transient contact; CastShape must
resolve either the inner or outer arc with exact nonzero logical indices, boundary
points and the correct impulse direction. A pin motor starting from rest supplies
the same inner arc while preserving its authored torque cap and bounded anchor.
A fourth case protects a stationary circle from a rotating offset annulus whose
own CCD setting remains Disabled. Both moving bodies receive opposite momentum
changes; the counterpart retains its configured mode.

These cases exposed an independent contact-response defect: full position recovery
can leave the recomputed pair just outside touching precision. Ignoring a false
geometry result replaced the valid contact point with zero, which corrupted both
its lever arm and the published point. The coordinator now starts with the valid
manifold point and replaces it only when the corrected geometry supplies another
valid point. Contact solving and frame publication use that same retained point.

Every case also captures its initial private history, solves the curved interval,
restores the discarded future and solves again. Restoration removes discarded
contact rows without another game callback; replay preserves the sampled motion
and impulses within .002 scene unit/unit/s/radian and .02 kg-unit/s. This cold
restore check does not measure warmed history cost or establish portable network
confirmation. The four reset/full-frame workloads separately run 128 warmup and
128 measured cycles at 0/0 owner/all-thread managed bytes. One callback and one
completed tick remain the outer publication boundary.

Boundary point and final geometry tolerances are .005 scene unit; frame linear
impulse agrees with momentum change within .02 kg-unit/s. The pinned case accepts
.1 scene-unit anchor error under the existing finite bias solver and verifies
that the final circle stays in the hole. It does not require an immovable origin
or a particular final angle when ordinary joint error permits motion. These
bounded custom checks pass through their independent algorithm; built-ins retain
their explicit rejection of unknown custom formats. General geometry, longer
coupled scenes, foreign/native execution and final performance remain open.

## Mass profiles across roles

The public-only --mass-roles fixture runs explicit CPU/GPU or the independent
registered solver, with --threaded selecting separate numerical execution. It checks both a
scene body and raw RID through Rigid, Static, Kinematic and RigidLinear transitions.
An asymmetric two-circle profile retains automatic center (3, 0); a custom center
(1, -2) likewise survives every role. Direct local/global-axis center offsets,
origin-relative point velocity and physical inverse mass/inertia remain coherent
before and after complete frames. Zero inverse dynamics does not erase resolved
center metadata under ADR 0073. Raw removal/reentry retains that profile, and both
logical circle slots remain visible in actual point queries throughout transitions.

The initial CPU measured bracket allocated 122880 owner/all-thread bytes over
128 four-role cycles. Phase diagnostics localized 61440 bytes each to Static and
RigidLinear configuration; queries and full frames themselves allocated zero.
Those transitions changed the internal fixture density and recreated unchanged
shape tags. The CPU adapter now treats density as an updateable property beside
materials, preserving native fixtures when slots, geometry, transforms, bias,
filters and sensor identity match. Its density setter defers automatic native
mass rebuilding; the owner reapplies the validated full engine mass profile.

The same 128-warm/128-measured bracket now requires 0/0 owner/all-thread bytes,
including 512 complete frames per bracket, two bodies, all role writes, direct
mass/point-velocity reads and two slot queries per body before/after each frame.
Center and point-velocity tolerance is .002 scene unit or unit/s; inverse values
use .0001 reciprocal tolerance. All six CPU/GPU/registered serial/separate profiles
now pass, including actual selection with no fallback. GPU execution uses CPU-only
llvmpipe Vulkan without NVIDIA; CPU/registered processes load no graphics driver.
Native allocation, physical hardware and wider platform/performance acceptance
remain open. Current evidence is retained under
bin/physics-public-mass-roles/2026-10-14. No public API or mass-policy change is introduced.

[Portable body roles](physics-snapshots.md#portable-body-roles) now transfer those
scene/raw profiles between CPU and explicitly selected CPU/GPU or the independent
registered numerical owner in both directions. Actual continued motion and one-interval pending force/torque
response, inverse values and logical-slot queries survive correction. The same
eight-case round trip runs serial/separate frames at strict 0/0 managed bytes;
this does not establish general collision geometry or complete network acceptance.

[Portable contact/motion history](physics-snapshots.md#portable-contact-and-motion-history)
now imports real CPU impulse/point records, scene body and raw Area observer pairs,
and character slides into the independent owner or a selected CPU/GPU world. Local peer identities and disabled/
enabled logical slots survive; malformed slot indices reject before state or events
change. Query-prepared character replay and complete serial/separate warm cycles
execute without managed allocation on CPU/CPU, CPU/GPU and CPU/registered pairs.
The related platform fixture also rejects malformed motion and invalid bindings
while preserving actual carried movement. Hardware, general geometry/joint and
full network acceptance remain open.

[Capture after topology edits](physics-snapshots.md#capture-after-topology-edits)
also guards the producer: invalid contact/overlap/slide references reject before
replacing a previous complete packet. Five real removal/clear cases on CPU and
registered owners pass in serial/separate modes, followed by successful history
refresh and recapture. This does not establish general topology correspondence.

[Same-count authoring validation](physics-snapshots.md#same-count-authoring-validation)
checks seven mismatched geometry/slot profiles in both CPU/registered transfer
directions and serial/separate execution. Rejection preserves complete destination
state and events; restoring all authored fields permits unchanged-packet correction,
real point queries and ordinary contact/Area continuation. Disabled rectangle data
is covered, not active rectangle simulation. Broader collider/object association lifetimes, arbitrary geometry and current
GPU/native acceptance remain open.

[Retained slot history](physics-snapshots.md#capture-after-topology-edits) now samples
private logical-slot lifetimes for contacts, overlaps and collided motion. Capture
rejects removal/reindex/readdition before replacing bytes while preserving sampled
public data and events. In-slot replacement stays valid. CPU/registered serial/
separate twelve-case verification executes; destination apply remaps tokens, which
never enter wire data. Current GPU/native and broader topology acceptance stay open.

[Character motion validation](physics-snapshots.md#character-motion-validation)
now rejects malformed completed-slide values and incompatible platform roles before
state/event mutation. CPU/registered serial/separate tests preserve complete packets
and world state, then exercise a valid body-platform correction and actual following
movement. This does not establish arbitrary platform geometry or GPU acceptance.

## Off-center spring anchors and portable joints

The independent consumer spring solver now applies forces at actual anchors with
opposite linear impulses and each body's angular moment. Axial damping includes
angular point velocity and effective inverse mass from both lever arms. Exponential
decay avoids the former explicit damping reversal; one scalar interval impulse cap
bounds elasticity plus damping. Static endpoints use zero numerical motion and
receive no dynamic impulse, matching the separate surface-velocity contract.

--spring-anchors compares CPU/CPU, CPU/GPU and CPU/registered scene/raw joint
identities and both endpoint orders in serial/separate modes. Actual selection
without fallback is asserted. A mass-2, inertia-2 body with a two-unit vertical
arm, 10-unit spring separation, rest 5 and stiffness 2 receives velocity -.005 X and
angular +.01 over .001s. Bounds .00005 unit/s and .0001 rad/s permit four-substep
integration drift. MaxForce=1 gives -.0005 X and +.001 rad/s with .00001/.00002 bounds.
With stiffness zero, damping 4 and initial angular speed 3, effective inverse mass
2.5 gives approximately +.01194 X and 2.97612 rad/s. Bounds .011.. .013 and
2.974..2.978 permit the small anchor rotation; total linear/rotational energy drops.
Damping 10000 removes axial point motion: +1.2 X and .6 rad/s within .002, with
complete kinetic energy below 2 rather than the initial 9. This rejects explicit
high-damping reversal. Eight configurations per thread profile execute all four responses. Springs require
two live bodies under the accepted contract; an empty second RID is not a world spring.

[Portable joint continuation](physics-snapshots.md#portable-joint-continuation) now
executes scene/raw pin/groove/spring transfer, local-frame correction, real response,
atomic policy mismatch, local replay and zero warm full-frame transfers on CPU/CPU,
CPU/GPU and CPU/registered pairs. All six serial/separate profiles retain six cases
and their original operation counts. GPU executes on CPU-only llvmpipe without
NVIDIA. These current checks extend the earlier independent-consumer numerical
proof; production runtime/API/ADRs/wire format remain unchanged. Wider joint graphs,
active geometry/contact coupling, hardware/native/network and performance gates
remain open. Evidence is retained under bin/physics-public-portable-joints/2026-10-14.

## Public rotating geometry verification (2026-10-14)

The public-only consumer --angular-geometry runs the same scene/raw fixture on
explicit CPU/GPU worlds, with serial and --threaded numerical policy. Four authored
geometries cover a 320 by .5 rectangle, radius-.25/height-320 rotated capsule,
a 12-vertex ellipse with semiaxes 160 and 4 (multiple native convex pieces), and
two 160 by .5 rectangles at local x=-80/+80 (separate logical slots).
Each body uses mass 2, inertia 20,000 and a custom zero local mass center.
A disabled slot precedes its active geometry; the raw radius-.25 target also has
a disabled slot before its active shape.

At 30 rad/s over .02 s, the active arm crosses a target at radius 160 and angle
.225 radians while initial/final and quarter-step poses remain clear. The Disabled
control retains zero reported contacts and unchanged angular speed within .01 rad/s.
CastRay remains an approximate finite-state control; it is not a full rotational
protection claim. CastShape must publish a real contact and reduce angular speed
below 24 rad/s without reversal below -1 rad/s. A second CastShape case moves and
rotates the target's offset collider, preserving its Disabled CCD policy.

Every active full frame checks its world tick and state callback once, exact RID
and nonzero logical shape indices (including native pieces sharing one logical index), full
contact impulse against body momentum within .02 kg-scene-unit/s, and equal opposite
momentum/angular response on the moving target. The momentum bound accounts for
float contact sums and velocity products at this scene scale, rather than requiring
equal CPU/GPU poses or contact counts. Static and moving target cases for all four
geometries each retain 128 warm/128 measured frames at strict 0/0 owner/all-thread
managed bytes: 1,024 measured active full frames per profile. Contact and momentum
checks execute inside every measured interval, not only on its final sample.

This covers the named public built-in trajectories. Arbitrary provider polygon/
capsule algorithms, larger coupled graphs, numerical-scale extremes, networking,
native allocations and hardware/window performance retain separate acceptance.
GPU kernels execute through CPU-only Vulkan; software timing is not hardware evidence.

## Inherited shape copying (2026-10-14)

Resource.CopyFromResource previously entered reset/change batching and assigned
base name/scene locality before Shape's policy copy discovered a forbidden
borrow. During a shape's own geometry supplier, private custom data could also
change before final geometry publication rejected it. Resource now invokes its
internal target preflight before retaining source file ownership or entering the
batch; Shape delegates to the existing complete geometry authoring guard.
Every application Shape inherits that dispatch without an extra public hook.

The public-only --shape-geometry consumer tests nine represented shape families
and one application Shape: circle, capsule, rectangle, segment, convex/concave
polygon, boundary, directed ray and custom geometry. Numerical creation/live/step
hooks, reentrant selected-status reads and geometry construction reject copying
before base/custom/reset/events/RID/cache change. Quiescent and post-solver copies
remain usable, preserve target path/scene ID and coalesce Changed; a real point
query observes the copied radius. Exact runtime-type/null/disposed/self-copy and
arbitrary post-preflight error policy remain owned by Resource.

--cpu, --gpu and registered profiles each support --threaded. The existing 128
warm/128 measured preflight/full-frame loop retains the strict zero owner/all
managed-byte gate; accepted copying is structural work outside that bracket.
This verifies inherited authoring safety and leaves complete arbitrary provider
geometry, native/platform/network and hardware performance acceptance open.

Standalone Shape.ResetState uses the same preflight before OnResetState. The public-only regression rejects reset during numerical/access/geometry hooks, checks no custom-reset side effects, and preserves allowed quiescent reset without Changed. Copying invokes reset exactly once after its own accepted preflight.

## Copied material revision (2026-10-14)

Resource.CopyFromResource used to assign all PhysicsMaterial values without
advancing its revision. If a preceding Changed observer threw, scene callbacks
were skipped and public bodies retained stale fixture coefficients and body-local
overrides. Copy now advances the target revision once, allowing the existing
authored-read/preparation polling path to recover. No extra subscriber delivery or
public API is introduced.

The --contact-response consumer includes four copied coefficient/modifier sets,
two shared scene borrowers, an unaffected contact peer, callback failure, archive
Replace/cache identity and analytic real bounce plus full impulse. It also checks
128 warm/128 measured contact-reset full frames at zero managed allocation. Both
thread policies execute on CPU/GPU/registered owners. This closes missed copy
publication rather than certifying arbitrary registered geometry or final native,
platform, network or hardware performance acceptance.

## World resource-copy preflight (2026-10-14)

The public-only --world-copies consumer checks early refusal of bound world
replacement, expired source retention, last-owner off-thread release and real
scene callback attempts without metadata/events/runtime mutation. Registered
status hooks additionally exercise exact borrowed release rejection. Copies
between bound aliases, ordinary reset, permitted unbound sharing and accepted
Changed failures retain their existing policies. Stale old space/map queries
prove final-owner retirement. A throwing provider Finish preserves the accepted
new runtime; disposal of both sharing wrappers proves balanced retention and
released source space/map. CPU/GPU/registered serial/separate profiles use
128 warm/128 measured real scene frames and resident point queries at strict
zero owner/all-thread allocation. Copies are structural work outside that bracket;
full inherited/provider geometry/native/platform/network/performance acceptance
remains open.


[Shape-pair operand borrowing](physics-shape-pairs.md#operand-borrowing-2026-10-14)
now protects both resources through standalone and selected callbacks, including
inherited copying/reset and owned RID replacement/retirement. Nested reads and
error cleanup retain their existing contract; full numerical/foreign/hardware
acceptance remains open.


## Borrowed motion output (2026-10-14)

A registered TestBodyMotion hook receives retained engine-owned output. Its physical
value getters and SetMotion/SetCollision are usable only during that hook on the
owning thread; consumer Dispose rejects during or after borrowing. Ordinary public
construction still creates independently writable/disposable caller snapshots.
The adapter resets and reuses scratch identity, copies completed values before
finally clearing the borrowed payload and thread scope, and permits later queries
after callback or invalid-output failures. Retirement releases scratch even after
Finish failure and aggregates its disposal errors with the earlier cleanup errors.

The public-only --motion-result-lifetime fixture checks all thirteen getters, both
fillers and disposal outside a hook and on a worker, normal owner access, exact
reused identity, failed/invalid output preservation, subsequent queries and scratch
retirement with simultaneous Finish/Disposed errors. CPU/GPU/registered serial and
separate profiles run 128 warm/128 measured complete frames with two body-motion
queries per frame at strict 0/0 owner/all-thread managed bytes. Real numerical motion
is 256 units within .02; circle first impact is 14 units within .2, covering eight
refinements over a 40-unit sweep (40/256 = .15625) plus float rounding. Completed
travel/remainder sum is within .001. GPU uses CPU-only Vulkan; hardware, native,
foreign, full extension/numerical and performance acceptance remain open.

## Completed numerical statistics

The public-only --statistics consumer uses the same complete scene on explicit
CPU/GPU worlds and an independent registered solver, in serial and separate numerical
modes. Counts describe actual completed work: awake nonstatic bodies (including
kinematic bodies), collision candidates (including sensors), and active dynamic
constraint groups containing contacts or joints. Static supports do not merge
independent dynamic groups; unconstrained bodies do not create islands.

The former registered fixture returned Active for Islands, omitted kinematic
activity and sensor pairs, and its lifecycle checks accepted those incorrect counts.
It now traverses its existing retained dynamic connection groups and separately
counts actual sensor shape pairs. Its circle/annulus numerical model remains bounded;
this change does not add rectangle or arbitrary geometry support. The shared count
scenario uses two-circle compound static support with real gravitational contacts,
an overlapping Area, a scene rigid body, a raw dynamic body, one awake kinematic
body and a pin. Kinematic CanSleep is explicitly false, avoiding the CPU's legitimate
quiet-body sleep during the settling interval.

Exact active/island expectations are 2/0 for isolated dynamics, 3/0 after kinematic
attachment, 3/2 for independent supported bodies and sensor overlap, 3/1 for the pin,
1/0 while dynamics sleep, and 3/1 after joint wake. Inactive spaces contribute 0/0;
reactivation retains the previous successful sample. A private checkpoint restores
that publication after a discarded sleeping future. All three Performance projections
match the cached process values, including a foreign-thread read. Candidate totals
need actual physical contacts and an increase after sensor attachment; backend
candidate construction/order is deliberately not compared as an equality gate.

The bridge now rejects negative fields and Islands greater than Active before
publication. The lifecycle fixture checks zero and nonzero Active overflow cases,
terminal statistics failure, previous-sample preservation and error cleanup. An
isolated baseline runtime failed this new refusal assertion: the bad sample was
accepted. The API's existing failed-world/no-replay policy is retained.

The first complete-frame/read trial reported 248/248 managed bytes after collection.
ProcessInfo's Enum.IsDefined rebuilt runtime enum metadata; bounded selector checks
now preserve all three accepted values and undefined-selector rejection without
that cache allocation. Both process and Performance reads, actual impulses and
full PhysicsFrame remain inside the measured interval: 128 warm and 128 measured
frames at strict 0/0 owner/all-thread bytes, with no allowance or discarded first read.

The final frozen six-process cohort is based on main 9b1da4e0, under
bin/physics-statistics/2026-10-14/final. CPU/provider processes have no display and
unavailable GPU/ICD; GPU uses explicitly selected CPU lavapipe with four driver
workers. Hardware NVIDIA is excluded. Native GPU, arbitrary provider geometry,
complete inherited/extension, network, native-memory and final performance acceptance
remain open. These are count/physical-workflow checks, not target GPU timing or FPS.

The cohort's 4,171 sorted source inputs fingerprint is
`5ef92e0671a46080b8cf8047589fd311a2e9afce7963cbfd2e9891c2f07f4586`;
loaded runtime/test-consumer DLLs are respectively
`3e1729fa105e1e147bd7cc6914e5d1bbb5b64a2b33ac73d3efc7a609f38035da` and
`3c4598fdbff2379cd11368082184ea1928bbc25714a596f2cd447b0b79ff56fd`.
All 61 isolated runtime files remain unchanged. Subsequent consumer README and
coverage metadata edits are documented separately; no compiled input changed.
Process proofs observe exactly zero/one retained numerical thread for serial/separate,
lavapipe only for GPU and no NVIDIA driver. The lifecycle regression and complete
default registered consumer also return zero on that same frozen runtime.

## Registered rectangle numerics

The independent public-only implementation now retains each enabled rectangle's
actual transformed half-edges and logical slot. Circle/annulus paths remain active.
Rectangle/circle and rectangle/rectangle contacts use separating axes, nearest
rounded-corner axes and clipped support ranges. Areas, point/ray queries, all four
shape-query families, body motion and selected shape-pair dispatch use those same
contours. The existing shared unit-scale/zero-skew physics-pose precondition is
retained; dimensions come from RectangleShape.Size. No built-in world or numerical
solver is invoked by this implementation.

Mass combines actual rectangle area and centroidal moment with existing circular
areas and parallel-axis offsets. Standalone pair dispatch tests independent swept
convex regions, including rectangles whose synchronized trajectories never meet.
Custom annulus/rectangle tests retain the hollow region and its inner boundary.
Continuous rectangle motion follows relative linear and angular motion. A fixed
separating-axis certificate considers each vertex-pair gap, closing speed and a
bound on rotational acceleration; it avoids exhausting global-speed microsteps
near tangency without skipping a possible support change. Explicit advancement
and interval-budget failures remain errors, not a fallback or silent miss.

The --rectangle-geometry consumer shares CPU/GPU/registered scenes in serial and
separate numerical modes. It verifies rotated/offset thin rectangles and disabled
index gaps, point misses within the long-side envelope, physical ray points/normals,
circle and rectangle casts, body motion, real contact pairs, deepest rest selection,
exclusions and masks. A compound rectangle/circle body checks area-weighted center
and inertia. Elastic rectangle/circle and rectangle/rectangle contacts run all
three CCD modes with actual scene contact monitoring and private replay. Entry
fires once per replayed frame; restore itself remains silent. Full step impulses
must explain both bodies' velocity changes. A corner contact exercises angular
leverage and inelastic energy; thin-wall and rotational-bar controls exercise
translational and angular CastShape response. Registered annulus checks verify
that all completed rectangle corners remain within the inner-boundary allowance,
with actual contacts and no added kinetic energy; tangential motion is permitted.

Each query/frame and six contact/replay cases plus the angular-contact case warm
128 intervals and measure another 128 at strict 0/0 owner/all-thread managed bytes.
Point/ray/rest coordinates permit .002 scene units, cast fractions 1/256 plus .00002
for the shared eight-refinement bracket, mass/center reads .0002, paired momentum
.002 and full impulse .02–.03. Angular moment comparison permits .005 plus step
duration × maximum before/after linear speed × total impulse magnitude: published
contact coordinates may precede final pose integration. This is a bound on the
lever's sampling interval, not a subtracted allocation allowance. The angular
corner runs at 1/2048 s with positive angular response and energy at most 400.1;
the rotating 320×.5 bar at 30 rad/s uses .015 s and a 102-percent energy ceiling.
The custom ring's radius limit is inner radius 5 plus the accepted .3 contact
allowance and .001 float slack; its energy ceiling is initial 50 plus .1.

The isolated baseline probe calls the old public statistics scenario to register
its real factory, then attaches a rectangle through ordinary public scene API.
The preserved baseline consumer rejects it in primitive decoding; the candidate
executes it. Evidence is under bin/physics-rectangle-provider/2026-10-14, with the
final source/runtime manifest and serial/separate process proofs in final2.
Software Vulkan uses explicitly pinned CPU lavapipe and excludes NVIDIA hardware.
No target-GPU advantage or real-window FPS is inferred. Capsule evidence is recorded
below; general segment/polygon/boundary/directed-ray numerical integration, arbitrary custom algorithms,
complete extension/inherited/network/native-memory and platform gates stay open.

Final evidence is based on main `3e948e73aea113d80389544dba624f931b72e662` and
this consumer slice. The 4,180 source inputs fingerprint is
`46dbbd710bcc502aafc236e6e88abd73e4e07b836942e0e4745f17065f4a3875`;
loaded runtime/consumer DLLs are respectively
`aa4b0dedab294af9ac4159ead2e36cfd6b76a718b6b4e2ff3d911771c67b4b5c` and
`9a86c6e425429c9402db4df6144c5223de34dae428cbcdfcfcf86e7fcb1126b2`.
The final2 cohort checks six numerical profiles plus default registered, custom,
curved-annulus and statistics regressions. All process/file proofs are retained;
only noncompiled README/coverage evidence changes afterward. Real rectangle Area
monitoring has one entry transition and repeated overlap/point reads inside the
query/frame zero-byte bracket. Production API and geometry-policy decisions are
unchanged; existing coverage states remain bounded by broader family acceptance.

Deepest-selection/mask/exclusion topology changes, standalone pairs, the thin-wall/
rotational-bar controls and the custom ring are separate functional controls.
Their construction/first-use costs are not labelled allocation-free. The warmed
brackets include all six direct query reads or complete contact/replay frames;
Area overlap and point filtering are also inside the measured query bracket.

## Registered capsule numerics

The independent application solver now reads Capsule geometry as its transformed
cap-center segment plus its actual round radius. Circle/annulus and rectangle
behavior is retained; capsules are never substituted with circumscribed disks.
A common contour path projects the segment or rectangle core plus the radius,
checks edge normals and both vertex-to-edge rounded-corner axes, clips contact
supports and uses separating-axis advancement for linear and angular motion.
The latter retains the vertex-pair speed/acceleration certificates, with constant
round radius and moving core vertices. Standalone pair queries project both swept
regions independently; their collision does not require synchronous trajectories.
The custom annulus retains its hollow inner boundary using farthest core vertices
plus the counterpart radius. Inner-boundary linear advancement uses the bounded
translation speed rather than a convex separating-axis closing certificate: zero
initial radial speed can still close later. A reproduced RED query missed that
crossing before this correction. Both elongated and disk-limit capsules now cast
from the hole center toward its inner wall, including all four shape-query families. Ray queries use actual straight sides and radial
end caps, including the disk limit, rather than a bounding box.

For capsule radius r and half middle height h, area is 4rh + pi r². The polar area
integral is 4rh(r²+h²)/3 + pi r²(r²/2+h²) + 8hr³/3. Dividing by area gives the
mass-normalized moment; compound center and inertia retain area weighting and
parallel-axis terms. Scene/raw slots preserve offsets, local rotation and disabled
logical indices. Query margin expands the round radius without replacing the
segment or losing the original radius. Live edits retire and rebuild that core.
Zero middle height is a disk; zero radius retains a segment in standalone pairs.
Zero-area dynamic numerical acceptance is not established by that pair check.

The public-only --capsule-geometry profile (optional --cpu/--gpu and --threaded)
uses rotated/offset body and Area capsules, one disabled slot, point/ray queries,
all four shape-query families and real body motion. Axial, straight-side and
oblique-cap ray points/normals, margin cast, deepest rest, masks and exclusions
are verified. Six actual capsule/circle or capsule/capsule contact cases cover
Disabled, CastRay and CastShape. Each restores private state silently, publishes
one real contact entry, conserves paired momentum and accounts for the entire
step impulse. An unlocked oblique cap contact checks angular impulse and energy.
One query/full-frame bracket plus those seven contact/replay brackets warm 128
frames and measure 128 more at strict 0/0 owner/all-thread managed bytes. No query,
restore, notification or full-frame operation is subtracted from that interval.

Cold controls check the compound analytic center/inertia, disk mass and query
limit, live disk/segment edits, zero-radius standalone segment pairs, separated
parallel capsules whose circumscribed disks overlap, independently swept regions,
rounded misses/hits, a 1000-unit/s capsule at a thin wall, a 320-height rotating
capsule's intermediate CastShape arc and the registered custom-ring inner wall.
Those controls are functional checks outside the warmed allocation brackets.
Poses retain the shared unit-scale/zero-skew contract.

Numeric bounds are .005 scene units for round ray/rest coordinates (one hundredth
of the CPU solver's .5 scene-unit linear tolerance), normal dot product greater
than .999, cast fraction 1/256 plus .00002, mass/center .0002, paired momentum .002
and total impulse .02–.03. The ray candidate originally rejected a CPU endpoint
error of .00339 under the rectangle's .002 bound; the round bound is explicit.
The thin-wall center must remain at most 8.75 + .501 with residual horizontal
speed below .1: .5 is the accepted CPU linear tolerance and .001 is float slack.
The first rectangle-specific bound 9.1 rejected an actually stopped CPU capsule
at 9.134204; this is not claimed as exact boundary placement. Angular moment
uses .005 plus interval × maximum linear speed × impulse magnitude because
published contacts may precede final pose integration. Oblique contact runs at
1/2048 s with negative angular response and energy at most 400.1. The 30-rad/s
rotating capsule uses .015 s and at most 102 percent initial energy; ring controls
bound every actual cap by inner radius 5 + .3 contact allowance + .001 float slack
and kinetic energy by 50.1.

The frozen baseline from the rectangle cohort rejects Capsule attachment in the
real registered factory's primitive decoding. The new consumer executes it without
engine internals or built-in solver delegation. Final evidence resides under
bin/physics-capsule-provider/2026-10-14; explicitly pinned CPU lavapipe provides
functional GPU evidence only. General segment/polygon/boundary/directed-ray
integration, arbitrary custom algorithms, complete extension/inherited/network/
native-memory/platform and target-GPU performance gates remain open. Production
API/XML and geometry-policy ADRs are unchanged; related coverage rows retain their
current Partial states.

Final2 evidence uses main `13609c1afe45a8806d40e0871c8952bdf9fc8890` and this consumer slice. The 4,182
source inputs fingerprint is `2d4a5a5024d292adfcd87de585b1030766aacfd53da89ea744c8aadf346dd049`; loaded runtime/consumer DLLs
are `cd57fff3cdca74f2fcb4e4bedcca476db9d799396a49edd06f95114bd10ceea6` and `9a6d38d6a5983356acedcd6412e5b11ee111915740f32ec7718980a4e9f9d7dd`.
All 61 runtime-file identities stay unchanged through the cohort. Six independent
fresh numerical processes (CPU/GPU/registered × serial/separate) pass with eight
strict warmed brackets each. The GPU processes load lavapipe only; CPU/provider
processes have no graphical device, and exact retained solver-lane counts are
0/1. Seven additional default/custom/curved/statistics/rectangle/compound processes
pass. Source-before/source-after fingerprints agree; the precommit runtime build
matches the loaded DLL. Independent processes share the host concurrently for
functional checks; this is not a latency or hardware-performance measurement.
Evidence manifests/results are under bin/physics-capsule-provider/2026-10-14/final2,
with six precommit gates under gates2. Production API remains 13,835 declarations
with 10,583 mappings, 3,252 reviewed extras and zero unmapped; wiki check covers
820 public types and 838 pages with zero updates/removals.

The Final2 receipt is the frozen pre-canvas-modulation cohort, not verification of
the later integrated main. Concurrent commit d8036b6ae8933c66d3e1c194e98d07448c9ebc4a
changed runtime/coverage inputs before the capsule commit. The precommit source
comparison detected that change, but the shell sequence incorrectly continued to
commit. History is retained: the implementation is 73a377557b0e7ea63961db066460314a1cc61323,
followed by complete current-main revalidation and this evidence correction.

Final3 now verifies integrated main `73a377557b0e7ea63961db066460314a1cc61323` with all 13 fresh processes and
all six commit gates passing again. Its 4,184 source inputs fingerprint is
`fed6b45f535d3539b8874f1ce8d22cb1ac5c2e155551289b53ce1b5fc39f6518`; runtime/consumer DLLs are
`4716f42e5343d9c03be6fb5a05dcae3497d1262bde86ab5a9cab27cd1e397e64` and `4595175250c4e25d2d879ffe0ba8f1847c2a49391ae7993633a7007de7cbd5a3`.
All 61 runtime-file identities and source-before/source-after identities agree,
and the runtime built by the commit gates matches the loaded DLL. CPU/provider
remain device-free, GPU loads only CPU lavapipe, and serial/separate solver-lane
counts are exactly 0/1. Every warmed bracket still measures strict 0/0 managed
bytes. Final3 process/files/results and gates3 logs are the authoritative integrated
receipts. The compiled API now has 13,842 declarations, 10,585 mappings, 3,257
reviewed extras and zero unmapped; wiki verification has 821 public types and
839 pages with zero updates/removals. The changed counts belong to the preserved
canvas-modulation integration, not a physics API addition.

## Registered segment numerics

The independent solver reads immutable Segment endpoints into an exact transformed
midpoint and half-edge. The contour path uses zero rounding radius; no thick disk
substitution is introduced. Edge/support projections, circle/rounded endpoint axes,
actual two-sided ray intersections, shape contacts, body motion and linear/angular
CCD share the existing contour machinery. Collinear segment rays have no transverse
crossing. Ordinary line/Area point queries have no filled interior, matching the
[pinned contains_point implementation](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/godot_physics_2d/godot_shape_2d.cpp).
The initial point oracle incorrectly expected a filled segment; the reference and
CPU implementation both reject that expectation. Point-query filtering still
resolves an actual circle peer, independently of line containment.

Attached segments at or below .5 scene-unit length use the accepted zero-radius
midpoint fixture under ADR 0061. Source endpoints and standalone collision regions
remain exact. A transverse ray .08 from a .2-length segment midpoint misses the
point fixture, whereas a circle shape at the midpoint still queries it. A live
coincident-endpoint edit preserves a valid shifted point. An initial microscopic
shape-overlap miss oracle was unsuitable under CPU query tolerance and was replaced
by this geometric transverse ray check, not by a looser success threshold.

For bodies with positive-area shapes, area weighting owns center/inertia and rods
contribute zero area. If all shapes have zero area, rod lengths own weights and
moments use length²/12 plus the parallel-axis term. Zero-radius capsules participate
in that rod fallback. No shape or only coincident points gives zero automatic
inertia while retaining requested mass; explicit positive inertia still overrides.
The former .001 automatic inertia floor is removed, and torque integration uses
guarded inverse inertia so zero no longer produces division by zero. Segment and
zero-radius contour CCD use a nonzero .5 interval-planning scale; it does not add
physical thickness or change the actual collision boundary.

The public-only --segment-geometry profile (optional --cpu/--gpu and --threaded)
checks one rotated/offset static line with disabled slot zero, a real segment Area
sensor and separate circle peer, all six query families, body motion, deepest rest,
masks and exclusions. Six segment/circle or segment/rectangle contact cases cover
Disabled, CastRay and CastShape with one contact entry per restored frame, full
impulse accounting and paired momentum. An unlocked off-center line contact checks
angular moment and energy. Those eight brackets warm 128 complete frames each,
then measure 128 at strict 0/0 owner/all-thread managed bytes, including queries,
Area publication, restore and contact effects. Cold controls verify compound rod
mass/torque, solid-plus-rod mass precedence, unshaped finite impulse response,
collinear/opposite-side rays, short/coincident live limits, independent swept
segment/circle regions, false circumscribed contacts, a thin wall and an intermediate
rotational CastShape arc. Their work is outside warmed allocation measurement.

The line's contact setup uses transverse extent: cold Disabled centers start at
.475 and continuous centers at 2.1, giving an actual collision within .4 seconds.
The earlier rectangle-specific 3.1 spacing never reached this line/circle pair in
that interval and was corrected without weakening the impulse/event assertions.
Unshaped impulse checks explicitly replace world damping with zero; they do not
interpret ordinary damping as a mass defect. Position/ray/rest bounds are .002
scene units, normals dot above .999, cast fraction 1/256 plus .00002, mass/center
.0002, momentum .002 and full impulses .02–.03. Angular moment permits .005 plus
interval × maximum linear speed × impulse magnitude for publication/integration
lever sampling. The .015-second, 30-rad/s rotating 320-length line retains at most
102 percent initial energy. Thin-wall center stays below 9.75+.501 with residual
horizontal speed below .1; .5 is accepted CPU linear tolerance and .001 float slack.
The shared unit-scale/zero-skew pose contract is unchanged.

The retained previous capsule consumer rejects Segment attachment in real
primitive decoding; this provider executes it through public APIs only. Evidence
is under bin/physics-segment-provider/2026-10-14. Explicit CPU lavapipe provides
functional GPU proof, with no target-device/native-memory/performance inference.
General convex/concave/boundary/directed-ray integration, arbitrary custom algorithms,
complete extension/inherited/network/platform and target-GPU acceptance remain open.
Production declarations/XML and the accepted geometry/mass ADRs are unchanged;
coverage reasons expand while their current Partial states remain.

Final evidence uses main `9d55d1b1cf9f89f1bb95b83280d500b79563fab4` and this consumer slice. Its 4,185
source inputs fingerprint is `15e5b39f558c0707496c87d28dc481273beb7eb04a9d889450d74d21e6b1e147`; loaded runtime/consumer DLLs
are `f5fa8921e0f5c4eca8c430afac7ec35228f2d9993813f8651f1ed0b1c2c49cff` and `8cd6404166454fd6e00d5b988f26c3ed84716066b25c7131f3b5bafe104e134d`.
The six CPU/GPU/registered serial/separate processes and nine default/custom/curved/
statistics/rectangle/compound/capsule/joint regressions all exit zero. Source-before/
source-after agree, all 61 runtime-file identities stay unchanged, and the precommit
runtime build matches the loaded DLL. GPU processes load CPU lavapipe only and no
NVIDIA library; CPU/provider use unavailable graphical drivers. Retained solver-lane
counts are exactly 0/1. Independent processes run concurrently for functional proof,
not timing or native-memory measurement. Results/manifests are under
bin/physics-segment-provider/2026-10-14/final; the six commit gates pass under gates.
API counts remain 13,842 declarations, 10,585 mapped, 3,257 reviewed extras and zero
unmapped; wiki check has 821 public types, 839 files and zero updates/removals.
The pinned shape source digest is `a63404db6e58924946ad9dc0848a54dd95ff4678df5c0f661951cbd6a6f3681d`.

## Registered convex numerics

The independent public-only solver now reads the complete accepted ConvexPolygon
perimeter. It normalizes winding and a repeated closing vertex, prepares immutable
centered vertices and exact mass samples once, and weakly caches those samples by
immutable ShapeGeometry identity. There is no eight-vertex numerical limit, native
piece copy, circumscribed disk or rectangular substitution. Empty contours retire
all numerical fixtures while retaining the authored logical slot and resource.
Local/global poses retain the shared unit-scale/zero-skew contract.

Area, centroid and polar integral use double accumulators around the first input
vertex, avoiding subtraction of large origin-dependent moments. The final polar
moment is central; compound mass retains area weighting and parallel-axis terms.
Rotation/offset preserve that centroid. Polygon projection has distinct lower and
upper bounds rather than a symmetric radius. Every edge contributes separating
axes; rounded partners add both vertex-to-edge directions. Support clipping
selects actual extreme vertices. Point containment and ray half-space clipping use
the real facets. Query margin expands the round offset without replacing the core.
Hollow custom-ring tests retain the outer support and farthest-vertex inner rule.

Angular CCD now streams every core vertex into the existing speed/acceleration
certificate rather than storing four corners. Standalone independent sweeps test
all candidate hull chords and rounded vertex-to-chord axes with streamed support
bounds; no array or fixed-axis capacity is allocated per warmed query. Candidate
chords include every hull edge and remain valid projection certificates. This
correctness-oriented consumer is quadratic/cubic in vertex count in those scans;
large-contour throughput is not established by its functional check. Final target
GPU performance remains an independent requirement of the engine's resident path.

The --convex-geometry profile (optional --cpu/--gpu and --threaded) uses an actual
12-vertex 120×15 contour in the full query/Area frame bracket, including one disabled
slot, real overlap transition, six query families, body motion, deepest rest,
mask/exclusion checks and a bounds-corner point outside the actual facets. A
12-vertex 320×40 contour rotates through an intermediate CastShape contact at
30 rad/s over .015 seconds. Four-vertex contours supply six actual polygon/circle
or polygon/polygon contact/replay cases for all CCD modes, plus an unlocked corner
impulse/energy case. Each of those eight brackets warms 128 complete frames and
measures 128 at strict 0/0 owner/all-thread managed bytes. The 12-vertex rotational
control is cold functional evidence, not a separate warmed allocation claim.

Cold mass controls use a 60×15 four-vertex contour plus a radius-five circle with
area weighting and an independently computed box moment. A translated right
triangle with legs six and three has centroid (6,-6), including its authoring
translation, and mass-three central inertia 7.5. Child rotation/offset, reversed
winding, repeated closing vertex, true interior versus bounding-box miss, ray face,
live empty removal and subsequent identity-preserving replacement are exercised.
Standalone independent regions, rounded misses and registered hollow-ring response
are additional cold controls. Existing rectangle/capsule/segment/compound/joint/
private-history profiles remain regression checks.

Initial twelve-entry small fixtures contained collinear vertices and fell below
accepted CPU polygon-piece size tolerance. They were rejected during authoring,
not used as successful twelve-vertex evidence. The final twelve-vertex contours
are noncollinear and large enough for actual native fixture partitioning. Smaller
four-vertex checks stay above native welding tolerance; the thin wall remains an
ordinary RectangleShape counterpart. Numeric bounds remain .002 scene units for
point/ray/rest coordinates, normal dot above .999, cast fraction 1/256 plus .00002,
centroid .0002, central inertia .0002 relative for the large compound sample and
.0002 absolute for the analytic triangle, momentum .002 and full impulses .02–.03.
Angular moment permits .005 plus interval × maximum linear speed × total impulse
for publication/integration lever sampling. The rotating contour retains at most
102 percent initial energy; the 2.2-wide moving polygon at the thin wall must keep
center below 8.65+.501 and residual horizontal speed below .1. That allowance uses
accepted .5 CPU linear tolerance plus .001 float slack. Custom-ring radius/energy
controls retain their prior 5.301 and 50.1 bounds, using the actual 2.2-square corners
and the reported automatic inertia.

The frozen previous segment consumer rejects convex attachment in real primitive
decoding; the new solver executes it without built-in numerical delegation or
engine internals. Evidence is under bin/physics-convex-provider/2026-10-14. CPU
lavapipe is functional GPU evidence only. General concave/boundary/directed-ray,
arbitrary custom/coupled geometry, complete extension/inherited/network/native/
platform and target-device performance acceptance remain open. Production API/XML
and the accepted geometry-policy ADRs are unchanged; coverage reasons expand while
current Partial states are retained.

Final evidence uses main `f2a681ed3e7da99aa1bf9e7f10d0f95e1721cf67` and this consumer slice. Its 4,187
source inputs fingerprint is `cd1bfc173f0d9c1e34552741316742da855c12796eb5ad227718c64c4c8aefb3`; runtime/consumer DLLs are
`26ed5c96a2c0851688d95afdbf6cf2b313571828855d6f2ba8b390931b7d6fc7` and `10ce74c53a5f322ee7dd9e1fdd8506d5ed6ee938547f70ca334417225d9e32c8`.
All 17 fresh processes pass: six CPU/GPU/registered serial/separate numerical
profiles and eleven default/custom/curved/statistics/rectangle/compound/capsule/
segment/joint/private-history regressions. Source-before/source-after identities
agree; all 61 runtime-file identities stay unchanged and the precommit runtime
matches the loaded DLL. GPU loads CPU lavapipe only, no NVIDIA library; CPU/provider
have unavailable graphics drivers and solver-lane counts are exactly 0/1. Eight
warmed brackets per numerical profile still measure strict 0/0 managed bytes.
Independent processes share the host concurrently for functional proof only.
Results/manifests are under bin/physics-convex-provider/2026-10-14/final; all six
commit gates pass under gates. API stays at 13,842 declarations, 10,585 mapped,
3,257 reviewed extras and zero unmapped; wiki verifies 821 public types and 839
files with zero updates/removals.

## Registered concave numerics

The independent public-only solver now reads every paired ConcavePolygon endpoint
into a numerical edge while retaining one authored logical slot. Storage capacity
is prepared for the enabled edge count before numerical work; repeated preparation
reuses arrays. Edge/point geometry keeps the accepted .5 scene-unit midpoint
fallback and the resource remains hollow, including closed bounds. Empty input
retires all leaves. Existing length-weighted zero-area mass and parallel-axis
moments apply across leaves and slots; positive-area shapes still own mass first.

Point/ray/shape/body-motion paths read actual edges. Shape query results deduplicate
RID/shape index, while contact query output retains individual edge pairs. Rest
collection now carries the exact prepared source and collider parts alongside
published pairs rather than resolving a physical leaf from a nonunique logical
index. Concave geometry can also be the direct-query source, including earliest
motion, multiple input contacts and empty-source results. Numerical contact history
stores local/peer part indices internally so two features of one public shape pair
retain distinct normals, points and full impulses through synchronization and
private replay. Those indices never enter the public result or wire identity.
Area overlaps deduplicate logical slot pairs independently of caller output
capacity, including the count-only read. Distinct features retain numerical pair
statistics while object/shape notifications stay singular.

Standalone collection ignores concave motion in either operand and excludes two
concave resources under ADR 0069. It evaluates each real edge against the convex
counterpart's independently swept region, preserving output operand order and the
supplied pair capacity. When capacity fills it retains deeper pairs, including
later edges. The existing custom-annulus callback now handles concave counterparts
through this same independent algorithm, retaining the annulus hole and motion
rules. No body bounds, filled hull or built-in numerical delegation substitutes
for concave material.

The --concave-geometry profile (optional --cpu/--gpu and --threaded) uses a rotated/
offset closed square with disabled slot zero, a matching line Area and a circle
at a two-edge corner. Six query families, nearest ray, body motion, masks/exclusions,
deepest rest and one logical Area entry are exercised every full query frame.
Moving the circle inside the bounds ends overlap; returning to the corner delivers
one new transition. A separate two-edge dynamic corner retains both axis normals,
logical slot one in every payload, one body/shape entry, silent checkpoint restore
and complete stopped momentum. Six concave/circle or concave/rectangle contact
cases cover all CCD modes and full impulses; an unlocked offset-rod case checks
angular moment and energy. These nine brackets each warm 128 complete frames and
measure 128 at strict 0/0 owner/all-thread managed bytes. No query, Area publication,
restore or contact delivery is removed from measurement.

Cold controls verify rod centroid/inertia across two edges of one slot, mixed solid
mass precedence, collinear/opposite-side rays, short/coincident/empty live edits,
identity-preserving replacement, concave query input and empty input, independently
swept resource motion rules, two-concave exclusion, 20 reversed-depth input edges
retained into four output pairs and custom callback hole/material behavior. The
1000-unit/s thin-wall and intermediate 30-rad/s rotating 320-length edge cases
verify actual linear/angular CCD; these are cold functional checks, not extra
warmed-allocation or hardware-performance claims. Existing primitive/convex/
compound/joint/history/contact-frame profiles remain regressions.

Ordinary ray/rest coordinates permit .002 scene units, normals dot above .999,
cast fraction 1/256 plus .00002, centroid/inertia .0002, paired momentum .002 and
full impulse .02–.03. The cold deepest corner point permits .01, two percent of the
accepted .5 CPU linear tolerance: a CPU point error of .008726 initially rejected
the stricter .002 ordinary face bound despite selecting the correct edge/normal.
Its first oracle also placed the circle beyond both endpoints, where the physical
normal is radial; the final inside-corner input has distinct edge penetrations
and a genuinely axis-aligned deepest face. Neither correction weakens event,
feature identity or full impulse assertions. Angular moment retains the sample-
interval bound, rotating energy stays at most 102 percent initial and thin-wall
position retains the .501 native-tolerance/float allowance. Poses keep the shared
unit-scale/zero-skew contract.

The frozen previous convex consumer rejects concave attachment in primitive
decoding; this provider executes the full exercised edge path through public
API only. Evidence is under bin/physics-concave-provider/2026-10-14. CPU lavapipe
provides functional GPU proof only. Large-edge-count throughput, arbitrary custom/
coupled geometry, boundary/directed ray, complete extension/inherited/network/
native/platform and target-device performance acceptance remain open. Production
public API/XML and accepted geometry/mass ADRs are unchanged; current coverage
Partial states remain while numerical evidence expands.

Final evidence uses integrated main `18db210fd00ce3364fa8d03fab837478be1cd946` and this consumer slice,
preserving the concurrent foldable GUI change. Its 4,197 source inputs
fingerprint is `6470018996ca03c457bc81f96e1b4874d0a57103974985908b1727ef99b177ae`; loaded runtime/consumer DLLs are
`902512f07a97f0ade7724d882a8101ec5bb16ee0f9a80e94e9935d504c1f15ef` and `c449809c0a6ac0a56ea230b2dcccf551cc170fb52b00350ac98d13ecc1d38fee`.
All 19 fresh processes pass: six CPU/GPU/registered serial/separate numerical
profiles and thirteen previous primitive/custom/convex/compound/joint/history/
contact-frame regressions. Source-before/source-after identities agree, all 61
runtime-file identities stay unchanged, and the precommit runtime matches the
loaded DLL. GPU loads CPU lavapipe only with no NVIDIA library; CPU/provider have
unavailable graphics drivers and retained solver lanes are exactly 0/1. Nine
warmed brackets per numerical profile still measure strict 0/0 managed bytes.
Independent processes share the host concurrently for functional proof only.
Results/manifests are under bin/physics-concave-provider/2026-10-14/final; all six
commit gates pass under gates. The integrated API has 13,878 declarations,
10,604 mapped, 3,274 reviewed extras and zero unmapped; wiki verifies 823 public
types and 841 files with zero updates/removals. These surface-count changes belong
to the preserved GUI integration, not a new physics API.


<a id="registered-boundary-numerics"></a>
## Registered infinite-boundary numerics

The public-only consumer now prepares WorldBoundaryShape as a normalized plane
normal and a transformed point, never as its finite editing bounds. Point/ray,
all six direct-space query families, body motion, Area overlap and solved contacts
route through analytic support separation. Plane/plane pairs are empty. The
boundary has zero geometric area, moment and rod weight; explicit body inertia
and positive-area companions retain their mass roles. Live normal/distance edits,
disable, replacement and disposal update borrowed geometry without changing the
body RID. Empty contours remain empty when paired with a plane.

Standalone resource collection keeps ADR 0069 precedence: ignore boundary motion,
test finite or concave counterparts at their motion endpoint, reject exact touching
without penetration and retain both ends of a support face in caller/other order.
The independent custom-annulus callback uses the same plane algorithm. Direct
shape casts use actual motion and ignore initial penetration; a boundary can also
be the query source, with margin shifting its plane outward. Prepared geometry
and reusable query/contact buffers allocate no temporary resources per frame.

Continuous simulation evaluates finite support against translating/rotating
planes. Candidate speed includes a rotating plane's distance to the finite
counterpart, because a plane has no finite radius. It conservatively advances the
analytic gap, preserving the existing iteration/error budgets and material/impulse
solver. The consumer reads captured sleep thresholds, solver iterations and
constraint bias through public getters at Initialize; the former zero-initialized
sleep policy prevented an otherwise resting boundary body from sleeping.

The common --boundary-geometry profile accepts --cpu/--gpu and --threaded. A
rotated/offset nonunit plane at tangent distance 6,000 tests exact point/ray/rest,
shape casts, logical slot one after disabled slot zero, body motion and one Area
entry. An independent corner retains two plane normals and shape indices under
one object entry. Six finite families (circle, rectangle, capsule, segment, convex
and concave) exercise Disabled/CastRay/CastShape, friction/restitution, constant
static surface velocity, complete normal/tangent frame impulses, exact contact
payloads and silent checkpoint restore. These twenty full-frame brackets each
warm 128 frames and measure 128 with strict 0/0 owner/all-thread managed bytes.
Cold controls cover infinite sensing at 4,000,000, reverse/source queries, margins,
inside-ray policy, exclusions/masks, mixed mass, sleeping/removal, two-support
standalone contacts, custom callback, 1,000-unit/s translation, rotational finite
support and a dynamically rotating plane. They do not add warmed or performance
claims beyond the measured brackets.

This profile exposed an existing CPU authoring conversion error: an approximate
rotation changed .37 rad to about .3712164 rad, shifting a plane roughly 7.3 units
at tangent distance 6,000. PhysicsShapeBackend.RotationToBackend now uses ordinary
sine/cosine for initial body poses, pose/kinematic target edits, query/body-motion
poses and joint frame angles. Local fixture rotation already used that conversion.
The backend's private integration remains unchanged; no reference-internal
identity or cross-backend bitwise equality is required. The far rotated query
keeps .005-unit coordinates, normal dot above .999 and 1/256 + .00005 cast bounds.
Mass uses .0002, frame impulse .03, rotation energy at most 102 percent initial,
and rotating-plane penetration allows .5 solver slack plus .25 advancement.
The authoring error is corrected rather than included in a looser tolerance.

Evidence is under bin/physics-boundary-provider/2026-10-14. Graphics-disabled CPU
and registered processes and CPU lavapipe Vulkan provide functional proof only.
Directed ray, arbitrary custom/coupled geometry, complete extension/inherited/
network/native/platform and target-device performance acceptance remain open.
No public API, XML surface or accepted architectural decision changes; existing
Partial coverage states remain while numerical evidence expands.


For resident CCD against a rotating plane, resting-contact advancement now bounds
the actual plane-support acceleration and its third derivative, with a bounded
fourth-derivative remainder over the remaining interval. A convex quadratic bound
uses its endpoint maximum across all finite support vertices. The earlier
radius-only curvature overestimate exhausted 128 intervals for a radius-one circle
near a 20-rad/s dynamic plane despite representable contact motion. Interval and
shader iteration budgets, .001 CCD tolerance, failure behavior and device-resident
execution remain unchanged. The same new scenario and existing continuous suites
verify the correction; no CPU numerical fallback or mass-state readback is added.


Final source base is `c6bb994472689890b1291134922848ba8853a55e` with this slice: 4,199
source inputs, SHA-256 `9df8e9c12b6eb8f847d6117c4dc67f0eaf0f056646d59dbd51f0e9a1c6769d1a`. Runtime/consumer DLLs are
`ce3d2d10732b245dff49f851d9d916d8f89207fec03204f8b8d27fa26fae6e86` and `6a3c77ae94d9cac7b71feb355cd86593485835fe437cfd837fb6087a18835107`; the built-in test DLL is
`81cbcb517aa23ddd70d08941e0d42970ae8961c503d6f4771ccd7d532f006241`. All 30 fresh processes pass on that runtime: six boundary
CPU/GPU/provider serial/separate profiles, eighteen consumer regressions, and six
built-in continuous/shared/scene/conformance profiles. Native conformance runs the
existing full CPU/GPU CCD, boundary and directed-ray suites in serial and separate
mode; these remain separate from new provider directed-ray acceptance. All twenty
boundary warmed brackets retain 0/0 owner/all-thread managed bytes. CPU/provider
load no graphics implementation; GPU uses CPU lavapipe with no NVIDIA mappings.
Retained solver lanes are exactly 0/1 for the selected mode. All 61 consumer and
75 built-in runtime files remain unchanged; source-before/source-after and current
source identities agree. These concurrent functional runs do not establish latency
or hardware performance. All seven commit gates pass: runtime build, both scoped
format checks, coverage and wiki test/generate/check. The API remains 13,878
public/protected declarations with 0 unmapped; wiki generation checks 823 public
types and 841 files with no drift. Evidence/manifests stay ignored under final,
native-final and gates. The double-arithmetic curvature audit covers 5,000 seeded
trajectories and 505,000 samples without an upper-bound violation, separately from
device-float functional response. Its first candidate's finite-rotation jerk sign
failed sample 25 and was corrected before final builds. A test-payload preparation
initially omitted the unchanged WebP library; the final retained payload includes
that prerequisite and passes native-path checks. Neither preliminary result is
reported as final acceptance.


<a id="registered-directed-numerics"></a>
## Registered directed-ray numerics

The public-only provider now prepares exact SeparationRayShape origin/endpoint
and SlideOnSlope policy independently of ordinary two-sided segments. Rays have
no geometric area, centroid contribution or rod inertia, remain invisible to
point/ray queries and reject zero length, ray/ray pairs, filled-origin containment
and back-facing crossings. Analytic world-boundary precedence remains separate.
The directed contact returns the authored/extended tip and boundary point; sliding
uses the hit surface normal and otherwise separates opposite the ray axis.
Ordinary material, surface velocity, contact history/events, inverse mass and
explicit inertia retain the shared solver path.

Direct queries extend margin and positive axial source motion, while transverse
ray motion does not create a swept solid. Reverse ordinary-source queries test
the actual convex swept region against the stationary ray, including rounded
support edges. A streamed hull-edge scan avoids an eight-vertex cap; circular
sweeps retain inner-hole intersections for the existing annulus format. Standalone
resource pairs ignore counterpart motion and keep the nearest directed entry in
caller/other order. Shape casts publish a safe low and unsafe high after eight
refinements. Body motion always recovers ray penetration, includes sliding rays
in motion and requires explicit CollideSeparationRay for other rays. Safe travel,
unsafe contact geometry, masks/exclusions, logical shape indices and point velocity
stay in the public result. Character movement and explicit non-sliding snap use
this same registered operation.

Continuous detection validates the complete directed ray after geometric proxy
advancement and samples the configured mass-center trajectory. Actual contacts
use original geometry rather than the candidate search allowance. The existing
interval/refinement/error budgets stay unchanged. An intermediate 160-unit ray
arc around an offset center initially missed a small circle because float
quadratic intermediates cancelled the discriminant. The shared independent
annulus/circle sweep now uses double intermediates, preserving the same inputs,
root selection and output contract rather than widening contact tolerances.

A cold mask control exposed symmetric response in the provider: a ray whose mask
rejected the target still stopped at its tip. Contact positional correction and
normal/tangent/angular effective masses now apply per endpoint's own mask, while
retaining the passive endpoint's actual velocity. CCD is enabled only for matching
response directions; filter edits wake the old connected group without rebuilding
geometry. Pair admission and geometry reports remain separate from impulse
response. Reverse roles, zero-layer receivers, rejected masks, exceptions,
ray/ray and zero-length cases verify the accepted ADR 0102 behavior without
changing actual body mass or inertia.

The --directed-geometry profile accepts --cpu/--gpu and --threaded. Twenty-three
full-frame brackets each warm 128 frames and measure 128 with strict 0/0
owner/all-thread managed bytes: all six query families/Area, raw body recovery,
six finite target families across all CCD modes, both slope policies and coupled
mass/complete opposite impulses with silent private restore. Six reverse target
family cases are cold controls, alongside margins/exclusions, zero/sub-slop/live
edits, throwing Changed publication, boundary precedence, standalone motion,
character/snap, sleep/support removal, directional masks and 6,000-unit/s thin-floor
and offset-center angular CCD. Two immutable prebuilt query rays keep authoring
outside the steady bracket; the initial length-editing query fixture allocated
262,144 bytes per 128 frames and was rejected as a steady-state fixture. Geometry
edits still execute as explicit cold cases. Queued scene impulses are checked
after consumption. Character movement is called outside the physics frame, so
the fixture first supplies a real process frame as well as a physics frame.
PhysicsProcessEnabled alone does not supply that process delta. Snap follows a
real floor departure. These are public clock and movement preconditions.

Point/rest coordinates permit .003 scene units, normal dot above .999, cast
fraction 1/256 plus .00005, mass/inertia .0001, full contact impulse .03,
coupled momentum .02 and shared velocity .05. The thin floor retains .501
solver/float allowance and angular energy at most 102 percent initial. Output
order, geometry exclusions, stable identities, event uniqueness and zero warm
allocations are exact assertions; no backend pose equality is required.

Evidence remains under bin/physics-ray-provider/2026-10-14. Public API, production
XML and accepted ADRs are unchanged. The provider now exercises directed geometry;
arbitrary custom/coupled and complete inherited/extension/network/native/platform
and target-device performance acceptance remain open. Software Vulkan establishes
functional behavior only, and coverage Partial states remain.


The offset-center directed arc also exposed the resident circle-entry quadratic:
its float discriminant cancelled large products and retained an exit/tangent
report without the required entry impulse. The shared resident ray-circle kernel
now projects onto a normalized ray basis, computes the half chord from perpendicular
distance and forms the entry normal in that basis. It retains the same facing
threshold, geometry, budgets and device-float portability. All users of the shared
kernel, including capsule caps, contacts, queries, motion and CCD, receive the
same correction. No FP64 requirement, CPU geometry fallback or extra readback is
introduced. Full continuous/query regressions remain separate from provider
acceptance and hardware performance.


The complete offset-center angular scenario still required a CCD correction after
the stable circle-root change: a near separated proxy without a directed entry
was incorrectly treated as no hit for the entire remaining interval. The resident
CCD kernel now retains the containment exclusion and continues projected-velocity/
curvature refinement until a real directed entry, keeping its existing 256-iteration
and 128-interval budgets. The first speed-only continuation exhausted refinement
and was rejected. A focused --directed-angular case uses only the public API and
retains contact/energy assertions; its successful GPU probe records a real nonzero
entry impulse. Earlier cohorts before this correction are not final acceptance.


Final directed-ray validation on the working source over local main
f01227723b01046792a7c5b466b74ecd5e80d47c retained 4,208 source files with SHA-256
d3d3002bae46e851768e711cbf3032b769864262cc97c035d661255a1354ee89.
The final3 consumer cohort passed 28 fresh processes, including all six
directed profiles; native-final3 passed six built-in regression processes using
the same runtime assembly. Each directed profile passed all 23 brackets with
128 warmup and 128 measured frames and 0/0 owner/all-thread managed bytes. CPU
and registered profiles had no available graphics device; GPU profiles loaded
only explicit software Vulkan, with observed solver thread counts zero/one.
All 61 consumer and 75 built-in retained runtime files were rehashed after runs.
The six gates3 checks passed Release build, scoped consumer formatting, compiled
API coverage, wiki generator tests, generation and generation --check. Coverage
reported 13,906 declarations, no unmapped engine members; wiki reported 827
public types and 845 files. Generated output remains unpublished and ignored.
These same-source functional results leave native allocation, physical GPU
performance, real-window FPS, cross-platform and complete physics/network
acceptance open. Earlier angular candidates, including cohorts before the
resident entry refinement fix, remain failed or preliminary evidence.


<a id="registered-priority-recovery"></a>
## Registered priority recovery

The independent public-only provider now uses one body-motion operation for all
prepared shape families, including directed rays. Each of four recovery passes
recomputes geometry, retains up to 32 deepest accepted contact planes and sums
positive obstacle priorities in double precision. Weights normalize to mean one;
a total below .00001 retains unnormalized weights. Sequential projection applies
40 percent of each remaining excess above five percent of the recovery margin.
Maximum finite and subnormal inputs remain finite without clamping authored values.
Area sensors, moving-body masks, body/object exclusions, exceptions, disabled slots
and one-way direction/depth acceptance run before weights are normalized. Per-slot
one-way metadata survives preparation, including separate numerical pieces.

Margin expands ordinary recovery geometry and extends directed rays. Infinite
boundary precedence expands a ray perpendicular to the boundary instead, retaining
the analytic half-plane meaning. Motion sweeps use original geometry; recovery
margin does not enlarge the solid being swept. Sliding/non-sliding ray participation,
safe/unsafe fractions, logical source/target indices, point velocity and supplied
pose remain in the shared public operation. Zero-motion recovery can publish travel
without a collision when RecoveryAsCollision is false. Ordinary character and
PhysicsBody movement now consume this same operation; the former ordinary-provider
path reported an initial hit with zero recovery/depth and was rejected by the new
public profile. The former ray-only path selected one deepest point and ignored
stored priority; it has been replaced rather than retained as another algorithm.

The --priority-geometry profile accepts --cpu/--gpu and --threaded. Its analytic
matrix uses nine equal/unequal/scaled/maximum/tiny/subnormal weight pairs for a
circle and two perpendicular directed rays against half-planes. It exercises
invalid writes, thread/kind rejection, detach/reentry, exclusions and exceptions,
nonblocking maximum-priority Areas, recovery reporting, disabled logical slots,
41 contacts competing for the 32-plane cap, all six finite source families,
one-way shallow/backface/deep and swept controls, test-only/applied movement and
real CharacterBody recovery. An isolated rigid circle collision alternates maximum
priorities, verifies unchanged inverse mass, full physical momentum/impulse and
unique events with private silent restore. Explicit zero replacement damping keeps
that momentum oracle independent of the world's default damping.

Five complete brackets each warm 128 frames and measure 128: edited circle query,
41-contact recovery, edited two-ray query, sleeping scene policy edits and active
rigid contact/replay. All frames include PhysicsFrame and the exercised public
operation, with strict 0/0 owner/all-thread managed bytes. The initial capacity
fixture allocated formatted success strings; formatting now occurs only on failure,
without excluding query/frame work or changing the zero budget. Analytic recovery
uses .002 scene-unit tolerance (.0000001 for tiny weights); rigid momentum/impulse
uses .003 and mass .0001. Geometry, identity, filtering, sleep and event counts use
exact assertions. Evidence lives under bin/physics-priority-provider/2026-10-14.

Production public API/XML and ADRs are unchanged. Priority affects motion recovery,
not rigid mass or impulses. General registered contact-cache settings and arbitrary coupled/custom solver behavior, complete inherited/extension
contract, native/network/platform and target-device performance acceptance remain
open. The priority profile is complemented by the registered contact numerics below;
contact-cache acceptance remains separate. Software Vulkan is functional
evidence, with physical GPU performance and real-window FPS unverified.


Final priority evidence uses the unchanged compiled runtime over local main
f9e89ab5bcb58480cd5c78d70a310f51fb5026da and 4,209 present source files with
SHA-256 c471269cb55939e757488be5d2d400f064e58c90d25f4342d182df5c9322976a.
The pre-commit path catalog includes one retired tracked filename; the source
manifest and digest include only present files. All 32 fresh final processes
passed: six priority profiles and 26 independent/common regressions, including
both directed solver-thread modes and shape policy/owners. The six commit gates
passed Release build, scoped formatting, compiled API coverage, wiki tests,
generation and generation --check. Coverage retained 13,906 declarations with
zero unmapped members; wiki retained 827 public types and 845 files. All 61
loaded runtime files were rehashed. CPU/registered profiles loaded no graphics
driver, GPU profiles loaded only software Vulkan, and numerical thread counts
matched zero/one. No native-memory, hardware throughput or window acceptance
is inferred from these results.


<a id="registered-contact-numerics"></a>
## Registered contact correction and sweeps

The independent public-only provider now captures PhysicsContactSettings on world
creation and applies the world bias/penetration slack in its numerical solver.
Prepared parts retain their owning resource and sampled bias; policy preparation
refreshes that value without recreating geometry, including a committed change
whose Changed subscriber throws. Actual changes wake the connected body group;
world correction/iteration changes wake dynamics, while equal writes preserve
explicit sleep. Zero shape bias inherits the world; one override replaces it and
two overrides use their arithmetic mean.

Each positive interval recomputes contacts and derives a target residual depth
from 1-(1-bias)^(interval/outer tick), retaining the allowed penetration slack.
Complete contact/joint sweeps converge towards that target without multiplying
the budget by the number of rows or sweeps. Twelve coincident contacts therefore
share the same required residual. Positional correction changes pose separately
from physical velocity and reported normal/tangent impulses. The common mask
rules retain per-endpoint inverse mass/inertia and the passive endpoint's velocity.

Contact rows retain accumulated normal/tangent impulses within each interval and
one restitution target sampled before sweeps. Each complete sweep solves contacts,
joints and positional correction. Published impulses are the final physical sums,
including release of earlier provisional impulses; friction limits use the complete
normal sum. Contact geometry, logical indices, reports, debug points and pair
statistics are prepared once per interval, retaining the existing outer-frame
aggregation/caps/events and private restore. Reusable row storage scales with
contact capacity rather than the authored iteration count and clears body references
in finally on success/failure. An empty constraint set skips empty sweeps, including
int.MaxValue with a shapeless CCD body. No iteration request is clamped.

CCD preserves the previous joint-velocity preparation before predicting curved
motion, applying spring input only once per outer interval. Contacts and joints
then participate in shared sweeps at each actual CCD duration. Removing that
preparation missed the pinned custom annulus arc and was rejected. The old strict
custom arc geometry fixture now explicitly requests bias one and zero slack,
retaining its original .005 allowed-side bounds; ordinary captured defaults and
nonzero slack are exercised by the new policy matrix.

Changed post-contact angular trajectories exposed slow convergence in independent
boundary advancement. Each finite core vertex now uses projected closing velocity
and a conservative second-derivative bound that includes both rotations, offset
mass centers and relative translation over the remaining duration. Earliest roots
bound the next advance; rounded support radius is constant along a unit plane
normal. The existing 1,024 refinement budget and .0001-times-thickness tolerance
remain unchanged. No search allowance becomes physical penetration slack.

The --contact-policy profile accepts --cpu/--gpu and --threaded. Seven built-in
finite/directed source families against an analytic boundary exercise seven bias
combinations at one/16/128 sweeps and .01/.04-second outer ticks, allowed slack,
physical momentum/friction, live policy and throwing-observer commits, unchanged
geometry/RID/mass, sleep/equal/changed writes and one contact episode. An unrelated
real CCD collision checks outer-budget distribution; twelve incident contacts
check shared correction. Eight-body contact and pin chains compare one versus
128 sweeps by convergence and total momentum, with complete private replay.
Nineteen brackets per profile each warm 128 full frames and measure 128 with
strict 0/0 owner/all-thread managed bytes; no query/frame/restore work is subtracted.
Correction permits .004 scene-unit numerical error, physical impulses .03 and
chain momentum .03; event/identity/geometry/sleep and allocation assertions remain
exact. Evidence lives under bin/physics-policy-provider/2026-10-14.

Public API, production XML, algorithms and ADRs are unchanged by this consumer
implementation. Registered geometric cache recycling/max-separation behavior,
arbitrary coupled/custom correction, full inherited/extension/network contract,
native allocation, platforms and target-device/window performance acceptance remain
open. Stored recycling settings do not imply cache acceptance. Software Vulkan
establishes functional behavior only.


Final contact-policy validation over local main 33529779e24ac251cbf2446a93f76179827cbaeb retained 4,222 source files with SHA-256 3a99a26f06b8e368a50a20bdfcd17fda6ccf6e647374dbef7367e16d5fe4c919.
All 34 fresh final processes passed (six policy profiles plus 28 linked regressions),
with nineteen 128-warmup/128-measured brackets per policy profile at 0/0 managed
bytes. All 61 retained runtime files and current source files were rehashed.
The six commit gates passed Release build, scoped formatting, compiled API, wiki
tests, generation and generation --check. Coverage reports 13,961 declarations
with zero unmapped members; wiki reports 832 public types and 850 files. Software
GPU/no-device CPU/registered proofs retain the exact zero/one solver-thread modes.
The boundary curvature bound also passed 80,000 derivative samples from 5,000
seeded cases without a violation; these checks supplement the actual rotating
boundary/offset CCD regression, not hardware or complete numerical acceptance.

## Initial rigid locks and coupled constraints

Raw bodies selected as RigidLinear before attachment now receive LockRotation=true in their creation configuration, matching scene bodies and the existing live-mode projection. The public motion-role consumer checks the flag, immediate zero inverse inertia and rejected torque response before the first solver step. The coupled four-body graph and loaded contact column also select the independent registered solver; see [the joint verification profile](physics-joints.md#coupled-support-continuous-motion-and-sleep). Wider arbitrary geometry and complete extension acceptance remain open.

## Geometry profile reconciliation, 2026-10-16

The current public-only consumer passes all seven registered geometry profiles
(rectangle, capsule, segment, convex, concave, infinite boundary, directed ray)
in both serial and separate numerical modes. Directed geometry also passes the
same CPU/GPU policies on pinned lavapipe, with the process clock prepared before
out-of-physics CharacterBody movement. The prior zero-process-delta failure is
retained in the diagnostic log; no numerical bound or movement assertion was
relaxed. Current evidence is in bin/physics-current-audit/2026-10-16 and the
[paired runner record](physics-backends.md#paired-complete-collider-runner-2026-10-16).
The earlier circle-only verification description no longer represents this fixture.
Full external numerical, inherited/network, native/driver memory and target-device
performance acceptance remain open; coverage states are unchanged.

## Failed shape rebuilds

The complete shared raw shape-edit family now restores authoring and retries prior
numerical geometry after failure. The registered consumer deliberately damages its
numerical shape table before throwing, checks 45 apply/restore/retry failure cases,
and preserves object/canvas identity and sensor effects. [Shared shape-edit
verification](physics-queries.md#failed-raw-shape-edits-and-warmed-reuse) retains
CPU/GPU geometry, resource lifetime and separate physical-step/immutable-authoring
allocation gates. Complete arbitrary numerical/extension acceptance remains open.

## Area observer registration transactions

PrepareMonitoringCapacity executes before a shared Area callback setter publishes
its delegate or resets logical pair history. The selected provider reserves
current collider/shape capacity; it does not need prospective callback state.
A preparation failure propagates once and preserves old observers and queued
generation. The public-only [transaction profile](physics-queries.md#area-observer-registration-transactions)
checks 24 registered failures across scene/raw body/Area callback lanes, rejected
weak candidate targets, exact nonzero logical indices and cross-receiver queued publication.
The full-frame warm path retains strict zero managed allocation. Complete
numerical/custom/native/foreign/performance acceptance remains open.

## Failed filter publication

The common scene/raw layer/mask family now commits authoring after the selected
UpdateFilter returns. A partial numerical failure queues current effective-filter
repair before query/step work, and a failed retry remains pending. The public-only
[eight-role profile](physics-filters.md#failed-updates-and-effective-sensing) checks
160 failures, committed borrowed reads, no hidden shape rebuild, logical gaps,
exact sensing and local replay. Full numerical/custom/native/platform/performance
acceptance remains open.

## Procedural server data

[The common data boundary](physics-shape-data.md) now imports procedural geometry
by represented kind and exports both owned and borrowed shape RIDs into independent
concrete resources. A no-factory public provider exercises nine canonical kinds,
two-world borrowing, supplier failures, exact logical indices and real selected
queries; complete warmed frames retain strict zero managed bytes. Full arbitrary
custom/numerical/native/foreign/network/hardware acceptance remains open.

## Shared replacement participants

A registered RebuildShapes hook cannot mutate a different world participating
in the same owned shape-data replacement. Common world borrows span numerical
rebuild, rollback and rejected-copy retirement; committed old-view retirement
keeps ordinary mutation access. [Three-world checks](physics-shape-data.md#shared-cpugpuregistered-replacement)
cover CPU/GPU/registered scene/raw roles, both input representations, partial
failures and complete warm groups without changing solver ownership.

[Shape data consumer guards](physics-shape-data.md#shared-cpugpuregistered-replacement)
also reject incoming RID/resource bindings from unrelated worlds and edits or
release of detached participants during data reads/replacements. Public-only
checks retain exact slot/property identity, error propagation and successful
retry/binding after the guard ends; full numerical/network/performance acceptance
remains open.

[Shared owned-shape release](physics-shape-data.md#shared-shape-release) now borrows resource membership and every
participating world through removal and private-view retirement. Status failure
preserves live data; committed cleanup attempts all users, retains exact errors
and unregisters the RID. Public-only checks retain unrelated slots, exact Area
entry/exit histories and zero warmed managed bytes. Wider acceptance stays open.
