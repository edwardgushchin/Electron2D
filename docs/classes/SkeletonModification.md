# SkeletonModification

Last updated: 2026-10-07

- Declaration: `public abstract class SkeletonModification : Resource`
- Source: [SkeletonModification.cs](../../src/Scene/Resources/SkeletonModification.cs)
- Inherits: [Resource](Resource.md)
- Component: [Skeletal animation](../components/skeletal-animation.md)

- Inherited by: [SkeletonModificationLookAt](SkeletonModificationLookAt.md), [SkeletonModificationTwoBoneIK](SkeletonModificationTwoBoneIK.md), [SkeletonModificationCCDIK](SkeletonModificationCCDIK.md), [SkeletonModificationFABRIK](SkeletonModificationFABRIK.md), [SkeletonModificationJiggle](SkeletonModificationJiggle.md), [SkeletonModificationStackHolder](SkeletonModificationStackHolder.md)

## Physical skeletal integration

SkeletonModificationPhysicalBones is the concrete real-body pose consumer, reusing this setup/phase/strength and owner transaction without adding a backend interface.

## Description

[SkeletonModificationJiggle](SkeletonModificationJiggle.md) and [SkeletonModificationStackHolder](SkeletonModificationStackHolder.md) add actual dynamic joint and nested graph behavior. Internal OnUnbindStack cleanup runs after raw binding drops and releases weak child leases or transient history. Bound reset and disposal validate owner/running state before changing lifetime, so execution cannot silently lose a borrowed graph.

Concrete [TwoBoneIK](SkeletonModificationTwoBoneIK.md), [CCDIK](SkeletonModificationCCDIK.md) and [FABRIK](SkeletonModificationFABRIK.md) resources now execute through the existing phase/enable/setup callbacks. Their relative path/index caches use an internal direct weak BoundSkeleton owner accessor for configuration checks, avoiding reverse stack-gate lookup. The shared base public API is unchanged.

Typed resource extension point for actual pose modifications. Enabled defaults true and ExecutionMode defaults ProcessPhase.Idle. A stack borrows this resource, binding weak transient stack/skeleton references before calling OnSetupModification. OnExecute receives finite nonnegative seconds on the scene owner thread. Override it to request a local pose through Skeleton.SetBoneLocalPoseOverride. Implement the existing Resource factory/copy and registered typed schema contract to persist a custom derived resource; arbitrary types are never constructed from archive text.

ClampAngle normalizes finite multi-turn radians, sorts normalized limits and uses nearest circular chord distance when clamping (min wins a tie). A positive exact full turn remains the upper bound. Inversion selects the interval complement. EnsureModificationMutable and CopyModificationState are real protected extension helpers for lifetime/thread guards and copied base settings. Setup state is transient; setters support typed initialization/retry. Runtime editor-gizmo callbacks/state are absent pending the actual editor drawing owner.

## Example

This excerpt uses the existing scene context indicated in comments. SkeletonTests exercises the complete producer/consumer path.

```csharp
sealed class OffsetBone : SkeletonModification
{
    protected override void OnExecute(double delta)
    {
        var stack = GetModificationStack()!;
        stack.GetSkeleton()!.SetBoneLocalPoseOverride(0,
            new Transform(.2f, Vector2.Zero), stack.Strength, false);
    }
} // Runtime-only extension; storage requires its registered factory/copy schema.
```

## Constructors

| Signature | Contract |
| --- | --- |
| `protected SkeletonModification()` | [Inherited lifecycle/schema override.](#ctor) |

## Properties

| Signature | Contract |
| --- | --- |
| `public System.Boolean Enabled { get; set; }` | [Gets or sets whether this modification executes.](#enabled) |
| `public Electron2D.ProcessPhase ExecutionMode { get; set; }` | [Gets or sets the matching scene phase.](#executionmode) |

## Methods and protected extension points

| Signature | Contract |
| --- | --- |
| `public System.Single ClampAngle(System.Single angle, System.Single min, System.Single max, System.Boolean invert)` | [Constrains an angle to the given circular interval or its complement.](#clampangle) |
| `protected System.Void CopyModificationState(Electron2D.SkeletonModification target)` | [Copies stored base settings without transient bindings.](#copymodificationstate) |
| `protected override System.Void Dispose(System.Boolean disposing)` | [Inherited lifecycle/schema override.](#dispose) |
| `protected System.Void EnsureModificationMutable()` | [Inherited lifecycle/schema override.](#ensuremodificationmutable) |
| `public System.Boolean GetIsSetup()` | [Inherited lifecycle/schema override.](#getissetup) |
| `public Electron2D.SkeletonModificationStack GetModificationStack()` | [Inherited lifecycle/schema override.](#getmodificationstack) |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | [Inherited lifecycle/schema override.](#getpropertydescriptors) |
| `protected abstract System.Void OnExecute(System.Double delta)` | [Runs this modification's concrete pose behavior on the skeleton owner thread.](#onexecute) |
| `protected override System.Void OnResetState()` | [Inherited lifecycle/schema override.](#onresetstate) |
| `protected virtual System.Void OnSetupModification(Electron2D.SkeletonModificationStack modificationStack)` | [Receives a committed binding for initialization.](#onsetupmodification) |
| `public System.Void SetIsSetup(System.Boolean isSetup)` | [Authors setup state for derived initialization and retry.](#setissetup) |

## Member descriptions

### .ctor

`protected SkeletonModification()`

Inherited lifecycle/schema override; see the linked base type.

### ClampAngle

`public System.Single ClampAngle(System.Single angle, System.Single min, System.Single max, System.Boolean invert)`

Constrains an angle to the given circular interval or its complement.

Normalized radians, or the nearest allowed endpoint, preferring min at a tie.

- `angle`: Finite radians, including multiple turns.
- `min`: Finite lower boundary.
- `max`: Finite upper boundary.
- `invert`: Select the outside of the sorted interval.

### CopyModificationState

`protected System.Void CopyModificationState(Electron2D.SkeletonModification target)`

Copies stored base settings without transient bindings.

- `target`: Fresh exact derived target.

### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`



### EnsureModificationMutable

`protected System.Void EnsureModificationMutable()`

Inherited lifecycle/schema override; see the linked base type.

### GetIsSetup

`public System.Boolean GetIsSetup()`

Inherited lifecycle/schema override; see the linked base type.

### GetModificationStack

`public Electron2D.SkeletonModificationStack GetModificationStack()`

Inherited lifecycle/schema override; see the linked base type.

### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Inherited lifecycle/schema override; see the linked base type.

### OnExecute

`protected abstract System.Void OnExecute(System.Double delta)`

Runs this modification's concrete pose behavior on the skeleton owner thread.

- `delta`: Finite nonnegative seconds.

### OnResetState

`protected override System.Void OnResetState()`

Inherited lifecycle/schema override; see the linked base type.

### OnSetupModification

`protected virtual System.Void OnSetupModification(Electron2D.SkeletonModificationStack modificationStack)`

Receives a committed binding for initialization.

- `modificationStack`: Borrowed live stack.

### SetIsSetup

`public System.Void SetIsSetup(System.Boolean isSetup)`

Authors setup state for derived initialization and retry.

- `isSetup`: Whether the bound modification can execute.

### Enabled

`public System.Boolean Enabled { get; set; }`

Gets or sets whether this modification executes.

True initially.

### ExecutionMode

`public Electron2D.ProcessPhase ExecutionMode { get; set; }`

Gets or sets the matching scene phase.

Idle initially; raw source integers project to the shared semantic ProcessPhase domain.

## Lifecycle, errors and verification

Scene-bound calls follow the skeleton's owner thread. Cold structure/rest/path edits prepare new arrays and weak bindings; ordinary pose updates and retained replay reuse capacity. All numeric configuration requires finite values; strengths range from zero to one and phase selections use the existing ProcessPhase semantics. Invalid indices, disposed values, competing live bindings and structural reentry reject. User callbacks run after binding or setup commitment. Execution failures attempt every authored-pose restoration and release guards; setup failures keep GetIsSetup false for explicit retry. Resource graph persistence excludes transient setup, target references, palette RIDs and simulation history.

SkeletonTests exercises DFS membership/rest/indices, auto endpoints, persistent/transient/physics overrides, AnimationPlayer tracks, presentation interpolation, noncommuting and TopLevel coordinate transforms, strongest-four skin weights, path rename/reconnect, constraints, callback failures/reentry, owner guards, unique scene copies, fresh-process files and actual native pixels. 128 warmed managed pose/redraw/skin/replay iterations and 64 prepared native GPU/compatibility intervals allocate zero managed bytes. Native/backend allocations, large-rig performance, other platforms and owner acceptance are unverified. Editor gizmos, physics synchronization and general Mesh skin channels retain exact coverage triggers.

See [skeletal animation](../components/skeletal-animation.md), [the mesh decision](../decisions/mesh.md#adr-0092) and the linked base-class lifecycle.

## Disposal validation extension

| Signature | Contract |
| --- | --- |
| `protected override System.Void ValidateDisposal()` | [Validates owner and active graph before disposal.](#validatedisposal) |

### ValidateDisposal

`protected override System.Void ValidateDisposal()`

Rejects wrong-owner or active setup/execution removal with InvalidOperationException before the terminal transition. This inherited validation callback is side-effect-free and tolerates repeated calls. Once the graph is idle, ordinary disposal clears transient bindings and owned storage, releases weak held-child leases and preserves caller-owned resources. Explicit disposal from a running child/modification callback is verified to leave IsDisposed=false and permit retry.
