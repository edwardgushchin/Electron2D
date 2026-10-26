using System.Diagnostics;
using System.Runtime.CompilerServices;
using Electron2D;
using Role = Electron2D.AccessibilityServer.AccessibilityRole;
using AAction = Electron2D.AccessibilityServer.AccessibilityAction;

internal static class AccessibilityModelTests
{
    private static int _checks, _disposed;
    private static void Check(bool value, string message) { _checks++; if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { _checks++; return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private interface IModel { string Name { get; } }
    private sealed class Model(string name) : IModel, IDisposable
    {
        public string Name { get; } = name;
        internal int Clicks;
        public void Dispose() => _disposed++;
    }
    private struct Value { internal int Count; }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference Store(RID id)
    { var model = new Model("Retained"); AccessibilityServer.ElementSetMeta(id, model); return new WeakReference(model); }
    private static bool Alive(WeakReference weak) { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); return weak.IsAlive; }
    internal static void Managed()
    {
        Reject<NotSupportedException>(() => AccessibilityServer.ElementSetMeta<Model>(default, null));
        Reject<NotSupportedException>(() => AccessibilityServer.ElementGetMeta<int?>(default));
        Console.WriteLine($"PASS {_checks} managed producer model contracts.");
    }
    private static void Slots(RID root, int windowID)
    {
        var id = AccessibilityServer.CreateSubElement(root, Role.Button);
        Check(AccessibilityServer.ElementGetMeta<Model>(id) is null && AccessibilityServer.ElementGetMeta<int>(id) == 0 && AccessibilityServer.ElementGetMeta<int?>(id) is null, "Empty slots use the requested type's default.");
        var model = new Model("Typed"); AccessibilityServer.ElementSetMeta(id, model);
        Check(ReferenceEquals(AccessibilityServer.ElementGetMeta<IModel>(id), model), "Compatible reference retrieval preserves identity.");
        Reject<InvalidCastException>(() => AccessibilityServer.ElementGetMeta<string>(id));
        AccessibilityServer.ElementSetMeta(id, 42); Check(AccessibilityServer.ElementGetMeta<int?>(id) == 42, "Nullable value retrieval accepts a boxed value.");
        Reject<InvalidCastException>(() => AccessibilityServer.ElementGetMeta<long>(id));
        var value = new Value { Count = 7 }; AccessibilityServer.ElementSetMeta(id, value); value.Count = 9;
        var copy = AccessibilityServer.ElementGetMeta<Value>(id); copy.Count = 11;
        Check(AccessibilityServer.ElementGetMeta<Value>(id).Count == 7, "Value assignment and retrieval copy without numeric conversion.");
        AccessibilityServer.ElementSetMeta<int?>(id, null); Check(AccessibilityServer.ElementGetMeta<Model>(id) is null, "Null clears independent of the former type.");
        var weak = Store(id); Check(Alive(weak), "The live slot strongly retains its model.");
        AccessibilityServer.ElementSetMeta(id, model); Check(!Alive(weak), "Replacement releases the former reference.");
        weak = Store(id); AccessibilityServer.ElementSetMeta<Model>(id, null); Check(!Alive(weak), "Explicit clearing releases the former reference.");
        weak = Store(id); AccessibilityServer.FreeElement(id); Check(!Alive(weak), "Explicit element retirement releases its model.");
        Reject<KeyNotFoundException>(() => AccessibilityServer.ElementGetMeta<Model>(id)); Reject<KeyNotFoundException>(() => AccessibilityServer.ElementSetMeta<Model>(id, null));
        Reject<KeyNotFoundException>(() => AccessibilityServer.ElementGetMeta<Model>(default)); Reject<KeyNotFoundException>(() => AccessibilityServer.ElementSetMeta(default, 0));
        var canvas = RenderingServer.CanvasCreate();
        try { Reject<KeyNotFoundException>(() => AccessibilityServer.ElementGetMeta<Model>(canvas)); Reject<KeyNotFoundException>(() => AccessibilityServer.ElementSetMeta(canvas, 1)); }
        finally { RenderingServer.FreeRID(canvas); }
        var parent = AccessibilityServer.CreateSubElement(root, Role.Container); var child = AccessibilityServer.CreateSubElement(parent, Role.Button); weak = Store(child);
        AccessibilityServer.FreeElement(parent); Check(!Alive(weak) && !AccessibilityServer.HasElement(child), "Parent-owned subelement retirement releases the model.");
        parent = AccessibilityServer.CreateSubElement(root, Role.Container);
        child = AccessibilityServer.CreateElement(windowID, Role.Button); weak = Store(child); AccessibilityServer.UpdateAddChild(parent, child);
        AccessibilityServer.FreeElement(parent); Check(Alive(weak) && AccessibilityServer.HasElement(child), "Independent child detachment preserves its model.");
        AccessibilityServer.UpdateAddChild(root, child); Check(AccessibilityServer.ElementGetMeta<Model>(child)?.Name == "Retained", "Reparenting a live generic identity preserves its slot.");
        AccessibilityServer.FreeElement(child); Check(!Alive(weak), "Independent element retirement releases its slot.");
        Task.Run(() => { Reject<InvalidOperationException>(() => AccessibilityServer.ElementGetMeta<Model>(root)); Reject<InvalidOperationException>(() => AccessibilityServer.ElementSetMeta<Model>(root, null)); }).GetAwaiter().GetResult();
        Check(_disposed == 0, "The service never disposes caller models.");
    }
    internal static void Native(string backend)
    {
        Managed(); ProjectSettings.Set(ProjectSettings.ApplicationName, "Electron2D Model Probe"); ProjectSettings.Set(ProjectSettings.AccessibilitySupport, AccessibilitySupport.Always);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var window = new Window { Size = new(520, 240) }; var source = new Button("Scene source") { Size = new(220, 55), Position = new(15, 20) }; window.AddChild(source);
        var first = new Model("First"); var second = new Model("Second"); var steady = new Model("Steady");
        var status = new Label("Ready") { Size = new(470, 70), Position = new(15, 120) }; window.AddChild(status);
        RID producer = default, controller = default, original = default; WeakReference? sourceWeak = null, windowWeak = null;
        Process? client = null; Task<string>? output = null, error = null; var complete = false; var clientDone = false; var post = 0; long before = 0, idle = 0, active = 0; var measuring = false;
        void Click()
        {
            var model = AccessibilityServer.ElementGetMeta<Model>(producer);
            if (model is not null) { model.Clicks++; AccessibilityServer.UpdateSetName(producer, model.Name + " " + model.Clicks); }
        }
        void Command(int id)
        {
            switch (id)
            {
                case 1: Check(first.Clicks == 1 && second.Clicks == 0, "Native action resolves the first model."); AccessibilityServer.ElementSetMeta(producer, second); status.Text = "Replaced"; break;
                case 2: Check(first.Clicks == 1 && second.Clicks == 1, "The same native action resolves a replacement model."); AccessibilityServer.ElementSetMeta<Model>(producer, null); status.Text = "Cleared"; break;
                case 3: Check(second.Clicks == 1 && AccessibilityServer.ElementGetMeta<Model>(producer) is null, "Cleared slots do not fabricate a model."); AccessibilityServer.ElementSetMeta(producer, first); status.Text = "Restored"; break;
                case 4:
                    original = source.GetAccessibilityElement(); sourceWeak = Store(original);
                    using (var packed = new PackedScene()) { packed.Pack(source); using var copy = (Button)packed.Instantiate(); Check(!copy.GetAccessibilityElement().IsValid(), "Packed scenes do not serialize native identity or producer models."); }
                    using (var copy = (Button)source.Duplicate()) Check(!copy.GetAccessibilityElement().IsValid(), "Duplication has no process-local identity.");
                    window.RemoveChild(source); Check(!Alive(sourceWeak), "Source detach releases metadata."); window.AddChild(source); status.Text = "Reattached"; break;
                case 5: Check(source.GetAccessibilityElement() != original && AccessibilityServer.ElementGetMeta<Model>(source.GetAccessibilityElement()) is null, "A reattached source starts with an empty slot."); status.Text = "Fresh source"; break;
                case 6:
                    AccessibilityServer.UpdateAddAction(producer, AAction.Click, () => { var model = AccessibilityServer.ElementGetMeta<Model>(producer)!; model.Clicks++; AccessibilityServer.FreeElement(producer); }); status.Text = "Reentrant"; break;
                case 7: Check(!AccessibilityServer.HasElement(producer) && first.Clicks == 3, "A native callback may retire its own model element."); status.Text = "Retired"; break;
                case 99: complete = true; status.Text = "Measuring"; break;
                default: throw new InvalidOperationException("Unexpected model command");
            }
        }
        window.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingMethod() == backend && AccessibilityServer.IsSupported(), "The requested backend and actual native bridge execute.");
            var root = window.GetAccessibilityElement(); Slots(root, window.GetWindowID()); windowWeak = Store(root);
            producer = AccessibilityServer.CreateSubElement(root, Role.Button); AccessibilityServer.UpdateSetName(producer, "Model action"); AccessibilityServer.ElementSetMeta(producer, first); AccessibilityServer.UpdateAddAction(producer, AAction.Click, Click);
            controller = AccessibilityServer.CreateSubElement(root, Role.Button); AccessibilityServer.UpdateSetName(controller, "Model controller"); AccessibilityServer.UpdateAddAction(controller, AAction.Custom, (int id) => Command(id));
            foreach (var id in new[] { 1, 2, 3, 4, 5, 6, 7, 99 }) AccessibilityServer.UpdateAddCustomAction(controller, id, "command-" + id);
            client = Process.Start(Client())!; output = client.StandardOutput.ReadToEndAsync(); error = client.StandardError.ReadToEndAsync();
            window.Tree!.ProcessFrameStarted += _ =>
            {
                measuring = complete && post >= 16 && (post < 80 || post >= 96 && post < 160); if (measuring) before = GC.GetAllocatedBytesForCurrentThread();
                if (complete && post >= 80) { AccessibilityServer.ElementSetMeta(controller, (post & 1) == 0 ? first : steady); AccessibilityServer.ElementGetMeta<IModel>(controller); }
            };
            RenderingServer.FramePostDraw += AfterDraw;
        };
        void AfterDraw()
        {
            if (measuring) { var bytes = GC.GetAllocatedBytesForCurrentThread() - before; if (post < 80) idle += bytes; else active += bytes; }
            if (complete && ++post == 160) Check(idle == 0 && active == 0, $"Producer model idle/active owner-frame managed bytes {idle}/{active}.");
        }
        var started = Stopwatch.GetTimestamp();
        try { Check(Engine.Run((_, _) => { if (!clientDone && client?.HasExited == true) { Console.Write(output!.GetAwaiter().GetResult()); Console.Write(error!.GetAwaiter().GetResult()); Check(client.ExitCode == 0 && complete, "Independent native producer model client completes."); clientDone = true; } if (clientDone && post >= 160) window.Tree!.Quit(); if (Stopwatch.GetElapsedTime(started).TotalSeconds > 60) throw new TimeoutException("Native model client timeout."); }, window) == 0, "Model host closes."); }
        finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= AfterDraw; if (client is { HasExited: false }) client.Kill(true); client?.Dispose(); }
        Check(!Alive(windowWeak!), "Window/display retirement releases root metadata.");
        Failure(); Check(_disposed == 0, "No retirement or native action owns model disposal.");
        Console.WriteLine($"PASS {_checks} typed producer model/native/lifetime contracts on {backend}; 64 idle/64 active {idle}/{active} managed bytes.");
    }
    private static ProcessStartInfo Client(string? mode = null)
    { var start = new ProcessStartInfo("python3") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(System.IO.Path.GetFullPath("tests/AccessibilityClient/models.py")); if (mode is not null) start.ArgumentList.Add(mode); return start; }
    private static void Failure()
    {
        var window = new Window { Size = new(240, 100) }; Process? client = null; Task<string>? output = null, error = null; WeakReference? weak = null; var failed = false; RID id = default;
        window.Ready += _ => { id = AccessibilityServer.CreateSubElement(window.GetAccessibilityElement(), Role.Button); AccessibilityServer.UpdateSetName(id, "Failing model"); weak = Store(id); AccessibilityServer.UpdateAddAction(id, AAction.Click, () => { Check(AccessibilityServer.ElementGetMeta<Model>(id) is not null, "The failing native action resolves its model."); throw new InvalidOperationException("Injected producer model failure"); }); client = Process.Start(Client("--failure"))!; output = client.StandardOutput.ReadToEndAsync(); error = client.StandardError.ReadToEndAsync(); };
        var started = Stopwatch.GetTimestamp();
        try { Engine.Run((_, _) => { if (Stopwatch.GetElapsedTime(started).TotalSeconds > 15) throw new TimeoutException("Native model failure timeout."); }, window); }
        catch (AggregateException errors) { failed = errors.Flatten().InnerExceptions.Any(e => e is InvalidOperationException { Message: "Injected producer model failure" }); }
        finally { if (client is { HasExited: false } && !client.WaitForExit(5000)) client.Kill(true); if (output is not null) Console.Write(output.GetAwaiter().GetResult()); if (error is not null) Console.Write(error.GetAwaiter().GetResult()); Check(failed && client?.ExitCode == 0, "Native model callback retains owner/wire failure behavior."); client?.Dispose(); }
        Check(!Alive(weak!) && !AccessibilityServer.HasElement(id), "Failure cleanup releases model ownership.");
        var fresh = new Window { Size = new(120, 80) }; fresh.Ready += _ => { Check(AccessibilityServer.ElementGetMeta<Model>(fresh.GetAccessibilityElement()) is null, "Fresh display metadata starts empty."); Reject<KeyNotFoundException>(() => AccessibilityServer.ElementGetMeta<Model>(id)); fresh.Tree!.Quit(); }; Check(Engine.Run(fresh) == 0, "Native bridge reopens after callback failure.");
    }
}
