# Electron2D physics force decisions

Last updated: 2026-09-26

This bounded document owns scene and RID body force/impulse operations. [The decision index](index.md) routes mass, monitoring and other physics capabilities.

<a id="adr-0057"></a>
## ADR 0057: Positioned and persistent rigid-body forces

Last updated: 2026-09-26

- Status: Accepted
- Scope: RigidBody force, impulse, torque and axis-velocity vertical slice
- Depends on: [0054](physics-backends.md#adr-0054), [0056](physics.md#adr-0056), [0008](scene.md#adr-0008)

### Context

The first body profile exposed central one-step force and impulse, but game code lacked positioned actions, torque, persistent forces and axis velocity. The pinned reference treats a position argument as an unrotated world-axis offset from the body origin, stores a positioned constant force as a force vector plus a moment computed at addition, and replaces only the velocity component parallel to the chosen axis. The internal backend exposes all required operations but uses meters rather than scene units.

### Decision

- Expose RigidBody `ApplyForce`, `ApplyImpulse`, `ApplyTorque`, `ApplyTorqueImpulse`, `AddConstantCentralForce`, `AddConstantForce`, `AddConstantTorque`, `ConstantForce`, `ConstantTorque` and `SetAxisVelocity`. An optional zero impulse on `ApplyCentralImpulse` matches the pinned default. Persistent totals are typed scene state, remain across fixed steps and scene packing, and stop when cleared. A frozen body stores them without applying them until unfrozen.
- Use 0.01 meters per scene unit for force and linear impulse, 0.0001 squared meters for torque and angular impulse, and convert positioned arguments from the body's current backend origin in world axes. `AddConstantForce` adds its moment about the current center of mass once at the call; a later rotation does not recompute that stored moment. Validate finite inputs, accumulated totals and generated positions/moments before applying an action.
- Synchronize pending body pose and child fixtures before public force/impulse/torque actions. This makes a newly attached or edited shape's mass and center of mass available immediately rather than waiting for the next fixed step. Apply persistent force/torque after area field reduction and before solver stepping. Wake bodies on explicit actions as in the pinned API; persistent force application itself does not repeatedly wake a resting body.
- Route force/impulse operations through the shared body runtime of ADR 0074. Keep scene immediate actions attached-only, retain pending force through removal/static/dormant participation, and capture native velocity before detaching.
- Synchronize a simulated body's position and rotation back to its scene node in one unit-scale global transform assignment. Reconstructing rotation from the previous decomposed scale accumulated float error and eventually rejected an otherwise valid rotating body. Warmed active force/torque frames allocate zero managed bytes in the checked Linux/.NET 8 profile; native allocation, other platforms and game acceptance remain unverified.

### Consequences

Games can push at an offset, add a one-time impact, spin a body, maintain propulsion and set a jump-axis velocity without public backend types. Shifted-center, unit-conversion, freeze/reentry, invalid-input rollback, packing and repeated-rotation paths have executable checks. Contact monitoring, custom integration, continuous collision, server axis velocity remains separate body-state work; [ADR 0074](#adr-0074) implements the server force family; [ADR 0073](physics-mass.md#adr-0073) now implements custom center and inertia through a shared mass profile.

<a id="adr-0074"></a>
## ADR 0074: Shared scene/server force state and detached resolution

Last updated: 2026-09-26

- Status: Accepted
- Scope: All 13 server force/impulse operations and their scene/direct-state projections
- Depends on: [0057](#adr-0057), [0070](physics.md#adr-0070), [0073](physics-mass.md#adr-0073), [0072](physics.md#adr-0072), [0014](resources.md#adr-0014)

### Context

Shared mass profiles exist, but the server lacks the 13 central/positioned/torque transient and constant operations. A native attachment is not a stable body identity: detachment destroys its velocity and force accumulators. Positioned calls need the current rotated center, including detached configuration. Axis velocity is a distinct body-state operation and remains in that slice. The pinned backend stores forces until eligible integration and applies impulses to current inverse values; its deferred mass queue timing is not the immediate profile policy accepted in ADR 0073.

### Decision

- Expose all 13 typed PhysicsServer methods for every live scene/server body RID. Impulses update retained state immediately; current mass/inertia and rotation lock determine inverse response. Use the accepted immediate profile resolution for both attached and detached calls instead of inheriting comparison-backend mass queue timing. Static/kinematic inverse values ignore impulses. Finite candidate scene and native velocities are checked before any impulse write.
- Reuse Shape.AppendQueryProxies for detached local mass geometry, transforming each proxy through the owner/slot pose. Measure the same circle/capsule/polygon/segment primitives for native fixtures and detached resource proxies; exclude sensors/rays. Cache per-body proxy buffers and resolved mass values. No temporary body or world is created. Geometry-dependent force/impulse calls may resolve automatic mass before first attachment; configuration-only constant getters/setters remain independent of geometry readiness.
- Keep one-step PendingForce/PendingTorque in the body runtime. Positioned actions capture world-axis moment about the current rotated body-origin center at call time. Preserve pending values across removal, static and dormant participation. The next awake dynamic fixed step applies them once after Area reduction; kinematic participation consumes them without dynamic response. Omission clears eligible pending/native accumulators while retaining impulses and configured constant totals. A later explicit Sleeping assignment wins over the force call's wakeup; it does not silently discard pending input.
- Persistent totals share RigidBody descriptors and the runtime record for other bodies. Add operations wake even for zero input; nonzero replacement wakes, zero replacement does not reassign/wake the untouched channel. Validate both accumulated channels before committing. Route scene and direct-view operations through this same logic, preserving existing scene immediate-action attachment requirements and borrowed-view lifetime guards.
- Capture dynamic native velocity before attachment destruction, so an impulse between the last solver tick and removal survives reentry. Disable intermediate native updateBodyMass during fixture creation; the complete profile applied after all fixtures preserves configured linear/angular velocity rather than injecting a COM-shift velocity during attach/rebuild. Keep all backend changes in engine-owned code; vendor stays unchanged.
- Enforce scene/space owner thread and non-stepping access before mutation. Post-solver callbacks may queue the next step's force. Wrong-kind/freed RID, malformed moment, overflowed totals or candidate velocities reject without partial force/impulse state. Reuse buffers and indexed scans after warmup.

### Consequences and verification

Games can control scene or explicit bodies by RID before attachment, keep propulsion settings across lifetime changes and combine transient force with instantaneous impacts. PhysicsServerForceTests verifies all 13 calls, analytic units and rotated centers, pending single-use integration, detached impulses, immediate-impulse removal capture, static/linear-only modes, last Sleeping write, omission, unchanged-channel clearing, every current primitive family, numeric/thread/phase errors and 64 warmed attached/detached action plus solver iterations with zero managed allocation on Linux/.NET 10. Native allocation, other platforms and owner visual acceptance remain unverified. Wider body-state/axis velocity, material/field parameter branches, CCD and joints retain separate exact coverage work.
