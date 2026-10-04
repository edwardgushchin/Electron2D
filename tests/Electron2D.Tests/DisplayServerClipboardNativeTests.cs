using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Electron2D;
using SDL3;

internal static class DisplayServerClipboardNativeTests
{
    private const string ChildFlag = "ELECTRON2D_TEST_DISPLAY_CLIPBOARD_CHILD";

    public static void Run(DisplayServer display)
    {
        if (DisplayServer.GetName() != "Wayland")
            return;

        var clipboardBefore = DisplayServer.ClipboardGet();
        var primaryBefore = DisplayServer.ClipboardGetPrimary();
        var start = new ProcessStartInfo(Environment.ProcessPath!)
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        start.ArgumentList.Add(typeof(DisplayServerClipboardNativeTests).Assembly.Location);
        start.Environment[ChildFlag] = "1";
        start.Environment["SDL_VIDEODRIVER"] = "wayland";

        using var child = Process.Start(start) ??
            throw new InvalidOperationException("Could not start the clipboard reader process.");
        var stdout = child.StandardOutput.ReadToEndAsync();
        var stderr = child.StandardError.ReadToEndAsync();
        child.StandardInput.WriteLine(Digest(clipboardBefore));
        child.StandardInput.WriteLine(Digest(primaryBefore));
        child.StandardInput.Close();
        var deadline = Stopwatch.StartNew();
        try
        {
            while (!child.HasExited && deadline.Elapsed < TimeSpan.FromSeconds(10))
            {
                DisplayServer.ProcessEvents();
                Thread.Sleep(5);
            }

            if (!child.HasExited)
                throw new TimeoutException("The separate Wayland clipboard reader did not finish.");
            if (child.ExitCode != 0)
                throw new InvalidOperationException("The separate Wayland clipboard reader failed.");
            _ = stderr.GetAwaiter().GetResult();

            var fields = stdout.GetAwaiter().GetResult().Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length != 3 || fields[0].Trim() is not ("FOCUS:0" or "FOCUS:1") ||
                !TryParseResult(fields[1], "CLIPBOARD", out var clipboardMatch, out var clipboardLength) ||
                !TryParseResult(fields[2], "PRIMARY", out var primaryMatch, out var primaryLength))
                throw new InvalidOperationException("The separate Wayland clipboard reader returned an invalid result.");

            if (fields[0].Trim() == "FOCUS:0")
                throw new InvalidOperationException("The Wayland clipboard reader never received keyboard focus.");

            var clipboardAfter = DisplayServer.ClipboardGet();
            var primaryAfter = DisplayServer.ClipboardGetPrimary();
            if (!clipboardMatch || !primaryMatch)
                throw new InvalidOperationException($"A separate Wayland process observed different clipboard text: " +
                    $"clipboard match={clipboardMatch}, lengths={clipboardBefore.Length}/{clipboardLength}/{clipboardAfter.Length}; " +
                    $"primary match={primaryMatch}, lengths={primaryBefore.Length}/{primaryLength}/{primaryAfter.Length}.");

            Console.WriteLine($"Wayland clipboard read-only cross-process match: clipboard nonempty={clipboardBefore.Length > 0}, primary nonempty={primaryBefore.Length > 0}; parent retained focus-dependent offers: clipboard={clipboardBefore == clipboardAfter}, primary={primaryBefore == primaryAfter}.");
        }
        finally
        {
            if (!child.HasExited)
                child.Kill(entireProcessTree: true);
        }
    }

    public static void RunChild()
    {
        var expectedClipboard = Console.ReadLine() ?? throw new InvalidOperationException("Missing clipboard expectation.");
        var expectedPrimary = Console.ReadLine() ?? throw new InvalidOperationException("Missing primary-selection expectation.");
        using var display = DisplayServer.Open("Clipboard reader", new Vector2i(64, 64));
        var windows = SDL.GetWindows(out var count);
        if (count != 1 || windows is not { Length: 1 })
            throw new InvalidOperationException("The clipboard reader did not create one window.");
        var window = windows[0];
        var renderer = SDL.CreateRenderer(window, "software");
        if (renderer == 0)
            throw new InvalidOperationException("The clipboard reader could not create a visible surface.");
        try
        {
            if (!SDL.SetRenderDrawColor(renderer, 35, 80, 180, 255) || !SDL.RenderClear(renderer) ||
                !SDL.RenderPresent(renderer) || !SDL.SyncWindow(window))
                throw new InvalidOperationException("The clipboard reader could not present its window.");
            var deadline = Stopwatch.StartNew();
            while (SDL.GetKeyboardFocus() != window && deadline.Elapsed < TimeSpan.FromSeconds(2))
            {
                DisplayServer.ProcessEvents();
                Thread.Sleep(10);
            }
            DisplayServer.ProcessEvents();
            var focused = SDL.GetKeyboardFocus() == window;
            var clipboard = DisplayServer.ClipboardGet();
            var primary = DisplayServer.ClipboardGetPrimary();
            Console.WriteLine($"FOCUS:{(focused ? 1 : 0)}");
            Console.WriteLine($"CLIPBOARD:{(Digest(clipboard) == expectedClipboard ? 1 : 0)}:{clipboard.Length}");
            Console.WriteLine($"PRIMARY:{(Digest(primary) == expectedPrimary ? 1 : 0)}:{primary.Length}");
        }
        finally
        {
            SDL.DestroyRenderer(renderer);
        }
    }

    private static bool TryParseResult(string line, string label, out bool match, out int length)
    {
        var fields = line.Trim().Split(':');
        length = 0;
        match = fields.Length == 3 && fields[1] == "1";
        return fields.Length == 3 && fields[0] == label && fields[1] is "0" or "1" &&
            int.TryParse(fields[2], out length) && length >= 0;
    }

    private static string Digest(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
}
