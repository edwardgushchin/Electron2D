# RemoteTransform

Last updated: 2026-09-23

**Inherits:** [Entity](Entity.md), [CanvasItem](CanvasItem.md), [Node](Node.md), [ElectronObject](ElectronObject.md)

**Inherited By:** —

- **Declaration:** `public class RemoteTransform : Entity`
- **Source:** [RemoteTransform.cs](../../src/Scene/2D/RemoteTransform.cs)
- **Component:** [Scene hierarchy](../components/scene-hierarchy.md)

## Description

Copies selected transform components to another Entity in the same SceneTree. The destination is identified by a Node path relative to this node and held weakly. The path is resolved on tree entry, on a change to RemotePath, or by ForceUpdateCache. A missing target or a target outside the tree is ignored. Self, ancestors, descendants and cycles of remote targets are rejected to prevent transform feedback. A newly added, moved or replaced target requires an explicit cache refresh.

Global coordinates are the default: a source global-transform notification queues a copy through the existing SceneTree transform queue. Local mode uses attached-only synchronous local-transform notifications. Changing the path or any copy policy immediately writes the selected components if a valid target is cached; ForceUpdateCache only refreshes the destination. If all three component switches are false, no target write occurs. Position means the transform origin; rotation carries the rotation/skew basis; scale is selected independently. A target's own callbacks can reenter this operation; a newer change is replayed after the current write, with a finite guard against a nonsettling callback loop.

RemotePath and all copy policies are stored in PackedScene along with inherited Entity state. The target reference, notification queue and callbacks are not stored. Attached mutation uses the SceneTree owner thread. A target transform callback can throw after its target state was written; the exception propagates through the normal scene notification path. Inherited Node disposal and scene ownership rules apply.

## Example

Standalone managed setup using only the public API and `using Electron2D;`:

```csharp
using var root = new Node { Name = "root" };
var driver = new RemoteTransform { Name = "driver", RemotePath = "../follower" };
var follower = new Entity { Name = "follower" };
root.AddChild(driver);
root.AddChild(follower);
using var tree = new SceneTree(root);
driver.Position = new Vector2(24, 12);
tree.ProcessFrame(0); // Delivers the queued global-transform notification.
```

## Constructors

| Declaration | Contract |
| --- | --- |
| [`public RemoteTransform()`](#remotetransform) | Creates a detached spatial node with global coordinates and all three copy switches enabled. |

## Properties

| Declaration | Contract |
| --- | --- |
| [`public string RemotePath { get; set; }`](#remotepath) | Target path; empty by default. |
| [`public bool UseGlobalCoordinates { get; set; }`](#useglobalcoordinates) | Selects global or local transforms; true by default. |
| [`public bool UpdatePosition { get; set; }`](#updateposition) | Copies the origin; true by default. |
| [`public bool UpdateRotation { get; set; }`](#updaterotation) | Copies the rotation/skew basis; true by default. |
| [`public bool UpdateScale { get; set; }`](#updatescale) | Copies scale; true by default. |

## Methods

| Declaration | Contract |
| --- | --- |
| [`public void ForceUpdateCache()`](#forceupdatecache) | Resolves RemotePath again without writing the destination. |
| [`public override string[] GetConfigurationWarnings()`](#getconfigurationwarnings) | Includes a warning for an invalid spatial target path. |

## Protected extension points

| Declaration | Contract |
| --- | --- |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#getpropertydescriptors) | Adds the five stored properties to inherited scene metadata. |
| [`protected override Func<Node> CreateSceneInstanceFactory()`](#createsceneinstancefactory) | Reconstructs this exact node type in PackedScene; derived classes supply their own factory. |
| [`protected override void OnNotification(int what)`](#onnotification) | Copies on the selected local or global transform notification, then delegates to the base handler. |

## Constructor descriptions

### RemoteTransform

`public RemoteTransform()`

Initializes a detached Entity with NotifyTransformChanges enabled, a blank target path and all copy policies enabled.

## Property descriptions

### RemotePath

`public string RemotePath { get; set; }`

Gets or sets the relative Node path. Changing it while attached replaces the cached weak target and immediately transfers enabled components. Empty, unresolved, unrelated-type, self and ancestor/descendant paths do not bind. Null is rejected. A cached target that leaves the tree is skipped until ForceUpdateCache or another path change resolves it again.

### UseGlobalCoordinates

`public bool UseGlobalCoordinates { get; set; }`

True copies GlobalTransform between nodes; false copies Transform independently of each node's parent placement. A change switches the inherited transform-notification policy and immediately updates a valid target. Global delivery follows the scene queue; local delivery is synchronous while attached.

### UpdatePosition

`public bool UpdatePosition { get; set; }`

Controls copying of the transform origin in the selected coordinate space. A change immediately updates a valid target. Disabled position preserves the destination origin.

### UpdateRotation

`public bool UpdateRotation { get; set; }`

Controls copying of the source rotation and skew basis. A change immediately updates a valid target. Scale remains independent, so a partial copy can reconstruct the destination transform from selected components.

### UpdateScale

`public bool UpdateScale { get; set; }`

Controls copying of source scale. A change immediately updates a valid target. Disabled scale preserves destination scale.

All setters use the inherited scene mutation guard. Attached changes require the owning thread; disposed objects reject access. A destination transform setter or its callback can fail after the local policy change has committed.

## Method descriptions

### ForceUpdateCache

`public void ForceUpdateCache()`

Re-resolves the current path against the live hierarchy. It does not itself copy a transform. A subsequent source notification or copy-policy change does. Requires the ordinary mutation guard.

### GetConfigurationWarnings

`public override string[] GetConfigurationWarnings()`

Returns inherited warnings plus a warning when RemotePath does not resolve to a valid unrelated Entity. It does not change the target cache or trigger a transfer.

## Protected extension point descriptions

### GetPropertyDescriptors

`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Appends RemotePath and the four copy-policy properties to the inherited typed stored metadata for PackedScene.

### CreateSceneInstanceFactory

`protected override Func<Node> CreateSceneInstanceFactory()`

Returns a static exact-type factory for RemoteTransform. A derived runtime type uses the inherited explicit-factory rule.

### OnNotification

`protected override void OnNotification(int what)`

Transfers on `NotificationTransformChanged` in global mode or `NotificationLocalTransformChanged` in local mode, then calls the inherited handler. Other notifications do not transfer. A reentrant target callback may request one or more additional writes; nonsettling callbacks fail after 64 passes.

## Verification and limits

[RemoteTransformTests](../../tests/Electron2D.Tests/RemoteTransformTests.cs) covers initial global and synchronous local updates, differing parent positions, independent switches, cache lifetime, invalid and reciprocal paths, PackedScene reconstruction, callback reentry/failure and owner-thread rejection. The behavior was audited against the pinned [remote transform implementation](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/scene/2d/remote_transform_2d.cpp). This managed check does not establish native renderer, other-platform or physical-input acceptance. Inherited physics interpolation reset is a separate missing scene capability.

## Decisions

- [0008: Scene inheritance and API correspondence](../decisions/scene.md#adr-0008)
- [0011: Scene lifecycle and failure continuation](../decisions/scene.md#adr-0011)
- [0023: Typed packed scenes](../decisions/scene.md#adr-0023)
