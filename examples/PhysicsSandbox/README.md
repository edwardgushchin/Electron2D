# Water playground

One resizable scene, with 65,536 physical liquid particles, a rubber duck and a sailboat. Water falls first; then the toys fall into it and float. The transparent aqua layer sits in front of the toys, so submerged parts remain visible. The palette uses cream, butter yellow, apricot and lavender.

```sh
dotnet run --project examples/PhysicsSandbox -c Release
```

Choose **CPU** or **GPU** in the upper-right corner, or press Tab. Both modes use the same particle count, mass, solver iterations and initial conditions; switching preserves the current state. The readout shows full fixed-step time and rendered FPS. Drag either toy, including the boat's sail, or drag through the water to stir it. Space pauses; R restarts the sequence; Escape closes the window.

GPU mode uses the public RenderingDevice compute API. CPU mode runs the same particle algorithm on managed arrays. Both modes use PhysicsServer for the rigid toys and exchange collision impulses with the fluid. The mode selector changes the liquid calculation, not the rigid-body backend. This example adds no fluid-specific runtime API.

The initial 1152×800 container holds approximately 3.07 m³ in its modeled shallow slice and settles around one third full. Resizing preserves water quantity, so the level changes with container width. Reset fills the current size from a fresh falling block. Under overload the fixed simulation can advance slower than wall time; CPU mode at this particle count is intentionally a demanding comparison.

To start without creating a compute device:

```sh
dotnet run --project examples/PhysicsSandbox -c Release -- --cpu
```

Add `--compatibility` to select the compatibility renderer independently of liquid simulation. That renderer expands particle geometry and is much slower at this population. See the [numerical and verification contract](../../docs/components/physics-sandbox.md).
