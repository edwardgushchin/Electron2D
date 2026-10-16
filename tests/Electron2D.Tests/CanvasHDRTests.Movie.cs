using Electron2D;
using static TextureArrayTests;

internal static partial class CanvasHDRTests
{
    private sealed class HDRWriter(Window window, string folder) : MovieWriter
    {
        internal int Frames, Ends;
        internal Image? Borrowed;
        protected override string[] OnGetSupportedExtensions() => ["hdrprobe"];
        protected override bool OnHandlesFile(string path) => path.EndsWith(".hdrprobe", StringComparison.Ordinal);
        protected override void OnWriteBegin(Vector2i size, int fps, string path) => Check(size == new Vector2i(128, 64) && fps == 60, "HDR movie preparation.");
        protected override void OnWriteFrame(Image image, ReadOnlySpan<int> pcm)
        {
            Check(Borrowed is null || Borrowed.IsDisposed, "Previous HDR movie frame retires before the next callback."); Borrowed = image;
            Check(image.PixelFormat == Image.Format.Rgb8 && pcm.Length == 1600, "HDR movie keeps the RGB8 and rational PCM contract.");
            foreach (var value in pcm) Check(value == 0, "HDR capture retains silence from the empty audio graph.");
            using var native = window.GetTexture().GetImage()!;
            float[] source = [.0001f, .001f, .003f, .214f, 2.5f, -.1f];
            for (var i = 0; i < source.Length; ++i)
            {
                var raw = native.GetPixel(i * 16 + 4, 4).R;
                Check(MathF.Abs(raw - (window.UseHDR2D ? source[i] : Math.Clamp(source[i], 0, 1))) < .004f, "Actual movie target preserves its selected precision.");
                var expected = window.UseHDR2D ? Encode(raw) : raw;
                expected = (int)(Math.Clamp(expected, 0, 1) * 255f) / 255f;
                var actual = image.GetPixel(i * 16 + 4, 4).R;
                Check(MathF.Abs(actual - expected) < .0001f, $"HDR movie frame {Frames}, native {native.Size}/{native.PixelFormat}/{native.GetPixel(i * 16 + 4, 4).R}, transfer before quantization: {source[i]} became {actual}, expected {expected}.");
            }
            image.SavePNG(System.IO.Path.Combine(folder, $"frame-{Frames}.png"));
            if (++Frames == 4) window.Tree!.Quit(); else window.UseHDR2D = !window.UseHDR2D;
        }
        protected override void OnWriteEnd() => Ends++;
    }
    private static float Encode(float value) => value <= .0031308f ? value * 12.92f : 1.055f * MathF.Pow(value, 1 / 2.4f) - .055f;
    internal static void RunMovie()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var image = Image.CreateEmpty(6, 1, false, Image.Format.Rgbaf);
        float[] source = [.0001f, .001f, .003f, .214f, 2.5f, -.1f];
        for (var i = 0; i < source.Length; ++i) image.SetPixel(i, 0, new(source[i], source[i], source[i], 1));
        using var texture = ImageTexture.CreateFromImage(image);
        var root = new Window { Size = new(128, 64), ContentScaleSize = new(128, 64), ContentScaleMode = ContentScaleMode.Viewport, UseHDR2D = true };
        var painter = new Entity { TextureFilter = TextureFilter.Nearest }; painter.Draw += item => item.DrawTextureRect(texture, new(0, 0, 96, 32), false); root.AddChild(painter);
        var folder = Environment.GetEnvironmentVariable("ELECTRON2D_HDR_OUTPUT") ?? System.IO.Path.Combine(System.IO.Path.GetTempPath(), "electron-hdr-movie"); Directory.CreateDirectory(folder);
        using var writer = new HDRWriter(root, folder); MovieWriter.AddWriter(writer);
        Check(Engine.Run(root, System.IO.Path.Combine(folder, "capture.hdrprobe"), 60) == 0 && writer.Frames == 4 && writer.Ends == 1, "HDR movie executes the public offline host.");
        Check(writer.Borrowed!.IsDisposed && root.IsDisposed && Engine.GetWriteMoviePath() == "", "HDR movie lifetime cleanup.");
        Console.WriteLine("HDR movie: four alternating HDR/SDR frames, dark gradients, clipping, RGB8 before quantization, exact PCM and cleanup passed.");
    }
}
