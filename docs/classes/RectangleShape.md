# RectangleShape

Last updated: 2026-10-05

**Inherits:** [Shape](Shape.md), [Resource](Resource.md)

- **Source:** [Shape.cs](../../src/Scene/Resources/Shape.cs)
- **Declaration:** `public sealed class RectangleShape : Shape`
- **Component:** [Collision shapes](../components/physics-shapes.md)

## Description

An independently owned rectangular collision resource centered on its local origin. A CollisionShape node beneath a body or area borrows it; a size change invalidates attached fixtures for the next fixed step even when an earlier user `Changed` subscriber throws. Managed duplication owns independent dimensions and clears external Resource identity.

## API summary

| Member | Contract |
| --- | --- |
| `public RectangleShape()` | Creates a 20 × 20 scene-unit rectangle. |
| `public Vector2 Size { get; set; }` | Finite positive width and height; default (20, 20). |
| `public override Rect2 GetRect()` | Returns a centered local rectangle of the current size. |
| `protected override Resource CreateDuplicateInstance()` | Creates a new exact-type resource. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | Copies the stored dimensions without intermediate events. |

## Member descriptions

<a id="size"></a>
### `Size`

Either zero, negative or nonfinite component throws `ArgumentOutOfRangeException` before mutation. A successful assignment commits and emits `Changed`, including an equal value. The Box2D fixture uses half-extents after scene-unit conversion.

<a id="getrect"></a>
### `GetRect()`

Returns `(-Size.X/2, -Size.Y/2, Size.X, Size.Y)` in local scene units, independent of SceneTree membership.

## Verification and limits

[PhysicsBodyTests](../../tests/Electron2D.Tests/PhysicsBodyTests.cs) verifies default/invalid geometry, falling box versus floor, collision filtering and borrowed resource lifetime. Inherited drawing and custom solver-bias members retain separate coverage prerequisites.

Direct [shape queries](PhysicsDirectSpaceState.md) use the current filled rectangle and support swept overlap, contact pairs and rest normals; [PhysicsShapeQueryTests](../../tests/Electron2D.Tests/PhysicsShapeQueryTests.cs) checks these paths.

Inherited [Shape collision methods](Shape.md#collide) now test posed resources and independently swept regions without a SceneTree. ShapeCollisionTests verifies this family under [ADR 0069](../decisions/physics.md#adr-0069), including caller/other boundary-point ordering, lifetime and the sixteen-pair cap.

## Typed file integration

See [resource-file contracts](../components/resource-files.md) for registered typed schemas, cache/UID resolution, file-root and scene-instance ownership, public extension hooks and exercised verification. File operations allocate outside frame processing. UID paths resolve through the permanent catalog before directory-backed path resolution; unknown UIDs fail explicitly. The archive profile does not add an editor, arbitrary import/remap rules or every resource schema.

## File integration API additions

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Returns the typed properties exposed to tooling before validation. |

## Method Descriptions

<a id="member-67c78ccb4b9a"></a>
### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

Overrides append or replace descriptors; they must not yield null entries.

Returns: The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

Inherited [Shape.CustomSolverBias](Shape.md#customsolverbias) is stored and copied
alongside geometry. CopyCustomStateTo calls the Shape base and publishes the changed
geometry revision, including in-place copying into a resource with existing borrowers.
PhysicsContactPolicyTests checks duplication, in-place copy and .e2dres round trips.
