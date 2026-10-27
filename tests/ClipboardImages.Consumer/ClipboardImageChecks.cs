using Electron2D;
using System.Buffers.Binary;
using System.Text;

internal static class ClipboardImageChecks
{
    private sealed class Patch(Texture texture) : Entity
    {
        protected override void OnDraw() => DrawTextureRect(texture, new(0, 0, 64, 64), false);
    }
    private sealed class OffscreenTick(Action tick) : Node
    {
        protected override void OnProcess(double delta) => tick();
    }
    internal static void Run(string[] args)
    {
        if (args[0] == "author") { Author(args[1]); return; }
        Reject<InvalidOperationException>(() => DisplayServer.ClipboardGetImage());
        Reject<InvalidOperationException>(() => DisplayServer.ClipboardHasImage());
        var scenario = args[1];
        ProjectSettings.Set(ProjectSettings.RenderingMethod, args[0]);
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var window = new Window { Size = new(64, 64), Borderless = true };
        ImageTexture? texture = null;
        var frames = 0; var pastedAt = -1;
        window.AddChild(new OffscreenTick(() => { if (args[0] == "gpu" && pastedAt >= 0) RenderingServer.ForceDraw(swapBuffers: false); }));
        window.Ready += _ =>
        {
            DisplayServer.WindowSetVSyncMode(DisplayServer.VSyncMode.Disabled);
            Console.WriteLine("READY"); Console.Out.Flush();
            RenderingServer.FramePostDraw += Frame;
        };
        try { Engine.Run(window); }
        finally { texture?.Dispose(); }
        Reject<InvalidOperationException>(() => DisplayServer.ClipboardGetImage());
        Reject<InvalidOperationException>(() => DisplayServer.ClipboardHasImage());
        Console.WriteLine("PASS " + scenario);

        void Frame()
        {
            if (++frames < 6) return;
            Check(frames < 900, "Clipboard offer/readback did not become ready.");
            if (pastedAt < 0)
            {
                if (DisplayServer.ClipboardGet() != "e2d-clipboard-" + scenario) return;
                if (scenario == "none")
                {
                    Check(!DisplayServer.ClipboardHasImage() && DisplayServer.ClipboardGetImage() is null, "File-manager paths and text are not image data.");
                    window.Tree!.Quit(); return;
                }
                if (scenario is "invalid" or "empty" or "priority" or "oversize")
                {
                    Check(!DisplayServer.ClipboardHasImage(), "Malformed preferred image data is not a valid image.");
                    if (scenario == "empty") Reject<IOException>(() => DisplayServer.ClipboardGetImage());
                    else Reject<InvalidDataException>(() => DisplayServer.ClipboardGetImage());
                    window.Tree!.Quit(); return;
                }
                if (!DisplayServer.ClipboardHasImage()) return;
                using var image = DisplayServer.ClipboardGetImage()!;
                Check(image.Size == new Vector2i(2, 1), "Image MIME data decodes at its source dimensions.");
                var first = image.GetPixel(0, 0); var second = image.GetPixel(1, 0);
                Check(first.R > .98f && first.G < .02f && first.B < .02f, "The clipboard contains the expected red pixel.");
                Check(scenario == "jpeg" ? second.R > .98f : second.G > .98f && second.R < .02f, "The second source pixel survives format selection.");
                if (scenario is "png" or "webp" or "svg" or "tga" or "targa") Check(MathF.Abs(first.A - .5f) < .01f, "Clipboard alpha is preserved.");
                if (scenario == "exr") Check(first.R > 1.99f, "Clipboard EXR retains its HDR value above one.");
                var expected = first;
                image.Fill(Colors.Blue);
                using var independent = DisplayServer.ClipboardGetImage()!;
                Check(independent.GetPixel(0, 0) == expected, "Returned pixels do not alias the clipboard or another result.");
                var count = Performance.GetMonitor(Performance.Monitor.ObjectCount);
                for (var i = 0; i < 8; i++) Check(DisplayServer.ClipboardHasImage(), "Repeated predicates decode successfully.");
                Check(Performance.GetMonitor(Performance.Monitor.ObjectCount) == count, "Predicate images retire deterministically.");
                Reject<InvalidOperationException>(() => Task.Run(DisplayServer.ClipboardGetImage).GetAwaiter().GetResult());
                Reject<InvalidOperationException>(() => Task.Run(DisplayServer.ClipboardHasImage).GetAwaiter().GetResult());
                if (args[0] == "compatibility" && scenario == "exr") independent.Convert(Image.Format.Rgba8);
                texture = ImageTexture.CreateFromImage(independent); window.AddChild(new Patch(texture));
                if (args[0] == "gpu") RenderingServer.RenderLoopEnabled = false;
                pastedAt = frames;
                Console.WriteLine("DECODED"); Console.Out.Flush();
                return;
            }
            if (frames - pastedAt < 10) return;
            // The producer revokes its MIME offer after DECODED; uploaded pixels must remain independent.
            if (DisplayServer.ClipboardHasImage()) return;
            Console.WriteLine("CAPTURE"); Console.Out.Flush();
            using var rendered = window.GetTexture().GetImage()!;
            var pixel = rendered.GetPixel(48, 24);
            Check(scenario == "jpeg" ? pixel.R > .95f && pixel.G < .05f : pixel.G > .95f && pixel.R < .05f, "Pasted image survives offer retirement and reaches actual native canvas pixels.");
            window.Tree!.Quit();
        }
    }

    private static void Author(string root)
    {
        Directory.CreateDirectory(root);
        using var image = Image.CreateFromData(2, 1, false, Image.Format.Rgba8, [255, 0, 0, 128, 0, 255, 0, 255]);
        File.WriteAllBytes(System.IO.Path.Combine(root, "png"), image.SavePNGToBuffer());
        File.WriteAllBytes(System.IO.Path.Combine(root, "webp"), image.SaveWebPToBuffer());
        using var hdr = new Image(); hdr.CopyFrom(image); hdr.Convert(Image.Format.Rgbaf); hdr.SetPixel(0, 0, new Color(2, 0, 0, .5f));
        File.WriteAllBytes(System.IO.Path.Combine(root, "exr"), hdr.SaveEXRToBuffer());
        using var jpeg = Image.CreateFromData(2, 1, false, Image.Format.Rgba8, [255, 0, 0, 255, 255, 0, 0, 255]);
        File.WriteAllBytes(System.IO.Path.Combine(root, "jpeg"), jpeg.SaveJPGToBuffer(1));
        File.WriteAllText(System.IO.Path.Combine(root, "svg"), "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"2\" height=\"1\"><path d=\"M0 0h1v1H0z\" fill=\"red\" fill-opacity=\".5\"/><path d=\"M1 0h1v1H1z\" fill=\"#00ff00\"/></svg>", new UTF8Encoding(false));
        var bmp = new byte[62]; bmp[0] = 66; bmp[1] = 77;
        foreach (var (offset, value) in new[] { (2, 62), (10, 54), (14, 40), (18, 2), (22, 1) }) BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(offset), value);
        bmp[26] = 1; bmp[28] = 24; bmp[56] = 255; bmp[58] = 255;
        File.WriteAllBytes(System.IO.Path.Combine(root, "bmp"), bmp);
        byte[] tga = [0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 0, 1, 0, 32, 40, 0, 0, 255, 128, 0, 255, 0, 255];
        File.WriteAllBytes(System.IO.Path.Combine(root, "tga"), tga);
        var ktx = new byte[76]; new byte[] { 0xAB, 0x4B, 0x54, 0x58, 0x20, 0x31, 0x31, 0xBB, 0x0D, 0x0A, 0x1A, 0x0A }.CopyTo(ktx, 0);
        uint[] header = [0x04030201, 0x1401, 1, 0x1908, 0x8058, 0x1908, 2, 1, 0, 0, 1, 1, 0, 8];
        for (var i = 0; i < header.Length; i++) BinaryPrimitives.WriteUInt32LittleEndian(ktx.AsSpan(12 + i * 4), header[i]);
        image.GetData().CopyTo(ktx, 68); File.WriteAllBytes(System.IO.Path.Combine(root, "ktx"), ktx);
        Console.WriteLine("Authored eight image encodings; TGA serves both MIME aliases.");
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
