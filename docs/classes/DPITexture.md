# DPITexture

Last updated: 2026-10-15

Namespace: Electron2D. Inherits Texture → Resource → ElectronObject.

DPITexture retains SVG authoring data and rasterizes it with the existing native SVG decoder. Canvas replay selects a retained raster matching the actual viewport oversampling. BaseScale changes intrinsic logical dimensions; SetSizeOverride changes independent drawing axes without resampling. An override of zero restores that axis. Empty source produces an empty texture and suppresses retained draws until source is restored.

```csharp
using var icon = DPITexture.CreateFromString(svg, colorMap: new()
{
    [Colors.Red] = Colors.Lime,
});
window.OversamplingOverride = 2;
canvas.Draw += item => item.DrawTexture(icon, new(10, 10));
```

CreateFromString accepts source, finite positive scale, finite saturation and an optional copied Dictionary<Color, Color>. GetSource/SetSource expose the original SVG. BaseScale, Saturation, FixAlphaBorder, PremultAlpha and ColorMap invalidate raster variants after actual changes. ColorMap reads and writes copy; equal maps/settings are silent. Double-quoted fill, stroke and stop-color paint attributes are replaced before decoding; none and URL references remain untouched. Replacement emits RGB while SVG opacity remains independent. CSS style declarations and arbitrary SVG syntax are governed by the native decoder's existing capabilities.

GetScaledRID returns the current recording item's viewport raster identity, or the stable base identity outside drawing. Other scale identities are borrowed until settings change, the last viewport using their scale releases it, or the texture is disposed. GetRID retains the ordinary stable resource identity until disposal. GetImage copies the base-scale image; frame queries use logical dimensions. HasAlpha is true. PremultAlpha requires the matching premultiplied-alpha material when drawing.

Source validation rejects malformed XML, DTDs, invalid text, oversized source and dimensions. Rasterization retains the image codec budgets and format limits. Source/settings mutation and pixel queries synchronize per resource; callers coordinate multi-call edits and disposal. Copies own independent authoring data/caches. ResourceLocalToScene defaults false. Source and settings persist through typed resources/scenes; runtime raster caches and size overrides are not stored.

See [scalable SVG textures](../components/scalable-svg.md) for ownership, scale lifetime and verification limits.
