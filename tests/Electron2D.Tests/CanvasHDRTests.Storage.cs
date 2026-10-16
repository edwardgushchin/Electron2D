using Electron2D;
using static TextureArrayTests;

internal static partial class CanvasHDRTests
{
    internal static void FileStorage()
    {
        var folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "electron-hdr-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            using var view = new SubViewport { UseHDR2D = true }; using var scene = new PackedScene(); scene.Pack(view);
            ResourceSaver.Save(scene, System.IO.Path.Combine(folder, "viewport.e2dscene"));
            using (var settings = new ProjectSettingsRegistry(folder))
            { settings.Set(ProjectSettings.HDR2D, false); settings.SetFeatureOverride(ProjectSettings.HDR2D, "dotnet", true); settings.Save(); }
            var info = new System.Diagnostics.ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true };
            info.ArgumentList.Add(typeof(CanvasHDRTests).Assembly.Location); info.ArgumentList.Add("--hdr-storage"); info.ArgumentList.Add(folder);
            using var child = System.Diagnostics.Process.Start(info)!; var stdout = child.StandardOutput.ReadToEnd(); var stderr = child.StandardError.ReadToEnd();
            child.WaitForExit(); Check(child.ExitCode == 0, $"Fresh HDR scene/project reader: {stdout} {stderr}");
        }
        finally { Directory.Delete(folder, recursive: true); }
    }
    internal static void ReadStorage(string folder)
    {
        using var scene = ResourceLoader.Load<PackedScene>(System.IO.Path.Combine(folder, "viewport.e2dscene"), ResourceLoader.CacheMode.Ignore);
        using var view = (SubViewport)scene.Instantiate(); Check(view.UseHDR2D && view.GetTexture().PixelFormat == Image.Format.Rgbah, "Fresh file scene restores typed HDR.");
        using var settings = new ProjectSettingsRegistry(folder); settings.Load();
        Check(!settings.Get(ProjectSettings.HDR2D) && settings.GetWithOverride(ProjectSettings.HDR2D), "Fresh typed project restores base and feature HDR values.");
        Console.WriteLine("Fresh HDR scene and project feature storage passed.");
    }
    private sealed class HDRGuard(Window window) : Entity
    {
        protected override void OnDraw()
        {
            var authored = window.UseHDR2D; window.UseHDR2D = authored;
            Check(window.UseHDR2D == authored, "Initial scene drawing may republish authoring before target recapture.");
            Reject<InvalidOperationException>(() => RenderingServer.ViewportSetUseHDR2D(window.GetViewportRID(), false));
        }
    }
}
