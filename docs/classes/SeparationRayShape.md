# SeparationRayShape

Last updated: 2026-10-05

**Inherits:** [Shape](Shape.md), [Resource](Resource.md)

- **Source:** [SeparationRayShape.cs](../../src/Scene/Resources/SeparationRayShape.cs)
- **Declaration:** `public sealed class SeparationRayShape : Shape`
- **Component:** [Collision shapes](../components/physics-shapes.md)

## Description

A caller-owned directed separation resource from local (0, 0) to (0, Length). A direct CollisionShape child rotates and offsets this ray in its body or Area. Body motion, direct shape queries and Area overlap scans use its directed surface contact; ray and point queries cannot intersect it. Edits reach scene and borrowed server fixtures before their next query or step, even if a user Changed subscriber throws. Attached queries require the physics-space owner thread.

The CPU backend registers a zero-density body fixture with a directed manifold callback; Areas retain sensor fixtures. RigidBody and raw server bodies receive ordinary material impulses, contact snapshots/events and sleep. Rays contribute no geometric mass or rod inertia; an explicitly configured body mass/inertia remains effective. [ADR 0068](../decisions/physics.md#adr-0068) owns this integration. Public independent-GPU binding remains open under ADR 0054.

## Example

Partial snippet; keep the resource alive while borrowed and call movement from the character's fixed callback:

```csharp
using var feet = new SeparationRayShape { Length = 24, SlideOnSlope = true };
var character = new CharacterBody();
character.AddChild(new CollisionShape { Shape = feet });
// Supply character.Velocity before calling character.MoveAndSlide().
```

## API summary

| Member | Contract |
| --- | --- |
| `public SeparationRayShape()` | Twenty-unit downward ray; slope sliding false. |
| `public float Length { get; set; }` | Finite nonnegative local scene-unit length within the backend squared-distance range. |
| `public bool SlideOnSlope { get; set; }` | Selects the hit surface normal instead of the opposite ray direction. |
| `public override Rect2 GetRect()` | Returns the padded local drawing envelope. |
| `protected override Resource CreateDuplicateInstance()` | Creates an independent ray for Resource duplication. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | Copies length and slope policy. |

## Property descriptions

<a id="length"></a>
### `Length`

Defaults to 20. Negative, nonfinite lengths and lengths whose backend squared distance overflows throw ArgumentOutOfRangeException before mutation. Zero contributes no contact. Unequal writes increment the geometry revision and emit Changed; equal writes are silent. A ray shorter than backend linear slop retains exact endpoints in special queries and overlap scans, although its bookkeeping fixture becomes a point.

<a id="slideonslope"></a>
### `SlideOnSlope`

Defaults to false: separation points opposite the rotated ray axis. True uses the surface normal. The corresponding collider point is the endpoint displaced by the axial penetration distance along that normal; it can differ from the literal intersection point. Unequal writes emit Changed and rebuild metadata; equal writes are silent.

## Method descriptions and lifecycle

<a id="getrect"></a>
### `GetRect()`

Returns `new Rect2(0, 0, 0, Length).Grow(MathF.Sqrt(0.5f) * 4f)`, including drawing padding rather than a tight zero-width segment. It needs no SceneTree. Disposal rejects access with ObjectDisposedException. Resource copy hooks preserve independent length/policy state; PackedScene retains the borrowed resource. Disposing it releases its resource-owned server RID and removes pending fixtures through the inherited lifetime contract.

## Motion, queries and limits

Contact requires a front-facing surface crossing. A ray starting inside filled geometry has no entry hit; ray-ray pairs never contact. Margin extends the endpoint along the axis. Ray motion extends it by the positive axial displacement; transverse movement does not create a swept solid segment. For an ordinary shape moving against a stationary ray, the union of initial/final native primitives and swept edges supplies directed contact, including rounded margins.

Recovery always includes rays. The motion phase includes sliding rays automatically and other rays only when PhysicsTestMotionParameters.CollideSeparationRay is true. CharacterBody floor snap explicitly sets that flag. A touching ray may move away. RID, shape indices, point velocity, masks and exclusions retain the shared contract. Complete convex targets reject origin containment and select exactly one first-entry partition, avoiding internal seam contacts and duplicate constraints. CastRay CCD validates the complete ray after reducing the sweep proxy to a support point; CastShape uses complete fixture trajectories.

[SeparationRayShapeTests](../../tests/Electron2D.Tests/SeparationRayShapeTests.cs) checks defaults, equal/invalid/disposed writes, bounds, copying, packing, every existing shape family, containment, rotation/offset, short/zero rays, both slope policies, forward/reverse sweeps, query exclusion, server creation, recovery, character sliding/snap, callback failure, Area sensing, zero inertia and existing query behavior. [SeparationRayDynamicsTests](../../tests/Electron2D.Tests/SeparationRayDynamicsTests.cs) adds ordinary/reversed dynamic contacts against six shape families, compound containment, slope normals, friction/restitution, coupled momentum and reported impulses, explicit mass/inertia, one-way filters, CCD, live edits, sleep and scene events. Sixty-four warmed body recovery queries, reverse casts/rest queries and active directed Area frames each allocate zero managed bytes on Linux/.NET 10. The active directed solver check warms 128 fixed steps, then measures 128 steps at 1/60 s with a supported awake ray and floor: zero all-thread managed bytes on CPU and the CPU-hosted GPU stage experiment. Native allocation, other platforms, large-world throughput and owner visual acceptance remain unverified.

Inherited [Shape collision methods](Shape.md#collide) now test posed resources and independently swept regions without a SceneTree. ShapeCollisionTests verifies this family under [ADR 0069](../decisions/physics.md#adr-0069), including caller/other boundary-point ordering, lifetime and the sixteen-pair cap.

WorldBoundaryShape takes precedence over ordinary ray surface-entry policy: a
nonzero ray inside the solid half-plane separates toward its free normal. Zero rays
still produce no contact. Shared boundary/ray query matrices verify both orders.

Inherited [Shape.CustomSolverBias](Shape.md#customsolverbias) is stored and copied
alongside geometry. CopyCustomStateTo calls the Shape base and publishes the changed
geometry revision, including in-place copying into a resource with existing borrowers.
PhysicsContactPolicyTests checks duplication, in-place copy and .e2dres round trips.

<a id="getpropertydescriptors"></a>
`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` combines
inherited resource/policy descriptors with this shape's authored geometry for storage.
The built-in resource file registry constructs this concrete shape on load.
