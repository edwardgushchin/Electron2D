# PinJoint

Last updated: 2026-09-30

**Inherits:** [Joint](Joint.md), Entity, CanvasItem, Node, ElectronObject

- **Source:** [PinJoint.cs](../../src/Scene/2D/PinJoint.cs)
- **Declaration:** `public sealed class PinJoint : Joint`
- **Component:** [Physics joints](../components/physics-joints.md)

## Physical skeletal integration

Authored PinJoint children of PhysicalBone use the existing native solver. Auto configuration places the anchor at the physical node origin; an offset shape center of mass permits an actual pendulum rotation.

## Description

A revolute constraint: two body-local anchor points meet at this node's global origin, while the bodies may rotate relative to one another. The angle is zero at the time of attachment, so limits and motor speed describe subsequent relative rotation. [Joint](Joint.md) owns path resolution, collision policy and backend lifetime. An empty body or joint geometry without two valid attached bodies creates no constraint. The pin's linear anchor is rigid; adjustable positional softness is not yet available.

## Example

```csharp
var hinge = new PinJoint
{
    NodeA = "../Frame", NodeB = "../Door",
    AngularLimitLower = -0.5f, AngularLimitUpper = 0.5f,
    AngularLimitEnabled = true,
    MotorTargetVelocity = 1.5f, MotorMaxTorque = 10f, MotorEnabled = true
};
root.AddChild(hinge);
```

The snippet assumes `root` has distinct PhysicsBody children named `Frame` and `Door`, and that a SceneTree advances physics frames. Place `hinge` at the desired global pivot before attachment.

## API summary

| Member | Default | Contract |
| --- | --- | --- |
| `public PinJoint()` | — | Detached, unconnected pin. |
| `public bool AngularLimitEnabled { get; set; }` | `false` | Apply the relative-angle interval. |
| `public float AngularLimitLower { get; set; }` | `0` | Lower angle in radians. |
| `public float AngularLimitUpper { get; set; }` | `0` | Upper angle in radians. |
| `public bool MotorEnabled { get; set; }` | `false` | Drive relative angular speed. |
| `public float MotorTargetVelocity { get; set; }` | `0` | Desired radians per second. |
| `public float MotorMaxTorque { get; set; }` | `10` | Motor torque cap in newton-meters. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | — | Store concrete settings in PackedScene. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | — | Restore the exact PinJoint role from PackedScene. |

## Property descriptions

### `AngularLimitEnabled`, `AngularLimitLower`, `AngularLimitUpper`

When enabled, the relative body angle remains between the stored lower and upper radians. Both bounds default to zero, which locks relative rotation if enabled. Disabled bounds can be prepared in either setter order. Enabling or editing active bounds requires finite, ordered values within the solver's ±0.99π range; rejection leaves prior state unchanged. An active pin updates the limit immediately; a detached pin stores it for attachment. The constraint is iterative, so very large motor torque can overpower a narrow limit during a step.

### `MotorEnabled`, `MotorTargetVelocity`, `MotorMaxTorque`

The motor drives body B relative to body A at the requested finite radians per second, subject to a finite nonnegative torque cap. Torque zero cannot drive. The default cap is 10 N·m; larger or heavier bodies may need a higher value, and callers should choose one compatible with any active angle limits. Changes update an active solver joint immediately. Nonfinite speed, nonfinite torque and negative torque reject before mutation.

## Lifecycle, verification and limits

All pin properties and inherited Joint paths/collision policy are stored in PackedScene. Joint entry after its body siblings, body reentry and path updates rebuild the backend constraint without replacing the public node. Moving the joint node after attachment does not change the connected body-local anchors. The scene tree owns cleanup. Attached writes require the owner thread and cannot run during the solver step.

[PinJointTests](../../tests/Electron2D.Tests/PinJointTests.cs) verifies a gravity pendulum, motor/limit response, live torque changes, connected-body contacts, path and body lifecycle, packing, invalid input and 64 warmed active fixed steps with zero managed allocation on Linux/.NET 10. Native allocation, other platforms and owner visual acceptance remain unverified. Positional bias and pin-anchor softness remain [exact coverage gaps](../coverage/classes/PinJoint2D.md) under [ADR 0084](../decisions/physics-joints.md#adr-0084).

The inherited stable RID and shared server settings are described by [Joint.GetRID](Joint.md) and [PhysicsServer joint methods](PhysicsServer.md#joints), under [ADR 0087](../decisions/physics-joints.md#adr-0087).
