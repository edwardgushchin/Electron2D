# CharacterMovementScene and Player

Last updated: 2026-10-05

- Declarations: `internal static class CharacterMovementScene`, `internal sealed class Player : Sprite`.
- Source: [CharacterMovementScene.cs](../../examples/CharacterMovement/CharacterMovementScene.cs).
- Assembly: `CharacterMovement`, a separate consumer of the public `Electron2D.dll` API.

`CharacterMovementScene.CreateWindow(Texture, Font)` returns a detached root window initially sized 800×600, with a 400×300 minimum. It adds a retained grid, two font-rendered labels and a nearest-filtered character sprite. The supplied texture and font are borrowed until `Engine.Run` completes. The Ready handler sets the default clear color; neither this builder nor Player accesses a backend or native binding.

Player enables input and process callbacks on Ready. Each process step reads the four arrow keys, normalizes the direction, moves at 160 pixels per second and clamps its center to the current field. Window.SizeChanged recomputes the field, redraws the retained grid and repositions the bottom instructions. Player.SetPlayArea retains its current position and immediately clamps it when the field shrinks. The character scales by the smaller window-width/800 and window-height/600 ratio, preserving its aspect ratio. Its bounds use half of the current sprite size; text and grid spacing stay constant. Releasing keys stops movement. The first non-echo Escape press calls `SceneTree.Quit`; native window close is handled by the ordinary engine host.

[Program.cs](../../examples/CharacterMovement/Program.cs) loads the bundled assets and retains them around the host call. The [example instructions](../../examples/CharacterMovement/README.md) describe setup and the screenshot provenance. [CharacterMovementTests](../../tests/Electron2D.Tests/CharacterMovementTests.cs) links this exact source, checks real GPU and compatibility output, typed input, native grow/shrink/minimum-size/restore, exits and cleanup on Linux x64/Wayland. Its readback remains internal to tests and adds no public capture API. The example adds no physics, project authoring or runtime API.
