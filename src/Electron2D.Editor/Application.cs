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
using Electron2D.Editor.Shell;

namespace Electron2D.Editor;

internal sealed class Application
{
    private const int DefaultViewportWidth = 1280;
    private const int DefaultViewportHeight = 720;

    public StartupResult Start()
    {
        var runtimeShell = CreateRuntimeShell(ShellLayout.CreateDefault());

        return new StartupResult(
            typeof(Electron2D.ElectronObject).Assembly.GetName().Name ?? "Electron2D",
            runtimeShell.Tree.Root.Name,
            runtimeShell.Viewport.Size,
            runtimeShell.Shell.GetType().FullName ?? runtimeShell.Shell.GetType().Name,
            runtimeShell.Shell.GetChildCount(),
            Electron2D.RenderingServer.CurrentProfile.ToString());
    }

    internal static ShellRuntimeUi CreateRuntimeShell(ShellLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);

        var tree = new Electron2D.SceneTree();
        var viewport = (Electron2D.Viewport)tree.Root;
        viewport.Size = new Electron2D.Vector2I(DefaultViewportWidth, DefaultViewportHeight);

        var shell = CreateShell(layout);
        viewport.AddChild(shell);

        return new ShellRuntimeUi(tree, viewport, shell, CountControls(shell));
    }

    internal static Electron2D.Panel CreateShell(ShellLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);

        var shell = new Electron2D.Panel
        {
            Name = "EditorRoot",
            Position = Electron2D.Vector2.Zero,
            Size = new Electron2D.Vector2(DefaultViewportWidth, DefaultViewportHeight),
            AnchorLeft = 0f,
            AnchorTop = 0f,
            AnchorRight = 1f,
            AnchorBottom = 1f,
            OffsetLeft = 0f,
            OffsetTop = 0f,
            OffsetRight = 0f,
            OffsetBottom = 0f,
            MouseFilter = Electron2D.MouseFilter.Ignore,
            Theme = new Electron2D.Theme
            {
                DefaultFont = new ShellFont(),
                DefaultFontSize = 8
            }
        };
        shell.AddThemeStyleBoxOverride(
            "panel",
            CreateStyleBox(new Electron2D.Color(24f / 255f, 28f / 255f, 33f / 255f, 1f), new Electron2D.Color(24f / 255f, 28f / 255f, 33f / 255f, 1f)));

        foreach (var region in layout.CreateVisualRegions())
        {
            shell.AddChild(CreateRegionControl(layout, region));
        }

        return shell;
    }

    private static Electron2D.Control CreateRegionControl(ShellLayout layout, ShellRegion region)
    {
        return region.Area == "CenterWorkspace"
            ? CreateCenterWorkspace(layout, region)
            : CreateRegionButton(layout, region);
    }

    private static Electron2D.Panel CreateCenterWorkspace(ShellLayout layout, ShellRegion region)
    {
        var panel = CreatePanel(region, new Electron2D.Color(31f / 255f, 36f / 255f, 42f / 255f, 1f));
        var contentY = 24f;
        if (layout.ProjectLoaded)
        {
            panel.AddChild(CreateLabel(
                "EditorShellProjectName",
                "Project " + layout.ProjectName,
                24f,
                contentY,
                region.Width - 48f,
                24f,
                fontSize: 14));
            contentY += 34f;
        }

        var state = layout.GetWorkspaceState(layout.SelectedWorkspace);
        panel.AddChild(CreateLabel(
            "EditorShellWorkspaceCaption",
            "Active workspace",
            24f,
            contentY,
            region.Width - 48f,
            22f,
            fontSize: 13));
        panel.AddChild(CreateLabel(
            "EditorShellWorkspaceName",
            layout.SelectedWorkspace,
            24f,
            contentY + 34f,
            region.Width - 48f,
            40f,
            fontSize: 24));
        panel.AddChild(CreateLabel(
            "EditorShellSelection",
            "Selection " + state.Selection,
            24f,
            contentY + 98f,
            region.Width - 48f,
            24f,
            fontSize: 12));
        panel.AddChild(CreateLabel(
            "EditorShellOpenDocuments",
            state.OpenDocuments.Length == 0 ? "No open documents" : string.Join("  ", state.OpenDocuments),
            24f,
            contentY + 128f,
            region.Width - 48f,
            24f,
            fontSize: 10));

        return panel;
    }

    private static Electron2D.Button CreateRegionButton(ShellLayout layout, ShellRegion region)
    {
        var fill = ResolveRegionFill(layout, region);
        var button = new Electron2D.Button
        {
            Name = "EditorShell" + SanitizeName(region.Area) + SanitizeName(region.Label),
            Text = region.Label,
            Position = new Electron2D.Vector2(region.X, region.Y),
            Size = new Electron2D.Vector2(region.Width, region.Height),
            MouseFilter = Electron2D.MouseFilter.Stop,
            ButtonPressed = region.Area == "WorkspaceSwitcher" && region.Label == layout.SelectedWorkspace
        };
        button.AddThemeColorOverride("normal_color", fill);
        button.AddThemeColorOverride("pressed_color", Lighten(fill, 0.18f));
        button.AddThemeColorOverride("focus_color", Lighten(fill, 0.10f));
        button.AddThemeColorOverride("font_color", new Electron2D.Color(0.91f, 0.94f, 0.97f, 1f));
        button.AddThemeFontSizeOverride("font_size", TextFontSize(region.Area));
        button.Connect("pressed", Electron2D.Callable.From(() => ActivateRegion(layout, region)));

        return button;
    }

    private static Electron2D.Panel CreatePanel(ShellRegion region, Electron2D.Color fill)
    {
        var panel = new Electron2D.Panel
        {
            Name = "EditorShell" + SanitizeName(region.Area) + SanitizeName(region.Label),
            Position = new Electron2D.Vector2(region.X, region.Y),
            Size = new Electron2D.Vector2(region.Width, region.Height),
            MouseFilter = Electron2D.MouseFilter.Ignore
        };
        panel.AddThemeStyleBoxOverride("panel", CreateStyleBox(fill, new Electron2D.Color(83f / 255f, 96f / 255f, 109f / 255f, 1f)));
        return panel;
    }

    private static Electron2D.Label CreateLabel(
        string name,
        string text,
        float x,
        float y,
        float width,
        float height,
        int fontSize)
    {
        var label = new Electron2D.Label
        {
            Name = name,
            Text = text,
            Position = new Electron2D.Vector2(x, y),
            Size = new Electron2D.Vector2(width, height),
            MouseFilter = Electron2D.MouseFilter.Ignore
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        return label;
    }

    private static Electron2D.StyleBoxFlat CreateStyleBox(Electron2D.Color fill, Electron2D.Color border)
    {
        return new Electron2D.StyleBoxFlat
        {
            BgColor = fill,
            BorderColor = border,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1
        };
    }

    private static void ActivateRegion(ShellLayout layout, ShellRegion region)
    {
        if (region.Area == "WorkspaceSwitcher")
        {
            layout.SwitchWorkspace(region.Label);
        }
        else if (region.Area == "BottomPanelToggle")
        {
            layout.ToggleBottomPanel();
        }
    }

    private static Electron2D.Color ResolveRegionFill(ShellLayout layout, ShellRegion region)
    {
        if (region.Area == "WorkspaceSwitcher" && region.Label == layout.SelectedWorkspace)
        {
            return new Electron2D.Color(72f / 255f, 105f / 255f, 117f / 255f, 1f);
        }

        return region.Area switch
        {
            "Menu" => new Electron2D.Color(42f / 255f, 49f / 255f, 58f / 255f, 1f),
            "WorkspaceSwitcher" => new Electron2D.Color(38f / 255f, 58f / 255f, 68f / 255f, 1f),
            "RunControls" => new Electron2D.Color(55f / 255f, 67f / 255f, 51f / 255f, 1f),
            "DocumentTabs" => new Electron2D.Color(35f / 255f, 40f / 255f, 48f / 255f, 1f),
            "LeftDock" => new Electron2D.Color(33f / 255f, 42f / 255f, 51f / 255f, 1f),
            "RightDock" => new Electron2D.Color(39f / 255f, 42f / 255f, 54f / 255f, 1f),
            "BottomPanel" => new Electron2D.Color(30f / 255f, 35f / 255f, 42f / 255f, 1f),
            "BottomPanelTab" => new Electron2D.Color(52f / 255f, 58f / 255f, 67f / 255f, 1f),
            "BottomPanelToggle" => new Electron2D.Color(76f / 255f, 62f / 255f, 44f / 255f, 1f),
            _ => new Electron2D.Color(40f / 255f, 45f / 255f, 52f / 255f, 1f)
        };
    }

    private static Electron2D.Color Lighten(Electron2D.Color color, float amount)
    {
        return new Electron2D.Color(
            Math.Clamp(color.R + amount, 0f, 1f),
            Math.Clamp(color.G + amount, 0f, 1f),
            Math.Clamp(color.B + amount, 0f, 1f),
            color.A);
    }

    private static int TextFontSize(string area)
    {
        return area is "Menu" or "WorkspaceSwitcher" or "RunControls" or "DocumentTabs" or "BottomPanelTab" or "BottomPanelToggle"
            ? 8
            : 12;
    }

    private static int CountControls(Electron2D.Node node)
    {
        var count = node is Electron2D.Control ? 1 : 0;
        for (var index = 0; index < node.GetChildCount(); index++)
        {
            if (node.GetChild(index) is { } child)
            {
                count += CountControls(child);
            }
        }

        return count;
    }

    private static string SanitizeName(string value)
    {
        return string.Concat(value.Where(char.IsLetterOrDigit));
    }
}

internal sealed record ShellRuntimeUi(
    Electron2D.SceneTree Tree,
    Electron2D.Viewport Viewport,
    Electron2D.Panel Shell,
    int ControlCount);

internal sealed class ShellFont : Electron2D.Font;
