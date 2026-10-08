# Resident GPU continuous collision

Last updated: 2026-10-09

## Executing boundary

[GPUPhysicsBodyStore](../classes/GPUPhysicsBodyStore.md) now stores per-body Disabled,
CastRay and CastShape modes, exposes internal GetCCDMode/SetCCDMode and performs
continuous work from Simulate. The default is Disabled. Configuration is retained
for all body roles; dynamic roles enable sweeping. Kinematic geometry participates
as a moving obstacle. Static virtual surface velocity affects impact response but
never translates its geometry. A changed mode wakes the affected body and coalesces
with other authored commands, including CanSleep. Reused slots start with their new
definition. Step and SolveConstraints remain their existing partial-stage controls.

This is an internal independent GPU implementation under
[ADR 0054](../decisions/physics.md#adr-0054). The public CPU CCD modes, scene/server
adapters, selectable GPU world, complete contact/event publication and networking
remain open. The new internal enum does not close public declaration coverage.
Existing public CPU behavior is unchanged.

The pinned [reference API](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RigidBody2D.xml)
defines disabled, ray and shape prediction. Its
[body-pair implementation](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/modules/godot_physics_2d/godot_body_pair_2d.cpp)
uses a leading support ray for the approximate mode and acknowledges loss of bounce
momentum from shortening velocity. The resident implementation uses time intervals
and contact impulses, preserving the checked incident restitution speed instead.
It does not reproduce private reference traversal or velocity-clamping order.

## Device trajectory and candidate search

Swept broad-phase bounds reuse the existing resident geometry, tree, layer/mask
filter and joint collision veto. Each moving shape includes translation and an
angular envelope about its resolved mass center. The angular expansion is bounded
by the maximum vertex radius and the sine of half the clamped rotation angle.
All moving counterparts contribute their trajectory, including peers with CCD
Disabled. Sleeping/static poses remain fixed. Sensors do not cause physical stops.
FindPairs caches both the horizon and positional-correction choice; ordinary calls
still request current bounds. ReadShapeBounds optionally exposes swept bounds for
explicit diagnostics. No body poses or bounds are uploaded from a CPU mirror.

The new CCD kernel reuses the contact kernel's complete convex/segment support,
projection, clipping and ray helpers in PhysicsResidentCollision.inc.glsl. Rounded
primitive tolerance and hollow segment decomposition remain common. A conservative
advance uses a separating-axis gap and an upper bound on its closing speed,
including both bodies' angular motion. Sampling and integration use the same
center-relative motion, including positional correction. Calculations stay near
the first body's initial origin to limit world-coordinate cancellation.

CastShape sweeps the whole supported geometry, including rotational arcs whose
endpoint poses are both clear. CastRay translates a leading support point per convex
piece, ignoring the source rotation while testing the counterpart trajectory, and
may miss off-ray corners. A ray-only body with no relative translation
does not detect its own pure rotation; full rotational protection requires
CastShape. When either endpoint uses CastShape, its full-shape pair is checked.
When both use CastRay, both directed leading-point casts are checked. Directed
separation-ray geometry retains its forward-intersection acceptance at a hit.
The existing hollow/hollow and separation-ray/separation-ray exclusions remain.

The nominal sweep skin is 0.001 scene unit. The search and impact solver share a
contact/restitution acceptance band of four times that skin, avoiding a dead band
during nearly tangent advancement. Ordinary non-CCD contacts keep
their existing restitution boundary. The authored contact margin is preserved for
ordinary stages. This bounded skin is numerical tolerance, not a claim of exact
arithmetic or arbitrary-scale precision.

A rotating body already near contact cannot skip the remaining arc. Such a pair
requests a smaller interval based on residual closing speed at the clipped
support pair and its angular-curvature bound, then reevaluates actual geometry/
constraints. This also prevents residual solver velocity from crossing a
zero-thickness wall over later frames; unrelated corners outside the actual
contact region must not constrain that interval. Rotation of a centered circle contributes zero
geometric radius to that bound; sub-slop capsule/segment midpoint adaptations use
the same effective geometry. Empty constraint lists skip iterative impulse/gather
dispatches. UniformBytes
counts actual dispatched solver passes, including the zero-contact case.
The per-pair search is bounded at 256 advances and
one scheduled substep at 128 continuous intervals. Unrepresentable or unconverged
work fails the store explicitly; remaining motion is never silently discarded or
replayed on CPU.

## Interval integration and force cadence

Each scheduled substep applies its normal force, damping, spring and bounded motor
contribution once, then solves its initial contacts/joints. GPU sweep threads reduce
the earliest fraction. Bodies advance to it, the ordinary geometry/manifold and
impulse solvers resolve the impact, and the remaining duration is swept again.
Thus restitution, friction, angular response, static surface motion, joint
constraints and waking use existing shared device operations.

Intermediate impact solves skip a second spring/motor contribution. A sleeper
newly activated by an impact receives the remaining interval's force integration
once. All poses consume the same elapsed interval; CCD-disabled peers retain their
mode, though global impact boundaries can add ordinary constraint evaluations.
Automatic sleep ages once for the full scheduled substep, not once per CCD query.
Zero delta does not consume motion. With no enabled dynamic bodies, no continuous
pipeline is created or dispatched.

Internal CCDQueryCount counts actual TOI dispatches, CCDIntervalCount counts split
intervals (including near-contact rotational refinement), and CCDWaitMS counts
those TOI summary waits. These are not public impact events or contact counts.
Transient contact publication across those intervals must be connected in the
future public-world adapter; a last internal manifold is not a complete frame
contact/event snapshot.

## Storage, transfer and native limits

The hot device body remains 80 B; CCD mode fits existing flag bits. Commands remain
128 B and selected snapshots 48 B. CPU slots are now 68 B, retaining one authored
enum in addition
to their prior configuration; there is no evolving body/trajectory mirror. At
65,536 slots the CPU slot/command payload is 12.25 MiB, measured by
AuthoredBodyCapacityBytes.
Swept bounds and candidate pairs reuse existing retained device buffers. Each
nonempty TOI query adds an 8-B status/fraction reset and 8-B result readback at one
fence. Additional impact boundaries incur the documented body/spatial/contact/
solver publication costs. The CPU reads only the scalar fraction to schedule the
next interval, never all poses to run a fallback sweep. This synchronous global
interval scheduler is a performance boundary, not evidence of ideal parallel CCD.

SDL compute stages permit eight readonly and eight readwrite buffer bindings.
The first candidate exceeded the readonly limit and produced inactive bounds in
this non-debug native path. The corrected spatial pipeline uses eight readonly
and seven readwrite bindings, including the read-only use of the existing center
buffer through a readwrite slot. ShaderCompiler now rejects excessive reflected
buffer/texture/sampler/uniform counts before native pipeline creation. An archived
nine-read-buffer shader was rejected before device creation in a standalone probe;
`/tmp/electron2d-ccd-binding-limit-result.log` records the result. The probe uses the
normal `runtimes/linux-x64/native` directory.

## Verification scope

Run `ELECTRON2D_TEST_GPU_RESIDENT_CCD=1 dotnet tests/Electron2D.Tests/bin/Release/net10.0/linux-x64/Electron2D.Tests.dll`
after a Release source-native build. The complete GPU runner includes this suite.

Checks cover all three modes and live disabling, combined sleep/CCD edits, stale
identities and rollback; a 3000-unit/s elastic impact on a 0.2-unit wall; thin segment
and hollow obstacles, circles, capsules, rectangles, segments, convex and hollow
sources; an offset computed center and directed separation ray; ray/full-shape
corner differences, two approaching bodies, fast kinematic translation and pure
rotational arcs of dynamic/kinematic geometry; masks, sensors and joint vetoes;
Coulomb rolling momentum, spinning-circle ray semantics, repeated-frame thin-wall
containment and unrepeated force/spring/motor budgets; failed-world
rejection after unrepresentable motion; repeated continuous impacts after warmup.

The elastic single-hit speed allows 0.01 scene unit/s and remaining position 0.02
scene unit. Thin-obstacle containment allows 0.01. The shifted circle's angular
error permits 0.02 rad/s: a two-micro-unit centroid conversion error multiplied by
its 3000-unit impulse and divided by inertia 0.5 is 0.012 rad/s. Ordinary force,
spring and motor cadence assertions allow 0.0001 in their scene/radian velocity
units. Friction allows 0.02 scene unit/s and 0.03 angular-momentum units. Identity,
policy and managed-allocation assertions are exact. These checks do not establish
foreign-device behavior, native allocation totals, window FPS or a complete
selectable/networked physics backend.

## Measured execution, 2026-10-09

The final full GPU runner passed on Vulkan / NVIDIA GeForce RTX 3090 Ti / .NET
10.0.1, with the CCD change applied over main `eb508e2841a33e4cd78951079b118203ef34c9d5`.
The device was shared with a game: an independent `nvidia-smi pmon -c 1 -s um`
sample, taken when no test process was running, reported ConanSandbox at 91% SM
utilization and 7,745 MB of device memory. This is evidence of external contention,
not a measurement of its exact share during each benchmark. No process was stopped
and no GPU configuration was changed. These timings are not a clean performance
baseline, a before/after comparison or proof of GPU advantage.

| Internal path | Load and sampling | p50 / p95 / p99, ms | Mean total fence wait, ms/tick | Managed allocation |
| --- | --- | --- | --- | --- |
| Continuous impacts | 256 elastic circles at 3000 units/s between two thin walls; 60 Hz, one substep, 32 iterations; 128 warmup + 128 samples | 5.4915 / 9.5524 / not recorded | 5.0586 | 0 B/tick |
| Contact response, CCD disabled | 4,096 circles, gravity 980; four substeps, 16 iterations; 384 warmup + 256 samples | 3.8576 / 4.6757 / 5.5458 | 2.6776 | 0 B/tick |
| Contact response, CCD disabled | 65,536 circles, same settings and sampling | 31.8002 / 68.0433 / 109.8465 | 27.5138 | 0 B/tick |
| Pin constraints, CCD disabled | 4,096 joints; four substeps, 16 iterations; 128 warmup + 128 samples | 7.0112 / 19.4177 / 39.4937 | 6.1277 | 0 B/tick |

The continuous sample made 170 TOI queries and 170 impact/refinement intervals.
It uploaded and downloaded 103.75 B/tick each, plus 9,685.50 B/tick of uniforms.
The whole measured owner-thread loop allocated zero managed bytes. Every projectile
remained inside the corridor, with absolute speed within 0.1 unit/s of 3000.
The final explicit 256-body read and its allocation were outside step and transfer
samples. Allocation results do not measure driver/native allocations or other
threads. The contact-response rows retain the entire loaded population; their
upload/readback was 160 B/tick each. None of these rows measures public-world event
delivery, a rendered window, networking or the full CPU path.

Final local verification:

- Release runtime/source-native build: zero warnings/errors.
- Dedicated CCD runner and complete `ELECTRON2D_TEST_GPU_PHYSICS=1` runner: passed,
  including renderer-independent compute lifetimes for GPU and compatibility
  renderers. The existing CPU comparison cases in that runner passed; the separate
  complete CPU collider aggregate was not run for this change.
- `tools/coverage/check.sh`: 11,275 compiled declarations, zero unmapped; public
  CCD remains blocked on the integrated CPU/scene/server slice.
- Runtime format check excluding Vendor and format check of the three touched
  test files: passed.
- `python3 -B tools/shaders/check_runtime.py`: passed, including rejection of an
  intentionally invalid include and 26 generated shader resources in a published
  probe assembly.
- `python3 -B tools/wiki/test_generate.py`, wiki generation and `--check`: passed;
  660 public types, 678 files, zero subsequent changes. Generated output is ignored.

Local evidence: `/tmp/electron2d-resident-ccd-final-gpu.log`,
`/tmp/electron2d-resident-ccd-tests12.log`,
`/tmp/electron2d-ccd-gpu-load.txt`,
`/tmp/electron2d-ccd-binding-limit-result.log` and the
`/tmp/electron2d-resident-ccd-*-final*.log` build/coverage/wiki logs. These temporary
files are not packaged artifacts. Generated SPIR-V SHA-256 identifies the tested
bytecode:

| Kernel | SHA-256 |
| --- | --- |
| PhysicsResidentCCD | `d98c0e8aa4547623cc57a2f7dab04979a96fb557e4cc95bdc77dee601c880e1a` |
| PhysicsResidentShapes | `791ccf5a16f9f95561ed66039691eb953e381e58359411e612b544b6f322158f` |
| PhysicsResidentContacts | `d5ac16e41b1a6a4024995edb031160ffe4715cc69311a3321629116f9ef92d3f` |
