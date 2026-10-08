# WaterSimulation

Last updated: 2026-10-09

**Namespace:** `Electron2D.Examples.WaterPlayground`. **Declaration:** `internal sealed partial class WaterSimulation : IDisposable`. **Sources:** [model/CPU](../../examples/WaterPlayground/WaterSimulation.cs), [public GPU consumer](../../examples/WaterPlayground/WaterSimulation.GPU.cs), [compute passes](../../examples/WaterPlayground/Water.comp.glsl). **Component:** [Water playground](../components/water-playground.md).

An internal example model using public RenderingDevice, PhysicsServer, RID and Shape API. DefaultCount is 65,536. The constructor prepares equal-mass particle state, an explicit active physics space and container geometry. Step advances the selected CPU/GPU density-constraint passes and exchanges reaction impulses with the duck/boat before advancing the rigid-body space. The scene tree does not also step that explicitly owned space. Time controls progressive particle release and one-time toy insertion; it never scripts their subsequent trajectories.

SetUseGPU borrows a local device and creates owned compute resources on first activation. Later switches preserve state; the device remains borrowed until window disposal. GPU state is read into retained arrays for display and CPU transitions. StepMS measures full caller elapsed time, including waiting/readback. Positions and toy poses are presentation snapshots. WorldSize is always 1152×800. There is no simulation-resize operation; window changes affect presentation only. ActiveCount grows during the eight-second pour until all 65,536 particles are released. The neighbor grid expands for high splashes. BeginDrag/MovePointer/EndDrag manage physical toy anchors or fluid stirring. Dispose is idempotent and releases owned resources without disposing the borrowed device.

The [component contract](../components/water-playground.md) states numerical parameters, units, rendering, limitations and executable checks. No public fluid type is added to Electron2D.dll.
