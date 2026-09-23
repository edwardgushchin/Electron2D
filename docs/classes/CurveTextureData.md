# CurveTextureData

Last updated: 2026-09-23

- Declaration: `internal sealed class CurveTextureData`
- Source: [CurveTextureData.cs](../../src/Scene/Resources/CurveTextureData.cs)
- Component: [Curves](../components/curves.md)
- Visibility: internal; no public inheritance layer.

## Description and flow

Shared state owned by CurveTexture and CurveXYZTexture. One per-resource lock serializes settings, coherent curve sampling and immutable pixel publication. Distinct sources subscribe before sampling so a racing edit cannot be lost; replacement detaches old sources only after success and removes new subscriptions on failure. Sampling errors preserve prior settings/pixels. Events run after the lock is released.

## Internal members

| Declaration | Contract |
| --- | --- |
| `CurveTextureData(Texture owner, bool single)` | Default width 256, RGB mode, one or three null curve slots, no pixels. |
| `int Width { get; set; }` | Validates 32..4096; changed width rebakes and notifies owner. |
| `CurveTexture.TextureModeEnum Mode { get; set; }` | Validates the scalar channel mode; XYZ owners never change it. |
| `Curve? GetCurve(int channel)` | Reads a known internal channel. |
| `void SetCurve(int channel, Curve? curve)` | Replaces a borrowed source, validates lifetime, deduplicates subscriptions and rebakes. |
| `TexturePixels? CapturePixels()` | Returns the current immutable payload or null. |
| `void CopyTo(CurveTextureData target, bool deep, Func<Resource?, Resource?> duplicate)` | Snapshots settings, follows graph copy policy and rebuilds under the target lock. Callers coordinate copies. |
| `void Dispose()` | Detaches every distinct source and clears pixels; does not own curves. |

Runtime usage: `new CurveTextureData(this, single: true)` inside a CurveTexture constructor. Private Bake captures each distinct curve once, writes little-endian RF/RGBF bytes and reuses a compatible TexturePixels allocation token. Curve.SampleTexture holds the curve lock across all samples. Different resources do not form a transaction. CopyTo releases the source lock before invoking graph callbacks or locking the target. No renderer/native call occurs during baking.

## Limits and verification

Baking and output copies allocate. Rebaking holds a resource lock; no throughput guarantee is claimed. Disposing/copying concurrently with use requires caller coordination. [CurveTextureTests](../../tests/Electron2D.Tests/CurveTextureTests.cs) verifies events, alias/copy policy, coherent snapshots and failure/disposal paths; [native checks](../../tests/Electron2D.Tests/RenderingCurveTextureTests.cs) verify existing backend consumption.
