# Selected shape-pair queries

## Complete selected pair operations

The two selected CollideShapes/BodyCollideShape operations now have a complete
public operation audit. The pinned required members and owning type have no
deprecated metadata. The explicit space selects resource/resource math; attached
body queries select their actual owner, while detached bodies retain the resource
path. Logical slots participate independently of slot enable, collision filters
and one-way response. Existing validation, world/resource borrowing, nested scratch
and atomic caller publication retain their own failure controls below.

The --shape-pair-matrix public-only consumer checks all 64 ordered combinations
of circle, rectangle, capsule, segment, convex polygon, concave edges, separation
ray and world boundary. Sixty-one deliberate intersection witnesses complement
three accepted noncolliding pairs (two concave resources, two rays, two boundaries).
Each combination runs resource, disabled raw slot and disabled scene slot routes
with pair capacities 0/1/4, both original and rigidly transformed poses, plus
separated controls. Every reported point lies on its own analytic resource boundary;
all unused caller storage retains its sentinel. Strict penetration must produce
points when capacity permits; zero-area exact touching may report only a boolean.
No internal manifold order, point count parity or backend implementation is assumed.

The same profile checks 25 finite-family independently displaced swept regions
and 50 raw/scene counterpart-motion queries. Explicit controls preserve boundary
endpoint/own-motion precedence, ignored concave displacement and ray positive-axial
motion while rejecting negative/perpendicular or counterpart displacement. A ray
against a boundary uses the full finite endpoint displacement, with original point
ownership. The existing --shape-pairs companion retains genuinely asynchronous
swept-region intersection, 20 distinct deep edges, 0/1/4/24 capacities, custom annulus,
invalid output, input failures, nesting and terminal-world rejection.

The independent provider previously read its last numerical body pose for a
logical source-slot operation. Immediately edited scene poses therefore missed
in its transformed matrix while built-ins succeeded. Its BodyCollideShape now reads
PhysicsServer.BodyGetTransform and the public local slot transform, matching the
common logical pose. This avoids forcing a numerical pose update just to query
resources. Seven scene controls cover StaticBody, synchronized/unsynchronized
AnimatableBody, CharacterBody, RigidBody, frozen kinematic RigidBody and PhysicalBone.
Pair queries preserve pose and tick; a synchronized animation target remains pending
until its next real interval. Existing direct-mode/pose transaction checks retain
partial-publication repair and deferred server-kinematic targets. Production runtime,
API/XML, resource wire formats and architectural decisions are unchanged.

After 128 warmups, 128 complete moving frames cycle every ordered combination in
both poses through all three routes, requiring strict zero owner/all-thread managed
bytes. Boundary distance tolerance is .002 scene units for float support geometry
and composed rigid transforms; shape dimensions are 6 units and separated controls
are tens of units apart. Moving-frame displacement tolerance is .0001 units; IDs,
flags, hit/miss, cap bounds, untouched tails and tick/pending-target state are exact.
Point arrays from standalone Shape methods retain their separately measured allocation.

All 75 qualified profiles pass: 35 managed consumer, 35 strict NativeAOT consumer
and five root scenarios. The latter retain their exact rebuilt DLL hash after the
consumer-only formatting correction. Twelve matrix profiles each measure 128
complete moving frames at 0/0 owner/all-thread managed bytes. Six portable-PDB
checks match all Compile inputs without DEBUG or missing/mismatched source: 2373
runtime, 852 root and 237 consumer documents; 3451 authored inputs are retained.
Validation SHA-256: dab62c1165adeb26dd8f04f7ae606f41bbba1616c6e34b64e7c0296e97823c88. The JIT consumer is
SHA-256 685717552f5d00fe447aed17dc300c2fc0b0c9eac72d04f6de4fe68d801f33a7; the strict NativeAOT executable is
SHA-256 7a7995e86f5d5452f519ee1326c5a468f28b580ce68ebd231649cdb3cfe08ab4. Release, all three format checks, compiled API coverage,
wiki tests and generation/check pass. The ledger is 1279/1369 Implemented
(93.43%), 90 open and 92 Excluded.

Evidence: bin/physics-shape-pair-matrix/2026-10-20/. The two operation rows close
independently of arbitrary custom/coupled numerical simulation, BodyTestMotion,
complete inherited/server/network acceptance, native allocation, other platforms
and target-hardware/window/performance gates. Software Vulkan is functional
execution; the unchecked GPU power cable excludes hardware load from this slice.


## Body-pair publication recovery and composed-pose validation

An attached `BodyCollideShape` query validates the logical slot and both live
configured resource identities before repairing that body's pending motion, mode
and pose publication. Scene and raw roles use the existing guarded preparation
paths. The selected pair implementation then observes the committed body pose.
Failed preparation propagates the original error before the hook or caller output
changes; it does not advance the physics tick. Terminal worlds retain their query
rejection. Queued later kinematic targets still wait for an actual nonzero step.

The reproduced registered Static-body failure previously returned a false miss,
changed the caller count from 997 to zero and made no pose retry after a partial
selected publication. The same consumer DLL now observes the original retry error
and unchanged caller contacts/count, then succeeds from the committed pose after
the publisher recovers. Pure two-resource queries and invalid body inputs do not
repair another body's publication. No whole-world query preparation is introduced.

The common attached-body path also validates the composed body/slot pose before
entering the selected hook. A disabled scene slot with nonunit scale previously
reached a registered implementation and returned a contact, unlike the built-in
implementations. Scale and skew now reject consistently with unchanged caller
contacts/count and no selected pair invocation. Disabled slots still participate
when their composed pose is valid.

The public-only `--shape-pairs` fixture now captures actual backend/thread selection
and exercises a real moving body during the strict 64-frame query bracket. All
eight built-in families execute selected pair math, including the registered owner.
A twenty-edge concave/circle case verifies independent concave-motion rules,
reversed point ownership, disabled/zero-filter/one-way raw slots, caller capacities
0/1/4/24, exact distinct deepest edges and unchanged tails. Its 128 warm/128 measured
complete frames require 0/0 owner/all-thread bytes. Line/circle boundary checks use
.001 scene-unit tolerance; edges are spaced one unit apart, avoiding threshold ties.
The caller-span capacity is independent of the resource-array sixteen-pair cap.

`--pose-transactions` adds explicit body-pair recovery to every applicable scene/raw
body and numerical mode, including repeated failure and unchanged ticks. Separate
controls verify that body-pair preparation does not apply a queued kinematic target
before its step. Existing numerical frame bounds and exact error identity checks
remain unchanged. `--pair-lifetime` and `--query-geometry` retain operand/using-world
borrows, cold supplier/error retry and real moving frames. Root ownership checks
retain the dynamic GPU pose query's one submission and 24 readback bytes.

Evidence: `bin/physics-shape-pair-audit/2026-10-19/final/validation.json`. This is a shared
runtime correction and expanded verification; the complete implementation-operation
accounting stays Partial. That checkpoint retained the 177-declaration open ledger; the complete operation
audit above records the later state. Full numerical-extension/native/foreign/network and target-GPU/window
performance acceptance remain open. The runtime-source-qualified ordinary root
frozen at `8912effe` completed with exit 0 in 1271.3658184509986 seconds; all 129
observed processes stayed free of hardware-driver mappings. Evidence:
`bin/physics-shape-pair-audit/2026-10-19/final/full-ordinary-current/check.json`,
SHA-256 `15e0e32964f034264d3cd7f8a8ed20333ebe16cfcf3256fefa24fb37d19af458`.
The root binary is SHA-256
`dacaf45c069905605a911a4a660f41fb539fbaf216df95b8b8efdd2ca13d97ef`.
This confirms the current shared runtime fix; later pickability test edits have
separate build/profile receipts. It does not establish final target-device or
whole-goal acceptance.

Both cold operands now use [world-query geometry preflight](physics-queries.md#cold-query-geometry) while their existing resource/pair borrows remain active. Numerical authoring rejects during supplier execution; selected pair hooks, staged output validation and nested scratch retain their existing contracts.

Last updated: 2026-10-20

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
