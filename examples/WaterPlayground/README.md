# WaterPlayground

A resizable pastel water scene. A stream gradually releases 65,536 simulated fluid particles, then a rubber duck and sailboat fall into the basin. Six fish then fall into the water and swim underwater. A continuous translucent surface covers the immersed toys and fish.

```sh
dotnet run --project examples/WaterPlayground -c Release
```

Drag a toy or fish to lift it, or stir the water. **Space** pauses, **R** refills, **F11** toggles borderless fullscreen. **CPU/GPU** buttons or **Tab** switch the liquid solver without resetting it. `--cpu` starts with CPU fluid; `--compatibility` independently selects the compatibility renderer.

The world always measures 1152×800 units and contains about 3.07 m³ after the eight-second pour. Resizing/fullscreen scales the view uniformly to the window width, with the bottom anchored to the lower edge. Shapes keep their proportions; the view shows more or less upper air. Existing positions, mass and physics stay unchanged. Toys and fish start above the visible top and may pass through it; their complete silhouettes remain inside the sides and bottom. CPU and GPU use identical particle counts and numerical settings. Both exchange impulses with CPU PhysicsServer toys and fish. Fish propulsion acts only underwater and yields to a mouse grab.

Rendering is capped at **144 FPS** and requests **VSync off**. On a Wayland desktop with XWayland available, this example selects XWayland for immediate presentation. An explicit `SDL_VIDEO_DRIVER` environment choice takes priority. Unsupported presentation policies are reported in the window. No system settings are changed. The cap is a maximum, not a performance guarantee; CPU fluid at 65,536 particles remains slow.

The example uses public RenderingDevice, PhysicsServer, canvas and input APIs. It adds no fluid-specific runtime API. See [the model and verification contract](../../docs/components/water-playground.md).
