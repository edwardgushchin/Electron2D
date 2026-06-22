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
using Xunit;

namespace Electron2D.Tests.Unit;

public sealed class ThemeTooltipPublicApiTests
{
    [Fact]
    public void ThemeAndStyleBoxesExposeExpectedInheritance()
    {
        Assert.True(typeof(Electron2D.Resource).IsAssignableFrom(typeof(Electron2D.Theme)));
        Assert.True(typeof(Electron2D.Resource).IsAssignableFrom(typeof(Electron2D.StyleBox)));
        Assert.True(typeof(Electron2D.StyleBox).IsAssignableFrom(typeof(Electron2D.StyleBoxFlat)));
    }

    [Fact]
    public void ThemeStoresItemsByNameAndType()
    {
        var theme = new Electron2D.Theme();
        var font = new TestFont();
        var icon = new TestTexture(16, 16);
        var styleBox = new Electron2D.StyleBoxFlat
        {
            BgColor = new Electron2D.Color(0.1f, 0.2f, 0.3f, 1f),
            BorderColor = new Electron2D.Color(0.9f, 0.8f, 0.7f, 1f),
            BorderWidthLeft = 1,
            BorderWidthTop = 2,
            BorderWidthRight = 3,
            BorderWidthBottom = 4,
            ContentMarginLeft = 5f,
            ContentMarginTop = 6f,
            ContentMarginRight = 7f,
            ContentMarginBottom = 8f
        };

        theme.DefaultBaseScale = 2f;
        theme.DefaultFont = font;
        theme.DefaultFontSize = 9;
        theme.SetColor("font_color", "Button", new Electron2D.Color(1f, 0f, 0f, 1f));
        theme.SetConstant("separation", "HBoxContainer", 4);
        theme.SetFont("font", "Button", font);
        theme.SetFontSize("font_size", "Button", 11);
        theme.SetIcon("checked", "CheckBox", icon);
        theme.SetStyleBox("normal", "Button", styleBox);

        Assert.True(theme.HasDefaultBaseScale());
        Assert.True(theme.HasDefaultFont());
        Assert.True(theme.HasDefaultFontSize());
        Assert.Equal(2f, theme.DefaultBaseScale);
        Assert.Same(font, theme.DefaultFont);
        Assert.Equal(9, theme.DefaultFontSize);
        Assert.Equal(new Electron2D.Color(1f, 0f, 0f, 1f), theme.GetColor("font_color", "Button"));
        Assert.Equal(4, theme.GetConstant("separation", "HBoxContainer"));
        Assert.Same(font, theme.GetFont("font", "Button"));
        Assert.Equal(11, theme.GetFontSize("font_size", "Button"));
        Assert.Same(icon, theme.GetIcon("checked", "CheckBox"));
        Assert.Same(styleBox, theme.GetStyleBox("normal", "Button"));

        theme.ClearColor("font_color", "Button");
        theme.ClearConstant("separation", "HBoxContainer");
        theme.ClearFont("font", "Button");
        theme.ClearFontSize("font_size", "Button");
        theme.ClearIcon("checked", "CheckBox");
        theme.ClearStyleBox("normal", "Button");

        Assert.False(theme.HasColor("font_color", "Button"));
        Assert.False(theme.HasConstant("separation", "HBoxContainer"));
        Assert.False(theme.HasFont("font", "Button"));
        Assert.False(theme.HasFontSize("font_size", "Button"));
        Assert.False(theme.HasIcon("checked", "CheckBox"));
        Assert.False(theme.HasStyleBox("normal", "Button"));
    }

    [Fact]
    public void ThemeAndStyleBoxRejectFreedInstances()
    {
        var theme = new Electron2D.Theme
        {
            DefaultBaseScale = 1.5f,
            DefaultFont = new TestFont(),
            DefaultFontSize = 18
        };

        theme.Free();

        Assert.Throws<InvalidOperationException>(() => theme.DefaultBaseScale);
        Assert.Throws<InvalidOperationException>(() => theme.DefaultBaseScale = 2f);
        Assert.Throws<InvalidOperationException>(() => theme.DefaultFont);
        Assert.Throws<InvalidOperationException>(() => theme.DefaultFont = new TestFont());
        Assert.Throws<InvalidOperationException>(() => theme.DefaultFontSize);
        Assert.Throws<InvalidOperationException>(() => theme.DefaultFontSize = 20);
        Assert.Throws<InvalidOperationException>(() => theme.SetColor("font_color", "Button", new Electron2D.Color(1f, 1f, 1f, 1f)));

        var styleBox = new Electron2D.StyleBoxFlat
        {
            BgColor = new Electron2D.Color(0.1f, 0.2f, 0.3f, 1f),
            BorderColor = new Electron2D.Color(0.4f, 0.5f, 0.6f, 1f),
            BorderWidthLeft = 1,
            ContentMarginLeft = 2f
        };

        styleBox.Free();

        Assert.Throws<InvalidOperationException>(() => styleBox.BgColor);
        Assert.Throws<InvalidOperationException>(() => styleBox.BgColor = new Electron2D.Color(1f, 0f, 0f, 1f));
        Assert.Throws<InvalidOperationException>(() => styleBox.BorderColor);
        Assert.Throws<InvalidOperationException>(() => styleBox.BorderWidthLeft);
        Assert.Throws<InvalidOperationException>(() => styleBox.BorderWidthLeft = 2);
        Assert.Throws<InvalidOperationException>(() => styleBox.ContentMarginLeft);
        Assert.Throws<InvalidOperationException>(() => styleBox.ContentMarginLeft = 3f);
        Assert.Throws<InvalidOperationException>(() => styleBox.GetMinimumSize());
    }

    [Fact]
    public void ControlThemeOverridesHavePriorityOverAssignedTheme()
    {
        var theme = new Electron2D.Theme();
        var themeFont = new TestFont();
        var overrideFont = new TestFont();
        var themeIcon = new TestTexture(8, 8);
        var overrideIcon = new TestTexture(10, 10);
        var themeStyleBox = new Electron2D.StyleBoxFlat();
        var overrideStyleBox = new Electron2D.StyleBoxFlat();
        var control = new Electron2D.Control
        {
            Theme = theme,
            ThemeTypeVariation = "PrimaryButton",
            TooltipText = "Save changes"
        };

        theme.SetColor("font_color", "PrimaryButton", new Electron2D.Color(0f, 1f, 0f, 1f));
        theme.SetConstant("separation", "Control", 4);
        theme.SetFont("font", "Control", themeFont);
        theme.SetFontSize("font_size", "Control", 12);
        theme.SetIcon("checked", "Control", themeIcon);
        theme.SetStyleBox("normal", "Control", themeStyleBox);

        control.AddThemeColorOverride("font_color", new Electron2D.Color(1f, 0f, 0f, 1f));
        control.AddThemeConstantOverride("separation", 6);
        control.AddThemeFontOverride("font", overrideFont);
        control.AddThemeFontSizeOverride("font_size", 14);
        control.AddThemeIconOverride("checked", overrideIcon);
        control.AddThemeStyleBoxOverride("normal", overrideStyleBox);

        Assert.True(control.HasThemeColorOverride("font_color"));
        Assert.True(control.HasThemeConstantOverride("separation"));
        Assert.True(control.HasThemeFontOverride("font"));
        Assert.True(control.HasThemeFontSizeOverride("font_size"));
        Assert.True(control.HasThemeIconOverride("checked"));
        Assert.True(control.HasThemeStyleBoxOverride("normal"));
        Assert.Equal(new Electron2D.Color(1f, 0f, 0f, 1f), control.GetThemeColor("font_color"));
        Assert.Equal(6, control.GetThemeConstant("separation"));
        Assert.Same(overrideFont, control.GetThemeFont("font"));
        Assert.Equal(14, control.GetThemeFontSize("font_size"));
        Assert.Same(overrideIcon, control.GetThemeIcon("checked"));
        Assert.Same(overrideStyleBox, control.GetThemeStyleBox("normal"));

        control.RemoveThemeColorOverride("font_color");
        control.RemoveThemeConstantOverride("separation");
        control.RemoveThemeFontOverride("font");
        control.RemoveThemeFontSizeOverride("font_size");
        control.RemoveThemeIconOverride("checked");
        control.RemoveThemeStyleBoxOverride("normal");

        Assert.Equal(new Electron2D.Color(0f, 1f, 0f, 1f), control.GetThemeColor("font_color"));
        Assert.Equal(4, control.GetThemeConstant("separation"));
        Assert.Same(themeFont, control.GetThemeFont("font"));
        Assert.Equal(12, control.GetThemeFontSize("font_size"));
        Assert.Same(themeIcon, control.GetThemeIcon("checked"));
        Assert.Same(themeStyleBox, control.GetThemeStyleBox("normal"));
        Assert.Equal("Save changes", control.GetTooltip(Electron2D.Vector2.Zero));
    }

    private sealed class TestFont : Electron2D.Font
    {
    }

    private sealed class TestTexture : Electron2D.Texture2D
    {
        private readonly int width;
        private readonly int height;

        public TestTexture(int width, int height)
        {
            this.width = width;
            this.height = height;
        }

        public override int GetWidth()
        {
            return width;
        }

        public override int GetHeight()
        {
            return height;
        }

        public override bool HasAlpha()
        {
            return true;
        }
    }
}
