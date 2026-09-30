# Physics indexed geometry decisions

Last updated: 2026-09-30

<a id="adr-0088"></a>
## ADR 0088: Shared scene/server logical shape slots

- Status: Accepted
- Scope: PhysicsServer indexed geometry, scene group projection, one-way slot policy and shape RID lifetime
- Depends on: [0063](physics.md#adr-0063), [0065](physics.md#adr-0065), [0071](physics.md#adr-0071), [0073](physics-mass.md#adr-0073), [0001](product.md#adr-0001), [0014](resources.md#adr-0014)

### Context

The pinned [server implementation](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/godot_physics_2d/godot_physics_server_2d.cpp) exposes indexed replacement, local poses, clear and body one-way policy. These indices count logical shapes rather than native fixture pieces. Electron2D already has scene owner groups and append-order global slots, while the first raw-server fixture methods only accepted caller-owned collider RIDs. A per-index edit must not affect other members of one group or create hidden scene children.

### Decision

- Route Body/Area add, count, get, replace, local-transform get/set, disable, remove and clear through the actual scene/server logical slots. Disabled/disposed scene resources keep logical slots under ADR 0071. Missing indices and wrong/stale owner/resource RIDs reject; a retained disposed resource returns empty from the raw getter. Removing reindexes later slots across all owner groups, and compound fixture tags retain their one logical index.
- Scene raw addition creates a transient null-owner group and appends one slot without a child node. Replacement changes the slot's effective resource while retaining its index, owner identity and local policies; ShapeOwnerGetShape reports that current borrowed geometry. Raw transform/disabled/one-way overrides affect only the selected slot and do not rewrite stored group or child fields. A corresponding group/child configuration edit reclaims those fields across that group; one-way edits restore the full stored group policy. Clear leaves owner identities and child configuration but removes shapes; a later child resource edit can repopulate its group. These runtime edits remain transient across PackedScene reconstruction.
- BodySetShapeAsOneWayCollision accepts an enabled flag, finite nonnegative recovery margin and finite normalized slot-local direction, default downward and preserving zero. Rotate it by the effective slot pose and body pose. Use the existing shared native pre-solve and body-motion kernels for both scene and raw-server body identities. Area fixtures remain sensors. Effective slot geometry/pose also drives dynamic mass calculations.
- Owned server shape entries provide shared RID-backed geometry to raw scene slots. ShapeSetData keeps the RID and replaces its same-kind private geometry copy, marks scene users and rebuilds raw-server users; FreeRID removes all dependent logical slots before releasing geometry. Preflight every related active world thread/phase before changing shared data or removing users. A retirement observer failure occurs after new data is committed; a free observer failure still unregisters the RID after removing users.
- Expose virtual Resource.GetRID as the typed dispatcher/hook: a base managed resource returns empty, and Shape overrides return stable registered physics identities. This collapses the source get_rid/_get_rid pair into C# virtual dispatch. Resource's [base source](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/core/io/resource.cpp) supplies empty when no role overrides it. Native rendering-resource RID projection remains an exact Partial dependency; this slice does not manufacture rendering identities.
- Bind each server-owned geometry view to its owning RID so resource queries and slots agree. Owner-group callers borrow that view; separate disposal rejects before lifetime changes. Data replacement retires a held old view, and free invalidates the current view. Clients needing independent long-lived resource data use ShapeGetData. Caller-owned managed Shape disposal still unregisters its borrowed RID and follows the existing disabled/disposed slot rules.
- Structural geometry edits may allocate. Warmed indexed reads, unchanged writes and prepared solver work reuse state; no second geometry store or simulator is added and no vendor source is modified.

### Verification and limits

PhysicsServerShapeSlotTests covers detached/active body and Area slots, actual query replacement/pose/disabled/reindex/clear, multiple members of one scene owner, child/group reclaim, shape RID/query identity, owned/borrowed lifetime, copy replacement, endpoint free, numeric/kind/phase/thread rejection, callback failure, mass-center geometry and real one-way solver/motion response for raw and scene platforms. Sixty-four warmed indexed-read/unchanged-write/solver frames allocate zero managed bytes on Linux/.NET 10. Native allocations, structural-edit allocation budgets, broad-world performance, other platforms and owner visual acceptance remain unverified.
