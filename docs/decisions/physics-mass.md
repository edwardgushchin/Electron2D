# Electron2D physics mass decisions

Last updated: 2026-09-26

This bounded document owns configured and geometry-derived body mass, center and polar inertia. [The decision index](index.md) routes other physics capabilities.

<a id="adr-0073"></a>
## ADR 0073: Shared body mass profiles and typed parameter branches

Last updated: 2026-09-26

- Status: Accepted
- Scope: RigidBody center/inertia and scene/server mass parameter branches
- Depends on: [0054](physics.md#adr-0054), [0061](physics.md#adr-0061), [0062](physics.md#adr-0062), [0070](physics.md#adr-0070), [0072](physics.md#adr-0072), [0001](product.md#adr-0001), [0014](resources.md#adr-0014)

### Context

RigidBody normalizes native fixture mass to its stored kilograms, but its existing calculation cannot restore a custom center or explicit inertia. Explicit server bodies still use raw unit-density fixture mass. A coherent force API across detached, scene and server bodies needs one mass policy. The pinned scene contract stores Inertia=0 for automatic calculation and CenterOfMass=zero separately from its computed center; Custom mode is required before a changed center assignment, returning to Auto clears that stored vector while retaining an explicit inertia override, and a changed mode emits a property-list notification.

### Decision

- Expose global RigidCenterOfMassMode Auto=0/default and Custom=1 to coexist with the same-named C# property. Add stored CenterOfMass and Inertia. Inertia uses kilograms times squared scene units; zero selects automatic geometry without replacing the stored zero. Mode changes retain explicit inertia and notify PropertyListChanged after a complete profile commit. Equal center assignment remains harmless in Auto; a changed value there rejects.
- Normalize all scene/server body geometry to configured positive kilograms (default one) through the same internal PhysicsMass calculation. Preserve the native area-weighted actual geometry centroid/moment policy from ADR 0062, rather than reproducing the comparison backend's AABB weighting and shape-origin approximation. Ignore sensor/ray fixtures. If there is no solid area, preserve ADR 0061 length-weighted thin-rod inertia for segment fixtures. Unshaped/point-only automatic bodies have zero inertia; explicit inertia permits angular response without shapes.
- For a custom center, derive automatic inertia with the parallel-axis theorem around that selected local point. An explicit positive inertia stays independent of subsequent mass or shape changes. Calculate using the fixture primitives with unit density independently of their static/dynamic density; do not derive the next profile by repeatedly scaling an already overridden native mass. Keep body origin, scene pose and configured velocity unchanged on profile edits. Refresh solver shape extents relative to the chosen center because native SetMassData does not do so.
- Keep configured profile and cached resolved geometry separate from physical inverse values. Static/kinematic bodies have zero solver mass/inertia while retaining the resolved center; Freeze/MakeStatic, mode switches, fixture edits, removal and reentry preserve configured profile and restore it with dynamic participation.
- Expose typed PhysicsServer BodySet/GetMass, BodySet/GetInertia, BodySet/GetCenterOfMass and BodyResetMassProperties for all live body RIDs. GetMass returns configured kilograms; GetInertia/GetCenterOfMass return explicit configuration or the most recently resolved automatic geometry, initially zero until first attachment. Attached reads synchronize pending geometry. Reset chooses both automatic channels without changing mass. Scene RigidBody descriptors share this server profile and reset clears their stored inertia and custom center. This is a typed ownership adaptation; no Variant dispatcher or inert selector enum is added. Remaining material, gravity and damping parameter branches stay Partial.
- Validate finite units, reciprocal native inertia and center-relative shape extents before native/profile mutation. Reject undefined enum values, negative inertia, off-owner attached changes and solver-owned pose callback edits. Property-list failures report after complete profile commit. Serialize mode before center, preserving zero versus explicit inertia. Reuse indexed shape scans with no per-change collections or native world allocation; vendored source stays unchanged.

### Consequences and verification

Games can define asymmetric bodies, tune their angular response and use the same mass policy through scene or RID APIs. PhysicsMassProfileTests checks defaults and mode values, automatic/custom center and parallel-axis values, center impulse, explicit inertia without shapes, live shape changes, native extent refresh, static/linear/dynamic mode restoration, Freeze/MakeStatic, detached configuration, reset, numeric/thread/phase rollback, property-list failure and PackedScene. Sixty-four warmed profile changes plus active solver frames allocate zero managed bytes on the owner thread in Linux/.NET 10. Native allocation, other platforms and owner visual acceptance remain unverified. The server force slice can now reuse normalized mass; detached impulse state and missing typed force operations remain its next implementation scope.
