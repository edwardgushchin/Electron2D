# PhysicsServer.JointType

Last updated: 2026-10-04

**Inherits:** System.Enum · **Source:** [PhysicsServer.Joints.cs](../../src/Servers/Physics/PhysicsServer.Joints.cs)

**Declaration:** `public enum PhysicsServer.JointType`

## Description and example

Configured role returned by [PhysicsServer.JointGetType](PhysicsServer.md#jointgettype). Pending detached connections retain their concrete role; allocation, clear or endpoint free reports Empty. Values describe the shared scene/server connection and do not expose backend IDs.

```csharp
var joint = PhysicsServer.JointCreate();
PhysicsServer.JointType type = PhysicsServer.JointGetType(joint); // Empty
PhysicsServer.FreeRID(joint);
```

## Values

| Value | Numeric identity | Description |
| --- | --- | --- |
| `Pin` | `0` | Two anchors at one point, relative rotation, optional angular limits/motor. |
| `Groove` | `1` | Finite body-A guide with a freely rotating body-B anchor. |
| `DampedSpring` | `2` | Hooke force and axial drag between sampled anchors. |
| `Empty` | `3` | Allocated/cleared identity with no configured connection; this is an observable state. |

<a id="pin"></a>
**Pin:** Can attach a single body to the fixed world through an empty second RID.

<a id="groove"></a>
**Groove:** Requires two distinct live bodies and retains a finite local guide.

<a id="dampedspring"></a>
**DampedSpring:** Requires two distinct live bodies; no maximum stretch cap is implied.

<a id="empty"></a>
**Empty:** Retains the RID and collision policy; scalar access on a caller-owned Empty resource rejects. An Empty scene node retains its declared concrete settings.

## Verification and decision

[PhysicsServerJointTests](../../tests/Electron2D.Tests/PhysicsServerJointTests.cs) checks creation, replacement, clear, endpoint free and pending lifetime. [ADR 0087](../decisions/physics-joints.md#adr-0087) owns the typed numeric-state adaptation.
