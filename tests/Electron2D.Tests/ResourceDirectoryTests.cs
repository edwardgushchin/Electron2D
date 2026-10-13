using System.Diagnostics;
using Electron2D;
using IOPath = System.IO.Path;

internal static class ResourceDirectoryTests
{
    internal static void Run()
    {
        var folder = IOPath.Combine(IOPath.GetTempPath(), "e2d-resource-directory-" + Guid.NewGuid().ToString("N"));
        var roots = (ProjectSettings.ProjectRoot, ProjectSettings.UserDataRoot);
        Directory.CreateDirectory(folder);
        var user = folder + "-user"; Directory.CreateDirectory(user);
        try
        {
            ProjectSettings.ConfigurePaths(folder, user);
            Directory.CreateDirectory(IOPath.Combine(folder, "nested"));
            Directory.CreateDirectory(IOPath.Combine(folder, ".hidden"));
            using var root = new Node { Name = "Discovered" };
            using var scene = new PackedScene(); scene.Pack(root);
            ResourceSaver.Save(scene, IOPath.Combine(folder, "scene.e2dscene"));
            using var curve = new Curve(); curve.AddPoint(new(0, 0)); curve.AddPoint(new(1, 1));
            ResourceSaver.Save(curve, IOPath.Combine(folder, "curve.e2dres"));
            File.WriteAllText(IOPath.Combine(folder, "picture.PNG"), "invalid pixels");
            File.WriteAllText(IOPath.Combine(folder, "ignored.txt"), "text");
            File.WriteAllText(IOPath.Combine(folder, ".hidden.e2dres"), "hidden");
            File.WriteAllText(IOPath.Combine(folder, "picture.PNG.uid"), "sidecar");
            File.WriteAllText(IOPath.Combine(folder, "absent.png.import"), "unsupported remap");
            var expected = new[] { "curve.e2dres", "nested/", "picture.PNG", "scene.e2dscene" };
            Check(ResourceLoader.ListDirectory(folder).SequenceEqual(expected), "Resource/subdirectory recognition, sorting and hidden/sidecar policy.");
            Check(ResourceLoader.ListDirectory("res://").SequenceEqual(expected), "Project root preserves listed leaf names.");
            File.WriteAllText(IOPath.Combine(user, "saved.e2dres"), "recognized data");
            Check(ResourceLoader.ListDirectory("user://").SequenceEqual(new[] { "saved.e2dres" }), "User root is independent.");
            var directoryID = ResourceUID.CreateID(); ResourceUID.AddID(directoryID, folder);
            try { Check(ResourceLoader.ListDirectory(ResourceUID.IDToText(directoryID)).SequenceEqual(expected), "Registered UID directory resolves before recognition."); }
            finally { ResourceUID.RemoveID(directoryID); }
            var copied = ResourceLoader.ListDirectory(folder); copied[0] = "changed";
            Check(ResourceLoader.ListDirectory(folder).SequenceEqual(expected), "Results are caller owned.");
            using var cached = ResourceLoader.Load<Curve>(IOPath.Combine(folder, "curve.e2dres"));
            File.Delete(IOPath.Combine(folder, "curve.e2dres"));
            Check(ResourceLoader.Exists<Curve>(IOPath.Combine(folder, "curve.e2dres")) && !ResourceLoader.ListDirectory(folder).Contains("curve.e2dres"), "Weak cache does not add missing disk entries.");
            ResourceSaver.Save(curve, IOPath.Combine(folder, "curve.e2dres"));
            using var plugin = new Loader(); ResourceLoader.AddResourceFormatLoader(plugin);
            File.WriteAllText(IOPath.Combine(folder, "special.accepted"), "7");
            File.WriteAllText(IOPath.Combine(folder, "other.accepted"), "8");
            Check(ResourceLoader.ListDirectory(folder).Contains("special.accepted") && !ResourceLoader.ListDirectory(folder).Contains("other.accepted"), "Custom RecognizePath decides support, not extension census alone.");
            using (var resource = ResourceLoader.Load<Curve>(IOPath.Combine(folder, "special.accepted"))) Check(resource.PointCount == 2, "A discovered plugin file loads through the same extension.");
            plugin.VirtualOnly = true;
            Check(ResourceLoader.ListDirectory("res://").Contains("special.accepted") && !ResourceLoader.ListDirectory(folder).Contains("special.accepted"), "Format recognition retains virtual caller path identity.");
            plugin.VirtualOnly = false; plugin.RemoveOnRecognize = true;
            Check(ResourceLoader.ListDirectory(folder).Contains("special.accepted") && !ResourceLoader.ListDirectory(folder).Contains("special.accepted"), "Registration snapshot survives removal during callback and changes the next listing.");
            ResourceLoader.AddResourceFormatLoader(plugin); plugin.RemoveOnRecognize = false;
            ResourceLoader.RemoveResourceFormatLoader(plugin);
            Check(!ResourceLoader.ListDirectory(folder).Contains("special.accepted"), "Removed registration stops discovery.");
            ResourceLoader.AddResourceFormatLoader(plugin); plugin.Fail = true;
            Throws<InvalidDataException>(() => ResourceLoader.ListDirectory(folder)); plugin.Fail = false; plugin.Dispose();
            Check(ResourceLoader.ListDirectory(folder).SequenceEqual(expected), "Disposed registrations are omitted."); ResourceLoader.RemoveResourceFormatLoader(plugin);
            Throws<DirectoryNotFoundException>(() => ResourceLoader.ListDirectory(IOPath.Combine(folder, "missing")));
            Throws<UnauthorizedAccessException>(() => ResourceLoader.ListDirectory("res://../escape"));
            Throws<NotSupportedException>(() => ResourceLoader.ListDirectory("other://directory"));
            Throws<ArgumentNullException>(() => ResourceLoader.ListDirectory(null!));
            Throws<ArgumentException>(() => ResourceLoader.ListDirectory(""));
            FreshProcess(folder);
        }
        finally { ProjectSettings.ConfigurePaths(roots.ProjectRoot, roots.UserDataRoot); Directory.Delete(folder, true); Directory.Delete(user, true); }
        Console.WriteLine("Resource directory recognition, path/lifetime/error policy and fresh-process discovery/load passed.");
    }
    internal static void RunChild(string folder)
    {
        var files = ResourceLoader.ListDirectory(folder);
        foreach (var name in files.Where(name => name.EndsWith(".e2dres", StringComparison.Ordinal)))
        { using var curve = ResourceLoader.Load<Curve>(IOPath.Combine(folder, name), ResourceLoader.CacheMode.Ignore); Check(curve.PointCount == 2, "Fresh curve data."); }
        using var scene = ResourceLoader.Load<PackedScene>(IOPath.Combine(folder, files.Single(name => name.EndsWith(".e2dscene", StringComparison.Ordinal))), ResourceLoader.CacheMode.Ignore);
        using var root = scene.Instantiate(); using var tree = new SceneTree(root); Check(root.Name == "Discovered" && root.IsInsideTree, "Fresh discovered scene executes.");
        Console.WriteLine("Fresh resource directory load passed.");
    }
    private static void FreshProcess(string folder)
    {
        var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true };
        if (IOPath.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(typeof(ResourceDirectoryTests).Assembly.Location);
        start.Environment["ELECTRON2D_TEST_RESOURCE_DIRECTORY"] = "0"; start.Environment["ELECTRON2D_TEST_RESOURCE_DIRECTORY_CHILD"] = folder;
        using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(15000)) { process.Kill(true); throw new TimeoutException("Resource directory child did not complete."); }
        Check(process.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh resource directory load passed."), error.GetAwaiter().GetResult());
    }
    private sealed class Loader : ResourceFormatLoader
    {
        internal bool Fail, VirtualOnly, RemoveOnRecognize;
        public override string[] GetRecognizedExtensions() => ["accepted"];
        public override Type? GetResourceType(string path) => typeof(Curve);
        public override bool HandlesType(Type type) => type.IsAssignableFrom(typeof(Curve));
        public override bool RecognizePath(string path, Type? type = null)
        { if (Fail) throw new InvalidDataException("recognition fixture"); if (RemoveOnRecognize) ResourceLoader.RemoveResourceFormatLoader(this); return (!VirtualOnly || path.StartsWith("res://", StringComparison.Ordinal)) && IOPath.GetFileName(path) == "special.accepted" && (type is null || HandlesType(type)); }
        public override Resource Load(string path, string originalPath, bool useSubThreads, ResourceLoader.CacheMode cacheMode)
        { var curve = new Curve(); curve.AddPoint(new(0, 0)); curve.AddPoint(new(1, 1)); return curve; }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
