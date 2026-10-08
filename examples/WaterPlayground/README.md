# WaterPlayground

A resizable pastel water scene. A stream gradually releases 65,536 simulated fluid particles, then a rubber duck and sailboat fall into the basin. Six fish swim underwater. A continuous translucent surface covers the immersed toys and fish.

```sh
dotnet run --project examples/WaterPlayground -c Release
```

Drag a toy to lift it, or stir the water. **Space** pauses, **R** refills, **F11** toggles borderless fullscreen. **CPU/GPU** buttons or **Tab** switch the liquid solver without resetting it. `--cpu` starts with CPU fluid; `--compatibility` independently selects the compatibility renderer.

The world always measures 1152×800 units and contains about 3.07 m³ after the eight-second pour. Resizing/fullscreen only fit the view; they never change positions, mass or physics. CPU and GPU use identical particle counts and numerical settings. Both couple to CPU PhysicsServer toys. Fish are swimming visual agents constrained to the wet field.

Rendering is capped at **144 FPS** and requests **VSync off**. On a Wayland desktop with XWayland available, this example selects XWayland for immediate presentation. An explicit `SDL_VIDEO_DRIVER` environment choice takes priority. Unsupported presentation policies are reported in the window. No system settings are changed. The cap is a maximum, not a performance guarantee; CPU fluid at 65,536 particles remains slow.

The example uses public RenderingDevice, PhysicsServer, canvas and input APIs. It adds no fluid-specific runtime API. See [the model and verification contract](../../docs/components/water-playground.md).
