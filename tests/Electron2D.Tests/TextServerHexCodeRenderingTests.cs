using System.Diagnostics;
using Electron2D;

internal static class TextServerHexCodeRenderingTests
{
    internal static void Run(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); Engine.MaxFPS = 120;
        var server = TextServerManager.GetPrimaryInterface();
        using var window = new Window { Size = new(540, 190) };
        var canvas = new Entity { Position = new(5, 7) }; window.AddChild(canvas);
        var clip = new Control { Position = new(420, 70), Size = new(10, 45), ClipContents = true }; window.AddChild(clip);
        var clipped = new Entity(); clip.AddChild(clipped);
        using var target = window.GetTexture();
        var frames = 0; var draws = 0; var changed = false; long before = 0, active = 0, idle = 0;
        canvas.Draw += item =>
        {
            draws++;
            server.DrawHexCodeBox(item, 15, new(20, 50), changed ? 0x34 : 0x12, Colors.White);
            server.DrawHexCodeBox(item, 30, new(160, 80), 0xABCD, Colors.Green);
            server.DrawHexCodeBox(item, changed ? int.MinValue : 45, new(300, 100), 0x10FFFF, Colors.Cyan);
            server.DrawHexCodeBox(item, int.MaxValue, new(480, 50), 0, Colors.Red);
        };
        clipped.Draw += item => server.DrawHexCodeBox(item, 30, new(0, 30), 0xFFFF, Colors.Yellow);
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black);
            window.Tree!.ProcessFrameStarted += _ =>
            {
                before = GC.GetAllocatedBytesForCurrentThread();
                if (frames == 25) changed = true;
                if (frames < 135) { canvas.QueueRedraw(); clipped.QueueRedraw(); }
                server.GetHexCodeBoxSize(15, 0x12); server.GetHexCodeBoxSize(30, 0xABCD); server.GetHexCodeBoxSize(45, 0x10FFFF);
            };
            RenderingServer.FramePostDraw += Capture;
        };
        void Capture()
        {
            var delta = GC.GetAllocatedBytesForCurrentThread() - before;
            if (frames is >= 71 and < 135) active += delta;
            if (frames is >= 135 and < 199) idle += delta;
            frames++;
            if (frames is 2 or 50)
            {
                using var image = target.GetImage() ?? throw new InvalidOperationException("Hexadecimal box readback.");
                Check(image.GetPixel(25, 45).R > .8f, "Transformed two-digit frame uses the text baseline.");
                var digit = image.GetPixel(27, 47); Check(changed ? digit.R > .8f : digit.R < .1f, "Live hexadecimal digit replacement changes the correct segment.");
                Check(image.GetPixel(165, 62).G > .4f && image.GetPixel(165, 62).R < .1f, "Four-digit frame keeps supplied green color and doubled scale.");
                Check(image.GetPixel(187, 70).G < .1f && image.GetPixel(188, 70).G < .1f, "Measured advance includes two unpainted trailing pixels.");
                Check(Count(image, new(300, 55, 55, 60), 0) > (changed ? 30 : 100), "Six-digit frame and segments render at both live scales.");
                Check(Count(image, new(420, 70, 10, 45), 1) > 20 && Count(image, new(430, 70, 25, 45), 1) == 0, "Control clipping cuts the actual placeholder primitives.");
                Check(Count(image, new(480, 20, 50, 55), 2) == 0, "Zero scalar records no red geometry.");
                if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } directory)
                { Directory.CreateDirectory(directory); image.SavePNG(System.IO.Path.Combine(directory, "text-hex-" + backend + "-" + frames + ".png")); }
            }
            if (frames < 199) return;
            Check(draws >= 135 && active == 0 && idle == 0, "64 active + 64 idle complete frames owner bytes=" + active + "/" + idle);
            RenderingServer.FramePostDraw -= Capture; window.Tree!.Quit();
        }
        try { Check(Engine.Run(window) == 0 && frames == 199, "Font-free hexadecimal drawing host completes."); }
        finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= Capture; }
        if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_TEXT_HEX_CHILD") == "1") Console.WriteLine("{\"hexCode\":true,\"freshProcess\":true,\"rendered\":true}");
        else Fresh(backend);
        Console.WriteLine("TextServer hexadecimal " + backend + ": font-free initial/live digits, transforms, colors, clipping and 64 active + 64 idle frames=0/0 owner bytes passed.");
    }
    private static void Fresh(string backend)
    {
        var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true };
        if (System.IO.Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(typeof(TextServerHexCodeRenderingTests).Assembly.Location);
        start.Environment["ELECTRON2D_TEST_TEXT_SERVER_HEX_RENDER"] = backend; start.Environment["ELECTRON2D_TEST_TEXT_HEX_CHILD"] = "1";
        using var child = Process.Start(start)!; var stdout = child.StandardOutput.ReadToEndAsync(); var stderr = child.StandardError.ReadToEndAsync();
        if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Fresh hexadecimal drawing host."); }
        var output = stdout.GetAwaiter().GetResult(); var error = stderr.GetAwaiter().GetResult();
        Check(child.ExitCode == 0 && output.Contains("\"hexCode\":true"), "Fresh font-free authoring/render: " + output + error); Console.WriteLine(output.Trim());
    }
    private static int Count(Image image, Rect2i area, int kind)
    {
        var count = 0;
        for (var y = area.Position.Y; y < area.End.Y; y++) for (var x = area.Position.X; x < area.End.X; x++)
            {
                var c = image.GetPixel(x, y);
                if (kind switch { 0 => c.G > .6f && c.B > .6f && c.R < .1f, 1 => c.R > .6f && c.G > .6f && c.B < .1f, _ => c.R > .6f && c.G < .1f && c.B < .1f }) count++;
            }
        return count;
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException("TextServer hexadecimal rendering: " + message); }
}
