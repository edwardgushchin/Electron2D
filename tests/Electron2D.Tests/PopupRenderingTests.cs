using Electron2D;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyPopupRendering(string backend)
    {
        using var blue = new StyleBoxFlat { BGColor = Colors.Blue, ShadowSize = 6 }; blue.SetContentMarginAll(8);
        using var green = new StyleBoxFlat { BGColor = Colors.Green };
        var root = new Window { Size = new(260, 170), GUIEmbedSubwindows = true };
        var popup = new PopupPanel { Name = "BluePopup", Size = new(120, 70) }; popup.AddThemeStyleBoxOverride("panel", blue);
        var child = new PopupPanel { Name = "GreenPopup", Size = new(50, 35) }; child.AddThemeStyleBoxOverride("panel", green); popup.AddChild(child); root.AddChild(popup);
        var frames = 0;
        root.Ready += _ =>
        {
            popup.Popup(new(new(50, 50), new(120, 70))); child.Popup(new(new(90, 70), new(50, 35)));
            RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                using var pixels = RenderingServer.Service!.Readback(); frames++;
                var bluePixel = pixels.GetPixel(54, 54); Check(bluePixel.B > .9f && bluePixel.R < .1f, "Embedded popup uses its actual themed offscreen texture.");
                var pixel = pixels.GetPixel(100, 80); Check(frames == 1 ? pixel.G > .4f && pixel.B < .1f : pixel.B > .9f && pixel.G < .1f, "Nested popup composes above parent and hiding restores parent pixels.");
                var outside = pixels.GetPixel(10, 10); Check(outside.R < .02f && outside.G < .02f && outside.B < .02f, "Transparent popup preserves pixels outside its content.");
                if (frames == 1) { File.WriteAllBytes(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"electron2d-popup-{backend}.png"), pixels.SavePNGToBuffer()); child.Hide(); return; }
                root.Tree!.Quit();
            };
        };
        Engine.Run(root); Released(root); VerifyPopupWarm(backend); Check(frames == 2, "Popup native composition completed."); Console.WriteLine($"Popup themed transparent textures, nested ordering and hide rendered on {backend}.");
    }
    private static void VerifyPopupWarm(string backend)
    {
        var root = new Window { Size = new(200, 120), GUIEmbedSubwindows = true }; var popup = new PopupPanel { Name = "WarmPopup", Size = new(80, 40) }; root.AddChild(popup); var frame = 0; long before = 0, bytes = 0;
        root.Ready += _ => { popup.PopupCentered(); root.Tree!.ProcessFrameStarted += _ => { before = GC.GetAllocatedBytesForCurrentThread(); popup.Position = new(40 + (frame & 1), 40); }; RenderingServer.FramePostDraw += () => { if (frame >= 32) bytes += GC.GetAllocatedBytesForCurrentThread() - before; if (++frame == 96) root.Tree!.Quit(); }; };
        Engine.Run(root); Released(root); Check(frame == 96 && bytes == 0, $"Popup {backend} warmed active placement/render allocated {bytes} managed bytes over 64 frames."); Console.WriteLine($"Popup {backend}: 64 warmed placement/render frames, {bytes} managed bytes.");
    }

}
