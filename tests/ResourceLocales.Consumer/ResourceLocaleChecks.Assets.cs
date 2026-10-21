using System.Globalization;
using System.IO.Compression;
using Electron2D;

internal static partial class ResourceLocaleChecks
{
    private static void RelativeDependencies()
    {
        var previous = ProjectSettings.Get(ProjectSettings.LocaleResourceRemaps);
        try
        {
            Directory.CreateDirectory(ProjectSettings.GlobalizePath("res://variants")); Save("res://variants/dependency.png", Colors.Green); Save("res://variants/other.png", Colors.Blue);
            ProjectSettings.Set(ProjectSettings.LocaleResourceRemaps, Array.Empty<ResourceLocaleRemap>());
            using var a = ResourceLoader.Load<ImageTexture>("res://base.png", ResourceLoader.CacheMode.IgnoreDeep);
            using var b = ResourceLoader.Load<ImageTexture>("res://variants/dependency.png", ResourceLoader.CacheMode.IgnoreDeep);
            using var original = new AtlasTexture { Atlas = a, Region = new(0, 0, 8, 8) };
            using var localized = new AtlasTexture { Atlas = b, Region = new(0, 0, 8, 8) };
            ResourceSaver.Save(original, "res://relative.e2dres", SaverFlags.RelativePaths);
            ResourceSaver.Save(localized, "res://variants/relative-en.e2dres", SaverFlags.RelativePaths);
            ProjectSettings.Set(ProjectSettings.LocaleResourceRemaps, new ResourceLocaleRemap[] { new("res://relative.e2dres", "en", "res://variants/relative-en.e2dres") });
            TranslationServer.Culture = CultureInfo.GetCultureInfo("en-US");
            Check(ResourceLoader.GetDependencies("res://relative.e2dres").Any(path => path.Contains("dependency.png", StringComparison.Ordinal)), "Dependency discovery reads the selected physical archive.");
            using var value = ResourceLoader.Load<AtlasTexture>("res://relative.e2dres");
            using (var image = value.GetImage()!) Check(image.GetPixel(0, 0) == Colors.Green, "Relative dependencies resolve beside the selected archive.");
            Check(value.Atlas!.ResourcePath == "res://variants/dependency.png", "Selected relative dependency path.");
            ResourceLoader.RenameDependencies("res://relative.e2dres", new Dictionary<string, string> { ["res://variants/dependency.png"] = "res://variants/other.png" });
            Check(ResourceLoader.GetDependencies("res://relative.e2dres").Any(path => path.Contains("other.png", StringComparison.Ordinal)), "Dependency rewriting edits the selected archive.");
            Check(ReferenceEquals(value, ResourceLoader.Load<AtlasTexture>("res://relative.e2dres", ResourceLoader.CacheMode.ReplaceDeep)), "ReplaceDeep retains the logical root.");
            using (var image = value.GetImage()!) Check(image.GetPixel(0, 0) == Colors.Blue, "Rewritten localized dependency executes.");
        }
        finally { ProjectSettings.Set(ProjectSettings.LocaleResourceRemaps, previous); }
    }
    private static void PackedImages()
    {
        var previous = ProjectSettings.Get(ProjectSettings.LocaleResourceRemaps);
        try
        {
            var pack = ProjectSettings.GlobalizePath("res://locale.zip");
            using (var stream = new FileStream(pack, FileMode.Create))
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create))
                foreach (var pair in new[] { ("base", Colors.Red), ("en", Colors.Green), ("ru", Colors.Blue) })
                {
                    using var image = Image.CreateEmpty(8, 8, false, Image.Format.Rgba8); image.Fill(pair.Item2);
                    using var entry = zip.CreateEntry("locale-pack/" + pair.Item1 + ".png").Open(); entry.Write(image.SavePNGToBuffer());
                }
            Check(ProjectSettings.LoadResourcePack(pack), "Mount locale resource pack.");
            ProjectSettings.Set(ProjectSettings.LocaleResourceRemaps, new ResourceLocaleRemap[] { new("res://locale-pack/base.png", "en", "res://locale-pack/en.png"), new("res://locale-pack/base.png", "ru", "res://locale-pack/ru.png") });
            TranslationServer.Culture = CultureInfo.GetCultureInfo("en-US");
            using var packed = ResourceLoader.Load<ImageTexture>("res://locale-pack/base.png"); Check(Pixel(packed) == Colors.Green, "Selected data comes from mounted input.");
            TranslationServer.Culture = CultureInfo.GetCultureInfo("ru"); Check(Pixel(packed) == Colors.Blue && packed.ResourcePath == "res://locale-pack/base.png", "Packed variants refresh the same logical resource.");
        }
        finally { ProjectSettings.Set(ProjectSettings.LocaleResourceRemaps, previous); }
    }
    private static void OtherFormats()
    {
        var previous = ProjectSettings.Get(ProjectSettings.LocaleResourceRemaps);
        try
        {
            using var original = new AudioStreamWAV { SampleFormat = AudioStreamWAV.Format.PCM16, MixRate = 8000, Data = new byte[16] };
            using var localized = new AudioStreamWAV { SampleFormat = AudioStreamWAV.Format.PCM16, MixRate = 16000, Data = Enumerable.Repeat((byte)1, 32).ToArray() };
            original.SaveToWAV("res://voice.wav"); localized.SaveToWAV("res://voice-en.wav");
            System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://code.glslinc"), "const float locale_value=1.0;");
            System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://code-en.glslinc"), "const float locale_value=2.0;");
            byte[] Font(string name) { using var input = typeof(ResourceLocaleChecks).Assembly.GetManifestResourceStream("Locale." + name + ".ttf")!; using var output = new MemoryStream(); input.CopyTo(output); return output.ToArray(); }
            var regular = Font("Regular"); var semibold = Font("SemiBold");
            System.IO.File.WriteAllBytes(ProjectSettings.GlobalizePath("res://font.ttf"), regular); System.IO.File.WriteAllBytes(ProjectSettings.GlobalizePath("res://font-en.ttf"), semibold);
            ProjectSettings.Set(ProjectSettings.LocaleResourceRemaps, new ResourceLocaleRemap[]
            {
                new("res://voice.wav", "en", "res://voice-en.wav"), new("res://code.glslinc", "en", "res://code-en.glslinc"), new("res://font.ttf", "en", "res://font-en.ttf")
            });
            TranslationServer.Culture = CultureInfo.GetCultureInfo("en-US");
            using var voice = ResourceLoader.Load<AudioStreamWAV>("res://voice.wav"); using var include = ResourceLoader.Load<ShaderInclude>("res://code.glslinc"); using var font = ResourceLoader.Load<FontFile>("res://font.ttf");
            Check(voice.MixRate == 16000 && voice.Data.Length == 32 && include.Code.Contains("2.0", StringComparison.Ordinal) && font.Data.SequenceEqual(semibold), "WAV, shader source and font variants decode through their actual loaders.");
            TranslationServer.Culture = CultureInfo.GetCultureInfo("fr-FR");
            Check(voice.MixRate == 8000 && voice.Data.Length == 16 && include.Code.Contains("1.0", StringComparison.Ordinal) && font.Data.SequenceEqual(regular), "Every retained resource family copies the original payload on locale fallback.");
            Check(voice.ResourcePath == "res://voice.wav" && include.ResourcePath == "res://code.glslinc" && font.ResourcePath == "res://font.ttf", "Resource family logical paths survive copying.");
        }
        finally { ProjectSettings.Set(ProjectSettings.LocaleResourceRemaps, previous); }
    }
}
