# Area.SpaceOverride

Last updated: 2026-09-24

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
