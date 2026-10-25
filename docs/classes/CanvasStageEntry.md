# CanvasStageEntry

Last updated: 2026-10-25

Declaration: `internal sealed record CanvasStageEntry(uint Function, uint State, uint StateType)`. Source: [CanvasStageProgram.cs](../../src/Servers/Rendering/CanvasStageProgram.cs).

Validated live module IDs for the authored no-argument surface/light function and exact private state structure. ShaderProgram retains this immutable identity for native composition; it owns no native handle.

See [canvas light graphs](../components/canvas-light-graphs.md) for shared resource slots, portable ownership and the exact verification corpus and limits.
