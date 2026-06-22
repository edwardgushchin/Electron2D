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

namespace Electron2D.Tests.Integration;

[Collection(InputStateCollection.Name)]
public sealed class ThemeTooltipRuntimeTests
{
    [Fact]
    public void ThemeLookupFallsBackThroughControlBranchAndAppliesBaseScale()
    {
        var tree = new Electron2D.SceneTree();
        var root = new Electron2D.Control
        {
            Theme = new Electron2D.Theme
            {
                DefaultBaseScale = 2f,
                DefaultFont = new TestFont(),
                DefaultFontSize = 9
            }
        };
        var button = new Electron2D.Button
        {
            ThemeTypeVariation = "PrimaryButton"
        };
        var container = new Electron2D.HBoxContainer();

        root.Theme.SetColor("font_color", "PrimaryButton", new Electron2D.Color(0.2f, 0.4f, 0.6f, 1f));
        root.Theme.SetConstant("separation", "HBoxContainer", 5);
        root.AddChild(container);
        root.AddChild(button);
        tree.Root.AddChild(root);

        Assert.Equal(new Electron2D.Color(0.2f, 0.4f, 0.6f, 1f), button.GetThemeColor("font_color"));
        Assert.Equal(18, button.GetThemeFontSize("font_size"));
        Assert.Same(root.Theme.DefaultFont, button.GetThemeFont("font"));
        Assert.Equal(10, container.GetThemeConstant("separation"));
        Assert.Equal(2f, button.GetThemeDefaultBaseScale());
    }

    [Fact]
    public void TooltipUsesLocalPositionAndViewportTracksHoveredControl()
    {
        Electron2D.Input.ResetForTests();
        Electron2D.InputMap.ClearForTests();

        var tree = new Electron2D.SceneTree();
        var viewport = Assert.IsType<Electron2D.Viewport>(tree.Root);
        var tooltip = new TooltipControl
        {
            Position = new Electron2D.Vector2(10f, 10f),
            Size = new Electron2D.Vector2(50f, 20f),
            MouseFilter = Electron2D.MouseFilter.Stop
        };
        tree.Root.AddChild(tooltip);

        tree.DispatchInput(new Electron2D.InputEventMouseMotion
        {
            Position = new Electron2D.Vector2(20f, 15f),
            GlobalPosition = new Electron2D.Vector2(20f, 15f)
        });

        Assert.Same(tooltip, viewport.GuiGetHoveredControl());
        Assert.Equal("local:10,5", tooltip.GetTooltip(new Electron2D.Vector2(10f, 5f)));
        Assert.IsType<Electron2D.Label>(tooltip._MakeCustomTooltip("local:10,5"));

        tree.DispatchInput(new Electron2D.InputEventMouseMotion
        {
            Position = new Electron2D.Vector2(100f, 100f),
            GlobalPosition = new Electron2D.Vector2(100f, 100f)
        });

        Assert.Null(viewport.GuiGetHoveredControl());
    }

    [Fact]
    public void ThemeResourceMetadataRoundTripsSerializableThemeItems()
    {
        var theme = new Electron2D.Theme
        {
            DefaultBaseScale = 1.5f,
            DefaultFontSize = 13
        };
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
        theme.SetColor("font_color", "Button", new Electron2D.Color(1f, 0f, 0f, 1f));
        theme.SetConstant("separation", "HBoxContainer", 4);
        theme.SetFontSize("font_size", "Button", 11);
        theme.SetStyleBox("normal", "Button", styleBox);

        var document = Electron2D.ResourceObjectSerializer.Capture(theme, "res://ui/default_theme.e2res");
        var restored = Assert.IsType<Electron2D.Theme>(Electron2D.ResourceObjectSerializer.Instantiate(document));
        var restoredStyleBox = Assert.IsType<Electron2D.StyleBoxFlat>(restored.GetStyleBox("normal", "Button"));

        Assert.Equal(1.5f, restored.DefaultBaseScale);
        Assert.Equal(13, restored.DefaultFontSize);
        Assert.Equal(new Electron2D.Color(1f, 0f, 0f, 1f), restored.GetColor("font_color", "Button"));
        Assert.Equal(4, restored.GetConstant("separation", "HBoxContainer"));
        Assert.Equal(11, restored.GetFontSize("font_size", "Button"));
        Assert.Equal(styleBox.BgColor, restoredStyleBox.BgColor);
        Assert.Equal(styleBox.BorderWidthBottom, restoredStyleBox.BorderWidthBottom);
        Assert.Equal(styleBox.GetMinimumSize(), restoredStyleBox.GetMinimumSize());
    }

    private sealed class TestFont : Electron2D.Font
    {
    }

    private sealed class TooltipControl : Electron2D.Control
    {
        public override string _GetTooltip(Electron2D.Vector2 atPosition)
        {
            return $"local:{atPosition.X:0},{atPosition.Y:0}";
        }

        public override Electron2D.Control? _MakeCustomTooltip(string forText)
        {
            return new Electron2D.Label
            {
                Text = forText
            };
        }
    }
}
