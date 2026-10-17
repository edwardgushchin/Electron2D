# Independent physics backend consumer

This executable references only the public Electron2D API. It registers a bounded circle/constraint test solver through PhysicsServerManager and owns numerical bodies, contact/query geometry, constraints and private history. It does not wrap a built-in solver and is not a production physics option.

```sh
dotnet run --project tests/PhysicsBackendExtension.Consumer/PhysicsBackendExtension.Consumer.csproj -c Release -r linux-x64 -p:Electron2DBuildNativeFromSource=true
```

Checks cover strict default priorities, lazy captured World aliases, explicit selections, reserved/duplicate names, factory failures and recursion, outside-lock registration, scene/raw full steps, real queries/exclusions, Area/contact events, spring response, local/portable correction, terminal solve failure and cleanup aggregation. After 64 warmup frames, 64 complete frames with raw callbacks require zero owner/all-thread managed bytes. A headless no-GPU run uses SDL_VIDEODRIVER=dummy and an unavailable Vulkan ICD.

The circle/constraint model intentionally exercises a bounded fixture. Arbitrary coupled geometry/CCD/sleep/contact histories, unexercised extension operations, physical platforms, native allocator totals, real rendering and final GPU performance acceptance remain separate requirements.

Initialization/Finish/disposal failure injection retains all three errors after retirement. Scene diagnostic capture executes the selected ReadDebugContacts hook after synchronization; invalid count or nonfinite points reject before diagnostic publication and fail the started interval.

Custom geometry checks use a canonical little-endian analytic annulus, exact unit-density mass and an empty hole. Use --draw for actual GPU/compatibility diagnostic pixels, and ELECTRON2D_CUSTOM_GPU=1 to include built-in GPU rejection. The custom boundary is not an approximation to a built-in primitive. Custom file codecs/editor registration and generic production solver acceptance remain open.

Portable episode checks validate bounded output after growth/shrink, invalid identities/shapes/pieces and duplicate reversal, preservation of a previous packet, different local creation order, real pass-through/contact impulse after restore and 64 allocation-free capture/apply cycles. The episode fixture uses explicit retained decisions, not a generic one-way detection algorithm.


Identity checks publish object/canvas associations and collision priority authored
before attachment, including real point-query membership, scene canvas entry,
fresh-world transfer, weak sampled targets, failure cleanup/retry and owner/borrow
guards. After initialized allocation counters and 64 warmups, 128 repeated
body/Area canvas edits, priority writes and point queries allocate zero managed
bytes on the owner and across all process threads. Numerical priority response in
an arbitrary solver is outside this bounded fixture.

Shape mutation checks reject nested body/Area slot/filter edits and shared server-shape data/release during initial identity and live geometry/filter hooks. They compare scene/raw borrowing, owner access, delivered transform/one-way values and real query membership after ordinary edits. After 64 warmups, 128 layer-edit/point-query cycles require zero owner/all-thread managed bytes. The bounded fixture does not certify arbitrary transform or one-way numerical response.

Body authoring checks reject nested force/mass/state/integration/callback changes
from initial and live numerical hooks for scene/raw bodies and direct views.
Ordinary impulses and callbacks still execute. After 64 warmups, 64 edited full
frames require zero owner/all-thread managed bytes. The separate body-state
consumer checks ordinary requested writes forwarded through its inner state.

Area field checks implement bounded independent circle reduction with descending
priority, all five gravity/damping modes, point falloff, vector magnitude and masks.
They verify exact ten-component delivery, monitoring independence, numerical
borrow/owner rejection, registry-lock boundaries and ordinary callback edits.
After 64 warmups, 64 edited full frames require zero owner/all-thread managed bytes.
The fixture does not certify arbitrary shapes or a complete production solver.

Parameter read checks inspect all ten authored body parameters during real
initial scene/raw attachment and live numerical callbacks without preparation.
Automatic circle inertia still observes geometry edits; automatic preparation
rejects numerical borrowing before invoking hooks. After 64 warmups, 128 paired
scene/raw read cycles require zero owner/all-thread managed bytes.

Motion role checks verify exact initial/live RigidLinear delivery, retained velocity, both freeze roles, disable precedence and restoration, owner/numerical guards, published motion, checkpoint restoration and zero allocations across 64 warmed full frames.

`--server-modes` runs the common scene/server mode checks independently. Add `--cpu` or `--gpu` to use a built-in implementation. Checks cover all five scene siblings, four modes, state/forces, reentry, checkpoints, portable state and zero warmed full-frame allocations.

The `--motion-exclusions` fixture (optionally `--cpu` or `--gpu`) shares scene/raw
motion filtering, object associations and zero-allocation checks. Registered
probes exercise actual helper calls, nested query/world scopes, parameter edits,
callback/invalid-result atomicity and lifetime/failure/thread guards. A positive
hook counter requires lazy world initialization to have actually occurred.

`--notification-phase` (optionally `--cpu`/`--gpu`) checks completed-notification
status in actual body/state/contact/Area/sleep callbacks, nested world/query
scopes, errors and retirement, with zero warm full-frame allocations.


`--shape-pairs` (optionally `--cpu`/`--gpu`) uses public selected shape/body pairs,
immutable authored geometry, real custom-hole contacts, nested staging and atomic
error/lifetime guards. Its 64 warm full frames execute 320 outer queries with zero
managed allocation; the registered numerical fixture remains bounded.


The `--motion-exclusions` registered checks also reject results constructed for a
same-world surrogate moving body. All caller fields stay unchanged, queries without
caller output reject too, and later valid results recover normally. CPU/GPU share
the positive/miss and zero-allocation matrix.

The --parameter-reads mode verifies all eighteen retained body parameter/policy getters inside numerical Solve on scene and raw bodies, strict write/query guards, automatic-profile rejection, stale/kind/owner/failed-world errors and 128 warmed active full frames with zero owner/all-thread managed bytes.

The --lifecycle mode traces the actual explicit/scene lifecycle, completed statistics, local/global pause and retirement. It checks activation errors, all negative count fields, terminal numerical failures, recoverable notification/finalization failures and 128 full moving frames per ownership role with zero owner/all-thread managed allocation.

`--debug-contacts` exercises selected diagnostic preparation before solving, odd/zero caps, actual circle contacts, pause, validation failures and teardown. `--debug-drawing` renders those contacts through both canvas renderers and checks pixels, live disable/re-enable, separation and native resource release.

`--factory-views` supplies body/query views from the registered solver, checking initial empty snapshots, actual contact publication before scene/raw callbacks, all query families, malformed output, factory failure/retry, attachment lifetime and zero warmed frame allocations. The project links the existing public CircleQueries fixture source.

`--construction` checks fresh body/space identities, selected ownership, all detached/live modes and CCD values, automatic mass reset, borrowing guards and view retirement. Add `--cpu` or `--gpu` for built-in execution. After 256 warmups and collection, 128 complete changing-CCD steps require zero owner/all-thread managed allocation. It also runs the existing five-sibling scene mode fixture. Registered CCD delivery is checked as configuration; independent numerical CCD acceptance remains open.

`--picking` checks scene Body/Area defaults and edits, selected point-query canvas routing, input/hover ordering, event-copy lifetime, numerical borrowing and off-owner guards. Add `--cpu` or `--gpu` for built-in execution. After 128 warmups and complete collection, 128 changed-policy full frames produce real passive transitions at zero owner/all-thread managed allocation. The native test host reuses this registered factory for both renderers and SDL pointer/pixel checks. Arbitrary registered geometry and full numerical acceptance remain open.

`--shape-owners` verifies the complete authored owner read family on tree-less Body/Area attachments, actual space owner/failure guards, numerical reads and equal-write rejection, indexing and failed-world cleanup. Add `--cpu` or `--gpu` for built-in checks. All five Body siblings and Area execute 128 warmed full frames with non-allocating scalar metadata reads at 0/0 owner/all-thread managed bytes; caller-owned GetShapeOwners arrays allocate separately. Full geometry/disable acceptance remains open.


`--joint-response`, with optional `--cpu` or `--gpu`, runs common physical
pin/groove assertions on scene/server joint identities. It covers coupled force
budgets, compliance, live inherited bias, world/off-center anchors, motor/stops,
finite/reversed/zero guides, free rotation and private next-step replay. Each of
four owner/role combinations measures 128 full edited frames after 128 warmups
at strict 0/0 owner/all-thread bytes. The independent fixture performs one outer
pose integration; full coupled numerical and portable/network acceptance remain open.


`--history`, optionally `--cpu` or `--gpu`, checks numerical sleep restoration,
grow/shrink recapture, real pending force/torque replay, immediate contact values
and 128 warmed full-step/capture/restore cycles at 0/0 owner/all-thread bytes.
The registered case also checks uncached numerical contacts/step and retained
one-way decisions, with a separate 128-cycle zero budget. Private history captures
typed state and releases retired references; generic CCD and
portable/network acceptance remain open.


`--sleep`, optionally `--cpu` or `--gpu`, verifies actual automatic sleep on
scene/raw bodies, strict thresholds/duration, zero policy, CanSleep, immediate
policy/impulse wake, inactive intervals and restored quiet clocks. Dynamic
contact/joint groups sleep and wake together; a shared static support does not
merge independent groups. After 128 warmups, 128 replay cycles (1920 frames) and
128 actual group sleep/wake cycles (896 frames) each require strict 0/0
owner/all-thread managed bytes. General geometry/CCD, portable networking and
final native/performance acceptance remain open.


`--contact-response`, optionally `--cpu` or `--gpu`, checks actual signed material
mixing, normal/tangential/angular static surfaces, inherited AnimatableBody
surface motion, dynamic lever arms, complete impulse versus momentum, geometry
edits, surface wake and private response replay. After 128 warmups, 128 cycles
editing the borrowed material and typed coefficients/surface, stepping and
capturing/restoring require strict 0/0 owner/all-thread bytes. The fixture remains
bounded primitive geometry; complete CCD/network/native/performance acceptance
remains open.


`--continuous-response`, optionally `--cpu` or `--gpu`, checks actual discrete,
ray and shape motion, relative moving-body impacts, elastic remaining time, full
signed impulse, filters/exceptions, one outer callback/tick and force duration.
128 warmed reset/full-step/capture/restore cycles require strict 0/0 bytes.
Registered explicit-space controls also check custom inner-wall rebounds and
terminal numerical interval-budget failure. General rotated/coupled geometry,
multiple-peer contact history, networking and final native/performance acceptance
remain open.


`--contact-frames`, optionally `--cpu` or `--gpu`, checks two separated CCD
impacts retained in one completed frame, full signed impulse versus momentum,
contact-free future clearing and immediate private restoration. Three report-only
peers exercise deepest caps 3/2/1/0, positive depth filters and encounter-order
ties. After warmup, 128 transient replay cycles and 128 changed cap/depth cycles
(768 full frames) each require strict 0/0 owner/all-thread managed bytes. The
independent fixture owns retained per-peer numerical rows and flat private
history; arbitrary shape features/coupled geometry and final acceptance remain open.

Use `--project-selection` for typed implicit-world selection: real independent-provider motion, lazy aliases, immutable DEFAULT priorities and factory errors with project fallback enabled. After 128 warmups, 128 complete frames require zero owner/all-thread managed bytes. The profile also runs with actual GPU capability denied; it uses only the public API.

Use `--compound` (optionally `--cpu` or `--gpu`) for rotated multiple-circle slots, disabled indices, all query families, scene/raw/Area ownership, compound inertia, per-feature momentum, deepest same-peer caps, CCD and private history. 128 full warm frame/query/restore cycles require 0/0 managed bytes. `--compound-native` checks all three physics choices with actual window pixels; add `--gpu-renderer` for GPU rendering. General polygon/capsule/piece and coupled angular CCD acceptance remains open.

The built-in compound profile also checks ten circle/segment query combinations against rounded/sharp represented targets. CastMotion must bracket analytic first touch within 1/256, with 128 allocation-free warm queries per case. The CPU query policy and resident GPU query/motion paths use geometric boundaries; ordinary native solver policy retains its existing settings.

The `--shape-geometry` profile accepts `--cpu`, `--gpu` and `--threaded` (omitting a built-in selects the registered provider). It checks inherited resource-copy guards for every represented shape and an application Shape, preserving base metadata/reset/events on rejection and verifying allowed callback copying through a real query. Accepted structural copying is outside the zero-allocation full-frame bracket.

Standalone Shape.ResetState uses the same preflight before OnResetState. The public-only regression rejects reset during numerical/access/geometry hooks, checks no custom-reset side effects, and preserves allowed quiescent reset without Changed. Copying invokes reset exactly once after its own accepted preflight.

`--contact-response` accepts `--cpu`, `--gpu` and `--threaded`, with the registered provider selected when no built-in is named. Besides existing material/surface/impulse checks it verifies copied and archive-reloaded material publication after an earlier Changed observer failure, shared borrowers, override retirement and 128 zero-allocation full contact frames. Cold copying and file I/O are outside that bracket.

`--world-copies` accepts `--cpu`, `--gpu` and `--threaded`, with a registered provider by default. It verifies world resource-copy preflight, lifetime/release restrictions, alias and replacement behavior, accepted notification failures and zero-allocation warm simulation/point queries.


`--pair-lifetime` verifies resource/body pair borrowing, custom callback geometry/
policy/reset/copy/disposal guards, nested/error continuation and owned shape RID
replacement/retirement. Add `--cpu` or `--gpu` to select a built-in owner and
`--threaded` for its separate numerical lane. Prepared full frames/queries require
zero owner/all-thread managed allocation.


`--motion-result-lifetime` accepts `--cpu`, `--gpu` and `--threaded`, selecting a
registered owner by default. It verifies the complete physical result-value family,
borrowed disposal/owner guards, scratch identity/reuse, callback/validation failures,
caller snapshots and retirement aggregation. Full measured frames include body
motion tests with and without caller output. All 128 measured frames after 128 warm
frames require zero owner/all-thread managed bytes and actual solved movement.


`--coupled-joints` selects CPU by default, with `--gpu` and `--threaded` available.
Two pins, a finite groove and an off-center spring close a four-body mixed scene/raw
mechanism; both joint ownership forms execute. Numerical contribution controls,
physical momentum/stability, checkpoint replay and CPU-authoritative portable
correction all use public API. The independent local-frame and full transport/frame
128-sample brackets require zero owner/all-thread bytes. Packet transport is local;
real multi-process networking and hardware GPU performance are separate gates.


`--joint-contacts` selects CPU by default; add `--gpu` and `--threaded` as needed.
A pin/groove/spring mechanism rests on a real frictional floor with virtual surface
motion. Scene/raw receivers and joint owners verify full normal/tangent contact
impulses, object/slot/velocity data, monitored checkpoint continuation and motion
after clearing constraints. Missing-floor and missing-elastic controls reject;
128 complete measured contact frames require zero owner/all-thread managed bytes.
Finite-step damping bounds follow the accepted spring law, preserving nonzero
damping on both backends. Hardware/native/foreign and larger workload gates remain
separate.

`--statistics`, optionally `--cpu` or `--gpu` and `--threaded`, verifies completed
active-body/candidate/island publication through real compound support, sensors,
kinematic bodies, pin groups, sleep, pause, replay and Performance projections.
The registered implementation uses its retained dynamic graph rather than reporting
one island per active body. Each profile measures 128 complete moving frames plus
reads after 128 warm frames and collection at zero owner/all-thread managed bytes.
`--lifecycle` additionally rejects negative samples and Islands greater than Active,
retaining the previous successful publication and terminal error cleanup.
The registered geometry now includes circle/annulus, rectangle and capsule numerics; other families remain open.

`--rectangle-geometry`, with optional `--cpu`/`--gpu` and `--threaded`, checks
actual rotated/offset unit-pose rectangular contours, Area monitoring and point
queries, all six direct queries, body motion, deepest rest, mass/inertia, all CCD
modes, full linear/angular impulse response and private replay. Each warmed query
and contact/replay bracket measures 128 complete frames at 0/0 managed bytes.
Registered-only controls preserve the custom annulus hole and inner-boundary
response. The shared pose precondition remains unit scale and zero skew; other
primitive/contour families and target-device acceptance retain separate gates.

`--capsule-geometry`, with optional `--cpu`/`--gpu` and `--threaded`, verifies actual
rounded segments, rotated/offset slots and Areas, point and axial/side/oblique-cap
rays, all shape-query families, margins, body motion, compound mass, all CCD modes,
full linear/angular impulses and silent private replay. Eight warmed brackets
measure 128 full frames each at 0/0 managed bytes. Cold controls cover disk and
standalone zero-radius limits, live edits, independent sweeps, false circumscribed
contacts, thin walls and intermediate rotational arcs. Registered-only custom-ring
checks retain the hollow boundary. Geometry uses public API only; complete
provider/zero-area dynamic/native/network/target-device acceptance stays open.

`--segment-geometry`, with optional `--cpu`/`--gpu` and `--threaded`, checks actual
two-sided lines, empty point interiors, transverse rays, real line sensors, all
shape queries/body motion, rod and mixed mass, all CCD modes, full impulses and
silent private replay. Eight 128-frame warmed brackets require 0/0 managed bytes.
Cold controls cover unshaped zero inertia, short/coincident edits, independent
swept pairs, collinear/opposite-side rays and rotational arcs. Wider contours,
provider/native/network/hardware and performance acceptance remain open.

`--convex-geometry`, with optional `--cpu`/`--gpu` and `--threaded`, verifies whole
accepted contours, asymmetric/closed/reversed winding, exact mass, live empty and
replacement behavior, six query families, body motion, contacts and linear/angular
CCD. Actual twelve-vertex query/Area and rotational cases preserve one logical slot.
Eight warmed frame/replay brackets measure 128 frames each at 0/0 managed bytes.
The independent provider streams all vertices without delegating to built-in
solvers. Its large-contour throughput and complete provider/native/network/target-
device acceptance remain open.

`--concave-geometry`, with optional `--cpu`/`--gpu` and `--threaded`, verifies paired
hollow edges, one logical slot, rod mass, direct-query source/target, nearest/deepest
edge output, Area deduplication, contacts and linear/angular CCD. Two corner features
retain full impulses through private history while one body/shape transition is
delivered. Nine warmed brackets measure 128 complete frames each at 0/0 managed bytes.
Cold controls include short/point/empty edits, ignored standalone concave motion,
two-concave exclusion, capped deepest pairs and custom annulus material. Larger
coupling, provider/native/network/hardware and final performance gates remain open.


`--boundary-geometry` (optional `--cpu`, `--gpu`, `--threaded`) checks infinite
analytic planes, transformed nonunit authoring, six direct queries, reverse query
source, Area/contact identity, zero geometric mass, sleep/edit/removal, standalone
endpoint policy and two support contacts. Six finite families × three CCD modes
plus query and corner replay each require 128 warmup/128 full measured frames at
0/0 owner/all-thread managed bytes, including normal/tangent material impulses and
static surface velocity. Cold linear/angular/rotating-plane CCD stays separate.
The provider initializes captured sleep/iteration/bias policy through public world
getters. CPU plane tests retain .005-unit point/ray/rest bounds at tangent distance
6,000 after correcting authored rotation conversion. Software Vulkan establishes
functional behavior; directed ray, broader coupled/custom, native/platform/network
and target GPU performance gates remain open.


`--directed-geometry` (optional `--cpu`, `--gpu`, `--threaded`) checks directed
surface entry and containment/back-facing/zero/ray-pair rejection, axis/slope
policy, six queries and reverse source, safe/unsafe casts, recovery/character snap,
no rod inertia, exact indices/events, materials/surface velocity and coupled
impulse replay. Twenty-three 128-warm/128-measured complete-frame brackets require
0/0 managed bytes. Explicit cold edits, reversed target families, directional
masks, sleeping/removal and linear/offset-center angular CCD remain separate.
The provider uses actual directed geometry and double circular-sweep intermediates;
no built-in numerical delegation or solid-segment substitution is used. Larger
custom/coupled, inherited/complete extension/native/network/platform and target
GPU performance gates remain open.


`--priority-geometry` accepts `--cpu`, `--gpu` and `--threaded`; omitting the backend
selects the independent public-only solver. It verifies normalized motion recovery
for finite shapes and directed rays, large/subnormal priority values, bounded
contact capacity, exclusions, one-way controls, character movement and rigid
momentum. Five 128-warmup/128-measured full-frame brackets require zero owner and
all-thread managed allocations. GPU execution needs its renderer/driver setup;
software-driver checks establish functional behavior only. This profile leaves
general registered contact correction/cache/iteration acceptance open.


`--contact-policy` selects the independent solver by default and accepts `--cpu`,
`--gpu` and `--threaded`. It checks mixed world/shape correction and slack,
complete contact/joint sweeps, dense contacts, actual CCD durations, momentum,
live policy/observer failures and private replay. Nineteen brackets use 128 warmup
and 128 measured complete frames with strict zero owner/all-thread managed
allocations. The custom curved arc fixture explicitly uses bias one/zero slack
for its strict geometry bounds; ordinary defaults and nonzero slack have their
own analytic matrix. Registered geometric cache and full numerical/performance
acceptance remain open.

`--node-orphans` checks the inherited static Node diagnostics in both Debug and
Release: weak live/detached membership, internal/physical roles, sampled output
and logger reentry, direct physics binding, lifetime failures and concurrent
construction/scans. It also runs 128 warmed/measured moving full frames at zero
owner/all-thread managed bytes, excluding cold diagnostic snapshots. Use `--cpu`,
`--gpu` or neither for the registered provider, with optional `--threaded`.
Debug supplies real diagnostics; Release requires empty IDs and no output.

`--node-names` verifies generated/readable collision naming, all four numeric
separators and feature overrides, long/leading-zero suffixes, internal siblings,
physical-role identity, owner/numerical guards, reparent/replacement and retained
reentrant path notifications. Public assignment covers all six reserved characters,
Unicode, typed descriptors, same-input and detached delivery, and five simultaneous
path/node/tree failures with continued delivery. Stored generated names remain exact;
public reassignment normalizes their internal markers. It saves scene/config files and loads them in a
fresh process. Use `--cpu`, `--gpu` or the default registered provider, with
optional `--threaded`. Each full-frame bracket alternates existing unique names
while simulating, after preparation, and requires 0/0 managed bytes. New collision
strings and first snapshot capacity growth remain cold authoring work.

`--pose-transactions`, optionally `--cpu`, `--gpu` and `--threaded`, covers failed authored Body/Area pose publication across all five scene body siblings, raw bodies and both Area roles. It checks 312 retry cases, 52 retirement/transfer cases and two scene-role takeovers for the registered provider, current query geometry, scalar/batch/view semantics, same-pose edits, direct-view writes, stale views and deferred kinematic targets. Each profile measures 128 complete changed-pose frames after 128 warmups and full collection at strict 0/0 owner/all-thread managed bytes. This bounded failure fixture does not establish all state-setter failures, arbitrary numerical behavior or native/hardware/performance acceptance.

`--motion-transactions`, optionally `--cpu`, `--gpu` and `--threaded`, checks linear/angular/axis writes, explicit sleep and CanSleep after partial selected publication. It covers all five scene siblings and raw bodies, four modes, facade/view/scene writes, preparation routes, retirement/transfer, regular scene roles, impulse-derived untouched motion and detached wake semantics. After 128 warmups and collection, 128 complete changing-motion frames require strict 0/0 owner/all-thread managed bytes. Failed-request/exception creation and native/foreign allocations are outside that successful-frame claim.
