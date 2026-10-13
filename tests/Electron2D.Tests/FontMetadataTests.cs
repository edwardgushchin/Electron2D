using Electron2D;
using System.Globalization;

internal static partial class FontMetadataTests
{
    private const uint SS01 = 0x73733031, Kern = 0x6b65726e, Required = 0x726c6967, Unknown = 0x7a7a7a7a;
    internal static void Run()
    {
        using var empty = new FontFile();
        Check(empty.GetSupportedFeatureList().Count == 0 && empty.GetOTNameStrings().Count == 0, "Empty fonts do not infer metadata or initialize a face.");
        using var font = new FontFile { Data = FontTestFixtures.Metadata };
        var features = font.GetSupportedFeatureList();
        Check(features.Keys.Order().SequenceEqual(new[] { SS01, Kern, Required, Unknown }.Order()), "GSUB and GPOS tags match the independent fixture tables.");
        Check(features[SS01] == new FontFeatureInfo("Wide A", true, false), "Font-defined label and boolean selection metadata.");
        Check(features[Kern].IsBoolean && !features[Kern].Hidden && features[Kern].Label is null, "Ordinary boolean positioning feature.");
        Check(features[Required].IsBoolean && features[Required].Hidden, "Required shaping feature is hidden.");
        Check(!features[Unknown].IsBoolean && !features[Unknown].Hidden && features[Unknown].Label is null, "Unknown tags are visible integer selectors.");
        features.Clear(); Check(font.GetSupportedFeatureList().Count == 4, "Returned feature dictionary is independent.");
        var oldCulture = TranslationServer.Culture;
        using (var catalog = new Translation { Locale = "ja" })
        {
            try
            {
                TranslationServer.AddTranslation(catalog); TranslationServer.Culture = CultureInfo.GetCultureInfo("ja");
                Check(font.GetSupportedFeatureList()[SS01].Label == "Wide A", "Realized label locale is retained after a global change.");
                using var japanese = new FontFile { Data = FontTestFixtures.Metadata };
                Check(japanese.GetSupportedFeatureList()[SS01].Label == "幅広A", "New realization captures actual localized feature text.");
            }
            finally { TranslationServer.Culture = oldCulture; TranslationServer.RemoveTranslation(catalog); }
        }
        var names = font.GetOTNameStrings();
        Check(names["en"]["family_name"] == "Electron2D Metadata Test" && names["ja"]["family_name"] == "メタデータ試験", "Localized native names remain distinct.");
        Check(names["en"]["license"] == "Engine-owned test font" && names["en"]["unknown_400"] == "Unknown role Ω" && names["en"]["unknown_256"] == "Wide A", "Known/unknown name IDs and Unicode text.");
        Check(!names["en"].ContainsKey("unknown_401"), "Empty names are omitted.");
        names["en"].Clear(); names.Clear(); Check(font.GetOTNameStrings()["en"].Count > 5, "Outer and inner dictionaries are independent.");
        using var duplicate = new FontFile { Data = FontTestFixtures.MetadataDuplicate };
        Check(duplicate.GetSupportedFeatureList()[SS01].Label is null, "GPOS duplicate replaces GSUB description in table order.");
        using var oversized = new FontFile { Data = FontTestFixtures.MetadataBudget };
        Check(oversized.GetOTNameStrings().Count == 0, "Native name sanitization rejects an excessive indirect table without inferred metadata.");
        Check(oversized.GetSupportedFeatureList().ContainsKey(SS01) && oversized.GetStringSize("A").X > 0, "Rejected native name table leaves feature/shaping state usable.");
        var textBytes = 0; var chunk = new string('X', 32768);
        for (var i = 0; i < 1024; i++) NativeFontPrecision.CheckMetadataTextBudget(chunk, ref textBytes);
        Check(textBytes == 64 * 1024 * 1024, "Exact expanded-text boundary is accepted.");
        Throws<InvalidDataException>(() => NativeFontPrecision.CheckMetadataTextBudget("X", ref textBytes));
        Check(textBytes == 64 * 1024 * 1024, "Budget rejection does not commit a partial count.");
        using var fallback = new FontFile { Data = FontTestFixtures.Metadata }; empty.Fallbacks = [fallback];
        Check(empty.GetSupportedFeatureList().Count == 0 && empty.GetOTNameStrings().Count == 0, "Fallback metadata is not merged into an empty primary face.");
        font.FontName = "Authored override";
        Check(font.GetOTNameStrings()["en"]["family_name"] == "Electron2D Metadata Test", "Authored family overrides do not rewrite native names.");
        var normal = font.GetStringSize("A", fontSize: 20).X;
        font.OpenTypeFeatureOverrides = new() { ["ss01"] = 1 };
        Check(font.GetStringSize("A", fontSize: 20).X == normal * 2, "Discovered feature executes through ordinary text shaping.");
        using var variation = new FontVariation { BaseFont = font, OpenTypeFeatures = new() { ["ss01"] = 0 } };
        Check(variation.GetSupportedFeatureList()[SS01].IsBoolean && variation.GetOTNameStrings()["ja"]["family_name"] == "メタデータ試験" && variation.GetStringSize("A", fontSize: 20).X == normal, "Variation queries and shaping use its selected source.");
        for (var i = 0; i < 128; i++) _ = font.GetStringSize("A", fontSize: 20);
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 128; i++) Check(font.GetStringSize("A", fontSize: 20).X == normal * 2, "Warm existing consumer.");
        Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "Metadata realization adds no warmed shaping allocation.");
        Check(Task.Run(() => font.GetOTNameStrings()["en"]["full_name"]).GetAwaiter().GetResult() == "Electron2D Metadata Test", "Metadata queries keep serialized native ownership from a worker caller.");
        var retained = font.PrimaryData!;
        using (font.BeginRead())
        {
            font.Data = FontTestFixtures.Strike;
            Check(retained.GetNameStrings()["en"]["family_name"] == "Electron2D Metadata Test", "An active reader retains old metadata until its read ends.");
        }
        Check(retained.NameStrings.Count == 0 && retained.SupportedFeatures.Count == 0, "Retired native sources release metadata after the final reader.");
        Check(!font.GetSupportedFeatureList().ContainsKey(SS01) && font.GetOTNameStrings()["en"]["family_name"] != "Electron2D Metadata Test", "Replacing data retires old metadata.");
        font.Dispose(); Throws<ObjectDisposedException>(() => font.GetSupportedFeatureList()); Throws<ObjectDisposedException>(() => font.GetOTNameStrings());
        using var collection = new FontFile { Data = FontTestFixtures.Collection };
        using var second = new FontVariation { BaseFont = collection, VariationFaceIndex = 1 };
        Check(collection.GetOTNameStrings()["en"]["subfamily_name"] == "Thin" && second.GetOTNameStrings()["en"]["subfamily_name"] == "Heavy", "Selected collection face owns its own names.");
        using var system = new SystemFont { FontNames = ["Unavailable Metadata Fixture"] };
        Check(system.GetOTNameStrings().Count > 0, "System/theme realization exposes the actual chosen primary source.");
        Console.WriteLine("Font metadata tables, names, policy, copies, source changes, collections, variations, system fallback and active shaping passed.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
