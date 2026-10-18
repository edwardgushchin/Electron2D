using System.Globalization;
using Electron2D;

internal static partial class GUITranslationContextTests
{
    internal static void RunNative()
    {
        var method = ProjectSettings.Get(ProjectSettings.RenderingMethod);
        var locale = ProjectSettings.Get(ProjectSettings.LocaleTest);
        var previous = TranslationServer.Culture;
        try
        {
            ProjectSettings.Set(ProjectSettings.LocaleTest, "en"); Register();
            foreach (var backend in new[] { "gpu", "compatibility" })
            {
                ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); Pixels(backend);
            }
        }
        finally
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, method); ProjectSettings.Set(ProjectSettings.LocaleTest, locale);
            TranslationServer.Culture = previous; TranslationServer.RemoveDomain(Domain);
        }
    }
    private static void Pixels(string backend)
    {
        var window = new Window { Size = new(240, 100), LayoutDirection = LayoutDirection.LTR };
        var contextual = new Label { Name = "Contextual", TranslationDomain = Domain, TranslationContext = "action", Text = "Token", Position = new(8, 8), Size = new(210, 28), LayoutDirection = LayoutDirection.LTR };
        var expected = new Label { Name = "Expected", AutoTranslateMode = NodeAutoTranslateMode.Disabled, Text = "AAAA", Position = new(8, 48), Size = new(210, 28), LayoutDirection = LayoutDirection.LTR };
        window.AddChild(contextual); window.AddChild(expected);
        var frames = 0;
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                using var image = RenderingServer.Service!.Readback();
                var ink = 0;
                for (var y = 0; y < 28; y++) for (var x = 0; x < 210; x++)
                    {
                        var actual = image.GetPixel(x + 8, y + 8); var reference = image.GetPixel(x + 8, y + 48);
                        Check(MathF.Abs(actual.R - reference.R) < .02f && MathF.Abs(actual.G - reference.G) < .02f && MathF.Abs(actual.B - reference.B) < .02f, "Contextual glyph pixels match independently authored expected text.");
                        if (actual.R > .2f) ink++;
                    }
                Check(ink > 30, "Comparison contains actual glyph pixels.");
                if (Environment.GetEnvironmentVariable("ELECTRON2D_LOCALE_CAPTURE") is { Length: > 0 } folder)
                    image.SavePNG(System.IO.Path.Combine(folder, $"{backend}-{frames + 1}.png"));
                frames++;
                if (frames == 1)
                {
                    contextual.TranslationContext = "noun";
                    Check(contextual.GetTotalCharacterCount() == 4, "Native setter retains the prepared caption.");
                    contextual.Notify(Node.NotificationTranslationChanged); expected.Text = "BBBBBB";
                    Check(contextual.GetTotalCharacterCount() == 6, "Native notification rebuilds contextual text.");
                }
                else if (frames == 2)
                {
                    TranslationServer.Culture = CultureInfo.GetCultureInfo("fr"); expected.Text = "CCCCCCCC";
                    Check(contextual.GetTotalCharacterCount() == 8, "Global culture publication refreshes actual prepared glyph text without manual notification.");
                }
                else window.Tree!.Quit();
            };
        };
        Check(Engine.Run(window) == 0 && frames == 3, "Contextual GUI renders all states and completes native lifecycle.");
        Console.WriteLine($"GUI translation context {backend}: independently matched initial, contextual and global-culture glyph pixels passed.");
    }
}
