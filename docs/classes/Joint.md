# Joint

Last updated: 2026-10-10

[Shared joint conformance](../components/physics-joint-policies.md#public-cpu-gpu-conformance)
exercises this family through explicitly selected CPU and independent GPU worlds.
The report records force/motion regressions, numerical tolerances and failure limits.

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

`Joint` is an engine-owned abstract base; applications instantiate [PinJoint](PinJoint.md), [GrooveJoint](GrooveJoint.md) or [DampedSpringJoint](DampedSpringJoint.md). `GetRID()` exposes the stable scene-owned physics identity; Bias, correction speed and force budget are shared with PhysicsServer and stored in PackedScene. See [joint policies](../components/physics-joint-policies.md) for equations and backend scope.

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
| `public float Bias { get; set; }` | `0` | Fraction of positional error per substep; zero inherits the space default. |
| `public float MaxBias { get; set; }` | `float.MaxValue` | Vector linear/angular correction-speed cap. |
| `public float MaxForce { get; set; }` | `float.MaxValue` | Shared linear and separate pure-angular force/impulse budget. |
| `public bool DisableCollision { get; set; }` | `true` | Suppress mutual body contacts while joined. |
| `public RID GetRID()` | — | Stable scene-owned identity until disposal; FreeRID rejects it. |
| `protected override void OnNotification(int what)` | — | Record local anchor/guide diagnostics before user draw callbacks. |
| `protected override void ValidateDisposal()` | — | Check scene/dependent world ownership before beginning disposal. |
| `public override string[] GetConfigurationWarnings()` | — | Return current missing/invalid endpoint warnings. |
| `protected override void OnEnterTree()` / `OnReady()` / `OnExitTree()` | — | Register, configure and release the scene constraint. |
| `protected override void Dispose(bool disposing)` | — | Release the constraint before base disposal. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | — | Store path and collision settings in PackedScene. |

## Property descriptions

### `NodeA` and `NodeB`

An empty string leaves the joint unconfigured; `null` rejects before mutation. Paths use [Node](Node.md) resolution, including relative `..` and active absolute paths. Both endpoints must be distinct PhysicsBody nodes in the joint's scene world. Changes take effect before the next nonzero fixed step. A later body entry can satisfy a previously unresolved path. These properties are stored by PackedScene and retain their text through detach/reentry.

### `Bias`, `MaxBias`, `MaxForce`

Bias is finite in [0,1]. Zero inherits the space default, initially the captured project value 0.2. A nonzero value requests that fraction of positional error per substep. MaxBias is finite nonnegative speed: the linear vector uses scene units/s, angular stops use rad/s, additionally bounded by the current world guard of 200. Zero preserves velocity constraints but disables positional recovery. Springs have no positional recovery rows, so these two values do not alter their force-law coefficients.

MaxForce is finite nonnegative, with float.MaxValue meaning unlimited. Each substep permits MaxForce times its duration: a shared linear vector budget in kg·scene-unit/s and a separate pure-angular budget in kg·scene-unit²/s. Springs cap their combined elastic/drag impulse. The pin motor retains its separate N·m cap. See [the solver policy](../components/physics-joint-policies.md) for coupled rows and the distinct CPU/GPU correction methods.

All three values share the scene's server RID, survive packing and general joint clear/replacement, and preserve sampled anchors. Valid live edits wake connected bodies and clear old impulses. Invalid scalar values, off-owner mutation and solver-owned access reject before configuration changes.

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

### `OnNotification(int what)` and diagnostic geometry

NotificationDraw records a pin cross or the groove/spring anchor extent when
SceneTree.DebugCollisionsHint is enabled. Groove also shows InitialOffset; geometry
edits invalidate recording. Markers follow local transforms and ordinary visibility,
clipping and modulation. They describe authored local anchors, not solved impulses.
The [shared diagnostic component](../components/physics-debug.md) verifies ordering,
zero warmed allocation and real pixels for CPU/GPU physics and both renderers.
