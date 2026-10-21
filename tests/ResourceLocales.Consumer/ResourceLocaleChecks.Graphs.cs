using System.Globalization;
using Electron2D;

internal static partial class ResourceLocaleChecks
{
    private static void ArchiveCases()
    {
        var previous = ProjectSettings.Get(ProjectSettings.LocaleResourceRemaps);
        try
        {
            using var red = Image.CreateEmpty(8, 8, false, Image.Format.Rgba8); red.Fill(Colors.Red);
            using var green = Image.CreateEmpty(8, 8, false, Image.Format.Rgba8); green.Fill(Colors.Green);
            using var a = ImageTexture.CreateFromImage(red); using var b = ImageTexture.CreateFromImage(green);
            using var original = new AtlasTexture { Atlas = a, Region = new(0, 0, 8, 8) };
            using var localized = new AtlasTexture { Atlas = b, Region = new(0, 0, 8, 8) };
            ResourceSaver.Save(original, "res://atlas.e2dres", SaverFlags.BundleResources);
            ResourceSaver.Save(localized, "res://atlas-en.e2dres", SaverFlags.BundleResources);
            var originalID = ResourceLoader.GetResourceUID("res://atlas.e2dres"); var localizedID = ResourceLoader.GetResourceUID("res://atlas-en.e2dres");
            ResourceUID.RemoveID(originalID);
            ProjectSettings.Set(ProjectSettings.LocaleResourceRemaps, new ResourceLocaleRemap[] { new("res://atlas.e2dres", "en", "res://atlas-en.e2dres") });
            TranslationServer.Culture = CultureInfo.GetCultureInfo("en-US");
            using var atlas = ResourceLoader.Load<AtlasTexture>("res://atlas.e2dres");
            Check(atlas.ResourcePath == "res://atlas.e2dres" && atlas.Atlas!.ResourcePath.StartsWith("res://atlas.e2dres::", StringComparison.Ordinal), "Archive internal identities use the original logical file.");
            using (var image = atlas.GetImage()!) Check(image.GetPixel(0, 0) == Colors.Green, "Selected archive root payload.");
            Check(ResourceUID.GetIDPath(originalID) == "res://atlas.e2dres" && ResourceUID.GetIDPath(localizedID) == "res://atlas-en.e2dres", "Source UID is restored without rebinding the selected archive UID.");
            Check(ReferenceEquals(atlas, ResourceLoader.Load<AtlasTexture>(ResourceUID.IDToText(originalID))), "Caller UID resolves to the logical cache.");
            TranslationServer.Culture = CultureInfo.GetCultureInfo("fr-FR");
            using (var image = atlas.GetImage()!) Check(image.GetPixel(0, 0) == Colors.Red, "Fallback archive updates the retained root.");
            TranslationServer.Culture = CultureInfo.GetCultureInfo("en-US");
            Check(atlas.Atlas!.ResourcePath.StartsWith("res://atlas.e2dres::", StringComparison.Ordinal), "Reloaded internal paths remain logical.");
            using var replaced = ResourceLoader.Load<AtlasTexture>("res://atlas.e2dres", ResourceLoader.CacheMode.IgnoreDeep);
            Check(!ReferenceEquals(atlas, replaced) && replaced.ResourcePath == atlas.ResourcePath, "Independent archived graph.");
        }
        finally { ProjectSettings.Set(ProjectSettings.LocaleResourceRemaps, previous); }
    }

    private static void ThreadedSnapshotCases()
    {
        var previous = ProjectSettings.Get(ProjectSettings.LocaleResourceRemaps);
        using var loader = new DelayedLocaleLoader(); ResourceLoader.AddResourceFormatLoader(loader, true);
        try
        {
            System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://delayed.locpayload"), "red");
            System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://delayed-en.locpayload"), "green");
            System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://delayed-ru.locpayload"), "blue");
            ProjectSettings.Set(ProjectSettings.LocaleResourceRemaps, new ResourceLocaleRemap[] { new("res://delayed.locpayload", "en", "res://delayed-en.locpayload"), new("res://delayed.locpayload", "ru", "res://delayed-ru.locpayload") });
            TranslationServer.Culture = CultureInfo.GetCultureInfo("en-US");
            ResourceLoader.LoadThreadedRequest<ImageTexture>("res://delayed.locpayload", cacheMode: ResourceLoader.CacheMode.IgnoreDeep);
            Check(loader.Entered.Wait(TimeSpan.FromSeconds(10)), "Worker entered captured locale source.");
            Check(loader.Physical == "res://delayed-en.locpayload" && loader.Original == "res://delayed.locpayload", "Format hook receives physical and original paths separately.");
            TranslationServer.Culture = CultureInfo.GetCultureInfo("ru");
            Reject<InvalidOperationException>(() => ResourceLoader.LoadThreadedRequest<ImageTexture>("res://delayed.locpayload", cacheMode: ResourceLoader.CacheMode.IgnoreDeep));
            loader.Release.Set();
            Reject<InvalidOperationException>(() => ResourceLoader.LoadThreadedGet<ImageTexture>("res://delayed.locpayload"));
            Check(!ResourceLoader.HasCached("res://delayed.locpayload") && loader.Prepared!.IsDisposed, "Stale publication fails before caching and disposes prepared state.");
            ResourceLoader.LoadThreadedRequest<ImageTexture>("res://delayed.locpayload");
            using var latest = ResourceLoader.LoadThreadedGet<ImageTexture>("res://delayed.locpayload");
            Check(Pixel(latest) == Colors.Blue && latest.ResourcePath == "res://delayed.locpayload", "A fresh request publishes the current locale.");
            loader.BlockNext();
            ResourceLoader.LoadThreadedRequest<ImageTexture>("res://delayed.locpayload", cacheMode: ResourceLoader.CacheMode.IgnoreDeep);
            Check(loader.Entered.Wait(TimeSpan.FromSeconds(10)), "Worker captured the remap configuration.");
            ProjectSettings.Set(ProjectSettings.LocaleResourceRemaps, new ResourceLocaleRemap[] { new("res://delayed.locpayload", "ru", "res://delayed-en.locpayload") });
            loader.Release.Set(); Reject<InvalidOperationException>(() => ResourceLoader.LoadThreadedGet<ImageTexture>("res://delayed.locpayload"));
            Check(Pixel(latest) == Colors.Blue, "Stale settings never overwrite an already cached current instance.");
            ResourceLoader.LoadThreadedRequest<ImageTexture>("res://delayed.locpayload", cacheMode: ResourceLoader.CacheMode.Replace);
            using var replaced = ResourceLoader.LoadThreadedGet<ImageTexture>("res://delayed.locpayload");
            Check(ReferenceEquals(latest, replaced) && Pixel(latest) == Colors.Green, "A new settings cohort refreshes the retained cache identity.");
            ProjectSettings.Set(ProjectSettings.LocaleResourceRemaps, Array.Empty<ResourceLocaleRemap>());
            loader.BlockNext();
            ResourceLoader.LoadThreadedRequest<ImageTexture>("res://delayed.locpayload", cacheMode: ResourceLoader.CacheMode.IgnoreDeep);
            Check(loader.Entered.Wait(TimeSpan.FromSeconds(10)), "Worker captured an initially unmapped file.");
            ProjectSettings.Set(ProjectSettings.LocaleResourceRemaps, new ResourceLocaleRemap[] { new("res://delayed.locpayload", "ru", "res://delayed-ru.locpayload") });
            loader.Release.Set(); Reject<InvalidOperationException>(() => ResourceLoader.LoadThreadedGet<ImageTexture>("res://delayed.locpayload"));
            Check(loader.Prepared!.IsDisposed && Pixel(latest) == Colors.Green, "Adding a map during preparation rejects stale original data before publication.");
        }
        finally { loader.Release.Set(); ResourceLoader.RemoveResourceFormatLoader(loader); ProjectSettings.Set(ProjectSettings.LocaleResourceRemaps, previous); }
    }
    private sealed class DelayedLocaleLoader : ResourceFormatLoader
    {
        internal readonly ManualResetEventSlim Entered = new(), Release = new();
        internal string? Physical, Original;
        internal ImageTexture? Prepared;
        private int _first = 1;
        internal void BlockNext() { Entered.Reset(); Release.Reset(); _first = 1; }
        public override string[] GetRecognizedExtensions() => ["locpayload"];
        public override bool HandlesType(Type type) => type.IsAssignableFrom(typeof(ImageTexture));
        public override Type? GetResourceType(string path) => typeof(ImageTexture);
        public override Resource Load(string path, string originalPath, bool useSubThreads, ResourceLoader.CacheMode cacheMode)
        {
            Physical = path; Original = originalPath;
            if (Interlocked.Exchange(ref _first, 0) == 1) { Entered.Set(); if (!Release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException("Locale fixture was not released."); }
            using var image = Image.CreateEmpty(8, 8, false, Image.Format.Rgba8);
            image.Fill(System.IO.File.ReadAllText(ProjectSettings.GlobalizePath(path)) switch { "green" => Colors.Green, "blue" => Colors.Blue, _ => Colors.Red });
            return Prepared = ImageTexture.CreateFromImage(image);
        }
        protected override void Dispose(bool disposing) { Entered.Dispose(); Release.Dispose(); base.Dispose(disposing); }
    }
}
