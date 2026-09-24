# Scene physics bodies component

Last updated: 2026-09-24

## Scope and owned types

[`CollisionObject`](../classes/CollisionObject.md) owns 32-bit layer/mask filtering. [`PhysicsBody`](../classes/PhysicsBody.md) owns direct CollisionShape children and backend body lifetime. [`StaticBody`](../classes/StaticBody.md) constrains movement; [`RigidBody`](../classes/RigidBody.md) responds to gravity, contacts, velocity and impulses. Both concrete bodies borrow [`PhysicsMaterial`](../classes/PhysicsMaterial.md) to set surface friction and bounce. The hierarchy preserves the reference intermediate roles above Entity.

## Fixed-step flow

SceneTree creates its internal Box2D.NET world when the first body or area enters. Scene-unit positions and velocities are converted to meters by 0.01; typed project settings default to downward gravity 980 scene units/s² and world damping 0.1/1 per second. The existing physics callback lane runs first so force and velocity changes affect that step. Area gravity and damping fields resolve against current overlap before four backend substeps; solved dynamic transforms and velocities synchronize back before area overlap events, timers, tweens and interpolation end capture. Zero elapsed time does not advance the world.

One or more direct CollisionShape children provide independent circle/rectangle fixtures. Category/mask bits filter responses. Shape, disabled-state, filter or borrowed material edits rebuild fixtures before the next step; an inactive/foreign child contributes none. Each fixture receives the body's material settings, and the world mixes friction and bounce through the pinned surface rules without a low-speed bounce threshold. A failed transform validation aborts that step without destroying prior fixtures, and a corrected next step can proceed. Tree exit, re-entry and disposal release and rebuild the backend handles without exposing them to callers.

## Current contract and limits

RigidBody defaults to mass 1 kg, gravity scale 1, zero velocity/body damping, sleeping allowed, unfrozen and rotation unlocked. It supports finite signed gravity scaling and damping, positive finite mass, typed linear/angular velocity, sleep/freeze/rotation policy, two body damping modes, and central force/impulse. Area fields resolve each gravity/damping channel by priority; a changed field wakes an affected sleeping body. `PhysicsBody.GetGravity()` returns the last resolved scaled field for a dynamic body. StaticBody supplies a stationary collision base; conveyor velocities remain separate rows. Null material overrides use friction one and bounce zero. Direct-space sweeps, joints, contact events and public server/RID methods remain incomplete, with exact triggers in coverage.

Box2D.NET 3.1.654 source and the scoped hot-path patch record live in [`src/Vendor/Box2D.NET`](../../src/Vendor/Box2D.NET/VENDOR.md). The compiled public surface contains no Box2D type. [PhysicsBodyTests](../../tests/Electron2D.Tests/PhysicsBodyTests.cs) checks falling/contact/impulse/filter behavior, shape edits, lifecycle and 64 warmed frames each for resting, active-contact and freely moving bodies with zero managed allocations. [PhysicsMaterialTests](../../tests/Electron2D.Tests/PhysicsMaterialTests.cs) checks material mixing, live edits and ownership. [PhysicsAreaFieldTests](../../tests/Electron2D.Tests/PhysicsAreaFieldTests.cs) checks priority, damping modes, point gravity, sampled defaults and the warmed active-field allocation boundary. Native allocator counts, other platforms and owner visual acceptance remain unverified.

## Decision

- [0054: First Box2D-backed scene-body profile](../decisions/physics.md#adr-0054)
- [0056: Area field priority and body damping](../decisions/physics.md#adr-0056)
