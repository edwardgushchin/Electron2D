using Electron2D;
using SDL3;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyMenuBarRendering(string backend)
    {
        var root = new Window { Size = new(480, 280), GUIEmbedSubwindows = true };
        var bar = new MenuBar { Position = new(20, 20), Size = new(400, 40) };
        var file = new PopupMenu { Name = "File" }; file.AddItem("Open", 42); var edit = new PopupMenu { Name = "Edit" }; edit.AddItem("Copy command", 84);
        bar.AddChild(file); bar.AddChild(edit); root.AddChild(bar); var selected = 0; edit.IDPressed += id => selected = id;
        using var normal = new StyleBoxFlat { BGColor = new(.8f, .05f, .05f), ContentMarginLeft = 6, ContentMarginRight = 6, ContentMarginTop = 4, ContentMarginBottom = 4 };
        using var mirrored = new StyleBoxFlat { BGColor = new(.05f, .1f, .8f), ContentMarginLeft = 6, ContentMarginRight = 6, ContentMarginTop = 4, ContentMarginBottom = 4 };
        bar.AddThemeStyleBoxOverride("normal", normal); bar.AddThemeStyleBoxOverride("normal_mirrored", mirrored); var frame = 0;
        root.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.FramePostDraw += () =>
            {
                frame++; if (frame <= 3) return; var step = frame - 3;
                if (step == 1) { DialogClick(MenuBarTests.Point(bar, 0)); return; }
                if (step == 2) { Check(file.Visible && file.GetFocusedItem() == -1, "Native pointer opens menu strip."); MenuPointer(MenuBarTests.Point(bar, 1)); return; }
                if (step == 3) { Check(edit.Visible && !file.Visible && edit.GetFocusedItem() == -1, "Native hover switches strip popup."); OptionKey(SDL.Keycode.Left); return; }
                if (step == 4) { Check(file.Visible && !edit.Visible && file.GetFocusedItem() == 0, "Native popup left selects prior header."); OptionKey(SDL.Keycode.Right); return; }
                if (step == 5)
                {
                    Check(edit.Visible && !file.Visible && edit.GetFocusedItem() == 0, "Native popup right selects next header.");
                    using var pixels = RenderingServer.Service!.Readback(); var bright = 0;
                    for (var y = edit.Position.Y; y < edit.Position.Y + edit.Size.Y; y++) for (var x = edit.Position.X; x < edit.Position.X + edit.Size.X; x++) { var color = pixels.GetPixel(x, y); if (color.R > .65f && color.G > .65f && color.B > .65f) bright++; }
                    Check(bright > 80, "Rendered command text inside actual popup."); File.WriteAllBytes(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"electron2d-menubar-open-{backend}.png"), pixels.SavePNGToBuffer()); OptionKey(SDL.Keycode.Return); return;
                }
                if (step == 6) { Check(selected == 84 && !edit.Visible, "Native strip command dispatch."); MenuPointer(new(440, 200)); bar.LayoutDirection = LayoutDirection.RTL; return; }
                if (step == 7)
                {
                    using var pixels = RenderingServer.Service!.Readback(); File.WriteAllBytes(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"electron2d-menubar-{backend}.png"), pixels.SavePNGToBuffer()); var sample = bar.GetGlobalTransformWithCanvas() * new Vector2(MenuBarTests.LocalPoint(bar, 0).X, 2); var blue = pixels.GetPixel((int)sample.X, (int)sample.Y);
                    Check(blue.B > .7f && blue.R < .1f, $"Mirrored item style pixels {backend}: {blue}, sample={sample}, bar={bar.GetGlobalRect()}.");
                    var bright = 0; for (var y = 20; y < 50; y++) for (var x = 0; x < 480; x++) { var c = pixels.GetPixel(x, y); if (c.R > .65f && c.G > .65f && c.B > .65f) bright++; }
                    Check(bright > 50, "Menu strip shaped title pixels."); File.WriteAllBytes(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"electron2d-menubar-{backend}.png"), pixels.SavePNGToBuffer()); bar.Flat = true; return;
                }
                if (step == 8) { using var pixels = RenderingServer.Service!.Readback(); var sample = bar.GetGlobalTransformWithCanvas() * new Vector2(MenuBarTests.LocalPoint(bar, 0).X, 2); var black = pixels.GetPixel((int)sample.X, (int)sample.Y); Check(black.R < .02f && black.B < .02f, "Flat suppresses header decoration."); bar.Flat = false; bar.SetMenuHidden(1, true); return; }
                Check(bar.GetMinimumSize().X < 60, "Hidden header updates strip minimum."); root.Tree!.Quit();
            };
        };
        Engine.Run(root); Released(root); Check(frame == 12, "Native menu strip sequence completed."); VerifyMenuBarWarm(backend);
        Console.WriteLine($"MenuBar {backend}: SDL pointer/hover, popup keyboard switching/command, RTL mirrored styles, flat/hidden layout and title pixels passed.");
    }
    private static void VerifyMenuBarWarm(string backend)
    {
        var root = new Window { Size = new(380, 240), GUIEmbedSubwindows = true }; var bar = new MenuBar { Position = new(20, 20), Size = new(300, 40) }; var popup = new PopupMenu { Name = "File" }; popup.AddItem("Open"); popup.AddItem("Save"); bar.AddChild(popup); root.AddChild(bar); var frame = 0; long before = 0, bytes = 0;
        root.Ready += _ =>
        {
            popup.Popup(new(20, 55, 140, 90));
            root.Tree!.ProcessFrameStarted += _ => { before = GC.GetAllocatedBytesForCurrentThread(); bar.SetMenuTitle(0, (frame & 1) == 0 ? "File" : "Edit"); popup.SetFocusedItem(frame & 1); };
            RenderingServer.FramePostDraw += () => { if (frame >= 32) bytes += GC.GetAllocatedBytesForCurrentThread() - before; if (++frame == 96) root.Tree!.Quit(); };
        };
        Engine.Run(root); Released(root); Check(frame == 96 && bytes == 0, $"MenuBar {backend} title/focus/render allocated {bytes} managed bytes over 64 warmed frames."); Console.WriteLine($"MenuBar {backend}: 64 warmed title/focus/render frames, {bytes} managed bytes.");
    }
}
