# PhysicsColliderShape

Last updated: 2026-10-19

**Declaration:** `public readonly record struct PhysicsColliderShape`
**Source:** [PhysicsColliderShape.cs](../../src/Servers/Physics/PhysicsColliderShape.cs)
**Component:** [Physics backends](../components/physics-backends.md#shared-collider-shape-inputs)

One borrowed authored slot: Shape resource, local Transform, logical Index,
Enabled state and optional body-local OneWay data. All slots, including disabled
ones, remain represented. This value contains no scene/server slot type or native
handle. It is public immutable implementation transport.

PhysicsColliderBackend prepares these values in retained scratch, preserves source
validation order, rejects recursive borrowing and clears references in finally.
Both concrete owners receive the same span and compile only enabled live shapes.
The shape-input fixture checks actual CPU/GPU bodies/Areas, public query indices,
one-way rotation and recovery after recursive preparation failure.

This immutable value is now public and crosses the [registered implementation boundary](../components/physics-registered-adapter.md); complete sampled fields retain the contracts above. No backend storage or native handles are exposed.

## Pending server geometry

Enabled describes effective numerical participation. A disabled, disposed or unconfigured logical slot supplies no active geometry. The slot still retains its authored index and resource identity; first successful server-data configuration makes ordinary enabled slots active.

The [custom bias accessor audit](../components/physics-contact-policy.md#custom-bias-accessor-audit)
verifies both implementation capabilities through the shared typed Shape property,
including numerical-lane reads, borrower write guards, supplier/disposal/observer
lifecycle and identity-preserving live correction. Fresh serial/separate public
profiles preserve exact zero managed allocation in complete measured intervals;
full numerical-extension/native/platform/network/performance acceptance is separate.
