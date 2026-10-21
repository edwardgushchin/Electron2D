using Electron2D;
using Path = System.IO.Path;
using System.Runtime.InteropServices;
using System.Text.Json;

internal static class HostPlatformChecks
{
    internal static void Run(string[] args)
    {
        if (args.Length > 0 && args[0] is "gpu" or "compatibility") { Native(args[0], args[1], args.Length > 2 && args[2] == "movie"); return; }
        Check(OS.GetName() == "Linux" && !RenderingServer.IsAvailable, "Host queries require no renderer.");
        Check(OS.GetProcessorCount() > 0 && OS.GetProcessorName().Length != 0, "Native CPU metadata.");
        Check(OS.GetDistributionName().Length != 0 && OS.GetVersion().Length != 0, "Distribution and version metadata.");
        foreach (var tag in new[] { "linux", "linuxbsd", "pc", "x86", "x86_64", "64", "single", "template", "threads", "dotnet", "csharp" })
            Check(OS.HasFeature(tag) && ProjectSettings.HasFeature(tag), "Shared host feature " + tag);
        Check(OS.HasFeature(OS.IsDebugBuild() ? "debug" : "release") && !OS.HasFeature(OS.IsDebugBuild() ? "release" : "debug"), "Engine configuration tags.");
        Check(!OS.HasFeature("movie") && !OS.HasFeature("s3tc") && !OS.HasFeature("") && !OS.HasFeature("not a feature"), "Inactive and unknown features do not fabricate capabilities.");
        Check(OS.HasFeature("LINUX") && OS.HasFeature("X86_64"), "Shared case-insensitive typed feature policy.");
        Reject<ArgumentNullException>(() => OS.HasFeature(null!));
        Reject<InvalidOperationException>(() => RenderingServer.HasOSFeature("s3tc"));
        var root = Directory.CreateTempSubdirectory("electron2d-host-").FullName;
        ProjectSettings.ConfigurePaths(root, Path.Combine(root, "user"));
        var setting = new ProjectSetting<int>("example/host_choice", 1); ProjectSettings.Register(setting);
        try
        {
            ProjectSettings.SetFeatureOverride(setting, "x86", 2);
            Check(ProjectSettings.GetWithOverride(setting) == 2, "New host family tags select actual typed overrides.");
            ProjectSettings.ClearFeatureOverride(setting, "x86");
            ProjectSettings.SetFeatureOverride(setting, "host_choice", 3); ProjectSettings.AddCustomFeature("host_choice");
            Check(OS.HasFeature("HOST_CHOICE") && ProjectSettings.GetWithOverride(setting) == 3, "Custom project tags flow into the OS query and values.");
            ProjectSettings.Save(); ProjectSettings.RemoveCustomFeature("host_choice");
            Check(!OS.HasFeature("host_choice") && ProjectSettings.GetWithOverride(setting) == 1, "Removal is live.");
            ProjectSettings.Load(); Check(!OS.HasFeature("host_choice"), "Loading settings does not recreate removed runtime-only custom features.");
            ProjectSettings.AddCustomFeature("host_choice"); Check(ProjectSettings.GetWithOverride(setting) == 3, "Persisted overrides follow the explicitly reactivated runtime feature.");
        }
        finally { ProjectSettings.RemoveCustomFeature("host_choice"); ProjectSettings.Unregister(setting); Directory.Delete(root, true); }
        var previous = OS.HasEnvironment("LANG") ? OS.GetEnvironment("LANG") : null;
        var culture = TranslationServer.Culture;
        try
        {
            foreach (var (input, locale, language) in new[] { ("en_US.UTF-8", "en_US", "en"), ("sr_RS@latin.UTF-8", "sr_RS@latin", "sr"), ("C.UTF-8", "C", "C"), ("de_DE.ISO8859-1@euro", "de_DE", "de"), ("fil_PH", "fil_PH", "fil") })
            { OS.SetEnvironment("LANG", input); Check(OS.GetLocale() == locale && OS.GetLocaleLanguage() == language, "Live POSIX locale parsing: " + input); }
            OS.UnsetEnvironment("LANG"); Check(OS.GetLocale() == "en" && OS.GetLocaleLanguage() == "en", "Absent LANG fallback.");
            Check(ReferenceEquals(TranslationServer.Culture, culture), "Host locale queries never assign the application culture.");
        }
        finally { Restore("LANG", previous); }
        var variables = new[] { "SNAP", "SNAP_NAME", "SNAP_REVISION" }; var values = variables.Select(name => OS.HasEnvironment(name) ? OS.GetEnvironment(name) : null).ToArray();
        try
        {
            foreach (var name in variables) OS.UnsetEnvironment(name);
            var marked = File.Exists("/.flatpak-info") || File.Exists("/run/host/container-manager");
            Check(OS.IsSandboxed() == marked, "Sandbox markers without Snap.");
            OS.SetEnvironment("SNAP", "/test"); Check(OS.IsSandboxed() == marked, "One Snap variable is insufficient.");
            OS.SetEnvironment("SNAP_NAME", "test"); Check(OS.IsSandboxed() == marked, "Two Snap variables are insufficient.");
            OS.SetEnvironment("SNAP_REVISION", "1"); Check(OS.IsSandboxed(), "Complete Snap environment is detected live.");
        }
        finally { for (var i = 0; i < variables.Length; i++) Restore(variables[i], values[i]); }
        for (var i = 0; i < 32; i++) QueryPrepared();
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 256; i++) QueryPrepared();
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared immutable host and feature queries allocate zero owner-managed bytes.");
        Check(!RenderingServer.IsAvailable, "Metadata and feature queries never opened a renderer.");
        using var bytes = new MemoryStream();
        using (var writer = new Utf8JsonWriter(bytes))
        {
            writer.WriteStartObject(); writer.WriteString("Name", OS.GetName()); writer.WriteString("Distribution", OS.GetDistributionName()); writer.WriteString("Version", OS.GetVersion());
            writer.WriteString("Processor", OS.GetProcessorName()); writer.WriteNumber("Cores", OS.GetProcessorCount()); writer.WriteString("Locale", OS.GetLocale());
            writer.WriteString("Architecture", RuntimeInformation.ProcessArchitecture.ToString()); writer.WriteBoolean("Debug", OS.IsDebugBuild()); writer.WriteBoolean("SystemFonts", OS.HasFeature("system_fonts")); writer.WriteEndObject();
        }
        Console.WriteLine(System.Text.Encoding.UTF8.GetString(bytes.ToArray()));
    }
    private static void QueryPrepared() { _ = OS.GetName(); _ = OS.GetDistributionName(); _ = OS.GetVersion(); _ = OS.GetProcessorName(); _ = OS.HasFeature("linux"); _ = OS.HasFeature("s3tc"); _ = OS.IsDebugBuild(); }
    private static void Restore(string name, string? value) { if (value is null) OS.UnsetEnvironment(name); else OS.SetEnvironment(name, value); }
    private static void Native(string backend, string output, bool movie = false)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        Check(!OS.HasFeature("s3tc"), "No stale capabilities before native startup.");
        using var window = new Window { Title = "Host feature selection", Size = new(128, 96) }; window.AddChild(new FeatureCapture(backend, output, movie));
        if (movie) Engine.Run(window, Path.ChangeExtension(output, ".movie.png"), 24); else Engine.Run(window);
        Check(!RenderingServer.IsAvailable && !OS.HasFeature("s3tc") && !OS.HasFeature("bptc") && !OS.HasFeature("movie"), "Retired backend capabilities are not retained.");
    }
    private sealed class FeatureCapture(string backend, string output, bool movie) : Node
    {
        private int _frames; private ImageTexture? _texture;
        protected override void OnReady()
        {
            Check(OS.HasFeature("movie") == movie, "Movie feature follows the active recording lifecycle before Ready.");
            var compressed = OS.HasFeature("s3tc");
            var worker = Task.Run(() => OS.HasFeature("s3tc") == compressed && RenderingServer.HasOSFeature("s3tc") == compressed);
            Check(worker.GetAwaiter().GetResult(), "Worker queries safely read the same immutable active capabilities.");
            foreach (var tag in new[] { "s3tc", "bptc", "rgtc", "etc2", "astc" })
                Check(OS.HasFeature(tag) == RenderingServer.HasOSFeature(tag), "OS follows current renderer: " + tag);
            if (backend == "compatibility") Check(!compressed, "Compatibility uses its actual ordinary-image upload profile.");
            Image image;
            if (compressed) image = Image.CreateFromData(4, 4, false, Image.Format.Dxt1, new byte[] { 0, 248, 31, 0, 0, 0, 0, 0 });
            else { image = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8); image.Fill(Colors.Red); }
            using (image) _texture = ImageTexture.CreateFromImage(image);
            Parent!.AddChild(new Sprite { Texture = _texture, Centered = false, Scale = new(32, 24) });
            Console.WriteLine("Feature-selected texture, backend=" + backend + ", s3tc=" + compressed);
        }
        protected override void OnProcess(double delta)
        {
            if (++_frames < 3) return;
            for (var i = 0; i < 32; i++) { _ = OS.HasFeature("s3tc"); _ = RenderingServer.HasOSFeature("bptc"); }
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 256; i++) { _ = OS.HasFeature("s3tc"); _ = RenderingServer.HasOSFeature("bptc"); }
            Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared session feature queries allocate zero managed bytes.");
            using var image = ((Window)Parent!).GetTexture().GetImage()!; Check(image.GetPixel(64, 48) == Colors.Red, "Selected compressed/fallback data reaches actual native pixels.");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!); image.SavePNG(output); Tree!.Quit();
        }
        protected override void Dispose(bool disposing) { _texture?.Dispose(); base.Dispose(disposing); }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
