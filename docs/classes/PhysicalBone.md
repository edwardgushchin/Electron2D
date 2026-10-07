# PhysicalBone

Last updated: 2026-10-07

- Declaration: `public class PhysicalBone : RigidBody`
- Source: [PhysicalBone.cs](../../src/Scene/2D/PhysicalBone.cs)
- Inherits: [RigidBody](RigidBody.md)
- Component: [Skeletal animation](../components/skeletal-animation.md)

## Description

A real RigidBody that selects a separate scene Bone. Skeleton ownership comes from a direct Skeleton parent or a chain of PhysicalBone parents; neutral nodes interrupt ownership. BoneNodePath is relative to the physical body and overrides numeric selection. Attached numeric selection authors a stable path; -1 clears selection. Missing, renamed, removed or foreign bones cannot activate simulation; the request survives and a repaired binding restarts from the current bone pose. Parent-first Ready reconciliation seeds nested body transforms before child setup.

SimulatePhysics stores the request; IsSimulatingPhysics reports an active attached backend with a valid selection. Inherited Freeze, Sleeping and disable policies still control actual movement. An inactive physical body follows its bone, uses a static backend and effective collision layer/mask zero. The configured public CollisionLayer/CollisionMask remain unchanged. Starting or stopping reseeds from the current bone, changes backend participation and refreshes fixture filters; these transitions are cold preparation boundaries. FollowBoneWhenSimulating aligns before each physics step, retains ordinary rigid forces/contacts during that step, and excludes body-to-bone transfer. It also follows in idle.

GetJoint borrows the first live direct Joint child; no joint or collision shape is created. AutoConfigureJoint=true aligns that joint origin with the physical body. A PhysicalBone parent supplies NodeA/NodeB paths; a Skeleton parent preserves authored endpoints. Existing attached anchors retain ADR 0084 sampling semantics: moving the Joint alone does not rebuild the constraint. Author a collision shape with an offset center of mass when a hinge at the physical origin should produce pendulum rotation.

Physics remains world-owned and subject to ordinary unit global scale and zero skew, material/mass/force/contact/exception, owner-thread and solver-mutation gates. Invalid transform preparation fails visibly and can be corrected. This type is not a Bone subclass and does not replace pose hierarchy/rest storage. SkeletonModificationPhysicalBones is the actual pose consumer.

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
| `public System.Boolean IsSimulatingPhysics()` | [Pure owner-thread read of active attached valid selection; inherited Freeze/Sleep still control motion.](#issimulatingphysics) |
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

Pure owner-thread read of active attached valid selection; inherited Freeze/Sleep still control motion.

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
