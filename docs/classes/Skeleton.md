# Skeleton

Last updated: 2026-10-07

- Declaration: `public class Skeleton : Entity`
- Source: [Skeleton.cs](../../src/Scene/2D/Skeleton.cs)
- Inherits: [Entity](Entity.md)
- Component: [Skeletal animation](../components/skeletal-animation.md)

## Physical skeletal integration

SkeletonModificationPhysicalBones now transfers actual solved PhysicalBone world poses into selected bones using the existing transient local override/strength/rollback transaction. Physics nodes keep separate scene ownership and inherited rigid-body restrictions.

## Description

[Jiggle and held stacks](../components/skeletal-animation.md#jiggle-and-nested-stacks) now use this same pose transaction. Internal ExecuteStack selects the explicitly requested child for a direct Stack.Execute call, preventing unrelated root siblings from running; ordinary ExecuteModifications still selects the assigned root. Nested calls retain the outer authored-pose/request transaction.

A failed modification pass now restores pre-pass override/strength/persistence arrays using reusable setup-capacity snapshots in addition to restoring authored bone poses. Earlier external requests survive; a partially requested IK result cannot appear on a later disabled frame. SkeletonIKTests covers both cases.

A scene-owned rig retains a prepared inverse-rest/current-pose palette over uninterrupted direct Bone chains. DFS order follows child order; a neutral Node/Entity breaks the chain. Detached rigs report zero bones and bones report index -1. Singular/unset rest poses remain authorable and contribute identity deformation. BoneSetupChanged runs after a dirty setup commits, so readers see current indices and a throwing subscriber does not replay that setup. Attached structural changes reset overrides and advance the palette generation.

The rig borrows bones and its optional SkeletonModificationStack. GetSkeleton returns a stable weak logical palette RID independent of graphics startup; RenderingServer.FreeRID rejects that borrowed identity. Skin commands consume it through the internal registry. ExecuteModifications restores separately retained authored poses before running a phase. Physics stages override requests and returns authored poses; Idle blends and consumes transient requests. Persistent=true survives later applications, including without a stack. LookAt submits transient requests so disabling it restores animated state. Automatic scene internal processing executes these phases while respecting ordinary pause/process policy.

Stack replacement commits before setup callbacks. Only one attached rig can bind a stack, and each modification can belong to one attached stack. Reentrant stack changes or rest/hierarchy changes during execution reject; borrowed resources remain caller-owned. Scene copying forces an independent stack/modification graph for each rig and retains resource aliases within that graph.

## Example

This excerpt uses the existing scene context indicated in comments. SkeletonTests exercises the complete producer/consumer path.

```csharp
var rig = new Skeleton { Name = "Rig" };
var bone = new Bone { Name = "Arm", Rest = Transform.Identity };
bone.SetAutocalculateLengthAndAngle(false);
rig.AddChild(bone);
window.AddChild(rig); // Existing Window/SceneTree context.
rig.SetBoneLocalPoseOverride(bone.GetIndexInSkeleton(),
    new Transform(.4f, Vector2.Zero), 1, persistent: true);
```

## Constructors

| Signature | Contract |
| --- | --- |
| `public Skeleton()` | [Inherited lifecycle/schema override.](#ctor) |

## Methods and protected extension points

| Signature | Contract |
| --- | --- |
| `protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()` | [Inherited lifecycle/schema override.](#createsceneinstancefactory) |
| `protected override System.Void Dispose(System.Boolean disposing)` | [Inherited lifecycle/schema override.](#dispose) |
| `public System.Void ExecuteModifications(System.Double delta, Electron2D.ProcessPhase executionMode)` | [Executes the matching stack phase and applies idle local overrides.](#executemodifications) |
| `public Electron2D.Bone GetBone(System.Int32 index)` | [Returns a borrowed bone by current depth-first index.](#getbone) |
| `public System.Int32 GetBoneCount()` | [Inherited lifecycle/schema override.](#getbonecount) |
| `public Electron2D.Transform GetBoneLocalPoseOverride(System.Int32 boneIndex)` | [Returns the latest stored local override, initially the bone's local Rest.](#getbonelocalposeoverride) |
| `public Electron2D.SkeletonModificationStack GetModificationStack()` | [Inherited lifecycle/schema override.](#getmodificationstack) |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | [Inherited lifecycle/schema override.](#getpropertydescriptors) |
| `public Electron2D.RID GetSkeleton()` | [Inherited lifecycle/schema override.](#getskeleton) |
| `protected override System.Void OnNotification(System.Int32 what)` | [Inherited lifecycle/schema override.](#onnotification) |
| `public System.Void SetBoneLocalPoseOverride(System.Int32 boneIndex, Electron2D.Transform overridePose, System.Single strength, System.Boolean persistent)` | [Stores a finite local bone override with an interpolation strength.](#setbonelocalposeoverride) |
| `public System.Void SetModificationStack(Electron2D.SkeletonModificationStack modificationStack)` | [Assigns a borrowed modification stack and prepares it while attached.](#setmodificationstack) |

## Events

| Signature | Contract |
| --- | --- |
| `public event System.Action BoneSetupChanged` | [Occurs after committing a changed bone/rest layout and indices.](#bonesetupchanged) |

## Member descriptions

### .ctor

`public Skeleton()`

Inherited lifecycle/schema override; see the linked base type.

### BoneSetupChanged

`public event System.Action BoneSetupChanged`

Occurs after committing a changed bone/rest layout and indices.

Reads during a handler observe the committed setup. Handler failure does not replay the transition.

### CreateSceneInstanceFactory

`protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()`

Inherited lifecycle/schema override; see the linked base type.

### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`



### ExecuteModifications

`public System.Void ExecuteModifications(System.Double delta, Electron2D.ProcessPhase executionMode)`

Executes the matching stack phase and applies idle local overrides.

Physics prepares overrides; idle applies them. Authored bone poses are retained independently of modification writes. Reentry and structural mutation during execution reject; callback failure restores authored poses and execution guards.

- `delta`: Finite nonnegative seconds.
- `executionMode`: Existing idle or physics phase domain.

### GetBone

`public Electron2D.Bone GetBone(System.Int32 index)`

Returns a borrowed bone by current depth-first index.

The live scene-owned bone.

- `index`: Index in the attached hierarchy.

Throws `System.ArgumentOutOfRangeException`: The index is outside the current attached palette.

### GetBoneCount

`public System.Int32 GetBoneCount()`

Inherited lifecycle/schema override; see the linked base type.

### GetBoneLocalPoseOverride

`public Electron2D.Transform GetBoneLocalPoseOverride(System.Int32 boneIndex)`

Returns the latest stored local override, initially the bone's local Rest.

The stored local pose.

- `boneIndex`: Current bone index.

### GetModificationStack

`public Electron2D.SkeletonModificationStack GetModificationStack()`

Inherited lifecycle/schema override; see the linked base type.

### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Inherited lifecycle/schema override; see the linked base type.

### GetSkeleton

`public Electron2D.RID GetSkeleton()`

Inherited lifecycle/schema override; see the linked base type.

### OnNotification

`protected override System.Void OnNotification(System.Int32 what)`



### SetBoneLocalPoseOverride

`public System.Void SetBoneLocalPoseOverride(System.Int32 boneIndex, Electron2D.Transform overridePose, System.Single strength, System.Boolean persistent)`

Stores a finite local bone override with an interpolation strength.

Applied during idle execution, including without a modification stack. Transient overrides are consumed once.

- `boneIndex`: Current bone index.
- `overridePose`: Finite local transform.
- `strength`: Interpolation amount from zero through one.
- `persistent`: Retain the override across later process applications.

### SetModificationStack

`public System.Void SetModificationStack(Electron2D.SkeletonModificationStack modificationStack)`

Assigns a borrowed modification stack and prepares it while attached.

One attached skeleton can bind a stack at a time. Replacement commits before setup callbacks; resources stay caller-owned.

- `modificationStack`: Live stack or null.

## Lifecycle, errors and verification

Scene-bound calls follow the skeleton's owner thread. Cold structure/rest/path edits prepare new arrays and weak bindings; ordinary pose updates and retained replay reuse capacity. All numeric configuration requires finite values; strengths range from zero to one and phase selections use the existing ProcessPhase semantics. Invalid indices, disposed values, competing live bindings and structural reentry reject. User callbacks run after binding or setup commitment. Execution failures attempt every authored-pose restoration and release guards; setup failures keep GetIsSetup false for explicit retry. Resource graph persistence excludes transient setup, target references, palette RIDs and simulation history.

SkeletonTests exercises DFS membership/rest/indices, auto endpoints, persistent/transient/physics overrides, AnimationPlayer tracks, presentation interpolation, noncommuting and TopLevel coordinate transforms, strongest-four skin weights, path rename/reconnect, constraints, callback failures/reentry, owner guards, unique scene copies, fresh-process files and actual native pixels. 128 warmed managed pose/redraw/skin/replay iterations and 64 prepared native GPU/compatibility intervals allocate zero managed bytes. Native/backend allocations, large-rig performance, other platforms and owner acceptance are unverified. Editor gizmos, physics synchronization and general Mesh skin channels retain exact coverage triggers.

See [skeletal animation](../components/skeletal-animation.md), [the mesh decision](../decisions/mesh.md#adr-0092) and the linked base-class lifecycle.
