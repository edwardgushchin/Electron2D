# Physics collision filters

<a id="adr-0102"></a>
## ADR 0102: Directional body masks and shared filter mutation

Last updated: 2026-10-10

### Status

Accepted.

### Context

The complete physics contract requires the same layer/mask meaning for scene bodies, caller-owned server bodies and generated tile bodies. The initial Box2D integration required reciprocal mask matches and recreated geometry for filter edits. This suppressed one-sided interactions, made motion depend on the target's mask and allocated during repeated filter changes. The static server setters also only resolved caller-owned bodies, and body filter getters were absent.

The pinned [collision object](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/godot_physics_2d/godot_collision_object_2d.h), [body pair](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/godot_physics_2d/godot_body_pair_2d.cpp) and [motion culling](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/godot_physics_2d/godot_space_2d.cpp) distinguish pair admission, each body's response and directional motion queries. Backend convenience does not authorize reducing these behaviors under [ADR 0004](product.md#adr-0004).

### Decision

- CollisionLayer contains 32 category bits. CollisionMask names categories that this body responds to. A physical pair is eligible when either mask includes the opposite layer, subject to geometry, roles, exceptions and joint vetoes. A dynamic endpoint responds only when its own mask matches the opposite layer. Its effective inverse mass/inertia is zero for the other direction of that contact; its real velocity still influences the responding endpoint. The object's actual mass, inertia and response to other contacts or joints remain unchanged.
- Pairs containing a kinematic body and a static or another kinematic body produce contact geometry without impulse response when either endpoint enables contact reporting. Two static bodies remain ineligible. Reporting can be enabled after attachment and immediately activates a kinematic receiver; disabling the final receiver retires the pair. The common snapshot cap, owner/index and event rules apply. Reporting kinematics stay active so static-peer edits cannot leave stale snapshots.
- Body motion tests use the moving body's mask against candidate layers. The target's mask does not veto movement queries. Direct-space queries retain their query-mask/target-layer rule. Area monitoring remains directional from the observing area's mask and keeps existing monitor lifetime/callback rules.
- The broad phase must conservatively retain asymmetric candidates, including zero-layer objects that respond through a nonzero mask. CPU and GPU contact preparation, scalar/vector constraints, positional correction and CCD must honor the same directional response. Internal graph ordering and backend-specific cache layouts are not public acceptance conditions.
- BodyGet/SetCollisionLayer and BodyGet/SetCollisionMask resolve both scene and caller-owned body RIDs, including tile bodies. Reads preserve all bits and work before attachment and while geometry is disabled. Stale/wrong-kind IDs and attached thread/solver/failure violations reject. Low-level edits to generated tile bodies are runtime edits; tile authoring remains in TileSet and may reassert its policy on rebuild.
- Filter mutation updates existing fixture metadata rather than recompiling geometry. Queries observe it immediately; solved contacts and overlap membership publish on the next step. Shape resources, logical indices, owners and body RIDs remain stable. Body filter assignment retains the reference wake behavior, including assignment of unchanged bits; wake the body and existing contact neighbors without requiring geometry replacement. Area filters retain their own monitoring policy.

### Consequences

Zero-allocation warmed filter changes become compatible with the directional public contract. A body may affect another body's motion without receiving the reciprocal impulse response. This is distinct from collision exceptions, one-way platforms and sensor monitoring.

### Rejected alternatives

- Reciprocal masks because the CPU dependency uses them by default: changes public behavior.
- Pair admission by either side but symmetric mass response: incorrectly pushes the endpoint whose mask rejects the contact.
- Rebuilding geometry for filter edits: duplicates authoring work and discards prepared fixture state.

### Verification boundary

Current implementation and open behavior are recorded in the physics component/coverage ledger, not implied by this decision. Acceptance requires common CPU/GPU query, actual motion/impulse, contact/sensor, sleep/wake, lifecycle/error and warmed-allocation checks. The kinematic/static and kinematic/kinematic report-only paths are exercised by PhysicsReportOnlyTests; wider CCD and unverified paths retain their own gates. This decision does not close backend extensions or the overall GPU physics goal.
