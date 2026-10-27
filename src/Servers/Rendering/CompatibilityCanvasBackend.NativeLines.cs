using SDL3;

namespace Electron2D;

internal sealed partial class CompatibilityCanvasBackend
{
    private RenderHandle? _lineCoverage, _lineComposite;
    private Vector2i _lineTargetSize;
    private static readonly SDL.BlendMode LineCoverageAdd = SDL.ComposeCustomBlendMode(SDL.BlendFactor.One, SDL.BlendFactor.One, SDL.BlendOperation.Add, SDL.BlendFactor.One, SDL.BlendFactor.One, SDL.BlendOperation.Add);
    private static readonly SDL.BlendMode LineCoverageSubtract = SDL.ComposeCustomBlendMode(SDL.BlendFactor.One, SDL.BlendFactor.One, SDL.BlendOperation.RevSubtract, SDL.BlendFactor.One, SDL.BlendFactor.One, SDL.BlendOperation.RevSubtract);
    private static readonly SDL.BlendMode LineCoverageMultiply = SDL.ComposeCustomBlendMode(SDL.BlendFactor.DstColor, SDL.BlendFactor.Zero, SDL.BlendOperation.Add, SDL.BlendFactor.DstAlpha, SDL.BlendFactor.Zero, SDL.BlendOperation.Add);

    private void PrepareNativeLineTargets(nint renderer, Vector2i size)
    {
        if (_lineCoverage is not null && _lineTargetSize.X >= size.X && _lineTargetSize.Y >= size.Y) return;
        var capacity = new Vector2i(Math.Max(size.X, _lineTargetSize.X), Math.Max(size.Y, _lineTargetSize.Y));
        var format = BitConverter.IsLittleEndian ? SDL.PixelFormat.ABGR8888 : SDL.PixelFormat.RGBA8888;
        var coverage = new RenderHandle(SDL.CreateTexture(renderer, format, SDL.TextureAccess.Target, capacity.X, capacity.Y), SDL.DestroyTexture, _renderer, _textureMemory, checked((long)capacity.X * capacity.Y * 4));
        RenderHandle? composite = null;
        try
        {
            composite = new RenderHandle(SDL.CreateTexture(renderer, format, SDL.TextureAccess.Target, capacity.X, capacity.Y), SDL.DestroyTexture, _renderer, _textureMemory, checked((long)capacity.X * capacity.Y * 4));
            Check(SDL.SetTextureBlendMode(coverage.DangerousGetHandle(), SDL.BlendMode.None), "set native line coverage copy");
            Check(SDL.SetTextureColorModFloat(coverage.DangerousGetHandle(), 255f / 32, 255f / 32, 255f / 32) && SDL.SetTextureAlphaModFloat(coverage.DangerousGetHandle(), 255f / 32), "normalize native line coverage");
            Check(SDL.SetTextureScaleMode(coverage.DangerousGetHandle(), SDL.ScaleMode.Nearest) && SDL.SetTextureScaleMode(composite.DangerousGetHandle(), SDL.ScaleMode.Nearest), "set native line target sampling");
        }
        catch { composite?.Dispose(); coverage.Dispose(); throw; }
        _lineCoverage?.Dispose(); _lineComposite?.Dispose();
        _lineCoverage = coverage; _lineComposite = composite; _lineTargetSize = capacity;
    }

    private int DrawMaskedNativeLines(nint renderer, nint texture, ReadOnlySpan<SDL.Vertex> vertices, Mesh.PrimitiveType primitive, bool snap)
    {
        Check(SDL.GetRenderViewport(renderer, out var viewport), "read native line viewport");
        Check(SDL.GetRenderClipRect(renderer, out var clip), "read native line clip");
        var clipped = SDL.RenderClipEnabled(renderer); var target = SDL.GetRenderTarget(renderer);
        var area = new SDL.Rect { W = viewport.W, H = viewport.H }; var bounds = area;
        if (clipped && !SDL.GetRectIntersection(area, clip, out bounds)) return 0;
        Check(SDL.GetRenderDrawBlendMode(renderer, out var drawBlend), "read native line draw blend");
        var blend = drawBlend;
        if (texture != 0) Check(SDL.GetTextureBlendMode(texture, out blend), "read native line texture blend");
        Check(SDL.GetRenderDrawColorFloat(renderer, out var red, out var green, out var blue, out var alpha), "read native line draw color");
        Check(SDL.GetRenderTextureAddressMode(renderer, out var addressU, out var addressV), "read native line texture addressing");
        PrepareNativeLineTargets(renderer, new(viewport.W, viewport.H));
        var coverage = _lineCoverage!.DangerousGetHandle(); var composite = _lineComposite!.DangerousGetHandle();
        var connected = primitive == Mesh.PrimitiveType.LineStrip; var last = vertices.Length - 1; var calls = 0;
        if (connected) while (last > 0 && EqualPoint(LinePoint(vertices[last - 1].Position, snap), LinePoint(vertices[last].Position, snap))) last--;
        Span<SDL.FPoint> path = stackalloc SDL.FPoint[3]; Span<SDL.Vertex> geometry = stackalloc SDL.Vertex[36];
        var region = new SDL.FRect { W = viewport.W, H = viewport.H };
        try
        {
            for (var i = 1; i <= last; i += connected ? 1 : 2)
            {
                var a = vertices[i - 1]; var b = vertices[i]; a.Position = LinePoint(a.Position, snap); b.Position = LinePoint(b.Position, snap);
                if (EqualPoint(a.Position, b.Position)) continue;
                var end = !connected || i == last && !EqualPoint(b.Position, LinePoint(vertices[0].Position, snap));
                Check(SDL.SetRenderTarget(renderer, coverage) && SDL.SetRenderViewport(renderer, viewport) && SDL.SetRenderClipRect(renderer, 0), "bind native line coverage");
                Check(SDL.SetRenderDrawColorFloat(renderer, 0, 0, 0, 0) && SDL.RenderClear(renderer), "clear native line coverage");
                if (clipped) Check(SDL.SetRenderClipRect(renderer, clip), "clip native line coverage");
                Check(SDL.SetRenderDrawColorFloat(renderer, 32f / 255, 32f / 255, 32f / 255, 32f / 255) && SDL.SetRenderDrawBlendMode(renderer, LineCoverageAdd), "weight native line coverage");
                path[0] = a.Position; path[1] = b.Position;
                if (!end)
                {
                    path[2] = new() { X = viewport.W * 4f + 100_003, Y = viewport.H * 4f + 131_071 };
                    if (EqualPoint(path[2], a.Position) || EqualPoint(path[2], b.Position)) path[2].X = -path[2].X;
                }
                Check(SDL.RenderLines(renderer, path, end ? 2 : 3), "draw native attributed coverage"); calls++;
                if (!end)
                {
                    Check(SDL.SetRenderDrawBlendMode(renderer, LineCoverageSubtract), "subtract native continuation");
                    Check(SDL.RenderLines(renderer, path[1..], 2), "retain native segment endpoint ownership"); calls++;
                }
                Check(SDL.SetRenderTarget(renderer, composite) && SDL.SetRenderViewport(renderer, viewport) && SDL.SetRenderClipRect(renderer, 0), "bind native line attributes");
                Check(SDL.SetRenderDrawColorFloat(renderer, 0, 0, 0, 0) && SDL.RenderClear(renderer), "clear native line attributes");
                Check(SDL.SetRenderTextureAddressMode(renderer, SDL.TextureAddressMode.Clamp, SDL.TextureAddressMode.Clamp), "clamp native line coverage sampling");
                Check(SDL.SetTextureBlendMode(coverage, SDL.BlendMode.None) && SDL.SetTextureColorModFloat(coverage, 255f / 32, 255f / 32, 255f / 32) && SDL.SetTextureAlphaModFloat(coverage, 255f / 32), "normalize native line coverage sampling");
                Check(SDL.RenderTexture(renderer, coverage, in region, in region), "normalize native line ownership");
                if (blend == SDL.BlendMode.None || blend == LineCoverageMultiply)
                {
                    Check(SDL.SetRenderTarget(renderer, coverage) && SDL.SetRenderClipRect(renderer, 0), "bind inverse native line ownership");
                    Check(SDL.SetRenderDrawColorFloat(renderer, 1, 1, 1, 1) && SDL.RenderClear(renderer), "clear inverse native line ownership");
                    Check(SDL.SetTextureBlendMode(composite, LineCoverageSubtract), "subtract normalized native line ownership");
                    Check(SDL.RenderTexture(renderer, composite, in region, in region), "retain uncovered native line destination");
                    Check(SDL.SetTextureColorModFloat(coverage, 1, 1, 1) && SDL.SetTextureAlphaModFloat(coverage, 1), "sample inverse native line ownership");
                    Check(SDL.SetRenderTarget(renderer, composite), "restore native line attribute target");
                }
                var geometryCount = NativeLineGradient(a, b, bounds, geometry);
                Check(SDL.SetRenderTextureAddressMode(renderer, addressU, addressV), "restore native line source addressing");
                if (texture == 0) Check(SDL.SetRenderDrawBlendMode(renderer, LineCoverageMultiply), "mask native line vertex colors");
                else Check(SDL.SetTextureBlendMode(texture, LineCoverageMultiply), "mask native line texture");
                Check(SDL.RenderGeometry(renderer, texture, geometry[..geometryCount], geometryCount, 0, 0), "apply native line attributes"); calls++;
                Check(SDL.SetRenderTextureAddressMode(renderer, SDL.TextureAddressMode.Clamp, SDL.TextureAddressMode.Clamp), "clamp native line composite sampling");
                if (blend == LineCoverageMultiply)
                {
                    Check(SDL.SetTextureBlendMode(coverage, LineCoverageAdd), "retain multiplicative native line background");
                    Check(SDL.RenderTexture(renderer, coverage, in region, in region), "add neutral uncovered native line pixels");
                }
                Check(SDL.SetRenderTarget(renderer, target), "restore native line destination");
                if (clipped) Check(SDL.SetRenderClipRect(renderer, clip), "restore native line destination clip");
                else Check(SDL.SetRenderClipRect(renderer, 0), "restore unclipped native line destination");
                if (blend == SDL.BlendMode.None)
                {
                    Check(SDL.SetTextureBlendMode(coverage, LineCoverageMultiply), "retain uncovered replacement destination");
                    Check(SDL.RenderTexture(renderer, coverage, in region, in region), "clear covered replacement destination");
                }
                Check(SDL.SetTextureBlendMode(composite, blend == SDL.BlendMode.None ? LineCoverageAdd : blend), "set native line composite blending");
                Check(SDL.RenderTexture(renderer, composite, in region, in region), "composite native attributed line");
            }
        }
        finally
        {
            try
            {
                if (texture != 0) Check(SDL.SetTextureBlendMode(texture, blend), "restore native line texture blend");
                Check(SDL.SetRenderDrawBlendMode(renderer, drawBlend) && SDL.SetRenderDrawColorFloat(renderer, red, green, blue, alpha), "restore native line draw state");
                Check(SDL.SetRenderTextureAddressMode(renderer, addressU, addressV), "restore native line addressing");
            }
            finally
            {
                try { Check(SDL.SetRenderTarget(renderer, target), "restore native line render target"); }
                finally
                {
                    if (clipped) Check(SDL.SetRenderClipRect(renderer, clip), "restore native line clipping");
                    else Check(SDL.SetRenderClipRect(renderer, 0), "restore native line clipping");
                }
            }
        }
        return calls;
    }

    private static int NativeLineGradient(SDL.Vertex a, SDL.Vertex b, SDL.Rect bounds, Span<SDL.Vertex> output)
    {
        var dx = (double)b.Position.X - a.Position.X; var dy = (double)b.Position.Y - a.Position.Y; var lengthSquared = dx * dx + dy * dy;
        Span<(Vector2 Point, double Weight)> rectangle = stackalloc (Vector2, double)[4];
        rectangle[0].Point = new(bounds.X, bounds.Y); rectangle[1].Point = new(bounds.X + bounds.W, bounds.Y);
        rectangle[2].Point = new(bounds.X + bounds.W, bounds.Y + bounds.H); rectangle[3].Point = new(bounds.X, bounds.Y + bounds.H);
        var minimum = double.PositiveInfinity; var maximum = double.NegativeInfinity;
        for (var i = 0; i < rectangle.Length; i++)
        {
            rectangle[i].Weight = (((double)rectangle[i].Point.X - .5 - a.Position.X) * dx + ((double)rectangle[i].Point.Y - .5 - a.Position.Y) * dy) / lengthSquared;
            minimum = Math.Min(minimum, rectangle[i].Weight); maximum = Math.Max(maximum, rectangle[i].Weight);
        }
        Span<(Vector2 Point, double Weight)> polygon = stackalloc (Vector2, double)[8], scratch = stackalloc (Vector2, double)[8];
        var written = 0;
        // Split the bounded rectangle at endpoint planes so clamping cannot distort interior attributes.
        for (var section = 0; section < 3; section++)
        {
            if (section == 0 && minimum >= 0 || section == 1 && (maximum < 0 || minimum > 1) || section == 2 && maximum <= 1) continue;
            rectangle.CopyTo(polygon); var count = 4;
            if (section != 0) { count = ClipLineGradient(polygon[..count], scratch, section == 1 ? 0 : 1, true); scratch[..count].CopyTo(polygon); }
            if (section != 2) { count = ClipLineGradient(polygon[..count], scratch, section == 0 ? 0 : 1, false); scratch[..count].CopyTo(polygon); }
            for (var i = 1; i + 1 < count; i++)
                for (var corner = 0; corner < 3; corner++)
                {
                    var index = corner == 0 ? 0 : corner == 1 ? i : i + 1;
                    var point = polygon[index]; var weight = Math.Clamp(point.Weight, 0, 1);
                    output[written++] = new()
                    {
                        Position = new() { X = point.Point.X, Y = point.Point.Y },
                        Color = new() { R = AttributeLerp(a.Color.R, b.Color.R, weight), G = AttributeLerp(a.Color.G, b.Color.G, weight), B = AttributeLerp(a.Color.B, b.Color.B, weight), A = AttributeLerp(a.Color.A, b.Color.A, weight) },
                        TexCoord = new() { X = AttributeLerp(a.TexCoord.X, b.TexCoord.X, weight), Y = AttributeLerp(a.TexCoord.Y, b.TexCoord.Y, weight) }
                    };
                }
        }
        return written;
    }

    private static int ClipLineGradient(ReadOnlySpan<(Vector2 Point, double Weight)> input, Span<(Vector2 Point, double Weight)> output, double boundary, bool above)
    {
        var written = 0;
        for (var i = 0; i < input.Length; i++)
        {
            var a = input[i == 0 ? input.Length - 1 : i - 1]; var b = input[i];
            var aInside = above ? a.Weight >= boundary : a.Weight <= boundary; var bInside = above ? b.Weight >= boundary : b.Weight <= boundary;
            if (aInside != bInside)
            {
                var t = (boundary - a.Weight) / (b.Weight - a.Weight);
                output[written++] = (new((float)(a.Point.X + (b.Point.X - (double)a.Point.X) * t), (float)(a.Point.Y + (b.Point.Y - (double)a.Point.Y) * t)), boundary);
            }
            if (bInside) output[written++] = b;
        }
        return written;
    }
}
