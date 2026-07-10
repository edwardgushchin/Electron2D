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
using System.Reflection;
using Xunit;

namespace Electron2D.Tests.Unit;

public sealed class StructuredControlsPublicApiTests
{
    [Fact]
    public void StructuredControlsExposeExpectedInheritanceSignalsAndEnumValues()
    {
        Assert.True(typeof(Electron2D.Control).IsAssignableFrom(typeof(Electron2D.ItemList)));
        Assert.True(typeof(Electron2D.Control).IsAssignableFrom(typeof(Electron2D.Tree)));
        Assert.True(typeof(Electron2D.ElectronObject).IsAssignableFrom(typeof(Electron2D.TreeItem)));
        Assert.True(typeof(Electron2D.Control).IsAssignableFrom(typeof(Electron2D.PopupMenu)));
        Assert.True(typeof(Electron2D.Container).IsAssignableFrom(typeof(Electron2D.TabContainer)));

        Assert.Equal(0, (int)Electron2D.ItemList.SelectModeEnum.Single);
        Assert.Equal(1, (int)Electron2D.ItemList.SelectModeEnum.Multi);
        Assert.Equal(0, (int)Electron2D.Tree.SelectModeEnum.Single);
        Assert.Equal(1, (int)Electron2D.Tree.SelectModeEnum.Row);
        Assert.Equal(2, (int)Electron2D.Tree.SelectModeEnum.Multi);

        var itemList = new Electron2D.ItemList();
        var tree = new Electron2D.Tree();
        var popupMenu = new Electron2D.PopupMenu();
        var tabContainer = new Electron2D.TabContainer();

        Assert.True(itemList.HasSignal("item_selected"));
        Assert.True(itemList.HasSignal("multi_selected"));
        Assert.True(itemList.HasSignal("item_activated"));
        Assert.True(tree.HasSignal("item_selected"));
        Assert.True(tree.HasSignal("multi_selected"));
        Assert.True(tree.HasSignal("item_activated"));
        Assert.True(tree.HasSignal("item_collapsed"));
        Assert.True(popupMenu.HasSignal("id_pressed"));
        Assert.True(popupMenu.HasSignal("index_pressed"));
        Assert.True(tabContainer.HasSignal("tab_changed"));
        Assert.Equal(Electron2D.FocusMode.All, itemList.FocusMode);
        Assert.Equal(Electron2D.FocusMode.All, tree.FocusMode);
        Assert.Equal(Electron2D.FocusMode.All, popupMenu.FocusMode);
    }

    [Fact]
    public void EditorPrivateWidgetsAreNotExportedAsRuntimePublicApi()
    {
        var assembly = Assembly.Load("Electron2D");

        Assert.Null(assembly.GetType("Electron2D.PropertyGrid"));
        Assert.Null(assembly.GetType("Electron2D.Dock"));
        Assert.Null(assembly.GetType("Electron2D.CodeDiagnosticsView"));
    }

    [Fact]
    public void ItemListStoresItemsSelectionAndFlags()
    {
        var icon = new TestTexture(16, 8);
        var itemList = new Electron2D.ItemList
        {
            SelectMode = Electron2D.ItemList.SelectModeEnum.Multi,
            AllowReselect = true,
            AllowRmbSelect = true,
            MaxColumns = 2,
            FixedColumnWidth = 80,
            FixedIconSize = new Electron2D.Vector2I(12, 10)
        };

        var first = itemList.AddItem("Scene", icon);
        var second = itemList.AddItem("Inspector");

        itemList.SetItemText(second, "Inspector Dock");
        itemList.SetItemIcon(second, icon);
        itemList.SetItemDisabled(second, true);
        itemList.SetItemSelectable(second, false);
        itemList.Select(first);
        itemList.Select(second);

        Assert.Equal(0, first);
        Assert.Equal(1, second);
        Assert.Equal(2, itemList.ItemCount);
        Assert.Equal("Scene", itemList.GetItemText(first));
        Assert.Equal("Inspector Dock", itemList.GetItemText(second));
        Assert.Same(icon, itemList.GetItemIcon(first));
        Assert.Same(icon, itemList.GetItemIcon(second));
        Assert.True(itemList.IsItemDisabled(second));
        Assert.False(itemList.IsItemSelectable(second));
        Assert.True(itemList.IsSelected(first));
        Assert.False(itemList.IsSelected(second));
        Assert.Equal(new[] { first }, itemList.GetSelectedItems());
        Assert.Equal(0, itemList.GetItemAtPosition(new Electron2D.Vector2(4f, 4f)));

        itemList.Deselect(first);
        Assert.Empty(itemList.GetSelectedItems());

        itemList.Clear();
        Assert.Equal(0, itemList.ItemCount);
    }

    [Fact]
    public void TreeCreatesItemsAndTracksSelection()
    {
        var icon = new TestTexture(10, 10);
        var tree = new Electron2D.Tree
        {
            Columns = 2,
            HideRoot = true,
            SelectMode = Electron2D.Tree.SelectModeEnum.Row
        };

        tree.SetColumnTitle(0, "Name");
        tree.SetColumnTitle(1, "Type");
        var root = tree.CreateItem();
        var player = tree.CreateItem(root);
        var camera = tree.CreateItem(root, 0);

        player.SetText(0, "Player");
        player.SetIcon(0, icon);
        player.SetSelectable(0, true);
        camera.SetText(0, "Camera");
        camera.SetCollapsed(true);
        player.Select(0);

        Assert.Same(root, tree.GetRoot());
        Assert.Same(camera, root.GetChild(0));
        Assert.Same(player, root.GetChild(1));
        Assert.Same(player, camera.GetNext());
        Assert.Same(root, player.GetParent());
        Assert.Same(tree, player.GetTree());
        Assert.Equal("Name", tree.GetColumnTitle(0));
        Assert.Equal("Player", player.GetText(0));
        Assert.Same(icon, player.GetIcon(0));
        Assert.True(player.IsSelectable(0));
        Assert.True(player.IsSelected(0));
        Assert.Same(player, tree.GetSelected());
        Assert.Equal(0, tree.GetSelectedColumn());
        Assert.True(camera.IsCollapsed());

        tree.DeselectAll();
        Assert.Null(tree.GetSelected());
        Assert.False(player.IsSelected(0));
    }

    [Fact]
    public void PopupMenuStoresItemsAndPopupState()
    {
        var icon = new TestTexture(8, 8);
        var menu = new Electron2D.PopupMenu();

        menu.AddItem("Run", 42);
        menu.AddIconCheckItem(icon, "Debug", 7);
        menu.AddSeparator("Group");
        menu.SetItemChecked(1, true);
        menu.SetItemDisabled(0, true);
        menu.SetItemText(0, "Run Scene");
        menu.Popup(new Electron2D.Rect2(4f, 5f, 100f, 72f));

        Assert.Equal(3, menu.GetItemCount());
        Assert.Equal("Run Scene", menu.GetItemText(0));
        Assert.Equal(42, menu.GetItemId(0));
        Assert.Same(icon, menu.GetItemIcon(1));
        Assert.True(menu.IsItemChecked(1));
        Assert.True(menu.IsItemDisabled(0));
        Assert.True(menu.IsItemSeparator(2));
        Assert.True(menu.Visible);
        Assert.Equal(new Electron2D.Rect2(new Electron2D.Vector2(4f, 5f), new Electron2D.Vector2(100f, 72f)), menu.GetRect());

        menu.Clear();
        Assert.Equal(0, menu.GetItemCount());
    }

    [Fact]
    public void TabContainerTracksTabsTitlesAndCurrentPage()
    {
        var icon = new TestTexture(8, 8);
        var tabs = new Electron2D.TabContainer();
        var scene = new Electron2D.Control { Name = "Scene" };
        var inspector = new Electron2D.Control { Name = "Inspector" };

        tabs.AddChild(scene);
        tabs.AddChild(inspector);
        tabs.SetTabTitle(0, "Scene");
        tabs.SetTabTitle(1, "Inspector");
        tabs.SetTabIcon(1, icon);
        tabs.SetTabDisabled(0, true);
        tabs.CurrentTab = 1;

        Assert.Equal(2, tabs.GetTabCount());
        Assert.Same(scene, tabs.GetTabControl(0));
        Assert.Same(inspector, tabs.GetCurrentTabControl());
        Assert.Equal(1, tabs.CurrentTab);
        Assert.Equal(0, tabs.GetPreviousTab());
        Assert.Equal("Scene", tabs.GetTabTitle(0));
        Assert.Equal("Inspector", tabs.GetTabTitle(1));
        Assert.Same(icon, tabs.GetTabIcon(1));
        Assert.True(tabs.IsTabDisabled(0));
        Assert.Equal(1, tabs.GetTabIdxFromControl(inspector));
        Assert.False(scene.Visible);
        Assert.True(inspector.Visible);

        Assert.False(tabs.SelectPreviousAvailable());

        tabs.SetTabDisabled(0, false);
        Assert.True(tabs.SelectPreviousAvailable());
        Assert.Equal(0, tabs.CurrentTab);
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
