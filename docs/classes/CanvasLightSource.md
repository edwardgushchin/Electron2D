# CanvasLightSource

Last updated: 2026-10-25

Declaration: `internal sealed record CanvasLightSource(string Function, string State)`. Source: [VisualShader.Lights.cs](../../src/Scene/Resources/VisualShader.Lights.cs).

Cold source identity for the generated light function/private state. The offline importer resolves these collision-free names to live module IDs, then persists the versioned record; runtime loading never relies on debug names.

See [canvas light graphs](../components/canvas-light-graphs.md) for shared resource slots, portable ownership and the exact verification corpus and limits.
