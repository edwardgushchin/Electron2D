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

public sealed class BasicControlsPublicApiTests
{
    [Fact]
    public void BasicControlsExposeExpectedInheritanceAndEnumValues()
    {
        Assert.True(typeof(Electron2D.Control).IsAssignableFrom(typeof(Electron2D.Panel)));
        Assert.True(typeof(Electron2D.Control).IsAssignableFrom(typeof(Electron2D.BaseButton)));
        Assert.True(typeof(Electron2D.BaseButton).IsAssignableFrom(typeof(Electron2D.Button)));
        Assert.True(typeof(Electron2D.BaseButton).IsAssignableFrom(typeof(Electron2D.TextureButton)));
        Assert.True(typeof(Electron2D.Button).IsAssignableFrom(typeof(Electron2D.CheckBox)));
        Assert.True(typeof(Electron2D.Control).IsAssignableFrom(typeof(Electron2D.LineEdit)));
        Assert.True(typeof(Electron2D.Control).IsAssignableFrom(typeof(Electron2D.Range)));
        Assert.True(typeof(Electron2D.Range).IsAssignableFrom(typeof(Electron2D.Slider)));
        Assert.True(typeof(Electron2D.Range).IsAssignableFrom(typeof(Electron2D.ProgressBar)));
        Assert.True(typeof(Electron2D.Control).IsAssignableFrom(typeof(Electron2D.TextureRect)));
        Assert.True(typeof(Electron2D.Control).IsAssignableFrom(typeof(Electron2D.NinePatchRect)));

        Assert.Equal(0, (int)Electron2D.BaseButton.ActionModeEnum.ButtonPress);
        Assert.Equal(1, (int)Electron2D.BaseButton.ActionModeEnum.ButtonRelease);
        Assert.Equal(0, (int)Electron2D.TextureButton.StretchModeEnum.Scale);
        Assert.Equal(1, (int)Electron2D.TextureButton.StretchModeEnum.Tile);
        Assert.Equal(2, (int)Electron2D.TextureButton.StretchModeEnum.Keep);
        Assert.Equal(3, (int)Electron2D.TextureButton.StretchModeEnum.KeepCentered);
        Assert.Equal(4, (int)Electron2D.TextureButton.StretchModeEnum.KeepAspect);
        Assert.Equal(5, (int)Electron2D.TextureButton.StretchModeEnum.KeepAspectCentered);
        Assert.Equal(6, (int)Electron2D.TextureButton.StretchModeEnum.KeepAspectCovered);
        Assert.Equal(0, (int)Electron2D.TextureRect.ExpandModeEnum.KeepSize);
        Assert.Equal(1, (int)Electron2D.TextureRect.ExpandModeEnum.IgnoreSize);
        Assert.Equal(2, (int)Electron2D.TextureRect.ExpandModeEnum.FitWidth);
        Assert.Equal(3, (int)Electron2D.TextureRect.ExpandModeEnum.FitWidthProportional);
        Assert.Equal(4, (int)Electron2D.TextureRect.ExpandModeEnum.FitHeight);
        Assert.Equal(5, (int)Electron2D.TextureRect.ExpandModeEnum.FitHeightProportional);
        Assert.Equal(0, (int)Electron2D.TextureRect.StretchModeEnum.Scale);
        Assert.Equal(1, (int)Electron2D.TextureRect.StretchModeEnum.Tile);
        Assert.Equal(2, (int)Electron2D.TextureRect.StretchModeEnum.Keep);
        Assert.Equal(3, (int)Electron2D.TextureRect.StretchModeEnum.KeepCentered);
        Assert.Equal(4, (int)Electron2D.TextureRect.StretchModeEnum.KeepAspect);
        Assert.Equal(5, (int)Electron2D.TextureRect.StretchModeEnum.KeepAspectCentered);
        Assert.Equal(6, (int)Electron2D.TextureRect.StretchModeEnum.KeepAspectCovered);
        Assert.Equal(0, (int)Electron2D.NinePatchRect.AxisStretchModeEnum.Stretch);
        Assert.Equal(1, (int)Electron2D.NinePatchRect.AxisStretchModeEnum.Tile);
        Assert.Equal(2, (int)Electron2D.NinePatchRect.AxisStretchModeEnum.TileFit);
    }

    [Fact]
    public void ButtonsExposeSignalsDefaultsAndTextureState()
    {
        var button = new Electron2D.Button();
        var checkBox = new Electron2D.CheckBox();
        var textureButton = new Electron2D.TextureButton();
        var normal = new TestTexture(16, 8);
        var pressed = new TestTexture(12, 6);

        textureButton.TextureNormal = normal;
        textureButton.TexturePressed = pressed;
        textureButton.TextureClickMask = normal;
        textureButton.ButtonPressed = true;
        textureButton.StretchMode = Electron2D.TextureButton.StretchModeEnum.KeepAspectCentered;

        Assert.True(button.HasSignal("button_down"));
        Assert.True(button.HasSignal("button_up"));
        Assert.True(button.HasSignal("pressed"));
        Assert.True(button.HasSignal("toggled"));
        Assert.Equal(Electron2D.FocusMode.All, button.FocusMode);
        Assert.Equal(Electron2D.BaseButton.ActionModeEnum.ButtonRelease, button.ActionMode);
        Assert.False(button.ToggleMode);
        Assert.False(button.Disabled);
        Assert.True(checkBox.ToggleMode);
        Assert.Same(normal, textureButton.TextureNormal);
        Assert.Same(pressed, textureButton.TexturePressed);
        Assert.Same(normal, textureButton.TextureClickMask);
        Assert.Equal(Electron2D.TextureButton.StretchModeEnum.KeepAspectCentered, textureButton.StretchMode);
    }

    [Fact]
    public void LineEditExposesTextEditingStateAndSignals()
    {
        var lineEdit = new Electron2D.LineEdit
        {
            PlaceholderText = "Name",
            Text = "Player",
            Secret = true,
            SecretCharacter = "#",
            MaxLength = 8,
            CaretColumn = 3,
            HorizontalAlignment = Electron2D.HorizontalAlignment.Center
        };

        Assert.True(lineEdit.HasSignal("text_changed"));
        Assert.True(lineEdit.HasSignal("text_submitted"));
        Assert.True(lineEdit.HasSignal("text_change_rejected"));
        Assert.Equal(Electron2D.FocusMode.All, lineEdit.FocusMode);
        Assert.Equal("Name", lineEdit.PlaceholderText);
        Assert.Equal("Player", lineEdit.Text);
        Assert.True(lineEdit.Secret);
        Assert.Equal("#", lineEdit.SecretCharacter);
        Assert.Equal(8, lineEdit.MaxLength);
        Assert.Equal(3, lineEdit.CaretColumn);
        Assert.Equal(Electron2D.HorizontalAlignment.Center, lineEdit.HorizontalAlignment);

        lineEdit.Clear();

        Assert.Equal(string.Empty, lineEdit.Text);
        Assert.Equal(0, lineEdit.CaretColumn);
    }

    [Fact]
    public void RangeClampsSnapsRatioAndCanSuppressSignal()
    {
        var range = new Electron2D.Range
        {
            MinValue = 0d,
            MaxValue = 10d,
            Step = 0.5d
        };
        var values = new List<double>();
        range.Connect("value_changed", Electron2D.Callable.From<double>(values.Add));

        range.Value = 4.76d;

        Assert.Equal(5d, range.Value);
        Assert.Equal(0.5d, range.Ratio);
        Assert.Equal(new[] { 5d }, values);

        range.SetValueNoSignal(8.76d);

        Assert.Equal(9d, range.Value);
        Assert.Equal(new[] { 5d }, values);

        range.Value = 99d;

        Assert.Equal(10d, range.Value);

        range.AllowGreater = true;
        range.Value = 12d;

        Assert.Equal(12d, range.Value);

        range.Ratio = 0.25d;

        Assert.Equal(2.5d, range.Value);
    }

    [Fact]
    public void TextureRectAndNinePatchExposeSizingState()
    {
        var texture = new TestTexture(30, 10);
        var textureRect = new Electron2D.TextureRect
        {
            Texture = texture,
            ExpandMode = Electron2D.TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = Electron2D.TextureRect.StretchModeEnum.KeepAspectCovered,
            FlipH = true,
            FlipV = true
        };
        var ninePatch = new Electron2D.NinePatchRect
        {
            Texture = texture,
            RegionRect = new Electron2D.Rect2(1f, 2f, 20f, 8f),
            DrawCenter = false,
            PatchMarginLeft = 2,
            PatchMarginTop = 3,
            PatchMarginRight = 4,
            PatchMarginBottom = 5,
            AxisStretchHorizontal = Electron2D.NinePatchRect.AxisStretchModeEnum.TileFit,
            AxisStretchVertical = Electron2D.NinePatchRect.AxisStretchModeEnum.Tile
        };

        Assert.Equal(Electron2D.MouseFilter.Pass, textureRect.MouseFilter);
        Assert.Same(texture, textureRect.Texture);
        Assert.Equal(Electron2D.TextureRect.ExpandModeEnum.IgnoreSize, textureRect.ExpandMode);
        Assert.Equal(Electron2D.TextureRect.StretchModeEnum.KeepAspectCovered, textureRect.StretchMode);
        Assert.True(textureRect.FlipH);
        Assert.True(textureRect.FlipV);
        Assert.Same(texture, ninePatch.Texture);
        Assert.Equal(new Electron2D.Rect2(1f, 2f, 20f, 8f), ninePatch.RegionRect);
        Assert.False(ninePatch.DrawCenter);
        Assert.Equal(new Electron2D.Vector2(6f, 8f), ninePatch.GetMinimumSize());
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
