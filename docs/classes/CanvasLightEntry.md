# CanvasLightEntry

Last updated: 2026-10-25

Declaration: `internal sealed record CanvasLightEntry(uint Function, uint State, uint StateType)`. Source: [CanvasLightProgram.cs](../../src/Servers/Rendering/CanvasLightProgram.cs).

Validated live module IDs for the authored no-argument light function and exact private state structure. ShaderProgram retains this immutable identity for native composition; it owns no native handle.

See [canvas light graphs](../components/canvas-light-graphs.md) for shared resource slots, portable ownership and the exact verification corpus and limits.
