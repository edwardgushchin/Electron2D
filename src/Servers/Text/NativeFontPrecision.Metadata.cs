using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Electron2D;

internal sealed unsafe partial class NativeFontPrecision
{
    internal Dictionary<uint, FontFeatureInfo> SupportedFeatures { get; } = [];
    internal Dictionary<string, Dictionary<string, string>> NameStrings { get; private set; } = new(StringComparer.Ordinal);

    private bool _namesRead;
    internal void ReadNameMetadata()
    {
        EnsureOwner();
        if (_namesRead) return;
        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        var textBytes = 0;
        var names = HBOTNameListNames(_hbFace, out var count);
        if (count > ushort.MaxValue || names == null && count != 0) throw new InvalidDataException("Invalid font name entry count.");
        for (var i = 0u; i < count; i++)
        {
            var entry = names[i]; var text = ReadName(entry.NameID, entry.Language);
            if (text.Length == 0) continue;
            CheckMetadataTextBudget(text, ref textBytes);
            var language = Marshal.PtrToStringUTF8(HBLanguageToString(entry.Language)) ?? string.Empty;
            if (!result.TryGetValue(language, out var values)) result.Add(language, values = new(StringComparer.Ordinal));
            values[NameKey(entry.NameID)] = text;
        }
        NameStrings = result; _namesRead = true;
    }

    private void ReadMetadata()
    {
        var textBytes = 0;
        var count = 0u;
        var labelLanguage = HBLanguageFromString(TranslationServer.GetToolLocale(), -1);
        foreach (var table in new uint[] { 0x47535542, 0x47504f53 })
        {
            count = 0;
            var total = HBOTLayoutTableGetFeatureTags(_hbFace, table, 0, ref count, null);
            if (total > ushort.MaxValue) throw new InvalidDataException("Invalid font feature entry count.");
            var tags = new uint[total]; count = total;
            fixed (uint* values = tags) HBOTLayoutTableGetFeatureTags(_hbFace, table, 0, ref count, values);
            if (count > total) throw new InvalidDataException("Font features changed during capture.");
            for (var i = 0u; i < count; i++)
            {
                uint labelID;
                var label = HBOTLayoutFeatureGetNameIDs(_hbFace, table, i, &labelID, null, null, null, null) != 0 ? ReadName(labelID, labelLanguage) : null;
                if (label is not null) CheckMetadataTextBudget(label, ref textBytes);
                SupportedFeatures[tags[i]] = OpenTypeFeatureTags.Describe(tags[i], label);
            }
        }
    }

    internal static void CheckMetadataTextBudget(string text, ref int bytes)
    {
        if (text.Length > (64 * 1024 * 1024 - bytes) / 2) throw new InvalidDataException("Font metadata text exceeds 64 MiB.");
        bytes += text.Length * 2;
    }

    private string ReadName(uint id, nint language)
    {
        var total = HBOTNameGetUTF8(_hbFace, id, language, null, null);
        if (total > ushort.MaxValue * 4u) throw new InvalidDataException("Font name text exceeds its portable table range.");
        if (total == 0) return string.Empty;
        var bytes = new byte[total + 1]; var length = (uint)bytes.Length;
        fixed (byte* buffer = bytes) HBOTNameGetUTF8(_hbFace, id, language, &length, buffer);
        if (length > total) throw new InvalidDataException("Font name text changed during capture.");
        return Encoding.UTF8.GetString(bytes.AsSpan(0, (int)length));
    }

    private static string NameKey(uint id) => id switch
    {
        0 => "copyright",
        1 => "family_name",
        2 => "subfamily_name",
        3 => "unique_identifier",
        4 => "full_name",
        5 => "version",
        6 => "postscript_name",
        7 => "trademark",
        8 => "manufacturer",
        9 => "designer",
        10 => "description",
        11 => "vendor_url",
        12 => "designer_url",
        13 => "license",
        14 => "license_url",
        16 => "typographic_family_name",
        17 => "typographic_subfamily_name",
        18 => "full_name_macos",
        19 => "sample_text",
        20 => "cid_findfont_name",
        21 => "weight_width_slope_family_name",
        22 => "weight_width_slope_subfamily_name",
        23 => "light_background_palette",
        24 => "dark_background_palette",
        25 => "postscript_name_prefix",
        _ => "unknown_" + id.ToString(CultureInfo.InvariantCulture)
    };

    [StructLayout(LayoutKind.Sequential)] private struct HBNameEntry { internal uint NameID, Reserved; internal nint Language; }
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_ot_name_list_names"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial HBNameEntry* HBOTNameListNames(nint face, out uint count);
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_ot_name_get_utf8"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial uint HBOTNameGetUTF8(nint face, uint id, nint language, uint* length, byte* text);
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_language_to_string"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint HBLanguageToString(nint language);
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_ot_layout_table_get_feature_tags"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial uint HBOTLayoutTableGetFeatureTags(nint face, uint table, uint start, ref uint count, uint* tags);
    [LibraryImport(NativeLibraries.HarfBuzzLibrary, EntryPoint = "hb_ot_layout_feature_get_name_ids"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int HBOTLayoutFeatureGetNameIDs(nint face, uint table, uint index, uint* label, uint* tooltip, uint* sample, uint* parameters, uint* firstParameter);
}
