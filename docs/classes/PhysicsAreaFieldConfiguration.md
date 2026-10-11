# PhysicsAreaFieldConfiguration

Last updated: 2026-10-11

**Declaration:** `public readonly record struct PhysicsAreaFieldConfiguration` · **Source:** [PhysicsAreaFieldConfiguration.cs](../../src/Servers/Physics/PhysicsAreaFieldConfiguration.cs) · **Component:** [Selected checkpoint ownership](../components/physics-backends.md#selected-checkpoint-ownership)

## Description and values

Complete immutable engine-unit authoring data sampled by `PhysicsAreaFields.Configuration`. It retains no Area, resource, world or solver identity. Common local replay configuration and portable schema hashing use the same values without GPU implementation types.

| Values | Contract |
| --- | --- |
| `Vector2 GravityVector`, `float Gravity`, `bool GravityPoint`, `float PointUnitDistance` | Authored direction or point center, signed strength, point selection and distance scaling. |
| `float LinearDamp`, `float AngularDamp` | Signed finite authored damping. |
| `Area.SpaceOverride GravityMode`, `LinearMode`, `AngularMode` | Existing typed field reduction policies. |
| `int Priority` | Integer Area priority; default-world authoring retains -1. |

Internal flow: `fields.Configuration` is stored in a replay configuration or passed to portable `AddFields`. Owners validate authoring and guard mutation before this sample. No unit conversion or normalization occurs. Portable hashing retains the exact previous field order and values; no wire version change is needed.

## Verification and limits

Shared checkpoint tests reject mismatched world/Area authoring before mutation. Portable snapshot tests exercise CPU/GPU schema agreement, all three joint roles, fields, contacts and silent restoration. This internal data type does not expose public backend registration or custom geometry. Native allocator and foreign-platform acceptance remain separate.

This immutable value is now public and crosses the [registered implementation boundary](../components/physics-registered-adapter.md); complete sampled fields retain the contracts above. No backend storage or native handles are exposed.

[Registered Area fields](../components/physics-registered-adapter.md#registered-area-fields) now exercise the complete typed profile/default shortcut through the selected hooks, including independent override/point/mask response and zero warmed edited full frames. Numerical-hook writes reject before mutation; authored reads and ordinary Area callback edits retain their guards and overlap history. Generic solver/extension acceptance remains open.
