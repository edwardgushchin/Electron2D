using System.Runtime.InteropServices;
using System.Text.Json;
using Electron2D;
using SDL3;
using Image = Electron2D.Image;
using static TextureArrayTests;

internal static partial class NativeStrokeTests
{
    private static Image SeparateSDLOracle(string profile, Vector2i size)
    {
        // Native Vulkan device dispatch must stay in a different process from game presentation/physics.
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "electron2d-stroke-oracle-" + Guid.NewGuid().ToString("N") + ".rgba");
        try
        {
            var start = new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false };
            var program = Environment.GetCommandLineArgs()[0];
            if (program.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(program);
            start.ArgumentList.Add(profile); start.Environment["ELECTRON2D_NATIVE_STROKE_ORACLE_OUTPUT"] = path;
            using var child = System.Diagnostics.Process.Start(start)!;
            if (!child.WaitForExit(60_000)) { child.Kill(entireProcessTree: true); child.WaitForExit(); throw new InvalidOperationException("The native SDL oracle exceeded its process deadline."); }
            Check(child.ExitCode == 0, $"The isolated native SDL oracle exited with {child.ExitCode}");
            var data = File.ReadAllBytes(path); Check(data.Length == size.X * size.Y * 4, "The isolated native oracle returns the complete RGBA image");
            return Image.CreateFromData(size.X, size.Y, false, Image.Format.Rgba8, data);
        }
        finally { File.Delete(path); }
    }

    private static void SDLGeometryAttributeOracle(nint renderer, Image result, Case definition, Vector2[] points, SDL.Rect clip)
    {
        using var source = typeof(NativeStrokeTests).Assembly.GetManifestResourceStream("Electron2D.SDLGeometryLineOracle.json")!;
        using var document = JsonDocument.Parse(source);
        var records = document.RootElement.GetProperty("segments").EnumerateArray().ToArray();
        var triangles = new List<SDL.Vertex>();
        SDL.Vertex Vertex(int point, int corner)
        {
            var position = points[point] + new Vector2(corner is 1 or 2 ? 1 : 0, corner is 2 or 3 ? 1 : 0);
            var tint = definition.Colors[point < definition.Colors.Length ? point : 0]; var uv = definition.UVs?[point] ?? Vector2.Zero;
            return new() { Position = new() { X = position.X, Y = position.Y }, Color = new() { R = tint.R, G = tint.G, B = tint.B, A = tint.A }, TexCoord = new() { X = uv.X, Y = uv.Y } };
        }
        void Point(int point)
        {
            foreach (var index in document.RootElement.GetProperty("pointIndices").EnumerateArray()) triangles.Add(Vertex(point, index.GetInt32()));
        }
        if (definition.Connected && points[0] != points[^1]) Point(0);
        for (var i = 1; i < points.Length; i += definition.Connected ? 1 : 2)
        {
            if (points[i - 1] == points[i]) continue;
            if (!definition.Connected) Point(i - 1);
            Point(i);
            var direction = points[i] - points[i - 1];
            var record = records.Single(row => row.GetProperty("direction")[0].GetInt32() == Math.Sign(direction.X) && row.GetProperty("direction")[1].GetInt32() == Math.Sign(direction.Y));
            foreach (var index in record.GetProperty("indices").EnumerateArray())
            {
                var value = index.GetInt32(); triangles.Add(Vertex(value < 4 ? i - 1 : i, value % 4));
            }
        }
        var geometry = triangles.ToArray(); var white = geometry.ToArray();
        foreach (ref var vertex in white.AsSpan()) vertex.Color = new() { R = 32f / 255, G = 32f / 255, B = 32f / 255, A = 1 };
        void Clear(float alpha)
        {
            Check(SDL.SetRenderDrawColorFloat(renderer, 0, 0, 0, alpha) && SDL.RenderClear(renderer), "Clear geometry oracle");
        }
        Check(SDL.SetRenderDrawBlendMode(renderer, SDL.BlendMode.Add), "Set native geometry coverage weights"); Clear(0);
        Check(SDL.SetRenderDrawColorFloat(renderer, 32f / 255, 32f / 255, 32f / 255, 1), "Set native geometry coverage color");
        if (definition.Connected)
        {
            var path = points.Where((point, index) => index == 0 || point != points[index - 1]).Select(point => new SDL.FPoint { X = point.X, Y = point.Y }).ToArray();
            if (path.Length > 1) Check(SDL.RenderLines(renderer, path, path.Length), "Draw actual SDL geometry path");
        }
        else
            for (var i = 1; i < points.Length; i += 2)
                if (points[i - 1] != points[i]) Check(SDL.RenderLines(renderer, [new() { X = points[i - 1].X, Y = points[i - 1].Y }, new() { X = points[i].X, Y = points[i].Y }], 2), "Draw actual SDL geometry pair");
        var native = ReadSDLGeometryCell(renderer, clip); Clear(0);
        if (white.Length != 0) Check(SDL.RenderGeometry(renderer, 0, white, white.Length, 0, 0), "Draw captured native geometry trace");
        var captured = ReadSDLGeometryCell(renderer, clip);
        for (var i = 0; i < native.Length; i += 4) Check(Math.Abs(native[i] - captured[i]) <= 1, $"Captured SDL geometry retains actual native coverage at {i / 4 % 64},{i / 4 / 64}");
        using var sampled = definition.SampleTexture?.GetImage();
        using var texture = sampled is null ? null : new RenderHandle(SDL.CreateTexture(renderer, BitConverter.IsLittleEndian ? SDL.PixelFormat.ABGR8888 : SDL.PixelFormat.RGBA8888, SDL.TextureAccess.Static, sampled.Width, sampled.Height), SDL.DestroyTexture);
        if (texture is not null)
            Check(SDL.UpdateTexture(texture.DangerousGetHandle(), 0, sampled!.GetData(), sampled.Width * 4) && SDL.SetTextureBlendMode(texture.DangerousGetHandle(), SDL.BlendMode.Blend) && SDL.SetTextureScaleMode(texture.DangerousGetHandle(), SDL.ScaleMode.Nearest), "Prepare independent geometry texture");
        Clear(1); Check(SDL.SetRenderDrawBlendMode(renderer, SDL.BlendMode.Blend), "Set attributed geometry oracle blend");
        if (geometry.Length != 0) Check(SDL.RenderGeometry(renderer, texture?.DangerousGetHandle() ?? 0, geometry, geometry.Length, 0, 0), "Draw captured attributed geometry");
        var pixels = ReadSDLGeometryCell(renderer, clip);
        for (var y = 0; y < clip.H; y++) for (var x = 0; x < clip.W; x++)
            {
                var index = (y * clip.W + x) * 4;
                result.SetPixel(clip.X + x, clip.Y + y, new(pixels[index] / 255f, pixels[index + 1] / 255f, pixels[index + 2] / 255f, pixels[index + 3] / 255f));
            }
    }

    private static byte[] ReadSDLGeometryCell(nint renderer, SDL.Rect clip)
    {
        using var surface = new RenderHandle(SDL.RenderReadPixels(renderer, clip), SDL.DestroySurface);
        using var rgba = new RenderHandle(SDL.ConvertSurface(surface.DangerousGetHandle(), BitConverter.IsLittleEndian ? SDL.PixelFormat.ABGR8888 : SDL.PixelFormat.RGBA8888), SDL.DestroySurface);
        var data = Marshal.PtrToStructure<SDL.Surface>(rgba.DangerousGetHandle()); var pixels = new byte[clip.W * clip.H * 4];
        for (var y = 0; y < clip.H; y++) Marshal.Copy(data.Pixels + y * data.Pitch, pixels, y * clip.W * 4, clip.W * 4);
        return pixels;
    }
}
