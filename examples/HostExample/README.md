# Window and input

This is the first runnable Electron2D example. It opens a window, adds a scene node, moves that node while the arrow keys are held, and exits on Escape or window close. It demonstrates window/input behavior and writes movement to the terminal without drawing the scene.

Run from the repository root on Linux Wayland:

```bash
dotnet publish examples/HostExample/HostExample.csproj -c Release -r linux-x64 --self-contained true -o /tmp/electron2d-host-example
/tmp/electron2d-host-example/HostExample
```

The published directory contains the application and `Electron2D.dll`, with platform dependencies supplied by the engine project. It runs without development environment settings.

`Program.cs` configures a `Window`, adds the scene, and calls `Engine.Run`. The runtime handles the event pump, monotonic frame time, frame limit, and teardown. Scene code calls `Tree.Quit()` to exit. This consumer demonstrates the implemented window/input API; it does not draw the scene.
