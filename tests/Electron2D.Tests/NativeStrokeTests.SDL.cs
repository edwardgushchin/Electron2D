using System.Runtime.InteropServices;
using Electron2D;
using SDL3;
using Image = Electron2D.Image;
using static TextureArrayTests;

internal static partial class NativeStrokeTests
{
    private static bool FlatSDLColor(Case definition) => !definition.VertexColors &&
        (!definition.Connected || definition.Colors.All(color => color == definition.Colors[0]));

    private static Image SDLOracle(List<(Painter Painter, Case Definition, Vector2i Origin)> subjects, Vector2i size, bool snap, string? selectedDriver = null)
    {
        var driver = selectedDriver ?? RenderingServer.GetCurrentRenderingDriverName();
        var previousWindow = SDL.GLGetCurrentWindow(); var previousContext = SDL.GLGetCurrentContext();
        try
        {
            using var window = new RenderHandle(SDL.CreateWindow("Primitive oracle", size.X, size.Y, SDL.WindowFlags.Hidden), SDL.DestroyWindow);
            using var renderer = new RenderHandle(SDL.CreateRenderer(window.DangerousGetHandle(), driver), SDL.DestroyRenderer, window);
            var handle = renderer.DangerousGetHandle();
            using var target = new RenderHandle(SDL.CreateTexture(handle, BitConverter.IsLittleEndian ? SDL.PixelFormat.ABGR8888 : SDL.PixelFormat.RGBA8888, SDL.TextureAccess.Target, size.X, size.Y), SDL.DestroyTexture, renderer);
            Check(SDL.SetRenderTarget(handle, target.DangerousGetHandle()), "Bind independent SDL texture target");
            Check(SDL.SetRenderDrawColorFloat(handle, 0, 0, 0, 1) && SDL.RenderClear(handle), "Clear independent SDL oracle");
            Check(SDL.SetRenderDrawBlendMode(handle, SDL.BlendMode.Blend), "Set independent SDL oracle blending");
            foreach (var (painter, definition, origin) in subjects)
            {
                Check(SDL.SetRenderClipRect(handle, new SDL.Rect { X = origin.X, Y = origin.Y, W = 64, H = 32 }), "Clip independent SDL primitive");
                if (definition.Background is { } backdrop)
                    Check(SDL.SetRenderDrawColorFloat(handle, backdrop.R, backdrop.G, backdrop.B, backdrop.A) && SDL.RenderFillRect(handle, new SDL.FRect { X = origin.X, Y = origin.Y, W = 64, H = 32 }), "Draw independent native line background");
                if (!FlatSDLColor(definition)) continue;
                var transform = painter.GetGlobalTransform(); var points = new SDL.FPoint[definition.Points.Length];
                for (var i = 0; i < points.Length; i++)
                {
                    var point = transform * definition.Points[i];
                    if (snap) point = (point + new Vector2(.5f, .5f)).Floor();
                    points[i] = new() { X = point.X, Y = point.Y };
                }
                var tint = definition.Colors[0];
                Check(SDL.SetRenderDrawColorFloat(handle, tint.R, tint.G, tint.B, tint.A), "Tint independent SDL primitive");
                if (points.Length == 1) Check(SDL.RenderPoints(handle, points, 1), "Draw independent SDL point");
                else if (definition.Connected)
                {
                    var unique = points.Where((point, index) => index == 0 || point.X != points[index - 1].X || point.Y != points[index - 1].Y).ToArray();
                    if (unique.Length > 1) Check(SDL.RenderLines(handle, unique, unique.Length), "Draw independent SDL strip");
                }
                else
                    for (var i = 0; i < points.Length; i += 2)
                    {
                        if (points[i].X == points[i + 1].X && points[i].Y == points[i + 1].Y) continue;
                        tint = definition.Colors[Math.Min(definition.Colors.Length - 1, i / 2)];
                        Check(SDL.SetRenderDrawColorFloat(handle, tint.R, tint.G, tint.B, tint.A) &&
                            SDL.RenderLines(handle, points.AsSpan(i, 2), 2), "Draw independent SDL segment");
                    }
            }
            Check(SDL.SetRenderClipRect(handle, 0), "Clear independent SDL clipping");
            using var surface = new RenderHandle(SDL.RenderReadPixels(handle, null), SDL.DestroySurface);
            using var rgba = new RenderHandle(SDL.ConvertSurface(surface.DangerousGetHandle(), BitConverter.IsLittleEndian ? SDL.PixelFormat.ABGR8888 : SDL.PixelFormat.RGBA8888), SDL.DestroySurface);
            var data = Marshal.PtrToStructure<SDL.Surface>(rgba.DangerousGetHandle()); var pixels = new byte[checked(size.X * size.Y * 4)];
            for (var y = 0; y < size.Y; y++) Marshal.Copy(data.Pixels + y * data.Pitch, pixels, y * size.X * 4, size.X * 4);
            var result = Image.CreateFromData(size.X, size.Y, false, Image.Format.Rgba8, pixels);
            using var mask = new RenderHandle(SDL.CreateTexture(handle, BitConverter.IsLittleEndian ? SDL.PixelFormat.ABGR8888 : SDL.PixelFormat.RGBA8888, SDL.TextureAccess.Target, size.X, size.Y), SDL.DestroyTexture, renderer);
            Check(SDL.SetRenderTarget(handle, mask.DangerousGetHandle()), "Bind independent attribute coverage");
            foreach (var (painter, definition, origin) in subjects)
                if (!FlatSDLColor(definition)) SDLAttributeOracle(handle, result, painter, definition, origin, snap, driver);
            return result;
        }
        finally
        {
            if (previousContext != 0) Check(SDL.GLMakeCurrent(previousWindow, previousContext), "Restore SDL oracle context");
        }
    }

    private static void SDLAttributeOracle(nint renderer, Image result, Painter painter, Case definition, Vector2i origin, bool snap, string driver)
    {
        var clip = new SDL.Rect { X = origin.X, Y = origin.Y, W = 64, H = 32 };
        Check(SDL.SetRenderClipRect(renderer, clip), "Clip native attribute coverage");
        var points = definition.Points.Select(point => painter.GetGlobalTransform() * point).ToArray();
        if (snap) for (var i = 0; i < points.Length; i++) points[i] = (points[i] + new Vector2(.5f, .5f)).Floor();
        if (SDL.GetHint(SDL.Hints.RenderLineMethod) == "3" && driver != "software")
        {
            SDLGeometryAttributeOracle(renderer, result, definition, points, clip);
            return;
        }
        using var sampled = definition.SampleTexture?.GetImage();
        var nativeLines = int.TryParse(SDL.GetHint(SDL.Hints.RenderLineMethod), out var method) && method == 2 && driver is "opengl" or "opengles2";
        var rasterPoints = points.ToArray();
        if (nativeLines)
            for (var i = 0; i < rasterPoints.Length; i++)
            {
                var point = points[i] + new Vector2(.5f, .5f);
                if (i != 0 && (definition.Connected || i % 2 != 0))
                {
                    if (points[i] == points[i - 1]) point = rasterPoints[i - 1];
                    else { var angle = MathF.Atan2(point.Y - rasterPoints[i - 1].Y, point.X - rasterPoints[i - 1].X); point += new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * .25f; }
                }
                rasterPoints[i] = point;
            }
        var last = points.Length - 1;
        if (definition.Connected) while (last > 0 && points[last - 1] == points[last]) last--;
        for (var i = 1; i <= last; i += definition.Connected ? 1 : 2)
        {
            var a = points[i - 1]; var b = points[i]; if (a == b) continue;
            var drawEnd = !definition.Connected || i == last && b != points[0];
            // A distant tail cannot accidentally close the path after software integer conversion.
            var tail = b + new Vector2(100_003, 131_071);
            byte[] Coverage(bool subtract)
            {
                Check(SDL.SetRenderDrawColorFloat(renderer, 0, 0, 0, 0) && SDL.RenderClear(renderer), "Clear native attribute coverage");
                Check(SDL.SetRenderDrawBlendMode(renderer, SDL.BlendMode.Add) && SDL.SetRenderDrawColorFloat(renderer, 32f / 255, 32f / 255, 32f / 255, 1), "Set independent coverage weight");
                SDL.FPoint P(Vector2 p) => new() { X = p.X, Y = p.Y };
                SDL.FPoint[] path;
                if (nativeLines)
                {
                    // Prefix subtraction preserves each preceding quarter-pixel endpoint displacement.
                    var prefix = definition.Connected ? points[..(i + (subtract ? 0 : 1))] : [a, b];
                    path = prefix.Where((point, index) => index == 0 || point != prefix[index - 1]).Select(P).ToArray();
                }
                else path = subtract ? [P(b), P(tail)] : drawEnd ? [P(a), P(b)] : [P(a), P(b), P(tail)];
                Check(SDL.RenderLines(renderer, path, path.Length), "Rasterize native attribute coverage");
                using var surface = new RenderHandle(SDL.RenderReadPixels(renderer, clip), SDL.DestroySurface);
                using var rgba = new RenderHandle(SDL.ConvertSurface(surface.DangerousGetHandle(), BitConverter.IsLittleEndian ? SDL.PixelFormat.ABGR8888 : SDL.PixelFormat.RGBA8888), SDL.DestroySurface);
                var data = Marshal.PtrToStructure<SDL.Surface>(rgba.DangerousGetHandle()); var values = new byte[64 * 32];
                for (var y = 0; y < 32; y++) for (var x = 0; x < 64; x++) values[y * 64 + x] = Marshal.ReadByte(data.Pixels + y * data.Pitch + x * 4);
                return values;
            }
            var coverage = Coverage(false); var following = nativeLines ? definition.Connected && i > 1 ? Coverage(true) : null : drawEnd ? null : Coverage(true);
            var ca = definition.Colors[i - 1 < definition.Colors.Length ? i - 1 : 0]; var cb = definition.Colors[i < definition.Colors.Length ? i : 0];
            for (var y = 0; y < 32; y++) for (var x = 0; x < 64; x++)
                {
                    var alpha = Math.Clamp((coverage[y * 64 + x] - (following?[y * 64 + x] ?? 0)) / 32f, 0, 1); if (alpha == 0) continue;
                    var from = nativeLines ? rasterPoints[i - 1] : a; var to = nativeLines ? rasterPoints[i] : b;
                    var sample = new Vector2(origin.X + x, origin.Y + y) + (nativeLines ? new Vector2(.5f, .5f) : Vector2.Zero);
                    var dx = (double)to.X - from.X; var dy = (double)to.Y - from.Y;
                    // The qualified NVIDIA native-line path interpolates along its major axis.
                    var weight = nativeLines ? Math.Abs(dx) >= Math.Abs(dy) ? (sample.X - from.X) / dx : (sample.Y - from.Y) / dy : ((sample.X - from.X) * dx + (sample.Y - from.Y) * dy) / (dx * dx + dy * dy);
                    var t = (float)Math.Clamp(weight, 0, 1);
                    var color = ca.Lerp(cb, t);
                    if (sampled is not null)
                    {
                        var uv = definition.UVs![i - 1].Lerp(definition.UVs[i], t);
                        color *= SDLOracleSample(sampled, uv, definition.Linear, definition.Repeat);
                    }
                    var destination = result.GetPixel(origin.X + x, origin.Y + y); color.A *= alpha;
                    var blended = definition.Blend switch
                    {
                        Electron2D.BlendMode.Add => destination + color * color.A,
                        Electron2D.BlendMode.Sub => destination - color * color.A,
                        Electron2D.BlendMode.Mul => destination * color,
                        Electron2D.BlendMode.PremultAlpha => color + destination * (1 - color.A),
                        _ => new Color(color.R * color.A + destination.R * (1 - color.A), color.G * color.A + destination.G * (1 - color.A), color.B * color.A + destination.B * (1 - color.A), color.A + destination.A * (1 - color.A))
                    };
                    result.SetPixel(origin.X + x, origin.Y + y, blended);
                }
        }
    }

    private static Color SDLOracleSample(Image image, Vector2 uv, bool linear, bool repeat)
    {
        Color Pixel(int x, int y) => image.GetPixel(repeat ? (x % image.Width + image.Width) % image.Width : Math.Clamp(x, 0, image.Width - 1), repeat ? (y % image.Height + image.Height) % image.Height : Math.Clamp(y, 0, image.Height - 1));
        if (!linear) return Pixel((int)MathF.Floor(uv.X * image.Width), (int)MathF.Floor(uv.Y * image.Height));
        var x = uv.X * image.Width - .5f; var y = uv.Y * image.Height - .5f; var ix = (int)MathF.Floor(x); var iy = (int)MathF.Floor(y);
        return Pixel(ix, iy).Lerp(Pixel(ix + 1, iy), x - ix).Lerp(Pixel(ix, iy + 1).Lerp(Pixel(ix + 1, iy + 1), x - ix), y - iy);
    }

    private static SDL.BlendMode SDLOracleBlend(Electron2D.BlendMode mode) => mode switch
    {
        Electron2D.BlendMode.Mix => SDL.BlendMode.Blend,
        Electron2D.BlendMode.PremultAlpha => SDL.BlendMode.BlendPremultiplied,
        Electron2D.BlendMode.Mul => SDL.ComposeCustomBlendMode(SDL.BlendFactor.DstColor, SDL.BlendFactor.Zero, SDL.BlendOperation.Add, SDL.BlendFactor.DstAlpha, SDL.BlendFactor.Zero, SDL.BlendOperation.Add),
        _ => SDL.ComposeCustomBlendMode(SDL.BlendFactor.SrcAlpha, SDL.BlendFactor.One, mode == Electron2D.BlendMode.Sub ? SDL.BlendOperation.RevSubtract : SDL.BlendOperation.Add, SDL.BlendFactor.SrcAlpha, SDL.BlendFactor.One, mode == Electron2D.BlendMode.Sub ? SDL.BlendOperation.RevSubtract : SDL.BlendOperation.Add)
    };
}
