# CanvasOperation

Last updated: 2026-10-04

Internal enum in [CanvasBackend.cs](../../src/Servers/Rendering/CanvasBackend.cs), owned by [canvas composition](../components/canvas-rendering.md#group-composition-and-screen-snapshots). Draw submits geometry; Copy snapshots a region; GroupBegin switches to and clears transparent backbuffer storage; GroupEnd completes optional mip generation and returns to owner drawing. These boundaries cannot coalesce with ordinary geometry. The enum owns no handles, nodes or material resources and is not public API. Both native backends consume it, with explicit profile capability checks before drawing.

MaskBegin copies the main region and selects captured drawing. MaskEnd returns to final owner drawing (GPU) or selects reusable alpha accumulation (hardware compatibility). MaskFinish completes compatibility screen-color/alpha composition and restores main drawing. Zero-count control markers cannot coalesce with Draw ranges.
