# GPU physics implementation status

Last updated: 2026-10-07

[ADR 0054](../decisions/physics.md#adr-0054) selects a full GPU world alongside the
managed CPU compatibility backend. Implementation is in progress. The existing
public physics API still selects CPU; no production GPU/fallback selector has
been exposed yet.

`GPUPhysicsWorld` currently executes velocity and delta-pose integration using
SDL compute commands and offline SPIR-V. The internal
`PhysicsSpace.EnableGPUIntegration` development entry connects those kernels to
the ordinary four-substep world. Contacts, constraints, broad phase, sleeping,
continuous collision and queries still use the managed backend. This is a hybrid
bring-up stage, not the completed GPU backend.

GPU storage and upload/download transfer buffers retain their capacity. Each
current integration stage packs awake body state, submits a compute pass, waits
for its fence and publishes a validated finite result into the live CPU mirror.
The intermediate path deliberately uses one solver worker so the GPU stage runs
on the world owner. The repeated stage readbacks are a known development cost:
the completed GPU world must retain state across its stages and publish once at
the world boundary.

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
Native allocation accounting, sustained throughput,
other devices/platforms and visual acceptance remain unverified.

Remaining work: GPU broad phase and collision geometry, contact/joint solving,
sleep and CCD, common query/event/state contracts, independent backend selection
and startup fallback, native end-to-end scene checks and performance profiling.
