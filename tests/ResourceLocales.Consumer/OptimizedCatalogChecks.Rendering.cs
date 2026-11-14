using System.Globalization;
using System.Security.Cryptography;
using Electron2D;
using IOPath = System.IO.Path;

internal static partial class OptimizedCatalogChecks
{
    private static void Render(string backend, string directory)
    {
        using var english = ResourceLoader.Load<OptimizedTranslation>("res://en.e2dres");
        using var french = ResourceLoader.Load<OptimizedTranslation>("res://fr.e2dres");
        using var fontData = typeof(OptimizedCatalogChecks).Assembly.GetManifestResourceStream("Locale.Regular.ttf")!;
        using var buffer = new MemoryStream();
        fontData.CopyTo(buffer);
        using var font = new FontFile { Data = buffer.ToArray() };
        var window = new Window { Size = new(256, 88), ContentScaleSize = new(256, 88), ContentScaleMode = ContentScaleMode.Viewport };
        var label = new Label { Text = Caption, Position = new(8, 35), Size = new(240, 45) };
        label.AddThemeFontOverride("font", font);
        label.AddThemeFontSizeOverride("font_size", 24);
        var chooser = new OptionButton { Size = new(150, 30), Position = new(8, 0) };
        chooser.AddThemeFontOverride("font", font);
        chooser.AddItem("English");
        chooser.AddItem("French");
        var popup = chooser.GetPopup();
        popup.SetItemAccelerator(1, Key.F);
        chooser.ItemSelected += selection => TranslationServer.Culture = CultureInfo.GetCultureInfo(selection == 0 ? english.Locale : french.Locale);
        window.AddChild(chooser);
        window.AddChild(label);

        var previous = TranslationServer.Culture;
        var phase = 0;
        var frames = 0;
        string englishPixels = "", frenchPixels = "";
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        ProjectSettings.Set(ProjectSettings.LocaleTest, english.Locale);
        TranslationServer.AddTranslation(english);
        TranslationServer.AddTranslation(french);
        window.Ready += _ =>
        {
            Check(TranslationServer.Culture.Name == english.Locale, "English is the startup locale.");
            Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Requested backend is active with fallback disabled.");
            Console.WriteLine($"{backend}/{RenderingServer.GetCurrentRenderingDriverName()} adapter={RenderingServer.GetVideoAdapterName()}");
            RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += Capture;
        };
        try
        {
            Check(Engine.Run(window) == 0 && phase == 4 && window.IsDisposed, "Four native frames complete and release the window.");
        }
        finally
        {
            if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= Capture;
            TranslationServer.RemoveTranslation(french);
            TranslationServer.RemoveTranslation(english);
            TranslationServer.Culture = previous;
        }

        void Capture()
        {
            if (++frames % 3 != 0) return;
            using var frame = window.GetTexture().GetImage() ?? throw new InvalidOperationException("Missing native readback.");
            using var caption = frame.GetRegion(new Rect2i(0, 35, 256, 49));
            var pixels = Convert.ToHexString(SHA256.HashData(caption.GetData()));
            switch (phase++)
            {
                case 0:
                    englishPixels = pixels;
                    frame.SavePNG(IOPath.Combine(directory, backend + "-en.png"));
                    using (var press = new InputEventKey { Keycode = Key.F, Pressed = true })
                        Check(popup.ActivateItemByEvent(press) && chooser.Selected == 1 && TranslationServer.Culture.Name == french.Locale, "The language menu changes Culture through ItemSelected.");
                    break;
                case 1:
                    frenchPixels = pixels;
                    Check(englishPixels != frenchPixels, "Changing the language changes caption pixels.");
                    frame.SavePNG(IOPath.Combine(directory, backend + "-fr.png"));
                    label.AutoTranslateMode = NodeAutoTranslateMode.Disabled;
                    label.Text = "MMMMM";
                    break;
                case 2:
                    Check(pixels == frenchPixels, "French pixels equal the independently authored literal label.");
                    label.Text = "I";
                    break;
                case 3:
                    Check(pixels == englishPixels, "English pixels equal the independently authored literal label.");
                    Console.WriteLine($"Caption region SHA-256 en={englishPixels} fr={frenchPixels}");
                    RenderingServer.FramePostDraw -= Capture;
                    window.Tree!.Quit();
                    break;
            }
        }
    }
}
