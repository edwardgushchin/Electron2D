using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

internal readonly record struct SystemFontMatch(string Path, int FaceIndex, string Family);

// Owns an optional host Fontconfig catalog. Discovery is cold; source owners retain prepared fallback faces.
internal sealed partial class NativeSystemFonts
{
#if IOS || TVOS || ANDROID || ELECTRON2D_BROWSER_NATIVE
    internal string[] Names() => [];
    internal SystemFontMatch[] Match(string family, string text, string locale, int weight, int stretch, bool italic) => [];
#else
    private readonly object _gate = new();
    private nint _config;
    private bool _initialized;
    private readonly Dictionary<(string Family, string Text, string Locale, int Weight, int Stretch, bool Italic), SystemFontMatch[]> _matches = [];
    private bool Ensure()
    {
        if (_initialized) return _config != 0;
        _initialized = true;
        if (!OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) return false;
        try { _config = FcInitLoadConfigAndFonts(); }
        catch (Exception error) when (error is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException) { return false; }
        return _config != 0;
    }
    internal string[] Names()
    {
        lock (_gate)
        {
            if (!Ensure()) return [];
            var names = new HashSet<string>(StringComparer.Ordinal); nint pattern = 0, objects = 0, fonts = 0;
            try
            {
                pattern = FcPatternCreate(); objects = FcObjectSetCreate(); Require(pattern); Require(objects);
                Check(FcObjectSetAdd(objects, "family")); Check(FcObjectSetAdd(objects, "fontformat")); Check(FcPatternAddBool(pattern, "scalable", 1));
                fonts = FcFontList(_config, pattern, objects); if (fonts == 0) return [];
                var set = Marshal.PtrToStructure<FontSet>(fonts); ValidateSet(set);
                for (var i = 0; i < set.Count; i++)
                {
                    var font = Marshal.ReadIntPtr(set.Fonts, i * nint.Size); if (!Supported(font)) continue;
                    for (var slot = 0; FcPatternGetString(font, "family", slot, out var value) == 0; slot++)
                    { var name = Marshal.PtrToStringUTF8(value); if (!string.IsNullOrEmpty(name)) names.Add(name); if (slot >= 1024) throw new InvalidDataException("Font family aliases exceed the catalog budget."); }
                }
                return names.Order(StringComparer.Ordinal).ToArray();
            }
            finally { if (fonts != 0) FcFontSetDestroy(fonts); if (objects != 0) FcObjectSetDestroy(objects); if (pattern != 0) FcPatternDestroy(pattern); }
        }
    }
    internal SystemFontMatch[] Match(string family, string text, string locale, int weight, int stretch, bool italic)
    {
        lock (_gate)
        {
            if (!Ensure()) return [];
            var key = (family, text, locale, weight, stretch, italic); if (_matches.TryGetValue(key, out var cached)) return cached;
            nint pattern = 0, characters = 0, fonts = 0, coverage = 0;
            try
            {
                pattern = FcPatternCreate(); characters = FcCharSetCreate(); Require(pattern); Require(characters);
                Check(FcPatternAddBool(pattern, "scalable", 1)); Check(FcPatternAddString(pattern, "family", family));
                Check(FcPatternAddInteger(pattern, "weight", FcWeightFromOpenType(Math.Clamp(weight, 100, 999)))); Check(FcPatternAddInteger(pattern, "width", Math.Clamp(stretch, 50, 200))); Check(FcPatternAddInteger(pattern, "slant", italic ? 100 : 0));
                if (locale.Length > 0) Check(FcPatternAddString(pattern, "lang", locale));
                foreach (var rune in text.EnumerateRunes()) Check(FcCharSetAddChar(characters, (uint)rune.Value));
                Check(FcPatternAddCharSet(pattern, "charset", characters)); Check(FcConfigSubstitute(_config, pattern, 0)); FcDefaultSubstitute(pattern);
                fonts = FcFontSort(_config, pattern, 0, out coverage, out _); if (fonts == 0) return [];
                var set = Marshal.PtrToStructure<FontSet>(fonts); ValidateSet(set); var result = new List<SystemFontMatch>(); var remaining = new HashSet<uint>();
                foreach (var rune in text.EnumerateRunes()) remaining.Add((uint)rune.Value);
                for (var i = 0; i < set.Count && result.Count < 64; i++)
                {
                    var font = Marshal.ReadIntPtr(set.Fonts, i * nint.Size); if (!Supported(font)) continue;
                    var path = String(font, "file"); if (path.Length == 0) continue;
                    if (remaining.Count > 0)
                    {
                        if (FcPatternGetCharSet(font, "charset", 0, out var chars) != 0) continue;
                        if (!remaining.Any(scalar => FcCharSetHasChar(chars, scalar) != 0)) continue;
                        remaining.RemoveWhere(scalar => FcCharSetHasChar(chars, scalar) != 0);
                    }
                    FcPatternGetInteger(font, "index", 0, out var face); if (face < 0) continue;
                    var match = new SystemFontMatch(path, face, String(font, "family")); if (!result.Contains(match)) result.Add(match);
                    if (remaining.Count == 0) break;
                }
                var values = result.ToArray();
                // ponytail: clear cold request keys above 4096; use per-entry LRU if cold catalog workloads outgrow this bound.
                if (_matches.Count >= 4096) _matches.Clear(); _matches[key] = values; return values;
            }
            finally { if (coverage != 0) FcCharSetDestroy(coverage); if (fonts != 0) FcFontSetDestroy(fonts); if (characters != 0) FcCharSetDestroy(characters); if (pattern != 0) FcPatternDestroy(pattern); }
        }
    }
    private static bool Supported(nint font) => String(font, "fontformat") is "TrueType" or "CFF";
    private static string String(nint pattern, string name) => FcPatternGetString(pattern, name, 0, out var value) == 0 ? Marshal.PtrToStringUTF8(value) ?? "" : "";
    private static void Require(nint value) { if (value == 0) throw new OutOfMemoryException("Font catalog allocation failed."); }
    private static void Check(int value) { if (value == 0) throw new InvalidOperationException("Font catalog rejected a request."); }
    private static void ValidateSet(FontSet set) { if (set.Count < 0 || set.Count > 65536 || set.Count > 0 && set.Fonts == 0) throw new InvalidDataException("Font catalog exceeds its budget."); }
    ~NativeSystemFonts() { if (_config != 0 && !Environment.HasShutdownStarted) try { FcConfigDestroy(_config); } catch { } }
    [StructLayout(LayoutKind.Sequential)] private struct FontSet { internal int Count, Capacity; internal nint Fonts; }
    private const string Library = "libfontconfig.so.1";
    [LibraryImport(Library), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial nint FcInitLoadConfigAndFonts();
    [LibraryImport(Library), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void FcConfigDestroy(nint config);
    [LibraryImport(Library), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial nint FcPatternCreate();
    [LibraryImport(Library), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void FcPatternDestroy(nint pattern);
    [LibraryImport(Library), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial nint FcObjectSetCreate();
    [LibraryImport(Library), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void FcObjectSetDestroy(nint set);
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int FcObjectSetAdd(nint set, string value);
    [LibraryImport(Library), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial nint FcFontList(nint config, nint pattern, nint objects);
    [LibraryImport(Library), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void FcFontSetDestroy(nint set);
    [LibraryImport(Library), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial nint FcFontSort(nint config, nint pattern, int trim, out nint coverage, out int result);
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int FcPatternAddString(nint pattern, string name, string value);
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int FcPatternAddInteger(nint pattern, string name, int value);
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int FcPatternAddBool(nint pattern, string name, int value);
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int FcPatternAddCharSet(nint pattern, string name, nint value);
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int FcPatternGetString(nint pattern, string name, int index, out nint value);
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int FcPatternGetInteger(nint pattern, string name, int index, out int value);
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int FcPatternGetCharSet(nint pattern, string name, int index, out nint value);
    [LibraryImport(Library), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial nint FcCharSetCreate();
    [LibraryImport(Library), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void FcCharSetDestroy(nint value);
    [LibraryImport(Library), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int FcCharSetAddChar(nint chars, uint value);
    [LibraryImport(Library), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int FcCharSetHasChar(nint chars, uint value);
    [LibraryImport(Library), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int FcConfigSubstitute(nint config, nint pattern, int kind);
    [LibraryImport(Library), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial void FcDefaultSubstitute(nint pattern);
    [LibraryImport(Library), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial int FcWeightFromOpenType(int weight);
#endif
}
