# Character movement

Run a small rendered scene, move the character with the arrow keys, and quit with Escape or window close. The character stays inside the grid. This example uses only public Electron2D APIs.

![The first scene, captured from the GPU renderer on Linux x64/Wayland](../../docs/images/character-movement.png)

Install .NET 10 SDK and run from the repository root:

```bash
dotnet run --project examples/CharacterMovement
```

The example explicitly selects the Electron2D platform package for its target OS and RID. NuGet supplies the native dependencies automatically. The current `0.1.0-alpha.1` packages are still being prepared for public publication; see [package availability](../../docs/native-packaging.md).

[Program.cs](Program.cs) loads the borrowed texture and font, limits the loop to 60 FPS, and passes [CharacterMovementScene](CharacterMovementScene.cs)'s window to `Engine.Run`. The 800×600 scene combines a retained grid, two live text labels and a nearest-filtered `Sprite`. Arrow keys move the sprite at 160 pixels per second, with normalized diagonal movement and a fixed field boundary. Change `MovementSpeed` in [Player.cs](Player.cs) to try your first code edit.

`Engine.Run` owns the window, event pump, frame timing and teardown. The entry point retains the texture and font until the host returns. The fixed layout demonstrates rendering and input; it has no collisions, project editing or public screenshot command.

## Verification and capture

One focused check links the exact [CharacterMovementScene source](CharacterMovementScene.cs) into the runtime test executable:

```bash
env -u LD_LIBRARY_PATH ELECTRON2D_TEST_CHARACTER_MOVEMENT=1 SDL_VIDEODRIVER=wayland dotnet run --project tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release
```

It runs real GPU and compatibility hosts, checks scene pixels, all four movement directions, key release, diagonal field bounds, Escape/native close and borrowed-resource cleanup. It saves each renderer's first frame to `bin/character-movement/`. The README image is the unmodified GPU capture from that check. Readback belongs to tests; the example itself uses no backend access. This verifies Linux x64/Wayland only. Human assessment of the controls and execution on other platforms remain separate.
