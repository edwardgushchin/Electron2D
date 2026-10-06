using Electron2D;
using SDL3;

internal static partial class RenderingRuntimeTests
{
    private static void MenuPointer(Vector2 point)
    { var input = new SDL.Event { Type = (uint)SDL.EventType.MouseMotion }; input.Motion.WindowID = SDL.GetWindowID(SDL.GetWindows(out var count)![0]); input.Motion.X = point.X; input.Motion.Y = point.Y; input.Motion.XRel = 1; input.Motion.YRel = 1; SDL.PushEvent(ref input); }
    private static void VerifyMenuButtonRendering(string backend)
    {
        var root = new Window { Size = new(460, 260), GUIEmbedSubwindows = true }; var bar = new HBoxContainer { Position = new(20, 20), Size = new(220, 40) }; var file = new MenuButton("File") { Name = "File", SwitchOnHover = true }; var edit = new MenuButton("Edit") { Name = "Edit", SwitchOnHover = true }; file.GetPopup().AddItem("Open", 42); edit.GetPopup().AddItem("Copy command", 84); bar.AddChild(file); bar.AddChild(edit); root.AddChild(bar); var selected = 0; edit.GetPopup().IDPressed += id => selected = id; var frame = 0;
        root.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.FramePostDraw += () =>
        {
            frame++; if (frame <= 3) return; var step = frame - 3;
            if (step == 1) { DialogClick(file.GetGlobalRect().GetCenter()); return; }
            if (step == 2) { Check(file.GetPopup().Visible && file.ButtonPressed, "Native pointer opens command menu."); MenuPointer(edit.GetGlobalRect().GetCenter()); return; }
            if (step == 3) { Check(edit.GetPopup().Visible && !file.GetPopup().Visible && edit.GetPopup().GetFocusedItem() == -1, "Native hover: file=" + file.GetPopup().Visible + " edit=" + edit.GetPopup().Visible + " focus=" + edit.GetPopup().GetFocusedItem() + " hovered=" + root.GUIGetHoveredControl()?.Name); OptionKey(SDL.Keycode.Down); return; }
            if (step == 4)
            {
                using var pixels = RenderingServer.Service!.Readback(); var bright = 0; for (var y = 20; y < 140; y++) for (var x = 15; x < 310; x++) { var c = pixels.GetPixel(x, y); if (c.R > .65f && c.G > .65f && c.B > .65f) bright++; }
                Check(bright > 160 && edit.GetPopup().GetFocusedItem() == 0, $"Menu button {backend} caption/menu pixels and keyboard focus ({bright})."); File.WriteAllBytes(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"electron2d-menubutton-{backend}.png"), pixels.SavePNGToBuffer()); OptionKey(SDL.Keycode.Return); return;
            }
            Check(selected == 84 && !edit.GetPopup().Visible && !edit.ButtonPressed, "Native command activation resets menu owner."); root.Tree!.Quit();
        };
        };
        Engine.Run(root); Released(root); Check(frame == 8, "Native menu button sequence completed."); VerifyMenuButtonWarm(backend); Console.WriteLine($"MenuButton {backend}: pointer opening, related hover switching, keyboard navigation/activation and text/menu pixels passed.");
    }
    private static void VerifyMenuButtonWarm(string backend)
    {
        var root = new Window { Size = new(380, 240), GUIEmbedSubwindows = true }; var menu = new MenuButton("File") { Position = new(20, 20), Size = new(110, 40), SwitchOnHover = true }; menu.GetPopup().AddItem("Open"); menu.GetPopup().AddItem("Save"); root.AddChild(menu); var frame = 0; long before = 0, bytes = 0;
        root.Ready += _ => { menu.ShowPopup(); root.Tree!.ProcessFrameStarted += _ => { before = GC.GetAllocatedBytesForCurrentThread(); menu.Text = (frame & 1) == 0 ? "File" : "Edit"; menu.GetPopup().SetFocusedItem(frame & 1); }; RenderingServer.FramePostDraw += () => { if (frame >= 32) bytes += GC.GetAllocatedBytesForCurrentThread() - before; if (++frame == 96) root.Tree!.Quit(); }; };
        Engine.Run(root); Released(root); Check(frame == 96 && bytes == 0, $"MenuButton {backend} text/focus/render allocated {bytes} managed bytes over 64 warmed frames."); Console.WriteLine($"MenuButton {backend}: 64 warmed caption/focus/render frames, {bytes} managed bytes.");
    }
}
