# PhysicsSeparationRay

Last updated: 2026-10-20

**Declaration:** `internal static class PhysicsSeparationRay` · **Source:**
[PhysicsSeparationRay.cs](../../src/Servers/Physics/PhysicsSeparationRay.cs) ·
**Component:** [Physics queries](../components/physics-queries.md)

This CPU adapter combines directed separation-ray contacts, body-local authored
ray data, native fixture/proxy geometry and current counterpart containment. Inputs and outputs use
CPU solver units; adapters convert public scene values at their boundary. The
helper owns no world, body or native allocation. Callers qualify resource lifetime,
world access and numerical borrowing before invoking it.

`SolverContact` selects the sole directed-ray side, handles unbounded planes and
compound outer contours, then returns a native manifold with origin-relative body
anchors. `PairContact` selects ordinary, ray or reverse-query composition.
`WorldProxy` transforms local endpoints into a two-point zero-radius proxy.
`Contains` tests the current proxy region. `Contact` evaluates the directed segment,
current counterpart pose, positive axial extension, margin and slope policy.
`PairContact` ignores requested ordinary counterpart displacement when the ray is
stationary; the former private swept-region helpers are removed. See
[special query displacement](../components/physics-queries.md#special-query-displacement).

Unrounded polygon origin containment uses
[PhysicsShapeBackend.ContainsPoint](PhysicsShapeBackend.md) with stable native
fixed-array storage and outward half-planes. Rounded geometry retains its distance
path, while single/two-point proxies retain circle/capsule containment. This
prevents an exterior ray origin being discarded as inside merely because a
zero-radius polygon distance converged to zero. See
[point-containment checks](../components/physics-point-containment.md) and
[ADR 0068](../decisions/physics.md#adr-0068) for scope and remaining gates.
