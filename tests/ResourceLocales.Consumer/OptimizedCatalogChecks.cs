using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Electron2D;
using IOFile = System.IO.File;
using IOPath = System.IO.Path;

internal static partial class OptimizedCatalogChecks
{
    private const string KeyText = "source-only-marker-4cc23 🦊";
    private const string Short = "短🦊";
    private const string Caption = "source caption";
    private static readonly string Long = new('W', 4096);

    internal static void Run(string operation, string directory)
    {
        Directory.CreateDirectory(directory);
        ProjectSettings.ConfigurePaths(directory, IOPath.Combine(directory, "user"));
        switch (operation)
        {
            case "write": Write(); break;
            case "read": Read(); break;
            case "gpu" or "compatibility": Render(operation, directory); break;
            default: throw new ArgumentException("Unknown optimized catalog check.", nameof(operation));
        }
    }

    private static void Write()
    {
        using var source = MakeSource("fr-CA", "MMMMM");
        using var optimized = new OptimizedTranslation();
        var changes = 0;
        optimized.Changed += _ => { changes++; Check(optimized.Locale == "fr-CA" && optimized.GetMessage(KeyText) == Long, "Observers see committed locale and values."); };
        Check(optimized.Generate(source) && changes == 1, "Generation emits one complete change.");
        using var noKeys = new Translation { Locale = "en" };
        noKeys.AddMessage("context", "unused", "menu");
        Check(!optimized.Generate(null) && !optimized.Generate(noKeys) && !optimized.Generate(optimized) && changes == 1,
            "Ineligible generation preserves the previous catalog.");
        noKeys.AddMessage("oversized", new string('a', 1024 * 1024 + 1));
        Reject<InvalidDataException>(() => optimized.Generate(noKeys));
        noKeys.AddMessage("oversized", "\ud800");
        Reject<EncoderFallbackException>(() => optimized.Generate(noKeys));
        Check(changes == 1, "Failed generation emits no change.");
        source.Dispose();
        optimized.AddMessage("hidden-base-key", "hidden-base-value");
        Verify(optimized, "fr-CA", "MMMMM");

        ResourceSaver.Save(optimized, "res://fr.e2dres");
        using var english = MakeSource("en-US", "I");
        using var other = new OptimizedTranslation();
        Check(other.Generate(english), "Author second catalog.");
        ResourceSaver.Save(other, "res://en.e2dres", SaverFlags.SaveBigEndian | SaverFlags.Compress);
        using var empty = new OptimizedTranslation { Locale = "ja" };
        ResourceSaver.Save(empty, "res://empty.e2dres");
        var file = IOFile.ReadAllBytes(ProjectSettings.GlobalizePath("res://fr.e2dres"));
        foreach (var omitted in new[] { KeyText, Caption, "hidden-base-key", "unused-context-value", Long })
            Check(file.AsSpan().IndexOf(Encoding.UTF8.GetBytes(omitted)) < 0
                && file.AsSpan().IndexOf(Encoding.Unicode.GetBytes(omitted)) < 0, "The archive omits source keys, excluded values and uncompressed long text.");
        Console.WriteLine("Authored little-endian, compressed big-endian and empty optimized resources.");
    }

    private static Translation MakeSource(string locale, string caption)
    {
        var source = new Translation { Locale = locale };
        source.AddMessage(Caption, caption);
        source.AddMessage(KeyText, Long);
        source.AddMessage("raw", Short);
        source.AddMessage("empty", "");
        source.AddMessage("context", "unused-context-value", "menu");
        source.AddPluralMessage("plural", ["primary", "secondary"]);
        return source;
    }

    private static void Read()
    {
        const string path = "res://fr.e2dres";
        Check(!ResourceLoader.HasCached(path) && ResourceLoader.Exists<OptimizedTranslation>(path)
            && ResourceLoader.GetClassesUsed(path).Contains(typeof(OptimizedTranslation)), "Exact built-in resource identity in a fresh process.");
        using var catalog = (OptimizedTranslation)ResourceLoader.Load<Translation>(path);
        using var english = ResourceLoader.Load<OptimizedTranslation>("res://en.e2dres");
        using var empty = ResourceLoader.Load<OptimizedTranslation>("res://empty.e2dres");
        Verify(catalog, "fr-CA", "MMMMM");
        Verify(english, "en-US", "I");
        Check(empty.Locale == "ja" && empty.GetTranslatedMessageList().Length == 0, "Empty catalog round trip.");
        using var copy = (OptimizedTranslation)catalog.DuplicateDeep();
        using var copiedState = new OptimizedTranslation();
        copiedState.CopyFromResource(catalog);

        RejectMalformedArchive(path, catalog);
        TranslationServer.Culture = CultureInfo.GetCultureInfo("fr-CA");
        TranslationServer.AddTranslation(catalog);
        try
        {
            Check(TranslationServer.Translate("", Caption) == "MMMMM" && TranslationServer.Translate("", KeyText) == Long, "Loaded borrowed catalog resolves through the server.");
            Check(TranslationServer.Translate("", "empty") == "empty", "Empty resource values participate in ordinary fallback.");
            catalog.Dispose();
            Check(TranslationServer.Translate("", Caption) == Caption, "Disposed borrowed catalog stops resolving.");
        }
        finally { TranslationServer.RemoveTranslation(catalog); }
        Verify(copy, "fr-CA", "MMMMM");
        Verify(copiedState, "fr-CA", "MMMMM");
        using var replacement = MakeSource("en-US", "I");
        Check(copy.Generate(replacement), "Regenerate independently.");
        Verify(copiedState, "fr-CA", "MMMMM");
        Console.WriteLine("Fresh optimized round trips, Brotli/raw/empty/Unicode, cache replacement, corruption, copies and registration passed.");
    }

    private static void Verify(OptimizedTranslation catalog, string locale, string caption)
    {
        Check(catalog.Locale == locale && catalog.GetMessage(Caption) == caption && catalog.GetMessage(KeyText, "ignored") == Long, "Locale and compressed lookup.");
        Check(catalog.GetMessage("raw") == Short && catalog.GetMessage("empty") == "" && catalog.GetMessage("context", "menu") == ""
            && catalog.GetMessage("hidden-base-key") == "", "Raw Unicode, empty, skipped contextual and inherited mutation behavior.");
        Check(catalog.GetPluralMessage("plural", "plural-other", 1) == "primary"
            && catalog.GetPluralMessage("plural", "plural-other", long.MaxValue, "ignored") == "primary", "First plural form for all counts and contexts.");
        Check(catalog.GetMessageCount() == 0 && catalog.GetMessageList().Length == 0, "Source keys remain hidden.");
        var values = catalog.GetTranslatedMessageList();
        Check(values.Order().SequenceEqual(new[] { "", caption, Long, Short, "primary" }.Order()), "All and only retained values are enumerable.");
        values[0] = "mutated";
        Check(!catalog.GetTranslatedMessageList().Contains("mutated"), "Value list is caller-owned.");
    }

    private static void RejectMalformedArchive(string path, OptimizedTranslation catalog)
    {
        var property = (PropertyDescriptor<OptimizedTranslation, byte[]>)catalog.GetPropertyList().Single(item => item.Name == "_optimized_catalog");
        var payload = property.GetValue(catalog);
        var localeUnits = BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(4));
        var countOffset = 8 + localeUnits * 2;
        var first = countOffset + 4;
        var second = first + 24 + BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(first + 20));
        var third = second + 24 + BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(second + 20));
        Check(BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(second + 16)) == Long.Length
            && BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(second + 20)) < Long.Length, "Long value is independently identified as compressed.");
        Check(BinaryPrimitives.ReadInt32LittleEndian(payload.AsSpan(third + 16)) == Encoding.UTF8.GetByteCount(Short)
            && payload.AsSpan(third + 24, Encoding.UTF8.GetByteCount(Short)).SequenceEqual(Encoding.UTF8.GetBytes(Short)), "Short value is independently identified as raw.");
        var physical = ProjectSettings.GlobalizePath(path);
        var validFile = IOFile.ReadAllBytes(physical);
        var payloadOffset = validFile.AsSpan(64).IndexOf(payload) + 64;
        Check(payloadOffset >= 64, "Locate the stored payload in the archive.");
        var changes = 0;
        catalog.Changed += _ => changes++;
        Action<byte[]>[] corruptions =
        [
            bytes => bytes[0]++,
            bytes => BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4), int.MaxValue),
            bytes => BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(countOffset), 65537),
            bytes => BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(countOffset), 4),
            bytes => bytes.AsSpan(first, 16).CopyTo(bytes.AsSpan(second, 16)),
            bytes => BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(second + 16), 1024 * 1024 + 1),
            bytes => bytes.AsSpan(second + 24, BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(second + 20))).Fill(0xff),
            bytes => bytes[third + 24] = 0xff,
        ];
        try
        {
            foreach (var corrupt in corruptions)
            {
                var malformed = (byte[])payload.Clone();
                corrupt(malformed);
                Reject<InvalidDataException>(() => property.SetValue(catalog, malformed));
                var file = (byte[])validFile.Clone();
                malformed.CopyTo(file, payloadOffset);
                SHA256.HashData(file.AsSpan(64), file.AsSpan(32, 32));
                IOFile.WriteAllBytes(physical, file);
                Reject<InvalidDataException>(() => ResourceLoader.Load<OptimizedTranslation>(path, ResourceLoader.CacheMode.Replace));
                Check(changes == 0 && ReferenceEquals(catalog, ResourceLoader.GetCachedRef<OptimizedTranslation>(path)), "Malformed replacement does not publish.");
                Verify(catalog, "fr-CA", "MMMMM");
            }
        }
        finally { IOFile.WriteAllBytes(physical, validFile); }
        Check(ReferenceEquals(catalog, ResourceLoader.Load<OptimizedTranslation>(path, ResourceLoader.CacheMode.Replace))
            && changes == 1, "Valid retry replaces the staged data once in the same identity.");
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        catch (AggregateException errors) when (errors.Flatten().InnerExceptions.Any(error => error is T)) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
