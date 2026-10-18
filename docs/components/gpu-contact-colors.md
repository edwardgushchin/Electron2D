# Resident contact scheduling and dense-world verification

Last updated: 2026-10-18

## Validated device color cache, 2026-10-18

Contact schedules now retain a device-only key table. Each record contains the
shape-pair identity/generations, complete contact features and assigned color;
lookup follows current contact keys rather than previous append indices. The
current constraints and dynamic endpoints still validate every proposed color.
Missing keys, invalid colors or endpoint conflicts reset the whole
candidate and run the complete bounded proposal algorithm. All current rows,
configured solver sweeps and the full high-degree Jacobi response remain.

The cache is derived scheduling data and contains no impulses, poses or observer
history. A local GPU checkpoint retains its records, table and logical lookup size
to preserve the bounded continuation of iterative solving; portable wire payloads
do not contain it. Restoring or editing a world still validates current keys and
endpoints before reuse. A single-row graph keeps the shorter cold path. A prior single-color result
does not seed the cache because it already completes in one productive round.
A reused subset may have empty color ranges; ContactColorCount is the exclusive
upper color index, and only populated ranges dispatch physical constraints.

Device capacity is 48 bytes per rounded contact record plus four bytes per rounded
table slot; the table reserves at least twice the contact count, with the existing
64-slot minimum/power-of-two growth. Buffer replacement discards the derived cache.
The same tracked resource owner releases both buffers. ContactColorCacheStorageBytes
reports this logical capacity, excluding native/driver overhead. ContactColorReuseCount
and ContactColorProposalRoundCount are cumulative diagnostic counts, not simulation
or checkpoint state. PhysicsMassPerformance reports them as ColorReuses,
EncodedColorProposalRounds and ColorCacheStorageBytes over the full session,
including warmup; ordinary transfer/timing/allocation fields retain their measured
interval scope.

Lookup and validation use the existing schedule completion check. Saving the next
table joins the ordinary remaining solver commands; there is no separate cache
submission/readback or CPU contact mirror. A miss after a previously reused graph
can extend the ordinary four-round adaptive batch and add a completion check. The
full workload counters include those waits and commands. Even a successful reuse
still records the bounded proposal commands, whose device bodies exit immediately;
ContactColorRounds is zero when no productive new proposal round was needed.

A RED four-row control rebuilt two colors with two productive rounds. The keyed
path reuses it with zero productive rounds, completely rebuilds a changed four-row
shared endpoint graph, and reuses a three-row subset by key. The unchanged
single-row control retains 1/1 encoded rounds and 2848/1872 uniform bytes. The
four-row warmed bracket measures strict 0 owner/all-thread managed bytes. A separate final fixture check also confirms zero tracked device/transfer bytes
and equal allocation/release counts after explicit store retirement.

An initial cache keyed only by append indices passed the small controls but reused
no schedule in the full 4096-body scene. It is rejected: median full step was
68.1707 ms versus 67.2999 ms in the preceding frozen baseline. The keyed candidate
instead records 1838 reuses over 480 four-substep ticks, 917504 bytes of cache
capacity and a 56.6798-ms median full step, while retaining the original geometry,
energy, population and strict zero managed-byte assertions. These are software
Vulkan development observations; the intervening independent input-only commit
and exact frozen sources are recorded separately. The 65536-body keyed candidate records 1224 reuses, 14680064 bytes of cache
capacity, a 648.6508-ms median and strict 0/0 managed bytes; maximum penetration
remains 0.300035 within the unchanged 0.601 scene-unit bound. CPU medians in that
same frozen candidate payload are 12.2080/262.7678 ms for 4096/65536 bodies, so
software GPU remains slower. Final regression and target-hardware/window/FPS gates
remain separate. Evidence:
bin/physics-matched-mass/2026-10-18/, including baseline-validation.json,
candidate-index-rejection.json and the source/PDB/payload manifests.

### Full backend software workload

The final four workload profiles use one frozen Release payload on 9a6c5f22 plus
the keyed-color implementation and checkpoint-cache retention fix. Both backends
retain the same deterministic lattice, mass-one radius-four circles, three walls,
gravity 980, damping .05, friction .3, bounce zero, 1/60-second ticks, four
substeps and sixteen sweeps. All bodies remain awake; each profile runs 240
warmup and 240 measured ticks. CPU uses the existing up-to-four-worker policy;
private lavapipe uses four LLVM workers. Tiered compilation is disabled.
Measurements are sequential and headless, excluding setup, final invariant
inspection, serialization and disposal.

| Backend / bodies | Mean | P50 | P95 | P99 | Max |
| --- | ---: | ---: | ---: | ---: | ---: |
| CPU / 4096 | 13.3385 | 12.8809 | 15.9504 | 18.0387 | 20.5316 |
| GPU / 4096 | 58.8325 | 57.8289 | 63.9172 | 66.4795 | 84.8088 |
| CPU / 65536 | 263.7213 | 262.5240 | 273.6732 | 289.5032 | 300.1793 |
| GPU / 65536 | 793.5437 | 753.9595 | 912.6568 | 1607.6988 | 2826.3070 |

Times are milliseconds. Every profile retains all requested active bodies, finite
enclosed state, energy below 102% of initial energy, maximum penetration below
twice the .3 allowed penetration plus .001, and strict 0 owner/all-thread managed
bytes over measured intervals. Candidate-pair counts are different backend work,
not a common count of touching contacts. The complete timing/wait distributions
and eight phase means remain in the raw reports.

| Backend / bodies | Mean fence wait, ms | Upload B/tick | Readback B/tick | Uniform B/tick | Submissions/tick |
| --- | ---: | ---: | ---: | ---: | ---: |
| CPU / 4096 | 0.0000 | 0.00 | 0.00 | 0.00 | 0.00 |
| GPU / 4096 | 56.9531 | 232.00 | 752.00 | 57320.00 | 17.00 |
| CPU / 65536 | 0.0000 | 0.00 | 0.00 | 0.00 | 0.00 |
| GPU / 65536 | 761.8551 | 232.00 | 783.80 | 64165.60 | 17.22 |

Software GPU remains slower than CPU. The final 65536-body median is also about
3% above the preceding 732.0492-ms baseline, despite fewer productive coloring
rounds; the repeated software observations do not establish a stable whole-step
gain. Before/after observations at the preceding
baseline, rejected append-index experiment and pre-retention candidate have
separate source/payload records; they do not establish a controlled target-device
speedup. Final workload validation: verified-mass-validation.json, SHA-256
dd4607a9af6919ddd927a7c6944516009cff39c970117b83489e8ea215085ed2.
The four runs share the source/PDB/payload set in source-proof-replay.json and
payloads-replay.json. The pre-retention candidate has its own keyed2-validation.json;
the original baseline has baseline-validation.json. Complete solver/network
acceptance and real Wayland window/FPS/target-GPU results retain separate gates.

### Final solver regression

The checkpoint-retaining sources complete the resident-solver profile in
874.273728 seconds. It executes the original 4096/65536-body loaded pits,
all three storage layouts, oblique friction, rotating stacks, directed rays,
signed materials, capped coupled joints and complete 64/257-support overflow.
The current four-row cache/rebuild/subset and single-row/growth controls run in
that same profile. This functional run overlaps other builds/checks and retains
its measured zero owner-thread managed-byte gate; its timings are not a matched
performance comparison. Evidence: verified-resident-solver/check.json and
runner.log under bin/physics-matched-mass/2026-10-18/.
All thirteen related CPU/shared-GPU, CCD, joint, field, report, portable/local/device
checkpoint, sleep, transient/dense contact and one-way profiles also pass on the
same frozen root payload. The fourteen-profile validation is
verified-root-validation.json, SHA-256
26c14211c185fe31cf190469da5f2543ea9e13b36bd20dfa60077688ee9e9f16.
This is a focused/related regression, not the complete repository suite.

All sixteen fresh separate-process network profiles on the same 9a6c5f22-based
frozen sources pass: instrumented/public managed/strict NativeAOT CPU/GPU clients,
owner/separate lanes and four phase-paced checks. The original 900 authority ticks,
late join at 310, adverse delivery, correction/replay, lifecycle, joints, sleep,
confirmed events and prepared/applied handoff assertions remain. All 48 simulation
participants retain positive warm samples and strict zero owner/all-thread managed
bytes: 17612 warm steps and 57581 warm replay steps in total. The 24 cost reports
retain twenty phase regions, exact byte/submission partitions and bounded wall
samples. Validation: verified-network-validation.json, SHA-256
57e374651cc493d32c94b5487ea583d2ecc1270177194e5f670a509fe57dffea. Eight Release PDB/source audits cover the frozen 3049 authored
documents and 336 payload files; four manifests bind all 32 physics programs to
current source fingerprints and embedded runtime bytes.


### Combined main regression

While these frozen checks ran, the independent low-processor rendering change
78a91158 entered main. Its source and documentation are preserved. The combined
runtime, public example and strict NativeAOT example are rebuilt separately;
six exact PDB/source audits cover 3050 authored documents and 271 payload files.
Three new shader manifests bind the unchanged physics source fingerprints to
all 32 embedded programs per runtime. Current-main color-cache, device-checkpoint
and shared CPU/GPU public-contract profiles all pass. Validation:
merged-root-validation.json, SHA-256
c647d6769d37bd563473f08202488f50182ce0d4d4e6237cd617feaaea174c80.
The nine formatting/Release/API/wiki checks also pass on this combined version.
Eight fresh public managed/strict NativeAOT network profiles also pass on this
combined runtime: CPU/GPU clients in both owner/separate lanes, with all original
900-tick checks and strict zero warm owner/all-thread managed bytes. The 24
simulation processes contribute 9057 warm steps and 12857 warm replay steps.
Validation: merged-network-validation.json, SHA-256
b68b1847b807b62ad7f965531ce5dfb58fcc55d1141863d0fb767f50ed1ab3df.

The mass timings and complete fourteen/sixteen-profile cohorts above remain
observations of their explicitly frozen 9a6c5f22-based sources, not a rerun of all
those workloads on the later rendering change. Exact old source bytes displaced
by that independent merge are retained and checked against their original PDB hashes.

## Row-bounded proposal batches, 2026-10-18

The encoded proposal budget now follows the current contact-row count. A one-row
graph records one proposal round on both cold and repeated solves, rather than
four; six coupled rows extend their initial four-round batch only to six, and a
subsequent one-row graph cannot inherit a seven-round budget. Solver sweeps,
contact/joint features, the 32-color limit and full overflow response remain.
The internal cumulative ContactColorProposalRoundCount counts encoded complete
proposal rounds, including rounds whose device work would immediately exit after
completion. It observes command work without another GPU submission or readback.

A frozen RED isolated-contact fixture recorded 4/4 rounds and 3136/2160 uniform
bytes for its first/repeated velocity solve. The row-bounded fixture records 1/1
rounds and 2848/1872 bytes. Two 1-kg bodies retain 6 scene-unit/s inelastic
velocities, total momentum of 12 kg·scene-unit/s and unchanged poses; 128 warmed
solves measure strict 0 owner/all-thread managed bytes. The six-row growth/shrink fixture also retains
physical static support. This is an encoded-work reduction in a 1/60-second,
single-iteration isolated velocity-solve control. Velocity and momentum tolerances
are 0.002 scene-unit/s and 0.002 kg·scene-unit/s, reusing the existing resident
coupled-chain bounds for FP32 warm/gather accumulation. Poses are exact because
the operation does not advance them. This establishes no whole-step throughput,
hardware speedup or window/FPS result. Evidence is retained under
bin/physics-network-stage-costs/2026-10-18/.

Initial separate-process profiling on the preceding 20cab511 runtime sources
completed 317/312 GPU-client frames in owner/separate modes. The combined resident
solver commands account for 73.632%/73.325% of accumulated store fence waits.
These counters include schedule construction; they are not an isolated coloring
kernel measurement. The original network inputs, positive warm prediction/replay
samples and strict 0/0 managed-byte gates remain. Evidence:
initial-stage-analysis.json and source-proof-initial.json in the same directory.
The waits motivate examining command work; they do not establish a hardware gain.

The complete resident-solver profile passes on the row-bounded sources in
692.254965 seconds, including dirty 16384-body scratch initialization, elastic and
speculative impacts, signed materials and stationary surfaces, directed rays,
64/257-support overflow, coupled capped joints, all three storage layouts, oblique
friction, the ten-second rotating stack and 4096/65536-body loaded pits. The pit
checks retain four substeps, sixteen sweeps and 384 warmup/256 measured intervals;
measured owner-thread managed bytes are zero. Their timing logs include concurrent
host compilation during part of this functional run and establish no matched
CPU/GPU throughput or target-hardware advantage. The simpler floor-one experiment
was replaced by the row-count bound and its interrupted run is excluded from final
acceptance. No physics declaration state is closed by this optimization.

Fourteen final root profiles now pass: the complete resident solver, shared
CPU/GPU bodies/shapes/worlds, common CCD/boundary/directed-ray and joint contracts,
fields, completed-frame reports, portable snapshots, public and private device
checkpoints, resident sleep, transient and dense contact histories, and one-way
response. The transient suite covers all 32 CPU/GPU, scene/server, CastRay/CastShape
and owner/separate combinations; dense histories retain 32-slot identity/cap/event
checks. Transient/dense history warm brackets remain at strict 0 owner/all-thread
managed bytes.
The corrected sleep fixture has its own fresh source/PDB proof; its runtime DLL
is byte-identical to the earlier final solver/runtime build. The original stale
fixture failures remain outside accepted results. Evidence: root-validation.json
and source-proof-fixed.json under the same artifact directory.

All sixteen fresh [network regression profiles](physics-network-example.md#row-bounded-contact-scheduling-regression-2026-10-18) also pass: twelve ordinary instrumented/public managed/strict NativeAOT profiles and four phase-paced profiles, with their original deadlines and physical/protocol assertions. The 17614 warm step and 56100 warm replay intervals remain at strict 0 owner/all-thread managed bytes. This does not replace the earlier failed timing observations or establish their cause. Functional payloads predate the independent controller-motion commit; integration build/API/wiki checks retain their own scope.

## Failure that motivated the change

The public PhysicsServer dense-circle workload exposed excessive compression in the
degree-damped Jacobi contact path. With 65,536 radius-four circles, four substeps,
16 iterations and sleeping disabled, a native run reached 6.03 scene units of
pair penetration after 480 ticks. The 8-unit particles could nearly pass through
each other. Finite state, nonescaping bodies and a good frame rate did not establish
acceptable contact solving. Those earlier throughput numbers are diagnostic data,
not acceptance of that simulation.

## Device schedule and response

[GPUPhysicsBodyStore.Colors](../../src/Servers/Physics/GPUPhysicsBodyStore.Colors.cs)
builds a conflict-free contact schedule from the current prepared constraints.
Each proposal round uses unique mixed integer priorities and per-body atomic minima
to select disjoint contacts. A selected row receives the lowest color unused by its
dynamic endpoints. Static endpoints do not serialize independent contacts. Every
row is retained. The device then validates that each color owns each dynamic body
at most once; a conflicting schedule fails the world before colored solving.

Device histogram/prefix/scatter passes group row indices by color. The host receives
only 32 color counts and status, and submits each populated range. It does not read
or color the contact graph. Rows of one color solve concurrently; later colors see
the earlier velocity and positional corrections. Each configured iteration visits
every color. Normal/friction limits, split position correction, surface motion,
warm history, completed-frame impulses and the four simulation substeps remain.

Joint rows follow each contact sweep using their existing coupled impulse caps and
gather. Contact deltas already applied by the colored kernel are cleared so a joint
gather cannot apply them twice. Joint presence does not disable colored contact
response. If coloring cannot complete within 32 colors and 64 proposal rounds, the
whole constraint batch uses the complete GPU Jacobi update/gather path; no contacts
are truncated and no CPU solve is substituted. The 64/257-support checks exercise
that high-degree path. Improving convergence for overflowing graphs remains open.

Proposal batches start with the previous completed round count plus one. For N
contact rows, the initial budget is clamped between min(4, N) and min(64, N).
A device-confirmed unfinished batch extends by sixteen rounds up to min(64, N).
An unfinished proposal round assigns at least the globally lowest-priority row,
or selects the complete fallback when its endpoint colors are exhausted; N rows
therefore need no padding beyond N rounds. The device still validates completion
and conflicting ownership before any colored response. A graph that remains
unfinished at its bound uses the complete GPU Jacobi path. This reduces commands
for rounds that would do no work after completion; it does not accept an incomplete graph,
change configured solver sweeps or discard contact rows. Graph growth may add a
status fence, measured by the complete scene benchmark. Its JSON records final
color/round counts and cumulative fallbacks separately from frame timing.

An unfinished graph continues without resetting or physically replaying it.
Derived colors are validated or rebuilt from current constraints; owners, buckets
and order are rebuilt. Local checkpoints retain the keyed scheduling cache for
bounded replay; portable snapshots keep it implementation-local. Generation, geometry and contact-history guards
remain in preparation. Owner-thread and failed-world lifetimes are unchanged.

## Storage and synchronization

Retained device scratch contains 4 bytes of color and 4 bytes of ordered index per
constraint capacity, 8 bytes per body capacity, 384 bytes of counts/offsets/cursors,
and 16 bytes of status. Host dispatch metadata is two 32-element integer arrays;
the retained download buffer is 144 bytes. There is no new host body/contact mirror.
Each build uploads a 16-byte status reset and reads 144 bytes per completion check.
The fence makes color counts available for bounded dispatch and validates conflicts.
Ordinary completed coloring adds one submission per substep; growing round budgets
can require another completion check. All traffic and waits enter existing store
counters. Native driver allocations remain outside managed-allocation measurements.

## Packed iterative state

After warm starting and coloring, the device now gathers contact coefficients and
impulses into contiguous color ranges. Iterative reads no longer follow a contact
index for each coefficient and impulse access. A separate 48-byte body record keeps
physical velocity/inverse mass, correction velocity/inverse inertia and surface
velocity together, instead of revisiting the 96-byte owning body and separate
16-byte correction records. The equations, configured iterations and color schedule
are unchanged. This is derived device scratch, not a new host mirror or wire format.

Joint updates and gathers consume the same compact current velocities while colored
contacts execute. Applied contact deltas stay zero in the original impulse storage,
so joint gathering cannot double-apply the warm start or contact sweep. Before
history, joint-state save, reporting, integration or publication, the device writes
the final contact impulses and body velocities/corrections back to their owning
records. Sleep clocks, poses, identity and authored properties remain in those
records. Checkpoints need no new payload; the compact state rebuilds on the next solve.

Packing/unpacking adds four compute dispatches per colored substep, without another
host fence or readback. Retained logical buffer capacity is 96 bytes per rounded
contact slot plus 48 bytes per rounded body slot; the mass report includes this as
PackedSolverStorageBytes. It is a memory-for-bandwidth tradeoff, not a claim about
driver heap overhead. The internal diagnostic controls PackColoredContacts and
PackSolverBodyState retain comparisons against indirect rows and full body records;
they change no public backend selection or physical features. Jacobi overflow still
uses the original complete path.

## Public workload and native window

[PhysicsMassPerformance](../../tests/Electron2D.Tests/PhysicsMassPerformance.cs)
constructs identical CPU/GPU worlds through public PhysicsServer APIs, not the
internal body store. A deterministic staggered lattice falls between three static
walls. It uses mass one, radius four, friction .3, bounce zero, gravity 980, damping
.05, four substeps and 16 iterations. Every particle has sleeping disabled. Default
measurement is 240 warmup plus 240 measured 1/60 s ticks. The exact requested backend
and active population, world tick, every body's finite state, enclosure, energy
and neighboring-particle penetration are checked.

Energy may not exceed initial mechanical energy by 2%, allowing numerical contact
correction without accepting sustained energy growth. Maximum circle penetration
must not exceed twice the configured allowed penetration plus .001 scene units.
This is a scene-specific finite-iteration pressure tolerance, not one global epsilon
or a claim of identical CPU/GPU trajectories. The .001 allowance is for float
coordinates; the default bound is .601 versus an 8-unit diameter. Geometry and energy
inspection run outside timing/allocation brackets.

Headless mode measures the complete public space step and its eight internal phases.
Native mode calls the same step from an ordinary fixed callback, uses public poses
to update one MultiMesh, and captures the actual GPU-rendered window. No physical
body is removed or replaced by a visual particle. It requests VSync disabled and
records the actual mode; the tested compositor returned Enabled. MaxFPS is zero
and catch-up is capped at one physics tick per rendered frame. Under overload the
simulation slows down, so rendered FPS and actual physics ticks/s are both reported.
FramePostDraw measures submitted native frames, not compositor scanout timing.

Full body state is an explicit public getter/render consumer here. Transfer totals
are reported for physical steps and the full window interval; MultiMesh buffer
payload is separate. The JSON schema labels latency arrays as mean/p50/p95/p99/max,
and records managed allocations, active bodies, candidate pairs, color usage,
geometry/energy checks and capture paths. Candidate pairs are backend broad-phase
work, not a common count of touching manifold points.

PhaseMeanMS uses the existing backend phase order: preparation, fields/activity,
body policies, simulation, publication, completion/contact collection, monitoring
and callbacks. CPU and GPU divide publication/completion differently; compare the
full StepMS for backend speed. Diagnostic per-pass fences report contact coloring
separately and include colored sweeps in update time; they disturb ordinary batching.

```sh
ELECTRON2D_TEST_PHYSICS_MASS=headless ELECTRON2D_MASS_COUNT=16384 dotnet run --project tests/Electron2D.Tests -c Release
ELECTRON2D_TEST_PHYSICS_MASS=window ELECTRON2D_MASS_COUNT=65536 ELECTRON2D_MASS_BACKEND=gpu dotnet run --project tests/Electron2D.Tests -c Release
```

ELECTRON2D_MASS_BACKEND accepts cpu, gpu or both (default). Count accepts 64–65,536;
WARMUP and SAMPLES use the same ELECTRON2D_MASS_ prefix. OUTPUT selects the report
directory (default bin/physics-mass). Reports and captures are generated artifacts,
not source files. An unsupported GPU renderer/backend fails explicitly.

## Packed-layout measurements

The 2026-10-10 layout comparison used the same Release binary, physical workload and
host described below. Headless development repetitions measured full-step p50 of
53.02 ms for indirect rows, 51.39–52.37 ms for packed contacts alone and
44.85–44.97 ms with compact body state too. All retained 65,536 awake bodies, four
substeps, 16 iterations, penetration about .30 and zero warmed managed allocation.

The final native comparison ran each process to a checked zero exit, including
validation, actual window capture and disposal. It uses 240 warmup and 240 measured
ticks, the same final build and a public pose/MultiMesh render consumer:

| Native-window measurement | CPU / Box2D.NET | GPU / indirect layout | GPU / packed layout |
| --- | ---: | ---: | ---: |
| Full step p50 / p95 / p99, ms | 237.42 / 256.92 / 291.58 | 64.22 / 71.10 / 73.33 | 54.11 / 56.07 / 56.93 |
| FPS and actual physics ticks/s | 3.89 | 12.40 | 13.91 |
| Full frame p50 / p95 / p99, ms | 254.43 / 277.25 / 314.38 | 79.70 / 87.90 / 90.33 | 71.81 / 74.24 / 76.17 |
| GPU physics wait p50, ms | 0 | 40.16 | 30.66 |
| Maximum penetration, scene units | .2694 | .2998 | .2996 |
| Warmed physics and frame bytes, owner/all threads | 0 | 0 | 0 |
| Additional iterative buffer capacity | — | 3 KiB binding buffer | 30 MiB |

Packed iteration reduced median full-step latency by 15.7% and increased window
FPS by 12.2% against the indirect control in this run. GPU median full-step latency
was 4.39 times lower than CPU, with 3.57 times the window FPS. The actual compositor
VSync mode remained Enabled. Background host work was not isolated; the repeats
establish the direction and scale on this host rather than a universal speedup.

Both GPU layouts submitted 4,560 batches over the 240 measured ticks, uploaded
57,600 buffer bytes and read back 1,258,473,600 bytes. The separate UniformBytes
counter recorded 16,420,288 bytes indirect and 16,498,208 bytes packed; small color
schedule differences also affect that total. Both retained ten colors at the final
tick and no fallback. Packing adds no host state mirror or extra readback/fence.
Renderer-native transfers remain outside these physical-store counters; the
logical MultiMesh payload is reported separately. The 30 MiB capacity is derived
GPU scratch, not a measurement of driver heap overhead or native allocation rate.

For a same-build diagnostic comparison, set ELECTRON2D_MASS_UNPACKED=1 to use the
indirect layout, or ELECTRON2D_MASS_UNPACKED_BODIES=1 to pack only contact rows.
Both are test-host controls, not public physics configuration. Normal GPU worlds
use the packed layout. Generated final reports, PNGs and check logs are retained
locally in bin/physics-layout-validation/2026-10-10; development layout trials are
saved alongside them. All are ignored build artifacts.

## Measurement before packed iterative state

The final sequential native runs on 2026-10-10 used the same Release build on
Linux 7.2.9 CachyOS, .NET 10.0.1, Ryzen 7 5700X and NVIDIA RTX 3090 Ti/Vulkan.
Both used 65,536 active circles, 240 warmup and 240 measured ticks, four substeps
and 16 iterations. Both processes exited zero after validation and resource cleanup.

| Native-window measurement | CPU / Box2D.NET | GPU / resident colors |
| --- | ---: | ---: |
| Complete physics step p50 / p95 / p99, ms | 242.80 / 282.22 / 305.31 | 69.56 / 73.40 / 76.81 |
| Rendered FPS and actual physics ticks/s | 3.79 | 11.86 |
| Whole frame p50 / p95 / p99, ms | 259.01 / 298.23 / 323.12 | 83.92 / 88.36 / 93.50 |
| GPU physics fence wait p50, ms | 0 | 44.34 |
| Maximum penetration, scene units | .2694 | .3003 |
| Warmed managed bytes: physics / whole frames, owner and all threads | 0 / 0 | 0 / 0 |

This run gives about 3.49 times lower median full-step latency and 3.13 times the
rendered frame rate on GPU, while both remain below 60 Hz. The GPU store recorded
57,600 uploaded buffer bytes, 1,258,473,600 readback bytes and 4,560 submissions over
240 ticks: approximately 5 MiB of readback and 19 submissions per tick. These store
buffer counters exclude shader-uniform traffic and renderer-native transfers.
Both render consumers published a logical 754,974,720-byte MultiMesh payload in the
same interval. Full state publication serves the public pose getters used by this
renderer; retained GPU physics state is not replaced by those getter snapshots.
The final GPU graph used ten colors, sixteen proposal rounds and no fallback.

An earlier corrected headless run measured GPU full-step p50/p95/p99 of
53.64/55.93/56.83 ms with penetration .3004 and zero managed bytes. It preceded the
mixed-joint regression extension and is a development measurement, not the final
same-build native comparison above. Earlier corrected window repetitions measured
12.42 GPU FPS and 3.88 CPU FPS. Background CPU research in a separate worktree and
the desktop compositor were not isolated; these are host observations rather than
a universal speedup promise. The old collapsing GPU path's 17.6 FPS is not a valid
quality/performance baseline. One earlier combined matrix ended with SIGTERM after
writing reports; only the separately completed final pair establishes normal exits.

Generated evidence is retained locally under bin/physics-mass-validation/2026-10-10:
CPU-65536-window.json, GPU-65536-window.json, both captured PNGs and the precommit
manifest/logs. Reports include complete latency arrays and energy diagnostics;
they remain ignored build artifacts rather than committed generated output.

## Verification boundary

Colored chain checks verify momentum, nonincreasing inelastic kinetic energy,
per-body reported impulses and fixed poses for standalone velocity solving, also
with an unrelated capped pin in the world. Device schedule validation checks actual
conflicts. A separate analytic collision/pin chain checks all three storage layouts:
the joint must consume the velocity produced by the preceding collision, preserve
linear momentum and leave the collision-only reported impulse unchanged. Existing
contact, surface, material, CCD, sleep, one-way, joint, report,
checkpoint and portable-snapshot tests remain the behavioral gates. The separate
network example additionally checks authoritative correction under impaired delivery.
Its scripted drift is now injected at a validated correction boundary, so physical
contacts between injection and packet arrival cannot erase the intended test offset.

The dense-pile fix establishes substantially better contact convergence, with no
change to body count or configured iterations. It costs additional device work;
the 65,536-body 60 Hz/window performance target remains open. CPU and GPU selection
remain independent of rendering. Foreign-platform, native-allocation and complete
physics-contract acceptance are not inferred from these Linux checks.


## Scratch initialization and preparation order

The contact schedule, packing pipelines and their capacity buffers are prepared
before acquiring the solver command buffer. Recording the interval performs no
lazy pipeline creation or scratch growth in these paths. This keeps resource
initialization outside already-recorded constraint work and leaves the solve,
packing, color counts, iterations and fallback equations unchanged.

`PhysicsMassPerformance.VerifyDirtyBufferInitialization` fills every newly
created resident storage buffer with NaN words and, in a separate world, finite
nonzero words before first use, then advances
16,384 overlapping circles through the packed contact/body path. It checks the
complete population, finite motion, bounded displacement and a real conflict-free
schedule. The displacement gate is one particle radius (4 scene units), above
the configured 200-unit/second correction integrated for 1/60 second plus gravity
displacement; it detects corrupted first-step state without requiring a solver
bit pattern. The internal allocation callback is restored after the check; ordinary
runs do not initialize scratch through this callback. The mass fixture exposes
`ELECTRON2D_MASS_POISON_BUFFERS=1` (NaN) or `finite` (float-one words) for the
same diagnostic in the full public/window path. Unknown values reject before
world creation. Reports identify the initialization pattern and requested versus
actual presentation policy. Its additional setup transfers/fences are diagnostic overhead and cannot be
attributed to ordinary physics throughput.

The diagnostic failed on the previous lazy preparation path at its first dense
interval; a 64-body control and the unpacked 16,384-body control passed. Temporary
stage masks and raw buffer dumps localized untouched NaN constraint coefficients
and were removed. The ordering change resolves that injected-initialization case;
the earlier sporadic uninstrumented CPU-then-GPU window failure still requires
independent causal evidence. Passing repetitions alone do not close that gate.


Verification on 2026-10-11 used Linux/.NET 10 Release and Vulkan/RTX 3090 Ti.
`ELECTRON2D_TEST_GPU_RESIDENT_SOLVER=1` fails at tick zero with the old resource
preparation order and the new public-pile regression; it passes after the change,
including the existing 65,536-body solver checks. Final raw-pile runs retain four
substeps, 16 iterations, no sleep, 240 warmup and 240 measured ticks at 60 Hz.
Both backends use the same final runtime/test assemblies; hashes, run exits,
latency distributions, traffic and waits are in
`bin/physics-window-stability/2026-10-11/final-commands.json` and
`final-summary.json` in that directory.

| Final window path | Mean full step ms | Mean frame ms | Actual FPS | Max penetration |
| --- | ---: | ---: | ---: | ---: |
| Ordinary CPU | 56.0812 | 61.4893 | 16.2630 | .227389 |
| Ordinary GPU | 14.8915 | 19.7636 | 50.5978 | .241057 |
| NaN-buffer CPU | 55.9632 | 61.4376 | 16.2767 | .227389 |
| NaN-buffer GPU | 14.7892 | 19.5128 | 51.2483 | .242476 |

The ordinary and injected window processes each run CPU then GPU with actual
240-frame captures. Zero escape and all physical gates pass, with zero owner and
all-thread managed bytes in both physics and frame samples. GPU frame demand
uploads 62,972,160 bytes and reads 63,096,960 bytes across 240 samples, including
explicit transform demand; it waits 2,018.79 ms in total in the ordinary run.
The separate NaN-buffer GPU headless run also passes (15.2150 ms mean full step,
.245142 max penetration). Inspected captures show the complete contained pile.
These results establish this workload's measured advantage on this device, not
sustained 60 Hz, a general scene-pipeline gain or the cause of the earlier sporadic
uninstrumented failure. Native allocation and other platforms remain unverified.
