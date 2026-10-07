# Bone

Last updated: 2026-10-07

- Declaration: `public class Bone : Entity`
- Source: [Bone.cs](../../src/Scene/2D/Bone.cs)
- Inherits: [Entity](Entity.md)
- Component: [Skeletal animation](../components/skeletal-animation.md)

## Description

A skeletal scene bone preserves the full applicable Entity/CanvasItem/Node behavior. Rest is a local bind transform, default zero (unset); GetSkeletonRest composes direct parent Bone rest poses even during detached authoring. Ordinary Transform/Position/Rotation/Scale hold the animated pose. ApplyRest changes that authored pose. Temporary modification writes do not replace it, and leaving the rig restores it.

Automatic endpoint calculation defaults true and runs when enabled and on Ready. The first direct child Bone supplies length and angle; a leaf retains length sixteen and uses its local Rotation as fallback. The endpoint values are metadata for modifications and do not resize or rotate the node. Signed finite manual length is retained. Tree membership supplies a borrowed Skeleton and lazy DFS index. A neutral parent or missing rig leaves the bone orphaned and produces a warning. TopLevel is respected by actual pose presentation while the authored rest chain remains skeleton-relative.

## Example

This excerpt uses the existing scene context indicated in comments. SkeletonTests exercises the complete producer/consumer path.

```csharp
var upper = new Bone { Name = "Upper", Rest = Transform.Identity };
var lower = new Bone { Name = "Lower", Position = new(32, 0),
    Rest = new Transform(0, new Vector2(32, 0)) };
upper.AddChild(lower);
rig.AddChild(upper); // Existing Skeleton context.
lower.Rotation = .5f; // Also writable through an AnimationPlayer track.
```

## Constructors

| Signature | Contract |
| --- | --- |
| `public Bone()` | [Inherited lifecycle/schema override.](#ctor) |

## Properties

| Signature | Contract |
| --- | --- |
| `public Electron2D.Transform Rest { get; set; }` | [Gets or sets the finite local rest transform.](#rest) |

## Methods and protected extension points

| Signature | Contract |
| --- | --- |
| `public System.Void ApplyRest()` | [Inherited lifecycle/schema override.](#applyrest) |
| `protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()` | [Inherited lifecycle/schema override.](#createsceneinstancefactory) |
| `public System.Boolean GetAutocalculateLengthAndAngle()` | [Inherited lifecycle/schema override.](#getautocalculatelengthandangle) |
| `public System.Single GetBoneAngle()` | [Inherited lifecycle/schema override.](#getboneangle) |
| `public override System.String[] GetConfigurationWarnings()` | [Inherited lifecycle/schema override.](#getconfigurationwarnings) |
| `public System.Int32 GetIndexInSkeleton()` | [Inherited lifecycle/schema override.](#getindexinskeleton) |
| `public System.Single GetLength()` | [Inherited lifecycle/schema override.](#getlength) |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | [Inherited lifecycle/schema override.](#getpropertydescriptors) |
| `public Electron2D.Transform GetSkeletonRest()` | [Inherited lifecycle/schema override.](#getskeletonrest) |
| `protected override System.Void OnNotification(System.Int32 what)` | [Inherited lifecycle/schema override.](#onnotification) |
| `public System.Void SetAutocalculateLengthAndAngle(System.Boolean autoCalculate)` | [Enables calculation from the first direct child Bone or keeps manually authored values.](#setautocalculatelengthandangle) |
| `public System.Void SetBoneAngle(System.Single angle)` | [Authors the finite endpoint direction used by skeletal modifications.](#setboneangle) |
| `public System.Void SetLength(System.Single length)` | [Authors a finite endpoint length; this does not change the node transform.](#setlength) |

## Member descriptions

### .ctor

`public Bone()`

Inherited lifecycle/schema override; see the linked base type.

### ApplyRest

`public System.Void ApplyRest()`

Inherited lifecycle/schema override; see the linked base type.

### CreateSceneInstanceFactory

`protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()`

Inherited lifecycle/schema override; see the linked base type.

### GetAutocalculateLengthAndAngle

`public System.Boolean GetAutocalculateLengthAndAngle()`

Inherited lifecycle/schema override; see the linked base type.

### GetBoneAngle

`public System.Single GetBoneAngle()`

Inherited lifecycle/schema override; see the linked base type.

### GetConfigurationWarnings

`public override System.String[] GetConfigurationWarnings()`

Inherited lifecycle/schema override; see the linked base type.

### GetIndexInSkeleton

`public System.Int32 GetIndexInSkeleton()`

Inherited lifecycle/schema override; see the linked base type.

### GetLength

`public System.Single GetLength()`

Inherited lifecycle/schema override; see the linked base type.

### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Inherited lifecycle/schema override; see the linked base type.

### GetSkeletonRest

`public Electron2D.Transform GetSkeletonRest()`

Inherited lifecycle/schema override; see the linked base type.

### OnNotification

`protected override System.Void OnNotification(System.Int32 what)`



### SetAutocalculateLengthAndAngle

`public System.Void SetAutocalculateLengthAndAngle(System.Boolean autoCalculate)`

Enables calculation from the first direct child Bone or keeps manually authored values.

A leaf retains its length and uses its transform rotation as the fallback angle.

- `autoCalculate`: Whether to calculate immediately and on Ready.

### SetBoneAngle

`public System.Void SetBoneAngle(System.Single angle)`

Authors the finite endpoint direction used by skeletal modifications.

- `angle`: Radians; independent of node Rotation.

### SetLength

`public System.Void SetLength(System.Single length)`

Authors a finite endpoint length; this does not change the node transform.

- `length`: Finite signed pixels.

### Rest

`public Electron2D.Transform Rest { get; set; }`

Gets or sets the finite local rest transform.

Zero initially. Singular rest poses remain authored but do not contribute skin deformation.

Throws `System.ArgumentException`: The transform is nonfinite.

## Lifecycle, errors and verification

Scene-bound calls follow the skeleton's owner thread. Cold structure/rest/path edits prepare new arrays and weak bindings; ordinary pose updates and retained replay reuse capacity. All numeric configuration requires finite values; strengths range from zero to one and phase selections use the existing ProcessPhase semantics. Invalid indices, disposed values, competing live bindings and structural reentry reject. User callbacks run after binding or setup commitment. Execution failures attempt every authored-pose restoration and release guards; setup failures keep GetIsSetup false for explicit retry. Resource graph persistence excludes transient setup, target references, palette RIDs and simulation history.

SkeletonTests exercises DFS membership/rest/indices, auto endpoints, persistent/transient/physics overrides, AnimationPlayer tracks, presentation interpolation, noncommuting and TopLevel coordinate transforms, strongest-four skin weights, path rename/reconnect, constraints, callback failures/reentry, owner guards, unique scene copies, fresh-process files and actual native pixels. 128 warmed managed pose/redraw/skin/replay iterations and 64 prepared native GPU/compatibility intervals allocate zero managed bytes. Native/backend allocations, large-rig performance, other platforms and owner acceptance are unverified. Editor gizmos retain their authoring trigger; IK solvers, physical bones and general Mesh skin now execute through their concrete components.

See [skeletal animation](../components/skeletal-animation.md), [the mesh decision](../decisions/mesh.md#adr-0092) and the linked base-class lifecycle.
