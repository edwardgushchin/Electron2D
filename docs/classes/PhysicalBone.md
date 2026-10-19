# PhysicalBone

Last updated: 2026-10-19

## Persistent force authoring guards

ConstantForce and ConstantTorque reject attached writes after terminal world
failure before either retained value changes, including zero and equal assignments.
Their configured getters remain non-preparing. Detached authoring and healthy
owner callbacks retain their existing behavior. The inherited scene force family,
RID facade and direct views reach the same retained totals and selected response;
see [complete force operations](../components/physics-registered-adapter.md#complete-force-operations)
for mode, lifetime, numerical and allocation evidence.


## Integration callback lifetime

Detachment preserves force/sync registrations for reentry with a fresh direct-state
view. Replacing or clearing a registration releases its previous delegate and typed
user data. Body release clears both callback references and its cached view through
one common registry retirement path, including raw-body cleanup errors and stale
scene registration removal. Keeping an invalid old view no longer keeps those
registrations alive. Explicit local history retains its own captured callback
identities under the existing replay contract.

See [complete integration operations](../components/physics-registered-adapter.md#complete-integration-policy-operations)
for owner/phase, selected motion, contact reporting and lifetime checks.


## Inactive sleep state

An inactive physical bone has a static effective role and reports Sleeping through
both its inherited scene property and PhysicsServer even while detached. This read
preserves its retained dynamic sleep request. Inherited CanSleep and Sleeping
storage use the RigidBody property descriptors. See [body state checks](../components/physics-registered-adapter.md#complete-body-state-operations).


[Gravity publication and terminal parameter guards](../components/physics-registered-adapter.md#gravity-publication-and-terminal-parameter-guards) retry the latest effective native gravity/omission request after a selected error and discard it on attachment retirement. Scene damping, omission and inherited material setters reject failed worlds before changing authoring while healthy post-solver callbacks retain writes. Public CPU/software-GPU/registered JIT/NativeAOT profiles verify real motion, history boundaries and strict zero managed-byte frames; wider acceptance remains open.

## Activation role recovery

SimulatePhysics retains the committed request after a selected role-publication error and retries before numerical use or repeated assignment. Freeze/lock/disabled participation use the inherited recovery boundary. A tree-reentry test explicitly calls RequestReady before AddChild to run the binding producer; this does not change Node Ready timing or promise immediate rebinding without it. See [scene role publication](../components/physics-registered-adapter.md#failed-scene-role-publication).

- Declaration: `public class PhysicalBone : RigidBody`
- Source: [PhysicalBone.cs](../../src/Scene/2D/PhysicalBone.cs)
- Inherits: [RigidBody](RigidBody.md)
- Component: [Skeletal animation](../components/skeletal-animation.md)

## Description

Metadata getters, GetJoint and IsSimulatingPhysics allow the scene owner or an active processing group. Current-group notifications update body/child-joint poses immediately; activation changes publish native mode/filters on the owner after joining. A failed selected publication retains the request and retries before numerical use on the same attachment. Tree departure or physical retirement discards stale pending work. Binding and role setters stay on the owner.

A real RigidBody that selects a separate scene Bone. Skeleton ownership comes from a direct Skeleton parent or a chain of PhysicalBone parents; neutral nodes interrupt ownership. BoneNodePath is relative to the physical body and overrides numeric selection. Attached numeric selection authors a stable path; -1 clears selection. Missing, renamed, removed or foreign bones cannot activate simulation; the request survives and a repaired binding restarts from the current bone pose. Parent-first Ready reconciliation seeds nested body transforms before child setup.

SimulatePhysics stores the request; IsSimulatingPhysics reports an active attached backend with a valid selection. Inherited Freeze, Sleeping and disable policies still control actual movement. An inactive physical body follows its bone, uses a static backend and effective collision layer/mask zero. The configured public CollisionLayer/CollisionMask remain unchanged. Starting or stopping reseeds from the current bone, changes backend participation and refreshes fixture filters; these transitions are cold preparation boundaries. FollowBoneWhenSimulating aligns before each physics step, retains ordinary rigid forces/contacts during that step, and excludes body-to-bone transfer. It also follows in idle.

GetJoint borrows the first live direct Joint child; no joint or collision shape is created. AutoConfigureJoint=true aligns that joint origin with the physical body. A PhysicalBone parent supplies NodeA/NodeB paths; a Skeleton parent preserves authored endpoints. Existing attached anchors retain ADR 0084 sampling semantics: moving the Joint alone does not rebuild the constraint. Author a collision shape with an offset center of mass when a hinge at the physical origin should produce pendulum rotation.

Physics remains world-owned and subject to ordinary unit global scale and zero skew, material/mass/force/contact/exception, the documented thread and solver-mutation gates. Invalid transform preparation fails visibly and can be corrected. This type is not a Bone subclass and does not replace pose hierarchy/rest storage. SkeletonModificationPhysicalBones is the actual pose consumer.

## Example

```csharp
var physical = new PhysicalBone { Name = "ArmBody", BoneNodePath = "../Arm" };
physical.AddChild(new CollisionShape { Shape = armShape });
rig.AddChild(physical); // Existing Skeleton/Bone and caller-owned Shape.
physical.SimulatePhysics = true;
// Add SkeletonModificationPhysicalBones to the rig stack to transfer solved poses.
```

## Constructors

| Signature | Contract |
| --- | --- |
| `public PhysicalBone()` | [Creates the default detached node/resource described above.](#ctor) |

## Properties

| Signature | Contract |
| --- | --- |
| `public System.Boolean AutoConfigureJoint { get; set; }` | [Configures the first direct authored child joint. Default true; false preserves authored position and endpoints.](#autoconfigurejoint) |
| `public System.Int32 BoneIndex { get; set; }` | [Resolved index or authored numeric selection. -1 clears; below -1 rejects; a valid attached index authors its path.](#boneindex) |
| `public System.String BoneNodePath { get; set; }` | [Bounded path relative to this body. Empty uses numeric selection; authored path takes precedence.](#bonenodepath) |
| `public System.Boolean FollowBoneWhenSimulating { get; set; }` | [Aligns the active body to the bone at processing boundaries and omits solved-pose transfer.](#followbonewhensimulating) |
| `public System.Boolean SimulatePhysics { get; set; }` | [Retained simulation request. A valid attached bone is required for actual activation.](#simulatephysics) |

## Methods and protected extension points

| Signature | Contract |
| --- | --- |
| `protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()` | [Supplies the exact PhysicalBone scene factory; inherited subclass construction follows the ordinary scene contract.](#createsceneinstancefactory) |
| `public override System.String[] GetConfigurationWarnings()` | [Returns current inherited physics and missing ownership/selection/child-joint warnings.](#getconfigurationwarnings) |
| `public Electron2D.Joint GetJoint()` | [Returns the borrowed first live direct Joint child, or null. Does not generate or dispose it.](#getjoint) |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | [Extends inherited typed schema with the described configuration; live bindings are not stored.](#getpropertydescriptors) |
| `public System.Boolean IsSimulatingPhysics()` | [Logical owner/active-group read of attached valid activation; inherited Freeze/Sleep still control motion.](#issimulatingphysics) |
| `protected override System.Void OnNotification(System.Int32 what)` | [Reconciles Ready/internal physics/idle following and child-joint invalidation through the existing scene lifecycle.](#onnotification) |

## Member descriptions

### .ctor

`public PhysicalBone()`

Creates the default detached node/resource described above.

### CreateSceneInstanceFactory

`protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()`

Supplies the exact PhysicalBone scene factory; inherited subclass construction follows the ordinary scene contract.

### GetConfigurationWarnings

`public override System.String[] GetConfigurationWarnings()`

Returns current inherited physics and missing ownership/selection/child-joint warnings.

### GetJoint

`public Electron2D.Joint GetJoint()`

Returns the borrowed first live direct Joint child, or null. Does not generate or dispose it.

### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Extends inherited typed schema with the described configuration; live bindings are not stored.

### IsSimulatingPhysics

`public System.Boolean IsSimulatingPhysics()`

Logical owner/active-group read of attached valid activation; inherited Freeze/Sleep still control motion.

### OnNotification

`protected override System.Void OnNotification(System.Int32 what)`

Reconciles Ready/internal physics/idle following and child-joint invalidation through the existing scene lifecycle.

### AutoConfigureJoint

`public System.Boolean AutoConfigureJoint { get; set; }`

Configures the first direct authored child joint. Default true; false preserves authored position and endpoints.

### BoneIndex

`public System.Int32 BoneIndex { get; set; }`

Resolved index or authored numeric selection. -1 clears; below -1 rejects; a valid attached index authors its path.

### BoneNodePath

`public System.String BoneNodePath { get; set; }`

Bounded path relative to this body. Empty uses numeric selection; authored path takes precedence.

### FollowBoneWhenSimulating

`public System.Boolean FollowBoneWhenSimulating { get; set; }`

Aligns the active body to the bone at processing boundaries and omits solved-pose transfer.

### SimulatePhysics

`public System.Boolean SimulatePhysics { get; set; }`

Retained simulation request. A valid attached bone is required for actual activation.

## Verification and limits

PhysicalBoneTests exercises actual velocity/Freeze, inherited body geometry recovery, follower collision filters, pose strength/disable and physics-phase staging, failed-pass rollback, post-solver transitions, Sleeping/disable, ownership/path repair, parent bootstrap, native pin constraints, manual joint ownership, copied/deferred named commands, BFS foreign-rig omission, resource copies and fresh-process .e2dscene execution. 128 prepared physics/pose/weighted Polygon steps measure zero managed bytes. Native Engine.Run hosts (ELECTRON2D_TEST_PHYSICAL_BONE_HOST=1, ELECTRON2D_PHYSICAL_BONE_RENDERER=gpu or compatibility) on current Linux x64 verify scheduled solver motion, matching scene Bone pose and actual deformed weighted Polygon pixels. Each measures 64 prepared draw/velocity-update intervals at zero managed allocation; headless checks separately cover full physics/pose/replay cycles. The host uses ordinary Engine scheduling rather than reentering SceneTree frames from lifecycle/render callbacks. Native allocator activity, large-rig throughput, foreign platforms and owner acceptance remain unverified. Editor generation/gizmos retain their separate authoring dependency; owned palettes and general Mesh skin now execute through the mesh component.

[Direct server modes](../components/physics-registered-adapter.md#common-server-mode-family) retain a separate runtime role. Animatable target staging follows that direct profile while it is active; physical-bone binding/role writes reject numerical borrowing before authoring changes. Scene role edits restore configured control.

The complete PhysicalBoneTests now executes on explicitly selected public CPU/GPU worlds, including command/follow and custom integration fixtures and restored graphs in a separate fresh process. It retains motion/filter identity, strength/disable/follow/freeze behavior, binding/hierarchy recovery, actual pin constraints, named commands, modifier failure rollback, phase/owner guards and existing numerical bounds. The ordinary full suite runs both backends; ELECTRON2D_TEST_PHYSICAL_BONE=1 with ELECTRON2D_PHYSICAL_BONE_BACKEND=cpu|gpu selects one. After 128 warmup frames, 128 complete physics/pose/weighted Polygon cycles require 0/0 owner/all-thread managed bytes. Native hosts select physics independently from ELECTRON2D_PHYSICAL_BONE_RENDERER=gpu|compatibility, reject renderer fallback and assert both actual selections. All four CPU/GPU-physics by hardware-renderer combinations verify real changed skin pixels and matching Bone/body pose; 64 prepared draw/velocity-update intervals require zero owner-thread managed bytes. This rendering interval is separate from full physical allocation measurement. Native allocations, large rigs, foreign platforms, human acceptance and final network/massive GPU/window FPS gates remain open.

CPU scene/server Area scans now use the same effective zero layer as an inactive
follower's fixtures and motion queries. Editing configured bits cannot make that
inactive follower visible to sensors. The [shared eight-role filter profile](../components/physics-filters.md#failed-updates-and-effective-sensing)
checks this alongside failure recovery and ordinary layer/mask publication.
