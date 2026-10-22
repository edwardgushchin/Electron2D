using System.Runtime.InteropServices;
using Electron2D;
using SDL3;
using Image = Electron2D.Image;
using static TextureArrayTests;

internal static partial class NativeStrokeTests
{
    private static bool FlatSDLColor(Case definition) => !definition.VertexColors &&
        (!definition.Connected || definition.Colors.All(color => color == definition.Colors[0]));

    private static Image SDLOracle(List<(Painter Painter, Case Definition, Vector2i Origin)> subjects, Vector2i size, bool snap)
    {
        var previousWindow = SDL.GLGetCurrentWindow(); var previousContext = SDL.GLGetCurrentContext();
        try
        {
            using var window = new RenderHandle(SDL.CreateWindow("Primitive oracle", size.X, size.Y, SDL.WindowFlags.Hidden), SDL.DestroyWindow);
            using var renderer = new RenderHandle(SDL.CreateRenderer(window.DangerousGetHandle(), RenderingServer.GetCurrentRenderingDriverName()), SDL.DestroyRenderer, window);
            var handle = renderer.DangerousGetHandle();
            using var target = new RenderHandle(SDL.CreateTexture(handle, BitConverter.IsLittleEndian ? SDL.PixelFormat.ABGR8888 : SDL.PixelFormat.RGBA8888, SDL.TextureAccess.Target, size.X, size.Y), SDL.DestroyTexture, renderer);
            Check(SDL.SetRenderTarget(handle, target.DangerousGetHandle()), "Bind independent SDL texture target");
            Check(SDL.SetRenderDrawColorFloat(handle, 0, 0, 0, 1) && SDL.RenderClear(handle), "Clear independent SDL oracle");
            Check(SDL.SetRenderDrawBlendMode(handle, SDL.BlendMode.Blend), "Set independent SDL oracle blending");
            foreach (var (painter, definition, origin) in subjects)
            {
                if (!FlatSDLColor(definition)) continue;
                Check(SDL.SetRenderClipRect(handle, new SDL.Rect { X = origin.X, Y = origin.Y, W = 64, H = 32 }), "Clip independent SDL primitive");
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
            return Image.CreateFromData(size.X, size.Y, false, Image.Format.Rgba8, pixels);
        }
        finally
        {
            if (previousContext != 0) Check(SDL.GLMakeCurrent(previousWindow, previousContext), "Restore SDL oracle context");
        }
    }
}
