# WaterWindow

Last updated: 2026-10-09

**Namespace:** `Electron2D.Examples.WaterPlayground`. **Declaration:** `internal sealed partial class WaterWindow : Window`. **Inherits:** [Window](Window.md). **Source:** [WaterWindow.cs](../../examples/WaterPlayground/WaterWindow.cs). **Tool controls/drawing:** [WaterWindow.Toys.cs](../../examples/WaterPlayground/WaterWindow.Toys.cs). **Component:** [Water playground](../components/water-playground.md).

An internal public-API consumer, not an exported engine type. Its constructor borrows a Font, creates a 1152×800 resizable Window with a 480×360 minimum and owns one WaterSimulation. One uniform display scale fits the fixed 1152-unit world width to the client width, with the world bottom aligned to the client bottom. Shapes retain their proportions in every window mode. A wider aspect ratio shows less upper air; a taller one shows more. There are no side gutters. CPU/GPU buttons and Tab choose the liquid algorithm; state is preserved across changes. A GPU creation failure is visibly reported with CPU selected. The --cpu entry-point option avoids initial compute creation.

Native SizeChanged updates the uniform view transform, UI placement and entry plane for future particles and bodies. Existing world state is unchanged. OnPhysicsProcess advances the liquid and reconstructs WaterSurface; Toy geometry is redrawn on physics changes; the visible HUD refreshes at four hertz and immediately after controls change. Transparent density triangles are drawn in front of the toys without additional object-waterline overlays.

H hides or restores the HUD, mode buttons, faucet handle and complete toolbar. Hidden controls do not intercept mouse input; keyboard shortcuts remain available. Space pauses, R refills, F11 toggles borderless fullscreen with window-size restoration, and Escape closes. Mouse dragging applies a physical spring to toys or stirs empty water. The top is open, while side and bottom containment remains active.

The bottom toolbar wraps at narrow widths and inserts a bucket, wheel with a guided lift, gate, wood, steel and ball, alongside flow/drain controls. The top handle moves the inlet; Q/E or the wheel tilts a held toy. Without a held toy they control jet direction/flow. F and D operate the valves. Focus loss releases dragging. Reset retains the borrowed compute device and interface visibility. Disposal frees simulation and render/control resources, then the device, and restores the previous physics catch-up budget. The caller retains the font throughout Engine.Run.

```csharp
using var font = new FontFile { Data = File.ReadAllBytes("Assets/IBMPlexSans-Regular.ttf") };
WaterWindow.ConfigurePresentation();
using var window = new WaterWindow(font);
Engine.Run(window);
```

The constructor's optional startOnGPU argument is internal to this executable. The [simulation contract](../components/water-playground.md) distinguishes liquid computation, rigid physics, rendering selection and platform verification.

Rendering is limited to 144 FPS and requests DisplayServer.VSyncMode.Disabled. ConfigurePresentation selects XWayland when available in a Wayland session unless the caller explicitly selects a video driver. Only this process environment changes; the desktop remains unchanged. The HUD reports the actual policy, including unsupported-mode fallback.
