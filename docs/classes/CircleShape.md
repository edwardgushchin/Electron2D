# CircleShape

Last updated: 2026-10-05

**Inherits:** [Shape](Shape.md), [Resource](Resource.md)

- **Source:** [Shape.cs](../../src/Scene/Resources/Shape.cs)
- **Declaration:** `public sealed class CircleShape : Shape`
- **Component:** [Collision shapes](../components/physics-shapes.md)

## Description

An independently owned circular collision resource. A CollisionShape node beneath a body or area borrows it; changing `Radius` publishes `Changed` and rebuilds every attached fixture before its next physics step. A throwing earlier subscriber does not prevent owners from detecting the changed geometry at that step. The managed Resource copying contract creates an independent circle and does not copy path identity.

## API summary

| Member | Contract |
| --- | --- |
| `public CircleShape()` | Creates a circle of radius 10 scene units. |
| `public float Radius { get; set; }` | Positive finite radius; default 10. |
| `public override Rect2 GetRect()` | Returns `(-Radius, -Radius, 2 × Radius, 2 × Radius)`. |
| `protected override Resource CreateDuplicateInstance()` | Creates a new exact-type resource for Resource copying. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | Copies the stored radius without publishing intermediate state. |

## Member descriptions

<a id="radius"></a>
### `Radius`

Zero, negative, nonfinite and values above `float.MaxValue / 2` throw `ArgumentOutOfRangeException` before mutation; the upper bound keeps the local diameter representable. A successful assignment commits the new value and synchronously emits `Changed`, including an equal value. The shape's backend radius uses the fixed scene-unit conversion but no native type appears publicly.

<a id="getrect"></a>
### `GetRect()`

Returns local bounds in scene units as a copied Rect2. It remains usable without a SceneTree or renderer and rejects access after disposal.

## Verification and limits

[PhysicsBodyTests](../../tests/Electron2D.Tests/PhysicsBodyTests.cs) covers defaults, bounds, invalid rollback, independent duplication, a live radius change and contact response on a static floor. Inherited drawing and custom solver-bias members retain separate coverage prerequisites.

Direct [shape queries](PhysicsDirectSpaceState.md) use the current circle radius for overlap, sweep and contact tests; [PhysicsShapeQueryTests](../../tests/Electron2D.Tests/PhysicsShapeQueryTests.cs) verifies this geometry family.

Inherited [Shape collision methods](Shape.md#collide) now test posed resources and independently swept regions without a SceneTree. ShapeCollisionTests verifies this family under [ADR 0069](../decisions/physics.md#adr-0069), including caller/other boundary-point ordering, lifetime and the sixteen-pair cap.

## Typed file integration

See [resource-file contracts](../components/resource-files.md) for registered typed schemas, cache/UID resolution, file-root and scene-instance ownership, public extension hooks and exercised verification. File operations allocate outside frame processing. UID paths resolve through the permanent catalog before directory-backed path resolution; unknown UIDs fail explicitly. The archive profile does not add an editor, arbitrary import/remap rules or every resource schema.

## File integration API additions

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Returns the typed properties exposed to tooling before validation. |

## Method Descriptions

<a id="member-a980710500c7"></a>
### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

Overrides append or replace descriptors; they must not yield null entries.

Returns: The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

Inherited [Shape.CustomSolverBias](Shape.md#customsolverbias) is stored and copied
alongside geometry. CopyCustomStateTo calls the Shape base and publishes the changed
geometry revision, including in-place copying into a resource with existing borrowers.
PhysicsContactPolicyTests checks duplication, in-place copy and .e2dres round trips.
