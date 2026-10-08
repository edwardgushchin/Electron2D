# Area

Last updated: 2026-10-08

**Inherits:** [CollisionObject](CollisionObject.md), [Entity](Entity.md), CanvasItem, Node, ElectronObject

- **Source:** [Area.cs](../../src/Scene/2D/Area.cs), [Area.Fields.cs](../../src/Scene/2D/Area.Fields.cs)
- **Declaration:** `public sealed partial class Area : CollisionObject`
- **Component:** [Physics areas](../components/physics-areas.md)

## Description

A nonresponding 2D sensor region. Direct [CollisionShape](CollisionShape.md) children supply borrowed circle, capsule, segment, convex polygon, concave segment collection or rectangle geometry. A child's one-way setting is retained for scene state but cannot filter this area's sensor overlaps. After each nonzero fixed physics step, the area records overlapping `PhysicsBody` and other `Area` nodes; moving a node or editing a filter does not immediately change the snapshot. Events are delivered after body synchronization and before physics timers and tweens. The area's `CollisionMask` tests the other object's `CollisionLayer`; the other object's mask can be zero. Other areas also need `Monitorable=true` to be reported. The area can monitor even when its own `Monitorable` is false. Independently, gravity and damping fields affect overlapping RigidBody dynamics, while gravity also feeds CharacterBody's inherited `GetGravity()` query before the solver step. A shaped area can also reroute a spatial audio source whose `AreaMask` intersects its collision layer; monitoring flags do not gate that point query.

## Example

A direct [CollisionPolygon](CollisionPolygon.md) child can sense an entire solid concave region or only the closed hollow contour. Its one-way flag does not filter this Area's sensor overlaps.

```csharp
using var region = new RectangleShape { Size = new Vector2(80, 80) };
var trigger = new Area { GravitySpaceOverride = Area.SpaceOverride.Replace, Gravity = 0 };
trigger.AddChild(new CollisionShape { Shape = region });
trigger.BodyEntered += body => Console.WriteLine(body.Name);
// Add trigger to a SceneTree, then advance a fixed physics frame.
```

## API summary

The inherited [CollisionObject.GetRID](CollisionObject.md#getrid) identifies this sensor across fixture rebuilds. Direct [PhysicsDirectSpaceState](PhysicsDirectSpaceState.md) queries can include Area fixtures when their parameters enable `CollideWithAreas`; object-level area monitoring continues on its fixed-step snapshot path.

| Member | Default | Contract |
| --- | --- | --- |
| `public Area()` | — | Creates a detached monitoring, monitorable area. |
| `public bool Monitoring { get; set; }` | true | Enables this area's detection after the next fixed step. |
| `public bool Monitorable { get; set; }` | true | Makes this area detectable by another area. |
| `public bool AudioBusOverride { get; set; }` | false | Enables spatial source bus selection inside this area. |
| `public string AudioBusName { get; set; }` | Master | Requested bus for eligible spatial sources. |
| `public enum SpaceOverride` | — | Five field combination modes; see [values](Area.SpaceOverride.md). |
| `public SpaceOverride GravitySpaceOverride { get; set; }` | Disabled | Determines gravity combination and stopping. |
| `public float Gravity { get; set; }` | 980 | Finite signed gravity strength in scene units/s². |
| `public Vector2 GravityDirection { get; set; }` | (0, 1) | Unnormalized local direction; shares storage with `GravityPointCenter`. |
| `public bool GravityPoint { get; set; }` | false | Uses a transformed local point as the gravity target. |
| `public Vector2 GravityPointCenter { get; set; }` | (0, 1) | Local attraction point; shares storage with `GravityDirection`. |
| `public float GravityPointUnitDistance { get; set; }` | 0 | Positive values enable inverse-square point falloff. |
| `public SpaceOverride LinearDampSpaceOverride { get; set; }` | Disabled | Determines linear damping combination and stopping. |
| `public float LinearDamp { get; set; }` | 0.1 | Finite signed linear damping rate per second. |
| `public SpaceOverride AngularDampSpaceOverride { get; set; }` | Disabled | Determines angular damping combination and stopping. |
| `public float AngularDamp { get; set; }` | 1 | Finite signed angular damping rate per second. |
| `public int Priority { get; set; }` | 0 | Higher area priorities resolve first. |
| `public Area[] GetOverlappingAreas()` | — | Returns a caller-owned copy of the latest area snapshot. |
| `public Entity[] GetOverlappingBodies()` | — | Returns a caller-owned copy of the latest body snapshot. |
| `public bool HasOverlappingAreas()` | — | Tests the area snapshot without creating an array. |
| `public bool HasOverlappingBodies()` | — | Tests the body snapshot without creating an array. |
| `public bool OverlapsArea(Node? area)` | — | Tests membership; null and unrelated nodes return false. |
| `public bool OverlapsBody(Node? body)` | — | Tests membership; null and unrelated nodes return false. |
| `public event Action<Area>? AreaEntered` / `AreaExited` | — | Reports another area entering or leaving. |
| `public event Action<Entity>? BodyEntered` / `BodyExited` | — | Reports a physics body entering or leaving. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | — | Stores monitoring flags and fields for PackedScene. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | — | Restores the exact area type in PackedScene. |
| `protected override void OnEnterTree()` / `OnExitTree()` | — | Joins or leaves this SceneTree's physics world. |
| `protected override void Dispose(bool disposing)` | — | Releases backend fixtures and overlap snapshots. |

## Property descriptions

<a id="audiobusoverride"></a>
<a id="audiobusname"></a>
### `AudioBusOverride` and `AudioBusName`

Both are stored by `PackedScene`. When override is true, a shaped area containing an [AudioStreamEmitter](AudioStreamEmitter.md) source position may route that source to `AudioBusName`, provided its collision layer intersects the player's `AreaMask`. This is a live point query at initial Play and each fixed step. Unknown bus names resolve to Master. `Monitoring`, `Monitorable`, the area's collision mask and its physical field priority do not select the audio route. The name cannot be null. Attached access requires the scene owner; mutation during scene capture and access after disposal reject.


<a id="monitoring"></a>
### `Monitoring` and `Monitorable`

Both default to true and are stored with the scene. `Monitoring=false` clears this area's overlaps on its next nonzero physics step and emits exits. `Monitorable=false` removes this area from other areas' snapshots on their next step, while this area's own detection continues. A body's mask and an area's own `Monitorable` do not gate this area's body detection. The inherited layer and mask use all 32 bits.

<a id="fields"></a>
<a id="gravityspaceoverride"></a>
<a id="lineardampspaceoverride"></a>
<a id="angulardampspaceoverride"></a>
<a id="priority"></a>
### `GravitySpaceOverride`, `LinearDampSpaceOverride`, `AngularDampSpaceOverride` and `Priority`

An area applies a field when its shape overlaps a dynamic body and its collision mask includes the body's layer. Its `Monitoring` and `Monitorable` flags do not gate field effects. Each of gravity, linear damping and angular damping has an independent [SpaceOverride](Area.SpaceOverride.md) mode. Areas resolve from greatest `Priority` to least; equal priorities use scene registration order. `Combine` adds, `CombineReplace` adds and stops, `Replace` replaces and stops, and `ReplaceCombine` replaces but continues. A field that has not stopped includes the world's typed default from [ProjectSettings](ProjectSettings.md#physics2ddefaults). Mode `Disabled` contributes nothing. Body damping then adds to or replaces the resolved linear/angular rate according to [RigidBody.DampMode](RigidBody.DampMode.md).

<a id="gravity"></a>
<a id="gravitydirection"></a>
<a id="gravitypoint"></a>
<a id="gravitypointcenter"></a>
<a id="gravitypointunitdistance"></a>
### `Gravity`, `GravityDirection`, `GravityPoint`, `GravityPointCenter` and `GravityPointUnitDistance`

Directional gravity is `GravityDirection * Gravity` without normalization. Point gravity aims from the body at the area's transformed `GravityPointCenter`; positive `GravityPointUnitDistance` gives inverse-square falloff relative to that distance, while zero or negative values use constant strength. At the attraction point the vector is zero. `GravityDirection` and `GravityPointCenter` are two views of the same stored vector, matching the reference. Finite signed strengths and distances are accepted; vectors require finite components. Invalid input and enum values throw before mutation. A finite combination that would produce nonfinite motion rejects the fixed step before changing body velocity and succeeds after correction.

<a id="lineardamp"></a>
<a id="angulardamp"></a>
### `LinearDamp` and `AngularDamp`

The two finite signed rates contribute independently to each overlapping dynamic body. Positive rates reduce speed; negative rates increase it. Body damping combines or replaces each resolved rate after area and world reduction, then velocity is multiplied by `max(0, 1 - delta * totalDamp)` before force integration. Changes to an effective field wake a sleeping body on the next fixed step.

## Method descriptions

<a id="overlaps"></a>
### Overlap queries

The six query methods read the most recently committed snapshot, even after a node moves. Area and body arrays are fresh caller-owned arrays; `HasOverlapping*` and `Overlaps*` avoid array allocation. `GetOverlappingBodies()` returns the implemented `PhysicsBody` family as `Entity` values, preserving the spatial role for future collider owners. Detached queries return empty results. Attached queries require the SceneTree owner thread. Removing an object from its tree clears it from other areas' snapshots without waiting for another frame.

## Event descriptions

<a id="events"></a>
### Area and body entry/exit events

Object-level events are raised once per other node even when multiple shape pairs overlap. The argument is the other area or spatial body. Physics backend stepping and body synchronization finish before callbacks; event handlers may remove or reparent nodes. A throwing handler does not replay the committed transition on the next frame, and other queued transitions still run before an aggregate exception reaches the physics-frame caller. Zero-delta frames do not change snapshots or emit events. Shape-index events require typed physics identity and shape-owner APIs and remain separate coverage rows.

## Lifecycle, errors and verification

The area owns its backend sensor fixtures, while its child shapes borrow caller-owned Shape resources. A moving body passes through without collision response. Active area and shape transforms require unit scale and zero skew; an invalid transform fails the step before replacing geometry, and a corrected step can continue. Filter, disabled-state and shape changes take effect on the next step. Exiting or disposing removes backend state; re-entry creates new fixtures. [AreaTests](../../tests/Electron2D.Tests/AreaTests.cs) checks directional filtering including body mask zero, area monitorability, multi-shape deduplication, moving-body passage, live geometry, packing, tree removal, callback failure and mutation. [PhysicsAreaFieldTests](../../tests/Electron2D.Tests/PhysicsAreaFieldTests.cs) checks field defaults, priority and combination modes, point falloff, signed damping, scene packing and finite failure recovery. The tests check 64 warmed steady, moving-field and sleeping-field frames with zero managed allocations on Linux/.NET 8. Native allocation, other platforms and large-scene performance remain unverified.

The body-result and body-event methods currently cover `PhysicsBody` nodes; tile-map virtual collision bodies are not yet integrated. Gravity and priority effects execute for RigidBody and CharacterBody; only RigidBody applies this field as force automatically. [CharacterBodyTests](../../tests/Electron2D.Tests/CharacterBodyTests.cs) verifies its inherited gravity query. Audio-bus routing and shape-index events remain incomplete on [Area2D coverage](../coverage/classes/Area2D.md). [ADRs 0055 and 0056](../decisions/physics.md#adr-0056) record the monitoring and field contracts.

A SeparationRayShape child senses a directed front-facing surface crossing, including its exact short-ray extent. A ray starting inside filled geometry and two rays do not overlap. Scene Area overlap and field selection use this same kernel under [ADR 0068](../decisions/physics.md#adr-0068), verified by SeparationRayShapeTests.

The [CollisionObject owner registry](CollisionObject.md#createshapeowner) now supplies logical shape slots for both child and manual groups. Query/contact indices identify global slots, while ShapeFindOwner returns the distinct group ID; removal shifts later indices. Motion owner accessors resolve weak configured objects as well as child nodes. See [ADR 0071](../decisions/physics.md#adr-0071).

## Logical shape-pair events

| Signature | Payload |
| --- | --- |
| `public event Action<RID, Area?, int, int>? AreaShapeEntered` | Collider RID, nullable scene Area, collider global logical shape index, local index. |
| `public event Action<RID, Area?, int, int>? AreaShapeExited` | Retained departed Area pair identity and sampled indices. |
| `public event Action<RID, Entity?, int, int>? BodyShapeEntered` | Collider RID, nullable spatial body, body global logical shape index, local index. |
| `public event Action<RID, Entity?, int, int>? BodyShapeExited` | Retained departed body pair identity and sampled indices. |

<a id="areashapeentered"></a>
<a id="areashapeexited"></a>
<a id="bodyshapeentered"></a>
<a id="bodyshapeexited"></a>
### Pair identity, order and lifecycle

Monitoring scans deduplicate all native fixtures contributing the same collider RID/remote/local logical slot pair. Multiple manifold points or compound convex fixtures do not produce duplicate logical entries. Each nonzero step commits both pair and deduplicated scene-object snapshots before callbacks. Unchanged pairs do not replay. First entry of an object precedes its first shape entry. Ordinary last-pair departure emits object exit before that pair exit; scene-tree departure emits object exit then all retained shape exits without another step. Exits preserve the sampled indices, which may be stale after structural slot reindexing.

Server-only body/Area colliders produce shape payloads with their RID and null scene object. They do not appear in GetOverlappingBodies/Areas object arrays. Server-created Areas default non-monitorable; PhysicsServer.AreaSetMonitorable enables detection. Scene object values preserve the typed declaring role, including Entity for bodies and Area for Areas. Virtual tile body payloads remain Partial until typed tile-body integration; the Area/Area branch is executable for current scene/server Area identities.

A failed callback does not replay committed transitions and later queued object/shape transitions still run. Changes to Monitoring or Monitorable during an in/out callback throw InvalidOperationException before mutation; equal assignments are harmless. Apply changed policies after the callback returns. Body/Area removal clears retained pairs and queues exits; server collider free completes registry cleanup even if a departure handler throws. Steady and pre-warmed entry/exit callbacks allocate zero managed bytes in ShapePairEventTests on Linux/.NET 10.

Partial handler snippet:

```csharp
area.BodyShapeEntered += (rid, body, bodyShapeIndex, localShapeIndex) =>
{
    uint ownerID = area.ShapeFindOwner(localShapeIndex);
    // body is null for a server-only collider; rid still identifies it.
};
```

[ShapePairEventTests](../../tests/Electron2D.Tests/ShapePairEventTests.cs) checks multi-slot/body/Area pairs, compound deduplication, object/pair ordering, masks and monitorability, server nullable identities, removals, callback locks/failures and warmed steady/active transition allocation. See [ADR 0055](../decisions/physics-monitoring.md#adr-0055).

## Disabled processing and physics

Inherited `DisableMode.Remove` detaches the sensor and its fields while retaining RID and owners. Local overlaps clear and peer Areas receive object/shape departure events. MakeStatic and KeepActive both keep ordinary sensing and fields active while Node process callbacks remain disabled. Re-enabling a removed sensor restores membership synchronously; overlap entry is evaluated at the next physics scan. See [CollisionObject.DisableMode](CollisionObject.md#disablemode) and [ADR 0072](../decisions/physics.md#adr-0072).

## External server monitor observers

PhysicsServer.AreaSetMonitorCallback/AreaSetAreaMonitorCallback accept this scene RID and add independent logical-pair observers with status/RID/InstanceID/other-shape/local-shape payload. Scene-owned arrays and typed events remain mandatory; raw observers can run even while scene Monitoring is disabled. Replacing either callback resets its raw pair history and replays current entries at the next scan without resetting scene snapshots. Filter/pose/monitorable server setters project the same scene state. Own callback configuration changes reject; peer lifecycle changes are drained after committed snapshots. [PhysicsAreaMonitorTests](../../tests/Electron2D.Tests/PhysicsAreaMonitorTests.cs) and [ADR 0077](../decisions/physics-monitoring.md#adr-0077) cover this typed ownership adaptation and verification limits.

## Shared server field configuration

PhysicsServer's ten concrete Area field getter/setter pairs accept this node's RID and use the same descriptor state. AreaSetGravityVector updates both GravityDirection and GravityPointCenter's shared value. Direct or server writes retain the scene defaults, packing and input validation; attached field writes reject solver-owned pose synchronization and preserve scene capture guards. Server-created Areas join the same current-overlap priority reducer and can affect this world's RigidBody/CharacterBody and explicit server bodies even without monitor callbacks. [PhysicsServer field reference](PhysicsServer.md#typed-area-fields-and-space-defaults), [PhysicsServerAreaFieldTests](../../tests/Electron2D.Tests/PhysicsServerAreaFieldTests.cs) and [ADR 0056](../decisions/physics-fields.md#adr-0056) define defaults, lifecycle and verification limits.

[Indexed PhysicsServer geometry](PhysicsServer.md#shape-slots) now shares the scene/server logical slots, effective local poses and native fixture/query path under [ADR 0088](../decisions/physics-shape-slots.md#adr-0088). Raw replacement/pose/disabled/one-way edits do not rewrite child configuration; group/child edits reclaim corresponding overrides. Shape free/replacement follows shared RID/view ownership and related-world phase guards. Effective poses also feed mass geometry. [PhysicsServerShapeSlotTests](../../tests/Electron2D.Tests/PhysicsServerShapeSlotTests.cs) verifies real geometry, body/Area lifetime and one-way contacts/motion on Linux/.NET 10.

Scene/server membership and monitor configuration prepare object/pair/change storage from the existing fixture inventory. Body creation also prepares dormant solver-set identities and buffers, so first sleep after a long run can reuse them. Prepared graph storage is four contacts per rounded body-capacity slot; unusual denser topologies can exceed that tested budget and are outside the sandbox measurement. Cache capacity grows during explicit construction/configuration and remains owned by the world until disposal.

After an internally enabled GPU backend fails, body/area/joint teardown preserves
owner/stepping guards but skips individual raw graph destruction and partial-motion
capture. Managed bindings/views are released; the failed space reclaims raw storage
in bulk. Queries and further simulation remain rejected. See the
[GPU island graph failure contract](../components/gpu-physics.md#gpu-contact-driven-island-graph-2026-10-08).
