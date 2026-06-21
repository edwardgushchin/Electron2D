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
using System.Text;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class TextLayoutSubmissionTests
{
    [Fact]
    public void DrawStringCapturesUnicodeLayoutAndReusesFontLayoutCache()
    {
        var font = new SelectiveFont("primary");
        var tree = new Electron2D.SceneTree();
        var node = new TextDrawNode(
            font,
            "Hi🙂",
            new Electron2D.Vector2(5f, 20f),
            Electron2D.HorizontalAlignment.Left,
            width: -1f,
            fontSize: 20);
        tree.Root.AddChild(node);

        tree.ProcessFrame(1.0 / 60.0);

        var command = Assert.Single(new Electron2D.CanvasSubmissionContext().BuildPlan(tree.Root).Commands);
        Assert.NotNull(command.TextLayout);
        var layout = command.TextLayout!;

        Assert.Equal(Electron2D.TextLayoutDirection.LeftToRight, layout.Direction);
        Assert.Equal(new Electron2D.Vector2(30f, 20f), layout.Size);
        Assert.Equal(0f, layout.AlignmentOffset);
        Assert.Equal(new Electron2D.Rect2(5f, 4f, 30f, 20f), command.DestinationRect);
        Assert.Equal(new[] { 'H', 'i', 0x1F642 }, layout.Glyphs.Select(glyph => glyph.CodePoint).ToArray());
        Assert.Equal(new[] { 0f, 10f, 20f }, layout.Glyphs.Select(glyph => glyph.Position.X).ToArray());
        Assert.All(layout.Glyphs, glyph => Assert.Same(font, glyph.Font));
        Assert.All(layout.Glyphs, glyph => Assert.True(glyph.GlyphAvailable));

        var cachedLayout = font.GetTextLayout("Hi🙂", Electron2D.HorizontalAlignment.Left, -1f, 20);
        var stats = font.GetTextLayoutCacheStats();

        Assert.Same(layout, cachedLayout);
        Assert.Equal(1, stats.Misses);
        Assert.Equal(1, stats.Hits);
    }

    [Fact]
    public void DrawStringResolvesFallbackFontAndBasicRtlLayout()
    {
        var fallback = new SelectiveFont("fallback", rune => IsHebrew(rune));
        var primary = new SelectiveFont("primary", rune => rune.Value is >= 'A' and <= 'Z', fallback);
        var tree = new Electron2D.SceneTree();
        var node = new TextDrawNode(
            primary,
            "אב",
            new Electron2D.Vector2(0f, 20f),
            Electron2D.HorizontalAlignment.Right,
            width: 100f,
            fontSize: 20);
        tree.Root.AddChild(node);

        tree.ProcessFrame(1.0 / 60.0);

        var command = Assert.Single(new Electron2D.CanvasSubmissionContext().BuildPlan(tree.Root).Commands);
        Assert.NotNull(command.TextLayout);
        var layout = command.TextLayout!;

        Assert.Equal(Electron2D.TextLayoutDirection.RightToLeft, layout.Direction);
        Assert.Equal(80f, layout.AlignmentOffset);
        Assert.Equal(new Electron2D.Rect2(80f, 4f, 20f, 20f), command.DestinationRect);
        Assert.Equal(new[] { (int)'ב', (int)'א' }, layout.Glyphs.Select(glyph => glyph.CodePoint).ToArray());
        Assert.Equal(new[] { 90f, 80f }, layout.Glyphs.Select(glyph => glyph.Position.X).ToArray());
        Assert.All(layout.Glyphs, glyph => Assert.Same(fallback, glyph.Font));
        Assert.All(layout.Glyphs, glyph => Assert.True(glyph.GlyphAvailable));
    }

    [Fact]
    public void LabelSubmitsTextInCompatibilityAndStandardProfiles()
    {
        var backends = new Electron2D.RenderingBackend[]
        {
            new Electron2D.CompatibilityRenderingBackend(),
            new Electron2D.StandardRenderingBackend()
        };

        foreach (var backend in backends)
        {
            Assert.True(backend.HasFeature(Electron2D.RenderingServer.RenderingFeature.Text));

            var font = new SelectiveFont("ui");
            var tree = new Electron2D.SceneTree();
            var label = new Electron2D.Label
            {
                Name = backend.Name,
                Text = "score",
                Position = new Electron2D.Vector2(7f, 9f),
                Size = new Electron2D.Vector2(100f, 40f),
                HorizontalAlignment = Electron2D.HorizontalAlignment.Center,
                VerticalAlignment = Electron2D.VerticalAlignment.Center,
                Uppercase = true
            };
            label.AddThemeFontOverride("font", font);
            label.AddThemeFontSizeOverride("font_size", 20);
            tree.Root.AddChild(label);

            tree.ProcessFrame(1.0 / 60.0);

            var command = Assert.Single(new Electron2D.CanvasSubmissionContext().BuildPlan(tree.Root).Commands);
            Assert.NotNull(command.TextLayout);
            var layout = command.TextLayout!;

            Assert.Equal(Electron2D.CanvasItemRenderCommandKind.String, command.Kind);
            Assert.Equal("SCORE", command.Text);
            Assert.Equal(Electron2D.HorizontalAlignment.Center, command.Alignment);
            Assert.Equal(100f, command.TextWidth);
            Assert.Equal(20, command.FontSize);
            Assert.Equal(new Electron2D.Vector2(0f, 26f), command.Position);
            Assert.Equal(new Electron2D.Vector2(7f, 9f), command.Transform.Origin);
            Assert.Equal(25f, layout.AlignmentOffset);
            Assert.Equal(new Electron2D.Rect2(25f, 10f, 50f, 20f), command.DestinationRect);
        }
    }

    [Fact]
    public void LabelSubmitsTranslatedTextAfterLocaleChange()
    {
        var oldLocale = Electron2D.TranslationServer.GetLocale();
        Electron2D.TranslationServer.Clear();

        try
        {
            var english = new Electron2D.Translation
            {
                Locale = "en"
            };
            english.AddMessage("ui.score", "Score");

            var french = new Electron2D.Translation
            {
                Locale = "fr"
            };
            french.AddMessage("ui.score", "Score FR");

            Electron2D.TranslationServer.AddTranslation(english);
            Electron2D.TranslationServer.AddTranslation(french);
            Electron2D.TranslationServer.SetLocale("en");

            var tree = new Electron2D.SceneTree();
            var label = new Electron2D.Label
            {
                Text = "ui.score"
            };
            label.AddThemeFontOverride("font", new SelectiveFont("ui"));
            tree.Root.AddChild(label);

            tree.ProcessFrame(1.0 / 60.0);

            var command = Assert.Single(new Electron2D.CanvasSubmissionContext().BuildPlan(tree.Root).Commands);
            Assert.Equal("Score", command.Text);

            Electron2D.TranslationServer.SetLocale("fr-FR");
            tree.ProcessFrame(1.0 / 60.0);

            command = Assert.Single(new Electron2D.CanvasSubmissionContext().BuildPlan(tree.Root).Commands);
            Assert.Equal("Score FR", command.Text);
        }
        finally
        {
            Electron2D.TranslationServer.Clear();
            Electron2D.TranslationServer.SetLocale(oldLocale);
        }
    }

    private static bool IsHebrew(Rune rune)
    {
        return rune.Value is >= 0x0590 and <= 0x05FF;
    }

    private sealed class TextDrawNode : Electron2D.Node2D
    {
        private readonly Electron2D.Font font;
        private readonly string text;
        private readonly Electron2D.Vector2 position;
        private readonly Electron2D.HorizontalAlignment alignment;
        private readonly float width;
        private readonly int fontSize;

        public TextDrawNode(
            Electron2D.Font font,
            string text,
            Electron2D.Vector2 position,
            Electron2D.HorizontalAlignment alignment,
            float width,
            int fontSize)
        {
            this.font = font;
            this.text = text;
            this.position = position;
            this.alignment = alignment;
            this.width = width;
            this.fontSize = fontSize;
        }

        public override void _Draw()
        {
            DrawString(font, position, text, alignment, width, fontSize);
        }
    }

    private sealed class SelectiveFont : Electron2D.Font
    {
        private readonly Func<Rune, bool> supports;
        private readonly Electron2D.Font[] fallbacks;

        public SelectiveFont(string name, Func<Rune, bool>? supports = null, params Electron2D.Font[] fallbacks)
        {
            ResourceName = name;
            this.supports = supports ?? (_ => true);
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
