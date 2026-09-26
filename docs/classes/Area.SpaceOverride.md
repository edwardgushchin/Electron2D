# Area.SpaceOverride

Last updated: 2026-09-26

**Owner:** [Area](Area.md) · **Source:** [Area.Fields.cs](../../src/Scene/2D/Area.Fields.cs)

Controls each gravity or damping field independently while overlapping areas are processed from greatest priority to least.

| Value | Integer | Field effect |
| --- | ---: | --- |
| `Disabled` | 0 | This area contributes nothing. |
| `Combine` | 1 | Adds this area; lower-priority areas and the world default may still contribute. |
| `CombineReplace` | 2 | Adds this area and stops lower-priority areas and the world default. |
| `Replace` | 3 | Replaces earlier contributions and stops lower-priority areas and the world default. |
| `ReplaceCombine` | 4 | Replaces earlier contributions, then continues to lower-priority areas and the world default. |

The three override properties default to `Disabled`. Invalid enum values throw before changing state. [PhysicsAreaFieldTests](../../tests/Electron2D.Tests/PhysicsAreaFieldTests.cs) checks numeric identities and each reduction mode on current RigidBody simulation. CharacterBody now reads the selected gravity field through inherited GetGravity; its desired Velocity remains caller-owned.

PhysicsServer Area field methods reuse this enum for all three channels across scene/server Areas and space defaults. No second enum or numeric parameter dispatcher is exposed. Space default modes are stored but its fallback is unconditional for unstopped channels; bounded Area modes select actual contributions. [PhysicsServerAreaFieldTests](../../tests/Electron2D.Tests/PhysicsServerAreaFieldTests.cs) verifies all five modes in mixed scene/server order under [ADR 0056](../decisions/physics-fields.md#adr-0056).
