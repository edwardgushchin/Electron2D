# SkeletonModificationStack

Last updated: 2026-10-07

- Declaration: `public sealed class SkeletonModificationStack : Resource`
- Source: [SkeletonModificationStack.cs](../../src/Scene/Resources/SkeletonModificationStack.cs)
- Inherits: [Resource](Resource.md)
- Component: [Skeletal animation](../components/skeletal-animation.md)

## Description

Three concrete [IK resources](../components/skeletal-animation.md#executable-ik-solvers) run as ordinary ordered borrowed modifications. They request transient poses at Strength; failed passes restore pre-existing requests as well as authored poses. Exact resource registration and the existing forced scene-copy policy give each instantiated rig independent joint configuration/caches.

Ordered borrowed modification list, initially empty and disabled, with strength one. ModificationCount grows with null slots or shrinks/detaches absent resources; the capacity limit is 4096. Null slots skip execution. Repeated references remain aliases and removing one slot does not detach a still-present modification. Enabled gates execution, while EnableAllModifications changes each present modifier. Strength is a requested blend value consumed by concrete modifiers.

Setup requires a rig binding and attempts every modifier, retaining committed binding and false setup on callback failures. Direct Execute routes through Skeleton.ExecuteModifications for authored-pose restoration and phase/override semantics. Structural edits are blocked during execution or setup, including setup of an appended/replaced modifier. Failed individual setup keeps the committed slot but permits explicit Setup retry. Duplicate always force-copies the modification graph, preserves aliases and drops scene binding regardless of shallow resource mode. Disposal detaches live borrowed modifications without disposing them.

## Example

This excerpt uses the existing scene context indicated in comments. SkeletonTests exercises the complete producer/consumer path.

```csharp
using var aim = new SkeletonModificationLookAt
    { BoneNode = "Arm", TargetNodePath = "../Target" };
using var stack = new SkeletonModificationStack { Enabled = true };
stack.AddModification(aim);
rig.SetModificationStack(stack); // Existing live Skeleton context.
// Caller owns both resources and detaches them before their lifetime ends.
```

## Constructors

| Signature | Contract |
| --- | --- |
| `public SkeletonModificationStack()` | [Inherited lifecycle/schema override.](#ctor) |

## Properties

| Signature | Contract |
| --- | --- |
| `public System.Boolean Enabled { get; set; }` | [Gets or sets whether the ordered stack executes.](#enabled) |
| `public System.Int32 ModificationCount { get; set; }` | [Gets or resizes the ordered list, growing with null slots.](#modificationcount) |
| `public System.Single Strength { get; set; }` | [Gets or sets modification interpolation strength.](#strength) |

## Methods and protected extension points

| Signature | Contract |
| --- | --- |
| `public System.Void AddModification(Electron2D.SkeletonModification modification)` | [Appends a borrowed nonnull modification, preparing it when already bound.](#addmodification) |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)` | [Inherited lifecycle/schema override.](#copycustomstateto) |
| `protected override Electron2D.Resource CreateDuplicateInstance()` | [Inherited lifecycle/schema override.](#createduplicateinstance) |
| `public System.Void DeleteModification(System.Int32 index)` | [Removes an indexed slot and detaches a resource no longer present in the list.](#deletemodification) |
| `protected override System.Void Dispose(System.Boolean disposing)` | [Inherited lifecycle/schema override.](#dispose) |
| `public System.Void EnableAllModifications(System.Boolean enabled)` | [Enables or disables all present modifications.](#enableallmodifications) |
| `public System.Void Execute(System.Double delta, Electron2D.ProcessPhase executionMode)` | [Executes matching enabled modifications in index order.](#execute) |
| `public System.Boolean GetIsSetup()` | [Inherited lifecycle/schema override.](#getissetup) |
| `public Electron2D.SkeletonModification GetModification(System.Int32 index)` | [Returns a borrowed modification from a slot.](#getmodification) |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | [Inherited lifecycle/schema override.](#getpropertydescriptors) |
| `public Electron2D.Skeleton GetSkeleton()` | [Inherited lifecycle/schema override.](#getskeleton) |
| `public System.Void SetModification(System.Int32 index, Electron2D.SkeletonModification modification)` | [Replaces a borrowed slot, allowing null.](#setmodification) |
| `public System.Void Setup()` | [Inherited lifecycle/schema override.](#setup) |

## Member descriptions

### .ctor

`public SkeletonModificationStack()`

Inherited lifecycle/schema override; see the linked base type.

### AddModification

`public System.Void AddModification(Electron2D.SkeletonModification modification)`

Appends a borrowed nonnull modification, preparing it when already bound.

- `modification`: Live resource, caller-owned.

### CopyCustomStateTo

`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)`

Inherited lifecycle/schema override; see the linked base type.

### CreateDuplicateInstance

`protected override Electron2D.Resource CreateDuplicateInstance()`

Inherited lifecycle/schema override; see the linked base type.

### DeleteModification

`public System.Void DeleteModification(System.Int32 index)`

Removes an indexed slot and detaches a resource no longer present in the list.

- `index`: Valid slot.

### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`



### EnableAllModifications

`public System.Void EnableAllModifications(System.Boolean enabled)`

Enables or disables all present modifications.

- `enabled`: Requested enable state.

### Execute

`public System.Void Execute(System.Double delta, Electron2D.ProcessPhase executionMode)`

Executes matching enabled modifications in index order.

Requires an attached prepared skeleton. Null slots are skipped. Guard cleanup permits retry after user failure.

- `delta`: Nonnegative finite seconds.
- `executionMode`: Idle or physics phase.

### GetIsSetup

`public System.Boolean GetIsSetup()`

Inherited lifecycle/schema override; see the linked base type.

### GetModification

`public Electron2D.SkeletonModification GetModification(System.Int32 index)`

Returns a borrowed modification from a slot.

The modification or null.

- `index`: Valid slot.

### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Inherited lifecycle/schema override; see the linked base type.

### GetSkeleton

`public Electron2D.Skeleton GetSkeleton()`

Inherited lifecycle/schema override; see the linked base type.

### SetModification

`public System.Void SetModification(System.Int32 index, Electron2D.SkeletonModification modification)`

Replaces a borrowed slot, allowing null.

- `index`: Valid slot.
- `modification`: Live resource or null.

### Setup

`public System.Void Setup()`

Inherited lifecycle/schema override; see the linked base type.

### Enabled

`public System.Boolean Enabled { get; set; }`

Gets or sets whether the ordered stack executes.

False initially.

### ModificationCount

`public System.Int32 ModificationCount { get; set; }`

Gets or resizes the ordered list, growing with null slots.

Zero initially; bounded to 4096 slots.

### Strength

`public System.Single Strength { get; set; }`

Gets or sets modification interpolation strength.

One initially; valid range zero through one.

## Lifecycle, errors and verification

Scene-bound calls follow the skeleton's owner thread. Cold structure/rest/path edits prepare new arrays and weak bindings; ordinary pose updates and retained replay reuse capacity. All numeric configuration requires finite values; strengths range from zero to one and phase selections use the existing ProcessPhase semantics. Invalid indices, disposed values, competing live bindings and structural reentry reject. User callbacks run after binding or setup commitment. Execution failures attempt every authored-pose restoration and release guards; setup failures keep GetIsSetup false for explicit retry. Resource graph persistence excludes transient setup, target references, palette RIDs and simulation history.

SkeletonTests exercises DFS membership/rest/indices, auto endpoints, persistent/transient/physics overrides, AnimationPlayer tracks, presentation interpolation, noncommuting and TopLevel coordinate transforms, strongest-four skin weights, path rename/reconnect, constraints, callback failures/reentry, owner guards, unique scene copies, fresh-process files and actual native pixels. 128 warmed managed pose/redraw/skin/replay iterations and 64 prepared native GPU/compatibility intervals allocate zero managed bytes. Native/backend allocations, large-rig performance, other platforms and owner acceptance are unverified. Editor gizmos, jiggle/stack-holder resources, physics synchronization and general Mesh skin channels retain exact coverage triggers.

See [skeletal animation](../components/skeletal-animation.md), [the mesh decision](../decisions/mesh.md#adr-0092) and the linked base-class lifecycle.
