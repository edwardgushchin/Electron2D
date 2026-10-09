# WaterSimulation

Last updated: 2026-10-09

**Namespace:** `Electron2D.Examples.WaterPlayground`. **Declaration:** `internal sealed partial class WaterSimulation : IDisposable`. **Sources:** [model/CPU](../../examples/WaterPlayground/WaterSimulation.cs), [public GPU consumer](../../examples/WaterPlayground/WaterSimulation.GPU.cs), [toys and mechanisms](../../examples/WaterPlayground/WaterSimulation.Toys.cs), [compute passes](../../examples/WaterPlayground/Water.comp.glsl). **Component:** [Water playground](../components/water-playground.md).

An internal example model using public RenderingDevice, PhysicsServer, RID and Shape API. DefaultCount is 65,536. The constructor prepares equal-mass particle state, an explicit active physics space and container geometry. Step advances eight coupled intervals, each with two CPU/GPU density-constraint passes, returning linear/angular reaction impulses to solid toys before advancing the rigid-body space. The scene tree does not also step this explicitly owned space.

SetUseGPU borrows a local device and creates owned compute resources on first activation. Later switches preserve particle state; the device remains borrowed until window disposal. Solid-side kernel samples account for boundary pressure, alongside contact nonpenetration. GPU records fit eight storage bindings. Particle storage is grouped by spatial cell once per tick; particle ordering is private and has no persistent identity contract. Body rotations and predicted transforms are prepared once per coupling interval. GPU reductions return final body impulses rather than the full array of workgroup partial sums. One retained particle readback publishes positions and velocities for presentation and CPU transitions. StepMS includes waiting and readback.

WorldSize is always 1152×800. Window changes preserve existing world coordinates and mass. The adjustable inlet progressively releases particles from a fixed reservoir; the drain compacts the active prefix and returns particles for later emission. The neighbor grid grows upward for offscreen births and splashes. There is no ceiling, reflection or deletion at the top. Rotated sprite bounds preserve side/bottom containment, including beaks and sails. EntryY changes the offscreen birth plane without moving existing objects.

BeginDrag, MovePointer and EndDrag control toy anchors or fluid stirring. The internal WaterToy enum identifies matching CPU/GPU solid geometry. AddToy creates a hollow bucket, pinned paddle wheel with force-coupled guided lift, kinematic gate, buoyant ball, wood or steel. Cargo is capped at eight bodies. Cached shapes are reused; Dispose releases joints before bodies and is idempotent. The borrowed compute device is not disposed by the simulation.

CPU particle passes reuse work items and a completion event; the owner joins all workers and propagates failures before reusing pass state. The warmed numerical step has a separate zero-managed-allocation check.

The [component contract](../components/water-playground.md) states units, numerical limitations and verification. No public fluid type is added to Electron2D.dll.
