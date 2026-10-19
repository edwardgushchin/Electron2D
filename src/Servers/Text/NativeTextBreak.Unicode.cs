using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Electron2D;

internal static unsafe partial class NativeTextBreak
{
    internal const int MaximumStringUnits = 33554432;
    internal static void ValidateString(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length > MaximumStringUnits) throw new ArgumentException("Text exceeds 64 MiB of UTF-16.", nameof(text));
    }
    internal static string TransformString(string text, string language, int operation)
    {
        ValidateString(text); ValidateString(language); if (text.Length == 0) return text;
        var locale = operation == 2 ? [] : Encoding.UTF8.GetBytes((language.Length == 0 ? TranslationServer.GetToolLocale() : language) + '\0');
        lock (Gate)
        {
            Initialize();
            fixed (char* source = text)
            fixed (byte* name = locale)
            {
                var count = operation == 2 ? StripDiacriticsNative(source, text.Length, null, 0, out var error) : ChangeCaseNative(source, text.Length, name, operation, null, 0, out error);
                if (error != 15) Check(error);
                if (count < 0 || count > MaximumStringUnits) throw new ArgumentException("The result exceeds 64 MiB of UTF-16.", nameof(text));
                if (count == 0) return string.Empty;
                var output = new char[count];
                fixed (char* destination = output)
                {
                    var written = operation == 2 ? StripDiacriticsNative(source, text.Length, destination, count, out error) : ChangeCaseNative(source, text.Length, name, operation, destination, count, out error);
                    Check(error);
                    if (written < 0 || written > count) throw new InvalidOperationException("Native text output exceeded its prepared buffer.");
                    return output.AsSpan(0, written).SequenceEqual(text) ? text : new string(output, 0, written);
                }
            }
        }
    }
    internal static bool IsIdentifier(string text)
    {
        ValidateString(text); if (text.Length == 0) return false;
        lock (Gate)
        {
            Initialize(); fixed (char* source = text) { var result = IdentifierNative(source, text.Length, out var error); Check(error); return result != 0; }
        }
    }
    internal static bool IsLetter(int scalar)
    {
        if (!Rune.IsValid(scalar)) return false;
        lock (Gate) { Initialize(); return (CharacterProperties((uint)scalar) & 1) != 0; }
    }
    internal static int[] StringCharacterBreaks(string text, string language)
    {
        ValidateString(text); ValidateString(language); if (text.Length == 0) return [];
        lock (Gate) { Initialize(); return StringBoundaries(text, language, 0); }
    }
    private static int[] StringBoundaries(string text, string language, int kind)
    {
        var locale = language.Length == 0 ? TranslationServer.GetToolLocale() : language;
        var encoded = Encoding.UTF8.GetBytes(locale + '\0'); var result = new List<int>(); var cursor = 0; var scalar = 0;
        fixed (char* source = text)
        fixed (byte* name = encoded)
        {
            var iterator = OpenNative(kind, name, source, text.Length, out var error);
            if (error > 0 || iterator == 0) { if (iterator != 0) Close(iterator); Check(error); throw new InvalidOperationException("Could not create a native text boundary iterator."); }
            try
            {
                First(iterator);
                for (var position = Next(iterator); position >= 0; position = Next(iterator))
                {
                    while (cursor < position) { cursor += char.IsHighSurrogate(text[cursor]) && cursor + 1 < text.Length && char.IsLowSurrogate(text[cursor + 1]) ? 2 : 1; scalar++; }
                    if (cursor != position) throw new InvalidOperationException("A native boundary splits a Unicode scalar.");
                    result.Add(scalar);
                }
            }
            finally { Close(iterator); }
        }
        return result.ToArray();
    }
    internal static int[] StringWordBreaks(string text, string language, int charsPerLine)
    {
        ValidateString(text); ValidateString(language); if (text.Length == 0) return [];
        var count = 0; foreach (var rune in text.EnumerateRunes()) count++;
        var scalars = new uint[count]; var at = 0; foreach (var rune in text.EnumerateRunes()) scalars[at++] = (uint)rune.Value;
        lock (Gate)
        {
            Initialize(); var breaks = new bool[count + 1]; foreach (var end in StringBoundaries(text, language, 1)) breaks[end] = true;
            var ranges = new List<int>();
            if (charsPerLine <= 0)
            {
                var start = -1;
                for (var i = 0; i < count; i++)
                {
                    if (StringWordSeparator(scalars[i])) { AppendStringRange(ranges, start, i); start = -1; }
                    else
                    {
                        if (breaks[i]) { AppendStringRange(ranges, start, i); start = i; }
                        else if (start < 0) start = i;
                    }
                }
                AppendStringRange(ranges, start, count);
            }
            else
            {
                var start = 0;
                for (var i = 0; i <= count; i++)
                {
                    if (i != count && !StringHardBreak(scalars[i])) continue;
                    WrapStringRange(scalars, breaks, ranges, start, i, charsPerLine); start = i + 1;
                }
            }
            return ranges.ToArray();
        }
    }
    private static void WrapStringRange(uint[] text, bool[] breaks, List<int> ranges, int start, int end, int width)
    {
        while (start < end)
        {
            var limit = start + Math.Min(width, end - start);
            if (limit == end) { AppendStringRange(ranges, start, end); return; }
            var cut = start;
            for (var i = start; i <= limit; i++) if (breaks[i] || StringWordSeparator(text[i])) cut = i;
            if (cut == start) cut = limit;
            var trimmed = cut; while (trimmed > start && StringWhitespace(text[trimmed - 1])) trimmed--;
            AppendStringRange(ranges, start, trimmed);
            start = cut; while (start < end && StringWhitespace(text[start])) start++;
        }
    }
    private static void AppendStringRange(List<int> ranges, int start, int end) { if (start < 0 || start >= end) return; ranges.Add(start); ranges.Add(end); }
    private static bool StringHardBreak(uint scalar) => scalar is >= 0x0a and <= 0x0d or 0x85 or 0x2028 or 0x2029;
    internal static bool StringWhitespace(uint scalar) => scalar is >= 9 and <= 13 or 0x20 or 0x85 or 0xa0 or 0x1680 or >= 0x2000 and <= 0x200b or 0x2028 or 0x2029 or 0x202f or 0x205f or 0x3000;
    private static bool StringWordSeparator(uint scalar) => StringWhitespace(scalar) || scalar is '_' or 0xfffc || (CharacterProperties(scalar) & 2) != 0;

    [LibraryImport(Library, EntryPoint = "e2d_text_case"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int ChangeCaseNative(char* text, int length, byte* locale, int title, char* output, int capacity, out int error);
    [LibraryImport(Library, EntryPoint = "e2d_text_strip_diacritics"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int StripDiacriticsNative(char* text, int length, char* output, int capacity, out int error);
    [LibraryImport(Library, EntryPoint = "e2d_text_character_properties"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial uint CharacterProperties(uint scalar);
    [LibraryImport(Library, EntryPoint = "e2d_text_is_identifier"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int IdentifierNative(char* text, int length, out int error);
}
