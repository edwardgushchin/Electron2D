using Electron2D;
using SDL3;
using Image = Electron2D.Image;

internal static partial class RenderingRuntimeTests
{
    private static void OptionKey(SDL.Keycode code)
    { var input = new SDL.Event { Type = (uint)SDL.EventType.KeyDown }; input.Key.WindowID = SDL.GetWindowID(SDL.GetWindows(out var count)![0]); input.Key.Key = code; input.Key.Down = true; SDL.PushEvent(ref input); input.Type = (uint)SDL.EventType.KeyUp; input.Key.Down = false; SDL.PushEvent(ref input); }
    private static void VerifyOptionRendering(string backend)
    {
        using var pixelsIcon = Image.CreateEmpty(8, 8, false, Image.Format.Rgba8); pixelsIcon.Fill(Colors.Blue); using var icon = ImageTexture.CreateFromImage(pixelsIcon);
        var root = new Window { Size = new(350, 230), GUIEmbedSubwindows = true }; var option = new OptionButton { Name = "NativeChoice", Position = new(20, 20), Size = new(180, 40) }; option.AddItem("Small", 10); option.AddIconItem(icon, "Bigger choice", 20); root.AddChild(option); var frame = 0;
        root.Ready += _ =>
        {
            option.GrabFocus(); RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.FramePostDraw += () =>
        {
            using var pixels = RenderingServer.Service!.Readback(); frame++;
            if (frame == 1) { OptionKey(SDL.Keycode.Space); return; }
            if (frame == 2) { Check(option.GetPopup().Visible && option.GetPopup().GetFocusedItem() == 0, "Native action opens dropdown with selected item focused."); OptionKey(SDL.Keycode.Down); return; }
            if (frame == 3) { Check(option.GetPopup().Visible && option.GetPopup().GetFocusedItem() == 1, "Native navigation focuses second option."); File.WriteAllBytes(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"electron2d-option-popup-{backend}.png"), pixels.SavePNGToBuffer()); OptionKey(SDL.Keycode.Return); return; }
            Check(option.Selected == 1 && option.Text == "Bigger choice" && !option.GetPopup().Visible && !option.ButtonPressed, "Native activation closes popup and updates choice.");
            var blue = 0; var bright = 0; for (var y = 20; y < 70; y++) for (var x = 20; x < 250; x++) { var c = pixels.GetPixel(x, y); if (c.B > .9f && c.R < .1f && c.G < .1f) blue++; if (c.R > .7f && c.G > .7f && c.B > .7f) bright++; }
            Check(blue >= 40 && bright > 25, $"Option {backend} renders borrowed icon, shaped caption and arrow ({blue}/{bright})."); File.WriteAllBytes(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"electron2d-option-{backend}.png"), pixels.SavePNGToBuffer()); root.Tree!.Quit();
        };
        };
        Engine.Run(root); Released(root); Check(frame == 4, "Option native sequence completed."); VerifyOptionWarm(backend); Console.WriteLine($"OptionButton native keyboard dropdown, radio selection, text/icon/arrow rendered on {backend}.");
    }
    private static void VerifyOptionWarm(string backend)
    {
        var root = new Window { Size = new(260, 160) }; var option = new OptionButton { Name = "WarmChoice", Position = new(10, 10), Size = new(200, 40) }; option.AddItem("Alpha"); option.AddItem("Beta"); root.AddChild(option); var frame = 0; long before = 0, bytes = 0;
        root.Ready += _ => { root.Tree!.ProcessFrameStarted += _ => { before = GC.GetAllocatedBytesForCurrentThread(); option.Select(frame & 1); }; RenderingServer.FramePostDraw += () => { if (frame >= 32) bytes += GC.GetAllocatedBytesForCurrentThread() - before; if (++frame == 96) root.Tree!.Quit(); }; };
        Engine.Run(root); Released(root); Check(frame == 96 && bytes == 0, $"Option {backend} selection/measurement/caption/render allocated {bytes} managed bytes over 64 warm frames."); Console.WriteLine($"OptionButton {backend}: 64 warmed selection/measurement/caption/render frames, {bytes} managed bytes.");
    }
}
