using Electron2D;
using IOPath = System.IO.Path;

internal static class ShaderIncludeTests
{
    internal static void Run()
    {
        using var source = new ShaderInclude(); var changed = 0; source.Changed += _ => changed++;
        Check(source.Code == "", "Empty initial source."); source.Code = "#define FACTOR 0.5\n"; source.Code = source.Code;
        Check(changed == 2, "Equal assignments retain Changed notification.");
        Reject<ArgumentNullException>(() => source.Code = null!); Reject<ArgumentException>(() => source.Code = "\ud800");
        Reject<ArgumentException>(() => source.Code = new string('a', 8 * 1024 * 1024 + 1)); Check(changed == 2, "Invalid edits preserve state and notification count.");
        using var copy = (ShaderInclude)source.Duplicate(); Check(copy.Code == source.Code, "Typed descriptor duplication."); copy.Code = "different"; Check(source.Code != copy.Code, "Independent source copies.");
        var folder = IOPath.Combine(IOPath.GetTempPath(), "electron2d-include-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            foreach (var extension in new[] { "hlslinc", "glslinc", "e2dres" })
            {
                var path = IOPath.Combine(folder, "factor." + extension); ResourceSaver.Save(source, path);
                Check(ResourceLoader.Exists<ShaderInclude>(path), "Source discovery.");
                using var restored = ResourceLoader.Load<ShaderInclude>(path); Check(restored.Code == source.Code, "Source/archive persistence.");
                Check(ReferenceEquals(restored, ResourceLoader.Load<ShaderInclude>(path)), "Weak cache reuse.");
                using var separate = ResourceLoader.Load<ShaderInclude>(path, ResourceLoader.CacheMode.Ignore); Check(!ReferenceEquals(separate, restored), "Independent load.");
                using var replacement = new ShaderInclude { Code = "#define FACTOR 0.2\n" }; ResourceSaver.Save(replacement, path);
                Check(ReferenceEquals(restored, ResourceLoader.Load<ShaderInclude>(path, ResourceLoader.CacheMode.Replace)) && restored.Code == replacement.Code, "Replacement preserves cache identity.");
            }
            var threaded = IOPath.Combine(folder, "threaded.hlslinc"); ResourceSaver.Save(source, threaded);
            ResourceLoader.LoadThreadedRequest<ShaderInclude>(threaded, cacheMode: ResourceLoader.CacheMode.Ignore);
            using var workerResult = ResourceLoader.LoadThreadedGet<ShaderInclude>(threaded); Check(workerResult.Code == source.Code, "Actual threaded source load.");
            var oversized = IOPath.Combine(folder, "oversized.glslinc"); using (var stream = File.Create(oversized)) stream.SetLength(8 * 1024 * 1024 + 1); Reject<InvalidDataException>(() => ResourceLoader.Load<ShaderInclude>(oversized));
            var virtualProject = ProjectSettings.ProjectRoot; var virtualUser = ProjectSettings.UserDataRoot;
            var userFolder = IOPath.Combine(folder, "user"); Directory.CreateDirectory(userFolder);
            try { ProjectSettings.ConfigurePaths(folder, userFolder); ResourceSaver.Save(source, "user://factor.glslinc"); using var virtualRead = ResourceLoader.Load<ShaderInclude>("user://factor.glslinc", ResourceLoader.CacheMode.Ignore); Check(virtualRead.Code == source.Code, "Virtual source path."); }
            finally { ProjectSettings.ConfigurePaths(virtualProject, virtualUser); }
            var failedPath = IOPath.Combine(folder, "destination.hlslinc"); Directory.CreateDirectory(failedPath);
            try { ResourceSaver.Save(source, failedPath); throw new InvalidOperationException("Expected atomic source failure."); }
            catch (AggregateException error) { Check(error.InnerExceptions.Count == 1 && error.InnerException is IOException, "Format saver reports atomic I/O failure."); }
            Check(Directory.Exists(failedPath), "Failed atomic source replacement preserves destination.");
            var malformed = IOPath.Combine(folder, "bad.hlslinc"); File.WriteAllBytes(malformed, [255]); Reject<System.Text.DecoderFallbackException>(() => ResourceLoader.Load<ShaderInclude>(malformed));
            source.Changed += _ => throw new IOException("Observer failure"); Reject<IOException>(() => source.Code = "committed"); Check(source.Code == "committed", "Observer failure follows state commitment.");
            source.Dispose(); Reject<ObjectDisposedException>(() => _ = source.Code); Reject<ObjectDisposedException>(() => source.Code = "x");
        }
        finally { Directory.Delete(folder, true); }
        Console.WriteLine("ShaderInclude edit/notification, validation, duplication, source/archive cache and lifetime passed.");
    }
    internal static void WriteSource(string path, string code)
    {
        using var source = new ShaderInclude { Code = code }; ResourceSaver.Save(source, path);
    }
    internal static void Render(string hlslPath, string glslPath, float expected)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var hlsl = Shader.CreateFromSPIRV(File.ReadAllBytes(hlslPath)); using var glsl = Shader.CreateFromSPIRV(File.ReadAllBytes(glslPath));
        using var a = new ShaderMaterial { Shader = hlsl }; using var b = new ShaderMaterial { Shader = glsl };
        var window = new Window { Size = new(64, 48) }; window.AddChild(new Picture { Name = "HLSL", X = 8, Material = a }); window.AddChild(new Picture { Name = "GLSL", X = 32, Material = b });
        var rendered = false;
        window.Ready += _ => { RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.FramePostDraw += () => { using var pixels = RenderingServer.Service!.Readback(); foreach (var x in new[] { 12, 36 }) { var p = pixels.GetPixel(x, 12); Check(Math.Abs(p.R - expected) < .025f && p.G < .025f && p.B < .025f, $"Include pixel x={x}: ({p.R},{p.G},{p.B},{p.A}), expected red={expected}."); } rendered = true; window.Tree!.Quit(); }; };
        Check(Engine.Run(window) == 0 && rendered && window.IsDisposed && !RenderingServer.IsAvailable, "Native imported material lifetime.");
        Console.WriteLine("HLSL/GLSL include factor " + expected + " native pixels passed.");
    }
    private sealed class Picture : Entity { internal float X; protected override void OnDraw() => DrawRect(new(X, 8, 16, 16), Colors.Red); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
