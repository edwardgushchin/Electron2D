using System.Text.Json;
using Electron2D;

internal static partial class ImageKTXTests
{
    private sealed class KTXPainter(IReadOnlyList<ImageTexture> textures) : Entity
    {
        protected override void OnDraw()
        {
            for (var i = 0; i < textures.Count; ++i)
                DrawTextureRect(textures[i], new Rect2(4 + (i % 2) * 64, 4 + (i / 2) * 40, 56, 32), false);
        }
    }

    internal static void RunNative(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var stream = typeof(ImageKTXTests).Assembly.GetManifestResourceStream("TestKTX.oracle.json")!;
        using var oracle = JsonDocument.Parse(stream);
        var textures = new List<ImageTexture>();
        try
        {
            foreach (var entry in oracle.RootElement.GetProperty("fixtures").EnumerateArray())
            {
                using var loaded = new Image(); loaded.LoadKTXFromBuffer(entry.GetProperty("encoded").GetBytesFromBase64());
                using var expected = Image.CreateFromData(7, 5, true, Enum.Parse<Image.Format>(entry.GetProperty("format").GetString()!), entry.GetProperty("pixels").GetBytesFromBase64());
                if (backend == "compatibility" && loaded.PixelFormat == Image.Format.Rgbh) { loaded.Convert(Image.Format.Rgba8); expected.Convert(Image.Format.Rgba8); }
                textures.Add(ImageTexture.CreateFromImage(loaded)); textures.Add(ImageTexture.CreateFromImage(expected));
            }
            using var blocks = Source(Image.Format.Rgba8); blocks.Compress(Image.CompressMode.S3tc);
            using var compressed = new Image(); compressed.LoadKTXFromBuffer(KTX1(blocks));
            textures.Add(ImageTexture.CreateFromImage(compressed)); blocks.Decompress(); textures.Add(ImageTexture.CreateFromImage(blocks));
            var window = new Window { Size = new(128, textures.Count / 2 * 40) };
            window.AddChild(new KTXPainter(textures) { TextureFilter = TextureFilter.Nearest }); using var target = window.GetTexture();
            var frames = 0; var verified = false;
            window.Ready += _ => { Check(RenderingServer.GetCurrentRenderingMethod() == backend, "KTX host uses the requested renderer."); RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.FramePostDraw += Capture; };
            void Capture()
            {
                if (++frames < 4) return;
                using var pixels = target.GetImage()!; var visible = 0;
                for (var y = 0; y < window.Size.Y; ++y) for (var x = 0; x < 64; ++x)
                    {
                        var left = pixels.GetPixel(x, y); var right = pixels.GetPixel(x + 64, y);
                        Check(MathF.Abs(left.R - right.R) < .012f && MathF.Abs(left.G - right.G) < .012f && MathF.Abs(left.B - right.B) < .012f,
                            "KTX/Basis sampling matches independent decoded pixels.");
                        if (left.B > .05f) ++visible;
                    }
                Check(visible > 1000, "KTX texture draws visible content."); verified = true;
                RenderingServer.FramePostDraw -= Capture; window.Tree!.Quit();
            }
            Check(Engine.Run(window) == 0 && verified, "Native KTX texture host finishes.");
            Console.WriteLine($"KTX {backend}: 9 raw/Basis LDR/HDR mip profiles and BC source pixel comparisons passed; Compatibility explicitly converts HDR to RGBA8.");
        }
        finally { foreach (var texture in textures) texture.Dispose(); }
    }
}
