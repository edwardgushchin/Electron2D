using Electron2D;
using static MovieWriterTests;
using IOPath = System.IO.Path;

internal static class PortalFileChooserTests
{
    [System.Runtime.InteropServices.DllImport("libgio-2.0.so.0", EntryPoint = "g_bus_get_sync")]
    private static extern nint GIOBusGetSync(int type, nint cancellable, nint error);
    [System.Runtime.InteropServices.DllImport("libgio-2.0.so.0", EntryPoint = "g_dbus_connection_get_exit_on_close")]
    private static extern int GIOGetExitOnClose(nint connection);
    [System.Runtime.InteropServices.DllImport("libgio-2.0.so.0", EntryPoint = "g_dbus_connection_is_closed")]
    private static extern int GIOIsClosed(nint connection);
    [System.Runtime.InteropServices.DllImport("libgobject-2.0.so.0", EntryPoint = "g_object_unref")]
    private static extern void GObjectUnref(nint instance);

    internal static void Run(string profile)
    {
        var connection = GIOBusGetSync(2, 0, 0); Check(connection != 0, "The private test session bus exists.");
        var previous = GIOGetExitOnClose(connection);
        try
        {
            RunCore(profile, connection);
            Check(GIOIsClosed(connection) == 0 ? GIOGetExitOnClose(connection) == previous : GIOGetExitOnClose(connection) == 0, "Teardown restores the prior live connection policy without rearming a closed connection.");
        }
        finally { GObjectUnref(connection); }
    }

    private static void CheckLiveBusPolicy()
    {
        var connection = GIOBusGetSync(2, 0, 0); Check(connection != 0, "The live native host retains its session connection.");
        try { Check(GIOGetExitOnClose(connection) == 0, "The native host handles bus loss without process termination."); }
        finally { GObjectUnref(connection); }
    }

    private static void RunCore(string profile, nint sessionConnection)
    {
        var folder = Environment.GetEnvironmentVariable("ELECTRON2D_PORTAL_FOLDER") ?? throw new InvalidOperationException("A fixture folder is required.");
        if (profile == "live") { Live(folder); return; }
        if (profile is "gpu" or "compatibility") { Scene(profile, folder); return; }
        using var display = DisplayServer.Open("Portal contract", new(320, 240), hidden: true);
        CheckLiveBusPolicy();
        var version = int.Parse(Environment.GetEnvironmentVariable("ELECTRON2D_PORTAL_VERSION") ?? "4");
        Check(DisplayServer.HasFeature(DisplayServer.Feature.NativeDialogFile) == (version > 0), "FileChooser availability reflects its native version.");
        Check(DisplayServer.HasFeature(DisplayServer.Feature.NativeDialogFileMime) == (version > 0), "MIME capability reflects the executing portal.");
        Check(!DisplayServer.HasFeature(DisplayServer.Feature.NativeDialogFileExtra), "Native extra-option capability remains separate.");
        if (version == 0)
        { Reject<NotSupportedException>(() => DisplayServer.FileDialogShow("absent", folder, "", false, FileDialogMode.OpenFile, [], (_, _, _) => throw new Exception("No callback for unlaunched requests."))); Console.WriteLine("Portal unavailable passed."); return; }
        if (version == 2)
        { Reject<NotSupportedException>(() => DisplayServer.FileDialogShow("old-directory", folder, "", false, FileDialogMode.OpenDirectory, [], (_, _, _) => { })); Console.WriteLine("Portal directory version guard passed."); return; }
        var owner = Environment.CurrentManagedThreadId;
        string[] filters = [";Images;image/png,image/jpeg", "*.txt;Text;text/plain"];
        if (profile == "bus-loss")
        {
            var calls = 0;
            DisplayServer.FileDialogShow("bus-disconnect", folder, "", false, FileDialogMode.OpenFile, filters, (ok, paths, _) => { Check(!ok && paths.Count == 0, "Bus loss fails the outstanding request."); calls++; });
            Check(Pump(() => calls == 1) == 1, "Bus loss reaches the owner callback once and releases pending state.");
            Check(!DisplayServer.HasFeature(DisplayServer.Feature.NativeDialogFile), "A dead session bus no longer advertises a chooser.");
            Check(Pump(() => GIOIsClosed(sessionConnection) != 0) == 0, "The native context observes disconnection before closed-policy teardown.");
            Console.WriteLine("Portal bus loss passed."); return;
        }
        foreach (var invalid in new[] { "not-an-extension", ";Bad;*/png", ";Bad;image/", ";Bad;image/png,", "*.png;Bad;image/png;extra", "*.png,*;Bad", "*.png;bad\0name", "" })
            Reject<ArgumentException>(() => DisplayServer.FileDialogShow("invalid", folder, "", false, FileDialogMode.OpenFile, [invalid], (_, _, _) => { }));
        Reject<ArgumentException>(() => DisplayServer.FileDialogShow("bad\0title", folder, "", false, FileDialogMode.OpenFile, [], (_, _, _) => { }));
        Reject<InvalidOperationException>(() => Task.Run(() => DisplayServer.FileDialogShow("off-owner", folder, "", false, FileDialogMode.OpenFile, [], (_, _, _) => { })).GetAwaiter().GetResult());
        Reject<NotSupportedException>(() => DisplayServer.FileDialogShow("mixed", folder, "", false, FileDialogMode.OpenAny, [], (_, _, _) => { }));
        Reject<InvalidOperationException>(() => DisplayServer.FileDialogShow("launch-error", folder, "", false, FileDialogMode.OpenFile, filters, (_, _, _) => throw new Exception("No callback for a failed launch.")));
        foreach (var name in new[] { "accept", "multi", "save", "directory", "cancel", "early", "alternate", "wrong-path", "wrong-sender", "unknown-filter", "missing-filter", "error", "bad-uri", "bad-nul", "bad-escape", "wrong-count", "wrong-type", "empty", "callback-failure" })
        {
            var called = 0; var failing = name is "error" or "bad-uri" or "bad-nul" or "bad-escape" or "wrong-count" or "wrong-type" or "empty" or "callback-failure";
            var mode = name == "save" ? FileDialogMode.SaveFile : name == "directory" ? FileDialogMode.OpenDirectory : name == "multi" ? FileDialogMode.OpenFiles : FileDialogMode.OpenFile;
            DisplayServer.FileDialogShow(name, folder, name == "save" ? "новый #.txt" : "", true, mode, filters, (ok, paths, filter) =>
            {
                called++; Check(Environment.CurrentManagedThreadId == owner, "Native results run on the display owner.");
                if (name == "callback-failure") throw new FormatException("portal callback failure");
                if (failing || name is "cancel" or "wrong-sender") { Check(!ok && paths.Count == 0 && filter == -1, name + ": canceled/failed result."); return; }
                Check(ok && paths.Count == (name == "multi" ? 2 : 1), name + ": accepted copied path count.");
                Check(paths[0] == IOPath.Combine(folder, name == "directory" ? "picked-dir" : "выбранный #.txt"), name + ": decoded local path.");
                Check(filter == (name is "directory" or "unknown-filter" or "missing-filter" ? -1 : 1), name + ": exact filter identity.");
            });
            Check(called == 0, "A synchronous native response still waits for event pumping.");
            Reject<InvalidOperationException>(display.Dispose);
            var failures = Pump(() => called == 1);
            Check(called == 1 && failures == (failing ? 1 : 0), name + ": one terminal result and expected failure.");
            Console.WriteLine("Portal " + name + " passed.");
        }
        var completed = 0;
        for (var i = 0; i < 2; i++)
        {
            var shouldFail = i == 0;
            DisplayServer.FileDialogShow("parallel", folder, "", false, FileDialogMode.OpenFile, filters, (_, _, _) => { completed++; if (shouldFail) throw new FormatException("parallel callback failure"); });
        }
        Check(Pump(() => completed == 2) == 1 && completed == 2, "A failing callback does not swallow another result.");
        DisplayServer.FileDialogShow("disconnect", folder, "", false, FileDialogMode.OpenFile, filters, (ok, paths, _) => { Check(!ok && paths.Count == 0, "Portal death fails its outstanding request."); completed++; });
        Check(Pump(() => completed == 3) == 1, "Portal death finalizes pending owner state.");
        Console.WriteLine("Portal contract passed.");
    }

    private static int Pump(Func<bool> done)
    {
        var errors = 0; var deadline = Environment.TickCount64 + 8000;
        while (!done() && Environment.TickCount64 < deadline)
        {
            try { DisplayServer.ProcessEvents(); }
            catch (AggregateException failure) { Check(failure.Flatten().InnerExceptions.Count == 1, "Only the intended failure propagates."); errors++; }
            Thread.Sleep(2);
        }
        Check(done(), "A native request must finish before its deadline."); return errors;
    }

    private static void Live(string folder)
    {
        using var display = DisplayServer.Open("Electron2D portal live parent", new(320, 240));
        CheckLiveBusPolicy();
        Check(DisplayServer.HasFeature(DisplayServer.Feature.NativeDialogFileMime), "The installed FileChooser advertises its real MIME path.");
        foreach (var name in new[] { "accept", "cancel" })
        {
            var calls = 0;
            DisplayServer.FileDialogShow("Electron2D MIME chooser " + name, folder, "", false, FileDialogMode.OpenFile, [";Text;text/plain", ";Images;image/png"], (ok, paths, filter) =>
            {
                calls++;
                if (name == "cancel") Check(!ok && paths.Count == 0, "Installed chooser cancellation.");
                else Check(ok && paths.Count == 1 && paths[0] == IOPath.Combine(folder, "выбранный #.txt") && filter == 0, "Installed chooser selected file and actual MIME filter.");
            });
            Console.WriteLine("NATIVE_READY:" + name);
            var deadline = Environment.TickCount64 + 20000;
            while (calls == 0 && Environment.TickCount64 < deadline) { DisplayServer.ProcessEvents(); Thread.Sleep(5); }
            Check(calls == 1, "Installed native chooser must finish.");
            Console.WriteLine("NATIVE_DONE:" + name);
        }
    }

    private static void Scene(string backend, string folder)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var root = new Window { Size = new(320, 240), Title = "Portal scene consumer" };
        var picker = new FileDialog { Access = FileDialogAccess.FileSystem, UseNativeDialog = true, CurrentDir = folder, FileMode = FileDialogMode.OpenFile, Title = "scene" };
        picker.Filters = [";Images;image/png", ";Text;text/plain"]; root.AddChild(picker);
        var selected = 0; picker.FileSelected += path => { Check(path == IOPath.Combine(folder, "выбранный #.txt"), "The real FileDialog receives the selected native path."); selected++; root.Tree!.Quit(); };
        root.Ready += _ => { CheckLiveBusPolicy(); picker.PopupFileDialog(); Check(!picker.Visible, "Native presentation does not open the custom browser."); };
        var deadline = Environment.TickCount64 + 8000;
        var code = Engine.Run((_, _) => { if (Environment.TickCount64 > deadline) root.Tree!.Quit(91); }, root);
        Check(code == 0 && selected == 1 && picker.IsDisposed && !DisplayServer.IsAvailable, "The scene native consumer completes and releases parent and display.");
        Console.WriteLine("Portal scene " + backend + " passed.");
    }
}
