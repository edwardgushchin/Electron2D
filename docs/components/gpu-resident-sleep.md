# Resident GPU sleep and wake

Last updated: 2026-10-08

## Executing boundary

[GPUPhysicsBodyStore](../classes/GPUPhysicsBodyStore.md) now performs connected-component
sleep and wake on the device. Dynamic bodies default to CanSleep=true and awake;
BodyDefinition can start one asleep. SetSleeping clears dynamic velocity when true;
SetCanSleep(false), explicit velocity (including zero), nonzero impulse and authored
body/shape/joint edits wake affected bodies. Ordered pending commands retain the
latest explicit sleep/velocity/impulse action. Constant force remains configured
while asleep. Static and kinematic roles do not enter dynamic sleep.

Simulate evaluates automatic sleep using independently configurable finite,
nonnegative linear/angular thresholds and a quiet interval. Initial internal values
are 2 scene units/s, 0.13962634 rad/s and 0.5 s. These are internal solver settings;
they do not close the missing public SpaceParameter integration. Step remains the
integration-only control and observes explicit sleep without evaluating automatic
island sleep. SolveConstraints propagates wake without advancing poses or timers.

This implements the resident stage required by [ADR 0054](../decisions/physics.md#adr-0054).
Public backend selection, scene/server ownership/publication, sleep callbacks,
complete queries/events, CCD and network checkpoints/replay remain open. The
current public CPU world and its callback contract are unchanged. Internal selected
snapshots are not event delivery or a portable replay format.

## Device graph and propagation

The graph uses all current nonsensor contact constraints and live pin/groove/spring
connections. Only dynamic endpoints merge. A static floor therefore does not join
otherwise disconnected piles. Sensor overlap never joins components. Speculative
contacts within the requested contact margin conservatively connect nearby bodies.

Parallel edge threads merge roots with monotone compare/exchange; parent walks
compress paths using atomic minimum. Separate dispatches provide visibility between
union, reduction and application. Traversal is bounded by the body population for
corruption detection, not by an arbitrary fixed number of propagation iterations.
The graph is rebuilt from current constraints, so removals also split components.

Generation-qualified prior edges and component roots remain on GPU until the next
build. A removed, reused, teleported or edited endpoint wakes its former neighbours
before the old edges are overwritten. This includes removal/filtering/disposal of a
static support. Old component wake skips newly reused body generations. New contacts
then propagate wake across the current component. A moving static/kinematic surface,
active motor or stretched spring wakes its constrained dynamics. Springs retain the
existing nonzero-force wake policy, including at an offset loaded equilibrium.

Wake occurs before contact/joint effective masses are prepared. If a body wakes
after that substep's initial force pass, the same device force function applies the
missing interval exactly once; no CPU velocity read/patch is involved. Sleepers have
zero effective inverse mass and skip integration/constraint gathering. Their
geometry remains available to broad phase, queries and wake detection.

After solving and pose integration, each dynamic body contributes its quiet clock
to its component's minimum. Both physical velocity and positional-correction
velocity must fit the linear/angular thresholds; CanSleep=false prevents automatic
sleep of its component. Eligible clocks saturate at the quiet interval. A separate
ineligible bit handles zero-duration sleep without sleeping moving bodies. The
entire eligible component sleeps together and its physical velocity becomes zero.

## Inactive-world fast path

The final device pass counts awake dynamics and moving nondynamic surfaces. Its
four-byte scalar travels beside the existing four-byte body error word at the
normal publication fence. ActiveSimulationBodyCount describes the last completed
Simulate stage, not an eagerly updated CPU mirror of all live body states.

A zero count allows subsequent Simulate calls to return without submission only
while body/shape versions, resource geometry epoch, pending commands, gravity, contact/solver policy and
sleep settings remain unchanged. Selected snapshot reads remain available. A body
command already flushed by Read changes the version and therefore still resumes
simulation. Failed-world validation precedes the fast path. No elapsed time is
accumulated for an inactive world. Other scene scheduling/callbacks remain outside
this internal component; this optimization does not authorize skipping them.

## Storage and traffic

| Storage | Purpose / size |
| --- | --- |
| Existing device body | Still 80 B; unused velocity.w now holds the quiet clock, flag bits hold sleep policy/state and pending wake. |
| Component graph | 16 B per retained body slot: parent, aggregate flags, minimum-clock bits, generation. |
| Prior/current edges | 16 B per retained contact-point-plus-joint-slot capacity; endpoint indices/generations. No CPU graph mirror. |
| Selected Snapshot | 48 B: unchanged exposed pose/velocity vectors plus quiet clock, sleep flags and padding. Read only requested bodies. |
| Body status | 8 B: numerical error and final active-body count. Shared command/read fence; no new sleep readback or wait. |

At 65,536 body slots the graph adds 1 MiB. Edge storage follows the retained
power-of-two contact/joint high-water capacity. Authored body metadata/command
payload stays 12 MiB; body/center payload stays 5.5 MiB. Body command/request/result
scratch and transfer capacities are now 12 MiB plus status (GPU 8 B, transfers 16 B).
These totals exclude all other geometry/solver/history buffers and driver overhead.

An active four-substep collider tick now exchanges 160 B in each direction (body
status increased from 4 to 8 B); no-shape joint ticks exchange 96 B. Integration-only
Step exchanges 8 B each way. Sleep adds dispatch uniforms but no additional fence.
A confirmed unchanged inactive world exchanges zero bytes and performs zero waits.

The graph currently rebuilds for every active substep, including quiet components
in a mixed active world. This is a measured performance boundary; a zero-work fully
inactive world is not evidence of fast partial-island scheduling or full GPU
advantage. Native allocator totals, other devices/platforms and window FPS remain
unverified.

## Verification

Run `ELECTRON2D_TEST_GPU_RESIDENT_SLEEP=1 dotnet tests/Electron2D.Tests/bin/Release/net10.0/linux-x64/Electron2D.Tests.dll`
after a Release source-native build. The full GPU runner includes this suite.

Checks cover initial/manual/automatic sleep, zero delta and delay, separate linear
and angular thresholds, CanSleep, command ordering, finite-input rollback,
257-body reversed contact chains, 129-body joint chains, support removal, generation
reuse, independent islands on one static floor, sensor exclusion, moving surfaces,
changed gravity/settings/resources, actual impact momentum and a stretched spring.
Wake impact and spring velocity allow 0.001 scene unit/s; force retention allows
0.00001. Identity, sleeping flags and zero-motion assertions are exact. Quiet
interval tests run well beyond 0.05 s so float accumulation does not select the
boundary frame. No cross-backend bitwise solver equality is required.

The 4,096/65,536-circle connected populations use 128 warmup calls/128 sampled batches of 256 inactive calls at 120 Hz,
four substeps/16 iterations. After device-confirmed sleep, tests require zero
transfers/waits and zero managed owner-thread allocation, then explicitly read every
body. Sixteen repeated component wake/sleep cycles are warmed before an additional
sixteen allocation-checked cycles. The existing active contact and joint benchmarks
explicitly disable automatic sleep to retain their all-awake workload.

The first test rejected a setup bug: setting thresholds before creating bodies left
a global wake pending and overrode their initial sleep. Settings changes now request
wake only when the world already contains bodies. Explicit sleep suppresses its own pending wake while retaining old-neighbour
invalidation for a preceding teleport or attachment edit. An initial GLSL build used the reserved word
`active` for a local; it was renamed before executable tests.

## Measurements and final checks

Linux/Vulkan, RTX 3090 Ti, .NET 10.0.1, 2026-10-08. Final logs:
`/tmp/electron2d-resident-sleep-final-gpu.log` (full GPU suite including renderer
lifetimes) and `/tmp/electron2d-resident-sleep-populations.log` (both sleep populations).

| Workload | p50 | p95 | p99 | Mean GPU wait | Managed B/tick |
| --- | ---: | ---: | ---: | ---: | ---: |
| 4,096 awake circles | 3.8434 ms | 5.1266 ms | 5.8371 ms | 2.7584 ms | 0 |
| 65,536 awake circles | 19.1524 ms | 25.8251 ms | 29.7712 ms | 18.7757 ms | 0 |
| 4,096 awake world pins | 1.6374 ms | 2.2142 ms | 3.0738 ms | 0.9200 ms | 0 |

Circle runs retain gravity 980, 120 Hz, four substeps, sixteen iterations and
384 warmup/256 samples. Pin runs use 128 warmup/128 samples. Circle uniform traffic
is 15,400 / 16,176 B/tick; pins use 12,288 B/tick. Final containment and decreasing
mechanical energy pass; maximum Y 126.0463 / 510.2770 stays below floor tops 127 / 511.
Maximum pin anchor error is 0.00858 scene unit. These are still private partial-world
pipelines without CCD, public state/event publication or rendering. No same-revision
full CPU comparison, 60 FPS claim or full GPU-backend acceptance follows.

After all 4,096 / 65,536 bodies sleep, 128 batches of 256 calls have p50
0.000018 / 0.000019 ms per inactive call and zero transfers/waits. This measures only
the version-checked idle guard, not simulation throughput. Every body's sleeping
state and zero velocity are read and verified outside the timing window. Warm
repeated wake/sleep cycles also allocate zero owner-thread managed bytes.

Integration-only control remains separate: 65,536 bodies, p50 0.0547 ms, 8 B each
way and 32 B uniforms. Active mass-edit + step + explicit-read mean is 0.1542 ms,
160 B upload / 64 B readback and zero managed bytes. Desktop clocks/load were not
pinned; especially the higher-percentile active timings vary between runs.

The shader delivery check verifies 25 generated embedded resources. Generated
SPIR-V SHA-256 (ignored build output):

- Sleep: `d27d758e02e55d10bbc13a4d20b7fb822bccbfacb3a343f6d5d806785b92d30b`.
- Bodies: `34b06f10a63877d4597c04fc4b3f87d75dc42650db499ee049873e59925aa06d`.

An intermediate full runner stopped on its obsolete 12-byte broad-phase traffic
assertion after the body status grew to 8 bytes. The assertions now require the
actual 16-byte integration+broad and 24-byte integration+broad+narrow transfers;
the following full run passes without dropping either status or activity data.
