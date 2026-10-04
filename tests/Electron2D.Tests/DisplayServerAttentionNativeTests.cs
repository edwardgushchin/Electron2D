using Electron2D;
using SDL3;

internal static class DisplayServerAttentionNativeTests
{
    public static void Run()
    {
        using var display = DisplayServer.Open("Electron2D attention protocol test", new Vector2i(200, 150));
        if (DisplayServer.GetName() != "Wayland")
            throw new InvalidOperationException("The attention protocol test requires Wayland.");

        var windows = SDL.GetWindows(out var count);
        if (count != 1 || windows is not { Length: 1 })
            throw new InvalidOperationException("The attention protocol test requires one native window.");
        var window = windows[0];
        var renderer = SDL.CreateRenderer(window, "software");
        if (renderer == 0)
            throw new InvalidOperationException($"Cannot present the test window: {SDL.GetError()}");
        try
        {
            if (!SDL.SetRenderDrawColor(renderer, 35, 80, 180, 255) ||
                !SDL.RenderClear(renderer) || !SDL.RenderPresent(renderer) || !SDL.SyncWindow(window))
                throw new InvalidOperationException($"Cannot show the test window: {SDL.GetError()}");

            ExpectInvalidWindow(() => DisplayServer.WindowMoveToForeground(1));
            ExpectInvalidWindow(() => DisplayServer.WindowRequestAttention(1));

            Console.Error.WriteLine("FOREGROUND_BEGIN");
            DisplayServer.WindowMoveToForeground();
            DisplayServer.ProcessEvents();
            Console.Error.WriteLine("FOREGROUND_END");

            Console.Error.WriteLine("ATTENTION_BEGIN");
            DisplayServer.WindowRequestAttention();
            for (var i = 0; i < 20; i++)
            {
                DisplayServer.ProcessEvents();
                Thread.Sleep(50);
            }
            Console.Error.WriteLine("ATTENTION_END");
        }
        finally
        {
            SDL.DestroyRenderer(renderer);
        }
    }

    private static void ExpectInvalidWindow(Action action)
    {
        try
        {
            action();
        }
        catch (ArgumentOutOfRangeException)
        {
            return;
        }

        throw new InvalidOperationException("The attention operation accepted a foreign window ID.");
    }
}
