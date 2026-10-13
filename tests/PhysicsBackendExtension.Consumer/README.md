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
