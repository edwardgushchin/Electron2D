namespace Electron2D;

// MIT License, Copyright (c) 2009 Florian Loitsch.
// Adapted from the Grisu2 binary-to-decimal algorithm for the JSON document boundary.
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
internal static class Grisu2Formatter
{
    private readonly record struct DiyFp(ulong F, int E)
    {
        internal static DiyFp Multiply(DiyFp left, DiyFp right)
        {
            var product = (UInt128)left.F * right.F;
            var upper = (ulong)(product >> 64);
            if (((ulong)product & (1UL << 63)) != 0) upper = unchecked(upper + 1);
            return new(upper, left.E + right.E + 64);
        }

        internal static DiyFp Normalize(DiyFp value)
        {
            while ((value.F >> 63) == 0) value = new(value.F << 1, value.E - 1);
            return value;
        }
    }

    private readonly record struct CachedPower(ulong F, int E, int K);

    private static readonly CachedPower[] CachedPowers =
    [
        new(0xAB70FE17C79AC6CAUL, -1060, -300),
        new(0xFF77B1FCBEBCDC4FUL, -1034, -292),
        new(0xBE5691EF416BD60CUL, -1007, -284),
        new(0x8DD01FAD907FFC3CUL, -980, -276),
        new(0xD3515C2831559A83UL, -954, -268),
        new(0x9D71AC8FADA6C9B5UL, -927, -260),
        new(0xEA9C227723EE8BCBUL, -901, -252),
        new(0xAECC49914078536DUL, -874, -244),
        new(0x823C12795DB6CE57UL, -847, -236),
        new(0xC21094364DFB5637UL, -821, -228),
        new(0x9096EA6F3848984FUL, -794, -220),
        new(0xD77485CB25823AC7UL, -768, -212),
        new(0xA086CFCD97BF97F4UL, -741, -204),
        new(0xEF340A98172AACE5UL, -715, -196),
        new(0xB23867FB2A35B28EUL, -688, -188),
        new(0x84C8D4DFD2C63F3BUL, -661, -180),
        new(0xC5DD44271AD3CDBAUL, -635, -172),
        new(0x936B9FCEBB25C996UL, -608, -164),
        new(0xDBAC6C247D62A584UL, -582, -156),
        new(0xA3AB66580D5FDAF6UL, -555, -148),
        new(0xF3E2F893DEC3F126UL, -529, -140),
        new(0xB5B5ADA8AAFF80B8UL, -502, -132),
        new(0x87625F056C7C4A8BUL, -475, -124),
        new(0xC9BCFF6034C13053UL, -449, -116),
        new(0x964E858C91BA2655UL, -422, -108),
        new(0xDFF9772470297EBDUL, -396, -100),
        new(0xA6DFBD9FB8E5B88FUL, -369, -92),
        new(0xF8A95FCF88747D94UL, -343, -84),
        new(0xB94470938FA89BCFUL, -316, -76),
        new(0x8A08F0F8BF0F156BUL, -289, -68),
        new(0xCDB02555653131B6UL, -263, -60),
        new(0x993FE2C6D07B7FACUL, -236, -52),
        new(0xE45C10C42A2B3B06UL, -210, -44),
        new(0xAA242499697392D3UL, -183, -36),
        new(0xFD87B5F28300CA0EUL, -157, -28),
        new(0xBCE5086492111AEBUL, -130, -20),
        new(0x8CBCCC096F5088CCUL, -103, -12),
        new(0xD1B71758E219652CUL, -77, -4),
        new(0x9C40000000000000UL, -50, 4),
        new(0xE8D4A51000000000UL, -24, 12),
        new(0xAD78EBC5AC620000UL, 3, 20),
        new(0x813F3978F8940984UL, 30, 28),
        new(0xC097CE7BC90715B3UL, 56, 36),
        new(0x8F7E32CE7BEA5C70UL, 83, 44),
        new(0xD5D238A4ABE98068UL, 109, 52),
        new(0x9F4F2726179A2245UL, 136, 60),
        new(0xED63A231D4C4FB27UL, 162, 68),
        new(0xB0DE65388CC8ADA8UL, 189, 76),
        new(0x83C7088E1AAB65DBUL, 216, 84),
        new(0xC45D1DF942711D9AUL, 242, 92),
        new(0x924D692CA61BE758UL, 269, 100),
        new(0xDA01EE641A708DEAUL, 295, 108),
        new(0xA26DA3999AEF774AUL, 322, 116),
        new(0xF209787BB47D6B85UL, 348, 124),
        new(0xB454E4A179DD1877UL, 375, 132),
        new(0x865B86925B9BC5C2UL, 402, 140),
        new(0xC83553C5C8965D3DUL, 428, 148),
        new(0x952AB45CFA97A0B3UL, 455, 156),
        new(0xDE469FBD99A05FE3UL, 481, 164),
        new(0xA59BC234DB398C25UL, 508, 172),
        new(0xF6C69A72A3989F5CUL, 534, 180),
        new(0xB7DCBF5354E9BECEUL, 561, 188),
        new(0x88FCF317F22241E2UL, 588, 196),
        new(0xCC20CE9BD35C78A5UL, 614, 204),
        new(0x98165AF37B2153DFUL, 641, 212),
        new(0xE2A0B5DC971F303AUL, 667, 220),
        new(0xA8D9D1535CE3B396UL, 694, 228),
        new(0xFB9B7CD9A4A7443CUL, 720, 236),
        new(0xBB764C4CA7A44410UL, 747, 244),
        new(0x8BAB8EEFB6409C1AUL, 774, 252),
        new(0xD01FEF10A657842CUL, 800, 260),
        new(0x9B10A4E5E9913129UL, 827, 268),
        new(0xE7109BFBA19C0C9DUL, 853, 276),
        new(0xAC2820D9623BF429UL, 880, 284),
        new(0x80444B5E7AA7CF85UL, 907, 292),
        new(0xBF21E44003ACDD2DUL, 933, 300),
        new(0x8E679C2F5E44FF8FUL, 960, 308),
        new(0xD433179D9C8CB841UL, 986, 316),
        new(0x9E19DB92B4E31BA9UL, 1013, 324),
    ];

    internal static string Format(double value)
    {
        var negative = value < 0;
        if (negative) value = -value;
        var bits = unchecked((ulong)BitConverter.DoubleToInt64Bits(value));
        var exponentBits = bits >> 52;
        var fractionBits = bits & ((1UL << 52) - 1);
        var floating = exponentBits == 0
            ? new DiyFp(fractionBits, -1074)
            : new DiyFp(fractionBits + (1UL << 52), (int)exponentBits - 1075);
        var lowerCloser = fractionBits == 0 && exponentBits > 1;
        var plus = new DiyFp(2 * floating.F + 1, floating.E - 1);
        var minus = lowerCloser
            ? new DiyFp(4 * floating.F - 1, floating.E - 2)
            : new DiyFp(2 * floating.F - 1, floating.E - 1);
        var normalizedPlus = DiyFp.Normalize(plus);
        var normalizedMinus = new DiyFp(minus.F << (minus.E - normalizedPlus.E), normalizedPlus.E);
        var normalized = DiyFp.Normalize(floating);

        var f = -60 - normalizedPlus.E - 1;
        var k = f * 78913 / (1 << 18) + (f > 0 ? 1 : 0);
        var cached = CachedPowers[(300 + k + 7) / 8];
        var multiplier = new DiyFp(cached.F, cached.E);
        var scaled = DiyFp.Multiply(normalized, multiplier);
        var lower = DiyFp.Multiply(normalizedMinus, multiplier);
        var upper = DiyFp.Multiply(normalizedPlus, multiplier);
        var decimalExponent = -cached.K;
        Span<char> digits = stackalloc char[32];
        var length = GenerateDigits(digits, ref decimalExponent,
            new DiyFp(lower.F + 1, lower.E), scaled, new DiyFp(upper.F - 1, upper.E));
        return FormatDigits(digits[..length], decimalExponent, negative);
    }

    private static int GenerateDigits(Span<char> digits, ref int decimalExponent,
        DiyFp lower, DiyFp value, DiyFp upper)
    {
        var delta = upper.F - lower.F;
        var distance = upper.F - value.F;
        var one = 1UL << -upper.E;
        var integer = (uint)(upper.F >> -upper.E);
        var fraction = upper.F & (one - 1);
        var power = 1u;
        var count = 1;
        while ((ulong)power * 10 <= integer) { power *= 10; count++; }

        var length = 0;
        while (count > 0)
        {
            var digit = integer / power;
            integer %= power;
            digits[length++] = (char)('0' + digit);
            count--;
            var rest = ((ulong)integer << -upper.E) + fraction;
            if (rest <= delta)
            {
                decimalExponent += count;
                Round(digits, length, distance, delta, rest, (ulong)power << -upper.E);
                return length;
            }
            power /= 10;
        }

        var fractionalDigits = 0;
        while (true)
        {
            fraction = unchecked(fraction * 10);
            var digit = fraction >> -upper.E;
            fraction &= one - 1;
            digits[length++] = (char)('0' + digit);
            fractionalDigits++;
            delta = unchecked(delta * 10);
            distance = unchecked(distance * 10);
            if (fraction <= delta) break;
        }
        decimalExponent -= fractionalDigits;
        Round(digits, length, distance, delta, fraction, one);
        return length;
    }

    private static void Round(Span<char> digits, int length, ulong distance,
        ulong delta, ulong rest, ulong unit)
    {
        while (rest < distance && delta - rest >= unit &&
               (rest + unit < distance || distance - rest > rest + unit - distance))
        {
            digits[length - 1]--;
            rest += unit;
        }
    }

    private static string FormatDigits(ReadOnlySpan<char> digits, int exponent, bool negative)
    {
        var position = digits.Length + exponent;
        var result = new System.Text.StringBuilder(negative ? 32 : 31);
        if (negative) result.Append('-');
        if (position > 15 || position <= -4)
        {
            result.Append(digits[0]);
            if (digits.Length > 1) { result.Append('.'); result.Append(digits[1..]); }
            var scientificExponent = position - 1;
            result.Append('e');
            result.Append(scientificExponent < 0 ? '-' : '+');
            result.Append(Math.Abs(scientificExponent).ToString("D2", System.Globalization.CultureInfo.InvariantCulture));
        }
        else if (position <= 0)
        {
            result.Append("0.");
            result.Append('0', -position);
            result.Append(digits);
        }
        else if (position >= digits.Length)
        {
            result.Append(digits);
            result.Append('0', position - digits.Length);
            result.Append(".0");
        }
        else
        {
            result.Append(digits[..position]);
            result.Append('.');
            result.Append(digits[position..]);
        }
        return result.ToString();
    }
}
