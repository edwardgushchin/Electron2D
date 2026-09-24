# Electron2D physics decisions

Last updated: 2026-09-24

This bounded document owns the first executable two-dimensional physics integration. [The decision index](index.md) routes other domains.

<a id="adr-0054"></a>
## ADR 0054: Box2D-backed scene bodies and collision shapes

Last updated: 2026-09-24

- Status: Accepted
- Scope: First typed scene-body and shape vertical slice
- Depends on: [0012](product.md#adr-0012), [0008](scene.md#adr-0008), [0014](resources.md#adr-0014)

### Context

The fixed physics frame, scene-node hierarchy, typed resources and retained renderer exist, but the physics domain was absent. ADR 0012 selected vendored managed Box2D.NET; this is the first executable use. A public physics server, direct-space query API, area monitoring, joints, kinematic sweeps and all shape families remain separate work. Adding backend source without body behavior was rejected by ADR 0012.

### Decision

- Vendor all 233 C# source files of `ikpil/Box2D.NET` tag `3.1.654` at commit `5efc96def866edbb4e5a9368d84de5bf8c2dcaca` under `src/Vendor/Box2D.NET`, retaining the MIT license and [patch record](../../src/Vendor/Box2D.NET/VENDOR.md). Compile them into the single `Electron2D.dll`; make namespace-level backend declarations internal and expose no backend type through public/protected Electron2D signatures. Nullable and malformed upstream XML-comment compiler diagnostics are scoped to vendored files.
- Map the reference's `Shape2D`, `CircleShape2D`, `RectangleShape2D`, `CollisionShape2D`, `CollisionObject2D`, `PhysicsBody2D`, `RigidBody2D` and `StaticBody2D` to `Shape`, `CircleShape`, `RectangleShape`, `CollisionShape`, `CollisionObject`, `PhysicsBody`, `RigidBody` and `StaticBody`. Preserve `RigidBody : PhysicsBody : CollisionObject : Entity` and the parallel `StaticBody` branch; `CollisionShape : Entity` remains a direct body child and borrows its shape resource. These names remove only the redundant dimensional suffix, not inherited responsibilities.
- A `SceneTree` lazily owns one internal Box2D world. Direct body entry creates a backend body; direct collision-shape children add circle/rectangle fixtures. Shape changes, disables and collision-layer/mask edits rebuild fixtures before the next step. Tree exit and disposal release bodies, fixtures and world, while borrowed Shape resources remain caller-owned. Failed geometry validation rejects before replacing existing fixtures.
- One scene unit is 0.01 Box2D meters. Default downward gravity is 980 scene units per second squared. Scene fixed physics callbacks run before a four-substep world step; body transforms and velocities synchronize back before timers, tweens and the physics-interpolation end snapshot. Geometry accepts translation and rotation with unit global scale and zero skew; unsupported scaled/skewed active physics transforms fail explicitly. This is an initial profile, not a claim that every inherited or own physics member is complete.
- The initial public body profile includes circle/rectangle dimensions and bounds, managed shape copying, collision-shape assignment and enablement, 32 collision-layer/mask bits, dynamic/static contact response, mass, gravity scale, linear/angular velocity and damping, sleep, freeze, rotation lock, central force and impulse. Other applicable members retain operation-specific Partial, Unimplemented or Blocked coverage rows.
- Engine-owned warmed resting-contact, active-contact and moving-body fixed steps allocate zero managed bytes in the checked Linux/.NET 8 profile. The pinned Box2D.NET port needed per-world reuse of its step context and graph-color block array plus zero-overflow-contact fast exits; the applicable current-upstream buffer reuse is proposed in [ikpil/Box2D.NET#101](https://github.com/ikpil/Box2D.NET/pull/101). No allocation claim is made for unmeasured native/platform paths or user callbacks.

### Consequences

Games can attach a `RigidBody` and `StaticBody` with `CollisionShape` children, advance the existing fixed physics frame, observe gravity and contact response, and apply impulses without backend types or a second managed assembly. Resource copies, scene packing, lifecycle transitions and invalid-input rollback have executable checks. Other target platforms, cross-platform packaging, broad geometry/query types, scene world sharing across viewports, and owner visual acceptance remain unverified.

### Rejected alternatives

- Ship Box2D.NET as a managed package or expose its types publicly: ADRs 0004 and 0012 require one Electron2D-owned managed surface.
- Add public PhysicsServer2D or RID placeholders around the first scene bodies: their resource-identity and direct-space contracts need a complete separate vertical slice.
- Write a custom rigid-body solver: ADR 0012 selected the managed Box2D.NET backend.
