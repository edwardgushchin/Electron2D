# Window and input

This is the first runnable Electron2D example. It opens a window, adds a scene node, moves that node while the arrow keys are held, and exits on Escape or window close. Until the rendering example arrives, it writes movement to the terminal.

Run from the repository root on Linux Wayland:

```bash
dotnet publish examples/HostExample/HostExample.csproj -c Release -r linux-x64 --self-contained true -o /tmp/electron2d-host-example
/tmp/electron2d-host-example/HostExample
```

The published directory contains the application and `Electron2D.dll`, with platform dependencies supplied by the engine project. It runs without development environment settings.

`Program.cs` shows the scene and input callbacks. `ApplicationHost.cs` is application bootstrap code: it opens the display, starts the scene loop, pumps events before each frame, uses a monotonic clock, waits to limit frame rate, and disposes the scene and window on exit. A game can replace this policy in its own executable while using the same public engine API.
