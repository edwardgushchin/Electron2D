# Electron2D physics decisions

Last updated: 2026-10-09

This bounded document owns executable two-dimensional bodies, shapes and areas. [The decision index](index.md) routes other domains.

<a id="adr-0054"></a>
## ADR 0054: CPU compatibility and GPU physics worlds

Last updated: 2026-10-09

- Status: Accepted
- Scope: Scene/server physics world backends, body integration and collision shapes
- Depends on: [0012](product.md#adr-0012), [0008](scene.md#adr-0008), [0014](resources.md#adr-0014), [0094](networking.md#adr-0094)

### Context

ADR 0012 selected vendored managed Box2D.NET for CPU physics. Scene/server bodies, direct queries, monitoring, joints and kinematic motion now execute. The existing GPU experiment replaces stages inside that CPU world's data model; it is not an independent GPU backend. The current objective requires two complete implementations of the applicable public contract. The [contract audit](../components/physics-contract-audit.md) and [generated declaration ledger](../coverage/physics-status.md) establish the current baseline; historical gap lists do not override current source evidence.

### Decision

- Preserve managed Box2D.NET as the CPU backend and develop an Electron2D-owned GPU backend through the already vendored SDL GPU/ShaderCross interface. Game developers explicitly choose CPU/Box2D or GPU according to their simulation and deployment goals; CPU is a first-class selectable backend. Physical computation and canvas rendering have independent selection; GPU rendering must continue to support CPU physics. Startup fallback to CPU when GPU is unavailable is a separate configurable policy, not a replacement for explicit backend choice. A failed or partially committed GPU step must not silently replay on CPU. Requested and actual backend identity and startup fallback must be observable. These selection operations are a required integration contract, not currently exposed functionality.
- Preserve public Electron2D capabilities, parameter meanings, object/RID identity, ownership, queries and promised callback/event semantics. Box2D internal APIs, storage, lists, island topology and algorithms are replaceable implementation details. Bitwise CPU/GPU equality and exact internal ordering are diagnostic tools only, never acceptance requirements or reasons to retain CPU mirrors. Backend-specific numerical methods must satisfy the same observable contract; this does not authorize dropping features or changing public units.
- The GPU backend owns persistent device state and executes broad phase, contact generation, contact/joint constraints, integration, sleeping and CCD on the device. Authoring changes, synchronous queries, direct-state callbacks, events and scene publication determine CPU traffic. Each full host mirror, bulk readback and device wait requires an identified public consumer, lifetime/freshness rule and measured cost. CPU copies maintained only to replay or validate Box2D internals must be replaced; diagnostic comparison remains opt-in. Existing experimental hooks can be reused only where they fit this boundary.
- Accept both backends through shared public API, physical-invariant, stability, lifecycle and error tests. Each numerical assertion states its units, step duration, tolerance and physical justification; identity, filtering, lifetime and promised event transitions remain exact. Exercise startup capability failure/fallback, user callbacks, partial GPU-step failure and resource cleanup. Compare complete CPU and GPU steps at one revision with identical reproducible workloads, population and features; label individual-stage controls separately. Record latency distributions, transfers/waits and warmed managed allocations. Required completion includes zero warmed engine-owned allocation and measurable GPU benefit on the target large scenes, not merely successful dispatch or isolated kernel speedups.
- Support authoritative-server games through the existing typed networking surface under ADR 0094. CPU dedicated servers run without a window, renderer or GPU device. Fixed-tick commands, portable network identities, authoritative snapshots, application/correction, client prediction/replay and remote interpolation require a common public physics contract. Keep sufficient body/contact/sleep/joint and lifecycle state to restore and replay unacknowledged input; separate predicted from confirmed events to avoid repeated effects. Creation/destruction, control-authority transfer and late join are part of that contract. CPU-server/GPU-client correction must work without cross-backend or cross-platform bitwise identity; document actual reproducibility and error guarantees separately. These networking capabilities remain required, not implemented.
- Network snapshot publication is an explicit GPU-readback consumer. Separate portable wire state from any backend-private local replay checkpoint; neither exposes vendor structures or process-local RIDs as wire identity. Bound and measure history storage, snapshot bytes, correction latency and replay cost. Accept networking through separate server/client processes with controlled latency, jitter, loss and reordering, including collisions/joints, sleep/wake, lifecycle, late join and divergence recovery. Verify a no-GPU CPU server and a real GPU client, plus a working public-API example. Window FPS, whole physics-step timing and networking/replay measurements remain separate.
- Per-body continuous detection uses shared `CCDMode` (Disabled=0, CastRay=1, CastShape=2) for RigidBody.ContinuousCD and PhysicsServer body getter/setter, with descriptor storage and a default of Disabled. Configuration survives detached/static/kinematic roles and changed modes wake dynamic bodies. CPU continuous detection inspects solved displacements through the existing owner finalization hook before pose publication, divides the world interval at potential impacts and delegates force/contact/joint response to the existing solver. Continuations preserve remaining time, nominal force/motor budgets and frame contact totals. CastRay uses a leading support point and ignores its own rotational sweep; CastShape uses complete geometry and rotational trajectories. Other bodies retain their configured modes. The independent GPU store uses the same enum and retains its device impact-interval algorithm. Full public GPU integration and cross-backend acceptance remain open.

- Keep body runtime mass/force/field policy in engine-valued types. The attachment adapter owns concrete body/world state, mass compilation, unit conversion and contact traversal. Direct views and queued callbacks qualify the adapter with a monotonically increasing attachment version; reentry to the same world/RID must not revive older views or callbacks. This boundary is implemented on the current CPU host; independent GPU binding, queries and event projection remain required.
- Keep the scene/server RID, resource, owner-thread, query, live-state, callback/event order and world lifecycle contracts common to both paths. GPU computation includes broad-phase pairs, manifolds, contact/joint constraints, integration, sleeping and continuous collision; moving graphics or particle effects alone does not satisfy the GPU world. GPU data uses packed caller-retained storage, completed submissions define publication points, and unsupported hardware retains the CPU path. Shader binaries are built offline; runtime source compilers and public backend handles are excluded. The current internal bring-up executes velocity/position integration, circle/capsule/segment/polygon manifold generation and contact/revolute/wheel constraint preparation and solving; [GPU physics implementation status](../components/gpu-physics.md) records the remaining stages and verification limits.
- Vendor all 233 C# source files of `ikpil/Box2D.NET` tag `3.1.654` at commit `5efc96def866edbb4e5a9368d84de5bf8c2dcaca` under `src/Vendor/Box2D.NET`, retaining the MIT license and [patch record](../../src/Vendor/Box2D.NET/VENDOR.md). Compile them into the single `Electron2D.dll`; make namespace-level backend declarations internal and expose no backend type through public/protected Electron2D signatures. Nullable and malformed upstream XML-comment compiler diagnostics are scoped to vendored files.
- Map the reference's `Shape2D`, `CircleShape2D`, `RectangleShape2D`, `CollisionShape2D`, `CollisionObject2D`, `PhysicsBody2D`, `RigidBody2D` and `StaticBody2D` to `Shape`, `CircleShape`, `RectangleShape`, `CollisionShape`, `CollisionObject`, `PhysicsBody`, `RigidBody` and `StaticBody`. Preserve `RigidBody : PhysicsBody : CollisionObject : Entity` and the parallel `StaticBody` branch; `CollisionShape : Entity` is a direct collision-object child that borrows its shape resource. ADR 0055 extends that child role to Area. These names remove only the redundant dimensional suffix, not inherited responsibilities.
- A `SceneTree` retains the distinct runtime worlds selected by its viewports, plus a fallback world for a viewport-free scene. Each world lazily owns one selected physics space and one logical canvas; the tree steps each distinct selected runtime once. Currently every public space uses Box2D. Direct body entry creates a backend body; collision-shape children supply geometry. Shape changes, disables and collision-layer/mask edits synchronize before the next step. Tree exit and disposal release bodies, geometry and world, while borrowed Shape resources remain caller-owned. Failed geometry validation rejects before replacing existing geometry.
- One scene unit is 0.01 Box2D meters. Default downward gravity is 980 scene units per second squared. Scene fixed physics callbacks run before a four-substep world step; body transforms and velocities synchronize back before timers, tweens and the physics-interpolation end snapshot. Geometry accepts translation and rotation with unit global scale and zero skew; unsupported scaled/skewed active physics transforms fail explicitly. This is an initial profile, not a claim that every inherited or own physics member is complete.
- The initial public body profile includes circle/rectangle dimensions and bounds, managed shape copying, collision-shape assignment and enablement, 32 collision-layer/mask bits, dynamic/static contact response, mass, gravity scale, linear/angular velocity and damping, sleep, freeze, rotation lock, central force and impulse. Other applicable members retain operation-specific Partial, Unimplemented or Blocked coverage rows.
- Large worlds with at least 256 awake backend bodies use up to four retained workers, bounded by available processors; smaller worlds and browser hosts use one. Worker tasks cover collision ranges, the existing colored constraint stages and independent per-body solved-contact snapshots without changing the four substeps or public callback lane. Threads and task storage belong to each world and are released on disposal. Contact workers read the finished solver and private receiver storage; the owner joins them and queues their changes in scene-body order before invoking user code. One-way pair state is synchronized internally; user integration/contact/area callbacks remain on the world owner. The backend uses eight-lane `Vector256<float>` arithmetic with .NET fallback and separate multiply/add operations. [The performance report](../components/box2d-performance.md) defines measured throughput and platform limits.
- Engine-owned warmed resting-contact, active-contact and moving-body fixed steps allocate zero managed bytes in the checked Linux/.NET 8 profile. The pinned Box2D.NET port needed per-world reuse of its step context and graph-color block array plus zero-overflow-contact fast exits; the applicable current-upstream buffer reuse is proposed in [ikpil/Box2D.NET#101](https://github.com/ikpil/Box2D.NET/pull/101). No allocation claim is made for unmeasured native/platform paths or user callbacks.

### Consequences

Existing CPU bodies, characters, areas, shapes, queries, joints, physical bones and shared viewport worlds remain required behavior of both implementations. The independent GPU backend, public selection and remaining applicable API are open work. The stage-hosted GPU experiment and its diagnostic hashes are retained evidence, not dual-backend acceptance. Backend extraction must remove concrete B2 body/shape/world ownership from common scene/server runtime consumers without exposing vendor types publicly. Platform and visual acceptance remain scoped to actual recorded checks.

### Rejected alternatives

- Ship Box2D.NET as a managed package or expose its types publicly: ADRs 0004 and 0012 require one Electron2D-owned managed surface.
- Add public PhysicsServer or RID placeholders around the first scene bodies: their resource-identity and direct-space contracts need a complete separate vertical slice.
- Replace the CPU compatibility backend with a GPU-only solver: unsupported devices and headless CPU execution must remain supported.

<a id="adr-0055"></a>
## ADR 0055

This active decision is maintained in [physics-monitoring.md](physics-monitoring.md#adr-0055).

<a id="adr-0056"></a>
## ADR 0056

This active decision is maintained in [physics-fields.md](physics-fields.md#adr-0056).

<a id="adr-0057"></a>
## ADR 0057

The active force decision is maintained in [physics-forces.md](physics-forces.md#adr-0057).

<a id="adr-0058"></a>
## ADR 0058

This active decision is maintained in [physics-monitoring.md](physics-monitoring.md#adr-0058).

<a id="adr-0059"></a>
## ADR 0059: Capsule collision resource and fixture geometry

Last updated: 2026-09-30

- Status: Accepted
- Scope: Concrete capsule Shape resource for existing body and area fixtures
- Depends on: [0054](#adr-0054), [0055](#adr-0055), [0013](resources.md#adr-0013)

### Context

The first scene-body profile supports circle and rectangle fixtures. A vertical capsule is a common game-body shape and the next geometry prerequisite for kinematic movement. The pinned resource links radius, total height and middle height; the vendored backend accepts capsules but returns a null shape ID when the segment between cap centers does not exceed its linear slop. Passing that ID to existing fixture ownership would invalidate collision scans.

### Decision

- Add `CapsuleShape : Shape` with default radius 10, full height 30 and derived middle height 10. Radius growth raises height to at least the diameter; height reduction lowers radius to at most half height. Setting middle height derives the full height and emits a resource change even for an equal value. Equal radius/height writes do not emit. Accept finite nonnegative dimensions, including circle and zero-radius segment limits; reject overflow before changing coupled state.
- Keep exact resource dimensions, local bounds and independent Resource copying. Direct CollisionShape children borrow a capsule and rebuild body or Area fixtures on edits using the existing geometry revision contract. Rotate the cap-center segment with the child's local rotation; convert scene units to backend meters by 0.01.
- Represent zero and sub-slop middle segments as circles in the backend, while keeping exact public values and bounds. At the current backend scale the threshold is 0.005 m, or 0.5 scene units. This is an accepted solver-tolerance adaptation for collision response; above the threshold use an actual capsule fixture. Never retain the backend's null shape ID. Reuse the existing no-allocation fixture and area paths.

### Consequences

Rigid and static bodies gain capsule contact response, and areas gain capsule sensors and field geometry. The public type adds no backend handle and uses the existing borrowed-resource lifetime. Managed tests cover linked dimensions, degenerate values, rotation, live edits, packing and warmed allocation. Separation-ray floor behavior, native allocator accounting, other platforms and owner acceptance remain separate work.

<a id="adr-0060"></a>
## ADR 0060: Fixed-step kinematic platform motion

Last updated: 2026-09-30

- Status: Accepted
- Scope: AnimatableBody scene role and synchronized kinematic movement
- Depends on: [0054](#adr-0054), [0059](#adr-0059), [0008](scene.md#adr-0008)

### Context

Static bodies constrain a rigid body but teleport when their scene transform changes, so a moving platform has no contact velocity. The pinned AnimatableBody2D inherits StaticBody2D, uses a kinematic backend body, estimates linear/angular velocity from manual movement, and defaults to delaying the scene transform until the next physics frame. Box2D.NET offers kinematic bodies and a target-transform operation that derives both velocities for a fixed step. The direct sweep and CharacterBody movement contracts are now defined under ADRs 0063 and 0067.

### Decision

- Allow `StaticBody` inheritance and add `AnimatableBody : StaticBody` with stored `SyncToPhysics=true`. It borrows existing collision shapes and the inherited surface material. Its backend body is kinematic with zero mass: contacts and external forces cannot displace it, but movement affects dynamic bodies through contact velocity.
- After node physics callbacks and before the world step, send the latest finite unit-scale, zero-skew target to the backend for that step. With synchronization enabled, a caller's local transform edit records the global target, restores the last solved scene transform, and presents the backend result after the step. Zero delta does not consume the target. With synchronization disabled, the scene transform changes immediately while the backend reaches it during the next step. Disabling synchronization with a pending target presents that target immediately. Preserve the exact derived node type and flag in PackedScene.
- Keep the existing body fixture and material synchronization, but avoid teleporting this kinematic backend when its scene transform changes. A shared internal transform hook leaves rigid/static behavior intact. Clear pending movement on tree exit; reentry captures the current scene pose. Invalid scaled/skewed targets reject after restoring the prior scene pose. A throwing user transform callback does not erase a valid pending target. Warm active and idle kinematic paths reuse existing backend storage and allocate zero managed bytes in the checked Linux/.NET 8 profile.

### Consequences

Games can animate moving platforms and doors that push or carry dynamic bodies, using typed scene transforms. CharacterBody now follows moving platforms and consumes kinematic sweeps under ADR 0067. Native allocation, other platforms and owner visual acceptance remain unverified.

<a id="adr-0061"></a>
## ADR 0061: Two-sided segment fixtures and zero-area body mass

Last updated: 2026-09-30

- Status: Accepted
- Scope: SegmentShape resource and executable static, dynamic and area fixtures
- Depends on: [0054](#adr-0054), [0059](#adr-0059), [0013](resources.md#adr-0013)

### Context

Circle, capsule and rectangle fixtures cannot form a zero-width sloped terrain edge. The pinned SegmentShape2D stores two independent local endpoints, emits only on a changed endpoint, and reports their tight bounds. Box2D.NET provides a two-sided segment fixture, but rejects an endpoint separation at or below its linear slop. A segment has zero area in that backend, so a dynamic body with only segments otherwise receives zero mass and cannot advance under gravity.

Joint2D/PinJoint2D was compared as another gameplay slice. Its public RID now projects shared joint/body/world identity under ADR 0087; the current Box2D revolute API has no verified direct mapping for Joint2D positional `bias` or PinJoint2D linear-anchor `softness`; its angular spring controls a different degree of freedom. Typed kinematic sweeps need an owned result with stable shape indices and an initial-overlap recovery rule. These exact member triggers remain in coverage while independent segment collision can execute now.

### Decision

- Map SegmentShape2D to `SegmentShape : Shape` with A=(0,0), B=(0,10), exact tight local bounds, independent Resource copying and finite endpoints. Reject values whose endpoint subtraction would overflow before mutation. An equal endpoint assignment has no change event. Rotate and translate both endpoints through a direct CollisionShape child before creating a two-sided backend fixture.
- Represent zero or sub-slop segments with a zero-radius point fixture at the midpoint, while retaining the exact public endpoints and bounds. Never keep the backend's null segment ID. At the current unit scale, its 0.005 m slop is 0.5 scene units. The same resource can be borrowed by static/dynamic bodies and areas.
- For dynamic bodies whose fixtures all have zero area, retain the requested positive mass. Segment mass is distributed by segment length; the center is length-weighted and rotational inertia follows the thin-rod and parallel-axis formulas. A body without fixtures keeps its requested mass with zero inertia. Reject mass/inertia whose inverse exceeds the finite solver range before backend mutation. Warmed unchanged segment contact/area paths allocate zero managed bytes on the checked Linux/.NET 8 profile.

### Consequences

Games can construct two-sided terrain edges and line sensors and attach segments to moving bodies. Live edits, rotation, short/point limits, resource copying, scene packing and dynamic mass/inertia have executable checks. Compound chain geometry, joint solver tuning/debug drawing, separation-ray participation, native allocator accounting, other platforms and owner acceptance remain separate work.

<a id="adr-0062"></a>
## ADR 0062: Compound convex collision fixtures

Last updated: 2026-09-30

- Status: Accepted
- Scope: Convex polygon resource, point-cloud hull and multiple fixtures per CollisionShape
- Depends on: [0054](#adr-0054), [0061](#adr-0061), [0013](resources.md#adr-0013)

### Context

Existing circle, capsule, segment and rectangle fixtures cannot reproduce an arbitrary solid convex body. The pinned convex polygon resource stores perimeter points, accepts either winding and computes a hull from point clouds. Box2D.NET limits one polygon fixture to eight vertices, while the public contour has no eight-vertex limit. The previous Shape-to-fixture method returned only one ID, so a shape with more vertices could not be represented without dropping geometry or splitting its public identity.

### Decision

- Map ConvexPolygonShape2D to `ConvexPolygonShape : Shape`. `Points` defaults empty, returns/accepts caller-owned arrays, retains perimeter order and may repeat the first vertex at the end. Assignment emits a resource change even for equal contents; empty input removes all fixtures. A typed `ReadOnlySpan<Vector2>` point-cloud method uses the existing convex-hull operation and stores its closed contour. Validate finite, nondegenerate convex perimeter input, including edge crossings and backend-size limits, before committing a new resource revision.
- Let each Shape append zero, one or several internal fixture IDs to the existing body/area owner list. The four existing concrete shapes still append one. Split a larger convex contour into a nonoverlapping fan of pieces with at most eight vertices each, sharing only seam edges. Precompute/validate all local hulls before resource mutation, then apply each child's position/rotation to those hulls on fixture rebuild. The body or area still borrows one public Shape resource, and object-level events deduplicate backend pieces by owner.
- Keep copies of public points and internal hull pieces independent across Resource duplication. Multiple dynamic fixtures contribute area-derived mass/inertia through the existing body mass policy. Warmed unchanged contact and area scans reuse fixture storage and allocate zero managed bytes on the checked Linux/.NET 8 path. No vendored source is changed.

### Consequences

Games can collide and sense with a solid convex contour containing more than eight vertices; direct clockwise/counterclockwise assignment and point-cloud hull generation are executable. `CollisionPolygon` and concave polygon decomposition can reuse the compound-fixture mechanism in later slices. Native allocator counts, other platforms and owner visual acceptance remain unverified.

<a id="adr-0063"></a>
## ADR 0063: Shared RID identity and world-scoped physics queries

Last updated: 2026-10-05

- Status: Accepted
- Scope: Public identity, ownership and access model for 2D physics server resources and direct queries
- Depends on: [0001](product.md#adr-0001), [0004](product.md#adr-0004), [0008](scene.md#adr-0008), [0054](#adr-0054), [0028](rendering.md#adr-0028)

### Context

Before this slice, the scene ran an internal Box2D world but had no public RID, PhysicsServer, World or direct-space state. The applicable reference query parameters exclude RIDs and may select a shape by RID; results identify a collider by both object and RID, plus a shape index. Server-created physics resources need an identity even without scene nodes. A scene-object-only query surface would leave those contracts and later renderer/navigation RID consumers unresolved.

### Decision

- Introduce one public opaque, backend-neutral RID value type shared by server domains. Its default/zero value is empty; identity is session-local and never exposes a Box2D ID. The owning server validates resource kind and liveness separately from the value type's nonzero validity test. Released IDs must not resolve to a later resource after internal slot reuse. Fixture rebuilds do not change the owning collision object's RID.
- Name the public two-dimensional server `PhysicsServer`, its live direct-space view `PhysicsDirectSpaceState`, and the shared scene world resource `World : Resource`. CanvasItem exposes that resource through `GetWorld()`. Name the caller-owned motion-contact snapshot `KinematicCollision : ElectronObject`; PhysicsBody motion queries and CharacterBody slide queries use that same type. Typed query parameters and results use the `PhysicsPointQueryParameters`, `PhysicsPointResult`, `PhysicsRayQueryParameters`, `PhysicsRayResult`, `PhysicsRestInfo`, `PhysicsShapeQueryParameters`, `PhysicsShapeResult`, `PhysicsTestMotionParameters`, and `PhysicsTestMotionResult` names. These renames preserve constructors, readonly result values, defaults, ownership, signatures and behavior; dimensional suffixes remain only in pinned coverage identities. The shorter names do not add a 3D physics domain; their existing typed methods, nested `BodyMode`, ownership and RID behavior remain unchanged. Update consumers, XML, class pages and coverage mappings together; do not retain duplicate public compatibility types.
- PhysicsServer owns the physics resource registry and real spaces, bodies, areas and shapes. Register the existing SceneTree physics world as a server space; scene membership and teardown retain the lifetime of scene-owned resources. Explicitly created server resources have an executable creation/use/free lifecycle. The scene and server access the same solver state; no parallel scene-only physics world is introduced.
- Expose each selected world through World.Space, World.DirectSpaceState, World.Canvas, Viewport.World, Viewport.FindWorld and CanvasItem.GetWorld. Ordinary viewports start with independent worlds; assigning another viewport's World shares both canvas and solver state. Null resets an independent default world, matching the pinned setter. A viewport-free scene uses one scene fallback world. NavigationMap owns the lazy active authored-region map under ADR 0097; baking/avoidance/query extensions retain their own dependencies. A direct-space state is a view of that live space, not a second world.
- World runtime identity survives disposal/recreation of a bound wrapper and resource duplication/copying never clones solver state. Scene-default runtime identities expire at owning tree teardown, including borrowed duplicates. Explicit caller worlds retain identities until their final resource reference and scene lease are released. A runtime world has one scene driver; binding it to another simultaneous SceneTree rejects before mutation. Spaces already created on another thread reject scene binding. Replacing Viewport.World moves bodies/areas/joints without changing collision RIDs or shape owners; callbacks follow committed assignment, and all removal/addition stages are attempted. CanvasItem.NotificationWorldChanged (36) follows committed transitions; failed collider attachment is retried before later physics steps after invalid geometry is corrected. CopyFromResource cannot replace the runtime identity of a bound target; use the viewport association instead.
- Keep ordinary gameplay object-oriented: scene nodes and typed ray/shape query objects expose collider references where available. Direct query parameters also retain RID exclusions and shape RID selection; typed C# result values retain collider RID and stable shape-owner index even when no scene CollisionObject exists. Specify each operation's no-hit, ordering, copy and maximum-result behavior in its implementing slice. Do not add Variant, dynamic dictionaries, public Box2D types or backend IDs.
- Build the public server and query layer in connected executable slices: shared RID/space/body/shape lifetime, world access, ray/point queries, direct shape sweeps and scene query nodes, then shape-index events and remaining applicable server methods. RayCast and ShapeCast consume the same direct-space view; other absent declarations retain their own implementation gates. An accepted architecture does not mark any absent declaration Implemented. Attached queries respect scene owner-thread and backend world-lock boundaries.
- The first executing slice registers each CollisionObject RID for its managed lifetime and each SceneTree's existing PhysicsSpace as one server space. Fixture tags carry collider RID and direct shape-owner index through compound shape rebuilds. Server-created spaces, bodies, Areas and the six existing Shape families attach to the same solver; explicit spaces start inactive and use typed host stepping after SpaceSetActive(true), under [ADR 0089](physics-activity.md#adr-0089). World exposes Canvas, Space and DirectSpaceState, and CanvasItems share the view selected by their nearest viewport. Typed ray/point parameters copy RID exclusions and results carry RID, optional scene collider, instance ID and shape index. A direct query prepares pending scene geometry before searching and rejects off-owner or in-step access.
- Scan the current fixture lists for ray, point and shape queries so a collider with collision mask zero remains eligible by its layer. The backend world query's reciprocal filter would incorrectly remove it. Ray results choose nearest fraction with RID/index tie order; point and shape results deduplicate by owner and apply their cap after RID/index ordering. Shape queries accept caller-owned resources or live shape RIDs. Resource assignment lazily registers a borrowed server RID; owner disposal invalidates it, while geometry edits mark server fixtures dirty before a later query. `IntersectShape` includes swept intersections. `CastMotion` ignores initial overlap and brackets the first new hit with eight refinements. `CollideShape` returns ordered query/collider point pairs and `GetRestInfo` selects the deepest contact with point velocity. Result records remain typed and keep RID/index even for server-only colliders. The current path is linear in fixture count, with a future broad-phase optimization gated by measured large-world cost. Point canvas-instance filtering, viewport world transitions, joint solver tuning/debug drawing and wider server methods keep explicit coverage gaps; no vendored source changes are needed.
- ShapeCast borrows its configured Shape, converts its local target to global motion, casts once to find the earliest safe/unsafe bracket, and gathers typed rest contacts at that impact pose. Each returned collider RID is excluded from later rest queries until `MaxResults` is reached. Zero local motion queries the current pose and retains zero fractions. Leaving a collision parent removes its automatic RID exclusion in both scene query nodes. Automatic sampling runs in the internal fixed physics lane; a forced update can run while disabled. Options and borrowed shape survive PackedScene, while contact snapshots and explicit exceptions do not. The node reuses query and exclusion storage on warmed frames. Debug rendering and virtual tile collider projection keep separate exact coverage gaps.
- Test motion for scene and server body RIDs in their registered space, using a supplied finite unit-scale global pose, displacement, recovery margin and copied RID/instance exclusions. Scan current body fixtures with reciprocal collision filters; Areas do not block. Recover initial penetration in up to four steps, then bracket the first new impact with eight sweep refinements. A deep residual overlap stops motion at zero safe fraction. Return typed collider/local shape-owner indices, point, outward normal, depth, point velocity, safe/unsafe fractions, travel and remainder. A completed miss clears stale contact fields. Scene `TestMove` and test-only `MoveAndCollide` leave the pose unchanged; regular `MoveAndCollide` applies safe travel. Separation-ray participation remains blocked until its shape resource exists. Reuse candidate storage and indexed fixture scans so warmed unchanged scene tests allocate no managed bytes.
- Store each body collision exception as a one-sided, insertion-ordered RID entry. A pair is excluded when either body lists the other's RID, both in solver contacts and body motion tests; Area sensing and direct ray/point/shape queries retain their own filtering contracts. Scene methods take typed PhysicsBody nodes, while server methods accept arbitrary target RIDs (including empty or later freed values) and require a live body owner. Scene enumeration copies the list and projects server-only or freed entries as null slots. A list edit marks the owner's fixtures dirty so existing backend contacts are recreated with the correct pre-solve setting before the next step or query; merely toggling the flag on a live fixture does not update a contact's cached flag. Owner disposal/free drops its list. Keep the shared registry lock on the small per-body list until large-world contact profiling shows a throughput problem; no vendored code change is needed.

### Consequences

Game code can query its current world through typed scene access, scene query nodes or direct body motion without manually managing RIDs, while advanced code can create and test resources through PhysicsServer, including server-only colliders in the SceneTree space. RID numeric values are not reused, and a freed RID no longer resolves at its owning server. PhysicsQueryTests checks shared identity and lifecycle; PhysicsShapeQueryTests checks direct sweeps; PhysicsMotionTests checks body movement; PhysicsCollisionExceptionTests checks unilateral scene/server entries, live contact edits, motion filtering, stale targets and warmed allocation; RayCastTests and ShapeCastTests check scene snapshots. Native allocation accounting, other platforms, virtual tile collision owners, physics debug drawing and owner acceptance remain unverified.

### Rejected alternatives

- Replace RID parameters and results with only CollisionObject/Shape references: server-created resources have no required scene object, and this would remove applicable API under ADR 0004.
- Add a physics-only PhysicsRID: it duplicates the reference's cross-server identity role needed by rendering and navigation.
- Publish inert RID/PhysicsServer placeholders or wait for every server method before the first query: either choice delays an executable, auditable physics-query slice without changing its required ownership contract.

<a id="adr-0064"></a>
## ADR 0064: Hollow paired-segment collision resource

Last updated: 2026-09-30

- Status: Accepted
- Scope: ConcavePolygonShape resource and hollow multi-segment body/area fixtures
- Depends on: [0061](#adr-0061), [0062](#adr-0062), [0013](resources.md#adr-0013)

### Context

SegmentShape supplies one two-sided edge, while a level boundary or line sensor needs several independent edges in one reusable resource. The pinned ConcavePolygonShape2D stores an array of endpoint pairs and has no solid interior, even if those pairs enclose an area. The current internal Shape contract can append several fixture IDs to one body or area. Box2D.NET rejects short segment fixtures, so the same numerical fallback used by SegmentShape is required for each pair.

CollisionPolygon2D was compared as the adjacent scene-node feature. ConvexPolygonShape and ConcavePolygonShape supply its two build geometries. ADR 0066 now owns the direct scene slot and conversion; ADR 0065 supplies one-way body contacts. The one-way margin now executes in typed body sweep recovery under ADR 0063.

### Decision

- Map ConcavePolygonShape2D to `ConcavePolygonShape : Shape` with an empty default and a caller-owned `Vector2[] Segments` copy boundary. Require an even number of finite endpoints with finite per-pair and overall bounds. Each consecutive pair defines one independent edge; empty input removes all fixtures. Assignment emits `Changed` even for equal content. `GetRect()` spans every stored endpoint and returns default when empty. Resource duplication copies the array independently.
- Reuse the internal multiple-fixture owner lists for bodies and areas. Transform each endpoint pair by its direct CollisionShape child, then create a two-sided segment or zero-radius point fixture using the SegmentShape backend tolerance rule. The public resource remains hollow; an object fully inside a closed set of edges is not reported as overlapping until it touches an edge. Body/area object events still deduplicate by owner.
- Keep the dynamic zero-area mass/inertia policy from ADR 0061 for bodies that borrow paired segments, while documenting that hollow multi-segment shapes are primarily level/static geometry. Warmed unchanged body/area scans allocate zero managed bytes in the checked Linux/.NET 8 profile. No vendored source is changed.

### Consequences

Games can use reusable multi-edge terrain, open contours and hollow line sensors. The geometry and owning resource are executable; ADR 0066 integrates the separate CollisionPolygon scene node. Native allocator accounting, other platforms and owner acceptance remain unverified.

<a id="adr-0065"></a>
## ADR 0065: One-way scene-body contacts

Last updated: 2026-10-09

- Status: Accepted
- Scope: CollisionShape one-way flag and local direction on scene physics bodies
- Depends on: [0054](#adr-0054), [0061](#adr-0061), [0008](scene.md#adr-0008)

### Context

Two-sided fixtures prevent a body from passing through a platform and landing on its opposite face. The pinned scene shape permits a local one-way direction, while the selected backend provides a pre-solve callback for awake dynamic-body contacts. The pinned rigid-body pair chooses the valid side when contact first appears and retains that decision until separation. Its one-way margin participates in kinematic motion/recovery queries, not in the rigid-body contact-side test.

### Decision

- `CollisionShape.OneWayCollision` defaults to false and `OneWayCollisionDirection` defaults to `(0, 1)`. A finite nonzero direction is normalized before mutation; zero rejects contact from every side when the flag is enabled. Direction rotates with the shape's local pose and its parent body. Area children retain these scene properties for packing but remain sensor-only and warn that one-way response does not apply.
- Mark only enabled body fixtures for Box2D pre-solve. Per-fixture user data carries the local contact direction; the world callback compares the first contact normal to its current body rotation. Keep the initial allowed/denied decision for the shape pair until no contact is observed. Fixture rebuilds change backend IDs and therefore discard old pair decisions without changing public node identity. Large worlds use the retained task system from ADR 0054. One-way pair updates are serialized within that world; callback state remains internal, and gameplay callbacks execute on the owner after solver tasks finish.
- The independent GPU store retains the same initial side decision per shape/piece pair in device-only generation/revision-qualified history, including rejected contacts. Completed manifold batches publish a new history table; complete capacity recovery never truncates or advances the world. Sensors bypass side selection. CCD consumes this history and, for rotating pass-through episodes, schedules a separation boundary before later recontact so the old decision is retired before a new side is selected. Public scene/server adapters and GPU motion-query recovery remain required integration work.
- Expose finite nonnegative `OneWayCollisionMargin`, default one scene unit, on CollisionShape and CollisionPolygon. Typed body motion accepts initial recovery against the solid side only up to the larger of this value and the query's safe margin; a deeper one-way overlap is ignored. Fixed-step pre-solve continues to use the established side decision. The value survives PackedScene, and edits rebuild fixture tags before a later motion query. No vendored source changes are needed.

### Consequences

Static, kinematic and dynamic scene-body fixtures accept contacts from the configured side and allow traversal from the other side. Existing contact reports see only solved contacts, and Area monitoring continues to sense crossings. OneWayCollisionTests verifies fixed-step side selection; PhysicsMotionTests verifies sweep direction, margin depth and recovery. Native allocator counts, other platforms and owner visual acceptance remain unverified.

<a id="adr-0066"></a>
## ADR 0066: Direct scene collision polygons

Last updated: 2026-09-30

- Status: Accepted
- Scope: CollisionPolygon scene node, solid convex decomposition and closed hollow edges
- Depends on: [0008](scene.md#adr-0008), [0054](#adr-0054), [0062](#adr-0062), [0064](#adr-0064), [0065](#adr-0065)

### Context

Games need an editable concave or convex polygon child directly under a physics body or Area. The two resource geometries and one-way body-contact callback exist, but CollisionObject previously accepted only CollisionShape children. Nesting a hidden CollisionShape under the polygon would not provide a direct owner and would introduce scene state that users did not request. C# cannot declare a nested enum and a property with the same `BuildMode` identifier in one class.

### Decision

- Map the reference node to `CollisionPolygon : Entity` as a sibling of CollisionShape. The internal `ICollisionGeometry` child contract now feeds the shared CollisionObject owner registry under [0071](#adr-0071), with one owner group and one logical slot per generated resource. Solid parts may therefore have distinct global shape indices while all resolve to the same polygon child; compound native fixtures within one resource retain one index. Name the typed enum `PolygonBuildMode` with Solids=0 and Segments=1, preserving the public `BuildMode` property name.
- Copy the caller's finite local `Polygon` array and validate finite overall bounds before mutation. Empty/insufficient or undecomposable contours remain editable and contribute no fixture in the affected mode, with configuration warnings. Solid mode uses `Geometry.DecomposePolygonInConvex` and owned ConvexPolygonShape resources for every part; Segments mode closes the contour into consecutive endpoint pairs in one owned ConcavePolygonShape. Both modes reuse the existing resource-to-Box2D fixture path, child translation/rotation and pre-rebuild scale/skew validation.
- Rebuild live fixtures for contour, mode, disabled and transform edits. A body child uses the existing one-way flag/direction pre-solve behavior and the one-way margin in typed body-motion recovery; Area remains a two-sided sensor and warns when one-way is enabled. The node owns/disposes generated resources and stores the typed contour/mode/options in PackedScene. A warning subscriber failure after a valid setter does not prevent the next fixture rebuild.

### Consequences

One node now supplies solid concave terrain, hollow closed boundaries or sensors without a hidden scene child. CollisionPolygonTests checks array ownership, errors, malformed editable contours, solid/segment mode, concave missing space, body contact, one-way traversal, live edits, warning failure, PackedScene and 64 warmed contact frames without managed allocation on Linux/.NET 8. Native allocator counts, other platforms and owner visual acceptance remain unverified.

<a id="adr-0067"></a>
## ADR 0067: Character sliding and platform following

Last updated: 2026-09-30

- Status: Accepted
- Scope: Caller-driven CharacterBody grounded/floating motion and typed slide snapshots
- Depends on: [0008](scene.md#adr-0008), [0054](#adr-0054), [0056](#adr-0056), [0060](#adr-0060), [0063](#adr-0063), [0065](#adr-0065)

### Context

The shared PhysicsBody motion test, typed KinematicCollision result, RID exceptions and kinematic backend exist. Games still lack the spatial character role that turns desired velocity into floor/wall/ceiling classification, repeated slide motion, floor snap and moving-platform following. The selected backend permits kinematic target motion but direct queries must see a character's new scene pose before the following solver step. A query that ignores an initially touching inclined segment can tunnel through that slope.

### Decision

- Map the spatial character to `CharacterBody : PhysicsBody`, keeping the Entity and CollisionObject branches intact. Keep `MotionMode` and `PlatformOnLeave` as properties; name their typed C# enum types `CharacterMotionMode` and `CharacterPlatformOnLeave` because C# cannot give a nested enum and property the same identifier. Preserve numeric enum values, applicable defaults and PackedScene state. Do not store transient contacts, flags or platform RID in PackedScene.
- Reuse the current server body-motion test for each slide without constructing a public result per fixed frame. In grounded mode classify normals against normalized UpDirection and FloorMaxAngle, apply stop/constant-speed/block-on-wall/ceiling controls, and snap a previously grounded body down only when not facing upward. Floating mode treats contacts as walls and applies WallMinSlideAngle. MaxSlides bounds the loop; copied KinematicCollision objects expose individual and last contacts. Desired Velocity remains caller-owned; the body reports Area/world gravity but game code chooses whether to add it. Validate finite motion and public options before mutation.
- Sample the last floor or wall body's point velocity and collision layer before movement. Floor/wall layer masks gate carry; movement caused by a platform excludes that platform RID, then a departure policy adds all, only upward, or none of its velocity to Velocity. Keep these operations on the space owner thread and use the existing body/Area field and RID lifetimes.
- After character scene movement, direct queries prepare its backend fixture at the new pose immediately. Retain the previous solved pose separately; before the next fixed step reset a temporarily prepared query pose and send the final scene pose through the backend kinematic target operation. This preserves contact velocity and same-frame query identity. The current registered-body scan for platform point velocity is linear; replace it only after measured large-world cost. Fix the shared body-motion kernel to report initial contact when movement points inward along a touching slope rather than skipping that pair. Warmed unchanged movement reuses candidate, platform-exclusion and slide-result storage.
- Include separation rays through the shared contact kernel under [0068](#adr-0068): recovery always considers them, sliding rays participate in motion, and floor snap explicitly includes non-sliding rays. CharacterBody's own class, movement and snap contracts have executable evidence; inherited virtual tile collision-object gaps remain on their declaring result types.

### Consequences

Caller-driven grounded and floating characters can move along current shapes, snap to floors, follow moving floors and walls, inspect typed contacts, and read selected gravity without public backend types. CharacterBodyTests covers default/invalid/packed state, floor/wall/ceiling and floating movement, slope/ceiling/wall controls, slide caps, platform masks/leave policies, Area gravity, immediate queries, fixed-lane sync and warmed allocation. PhysicsMotionTests guards the touching-slope regression; SeparationRayShapeTests checks ray sliding and snap. Native allocation, other platforms, large-world throughput and owner visual acceptance remain unverified.

### Rejected alternatives

- Use StaticBody teleportation for scene movement: it loses kinematic contact velocity and leaves the backend at an old pose during a same-frame query.
- Add a second character-only collision world: it would split RID identity, shape owners, masks and Area fields from the shared SceneTree space.

<a id="adr-0068"></a>
## ADR 0068: Directed separation rays in queries, body motion and solver contacts

Last updated: 2026-10-09

- Status: Accepted
- Scope: SeparationRayShape resource, directed sensing, shared body-motion and dynamic contact
- Depends on: [0054](#adr-0054), [0061](#adr-0061), [0063](#adr-0063), [0067](#adr-0067)

### Context

Character floor behavior requires a directed ray resource. The current Box2D.NET pre-solve callback receives a point/normal and returns a contact veto; it cannot supply an alternative manifold. A regular solver segment would produce wrong slope, containment, query visibility and inertia behavior.

### Decision

- Expose Length=20 and SlideOnSlope=false. Accept finite nonnegative length within the backend squared-distance range; zero contributes no contact. Preserve padded drawing bounds, independent copies and exact indexed geometry metadata.
- Use a zero-density fixture for registration, retaining sensor behavior only for Areas. Supply a directed alternative manifold before body constraint creation through an internal per-shape callback; preserve native material, warm-start, sleep, filtering and report handling. Route direct shape queries, Area overlap scans and body motion through one directed native ray-cast kernel. Ray/point queries exclude these fixtures. Reject containment, back-facing hits and ray-ray pairs. Extend margin and positive axial motion along the ray; sliding follows the surface normal, otherwise separation opposes the axis. The swept ordinary convex region is the union of initial/final primitives and swept edges, avoiding the eight-vertex hull ceiling.
- Include rays in recovery regardless of the flag. Include sliding rays in motion automatically; non-sliding rays require CollideSeparationRay, which snap enables. Preserve reciprocal filters, exceptions, exclusions, shape indices and point velocity. Reconstruct the collider contact point from the manifold, including ordinary contacts. A touching ray moving outward does not block motion. Rays do not receive thin-rod inertia.
- The CPU-hosted GPU stage experiment retains custom manifold generation on the host and uploads those constraints explicitly; this is not independent GPU acceptance or a failure fallback. The independent GPU store owns device ray geometry. Public independent-GPU binding remains required under ADR 0054.

### Consequences and verification

Character ray floors, ray-specific body tests and direct/Area sensing execute in the registered world. SeparationRayShapeTests checks every existing shape family, forward/reverse casts, short/zero rays, policy, copying/server state, recovery, snap and 64 warmed body queries with zero managed allocation. SeparationRayDynamicsTests verifies six-family forward/reverse contacts, compound containment, slope normals, friction/restitution, coupled mass/impulses, zero geometric inertia, sleep, geometry edits, one-way/CCD and scene contact events. The active CPU and CPU-hosted GPU stage probes allocate zero all-thread managed bytes across 128 measured frames after 128 warmup frames. Native allocation, other platforms and owner visual acceptance remain unverified.

### Rejected alternatives

- Solid segment fixtures: their two-sided manifold, query visibility and rod inertia change the contract.
- Correct positions after the native solver as a substitute for constraints: coupled impulses, friction, sleep and reports would remain missing.

<a id="adr-0069"></a>
## ADR 0069: Standalone resource collision regions and boundary contacts

Last updated: 2026-09-30

- Status: Accepted
- Scope: Shape.Collide, CollideWithMotion and the two contact-array variants
- Depends on: [0054](#adr-0054), [0061](#adr-0061), [0064](#adr-0064), [0068](#adr-0068), [0014](resources.md#adr-0014)

### Context

Direct-space queries execute against registered scene/server fixtures. Resource-pair collision methods must also work without a scene or space. The pinned Shape2D implementation delegates these methods to an internal shape-collision entry point and caps contact arrays at sixteen pairs. Its SAT solver projects each shape's motion independently: the operation tests the two swept regions, not a synchronized relative-velocity cast. Concave terrain motion is ignored; a separation-ray pair uses only the ray's positive axial motion. Those are distinct contracts from direct-space motion queries. The internal server entry point is absent from the pinned public PhysicsServer2D table, so it does not require a new public server member.

### Decision

- Expose all four typed Shape methods using explicit translated/rotated, unit-scale, zero-skew poses and global-axis displacements. Validate finite inputs, live resources and transformed geometry. Queries read caller-owned resources without creating RIDs or temporary worlds and do not mutate them or their scene borrowers. Concurrent resource mutation/disposal is outside the query contract.
- Use original scene-unit dimensions for primitive support geometry, avoiding round-trip rounding at exact touching. Build each convex swept hull from its original and displaced vertices and retain the linked circle/capsule radius. Test edge and rounded-corner separating axes. Reuse the span hull builder behind Geometry.ConvexHull and double orientation intermediates. A full ConvexPolygonShape contour stays one collision region regardless of backend fixture partitioning; concave resources remain hollow segment collections with the accepted short-segment point fallback.
- Return global boundary pairs in caller/other order, capped at sixteen. The difference from the first point to the second gives separation normal and depth. Retain deeper pairs when capacity is reached; exact edge touching may return true without a nonzero separating contact. Arrays are caller-owned; empty results share an empty array. Reuse private per-thread proxy and hull buffers after warmup; no user resource is retained by those buffers.
- Preserve the pinned special pairs: two concave resources or two rays do not collide; concave motion is ignored in either operand. Ray pairs reject containment and use only the ray's own axial motion, ignoring counterpart motion. Reuse the directed native kernel for native-sized target hulls and clip the full convex boundary above that hull limit. Choose the nearest surface crossing across concave pieces.
- Leave Shape.Draw and CustomSolverBias Blocked by renderer RID drawing and verified per-shape contact-softness integration. Standalone collision does not supply those prerequisites; ordinary directed body response is owned by ADR 0068.

### Consequences and verification

Games can test reusable geometry for placement and procedural tools without entering a SceneTree. ShapeCollisionTests checks contact order/depth, containment, touching, independent simultaneous motions, all ordinary family pairings, full convex boundaries, hollow/special pairs, sixteen-deepest-pair retention, invalid/disposed inputs, geometry committed before a throwing Changed callback and off-thread use. Sixty-four warmed active, full-contour and empty-contact queries each allocate zero managed bytes on Linux/.NET 10; successful contact arrays allocate caller-owned output. Native allocation, large-world performance, other platforms and owner visual acceptance remain unverified.

### Rejected alternatives

- A relative-motion cast or an earliest-time contact snapshot: neither represents independently swept resource regions.
- Create a temporary physics world or register colliders for every pair: lifecycle and allocation would be unrelated to this resource-only operation.
- Return contacts from partitioned convex fixtures: internal partition edges are not the resource boundary.

<a id="adr-0070"></a>
## ADR 0070: Live body state and post-solver integration callbacks

Last updated: 2026-10-09

- Status: Accepted
- Scope: PhysicsDirectBodyState, RigidBody custom integration and typed body callbacks
- Depends on: [0054](#adr-0054), [0056](#adr-0056), [0057](#adr-0057), [0058](#adr-0058), [0063](#adr-0063), [0014](resources.md#adr-0014)

### Context

The engine exposes forces, selected Area fields and object-level contacts, but games cannot customize an active body's integration through a live typed view. The pinned body force callback executes after solving and precedes state synchronization. Explicit state IntegrateForces adds gravity before damping; ordinary force integration damps before adding gravity/forces. A pre-solver callback or copied inert state would change those contracts.

### Decision

- Map PhysicsDirectBodyState2D to an engine-created PhysicsDirectBodyState with no public constructor. Cache one wrapper per native body attachment, validate owner thread and non-stepping access, and invalidate it permanently on detachment/replacement/free. Off-callback consumer disposal allows wrapper recreation; borrowed callback disposal rejects. Views own no native body or world and retain contact values/RIDs rather than scene object ownership.
- Expose actual native velocity, pose, center offsets, inverse mass/inertia, sleep and filters. CenterOfMass is a global-axis offset; CenterOfMassLocal is local. Native inertia/torque/impulse units convert to scene units. Force positions are global-axis offsets from body origin. Persistent scene-body forces share RigidBody state; other bodies retain them in the server body runtime record. Extend scene Area field reduction and field-change wakeup to server bodies using the same masks and overlap reducer.
- Project all five body-state selector branches as typed PhysicsServer transform, linear/angular velocity, Sleeping and CanSleep getter/setter pairs for scene and server RIDs. Share validation, detached configuration and live state with direct views; expose BodySetAxisVelocity through the same velocity path. Sleeping assignments affect dynamic roles only, clear velocity when sleeping and retain pending forces; disabling automatic sleep wakes dynamic bodies. Explicit velocity assignments wake dynamics even at zero. Static and kinematic velocity is a virtual surface channel; kinematic target travel contributes only for its step. CharacterBody's movement-input Velocity remains distinct from low-level body contact velocity. Retain existing scene freeze/disable configuration rules. Nonfinite inputs reject before mutation, and attached operations enforce owner/solver/failure boundaries. No Variant or inert BodyState enum is exposed.
- Raw server kinematic transforms initialize the first pose immediately, then retain the latest target until the next nonzero active step; queries/getters expose the current pose until that step. Derive actual travel velocity from the target, keep configured virtual velocity separate, use the shared kinematic subdivision path and clear only consumed target motion after publication. A stationary next step must not drift from decoded angles or virtual velocity. Detach/reentry preserves configured velocity, sleep policy and an unconsumed target. Direct-state transforms use the same target contract; scene nodes retain their existing synchronized or immediate query presentation.
- Make RigidBody extensible and expose a protected virtual IntegrateForces hook plus stored CustomIntegrator=false. Execute hooks after solving, with scene synchronization before/after. Custom integration omits default gravity, damping and force accumulators but preserves impulses and contacts. Explicit state IntegrateForces applies selected gravity then damping for one tick on every call; it does not consume force accumulators.
- Expose cached body state, force/sync callback setters, omission control and contact caps through PhysicsServer for scene/server RIDs. Use typed actions and a generic userdata adapter allocated once at registration. User force callback precedes the scene hook and user sync observer. Under the typed host lifecycle, scene-owned synchronization remains mandatory; the user sync observer supplements it. Delegates are transient and are replaced/cleared by setters. Sleeping/static bodies follow their inactive native callback policy.
- Snapshot callback identities and solved contacts before user code. Permit post-solver body/fixture mutations and space queries; skip removed or replaced entries. Reject recursive stepping and world disposal. Aggregate callback failures after completing remaining callbacks and events without replaying committed changes. Clear cached views at detachment and registry entries at body release/weak cleanup.
- Report global contact positions, normals, point velocities and both shape indices independently of object ContactMonitor. Keep collider-object virtual tile identity Partial until typed tile-body integration. Both normal and signed tangential contact impulses accumulate applied warm-start and solver deltas across CPU scalar/SIMD and GPU substeps. Tag stored constraints by native solve epoch so sleeping contacts cannot replay a previous step. For subdivided kinematic frames, sum world-axis impulses by canonical fixture pair and feature across intervals, only for reporting bodies; deduplicate shared contacts. Retain every reported fixture/feature observed during the outer frame, including contacts that separate before its final interval. Keep each feature's last geometry and greatest observed depth, with per-body encounter lists, then publish bounded snapshots before callbacks. A later frame without that contact emits departure rather than replaying the old impulse. Scan all current contact candidates before applying a reporting cap: fill slots in encounter order, then replace the first shallowest slot only for strictly deeper penetration. Equal-depth candidates retain the earlier slots. Rigid object/shape monitoring uses these same retained points. Retain peak scratch capacity after warmup; unreported worlds avoid frame accumulation. The CPU single-interval path reads its completed manifold directly; independent GPU publication follows the device record path below.

- The independent GPU store retains one device frame record per observed shape-generation/feature/piece key, accumulating world-axis impulses across all solver and CCD intervals. Opt-in worlds share these records between receiver incidence lists and apply reporting limits only during explicit batched reads. Final publication freezes point velocities before later authoring; idle sleeping ticks retain geometry with zero applied impulse. Device capacity grows before submission without solver replay, and status joins the existing interval fence. Public RID/scene snapshot and event projection remains open. Contact and joint warm starts may scale down for shorter intervals but never amplify cached impulses after a short CCD interval; fresh iteration supplies any additional impulse required by a larger step.

### Consequences and verification

Games can control solver bodies through typed callbacks, manually integrate selected fields, query the world at a safe point, and inspect solved contact identities. PhysicsBodyStateTests covers center/inertia/force units, scene/server fields, Area reduction/wakeup, omission/manual order, callback userdata/order, lifetime/thread guards, contacts surviving fixture queries, hierarchy mutation, failures, packing and 64 warmed active callback frames with zero managed allocation on Linux/.NET 10. Native allocation, other platforms and owner visual acceptance remain unverified.

### Rejected alternatives

- Invoke the hook before native solving or return detached state copies: callback ordering and contact availability would differ.
- Permit arbitrary live access while constraints execute: this violates solver ownership.
- Add dynamic callback data or expose native body handles: typed actions and native-private attachment identity cover the required role.

<a id="adr-0071"></a>
## ADR 0071: Shape-owner groups and global logical collision slots

Last updated: 2026-09-30

- Status: Accepted
- Scope: CollisionObject owner API, child binding and shared body/Area geometry
- Depends on: [0054](#adr-0054), [0063](#adr-0063), [0065](#adr-0065), [0066](#adr-0066), [0014](resources.md#adr-0014)

### Context

Body/Area geometry previously consisted only of direct child providers. The applicable owner API permits arbitrary weak owner identity, several shapes per group and mutable geometry without children. Owner IDs and native/global shape indices are different spaces: group shapes can interleave in global append order, and removals shift later indices.

### Decision

- Expose all 21 typed owner operations. Use uint owner IDs, sorted owner enumeration and zero/max-current-plus-one allocation; deleting the highest ID permits reuse. Store arbitrary ElectronObject/null identity weakly. Groups borrow live Shape resources, retain logical disposed/disabled slots and never dispose caller resources. Missing IDs/indices and malformed inputs throw typed exceptions before mutation.
- Raw scene/server indexed geometry now uses the shared slots and transient per-slot overrides under [ADR 0088](physics-shape-slots.md#adr-0088). Store one append-order global slot per resource, with an independent group-local list. Adding to an earlier owner still appends globally; removal renumbers later slots across all groups. ShapeFindOwner reverses the current global mapping. Compound native fixtures share the resource slot's index. Motion owner lookup resolves the weak group object rather than assuming a scene child.
- Route both Body and Area fixture preparation through the same slots, resource revisions, local group pose and disabled policy. Public poses require finite unit scale/zero skew; child transforms preserve the existing pre-rebuild gate. Body groups apply normalized local one-way direction rotated by group pose plus finite nonnegative recovery margin. Manual defaults are false/zero-margin/down; child defaults remain their configured options. Area one-way setters have no effect. Zero direction is preserved.
- Bind CollisionShape/CollisionPolygon groups at parenting, keep them across tree exit, synchronize child configuration on entry, and remove them at unparenting/disposal. Active local-transform notifications update only transform; detached notifications stay inactive. Independent public owner overrides do not rewrite child properties; a later corresponding child edit updates that field. Child resource/contour replacement clears and rebuilds that group while retaining its ID.
- Keep manual groups transient; PackedScene reconstructs child groups from existing stored node/resource configuration. GetShapeOwners allocates caller-owned output; warmed unchanged slot/fixture/solver work reuses capacity. Configuration and structural changes may allocate. Indices in retained contact results describe their sampled step and can become stale after structural reindexing.

### Consequences and verification

Games can create procedural multi-shape groups without hidden scene nodes, disable or transform them, and recover owner identity from real query/motion results. ShapeOwnerTests checks defaults/ID reuse, interleaved indices, removals, weak/disposed identity, copied arrays, numeric errors, manual query/motion/rigid response, one-way/Area behavior, child lifecycle/packing, callback-failure revisions, resource disposal and 64 warmed owner solver frames with zero managed allocation on Linux/.NET 10. Native allocation, other platforms and owner visual acceptance remain unverified. Input picking and priority retain their own coverage gaps; ADR 0072 adds disable modes, and the monitoring decisions own shape-index events.

### Rejected alternatives

- Treat owner ID as shape index or enumerate fixtures by group order: interleaved additions and structural removals would return incorrect indices.
- Create hidden CollisionShape children for manual groups: arbitrary owner identity and group lifetime do not require scene nodes.

<a id="adr-0072"></a>
## ADR 0072: Disabled scene collision participation

Last updated: 2026-09-30

- Status: Accepted
- Scope: CollisionObject disable policy across all current Body and Area siblings
- Depends on: [0054](#adr-0054), [0060](#adr-0060), [0067](#adr-0067), [0070](#adr-0070), [0071](#adr-0071), [0055](physics-monitoring.md#adr-0055)

### Context

Effective inherited Node.ProcessMode.Disabled already issues synchronous disabled/enabled notifications, but collision objects remained in the world. The pinned CollisionObject2D policy changes world membership or temporarily replaces a body's requested mode. Its MakeStatic body transition clears pre-existing linear/angular velocity and does not change the configured body role; Area remains a sensor.

### Decision

- Project the nested DisableMode enum as global CollisionDisableMode to coexist with the same-named C# property. Preserve Remove=0/default, MakeStatic=1 and KeepActive=2. Store the policy in PackedScene and reject undefined values before mutation.
- Remove detaches an effectively disabled object from the scene world, query results, solver and Area fields. Retain RID, exception lists, owner groups, borrowed resources and configured body state. Invalidate attachment-bound direct-body views and clear local snapshots; report peer departure events through the existing removal path. Re-enable or a policy change reattaches synchronously. Entering a disabled inherited branch applies the policy before creating fixtures; reparent and reentry use the new effective mode. Pause alone does not trigger it.
- MakeStatic keeps Body fixtures and direct views attached with a static native type. Clear a newly static unfrozen rigid body's prior velocities; assignments while static retain their stored values for restoration. Preserve Freeze, mass, rotation lock, custom integrator, forces and the requested dynamic/kinematic/static role. Restore the requested type when enabled or changing to KeepActive. Area stays active for sensors and fields in both MakeStatic and KeepActive.
- Static manual transforms teleport without derived kinematic contact velocity. AnimatableBody keeps its existing fixed-step synchronized presentation; CharacterBody keeps typed manual motion. KeepActive continues ordinary solver behavior while Node callbacks remain disabled.
- Preflight attached policy, ProcessMode and Freeze changes against the scene world's solver ownership before changing flags. Post-solver integration callbacks can disable objects under ADR 0070. Aggregate notification failures after visiting all surviving affected descendants, so one departure subscriber cannot strand later bodies in the old participation state. Manual notification IDs do not override the effective ProcessMode.

### Consequences and verification

Games can deactivate collision branches, keep disabled scenery solid, or leave autonomous simulation active. CollisionDisableModeTests covers real query/solver/sensor behavior, disabled entry and inheritance breaks, policy switches, reparent/reentry, view invalidation, RID/owner retention, mass/locks/freeze/velocity restoration, user failures, phase/thread validation and packing. Sixty-four warmed mixed active rigid/static kinematic/sensor frames allocate zero managed bytes on the scene owner thread in Linux/.NET 10. Policy transitions themselves use the existing allocating Node notification/fixture lifecycle; native allocation, other platforms and owner visual acceptance remain unverified. No vendor change is required.

<a id="adr-0075"></a>
## ADR 0075: Static and kinematic frozen body roles

Last updated: 2026-10-08

- Status: Accepted
- Scope: RigidBody freeze policy, kinematic body-path integration and stationary surface velocity
- Depends on: [0054](#adr-0054), [0060](#adr-0060), [0067](#adr-0067), [0072](#adr-0072), [0073](physics-mass.md#adr-0073), [0074](physics-forces.md#adr-0074)

### Context

Freeze currently selects only a static body. The pinned FreezeMode offers Static=0/default and Kinematic=1: frozen bodies ignore gravity/forces, but a manually animated kinematic body has contact velocity along its path. Native target transforms support this role, while a single native world call can skip a dynamic body crossed by a long kinematic move. Rebuilding targets from the approximate native angle decoder also produces idle angular drift in synchronized scene presentation.

### Decision

- Project the enum as global RigidFreezeMode with the same values and a stored FreezeMode property. The mode has no effect while Freeze is false. A changed frozen mode selects the corresponding native role; mode/freeze mutations validate owner thread and solver ownership before writes and synchronize pending pose before switching. Pack mode before Freeze. Undefined enums reject before mutation.
- Frozen Static retains manual teleport behavior. Frozen Kinematic derives linear/angular velocity from the latest manual global target over a nonzero fixed frame; it ignores gravity/forces and retains zero inverse mass/inertia. Scene pose queries present the target immediately, zero delta does not consume it, and the next solver pass restores exact native history before deriving motion. Snapshot history on mode change/entry, preserve configured mass/inertia and body role across disable/remove/reentry, and let inherited MakeStatic temporarily override the role. Dynamic rotation lock is retained but does not prevent manually animated frozen rotation.
- Keep observed scene pose separate from exact native transform history. An unchanged frozen target clears velocity without reconstructing a decoded native angle. Apply the same idle guard to synchronized AnimatableBody. Native angular target estimation remains approximate under the existing ADR 0060 backend policy; tests allow 0.1 rad/s deviation for a 0.1 rad target over 1/60 s and 0.01 rad presentation tolerance.
- Subdivide native world calls when actual kinematic linear/angular travel exceeds a conservative fraction of current mover/dynamic collider extents, with native slop as the minimum interval. Use actual native kinematic roles across scene/server bodies, skip extra calls without dynamic targets, and reject counts outside the finite integer integration range. Retain one outer callback/event/snapshot cycle and the original outer Step value. Capture dynamic force/torque accumulators once and replay them across internal intervals so their full outer duration is preserved. Cache the snapshot list. Cost grows with travel and small geometry; a native kinematic time-of-impact path is the profiling-triggered upgrade. Contact impulse snapshots now aggregate all native intervals through ADR 0070.
- StaticBody exposes stored finite ConstantLinearVelocity (global scene units/s) and ConstantAngularVelocity (rad/s), both zero by default. Keep virtual surface motion separate from pose integration in the shared body simulation data. Contact preparation and scalar/SIMD/GPU solve stages add the endpoint surface velocities for normal, friction, rolling and restitution response while never integrating that extra velocity into the body pose. AnimatableBody adds inherited surface motion to contact/query velocity alongside its actual target motion; it still reaches only the requested pose. Point-motion queries, character platform carry and captured contact velocities use the same combined channel. Surface edits wake touching sleeping bodies. Reentry and fixture rebuild preserve scene configuration; raw server mode changes clear velocities under their existing state policy. CPU and GPU implementations are required together; scalar tangentSpeed or moving/restoring a kinematic pose does not implement this contract. Wider server state and CCD modes remain separate API work.

### Consequences and verification

RigidFreezeModeTests covers values/defaults, unfrozen invariance, immediate queries/zero delta, rotation and idle behavior, real moderate/fast path contacts, preserved force duration/single callback, mass/lock restoration, MakeStatic override/reentry, packed state, numeric/thread/phase/disposal errors, synchronized sibling idle regression and 64 warmed active subdivided solver frames with zero managed allocation on Linux/.NET 10. Native allocation, other platforms, broad-scene performance and owner visual acceptance remain unverified.

PhysicsSurfaceVelocityTests covers stored linear/angular surface velocities, normal/tangential/angular contact response, waking, point/contact queries, character carry, inherited target motion, scene/server lifecycle and 64 warmed active contact frames with zero all-thread managed allocation on CPU and Linux/Vulkan GPU. GPU constraint checks compare the same endpoint channels against scalar overflow and SIMD preparation/solving. These checks do not establish full GPU backend completion.

<a id="adr-0084"></a>
## ADR 0084

This active decision is maintained in [physics-joints.md](physics-joints.md#adr-0084).

<a id="adr-0085"></a>
## ADR 0085

This active decision is maintained in [physics-joints.md](physics-joints.md#adr-0085).
