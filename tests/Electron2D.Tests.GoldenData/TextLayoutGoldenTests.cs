/*
    Electron2D
    MIT License
    Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
    SPDX-License-Identifier: MIT

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/
using System.Globalization;
using System.Text;
using Xunit;

namespace Electron2D.Tests.GoldenData;

public sealed class TextLayoutGoldenTests
{
    [Fact]
    public void MixedDirectionTextLayoutMatchesGoldenSummary()
    {
        var fallback = new GoldenFont("fallback", rune => rune.Value is >= 0x0590 and <= 0x05FF);
        var primary = new GoldenFont("primary", rune => rune.Value is < 0x0590 or > 0x05FF, fallback);

        var layout = primary.GetTextLayout("Aאב", Electron2D.HorizontalAlignment.Center, 80f, 20);

        var actual = string.Join(
            "\n",
            layout.Glyphs.Select(glyph =>
                $"direction={layout.Direction}|size={Format(layout.Size)}|offset={Format(layout.AlignmentOffset)}|glyph=U+{glyph.CodePoint:X4}|font={glyph.Font.ResourceName}|pos={Format(glyph.Position)}|advance={Format(glyph.Advance)}|available={glyph.GlyphAvailable}"));

        const string expected =
            "direction=RightToLeft|size=30,20|offset=25|glyph=U+05D1|font=fallback|pos=45,0|advance=10|available=True\n" +
            "direction=RightToLeft|size=30,20|offset=25|glyph=U+05D0|font=fallback|pos=35,0|advance=10|available=True\n" +
            "direction=RightToLeft|size=30,20|offset=25|glyph=U+0041|font=primary|pos=25,0|advance=10|available=True";

        Assert.Equal(expected, actual);
    }

    private static string Format(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string Format(Electron2D.Vector2 value)
    {
        return $"{Format(value.X)},{Format(value.Y)}";
    }

    private sealed class GoldenFont : Electron2D.Font
    {
        private readonly Func<Rune, bool> supports;
        private readonly Electron2D.Font[] fallbacks;

        public GoldenFont(string name, Func<Rune, bool> supports, params Electron2D.Font[] fallbacks)
        {
            ResourceName = name;
            this.supports = supports;
            this.fallbacks = fallbacks;
        }

        internal override bool HasGlyph(Rune rune)
        {
            return supports(rune);
        }

        internal override float GetGlyphAdvance(Rune rune, int fontSize)
        {
            return fontSize / 2f;
        }

        internal override IReadOnlyList<Electron2D.Font> GetFallbackFonts()
        {
            return fallbacks;
        }
    }
}
