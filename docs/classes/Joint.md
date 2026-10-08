# Joint

Last updated: 2026-10-08

**Inherits:** [Entity](Entity.md), CanvasItem, Node, ElectronObject
**Inherited By:** [PinJoint](PinJoint.md), [GrooveJoint](GrooveJoint.md), [DampedSpringJoint](DampedSpringJoint.md)

- **Source:** [Joint.cs](../../src/Scene/2D/Joint.cs)
- **Declaration:** `public abstract class Joint : Entity`
- **Component:** [Physics joints](../components/physics-joints.md)

## Physical skeletal integration

PhysicalBone can configure an authored direct child joint to its physical parent/body and align its origin. The existing connection-time anchor contract remains unchanged; moving the joint alone does not rebuild it.

## Description

Scene joint nodes and the shared PhysicsJointRuntime retain paths, engine-valued
local frames, settings and common lifetime policy. PhysicsJointBackend owns the
current solver handles and operations. A scene node tests attachment status without
depending on a backend ID; public RID identity remains stable.

The base spatial role for constraints between two distinct [PhysicsBody](PhysicsBody.md) nodes. `NodeA` and `NodeB` use the existing string node-path syntax and resolve from the joint in its scene tree. The joint stores configuration while detached. After both endpoints belong to the same active physics world, its concrete subclass creates a solver constraint. Invalid paths, non-body nodes, duplicate endpoints or bodies outside the same world leave it unconfigured and produce warnings. It reconnects after a body reenters, and the old constraint is removed before a body or the joint exits. The global anchor is sampled when the connection is made; moving the joint node alone does not retune an existing constraint. The node itself draws no geometry.

`Joint` is an engine-owned abstract base; applications instantiate [PinJoint](PinJoint.md), [GrooveJoint](GrooveJoint.md) or [DampedSpringJoint](DampedSpringJoint.md). `GetRID()` exposes the stable scene-owned physics identity; per-joint positional bias is not yet exposed. See [coverage](../coverage/classes/Joint2D.md) for exact dependency triggers.

## Example

```csharp
var pin = new PinJoint { NodeA = "../Base", NodeB = "../Door", DisableCollision = true };
root.AddChild(pin); // Base and Door are PhysicsBody siblings under root.
```

The snippet assumes `root` is a Node, the two named bodies are or will be its children, and a SceneTree will advance fixed physics frames.

## API summary

| Member | Default | Contract |
| --- | --- | --- |
| `private protected Joint(PhysicsServer.JointType type)` | — | Creates a detached base for engine-owned concrete joints. |
| `public string NodeA { get; set; }` | `""` | Path from this node to the first body. |
| `public string NodeB { get; set; }` | `""` | Path from this node to the second body. |
| `public bool DisableCollision { get; set; }` | `true` | Suppress mutual body contacts while joined. |
| `public RID GetRID()` | — | Stable scene-owned identity until disposal; FreeRID rejects it. |
| `protected override void ValidateDisposal()` | — | Check scene/dependent world ownership before beginning disposal. |
| `public override string[] GetConfigurationWarnings()` | — | Return current missing/invalid endpoint warnings. |
| `protected override void OnEnterTree()` / `OnReady()` / `OnExitTree()` | — | Register, configure and release the scene constraint. |
| `protected override void Dispose(bool disposing)` | — | Release the constraint before base disposal. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | — | Store path and collision settings in PackedScene. |

## Property descriptions

### `NodeA` and `NodeB`

An empty string leaves the joint unconfigured; `null` rejects before mutation. Paths use [Node](Node.md) resolution, including relative `..` and active absolute paths. Both endpoints must be distinct PhysicsBody nodes in the joint's scene world. Changes take effect before the next nonzero fixed step. A later body entry can satisfy a previously unresolved path. These properties are stored by PackedScene and retain their text through detach/reentry.

### `DisableCollision`

True suppresses physical contacts between the connected pair while the constraint exists. A live change preserves local anchors, updates native contact policy and refreshes both bodies' fixtures so already-overlapping shapes adopt the new policy at the next step. Active disabled joints also contribute to body motion tests and exception snapshots. Contributions are independent and deduplicated; other joints and explicit body exceptions can retain suppression.

## Method and lifecycle descriptions

### `GetRID()`

Returns a nonempty opaque RID stable from construction across path rebuilds, clear and tree reentry. It becomes stale on node disposal. PhysicsServer can read/change shared concrete settings through it; FreeRID cannot release the node-owned identity. Raw same-role make/clear overrides scene geometry until a scene path/geometry/name edit or reentry. A different concrete role or active foreign world rejects. See [server joint methods](PhysicsServer.md#joints).

### `ValidateDisposal()`

Checks attached scene and all related active world ownership/phases before the node begins disposal. An off-owner or in-step rejection preserves a live node and its RID.

### `GetConfigurationWarnings()`

Returns a caller-owned array, including base warnings. While attached, it reports missing/non-body endpoints, a duplicate body, or endpoints outside the same active world. Detached joints return only base warnings. It does not force a solver step or cache a warning snapshot.

The scene owner thread owns attached mutation and queries. Property writes reject during a solver step. Joint creation needs finite unit-scale, zero-skew global geometry; invalid geometry fails the frame and can be corrected before the next one. A completed joint keeps both body-local anchors until a path change or tree reentry rebuilds it. Fixed-step callbacks run before joint preparation, so a path edit in a callback affects that step. Native allocation, non-Linux platforms and visual acceptance are unverified.

## Verification and decisions

[PinJointTests](../../tests/Electron2D.Tests/PinJointTests.cs), [GrooveJointTests](../../tests/Electron2D.Tests/GrooveJointTests.cs) and [DampedSpringJointTests](../../tests/Electron2D.Tests/DampedSpringJointTests.cs) cover three concrete joint roles, path changes, body exit/reentry, active collision changes, packing, thread/phase rejection and invalid-geometry recovery. [The physics joint decisions](../decisions/physics-joints.md) define those roles.

[PhysicsServerJointTests](../../tests/Electron2D.Tests/PhysicsServerJointTests.cs) verifies RID projection, server overrides, shared settings and phase/lifetime rollback under [ADR 0087](../decisions/physics-joints.md#adr-0087).

After an internally enabled GPU backend fails, body/area/joint teardown preserves
owner/stepping guards but skips individual raw graph destruction and partial-motion
capture. Managed bindings/views are released; the failed space reclaims raw storage
in bulk. Queries and further simulation remain rejected. See the
[GPU island graph failure contract](../components/gpu-physics.md#gpu-contact-driven-island-graph-2026-10-08).
