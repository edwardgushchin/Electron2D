# PhysicsColliderShape

Last updated: 2026-10-11

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
