using System.Diagnostics;
using Electron2D;
using Path = System.IO.Path;

internal static partial class NativePopupWindowTests
{
    private static void Authoring(string backend)
    {
        using var root = new Window { Name = "Authored", Size = new(240, 160) };
        var menu = new PopupMenu { Name = "Commands", Transparent = backend != "gpu" }; root.AddChild(menu); menu.Owner = root;
        menu.AddItem("Saved command", 42);
        using var packed = new PackedScene(); packed.Pack(root);
        var path = Path.Combine(Path.GetTempPath(), "electron2d-native-popup-" + Guid.NewGuid().ToString("N") + ".e2dscene");
        try
        {
            ResourceSaver.Save(packed, path);
            var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true };
            if (Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(typeof(NativePopupWindowTests).Assembly.Location);
            start.Environment["ELECTRON2D_TEST_NATIVE_POPUP_SCENE"] = path; start.Environment["ELECTRON2D_TEST_NATIVE_POPUPS"] = backend;
            using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(30000)) { process.Kill(true); throw new TimeoutException("Native popup fresh process."); }
            var text = output.GetAwaiter().GetResult(); var errors = error.GetAwaiter().GetResult();
            Check(process.ExitCode == 0 && text.Contains("\"nativePopup\":true"), "Fresh public popup authoring: " + text + errors);
            Console.WriteLine(text.Trim());
        }
        finally { File.Delete(path); }
    }

    internal static void RunScene(string path, string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        using var packed = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore)!;
        var root = (Window)packed.Instantiate(); var menu = root.GetNode<PopupMenu>("Commands");
        Check(menu.ItemCount == 1 && menu.GetItemID(0) == 42 && !menu.Visible && menu.Transparent == (backend != "gpu"), "Fresh public archive retains the command menu.");
        var frames = 0;
        root.Ready += _ =>
        {
            menu.PopupOnParent(new(16, 48, 150, 32));
            RenderingServer.FramePostDraw += () =>
            {
                Check(menu.Visible && !menu.IsEmbedded() && menu.GetWindowID() > 0 && DisplayServer.WindowGetActivePopup() == menu.GetWindowID(), "The restored public scene executes native popup presentation.");
                using var image = menu.GetTexture().GetImage(); Check(image is not null && image.Width > 1 && image.Height > 1, "The restored popup returns real rendered output through its public viewport texture.");
                frames++; root.Tree!.Quit();
            };
        };
        Check(Engine.Run(root) == 0 && frames == 1, "Fresh public scene renders and retires its native popup.");
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { nativePopup = true, backend, freshProcess = true, frames }));
    }
}
