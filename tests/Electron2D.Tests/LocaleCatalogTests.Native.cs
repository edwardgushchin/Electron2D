using Electron2D;
using System.Globalization;

internal static partial class LocaleCatalogTests
{
    private sealed class Counter : Entity
    {
        internal long Count;
        internal string Value = "";
        internal void Refresh() { Value = AtrN("apple", "apples", Count); QueueRedraw(); }
        protected override void OnDraw() => DrawRect(new(8, 36, 16, 12), Value == "few" ? Colors.Red : Value == "many" ? Colors.Blue : Colors.Black);
    }
    internal static void RunNative(string backend)
    {
        var original = TranslationServer.Culture;
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        using var catalog = new Translation { Locale = "ru" }; catalog.AddPluralMessage("apple", ["one", "few", "many"]); TranslationServer.AddTranslation(catalog);
        var counter = new Counter { Name = "Counter", Count = 2 }; var menu = new OptionButton { Name = "Languages", Position = new(30, 4), Size = new(120, 28) };
        var languages = TranslationServer.GetAllLanguages(); var selected = Array.IndexOf(languages, "ru");
        foreach (var language in languages) menu.AddItem(TranslationServer.GetLanguageName(language)); menu.Select(selected);
        var window = new Window { Size = new(180, 80) }; window.AddChild(counter); window.AddChild(menu); var frames = 0;
        window.Ready += _ =>
        {
            TranslationServer.Culture = CultureInfo.GetCultureInfo(TranslationServer.StandardizeLocale(languages[menu.Selected]).Replace('_', '-'));
            RenderingServer.SetDefaultClearColor(Colors.Black); counter.Refresh();
            RenderingServer.FramePostDraw += () =>
            {
                using var image = RenderingServer.Service!.Readback(); var pixel = image.GetPixel(12, 40);
                if (frames++ == 0) { Check(counter.Value == "few" && pixel.R > .95f && pixel.B < .05f, "Language selector and two-item plural reach red native pixels."); counter.Count = 5; counter.Refresh(); }
                else { Check(counter.Value == "many" && pixel.B > .95f && pixel.R < .05f, "Five-item automatic plural reaches blue native pixels."); window.Tree!.Quit(); }
            };
        };
        try { Check(Engine.Run(window) == 0 && frames == 2 && window.IsDisposed && !RenderingServer.IsAvailable, "Localized native consumer lifetime."); }
        finally { TranslationServer.RemoveTranslation(catalog); TranslationServer.Culture = original; }
        Console.WriteLine("Locale language menu, automatic Russian forms, " + backend + " red/blue native pixels and teardown passed.");
    }
}
