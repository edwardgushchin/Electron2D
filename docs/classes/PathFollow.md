# PathFollow

Last updated: 2026-09-23

**Inherits:** [Entity](Entity.md), [CanvasItem](CanvasItem.md), [Node](Node.md), [ElectronObject](ElectronObject.md)

- **Declaration:** `public class PathFollow : Entity`
- **Source:** [PathFollow.cs](../../src/Scene/2D/PathFollow.cs)
- **Component:** [Scene paths](../components/scene-paths.md)

## Description

Samples its direct [Path](Path.md) parent while inside a SceneTree. Descendants inherit its position/rotation through the ordinary Entity transform. A parent Path has to hold a nonzero-length [PathCurve](PathCurve.md) before sampling can move the follower. Detached followers retain raw Progress; neutral intermediaries and other parent types do not bind. Entry/reentry samples the retained value, exit clears the binding. Manual notification dispatch does not forge membership.

Progress is local distance. Assignments wrap when Loop is true and clamp otherwise. Nonzero input wrapping approximately to zero selects the final endpoint; both positive and negative exact multiples select the end. The canonical Mathf.Epsilon policy applies. Entering a tree and replacing/editing a curve resample without renormalizing stored progress, so ProgressRatio can exceed one. Null/zero-length curves retain the transform; a Progress assignment on a zero-length curve becomes zero. Ratio reads return zero without a positive-length attached curve; ratio writes require one and throw otherwise.

Rotates=true follows the tangent with +X forward and applies HOffset/VOffset along/perpendicular to it. Rotates=false preserves rotation and applies offsets along local X/Y. Sampling preserves the node's scale/skew. Rotation is assigned before position; exceptions can leave the committed progress/rotation with the previous position. A reentrant follower update or reparent/disposal during rotation prevents the stale outer update from overwriting the later position. Merely changing Loop or CubicInterp does not resample; a subsequent progress/offset/rotation/curve update uses the new policy.

There is no automatic speed or clock. Gameplay code or an existing Tween writes Progress/ProgressRatio. Inherited ProcessMode/visibility do not prevent an explicit setter from resampling. The inherited Reparent keepGlobalTransform=true restores the original global transform after new-parent binding; the next path update resamples the new path. Use false for immediate path placement.

Setters require the scene owner thread and reject scene capture; finite values are required. Node state is not a cross-thread transaction. Resource edits from a worker are marshalled by Path. PackedScene stores Progress, HOffset, VOffset, Rotates, CubicInterp and Loop; ProgressRatio is derived tooling metadata and is not stored. Detached reconstruction restores spatial state before binding on activation. Disposal and callback failure use inherited Node lifecycle rules.

## Example

Standalone managed setup using the public API and `using Electron2D;`.

```csharp
using var curve = new PathCurve();
curve.AddPoint(Vector2.Zero, outHandle: new(10, 0));
curve.AddPoint(new(30, 0), inHandle: new(-10, 0));
var path = new Electron2D.Path { Curve = curve };
var follower = new PathFollow { Progress = 15, CubicInterp = false };
path.AddChild(follower); // Add drawable/game nodes beneath follower.
using var tree = new SceneTree(path);
follower.ProgressRatio = .5f; // Position is (15, 0).
follower.CreateTween().TweenProperty(follower,
    n => n.Progress, (n, value) => n.Progress = value, 30f, 1);
tree.ProcessFrame(.5); // Embedding caller supplies time; Engine.Run normally drives a Window.
```

## Constructors

| Declaration | Contract |
| --- | --- |
| [`public PathFollow()`](#pathfollow) | Creates a detached follower at zero progress/offset with rotation, cubic interpolation and looping enabled. |

## Properties

| Declaration | Contract |
| --- | --- |
| [`public float Progress { get; set; }`](#progress) | Gets or sets local distance along the parent curve. |
| [`public float ProgressRatio { get; set; }`](#progressratio) | Gets or sets progress as a fraction of the current curve length. |
| [`public float HOffset { get; set; }`](#hoffset) | Gets or sets the forward offset, or local X offset when Rotates is false. |
| [`public float VOffset { get; set; }`](#voffset) | Gets or sets the perpendicular offset, or local Y offset when Rotates is false. |
| [`public bool Rotates { get; set; }`](#rotates) | Gets or sets whether +X follows the sampled forward direction. |
| [`public bool CubicInterp { get; set; }`](#cubicinterp) | Gets or sets cubic position interpolation between baked points. |
| [`public bool Loop { get; set; }`](#loop) | Gets or sets whether future Progress assignments wrap by curve length. |

## Methods

| Declaration | Contract |
| --- | --- |
| [`public override string[] GetConfigurationWarnings()`](#diagnostics-getconfigurationwarnings) | Returns current configuration warnings; includes inherited warnings. |

## Protected hooks

| Declaration | Contract |
| --- | --- |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#getpropertydescriptors) | Appends the typed properties listed above to the inherited stored Entity state. |
| [`protected override Func<Node> CreateSceneInstanceFactory()`](#createsceneinstancefactory) | Supplies a static factory for this exact node type. Derived types must supply their own existing Node factory contract. |

## Constructors descriptions

### PathFollow

`public PathFollow()`

Creates a detached follower at zero progress/offset with rotation, cubic interpolation and looping enabled.

## Properties descriptions

### Progress

`public float Progress { get; set; }`

Gets or sets local distance along the parent curve.

Value: Zero initially; any finite value can be assigned.

Contract: While attached to a Path with a curve, wraps by its length when Loop is true, otherwise clamps. A nonzero request wrapping approximately to zero selects the final endpoint. Detached or null-curve state retains the raw value. Attachment/curve changes sample it without renormalizing. Every assignment updates the transform, even if unchanged. A zero-length curve clamps progress to zero but retains the transform.

`ArgumentOutOfRangeException`: The value is nonfinite.

`InvalidOperationException`: Mutation is unavailable or curve geometry is invalid.

`ObjectDisposedException`: This node or its curve is disposed.

`Exception`: A transform callback fails after stored progress has changed.

### ProgressRatio

`public float ProgressRatio { get; set; }`

Gets or sets progress as a fraction of the current curve length.

Value: Zero when unbound, missing a curve or its length is zero; otherwise Progress divided by length.

Contract: The setter requires a direct Path parent in a tree and a positive-length curve. It multiplies the finite ratio by length and delegates to Progress, so looping/clamping applies; the getter can exceed one after a curve changes length. This derived property is not stored in PackedScene.

`ArgumentOutOfRangeException`: The ratio or its distance product is nonfinite.

`InvalidOperationException`: The binding/curve/length is unavailable, or mutation is unavailable.

`ObjectDisposedException`: This node or its curve is disposed.

`Exception`: A transform callback fails after progress has changed.

### HOffset

`public float HOffset { get; set; }`

Gets or sets the forward offset, or local X offset when Rotates is false.

Value: Zero initially, in finite local units.

Contract: Every assignment immediately resamples the attached curve.

`ArgumentOutOfRangeException`: The offset is nonfinite.

`InvalidOperationException`: Mutation or valid curve sampling is unavailable.

`ObjectDisposedException`: The node or curve is disposed.

`Exception`: A transform callback fails after assignment.

### VOffset

`public float VOffset { get; set; }`

Gets or sets the perpendicular offset, or local Y offset when Rotates is false.

Value: Zero initially, in finite local units.

Contract: Every assignment immediately resamples the attached curve.

`ArgumentOutOfRangeException`: The offset is nonfinite.

`InvalidOperationException`: Mutation or valid curve sampling is unavailable.

`ObjectDisposedException`: The node or curve is disposed.

`Exception`: A transform callback fails after assignment.

### Rotates

`public bool Rotates { get; set; }`

Gets or sets whether +X follows the sampled forward direction.

Value: True initially.

Contract: Every assignment resamples. False preserves the current rotation and applies offsets in local X/Y.

`InvalidOperationException`: Mutation or valid curve sampling is unavailable.

`ObjectDisposedException`: The node or curve is disposed.

`Exception`: A transform callback fails after assignment.

### CubicInterp

`public bool CubicInterp { get; set; }`

Gets or sets cubic position interpolation between baked points.

Value: True initially.

Contract: Assignment stores the policy without resampling; the next progress/offset/rotation/curve update uses it.

`InvalidOperationException`: Mutation is unavailable.

`ObjectDisposedException`: The node is disposed.

### Loop

`public bool Loop { get; set; }`

Gets or sets whether future Progress assignments wrap by curve length.

Value: True initially.

Contract: Assignment does not change current progress or transform. Nonlooping assignments clamp instead.

`InvalidOperationException`: Mutation is unavailable.

`ObjectDisposedException`: The node is disposed.

## Method Descriptions

<a id="diagnostics-getconfigurationwarnings"></a>
### `public override string[] GetConfigurationWarnings()`

Returns current configuration warnings; includes inherited warnings.

Contract: Appends a warning when visible in a tree without a direct Path parent. Hidden or detached followers add no warning. A missing or zero-length curve is valid and adds no warning.


## Protected hooks descriptions

### GetPropertyDescriptors

`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Appends the typed properties listed above to the inherited stored Entity state.

Contract: Stores progress, offsets and policies; ProgressRatio is tooling-only and derived on attachment.

### CreateSceneInstanceFactory

`protected override Func<Node> CreateSceneInstanceFactory()`

Supplies a static factory for this exact node type. Derived types must supply their own existing Node factory contract.

## Dependencies, audit and verification limits

Depends on Entity/CanvasItem/Node membership and transforms, PathCurve sampling, Resource.Changed, typed PropertyDescriptor, PackedScene and the existing SceneTree deferred queue. No native dependency or independent scheduling API is added. [PathTests](../../tests/Electron2D.Tests/PathTests.cs) covers analytic geometry, defaults, wrapping/clamping, policy timing, offsets, rotation/scale/skew, reparenting, scene ownership, resource changes, callback reentry/failures, worker delivery, stale queued work and zero warmed movement allocation with descendants. Canvas transform snapshots use the standard shared array pool; pool growth, resource edits and user callbacks can allocate.

[PathRenderingTests](../../tests/Electron2D.Tests/PathRenderingTests.cs) verifies pixel movement, curve replacement, worker edits, offsets and inherited visibility on Linux Wayland compatibility/GPU and dummy/software. This is automated native evidence, not owner visual acceptance, other-platform or published/AOT verification. The [pinned implementation](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/scene/2d/path_2d.cpp) was compared with the complete XML. Typed errors, membership safety, thread delivery and failure aggregation follow the existing Electron2D decisions. Reference identities remain on the coverage pages.

Editor curve handles/selection and the editor-only debounce timer remain missing capabilities; [scene-path dependency triggers](../components/scene-paths.md#remaining-shared-capabilities) identify their first slices. No private editor method or inert timer is exposed here. Path3D/PathFollow3D remain permanently excluded. See ADRs [0008](../decisions/scene.md#adr-0008), [0011](../decisions/scene.md#adr-0011), [0013](../decisions/resources.md#adr-0013), [0023](../decisions/scene.md#adr-0023).

Configuration queries add one warning only when the follower is visible in a tree and its direct parent is not Path. Null/empty parent curves are valid. GetConfigurationWarnings is a typed override; a further override should include base results. Warning conditions are read live; UpdateConfigurationWarnings is an explicit refresh request, not an automatic visibility or hierarchy subscription. Verification: [SceneDiagnosticsTests](../../tests/Electron2D.Tests/SceneDiagnosticsTests.cs).
