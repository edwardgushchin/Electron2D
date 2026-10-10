# Authoritative physics network example

Last updated: 2026-10-10

[PhysicsNetwork](../../examples/PhysicsNetwork/README.md) connects the existing
`SceneMultiplayer`/`ENetMultiplayerPeer`, `PhysicsSnapshotMap`, `PhysicsSnapshot` and
world-tick APIs. Its CPU authority and GPU/CPU clients are separate OS processes.
All example code uses public Electron2D types; the test-only partial probe supplies
private GPU transfer counters. No alternative physics solver or private engine API
is embedded in the consumer.

## Tick and authority protocol

The game uses exactly 1/60 s per physical tick and asserts that every SceneTree step
advances its world tick once. Its viewport world is explicitly bound with FindWorld.
Clients send bounded records of tick, network ID, object generation, control epoch
and typed move/jump input. The server derives sender identity from SceneMultiplayer,
checks the controller and incarnation, rejects elapsed/over-budget ticks and
conflicting duplicate commands, and applies accepted input on the numbered tick.
The command window contains 256 ticks with 16 prepared object slots; this game's
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
and smoothing actually executed. They are numerical presentation verification;
there is no window or rendered visual acceptance in this example.

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
Final pose/velocity tolerance is 0.002 scene units for the applied authoritative
state, not a claim of equal CPU/GPU trajectories between corrections.

The example also injects a client position/impulse error at a validated incoming
correction boundary and checks restore/replay. This timing prevents intervening
physical contacts from erasing the intended test offset before a packet arrives. Two ordinary players, a pin pair, an Area and a transient/recreated crate
exercise contacts, joints, topology and authority changes. At tick 300 the server
asserts that the recreated crate is sleeping, then wakes it with an impulse; clients
must observe both sleep and wake transitions. The latter
client must join at or after tick 90 and after the GPU client. A successful process exit alone is insufficient:
the parent reads all three reports and compares their actual world contents.

Allocation measurements wrap actual SceneTree steps. Tick 300 onward measures
prepared authority/prediction/replay steps on both owner and all managed threads,
with positive sample counts required. Cold contact construction and lifecycle costs
remain in the separate total replay allocation counter. Each process reserves
131,072 bytes of command records and 36,864 bytes of event records, excluding array
headers. Additional presentation storage, the 4 MiB impairment slab, reusable wire
buffers, engine world/checkpoint buffers, transport state and native/GPU storage are
separate costs. Histories are bounded; no unlimited snapshot list is retained.

## Remaining limits

The executable scenario is application policy built on the common public API. It
does not establish arbitrary game-state rollback, speculative client lifecycle,
production authentication, reconnection/migration, cross-platform numerical bounds,
rendered smoothing or networked-window FPS. The independent GPU backend's small-world
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
