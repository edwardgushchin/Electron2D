# PhysicsMass

Last updated: 2026-10-08

**Declaration:** `internal static class PhysicsMass`

**Source:** [PhysicsMass.cs](../../src/Servers/Physics/PhysicsMass.cs)
**Component:** [Physics bodies](../components/physics-bodies.md#shared-body-mass-profile)

## Responsibility

Resolve the configured kilograms, local center and polar inertia under
[ADR 0073](../decisions/physics-mass.md#adr-0073). Body origins and velocities are
not mass-profile outputs. CPU attachments apply the result through their existing
backend; independent GPU bodies consume the same authoring geometry policy without
creating a CPU world. Backend proxy/mass types remain inside this helper and the
CPU adapters; GPU consumers receive engine-valued Properties.

| Member | Contract |
| --- | --- |
| `InertiaScale` | Squared scene-to-meter conversion used only at CPU backend boundaries. |
| `Validate(mass, inertia, center)` | Reject nonpositive/nonfinite mass, negative/nonfinite inertia, nonrepresentable reciprocal inertia and nonfinite center extents before mutation. Zero inertia selects automatic geometry; nullable center selects automatic centroid. |
| `AppendGeometry(shape, pose, proxies)` | Append non-disposed, non-ray local geometry using its authored unit rotation and CPU primitive/decomposition/tolerance policy. The caller owns reusable proxy storage. |
| `Calculate(proxies, mass, inertia, customCenter)` | Normalize solid area to configured mass, or use length-weighted thin rods when no solid area exists. Apply the parallel-axis theorem at the selected center; explicit inertia remains independent of mass/shape changes. Empty/point-only automatic geometry has zero center and inertia. |
| `Apply(body, shapes, mass, inertia, center, proxies)` | Resolve current CPU fixtures, validate center-relative extents and apply mass data; mask inverse values for nondynamic roles, retain the resolved profile and complete deferred fixture mass work. |
| `Properties(Mass, Inertia, Center)` | Immutable resolved engine values in kilograms, kg·scene-unit² and body-local scene units. |
| `Geometry.Clear()` / `Append(shape, pose, sensor)` | Reuse one authoring scratch object, retaining all primitive extents and only mass-contributing solid/rod proxies. Sensors and rays contribute no mass. Borrowed resources are not owned. |
| `Geometry.Calculate(mass, inertia, center)` | Return Properties after the common normalization and extent validation; explicit authored inertia remains exact. No body/world, live pose/velocity mirror or GPU readback is created. |

Geometry scratch is caller-owned and reused sequentially on its physics owner.
Structural growth may allocate. The configured/resolved values depend on authored
geometry, not simulation motion. CPU storage of these values is not a second
solver state. Circle/capsule/segment/rectangle/convex/concave/ray profiles use the
existing compiled primitive policy, including the half-unit segment/capsule cutoff.

## Verification

PhysicsMassProfileTests covers the public scene/server profile, rollback, role
restoration, notifications, packing and warmed allocation. Its rotated asymmetric
polygon/hollow-line checks compare analytic centroids and polar moments before and
after CPU attachment. GPUPhysicsMassStoreTests compares every current shape family
against those public CPU getters, exercises custom centers, actual device force,
contact and joint responses, edit ordering, disposal/revision recovery and warmed
profile edits. See the [resident mass report](../components/gpu-resident-mass.md)
for numerical bounds, storage costs and the remaining backend integration boundary.
