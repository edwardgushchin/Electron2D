# Authoritative physics network example

Last updated: 2026-10-16

[Retained scene/network memory](physics-network-memory.md) now measures the actual
three-process example, its managed histories/queues and an additional four-point
local scene-history choice. Completed and disposed graphs are separate from final
collection. These CoreCLR object sizes do not close native/driver/AOT or hardware
performance acceptance.

[PhysicsNetwork](../../examples/PhysicsNetwork/README.md) connects the existing
`SceneMultiplayer`/`ENetMultiplayerPeer`, `PhysicsSnapshotMap`, `PhysicsSnapshot` and
world-tick APIs. Its CPU authority and GPU/CPU clients are separate OS processes.
All example code uses public Electron2D types; the test-only partial probe supplies
private GPU transfer counters. No alternative physics solver or private engine API
is embedded in the consumer.

## Deterministic validation phases and control handoff

The application check explicitly coordinates all impulse phases from tick 240
in 120-tick intervals. A client records a phase only after its actual physical
step and invariant checks complete. Ready authenticated peers send bounded phase
masks over the existing impaired link; duplicate/reordered masks merge by union.
Zero, out-of-budget and too-far-future claims reject before changing progress.
Each client also sends one invalid future-mask probe for the protocol guard.

The check authority waits before a required phase until all admitted clients have
executed it. It also waits for matching numbered input from the current controller,
generation and epoch. Polling and snapshot delivery continue during waits; physical
dt remains 1/60 and the scripted impulses retain their original tick numbers.
Check-only prediction stops at fourteen ticks ahead of confirmed state, retaining
two live prediction ticks beyond the twelve replay lead ticks within the existing
sixteen-tick input resend span. Ordinary sessions keep their prior clock/prediction
policy. Headless clients accept an explicit 0–250 ms poll delay for slow-client
checks. Coordinated checks allow simulated duration plus 120 seconds; ordinary
sessions retain simulated duration plus 30 seconds. These paced timings are a
functional coverage envelope, not a throughput/window-performance result.

A related application-input defect is corrected in every session: after current
owner/generation/epoch validation, future input replaces an obsolete queued entry
for the same network ID/tick. Same-incarnation/epoch duplicates remain idempotent
and conflicting values reject. The command preflight decodes actual wire payloads
through the receiver and checks duplicates, conflicts, retired-controller and
retired-incarnation replacement. Process checks additionally require real handoff
replacements. No physical backend or public engine API changes.

Reports retain completed phase masks, separate wait counts/time and twelve-byte
progress-payload attempt totals, including invalid probes; transport framing is
outside these application-byte counters. Failed checks stop owned children, drain
their logs, then dispose streams, retaining the diagnostic output.

The final Linux x64 source passes nine coordinated three-process profiles:
five instrumented test-host cases (CPU default, slow threaded CPU, serial GPU,
threaded GPU during full Release compilation, slow threaded GPU), one standalone
managed example build with slow serial GPU, and three strict NativeAOT cases
(CPU default, serial GPU, slow threaded GPU). Defaults use 360/90 ticks; the other
seven profiles use 600/310. Slow cases delay primary polling by 250 ms. All 27
participants complete their required phases, contact/impulse/sleep/wake checks,
28-object final state and original .002 convergence tolerances. Every warmed
owner/all-thread authority, prediction and replay byte counter is zero with
positive required samples. The authority replaces 6–23 retired future commands
per profile. Maximum observed geometry penetration is .36610413, below .65.
Process maps confirm pinned lavapipe, no physical GPU driver, and no CoreCLR in
all nine NativeAOT child processes.

The coordinated software-GPU checks take 61–123 seconds in this loaded cohort;
these durations include deliberate waits. A separate ordinary 250 ms GPU control
under parallel software-GPU load reaches tick 600 with 104 local steps and no
warmed sample, correctly failing its positive-sample assertion. Its zero counters
are not allocation evidence. Three isolated ordinary controls (serial/separate GPU
and separate CPU, all with 250 ms polling) pass in 13.17–13.37 seconds. Check
progress and wait counters remain disabled; actual snapshot coalescing, corrections,
replay, exact confirmed events and 28-object final convergence execute. GPU clients
complete 173 steps each, including four warmed prediction and 41 replay samples;
all measured warm byte counters are zero. This smaller sampling envelope does not
replace the strict phase checks. Ordinary sessions do not gain check pacing or a
wider deadline. Complete throughput, native/device memory, real-window and
physical-GPU acceptance remain open. Artifacts, the failed loaded control and rejected implementation candidates:
`bin/physics-network-phases/2026-10-15/`. Release/source-native and strict AOT
builds, three format checks, compiled coverage and wiki tests/generation/check
pass. Six PDB/source sets have no missing or mismatched declared sources. Runtime
source/API and coverage classifications are unchanged; the default full runtime
suite was not rerun for this consumer-only change.

## Compound slots and rotating ray under correction, 2026-10-15

The current application retains the preceding 27 objects and adds ID 214, a second
ray-only body at (1700,40) with free rotation, explicit inertia 2, initial rotation
.3 radians, local angular damping 4 and CastShape CCD. The original locked ray is
unchanged. The final manifest contains 28 objects in the same 32 slots and retains
the same command/event history budgets. The application header becomes version
four and rejects versions one through three; the runtime portable format and API
are unchanged.

The fifth geometry bit requires the same positive contact, impulse, sleep, wake and
imported contact/sleep evidence as the preceding four families. Alternating torque
impulses +2/-2 accompany the existing upward impulses at ticks 240 + 120n. An angular
step requires actual pose change greater than .0001 rad and speed greater than .01
rad/s; each process must observe speed above .1 rad/s. Client corrections must carry
both nonzero angular speed and an orientation differing from the initial pose by
more than .01 rad. The support bound includes both ray endpoints at arbitrary angle,
with the existing .65 penetration and 50-unit horizontal bounds.

Compound evidence is now per local slot: masks 3 require actual contact and positive
upward impulse from both circle slot 0 and box slot 1, plus both restored indices.
Every reported point is transformed from world coordinates into body coordinates;
its X sign must match the authored left-circle/right-box layout, outside a .25-unit
ambiguity band. The geometries occupy disjoint local X intervals, with more than a
three-unit gap, so this association also rejects swapped indices. Correction reads
still do not count toward simulated impulse evidence. The startup controls retain
removed-boundary/ledge checks and additionally disable each compound slot or lock
the free ray; each must lose a required physical/evidence invariant.

The updated scenario passed eleven real three-process ENet profiles: four managed
and four strict NativeAOT CPU/software-GPU combinations with serial/separate
numerical execution, one instrumented threaded profile (900 authority ticks, late
admission at 310), and two default managed profiles (360 ticks, admission at 90).
All 33 processes produced complete five-family masks, compound contact/impulse
masks 3 and actual angular steps; all clients imported both compound slots and
moving angular state. Maximum observed penetration was .10626221. All warmed
owner/all-thread prediction, authority and replay allocation counters stayed zero.
Process maps confirmed software Vulkan only on GPU clients and the same native
executable without CoreCLR for all twelve NativeAOT children. Source/binary
manifests and reports are retained under ignored
`bin/physics-network-slots/2026-10-15/`. Release/source-native builds, linked test-host
workflow, compiled coverage and wiki tests/generation/check passed. The default full
suite was not rerun for this example-only change; runtime source is unchanged.

The headless check matrix retains CPU authority and CPU/software-GPU clients,
separate numerical lanes, actual ENet impairments and late admission. New drawing
cases use the existing Shape.Draw consumer; compilation/headless simulation does
not establish rendered output or real-window FPS. Dense arbitrary feature histories,
complete inherited/extension semantics, native/device memory, target hardware and
final performance acceptance remain open.

## Cross-family geometry under correction, 2026-10-15

At f0aa93aa, the manifest added six recipes without removing earlier players, events,
trajectories or the loaded joint column: an infinite boundary at y=100, a paired
concave ledge at y=80, rotated capsule, asymmetric convex polygon, directed ray and
rotated two-slot circle/rectangle body. That profile has 27 objects in the
existing 32 prepared slots; command/event history capacities are unchanged. The
application header is version three and rejects versions one and two before decode;
the runtime portable snapshot format and API are unchanged.

Four moving families use isolated layer/mask 4. Capsule, directed ray and compound
body select shape CCD; the polygon selects ray CCD. The ray explicitly locks
rotation; the other finite bodies start at .3 radians and remain free to rotate.
Every numbered tick 240 + 120n
applies an upward impulse of 40 to each body. Actual contact reads require mapped
floor RID/instance/object identity, logical collider shape zero, valid local slot,
finite unit normal and impulse. Positive impulse evidence is sampled only from
simulation, not from correction reads. Bit masks map bits 0..3 to capsule, polygon,
ray and compound; all four bits are required for contacts, impulses, sleep and wake,
and separately for imported contacts/sleep after corrections.

Support bounds use exact capsule support, authored convex vertices, ray tip and
both transformed compound pieces. Penetration is limited to .65 scene units,
consistent with the existing .6 complete-scene tolerance plus .05 float/contact
margin; horizontal drift is bounded to 50 units inside the ledge's 60-unit half
width. Contact normal length tolerance is .002, independently of orientation.
Two startup controls remove boundary or ledge collision and require rejection.
The instrumented host observes native collision intervals through the existing
notification hook, including CCD intervals whose pose finalizer belongs to the CCD
algorithm. Thread identity remains checked against the requested numerical lane.
Final cross-process pose/motion/sleep convergence retains the .002 tolerances and
all previous adverse delivery, identity, ownership and exactly-once effect gates.

The wider trajectories exposed CPU frame-contact list growth from 16 to 32 records
after warmup (2328 bytes in the observed late client). Frame contact aggregation now
prepares its list and index for two points per contact in the existing four-contact
per prepared-body budget, before intervals run. Reservation occurs only when a
receiver requests contacts; unobserved worlds acquire no extra frame-contact store.
First use/topology growth can allocate; warmed continuation within that budget
retains the strict zero owner/all-thread managed-byte requirement. Private profiling
prints used to identify the allocation were removed. Native/device totals, arbitrary
dense feature histories, foreign platforms, physical GPU advantage and real-window
FPS remain separate acceptance work. The drawing consumer uses Shape.Draw for the
new recipes; this headless profile does not certify rendered output.

### Executed headless matrix

| Host/profile | Profiles | Authority ticks / late admission |
| --- | ---: | --- |
| Managed CPU or software-GPU client, serial and separate numerical thread | 4 | 900 / 310 |
| Strict NativeAOT CPU or software-GPU client, serial and separate numerical thread | 4 | 900 / 310 |
| Instrumented managed CPU authority/software-GPU client, separate thread | 1 | 900 / 310 |
| Default managed CPU or software-GPU client | 2 | 360 / 90 |

All eleven real three-process ENet checks passed, including all prior packet
loss/reordering/duplication, lifecycle, ownership, effects, presentation-coordinate
and final convergence gates. All 33 processes reported zero warmed owner/all-thread
managed bytes for complete prediction/authority/replay steps. The four family masks
are complete in every process; clients also import complete contact/sleep masks.
All twelve NativeAOT children map the same native executable without CoreCLR.
Process maps show lavapipe only for GPU clients, no NVIDIA library, and numerical
threads only for requested separate execution. Test-only observer and random-phase fixture corrections followed the application
builds; runtime/application source and binary manifests stayed unchanged. The
instrumented host was rebuilt and its network profile rerun after both changes.
These are functional/allocation checks, not physical-GPU or real-window performance
acceptance. Source/binary manifests and reports are under the ignored
`bin/physics-network-geometry/2026-10-15/` artifact root.

## Tick and authority protocol

The game uses exactly 1/60 s per physical tick and asserts that every SceneTree step
advances its world tick once. Its viewport world is explicitly bound with FindWorld.
Clients send bounded records of tick, network ID, object generation, control epoch
and typed move/jump input. The server derives sender identity from SceneMultiplayer,
checks the controller and incarnation, rejects elapsed/over-budget ticks and
conflicting duplicate commands, and applies accepted input on the numbered tick.
The command window contains 256 ticks with 32 prepared object slots; this game's
controlled IDs occupy distinct slots. The server holds the last continuous control
when a command misses its deadline, but does not repeat a jump edge. This is an
explicit gameplay policy, not a promise that unreliable packets arrive in time.

Snapshots contain complete current physical state plus application lifecycle recipes,
control ownership and held-input state. A recipe describes the fixed example kinds,
shared network ID/generation and joint endpoints; it does not encode a local RID.
The receiver validates bounded wire records, reconciles creation/removal/reused IDs,
binds the local physical objects, and applies the portable snapshot. Current
scene/server roles, geometry recipes and physical authoring must agree. Speculative
client spawning and arbitrary scene/resource serialization are outside this example.

The server samples state every four ticks. Clients resend a bounded recent input
window and acknowledge received world/event state. Physics correction is performed
outside the packet callback; pending authority snapshots coalesce to the newest
state so a backlog cannot require applying every obsolete physical state. Newer
packets retain unacknowledged events, so coalescing does not lose game effects.
Clients reject older ticks/serials, restore authority state and replay saved local
input, retaining a 12-tick prediction lead within the bounded history. Remote
controls use the last received authority value. Injected pose/impulse divergence
must be corrected by later authority state.

Structural changes are authoritative: clients learn them from complete manifests.
A delayed pre-despawn packet cannot revive an older generation after a newer state,
and old-generation/old-owner input cannot affect its replacement. Late clients
receive the current full world and begin confirming effects after their own admission
baseline. Control epochs invalidate queued input when a body changes controller.

## Effects and presentation

Ordinary physics callbacks run during both prediction and replay. The game records
the phase explicitly; neither phase commits a confirmed effect. Only the authority
numbers real contact/Area events. The 256-entry event journal retains unacknowledged
records, rejects overflow explicitly, and resends until acknowledgement. Clients
validate contiguous sequence identities and commit each new event once. Reports
compare the actual confirmed records, including tick and object generations, with
the authority's journal after the client's admission baseline.

Remote presentation interpolates a 16-snapshot ring by server tick with an eight-tick
view delay. Missing endpoints clamp; interpolation never crosses object generations.
Owned presentation retains and decays a correction offset over roughly 100 ms.
These calculations write separate coordinate arrays, and the example checks that
they never modify solved body positions. Sample counters establish that interpolation
and smoothing actually executed. They remain separate from the optional native client window described below,
which draws the same coordinate arrays and verifies complete portable state
before/after an observed frame.

## Executable acceptance

`--check` first validates input wire truncation, finite/range/bool/generation guards.
It then starts a no-GPU CPU server, an independent GPU client, and a later no-GPU CPU
client. The launcher waits for the authority's tick-90 signal before starting the
late client; admission order does not depend on process startup time. Prepared queues
delay application packets by 20–99 ms, drop each seventh,
duplicate each eleventh and deliver by scheduled due time. ENet still performs real
cross-process UDP I/O; this is application delivery impairment, not an operating-system
or routed Internet loss test. The reports assert nonzero actual impairment counts,
accepted/applied inputs, rejected control/token/incarnation attempts, old snapshot
rejection, lifecycle convergence, sleep observations and exact confirmed-event identity.
Final position/linear-velocity tolerance is 0.002 scene units; rotation uses wrapped
angle difference below 0.002 radians and angular velocity differs by less than
0.002 radians/s. These bounds validate the applied authoritative state, not equal
CPU/GPU trajectories between corrections. Joint reports now use their actual
authored global pose rather than an implicit zero coordinate.

The example also injects a client position/impulse error at a validated incoming
correction boundary and checks restore/replay. This timing prevents intervening
physical contacts from erasing the intended test offset before a packet arrives. Two ordinary players, a pin pair, an Area and a transient/recreated crate
exercise contacts, joints, topology and authority changes. At tick 300 the server
asserts that the recreated crate is sleeping, then wakes it with an impulse; clients
must observe both sleep and wake transitions. The latter
client must join at or after tick 90 and after the GPU client. A successful process exit alone is insufficient:
the parent reads all three reports and compares their actual world contents.

Allocation measurements wrap actual SceneTree steps. Tick 300 onward measures
prepared authority/prediction/replay steps on both owner and all managed threads
after each process completes its own first 128 physical steps,
with positive sample counts required. Cold contact construction and lifecycle costs
remain in the separate total replay allocation counter. Each process reserves
262,144 bytes of command records and 36,864 bytes of event records, excluding array
headers. Additional presentation storage, the 4 MiB impairment slab, reusable wire
buffers, engine world/checkpoint buffers, transport state and native/GPU storage are
separate costs. Histories are bounded; no unlimited snapshot list is retained.

## CPU-only and separate numerical profiles

`--check --client-backend cpu` executes the same authority, early prediction client
and late client without an available GPU. The early client report is named cpu.json;
the default GPU scenario retains gpu.json. `--threaded` passes the captured numerical
thread policy to every child before its world is created. The CPU-only check rejects
window presentation; native-window and CPU-server/GPU-client gates remain separate.

The instrumented host observes actual CPU finalization intervals, counts them and
checks their thread against the captured policy. Reports distinguish SeparateThread,
BackendInstrumented, NumericalSteps and NumericalThreadVerified. The public-only
example records the selected policy; it does not claim private lane observation.
Both profiles use unchanged command/event/topology, impairment and convergence
assertions, including positive warm prediction/replay counts and strict 0/0 owner/
all-thread allocation. They retain the original .002 unit/radian/unit-per-second
authoritative-state tolerances; this does not require matching unsynchronized paths.

On 2026-10-13 the initial instrumented CPU-only serial and separate-thread runs
completed tick 360 on all three processes. Each generated 14 authority events;
the early client confirmed all 14, while the late client joined at tick 123 and
confirmed the eight events after its baseline. Actual numerical intervals were
360/1315/860 in serial and 360/1267/845 with a separate thread (authority/early/late).
Each client's prepared prediction and replay allocated zero owner/all-thread bytes.
These are individual live network observations, not a scheduling benchmark.
Evidence is retained under bin/physics-network-thread/2026-10-13. These are CPU-only
observations; [the current mixed-backend regression](#network-regression-after-spring-sleep-fixes-2026-10-15)
provides later headless GPU evidence. Native/foreign-platform, native allocator and
final performance acceptance remain open.

A subsequent same-source check exposed a late client joining at tick 308 with only
81 completed steps. The old tick-only classification called its first cold replay
warm and recorded 571008 owner / 572248 all-thread bytes. The classification now
requires 128 completed steps in that process before the measured interval; positive
warm prediction/replay sample assertions remain mandatory. Cold replay stays in
ReplayAllocations rather than being subtracted. CompletedSteps/WarmupSteps expose
the preparation boundary in every report. `--late-join-tick 310 --ticks 600` makes
late admission after the old threshold reproducible while allowing real preparation
and measured work. The default mixed-backend run remains 360 ticks; CPU test-host
profiles use 600 ticks. Extending duration retains every lifecycle/physical check.
Admission after tick 300 also missed the original crate wake, so the extended
scenario schedules an explicit crate sleep 32 ticks after the second peer's
admission, an impulse wake 16 ticks later and a control handoff after another 16.
Sleep/wake observations remain required on both clients. The final torque impulse
uses the selected final tick so the convergence check still contains real angular
state. Earlier lifecycle and natural-sleep/wake cases remain in the same run.

The final frozen-build matrix passes seven 600-tick three-process runs: instrumented
serial/separate profiles with tick-90 and tick-310 admission, public-only serial/
separate profiles with tick-310 admission, and the cpu-threaded environment selector.
Both hosts use the same runtime assembly. In the instrumented late separate-thread
run, the client joined at tick 344, executed 925 actual numerical intervals and
measured 190 predicted plus 607 replay steps at 0/0 owner/all-thread bytes. It
confirmed exactly nine events after its baseline from the authority's 21-event
journal and observed four wakes. Its 5696 cold replay bytes remain reported.
The matrix and manifests are retained in
bin/physics-network-thread/2026-10-13/final-verified. These runs establish CPU
integration through the stated public protocol. Later headless GPU evidence is
recorded in [the current regression](#network-regression-after-spring-sleep-fixes-2026-10-15);
this CPU-only matrix establishes no window rendering.

On source baseline ba48113bd825f3542a76b1b2e1ee4081cc01c6fd plus this change, the
late separate-thread CPU run reports the following full step/correction costs.
Timing includes cold preparation and actual concurrent packet handling; this is
one impaired loopback run, not an isolated scheduling or GPU comparison.

| Process | Step p50 / p95 / p99 ms | Correction + replay p50 / p95 / p99 ms | Sent / received application bytes |
| --- | --- | --- | --- |
| CPU authority | 0.1001 / 0.1629 / 5.0383 | — | 685864 / 200396 |
| Early CPU client | 0.0899 / 0.1373 / 0.2855 | 0.8510 / 1.4539 / 18.5147 | 141456 / 463700 |
| Late CPU client | 0.0846 / 0.1253 / 0.1553 | 0.8011 / 1.1100 / 2.7161 | 59192 / 222196 |

That recorded sixteen-slot matrix retains 131072 command-history bytes and 36864
event-history bytes, excluding array headers and other storage. The current
32-slot scene uses 262144 command bytes; [measured object totals](physics-network-memory.md)
include headers and the complete controlled scene/network graph. Cold replay totals
are 11696/5696 bytes on early/late clients, while all measured warm groups remain
0/0. No window, GPU transfer/wait or native allocator measurement is inferred.

## Remaining limits

The executable scenario is application policy built on the common public API. It
does not establish arbitrary game-state rollback, speculative client lifecycle,
production authentication, reconnection/migration, cross-platform numerical bounds,
native allocator accounting or universal performance bounds. The independent GPU backend's small-world
replay still incurs substantial submission/publication overhead; the measured
network behavior does not close the overall GPU performance gate. The broader
[physics contract audit](physics-contract-audit.md) remains authoritative for open work.

## Recorded Linux run

On 2026-10-10, Release/.NET SDK 10.0.101, Ryzen 7 5700X and RTX 3090 Ti, the
instrumented test completed all three processes at tick 360. The authority applied
424 commands and generated 13 physical events. The GPU client confirmed all 13;
the CPU client joined at tick 91 and confirmed the eight events after its baseline.
Both rejected obsolete snapshots, recovered the injected error, reproduced the
final state and observed sleeping bodies waking. Three earlier consecutive
consumer-only runs also passed the delivery/identity/event/convergence checks.

| Process | Step p50 / p95 / p99 ms | Correction + replay p50 / p95 / p99 ms | Corrections / replayed ticks | Sent / received application bytes |
| --- | --- | --- | --- | --- |
| CPU authority | 0.0884 / 0.1524 / 6.0802 | — | 0 / 0 | 443,532 / 109,104 |
| GPU client | 3.6068 / 4.9024 / 6.6928 | 52.7828 / 82.1463 / 95.5141 | 65 / 931 | 49,528 / 235,720 |
| Late CPU client | 0.0733 / 0.1156 / 0.1523 | 0.6238 / 1.0344 / 21.6190 | 53 / 723 | 59,828 / 196,808 |

Latency samples include startup/contact preparation; this is a live concurrent
network run, not an isolated CPU/GPU solver comparison. Warm allocation samples
start at tick 300: authority 61 steps, GPU 54 predicted + 149 replayed steps, late
CPU 61 predicted + 179 replayed steps. Each measured group allocated 0 managed bytes
on owner and all threads. Creation, wire-buffer preparation and cold replay allocation
are reported separately and are not called allocation-free.

The test-only probe recorded 1,353,456 GPU upload bytes, 4,390,212 readback bytes,
38,002 completed submissions and 2,574.9583 ms of fence wait over the entire GPU
client lifetime, including setup, publication, prediction and replay. A standalone
consumer report sets BackendInstrumented=false rather than pretending those private
counters were measured. Snapshot publication reads physical state needed by the
public scene/network view; each replay step currently also retains ordinary scene
publication/callback semantics. These many submissions and the measured correction
latency identify remaining GPU integration work. No networked rendering/FPS or
production-performance acceptance follows from successful final-state convergence.


## Joint wake allocation regression, 2026-10-10

A later separate-process run found 856 managed bytes in a late CPU client's
warmed replay. Temporary allocation tracing located `b2CreateJointInGraph` while
player input woke a sleeping connected component: contacts took their colors
first, moving an existing pin into a previously unused joint color. That color
allocated a two-slot joint array although the pin's former color retained an empty
prepared buffer. The fixed CPU path transfers that empty buffer before allocating.
It leaves populated colors untouched and retains ordinary growth when no empty
prepared buffer is available. No public state, solver order or GPU mirror is added.

A captured sequence reproduces the problem without sockets: the baseline runtime
allocates 632 bytes across its 220 warmed steps, while the candidate runtime allocates
zero with the same trace and replay driver. The original live 856-byte observation
and the isolated 632-byte observation are both retained, rather than treated as
identical measurements. `PhysicsServerJointTests.VerifyWakeStorage` reduces the
case to three bodies, a pin and a real contact. Its first sleep/wake recoloring
allocated 632 bytes before the fix and zero owner/all-thread bytes afterward on
CPU and GPU. It also checks component wake, joint identity and anchor response;
the one-unit anchor tolerance includes half-unit contact slop and integration error.

The local trace, baseline/candidate results and diagnostic sources are retained in
`bin/physics-replay-validation/2026-10-10/`. Diagnostic logging and recording hooks
are not part of the runtime or example. This resolves the observed allocation
source; new populations/capacity and arbitrary gameplay remain separate workloads.


Three subsequent ordinary separate-process checks passed without diagnostic hooks.
The combined measured groups were 183 CPU-authority steps, 110 predicted plus 440
replayed GPU-client steps, and 182 predicted plus 483 replayed late-CPU-client steps.
Every group reported zero owner/all-thread managed bytes. Joint, CPU/common-world
checkpoint, portable snapshot, CPU-hosted GPU joint controls and the default test
runner also passed. Reports retain whole-step/correction latency and traffic for
these live concurrent runs; they are not a controlled backend speedup comparison.


## Stronger process acceptance, 2026-10-12

The launcher denies GPU startup in each CPU process with both Vulkan loader path
variables and the SDL GPU driver setting. Display variables are removed and the
video driver is dummy. Before creating its CPU session, each worker exercises
strict public SpaceCreate(GPU), requires rejection, and records
GPUUnavailableVerified=true. Both workers must still complete tick 360 without
creating display/rendering services. A normal-GPU negative control rejects the
no-GPU verification request when the same public operation succeeds. The GPU
client inherits its ordinary driver environment and must actually select GPU.

Correction now injects .45 radians of rotation and 2 radians/s of angular velocity
at the same validated boundary as the position/impulse drift. Client reports must
observe a wrapped angular correction above .1 radians. An authority torque impulse
of 800 at tick 360 produces nonzero final angular velocity; the parent requires
at least one absolute angular velocity above .05 radians/s and compares complete
poses/linear/angular velocities across all three reports. This includes the
reported pin author's pose and the independently solved body states.

The angular sample uses one retained 16-float array (64 payload bytes) per process,
outside warm physics-step brackets. It does not change the wire command format or
physical snapshot API. The example remains a bounded application protocol; no
rendered smoothing, networked-window FPS, WAN impairment, cross-platform bounds
or complete arbitrary joint/contact topology acceptance follows from this test.


### Instrumented Linux sample on current sources

The 2026-10-12 run uses the same scenario code in all three processes, based on
main d4073d59 plus this verification change. The [artifact manifest](../../bin/physics-network-acceptance/2026-10-12/final-instrumented/evidence.json)
records complete source, test/runtime binary and report SHA-256 values.

| Process | Join tick | Corrections / replay ticks | Warm ordinary / replay samples | Step p50/p95/p99 ms | Correction p50/p95/p99 ms | Sent / received bytes |
| --- | ---: | ---: | ---: | --- | --- | ---: |
| server (CPU) | 0 | 0 / 0 | 61 / 0 | 0.0789/0.1258/5.4861 | 0.0000/0.0000/0.0000 | 470,252 / 95,444 |
| gpu (GPU) | 1 | 66 / 850 | 45 / 161 | 4.6033/5.7100/6.5292 | 58.3020/78.1695/103.6230 | 45,144 / 271,584 |
| late (CPU) | 121 | 48 / 619 | 61 / 169 | 0.0704/0.1141/1.9869 | 0.6317/0.9584/27.9912 | 53,792 / 195,760 |

All sampled warm physics/replay brackets measured 0 owner/all-thread managed
bytes. The authority applied 290 commands and numbered twelve confirmed effects;
GPU confirmed twelve and the late client confirmed six after its sequence-six
admission baseline. Both clients recorded .45-radian drift correction. Final maximum
absolute angular velocity was 7.7088966 radians/s in all three reports, with the
full state checks below the stated tolerances.

The instrumented GPU client recorded 1,257,800 upload bytes,
4,715,216 readback bytes, 29,720 completed submissions and
2486.3535 ms of aggregate GPU wait across setup, prediction, publication and replay.
These lifetime counters do not isolate one correction or one physics kernel. Each
process retains 131,072 command-record and 36,864 event-record payload bytes, plus
the new 64 rotation-sample payload bytes and existing separately bounded buffers.

This is one small-world networking sample on Linux. It demonstrates the named
protocol/state gates and records substantial GPU replay overhead; it does not
establish the target massive-scene speed advantage or final performance acceptance.

## Rendered client window

`--check --window-client` retains the same impaired three-process protocol while
the GPU client opens a real 800 x 400 window. `--renderer gpu|compatibility` chooses
its renderer independently of GPU game physics; the CPU server and late CPU client
still deny GPU startup and create no display/render service. Manual clients use
`--window`, with optional `--renderer` and `--capture` output. Scripted numbered
input, ownership/lifecycle changes and final authority convergence are unchanged.

NetworkWindow uses public Engine.Run and a presentation-only scene tree.
NetworkWorld retains the existing fixed-step game tree: rendering never adds an
automatic game-world tick. Polling, prediction and replay still use 1/60-s steps.
Drawing fits the entire arena and current solved/presented actor positions into
the view without changing any physical node. White centers show presented poses;
orange centers show sufficiently separated solved poses during local correction.
The yellow line connects the two presented pin bodies. Remote interpolation
counters require an interpolated view that differs from the current solved pose.

At one visibly separated local correction, the consumer captures the complete
portable physical snapshot before drawing and after FramePostDraw and requires
identical encoded bytes. Each body's transform also stays unchanged during draw.
Pixel checks require distinct white/orange centers, with 0.05 RGB tolerance for
raster/color conversion; center separation exceeds eight pixels for radius-three
markers. The resulting PNG is saved as `gpu-window.png`. This proves actual
presentation at that boundary without asserting cross-backend bitwise identity.

Reports add renderer, requested Disabled/effective VSync, MaxFPS, actual rendered
frame count/FPS, frame p50/p95/p99,
owner/all-thread window allocations and rendered interpolation/smoothing counts.
FPS and frame intervals span the first through last completed draw and include
polling, prediction/correction/replay, input, rendering and the one-time observation
work between those boundaries. Initialization/teardown and report serialization
are outside that bracket. Frame percentiles use the first up to 4096 complete
intervals; frame count/FPS and allocation totals cover all completed intervals. Existing physics/replay warm allocation and timing
brackets remain separate. Release uses the same non-tiered compilation policy
as the test host, so prepared-code measurements retain a defined runtime mode.
An earlier default-runtime headless trial recorded 24,624 non-owner bytes during
a GPU warm replay bracket; subsequent identical default and non-tiered controls
passed. That intermittent background allocation was not attributed to a physics
operation, and the original failed report remains retained. The window adds no claim of whole-loop zero allocation
or a CPU/GPU speed comparison.

The frozen executable runs and images are retained under
`bin/physics-network-window/2026-10-12/final-prepared/`. WAN impairment, foreign-platform
numerical/render bounds, native allocation totals, arbitrary game topology and
final full-physics performance acceptance remain open. Historical headless
measurements above retain their original scope and revisions.

### Frozen Linux window observations

The five-profile run at `1f5c8dad7cc51edea46639f8d1fec38d4167bfdf` uses one
copied runtime for example headless, both example renderers and instrumented
headless/GPU-window cases. Every process check completes the same impaired
CPU-authority/GPU-client/late-CPU scenario through tick 360 and final convergence.

| GPU client host | Frames / actual FPS | Frame p50 / p95 / p99 ms | Effective VSync | Window owner/all managed B | Drawn smoothing / interpolation samples |
| --- | --- | --- | --- | --- | --- |
| Public example, GPU renderer | 334 / 52.4136 | 6.9444 / 69.5450 / 96.3456 | Enabled | 7,822,656 / 7,822,656 | 281 / 183 |
| Public example, compatibility renderer | 4984 / 784.1918 | 0.1322 / 4.4967 / 63.2945 | Disabled | 7,959,680 / 7,959,680 | 2734 / 2119 |
| Instrumented host, GPU renderer | 318 / 49.6862 | 6.9492 / 73.2953 / 95.7834 | Enabled | 7,822,872 / 7,822,872 | 269 / 152 |

All requested Disabled VSync with MaxFPS=0. Actual presentation differs, so these
are observed native-client costs, not a renderer or CPU/GPU physics speedup claim.
The compatibility frame percentiles cover the first 4096 intervals; other rows
cover all completed intervals. The table retains whole-window network/lifecycle/
observation allocations. Warm predicted and replayed physical steps remain 0/0
owner/all-thread bytes for every client/server profile; the window counts do not
replace or relax that physical gate.

Source SHA-256 is `e879eb369786b229952d0ec93b6a01cdc56fa35041e08d51f31e0fb8194341fc`.
The complete runtime manifest and source/revision are unchanged across all five
runs. The GPU-renderer example PNG is
`5a46e48ae335b726cc37aaf2a4c8525167aa98d9198655088eecb58bad2d848f`;
compatibility is `33857c36bd0cf0d06c1fce142985b03d01f08a5ac2b884c917704d457de467fb`.


## Frozen process/window checks at 7c37a291, 2026-10-13

The instrumented public example ran three real ENet processes twice using the
byte-identical frozen runtime from the complete scene measurement above. CPU
authority and late CPU client removed display access and required actual strict
GPU startup rejection before their working CPU worlds. The early client selected
GPU physics with either GPU or compatibility rendering. Every process completed
tick 360; both parent checks exited zero.

| Renderer / process | Join tick | Corrections / replay ticks | Step p50 / p95 / p99 ms | Correction p50 / p95 / p99 ms | Sent / received application B |
| --- | ---: | ---: | --- | --- | ---: |
| gpu / server | 0 | 0 / 0 | 0.0826 / 0.1248 / 5.6666 | 0.0000 / 0.0000 / 0.0000 | 469304 / 92268 |
| gpu / gpu | 1 | 69 / 843 | 4.7845 / 5.6465 / 5.9987 | 58.5214 / 77.3214 / 84.5448 | 43524 / 275908 |
| gpu / late | 123 | 46 / 614 | 0.0736 / 0.1142 / 1.8890 | 0.6953 / 0.8981 / 30.4094 | 52776 / 190572 |
| compatibility / server | 0 | 0 / 0 | 0.0867 / 0.1359 / 5.5279 | 0.0000 / 0.0000 / 0.0000 | 466212 / 93316 |
| compatibility / gpu | 1 | 69 / 853 | 4.6651 / 5.7695 / 6.5362 | 57.7216 / 79.1319 / 101.8850 | 45144 / 274240 |
| compatibility / late | 123 | 47 / 619 | 0.0754 / 0.1215 / 1.8983 | 0.6641 / 0.9385 / 30.0676 | 52744 / 189148 |

Numbered input, generation/ownership errors, body creation/removal, contact/joint
response, sleep/wake, late admission, 20–99 ms application delay, loss/duplication/
reordering, stale rejection, injected position/rotation divergence and exact-once
confirmed effects all execute with positive assertions. Final identity/topology/
control epochs, sleep and numerical state converge within the unchanged .002
position/velocity/angle/angular-velocity bounds. GPU/local smoothing and remote
interpolation are rendered while the encoded physical state remains unchanged.

| GPU-client renderer | Window FPS / frames | Corrections after drift | Warm ordinary / replay samples | Upload / readback B | GPU submissions / lifetime wait ms |
| --- | ---: | ---: | ---: | ---: | ---: |
| gpu | 50.5363 / 317 | 35 | 38 / 136 | 1227624 / 4597832 | 28850 / 2525.2924 |
| compatibility | 831.6008 / 5193 | 36 | 40 / 141 | 1242072 / 4673644 | 29282 / 2521.7569 |

Every process has positive warmed physics samples and every client has positive
warmed replay samples; all those brackets measure zero owner/all-thread managed
bytes. Each retains 131,072 command-record and 36,864 event-record payload bytes,
with existing bounded packet, snapshot, interpolation and visual buffers accounted
separately. These payload counts exclude array headers and are not whole-process
memory. GPU waits/transfers above cover setup, publication, ordinary prediction
and replay, not only the sampled physics loop.

The complete network/window applications still allocate: GPU-rendered client
owner/all-thread totals are 7,115,976/7,115,976 B and compatibility totals are
7,252,984/7,252,984 B; cold/lifecycle total replay allocations are
1,056,448/1,064,664 B. These are separate from the zero warmed physics brackets.
GPU small-world correction medians near 58 ms remain a real cost; massive-scene
GPU advantage does not imply a small-world or replay advantage.

GPU rendering requested Disabled VSync but reported Enabled; compatibility
reported Disabled and both had MaxFPS zero. Thus 50.5363 versus 831.6008 FPS is
recorded renderer/window throughput under different presentation modes, not a
controlled physics-throughput comparison. Both retained PNGs were visually
checked and show the live network actors and world.

Evidence is bin/physics-goal-acceptance/2026-10-13/frozen-7c37a291/
network-window-gpu and /network-window-compatibility with exact process outcomes
and manifests in the parent. Main changed during the preceding scene run; these
network results belong to the immutable 7c37a291 runtime, not newer main. No full
platform, arbitrary-provider geometry or repeatability acceptance is inferred.


[Software Vulkan validation](physics-backends.md#software-vulkan-functional-checks) now executes
the resident GPU modules, point-range/failure checks and common public physics suites
without physical-GPU load. A separate CPU authority/GPU prediction/late CPU client
run covers the existing impaired-network scenario. Hardware GPU execution, performance
and full-goal acceptance remain open; software timing is not hardware evidence.

Instrumented test-host reports include GPUDriver and GPUDeviceName from the actual
retained compute device. The executable example still consumes only public engine
APIs; backend introspection remains in PhysicsNetworkProbe. Empty strings in the
public-only host mean no device observation, not a hardware identity.


## Three joint families across network correction (2026-10-14)

The live scenario includes PinJoint, an 80-unit GrooveJoint and a DampedSpringJoint
with 30-unit rest length, 30 kg/s² stiffness and 8 kg/s damping. Separate fixed
anchors and four extra endpoints bring final topology to fourteen objects,
within the existing sixteen-slot budget. The complete manifest validation,
joint-first retirement/body-first construction, snapshot object census and late
admission handle every joint role. The wire retains application-owned recipes;
backend handles and local RIDs do not become network identities.

Numbered impulses from tick 120 onward act on the guided (.5,-.1) and sprung
(.2,-.1) bodies during authority, prediction and replay. Continuous bounded input
ensures both fast and coalescing clients exercise post-correction impulse work;
three isolated tick-specific kicks could already be confirmed before a slow
client simulates those ticks. Every actual complete tick verifies transformed
pin-anchor separation, finite groove containment and spring length. Pin/groove
error must remain below .5 scene units, less than 1/24 of the 12-unit body radius.
The spring bound is 130 units: gravity gives a 980/30-unit static extension above
the 30-unit rest length, with room for the specified finite impulses and damped
transients. Free fall escapes the bound; this is a physical invariant, not a
requirement for identical CPU/GPU trajectories. Startup checks disable each
joint's MaxForce and require the real trajectory guard to fail, after one normal
constrained tick. These checks use the public CPU API without a graphics device.

Reports retain JointSteps, JointKicks, MaxPinError, MaxGrooveError and
MaxSpringLength. The parent requires checks for every completed authority,
predicted and replayed step, positive kick counts, all three final joint identities
and unchanged complete authority correction bounds (.002 for poses/velocities,
exact identities/generations/owners/epochs/sleep and confirmed event contents).
Instrumented GPU reports must carry actual device submissions, positive transfer
bytes and completed waits. CPU numerical interval threading retains its independent
probe; a GPU solver thread seen in process inventory is lifetime evidence rather
than a measurement of every numerical invocation.

The implementation retains impaired separate-process ENet, late joining, lifecycle,
sleep/wake, ownership transfer, history budgets, interpolation/smoothing and strict
zero warmed owner/all-thread prediction/replay allocation checks. Physical hardware,
real-window/FPS/native allocator and full physics/performance acceptance remain open.


The standalone example disables JSON reflection. Its startup exposed missing
compiled metadata for built-in MSAA/LCD settings; those schemas now initialize
and round-trip through public ConfigFile before all network checks. The test host
and standalone example must both execute, because the reflection-enabled test
host alone could conceal this startup failure. [The native process profile](#nativeaot-network-processes-2026-10-15)
now verifies a separate strict NativeAOT publish.


Four Linux profiles passed on one frozen runtime revision (2026-10-14):

| Host/policy | Final tick / late admission | Warm authority | Warm GPU prediction / replay | Warm late CPU prediction / replay |
| --- | --- | --- | --- | --- |
| example-serial | 360 / 123 | 61 | 13 / 41 | 61 / 176 |
| tests-serial | 900 / 379 | 601 | 19 / 221 | 428 / 1298 |
| tests-thread | 900 / 353 | 601 | 41 / 487 | 481 / 1468 |
| example-thread | 900 / 343 | 601 | 68 / 645 | 486 / 1441 |

Every warm sample in the table reports 0/0 owner/all-thread managed bytes.
The authority applied 197/451/501/511 numbered player commands respectively.
Maximum pin error was .218727 units, groove error .000031 units and spring
length 65.817 units; the existing bounds were retained. Every profile delivered
the complete post-admission authority event sequence exactly once, with final
fourteen-object identities/poses/velocities/sleep converging at the original bounds.

The longer software-Vulkan profiles use 900 ticks and admission after tick 310.
An initial 600-tick run admitted its late CPU client at tick 390 and applied only
136 inputs, below the unchanged >180 gate. Extending the run adds 300 authority
ticks; no operation, object, tolerance or allocation gate is reduced. The default
360-tick public example remains checked with admission after tick 90.

Instrumented GPU serial/separate reports retain 643448/1210728 uploaded bytes,
2253920/4093144 readback bytes and 12562/22458 submissions. Total completed wait
was 12099.1352/9470.0663 ms. Full-step p50/p95/p99 was
16.6123/79.9145/82.7428 ms and 14.0340/18.6431/21.3849 ms; correction including
real replay was 290.1953/740.4986/883.3356 ms and 171.7658/231.8722/279.7625 ms.
These are impaired software-Vulkan process runs, with other verification work
overlapping; they establish execution/transfer cost, not isolated scheduling
benefit or target GPU speed. Process maps show llvmpipe only in the GPU child,
no NVIDIA library in any game process, and solver threads only in requested
separate profiles. Both CPU children require strict GPU-startup denial.

Evidence is retained under bin/physics-network-current/2026-10-14/final4.
The standalone example has JSON reflection disabled and passes its compiled
MSAA/LCD metadata checks; no NativeAOT publication or window rendering is inferred.


## Current headless production network matrix, 2026-10-14

Seven actual three-process scenarios on local main b06d137c255052a81ff4ed82084d8091f04589a7
passed at tick 600 with late admission requested at tick 310. Four runs use the
public-only example (CPU/GPU early client, serial/separate numerical execution);
three use the instrumented test host (CPU separate and GPU serial/separate). The
21 selected child processes share one byte-identical engine assembly. CPU server
and CPU clients require strict GPU startup rejection; only GPU clients load explicit
software Vulkan. External process observation enumerates all parent tasks and ignores retired
children with empty command lines. It records every live role and zero/one numerical
threads; instrumented CPU counters additionally verify actual interval execution
on the requested lane.

| Profile | Early / late join tick | Early warm step / replay samples | Late warm step / replay samples |
| --- | ---: | ---: | ---: |
| example-cpu | 1 / 347 | 301 / 915 | 183 / 549 |
| example-cpu-thread | 1 / 341 | 301 / 897 | 190 / 603 |
| example-gpu | 1 / 341 | 28 / 333 | 191 / 626 |
| example-gpu-thread | 1 / 342 | 46 / 311 | 190 / 608 |
| instrumented-cpu-thread | 1 / 345 | 301 / 906 | 185 / 540 |
| instrumented-gpu | 1 / 345 | 33 / 330 | 185 / 555 |
| instrumented-gpu-thread | 1 / 345 | 29 / 338 | 185 / 540 |

Every selected authority has 301 measured warm physics steps. All measured ordinary
and replayed physical steps allocate 0/0 owner/all-thread managed bytes. Every
client has positive warmed replay, correction, interpolation and smoothing counts.
The launcher verifies adverse delivery, numbered input, invalid ownership/token/
generation rejection, creation/removal/reincarnation, sleep/wake, all three joint
families, injected linear/angular divergence, and exact confirmed-event contents
without duplicated effects. Final 14-object identity/generation/ownership/epoch,
pose, velocity and sleep agree within the existing .002 numerical bounds. This
proves the scripted production workflow, not arbitrary gameplay rollback.

The following instrumented separate-thread mixed run records the complete physical
step and snapshot-application/replay distributions. Software-driver timings are
functional diagnostics, not target-device performance comparisons.

| Process | Step p50 / p95 / p99 ms | Correction p50 / p95 / p99 ms | Sent / received application B |
| --- | --- | --- | ---: |
| server | 0.1149 / 0.1758 / 3.7253 | 0.0000 / 0.0000 / 0.0000 | 1069196 / 79400 |
| gpu | 13.4365 / 14.2836 / 14.7390 | 163.0733 / 172.1447 / 193.2165 | 24524 / 668624 |
| late | 0.1018 / 0.1357 / 0.1585 | 1.0493 / 1.3775 / 6.8823 | 58716 / 327900 |

GPU lifetime counters record 989,424 upload B, 3,564,484 readback B,
19,845 submissions and 7335.5451 summed wait ms across setup, ordinary
prediction, restoration and replay; they are not a per-step transfer minimum. Each
process retains 131,072 command-record and 36,864 event-record payload bytes,
excluding array headers, packet slabs, other managed storage and native/device
memory. No window was opened. Native allocation, cross-platform/owner acceptance,
arbitrary game-state rollback, massive target-GPU advantage and real-window FPS
remain open. Physical GPU load remains deferred until its power prerequisite is
verified; software evidence does not close that gate.

Evidence: bin/physics-production-network/2026-10-14/validation.json and its
retained reports/logs/manifests. The frozen source has 4,222 files, SHA-256
3a99a26f06b8e368a50a20bdfcd17fda6ccf6e647374dbef7367e16d5fe4c919; both hosts load engine DLL SHA-256
40477b2239d37517f14141c5d40f84af8590bdaa3383de0547dd1adfe37c2bf5.


## Network regression after spring sleep fixes, 2026-10-15

Eight frozen-source 900-tick runs refresh the existing headless workload after
the initial RigidLinear lock and supported-spring sleep/wake fixes. Six runs use
a CPU authority, software-Vulkan early client and late CPU client; two CPU-only
controls retain the same protocol and physical workload. Both public example
and instrumented hosts execute. Requested late admission is 90, or 310 in the
two explicitly labelled late profiles. All 24 selected child processes pass.

| Profile | Early / late admission | Early warm step / replay | Late warm step / replay |
| --- | ---: | ---: | ---: |
| instrumented-gpu-serial-900 | 1 / 131 | 59 / 590 | 601 / 1884 |
| instrumented-gpu-threaded-900 | 1 / 141 | 28 / 325 | 601 / 1861 |
| instrumented-gpu-serial-late310-900 | 0 / 363 | 27 / 324 | 460 / 1445 |
| instrumented-gpu-threaded-late310-900 | 1 / 365 | 26 / 312 | 456 / 1423 |
| public-gpu-serial-900 | 1 / 132 | 27 / 320 | 601 / 1793 |
| public-gpu-threaded-900 | 1 / 135 | 24 / 277 | 600 / 1837 |
| instrumented-cpu-serial-900 | 1 / 139 | 600 / 1808 | 601 / 2006 |
| instrumented-cpu-threaded-900 | 1 / 140 | 600 / 1846 | 601 / 1835 |

Every authority has 601 warmed measured physical steps. All measured ordinary
and replayed physical steps retain 0/0 owner/all-thread managed bytes. The
unchanged checks require positive impaired delivery, correction/replay,
interpolation/smoothing, lifecycle/ownership, sleep/wake, joint kicks and exact
post-admission confirmed effects. Final fourteen-object identities, state and
sleep converge at the existing .002 numerical bounds. Maximum pin/groove errors
are 0.218787/0.000031 scene units; maximum spring length is 65.816680 units.
Their existing .5/.5/130-unit bounds remain unchanged.

One initial 600-tick separate-GPU late-admission candidate completed but did not
reach 128 local physical steps, so its warm acceptance correctly failed. The
canonical comparison extends every profile to 900 ticks without reducing its
objects, features, tolerances or positive-sample requirements. The ignored
observer enumerates children of all parent tasks; its first main-task-only
sample missed two otherwise passing children and is retained as a candidate.

The instrumented separate-thread late-admission run reports full step and
correction/replay distributions below. These software-driver loopback timings
are diagnostics, not an isolated scheduling or target-GPU comparison.

| Process | Step p50 / p95 / p99 ms | Correction p50 / p95 / p99 ms | Sent / received application B |
| --- | --- | --- | ---: |
| server | 0.2199 / 0.3660 / 2.1104 | 0.0000 / 0.0000 / 0.0000 | 1768684 / 141092 |
| gpu | 23.8583 / 28.2263 / 28.2293 | 289.7416 / 332.4032 / 399.2790 | 15700 / 1045052 |
| late | 0.2247 / 0.3671 / 0.4850 | 1.8742 / 2.8157 / 5.8518 | 125644 / 654384 |

GPU lifetime totals: 838,384 upload B, 2,889,656 readback B,
15,933 submissions and 10383.1286 summed wait ms across setup,
prediction, correction and replay. Each process retains 131,072 command-record
and 36,864 event-record payload bytes; these exclude headers, packet slabs and
other managed/native/device storage. Only GPU children map lavapipe; all CPU
children exercise explicit GPU-startup denial. Named solver threads occur only
in separate profiles, with instrumented CPU intervals verified on that lane.

This refreshes the scripted workload rather than adding networking features.
Arbitrary/dense coupled graphs, native execution beyond the profile below, foreign platforms,
native allocator totals, target GPU advantage and real-window FPS remain open.
No window or physical GPU was used. Evidence:
bin/physics-network-current/2026-10-15/validation.json and its frozen manifests,
reports and logs.


## NativeAOT network processes, 2026-10-15

The public PhysicsNetwork example now publishes with NativeAOT and linker/compiler
warnings treated as errors. Its launcher formerly used Assembly.Location when
restarting a framework-dependent entry DLL, which produced IL3000 for native
publication. It now takes that DLL path from the application command line;
direct native and ordinary apphost execution restart Environment.ProcessPath.
The engine API, numerical rules and network protocol are unchanged.

Four native and four managed control profiles pass 900 ticks with admission
requested after tick 310. All eight parents enforce the same physical, protocol,
lifecycle, ownership, sleep, joint, event and final convergence assertions.

| Entry / client / lane | Early / late admission | Early warm step / replay | Late warm step / replay |
| --- | ---: | ---: | ---: |
| aot-cpu-serial | 0 / 316 | 601 / 1906 | 535 / 1643 |
| aot-gpu-serial | 0 / 315 | 102 / 692 | 547 / 1728 |
| aot-cpu-threaded | 0 / 317 | 601 / 1914 | 535 / 1654 |
| aot-gpu-threaded | 0 / 315 | 97 / 695 | 536 / 1589 |
| dotnet-cpu-serial | 1 / 341 | 600 / 1821 | 495 / 1571 |
| dotnet-gpu-threaded | 1 / 341 | 82 / 656 | 491 / 1547 |
| apphost-cpu-serial | 0 / 381 | 601 / 2085 | 445 / 1389 |
| testhost-cpu-serial | 1 / 345 | 600 / 1818 | 485 / 1431 |

Every authority has 601 warmed measured steps. All measured ordinary and replayed
physical steps retain 0/0 owner/all-thread managed bytes. Native CPU/GPU profiles
cover serial and separate numerical policies; the managed controls exercise both
the dotnet DLL branch, direct apphost restart and linked instrumented test host. All final fourteen-object
identities, generations, owners, epochs and sleep match exactly, with unchanged
.002 pose/velocity/angle bounds and exact post-admission confirmed event contents.

The 24 selected children include twelve native processes. Each native child
executes the same published ELF and maps no CoreCLR; the twelve managed controls
map CoreCLR. GPU clients alone map explicit lavapipe, no process maps NVIDIA,
and named solver threads appear only in separate profiles. Public reports retain
their ordinary policy identity; lifetime observation does not measure every
private numerical invocation.

The native run uses the exact 123-file publish output without helper overlays
or LD_LIBRARY_PATH. The managed example controls retain their own 62-file build output; the linked
instrumented test host has a separate frozen manifest.
The source set and both binary manifests are rechecked after execution. A strict
publish failure before the launcher fix is retained as regression evidence.

This closes the scripted Linux x64 NativeAOT network gate. Arbitrary/dense coupled
graphs, other native platforms, full session/native/device allocation, real-window
rendering/FPS and target hardware performance remain open. The physical RTX
power prerequisite is unchanged. Exact reports, commands and manifests:
bin/physics-network-aot/2026-10-15/validation.json.


## Coupled support across network correction, 2026-10-15

The current example adds a loaded column without removing the earlier players,
separate three-joint trajectories, Area, lifecycle or angular-divergence cases.
Two unit-mass, rotation-locked radius-two bodies share a pin with a four-unit
separation. The upper body slides along a 100-unit groove and carries a spring
with rest length 50, stiffness 20 and damping 4. The lower body rests on a real
static floor. The column occupies collision layer two and is spatially separate
from the earlier scene. Six joint recipes and fifteen body/Area recipes give
21 final objects. The prepared budget grows from sixteen to 32 slots; command
history is now 262144 bytes per process. The existing 36864-byte event journal,
4 MiB impairment slab and 32768-byte packet capacity retain their separate costs.
Version-two application headers reject the old recipe/command layout before
payload decoding. This does not change the engine portable snapshot format.

Every authority, predicted and replayed physical step checks finite poses,
rotation lock, pin offset and groove containment below .5 scene units. For the
radius-two column this is a quarter-radius stability bound, not equality between
unsynchronized backend paths. Actual reported contacts must identify the local
floor RID, InstanceID, object and shape indices exactly, with an upward unit
normal within .001. A sleeping pair must retain upper/lower support heights 23/27
within .15 scene units. Periodic numbered impulses (10,-20), beginning at tick
240 and repeated every 120 ticks, must wake both connected bodies when asleep;
positive contact, sleep, kick and actual wake counters are required in every
process. Clients additionally require immediate imported contacts and sleeping
pairs after real authority corrections.

For contact frames with both bodies awake before and after the step and final
speeds below .1, summed normal impulse balances weight plus the authored elastic
load minus measured momentum change. The one-sided deficit permits the analytic
exponential spring damping bound plus .15 impulse units; .15 is also the allowed
opposite residual. Positive impulse sample counts are mandatory. Frames entering
sleep do not integrate gravity over the entire outer interval, so they use the
separate support-height and sleep assertions rather than the full-duration force
formula. A retained failing probe at tick 630 demonstrated precisely that case:
the pair became asleep and reported the impulse for its active subinterval.
Neither numeric tolerances nor numerical runtime behavior were changed.

Startup CPU controls independently remove floor collision or spring stiffness;
both must fail the real support assertions. Ordinary input validation, the three
separate disabled-joint controls, exact confirmed events, impaired ENet delivery,
late admission and .002 final authoritative bounds remain required. Reports expose
support steps/contact/sleep/wake/kick/impulse-sample counts, imported-contact/sleep
corrections and maximum constraint/impulse residuals.

Nine frozen-source profiles pass 900 ticks with admission requested after tick 310:
four public managed CPU/GPU serial/separate runs, the same four NativeAOT runs,
and an instrumented GPU/separate run. All 27 selected children retain the entire
existing protocol and final-state assertions plus the new coupled checks.
Every authority measures 601 warmed physical steps; all warmed ordinary and
replayed physical brackets allocate 0/0 owner/all-thread managed bytes.

| Entry / client / lane | Early / late admission | Early warm step / replay | Late warm step / replay | Early support wakes / impulse frames |
| --- | ---: | ---: | ---: | ---: |
| dotnet-gpu-serial | 1 / 341 | 67 / 672 | 488 / 1483 | 7 / 224 |
| dotnet-cpu-serial | 1 / 341 | 601 / 1817 | 488 / 1483 | 24 / 709 |
| dotnet-cpu-threaded | 1 / 341 | 601 / 1902 | 488 / 1505 | 24 / 718 |
| dotnet-gpu-threaded | 1 / 341 | 89 / 641 | 487 / 1472 | 7 / 214 |
| aot-cpu-serial | 0 / 315 | 601 / 1838 | 548 / 1715 | 25 / 813 |
| aot-gpu-serial | 0 / 315 | 101 / 692 | 549 / 1722 | 9 / 235 |
| aot-cpu-threaded | 0 / 315 | 601 / 1847 | 547 / 1702 | 25 / 815 |
| aot-gpu-threaded | 0 / 315 | 90 / 643 | 547 / 1707 | 9 / 233 |
| testhost-gpu-threaded | 1 / 346 | 58 / 674 | 487 / 1534 | 7 / 215 |

The instrumented separate-thread run records these complete-path observations:

| Process | Step p50 / p95 / p99 ms | Correction + replay p50 / p95 / p99 ms | Sent / received application bytes |
| --- | --- | --- | --- |
| server | 0.1129 / 0.1759 / 1.3928 | — | 2494668 / 164864 |
| gpu | 13.5804 / 14.8467 / 15.1627 | 165.0199 / 176.5111 / 186.1942 | 35628 / 1451588 |
| late | 0.1038 / 0.1488 / 0.1673 | 1.2438 / 1.6464 / 9.7587 | 131648 / 949120 |

The GPU client executes 35409 submissions, uploads 1859592 bytes and reads
6801932 bytes; accumulated wait is 12069.8134 ms. Its full cold-inclusive
replay allocation is 573432 bytes, outside the strict prepared physical brackets.
These are concurrent impaired loopback observations on explicit lavapipe, not a
physical GPU speed comparison or native/device memory accounting. The full
correction/replay samples include packet application, not only solver intervals.

Every CPU child rejects strict GPU startup and maps no Vulkan software driver.
GPU clients alone map lavapipe; no child maps NVIDIA. Solver threads appear only
in separate profiles. Twelve native children execute the exact 123-file published
ELF closure without CoreCLR; fifteen managed children map CoreCLR. Public example
and instrumented host share one current production runtime DLL. Source and binary
manifests are rechecked after execution. The source base includes the independent
resource-package commits through cf585ca71ad43a597381618fc00956c50b853509; this
network change adds no engine declarations or portable-format changes.

This verifies one loaded graph across real network correction/replay on Linux x64.
Dense/arbitrary coupling, full inherited/extension behavior, foreign platforms,
full session/native/device memory, real-window rendering/FPS and target GPU
advantage remain open. The physical RTX power prerequisite is unchanged.
Commands, all reports, retained failed candidates and manifests:
bin/physics-network-support/2026-10-15/validation.json.
