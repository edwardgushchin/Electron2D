# CanvasBatch

Last updated: 2026-09-23

- Declaration: `internal readonly record struct CanvasBatch`
- Source: [CanvasBackend.cs](../../src/Servers/Rendering/CanvasBackend.cs)
- Component: [canvas-rendering](../components/canvas-rendering.md)
- Visibility: internal; unavailable to engine consumers.

## Description

A contiguous vertex range with one borrowed material state, command texture and addressing mode. CanvasItem.AppendCanvas coalesces adjacent ranges only when those values match, preserving draw order. Backend caches own native resources; this record owns none.

## Member summary

| Declaration | Contract |
| --- | --- |
| `CanvasBatch(int First, int Count, MaterialState? Material, Texture? Texture = null, bool Tile = false)` | [Construction and values](#construction-and-values) |
| `internal byte[]? ShaderCode { get; }` | [Shader code](#shader-code) |

## Member descriptions

### Construction and values

`CanvasBatch(int First, int Count, MaterialState? Material, Texture? Texture = null, bool Tile = false)`

First and Count index the prepared triangle list. Material null selects the built-in program. Texture null means opaque white for the built-in command sampler. Tile selects repeat rather than clamp addressing. These arguments become record properties.

### Shader code

`internal byte[]? ShaderCode { get; }`

Returns Material.Program.Code or null for the default program. The byte array is shared immutable runtime state, never returned directly through public Shader APIs.

## Verification and limits

[RenderingRuntimeTests](../../tests/Electron2D.Tests/RenderingRuntimeTests.cs) and [CanvasTextureTests](../../tests/Electron2D.Tests/CanvasTextureTests.cs) exercise this path through retained drawing and native readback on Linux Wayland and dummy/software. Pixel and allocation checks cover the documented baseline; they do not establish other platforms or frame-time guarantees.
