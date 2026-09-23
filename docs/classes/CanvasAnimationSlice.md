# CanvasAnimationSlice

Last updated: 2026-09-23

- Declaration: `internal readonly record struct CanvasAnimationSlice(double Length, double Begin, double End, double Offset)`
- Source: [CanvasGeometry.cs](../../src/Servers/Rendering/CanvasGeometry.cs)
- Component: [Canvas rendering](../components/canvas-rendering.md#animation-intervals-and-rectangles)

## Description and members

An immutable value retained inside CanvasCommand. All constructor values are finite double-precision seconds validated by CanvasItem; it has no separate resource ownership, disposal or public API. Constructor arguments become record properties. CanvasItem and RenderingServer access it on the scene owner thread.

`internal bool Includes(double time)` computes the signed positive remainder of `time - Offset` by Length using the existing Mathf.PosMod and returns whether the phase is at least Begin and below End. Bounds are not clamped, normalized or wrapped. A zero period produces NaN and therefore false; a negative period produces the divisor's signed phase range. Overflow of the subtraction also produces no matching phase. The finite render clock is supplied by RenderingServer; direct managed tests supply deterministic times.

CanvasItem evaluates interval commands even while hidden, so the next interval or DrawEndAnimation can restore geometry. Intervals replace one another rather than nesting. This value allocates no objects and stores no callbacks. See [CanvasItem](CanvasItem.md#drawanimationslice) for public examples, guards, clock behavior and verified tests.
