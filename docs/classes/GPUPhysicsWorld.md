# GPUPhysicsWorld

Last updated: 2026-10-07

**Declaration:** `internal sealed unsafe class GPUPhysicsWorld : IDisposable`

**Source:** [GPUPhysicsWorld.cs](../../src/Servers/Physics/GPUPhysicsWorld.cs) · **Component:** [GPU physics](../components/gpu-physics.md)

## Internal flow

The developing GPU-world host currently executes velocity and delta-pose
integration. It retains the rendering device when available or creates a
windowless SDL compute device, with its own video-subsystem reference. Packed
80-byte body records and 32-byte step uniforms have matching compute layouts.
GPU/transfer buffers grow together before use and retain their capacity.

`Integrate` requires the live world owner. It packs awake states, submits the
integration kernel, waits for the submission fence, verifies every returned
pose/velocity is finite, then publishes the result into the managed query mirror.
`Dispose` releases buffers, pipeline, device reference and video reference on
that owner; repeated disposal is inert. Partial native-resource creation releases
already created resources before throwing. No handles or backend types are public.

## Verification and limits

[GPUPhysicsTests](../../tests/Electron2D.Tests/GPUPhysicsTests.cs) selects real SDL
compute execution through `ELECTRON2D_TEST_GPU_PHYSICS=1`. CPU integration is the
numeric comparison source. Current tests do not establish a full GPU solver,
cross-device determinism or sustained application frame rate. See the component
status for remaining world stages and backend selection/fallback work.
