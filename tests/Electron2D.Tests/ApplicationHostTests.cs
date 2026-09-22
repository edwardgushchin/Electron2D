using System.Diagnostics;
using Electron2D;
using Electron2D.HostExample;
using SDL3;

internal static class ApplicationHostTests
{
    public static void Run()
    {
        var host = new ApplicationHost("Host checks", new Vector2I(160, 100), 20);
        var order = new List<string>();
        var root = new ProbeNode(host, order);
        var clock = Stopwatch.StartNew();
        host.Run(root);
        Check(root.IsDisposed && root.Frames == 2 && root.InputSeen && root.PhysicsSeen &&
              order.IndexOf("input") < order.IndexOf("process") &&
              order.IndexOf("physics") < order.LastIndexOf("process") &&
              clock.Elapsed >= TimeSpan.FromMilliseconds(40),
            "The host pumps native input before scene frames, caps cadence, and disposes its root.");
        Check(Engine.Instance.MainLoop is null && DisplayServer.Instance is null &&
              !Input.Instance.IsKeyPressed(Key.A),
            "Host shutdown detaches Engine, releases SDL video, and clears native key state.");

        var close = new ProbeNode(host, [], closeOnReady: true);
        host.Run(close);
        Check(close.IsDisposed && close.Frames == 0 && DisplayServer.Instance is null,
            "A native close request exits before the next frame and permits reopening.");

        var failing = new ProbeNode(host, [], throwOnInput: true);
        try
        {
            host.Run(failing);
            throw new InvalidOperationException("A throwing input callback must fail the run.");
        }
        catch (AggregateException error) when (error.ToString().Contains("injected input failure", StringComparison.Ordinal))
        {
            Check(failing.IsDisposed && Engine.Instance.MainLoop is null && DisplayServer.Instance is null,
                "Input failure releases the loop, scene, and window.");
        }

        var startup = new ProbeNode(host, [], throwOnReady: true);
        try
        {
            host.Run(startup);
            throw new InvalidOperationException("A throwing ready callback must fail startup.");
        }
        catch (AggregateException error) when (error.ToString().Contains("injected ready failure", StringComparison.Ordinal))
        {
            Check(startup.IsDisposed && Engine.Instance.MainLoop is null && DisplayServer.Instance is null,
                "Failed scene construction releases the transferred root and SDL window.");
        }

        host.Run(new ProbeNode(host, [], closeOnReady: true));
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private sealed class ProbeNode(
        ApplicationHost host,
        List<string> order,
        bool closeOnReady = false,
        bool throwOnInput = false,
        bool throwOnReady = false) : Node
    {
        public int Frames { get; private set; }
        public bool InputSeen { get; private set; }
        public bool PhysicsSeen { get; private set; }

        protected override void OnReady()
        {
            if (throwOnReady)
                throw new InvalidOperationException("injected ready failure");
            InputEnabled = true;
            ProcessEnabled = true;
            PhysicsProcessEnabled = true;
            var windows = SDL.GetWindows(out var count);
            Check(count == 1 && windows is { Length: 1 }, "The host owns one SDL window.");
            var windowId = SDL.GetWindowID(windows![0]);
            var @event = closeOnReady
                ? new SDL.Event { Window = new SDL.WindowEvent { Type = SDL.EventType.WindowCloseRequested, WindowID = windowId } }
                : new SDL.Event
                {
                    Key = new SDL.KeyboardEvent
                    {
                        Type = SDL.EventType.KeyDown,
                        WindowID = windowId,
                        Key = SDL.Keycode.A,
                        Scancode = SDL.Scancode.A,
                        Down = true
                    }
                };
            Check(SDL.PushEvent(ref @event), "SDL accepts the host probe event.");
        }

        protected override void OnInput(InputEvent @event)
        {
            if (@event is not InputEventKey { Keycode: Key.A, Pressed: true })
                return;
            Check(Input.Instance.IsKeyPressed(Key.A), "Native key state commits before scene input.");
            InputSeen = true;
            order.Add("input");
            if (throwOnInput)
                throw new InvalidOperationException("injected input failure");
        }

        protected override void OnPhysicsProcess(double delta)
        {
            Check(double.IsFinite(delta) && delta >= 0d, "Physics delta is finite and nonnegative.");
            PhysicsSeen = true;
            order.Add("physics");
        }

        protected override void OnProcess(double delta)
        {
            Check(double.IsFinite(delta) && delta >= 0d && InputSeen,
                "Input precedes every process frame with a finite monotonic delta.");
            Frames++;
            order.Add("process");
            if (Frames == 2)
                host.RequestExit();
        }
    }
}
