# Physics backend extension decisions

Last updated: 2026-10-12

<a id="adr-0103"></a>
## ADR 0103: Registered world backends and typed extension dispatch

### Status and scope

Accepted architecture; implementation and acceptance remain open. Owns backend
registration, factory selection, world-scoped implementation operations, direct
state/query extension families and their complete parameter/result boundary.
Depends on [0004](product.md#adr-0004), [0095](singleton-services.md#adr-0095),
[0054](physics-backends.md#adr-0054), [0063](physics.md#adr-0063) and
[0070](physics.md#adr-0070). Public registration and the typed world implementation family now execute; arbitrary custom geometry and full operation acceptance remain open.

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
sealed PhysicsServer facade. PhysicsDirectBodyState now supports caller-created PhysicsDirectBodyStateExtension with generation-qualified state/force/space hooks and immutable PhysicsBodyContact projection; registered callback-view integration now has a bounded public-only factory fixture; full numerical acceptance remains open. PhysicsDirectSpaceState now supports the caller-created PhysicsDirectSpaceStateExtension with guarded six-query dispatch and scoped exclusions. Registered backend factories can supply guarded direct state views; the public-only factory-view fixture now verifies its bounded cache/contact/query/callback paths; full operation acceptance remains open. Validated query-result construction and reusable motion-result filling now execute through a separate public consumer; RID allocation belongs to the engine.
The internal factory now creates a retained implementation that owns each built-in solver and complete interval dispatch. All six direct-space operations now dispatch through that owner with implementation-owned result/CPU geometry scratch; public guards and array/span projection stay common. The selected owner now creates fresh concrete CPU/GPU collider attachments for scene/server bodies and Areas; state, forces, mass, shape/filter, surface, contact and portable operations dispatch through that attachment. The selected owner also creates fresh concrete CPU/GPU joint attachments; local-frame sampling, live joint settings, solver policy, portable frames and release route to their owning implementations. The shape-free CPU world anchor now belongs to the CPU world owner. World settings, solver/sensor capacity, worker selection and completed statistics now dispatch through the selected owner, including native initialization. One common step boundary now dispatches owner-specific world/body preparation, solve, synchronization, body publication, contacts, Areas and finalization while retaining common event/callback guarantees. Body-motion tests now dispatch engine-unit inputs and RID exclusions through that owner; CPU owns its candidate/recovery/sweep geometry, while GPU uses resident motion queries. Platform point velocity resolves current scene/raw RID membership and dispatches through the selected collider without a world-body scan. CPU native Area overlap/priority reduction and body preparation now belong to the CPU owner; both owners execute scene/raw Area scans and audio containment with their own retained scratch. CPU discrete/kinematic/CCD scheduling, pre-solve/one-way episodes, solved motion/contact/report-only history and material callbacks now belong to that owner; private checkpoints, portable one-way mapping and debug points dispatch through the selected implementation. Default-force preparation now dispatches through selected collider owners; CPU field integration is concrete rather than a common CPU cast. Other server operations and remaining concrete integration helpers remain separate built-in paths, while resident queries retain the existing space driver and geometry leases. A registry returning enum aliases, an unused factory, or facade-only
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
- Exception authoring remains in the common facade. Its add/remove operations reject
  numerical-hook writes before changing the owner list. The applicable extension
  exception getter projects to protected PhysicsServerBackend.GetCollisionExceptions
  rather than adding a game-facing server helper: it copies retained explicit
  entries followed by deduplicated active-joint targets, preserving inert RIDs.
  It requires a bound live owner-thread world, rejects foreign attached bodies,
  and may read detached common authoring. During that implementation's numerical
  hooks it reads only common authored input, without querying or reentering solver
  state; ordinary post-hook reads retain world-failure guards. Returned arrays
  belong to the caller and intentionally allocate. Selected pair spans remain
  the allocation-free input for steady simulation. Scene enumeration projects the
  same list to nodes/null slots. This is the typed projection of the pinned
  `_body_get_collision_exceptions` capability, not backend state exposure.
- Direct-body and direct-space extensions preserve their applicable base API and
  inheritance through guarded public operations and typed protected implementation
  hooks. Library context constructors bind current space/body attachment identity;
  validate owner thread, lifetime, world failure, solver phase and borrowed callback
  rules before invoking user code. Concrete built-in views keep their current public
  semantics and cached lifetime. Unsealing a type alone is not an extension contract.
- Caller-created direct-body and direct-space extension views may bind a live engine-assigned
  body attachment or space RID and execute their complete operation families before registered server
  implementations ship. Every inherited public operation must reach its typed hook,
  with the same guarded context, result validation and nested exclusion lifetime.
  The view owns no body/world and does not replace the cached built-in view. Body contexts are qualified by attachment generation and cannot revive on reentry.
  This executes the caller-created view capability. Selected factories supply
  scene/server integration views, now also checked by the public-only registered
  consumer. Cached views begin with an empty published contact snapshot; uncached
  caller-created views retain direct contact-hook dispatch. The wider numerical
  operation and custom-geometry acceptance gates remain open. The complete contact getter family may project one immutable PhysicsBodyContact hook result: its engine-validated construction samples RID/shape/object identity once, so retained contact values survive later geometry/association changes without trusting forged IDs.
- Body authoring and direct-body writes reject before mutation while a selected
  numerical implementation hook is borrowed, including partial initial attachment.
  Keep numerical borrowing distinct from a caller-created body-state view's
  ordinary requested write: that view may forward to its inner state. Guarded
  authored reads and ordinary post-solver callback writes retain their existing
  access. Hierarchy/resource-lifetime guards continue to cover every extension
  scope; state-write delegation does not authorize binding or geometry changes.
- A factory-returned cached body extension supplies its contact count/values during
  owner-thread publication after native solver/worker capture completes. The engine
  validates and commits a complete immutable contact snapshot before scene monitors
  and user callbacks. Later cached getters project that snapshot without reinvoking
  contact hooks or resampling associations; uncached caller-created views retain
  their ordinary direct hook dispatch. Count assignment and replay restore keep
  their established snapshot semantics. A qualified engine-only publication scope
  permits PhysicsBodyContact identity construction, not general solver queries,
  state mutation or hierarchy/lifetime changes. One bad contact receiver retains
  its prior published snapshot and contributes an aggregated callback error while
  other receivers and the already committed solver tick continue. Native device
  or solver failures keep their existing terminal policy.
- Map raw output pointers to typed spans/values using the existing query/motion
  result family. Provide the complete construction/projection path required by
  backend authors. The library samples object associations and validates physical
  RID/shape ownership, finite values, result counts and fractions; it must not trust
  forged backend object identities. Query exclusions are scoped to the current
  synchronous invocation and restored in finally, including nesting and exceptions.
  Public PhysicsServer.ShapeCollide selects an explicit live space for the two
  resource-pair implementation hook; BodyCollideShape selects the body's actual
  attachment or the detached resource path. Caller spans hold alternating global
  points, with pair count and atomic validated publication. These are logical
  shape-slot operations independent of scene collision filters and slot enables;
  preserve the independent swept-region rules of ADR 0069. Standalone Shape
  methods retain their resource-only ownership. Protected backend GetShapeGeometry
  reads immutable current authored data by live shape RID without cloning a
  resource or querying numerical state; qualify owner/lifetime/failure access and
  borrow the world while evaluating a procedural supplier.
  The shared world owns notification-delivery status: public backend
  IsFlushingQueries and static PhysicsServer.SpaceIsFlushingQueries(RID) read the
  same owner-thread/lifetime-qualified flag. It includes body force/integration/state,
  sleep, contact and Area notification callbacks and selected FlushQueries;
  ordinary simulation/query hooks are outside that phase unless a query is nested
  inside ongoing notification delivery. Status reads do not
  observe numerical state and remain available during terminal-error notification
  cleanup. Preserve existing callback order, mutation guards and error aggregation.
  Motion-test membership helpers are public operations on the world-owned
  PhysicsServerBackend/Extension: IsBodyExcludedFromMotionTest(RID) and
  IsObjectExcludedFromMotionTest(ulong), using the same object-ID type as motion
  parameters/results. They read the original borrowed input lists without copying
  or resolving identities, return false outside the world's motion test, and keep
  owner/lifetime/failure guards. Parameter edits affect the next invocation.
  Same-world recursive motion tests reject before replacing exclusions; another
  world's nested motion scope is independent. Hook/result failures clear the scope.
- Backend lifecycle executes for real: initialization precedes attachment; solve,
  synchronization and query/event flush have explicit phases; end-sync and finish
  run on all required cleanup paths. A started failing interval becomes unusable
  without hidden CPU replay. Aggregate cleanup failures after attempting every
  owned resource. Borrowed backend/state objects cannot release a live callback
  context or retarget to another world.
- Diagnostic capacity is an implementation input before solving, not a value first
  discovered during readback. Map `_space_set_debug_contacts` to the required
  world-scoped `SetDebugContacts(int maximum)` hook; zero disables capture and remains
  available for failed-world cleanup. Implementations preserve the previous limit
  if preparation throws. The engine commits its cap after successful preparation,
  rejects borrowed numerical writes and suppresses unchanged assignments.
  `ReadDebugContacts(Span<Vector2>)` combines the applicable count and point-copy
  operations, returning a validated bounded prefix after synchronization. SceneTree
  supplies the sampled project cap and relinquishes it on disable/detach. This uses
  no public static debug-space wrappers and retains ADR 0100's sample semantics.
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
- Caller-defined Shape resources may supply exact supported geometry before registered
  solvers ship. A protected GetGeometryCore returns immutable ShapeGeometry data for
  circle, vertical capsule, segment, rectangle, convex contour, paired concave edges,
  separation ray or infinite boundary. Factories validate and copy authoring data;
  the runtime caches the result until protected EmitGeometryChanged invalidates it.
  GetGeometryData and complete typed accessors expose that exact immutable payload
  without solver handles; retained data survives later resource edits and disposal.
  Bounds, drawing, CPU/GPU compilation, mass, queries, contacts and replay consume
  that same representation. This is procedural resource authoring, not approximation
  of an unsupported geometry algorithm. ShapeGetType reports its actual represented
  primitive.
- Geometry authors validate input, call protected EnsureGeometryChangeAccess before
  changing private resource data, then call EmitGeometryChanged to publish the edit.
  The preflight checks all scene/raw borrowers, including disabled slots and equal
  writes, and invokes selected status hooks on the owner before mutation without
  resource locks. Coordinated worker geometry edits remain valid while all using
  worlds are quiescent; workers never invoke owner-bound status hooks. A step or
  numerical borrow rejects worker edits before private authoring.
  Publication rechecks owner/phase access without repeating fallible status hooks.
  It cannot roll back arbitrary private subclass writes; procedural subclasses must
  follow the preflight protocol. Built-in geometry writers use that same boundary.
- Custom geometry uses the same immutable ShapeGeometry value with an explicit
  format ID, bounded copied canonical bytes, finite bounds, copied diagnostic
  segment pairs and exact unit-density mass/center/polar inertia. These are
  shape-specific serialized data, not an object bag or solver handle. The author
  supplies a typed synchronous shape-pair algorithm for standalone resource
  queries; selected world implementations consume the same data for their own
  numerical queries and solving. Canonical bytes, format, bounds, mass and drawing
  participate in portable authoring compatibility; callback objects never enter
  network payloads. Authors must use a stable format and canonical encoding.
  The pinned [extension header](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/servers/physics_2d/physics_server_2d_extension.h)
  and XML expose eight built-in shape factories, not a custom_shape_create hook.
  C# CustomShapeCreate is an initialized construction projection of the existing
  Custom shape identity, typed shape data and owned RID capability; it adds no
  fictional reference census member. It requires custom geometry and copies a concrete
  CustomShape resource, avoiding an inert unconfigured factory result. Built-in
  attachment/query compilation rejects Custom before destroying existing shapes
  or transferring a body; it never substitutes a supported primitive. Custom
  geometry mutation retains RID identity, invalidates compilation and private
  checkpoints, and rejects incompatible portable correction before mutation.

### Current implementation boundary

PhysicsServerManager now registers typed factories returning fresh PhysicsServerBackend/Extension objects. Named World/Space selection exposes requested/actual names and Extension identity, with immutable lazy captures and reserved built-in choices. The complete implemented numerical boundary includes public collider/joint/history hooks and immutable configuration/observation values; query exclusions are borrowed spans. A separate public-only circle/spring consumer drives real scene and owned server spaces, priority/factory/lifetime errors, queries, observations and local/portable restore with zero warmed frame allocations. This does not close custom geometry or certify all 140 reference operation semantics; those missing or unexercised paths stay open.

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
