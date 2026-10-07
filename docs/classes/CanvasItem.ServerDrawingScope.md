# CanvasItem.ServerDrawingScope

Last updated: 2026-10-07

- Visibility: internal readonly value type
- Source: [CanvasItem.Rendering.cs](../../src/Scene/Main/CanvasItem.Rendering.cs)
- Component: [Canvas rendering](../components/canvas-rendering.md)

Saves/restores the current drawing item and recording flag for low-level commands using the standard IDisposable scope. Constrained value-type disposal requires no heap allocation; failure restoration is covered by RenderingPrimitiveTests. It introduces no public drawing scope or scene ownership.
