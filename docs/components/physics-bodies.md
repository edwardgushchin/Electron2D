# Scene physics bodies component

Last updated: 2026-09-24

## Scope and owned types

[`CollisionObject`](../classes/CollisionObject.md) owns 32-bit layer/mask filtering. [`PhysicsBody`](../classes/PhysicsBody.md) owns direct CollisionShape children and backend body lifetime. [`StaticBody`](../classes/StaticBody.md) constrains movement; [`RigidBody`](../classes/RigidBody.md) responds to gravity, contacts, velocity and impulses. The hierarchy preserves the reference intermediate roles above Entity.

## Fixed-step flow

SceneTree creates its internal Box2D.NET world only when the first body enters. Scene-unit positions and velocities are converted to meters by 0.01; the world uses downward gravity 9.8 m/s². The existing physics callback lane runs first so force and velocity changes affect that step. The world advances with four Box2D substeps, then dynamic transforms and velocities synchronize back before timers, tweens and the interpolation end snapshot. Zero elapsed time does not advance the world.

One or more direct CollisionShape children provide independent circle/rectangle fixtures. Category/mask bits filter responses. Shape, disabled-state or filter edits rebuild fixtures before the next step; an inactive/foreign child contributes none. A failed transform validation aborts that step without destroying prior fixtures, and a corrected next step can proceed. Tree exit, re-entry and disposal release and rebuild the backend handles without exposing them to callers.

## Current contract and limits

RigidBody defaults to mass 1 kg, gravity scale 1, zero velocity/damping, sleeping allowed, unfrozen and rotation unlocked. It supports finite signed gravity scaling, positive finite mass, nonnegative damping, typed linear/angular velocity, sleep/freeze/rotation policy, and central force/impulse. StaticBody currently supplies a stationary collision base; conveyor velocities and material override are separate rows. Direct-space sweeps, areas, joints, contact events, physics material and public server/RID methods remain incomplete, with exact triggers in coverage.

Box2D.NET 3.1.654 source and the scoped hot-path patch record live in [`src/Vendor/Box2D.NET`](../../src/Vendor/Box2D.NET/VENDOR.md). The compiled public surface contains no Box2D type. [PhysicsBodyTests](../../tests/Electron2D.Tests/PhysicsBodyTests.cs) checks falling/contact/impulse/filter behavior, shape edits, lifecycle and 64 warmed frames each for resting, active-contact and freely moving bodies with zero managed allocations. Native allocator counts, other platforms and owner visual acceptance remain unverified.

## Decision

- [0054: First Box2D-backed scene-body profile](../decisions/physics.md#adr-0054)
