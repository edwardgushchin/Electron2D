# GPUPhysicsWorld

Last updated: 2026-10-07

**Declaration:** `internal sealed unsafe partial class GPUPhysicsWorld : IDisposable`

**Source:** [GPUPhysicsWorld.cs](../../src/Servers/Physics/GPUPhysicsWorld.cs), [GPUPhysicsWorld.Solver.cs](../../src/Servers/Physics/GPUPhysicsWorld.Solver.cs), [GPUPhysicsWorld.Collision.cs](../../src/Servers/Physics/GPUPhysicsWorld.Collision.cs), [GPUPhysicsWorld.Storage.cs](../../src/Servers/Physics/GPUPhysicsWorld.Storage.cs) · **Component:** [GPU physics](../components/gpu-physics.md)

## Internal flow

The developing GPU-world host currently executes velocity and delta-pose
integration, circle/capsule/segment/polygon manifolds, contacts and revolute/wheel constraints. It retains the rendering device when available or creates a
windowless SDL compute device, with its own video-subsystem reference. Packed
80-byte body records and 32-byte integration/64-byte solver uniforms have matching compute layouts.
Contact/joint records occupy 176/192 bytes. GPU/transfer buffers grow together
before use and retain their capacity.

`GenerateManifolds` packs geometry once per referenced shape, current pair
transforms and fat-proxy overlap, then generates all contact points in one
compute submission. Its packed shape/pair/result records occupy 144/48/80
bytes. The complete result is validated before reaching material, pre-solve,
warm-start and contact-transition processing in the managed world. Chain
segments are rejected explicitly; broad-phase pairs and sensor queries still
belong to the CPU path.

`Integrate` requires the live world owner. It packs awake states, submits the
integration kernel, waits for the submission fence, verifies every returned
pose/velocity is finite, then publishes the result into the managed query mirror.
`Solve` uploads raw constraints and computes their effective masses, softness,
anchor frames and warm-start state on GPU. It retains states across all four substeps, preserves colored/overflow
ordering, then publishes poses, prepared joint frames and contact/joint impulses after one
submission fence. `Dispose` releases buffers, pipelines, device reference and video reference on
that owner; repeated disposal is inert. Partial native-resource creation releases
already created resources before throwing. No handles or backend types are public.

## Verification and limits

[GPUPhysicsTests](../../tests/Electron2D.Tests/GPUPhysicsTests.cs) selects real SDL
compute execution through `ELECTRON2D_TEST_GPU_PHYSICS=1`. CPU integration is the
numeric comparison source. Current tests do not establish a full GPU physics world,
cross-device determinism or sustained application frame rate. See the component
status for remaining world stages and backend selection/fallback work.
