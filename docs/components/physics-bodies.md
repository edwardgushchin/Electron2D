# Scene physics bodies component

Last updated: 2026-09-26

## Scope and owned types

[`CollisionObject`](../classes/CollisionObject.md) owns 32-bit layer/mask filtering. [`PhysicsBody`](../classes/PhysicsBody.md) owns direct CollisionShape children, backend body lifetime and typed motion queries; [`KinematicCollision2D`](../classes/KinematicCollision2D.md) carries motion contacts. [`StaticBody`](../classes/StaticBody.md) constrains movement; [`AnimatableBody`](../classes/AnimatableBody.md) inherits it and moves manually with kinematic contact velocity; [`RigidBody`](../classes/RigidBody.md) responds to gravity, contacts, velocity and impulses. All three concrete bodies borrow [`PhysicsMaterial`](../classes/PhysicsMaterial.md) to set surface friction and bounce. The hierarchy preserves the reference intermediate roles above Entity.

## Fixed-step flow

Direct CollisionPolygon children join the same body fixture owner list as borrowed CollisionShape children. Their solid convex pieces or hollow closed edges use the body's filters, material and mass policy, and their one-way body-contact setting uses the same world pre-solve callback.

SceneTree creates its internal Box2D.NET world when the first body or area enters. Scene-unit positions and velocities are converted to meters by 0.01; typed project settings default to downward gravity 980 scene units/s² and world damping 0.1/1 per second. The existing physics callback lane runs first so force and velocity changes affect that step. Area gravity and damping fields resolve against current overlap, then each active rigid body's stored constant force and torque join the solver's force accumulator. AnimatableBody sends its latest kinematic target before four backend substeps. Solved dynamic pose and velocity synchronize back; synchronized kinematic poses are then presented; current body contacts and sleep transitions commit before object-level contact callbacks, area overlap events, timers, tweens and interpolation end capture. Zero elapsed time does not advance the world.

One or more direct CollisionShape children provide independent circle/capsule/segment/rectangle fixtures or compound convex and hollow concave polygon fixtures. Category/mask bits filter responses. Shape, disabled-state, filter or borrowed material edits rebuild fixtures before the next step; an inactive/foreign child contributes none. Each fixture receives the body's material settings, and the world mixes friction and bounce through the pinned surface rules without a low-speed bounce threshold. A failed transform validation aborts that step without destroying prior fixtures, and a corrected next step can proceed. Tree exit, re-entry and disposal release and rebuild the backend handles without exposing them to callers.

`TestMove` and `MoveAndCollide` prepare those same fixtures and scan body contacts before a solver step. They recover from initial penetration, test a supplied or current global pose with reciprocal masks, skip pass-through one-way contacts, then bracket the first new hit. `TestMove` and `testOnly` leave the scene pose unchanged; a regular move applies travel before collision. Typed contact snapshots include both direct shape-owner indices, point, normal, depth, point velocity, travel and remainder. A deep residual overlap stops motion rather than tunneling. A one-way child margin limits accepted recovery depth, while ordinary fixed-step pair contacts continue their established side decision.

A PhysicsBody can list another scene body as a collision exception. The server exposes the same one-sided RID operation for scene or explicit bodies; either side's entry suppresses that pair in fixed-step solver contacts and body motion tests. Exception edits mark the owner fixtures dirty, so even an already touching pair uses the new rule at the next query or step. Scene enumeration copies insertion order and maps server-only or freed RIDs to null scene slots. Areas remain independent sensors; exception lists stay with body identity through fixture rebuild and tree reentry.

Explicit force and impulse calls synchronize any pending body pose and child fixtures first, including mass and center of mass. A positioned argument is an unrotated world-axis offset from the body's current backend origin. Linear forces/impulses convert by 0.01 and torque/angular impulses by 0.0001; constant totals persist until cleared and are serialized with scene state. Solver rotation and translation return through one unit-scale global transform assignment so repeated angle updates do not accumulate decomposition scale error.

## Current contract and limits

Every scene CollisionObject now has a stable opaque [RID](../classes/RID.md) until disposal. The SceneTree registers its one solver world with [PhysicsServer2D](../classes/PhysicsServer2D.md), and server-created bodies may attach to that same space; [direct ray/point queries](physics-queries.md) return both RID and scene object when available. Fixture rebuilds retain the collider RID and shape-owner index. Query layers do not depend on the collider's own mask. Wider body-state, shape-index event and joint methods remain separate server slices.

An enabled one-way CollisionShape labels its body fixtures for the world pre-solve callback. The child's local direction follows both child and body rotation. The initial side decision remains stable while the fixture pair touches; contact snapshots exclude pairs rejected from the pass-through side. StaticBody, AnimatableBody and RigidBody use the same owner path. Its one-way margin, and the sibling CollisionPolygon margin, now execute in typed kinematic recovery; neither changes the ordinary fixed-step contact side decision.

RigidBody defaults to mass 1 kg, gravity scale 1, zero velocity/body damping and constant force/torque, sleeping allowed, unfrozen and rotation unlocked. It supports finite signed gravity scaling and damping, positive finite mass, typed linear/angular velocity, sleep/freeze/rotation policy, two body damping modes, central and positioned force/impulse, torque/torque impulse, persistent force/torque, axis velocity, bounded contact-point counts, opt-in object contact reports and solver sleep events. Area fields resolve each gravity/damping channel by priority; a changed field wakes an affected sleeping body. `PhysicsBody.GetGravity()` returns the last resolved scaled field for a dynamic body. Dynamic segment-only bodies use length-weighted thin-rod mass and inertia; unshaped bodies retain mass without fixtures. StaticBody supplies a stationary collision base; AnimatableBody moves that role kinematically with a stored synchronization policy. Stationary conveyor velocities remain separate rows. Null material overrides use friction one and bounce zero. Direct-space sweeps, shape-index/RID events, joints and public server methods remain incomplete, with exact triggers in coverage.

Box2D.NET 3.1.654 source and the scoped hot-path patch record live in [`src/Vendor/Box2D.NET`](../../src/Vendor/Box2D.NET/VENDOR.md). The compiled public surface contains no Box2D type. [PhysicsMotionTests](../../tests/Electron2D.Tests/PhysicsMotionTests.cs) checks scene/server body sweeps; [PhysicsCollisionExceptionTests](../../tests/Electron2D.Tests/PhysicsCollisionExceptionTests.cs) checks unilateral exceptions, live solver contacts, motion filtering, lifetime and 64 warmed frames without managed allocation. Existing [PhysicsBodyTests](../../tests/Electron2D.Tests/PhysicsBodyTests.cs), shape, area, material and RigidBody tests cover other fixed-step responses and lifecycle. Native allocator counts, other platforms and owner visual acceptance remain unverified.

## Decision

- [0054: First Box2D-backed scene-body profile](../decisions/physics.md#adr-0054)
- [0056: Area field priority and body damping](../decisions/physics.md#adr-0056)
- [0057: Positioned and persistent rigid-body forces](../decisions/physics.md#adr-0057)
- [0058: Rigid-body contact and sleep snapshots](../decisions/physics.md#adr-0058)
- [0060: Fixed-step kinematic platform motion](../decisions/physics.md#adr-0060)
- [0061: Two-sided segment fixtures and zero-area mass](../decisions/physics.md#adr-0061)
- [0062: Compound convex fixtures](../decisions/physics.md#adr-0062)
- [0064: Hollow paired-segment resource](../decisions/physics.md#adr-0064)
- [0065: One-way scene-body contacts](../decisions/physics.md#adr-0065)
