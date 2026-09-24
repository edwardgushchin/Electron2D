# Physics domain

Last updated: 2026-09-24

## Responsibility

Physics owns the executable 2D rigid-body, collision-shape, surface-material and area-monitoring profiles. A SceneTree lazily owns one internal Box2D.NET world, advances it during its fixed physics lane, synchronizes dynamic body transforms and updates area overlap snapshots before timers and tweens. The selected managed backend is vendored and internal to `Electron2D.dll` under [ADR 0012](../decisions/product.md#adr-0012).

## Component inventory

| Component | Production types | State |
| --- | --- | --- |
| [Collision shapes](../components/physics-shapes.md) | [`Shape`](../classes/Shape.md), [`CircleShape`](../classes/CircleShape.md), [`RectangleShape`](../classes/RectangleShape.md), [`CollisionShape`](../classes/CollisionShape.md) | Circle/rectangle dimensions, bounds, borrowed placement and fixture updates executable; broad shape queries and one-way/debug options incomplete |
| [Scene physics bodies](../components/physics-bodies.md) | [`CollisionObject`](../classes/CollisionObject.md), [`PhysicsBody`](../classes/PhysicsBody.md), [`RigidBody`](../classes/RigidBody.md), [`StaticBody`](../classes/StaticBody.md), [`PhysicsMaterial`](../classes/PhysicsMaterial.md) | Dynamic/static motion, contact response, positioned and persistent force/torque, field gravity/damping, filtering and surface mixing executable; wider body/server contracts incomplete |
| [Physics areas](../components/physics-areas.md) | [`Area`](../classes/Area.md), [`Area.SpaceOverride`](../classes/Area.SpaceOverride.md) | Directional monitoring, snapshots, object events and priority gravity/damping fields executable; audio and shape events incomplete |

## Public surface

`RigidBody` and `StaticBody` inherit the spatial `Entity` role through `PhysicsBody` and `CollisionObject`; `Area` is the parallel `CollisionObject` branch. `CollisionShape : Entity` must be their direct child to supply a borrowed Shape resource. Circle and rectangle resources expose their pinned dimensions and local bounds. Bodies and areas expose 32 collision-layer/mask bits. Both concrete bodies can borrow a PhysicsMaterial with friction, bounce, rough and absorbent settings. A dynamic body exposes mass, gravity scale, velocity, signed damping, two damping modes, sleep, freeze, rotation lock, central/positioned force and impulse, torque, persistent force/torque, axis velocity and last-step `GetGravity()`. Areas expose monitoring flags, object-level overlap events, snapshot queries and independent gravity/damping fields with priority and five space modes.

## Runtime flow and invariants

One scene unit maps to 0.01 Box2D meters. Typed project defaults provide downward gravity 980 scene units/s² and linear/angular damping 0.1/1 per second, sampled when a physics world is created. The scene physics callback lane runs first; current area/body shape overlaps then resolve gravity and damping by descending priority, and persistent body force/torque joins the accumulator before a four-substep backend step. Body damping applies `max(0, 1 - delta * totalDamp)` before force integration. Linear forces/impulses convert by 0.01 and torque/angular impulses by 0.0001. Updated positions and velocities reach scene nodes through one unit-scale global transform before area monitoring snapshots, timers, tweens and the interpolation end snapshot. Bodies and areas register on tree entry and leave on exit/disposal. Shape, collision-filter and material edits rebuild fixtures before the next step, or immediately before an explicit force/impulse action. Shape and material resources remain caller-owned; a disposed borrowed shape stops contributing a fixture, while a disposed material restores default friction and bounce.

The current geometry profile accepts translated/rotated bodies and areas with unit global scale and zero skew. Active scaled/skewed body, area or shape transforms fail explicitly and can be corrected before a later step. Invalid mass, dimensions, velocity, damping and bit indices reject before changing state. Engine-owned warmed resting-contact, active-contact, freely moving and steady area-monitoring frames in the checked Linux/.NET 8 setup allocate zero managed bytes. Backend types do not appear in the public/protected Electron2D assembly surface.

## Verification and limits

[PhysicsBodyTests](../../tests/Electron2D.Tests/PhysicsBodyTests.cs) checks rectangle and circle geometry, falling/contact response, impulse, filter changes, live shape edits, freeze and velocity, rejected transforms, tree exit/re-entry, managed shape copying, PackedScene state, disposal and the warmed allocation boundary. [PhysicsMaterialTests](../../tests/Electron2D.Tests/PhysicsMaterialTests.cs) checks defaults, mixing, live updates and disposal, duplication and PackedScene ownership. [AreaTests](../../tests/Electron2D.Tests/AreaTests.cs) checks directional detection, events, geometry changes and lifecycle. [PhysicsAreaFieldTests](../../tests/Electron2D.Tests/PhysicsAreaFieldTests.cs) checks field modes, point falloff, signed damping, sampled project settings, sleep wakeup, packing and failure recovery. [RigidBodyForceTests](../../tests/Electron2D.Tests/RigidBodyForceTests.cs) checks force/torque semantics, first-step mass geometry, packing, invalid rollback and warmed active allocations. Release build and the public-surface exporter verify the single-DLL boundary. Other platforms, owner visual acceptance, native allocator accounting and larger-world performance remain unverified.

The shape-query family, other shape resources, kinematic bodies, area audio integration, joints, direct-space state, public `PhysicsServer2D`/RID services, shape-index and contact monitoring, and remaining RigidBody modes retain exact incomplete coverage rows. An executing node and backend do not make those rows complete.

## Decisions

- [0054: Box2D-backed scene bodies](../decisions/physics.md#adr-0054)
- [0055: Directional scene area monitoring](../decisions/physics.md#adr-0055)
- [0056: Area field priority and body damping](../decisions/physics.md#adr-0056)
- [0057: Positioned and persistent rigid-body forces](../decisions/physics.md#adr-0057)
- [0012: Managed dependency vendoring](../decisions/product.md#adr-0012)
- [0008: Spatial scene inheritance](../decisions/scene.md#adr-0008)
- [0014: Resource lifetime and hot paths](../decisions/resources.md#adr-0014)
