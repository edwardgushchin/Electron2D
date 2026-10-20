using Electron2D;
using System.Reflection;
using System.Runtime.InteropServices;
using Path = System.IO.Path;
using SDL = SDL3.SDL;

internal static class DisplayServerRelationsTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static T Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T error) { return error; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static nint Native(DisplayServer display) { var handle = display.AcquireRenderingWindow(); try { return handle.DangerousGetHandle(); } finally { display.ReleaseRenderingWindow(); } }
    private static nint Native(Window window) => SDL.GetWindows(out _)!.Single(native => SDL.GetWindowTitle(native) == window.Title);
    private static bool Modal(nint window) => (SDL.GetWindowFlags(window) & SDL.WindowFlags.Modal) != 0;
    private static FieldInfo ParentCall => typeof(SDL).GetField("SetWindowParentNativeFunction", BindingFlags.Static | BindingFlags.NonPublic)!;
    private static FieldInfo ModalCall => typeof(SDL).GetField("SetWindowModalNativeFunction", BindingFlags.Static | BindingFlags.NonPublic)!;
    private static Delegate? _parentCall, _modalCall;
    private static int _parentFailure, _modalFailure;
    private static bool Parent(nint window, nint parent)
    { if (--_parentFailure == 0) { SDL.SetError("injected transient parent failure"); return false; } return (bool)_parentCall!.DynamicInvoke(window, parent)!; }
    private static bool SetModal(nint window, bool modal)
    { if (--_modalFailure == 0) { SDL.SetError("injected transient modality failure"); return false; } return (bool)_modalCall!.DynamicInvoke(window, modal)!; }
    private static void WithFailure(int parent, int modal, Action action)
    {
        _parentCall = (Delegate)ParentCall.GetValue(null)!; _modalCall = (Delegate)ModalCall.GetValue(null)!;
        _parentFailure = parent; _modalFailure = modal;
        try
        {
            ParentCall.SetValue(null, typeof(DisplayServerRelationsTests).GetMethod(nameof(Parent), BindingFlags.Static | BindingFlags.NonPublic)!.CreateDelegate(ParentCall.FieldType));
            ModalCall.SetValue(null, typeof(DisplayServerRelationsTests).GetMethod(nameof(SetModal), BindingFlags.Static | BindingFlags.NonPublic)!.CreateDelegate(ModalCall.FieldType));
            action();
        }
        finally { ParentCall.SetValue(null, _parentCall); ModalCall.SetValue(null, _modalCall); _parentCall = _modalCall = null; }
    }
    internal static void Display()
    {
        Reject<InvalidOperationException>(() => DisplayServer.WindowGetAttachedInstanceID());
        Reject<InvalidOperationException>(() => DisplayServer.WindowSetExclusive(0, true));
        Reject<InvalidOperationException>(() => DisplayServer.WindowSetTransient(0, -1));
        using var root = DisplayServer.Open("Window relation contracts", new(80, 64), hidden: true);
        using var first = root.CreateChildWindow("First", new(80, 64));
        using var second = root.CreateChildWindow("Second", new(80, 64));
        var id = first.WindowID; var other = second.WindowID;
        Check(DisplayServer.WindowGetAttachedInstanceID() == 0 && DisplayServer.WindowGetAttachedInstanceID(id) == 0, "Lower-level native hosts have no invented scene identity.");
        foreach (var missing in new[] { -2, -1, int.MaxValue })
        {
            Reject<ArgumentOutOfRangeException>(() => DisplayServer.WindowGetAttachedInstanceID(missing));
            Reject<ArgumentOutOfRangeException>(() => DisplayServer.WindowSetExclusive(missing, true));
            Reject<ArgumentOutOfRangeException>(() => DisplayServer.WindowSetTransient(missing, -1));
        }
        Reject<ArgumentOutOfRangeException>(() => DisplayServer.WindowSetTransient(id, int.MaxValue));
        Reject<ArgumentException>(() => DisplayServer.WindowSetTransient(id, id));
        Task.Run(() =>
        {
            Reject<InvalidOperationException>(() => DisplayServer.WindowGetAttachedInstanceID(id));
            Reject<InvalidOperationException>(() => DisplayServer.WindowSetTransient(id, 0));
            Reject<InvalidOperationException>(() => DisplayServer.WindowSetExclusive(id, false));
        }).GetAwaiter().GetResult();
        var a = Native(first); var b = Native(second); var main = Native(root);
        WithFailure(1, 0, () => Reject<InvalidOperationException>(() => DisplayServer.WindowSetTransient(id, 0)));
        Check(SDL.GetWindowParent(a) == 0 && !Modal(a), "Failed initial native parenting publishes no relationship.");
        if (SDL.GetCurrentVideoDriver() == "dummy")
        {
            Console.WriteLine("Display relation absent-host/identity/thread guards and initial native failure passed (dummy)."); return;
        }
        DisplayServer.WindowSetExclusive(id, true);
        Check(!Modal(a), "An unparented exclusive request does not fabricate native modality.");
        DisplayServer.WindowSetTransient(id, 0);
        Check(SDL.GetWindowParent(a) == main && Modal(a), "Parenting applies the pending native exclusive request.");
        DisplayServer.WindowSetTransient(id, 0); DisplayServer.WindowSetExclusive(id, true);
        WithFailure(0, 2, () => Reject<InvalidOperationException>(() => DisplayServer.WindowSetTransient(id, other)));
        Check(SDL.GetWindowParent(a) == main && Modal(a), "Reparent failure restores both the previous parent and native modality.");
        DisplayServer.WindowSetExclusive(id, false);
        WithFailure(0, 1, () => Reject<InvalidOperationException>(() => DisplayServer.WindowSetExclusive(id, true)));
        Check(!Modal(a), "Rejected modality preserves the previous request and observed native state.");
        Reject<InvalidOperationException>(() => DisplayServer.WindowSetMode(WindowMode.Fullscreen, id));
        Reject<InvalidOperationException>(() => DisplayServer.WindowSetMode(WindowMode.ExclusiveFullscreen, id));
        DisplayServer.WindowSetTransient(id, other);
        Check(SDL.GetWindowParent(a) == b && !Modal(a), "Successful reparenting keeps the last accepted exclusive request.");
        DisplayServer.WindowSetExclusive(id, true);
        var failure = WithRollbackFailure(id);
        Check(failure.InnerExceptions.Count == 2 && SDL.GetWindowParent(a) == b && !Modal(a), "Request and rollback failures remain visible with the actual partial native state.");
        DisplayServer.WindowSetExclusive(id, true);
        Check(Modal(a), "The last accepted exclusive request can recover after a failed rollback.");
        DisplayServer.WindowSetTransient(other, 0);
        Reject<ArgumentException>(() => DisplayServer.WindowSetTransient(0, id));
        Check(SDL.GetWindowParent(main) == 0 && SDL.GetWindowParent(b) == main, "A three-window cycle fails before native publication.");
        var secondClosed = 0;
        Action close = () => { DisplayServer.WindowSetTransient(id, other); throw new InvalidOperationException("relation observer"); };
        DisplayServer.CloseRequested += close; second.CloseRequestedCore += () => secondClosed++;
        DisplayServer.ProcessEvents();
        foreach (var native in new[] { main, b })
        {
            var value = new SDL.Event { Window = new SDL.WindowEvent { Type = SDL.EventType.WindowCloseRequested, WindowID = SDL.GetWindowID(native) } };
            Check(SDL.PushEvent(ref value), "Queue native relationship callbacks.");
        }
        try { Reject<AggregateException>(DisplayServer.ProcessEvents); }
        finally { DisplayServer.CloseRequested -= close; }
        Check(secondClosed == 1 && SDL.GetWindowParent(a) == b, "A failing callback retains its committed native relationship and later queued callbacks still run.");
        using (var popup = root.CreateChildWindow("Retirement failure popup", new(24, 24), popupParent: second))
        {
            popup.Disposed += _ => throw new InvalidOperationException("popup retirement observer");
            AggregateException? combined = null;
            WithFailure(1, 0, () => combined = Reject<AggregateException>(() => second.PrepareNativeWindowRetirement()));
            Check(combined!.InnerExceptions.Count == 2 && popup.IsDisposed && SDL.GetWindowParent(a) == b && Modal(a), "Popup observer and parent-detachment failures remain visible while ordinary ownership rolls back.");
            DisplayServer.WindowSetExclusive(other, false);
        }
        DisplayServer.WindowSetTransient(other, -1); DisplayServer.WindowSetTransient(id, 0);
        DisplayServer.WindowSetTransient(0, other);
        second.Dispose();
        Check(SDL.GetWindowParent(main) == 0 && SDL.GetWindowParent(a) == main, "Retiring a transient parent detaches the surviving main window without disturbing its children.");
        Reject<ArgumentOutOfRangeException>(() => DisplayServer.WindowGetAttachedInstanceID(other));
        Reject<ArgumentOutOfRangeException>(() => DisplayServer.WindowSetTransient(id, other));
        Reject<ArgumentOutOfRangeException>(() => DisplayServer.WindowSetExclusive(other, false));
        DisplayServer.WindowSetTransient(id, -1);
        Check(SDL.GetWindowParent(a) == 0 && !Modal(a), "Explicit detachment clears effective modality.");
        Console.WriteLine("Display relation native parenting/modality, pending policy, reparent/rollback failures, cycles, main/child retirement and owner guards passed.");
    }
    private static AggregateException WithRollbackFailure(int id)
    {
        AggregateException? failure = null;
        WithFailure(1, 2, () => failure = Reject<AggregateException>(() => DisplayServer.WindowSetTransient(id, 0)));
        return failure!;
    }
    private static void Recreate(Window first, Popup popup)
    {
        var retired = first.GetWindowID(); Reject<AggregateException>(first.Hide);
        Check(!first.Visible && !popup.Visible, "Native parent/popup retirement completes despite observer failure and rejects reentrant relationship changes.");
        Reject<ArgumentOutOfRangeException>(() => DisplayServer.WindowGetAttachedInstanceID(retired));
        first.Show(); Check(first.GetWindowID() > retired && DisplayServer.WindowGetAttachedInstanceID(first.GetWindowID()) == first.InstanceID, "Native recreation preserves scene identity while replacing native identity.");
    }
    private sealed class TextReceiver : Control
    {
        internal int Commits, Compositions;
        internal string Composition = string.Empty;
        protected override void OnTextInput(string text) { Commits++; }
        protected override void OnIMECompositionChanged(string text, Vector2i selection) { Composition = text; if (text.Length != 0) Compositions++; }
    }
    private static void QueueText(Window window, nint text)
    {
        var editing = new SDL.Event { Edit = new SDL.TextEditingEvent { Type = SDL.EventType.TextEditing, WindowID = SDL.GetWindowID(Native(window)), Text = text, Start = 0, Length = 1 } };
        var commit = new SDL.Event { Text = new SDL.TextInputEvent { Type = SDL.EventType.TextInput, WindowID = SDL.GetWindowID(Native(window)), Text = text } };
        var clear = new SDL.Event { Edit = new SDL.TextEditingEvent { Type = SDL.EventType.TextEditing, WindowID = SDL.GetWindowID(Native(window)), Text = 0 } };
        Check(SDL.PushEvent(ref editing) && SDL.PushEvent(ref clear) && SDL.PushEvent(ref commit), "Queue parent native composition and committed text.");
    }
    private static void QueueKey(Window window)
    {
        foreach (var pressed in new[] { true, false })
        {
            var value = new SDL.Event { Key = new SDL.KeyboardEvent { Type = pressed ? SDL.EventType.KeyDown : SDL.EventType.KeyUp, WindowID = SDL.GetWindowID(Native(window)), Down = pressed, Key = SDL.Keycode.F12, Scancode = SDL.Scancode.F12 } };
            Check(SDL.PushEvent(ref value), "Queue native parent input.");
        }
    }
    internal static void RunNative(string backend)
    {
        if (backend is not ("gpu" or "compatibility")) throw new ArgumentException("Unknown renderer.", nameof(backend));
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var root = new Window { Title = "Window relations", Size = new(128, 96), Borderless = true };
        var first = new Window { Title = "Transient first", Size = new(80, 64), Borderless = true, Transient = true };
        var second = new Window { Title = "Transient second", Size = new(80, 64), Borderless = true };
        first.AddChild(new ColorRect { Color = Colors.Red, Size = new(80, 64) }); second.AddChild(new ColorRect { Color = Colors.Blue, Size = new(80, 64) });
        var receiver = new TextReceiver { Size = new(40, 16), FocusMode = FocusMode.All }; first.AddChild(receiver);
        var nativeText = Marshal.StringToCoTaskMemUTF8("parent é");
        var parentKeys = 0;
        first.WindowInput += input => { if (input is InputEventKey { Pressed: true, Keycode: Key.F12 }) parentKeys++; };
        var popup = new Popup { Title = "Transient fixed popup", Size = new(32, 24) }; first.AddChild(popup);
        popup.AddChild(new ColorRect { Color = Colors.Green, Size = new(32, 24) });
        root.AddChild(first); root.AddChild(second); var frames = 0; long start = 0, allocated = 0;
        Action? before = null, after = null;
        try
        {
            root.Ready += _ =>
            {
                var a = first.GetWindowID(); var b = second.GetWindowID();
                Check(DisplayServer.WindowGetAttachedInstanceID() == root.InstanceID && DisplayServer.WindowGetAttachedInstanceID(a) == first.InstanceID && DisplayServer.WindowGetAttachedInstanceID(b) == second.InstanceID, "Attached IDs resolve the actual scene Window independently of native IDs.");
                DisplayServer.WindowSetExclusive(a, true); DisplayServer.WindowSetTransient(a, b);
                Check(SDL.GetWindowParent(Native(first)) == Native(second) && Modal(Native(first)) && first.Transient && !first.Exclusive && first.Parent == root, "Runtime native overrides preserve stored scene policy and ancestry.");
                first.Transient = false;
                Check(SDL.GetWindowParent(Native(first)) == 0 && !Modal(Native(first)), "The next explicit scene relationship update reapplies stored policy.");
                first.Transient = true;
                var flag = typeof(RenderingServer).GetField("_nativeFrameStarted", BindingFlags.Instance | BindingFlags.NonPublic)!;
                flag.SetValue(RenderingServer.Service, true);
                try { Reject<InvalidOperationException>(() => DisplayServer.WindowSetTransient(a, b)); Reject<InvalidOperationException>(() => DisplayServer.WindowSetExclusive(a, true)); }
                finally { flag.SetValue(RenderingServer.Service, false); }
                DisplayServer.WindowSetTransient(a, 0); DisplayServer.WindowSetExclusive(a, false);
                receiver.GrabFocus();
                popup.PopupOnParent(new(8, 8, 32, 24)); var popupID = popup.GetWindowID();
                DisplayServer.WindowSetTransient(popupID, a);
                Reject<InvalidOperationException>(() => DisplayServer.WindowSetTransient(popupID, -1));
                Reject<InvalidOperationException>(() => DisplayServer.WindowSetTransient(popupID, b));
                Reject<ArgumentException>(() => DisplayServer.WindowSetTransient(a, popupID));
                DisplayServer.WindowSetExclusive(popupID, true); Check(!popup.Exclusive, "Runtime popup exclusivity preserves its stored scene policy.");
                receiver.Composition = "stale"; QueueKey(first); QueueText(first, nativeText);
                var popupRetired = 0;
                popup.PopupHide += () =>
                {
                    Reject<InvalidOperationException>(() => DisplayServer.WindowSetExclusive(a, false));
                    popupRetired++; throw new InvalidOperationException("retirement observer");
                };
                before = () =>
                {
                    if (frames >= 32)
                    {
                        start = GC.GetAllocatedBytesForCurrentThread();
                        DisplayServer.WindowSetTransient(a, 0); DisplayServer.WindowSetExclusive(a, false);
                        Check(DisplayServer.WindowGetAttachedInstanceID(a) == first.InstanceID, "Prepared attached identity remains stable.");
                    }
                };
                after = () =>
                {
                    if (frames == 0)
                    {
                        Check(parentKeys == 0 && receiver.Commits == 0 && receiver.Compositions == 0 && receiver.Composition == string.Empty, "A runtime exclusive popup suppresses parent key, composition and committed text before scene input.");
                        DisplayServer.WindowSetExclusive(popupID, false); QueueKey(first); QueueText(first, nativeText);
                    }
                    if (frames == 1) Check(parentKeys == 1 && receiver.Commits == 1 && receiver.Compositions == 1, "Clearing runtime popup exclusivity restores parent key, composition and text reception.");
                    if (frames == 2)
                    {
                        using var pixels = RenderingServer.Service!.ReadbackNativeWindow(a);
                        Check(pixels.GetPixel(8, 8).R > .95f, "The transient child retains real rendering after native reparenting.");
                        var capture = Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR");
                        if (capture is not null) { Directory.CreateDirectory(capture); pixels.SavePNG(Path.Combine(capture, "window-relations-" + backend + ".png")); }
                    }
                    if (frames >= 32) allocated += GC.GetAllocatedBytesForCurrentThread() - start;
                    if (++frames < 96) return;
                    Check(allocated == 0, "Prepared relation queries/no-op requests allocated " + allocated + " owner bytes.");
                    Recreate(first, popup); Check(popupRetired == 1, "The popup retirement callback ran once.");
                    Console.WriteLine(backend + " native relation/scene/queue/pixels and 64 prepared frames: 0 managed owner bytes passed."); root.Tree!.Quit();
                };
                RenderingServer.FramePreDraw += before; RenderingServer.FramePostDraw += after;
            };
            Engine.Run(root);
        }
        finally
        {
            if (DisplayServer.IsAvailable) { RenderingServer.FramePreDraw -= before; RenderingServer.FramePostDraw -= after; }
            try { root.Dispose(); } finally { Marshal.FreeCoTaskMem(nativeText); }
        }
    }
}
