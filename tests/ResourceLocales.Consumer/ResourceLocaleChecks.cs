using System.Globalization;
using Electron2D;
using IOPath = System.IO.Path;

internal static partial class ResourceLocaleChecks
{
    internal static void Run(string[] args)
    {
        if (args.Length > 0 && args[0] == "author") { Author(args[1]); return; }
        if (args.Length > 0 && args[0] is "gpu" or "compatibility") { Render(args[0], args[1], args[2]); return; }
        if (args.Length > 0 && args[0] == "read")
        {
            ProjectSettings.ConfigurePaths(args[1], IOPath.Combine(args[1], "user")); ProjectSettings.Load();
            TranslationServer.Culture = CultureInfo.GetCultureInfo("en-US");
            using var restored = ResourceLoader.Load<ImageTexture>("res://base.png");
            Check(restored.ResourcePath == "res://base.png" && Pixel(restored) == Colors.Green, "Fresh persisted map retains the logical resource path.");
            Console.WriteLine("Fresh locale map restoration passed."); return;
        }
        var root = args.Length > 0 ? IOPath.GetFullPath(args[0]) : Directory.CreateTempSubdirectory("electron2d-locales-").FullName;
        Directory.CreateDirectory(root); ProjectSettings.ConfigurePaths(root, IOPath.Combine(root, "user"));
        TranslationServer.Culture = CultureInfo.GetCultureInfo("en-US");
        var firstCulture = CultureInfo.GetCultureInfo("en-US"); var secondCulture = CultureInfo.GetCultureInfo("ru");
        for (var i = 0; i < 32; i++) TranslationServer.Culture = (i & 1) == 0 ? firstCulture : secondCulture;
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 256; i++) TranslationServer.Culture = (i & 1) == 0 ? firstCulture : secondCulture;
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared culture changes without remaps preserve zero managed bytes.");
        TranslationServer.Culture = firstCulture;
        Save("res://base.png", Colors.Red); Save("res://en.png", Colors.Green); Save("res://ru.png", Colors.Blue);
        ResourceLocaleRemap[] records = [new("res://base.png", "en-US", "res://en.png"), new("res://base.png", "ru", "res://ru.png")];
        ProjectSettings.Set(ProjectSettings.LocaleResourceRemaps, records); records[0] = new("res://base.png", "en-US", "res://ru.png");
        Check(ProjectSettings.Get(ProjectSettings.LocaleResourceRemaps)[0].LocalizedPath == "res://en.png", "Project values snapshot mutable arrays.");
        ProjectSettings.Save();
        Check(ResourceLoader.Exists<ImageTexture>("res://base.png"), "Localized existence.");
        using var loaded = ResourceLoader.Load<ImageTexture>("res://base.png");
        using var independent = ResourceLoader.Load<ImageTexture>("res://base.png", ResourceLoader.CacheMode.Ignore);
        Check(Pixel(loaded) == Colors.Green && loaded.ResourcePath == "res://base.png" && !ReferenceEquals(loaded, independent), "Logical cache and independent selected data.");
        Check(ReferenceEquals(loaded, ResourceLoader.Load<ImageTexture>("res://base.png")), "Reuse retains logical identity.");
        var changes = 0; loaded.Changed += _ => changes++;
        TranslationServer.Culture = CultureInfo.GetCultureInfo("ru-RU");
        Check(Pixel(loaded) == Colors.Blue && Pixel(independent) == Colors.Blue && changes == 1, "Locale changes refresh registered and ignored instances exactly once.");
        var equalCulture = new CultureInfo("ru-RU"); TranslationServer.Culture = equalCulture;
        Check(changes == 1 && ReferenceEquals(TranslationServer.Culture, equalCulture), "Equal-name culture preserves supplied identity and notification policy.");
        TranslationServer.Culture = CultureInfo.GetCultureInfo("fr-FR");
        Check(Pixel(loaded) == Colors.Red && Pixel(independent) == Colors.Red, "Another language uses the original, independent of catalog fallback.");
        ProjectSettings.Set(ProjectSettings.LocaleResourceRemaps, new ResourceLocaleRemap[]
        {
            new("res://base.png", "en-US", "res://en.png"), new("res://base.png", "en-GB", "res://ru.png")
        });
        TranslationServer.Culture = CultureInfo.GetCultureInfo("en-AU"); Check(Pixel(loaded) == Colors.Blue, "Last nonexact score tie wins.");
        TranslationServer.Culture = CultureInfo.GetCultureInfo("en-US"); Check(Pixel(loaded) == Colors.Green, "Exact candidate wins before later weaker candidates.");
        ProjectSettings.Set(ProjectSettings.LocaleResourceRemaps, new ResourceLocaleRemap[]
        {
            new("res://base.png", "en-US", "res://en.png"), new("res://base.png", "en-US", "res://ru.png")
        });
        TranslationServer.ReloadPseudolocalization(); Check(Pixel(loaded) == Colors.Green, "First exact candidate wins on explicit translation refresh.");
        System.IO.File.Delete(ProjectSettings.GlobalizePath("res://en.png"));
        TranslationServer.ReloadPseudolocalization(); Check(Pixel(loaded) == Colors.Red, "Missing selected file falls back before decode.");
        Save("res://en.png", Colors.Green);
        ProjectSettings.Set(ProjectSettings.LocaleResourceRemaps, new ResourceLocaleRemap[] { new("res://base.png", "en-US", "res://en.png"), new("res://base.png", "ru", "res://ru.png") });
        TranslationServer.Culture = CultureInfo.GetCultureInfo("ru");
        System.IO.File.WriteAllBytes(ProjectSettings.GlobalizePath("res://en.png"), [0, 1, 2]);
        Reject<AggregateException>(() => TranslationServer.Culture = CultureInfo.GetCultureInfo("en-US"));
        Check(Pixel(loaded) == Colors.Blue && Pixel(independent) == Colors.Blue, "Malformed selected data leaves both prior resource payloads intact.");
        Save("res://en.png", Colors.Green); TranslationServer.ReloadPseudolocalization();
        TranslationServer.Culture = CultureInfo.GetCultureInfo("ru");
        var failedObservers = 0;
        Action<Resource> failing = _ => { failedObservers++; throw new ApplicationException("locale observer"); }; loaded.Changed += failing; independent.Changed += failing;
        try { Reject<AggregateException>(() => TranslationServer.Culture = CultureInfo.GetCultureInfo("en-US")); }
        finally { loaded.Changed -= failing; independent.Changed -= failing; }
        Check(failedObservers == 2, "All resource observers are attempted after either one fails.");
        Check(Pixel(loaded) == Colors.Green && Pixel(independent) == Colors.Green && TranslationServer.Culture.Name == "en-US", "Observer failure preserves committed culture/data and later resources.");
        ResourceLoader.LoadThreadedRequest<ImageTexture>("res://base.png", cacheMode: ResourceLoader.CacheMode.IgnoreDeep);
        using var threaded = ResourceLoader.LoadThreadedGet<ImageTexture>("res://base.png");
        Check(Pixel(threaded) == Colors.Green && threaded.ResourcePath == "res://base.png", "Threaded graph retains logical identity.");
        var reentered = false;
        Action<Resource> nested = _ => { if (!reentered) { reentered = true; TranslationServer.Culture = CultureInfo.GetCultureInfo("ru"); } };
        loaded.Changed += nested;
        try { TranslationServer.Culture = CultureInfo.GetCultureInfo("fr-FR"); }
        finally { loaded.Changed -= nested; }
        Check(reentered && TranslationServer.Culture.Name == "ru" && Pixel(loaded) == Colors.Blue && Pixel(independent) == Colors.Blue && Pixel(threaded) == Colors.Blue, "Nested culture refresh settles every retained resource on the latest locale.");
        var retained = ProjectSettings.Get(ProjectSettings.LocaleResourceRemaps);
        ProjectSettings.Set(ProjectSettings.LocaleResourceRemaps, Array.Empty<ResourceLocaleRemap>()); TranslationServer.ReloadPseudolocalization();
        Check(Pixel(loaded) == Colors.Red && Pixel(independent) == Colors.Red && Pixel(threaded) == Colors.Red, "Removing remaps restores original data.");
        ProjectSettings.Set(ProjectSettings.LocaleResourceRemaps, retained); TranslationServer.ReloadPseudolocalization();
        Check(Pixel(loaded) == Colors.Blue && Pixel(independent) == Colors.Blue && Pixel(threaded) == Colors.Blue, "Restoring remaps refreshes every associated ignored instance too.");
        Action<Resource> disposeOthers = _ => { independent.Dispose(); threaded.Dispose(); }; loaded.Changed += disposeOthers;
        try { TranslationServer.Culture = CultureInfo.GetCultureInfo("fr-FR"); }
        finally { loaded.Changed -= disposeOthers; }
        Check(Pixel(loaded) == Colors.Red && independent.IsDisposed && threaded.IsDisposed, "Disposal during resource notifications leaves later snapshot traversal safe.");
        ArchiveCases(); ThreadedSnapshotCases(); RelativeDependencies(); PackedImages(); OtherFormats();
        var weak = WeakLocalizedResource();
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        Check(!weak.TryGetTarget(out _), "The remap registry does not retain caller resources.");
        Reject<ArgumentException>(() => new ResourceLocaleRemap("", "en", "a"));
        Reject<ArgumentException>(() => new ResourceLocaleRemap("a", "", "b"));
        Reject<ArgumentException>(() => new ResourceLocaleRemap("a::child", "en", "b"));
        Reject<ArgumentException>(() => new ResourceLocaleRemap("uid://a", "en", "b"));
        Reject<ArgumentException>(() => new ResourceLocaleRemap("a", "en", "\ud800"));
        Reject<ArgumentException>(() => new ResourceLocaleRemap("a", "\ud800", "b"));
        Reject<ArgumentOutOfRangeException>(() => ProjectSettings.Set(ProjectSettings.LocaleResourceRemaps, new ResourceLocaleRemap[] { null! }));
        ProjectSettings.Set(ProjectSettings.LocaleResourceRemaps, new ResourceLocaleRemap[] { new("res://base.png", "en-US", "res://en.png"), new("res://base.png", "ru", "res://ru.png") });
        TranslationServer.Culture = CultureInfo.GetCultureInfo("en-US"); ProjectSettings.Save();
        Console.WriteLine("Locale resource selection, identity, synchronous refresh, failure continuation, threaded load and persistence passed: " + root);
    }
    private static void Save(string path, Color color) { using var image = Image.CreateEmpty(8, 8, false, Image.Format.Rgba8); image.Fill(color); image.SavePNG(path); }
    private static Color Pixel(ImageTexture texture) { using var image = texture.GetImage()!; return image.GetPixel(0, 0); }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static WeakReference<Resource> WeakLocalizedResource() => new(ResourceLoader.Load<ImageTexture>("res://base.png", ResourceLoader.CacheMode.IgnoreDeep));
}
