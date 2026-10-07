# SkeletonModificationLookAt

Last updated: 2026-10-07

- Declaration: `public sealed class SkeletonModificationLookAt : SkeletonModification`
- Source: [SkeletonModificationLookAt.cs](../../src/Scene/Resources/SkeletonModificationLookAt.cs)
- Inherits: [SkeletonModification](SkeletonModification.md)
- Component: [Skeletal animation](../components/skeletal-animation.md)

## Description

Aims one bone at a borrowed Entity target. BoneNode and TargetNodePath resolve relative to the rig; BoneNode takes precedence and assigning BoneIndex clears it. Numeric selection defaults -1. Missing paths, an orphan bone or disposed target skip execution until a path/setup revision reconnects them. Scene rename/membership revisions update before public callbacks, so a failed event handler does not leave stale paths.

The rotation offset subtracts Bone.GetBoneAngle and adds GetAdditionalRotation. Constraint limits default [0, Tau], disabled and noninverted; ConstraintInLocalSpace defaults true as the typed stored coordinate policy. Global constraints use GlobalRotation while preserving Entity transforms. The resulting temporary local pose is submitted at stack.Strength as a transient override. Disabling the modifier or stack restores the authored animation pose on the next idle application. All settings copy/store; weak caches are never persisted.

## Example

This excerpt uses the existing scene context indicated in comments. SkeletonTests exercises the complete producer/consumer path.

```csharp
var target = new Entity { Name = "Target", Position = new(100, 80) };
window.AddChild(target); // Existing Window and sibling Skeleton contexts.
using var aim = new SkeletonModificationLookAt
    { BoneNode = "Upper/Lower", TargetNodePath = "../Target" };
aim.SetEnableConstraint(true);
aim.SetConstraintAngleMin(-.4f);
aim.SetConstraintAngleMax(.6f);
using var stack = new SkeletonModificationStack { Enabled = true };
stack.AddModification(aim);
rig.SetModificationStack(stack);
```

## Constructors

| Signature | Contract |
| --- | --- |
| `public SkeletonModificationLookAt()` | [Inherited lifecycle/schema override.](#ctor) |

## Properties

| Signature | Contract |
| --- | --- |
| `public System.Int32 BoneIndex { get; set; }` | [Gets or sets the explicit bone index when BoneNode is empty.](#boneindex) |
| `public System.String BoneNode { get; set; }` | [Gets or sets the relative bone path, overriding a numeric selection.](#bonenode) |
| `public System.Boolean ConstraintInLocalSpace { get; set; }` | [Gets or sets whether limits are relative to the bone's parent or world basis.](#constraintinlocalspace) |
| `public System.String TargetNodePath { get; set; }` | [Gets or sets the target path relative to its skeleton.](#targetnodepath) |

## Methods and protected extension points

| Signature | Contract |
| --- | --- |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)` | [Inherited lifecycle/schema override.](#copycustomstateto) |
| `protected override Electron2D.Resource CreateDuplicateInstance()` | [Inherited lifecycle/schema override.](#createduplicateinstance) |
| `public System.Single GetAdditionalRotation()` | [Inherited lifecycle/schema override.](#getadditionalrotation) |
| `public System.Boolean GetConstraintAngleInvert()` | [Inherited lifecycle/schema override.](#getconstraintangleinvert) |
| `public System.Single GetConstraintAngleMax()` | [Inherited lifecycle/schema override.](#getconstraintanglemax) |
| `public System.Single GetConstraintAngleMin()` | [Inherited lifecycle/schema override.](#getconstraintanglemin) |
| `public System.Boolean GetEnableConstraint()` | [Inherited lifecycle/schema override.](#getenableconstraint) |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | [Inherited lifecycle/schema override.](#getpropertydescriptors) |
| `protected override System.Void OnExecute(System.Double delta)` | [Inherited lifecycle/schema override.](#onexecute) |
| `protected override System.Void OnResetState()` | [Inherited lifecycle/schema override.](#onresetstate) |
| `protected override System.Void OnSetupModification(Electron2D.SkeletonModificationStack modificationStack)` | [Inherited lifecycle/schema override.](#onsetupmodification) |
| `public System.Void SetAdditionalRotation(System.Single rotation)` | [Sets additional finite aiming rotation.](#setadditionalrotation) |
| `public System.Void SetConstraintAngleInvert(System.Boolean invert)` | [Selects the complement of the angle interval.](#setconstraintangleinvert) |
| `public System.Void SetConstraintAngleMax(System.Single angle)` | [Sets the finite upper angle boundary.](#setconstraintanglemax) |
| `public System.Void SetConstraintAngleMin(System.Single angle)` | [Sets the finite lower angle boundary.](#setconstraintanglemin) |
| `public System.Void SetEnableConstraint(System.Boolean enableConstraint)` | [Enables angle constraints.](#setenableconstraint) |

## Member descriptions

### .ctor

`public SkeletonModificationLookAt()`

Inherited lifecycle/schema override; see the linked base type.

### CopyCustomStateTo

`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)`

Inherited lifecycle/schema override; see the linked base type.

### CreateDuplicateInstance

`protected override Electron2D.Resource CreateDuplicateInstance()`

Inherited lifecycle/schema override; see the linked base type.

### GetAdditionalRotation

`public System.Single GetAdditionalRotation()`

Inherited lifecycle/schema override; see the linked base type.

### GetConstraintAngleInvert

`public System.Boolean GetConstraintAngleInvert()`

Inherited lifecycle/schema override; see the linked base type.

### GetConstraintAngleMax

`public System.Single GetConstraintAngleMax()`

Inherited lifecycle/schema override; see the linked base type.

### GetConstraintAngleMin

`public System.Single GetConstraintAngleMin()`

Inherited lifecycle/schema override; see the linked base type.

### GetEnableConstraint

`public System.Boolean GetEnableConstraint()`

Inherited lifecycle/schema override; see the linked base type.

### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Inherited lifecycle/schema override; see the linked base type.

### OnExecute

`protected override System.Void OnExecute(System.Double delta)`



### OnResetState

`protected override System.Void OnResetState()`

Inherited lifecycle/schema override; see the linked base type.

### OnSetupModification

`protected override System.Void OnSetupModification(Electron2D.SkeletonModificationStack modificationStack)`



### SetAdditionalRotation

`public System.Void SetAdditionalRotation(System.Single rotation)`

Sets additional finite aiming rotation.

- `rotation`: Radians.

### SetConstraintAngleInvert

`public System.Void SetConstraintAngleInvert(System.Boolean invert)`

Selects the complement of the angle interval.

- `invert`: Whether to invert its interior.

### SetConstraintAngleMax

`public System.Void SetConstraintAngleMax(System.Single angle)`

Sets the finite upper angle boundary.

- `angle`: Radians.

### SetConstraintAngleMin

`public System.Void SetConstraintAngleMin(System.Single angle)`

Sets the finite lower angle boundary.

- `angle`: Radians.

### SetEnableConstraint

`public System.Void SetEnableConstraint(System.Boolean enableConstraint)`

Enables angle constraints.

- `enableConstraint`: Whether to clamp the resulting angle.

### BoneIndex

`public System.Int32 BoneIndex { get; set; }`

Gets or sets the explicit bone index when BoneNode is empty.

Minus one initially.

### BoneNode

`public System.String BoneNode { get; set; }`

Gets or sets the relative bone path, overriding a numeric selection.

Empty initially.

### ConstraintInLocalSpace

`public System.Boolean ConstraintInLocalSpace { get; set; }`

Gets or sets whether limits are relative to the bone's parent or world basis.

True initially; typed projection of the stored constraint_in_localspace setting.

### TargetNodePath

`public System.String TargetNodePath { get; set; }`

Gets or sets the target path relative to its skeleton.

Empty initially; target must be a live Entity.

## Lifecycle, errors and verification

Scene-bound calls follow the skeleton's owner thread. Cold structure/rest/path edits prepare new arrays and weak bindings; ordinary pose updates and retained replay reuse capacity. All numeric configuration requires finite values; strengths range from zero to one and phase selections use the existing ProcessPhase semantics. Invalid indices, disposed values, competing live bindings and structural reentry reject. User callbacks run after binding or setup commitment. Execution failures attempt every authored-pose restoration and release guards; setup failures keep GetIsSetup false for explicit retry. Resource graph persistence excludes transient setup, target references, palette RIDs and simulation history.

SkeletonTests exercises DFS membership/rest/indices, auto endpoints, persistent/transient/physics overrides, AnimationPlayer tracks, presentation interpolation, noncommuting and TopLevel coordinate transforms, strongest-four skin weights, path rename/reconnect, constraints, callback failures/reentry, owner guards, unique scene copies, fresh-process files and actual native pixels. 128 warmed managed pose/redraw/skin/replay iterations and 64 prepared native GPU/compatibility intervals allocate zero managed bytes. Native/backend allocations, large-rig performance, other platforms and owner acceptance are unverified. Editor gizmos retain their authoring trigger; IK solvers, physical bones and general Mesh skin now execute through their concrete components.

See [skeletal animation](../components/skeletal-animation.md), [the mesh decision](../decisions/mesh.md#adr-0092) and the linked base-class lifecycle.
