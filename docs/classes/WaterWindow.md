# WaterWindow

Last updated: 2026-10-09

**Namespace:** `Electron2D.Examples.WaterPlayground`. **Declaration:** `internal sealed class WaterWindow : Window`. **Inherits:** [Window](Window.md). **Source:** [WaterWindow.cs](../../examples/WaterPlayground/WaterWindow.cs). **Component:** [Water playground](../components/water-playground.md).

An internal public-API consumer, not an exported engine type. Its constructor borrows a Font, creates a 1152×800 resizable Window with a 480×360 minimum and owns one WaterSimulation. One uniform display scale fits the fixed 1152-unit world width to the client width, with the world bottom aligned to the client bottom. Shapes retain their proportions in every window mode. A wider aspect ratio shows less upper air; a taller one shows more. There are no side gutters. CPU/GPU buttons and Tab choose the liquid algorithm; state is preserved across changes. A GPU creation failure is visibly reported with CPU selected. The --cpu entry-point option avoids initial compute creation.

Native SizeChanged updates the view transform, UI placement and entry plane for future water particles and bodies, then rebuilds the surface for the visible height. Existing body/particle state remains unchanged; future water, toys and fish start above the visible top, even in tall windows. Reset refreshes that entry plane. OnPhysicsProcess advances the liquid, reconstructs WaterSurface and publishes the physical fish poses. OnProcess publishes toy/fish drawings and the HUD. Transparent clipped density triangles are drawn in front of toys and fish, replacing the old disc instances. The readout identifies the active liquid backend, full step time and rendered FPS. Space pauses, R refills, F11 toggles borderless fullscreen with window-size restoration, Escape closes, and mouse dragging applies physical impulses to the duck, boat or any fish. The top is open for water, toys and fish, including a mouse grab above the window; side and bottom containment remain active. Focus loss releases dragging. Reset retains the window's borrowed-device owner; disposal frees the simulation, controls and render resources, then its compute device. The captured engine catch-up budget is restored. The caller retains the font through Engine.Run.

```csharp
using var font = new FontFile { Data = File.ReadAllBytes("Assets/IBMPlexSans-Regular.ttf") };
WaterWindow.ConfigurePresentation();
using var window = new WaterWindow(font);
Engine.Run(window);
```

The constructor's optional startOnGPU argument is internal to this executable. The [simulation contract](../components/water-playground.md) distinguishes liquid computation, rigid physics, rendering selection and platform verification.

Rendering is limited to 144 FPS and requests DisplayServer.VSyncMode.Disabled. ConfigurePresentation selects XWayland when available in a Wayland session unless the caller explicitly selects a video driver. Only this process environment changes; the desktop remains unchanged. The HUD reports the actual policy, including unsupported-mode fallback.
