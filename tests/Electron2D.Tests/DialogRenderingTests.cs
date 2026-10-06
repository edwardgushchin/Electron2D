using Electron2D;
using SDL3;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyDialogRendering(string backend)
    {
        var root = new Window { Size = new(540, 330), GUIEmbedSubwindows = true }; var dialog = new ConfirmationDialog { DialogText = "Apply the selected settings?", OKButtonText = "Apply", CancelButtonText = "Back" }; var confirmed = 0; var canceled = 0; var custom = 0; dialog.Confirmed += () => confirmed++; dialog.Canceled += () => canceled++; dialog.CustomAction += _ => custom++; var help = dialog.AddButton("Help", action: "help"); root.AddChild(dialog); var frame = 0;
        root.Ready += _ =>
        {
            dialog.PopupCentered(new(380, 180)); RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.FramePostDraw += () =>
        {
            frame++; using var pixels = RenderingServer.Service!.Readback();
            if (frame == 1)
            {
                var light = 0; for (var y = 30; y < 310; y++) for (var x = 40; x < 500; x++) { var c = pixels.GetPixel(x, y); if (c.R > .6f && c.G > .6f && c.B > .6f) light++; }
                Check(pixels.GetPixel(dialog.Position.X + dialog.Size.X + 15, dialog.Position.Y + 20).R < .01f, "Embedded decoration stays within its actual width.");
                Check(light > 250 && dialog.GetOKButton().HasFocus(), $"Dialog {backend} title/message/button pixels and focus ({light})."); File.WriteAllBytes(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"electron2d-dialog-{backend}.png"), pixels.SavePNGToBuffer());
                DialogClick((Vector2)dialog.Position + help.GetGlobalRect().GetCenter()); return;
            }
            if (frame == 2) { Check(custom == 1 && dialog.Visible, "Native custom action retains dialog."); OptionKey(SDL.Keycode.Escape); return; }
            if (frame == 3) { Check(canceled == 1 && !dialog.Visible, "Native Escape cancels dialog."); dialog.PopupCentered(); OptionKey(SDL.Keycode.Return); return; }
            Check(confirmed == 1 && !dialog.Visible, "Native Enter confirms focused OK."); root.Tree!.Quit();
        };
        };
        Engine.Run(root); Released(root); Check(frame == 4, "Native dialog action sequence completed."); VerifyDialogWarm(backend); var unsupported = new Window { KeepTitleVisible = true }; try { Engine.Run(unsupported); throw new InvalidOperationException("Native title measurement must reject."); } catch (NotSupportedException) { Released(unsupported); }
        Console.WriteLine($"Dialogs {backend}: native pointer custom action, Escape cancellation, Return confirmation and themed pixels passed.");
    }
    private static void DialogClick(Vector2 point)
    {
        var input = new SDL.Event { Type = (uint)SDL.EventType.MouseButtonDown }; input.Button.WindowID = SDL.GetWindowID(SDL.GetWindows(out var count)![0]); input.Button.X = point.X; input.Button.Y = point.Y; input.Button.Button = SDL.ButtonLeft; input.Button.Down = true; SDL.PushEvent(ref input); input.Type = (uint)SDL.EventType.MouseButtonUp; input.Button.Down = false; SDL.PushEvent(ref input);
    }
    private static void VerifyDialogWarm(string backend)
    {
        var root = new Window { Size = new(540, 320), GUIEmbedSubwindows = true }; var dialog = new ConfirmationDialog { DialogText = "Active dialog", DialogHideOnOK = false }; root.AddChild(dialog); var frame = 0; long before = 0, bytes = 0;
        root.Ready += _ => { dialog.PopupCentered(new(380, 180)); root.Tree!.ProcessFrameStarted += _ => { before = GC.GetAllocatedBytesForCurrentThread(); dialog.DialogText = (frame & 1) == 0 ? "Active dialog" : "Updated dialog"; dialog.GetLabel().Size = new(364 + (frame & 1), 125); }; RenderingServer.FramePostDraw += () => { if (frame >= 32) bytes += GC.GetAllocatedBytesForCurrentThread() - before; if (++frame == 96) root.Tree!.Quit(); }; };
        Engine.Run(root); Released(root); Check(frame == 96 && bytes == 0, $"Dialog {backend} message/layout/render allocated {bytes} managed bytes over 64 warmed frames."); Console.WriteLine($"Dialogs {backend}: 64 warmed message/layout/render frames, {bytes} managed bytes.");
    }
}
