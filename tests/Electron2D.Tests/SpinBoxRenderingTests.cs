using Electron2D;
using SDL3;

internal static partial class RenderingRuntimeTests
{
    private static void SpinButton(Vector2 point, bool pressed)
    { var input = new SDL.Event { Type = (uint)(pressed ? SDL.EventType.MouseButtonDown : SDL.EventType.MouseButtonUp) }; input.Button.WindowID = SDL.GetWindowID(SDL.GetWindows(out _)![0]); input.Button.Which = 987; input.Button.X = point.X; input.Button.Y = point.Y; input.Button.Button = SDL.ButtonLeft; input.Button.Down = pressed; SDL.PushEvent(ref input); }
    private static void SpinMotion(Vector2 point, float relative)
    { var input = new SDL.Event { Type = (uint)SDL.EventType.MouseMotion }; input.Motion.WindowID = SDL.GetWindowID(SDL.GetWindows(out _)![0]); input.Motion.Which = 987; input.Motion.State = SDL.MouseButtonFlags.Left; input.Motion.X = point.X; input.Motion.Y = point.Y; input.Motion.YRel = relative; SDL.PushEvent(ref input); }
    private static void VerifySpinBoxRendering(string backend)
    {
        var root = new Window { Size = new(460, 250) }; var spin = new SpinBox { Position = new(30, 55), Size = new(280, 44), Step = .01, Value = 12.5, Prefix = "$", Suffix = "kg" }; root.AddChild(spin); var frame = 0; var originalMode = MouseMode.Visible;
        root.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.FramePostDraw += () =>
        {
            if (++frame <= 3) return;
            if (frame == 4)
            {
                using var pixels = RenderingServer.Service!.Readback(); var light = 0; for (var y = 55; y < 100; y++) for (var x = 30; x < 315; x++) if (pixels.GetPixel(x, y).R > .6f) light++;
                Check(light > 60, "SpinBox numeric glyphs and arrows draw real pixels."); File.WriteAllBytes(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"electron2d-spinbox-{backend}.png"), pixels.SavePNGToBuffer()); DialogClick(spin.Position + new Vector2(spin.Size.X - 8, 6)); return;
            }
            if (frame == 5) { Check(Math.Abs(spin.Value - 12.51) < 1e-9 && spin.GetLineEdit().IsEditing(), "Native arrow press steps and focuses the numeric field."); spin.GetLineEdit().Text = "pow(2, 4)"; OptionKey(SDL.Keycode.Return); return; }
            if (frame == 6) { Check(spin.Value == 16 && !spin.GetLineEdit().IsEditing(), "Native text submission evaluates the expression."); originalMode = Input.MouseMode; SpinButton(spin.Position + new Vector2(spin.Size.X - 8, 6), true); return; }
            if (frame == 7) { SpinMotion(spin.Position + new Vector2(spin.Size.X - 8, 12), 6); return; }
            if (frame == 8) { Check(Input.MouseMode == MouseMode.Captured, "Native drag acquires pointer capture."); SpinMotion(spin.Position + new Vector2(spin.Size.X - 8, 12), -40); return; }
            if (frame == 9) { Check(spin.Value > 16.01, "Captured native relative drag executes accelerated stepping."); SpinButton(spin.Position + new Vector2(spin.Size.X - 8, 6), false); return; }
            if (frame == 10) { Check(Input.MouseMode == originalMode, "Drag release restores the caller's pointer mode."); root.Tree!.Quit(); }
        };
        };
        Engine.Run(root); Released(root); Check(frame == 10, "SpinBox native sequence completed.");
        var warmRoot = new Window { Size = new(460, 250) }; var warm = new SpinBox { Position = new(30, 55), Size = new(280, 44), Step = .01, Value = 12.5 }; warmRoot.AddChild(warm); frame = 0; long before = 0, bytes = 0;
        warmRoot.Ready += _ => { warmRoot.Tree!.ProcessFrameStarted += _ => { before = GC.GetAllocatedBytesForCurrentThread(); warm.Value = (frame & 1) == 0 ? 12.5 : 25.75; }; RenderingServer.FramePostDraw += () => { if (frame >= 64) bytes += GC.GetAllocatedBytesForCurrentThread() - before; if (++frame == 128) warmRoot.Tree!.Quit(); }; };
        Engine.Run(warmRoot); Released(warmRoot); Check(bytes == 0, $"SpinBox {backend}: 64 prepared value/field/render frames allocated {bytes} managed bytes."); Console.WriteLine($"SpinBox {backend}: native arrows, formula submission, numeric pixels and 64 prepared field/render frames with {bytes} managed bytes passed.");
    }
}
