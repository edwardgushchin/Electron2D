using Electron2D;
using IOPath = System.IO.Path;

internal static partial class ImageLoaderExtensionTests
{
    private sealed class Picture : Entity
    {
        internal required Texture Texture;
        protected override void OnDraw() => DrawTextureRect(Texture, new(8, 8, 16, 16), false);
    }
    internal static void RunNative(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        using var folder = DirAccess.CreateTemp("image-decoder-native-");
        var path = IOPath.Combine(folder.GetCurrentDir(), "red.e2dpix");
        System.IO.File.WriteAllBytes(path, [255, 0, 0, 255]);
        using var decoder = new Decoder(); decoder.AddFormatLoader();
        using var texture = ResourceLoader.Load<ImageTexture>(path, ResourceLoader.CacheMode.Ignore);
        var window = new Window { Size = new(64, 48) }; window.AddChild(new Picture { Texture = texture });
        var rendered = false;
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                using var image = RenderingServer.Service!.Readback();
                var pixel = image.GetPixel(12, 12);
                Check(pixel.R > .95f && pixel.G < .05f && pixel.B < .05f, "Custom decoded texture samples actual native red pixels.");
                rendered = true; window.Tree!.Quit();
            };
        };
        Check(Engine.Run(window) == 0 && rendered && window.IsDisposed && !RenderingServer.IsAvailable, "Native image-decoder consumer renders and cleans up.");
        Console.WriteLine("Custom image decoder native " + backend + " pixels and teardown passed.");
    }
}
