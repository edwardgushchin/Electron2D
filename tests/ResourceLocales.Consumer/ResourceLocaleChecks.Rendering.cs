using System.Globalization;
using Electron2D;
using IOPath = System.IO.Path;

internal static partial class ResourceLocaleChecks
{
    private static void Author(string root)
    {
        Directory.CreateDirectory(root); ProjectSettings.ConfigurePaths(root, IOPath.Combine(root, "user"));
        Save("res://base.png", Colors.Red); Save("res://en.png", Colors.Green); Save("res://ru.png", Colors.Blue);
        ProjectSettings.Set(ProjectSettings.LocaleResourceRemaps, new ResourceLocaleRemap[] { new("res://base.png", "en-US", "res://en.png"), new("res://base.png", "ru", "res://ru.png") });
        ProjectSettings.Set(ProjectSettings.LocaleTest, "en-US"); ProjectSettings.Save();
        TranslationServer.Culture = CultureInfo.GetCultureInfo("en-US");
        using var texture = ResourceLoader.Load<ImageTexture>("res://base.png");
        using var window = new Window { Name = "LocaleScene", Size = new(64, 32), ContentScaleMode = ContentScaleMode.Viewport, ContentScaleSize = new(64, 32), Title = "Electron2D localized resources" };
        var banner = new Sprite { Name = "Banner", Texture = texture, Position = new(32, 16), Scale = new(8, 4) };
        window.AddChild(banner); banner.Owner = window;
        using var scene = new PackedScene(); scene.Pack(window); ResourceSaver.Save(scene, "res://locale.e2dscene");
        Check(ResourceLoader.GetDependencies("res://locale.e2dscene").Any(path => path.Contains("base.png", StringComparison.Ordinal)), "Authoring preserves the logical external resource dependency.");
        Console.WriteLine("Authored project and scene with a logical localized texture: " + root);
    }
    private static void Render(string backend, string root, string output)
    {
        ProjectSettings.ConfigurePaths(root, IOPath.Combine(root, "user")); ProjectSettings.Load();
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        TranslationServer.Culture = CultureInfo.GetCultureInfo("fr-FR");
        using var scene = ResourceLoader.Load<PackedScene>("res://locale.e2dscene");
        var window = (Window)scene.Instantiate(); var texture = (ImageTexture)window.GetNode<Sprite>("Banner").Texture!;
        Check(texture.ResourcePath == "res://base.png", "Fresh scene dependency retains its logical path.");
        Check(Pixel(texture) == Colors.Red, "Before startup the current French locale uses original data.");
        window.AddChild(new LocaleCapture(window, texture, output)); Engine.Run(window);
        Console.WriteLine("Fresh localized scene rendered en/ru/original/en through " + backend);
    }
    private sealed class LocaleCapture(Window window, ImageTexture texture, string output) : Node
    {
        private int _frames, _phase, _notifications;
        protected override void OnReady() => Check(Pixel(texture) == Colors.Green && TranslationServer.Culture.Name == "en-US", "Project startup updates localized resources before scene ready.");
        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what != NotificationTranslationChanged) return;
            var expected = Expected();
            Check(Pixel(texture) == expected, "Localized resource state commits before scene translation callbacks.");
            _notifications++;
        }
        protected override void OnProcess(double delta)
        {
            if (++_frames % 3 != 0) return;
            using var frame = window.GetTexture().GetImage() ?? throw new InvalidOperationException("No scene framebuffer.");
            var color = frame.GetPixel(frame.Width / 2, frame.Height / 2);
            Check(color == Expected(), "Current locale reaches retained native pixels: " + TranslationServer.Culture.Name + " " + color);
            Directory.CreateDirectory(output); frame.SavePNG(IOPath.Combine(output, _phase + ".png"));
            _phase++;
            if (_phase == 4) { Check(_notifications >= 3, "Three owner translation notifications."); Tree!.Quit(); return; }
            TranslationServer.Culture = CultureInfo.GetCultureInfo(_phase == 1 ? "ru" : _phase == 2 ? "fr-FR" : "en-US");
        }
        private static Color Expected() => TranslationServer.Culture.Name.StartsWith("en", StringComparison.Ordinal) ? Colors.Green : TranslationServer.Culture.Name.StartsWith("ru", StringComparison.Ordinal) ? Colors.Blue : Colors.Red;
    }
}
