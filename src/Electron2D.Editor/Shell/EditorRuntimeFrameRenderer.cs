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
namespace Electron2D.Editor.Shell;

internal static class EditorRuntimeFrameRenderer
{
    public static EditorRuntimeFrame Render(EditorShellLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);

        var shell = EditorApplication.CreateShell(layout);
        var regions = layout.CreateVisualRegions();
        var canvas = new PixelCanvas(EditorShellLayout.DefaultViewportWidth, EditorShellLayout.DefaultViewportHeight);
        var drawCommands = 0;

        DrawClear(canvas, new Rgba(24, 28, 33), ref drawCommands);
        DrawArea(canvas, regions, layout, "Menu", new Rgba(42, 49, 58), new Rgba(210, 219, 229), ref drawCommands);
        DrawArea(canvas, regions, layout, "WorkspaceSwitcher", new Rgba(38, 58, 68), new Rgba(230, 246, 248), ref drawCommands);
        DrawArea(canvas, regions, layout, "RunControls", new Rgba(55, 67, 51), new Rgba(228, 240, 219), ref drawCommands);
        DrawArea(canvas, regions, layout, "DocumentTabs", new Rgba(35, 40, 48), new Rgba(218, 224, 232), ref drawCommands);
        DrawArea(canvas, regions, layout, "LeftDock", new Rgba(33, 42, 51), new Rgba(221, 228, 236), ref drawCommands);
        DrawArea(canvas, regions, layout, "RightDock", new Rgba(39, 42, 54), new Rgba(221, 228, 236), ref drawCommands);
        DrawArea(canvas, regions, layout, "BottomPanel", new Rgba(30, 35, 42), new Rgba(205, 214, 224), ref drawCommands);
        DrawArea(canvas, regions, layout, "BottomPanelTab", new Rgba(52, 58, 67), new Rgba(231, 236, 242), ref drawCommands);
        DrawArea(canvas, regions, layout, "BottomPanelToggle", new Rgba(76, 62, 44), new Rgba(255, 239, 218), ref drawCommands);
        DrawCenterWorkspace(canvas, regions.Single(region => region.Area == "CenterWorkspace"), layout, ref drawCommands);

        return new EditorRuntimeFrame(
            canvas,
            drawCommands,
            CalculateRedDominantPixelRatio(canvas),
            shell.GetChildCount());
    }

    private static void DrawClear(PixelCanvas canvas, Rgba color, ref int drawCommands)
    {
        canvas.Clear(color);
        drawCommands++;
    }

    private static void DrawArea(
        PixelCanvas canvas,
        IReadOnlyList<EditorShellRegion> regions,
        EditorShellLayout layout,
        string area,
        Rgba fill,
        Rgba text,
        ref int drawCommands)
    {
        foreach (var region in regions.Where(region => region.Area == area))
        {
            var regionFill = area == "WorkspaceSwitcher" && region.Label == layout.SelectedWorkspace
                ? new Rgba(72, 105, 117)
                : fill;
            DrawFilledRectangle(canvas, region, regionFill, ref drawCommands);
            DrawRectangle(canvas, region, new Rgba(91, 103, 116), ref drawCommands);
            DrawText(canvas, region.Label, region.X + 8, TextY(region), text, TextScale(region.Area), ref drawCommands);
        }
    }

    private static void DrawCenterWorkspace(
        PixelCanvas canvas,
        EditorShellRegion center,
        EditorShellLayout layout,
        ref int drawCommands)
    {
        DrawFilledRectangle(canvas, center, new Rgba(31, 36, 42), ref drawCommands);
        DrawRectangle(canvas, center, new Rgba(83, 96, 109), ref drawCommands);

        var contentY = center.Y + 26;
        if (layout.ProjectLoaded)
        {
            DrawText(
                canvas,
                "PROJECT " + layout.ProjectName.ToUpperInvariant(),
                center.X + 24,
                contentY,
                new Rgba(166, 180, 194),
                scale: 2,
                ref drawCommands);
            contentY += 32;
        }

        var state = layout.GetWorkspaceState(layout.SelectedWorkspace);
        DrawText(canvas, "ACTIVE WORKSPACE", center.X + 24, contentY, new Rgba(159, 174, 188), scale: 2, ref drawCommands);
        DrawText(canvas, layout.SelectedWorkspace.ToUpperInvariant(), center.X + 24, contentY + 32, new Rgba(246, 249, 252), scale: 4, ref drawCommands);
        DrawText(canvas, "SELECTION " + state.Selection.ToUpperInvariant(), center.X + 24, contentY + 100, new Rgba(190, 212, 196), scale: 2, ref drawCommands);
        DrawText(canvas, "OPEN DOCUMENTS PRESERVED", center.X + 24, contentY + 134, new Rgba(190, 212, 196), scale: 2, ref drawCommands);

        var documents = state.OpenDocuments.Length == 0
            ? "NO OPEN DOCUMENTS"
            : string.Join("  ", state.OpenDocuments).ToUpperInvariant();
        DrawText(canvas, documents, center.X + 24, contentY + 168, new Rgba(214, 221, 229), scale: 1, ref drawCommands);
    }

    private static void DrawFilledRectangle(PixelCanvas canvas, EditorShellRegion region, Rgba color, ref int drawCommands)
    {
        canvas.FillRectangle(region.X, region.Y, region.Width, region.Height, color);
        drawCommands++;
    }

    private static void DrawRectangle(PixelCanvas canvas, EditorShellRegion region, Rgba color, ref int drawCommands)
    {
        canvas.DrawRectangle(region.X, region.Y, region.Width, region.Height, color);
        drawCommands++;
    }

    private static void DrawText(PixelCanvas canvas, string text, int x, int y, Rgba color, int scale, ref int drawCommands)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        canvas.DrawText(text, x, y, color, scale);
        drawCommands++;
    }

    private static int TextY(EditorShellRegion region)
    {
        return region.Area == "BottomPanel" ? region.Y + 54 : region.Y + 7;
    }

    private static int TextScale(string area)
    {
        return area is "Menu" or "WorkspaceSwitcher" or "RunControls" or "DocumentTabs" or "BottomPanelTab" or "BottomPanelToggle"
            ? 1
            : 2;
    }

    private static double CalculateRedDominantPixelRatio(PixelCanvas canvas)
    {
        var redDominant = 0;
        var total = canvas.Pixels.Length / 4;
        for (var index = 0; index < canvas.Pixels.Length; index += 4)
        {
            var red = canvas.Pixels[index];
            var green = canvas.Pixels[index + 1];
            var blue = canvas.Pixels[index + 2];
            if (red > 200 && red > green * 2 && red > blue * 2)
            {
                redDominant++;
            }
        }

        return (double)redDominant / total;
    }
}

internal sealed record EditorRuntimeFrame(
    PixelCanvas Canvas,
    int DrawCommands,
    double RedDominantPixelRatio,
    int ControlCount);
