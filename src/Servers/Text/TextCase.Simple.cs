using System.Text;

namespace Electron2D;

internal static partial class TextCase
{
    internal static bool SimplePunctuation(uint scalar) => Contains(SimplePunctuationRanges, scalar);
    internal static bool SimpleNonprinting(uint scalar) => Contains(SimpleNonprintingRanges, scalar);
    private static uint SimpleLower(uint scalar)
    {
        var pairs = LowerPairs; var low = 0; var high = pairs.Length / 2 - 1;
        while (low <= high)
        {
            var middle = (low + high) / 2; var value = pairs[middle * 2];
            if (scalar < value) high = middle - 1;
            else if (scalar > value) low = middle + 1;
            else return pairs[middle * 2 + 1];
        }
        return scalar;
    }
    internal static string SimpleCase(string text, bool upper)
    {
        StringBuilder? result = null;
        for (var start = 0; start < text.Length;)
        {
            var scalar = ReadScalar(text, start, out var length);
            AppendScalar(ref result, text, start, length, scalar, upper ? SimpleUpper(scalar) : SimpleLower(scalar));
            start += length;
        }
        return result?.ToString() ?? text;
    }
    internal static string SimpleTitle(string text)
    {
        var scalars = new List<uint>(text.Length);
        for (var start = 0; start < text.Length;)
        { scalars.Add(ReadScalar(text, start, out var length)); start += length; }
        var result = new StringBuilder(text.Length); var wordStart = true;
        for (var i = 0; i < scalars.Count; i++)
        {
            var value = scalars[i]; var upper = Contains(SimpleUpperRanges, value); var lower = Contains(SimpleLowerRanges, value); var digit = value is >= '0' and <= '9';
            if (i > 0)
            {
                var previous = scalars[i - 1]; var prevUpper = Contains(SimpleUpperRanges, previous); var prevLower = Contains(SimpleLowerRanges, previous); var prevDigit = previous is >= '0' and <= '9';
                var nextLower = i + 1 < scalars.Count && Contains(SimpleLowerRanges, scalars[i + 1]);
                if (prevLower && upper || (prevUpper || prevDigit) && upper && nextLower || prevDigit && lower && nextLower || (prevUpper || prevLower) && digit) wordStart = true;
            }
            if (NativeTextBreak.StringWhitespace(value) || value is '_' or '-' or 0x2010 or 0x2011) { wordStart = true; continue; }
            if (wordStart && result.Length != 0) result.Append(' ');
            value = SimpleLower(value); if (wordStart) value = SimpleUpper(value);
            if (value <= 0xffff) result.Append((char)value); else result.Append(char.ConvertFromUtf32((int)value));
            wordStart = false;
        }
        return result.ToString();
    }
    internal static int[] ScalarBreaks(string text)
    {
        NativeTextBreak.ValidateString(text);
        var count = 0;
        for (var at = 0; at < text.Length;) { ReadScalar(text, at, out var length); at += length; count++; }
        var result = new int[count]; for (var i = 0; i < count; i++) result[i] = i + 1; return result;
    }
}
