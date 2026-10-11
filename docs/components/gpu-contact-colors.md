# Resident contact scheduling and dense-world verification

Last updated: 2026-10-11

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

Proposal batches start with the previous completed round count plus one, bounded
to 4 through 64 rounds. A device-confirmed unfinished batch extends by sixteen
rounds until completion or the existing limit. This reduces commands for rounds
that would do no work after completion; it does not accept an incomplete graph,
change configured solver sweeps or discard contact rows. Graph growth may add a
status fence, measured by the complete scene benchmark. Its JSON records final
color/round counts and cumulative fallbacks separately from frame timing.

An unfinished graph continues without resetting or physically replaying it.
Derived colors, owners, buckets and order are rebuilt from current constraints and
are not physical checkpoint state. Generation, geometry and contact-history guards
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
