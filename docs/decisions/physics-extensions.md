# Physics backend extension decisions

Last updated: 2026-10-10

<a id="adr-0103"></a>
## ADR 0103: Registered world backends and typed extension dispatch

### Status and scope

Accepted architecture; implementation and acceptance remain open. Owns backend
registration, factory selection, world-scoped implementation operations, direct
state/query extension families and their complete parameter/result boundary.
Depends on [0004](product.md#adr-0004), [0095](singleton-services.md#adr-0095),
[0054](physics-backends.md#adr-0054), [0063](physics.md#adr-0063) and
[0070](physics.md#adr-0070). This decision does not declare new runtime API shipped.

### Evidence and problem

The pinned [manager](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/servers/physics_2d/physics_server_2d.cpp)
registers named factories and selects a default only at strictly greater priority.
Its public comparison census has two manager members. The
[extension header](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/servers/physics_2d/physics_server_2d_extension.h)
and pinned XML census contain 140 server-extension members, 48 body-state members
and seven direct-space members. Their types/member metadata has no deprecated
attribute. The header's older four-argument one-way binding is a separate
DISABLE_DEPRECATED compatibility path and is excluded; retain the current
five-argument direction contract. Counts include lifecycle and query-exclusion
helpers, not just body simulation.

Current Electron2D has two built-in per-world implementations behind a static
sealed PhysicsServer facade. PhysicsDirectBodyState remains sealed; PhysicsDirectSpaceState now supports the caller-created PhysicsDirectSpaceStateExtension with guarded six-query dispatch and scoped exclusions. Registered backend factories do not yet supply extension views to scene/server consumers. Validated query-result construction and reusable motion-result filling now execute through a separate public consumer; RID allocation belongs to the engine.
The internal factory now creates a retained implementation that owns each built-in solver and complete interval dispatch. All six direct-space operations now dispatch through that owner with implementation-owned result/CPU geometry scratch; public guards and array/span projection stay common. The selected owner now creates fresh concrete CPU/GPU collider attachments for scene/server bodies and Areas; state, forces, mass, shape/filter, surface, contact and portable operations dispatch through that attachment. The selected owner also creates fresh concrete CPU/GPU joint attachments; local-frame sampling, live joint settings, solver policy, portable frames and release route to their owning implementations. The shape-free CPU world anchor now belongs to the CPU world owner. World settings, solver/sensor capacity, worker selection and completed statistics now dispatch through the selected owner, including native initialization. One common step boundary now dispatches owner-specific world/body preparation, solve, synchronization, body publication, contacts, Areas and finalization while retaining common event/callback guarantees. Body-motion tests now dispatch engine-unit inputs and RID exclusions through that owner; CPU owns its candidate/recovery/sweep geometry, while GPU uses resident motion queries. Platform point velocity resolves current scene/raw RID membership and dispatches through the selected collider without a world-body scan. Other server operations and concrete native scene helpers remain separate built-in paths, while resident queries retain the existing space driver and geometry leases. A registry returning enum aliases, an unused factory, or facade-only
interception would leave scene behavior and direct queries outside the extension.

### Decision

- Keep PhysicsServer and PhysicsServerManager as sealed retained service objects
  with static public operations under ADR 0095. Separate the reference server's
  implementation-object role as PhysicsServerBackend : ElectronObject, with
  PhysicsServerExtension : PhysicsServerBackend. This explicit C# role mapping
  preserves the full applicable implementation capability and extension inheritance
  without making global service operations an instance API. The scoped backend is
  owned by one immutable world context; it is not another process-wide singleton.
  Built-in CPU/GPU implementations remain internal and keep their existing fast
  paths and independent state ownership.
- RegisterServer uses a typed factory returning a fresh backend implementation.
  Duplicate names reject; SetDefaultServer requires a registered name and only a
  strictly greater priority replaces the default. Invoke user factories outside
  the registry lock and reject null, disposed, already-bound or recursively created
  implementations. Failure must preserve other registrations and existing worlds.
  Do not add unrequested unregister/enumeration operations absent from the pinned
  public manager contract.
- Named World/Space creation projects startup implementation selection in the
  existing per-world library lifecycle. DEFAULT resolves the manager's priority
  choice; CPU/GPU retain reserved built-in identities. Capture selection for that
  runtime and preserve it across resource aliases/duplicates. Existing parameterless
  creation remains CPU; explicit enum selection remains independent of named
  defaults. Registration/default changes affect later creation only, never a live
  simulation. Requested/actual named identity must be observable alongside the
  existing CPU/GPU identity and fallback diagnostic. A distinct extension backend
  identity must not falsely claim that an arbitrary implementation is Box2D or the
  resident GPU store.
- The facade retains shared resource RIDs and common authoring/lifetime metadata.
  Backend-private handles stay private. Adapt creation hooks to receive engine-
  assigned resource identities where needed; the capability is still actual
  creation of backend state, not public forging of numeric RIDs. Detach/reattach
  preserves scene/server identities and transfers common authored/observable state
  before retiring the old attachment. Shapes shared across worlds retain their
  logical resource identity with separate backend attachments.
- Implement the complete server-operation family, including typed parameter/state
  splits, shape/Area/body/joint operations, active policy, input picking, monitors,
  direct states, debug snapshots, process statistics and init/step/sync/flush/end-sync/
  finish. Scene nodes, raw server consumers, physics bones, motion and queries must
  reach the same attached implementation. A backend can own its simulation and
  geometry; an extension restricted to modifying built-in forces is insufficient.
  Do not export abstract or virtual families whose runtime call paths are absent.
- Direct-body and direct-space extensions preserve their applicable base API and
  inheritance through guarded public operations and typed protected implementation
  hooks. Library context constructors bind current space/body attachment identity;
  validate owner thread, lifetime, world failure, solver phase and borrowed callback
  rules before invoking user code. Concrete built-in views keep their current public
  semantics and cached lifetime. Unsealing a type alone is not an extension contract.
- A caller-created direct-space extension view may bind a live engine-assigned
  space RID and execute its complete six-query family before registered server
  implementations ship. Every inherited public query must reach the typed hook,
  with the same guarded context, result validation and nested exclusion lifetime.
  The view owns no world and does not replace the world's cached built-in view.
  This executes the direct-space extension capability; factory-returned scene/server
  integration and the wider server/body/custom-geometry families remain open.
- Map raw output pointers to typed spans/values using the existing query/motion
  result family. Provide the complete construction/projection path required by
  backend authors. The library samples object associations and validates physical
  RID/shape ownership, finite values, result counts and fractions; it must not trust
  forged backend object identities. Query exclusions are scoped to the current
  synchronous invocation and restored in finally, including nesting and exceptions.
- Backend lifecycle executes for real: initialization precedes attachment; solve,
  synchronization and query/event flush have explicit phases; end-sync and finish
  run on all required cleanup paths. A started failing interval becomes unusable
  without hidden CPU replay. Aggregate cleanup failures after attempting every
  owned resource. Borrowed backend/state objects cannot release a live callback
  context or retarget to another world.
- Preserve checkpoint and portable-state guarantees under ADR 0054. Same-backend
  private replay state belongs to the implementation and must have an executable
  capture/restore ownership path. Portable network state retains common identities,
  authoring compatibility, observer history and silent correction; backend-private
  handles or unbounded user objects never enter the wire payload.
- Custom geometry, typed data, drawing/bounds, resource mutation, body/Area/query/
  contact participation and result construction are one connected extension boundary.
  A reserved Custom enum or an externally non-derivable Shape is insufficient.
  Built-in kernels reject unsupported geometry before mutation; an extension must
  execute the applicable geometry contract without replacing it with a finite
  placeholder. Preserve all current CPU/GPU shape behavior.

### Rejected alternatives

- Registry entries that only rename CPU/GPU or select an existing enum: no backend
  extension implementation is created or exercised.
- Global singleton replacement or unsealing PhysicsServer to publish instance
  service operations: conflicts with ADR 0095 and immutable per-world selection.
- Static-facade hooks only: scene nodes and internal query/motion paths bypass them.
- New untyped parameter bags, object payloads or native pointers: conflicts with
  the typed API, ownership and networking boundary.
- Public empty/default-result extension stubs or blanket NotSupported forwarding:
  do not implement the required capability and cannot close coverage.

### Acceptance

Audit every pinned member, its declaring owner, type/parameter/result family and
current deprecation metadata in both directions. Preserve exact defaults, identities,
callback/event guarantees and numerical contract. Run a separate consumer using only
public Electron2D API that registers an actual implementation, changes physical
behavior, supplies real body/query results and drives both scene and server worlds.
Exercise priority ties, explicit selection, lazy aliases, factory failures, callback
failures/reentrancy, exclusions, wrong/stale IDs, attachment transfers, release and
zero warmed managed allocations. Common CPU/GPU, checkpoint, network, no-GPU server
and independent-renderer tests remain required; extension declarations or a passing
factory-only test do not establish full acceptance. All extension coverage remains
open until its actual exercised path exists.
