using Path = System.IO.Path;
using Electron2D;

internal static class FontFileTests
{
    internal static void Run(byte[] openSansData, byte[] arabicData)
    {
        VerifyDefaultsAndConfiguration();
        VerifyDeferredEmbeddedData(openSansData);
        VerifyTransactionalData(openSansData, arabicData);
        VerifyFeatureAliases(openSansData);
        VerifySharedDataPolicyLock(openSansData);
        VerifyFileLoadingAndReset(openSansData);
        VerifyCopiesAndDescriptors(openSansData);
        VerifyWarmedConfiguration();
        Console.WriteLine("FontFile defaults, typed settings, metadata, transactional data/file loading, resource copies, observers and warmed configuration passed.");
    }

    private static void VerifyDefaultsAndConfiguration()
    {
        using var font = new FontFile();
        Check(font.Data.Length == 0 && font.GetFaceCount() == 0 && font.FontName.Length == 0 && font.StyleName.Length == 0 && font.FontWeight == 400 && font.FontStretch == 100,
            "An empty font has usable metadata without requiring native initialization.");
        Check(font.Hinting == FontHinting.Light && font.SubpixelPositioning == FontSubpixelPositioning.Auto && font.KeepRoundingRemainders && font.Oversampling == 0 && !font.ModulateColorGlyphs && font.OpenTypeFeatureOverrides.Count == 0,
            "Constructor defaults differ intentionally from file-load subpixel defaults.");
        var changed = 0; font.Changed += _ => changed++;
        font.FontName = "Custom family"; font.StyleName = "Custom style"; font.FontStyle = (FontStyle)123;
        font.FontWeight = int.MinValue; font.FontStretch = int.MaxValue;
        Check(font.GetFontName() == "Custom family" && font.GetFontStyleName() == "Custom style" && (int)font.GetFontStyle() == 123 && font.GetFontWeight() == 100 && font.GetFontStretch() == 200 && changed == 0,
            "Metadata overrides are descriptive, clamp numeric classes and remain silent.");
        font.FontWeight = int.MaxValue; font.FontStretch = int.MinValue;
        Check(font.FontWeight == 999 && font.FontStretch == 50, "Both ends of metadata clamps execute.");
        font.Hinting = FontHinting.Light; font.SubpixelPositioning = FontSubpixelPositioning.Auto; font.KeepRoundingRemainders = true; font.Oversampling = 0; font.ModulateColorGlyphs = false;
        Check(changed == 0, "Equal configuration assignments are silent.");
        Check((int)FontSubpixelPositioning.HalfMaxSize == 20 && (int)FontSubpixelPositioning.QuarterMaxSize == 16,
            "Automatic raster phase thresholds retain their public source values independently of positioning modes.");
        font.Hinting = (FontHinting)42; font.SubpixelPositioning = (FontSubpixelPositioning)44; font.KeepRoundingRemainders = false; font.Oversampling = -2; font.ModulateColorGlyphs = true;
        Check(changed == 5 && (int)font.Hinting == 42 && (int)font.SubpixelPositioning == 44 && font.Oversampling == -2, "Raw enum and finite signed oversampling values are preserved.");
        Reject<ArgumentOutOfRangeException>(() => font.Oversampling = float.NaN);
        Reject<ArgumentOutOfRangeException>(() => font.Oversampling = float.PositiveInfinity);
        Reject<ArgumentNullException>(() => font.FontName = null!);
        Reject<ArgumentNullException>(() => font.StyleName = null!);
        var overrides = new Dictionary<string, int> { ["liga"] = 0, ["kern"] = -1 };
        font.OpenTypeFeatureOverrides = overrides; overrides["liga"] = 1;
        var snapshot = font.OpenTypeFeatureOverrides; snapshot.Clear();
        Check(font.OpenTypeFeatureOverrides["liga"] == 0 && font.OpenTypeFeatureOverrides["kern"] == -1 && changed == 6, "Feature dictionaries are copied on both assignment and query.");
        Reject<ArgumentNullException>(() => font.OpenTypeFeatureOverrides = null!);
        Check(font.OpenTypeFeatureOverrides.Count == 2 && changed == 6, "Rejected null features leave previous settings and notifications untouched.");
    }

    private static void VerifyFeatureAliases(byte[] bytes)
    {
        Check(OpenTypeFeatureTags.Resolve("standard_ligatures") == 0x6C696761 && OpenTypeFeatureTags.Resolve("kerning") == 0x6B65726E,
            "Readable registered names resolve to their exact OpenType tags.");
        Check(OpenTypeFeatureTags.Resolve("character_variant_01") == 0x63763031 && OpenTypeFeatureTags.Resolve("character_variant_99") == 0x63763939 &&
            OpenTypeFeatureTags.Resolve("stylistic_set_01") == 0x73733031 && OpenTypeFeatureTags.Resolve("stylistic_set_20") == 0x73733230,
            "Numbered character variants and stylistic sets preserve their two-digit tag suffixes.");
        Check(OpenTypeFeatureTags.Resolve("character_variant_00") == 0x63686172 && OpenTypeFeatureTags.Resolve("stylistic_set_21") == 0x7374796C,
            "Names outside registered numbered ranges follow the unknown-name conversion.");
        Check(OpenTypeFeatureTags.Resolve("ZZZZ") == 0x5A5A5A5A && OpenTypeFeatureTags.Resolve("custom_liga") == 0x6C696761 &&
            OpenTypeFeatureTags.Resolve("custom_acustom_b") == 0x61622020 && OpenTypeFeatureTags.Resolve("abcde") == 0x61626364 &&
            OpenTypeFeatureTags.Resolve("a\0bc") == 0x61202020 && OpenTypeFeatureTags.Resolve("éx") == 0x20782020 && OpenTypeFeatureTags.Resolve("") == 0,
            "Unknown tags retain native truncation, padding, ASCII substitution, global custom_ removal and NUL behavior.");
        using var font = new FontFile { Data = bytes };
        uint[] text = ['f', 'f', 'i']; var glyphs = new List<NativeShapedGlyph>();
        font.OpenTypeFeatureOverrides = new() { ["standard_ligatures"] = 0, ["ZZZZ"] = 17 };
        font.PrimaryData!.Shape(text, 0, text.Length, 16, NativeTextDirection.LTR, 0x4C61746E, "en", glyphs);
        var aliasResult = glyphs.ToArray();
        Check(aliasResult.Length == 3 && font.OpenTypeFeatureOverrides["ZZZZ"] == 17, "Readable aliases affect glyph shaping while unknown raw feature tags remain stored.");
        font.OpenTypeFeatureOverrides = new() { ["liga"] = 0 };
        font.PrimaryData!.Shape(text, 0, text.Length, 16, NativeTextDirection.LTR, 0x4C61746E, "en", glyphs);
        Check(glyphs.SequenceEqual(aliasResult), "Readable and raw tag overrides produce the same glyph output.");
        font.OpenTypeFeatureOverrides = new() { ["standard_ligatures"] = -1 };
        font.PrimaryData!.Shape(text, 0, text.Length, 16, NativeTextDirection.LTR, 0x4C61746E, "en", glyphs);
        Check(glyphs.Count == 1 && glyphs[0].GlyphIndex == 909 && font.OpenTypeFeatureOverrides["standard_ligatures"] == -1,
            "Negative feature values are retained as data but do not override default shaping.");
    }

    private static void VerifyDeferredEmbeddedData(byte[] bytes)
    {
        using var deferred = new FontFile([1, 2, 3]);
        deferred.Hinting = FontHinting.None;
        Check(deferred.Data.SequenceEqual(new byte[] { 1, 2, 3 }) && deferred.Hinting == FontHinting.None,
            "Embedded font construction and configuration preserve bytes without touching native decoders.");
        using var copy = (FontFile)deferred.Duplicate();
        Check(copy.Data.AsSpan().SequenceEqual(deferred.Data), "Copying deferred embedded font state also avoids native initialization.");
        Reject<InvalidDataException>(() => _ = deferred.FontName);
        Reject<InvalidDataException>(() => copy.GetSupportedChars());
        deferred.Data = bytes;
        Check(deferred.FontName == "Open Sans" && deferred.Hinting == FontHinting.None, "An eager validated replacement recovers after failed deferred initialization.");
        var mutable = (byte[])bytes.Clone();
        using var valid = new FontFile(mutable); Array.Clear(mutable);
        Check(valid.GetFontName() == "Open Sans" && valid.GetFaceCount() == 1 && valid.HasChar('A'), "Inherited metadata and glyph queries realize owned deferred source data.");
    }

    private static void VerifyTransactionalData(byte[] openSans, byte[] arabic)
    {
        using var font = new FontFile { Hinting = FontHinting.None, SubpixelPositioning = FontSubpixelPositioning.Quarter, Oversampling = -1, KeepRoundingRemainders = false, ModulateColorGlyphs = true };
        var original = (byte[])openSans.Clone(); var changed = 0;
        font.Changed += _ => changed++;
        font.Data = original; Array.Clear(original);
        Check(font.HasChar('A') && font.FontName == "Open Sans" && font.FontWeight == 600 && changed == 1, "Validated bytes become an owned live scalable font.");
        Check(font.Hinting == FontHinting.None && font.SubpixelPositioning == FontSubpixelPositioning.Quarter && font.Oversampling == -1 && !font.KeepRoundingRemainders && font.ModulateColorGlyphs,
            "Data assignment preserves current shaping and raster configuration.");
        var returned = font.Data; returned[0] ^= 255;
        Check(font.Data.AsSpan().SequenceEqual(openSans), "Returned font data cannot mutate the native source.");
        var before = font.GetCharSize('A', 16); var primary = font.PrimaryData;
        Reject<InvalidDataException>(() => font.Data = [0, 1, 2, 3]);
        Reject<ArgumentNullException>(() => font.Data = null!);
        Check(ReferenceEquals(font.PrimaryData, primary) && font.Data.AsSpan().SequenceEqual(openSans) && font.GetCharSize('A', 16) == before && changed == 1,
            "Invalid source data leaves the previous native face, bytes, metrics and event state intact.");
        Reject<InvalidDataException>(() => font.Data = new byte[64 * 1024 * 1024 + 1]);
        var outsideLock = false;
        void Observer(Resource _)
        {
            var read = Task.Run(() => font.FontName);
            outsideLock = read.Wait(TimeSpan.FromSeconds(5));
            Check(outsideLock, "Changed must be delivered outside FontGate.");
        }
        font.Changed += Observer; font.Data = arabic; font.Changed -= Observer;
        Check(outsideLock && font.HasChar(0x628), "A replacement publishes a usable new font before Changed observers execute.");
        Reject<ObjectDisposedException>(() => primary!.GetMetrics(16));
        void Fail(Resource _) => throw new MarkerException();
        font.Changed += Fail;
        Reject<MarkerException>(() => font.Data = openSans);
        font.Changed -= Fail;
        Check(font.FontName == "Open Sans" && font.HasChar('A'), "Observer failure does not roll back a successful replacement.");
        font.Data = [];
        Check(font.GetFaceCount() == 0 && !font.HasChar('A') && font.Hinting == FontHinting.None, "Empty data clears the face but preserves configuration.");
    }

    private static void VerifyFileLoadingAndReset(byte[] bytes)
    {
        var directory = Path.Combine(Path.GetTempPath(), "electron2d-fontfile-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            var valid = Path.Combine(directory, "valid.woff2"); File.WriteAllBytes(valid, bytes);
            var corrupt = Path.Combine(directory, "corrupt.ttf"); File.WriteAllBytes(corrupt, [1, 2, 3]);
            var huge = Path.Combine(directory, "huge.ttf"); using (var stream = File.Create(huge)) stream.SetLength(64L * 1024 * 1024 + 1);
            using var fallback = new FontFile();
            using var font = new FontFile
            {
                Data = bytes,
                FontName = "override",
                Hinting = FontHinting.None,
                SubpixelPositioning = FontSubpixelPositioning.Quarter,
                KeepRoundingRemainders = false,
                Oversampling = 2,
                ModulateColorGlyphs = true,
                Fallbacks = [fallback],
                OpenTypeFeatureOverrides = new() { ["liga"] = 0 }
            };
            Reject<InvalidDataException>(() => font.LoadDynamicFont(corrupt));
            Reject<InvalidDataException>(() => font.LoadDynamicFont(huge));
            Reject<IOException>(() => font.LoadDynamicFont(Path.Combine(directory, "missing.ttf")));
            Check(font.FontName == "override" && font.Hinting == FontHinting.None && font.Oversampling == 2 && font.Data.AsSpan().SequenceEqual(bytes), "File read/decode failures do not reset existing state.");
            var changes = 0; font.Changed += _ => changes++;
            font.LoadDynamicFont(valid);
            Check(changes == 1 && font.FontName == "Open Sans" && font.Hinting == FontHinting.Light && font.SubpixelPositioning == FontSubpixelPositioning.Disabled && font.KeepRoundingRemainders && font.Oversampling == 0 && !font.ModulateColorGlyphs,
                "Successful file loading atomically applies intrinsic metadata and reset-state configuration.");
            Check(ReferenceEquals(font.Fallbacks[0], fallback) && font.OpenTypeFeatureOverrides["liga"] == 0, "File reset preserves fallback and feature configuration.");
            font.ResetState();
            Check(font.Data.Length == 0 && font.SubpixelPositioning == FontSubpixelPositioning.Disabled && font.OpenTypeFeatureOverrides.Count == 1 && ReferenceEquals(font.Fallbacks[0], fallback), "ResetState clears data and transient native state while preserving fallback/features.");
        }
        finally { Directory.Delete(directory, true); }
    }

    private static void VerifyCopiesAndDescriptors(byte[] bytes)
    {
        using var fallback = new FontFile();
        using var source = new FontFile
        {
            Data = bytes,
            FontName = "Named",
            StyleName = "Custom",
            FontWeight = 555,
            FontStretch = 123,
            FontStyle = FontStyle.Italic,
            Hinting = FontHinting.Normal,
            SubpixelPositioning = FontSubpixelPositioning.Half,
            KeepRoundingRemainders = false,
            Oversampling = 1.5f,
            ModulateColorGlyphs = true,
            OpenTypeFeatureOverrides = new() { ["liga"] = 0 },
            Fallbacks = [fallback, null, fallback]
        };
        using var shallow = (FontFile)source.Duplicate(); using var deep = (FontFile)source.Duplicate(true);
        Check(shallow.GetType() == typeof(FontFile) && !ReferenceEquals(shallow.PrimaryData, source.PrimaryData) && shallow.Data.AsSpan().SequenceEqual(bytes), "Shallow duplicates own independent native font caches and immutable source views.");
        Check(shallow.FontName == "Named" && shallow.StyleName == "Custom" && shallow.FontWeight == 555 && shallow.FontStretch == 123 && shallow.FontStyle == FontStyle.Italic && shallow.Hinting == FontHinting.Normal && shallow.SubpixelPositioning == FontSubpixelPositioning.Half && !shallow.KeepRoundingRemainders && shallow.Oversampling == 1.5f && shallow.ModulateColorGlyphs && shallow.OpenTypeFeatureOverrides["liga"] == 0,
            "Resource duplication preserves exact metadata and every stored font setting.");
        Check(ReferenceEquals(shallow.Fallbacks[0], fallback) && !ReferenceEquals(deep.Fallbacks[0], fallback) && ReferenceEquals(deep.Fallbacks[0], deep.Fallbacks[2]), "Resource graph policy preserves repeated fallback aliases and deep-copy ownership.");
        using var target = new FontFile(); var changes = 0; target.Changed += _ => changes++; target.CopyFromResource(source);
        Check(changes == 1 && target.FontName == "Named" && target.OpenTypeFeatureOverrides.Count == 1, "CopyFromResource batches the reset and exact custom state into one Changed event.");
        var properties = source.GetPropertyList();
        var subpixel = properties.OfType<PropertyDescriptor<FontFile, FontSubpixelPositioning>>().Single();
        Check(subpixel.IsStored && subpixel.TryGetRevertValue(source, out var revert) && revert == FontSubpixelPositioning.Auto, "The stored typed descriptor retains constructor rather than file-load defaults.");
        subpixel.Revert(source); Check(source.SubpixelPositioning == FontSubpixelPositioning.Auto, "Typed property revert executes the setting behavior.");
        source.Data = [];
        Check(shallow.HasChar('A') && target.HasChar('A'), "Replacing source data leaves duplicated native resources operational.");
        using var derived = new UnhandledFontFile(); Reject<NotSupportedException>(() => derived.Duplicate());
        deep.Fallbacks[0]!.Dispose();
        target.Dispose(); Reject<ObjectDisposedException>(() => target.Data = bytes); Reject<ObjectDisposedException>(() => target.Hinting = FontHinting.Light);
    }

    private static void VerifyWarmedConfiguration()
    {
        using var font = new FontFile();
        void Cycle(int i)
        {
            font.Hinting = (i & 1) == 0 ? FontHinting.Light : FontHinting.None;
            font.SubpixelPositioning = (i & 1) == 0 ? FontSubpixelPositioning.Auto : FontSubpixelPositioning.Disabled;
            font.KeepRoundingRemainders = (i & 1) == 0; font.Oversampling = i & 1; font.ModulateColorGlyphs = (i & 1) != 0;
        }
        for (var i = 0; i < 64; i++) Cycle(i);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++) Cycle(i);
        Check(GC.GetAllocatedBytesForCurrentThread() - before == 0, "Warmed scalar setting changes and font invalidation allocate no managed bytes.");
    }
    private static void VerifySharedDataPolicyLock(byte[] bytes)
    {
        using var font = new FontFile { Data = bytes };
        var data = font.PrimaryData!;
        using var started = new ManualResetEventSlim();
        Task update;
        lock (data.Gate)
        {
            update = Task.Run(() => { started.Set(); font.Hinting = FontHinting.None; });
            Check(started.Wait(TimeSpan.FromSeconds(5)), "The shared-data policy writer started.");
            Check(!update.Wait(50) && data.Hinting == FontHinting.Light, "Policy mutation waits for the same lock held during a fallback raster operation.");
        }
        Check(update.Wait(TimeSpan.FromSeconds(5)) && font.Hinting == FontHinting.None, "Policy mutation commits after the native operation releases its snapshot.");
        started.Reset();
        lock (data.Gate)
        {
            update = Task.Run(() => { started.Set(); font.OpenTypeFeatureOverrides = new() { ["liga"] = 0 }; });
            Check(started.Wait(TimeSpan.FromSeconds(5)), "The shared-data feature writer started.");
            Check(!update.Wait(50) && data.OpenTypeFeatures.Length == 0, "Feature mutation cannot replace the native argument snapshot during shaping.");
        }
        Check(update.Wait(TimeSpan.FromSeconds(5)) && data.OpenTypeFeatures.Length == 1, "Feature mutation commits after shaping ownership is released.");
    }
    private sealed class MarkerException : Exception { }
    private sealed class UnhandledFontFile : FontFile { }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
