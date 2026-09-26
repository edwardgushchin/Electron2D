# Electron2D physics field decisions

Last updated: 2026-09-26

This bounded document owns scene/server Area gravity, damping and space default fields. The stable [physics ADR 0056 anchor](physics.md#adr-0056) routes here.

<a id="adr-0056"></a>
## ADR 0056: Area field priority and body damping

Last updated: 2026-09-26

- Status: Accepted
- Scope: Executable area gravity/damping, typed world defaults and body field response
- Depends on: [0055](physics-monitoring.md#adr-0055), [0019](core-data-io.md#adr-0019), [0014](resources.md#adr-0014)

### Context

Area monitoring supplies shape geometry and directional filtering but initially did not affect simulated bodies. The pinned 2D reference resolves gravity, linear damping and angular damping independently in descending area-priority order, then adds any unstopped world default and finally combines or replaces body damping. It applies damping to velocity before force integration. The managed backend offers one world gravity and a different built-in damping equation, so simply exposing field properties would not execute the accepted behavior.

### Decision

- Add typed `ProjectSettings` keys for the 2D default gravity strength/vector and linear/angular damping, with pinned defaults 980, (0, 1), 0.1 and 1. A SceneTree physics world samples active overrides when its first body or area attaches; the existing world retains that snapshot until an explicit typed server default-field edit.
- Give scene and server Areas the five numeric `SpaceOverride` modes and finite signed gravity/damping fields, including transformed point gravity, constant-strength or inverse-square falloff, shared direction/point storage and integer priority. Field participation is independent of `Monitoring` and `Monitorable`; an area's mask still tests the body's layer. Resolve each channel in descending priority before the backend world step, using current shape overlap rather than the preceding event snapshot.
- Preserve the backend's sampled world gravity, then apply each current dynamic body's difference from it as a mass-scaled force. Apply the pinned `max(0, 1 - delta * totalDamp)` linear/angular velocity factors before solver stepping and keep backend damping at zero. `RigidBody.DampMode` selects Combine or Replace separately for each channel. `PhysicsBody.GetGravity()` reports the last resolved vector after body gravity scaling on RigidBody; CharacterBody reports the same selected world/Area gravity without automatically applying it to caller-owned Velocity. Detached and StaticBody queries return zero.
- Validate all public numeric and enum inputs before mutation and reject a nonfinite resolved field or motion before changing body velocity. A changed resolved field wakes a sleeping body. Reuse area-order scratch storage and existing shape-distance scans; the checked warmed moving and sleeping-body field paths allocate zero managed bytes on Linux/.NET 8. No vendored source is changed.

### Consequences

Overlapping areas can create zero-gravity zones, directional and point attraction, and independent linear/angular damping with priority and stopping modes. Packed scenes retain field and body-mode state. Initial world defaults are sampled at creation and can then be changed explicitly by space RID; native allocator counts, other platforms, large-scene cost and owner visual acceptance remain unverified. Solver/contact behavior remains subject to the backend rather than a claim of complete physics-server parity.

### Rejected alternatives

- Add only field properties without simulation effects: these would be inert compatibility stubs.
- Change the shared world gravity or use its built-in damping for each area: those backend controls cannot represent simultaneous per-body fields or the pinned damping equation.

### Shared typed server Area fields

- Keep one internal PhysicsAreaFields profile per scene/server Area and per space default Area. Scene properties, descriptors and twenty concrete PhysicsServer getter/setters share that state. Reuse Area.SpaceOverride numeric values 0..4 for the server AreaSpaceOverrideMode projection; do not add a duplicate enum. All ten parameter capabilities execute: gravity mode, strength, vector, point flag, point unit distance, linear mode/rate, angular mode/rate and integer priority.
- Preserve distinct pinned initial profiles: scene Area gravity 980 with vector (0, 1), server-created Area 9.80665 with (0, -1); both have disabled override modes, non-point gravity, zero unit distance/priority, linear damp 0.1 and angular damp 1. Direction and point center share one vector. Directional vectors stay unnormalized and independent of Area rotation. Point centers use the current global pose; nonpositive unit distance means constant strength, positive distance uses inverse-square falloff and exact zero distance yields zero gravity.
- Add server-created Areas to the same stable descending-priority reducer as scene Areas. Select current geometry by exact overlap and directional mask against body layer, independently of callbacks, Monitoring and Monitorable. Deduplicate receiver geometry through one boolean overlap per Area/body. Disabled shapes, detach, reentry and free govern actual field membership. Resolve gravity and each damping channel independently, then apply the existing body scale/Combine/Replace and omission rules.
- Accept a live space RID in every typed field getter/setter, projecting the reference default Area shortcut. Under ADR 0056's host integration, each space starts with sampled ProjectSettings strength/vector/damping and priority -1. Default override modes and priority remain stored values; the default Area contributes the final unbounded fallback for each unstopped channel, with identity transform for point gravity. Changing defaults wakes affected dynamics at the next nonzero reduction and does not mutate ProjectSettings or other spaces. Keep the initial native world gravity as the subtraction baseline so edited per-body defaults retain correct physical response.
- Numeric AreaParameter selectors/container are excluded only as the untyped Variant dispatch representation under ADR 0001. Concrete typed pairs preserve every field behavior, including the space shortcut; this is not a field-capability exclusion.
- Validate finite signed numeric/vector values and defined modes before assignment. Reject attached off-owner or solver-owned access, including direct scene field mutation during pose synchronization; preserve scene capture guards. Field changes inside post-step Area callbacks are permitted for the next step and do not reset monitor histories. Reject nonfinite totals/motion before committing that body's field cache/velocity and surface the existing aggregate world-step failure; corrected fields can continue stepping.

PhysicsServerAreaFieldTests verifies all defaults and typed pairs, shared scene state, five modes and mixed priorities, body bit 32 and zero reciprocal masks, disabled/detached/reentered/freed receiver geometry, actual native server gravity/signed damping, space defaults and point fallback, point transforms/falloff/center/direction, sleeping wakeup, zero delta, overflow recovery, callback history and owner/solver guards. Sixty-four warmed active scene/server field edits, priority sorts, reads and solver frames allocate zero managed bytes on Linux/.NET 10. Native allocation, other platforms, broad-world cost and owner visual acceptance remain unverified. No vendor source changes.

Primary pinned sources: [Area profile and defaults](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/godot_physics_2d/godot_area_2d.h), [field computation](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/godot_physics_2d/godot_area_2d.cpp), [space default Area shortcut](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/godot_physics_2d/godot_physics_server_2d.cpp).
