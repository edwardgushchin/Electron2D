# PhysicsBody

The [complete common-body audit](../components/physics-body-contract.md) reconciles all six declared methods and the body-specific InputPickable default across the five concrete body branches. Its public consumer composes gravity snapshots, transformed-parent motion, caller-owned results, exceptions, direct static/kinematic publication and warmed full frames. General inherited API and full-goal acceptance retain their own open requirements.

[Long circle sweeps and hollow query sources](../components/physics-queries.md#long-circle-sweeps-and-hollow-query-sources) preserve geometric hit truth, first entry and contact ownership across the exercised implementations.

[Motion source selection and contact velocity](../components/physics-queries.md#motion-source-selection-and-contact-velocity) verifies the winning logical source, recovery-only selection and rotated mass-center velocity through scene/raw callers.

Last updated: 2026-10-26

## Collision exception publication

Explicit body exceptions and independently counted active-joint contributions
share one deduplicated input union. A failed joint collision-policy publication
retains the requested flag and complete common contribution, then retries before
a world query, local/portable capture or active positive step. An equal setter
also retries; configured flag and scoped exception-input reads do not repair.
Clear, free, endpoint departure and scene disposal remove only that attachment's
contribution and pending request. Reentry applies the retained flag to a fresh
attachment. The selected SetCollideConnected hook applies a complete value and
may be retried after a publication error.
See [complete exception operations](../components/physics-registered-adapter.md#complete-exception-operations)
for threading, response, snapshots, cleanup checks and remaining limits.


## Finite hierarchy-global poses

Finite local values can overflow through parent composition. Physics attachment
and preparation reject a nonfinite global pose before selected collider creation,
SetPose or Solve; body validation checks finiteness before its basis cache. The
caller can inspect and repair authored transforms, then retry the original nodes.
Getters retain their non-preparing presentation contract. Finite top-level poses
remain independent of ancestors. See [the hierarchy/Area pose audit](../components/physics-registered-adapter.md#finite-hierarchy-pose-and-area-transform-audit)
for all scene siblings, raw Area, rotated geometry, original errors, lifecycle and
strict zero warmed managed-byte checks and their acceptance limits.

## Failed initial attachment

Failed initial preparation discards incomplete numerical pose, velocity and sleep instead of reading them into common authoring. Public authored state remains available for healthy reattachment. Ordinary retirement of a successfully attached body still preserves accepted solver motion, including immediate impulses. See [state preservation checks](../components/physics-registered-adapter.md#authored-state-after-failed-initial-attachment) for all owner/role, sleep, error and lifecycle controls and their limits.

Failed initial preparation retires the partial physical binding. Successful cleanup preserves the original exception; a cleanup failure is aggregated alongside it. The RID and authored shapes remain usable for a healthy later attachment, and unrelated live colliders remain available. This applies to server Body/Area and every scene PhysicsBody sibling plus Area. See [initial attachment checks](../components/physics-registered-adapter.md#initial-attachment-cleanup-errors) for executed scope and limits.

[Gravity publication and terminal parameter guards](../components/physics-registered-adapter.md#gravity-publication-and-terminal-parameter-guards) retry the latest effective native gravity/omission request after a selected error and discard it on attachment retirement. Scene damping, omission and inherited material setters reject failed worlds before changing authoring while healthy post-solver callbacks retain writes. Public CPU/software-GPU/registered JIT/NativeAOT profiles verify real motion, history boundaries and strict zero managed-byte frames; wider acceptance remains open.

[Body mode operation audit](../components/physics-registered-adapter.md#body-mode-operation-audit) now checks all four roles across five scene siblings and raw bodies, real changed-frame motion, captured numerical input reads, guarded writes and complete mode-publication recovery. Public JIT/strict NativeAOT CPU/software-GPU/registered companions retain strict zero managed-byte frames; broader numerical/native/network/performance acceptance remains open.

## Terminal world access

Terminal simulation or local/portable restore failure rejects attached motion/role writes before changing authored values, including equal assignments. Numerical scene sleep and direct-profile motion reads use the same common failure gate; ordinary native authored velocity/policy and constant-surface reads remain non-preparing. The gate does not impose public-query phase rules on otherwise valid force/state callbacks. Detachment and cleanup remain available. See [terminal world authoring](../components/physics-registered-adapter.md#terminal-world-authoring) for the public regression scope and limits.

## Failed scene role publication

Configured freeze, rotation lock, effective MakeStatic participation and physical-bone activation share complete recoverable role preparation with direct server modes. Numerical body/view/query access and nonzero steps repair incomplete selected publication without replaying Solve. Retirement retains committed configuration and invalidates old views. See [scene role publication](../components/physics-registered-adapter.md#failed-scene-role-publication) for the tested scope and limits.

## Failed motion publication

Failed velocity/sleep/policy publication retains the complete requested write for retry before numerical access, body views, live queries or a nonzero step. Ordinary authored reads stay non-preparing. Linear/angular edits preserve the other selected motion channel, including impulse-derived values. Retirement overlays requested channels on retained selected motion without repairing the discarded attachment. See [motion publication](../components/physics-registered-adapter.md#failed-motion-publication) for executable scope and limits.

## Failed pose publication

Failed BodySetTransform and raw AreaSetTransform publication retains the committed pose. Live queries and nonzero steps retry current authored input; raw body scalar/batch reads and acquired or retained direct-body views also prepare incomplete publication. Scene presentation and Area pose getters remain non-preparing. Repeated errors retain their original exception and leave the tick unchanged. Detachment preserves authored pose instead of sampling a partially changed raw solver pose, discards retired attachment debt and invalidates old views. Later kinematic targets still wait for a nonzero active step. See [pose publication](../components/physics-registered-adapter.md#failed-pose-publication) for the executable scope and limits.

## Failed body-mode publication

A failed direct BodySetMode publication retains the committed mode and motion policy. Numerical body access, including retained direct views and scene direct-mode velocity/sleep access, repairs the complete transition before reading or writing selected state. Live space queries and nonzero steps use the same preparation; repeated failure leaves the tick unchanged. Detachment preserves authored motion and discards retired attachment debt. Scene role takeover prepares the previous direct profile before changing its descriptors. See [mode preparation](../components/physics-registered-adapter.md#failed-body-mode-publication) for executable scope and limits.

**Inherits:** [CollisionObject](CollisionObject.md), [Entity](Entity.md), CanvasItem, Node, ElectronObject · **Inherited By:** [RigidBody](RigidBody.md), [StaticBody](StaticBody.md), [CharacterBody](CharacterBody.md)

- **Source:** [PhysicsBody.GroupMotion.cs](../../src/Scene/2D/PhysicsBody.GroupMotion.cs), [PhysicsBody.cs](../../src/Scene/2D/PhysicsBody.cs)
- **Declaration:** `public abstract class PhysicsBody : CollisionObject`
- **Component:** [Scene physics bodies](../components/physics-bodies.md)

PhysicsServer BodyGetSpace/BodySetSpace share the actual attachment across static, animatable, character, rigid and physical-bone roles. Direct space assignment retains the node's World descriptor and hierarchy; scene entry or viewport world replacement reclaims scene control. Caller-owned spaces may simulate these nodes through SpaceStep. Retained direct-state views expire on detach/transfer. See [common space bindings](../components/physics-registered-adapter.md#common-space-bindings).

## Physical skeletal integration

PhysicalBone overrides internal effective filters for inactive followers: prepared fixtures and motion-layer lookup use zero, while public CollisionLayer/CollisionMask retain configured values. Other body classes retain their ordinary filters.

Backend body and fixture ownership is retained by the shared [PhysicsColliderBackend](PhysicsColliderBackend.md), also used by Area and raw server colliders. Scene state, mass policy, view invalidation and pose publication retain their existing responsibilities.

Scene roles express initial motion through [PhysicsBodyConfiguration](PhysicsBodyConfiguration.md)
and the existing PhysicsServer.BodyMode. Effective disable/freeze choices and
physical-bone activity select that mode; the shared backend builds the vendor
definition and converts linear units.

Live pose, velocity, sleeping and target-motion operations also pass through that
adapter. CompleteBackend publishes the solved scene pose before sampling rigid
motion, preserving velocity edits made by synchronous transform notifications.
Resolved gravity/damping are shared with server bodies in PhysicsBodyRuntime;
scene authored settings and custom-integration policy remain unchanged.

## Description

The shared scene-body role. It registers a backend body when entering a SceneTree and unregisters on exit or disposal. Attached [Joint](Joint.md) constraints are released before the backend body is destroyed. Direct [CollisionShape](CollisionShape.md) children supply fixtures; their resource, disabled state, one-way side, local pose and collision-filter changes are applied before the next physics step or motion query. The body owns backend fixtures and never owns a borrowed Shape or PhysicsMaterial resource. Concrete RigidBody and StaticBody types expose their material override properties; edits rebuild these fixtures before stepping. `MoveAndCollide` and `TestMove` run kinematic sweeps over the registered space; explicit and active-joint RID exceptions suppress a body pair in both those sweeps and ordinary solver contacts. `GetGravity()` exposes the last resolved field for a dynamic body.

## API summary

A direct [CollisionPolygon](CollisionPolygon.md) child supplies owned solid or hollow fixtures through the same body lifetime, filtering and material path as a direct CollisionShape child. Its contour and mode can change while attached; the next fixed step rebuilds its fixtures.

The inherited [CollisionObject.GetRID](CollisionObject.md#getrid) remains stable when the backend body and its fixtures are recreated. [PhysicsDirectSpaceState](PhysicsDirectSpaceState.md) returns this RID and a direct child shape-owner index in typed ray and point results.

| Member | Contract |
| --- | --- |
| `protected PhysicsBody()` | Creates a detached body with no fixtures. |
| `public Vector2 GetGravity()` | Returns the last area/world gravity after body scaling for RigidBody and CharacterBody, including temporary static/frozen participation; zero for detached bodies and StaticBody. |
| `public void AddCollisionExceptionWith(PhysicsBody body)` / `RemoveCollisionExceptionWith(PhysicsBody body)` | Add or remove a one-sided exception entry by another scene body's RID. |
| `protected override void ValidateDisposal()` | Preflight scene and dependent-joint world ownership/phases before disposal. |
| `public PhysicsBody?[] GetCollisionExceptions()` | Caller-owned deduplicated explicit/joint array; a server-only or freed RID has a null scene slot. |
| `public KinematicCollision? MoveAndCollide(Vector2 motion, bool testOnly = false, float safeMargin = 0.08f, bool recoveryAsCollision = false)` | Move to safe travel or test without moving; return a caller-owned contact or null. |
| `public bool TestMove(Transform from, Vector2 motion, KinematicCollision? collision = null, float safeMargin = 0.08f, bool recoveryAsCollision = false)` | Query from an arbitrary global pose without moving; optionally fill a caller-owned result. |
| `protected override void OnEnterTree()` | Attaches the backend body; custom overrides retain ordinary base setup. Departure release belongs to engine membership cleanup after TreeExiting. |
| `protected override void Dispose(bool disposing)` | Releases any remaining backend body and shape slots before inherited cleanup. |

## State and failures

The first profile accepts unit global scale and zero skew; a transformed active body that violates this fails its step before any shape replacement. Its translated/rotated pose can be changed by game code while attached and reaches the backend on the next fixed step. Explicit RigidBody force/impulse actions also synchronize pending pose and fixture edits before applying, so current mass and center of mass are available before the first frame. Solver pose sync writes one unit-scale global transform, avoiding scale drift during repeated rotation. A failed step leaves the world available for a corrected later step. Attachment, scene callbacks and disposal use the scene owner thread. Re-entering the tree creates a fresh backend body from current typed state.

<a id="getgravity"></a>
### `GetGravity()`

[RigidBody](RigidBody.md) reports its most recently resolved [Area](Area.md) and world gravity vector after `GravityScale`. [CharacterBody](CharacterBody.md) reports the same resolved field without automatically applying it to its desired velocity. Both retain field resolution while MakeStatic or Freeze prevents dynamic force integration. The value is zero before its first fixed step, when detached, and for [StaticBody](StaticBody.md). Area edits update it on the next nonzero physics step. An attached read requires the scene owner thread; a disposed body throws. [PhysicsAreaFieldTests](../../tests/Electron2D.Tests/PhysicsAreaFieldTests.cs) checks rigid-body field reduction; [CharacterBodyTests](../../tests/Electron2D.Tests/CharacterBodyTests.cs) checks world and Area gravity on a kinematic character. [CollisionDisableModeTests](../../tests/Electron2D.Tests/CollisionDisableModeTests.cs) exercises scaled temporary-static/frozen scene and direct-state fields, Area removal and world fallback on explicitly selected CPU and GPU worlds.

<a id="motion"></a>
### `MoveAndCollide` and `TestMove`

Both methods use finite global scene-unit motion and a finite nonnegative recovery margin. They prepare pending body/shape/filter edits before scanning other bodies in the same physics space; Areas are sensors and do not block. The moving body's mask against candidate layers, the body's own RID and one-way pass-through direction are respected. Initial penetration is moved out before the sweep; `recoveryAsCollision=true` may report a contact remaining at the final tested pose. Remaining overlap after recovery attempts stops motion at a zero safe fraction rather than allowing tunneling. Eight adaptive sweep refinements bracket the first new impact; [endpoint refinement and geometric arrival sampling](../components/physics-queries.md#adaptive-sweep-refinement) preserve thin targets over long motion. Compound fixtures retain their direct owner indices. `TestMove` uses its supplied finite unit-scale global pose, leaves the body unchanged and fills `collision` on a completed hit or miss. A detached `TestMove` returns false; an attached `MoveAndCollide` is required and throws if no registered space exists. `MoveAndCollide(testOnly:true)` returns a collision without changing pose. On a regular call it applies travel, including recovery; a miss returns null. Callers own returned [KinematicCollision](KinematicCollision.md) objects.

[PhysicsMotionTests](../../tests/Electron2D.Tests/PhysicsMotionTests.cs) covers scene and server colliders, contact owners/angle, test-only and actual travel, alternate starting pose, masks, disabled fixtures, one-way approach and margin, deep overlap, owner-thread rejection and 64 warmed unchanged `TestMove` calls with zero managed allocation on Linux/.NET 8. [Input picking](../components/physics-picking.md) now executes on both public backends; the derived CharacterBody's separation-ray floor behavior remains a distinct [coverage gap](../coverage/classes/CharacterBody2D.md). Native allocation, other platforms and owner acceptance remain unverified. See [ADR 0063](../decisions/physics.md#adr-0063).

<a id="collisionexceptions"></a>
### Collision exceptions

`AddCollisionExceptionWith` and `RemoveCollisionExceptionWith` take a live PhysicsBody, reject null, and preserve idempotent list membership. Each method changes only this body's exception list; a pair stops colliding when either body lists the other. `GetCollisionExceptions()` returns a new deduplicated array with explicit entries in insertion order followed by active joint targets. Entries introduced through the server API may contain a server-only or later freed RID; their scene array slot is null until the RID is explicitly removed. An exception survives fixture rebuild and tree detachment while this body remains live. Changing the list while attached refreshes cached contact policy before the next physics step or motion query, so an already touching pair responds while geometry remains reusable. Areas continue to sense normally. Attached calls and reads require the scene owner thread. [PhysicsCollisionExceptionTests](../../tests/Electron2D.Tests/PhysicsCollisionExceptionTests.cs) covers unilateral lists, duplicates, scene/server RIDs, live contact changes, motion queries, lifecycle and warmed managed allocation.

The [CollisionObject owner registry](CollisionObject.md#createshapeowner) now supplies logical shape slots for both child and manual groups. Query/contact indices identify global slots, while ShapeFindOwner returns the distinct group ID; removal shifts later indices. Motion owner accessors resolve weak configured objects as well as child nodes. See [ADR 0071](../decisions/physics.md#adr-0071).

## Disabled processing and physics

Inherited `DisableMode` applies to every concrete body. Remove detaches native fixtures and invalidates live body views while retaining RID and owner state. MakeStatic temporarily changes the native type, preserving configured mass and role; enabling restores dynamic, kinematic or static behavior. [CollisionDisableModeTests](../../tests/Electron2D.Tests/CollisionDisableModeTests.cs) verifies queries, contacts, restoration and lifecycle. See [CollisionObject.DisableMode](CollisionObject.md#disablemode) and [ADR 0072](../decisions/physics.md#adr-0072).

## Dependent joint lifetime

`protected override void ValidateDisposal()` checks the scene phase and every active world referenced by a dependent server joint before beginning disposal. Rejected off-owner or in-step disposal leaves the body alive. This applies even after the body is detached while the other endpoint remains attached. Body departure suspends caller-owned connections before native destruction; final disposal clears their configured role. `GetCollisionExceptions()` includes deduplicated explicit and active collision-disabled joint targets, returning null for a server-only peer. Removing an explicit exception never removes a joint contribution. [PhysicsServerJointTests](../../tests/Electron2D.Tests/PhysicsServerJointTests.cs) covers these cases under [ADR 0087](../decisions/physics-joints.md#adr-0087).

[Indexed PhysicsServer geometry](PhysicsServer.md#shape-slots) now shares the scene/server logical slots, effective local poses and native fixture/query path under [ADR 0088](../decisions/physics-shape-slots.md#adr-0088). Raw replacement/pose/disabled/one-way edits do not rewrite child configuration; group/child edits reclaim corresponding overrides. Shape free/replacement follows shared RID/view ownership and related-world phase guards. Effective poses also feed mass geometry. [PhysicsServerShapeSlotTests](../../tests/Electron2D.Tests/PhysicsServerShapeSlotTests.cs) verifies real geometry, body/Area lifetime and one-way contacts/motion on Linux/.NET 10.

PhysicsServer's typed state methods accept this body's RID and preserve its scene
role. Attachment restores non-rigid contact velocity/sleep policy and explicitly
requested dynamic sleep even when automatic sleep is disabled. See [typed state](PhysicsServer.md#body-state).

After an internally enabled GPU backend fails, body/area/joint teardown preserves
owner/stepping guards but skips individual raw graph destruction and partial-motion
capture. Managed bindings/views are released; the failed space reclaims raw storage
in bulk. Queries and further simulation remain rejected. See the
[GPU island graph failure contract](../components/gpu-physics.md#gpu-contact-driven-island-graph-2026-10-08).

## Pointer eligibility default

Inherited [InputPickable](CollisionObject.md#pointer-input) defaults **false** on
PhysicsBody and its descendants, including StaticBody, AnimatableBody, RigidBody,
CharacterBody and PhysicalBone. Enable it explicitly for viewport physics picking.
Its typed stored descriptor exposes the same false revert default. This does not
change simulation, collision layers or ordinary direct-space queries.

[Directional filter mutation](../components/physics-filters.md) shares scene/server/tile metadata and query rules across CPU/GPU; it updates existing fixtures, preserving shape/resource identity. Body assignments wake the body and contact neighbors, including unchanged bits; Area snapshots update on the next step.

Selected motion geometry and logical identities pass [shared output validation](../components/physics-backends.md#selected-motion-result-publication) before MoveAndCollide applies travel or TestMove replaces a supplied snapshot. Invalid implementation output leaves the scene pose and supplied result unchanged. Motion hooks borrow attachment/world lifetime and reject same-space recursive motion.

[Common server modes](../components/physics-registered-adapter.md#common-server-mode-family) now project all four modes through scene and raw RIDs, with independent scene descriptors, effective state/force response, pose publication, reentry and replay. CPU/GPU/registered checks share the public consumer; complete solver and final performance acceptance remain open.

Exception edits request wakeup for duplicate additions and absent removals, and reject numerical-hook writes before changing the owner list. CPU contact policy updates cached flags while retaining geometry. [Shared public checks](../components/physics-registered-adapter.md#collision-exception-authoring-and-response) exercise every body sibling and selected backend.


Departure attempts state capture and numerical detach independently and resets shape revisions for reentry after either error. [Resource retirement](../components/physics-registered-adapter.md#resource-retirement) checks every concrete body sibling and expired views.

The complete PhysicsMotionTests, PhysicsQueryTests and PhysicsShapeQueryTests now use explicitly selected public CPU/GPU worlds. The common suites retain scene/raw identities, view replacement and last-resource retirement, filters/exclusions, compound origin-inside and initial-overlap rules, hollow/segment/convex contact families, one-way recovery, motion fractions/travel and scene shape-owner payloads. Existing numerical bounds are unchanged. Warm gates include both owner/all-thread managed counters: 64 scene-motion, compound recovery/ray/cast/rest queries and 128 alternating span-query cycles require 0/0 bytes. Caller-owned World aliases may outlive the SceneTree; its colliders retire immediately and the view expires after the last World resource is released. Native allocations, foreign platforms, visual and final performance acceptance remain open.

The complete PhysicsCollisionExceptionTests now runs on explicitly selected public CPU/GPU worlds for scene/raw owners. It checks unilateral filtering in both motion directions and actual solver response, duplicate edits, server-only/null and freed-target slots, live contacts and reentry. Sixty-four warm active exception frames and 64 changed raw-exception/step cycles require 0/0 owner/all-thread managed bytes. Internal GPU store probes remain diagnostics; broader inherited/extension, native/platform and final performance acceptance stays open.

The complete PhysicsBodyTests, RigidBodyForceTests, RigidBodyContactTests and ShapePairEventTests now execute on explicitly selected public CPU/GPU worlds, with actual backend assertions in every numerical scene. The ordinary full suite runs both variants; ELECTRON2D_TEST_SCENE_PHYSICS=cpu|gpu selects the same complete set. Existing force/impulse/center units and numerical bounds, freeze/lock/mass/geometry recovery, persistent force packing, sleep/wake notification policy, contact caps/deduplication and object-before-shape entry/departure ordering are unchanged. Scene/raw nullable payloads, compound logical slots, handler mutation/errors and committed-event no-replay remain asserted. All existing 64-frame warm gates now require 0/0 owner/all-thread managed bytes for resting/active/moving bodies, forces, monitored/no-contact frames and steady/transitioning shape pairs. Box2D SIMD and worker scheduling probes remain CPU diagnostics; they do not establish independent GPU acceptance. Native allocations, wider inherited/extension, foreign platforms, full networking and final GPU/window performance acceptance remain open.


Material-driven shape preparation retains indexed logical-slot validation
and applied revision tracking without boxed list enumerators.
[Shared edited contact checks](../components/physics-registered-adapter.md#independent-contact-response)
exercise scene/raw bodies, actual surface transfer, complete impulses and replay
at 0/0 owner/all-thread managed bytes on registered/CPU/GPU implementations.

## Physical event teardown

Base collider disposal clears publisher-owned pointer and derived contact/sleep handler lists after scene teardown. Body space-detachment failures still reach base disposal, and material subscriptions release in the same cleanup path. Independent detach and scene-exit errors are preserved together; a sole failure retains its original exception. Area follows the same guaranteed base cleanup boundary for its overlap lists. See [prepared physical events](../components/event-connections.md#prepared-physical-event-delivery).

Public-only registered resource checks inject combined collider-detachment and TreeExiting callback failures for StaticBody, AnimatableBody, CharacterBody, RigidBody, PhysicalBone and Area. They assert both errors remain observable, disposal completes, scene membership clears and subscriber payloads become collectible. This covers the guaranteed base cleanup path, separately from numerical acceptance.


[Registered priority recovery](../components/physics-registered-adapter.md#registered-priority-recovery)
now exercises weighted motion recovery independently through the public API,
including finite/directed families, bounded contact planes, exclusions, one-way
controls, character movement and unchanged rigid momentum. The shared CPU/GPU/
registered profile covers five 128-warmup/128-measured full-frame brackets at zero
managed allocation. Wider registered contact settings and complete numerical,
native/network/platform/performance acceptance remain open.


[Moving one-way recovery](../components/gpu-resident-motion-queries.md#moving-one-way-recovery)
now includes kinematic/dynamic linear approach over the last physical step on
both production backends, including private/portable restoration. Static surface,
receding/tangent and angular-only velocities do not enlarge recovery depth.
PhysicsMovingOneWayTests checks raw/scene callers, roles, rotated slots, six finite
source families and eight 128-warmup/128-measured complete-frame brackets at zero
managed allocation. Native/platform/hardware/performance acceptance remains open.

Filter forwarding consumes proposed bits explicitly and applies the existing
HasServerMode/CollisionResponseEnabled gate before publication. Configured values
remain committed during failed hooks and repair uses the current effective role.
CPU Area sensing now uses EffectiveCollisionLayer too, matching inactive follower
fixtures and motion queries. See [filter failures and sensing](../components/physics-filters.md#failed-updates-and-effective-sensing).

Scene departure retains the physical binding through the inherited OnExitTree hook and TreeExiting. Non-public membership cleanup then retires it before NodeRemoved/TreeExited even if a user hook or event fails. The OnExitTree hook is inherited from Node; no separate public cleanup alias is added. The [public lifecycle consumer](../components/scene-hierarchy.md#entry-readiness-and-departure) verifies live body/Area bindings, joint snapshot-map admission/removal and multi-error release on selected CPU/software-GPU/registered worlds.

[Current-group Entity pose edits](../components/scene-hierarchy.md#transform-authoring-from-processing-groups) now defer selected backend publication to the joined owner phase. Scene-space borrowing excludes native/query/step/release access during worker callbacks, rolls back partial acquisition and always releases before numerical work. Body identities, child-shape transforms and synchronized AnimatableBody presentation retain their existing semantics; velocity, forces, geometry and other physical operations keep their own owner/borrow guards.

Changed body one-way slot policies are captured during authoring and reused by
the next numerical preparation. The [one-way input profile](../components/physics-shapes.md#one-way-input-preparation)
checks edits without an intervening query, retained immutable values and strict
zero-allocation physical frames; changed authoring measured 32 bytes per
logical slot on Linux x64. Area sensing policy is unchanged.

[Pickability operation audit](../components/physics-picking.md#pickability-operation-audit)
verifies the retained Body/Area input, inherited defaults, actual selected query and
numerical lanes, owner/disposal guards and callback edits. Changed-policy moving
frames preserve exact transitions and zero managed allocation; native rendering
and wider numerical/platform/performance acceptance keep their separate scopes.

[Final body-motion contact sampling](../components/physics-registered-adapter.md#final-body-motion-contact-sampling) resamples completed recovery and unsafe motion. Contact geometry is sampled after the completed recovery at the unsafe motion pose, including the query margin. The deepest eligible final contact supplies point, normal, depth, logical identities and point velocity. When requested recovery is followed by an unobstructed departure with no final contact, the result clears collision data and retains full requested motion plus recovery. Safe travel and remainder still use the safe fraction.

[Last-recovery one-way pair exclusions](../components/physics-registered-adapter.md#last-recovery-one-way-pair-exclusions) retain rejected source-resource/target-slot pairs through the remaining sweep and final contact sample. The last recovery attempt replaces the set, accepted pieces keep their logical pair eligible, and the next query starts empty.

[Oblique long circle motion](../components/physics-queries.md#oblique-long-circle-motion) verifies the existing public cast/overlap and scene/raw body-motion paths across four unit-basis poses, both source/peer roles and signs. CPU/software-GPU/registered owner/separate CLR and NativeAOT profiles retain the existing tolerances and strict zero warmed managed allocation; wider acceptance remains open.

[One-way motion entry](../components/physics-queries.md#one-way-body-motion-entry) selects new impacts from their actual contact side. Tangent/receding front-corner hits are distinct from releasing a receding initial overlap; invalid sides cannot hide a later obstacle. Recovery depth and separation-ray source policy retain their contracts.

[Processing-group motion](../components/physics-group-motion.md) permits current-group velocity/state authoring with ordered owner publication, prepared activity observations and retained failure/retry state. Numerical operations and other authoring families retain their individual access rules.
