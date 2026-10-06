using Electron2D;
using SDL3;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyFileDialogRendering(string backend)
    {
        using var temp = DirAccess.CreateTemp("electron2d-picker-render"); var path = System.IO.Path.Combine(temp.GetCurrentDir(), "document.txt"); File.WriteAllText(path, "fixture");
        var root = new Window { Size = new(900, 600), GUIEmbedSubwindows = true }; var dialog = new FileDialog { Access = FileDialogAccess.FileSystem, CurrentDir = temp.GetCurrentDir(), FileMode = FileDialogMode.OpenFile, DisplayMode = FileDialog.DisplayModeType.List }; dialog.AddFilter("*.txt", "Text"); root.AddChild(dialog); var selected = ""; dialog.FileSelected += value => selected = value; var frame = 0;
        root.Ready += _ =>
        {
            dialog.PopupFileDialog(); RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.FramePostDraw += () =>
        {
            frame++; if (frame <= 3) return;
            if (frame == 4)
            {
                using var pixels = RenderingServer.Service!.Readback(); var light = 0; for (var y = 90; y < 510; y++) for (var x = 100; x < 800; x++) { var c = pixels.GetPixel(x, y); if (c.R > .65f && c.G > .65f && c.B > .65f) light++; }
                Check(light > 300, $"FileDialog {backend} browser and toolbar pixels ({light})."); File.WriteAllBytes(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"electron2d-filedialog-{backend}.png"), pixels.SavePNGToBuffer());
                var list = (ItemList)dialog.GetVBox().GetNode("Browser/Files"); list.GrabFocus(); OptionKey(SDL.Keycode.Down); return;
            }
            if (frame == 5) { Check(dialog.GetLineEdit().Text == "document.txt", "Native key selects the real file row."); DialogClick((Vector2)dialog.Position + dialog.GetOKButton().GetGlobalRect().GetCenter()); return; }
            Check(selected == path && !dialog.Visible, "Native pointer accepts selected path after hide."); root.Tree!.Quit();
        };
        };
        Engine.Run(root); Released(root); Check(frame == 6, "File browser native sequence completed.");
        var warmRoot = new Window { Size = new(900, 600), GUIEmbedSubwindows = true }; var warm = new FileDialog { Access = FileDialogAccess.FileSystem, CurrentDir = temp.GetCurrentDir(), FileMode = FileDialogMode.OpenFile, DisplayMode = FileDialog.DisplayModeType.Thumbnails }; warmRoot.AddChild(warm); frame = 0; long before = 0, bytes = 0;
        warmRoot.Ready += _ => { warm.PopupFileDialog(); warmRoot.Tree!.ProcessFrameStarted += _ => before = GC.GetAllocatedBytesForCurrentThread(); RenderingServer.FramePostDraw += () => { if (frame == 30) { using var pixels = RenderingServer.Service!.Readback(); File.WriteAllBytes(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"electron2d-filedialog-thumbnails-{backend}.png"), pixels.SavePNGToBuffer()); } if (frame >= 32) bytes += GC.GetAllocatedBytesForCurrentThread() - before; if (++frame == 96) warmRoot.Tree!.Quit(); }; };
        Engine.Run(warmRoot); Released(warmRoot); Check(frame == 96 && bytes == 0, $"FileDialog {backend}: warm retained rendering allocated {bytes} managed bytes."); Console.WriteLine($"FileDialog {backend}: native key/pointer, browser pixels, 64 warmed retained frames with {bytes} managed bytes passed.");
    }
}
