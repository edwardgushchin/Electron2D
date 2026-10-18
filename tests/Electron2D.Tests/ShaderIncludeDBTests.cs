using Electron2D;

internal static class ShaderIncludeDBTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void RejectNull(Action action) { try { action(); } catch (ArgumentNullException) { return; } throw new InvalidOperationException("Null include name accepted."); }
    internal static void RunManaged()
    {
        Check(!RenderingServer.IsAvailable, "Library lookup works before a renderer exists.");
        var names = ShaderIncludeDB.ListBuiltInIncludeFiles();
        Check(names.SequenceEqual(new[] { "electron2d/color.glslinc", "electron2d/color.hlslinc", "electron2d/particles.glslinc", "electron2d/particles.hlslinc" }), "Portable library names are complete and ordinal.");
        foreach (var name in names)
        {
            var code = ShaderIncludeDB.GetBuiltInIncludeFile(name);
            Check(ShaderIncludeDB.HasBuiltInIncludeFile(name) && (name.Contains("/color.", StringComparison.Ordinal) ? code.Contains("Electron2D_SRGBToLinear", StringComparison.Ordinal) && code.Contains("Electron2D_LinearToSRGB", StringComparison.Ordinal) : code.Contains("Electron2D_EmitSubparticle", StringComparison.Ordinal) && code.Contains("ParticleFrame", StringComparison.Ordinal)), "Discovered files contain the actual built-in functions.");
            using var resource = new ShaderInclude { Code = code }; Check(resource.Code == code, "Library fragments are ordinary portable include source.");
        }
        names[0] = "modified"; Check(ShaderIncludeDB.ListBuiltInIncludeFiles()[0] == "electron2d/color.glslinc", "Discovery results do not expose retained storage.");
        Check(!ShaderIncludeDB.HasBuiltInIncludeFile("") && !ShaderIncludeDB.HasBuiltInIncludeFile("Electron2D/color.hlslinc") && ShaderIncludeDB.GetBuiltInIncludeFile("absent") == "" && ShaderIncludeDB.GetBuiltInIncludeFile("../electron2d/color.hlslinc") == "", "Missing and differently cased names remain literal lookups.");
        RejectNull(() => ShaderIncludeDB.HasBuiltInIncludeFile(null!)); RejectNull(() => ShaderIncludeDB.GetBuiltInIncludeFile(null!));
        Task.Run(() => Check(ShaderIncludeDB.HasBuiltInIncludeFile("electron2d/color.hlslinc"), "Immutable lookup supports worker threads.")).GetAwaiter().GetResult();
        for (var i = 0; i < 1024; i++) _ = ShaderIncludeDB.GetBuiltInIncludeFile("electron2d/color.hlslinc");
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1024; i++) Check(ShaderIncludeDB.HasBuiltInIncludeFile("electron2d/color.hlslinc") && ShaderIncludeDB.GetBuiltInIncludeFile("electron2d/color.hlslinc").Length != 0, "Prepared library lookup.");
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "1024 prepared source/existence lookups allocate zero owner bytes.");
        Console.WriteLine("Built-in include discovery, source, isolation, worker access and 1024 lookups=0 B passed.");
    }
    internal static void RunNative(string directory)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var glsl = Shader.CreateFromSPIRV(File.ReadAllBytes(System.IO.Path.Combine(directory, "GLSL.frag.spv")));
        using var hlsl = Shader.CreateFromSPIRV(File.ReadAllBytes(System.IO.Path.Combine(directory, "HLSL.frag.spv")));
        using var first = new ShaderMaterial { Shader = glsl }; using var second = new ShaderMaterial { Shader = hlsl };
        var window = new Window { Size = new(48, 24) }; window.AddChild(new Box { Position = new(4, 4), Material = first }); window.AddChild(new Box { Position = new(28, 4), Material = second }); var frames = 0;
        window.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingMethod() == "gpu", "Effective programmable backend."); RenderingServer.SetDefaultClearColor(Colors.Black);
            SetValues(new Color(-.2f, .5f, 1.5f, .375f));
            RenderingServer.FramePostDraw += () =>
            {
                using var image = window.GetTexture().GetImage()!;
                foreach (var x in new[] { 8, 32 }) { var color = image.GetPixel(x, 8); Check(color.G > .99f && color.R < .01f && color.B < .01f, "Built-in scalar/RGB/RGBA conversions preserve negative/HDR values, thresholds and alpha in both languages: " + color); }
                if (++frames == 3) window.Tree!.Quit();
                else SetValues(frames == 1 ? new Color(.04045f, .0031308f, 0, .75f) : new Color(1, .04046f, .0031309f, 0));
            };
        };
        try { Check(Engine.Run(window) == 0 && frames == 3, "Library shader frame lifecycle."); }
        finally { if (!window.IsDisposed) window.Dispose(); }
        Console.WriteLine("Built-in include HLSL/GLSL native math and alpha pixels passed.");
        void SetValues(Color value)
        {
            foreach (var material in new[] { first, second })
            {
                material.SetShaderParameter("value", value); material.SetShaderParameter("expectedDecode", value.SRGBToLinear()); material.SetShaderParameter("expectedEncode", value.LinearToSRGB());
            }
        }
    }
    private sealed class Box : Entity { protected override void OnDraw() => DrawRect(new Rect2(0, 0, 16, 16), Colors.White); }
}
