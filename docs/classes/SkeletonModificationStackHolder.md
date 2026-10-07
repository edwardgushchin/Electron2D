# SkeletonModificationStackHolder

Last updated: 2026-10-07

- Declaration: `public sealed class SkeletonModificationStackHolder : SkeletonModification`
- Source: [SkeletonModificationStackHolder.cs](../../src/Scene/Resources/SkeletonModificationStackHolder.cs)
- Inherits: [SkeletonModification](SkeletonModification.md)
- Component: [Skeletal animation](../components/skeletal-animation.md)

## Description

An ordinary SkeletonModification which borrows a child SkeletonModificationStack. Only the holder's matching phase enters the child, which in turn executes child modifications matching that same phase. Child Enabled gates its list; holder Enabled gates the entire subtree. Child Strength remains independent of parent Strength, matching the actual nested-stack contract. No additional blend multiplier is invented.

The child binds the same skeleton and tracks incoming holders with weak leases alongside an independent direct-root binding flag. Removing one alias/holder does not detach a child still referenced by another holder. Removing the final holder releases its child and descendants unless the child is also directly bound. Disposal/detachment drops scene leases without disposing caller-owned child resources. Setup/replacement commits before callbacks; failed child setup invalidates the parent for retry while keeping assigned graph and lease cleanup reviewable.

Setter traversal rejects graphs containing this holder before replacement. Runtime preparing/executing guards and depth bounds reject cycles completed later through list authoring or archive restoration. Replacement/reset/disposal during active execution rejects before lifetime state changes. Direct Execute on an attached child starts a pose transaction for that child instead of entering the skeleton's root siblings; nested calls reuse the already-running skeleton transaction. Null children skip execution. Graph preparation/copy/editing is cold and may allocate; prepared nested execution uses retained lists and fields.

## Example

This excerpt requires the indicated scene; SkeletonJiggleTests exercises complete producers/consumers.

```csharp
using var jiggle = new SkeletonModificationJiggle
    { JiggleDataChainLength = 1, TargetNodePath = "../Target" };
jiggle.SetJiggleJointBoneNode(0, "Hair");
using var child = new SkeletonModificationStack { Enabled = true, Strength = .5f };
child.AddModification(jiggle);
using var holder = new SkeletonModificationStackHolder();
holder.SetHeldModificationStack(child);
using var root = new SkeletonModificationStack { Enabled = true };
root.AddModification(holder);
rig.SetModificationStack(root); // Existing live Skeleton context.
// Resources stay caller-owned; detach graph before their lifetime ends.
```

## Constructors

| Signature | Contract |
| --- | --- |
| `public SkeletonModificationStackHolder()` | [Inherited typed lifecycle/schema callback.](#ctor) |

## Methods and protected extension points

| Signature | Contract |
| --- | --- |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)` | [Inherited typed lifecycle/schema callback.](#copycustomstateto) |
| `protected override Electron2D.Resource CreateDuplicateInstance()` | [Inherited typed lifecycle/schema callback.](#createduplicateinstance) |
| `public Electron2D.SkeletonModificationStack GetHeldModificationStack()` | [Inherited typed lifecycle/schema callback.](#getheldmodificationstack) |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | [Inherited typed lifecycle/schema callback.](#getpropertydescriptors) |
| `protected override System.Void OnExecute(System.Double delta)` | [Inherited typed lifecycle/schema callback.](#onexecute) |
| `protected override System.Void OnSetupModification(Electron2D.SkeletonModificationStack modificationStack)` | [Inherited typed lifecycle/schema callback.](#onsetupmodification) |
| `public System.Void SetHeldModificationStack(Electron2D.SkeletonModificationStack heldModificationStack)` | [Assigns a borrowed child stack, committing before child setup callbacks.](#setheldmodificationstack) |

## Member descriptions

### .ctor

`public SkeletonModificationStackHolder()`

Inherited typed lifecycle/schema callback; executes the described resource contract.

### CopyCustomStateTo

`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)`

Inherited typed lifecycle/schema callback; executes the described resource contract.

Invalid numeric/count/index inputs throw ArgumentOutOfRangeException; invalid paths/vectors throw ArgumentException. Disposed access rejects; scene-bound mutation requires its owner thread. Settings remain committed before Changed/PropertyListChanged callbacks; exception cleanup is described below. Referenced resources and scene nodes remain borrowed.

### CreateDuplicateInstance

`protected override Electron2D.Resource CreateDuplicateInstance()`

Inherited typed lifecycle/schema callback; executes the described resource contract.

Invalid numeric/count/index inputs throw ArgumentOutOfRangeException; invalid paths/vectors throw ArgumentException. Disposed access rejects; scene-bound mutation requires its owner thread. Settings remain committed before Changed/PropertyListChanged callbacks; exception cleanup is described below. Referenced resources and scene nodes remain borrowed.

### GetHeldModificationStack

`public Electron2D.SkeletonModificationStack GetHeldModificationStack()`

Inherited typed lifecycle/schema callback; executes the described resource contract.

Invalid numeric/count/index inputs throw ArgumentOutOfRangeException; invalid paths/vectors throw ArgumentException. Disposed access rejects; scene-bound mutation requires its owner thread. Settings remain committed before Changed/PropertyListChanged callbacks; exception cleanup is described below. Referenced resources and scene nodes remain borrowed.

### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

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

### SetHeldModificationStack

`public System.Void SetHeldModificationStack(Electron2D.SkeletonModificationStack heldModificationStack)`

Assigns a borrowed child stack, committing before child setup callbacks.

Replacement during setup/execution rejects. A graph containing this holder rejects before mutation. Child setup failure leaves the assigned graph reviewable and permits explicit parent Setup retry.

- `heldModificationStack`: Live resource or null. Caller retains ownership.

Invalid numeric/count/index inputs throw ArgumentOutOfRangeException; invalid paths/vectors throw ArgumentException. Disposed access rejects; scene-bound mutation requires its owner thread. Settings remain committed before Changed/PropertyListChanged callbacks; exception cleanup is described below. Referenced resources and scene nodes remain borrowed.

## Ownership, storage, failure and verification

Stacks/modifications remain caller-owned borrowed resources. Scene binding is weak, follows one attached skeleton and rejects competing live bindings. Setup callbacks run after binding commitment; failures retain assigned resources and keep the affected parent unprepared for explicit Setup retry. Structural graph replacement, disposal or binding reset during running setup/execution rejects before terminal changes. Nested setup/execution depth is bounded to 128; cold graph validation also caps 16384 visited stacks. Warm solve/replay uses existing scene ownership and reusable resource state, independent of graphics startup.

PackedScene already force-copies a rig's stack graph. Holder copying always force-copies the child graph through the same duplication session, preserving shared child/modification aliases with independent bindings per scene instance. Exact built-in schemas/factories persist configuration; no target caches, holder leases, force/velocity histories or native identities are saved. Joint blobs are versioned, limited to 4096 joints and 64 MiB and validated fully before replacement. Loading in another process executes the ordinary public controller path.

SkeletonJiggleTests checks exact force/velocity samples, reset/nonzero origins, gravity/default overrides, guarded numeric inputs, reflected aiming, path reconnection, actual collision/mask/phase behavior, direct child selection, independent child strength, shared-child removal, cycles, setup retry, running disposal/reentry, graph copies/aliases and fresh .e2dscene execution. 128 prepared nested simulation cycles report zero managed bytes. Native Engine.Run hosts on current Linux GPU/compatibility verify actual Jiggle, held-stack and ray-blocked Polygon pixels and 64 prepared render/query/solve intervals at zero managed allocation. Native allocator behavior, large graphs/rig throughput, foreign targets and owner acceptance remain unverified. Editor gizmos, PhysicalBone synchronization, server palettes and generic Mesh skin channels keep separate coverage dependencies.

See [skeletal animation](../components/skeletal-animation.md), [Skeleton](Skeleton.md), [SkeletonModificationStack](SkeletonModificationStack.md), [physics queries](PhysicsDirectSpaceState.md) and [ADR 0092](../decisions/mesh.md#adr-0092).
