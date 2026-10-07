# SkeletonModificationPhysicalBones

Last updated: 2026-10-07

- Declaration: `public sealed class SkeletonModificationPhysicalBones : SkeletonModification`
- Source: [SkeletonModificationPhysicalBones.cs](../../src/Scene/Resources/SkeletonModificationPhysicalBones.cs)
- Inherits: [SkeletonModification](SkeletonModification.md)
- Component: [Skeletal animation](../components/skeletal-animation.md)

## Description

A concrete SkeletonModification that borrows authored PhysicalBone nodes and transfers their completed rigid-body world poses into selected scene Bones. Paths resolve relative to the bound Skeleton. Default ExecutionMode=Idle observes completed physics poses, writes local transforms through the existing scene hierarchy and requests transient overrides at stack.Strength. Disable releases the last request on the next idle pass. An explicitly selected Physics phase stages the previous solved body pose before the next solver interval.

The copied consumer path list contains zero through 4096 slots; its index is distinct from the Skeleton bone index. Missing bodies, invalid/foreign bone ownership and FollowBoneWhenSimulating bodies are skipped. FetchPhysicalBones requires an attached skeleton, walks descendants breadth-first, omits nested foreign rigs and replaces the path list after traversal. It constructs no nodes, shapes or joints.

StartSimulation/StopSimulation select every listed body when names are null/empty, otherwise exact ordinal physical-node names, not bound bone names. Names are copied. Commands apply immediately when attached or once during actual setup if requested detached. Changes follow the physical participation guard; an earlier successful body transition remains committed if a later transition fails. Pending requests are transient and do not copy or serialize. Pose pass failure uses the existing Skeleton authored-pose and previous-request rollback.

Exact resource/node factories and copied string[] storage persist physical configuration and independently duplicated modification graphs through .e2dscene files. Scene nodes/shapes/joints remain scene-owned, resources borrow body paths, and weak bindings/history do not extend node lifetime. Configuration transitions, fetch, archives and duplication are cold work. Prepared body/pose/weighted Polygon replay reuses retained buffers.

## Example

```csharp
using var physical = new SkeletonModificationPhysicalBones();
using var stack = new SkeletonModificationStack { Enabled = true };
stack.AddModification(physical);
rig.SetModificationStack(stack); // Existing attached Skeleton with authored physical nodes.
stack.Setup();
physical.FetchPhysicalBones();
physical.StartSimulation(["ArmBody"]);
physical.StopSimulation();
```

## Constructors

| Signature | Contract |
| --- | --- |
| `public SkeletonModificationPhysicalBones()` | [Creates the default detached node/resource described above.](#ctor) |

## Properties

| Signature | Contract |
| --- | --- |
| `public System.Int32 PhysicalBoneChainLength { get; set; }` | [Retains the prefix when resizing between zero and 4096; new slots are empty.](#physicalbonechainlength) |

## Methods and protected extension points

| Signature | Contract |
| --- | --- |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)` | [Copies configuration and base modification settings without pending commands, live scene bindings or simulation history.](#copycustomstateto) |
| `protected override Electron2D.Resource CreateDuplicateInstance()` | [Constructs the exact concrete resource factory.](#createduplicateinstance) |
| `public System.Void FetchPhysicalBones()` | [Replaces the bound skeleton consumer list in breadth-first order, omitting nested foreign rigs.](#fetchphysicalbones) |
| `public System.String GetPhysicalBoneNode(System.Int32 jointIndex)` | [Returns a selected consumer path; out-of-range slot rejects.](#getphysicalbonenode) |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | [Extends inherited typed schema with the described configuration; live bindings are not stored.](#getpropertydescriptors) |
| `protected override System.Void OnExecute(System.Double delta)` | [Transfers active nonfollowing body poses and publishes transient local overrides through its stack strength.](#onexecute) |
| `protected override System.Void OnSetupModification(Electron2D.SkeletonModificationStack modificationStack)` | [Applies a retained detached command once the resource has an actual skeleton binding.](#onsetupmodification) |
| `public System.Void SetPhysicalBoneNode(System.Int32 jointIndex, System.String physicalBoneNode)` | [Copies the immutable bounded path into a valid consumer slot, invalidating resolution on its next use.](#setphysicalbonenode) |
| `public System.Void StartSimulation(System.String[] bones = null)` | [Copies names and requests active simulation for matching physical node names; null/empty selects all.](#startsimulation) |
| `public System.Void StopSimulation(System.String[] bones = null)` | [Copies names and requests static noncolliding follower mode for matching physical node names.](#stopsimulation) |

## Member descriptions

### .ctor

`public SkeletonModificationPhysicalBones()`

Creates the default detached node/resource described above.

### CopyCustomStateTo

`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)`

Copies configuration and base modification settings without pending commands, live scene bindings or simulation history.

### CreateDuplicateInstance

`protected override Electron2D.Resource CreateDuplicateInstance()`

Constructs the exact concrete resource factory.

### FetchPhysicalBones

`public System.Void FetchPhysicalBones()`

Replaces the bound skeleton consumer list in breadth-first order, omitting nested foreign rigs.

### GetPhysicalBoneNode

`public System.String GetPhysicalBoneNode(System.Int32 jointIndex)`

Returns a selected consumer path; out-of-range slot rejects.

### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Extends inherited typed schema with the described configuration; live bindings are not stored.

### OnExecute

`protected override System.Void OnExecute(System.Double delta)`

Transfers active nonfollowing body poses and publishes transient local overrides through its stack strength.

### OnSetupModification

`protected override System.Void OnSetupModification(Electron2D.SkeletonModificationStack modificationStack)`

Applies a retained detached command once the resource has an actual skeleton binding.

### SetPhysicalBoneNode

`public System.Void SetPhysicalBoneNode(System.Int32 jointIndex, System.String physicalBoneNode)`

Copies the immutable bounded path into a valid consumer slot, invalidating resolution on its next use.

### StartSimulation

`public System.Void StartSimulation(System.String[] bones = null)`

Copies names and requests active simulation for matching physical node names; null/empty selects all.

### StopSimulation

`public System.Void StopSimulation(System.String[] bones = null)`

Copies names and requests static noncolliding follower mode for matching physical node names.

### PhysicalBoneChainLength

`public System.Int32 PhysicalBoneChainLength { get; set; }`

Retains the prefix when resizing between zero and 4096; new slots are empty.

## Verification and limits

PhysicalBoneTests exercises actual velocity/Freeze, inherited body geometry recovery, follower collision filters, pose strength/disable and physics-phase staging, failed-pass rollback, post-solver transitions, Sleeping/disable, ownership/path repair, parent bootstrap, native pin constraints, manual joint ownership, copied/deferred named commands, BFS foreign-rig omission, resource copies and fresh-process .e2dscene execution. 128 prepared physics/pose/weighted Polygon steps measure zero managed bytes. Native Engine.Run hosts (ELECTRON2D_TEST_PHYSICAL_BONE_HOST=1, ELECTRON2D_PHYSICAL_BONE_RENDERER=gpu or compatibility) on current Linux x64 verify scheduled solver motion, matching scene Bone pose and actual deformed weighted Polygon pixels. Each measures 64 prepared draw/velocity-update intervals at zero managed allocation; headless checks separately cover full physics/pose/replay cycles. The host uses ordinary Engine scheduling rather than reentering SceneTree frames from lifecycle/render callbacks. Native allocator activity, large-rig throughput, foreign platforms and owner acceptance remain unverified. Editor generation/gizmos, server-owned palettes and general Mesh skin channels retain their separate coverage dependencies.
