# MultiMesh.PhysicsInterpolationQuality

Last updated: 2026-10-03

**Namespace:** `Electron2D` · **Declaration:** `public enum Electron2D.MultiMesh.PhysicsInterpolationQuality` · **Source:** [MultiMesh.cs](../../src/Scene/Resources/MultiMesh.cs).

## Description

Shared resource/server choice for fixed 2D instance bases. Fast blends stored components. High decomposes finite nonsingular bases with matching determinant sign into rotation/scale/skew, using component blending for incompatible or unrepresentable decomposition. Translation, instance color and raw custom data always blend components. Identical snapshots and reset retain exact current values. Default is Fast; undefined values reject.

## Example

```csharp
instances.PhysicsInterpolationQualityMode = MultiMesh.PhysicsInterpolationQuality.High; // Existing MultiMesh.
```

## Enumeration values

| Complete signature | Contract |
| --- | --- |
| `public const Electron2D.MultiMesh.PhysicsInterpolationQuality Fast = 0` | Interpolate each basis and translation component linearly. |
| `public const Electron2D.MultiMesh.PhysicsInterpolationQuality High = 1` | Interpolate rotation, scale and skew for nonsingular compatible bases; otherwise interpolate components. |

## Enumeration values descriptions

<a id="member-c27e8902213b"></a>
### Fast

`public const Electron2D.MultiMesh.PhysicsInterpolationQuality Fast = 0`

Interpolate each basis and translation component linearly.

<a id="member-e7205ce8b2da"></a>
### High

`public const Electron2D.MultiMesh.PhysicsInterpolationQuality High = 1`

Interpolate rotation, scale and skew for nonsingular compatible bases; otherwise interpolate components.

## Dependencies, errors and verification

[ADR 0092](../decisions/mesh.md#adr-0092) and [mesh component](../components/meshes.md#repeated-instance-resources) define ownership, typed bounds, callbacks and backend limits. MultiMeshTests checks resource/scene failures and warmed replay; MultiMeshRenderingTests checks native pixels, actual intermediate poses, owned identities and 256-instance active/idle frames. Native allocator totals, structural allocation, dense-scene cost, other targets and owner acceptance remain unverified. Scene-file persistence remains a separate dependency.
