# SkeletonModificationJiggle

Last updated: 2026-10-07

- Declaration: `public sealed class SkeletonModificationJiggle : SkeletonModification`
- Source: [SkeletonModificationJiggle.cs](../../src/Scene/Resources/SkeletonModificationJiggle.cs)
- Inherits: [SkeletonModification](SkeletonModification.md)
- Component: [Skeletal animation](../components/skeletal-animation.md)

## Description

A retained spring-like joint controller, distinct from a Bone node. Each selected bone stores force, acceleration, velocity, dynamic point, prior root position and last clear point. Per step, force is (target minus dynamic point) times Stiffness times delta, plus optional Gravity times delta; acceleration is force divided by positive Mass; velocity adds acceleration times (1 minus Damping); the dynamic point adds velocity, force and the bone-origin displacement. This preserves the discrete update law. Delta scales force, not a continuous-time velocity integrator. Larger mass reduces acceleration; Damping attenuates new acceleration, so Damping=1 does not erase existing velocity.

The bone aims its endpoint at the resulting dynamic point and submits a transient local override at stack.Strength. Bone angle and reflection use the existing finite endpoint helpers. A coincident point leaves ordinary rotation intact. Reset clears all accumulated terms and seeds each live joint at its current world origin, including index zero; first valid execution and changed layout/selection seed missing history similarly. Target absence freezes history; missing/singular bones invalidate their own state. Positive mass and widened finite checks avoid undefined division/NaN. Each joint's state commits after its successful pose/request publication; an earlier successful joint remains advanced if a later joint/modifier fails. Skeleton still restores authored poses and pre-pass override requests on failure.

Default edits copy all default settings to nonoverriding joints. Turning Override off does so immediately. Individual setters change the actual used value; enable Override to retain it through later default edits. New slots inherit the current defaults. Structural resizing discards simulation history; copying/files discard it too. Settings reads serialize through the resource gate; bound mutation/reset/disposal follows the scene owner.

UseColliders=true requires ExecutionMode=ProcessPhase.Physics; idle execution rejects before state advances. Existing World.DirectSpaceState ray-tests from the bone origin to the candidate dynamic point, using the unsigned 32-bit CollisionMask, bodies only, no exclusions and ordinary hit-from-inside=false semantics. A hit restores LastClear and clears acceleration/velocity; a miss updates LastClear. Mask zero ignores colliders. This is the dynamic-point ray contract, not swept mesh collision, joint penetration correction or whole-bone shape collision. Query ownership/preparation follows the real current scene physics world.

## Example

This excerpt requires the indicated scene; SkeletonJiggleTests exercises complete producers/consumers.

```csharp
using var jiggle = new SkeletonModificationJiggle
{
    JiggleDataChainLength = 1,
    TargetNodePath = "../Target",
    ExecutionMode = ProcessPhase.Physics,
};
jiggle.SetJiggleJointBoneNode(0, "Hair");
jiggle.SetUseColliders(true);
using var stack = new SkeletonModificationStack { Enabled = true };
stack.AddModification(jiggle);
rig.SetModificationStack(stack); // Existing Skeleton, target and world collider context.
jiggle.Reset(); // Reseed after a teleport or explicit simulation restart.
```

## Constructors

| Signature | Contract |
| --- | --- |
| `public SkeletonModificationJiggle()` | [Inherited typed lifecycle/schema callback.](#ctor) |

## Properties

| Signature | Contract |
| --- | --- |
| `public System.Single Damping { get; set; }` | [Gets or sets the default joint damping. Acceleration contribution attenuation; velocity increments use one minus this value.](#damping) |
| `public Electron2D.Vector2 Gravity { get; set; }` | [Gets or sets the default joint gravity. Optional finite canvas-world force vector.](#gravity) |
| `public System.Int32 JiggleDataChainLength { get; set; }` | [Gets or resizes the joint list, retaining the prefix and seeding new slots from current defaults.](#jiggledatachainlength) |
| `public System.Single Mass { get; set; }` | [Gets or sets the default joint mass. Positive force-to-acceleration divisor.](#mass) |
| `public System.Single Stiffness { get; set; }` | [Gets or sets the default joint stiffness. Nonnegative force multiplier.](#stiffness) |
| `public System.String TargetNodePath { get; set; }` | [Gets or sets the Entity target path relative to its skeleton.](#targetnodepath) |
| `public System.Boolean UseGravity { get; set; }` | [Gets or sets the default joint usegravity. Whether gravity contributes to each force step.](#usegravity) |

## Methods and protected extension points

| Signature | Contract |
| --- | --- |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)` | [Inherited typed lifecycle/schema callback.](#copycustomstateto) |
| `protected override Electron2D.Resource CreateDuplicateInstance()` | [Inherited typed lifecycle/schema callback.](#createduplicateinstance) |
| `public System.UInt32 GetCollisionMask()` | [Inherited typed lifecycle/schema callback.](#getcollisionmask) |
| `public System.Int32 GetJiggleJointBoneIndex(System.Int32 jointIndex)` | [Returns the resolved or authored bone index.](#getjigglejointboneindex) |
| `public System.String GetJiggleJointBoneNode(System.Int32 jointIndex)` | [Returns a joint's relative bone path.](#getjigglejointbonenode) |
| `public System.Single GetJiggleJointDamping(System.Int32 jointIndex)` | [Returns the actual joint damping used by simulation.](#getjigglejointdamping) |
| `public Electron2D.Vector2 GetJiggleJointGravity(System.Int32 jointIndex)` | [Returns the actual joint gravity used by simulation.](#getjigglejointgravity) |
| `public System.Single GetJiggleJointMass(System.Int32 jointIndex)` | [Returns the actual joint mass used by simulation.](#getjigglejointmass) |
| `public System.Boolean GetJiggleJointOverride(System.Int32 jointIndex)` | [Returns whether this joint is protected from default propagation.](#getjigglejointoverride) |
| `public System.Single GetJiggleJointStiffness(System.Int32 jointIndex)` | [Returns the actual joint stiffness used by simulation.](#getjigglejointstiffness) |
| `public System.Boolean GetJiggleJointUseGravity(System.Int32 jointIndex)` | [Returns the actual joint usegravity used by simulation.](#getjigglejointusegravity) |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | [Inherited typed lifecycle/schema callback.](#getpropertydescriptors) |
| `public System.Boolean GetUseColliders()` | [Inherited typed lifecycle/schema callback.](#getusecolliders) |
| `protected override System.Void OnExecute(System.Double delta)` | [Inherited typed lifecycle/schema callback.](#onexecute) |
| `protected override System.Void OnSetupModification(Electron2D.SkeletonModificationStack modificationStack)` | [Inherited typed lifecycle/schema callback.](#onsetupmodification) |
| `public System.Void Reset()` | [Inherited typed lifecycle/schema callback.](#reset) |
| `public System.Void SetCollisionMask(System.UInt32 collisionMask)` | [Sets the unsigned 32-bit physics collision mask.](#setcollisionmask) |
| `public System.Void SetJiggleJointBoneIndex(System.Int32 jointIndex, System.Int32 boneIndex)` | [Selects a bone by nonnegative index, authoring its path when attached and discarding history.](#setjigglejointboneindex) |
| `public System.Void SetJiggleJointBoneNode(System.Int32 jointIndex, System.String boneNode)` | [Selects a joint by relative bone path and discards its previous history.](#setjigglejointbonenode) |
| `public System.Void SetJiggleJointDamping(System.Int32 jointIndex, System.Single value)` | [Sets the actual joint damping; enable Override to retain it through default edits.](#setjigglejointdamping) |
| `public System.Void SetJiggleJointGravity(System.Int32 jointIndex, Electron2D.Vector2 value)` | [Sets the actual joint gravity; enable Override to retain it through default edits.](#setjigglejointgravity) |
| `public System.Void SetJiggleJointMass(System.Int32 jointIndex, System.Single value)` | [Sets the actual joint mass; enable Override to retain it through default edits.](#setjigglejointmass) |
| `public System.Void SetJiggleJointOverride(System.Int32 jointIndex, System.Boolean value)` | [Authors default override policy; disabling copies current defaults immediately.](#setjigglejointoverride) |
| `public System.Void SetJiggleJointStiffness(System.Int32 jointIndex, System.Single value)` | [Sets the actual joint stiffness; enable Override to retain it through default edits.](#setjigglejointstiffness) |
| `public System.Void SetJiggleJointUseGravity(System.Int32 jointIndex, System.Boolean value)` | [Sets the actual joint usegravity; enable Override to retain it through default edits.](#setjigglejointusegravity) |
| `public System.Void SetUseColliders(System.Boolean useColliders)` | [Enables physics-only collider rejection.](#setusecolliders) |

## Member descriptions

### .ctor

`public SkeletonModificationJiggle()`

Inherited typed lifecycle/schema callback; executes the described resource contract.

### CopyCustomStateTo

`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)`

Inherited typed lifecycle/schema callback; executes the described resource contract.

Invalid numeric/count/index inputs throw ArgumentOutOfRangeException; invalid paths/vectors throw ArgumentException. Disposed access rejects; scene-bound mutation requires its owner thread. Settings remain committed before Changed/PropertyListChanged callbacks; exception cleanup is described below. Referenced resources and scene nodes remain borrowed.

### CreateDuplicateInstance

`protected override Electron2D.Resource CreateDuplicateInstance()`

Inherited typed lifecycle/schema callback; executes the described resource contract.

Invalid numeric/count/index inputs throw ArgumentOutOfRangeException; invalid paths/vectors throw ArgumentException. Disposed access rejects; scene-bound mutation requires its owner thread. Settings remain committed before Changed/PropertyListChanged callbacks; exception cleanup is described below. Referenced resources and scene nodes remain borrowed.

### GetCollisionMask

`public System.UInt32 GetCollisionMask()`

Inherited typed lifecycle/schema callback; executes the described resource contract.

Invalid numeric/count/index inputs throw ArgumentOutOfRangeException; invalid paths/vectors throw ArgumentException. Disposed access rejects; scene-bound mutation requires its owner thread. Settings remain committed before Changed/PropertyListChanged callbacks; exception cleanup is described below. Referenced resources and scene nodes remain borrowed.

### GetJiggleJointBoneIndex

`public System.Int32 GetJiggleJointBoneIndex(System.Int32 jointIndex)`

Returns the resolved or authored bone index.

Minus one for a missing/unselected path.

- `jointIndex`: Valid joint slot.

Invalid numeric/count/index inputs throw ArgumentOutOfRangeException; invalid paths/vectors throw ArgumentException. Disposed access rejects; scene-bound mutation requires its owner thread. Settings remain committed before Changed/PropertyListChanged callbacks; exception cleanup is described below. Referenced resources and scene nodes remain borrowed.

### GetJiggleJointBoneNode

`public System.String GetJiggleJointBoneNode(System.Int32 jointIndex)`

Returns a joint's relative bone path.

Empty for deferred numeric selection; attached numeric setters author a path.

- `jointIndex`: Valid joint slot.

Invalid numeric/count/index inputs throw ArgumentOutOfRangeException; invalid paths/vectors throw ArgumentException. Disposed access rejects; scene-bound mutation requires its owner thread. Settings remain committed before Changed/PropertyListChanged callbacks; exception cleanup is described below. Referenced resources and scene nodes remain borrowed.

### GetJiggleJointDamping

`public System.Single GetJiggleJointDamping(System.Int32 jointIndex)`

Returns the actual joint damping used by simulation.

Acceleration contribution attenuation; velocity increments use one minus this value. Default .75.

- `jointIndex`: Valid joint slot.

Invalid numeric/count/index inputs throw ArgumentOutOfRangeException; invalid paths/vectors throw ArgumentException. Disposed access rejects; scene-bound mutation requires its owner thread. Settings remain committed before Changed/PropertyListChanged callbacks; exception cleanup is described below. Referenced resources and scene nodes remain borrowed.

### GetJiggleJointGravity

`public Electron2D.Vector2 GetJiggleJointGravity(System.Int32 jointIndex)`

Returns the actual joint gravity used by simulation.

Optional finite canvas-world force vector. Default new Vector2(0, 6).

- `jointIndex`: Valid joint slot.

Invalid numeric/count/index inputs throw ArgumentOutOfRangeException; invalid paths/vectors throw ArgumentException. Disposed access rejects; scene-bound mutation requires its owner thread. Settings remain committed before Changed/PropertyListChanged callbacks; exception cleanup is described below. Referenced resources and scene nodes remain borrowed.

### GetJiggleJointMass

`public System.Single GetJiggleJointMass(System.Int32 jointIndex)`

Returns the actual joint mass used by simulation.

Positive force-to-acceleration divisor. Default .75.

- `jointIndex`: Valid joint slot.

Invalid numeric/count/index inputs throw ArgumentOutOfRangeException; invalid paths/vectors throw ArgumentException. Disposed access rejects; scene-bound mutation requires its owner thread. Settings remain committed before Changed/PropertyListChanged callbacks; exception cleanup is described below. Referenced resources and scene nodes remain borrowed.

### GetJiggleJointOverride

`public System.Boolean GetJiggleJointOverride(System.Int32 jointIndex)`

Returns whether this joint is protected from default propagation.

False initially.

- `jointIndex`: Valid joint slot.

Invalid numeric/count/index inputs throw ArgumentOutOfRangeException; invalid paths/vectors throw ArgumentException. Disposed access rejects; scene-bound mutation requires its owner thread. Settings remain committed before Changed/PropertyListChanged callbacks; exception cleanup is described below. Referenced resources and scene nodes remain borrowed.

### GetJiggleJointStiffness

`public System.Single GetJiggleJointStiffness(System.Int32 jointIndex)`

Returns the actual joint stiffness used by simulation.

Nonnegative force multiplier. Default 3.

- `jointIndex`: Valid joint slot.

Invalid numeric/count/index inputs throw ArgumentOutOfRangeException; invalid paths/vectors throw ArgumentException. Disposed access rejects; scene-bound mutation requires its owner thread. Settings remain committed before Changed/PropertyListChanged callbacks; exception cleanup is described below. Referenced resources and scene nodes remain borrowed.

### GetJiggleJointUseGravity

`public System.Boolean GetJiggleJointUseGravity(System.Int32 jointIndex)`

Returns the actual joint usegravity used by simulation.

Whether gravity contributes to each force step. Default false.

- `jointIndex`: Valid joint slot.

Invalid numeric/count/index inputs throw ArgumentOutOfRangeException; invalid paths/vectors throw ArgumentException. Disposed access rejects; scene-bound mutation requires its owner thread. Settings remain committed before Changed/PropertyListChanged callbacks; exception cleanup is described below. Referenced resources and scene nodes remain borrowed.

### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Inherited typed lifecycle/schema callback; executes the described resource contract.

Invalid numeric/count/index inputs throw ArgumentOutOfRangeException; invalid paths/vectors throw ArgumentException. Disposed access rejects; scene-bound mutation requires its owner thread. Settings remain committed before Changed/PropertyListChanged callbacks; exception cleanup is described below. Referenced resources and scene nodes remain borrowed.

### GetUseColliders

`public System.Boolean GetUseColliders()`

Inherited typed lifecycle/schema callback; executes the described resource contract.

Invalid numeric/count/index inputs throw ArgumentOutOfRangeException; invalid paths/vectors throw ArgumentException. Disposed access rejects; scene-bound mutation requires its owner thread. Settings remain committed before Changed/PropertyListChanged callbacks; exception cleanup is described below. Referenced resources and scene nodes remain borrowed.

### OnExecute

`protected override System.Void OnExecute(System.Double delta)`

Inherited typed lifecycle/schema callback; executes the described resource contract.

Invalid numeric/count/index inputs throw ArgumentOutOfRangeException; invalid paths/vectors throw ArgumentException. Disposed access rejects; scene-bound mutation requires its owner thread. Settings remain committed before Changed/PropertyListChanged callbacks; exception cleanup is described below. Referenced resources and scene nodes remain borrowed.

### OnSetupModification

`protected override System.Void OnSetupModification(Electron2D.SkeletonModificationStack modificationStack)`

Inherited typed lifecycle/schema callback; executes the described resource contract.

Invalid numeric/count/index inputs throw ArgumentOutOfRangeException; invalid paths/vectors throw ArgumentException. Disposed access rejects; scene-bound mutation requires its owner thread. Settings remain committed before Changed/PropertyListChanged callbacks; exception cleanup is described below. Referenced resources and scene nodes remain borrowed.

### Reset

`public System.Void Reset()`

Inherited typed lifecycle/schema callback; executes the described resource contract.

Invalid numeric/count/index inputs throw ArgumentOutOfRangeException; invalid paths/vectors throw ArgumentException. Disposed access rejects; scene-bound mutation requires its owner thread. Settings remain committed before Changed/PropertyListChanged callbacks; exception cleanup is described below. Referenced resources and scene nodes remain borrowed.

### SetCollisionMask

`public System.Void SetCollisionMask(System.UInt32 collisionMask)`

Sets the unsigned 32-bit physics collision mask.

- `collisionMask`: Layer bits, including zero.

Invalid numeric/count/index inputs throw ArgumentOutOfRangeException; invalid paths/vectors throw ArgumentException. Disposed access rejects; scene-bound mutation requires its owner thread. Settings remain committed before Changed/PropertyListChanged callbacks; exception cleanup is described below. Referenced resources and scene nodes remain borrowed.

### SetJiggleJointBoneIndex

`public System.Void SetJiggleJointBoneIndex(System.Int32 jointIndex, System.Int32 boneIndex)`

Selects a bone by nonnegative index, authoring its path when attached and discarding history.

- `jointIndex`: Valid joint slot.
- `boneIndex`: Current index when attached; nonnegative deferred index otherwise.

Invalid numeric/count/index inputs throw ArgumentOutOfRangeException; invalid paths/vectors throw ArgumentException. Disposed access rejects; scene-bound mutation requires its owner thread. Settings remain committed before Changed/PropertyListChanged callbacks; exception cleanup is described below. Referenced resources and scene nodes remain borrowed.

### SetJiggleJointBoneNode

`public System.Void SetJiggleJointBoneNode(System.Int32 jointIndex, System.String boneNode)`

Selects a joint by relative bone path and discards its previous history.

- `jointIndex`: Valid joint slot.
- `boneNode`: Path relative to its skeleton.

Invalid numeric/count/index inputs throw ArgumentOutOfRangeException; invalid paths/vectors throw ArgumentException. Disposed access rejects; scene-bound mutation requires its owner thread. Settings remain committed before Changed/PropertyListChanged callbacks; exception cleanup is described below. Referenced resources and scene nodes remain borrowed.

### SetJiggleJointDamping

`public System.Void SetJiggleJointDamping(System.Int32 jointIndex, System.Single value)`

Sets the actual joint damping; enable Override to retain it through default edits.

- `jointIndex`: Valid joint slot.
- `value`: Acceleration contribution attenuation; velocity increments use one minus this value.

Invalid numeric/count/index inputs throw ArgumentOutOfRangeException; invalid paths/vectors throw ArgumentException. Disposed access rejects; scene-bound mutation requires its owner thread. Settings remain committed before Changed/PropertyListChanged callbacks; exception cleanup is described below. Referenced resources and scene nodes remain borrowed.

### SetJiggleJointGravity

`public System.Void SetJiggleJointGravity(System.Int32 jointIndex, Electron2D.Vector2 value)`

Sets the actual joint gravity; enable Override to retain it through default edits.

- `jointIndex`: Valid joint slot.
- `value`: Optional finite canvas-world force vector.

Invalid numeric/count/index inputs throw ArgumentOutOfRangeException; invalid paths/vectors throw ArgumentException. Disposed access rejects; scene-bound mutation requires its owner thread. Settings remain committed before Changed/PropertyListChanged callbacks; exception cleanup is described below. Referenced resources and scene nodes remain borrowed.

### SetJiggleJointMass

`public System.Void SetJiggleJointMass(System.Int32 jointIndex, System.Single value)`

Sets the actual joint mass; enable Override to retain it through default edits.

- `jointIndex`: Valid joint slot.
- `value`: Positive force-to-acceleration divisor.

Invalid numeric/count/index inputs throw ArgumentOutOfRangeException; invalid paths/vectors throw ArgumentException. Disposed access rejects; scene-bound mutation requires its owner thread. Settings remain committed before Changed/PropertyListChanged callbacks; exception cleanup is described below. Referenced resources and scene nodes remain borrowed.

### SetJiggleJointOverride

`public System.Void SetJiggleJointOverride(System.Int32 jointIndex, System.Boolean value)`

Authors default override policy; disabling copies current defaults immediately.

- `jointIndex`: Valid joint slot.
- `value`: Whether to retain independent settings through default edits.

Invalid numeric/count/index inputs throw ArgumentOutOfRangeException; invalid paths/vectors throw ArgumentException. Disposed access rejects; scene-bound mutation requires its owner thread. Settings remain committed before Changed/PropertyListChanged callbacks; exception cleanup is described below. Referenced resources and scene nodes remain borrowed.

### SetJiggleJointStiffness

`public System.Void SetJiggleJointStiffness(System.Int32 jointIndex, System.Single value)`

Sets the actual joint stiffness; enable Override to retain it through default edits.

- `jointIndex`: Valid joint slot.
- `value`: Nonnegative force multiplier.

Invalid numeric/count/index inputs throw ArgumentOutOfRangeException; invalid paths/vectors throw ArgumentException. Disposed access rejects; scene-bound mutation requires its owner thread. Settings remain committed before Changed/PropertyListChanged callbacks; exception cleanup is described below. Referenced resources and scene nodes remain borrowed.

### SetJiggleJointUseGravity

`public System.Void SetJiggleJointUseGravity(System.Int32 jointIndex, System.Boolean value)`

Sets the actual joint usegravity; enable Override to retain it through default edits.

- `jointIndex`: Valid joint slot.
- `value`: Whether gravity contributes to each force step.

Invalid numeric/count/index inputs throw ArgumentOutOfRangeException; invalid paths/vectors throw ArgumentException. Disposed access rejects; scene-bound mutation requires its owner thread. Settings remain committed before Changed/PropertyListChanged callbacks; exception cleanup is described below. Referenced resources and scene nodes remain borrowed.

### SetUseColliders

`public System.Void SetUseColliders(System.Boolean useColliders)`

Enables physics-only collider rejection.

Execution with colliders in Idle rejects before advancing state. Select ProcessPhase.Physics.

- `useColliders`: Whether to ray-test candidate dynamic points.

Invalid numeric/count/index inputs throw ArgumentOutOfRangeException; invalid paths/vectors throw ArgumentException. Disposed access rejects; scene-bound mutation requires its owner thread. Settings remain committed before Changed/PropertyListChanged callbacks; exception cleanup is described below. Referenced resources and scene nodes remain borrowed.

### Damping

`public System.Single Damping { get; set; }`

Gets or sets the default joint damping. Acceleration contribution attenuation; velocity increments use one minus this value.

.75 initially; changes copy all defaults to nonoverriding joints.

Invalid numeric/count/index inputs throw ArgumentOutOfRangeException; invalid paths/vectors throw ArgumentException. Disposed access rejects; scene-bound mutation requires its owner thread. Settings remain committed before Changed/PropertyListChanged callbacks; exception cleanup is described below. Referenced resources and scene nodes remain borrowed.

### Gravity

`public Electron2D.Vector2 Gravity { get; set; }`

Gets or sets the default joint gravity. Optional finite canvas-world force vector.

new Vector2(0, 6) initially; changes copy all defaults to nonoverriding joints.

Invalid numeric/count/index inputs throw ArgumentOutOfRangeException; invalid paths/vectors throw ArgumentException. Disposed access rejects; scene-bound mutation requires its owner thread. Settings remain committed before Changed/PropertyListChanged callbacks; exception cleanup is described below. Referenced resources and scene nodes remain borrowed.

### JiggleDataChainLength

`public System.Int32 JiggleDataChainLength { get; set; }`

Gets or resizes the joint list, retaining the prefix and seeding new slots from current defaults.

Zero initially; zero through 4096. Structural changes clear simulation history.

Invalid numeric/count/index inputs throw ArgumentOutOfRangeException; invalid paths/vectors throw ArgumentException. Disposed access rejects; scene-bound mutation requires its owner thread. Settings remain committed before Changed/PropertyListChanged callbacks; exception cleanup is described below. Referenced resources and scene nodes remain borrowed.

### Mass

`public System.Single Mass { get; set; }`

Gets or sets the default joint mass. Positive force-to-acceleration divisor.

.75 initially; changes copy all defaults to nonoverriding joints.

Invalid numeric/count/index inputs throw ArgumentOutOfRangeException; invalid paths/vectors throw ArgumentException. Disposed access rejects; scene-bound mutation requires its owner thread. Settings remain committed before Changed/PropertyListChanged callbacks; exception cleanup is described below. Referenced resources and scene nodes remain borrowed.

### Stiffness

`public System.Single Stiffness { get; set; }`

Gets or sets the default joint stiffness. Nonnegative force multiplier.

3 initially; changes copy all defaults to nonoverriding joints.

Invalid numeric/count/index inputs throw ArgumentOutOfRangeException; invalid paths/vectors throw ArgumentException. Disposed access rejects; scene-bound mutation requires its owner thread. Settings remain committed before Changed/PropertyListChanged callbacks; exception cleanup is described below. Referenced resources and scene nodes remain borrowed.

### TargetNodePath

`public System.String TargetNodePath { get; set; }`

Gets or sets the Entity target path relative to its skeleton.

Empty initially. Missing targets freeze history and restore ordinary authored poses.

Invalid numeric/count/index inputs throw ArgumentOutOfRangeException; invalid paths/vectors throw ArgumentException. Disposed access rejects; scene-bound mutation requires its owner thread. Settings remain committed before Changed/PropertyListChanged callbacks; exception cleanup is described below. Referenced resources and scene nodes remain borrowed.

### UseGravity

`public System.Boolean UseGravity { get; set; }`

Gets or sets the default joint usegravity. Whether gravity contributes to each force step.

false initially; changes copy all defaults to nonoverriding joints.

Invalid numeric/count/index inputs throw ArgumentOutOfRangeException; invalid paths/vectors throw ArgumentException. Disposed access rejects; scene-bound mutation requires its owner thread. Settings remain committed before Changed/PropertyListChanged callbacks; exception cleanup is described below. Referenced resources and scene nodes remain borrowed.

## Ownership, storage, failure and verification

Stacks/modifications remain caller-owned borrowed resources. Scene binding is weak, follows one attached skeleton and rejects competing live bindings. Setup callbacks run after binding commitment; failures retain assigned resources and keep the affected parent unprepared for explicit Setup retry. Structural graph replacement, disposal or binding reset during running setup/execution rejects before terminal changes. Nested setup/execution depth is bounded to 128; cold graph validation also caps 16384 visited stacks. Warm solve/replay uses existing scene ownership and reusable resource state, independent of graphics startup.

PackedScene already force-copies a rig's stack graph. Holder copying always force-copies the child graph through the same duplication session, preserving shared child/modification aliases with independent bindings per scene instance. Exact built-in schemas/factories persist configuration; no target caches, holder leases, force/velocity histories or native identities are saved. Joint blobs are versioned, limited to 4096 joints and 64 MiB and validated fully before replacement. Loading in another process executes the ordinary public controller path.

SkeletonJiggleTests checks exact force/velocity samples, reset/nonzero origins, gravity/default overrides, guarded numeric inputs, reflected aiming, path reconnection, actual collision/mask/phase behavior, direct child selection, independent child strength, shared-child removal, cycles, setup retry, running disposal/reentry, graph copies/aliases and fresh .e2dscene execution. 128 prepared nested simulation cycles report zero managed bytes. Native Engine.Run hosts on current Linux GPU/compatibility verify actual Jiggle, held-stack and ray-blocked Polygon pixels and 64 prepared render/query/solve intervals at zero managed allocation. Native allocator behavior, large graphs/rig throughput, foreign targets and owner acceptance remain unverified. Editor gizmos, PhysicalBone synchronization, server palettes and generic Mesh skin channels keep separate coverage dependencies.

See [skeletal animation](../components/skeletal-animation.md), [Skeleton](Skeleton.md), [SkeletonModificationStack](SkeletonModificationStack.md), [physics queries](PhysicsDirectSpaceState.md) and [ADR 0092](../decisions/mesh.md#adr-0092).
