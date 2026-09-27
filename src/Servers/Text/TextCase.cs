using System.Buffers;
using System.Text;

namespace Electron2D;

// Unicode 17 full uppercase, with locale tailoring from ICU 78.3.
// Unicode License V3 applies to the tables and adapted Greek rules; see tools/text-case.json.
internal static partial class TextCase
{
    private enum Locale { Default, Turkic, Lithuanian, Greek, Armenian }

    internal static string ToUpper(string text, string language = "")
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(language);
        if (text.Length == 0) return text;
        if (language.Length == 0) language = TranslationServer.GetToolLocale();
        var locale = GetLocale(language);
        if (locale == Locale.Greek) return GreekUpper(text);
        StringBuilder? result = null;
        var afterSoftDotted = false;
        for (var start = 0; start < text.Length;)
        {
            var scalar = ReadScalar(text, start, out var length);
            if (locale == Locale.Lithuanian && scalar == 0x307 && afterSoftDotted)
            {
                result ??= StartResult(text, start);
            }
            else
            {
                var expansion = locale == Locale.Armenian && scalar == 0x587 ? "\u0535\u054E" : Expansion(scalar);
                if (expansion is not null)
                {
                    result ??= StartResult(text, start); result.Append(expansion);
                }
                else
                {
                    var upper = locale == Locale.Turkic && scalar == 'i' ? 0x130u : SimpleUpper(scalar);
                    AppendScalar(ref result, text, start, length, scalar, upper);
                }
            }
            if (locale == Locale.Lithuanian)
            {
                if (!Contains(NonzeroCombiningRanges, scalar)) afterSoftDotted = Contains(SoftDottedRanges, scalar);
                else if (Contains(AboveCombiningRanges, scalar)) afterSoftDotted = false;
            }
            start += length;
        }
        return result?.ToString() ?? text;
    }

    private static Locale GetLocale(string language)
    {
        var end = language.AsSpan().IndexOfAny('_', '-', '\0');
        var code = end < 0 ? language.AsSpan() : language.AsSpan(0, end);
        if (code.Equals("tr", StringComparison.OrdinalIgnoreCase) || code.Equals("tur", StringComparison.OrdinalIgnoreCase) ||
            code.Equals("az", StringComparison.OrdinalIgnoreCase) || code.Equals("aze", StringComparison.OrdinalIgnoreCase)) return Locale.Turkic;
        if (code.Equals("lt", StringComparison.OrdinalIgnoreCase) || code.Equals("lit", StringComparison.OrdinalIgnoreCase)) return Locale.Lithuanian;
        if (code.Equals("el", StringComparison.OrdinalIgnoreCase) || code.Equals("ell", StringComparison.OrdinalIgnoreCase)) return Locale.Greek;
        if (code.Equals("hy", StringComparison.OrdinalIgnoreCase) || code.Equals("hye", StringComparison.OrdinalIgnoreCase)) return Locale.Armenian;
        return Locale.Default;
    }

    private static uint ReadScalar(string text, int start, out int length)
    {
        if (Rune.DecodeFromUtf16(text.AsSpan(start), out var rune, out length) == OperationStatus.Done) return (uint)rune.Value;
        length = 1; return text[start]; // Preserve an unpaired UTF-16 code unit rather than changing unrelated input.
    }

    private static uint SimpleUpper(uint scalar)
    {
        var pairs = UpperPairs; var low = 0; var high = pairs.Length / 2 - 1;
        while (low <= high)
        {
            var middle = (low + high) / 2; var value = pairs[middle * 2];
            if (scalar < value) high = middle - 1;
            else if (scalar > value) low = middle + 1;
            else return pairs[middle * 2 + 1];
        }
        return scalar;
    }

    private static bool Contains(ReadOnlySpan<uint> ranges, uint scalar)
    {
        var low = 0; var high = ranges.Length / 2 - 1;
        while (low <= high)
        {
            var middle = (low + high) / 2; var index = middle * 2;
            if (scalar < ranges[index]) high = middle - 1;
            else if (scalar > ranges[index + 1]) low = middle + 1;
            else return true;
        }
        return false;
    }

    private static StringBuilder StartResult(string text, int start) => new StringBuilder(text.Length).Append(text.AsSpan(0, start));

    private static void AppendScalar(ref StringBuilder? result, string text, int start, int length, uint original, uint upper)
    {
        if (result is null && original == upper) return;
        result ??= StartResult(text, start);
        if (original == upper) result.Append(text.AsSpan(start, length));
        else if (upper <= 0xFFFF) result.Append((char)upper);
        else
        {
            upper -= 0x10000;
            result.Append((char)(0xD800 + (upper >> 10))).Append((char)(0xDC00 + (upper & 1023)));
        }
    }

    // Greek uppercase tailoring adapted from ICU's UTF-16 GreekUpper implementation.
    // Copyright (C) 2016 and later: Unicode, Inc. Unicode License V3.
    private static string GreekUpper(string text)
    {
        const uint vowel = 0x1000, subscriptIota = 0x2000, accent = 0x4000, diaeresis = 0x8000, combiningDiaeresis = 0x10000;
        const uint eitherDiaeresis = diaeresis | combiningDiaeresis;
        StringBuilder? result = null; uint state = 0;
        for (var start = 0; start < text.Length;)
        {
            var scalar = ReadScalar(text, start, out var length); var end = start + length;
            uint nextState = Contains(CaseIgnorableRanges, scalar) ? state & 1u : Contains(CasedRanges, scalar) ? 1u : 0u;
            var data = GreekData(scalar);
            if (data != 0)
            {
                var upper = data & 0x3FF;
                if ((data & vowel) != 0 && (state & 6) != 0 && upper is 0x399 or 0x3A5)
                    data |= (state & 4) != 0 ? diaeresis : combiningDiaeresis;
                var iotas = (data & subscriptIota) != 0 ? 1 : 0;
                var precomposedAccent = (data & accent) != 0;
                while (end < text.Length)
                {
                    var next = ReadScalar(text, end, out var nextLength);
                    var diacritic = GreekDiacritic(next);
                    if (diacritic == 0) break;
                    data |= diacritic;
                    if ((diacritic & subscriptIota) != 0) iotas++;
                    end += nextLength;
                }
                if ((data & (vowel | accent | diaeresis)) == (vowel | accent)) nextState |= precomposedAccent ? 4u : 2u;
                var tonos = false;
                if (upper == 0x397 && (data & accent) != 0 && iotas == 0 && (state & 1) == 0 && !FollowedByCased(text, end))
                {
                    if (precomposedAccent) upper = 0x389;
                    else tonos = true;
                }
                else if ((data & diaeresis) != 0)
                {
                    if (upper is 0x399 or 0x3A5)
                    {
                        upper = upper == 0x399 ? 0x3AAu : 0x3ABu;
                        data &= ~eitherDiaeresis;
                    }
                }
                AppendGreek(ref result, text, start, end, (char)upper, (data & eitherDiaeresis) != 0, tonos, iotas);
            }
            else
            {
                var expansion = Expansion(scalar);
                if (expansion is not null) { result ??= StartResult(text, start); result.Append(expansion); }
                else AppendScalar(ref result, text, start, length, scalar, SimpleUpper(scalar));
            }
            start = end; state = nextState;
        }
        return result?.ToString() ?? text;
    }

    private static void AppendGreek(ref StringBuilder? result, string text, int start, int end, char upper, bool diaeresis, bool tonos, int iotas)
    {
        if (result is null)
        {
            var expectedLength = 1 + (diaeresis ? 1 : 0) + (tonos ? 1 : 0) + iotas;
            var equal = end - start == expectedLength && text[start] == upper;
            var offset = start + 1;
            if (equal && diaeresis) equal = text[offset++] == '\u0308';
            if (equal && tonos) equal = text[offset++] == '\u0301';
            for (var i = 0; equal && i < iotas; i++) equal = text[offset++] == '\u0399';
            if (equal) return;
            result = StartResult(text, start);
        }
        result.Append(upper);
        if (diaeresis) result.Append('\u0308');
        if (tonos) result.Append('\u0301');
        result.Append('\u0399', iotas);
    }

    private static bool FollowedByCased(string text, int start)
    {
        while (start < text.Length)
        {
            var scalar = ReadScalar(text, start, out var length); start += length;
            if (Contains(CaseIgnorableRanges, scalar)) continue;
            return Contains(CasedRanges, scalar);
        }
        return false;
    }

    private static uint GreekData(uint scalar) => scalar switch
    {
        >= 0x370 and <= 0x3FF => Greek0370[(int)(scalar - 0x370)],
        >= 0x1F00 and <= 0x1FFF => Greek1F00[(int)(scalar - 0x1F00)],
        0x2126 => 0x3A9 | 0x1000,
        _ => 0
    };

    private static uint GreekDiacritic(uint scalar) => scalar switch
    {
        0x300 or 0x301 or 0x342 or 0x302 or 0x303 or 0x311 => 0x4000,
        0x308 => 0x10000,
        0x344 => 0x10000 | 0x4000,
        0x345 => 0x2000,
        0x304 or 0x306 or 0x313 or 0x314 or 0x343 => 0x20000,
        _ => 0
    };
}
