using System.Diagnostics;
using Electron2D;
using SDL3;
using static MovieWriterTests;

internal static class WindowContentFitTests
{
    internal static void Run(string backend)
    {
        if (backend == "managed") Managed();
        else Native(backend);
        Console.WriteLine($"Window content fitting passed ({backend}).");
    }

    private static void Managed()
    {
        using var host = new SubViewport { Size = new(640, 480), GUIEmbedSubwindows = true };
        using var window = new ProbeWindow { Size = new(60, 50), Borderless = true, WrapControls = true };
        window.Extent = new(80, 70);
        window.ChildControlsChanged();
        Check(window.Size == new Vector2i(60, 50), "Detached requests leave authored size alone.");
        host.AddChild(window);
        using var tree = new SceneTree(host);
        tree.FlushDeferred();
        Check(window.Size == new Vector2i(80, 70), "A visible wrapped window fits content when entering the tree.");

        window.Measurements = 0;
        window.Extent = new(90, 75);
        window.ChildControlsChanged();
        window.Extent = new(100.25f, 85.5f);
        window.ChildControlsChanged();
        Check(window.Size == new Vector2i(80, 70), "The request is deferred.");
        tree.FlushDeferred();
        Check(window.Size == new Vector2i(101, 86) && window.Measurements == 1, "Requests coalesce and measure the latest content once.");

        window.Extent = new(120, 95);
        window.ChildControlsChanged();
        window.Hide();
        tree.FlushDeferred();
        Check(window.Size == new Vector2i(101, 86), "Hiding before flush prevents resizing.");
        window.ChildControlsChanged();
        window.Show();
        tree.FlushDeferred();
        Check(window.Size == new Vector2i(120, 95), "Showing refreshes a hidden window's changed contents.");

        window.Extent = new(130, 110);
        window.ChildControlsChanged();
        host.RemoveChild(window);
        host.AddChild(window);
        window.Measurements = 0;
        tree.FlushDeferred();
        Check(window.Size == new Vector2i(130, 110) && window.Measurements == 1, "Old work cannot consume the reattached window's request.");

        window.Fail = true;
        window.ChildControlsChanged();
        Reject<AggregateException>(tree.FlushDeferred);
        Check(window.Size == new Vector2i(130, 110), "A failed measurement preserves geometry.");
        window.Fail = false;
        window.Extent = new(200, 180);
        window.MaxSize = new(140, 0);
        window.ChildControlsChanged();
        tree.FlushDeferred();
        Check(window.Size == new Vector2i(140, 180), "The refresh caps only the bounded axis.");
        window.Extent = new(70, 70);
        window.ChildControlsChanged();
        tree.FlushDeferred();
        Check(window.Size == new Vector2i(140, 180), "A lower minimum preserves the current size.");

        window.MaxSize = default;
        window.ContentScaleFactor = 1.5f;
        window.Extent = new(110.25f, 125.5f);
        window.ChildControlsChanged();
        tree.FlushDeferred();
        Check(window.Size == new Vector2i(166, 189), "Deferred measurement applies content scaling and rounds up.");
        window.Extent = new(200, 200);
        window.DuringMeasure = () => host.RemoveChild(window);
        window.ChildControlsChanged();
        tree.FlushDeferred();
        Check(window.Tree is null && window.Size == new Vector2i(166, 189), "Detaching from the measurement hook cancels the commit.");
        window.DuringMeasure = null;
        host.AddChild(window);
        tree.FlushDeferred();

        Task.Run(() => Reject<InvalidOperationException>(window.ChildControlsChanged)).GetAwaiter().GetResult();
        var capture = window.BeginSceneCapture();
        try { Reject<InvalidOperationException>(window.ChildControlsChanged); }
        finally { Node.EndSceneCapture(capture); }

        using var automatic = new Window { Borderless = true, Size = new(50, 50), WrapControls = true };
        host.AddChild(automatic);
        using var container = new VBoxContainer { Position = new(3, 4) };
        using var child = new Control { CustomMinimumSize = new(85, 65) };
        automatic.AddChild(container);
        container.AddChild(child);
        tree.FlushDeferred();
        Check(automatic.Size == new Vector2i(88, 69), "Nested layout minimums reach the containing window.");
        child.CustomMinimumSize = new(105, 76);
        tree.FlushDeferred();
        Check(automatic.Size == new Vector2i(108, 80), "A live child minimum grows its containing window.");
        container.CustomMaximumSize = new(125, 95);
        container.CustomMinimumSize = new(150, 120);
        tree.FlushDeferred();
        Check(automatic.Size == new Vector2i(128, 99), "The bounded minimum is used for fitting.");
        container.CustomMaximumSize = new(-1, -1);
        tree.FlushDeferred();
        Check(automatic.Size == new Vector2i(153, 124), "A changed maximum can raise the bounded minimum.");

        window.ChildControlsChanged();
        window.Dispose();
        tree.FlushDeferred();
        Reject<ObjectDisposedException>(window.ChildControlsChanged);
    }

    private static void Native(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        Engine.MaxFPS = 60;
        using var window = new Window { Borderless = true, Size = new(128, 96), WrapControls = true };
        var content = new ColorRect { Position = new(6, 4), Size = new(32, 24), Color = Colors.Green };
        window.AddChild(content);
        var phase = 0;
        window.Ready += _window =>
        {
            var timeout = Stopwatch.StartNew();
            content.CustomMinimumSize = new(184, 130);
            Check(window.Size.X < 190, "An edit in Ready does not synchronously resize the host.");
            RenderingServer.FramePostDraw += () =>
            {
                Check(timeout.Elapsed < TimeSpan.FromSeconds(12), "Native fitting did not deliver the resized frame.");
                if (phase == 0)
                {
                    if (window.Size != new Vector2i(190, 134)) return;
                    Check(DisplayServer.WindowGetSize() == window.Size, "Native client geometry acknowledges the new size.");
                    using var pixels = window.GetTexture().GetImage();
                    if (pixels?.Size != window.Size) return;
                    Check(pixels.GetPixel(170, 115).IsEqualApprox(Colors.Green), "The expanded client contains real child pixels.");
                    var native = SDL.GetWindows(out var count);
                    Check(count == 1, "One window owns the native limits.");
                    CheckNativeMinimum(native![0], new(190, 134));
                    Check(window.MinSize == default && DisplayServer.WindowGetMinSize() == default, "The authored minimum excludes the content constraint.");
                    window.MinSize = new(140, 110);
                    Reject<ArgumentOutOfRangeException>(() => window.MaxSize = new(139, 0));
                    CheckNativeMinimum(native[0], new(190, 134));
                    window.MaxSize = new(160, 0);
                    CheckNativeMinimum(native[0], new(160, 134));
                    Check(window.MinSize == new Vector2i(140, 110) && window.MaxSize == new Vector2i(160, 0), "A lower maximum clamps the effective minimum only.");
                    DisplayServer.WindowSetSize(new(130, 120));
                    phase++;
                    return;
                }

                if (phase != 1 || window.Size != new Vector2i(160, 134)) return;
                var handle = SDL.GetWindows(out _)![0];
                Check(DisplayServer.WindowGetSize() == window.Size, "The native size request respects the effective minimum.");
                window.ContentScaleFactor = .5f;
                window.Tree!.Defer(() =>
                {
                    CheckNativeMinimum(handle, new(140, 110));
                    window.ContentScaleFactor = 1;
                    window.Tree!.Defer(() =>
                    {
                        CheckNativeMinimum(handle, new(160, 134));
                        window.WrapControls = false;
                        CheckNativeMinimum(handle, new(140, 110));
                        phase++;
                        window.Tree!.Quit();
                    });
                });
                phase++;
            };
        };
        Check(Engine.Run(window) == 0 && phase == 3 && window.IsDisposed, "Fitting completes through the ordinary native host.");
    }

    private static void CheckNativeMinimum(nint window, Vector2i expected)
    {
        Check(SDL.GetWindowMinimumSize(window, out var width, out var height) && new Vector2i(width, height) == expected,
            $"SDL minimum ({width}, {height}) should equal {expected}.");
    }

    private sealed class ProbeWindow : Window
    {
        internal Vector2 Extent;
        internal int Measurements;
        internal bool Fail;
        internal Action? DuringMeasure;

        protected override Vector2 OnGetContentsMinimumSize()
        {
            Measurements++;
            if (Fail) throw new ArithmeticException("Injected window content measurement failure.");
            DuringMeasure?.Invoke();
            return Extent;
        }
    }
}
