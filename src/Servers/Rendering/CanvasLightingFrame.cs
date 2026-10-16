using System.Runtime.InteropServices;

namespace Electron2D;

internal sealed class CanvasLightingFrame
{
    internal readonly List<Vector4> Data = [];
    internal readonly List<(Texture Texture, int Light, Rect2i Region)> Masks = [];
    internal int LightCount, SegmentCount;
    private int _contourCount;
    internal Rect2 SDFRect;
    internal Vector2i SDFSize;
    internal Vector2i AtlasSize;
    internal void Clear() { Data.Clear(); Masks.Clear(); LightCount = SegmentCount = _contourCount = 0; AtlasSize = Vector2i.Zero; }
    internal int Bytes => checked(Math.Max(1, Data.Count) * 16);
    internal void CopyTo(Span<byte> target)
    {
        target[..Bytes].Clear(); MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(Data)).CopyTo(target);
    }
    private static float Bits(uint value) => BitConverter.UInt32BitsToSingle(value);
    private static float Bits(int value) => BitConverter.Int32BitsToSingle(value);

    internal void AddLight(Light light, Transform transform, bool linear = false)
    {
        var directional = light is DirectionalLight || light is RetainedCanvasLight { Mode: CanvasLightMode.Directional };
        Texture? texture = directional ? null : (light as PointLight)?.Texture;
        if (!directional && light is PointLight && (texture is null || texture.IsDisposed || texture.GetWidth() <= 0 || texture.GetHeight() <= 0)) return;
        var inverse = Transform.Identity; var height = light.GetHeight(); var kind = directional ? 1f : 0f;
        var origin = transform.Origin; var direction = transform.Y.Normalized(); var maxDistance = (light as DirectionalLight)?.MaxDistance ?? (directional ? 10000 : 0);
        if (!directional && light is PointLight point)
        {
            var size = texture!.GetSize() * point.TextureScale;
            var mask = transform * new Transform(new Vector2(size.X, 0), new Vector2(0, size.Y), point.Offset - size / 2);
            if (!mask.IsFinite() || !float.IsFinite(mask.Determinant()) || mask.Determinant() == 0) throw new InvalidOperationException("Point-light mask transforms must be finite and invertible.");
            inverse = mask.AffineInverse(); height *= MathF.Sqrt(MathF.Abs(transform.Determinant()));
        }
        if (!transform.IsFinite() || !inverse.IsFinite() || !float.IsFinite(height)) throw new InvalidOperationException("Light transforms exceed the finite canvas range.");
        var sourceColor = linear ? light.Color.SRGBToLinear() : light.Color;
        var lightColor = new Color(sourceColor.R * light.Energy, sourceColor.G * light.Energy, sourceColor.B * light.Energy, sourceColor.A);
        if (!lightColor.IsFinite()) throw new InvalidOperationException("Light color and energy products must be finite.");
        var first = Data.Count;
        Data.Add(new(inverse.X.X, inverse.Y.X, inverse.Origin.X, kind));
        Data.Add(new(inverse.X.Y, inverse.Y.Y, inverse.Origin.Y, height));
        Data.Add(new(origin.X, origin.Y, direction.X, direction.Y));
        Data.Add(new(lightColor.R, lightColor.G, lightColor.B, light.Color.A));
        Data.Add(new(light.ShadowColor.R, light.ShadowColor.G, light.ShadowColor.B, light.ShadowColor.A));
        Data.Add(new(light.ShadowEnabled ? (int)light.ShadowFilter + 1 : 0, light.ShadowFilterSmooth, maxDistance, (int)light.BlendMode));
        Data.Add(new(Bits(light.RangeItemCullMask), Bits(light.ShadowItemCullMask), Bits(light.RangeZMin), Bits(light.RangeZMax)));
        Data.Add(new(Bits(light.RangeLayerMin), Bits(light.RangeLayerMax), 0, 0));
        // Atlas regions follow the fixed eight-vector light records in a separate table.
        if (texture is not null) Masks.Add((texture, LightCount, default));
        LightCount++;
        System.Diagnostics.Debug.Assert(Data.Count == first + 8);
    }

    internal bool Affects(uint mask, int z, int layer)
    {
        for (var i = 0; i < LightCount; i++)
        {
            var first = i * 8; var membership = Data[first + 6]; var range = Data[first + 7];
            if (z < BitConverter.SingleToInt32Bits(membership.Z) || z > BitConverter.SingleToInt32Bits(membership.W) ||
                layer < BitConverter.SingleToInt32Bits(range.X) || layer > BitConverter.SingleToInt32Bits(range.Y)) continue;
            if (Data[first].W != 0 || (mask & BitConverter.SingleToUInt32Bits(membership.X)) != 0) return true;
        }
        return false;
    }

    internal void PackMasks(Func<Texture, Vector2i>? sizeOf = null, bool initialize = true)
    {
        var widest = 1; long area = 1;
        foreach (var mask in Masks) { var size = sizeOf?.Invoke(mask.Texture) ?? new Vector2i(mask.Texture.GetWidth(), mask.Texture.GetHeight()); widest = Math.Max(widest, size.X); area = checked(area + checked((long)size.X * size.Y)); }
        var width = 1; var desired = Math.Max(widest, (int)Math.Min(16384, Math.Ceiling(Math.Sqrt(area))));
        while (width < desired && width < 16384) width *= 2;
        var x = 0; var y = 0; var row = 0;
        for (var i = 0; i < Masks.Count; i++)
        {
            var mask = Masks[i]; var size = sizeOf?.Invoke(mask.Texture) ?? new Vector2i(mask.Texture.GetWidth(), mask.Texture.GetHeight()); var w = size.X; var h = size.Y;
            if (w > 16384 || h > 16384) throw new NotSupportedException("Light masks require at most 16384 pixels per axis.");
            if (x + w > width) { x = 0; y = checked(y + row); row = 0; }
            if ((long)y + h > 16384) throw new NotSupportedException("The combined light-mask atlas exceeds the current 16384-pixel canvas texture bound.");
            Masks[i] = mask with { Region = new(new(x, y), new(w, h)) }; x += w; row = Math.Max(row, h);
        }
        AtlasSize = new(width, Math.Max(1, checked(y + row)));
        if (initialize) for (var i = 0; i < LightCount; i++) Data.Add(default);
        foreach (var mask in Masks)
            Data[LightCount * 8 + mask.Light] = new((float)mask.Region.Position.X / AtlasSize.X, (float)mask.Region.Position.Y / AtlasSize.Y, (float)mask.Region.Size.X / AtlasSize.X, (float)mask.Region.Size.Y / AtlasSize.Y);
    }

    internal void ConfigureSDF(Viewport viewport, Vector2i pixels)
    {
        var oversize = viewport.SDFOversize switch { CanvasSDFOversize.Percent100 => 1f, CanvasSDFOversize.Percent120 => 1.2f, CanvasSDFOversize.Percent150 => 1.5f, _ => 2f };
        var margin = new Vector2(MathF.Floor(pixels.X * (oversize - 1) / 2), MathF.Floor(pixels.Y * (oversize - 1) / 2));
        SDFRect = new(-margin, new Vector2(pixels.X, pixels.Y) + margin * 2);
        var scale = 1f / (1 << (int)viewport.SDFScale);
        SDFSize = new(Math.Max(1, (int)MathF.Ceiling(SDFRect.Size.X * scale)), Math.Max(1, (int)MathF.Ceiling(SDFRect.Size.Y * scale)));
    }

    internal void AddContour(OccluderPolygon polygon, Transform transform, uint mask, int layer, bool sdf)
    {
        var points = polygon.Points; if (points.Length < 2) return;
        var count = polygon.Closed ? points.Length : points.Length - 1;
        var group = checked(++_contourCount);
        for (var i = 0; i < count; i++)
        {
            var a = transform * points[i]; var b = transform * points[(i + 1) % points.Length];
            if (!a.IsFinite() || !b.IsFinite() || !(b - a).IsFinite()) throw new InvalidOperationException("Occluder transforms exceed the finite canvas range.");
            if (a == b) continue;
            Data.Add(new(a.X, a.Y, b.X, b.Y));
            Data.Add(new(Bits(mask), Bits(layer), (int)polygon.CullMode, Bits(sdf ? (polygon.Closed ? group : -group) : 0)));
            SegmentCount++;
        }
    }
}

internal readonly record struct CanvasLightingItem(Color Ambient, uint Mask, int Z, int Layer, LightMode Mode);
