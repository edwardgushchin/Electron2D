# CanvasLightProgram

Last updated: 2026-10-25

Declaration: `internal static class CanvasLightProgram`. Source: [CanvasLightProgram.cs](../../src/Servers/Rendering/CanvasLightProgram.cs).

Annotate adds the version-one light record during import. Read checks unique metadata, canonical live IDs, fragment entry, void function/signature, ten typed fields and one marker call. RemoveMarker returns native unlit code with only the marker call/record removed, preserving the retained helper and resource declarations. Invalid replacement preserves the previous program.

See [canvas light graphs](../components/canvas-light-graphs.md) for shared resource slots, portable ownership and the exact verification corpus and limits.
