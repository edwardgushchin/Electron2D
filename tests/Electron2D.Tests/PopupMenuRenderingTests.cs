using Electron2D;
using SDL3;
using Image = Electron2D.Image;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyPopupMenuRendering(string backend)
    {
        using var image = Image.CreateEmpty(8, 8, false, Image.Format.Rgba8); image.Fill(Colors.Blue); using var icon = ImageTexture.CreateFromImage(image);
        var root = new Window { Size = new(360, 230), GUIEmbedSubwindows = true }; var menu = new PopupMenu { Name = "MainMenu" };
        menu.AddIconItem(icon, "Open", 10); menu.AddCheckItem("Checked", 20); menu.SetItemChecked(1, true); menu.AddRadioCheckItem("Disabled", 30); menu.SetItemDisabled(2, true); menu.AddSeparator("Group");
        var child = new PopupMenu { Name = "Submenu" }; child.AddItem("Child item"); menu.AddSubmenuNodeItem("More", child, 40); root.AddChild(menu);
        var frame = 0;
        root.Ready += _ =>
        {
            menu.Popup(new(new(20, 20), new(180, 160))); menu.SetFocusedItem(0); RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                using var pixels = RenderingServer.Service!.Readback(); frame++;
                var bright = 0; var blue = 0; for (var y = 20; y < 200; y++) for (var x = 20; x < 200; x++) { var color = pixels.GetPixel(x, y); if (color.R > .65f && color.G > .65f && color.B > .65f) bright++; if (color.B > .9f && color.R < .1f && color.G < .1f) blue++; }
                Check(bright > 60 && blue >= 40, $"Menu {backend} renders shaped text, check/radio icons and borrowed blue icon ({bright}/{blue}).");
                if (frame == 1) { menu.SetFocusedItem(4); var native = new SDL.Event { Type = (uint)SDL.EventType.KeyDown }; native.Key.WindowID = SDL.GetWindowID(SDL.GetWindows(out var nativeCount)![0]); native.Key.Key = SDL.Keycode.Right; native.Key.Down = true; SDL.PushEvent(ref native); return; }
                Check(child.Visible && child.Position.X >= menu.Position.X + menu.Size.X - 12, "Nested menu opens beside parent.");
                File.WriteAllBytes(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"electron2d-menu-{backend}.png"), pixels.SavePNGToBuffer()); root.Tree!.Quit();
            };
        };
        Engine.Run(root); Released(root); Check(frame == 2, "Menu renderer completed."); VerifyPopupMenuWarm(backend); Console.WriteLine($"PopupMenu text/icons/selection/separators and submenu rendered on {backend}.");
    }
    private static void VerifyPopupMenuWarm(string backend)
    {
        var root = new Window { Size = new(220, 140), GUIEmbedSubwindows = true }; var menu = new PopupMenu { Name = "WarmMenu" }; menu.AddCheckItem("Alpha"); menu.SetItemChecked(0, true); menu.AddRadioCheckItem("Beta"); root.AddChild(menu); var frame = 0; long before = 0, bytes = 0;
        root.Ready += _ => { menu.PopupCentered(); root.Tree!.ProcessFrameStarted += _ => { before = GC.GetAllocatedBytesForCurrentThread(); menu.SetFocusedItem(frame & 1); }; RenderingServer.FramePostDraw += () => { if (frame >= 32) bytes += GC.GetAllocatedBytesForCurrentThread() - before; if (++frame == 96) root.Tree!.Quit(); }; };
        Engine.Run(root); Released(root); Check(frame == 96 && bytes == 0, $"Menu {backend} warmed focus/layout/render allocated {bytes} managed bytes over 64 frames."); Console.WriteLine($"PopupMenu {backend}: 64 warmed focus/render frames, {bytes} managed bytes.");
    }
}
