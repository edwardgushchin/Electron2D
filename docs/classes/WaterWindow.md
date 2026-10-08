# WaterWindow

Last updated: 2026-10-09

**Namespace:** `Electron2D.Examples.PhysicsSandbox`. **Declaration:** `internal sealed class WaterWindow : Window`. **Inherits:** [Window](Window.md). **Source:** [WaterWindow.cs](../../examples/PhysicsSandbox/WaterWindow.cs). **Component:** [Water playground](../components/physics-sandbox.md).

An internal public-API consumer, not an exported engine type. Its constructor borrows a Font, creates a 1152×800 resizable Window with a 480×360 minimum and owns one WaterSimulation. The client surface is the physical container. CPU/GPU buttons and Tab choose the liquid algorithm; state is preserved across changes. A GPU creation failure is visibly reported with CPU selected. The --cpu entry-point option avoids initial compute creation.

Native SizeChanged edits physical boundaries; OnPhysicsProcess advances one fixed interval; OnProcess publishes retained MultiMesh instances and current toy drawings. Toy geometry is behind a CanvasGroup whose 0.65 opacity applies once to the whole opaque liquid layer. This preserves translucency through overlapping particles. The readout identifies the active liquid backend, full step time and rendered FPS. Space pauses, R resets, Escape closes, and mouse dragging applies physical impulses. Focus loss releases dragging. Reset retains the window's borrowed-device owner; disposal frees the simulation, controls and render resources, then its compute device. The captured engine catch-up budget is restored. The caller retains the font through Engine.Run.

```csharp
using var font = new FontFile { Data = File.ReadAllBytes("Assets/IBMPlexSans-Regular.ttf") };
using var window = new WaterWindow(font);
Engine.Run(window);
```

The constructor's optional startOnGPU argument is internal to this executable. The [simulation contract](../components/physics-sandbox.md) distinguishes liquid computation, rigid physics, rendering selection and platform verification.
