# CharacterMovementScene and Player

Last updated: 2026-10-05

- Declarations: `internal static class CharacterMovementScene`, `internal sealed class Player : Sprite`.
- Source: [CharacterMovementScene.cs](../../examples/CharacterMovement/CharacterMovementScene.cs).
- Assembly: `CharacterMovement`, a separate consumer of the public `Electron2D.dll` API.

`CharacterMovementScene.CreateWindow(Texture, Font)` returns a detached 800×600 root window. It adds a retained grid, two font-rendered labels and a nearest-filtered character sprite. The supplied texture and font are borrowed until `Engine.Run` completes. The Ready handler sets the default clear color; neither this builder nor Player accesses a backend or native binding.

Player enables input and process callbacks on Ready. Each process step reads the four arrow keys, normalizes the direction, moves at 160 pixels per second and clamps its center to the fixed field. Releasing keys stops movement. The first non-echo Escape press calls `SceneTree.Quit`; native window close is handled by the ordinary engine host.

[Program.cs](../../examples/CharacterMovement/Program.cs) loads the bundled assets and retains them around the host call. The [example instructions](../../examples/CharacterMovement/README.md) describe setup and the screenshot provenance. [CharacterMovementTests](../../tests/Electron2D.Tests/CharacterMovementTests.cs) links this exact source, checks real GPU and compatibility output, typed input, exits and cleanup on Linux x64/Wayland. Its readback remains internal to tests and adds no public capture API. The example adds no physics, project authoring or runtime API.
