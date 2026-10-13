using Electron2D;
using FileAccess = Electron2D.FileAccess;
using IOPath = System.IO.Path;

internal static partial class FileCompressionTests
{
    private sealed class Picture : Entity
    {
        internal required Texture Texture;
        internal float X;
        protected override void OnDraw() => DrawTextureRect(Texture, new(X, 8, 16, 16), false);
    }
    internal static void RunNative(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        using var folder = DirAccess.CreateTemp();
        using var source = Image.CreateFromData(1, 1, false, Image.Format.Rgba8, [255, 0, 0, 255]);
        var png = source.SavePNGToBuffer();
        using var decodedFast = new Image(); using var decodedZstd = new Image();
        foreach (var mode in new[] { FileCompressionMode.FastLz, FileCompressionMode.Zstandard })
        {
            var path = IOPath.Combine(folder.GetCurrentDir(), mode + ".bin");
            using (var file = FileAccess.OpenCompressed(path, FileAccessModeFlags.Write, mode)) file.WriteBytes(png);
            using var read = FileAccess.OpenCompressed(path, FileAccessModeFlags.Read, mode);
            (mode == FileCompressionMode.FastLz ? decodedFast : decodedZstd).LoadPNGFromBuffer(read.ReadBytes(checked((int)read.Length)));
        }
        using var fast = ImageTexture.CreateFromImage(decodedFast); using var zstd = ImageTexture.CreateFromImage(decodedZstd);
        var window = new Window { Size = new(64, 48) };
        window.AddChild(new Picture { Name = "Fast", Texture = fast, X = 8 }); window.AddChild(new Picture { Name = "Zstd", Texture = zstd, X = 32 });
        var rendered = false;
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                using var image = RenderingServer.Service!.Readback();
                foreach (var x in new[] { 12, 36 }) { var pixel = image.GetPixel(x, 12); Check(pixel.R > .95f && pixel.G < .05f && pixel.B < .05f, "Compressed PNG reaches actual native pixels."); }
                rendered = true; window.Tree!.Quit();
            };
        };
        Check(Engine.Run(window) == 0 && rendered && window.IsDisposed && !RenderingServer.IsAvailable, "Compressed texture consumer renders and cleans up.");
        Console.WriteLine("FastLZ/Zstandard files -> PNG -> texture: " + backend + " red pixels and teardown passed.");
    }
}
