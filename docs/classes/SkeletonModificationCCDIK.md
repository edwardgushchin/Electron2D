# SkeletonModificationCCDIK

Last updated: 2026-10-07

- Declaration: `public sealed class SkeletonModificationCCDIK : SkeletonModification`
- Source: [SkeletonModificationCCDIK.cs](../../src/Scene/Resources/SkeletonModificationCCDIK.cs)
- Inherits: [SkeletonModification](SkeletonModification.md)
- Component: [Skeletal animation](../components/skeletal-animation.md)

## Description

Cyclic coordinate descent with an explicit tip Entity and one pass in authored joint order. A tip-to-target angular offset rotates each joint after preceding joints update the tip's world position. RotateFromJoint instead aims the bone endpoint directly, accounting for GetBoneAngle. Configuration order matters: distal-to-proximal often reaches a simple limb in one pass; no unexposed iterative CCD budget is invented. A missing target/tip skips the pass and an invalid individual bone skips that joint.

Each joint can apply finite angular min/max, inversion and local or global coordinate policy. ConstraintInLocalSpace defaults true and is exposed through typed indexed methods for the actual stored coordinate setting. Circular normalization and nearest-endpoint clamping use the existing base ClampAngle. Coincident tip/target direction and singular bone transforms skip the undefined angular update. No translation is authored. Every solved joint immediately changes its temporary transform and submits a transient override for child/global composition and final stack strength.

## Example

This excerpt requires the indicated existing scene; SkeletonIKTests supplies complete executable producers/consumers.

```csharp
using var ik = new SkeletonModificationCCDIK
    { CCDIKDataChainLength = 2, TargetNodePath = "../Target", TipNodePath = "Upper/Lower/Tip" };
ik.SetCCDIKJointBoneNode(0, "Upper/Lower");
ik.SetCCDIKJointBoneNode(1, "Upper");
ik.SetCCDIKJointEnableConstraint(1, true);
ik.SetCCDIKJointConstraintAngleMax(1, .6f);
using var stack = new SkeletonModificationStack { Enabled = true };
stack.AddModification(ik);
rig.SetModificationStack(stack); // Existing scene context.
```

## Constructors

| Signature | Contract |
| --- | --- |
| `public SkeletonModificationCCDIK()` | [Inherited typed resource/execute callback.](#ctor) |

## Properties

| Signature | Contract |
| --- | --- |
| `public System.Int32 CCDIKDataChainLength { get; set; }` | [Gets or resizes the number of authored joints, preserving the retained prefix.](#ccdikdatachainlength) |
| `public System.String TargetNodePath { get; set; }` | [Gets or sets the Entity target path relative to its skeleton.](#targetnodepath) |
| `public System.String TipNodePath { get; set; }` | [Gets or sets the explicit endpoint path relative to its skeleton.](#tipnodepath) |

## Methods and protected extension points

| Signature | Contract |
| --- | --- |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)` | [Inherited typed resource/execute callback.](#copycustomstateto) |
| `protected override Electron2D.Resource CreateDuplicateInstance()` | [Inherited typed resource/execute callback.](#createduplicateinstance) |
| `public System.Int32 GetCCDIKJointBoneIndex(System.Int32 jointIndex)` | [Returns a joint's resolved or authored bone index.](#getccdikjointboneindex) |
| `public System.String GetCCDIKJointBoneNode(System.Int32 jointIndex)` | [Returns a joint's authored relative bone path.](#getccdikjointbonenode) |
| `public System.Boolean GetCCDIKJointConstraintAngleInvert(System.Int32 jointIndex)` | [Returns whether the permitted angle interval is inverted.](#getccdikjointconstraintangleinvert) |
| `public System.Single GetCCDIKJointConstraintAngleMax(System.Int32 jointIndex)` | [Returns upper angular boundary in radians.](#getccdikjointconstraintanglemax) |
| `public System.Single GetCCDIKJointConstraintAngleMin(System.Int32 jointIndex)` | [Returns lower angular boundary in radians.](#getccdikjointconstraintanglemin) |
| `public System.Boolean GetCCDIKJointConstraintInLocalSpace(System.Int32 jointIndex)` | [Returns whether limits use parent-relative rotation rather than canvas-world rotation.](#getccdikjointconstraintinlocalspace) |
| `public System.Boolean GetCCDIKJointEnableConstraint(System.Int32 jointIndex)` | [Returns whether joint angle limits apply.](#getccdikjointenableconstraint) |
| `public System.Boolean GetCCDIKJointRotateFromJoint(System.Int32 jointIndex)` | [Returns whether to aim the bone endpoint directly instead of rotating the tip-to-target offset.](#getccdikjointrotatefromjoint) |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | [Inherited typed resource/execute callback.](#getpropertydescriptors) |
| `protected override System.Void OnExecute(System.Double delta)` | [Inherited typed resource/execute callback.](#onexecute) |
| `public System.Void SetCCDIKJointBoneIndex(System.Int32 jointIndex, System.Int32 boneIndex)` | [Selects by nonnegative index and authors its path when attached.](#setccdikjointboneindex) |
| `public System.Void SetCCDIKJointBoneNode(System.Int32 jointIndex, System.String boneNode)` | [Selects a joint by relative bone path.](#setccdikjointbonenode) |
| `public System.Void SetCCDIKJointConstraintAngleInvert(System.Int32 jointIndex, System.Boolean value)` | [Sets whether the permitted angle interval is inverted.](#setccdikjointconstraintangleinvert) |
| `public System.Void SetCCDIKJointConstraintAngleMax(System.Int32 jointIndex, System.Single value)` | [Sets upper angular boundary in radians.](#setccdikjointconstraintanglemax) |
| `public System.Void SetCCDIKJointConstraintAngleMin(System.Int32 jointIndex, System.Single value)` | [Sets lower angular boundary in radians.](#setccdikjointconstraintanglemin) |
| `public System.Void SetCCDIKJointConstraintInLocalSpace(System.Int32 jointIndex, System.Boolean value)` | [Sets whether limits use parent-relative rotation rather than canvas-world rotation.](#setccdikjointconstraintinlocalspace) |
| `public System.Void SetCCDIKJointEnableConstraint(System.Int32 jointIndex, System.Boolean value)` | [Sets whether joint angle limits apply.](#setccdikjointenableconstraint) |
| `public System.Void SetCCDIKJointRotateFromJoint(System.Int32 jointIndex, System.Boolean value)` | [Sets whether to aim the bone endpoint directly instead of rotating the tip-to-target offset.](#setccdikjointrotatefromjoint) |

## Member descriptions

### .ctor

`public SkeletonModificationCCDIK()`

Inherited typed resource/execute callback; executes the contract described above.

### CopyCustomStateTo

`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)`

Inherited typed resource/execute callback; executes the contract described above.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### CreateDuplicateInstance

`protected override Electron2D.Resource CreateDuplicateInstance()`

Inherited typed resource/execute callback; executes the contract described above.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### GetCCDIKJointBoneIndex

`public System.Int32 GetCCDIKJointBoneIndex(System.Int32 jointIndex)`

Returns a joint's resolved or authored bone index.

Minus one for unselected/missing paths.

- `jointIndex`: Valid joint slot.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### GetCCDIKJointBoneNode

`public System.String GetCCDIKJointBoneNode(System.Int32 jointIndex)`

Returns a joint's authored relative bone path.

Empty for deferred numeric selection; attached numeric setters author a path.

- `jointIndex`: Valid joint slot.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### GetCCDIKJointConstraintAngleInvert

`public System.Boolean GetCCDIKJointConstraintAngleInvert(System.Int32 jointIndex)`

Returns whether the permitted angle interval is inverted.

Authored setting; default false.

- `jointIndex`: Valid joint slot.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### GetCCDIKJointConstraintAngleMax

`public System.Single GetCCDIKJointConstraintAngleMax(System.Int32 jointIndex)`

Returns upper angular boundary in radians.

Authored setting; default Tau.

- `jointIndex`: Valid joint slot.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### GetCCDIKJointConstraintAngleMin

`public System.Single GetCCDIKJointConstraintAngleMin(System.Int32 jointIndex)`

Returns lower angular boundary in radians.

Authored setting; default 0.

- `jointIndex`: Valid joint slot.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### GetCCDIKJointConstraintInLocalSpace

`public System.Boolean GetCCDIKJointConstraintInLocalSpace(System.Int32 jointIndex)`

Returns whether limits use parent-relative rotation rather than canvas-world rotation.

Authored setting; default true.

- `jointIndex`: Valid joint slot.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### GetCCDIKJointEnableConstraint

`public System.Boolean GetCCDIKJointEnableConstraint(System.Int32 jointIndex)`

Returns whether joint angle limits apply.

Authored setting; default false.

- `jointIndex`: Valid joint slot.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### GetCCDIKJointRotateFromJoint

`public System.Boolean GetCCDIKJointRotateFromJoint(System.Int32 jointIndex)`

Returns whether to aim the bone endpoint directly instead of rotating the tip-to-target offset.

Authored setting; default false.

- `jointIndex`: Valid joint slot.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Inherited typed resource/execute callback; executes the contract described above.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### OnExecute

`protected override System.Void OnExecute(System.Double delta)`

Inherited typed resource/execute callback; executes the contract described above.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### SetCCDIKJointBoneIndex

`public System.Void SetCCDIKJointBoneIndex(System.Int32 jointIndex, System.Int32 boneIndex)`

Selects by nonnegative index and authors its path when attached.

- `jointIndex`: Valid joint slot.
- `boneIndex`: Current valid skeleton index, or nonnegative deferred selection.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### SetCCDIKJointBoneNode

`public System.Void SetCCDIKJointBoneNode(System.Int32 jointIndex, System.String boneNode)`

Selects a joint by relative bone path.

- `jointIndex`: Valid joint slot.
- `boneNode`: Path relative to the skeleton; missing paths do not fall back to an old index.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### SetCCDIKJointConstraintAngleInvert

`public System.Void SetCCDIKJointConstraintAngleInvert(System.Int32 jointIndex, System.Boolean value)`

Sets whether the permitted angle interval is inverted.

- `jointIndex`: Valid joint slot.
- `value`: Requested policy.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### SetCCDIKJointConstraintAngleMax

`public System.Void SetCCDIKJointConstraintAngleMax(System.Int32 jointIndex, System.Single value)`

Sets upper angular boundary in radians.

- `jointIndex`: Valid joint slot.
- `value`: Finite radians.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### SetCCDIKJointConstraintAngleMin

`public System.Void SetCCDIKJointConstraintAngleMin(System.Int32 jointIndex, System.Single value)`

Sets lower angular boundary in radians.

- `jointIndex`: Valid joint slot.
- `value`: Finite radians.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### SetCCDIKJointConstraintInLocalSpace

`public System.Void SetCCDIKJointConstraintInLocalSpace(System.Int32 jointIndex, System.Boolean value)`

Sets whether limits use parent-relative rotation rather than canvas-world rotation.

- `jointIndex`: Valid joint slot.
- `value`: Requested policy.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### SetCCDIKJointEnableConstraint

`public System.Void SetCCDIKJointEnableConstraint(System.Int32 jointIndex, System.Boolean value)`

Sets whether joint angle limits apply.

- `jointIndex`: Valid joint slot.
- `value`: Requested policy.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### SetCCDIKJointRotateFromJoint

`public System.Void SetCCDIKJointRotateFromJoint(System.Int32 jointIndex, System.Boolean value)`

Sets whether to aim the bone endpoint directly instead of rotating the tip-to-target offset.

- `jointIndex`: Valid joint slot.
- `value`: Requested policy.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### CCDIKDataChainLength

`public System.Int32 CCDIKDataChainLength { get; set; }`

Gets or resizes the number of authored joints, preserving the retained prefix.

Zero initially; zero through 4096. New slots have no bone selection.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### TargetNodePath

`public System.String TargetNodePath { get; set; }`

Gets or sets the Entity target path relative to its skeleton.

Empty initially; missing targets skip execution.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### TipNodePath

`public System.String TipNodePath { get; set; }`

Gets or sets the explicit endpoint path relative to its skeleton.

Empty initially. Typically an Entity child of the final bone.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

## Lifecycle, errors, storage and verification

Stacks borrow the resource and scene selections. Setters follow the bound skeleton owner; copied configuration is guarded by the resource gate. Joint/target bindings are weak and refresh on scene path, hierarchy/rest, owner or selection changes. Missing/renamed/orphan selections do not fall back to a stale index. A bound numeric setter validates the current index and authors its relative path, retaining the bone through later reordering; a detached setter keeps its nonnegative deferred index. Initial unselected indices are -1. Paths are bounded to 65536 characters and contain no NUL; counts range from zero to 4096; numeric inputs must be finite. Invalid setters/archives preserve configuration. Attached disposal and stack ownership follow SkeletonModification/Stack.

Enabled and ExecutionMode retain base semantics. Solvers request transient local overrides at stack.Strength. Physics stages them and restores authored poses; Idle applies them. Disable returns to ordinary authored animation. If a modifier/notification fails, Skeleton restores the pre-pass override/strength/persistence arrays as well as authored poses, preventing delayed changes from a failed pass and preserving prior requests. Structural rest/hierarchy mutation still rejects mixed execution. Cold structural changes, copying and files may allocate; prepared solve loops reuse bindings/scratch.

Built-in exact factories and stored descriptors restore settings through .e2dscene/PackedScene. Chains use a versioned blob with joint selection and scalar/vector policies, bounded to 64 MiB and validated completely before replacement. Copying owns independent joint records and drops transient scratch/binding. PackedScene already force-copies complete stack graphs per rig, preserving resource aliases and instance-owned lifetime. Custom resource consumers still use the existing typed registration contract.

SkeletonIKTests exercises actual target endpoints, bend/length/distance constraints, mirrors/endpoint angles, local/global/inverted CCD limits, magnets, final orientation, three-joint convergence, degenerate and missing selections, copied/fresh-process scenes, failed-pass rollback and 128 prepared iterations per solver with zero managed bytes. Engine.Run hosts verify all three solvers' deformed Polygon pixels on current Linux GPU/compatibility and 64 prepared render/solve intervals with zero managed bytes. Native allocator activity, large-rig performance, foreign platforms and owner acceptance remain unverified. Editor gizmos retain their exact authoring trigger; server palettes and general Mesh skin channels now execute through the mesh component.

See [skeletal animation](../components/skeletal-animation.md), [Skeleton](Skeleton.md), [SkeletonModificationStack](SkeletonModificationStack.md) and [ADR 0092](../decisions/mesh.md#adr-0092).
