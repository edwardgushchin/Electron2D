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

public sealed class TextPublicApiTests
{
    [Fact]
    public void FontExposesElectron2DMeasurementAndGlyphAvailability()
    {
        var font = new TestFont();

        Assert.Equal(new Electron2D.Vector2(50f, 20f), font.GetStringSize("Hello", fontSize: 20));
        Assert.Equal(20f, font.GetHeight(20));
        Assert.Equal(16f, font.GetAscent(20));
        Assert.Equal(4f, font.GetDescent(20));
        Assert.True(font.HasChar('A'));
        Assert.False(font.HasChar(-1));

        Assert.Throws<ArgumentNullException>(() => font.GetStringSize(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => font.GetStringSize("A", fontSize: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => font.GetHeight(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => font.GetAscent(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => font.GetDescent(0));
    }

    [Fact]
    public void ControlLabelAndVerticalAlignmentExposeElectron2DSurface()
    {
        Assert.True(typeof(Electron2D.CanvasItem).IsAssignableFrom(typeof(Electron2D.Control)));
        Assert.True(typeof(Electron2D.Control).IsAssignableFrom(typeof(Electron2D.Label)));
        Assert.Equal(0, (int)Electron2D.VerticalAlignment.Top);
        Assert.Equal(1, (int)Electron2D.VerticalAlignment.Center);
        Assert.Equal(2, (int)Electron2D.VerticalAlignment.Bottom);
        Assert.Equal(3, (int)Electron2D.VerticalAlignment.Fill);
        Assert.Equal(0, (int)Electron2D.MouseFilter.Stop);
        Assert.Equal(1, (int)Electron2D.MouseFilter.Pass);
        Assert.Equal(2, (int)Electron2D.MouseFilter.Ignore);
        Assert.Equal(0, (int)Electron2D.FocusMode.None);
        Assert.Equal(1, (int)Electron2D.FocusMode.Click);
        Assert.Equal(2, (int)Electron2D.FocusMode.All);
        Assert.Equal(0, (int)Electron2D.GrowDirection.Begin);
        Assert.Equal(1, (int)Electron2D.GrowDirection.End);
        Assert.Equal(2, (int)Electron2D.GrowDirection.Both);

        var font = new TestFont();
        var control = new Electron2D.Control
        {
            Position = new Electron2D.Vector2(4f, 5f),
            Size = new Electron2D.Vector2(120f, 32f),
            MouseFilter = Electron2D.MouseFilter.Pass,
            FocusMode = Electron2D.FocusMode.Click,
            CustomMinimumSize = new Electron2D.Vector2(8f, 9f),
            GrowHorizontal = Electron2D.GrowDirection.Begin,
            GrowVertical = Electron2D.GrowDirection.Both,
            ClipContents = true,
            FocusNext = new Electron2D.NodePath("../next"),
            FocusPrevious = new Electron2D.NodePath("../previous"),
            FocusNeighborLeft = new Electron2D.NodePath("../left"),
            FocusNeighborTop = new Electron2D.NodePath("../top"),
            FocusNeighborRight = new Electron2D.NodePath("../right"),
            FocusNeighborBottom = new Electron2D.NodePath("../bottom")
        };

        control.AddThemeFontOverride("font", font);
        control.AddThemeFontSizeOverride("font_size", 20);

        Assert.Same(font, control.GetThemeFont("font"));
        Assert.Null(control.GetThemeFont("missing"));
        Assert.Equal(20, control.GetThemeFontSize("font_size"));
        Assert.Equal(16, control.GetThemeFontSize("missing"));
        Assert.Equal(new Electron2D.Vector2(4f, 5f), control.Position);
        Assert.Equal(new Electron2D.Vector2(120f, 32f), control.Size);
        Assert.Equal(new Electron2D.Vector2(8f, 9f), control.CustomMinimumSize);
        Assert.Equal(new Electron2D.Vector2(8f, 9f), control.GetMinimumSize());
        Assert.Equal(new Electron2D.Vector2(8f, 9f), control.GetCombinedMinimumSize());
        Assert.Equal(new Electron2D.Rect2(new Electron2D.Vector2(4f, 5f), new Electron2D.Vector2(120f, 32f)), control.GetRect());
        Assert.Equal(Electron2D.MouseFilter.Pass, control.MouseFilter);
        Assert.Equal(Electron2D.FocusMode.Click, control.FocusMode);
        Assert.Equal(Electron2D.GrowDirection.Begin, control.GrowHorizontal);
        Assert.Equal(Electron2D.GrowDirection.Both, control.GrowVertical);
        Assert.True(control.ClipContents);
        Assert.Equal(new Electron2D.NodePath("../next"), control.FocusNext);
        Assert.Equal(new Electron2D.NodePath("../previous"), control.FocusPrevious);
        Assert.Equal(new Electron2D.NodePath("../left"), control.FocusNeighborLeft);
        Assert.Equal(new Electron2D.NodePath("../top"), control.FocusNeighborTop);
        Assert.Equal(new Electron2D.NodePath("../right"), control.FocusNeighborRight);
        Assert.Equal(new Electron2D.NodePath("../bottom"), control.FocusNeighborBottom);

        var label = new Electron2D.Label
        {
            Text = "score",
            HorizontalAlignment = Electron2D.HorizontalAlignment.Center,
            VerticalAlignment = Electron2D.VerticalAlignment.Bottom,
            Uppercase = true
        };

        Assert.Equal("score", label.Text);
        Assert.Equal(Electron2D.HorizontalAlignment.Center, label.HorizontalAlignment);
        Assert.Equal(Electron2D.VerticalAlignment.Bottom, label.VerticalAlignment);
        Assert.True(label.Uppercase);
    }

    private sealed class TestFont : Electron2D.Font
    {
    }
}
