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

public sealed class ContainerPublicApiTests
{
    [Fact]
    public void ContainersExposeExpectedInheritanceAndEnumValues()
    {
        Assert.True(typeof(Electron2D.Control).IsAssignableFrom(typeof(Electron2D.Container)));
        Assert.True(typeof(Electron2D.Container).IsAssignableFrom(typeof(Electron2D.BoxContainer)));
        Assert.True(typeof(Electron2D.BoxContainer).IsAssignableFrom(typeof(Electron2D.HBoxContainer)));
        Assert.True(typeof(Electron2D.BoxContainer).IsAssignableFrom(typeof(Electron2D.VBoxContainer)));
        Assert.True(typeof(Electron2D.Container).IsAssignableFrom(typeof(Electron2D.GridContainer)));
        Assert.True(typeof(Electron2D.Container).IsAssignableFrom(typeof(Electron2D.MarginContainer)));
        Assert.True(typeof(Electron2D.Container).IsAssignableFrom(typeof(Electron2D.CenterContainer)));
        Assert.True(typeof(Electron2D.Container).IsAssignableFrom(typeof(Electron2D.ScrollContainer)));

        Assert.Equal(0, (int)Electron2D.SizeFlags.ShrinkBegin);
        Assert.Equal(1, (int)Electron2D.SizeFlags.Fill);
        Assert.Equal(2, (int)Electron2D.SizeFlags.Expand);
        Assert.Equal(3, (int)Electron2D.SizeFlags.ExpandFill);
        Assert.Equal(4, (int)Electron2D.SizeFlags.ShrinkCenter);
        Assert.Equal(8, (int)Electron2D.SizeFlags.ShrinkEnd);
        Assert.Equal(0, (int)Electron2D.BoxContainerAlignmentMode.Begin);
        Assert.Equal(1, (int)Electron2D.BoxContainerAlignmentMode.Center);
        Assert.Equal(2, (int)Electron2D.BoxContainerAlignmentMode.End);
        Assert.Equal(0, (int)Electron2D.ScrollMode.Disabled);
        Assert.Equal(1, (int)Electron2D.ScrollMode.Auto);
        Assert.Equal(2, (int)Electron2D.ScrollMode.ShowAlways);
        Assert.Equal(3, (int)Electron2D.ScrollMode.ShowNever);
        Assert.Equal(4, (int)Electron2D.ScrollMode.Reserve);
        Assert.Equal(5, (int)Electron2D.ScrollMode.MaximizeFirst);
        Assert.Equal(0, (int)Electron2D.ScrollHintMode.Disabled);
        Assert.Equal(1, (int)Electron2D.ScrollHintMode.All);
        Assert.Equal(2, (int)Electron2D.ScrollHintMode.TopAndLeft);
        Assert.Equal(3, (int)Electron2D.ScrollHintMode.BottomAndRight);
    }

    [Fact]
    public void ControlExposesContainerLayoutPropertiesAndThemeConstants()
    {
        var control = new Electron2D.Control
        {
            SizeFlagsHorizontal = Electron2D.SizeFlags.ExpandFill,
            SizeFlagsVertical = Electron2D.SizeFlags.ShrinkCenter,
            SizeFlagsStretchRatio = 3f
        };

        control.AddThemeConstantOverride("separation", 12);

        Assert.Equal(Electron2D.SizeFlags.ExpandFill, control.SizeFlagsHorizontal);
        Assert.Equal(Electron2D.SizeFlags.ShrinkCenter, control.SizeFlagsVertical);
        Assert.Equal(3f, control.SizeFlagsStretchRatio);
        Assert.True(control.HasThemeConstantOverride("separation"));
        Assert.Equal(12, control.GetThemeConstant("separation"));
        Assert.Equal(0, control.GetThemeConstant("missing"));
        Assert.Throws<ArgumentOutOfRangeException>(() => control.SizeFlagsStretchRatio = 0f);
        Assert.Throws<ArgumentOutOfRangeException>(() => control.AddThemeConstantOverride("bad", -1));
    }

    [Fact]
    public void ContainerFitChildInRectRequiresDirectControlChild()
    {
        var container = new Electron2D.Container();
        var child = new Electron2D.Control();
        var foreignChild = new Electron2D.Control();

        container.AddChild(child);
        container.FitChildInRect(child, new Electron2D.Rect2(4f, 5f, 20f, 10f));

        Assert.Equal(new Electron2D.Rect2(new Electron2D.Vector2(4f, 5f), new Electron2D.Vector2(20f, 10f)), child.GetRect());
        Assert.Throws<InvalidOperationException>(() => container.FitChildInRect(foreignChild, new Electron2D.Rect2(0f, 0f, 1f, 1f)));
    }
}
