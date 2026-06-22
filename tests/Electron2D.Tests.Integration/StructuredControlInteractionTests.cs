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
public sealed class StructuredControlInteractionTests
{
    [Fact]
    public void ItemListSelectsActivatesAndSkipsDisabledItems()
    {
        ResetInputState();

        var tree = new Electron2D.SceneTree();
        var list = new Electron2D.ItemList
        {
            Position = new Electron2D.Vector2(0f, 0f),
            Size = new Electron2D.Vector2(120f, 72f)
        };
        var selected = new List<int>();
        var activated = new List<int>();
        list.AddItem("Scene");
        list.AddItem("Inspector");
        list.AddItem("Disabled");
        list.SetItemDisabled(2, true);
        list.Connect("item_selected", Electron2D.Callable.From<int>(selected.Add));
        list.Connect("item_activated", Electron2D.Callable.From<int>(activated.Add));
        tree.Root.AddChild(list);

        tree.DispatchInput(MouseButton(new Electron2D.Vector2(8f, 28f), true));
        tree.DispatchInput(MouseButton(new Electron2D.Vector2(8f, 52f), true));
        tree.DispatchInput(MouseDoubleClick(new Electron2D.Vector2(8f, 28f)));

        Assert.Equal(new[] { 1 }, selected);
        Assert.Equal(new[] { 1 }, list.GetSelectedItems());
        Assert.Equal(new[] { 1 }, activated);
    }

    [Fact]
    public void TreeSelectsVisibleItemsAndSkipsCollapsedDescendants()
    {
        ResetInputState();

        var sceneTree = new Electron2D.SceneTree();
        var tree = new Electron2D.Tree
        {
            Position = new Electron2D.Vector2(0f, 0f),
            Size = new Electron2D.Vector2(160f, 96f),
            HideRoot = true
        };
        var root = tree.CreateItem();
        var folder = tree.CreateItem(root);
        var hiddenChild = tree.CreateItem(folder);
        var sibling = tree.CreateItem(root);
        folder.SetText(0, "Folder");
        hiddenChild.SetText(0, "Hidden");
        sibling.SetText(0, "Sibling");
        folder.SetCollapsed(true);
        var selected = new List<string>();
        tree.Connect("item_selected", Electron2D.Callable.From(() => selected.Add("selected")));
        sceneTree.Root.AddChild(tree);

        sceneTree.DispatchInput(MouseButton(new Electron2D.Vector2(8f, 28f), true));

        Assert.Same(sibling, tree.GetSelected());
        Assert.Equal(new[] { "selected" }, selected);
        Assert.Same(sibling, tree.GetItemAtPosition(new Electron2D.Vector2(8f, 28f)));
        Assert.Same(folder, tree.GetItemAtPosition(new Electron2D.Vector2(8f, 4f)));
    }

    [Fact]
    public void PopupMenuActivatesEnabledItemsAndHidesAfterPress()
    {
        ResetInputState();

        var tree = new Electron2D.SceneTree();
        var menu = new Electron2D.PopupMenu();
        var indices = new List<int>();
        var ids = new List<int>();
        menu.AddItem("Run", 10);
        menu.AddItem("Disabled", 20);
        menu.SetItemDisabled(1, true);
        menu.AddSeparator();
        menu.Popup(new Electron2D.Rect2(0f, 0f, 120f, 72f));
        menu.Connect("index_pressed", Electron2D.Callable.From<int>(indices.Add));
        menu.Connect("id_pressed", Electron2D.Callable.From<int>(ids.Add));
        tree.Root.AddChild(menu);

        tree.DispatchInput(MouseButton(new Electron2D.Vector2(8f, 28f), true));
        tree.DispatchInput(MouseButton(new Electron2D.Vector2(8f, 4f), true));

        Assert.Equal(new[] { 0 }, indices);
        Assert.Equal(new[] { 10 }, ids);
        Assert.False(menu.Visible);
    }

    [Fact]
    public void TabContainerSwitchesCurrentTabFromPointer()
    {
        ResetInputState();

        var tree = new Electron2D.SceneTree();
        var tabs = new Electron2D.TabContainer
        {
            Position = new Electron2D.Vector2(0f, 0f),
            Size = new Electron2D.Vector2(160f, 80f)
        };
        var first = new Electron2D.Control { Name = "Scene" };
        var second = new Electron2D.Control { Name = "Inspector" };
        var changed = new List<int>();
        tabs.AddChild(first);
        tabs.AddChild(second);
        tabs.SetTabTitle(0, "Scene");
        tabs.SetTabTitle(1, "Inspector");
        tabs.Connect("tab_changed", Electron2D.Callable.From<int>(changed.Add));
        tree.Root.AddChild(tabs);

        tree.DispatchInput(MouseButton(new Electron2D.Vector2(68f, 8f), true));

        Assert.Equal(1, tabs.CurrentTab);
        Assert.False(first.Visible);
        Assert.True(second.Visible);
        Assert.Equal(new[] { 1 }, changed);
    }

    private static Electron2D.InputEventMouseButton MouseButton(Electron2D.Vector2 position, bool pressed)
    {
        return new Electron2D.InputEventMouseButton
        {
            ButtonIndex = Electron2D.MouseButton.Left,
            Pressed = pressed,
            Position = position,
            GlobalPosition = position
        };
    }

    private static Electron2D.InputEventMouseButton MouseDoubleClick(Electron2D.Vector2 position)
    {
        return new Electron2D.InputEventMouseButton
        {
            ButtonIndex = Electron2D.MouseButton.Left,
            Pressed = true,
            DoubleClick = true,
            Position = position,
            GlobalPosition = position
        };
    }

    private static void ResetInputState()
    {
        Electron2D.Input.ResetForTests();
        Electron2D.InputMap.ClearForTests();
    }
}
