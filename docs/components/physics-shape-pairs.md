# Selected shape-pair queries

Last updated: 2026-10-14

The internal selected-owner bridge executes resource/resource and body-slot/resource
collision for the pending complete server-extension family. No new public family is
shipped by this slice; [ADR 0103](../decisions/physics-extensions.md#adr-0103) retains
public manager/backend/extension and custom-algorithm acceptance as open work.

## Runtime flow and contract

[PhysicsSpace.ShapePairs](../../src/Servers/Physics/PhysicsSpace.ShapePairs.cs) checks
owner/lifetime/failure, finite rigid poses and motion, complete destination point
pairs, real shape RIDs and body membership before invoking the selected owner's
CollideShapes or BodyCollideShape. A nested call has an independent retained staging
frame. Counts, miss/count consistency and every finite result point validate before
only the used caller prefix changes. Hit-only requests use an empty span and return
zero pairs; an exact touch may also report a hit without separating points.

[PhysicsServer.ShapePairs](../../src/Servers/Physics/PhysicsServer.ShapePairs.cs)
resolves logical slots and composes the current body pose with the slot transform.
The explicit operation uses authored disabled shapes and ignores collision filters
and one-way response, preserving the [pinned native body-pair operation](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/godot_physics_2d/godot_physics_server_2d.cpp#L979). Detached
scene/raw bodies execute the same resource algorithm without constructing a solver
or device, with per-thread nested staging. They borrow body lifetime until return.

CPU/GPU owners use the existing exact resource kernel: full convex contours,
independently swept regions, concave edges, directed separation rays and infinite
boundaries retain their resource-pair semantics. This explicit resource calculation
runs on the calling CPU; it does not simulate or mirror the world's solver. The
public resource's sixteen-pair array limit does not constrain internal caller-span
capacity. Geometry belongs to authored resources; one observed body pose is the
only solved input needed for its body-slot query.

The GPU collider's scalar pose cache now gathers a current sixteen-byte pose when
neither the complete state nor a current pose is cached. It also reads the existing
eight-byte dispatch status to preserve device failure detection. This replaces an
unnecessary sixty-four-byte state record plus status. One request submits once;
subsequent pose-only accesses reuse the current epoch. Velocity/field/sleep callers
still request complete state when required. Replay and portable invalidation remain
qualified by the existing epoch and selected history.

While a selected pair hook is borrowed, stepping, world binding, hierarchy change
and collider release/transfer reject. FreeRID now preflights that context before
entering the cleanup finally block; a rejected body or Area release cannot delete
its registry identity while leaving its attachment alive. Ordinary post-solver
callbacks preserve their existing mutation/release rules. Per-world staging is
cleared on disposal; detached thread scratch retains only vectors at peak capacity.

## Verification and limits

[PhysicsBackendOwnershipTests.ShapePairs](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.ShapePairs.cs)
checks scene/raw/detached slots, disabled geometry, composed poses and exact point
order, boundary/ray/concave motion, caller capacities above sixteen pairs, nested
selected hooks, genuine misses, invalid counts/nonfinite points/throwing hooks,
foreign body ownership and borrowed body/Area registry preservation. Input and
output equality use the same deterministic resource geometry, not CPU/GPU solver
bitwise equivalence. A dynamic GPU body query asserts one submission and exactly
24 readback bytes: sixteen pose bytes plus eight status bytes.

The fixture records query wall time/wait and complete step + three pair-query
p50/p95/p99 times, uploads/readbacks and mean wait for two attached static bodies,
64 warmups and 64 measured 1/60-second cycles. Owner/all-thread managed allocation
must remain exactly zero. This is a small contract/overhead check, not final dense
GPU performance or window/FPS acceptance. CPU also runs in the existing fresh
no-device ownership child. Native allocations, foreign platforms and public-only
registered solver acceptance remain unverified.


## Operand borrowing (2026-10-14)

Standalone resource queries borrow both Shape operands before acquiring immutable
geometry, through result validation and callback completion. Selected ShapeCollide
and BodyCollideShape do the same around implementation hooks; detached body pairs
use the standalone path. Geometry/policy writes, equal geometry writes, inherited
copy/reset and disposal reject before mutation. Owned server ShapeSetData and
FreeRID preflight the old geometry before duplicate/registry/attachment changes.
Nested different pairs and same-resource pairs retain independent borrow counts;
readonly geometry and resource duplication remain available. User callbacks run
outside resource locks. Finally releases every acquired operand after success,
invalid output or callback failure. Existing coordinated authoring/thread policies
remain; this is not a general concurrent resource transaction protocol.

The public-only --pair-lifetime fixture exercises all four standalone pair families,
reverse ownership, shared nested custom operands, equal writes, inherited metadata,
worker callback-time refusal, source disposal, selected hook nesting/owned RID
replacement/retirement and caller output preservation after failure. CPU/GPU/
registered serial/separate profiles retain actual primitive contact output and
128 warm/128 measured complete scene frames plus resource/body pair queries at
strict 0/0 owner/all-thread managed bytes. A moving raw body verifies 256 units
over the 256 actual numerical ticks within .02 units; no idle-only thread claim
is inferred from the requested setting. Pure standalone custom checks need no
GPU; the selected GPU profile uses CPU-only Vulkan. Wider numerical extension,
hardware/native/platform/network and full performance acceptance remain open.
