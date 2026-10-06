using Electron2D;
using SDL3;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyColorPickerRendering(string backend)
    {
        var root = new Window { Size = new(1000, 860), GUIEmbedSubwindows = true }; var picker = new ColorPicker { Position = new(20, 20), Size = new(600, 750), Color = Colors.Red }; root.AddChild(picker);
        var target = new ColorTarget { Position = new(760, 80), Size = new(120, 120) }; root.AddChild(target);
        var button = new ColorPickerButton { Position = new(730, 270), Size = new(150, 40), Color = Colors.Blue }; root.AddChild(button); var frame = 0;
        root.Ready += _ => RenderingServer.FramePostDraw += () =>
        {
            frame++; if (frame < 5) return;
            if (frame == 5) { var surface = (Control)ColorPickerTests.Find(picker, "Surface"); var point = surface.GetGlobalTransformWithCanvas() * (surface.Size * new Vector2(.5f, .5f)); DialogClick(point); return; }
            if (frame == 6) { Check(picker.Color.R > .45f && picker.Color.R < .55f && picker.Color.G > .20f && picker.Color.G < .30f, "Native HSV square click changes saturation/value."); picker.Color = Colors.Red; return; }
            if (frame is >= 7 and <= 12)
            {
                var shape = new[] { ColorPicker.PickerShapeType.HSVRectangle, ColorPicker.PickerShapeType.HSVWheel, ColorPicker.PickerShapeType.VHSCircle, ColorPicker.PickerShapeType.OKHSLCircle, ColorPicker.PickerShapeType.OKHSRectangle, ColorPicker.PickerShapeType.OKHLRectangle }[Math.Min(5, frame - 6)];
                using var image = RenderingServer.Service!.Readback(); var surface = (Control)ColorPickerTests.Find(picker, "Surface"); var origin = surface.GetGlobalTransformWithCanvas().Origin;
                var count = 0; for (var y = (int)origin.Y + 5; y < origin.Y + surface.Size.Y - 5; y += 10) for (var x = (int)origin.X + 5; x < origin.X + surface.Size.X - 5; x += 10) { var color = image.GetPixel(x, y); if (Math.Max(color.R, Math.Max(color.G, color.B)) > .3) count++; }
                Check(count > 15, "Color surface draws real native pixels: " + picker.PickerShape); File.WriteAllBytes(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"electron2d-color-{backend}-{picker.PickerShape}.png"), image.SavePNGToBuffer());
                picker.PickerShape = shape; return;
            }
            if (frame == 13) { DialogClick(button.GetGlobalTransformWithCanvas() * (button.Size / 2)); return; }
            if (frame == 14) { Check(button.GetPopup().Visible && button.ButtonPressed, "Native button opens its actual editor popup."); button.GetPopup().Hide(); return; }
            if (frame == 15) { using var captured = RenderingServer.Service!.Readback(); Check(captured.GetPixel(800, 120).B > .95f, "Sampler target is visible in the completed frame after popup hiding."); var pick = (Control)ColorPickerTests.Find(picker, "Pick"); DialogClick(pick.GetGlobalTransformWithCanvas() * (pick.Size / 2)); return; }
            if (frame == 16) { var sampler = (Popup)ColorPickerTests.Find(picker, "_color_sampler"); Check(sampler.Visible, "Native sampler captures the completed application viewport."); ColorMotion(new(800, 120)); return; }
            if (frame == 17) { DialogClick(new(800, 120)); return; }
            if (frame == 18) { Check(picker.Color.B > .95f && picker.Color.R < .05f, "Native sampler selects actual blue target pixels."); root.Tree!.Quit(); }
        };
        Engine.Run(root); Released(root); Check(frame == 18, "Native color editor sequence completed.");
        var warmRoot = new Window { Size = new(800, 800) }; var warm = new ColorPicker { Position = new(20, 20), Size = new(600, 750) }; warmRoot.AddChild(warm); frame = 0; long before = 0, bytes = 0;
        warmRoot.Ready += _ => { warmRoot.Tree!.ProcessFrameStarted += _ => { before = GC.GetAllocatedBytesForCurrentThread(); warm.Color = (frame & 1) == 0 ? new Color(.2f, .4f, .6f) : new Color(.8f, .6f, .4f); }; RenderingServer.FramePostDraw += () => { if (frame >= 64) bytes += GC.GetAllocatedBytesForCurrentThread() - before; if (++frame == 128) warmRoot.Tree!.Quit(); }; };
        Engine.Run(warmRoot); Released(warmRoot); Check(bytes == 0, $"ColorPicker {backend}: 64 prepared channel/shape/render frames allocated {bytes} managed bytes."); Console.WriteLine($"ColorPicker {backend}: native surface, six shape pixel readbacks, owned popup, application sampler and 64 prepared frames with {bytes} managed bytes passed.");
    }
    private static void ColorMotion(Vector2 point) { var input = new SDL.Event { Type = (uint)SDL.EventType.MouseMotion }; input.Motion.WindowID = SDL.GetWindowID(SDL.GetWindows(out _)![0]); input.Motion.Which = 987; input.Motion.X = point.X; input.Motion.Y = point.Y; SDL.PushEvent(ref input); }
    private sealed class ColorTarget : Control { protected override void OnDraw() => DrawRect(new(Vector2.Zero, Size), Colors.Blue); }
}
