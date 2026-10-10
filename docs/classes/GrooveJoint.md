# GrooveJoint

Last updated: 2026-10-10

[Shared joint conformance](../components/physics-joint-policies.md#public-cpu-gpu-conformance)
exercises this family through explicitly selected CPU and independent GPU worlds.
The report records force/motion regressions, numerical tolerances and failure limits.

**Inherits:** [Joint](Joint.md), Entity, CanvasItem, Node, ElectronObject

- **Source:** [GrooveJoint.cs](../../src/Scene/2D/GrooveJoint.cs)
- **Declaration:** `public sealed class GrooveJoint : Joint`
- **Component:** [Physics joints](../components/physics-joints.md)

## Description

A finite straight guide fixed to body A. Its endpoints are this node's global origin and its local `(0, Length)` transformed into the world at attachment. Body B attaches at the node's transformed `(0, InitialOffset)` point and may slide between the two guide endpoints while rotating freely. The guide follows body A's subsequent translation and rotation because its axis and origin are stored in A's local frame. Scene units convert to meters by 0.01 inside the solver; the public properties remain in scene units.

The inherited [Joint](Joint.md) paths, collision policy, scene lifetime and owner-thread rules apply. A moved groove node alone does not retune an attached guide. Changing `Length` adjusts the active solver limits without replacing the body-local anchors; changing `InitialOffset` samples a new body-B anchor and rebuilds the constraint before the next fixed step.

## Example

```csharp
var guide = new GrooveJoint
{
    NodeA = "../Rail", NodeB = "../Slider",
    Length = 80, InitialOffset = 40
};
root.AddChild(guide);
```

This partial snippet assumes `root` owns two PhysicsBody siblings named `Rail` and `Slider`, and a SceneTree will run fixed physics frames. Set the guide's spatial transform before the bodies attach it.

## API summary

| Member | Default | Contract |
| --- | --- | --- |
| `public GrooveJoint()` | — | Creates an unconnected guide. |
| [`public float Length { get; set; }`](#length) | `50` | Signed distance from the guide origin to its far endpoint, in scene units. |
| [`public float InitialOffset { get; set; }`](#initialoffset) | `25` | Local Y position used to sample body B's anchor, in scene units. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | — | Stores guide dimensions in PackedScene. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | — | Recreates the exact GrooveJoint role. |

## Property descriptions

<a id="length"></a>
### `Length`

The guide extends from local Y zero to `Length`. A negative value reverses its direction; zero makes a point constraint while leaving relative rotation free. Both limits update immediately on an attached joint, retaining the existing local anchors. Nonfinite values and magnitudes above ten million scene units reject before mutation, matching the backend's finite joint-extent range. The caller may move the slider along either sign of the axis, but the solver keeps its anchor between the endpoints.

<a id="initialoffset"></a>
### `InitialOffset`

At connection, the joint samples body B's local anchor from this node's transformed `(0, InitialOffset)` point. The value may lie outside the guide, within the same ten-million-unit solver bound; the solver then pulls that anchor toward the nearest endpoint. A changed attached value replaces the native constraint before the next nonzero fixed step. An invalid transform or unrepresentable transformed anchor fails that step while preserving the previous constraint, and can be corrected. Nonfinite and over-range assignments reject immediately.

## Lifecycle, verification and limits

Both properties and the inherited paths/collision policy persist through PackedScene. A later body entry, path edit or name change can connect an unresolved guide. Body departure destroys the constraint before the body backend handle. Configuration warnings come from Joint. Active mutation and reads use the SceneTree owner thread; a solver-step mutation rejects.

[GrooveJointTests](../../tests/Electron2D.Tests/GrooveJointTests.cs) verifies finite endpoints, free rotation, rotated/reversed/zero-length guides, live geometry, outside offsets, packing, body reentry, numeric/thread failure recovery and 64 warmed active solver frames with zero managed allocation on Linux/.NET 10. The wheel solver's allocation fix is tracked in [Box2D.NET#102](https://github.com/ikpil/Box2D.NET/pull/102). Native allocation, other platforms and owner visual acceptance remain unverified. The class draws its authored guide and offset under SceneTree.DebugCollisionsHint; see [coverage](../coverage/classes/GrooveJoint2D.md) and [ADR 0085](../decisions/physics-joints.md#adr-0085).

The inherited stable RID and shared server settings are described by [Joint.GetRID](Joint.md) and [PhysicsServer joint methods](PhysicsServer.md#joints), under [ADR 0087](../decisions/physics-joints.md#adr-0087).

[Physics canvas diagnostics](../components/physics-debug.md) describes this node's retained geometry, live redraw, ordinary canvas behavior and CPU/GPU/native checks.
