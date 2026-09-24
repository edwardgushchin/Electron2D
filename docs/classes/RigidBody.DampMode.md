# RigidBody.DampMode

Last updated: 2026-09-24

**Owner:** [RigidBody](RigidBody.md) · **Source:** [RigidBody.cs](../../src/Scene/2D/RigidBody.cs)

Controls how body damping affects the rate already resolved from areas and the world default.

| Value | Integer | Body effect |
| --- | ---: | --- |
| `Combine` | 0 | Adds the body's damping to the resolved area/world rate. |
| `Replace` | 1 | Uses only the body's damping rate. |

Both body mode properties default to `Combine`. Invalid enum values throw before changing state. [PhysicsAreaFieldTests](../../tests/Electron2D.Tests/PhysicsAreaFieldTests.cs) checks both modes for linear and angular motion.
