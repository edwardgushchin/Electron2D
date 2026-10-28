using System.Runtime.InteropServices;
using Electron2D;
using static MovieWriterTests;

internal static class NativeTextInputTests
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int Tick(nint data);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void Destroy(nint data);
    private static readonly Tick ProbeCallback = Probe;
    private static readonly Destroy FreeProbe = data => GCHandle.FromIntPtr(data).Free();
    private static int _closeRequested;
    private sealed class ProbeState(DisplayServer display, bool close)
    {
        internal readonly DisplayServer Display = display;
        internal readonly bool Close = close;
        internal bool Fired;
        internal bool Active = true;
        internal Exception? Failure;
    }
    [DllImport("libglib-2.0.so.0", EntryPoint = "g_timeout_add_full")] private static extern uint AddTimeout(int priority, uint milliseconds, Tick callback, nint data, Destroy destroy);
    [DllImport("libglib-2.0.so.0", EntryPoint = "g_source_remove")] private static extern int RemoveSource(uint source);
    [DllImport("libgtk-3.so.0", EntryPoint = "gtk_init_check")] private static extern int InitGTK(nint argc, nint argv);
    [DllImport("libgtk-3.so.0", EntryPoint = "gtk_window_list_toplevels")] private static extern nint ListWindows();
    [DllImport("libgtk-3.so.0", EntryPoint = "gtk_window_get_title")] private static extern nint GetTitle(nint window);
    [DllImport("libgtk-3.so.0", EntryPoint = "gtk_window_close")] private static extern void CloseWindow(nint window);
    [DllImport("libglib-2.0.so.0", EntryPoint = "g_list_free")] private static extern void FreeList(nint list);
    private static int Probe(nint data)
    {
        var state = (ProbeState)GCHandle.FromIntPtr(data).Target!;
        try
        {
            if (!state.Fired)
            {
                Reject<InvalidOperationException>(state.Display.Dispose);
                Reject<InvalidOperationException>(DisplayServer.ProcessEvents);
                Reject<InvalidOperationException>(DisplayServer.ForceProcessAndDropEvents);
                Reject<InvalidOperationException>(() => DisplayServer.DialogInputText("reenter", "", "", _ => { }));
                Console.WriteLine("INPUT_GUARDS_OK"); state.Fired = true;
            }
            if (state.Close)
            {
                if (Volatile.Read(ref _closeRequested) == 0) return 1;
                var list = ListWindows();
                try
                {
                    var closed = false;
                    for (var item = list; item != 0; item = Marshal.ReadIntPtr(item, IntPtr.Size))
                    {
                        var window = Marshal.ReadIntPtr(item);
                        if (Marshal.PtrToStringUTF8(GetTitle(window)) != "Electron2D input close") continue;
                        CloseWindow(window); closed = true; break;
                    }
                    Check(closed, "The matching native top-level receives a real close request.");
                }
                finally { FreeList(list); }
            }
        }
        catch (Exception error) { state.Failure = error; Console.WriteLine("INPUT_GUARDS_FAILED"); }
        state.Fired = true; state.Active = false; return 0;
    }
    private static void WithProbe(DisplayServer display, Action operation, bool close = false)
    {
        var state = new ProbeState(display, close); var handle = GCHandle.Alloc(state); var source = AddTimeout(0, 50, ProbeCallback, GCHandle.ToIntPtr(handle), FreeProbe);
        if (source == 0) { handle.Free(); throw new InvalidOperationException("Could not register the native guard probe."); }
        try { operation(); }
        finally { if (state.Active) _ = RemoveSource(source); }
        Check(state.Fired && state.Failure == null, "The real GTK modal loop enforces owner/retirement/pump/reentry guards.");
    }

    internal static void Run(string profile)
    {
        if (profile is "gpu" or "compatibility") { RunEngine(profile); return; }
        Reject<InvalidOperationException>(() => DisplayServer.DialogInputText("", "", "", _ => { }));
        if (profile == "mismatch") Check(InitGTK(0, 0) != 0, "The host initializes its foreign GTK display before SDL opens.");
        using var display = DisplayServer.Open("Electron2D input parent", new(320, 240));
        Reject<ArgumentNullException>(() => DisplayServer.DialogInputText(null!, "", "", _ => { }));
        Reject<ArgumentNullException>(() => DisplayServer.DialogInputText("", null!, "", _ => { }));
        Reject<ArgumentNullException>(() => DisplayServer.DialogInputText("", "", null!, _ => { }));
        Reject<ArgumentNullException>(() => DisplayServer.DialogInputText("", "", "", null!));
        foreach (var field in new[] { 0, 1, 2 })
            Reject<ArgumentException>(() => DisplayServer.DialogInputText(field == 0 ? "bad\0title" : "", field == 1 ? "bad\0description" : "", field == 2 ? "bad\0text" : "", _ => { }));
        Reject<InvalidOperationException>(() => Task.Run(() => DisplayServer.DialogInputText("", "", "", _ => { })).GetAwaiter().GetResult());
        if (profile is "unsupported" or "mismatch")
        {
            Check(!DisplayServer.HasFeature(DisplayServer.Feature.NativeDialogInput), "An unsupported driver advertises no native text input.");
            Reject<NotSupportedException>(() => DisplayServer.DialogInputText("unused", "", "", _ => throw new Exception("No unlaunched callback.")));
            DisplayServer.ProcessEvents(); Console.WriteLine("Native text input unsupported/validation passed."); return;
        }
        Check(DisplayServer.HasFeature(DisplayServer.Feature.NativeDialogInput), "The executing GTK backend advertises native text input.");
        Reject<ArgumentException>(() => DisplayServer.DialogInputText("roundtrip", "not shown", "bad\ud800text", _ => { }));
        Reject<ArgumentException>(() => DisplayServer.DialogInputText("roundtrip", "not shown", new string('x', 65536), _ => { }));
        var owner = Environment.CurrentManagedThreadId;
        foreach (var name in new[] { "accept", "empty", "cancel", "close", "callback-error", "outer" })
        {
            var calls = 0; Console.WriteLine("INPUT_READY:" + name);
            var closeRequest = name == "close" ? Task.Run(() => { Check(Console.ReadLine() == "close", "The independent controller requested native close."); Volatile.Write(ref _closeRequested, 1); }) : null;
            void Show() => WithProbe(display, () => DisplayServer.DialogInputText("Electron2D input " + name, "Введите текст / Input <plain> & text", "Исходный 🌙 e\u0301", value =>
            {
                calls++; Check(Environment.CurrentManagedThreadId == owner && value == (name == "empty" ? "" : "Новый 🌌 e\u0301"), "The native callback receives the exact edited UTF-8 text on its owner.");
                DisplayServer.ProcessEvents();
                if (name == "callback-error") throw new FormatException("Expected native text callback error");
                if (name == "outer")
                {
                    var nestedCalls = 0; Console.WriteLine("INPUT_READY:nested");
                    WithProbe(display, () => DisplayServer.DialogInputText("Electron2D input nested", "Nested input", "Nested", nested => { Check(nested == "Nested edited", "A callback can open a new native dialog after cleanup."); nestedCalls++; }));
                    Check(nestedCalls == 1, "One nested confirmation."); Console.WriteLine("INPUT_DONE:nested");
                }
            }), close: name == "close");
            if (name == "callback-error") Reject<FormatException>(Show); else Show();
            closeRequest?.GetAwaiter().GetResult();
            Check(calls == (name is "cancel" or "close" ? 0 : 1), "Only confirmation invokes the string callback, exactly once.");
            DisplayServer.ProcessEvents(); Console.WriteLine("INPUT_DONE:" + name);
        }
        DisplayServer.DialogInputText("Electron2D input disposal", "Dispose in callback", "Dispose", value => { Check(value == "Disposed safely", "Final edited value copied before native teardown."); display.Dispose(); });
        Check(!DisplayServer.IsAvailable, "Callback disposal occurs after modal/native ownership release.");
        Console.WriteLine("Native text input contract passed.");
    }
    private static void RunEngine(string backend)
    {
        var previous = ProjectSettings.Get(ProjectSettings.RenderingMethod); ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        var root = new Window { Title = "Native text input engine parent", Size = new(320, 240) };
        root.AddChild(new ColorRect { Size = new(320, 240), Color = Colors.Red });
        var frames = 0; var calls = 0;
        void Capture()
        {
            frames++;
            if (frames == 1)
                DisplayServer.DialogInputText("Electron2D input engine", "Engine input", "Engine", text => { Check(text == "Engine edited", "A running game receives the native edited value."); calls++; Reject<InvalidOperationException>(DisplayServer.ProcessEvents); });
            else
            {
                using var image = RenderingServer.Service!.Readback();
                Check(image.GetPixel(8, 8) == Colors.Red && calls == 1, "The running renderer resumes its real pixel output after native input.");
                RenderingServer.FramePostDraw -= Capture; root.Tree!.Quit();
            }
        }
        root.Ready += _ => RenderingServer.FramePostDraw += Capture;
        try { Check(Engine.Run(root) == 0 && frames == 2 && !DisplayServer.IsAvailable && !RenderingServer.IsAvailable, "The scene/native lifecycle finishes after confirmation."); }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, previous); }
        Console.WriteLine("Native text input engine " + backend + " passed.");
    }

}
