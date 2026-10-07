# SkeletonModificationFABRIK

Last updated: 2026-10-07

- Declaration: `public sealed class SkeletonModificationFABRIK : SkeletonModification`
- Source: [SkeletonModificationFABRIK.cs](../../src/Scene/Resources/SkeletonModificationFABRIK.cs)
- Inherits: [SkeletonModification](SkeletonModification.md)
- Component: [Skeletal animation](../components/skeletal-animation.md)

## Description

Forward and backward reaching inverse kinematics over independent prepared world positions. At least two unique live bones with positive conservative world lengths are required. Bone paths are in chain order; the root position remains anchored. Each backward pass biases non-root joint positions by the authored world-space MagnetPosition and restores segment lengths; each forward pass restores the root and segment lengths. Ten iterations and a .01 canvas-world pixel endpoint tolerance bound the solve. The stopping distance uses updated scratch positions, correcting stale live-bone convergence sampling.

UseTargetRotation controls the final joint only, as its tip-orientation contract specifies. Other slots retain that setting for later chain edits. The oriented final segment participates in both passes; the resulting endpoint may remain unreachable under that extra orientation constraint. Root magnets do not move the anchored root. Missing/duplicate/zero-length/singular selections skip the entire solve. Coincident positions use the existing bone endpoint axis as a finite fallback. Length is GetLength times the minimum absolute global scale; reflected scales preserve positive distance. Nonuniform/sheared bases retain the conservative length policy rather than a full anisotropic constraint solver.

Application writes solved global joint positions and endpoint directions through ordinary Entity setters, then submits transient local overrides. Independent position/axis/length/bone/membership arrays reuse capacity. Strong temporary bone references are cleared in finally on every path; cached scene ownership remains weak. Scratch is never copied or archived.

## Example

This excerpt requires the indicated existing scene; SkeletonIKTests supplies complete executable producers/consumers.

```csharp
using var ik = new SkeletonModificationFABRIK
    { FABRIKDataChainLength = 3, TargetNodePath = "../Target" };
ik.SetFABRIKJointBoneNode(0, "Upper");
ik.SetFABRIKJointBoneNode(1, "Upper/Middle");
ik.SetFABRIKJointBoneNode(2, "Upper/Middle/Lower");
ik.SetFABRIKJointMagnetPosition(1, new Vector2(-10, 20));
ik.SetFABRIKJointUseTargetRotation(2, true);
using var stack = new SkeletonModificationStack { Enabled = true };
stack.AddModification(ik);
rig.SetModificationStack(stack); // Existing scene context.
```

## Constructors

| Signature | Contract |
| --- | --- |
| `public SkeletonModificationFABRIK()` | [Inherited typed resource/execute callback.](#ctor) |

## Properties

| Signature | Contract |
| --- | --- |
| `public System.Int32 FABRIKDataChainLength { get; set; }` | [Gets or resizes the number of authored joints, preserving the retained prefix.](#fabrikdatachainlength) |
| `public System.String TargetNodePath { get; set; }` | [Gets or sets the Entity target path relative to its skeleton.](#targetnodepath) |

## Methods and protected extension points

| Signature | Contract |
| --- | --- |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)` | [Inherited typed resource/execute callback.](#copycustomstateto) |
| `protected override Electron2D.Resource CreateDuplicateInstance()` | [Inherited typed resource/execute callback.](#createduplicateinstance) |
| `public System.Int32 GetFABRIKJointBoneIndex(System.Int32 jointIndex)` | [Returns a joint's resolved or authored bone index.](#getfabrikjointboneindex) |
| `public System.String GetFABRIKJointBoneNode(System.Int32 jointIndex)` | [Returns a joint's authored relative bone path.](#getfabrikjointbonenode) |
| `public Electron2D.Vector2 GetFABRIKJointMagnetPosition(System.Int32 jointIndex)` | [Returns world-space positional bias applied on each backward pass; root remains anchored.](#getfabrikjointmagnetposition) |
| `public System.Boolean GetFABRIKJointUseTargetRotation(System.Int32 jointIndex)` | [Returns whether the final joint uses target orientation; other slots retain the value for later chain edits.](#getfabrikjointusetargetrotation) |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | [Inherited typed resource/execute callback.](#getpropertydescriptors) |
| `protected override System.Void OnExecute(System.Double delta)` | [Inherited typed resource/execute callback.](#onexecute) |
| `public System.Void SetFABRIKJointBoneIndex(System.Int32 jointIndex, System.Int32 boneIndex)` | [Selects by nonnegative index and authors its path when attached.](#setfabrikjointboneindex) |
| `public System.Void SetFABRIKJointBoneNode(System.Int32 jointIndex, System.String boneNode)` | [Selects a joint by relative bone path.](#setfabrikjointbonenode) |
| `public System.Void SetFABRIKJointMagnetPosition(System.Int32 jointIndex, Electron2D.Vector2 value)` | [Sets world-space positional bias applied on each backward pass; root remains anchored.](#setfabrikjointmagnetposition) |
| `public System.Void SetFABRIKJointUseTargetRotation(System.Int32 jointIndex, System.Boolean value)` | [Sets whether the final joint uses target orientation; other slots retain the value for later chain edits.](#setfabrikjointusetargetrotation) |

## Member descriptions

### .ctor

`public SkeletonModificationFABRIK()`

Inherited typed resource/execute callback; executes the contract described above.

### CopyCustomStateTo

`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)`

Inherited typed resource/execute callback; executes the contract described above.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### CreateDuplicateInstance

`protected override Electron2D.Resource CreateDuplicateInstance()`

Inherited typed resource/execute callback; executes the contract described above.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### GetFABRIKJointBoneIndex

`public System.Int32 GetFABRIKJointBoneIndex(System.Int32 jointIndex)`

Returns a joint's resolved or authored bone index.

Minus one for unselected/missing paths.

- `jointIndex`: Valid joint slot.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### GetFABRIKJointBoneNode

`public System.String GetFABRIKJointBoneNode(System.Int32 jointIndex)`

Returns a joint's authored relative bone path.

Empty for deferred numeric selection; attached numeric setters author a path.

- `jointIndex`: Valid joint slot.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### GetFABRIKJointMagnetPosition

`public Electron2D.Vector2 GetFABRIKJointMagnetPosition(System.Int32 jointIndex)`

Returns world-space positional bias applied on each backward pass; root remains anchored.

Authored setting; default Vector2.Zero.

- `jointIndex`: Valid joint slot.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### GetFABRIKJointUseTargetRotation

`public System.Boolean GetFABRIKJointUseTargetRotation(System.Int32 jointIndex)`

Returns whether the final joint uses target orientation; other slots retain the value for later chain edits.

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

### SetFABRIKJointBoneIndex

`public System.Void SetFABRIKJointBoneIndex(System.Int32 jointIndex, System.Int32 boneIndex)`

Selects by nonnegative index and authors its path when attached.

- `jointIndex`: Valid joint slot.
- `boneIndex`: Current valid skeleton index, or nonnegative deferred selection.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### SetFABRIKJointBoneNode

`public System.Void SetFABRIKJointBoneNode(System.Int32 jointIndex, System.String boneNode)`

Selects a joint by relative bone path.

- `jointIndex`: Valid joint slot.
- `boneNode`: Path relative to the skeleton; missing paths do not fall back to an old index.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### SetFABRIKJointMagnetPosition

`public System.Void SetFABRIKJointMagnetPosition(System.Int32 jointIndex, Electron2D.Vector2 value)`

Sets world-space positional bias applied on each backward pass; root remains anchored.

- `jointIndex`: Valid joint slot.
- `value`: Finite canvas-world pixel vector.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### SetFABRIKJointUseTargetRotation

`public System.Void SetFABRIKJointUseTargetRotation(System.Int32 jointIndex, System.Boolean value)`

Sets whether the final joint uses target orientation; other slots retain the value for later chain edits.

- `jointIndex`: Valid joint slot.
- `value`: Requested policy.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### FABRIKDataChainLength

`public System.Int32 FABRIKDataChainLength { get; set; }`

Gets or resizes the number of authored joints, preserving the retained prefix.

Zero initially; zero through 4096. New slots have no bone selection.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

### TargetNodePath

`public System.String TargetNodePath { get; set; }`

Gets or sets the Entity target path relative to its skeleton.

Empty initially; missing targets skip execution.

Invalid indices/phase/nonfinite values throw ArgumentOutOfRangeException or ArgumentException before configuration changes. Disposed calls reject with ObjectDisposedException; attached off-owner mutation rejects with InvalidOperationException. Callback and arithmetic failures follow the restoration contract below. Scene references remain borrowed.

## Lifecycle, errors, storage and verification

Stacks borrow the resource and scene selections. Setters follow the bound skeleton owner; copied configuration is guarded by the resource gate. Joint/target bindings are weak and refresh on scene path, hierarchy/rest, owner or selection changes. Missing/renamed/orphan selections do not fall back to a stale index. A bound numeric setter validates the current index and authors its relative path, retaining the bone through later reordering; a detached setter keeps its nonnegative deferred index. Initial unselected indices are -1. Paths are bounded to 65536 characters and contain no NUL; counts range from zero to 4096; numeric inputs must be finite. Invalid setters/archives preserve configuration. Attached disposal and stack ownership follow SkeletonModification/Stack.

Enabled and ExecutionMode retain base semantics. Solvers request transient local overrides at stack.Strength. Physics stages them and restores authored poses; Idle applies them. Disable returns to ordinary authored animation. If a modifier/notification fails, Skeleton restores the pre-pass override/strength/persistence arrays as well as authored poses, preventing delayed changes from a failed pass and preserving prior requests. Structural rest/hierarchy mutation still rejects mixed execution. Cold structural changes, copying and files may allocate; prepared solve loops reuse bindings/scratch.

Built-in exact factories and stored descriptors restore settings through .e2dscene/PackedScene. Chains use a versioned blob with joint selection and scalar/vector policies, bounded to 64 MiB and validated completely before replacement. Copying owns independent joint records and drops transient scratch/binding. PackedScene already force-copies complete stack graphs per rig, preserving resource aliases and instance-owned lifetime. Custom resource consumers still use the existing typed registration contract.

SkeletonIKTests exercises actual target endpoints, bend/length/distance constraints, mirrors/endpoint angles, local/global/inverted CCD limits, magnets, final orientation, three-joint convergence, degenerate and missing selections, copied/fresh-process scenes, failed-pass rollback and 128 prepared iterations per solver with zero managed bytes. Engine.Run hosts verify all three solvers' deformed Polygon pixels on current Linux GPU/compatibility and 64 prepared render/solve intervals with zero managed bytes. Native allocator activity, large-rig performance, foreign platforms and owner acceptance remain unverified. Editor gizmos retain their exact authoring trigger; server palettes and general Mesh skin channels now execute through the mesh component.

See [skeletal animation](../components/skeletal-animation.md), [Skeleton](Skeleton.md), [SkeletonModificationStack](SkeletonModificationStack.md) and [ADR 0092](../decisions/mesh.md#adr-0092).
