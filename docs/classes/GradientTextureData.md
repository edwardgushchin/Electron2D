# GradientTextureData

Last updated: 2026-09-23

- Declaration: `internal sealed class GradientTextureData`
- Source: [GradientTextureData.cs](../../src/Scene/Resources/GradientTextureData.cs)
- Component: [Gradients](../components/gradients.md)

## Description and runtime use

Shared composition used by GradientRampTexture and GradientTexture, without a new public base class. A texture constructs `new GradientTextureData(this, ramp: true)` for a row or false for a fill. Settings commit under one gate. SetSource detaches the previous borrowed gradient, attaches the new one and marks pending work. Source notifications only invalidate. CapturePixels lazily samples the complete gradient under its lock and atomically replaces the immutable TexturePixels payload. A null source freezes the previous payload. Baking has no native dependency.

## Internal API

| Declaration | Contract |
| --- | --- |
| `GradientTextureData(Texture owner, bool ramp)` | Row defaults 256×1; fill defaults 64×64. Null source, byte storage, dirty state, no pixels. |
| `int Width { get; set; }`, `int Height { get; set; }` | Validate 1..16384 and always invalidate/notify. Row owners never change Height. |
| `bool UseHDR { get; set; }` | Actual changes invalidate/notify. |
| `Gradient? Gradient { get; set; }` | Borrowed live source, identity no-op, changed reference invalidates/notifies. |
| `FillEnum Fill { get; set; }` | Validated fill; always invalidate/notify. |
| `Repeat Repeat { get; set; }` | Validated repeat; always invalidate/notify. |
| `Vector2 FillFrom { get; set; }`, `Vector2 FillTo { get; set; }` | Finite UV endpoints; always invalidate/notify. |
| `Vector2 GetSize()` | Atomic logical dimensions without baking. |
| `TexturePixels? CapturePixels()` | Coalesce pending work, publish complete new pixels or retain frozen pixels; no notification. |
| `void CopyTo(GradientTextureData target, bool deep, Func<Resource?, Resource?> duplicate)` | Snapshot settings/pixels, apply graph source policy outside source lock, install under target lock and mark dirty. |
| `void Dispose()` | Detach source and clear pixels without disposing the gradient. |

## Algorithms and invariants

Offset maps inclusive row or UV coordinates through the selected fill and repeat equations. Finite geometry arithmetic widens to double; the reference near-zero Linear line cutoff is retained. Gradient.BakeTexture holds the source lock throughout all color samples, reuses internal OKLAB conversion, writes canonical little-endian float RGBA or rounded RGBA8 and publishes copied source bytes through TexturePixels. Solid planar fills use Image.Fill, retaining its byte truncation. New bakes replace allocation identity even for equal physical dimensions. Repeated reads reuse the snapshot until invalidation. CPU copies/bakes allocate; no new work queue or renderer subsystem is introduced.

CheckAlive rejects disposed owners. Dimension validates settings before commitment. Source notifications are checked against the current source. Events execute after releasing the texture gate. A failed bake keeps pending work and old pixels; a failed setting validation keeps the configuration. Resource copying/disposal and cross-resource transactions require caller coordination. Large images hold the resource locks for the bake duration and may exceed managed buffer/memory limits. Upgrade to versioned snapshots only if measured contention warrants it.

## Verification

[GradientTests](../../tests/Electron2D.Tests/GradientTests.cs) checks coherence, copies, subscription replacement, failed baking and disposal. [RenderingGradientTests](../../tests/Electron2D.Tests/RenderingGradientTests.cs) verifies native consumption and retained-geometry updates.
