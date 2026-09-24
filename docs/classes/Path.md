# Path

Last updated: 2026-09-23

**Inherits:** [Entity](Entity.md), [CanvasItem](CanvasItem.md), [Node](Node.md), [ElectronObject](ElectronObject.md)

- **Declaration:** `public class Path : Entity`
- **Source:** [Path.cs](../../src/Scene/2D/Path.cs)
- **Component:** [Scene paths](../components/scene-paths.md)

## Description

A spatial parent containing a borrowed [Curve2D](Curve2D.md). Only direct attached [PathFollow](PathFollow.md) children sample it. Path starts with a null curve and draws diagnostic geometry only when SceneTree.DebugPathsHint is enabled. Its transform affects descendants through Entity/CanvasItem; neutral intermediary nodes do not bind followers.

Assigning Curve always disconnects/reconnects its Changed subscription and resamples attached followers, including assignment of the same resource. Detached changes do not move detached followers; tree entry samples current state. Null or zero-length curves retain existing transforms. Curve replacement/edits do not wrap or clamp stored progress. External disposal of a borrowed curve is caller error; later resource access throws.

Owner-thread changes synchronously visit a child snapshot, revalidate membership, attempt all eligible followers and aggregate failures. Worker-thread changes queue one action per Changed through SceneTree.Defer. The next flush updates transforms on the owner even while paused; stale work from a different resource or membership is ignored. Closing the scene queue drops that update, and later tree entry samples current data. Deferred resource edits allocate closures/snapshots; no unbounded polling loop or background scheduler is introduced. Detached resource edits never mutate node transforms.

Disposal disconnects the borrowed resource and invalidates pending work. Ordinary node disposal does not dispose the curve; scene-local duplication and root ownership still follow PackedScene. The stored Curve descriptor participates in the existing graph duplication session. User callbacks can fail after assignment; it is not rolled back.

The public name projects Path2D without a redundant dimension suffix under ADR 0008. It differs from System.IO.Path: qualify filesystem access or use an alias such as `using IOPath = System.IO.Path;` when importing both namespaces.

## Example

Standalone managed setup using the public API and `using Electron2D;`.

```csharp
using var curve = new Curve2D();
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
| [`public Path()`](#path) | Creates a detached path with no curve. |

## Properties

| Declaration | Contract |
| --- | --- |
| [`public Curve2D? Curve { get; set; }`](#curve) | Gets or sets the borrowed local curve. |

## Protected hooks

| Declaration | Contract |
| --- | --- |
| [`protected override void OnDraw()`](#diagnostics-ondraw) | Records diagnostic path geometry during the inherited drawing scope. |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#getpropertydescriptors) | Appends the typed properties listed above to the inherited stored Entity state. |
| [`protected override Func<Node> CreateSceneInstanceFactory()`](#createsceneinstancefactory) | Supplies a static factory for this exact node type. Derived types must supply their own existing Node factory contract. |
| [`protected override void Dispose(bool disposing)`](#dispose) | Disconnects the borrowed curve when disposing is true, invalidates pending membership work, and delegates owned subtree cleanup to Entity/Node. Does not dispose the borrowed curve. |

## Constructors descriptions

### Path

`public Path()`

Creates a detached path with no curve.

## Properties descriptions

### Curve

`public Curve2D? Curve { get; set; }`

Gets or sets the borrowed local curve.

Value: Null initially; null or zero-length curves leave followers' transforms unchanged.

Contract: Every assignment reconnects Changed and updates attached direct followers, even for the same resource. Progress is not rewrapped or clamped when replacing/editing the curve. All eligible followers are attempted after callback failures. Detached paths defer sampling until followers enter a tree.

`ObjectDisposedException`: This node or the assigned curve is disposed.

`InvalidOperationException`: Scene mutation is unavailable or curve geometry is invalid.

`AggregateException`: A follower update fails after the curve has been assigned.

## Protected hooks descriptions

<a id="diagnostics-ondraw"></a>
### `protected override void OnDraw()`

Records diagnostic path geometry during the inherited drawing scope.

Contract: When DebugPathsHint is enabled, records one-pixel curve segments at approximately ten local units and paired tangent markers every fourth sample. Null, single-point and near-zero-length curves draw nothing. Recording is bounded to 1,048,576 samples; longer geometry fails before adding commands. Inherited drawing, transforms, visibility, modulation and callback failure behavior remain in force.

InvalidOperationException: The curve exceeds the diagnostic sampling budget or has invalid geometry.


### GetPropertyDescriptors

`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Appends the typed properties listed above to the inherited stored Entity state.

Contract: Appends the stored borrowed Curve resource; local-scene copying uses existing PackedScene rules.

### CreateSceneInstanceFactory

`protected override Func<Node> CreateSceneInstanceFactory()`

Supplies a static factory for this exact node type. Derived types must supply their own existing Node factory contract.

### Dispose

`protected override void Dispose(bool disposing)`

Disconnects the borrowed curve when disposing is true, invalidates pending membership work, and delegates owned subtree cleanup to Entity/Node. Does not dispose the borrowed curve.

## Dependencies, audit and verification limits

Depends on Entity/CanvasItem/Node membership and transforms, Curve2D sampling, Resource.Changed, typed PropertyDescriptor, PackedScene and the existing SceneTree deferred queue. No native dependency or independent scheduling API is added. [PathTests](../../tests/Electron2D.Tests/PathTests.cs) covers analytic geometry, defaults, wrapping/clamping, policy timing, offsets, rotation/scale/skew, reparenting, scene ownership, resource changes, callback reentry/failures, worker delivery, stale queued work and zero warmed movement allocation with descendants. Canvas transform snapshots use the standard shared array pool; pool growth, resource edits and user callbacks can allocate.

[PathRenderingTests](../../tests/Electron2D.Tests/PathRenderingTests.cs) verifies pixel movement, curve replacement, worker edits, offsets and inherited visibility on Linux Wayland compatibility/GPU and dummy/software. This is automated native evidence, not owner visual acceptance, other-platform or published/AOT verification. The [pinned implementation](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/scene/2d/path_2d.cpp) was compared with the complete XML. Typed errors, membership safety, thread delivery and failure aggregation follow the existing Electron2D decisions. Reference identities remain on the coverage pages.

Editor curve handles/selection and the editor-only debounce timer remain missing capabilities; [scene-path dependency triggers](../components/scene-paths.md#remaining-shared-capabilities) identify their first slices. No private editor method or inert timer is exposed here. Path3D/PathFollow3D remain permanently excluded. See ADRs [0008](../decisions/scene.md#adr-0008), [0011](../decisions/scene.md#adr-0011), [0013](../decisions/resources.md#adr-0013), [0023](../decisions/scene.md#adr-0023).

Diagnostic drawing is opt-in through SceneTree.DebugPathsHint. It uses the construction-time ProjectSettings.DebugPathsColor snapshot, existing one-framebuffer-pixel lines, approximately ten-unit samples, and paired five-unit backward tangent markers every fourth sample. Scale does not thicken the pixel-width lines. Inherited visibility, transforms, modulation, material and ordering still apply. Curve edits/hint toggles invalidate retained drawing; worker changes use the existing deferred owner-thread path. Curve length must produce at most 1,048,576 samples, otherwise recording fails and discards the whole partial command list. No geometry is recorded for null, fewer than two points, or length at most Mathf.Epsilon.

This is runtime debug visualization. Automatic editor hints, selection handles and authoring remain absent. Native checks: [PathRenderingTests](../../tests/Electron2D.Tests/PathRenderingTests.cs); managed budget/failure checks: [SceneDiagnosticsTests](../../tests/Electron2D.Tests/SceneDiagnosticsTests.cs).
