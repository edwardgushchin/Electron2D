using Electron2D;
using Path = System.IO.Path;

internal static partial class NativeChildWindowTests
{
    internal static void RunManaged()
    {
        var host = new SubViewport { Size = new(160, 120), GUIEmbedSubwindows = true };
        var window = new Window { Visible = false, Size = new(64, 64), Borderless = true, TransientToFocused = true };
        host.AddChild(window); using var tree = new SceneTree(host);
        Check(!window.ForceNative && window.TransientToFocused && window.GetWindowID() == -1, "Managed host policies and absent native identity.");
        window.Show(); window.GrabFocus(); Check(Window.GetFocusedWindow() == window, "Embedded focus publishes the same public identity.");
        Reject<InvalidOperationException>(() => window.ForceNative = true);
        window.SetUnparentWhenInvisible(true); window.Hide();
        Check(window.Parent is null && window.Tree is null && !window.IsDisposed && Window.GetFocusedWindow() is null, "Hide unparents without disposing and retires focus.");
        window.SetUnparentWhenInvisible(false); window.ForceNative = true; host.AddChild(window);
        Reject<InvalidOperationException>(window.Show); Check(!window.Visible && window.GetWindowID() == -1, "An absent native host rejects before visible state commits.");
        var captured = host.BeginSceneCapture();
        try { Reject<InvalidOperationException>(() => window.ForceNative = false); Reject<InvalidOperationException>(() => window.TransientToFocused = false); Reject<InvalidOperationException>(() => window.SetUnparentWhenInvisible(true)); }
        finally { Node.EndSceneCapture(captured); }
        Task.Run(() => Reject<InvalidOperationException>(() => window.ForceNative = false)).GetAwaiter().GetResult();
        window.Owner = host;
        using var packed = new PackedScene(); packed.Pack(host);
        var path = Path.Combine(Path.GetTempPath(), "electron2d-native-window-" + Guid.NewGuid().ToString("N") + ".e2dscene");
        try
        {
            ResourceSaver.Save(packed, path);
            var start = new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true };
            if (Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(typeof(NativeChildWindowTests).Assembly.Location);
            start.Environment.Remove("ELECTRON2D_TEST_NATIVE_WINDOWS"); start.ArgumentList.Add("--native-window-archive"); start.ArgumentList.Add(path);
            using var process = System.Diagnostics.Process.Start(start)!;
            var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(30000)) { process.Kill(true); throw new TimeoutException("Native window archive child."); }
            Check(process.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh native-window policy passed"), "Fresh native-window policy: " + error.GetAwaiter().GetResult());
        }
        finally { File.Delete(path); }
        Console.WriteLine("Native-window policy storage, owner/capture guards, absent host, embedded focus and automatic detachment passed.");
    }

    internal static void RunArchive(string path)
    {
        using var packed = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore)!;
        var host = (SubViewport)packed.Instantiate(); using var tree = new SceneTree(host);
        var window = host.GetChildren().OfType<Window>().Single();
        Check(window.ForceNative && window.TransientToFocused && !window.Visible && window.GetWindowID() == -1, "Fresh scene retains both independent host policies.");
        window.ForceNative = false; window.Show(); Check(window.IsEmbedded() && window.Visible, "Restored hidden host policy switches to executable embedded presentation.");
        Console.WriteLine("Fresh native-window policy passed");
    }
}
