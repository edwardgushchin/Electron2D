using System.Diagnostics;
using System.Text;
using Electron2D;

internal static partial class DesktopScreenCaptureTests
{
    private static void WaylandScene(string[] args)
    {
        var fixture = Environment.GetEnvironmentVariable("ELECTRON2D_TEST_WAYLAND_PRIVATE_DISPLAY");
        Check(OperatingSystem.IsLinux() && !string.IsNullOrEmpty(fixture) && fixture == Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") &&
              Environment.GetEnvironmentVariable("SDL_VIDEODRIVER") == "wayland" && args.Length == 1, "Use a private Wayland test display and provide grim.");

        ProjectSettings.Set(ProjectSettings.RenderingMethod, "compatibility");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        Engine.MaxFPS = 30;
        var window = new Window { Size = new(480, 320), Title = "Electron2D Wayland capture", Borderless = true };
        window.AddChild(new ColorRect { Size = new(4096, 4096), Color = Colors.Blue });
        window.AddChild(new ColorRect { Position = new(68, 66), Size = new(104, 60), Color = Colors.Red });
        window.AddChild(new ColorRect { Position = new(208, 66), Size = new(104, 60), Color = Colors.Green });
        var deadline = Stopwatch.StartNew();
        var frames = 0;
        Image? retained = null;
        byte[]? retainedData = null;
        window.Ready += _ => RenderingServer.FramePostDraw += Drawn;

        void Drawn()
        {
            Check(deadline.Elapsed < TimeSpan.FromSeconds(20), "The Wayland compositor did not present the test markers.");
            if (++frames % 10 != 0)
                return;

            Check(DisplayServer.HasFeature(DisplayServer.Feature.ScreenCapture), "Wayland output capture protocols are present.");
            Check(DisplayServer.GetScreenCount() == 2, "The fixture exposes two Wayland outputs.");
            var first = ReadGrim(args[0], "HEADLESS-1");
            var second = ReadGrim(args[0], "HEADLESS-2");
            if (!second.TryFindColor(Colors.Red, out var red) || !second.TryFindColor(Colors.Green, out var green))
                return;
            Check(first.Bounds.Size != second.Bounds.Size && first.Bounds.Position.X < second.Bounds.Position.X, "The private outputs have distinct geometry.");
            var outputs = first.Index == 0 ? new[] { first, second } : new[] { second, first };

            foreach (var output in outputs)
            {
                using var screen = DisplayServer.ScreenGetImage(output.Index);
                CompareImage(screen, output.Bounds, [output]);
            }
            using (var screen = DisplayServer.ScreenGetImage(DisplayServer.ScreenOfMainWindow))
                CompareImage(screen, second.Bounds, [second]);

            Check(DisplayServer.ScreenGetPixel(green) == WaylandExpected(green, outputs), "Desktop pixel matches the green marker or the first overlapping output.");
            var markerCrop = new Rect2i(red, new(17, 9));
            using (var image = DisplayServer.ScreenGetImageRect(markerCrop))
                CompareImage(image, markerCrop, outputs);

            var seam = new Rect2i(new Vector2i(first.Bounds.End.X - 8, 90), new(17, 13));
            retained = DisplayServer.ScreenGetImageRect(seam);
            CompareImage(retained, seam, outputs);
            retainedData = retained.GetData();
            for (var i = 0; i < 8; i++)
            {
                using var again = DisplayServer.ScreenGetImageRect(seam);
                CompareImage(again, seam, outputs);
            }

            Check(!File.ReadLines("/proc/self/maps").Any(static line => line.Contains("memfd:electron2d-screen", StringComparison.Ordinal)) &&
                !Directory.EnumerateFiles("/proc/self/fd").Any(static fd => new FileInfo(fd).LinkTarget?.Contains("memfd:electron2d-screen", StringComparison.Ordinal) == true),
                "Repeated capture releases its shared-memory mappings and descriptors.");

            // The shorter screen leaves part of the desktop bounds uncovered.
            var shorter = first.Bounds.End.Y < second.Bounds.End.Y ? first : second;
            var gap = new Vector2i(shorter == first ? shorter.Bounds.Position.X : shorter.Bounds.End.X - 1, shorter.Bounds.End.Y);
            if (!first.Bounds.HasPoint(gap) && !second.Bounds.HasPoint(gap))
            {
                Check(DisplayServer.ScreenGetPixel(gap) == Colors.Black, "Desktop gaps read as opaque black.");
                using var black = DisplayServer.ScreenGetImageRect(new(gap, Vector2i.One));
                CompareImage(black, new(gap, Vector2i.One), outputs);
            }

            Reject<ArgumentOutOfRangeException>(() => DisplayServer.ScreenGetImage(int.MaxValue));
            Reject<ArgumentOutOfRangeException>(() => DisplayServer.ScreenGetImageRect(default));
            Reject<ArgumentOutOfRangeException>(() => DisplayServer.ScreenGetImageRect(new(int.MaxValue, 0, 8, 8)));
            Reject<ArgumentOutOfRangeException>(() => DisplayServer.ScreenGetPixel(new(int.MinValue, int.MinValue)));
            Reject<InvalidOperationException>(() => Task.Run(() => DisplayServer.ScreenGetPixel(red)).GetAwaiter().GetResult());

            Console.WriteLine($"Independent grim frames match both Electron2D outputs ({first.Bounds.Size} and {second.Bounds.Size}) at positions {first.Bounds.Position} and {second.Bounds.Position}.");
            RenderingServer.FramePostDraw -= Drawn;
            window.Tree!.Quit();
        }

        try
        {
            Check(Engine.Run(window) == 0 && retained is not null && retainedData is not null, "The Wayland capture scene retired.");
            Check(retained!.GetData().AsSpan().SequenceEqual(retainedData), "Captured pixels survive display shutdown.");
            Reject<InvalidOperationException>(() => DisplayServer.ScreenGetPixel(Vector2i.Zero));
            Console.WriteLine("Desktop screen, rectangle and pixel capture passed (Wayland).");
        }
        finally { retained?.Dispose(); }
    }

    private static WaylandReference ReadGrim(string executable, string output)
    {
        var wantedFirst = output == "HEADLESS-1";
        var firstIndex = DisplayServer.ScreenGetPosition(0).X < DisplayServer.ScreenGetPosition(1).X ? 0 : 1;
        var index = wantedFirst ? firstIndex : 1 - firstIndex;
        var bounds = new Rect2i(DisplayServer.ScreenGetPosition(index), DisplayServer.ScreenGetSize(index));
        var start = new ProcessStartInfo(executable) { UseShellExecute = false, RedirectStandardOutput = true };
        foreach (var value in new[] { "-o", output, "-t", "ppm", "-" })
            start.ArgumentList.Add(value);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not launch grim.");
        using var stream = new MemoryStream();
        try
        {
            var copy = process.StandardOutput.BaseStream.CopyToAsync(stream);
            Check(process.WaitForExit(4000) && process.ExitCode == 0, "grim captured the entire Wayland output.");
            copy.GetAwaiter().GetResult();
        }
        finally { if (!process.HasExited) process.Kill(); }

        var data = stream.GetBuffer().AsSpan(0, (int)stream.Length);
        var header = Encoding.ASCII.GetBytes($"P6\n{bounds.Size.X} {bounds.Size.Y}\n255\n");
        Check(data.StartsWith(header) && data.Length == header.Length + bounds.Size.X * bounds.Size.Y * 3,
            $"grim and Electron2D agree on the physical size of {output}. grim: {Encoding.ASCII.GetString(data[..Math.Min(header.Length, data.Length)]).Trim()} Electron2D: {bounds.Size}.");
        return new(index, bounds, data[header.Length..].ToArray());
    }

    private static void CompareImage(Image image, Rect2i crop, WaylandReference[] outputs)
    {
        Check(image.Size == crop.Size && image.PixelFormat == Image.Format.Rgba8 && !image.HasMIPMaps, "The image preserves the requested dimensions and format.");
        var data = image.GetData();
        for (var y = 0; y < crop.Size.Y; y++)
        {
            for (var x = 0; x < crop.Size.X; x++)
            {
                var i = (y * crop.Size.X + x) * 4;
                var pixel = new Color(data[i] / 255f, data[i + 1] / 255f, data[i + 2] / 255f, data[i + 3] / 255f);
                var at = crop.Position + new Vector2i(x, y);
                var expected = WaylandExpected(at, outputs);
                if (pixel != expected)
                    throw new InvalidOperationException($"Unexpected desktop color at {at}: {pixel}, expected {expected} from grim.");
            }
        }
    }

    private static Color WaylandExpected(Vector2i position, WaylandReference[] outputs)
    {
        foreach (var output in outputs)
            if (output.Bounds.HasPoint(position))
                return output.Pixel(position);
        return Colors.Black;
    }

    private sealed record WaylandReference(int Index, Rect2i Bounds, byte[] Pixels)
    {
        internal Color Pixel(Vector2i position)
        {
            var local = position - Bounds.Position;
            var index = (local.Y * Bounds.Size.X + local.X) * 3;
            return new Color(Pixels[index] / 255f, Pixels[index + 1] / 255f, Pixels[index + 2] / 255f);
        }

        internal bool TryFindColor(Color color, out Vector2i point)
        {
            for (var y = 0; y < Bounds.Size.Y; y++)
            {
                for (var x = 0; x < Bounds.Size.X; x++)
                {
                    point = Bounds.Position + new Vector2i(x, y);
                    if (Pixel(point) == color) return true;
                }
            }
            point = default;
            return false;
        }
    }
}
