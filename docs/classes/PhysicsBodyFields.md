# PhysicsBodyFields

Last updated: 2026-10-11

**Declaration:** `public readonly record struct PhysicsBodyFields` · **Source:** [PhysicsBodyFields.cs](../../src/Servers/Physics/PhysicsBodyFields.cs) · **Component:** [Selected observation and collision policy](../components/physics-backends.md#selected-observation-and-collision-policy)

## Description and values

Immutable complete resolved field values for one current body observer. It owns no body, world, resource, runtime or native handles. PhysicsBodyRuntime samples current common values; the selected collider returns the actual observer values. CPU preserves its completed common field reduction, while GPU reads its qualified resident cache.

| Value | Contract |
| --- | --- |
| `Vector2 Gravity` | Finite resolved acceleration in engine coordinates and scene units. |
| `float LinearDamp`, `float AngularDamp` | Finite signed resolved damping; existing negative-coefficient behavior is preserved. |
| `bool Initialized` | Whether those resolved fields have been initialized by the implementation. |

Internal flow: `Backend.ReadResolvedFields(current)` returns one complete value to `PhysicsBodyRuntime.PublishFields`. The runtime validates all numeric fields before assignment. Failed publication leaves every common field and attachment unchanged. This public observation value is sampled through guarded implementation hooks.

## Lifecycle and verification

Publication borrows body/world lifetime, rejects recursive field access and releases borrowing in finally. Public access requires a queryable world; internal completed-step publication instead requires the owner thread and solved interval, while ordinary queries remain closed. Scene/raw direct-state consumers, callbacks and portable capture share this route.

[Selected-view tests](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.SelectedViews.cs) delegate real CPU/GPU fields, inject nonfinite results and check atomicity, recursive/release/membership guards and view recovery. Existing field/checkpoint/snapshot suites preserve numeric and allocation checks. Public backend registration, native allocator and foreign-platform acceptance remain open.

This immutable value is now public and crosses the [registered implementation boundary](../components/physics-registered-adapter.md); complete sampled fields retain the contracts above. No backend storage or native handles are exposed.
