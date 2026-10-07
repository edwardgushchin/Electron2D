# GPU physics implementation status

Last updated: 2026-10-07

[ADR 0054](../decisions/physics.md#adr-0054) selects a full GPU world alongside the
managed CPU compatibility backend. Implementation is in progress. The existing
public physics API still selects CPU; no production GPU/fallback selector has
been exposed yet.

`GPUPhysicsWorld` executes velocity/delta-pose integration and the colored and
overflow contact solver: warm start, speculative/soft bias, one/two contact
points, friction, tangent speed, rolling resistance and restitution. Revolute
and wheel constraints execute on GPU, including springs, motors and limits.
These implement the current PinJoint and GrooveJoint backend constraint roles.
The spring-joint force preflight/application still belongs to CPU world setup.

The internal `PhysicsSpace.EnableGPUSolver` development entry submits all four
substeps as one GPU command buffer. Body/contact/joint state remains resident
between stages and is published once after its fence. Packed records are 80,
176 and 160 bytes; solver uniforms occupy 48 bytes. GPU/transfer buffers retain
capacity. Colored groups execute in parallel without shared dynamic-body writes;
overflow preserves serial joint/contact order. The earlier
`EnableGPUIntegration` entry remains a numeric development check.

The solver callback runs on the world owner. CPU pair generation, collision,
constraint preparation, sleep/CCD finalization and queries remain in the managed
backend; large worlds retain CPU collision workers. There is no production
backend selector yet. A GPU failure drains pending CPU tasks, releases scratch
ownership and rejects replay while permitting world disposal. This hybrid stage
is not the completed GPU backend.

When a GPU renderer exists, the physics world retains its SDL device handle;
otherwise it creates a compute device without a window. Its video-subsystem
reference and safe native resource handles have independent lifetimes. A CPU
world does not create compute resources. Native checks exercise both a retained
GPU-render device and a separate compute device beside the compatibility
renderer, then dispatch after renderer teardown. Complete startup fallback
still needs integration verification.

`ELECTRON2D_TEST_GPU_PHYSICS=1` selects the native compute checks. These exercise
gravity, forces/torque, damping, locks, speed caps and rotation at dispatch sizes
1, 63, 64, 65, 4,097 and 65,536. GPU/CPU floating point results use a relative
tolerance; this does not establish bit-identical cross-device simulation or
complete world parity. The development world check compares 120 falling/contact
ticks with CPU, reads contacts and direct queries, invokes post-solver callbacks
and verifies faulted-world disposal without replay. Eight prepared dispatches
at each tested size allocate zero managed bytes in the checked Linux/Vulkan run.
The solver conformance check compares 64 four-substep batches, including
colored/serial contacts and motor/limit/spring joint equations; eight prepared
solver submissions also allocate zero managed bytes.
The first maximum-Smash GPU-solver profile (65,537 bodies, 32 warmup/64
measured headless ticks) averaged 132.88 ms, p95 185.80 ms, with zero
all-thread managed bytes after storage reservation followed the prepared world
capacity. A phase-instrumented repeat averaged 140.36 ms: packing 5.15 ms,
command recording/upload 2.45 ms, submission/fence wait 22.73 ms, publication
3.91 ms. The wait includes transfers and execution; these are host timings,
not GPU timestamp queries. This hybrid path does not establish an application
speedup, sustained frame rate or complete GPU-world acceptance. An earlier
contended run grew body/contact buffers during wakeup and allocated 32,245,152
bytes across 64 ticks; that growth was corrected before the clean profiles.
The real native window run (32 warmup/64 measured frames, 65,537 bodies)
reached 5.32 FPS. Its zero-allocation gate failed: three frames allocated 4,992
managed bytes each outside the measured physics phases and renderer. The same
rare allocation signature was already observed in the CPU-host profile; its
source remains unresolved. This run is retained as failed evidence and does
not support a whole-frame zero-allocation claim.
Native allocation accounting, other devices/platforms and visual acceptance
remain unverified.

Remaining work: GPU broad phase/collision and constraint preparation, spring
force setup, sleep/CCD finalization, complete query/event/state contracts, independent backend selection
and startup fallback, native end-to-end scene checks and performance profiling.
