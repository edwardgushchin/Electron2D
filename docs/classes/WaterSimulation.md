# WaterSimulation

Last updated: 2026-10-09

**Namespace:** `Electron2D.Examples.PhysicsSandbox`. **Declaration:** `internal sealed partial class WaterSimulation : IDisposable`. **Sources:** [model/CPU](../../examples/PhysicsSandbox/WaterSimulation.cs), [public GPU consumer](../../examples/PhysicsSandbox/WaterSimulation.GPU.cs), [compute passes](../../examples/PhysicsSandbox/Water.comp.glsl). **Component:** [Water playground](../components/physics-sandbox.md).

An internal example model using public RenderingDevice, PhysicsServer, RID and Shape API. DefaultCount is 65,536. The constructor prepares equal-mass particle state, an explicit active physics space and container geometry. Step advances the selected CPU/GPU density-constraint passes and exchanges reaction impulses with the duck/boat before advancing the rigid-body space. The scene tree does not also step that explicitly owned space. Time controls only one-time toy insertion; it never scripts their subsequent trajectories.

SetUseGPU borrows a local device and creates owned compute resources on first activation. Later switches preserve state; the device remains borrowed until window disposal. GPU state is read into retained arrays for display and CPU transitions. StepMS measures full caller elapsed time, including waiting/readback. Positions and toy poses are presentation snapshots. Resize accepts finite dimensions of at least 320×240, edits the container, rebuilds necessary capacity and remaps contents without changing particle count or mass. The neighbor grid expands for high splashes. BeginDrag/MovePointer/EndDrag manage physical toy anchors or fluid stirring. Dispose is idempotent and releases owned resources without disposing the borrowed device.

The [component contract](../components/physics-sandbox.md) states numerical parameters, units, rendering, limitations and executable checks. No public fluid type is added to Electron2D.dll.
