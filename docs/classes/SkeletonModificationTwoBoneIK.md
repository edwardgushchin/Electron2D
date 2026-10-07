# SkeletonModificationTwoBoneIK

Last updated: 2026-10-07

- Declaration: `public sealed class SkeletonModificationTwoBoneIK : SkeletonModification`
- Source: [SkeletonModificationTwoBoneIK.cs](../../src/Scene/Resources/SkeletonModificationTwoBoneIK.cs)
- Inherits: [SkeletonModification](SkeletonModification.md)
- Component: [Skeletal animation](../components/skeletal-animation.md)

## Description

A two-segment limb solver using the law of cosines. The root-to-target solve distance first clamps to TargetMinimumDistance and then to a positive TargetMaximumDistance (maximum wins if limits conflict). Zero disables each limit. Targets beyond total reach extend the limb; targets inside the inner reach fold to the closest attainable distance. A coincident equal-length target folds finitely using the existing endpoint direction. FlipBendDirection reverses the elbow side. Bone endpoint angles and reflected handedness participate.

Bone lengths use GetLength times the minimum absolute global scale. Nonpositive lengths, duplicate selections or missing target/bones skip execution and preserve authoring. Selected bones should form the authored two-segment limb with child offsets matching endpoint length/direction. Nonuniform/sheared transforms keep the conservative minimum-scale length policy; exact endpoint reach is verified for ordinary and reflected uniform bases. Rotations use Entity's existing setters, preserving ordinary local scale/skew/position. No positions are teleported by this solver.

## Example

This excerpt requires the indicated existing scene; SkeletonIKTests supplies complete executable producers/consumers.

```csharp
using var ik = new SkeletonModificationTwoBoneIK
    { TargetNodePath = "../Target", TargetMaximumDistance = 80 };
ik.SetJointOneBoneNode("Upper");
ik.SetJointTwoBoneNode("Upper/Lower");
using var stack = new SkeletonModificationStack { Enabled = true };
stack.AddModification(ik);
rig.SetModificationStack(stack); // Existing Skeleton and target in a live scene.
```

## Constructors

| Signature | Contract |
| --- | --- |
| `public SkeletonModificationTwoBoneIK()` | [Inherited typed resource/execute callback.](#ctor) |

## Properties

| Signature | Contract |
| --- | --- |
| `public System.Boolean FlipBendDirection { get; set; }` | [Gets or sets whether the limb bends toward the opposite side.](#flipbenddirection) |
| `public System.Single TargetMaximumDistance { get; set; }` | [Gets or sets maximum root-to-target solve distance in canvas-world pixels.](#targetmaximumdistance) |
| `public System.Single TargetMinimumDistance { get; set; }` | [Gets or sets minimum root-to-target solve distance in canvas-world pixels.](#targetminimumdistance) |
| `public System.String TargetNodePath { get; set; }` | [Gets or sets relative scene path of the Entity target.](#targetnodepath) |

## Methods and protected extension points

| Signature | Contract |
| --- | --- |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)` | [Inherited typed resource/execute callback.](#copycustomstateto) |
| `protected override Electron2D.Resource CreateDuplicateInstance()` | [Inherited typed resource/execute callback.](#createduplicateinstance) |
| `public System.Int32 GetJointOneBoneIndex()` | [Inherited typed resource/execute callback.](#getjointoneboneindex) |
| `public System.String GetJointOneBoneNode()` | [Inherited typed resource/execute callback.](#getjointonebonenode) |
| `public System.Int32 GetJointTwoBoneIndex()` | [Inherited typed resource/execute callback.](#getjointtwoboneindex) |
| `public System.String GetJointTwoBoneNode()` | [Inherited typed resource/execute callback.](#getjointtwobonenode) |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | [Inherited typed resource/execute callback.](#getpropertydescriptors) |
| `protected override System.Void OnExecute(System.Double delta)` | [Inherited typed resource/execute callback.](#onexecute) |
| `public System.Void SetJointOneBoneIndex(System.Int32 boneIndex)` | [Selects joint one by nonnegative skeleton index, updating its path when attached.](#setjointoneboneindex) |
| `public System.Void SetJointOneBoneNode(System.String boneNode)` | [Selects joint one by a bone path relative to its skeleton.](#setjointonebonenode) |
| `public System.Void SetJointTwoBoneIndex(System.Int32 boneIndex)` | [Selects joint two by nonnegative skeleton index, updating its path when attached.](#setjointtwoboneindex) |
| `public System.Void SetJointTwoBoneNode(System.String boneNode)` | [Selects joint two by a bone path relative to its skeleton.](#setjointtwobonenode) |

## Member descriptions

### .ctor

`public SkeletonModificationTwoBoneIK()`

Inherited typed resource/execute callback; executes the contract described above.

### CopyCustomStateTo

`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)`

Inherited typed resource/execute callback; executes the contract described above.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### CreateDuplicateInstance

`protected override Electron2D.Resource CreateDuplicateInstance()`

Inherited typed resource/execute callback; executes the contract described above.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### GetJointOneBoneIndex

`public System.Int32 GetJointOneBoneIndex()`

Inherited typed resource/execute callback; executes the contract described above.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### GetJointOneBoneNode

`public System.String GetJointOneBoneNode()`

Inherited typed resource/execute callback; executes the contract described above.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### GetJointTwoBoneIndex

`public System.Int32 GetJointTwoBoneIndex()`

Inherited typed resource/execute callback; executes the contract described above.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### GetJointTwoBoneNode

`public System.String GetJointTwoBoneNode()`

Inherited typed resource/execute callback; executes the contract described above.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Inherited typed resource/execute callback; executes the contract described above.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### OnExecute

`protected override System.Void OnExecute(System.Double delta)`

Inherited typed resource/execute callback; executes the contract described above.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### SetJointOneBoneIndex

`public System.Void SetJointOneBoneIndex(System.Int32 boneIndex)`

Selects joint one by nonnegative skeleton index, updating its path when attached.

- `boneIndex`: Valid current index when bound; nonnegative deferred index otherwise.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### SetJointOneBoneNode

`public System.Void SetJointOneBoneNode(System.String boneNode)`

Selects joint one by a bone path relative to its skeleton.

- `boneNode`: Bounded scene path; empty uses its stored index.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### SetJointTwoBoneIndex

`public System.Void SetJointTwoBoneIndex(System.Int32 boneIndex)`

Selects joint two by nonnegative skeleton index, updating its path when attached.

- `boneIndex`: Valid current index when bound; nonnegative deferred index otherwise.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### SetJointTwoBoneNode

`public System.Void SetJointTwoBoneNode(System.String boneNode)`

Selects joint two by a bone path relative to its skeleton.

- `boneNode`: Bounded scene path; empty uses its stored index.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### FlipBendDirection

`public System.Boolean FlipBendDirection { get; set; }`

Gets or sets whether the limb bends toward the opposite side.

False initially.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### TargetMaximumDistance

`public System.Single TargetMaximumDistance { get; set; }`

Gets or sets maximum root-to-target solve distance in canvas-world pixels.

Zero disables the maximum.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### TargetMinimumDistance

`public System.Single TargetMinimumDistance { get; set; }`

Gets or sets minimum root-to-target solve distance in canvas-world pixels.

Zero disables the minimum.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### TargetNodePath

`public System.String TargetNodePath { get; set; }`

Gets or sets relative scene path of the Entity target.

Empty initially.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

## Lifecycle, errors, storage and verification

Stacks borrow the resource and scene selections. Setters follow the bound skeleton owner; copied configuration is guarded by the resource gate. Joint/target bindings are weak and refresh on scene path, hierarchy/rest, owner or selection changes. Missing/renamed/orphan selections do not fall back to a stale index. A bound numeric setter validates the current index and authors its relative path, retaining the bone through later reordering; a detached setter keeps its nonnegative deferred index. Initial unselected indices are -1. Paths are bounded to 65536 characters and contain no NUL; counts range from zero to 4096; numeric inputs must be finite. Invalid setters/archives preserve configuration. Attached disposal and stack ownership follow SkeletonModification/Stack.

Enabled and ExecutionMode retain base semantics. Solvers request transient local overrides at stack.Strength. Physics stages them and restores authored poses; Idle applies them. Disable returns to ordinary authored animation. If a modifier/notification fails, Skeleton restores the pre-pass override/strength/persistence arrays as well as authored poses, preventing delayed changes from a failed pass and preserving prior requests. Structural rest/hierarchy mutation still rejects mixed execution. Cold structural changes, copying and files may allocate; prepared solve loops reuse bindings/scratch.

Built-in exact factories and stored descriptors restore settings through .e2dscene/PackedScene. Chains use a versioned blob with joint selection and scalar/vector policies, bounded to 64 MiB and validated completely before replacement. Copying owns independent joint records and drops transient scratch/binding. PackedScene already force-copies complete stack graphs per rig, preserving resource aliases and instance-owned lifetime. Custom resource consumers still use the existing typed registration contract.

SkeletonIKTests exercises actual target endpoints, bend/length/distance constraints, mirrors/endpoint angles, local/global/inverted CCD limits, magnets, final orientation, three-joint convergence, degenerate and missing selections, copied/fresh-process scenes, failed-pass rollback and 128 prepared iterations per solver with zero managed bytes. Engine.Run hosts verify all three solvers' deformed Polygon pixels on current Linux GPU/compatibility and 64 prepared render/solve intervals with zero managed bytes. Native allocator activity, large-rig performance, foreign platforms and owner acceptance remain unverified. Editor gizmos, server palettes and general Mesh skin channels retain exact triggers.

See [skeletal animation](../components/skeletal-animation.md), [Skeleton](Skeleton.md), [SkeletonModificationStack](SkeletonModificationStack.md) and [ADR 0092](../decisions/mesh.md#adr-0092).
