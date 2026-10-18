using System.Diagnostics;
using System.Runtime.InteropServices;
using Electron2D;
using static TextureArrayTests;

internal static class NumericMatrixTests
{
    private static readonly float[] M3 = [.125f, .25f, 0, .0625f, .125f, 0, .03125f, .0625f, 1];
    private static readonly float[] M4 = [.125f, 0, .0625f, 0, 0, .25f, .03125f, 0, 0, 0, .125f, 0, 0, 0, .015625f, 1];
    private static float[] Identity(int width) { var value = new float[width * width]; for (var i = 0; i < width; i++) value[i * (width + 1)] = 1; return value; }
    private static readonly float[] A3 = [.. M3, .. Identity(3)], A4 = [.. Identity(4), .. M4];
    private static byte[] Module(string name, string stage = "frag")
    {
        using var stream = typeof(NumericMatrixTests).Assembly.GetManifestResourceStream($"TestShaders.NumericMatrices{name}.{stage}.spv")!;
        using var bytes = new MemoryStream(); stream.CopyTo(bytes); return bytes.ToArray();
    }
    private static void Set(ShaderMaterial material)
    {
        material.SetShaderMatrixParameter("m3", M3); material.SetShaderMatrixParameter("m4", M4);
        material.SetShaderMatrixParameter("a3", A3); material.SetShaderMatrixParameter("a4", A4); material.SetShaderParameter("tail", .875f);
    }
    private static void Values(ShaderMaterial material)
    {
        foreach (var (name, value) in new[] { ("m3", M3), ("m4", M4), ("a3", A3), ("a4", A4) })
            Check(material.GetShaderMatrixParameter(name).SequenceEqual(value), "Matrix value: " + name);
        Check(material.GetShaderParameter<float>("tail") == .875f, "Adjacent scalar remains intact.");
    }
    private static byte[] Widen(byte[] code)
    {
        var bytes = (byte[])code.Clone(); var words = MemoryMarshal.Cast<byte, uint>(bytes.AsSpan());
        for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
        {
            if ((words[at] & 65535) == 72 && (words[at] >> 16) == 5 && words[at + 3] is 7 or 35) words[at + 4] *= 2;
            if ((words[at] & 65535) == 71 && (words[at] >> 16) == 4 && words[at + 2] == 6) words[at + 3] *= 2;
        }
        return bytes;
    }
    private static void Managed(string language)
    {
        using var shader = Shader.CreateFromSPIRV(Module(language)); using var material = new ShaderMaterial { Shader = shader };
        var descriptors = shader.GetShaderUniformList(); Check(descriptors.Count == 5, "Numeric matrices retain one typed descriptor per field.");
        foreach (var (name, width, count) in new[] { ("m3", 3, 1), ("m4", 4, 1), ("a3", 3, 2), ("a4", 4, 2) })
        {
            var descriptor = (PropertyDescriptor<ShaderMaterial, float[]>)descriptors.Single(p => p.Name == name);
            var expected = Enumerable.Range(0, count).SelectMany(_ => Identity(width)).ToArray();
            Check(material.GetShaderMatrixParameter(name).SequenceEqual(expected), "Identity per matrix element.");
            Check(descriptor.TryGetRevertValue(material, out var initial) && initial.SequenceEqual(expected), "Descriptor identity default.");
            initial[0] = -10; Check(descriptor.TryGetRevertValue(material, out initial) && initial[0] == 1, "Revert arrays are independent.");
        }
        Set(material); Values(material);
        var copy = material.GetShaderMatrixParameter("m3"); copy[0] = -1; Values(material);
        Reject<ArgumentException>(() => material.SetShaderMatrixParameter("m3", M4));
        Reject<ArgumentException>(() => material.SetShaderMatrixParameter("a3", M3));
        Reject<ArgumentException>(() => material.SetShaderMatrixParameter("tail", M3));
        Reject<ArgumentException>(() => material.SetShaderParameter<float>("a3", A3));
        Reject<ArgumentException>(() => material.GetShaderParameterArray<float>("m3"));
        Reject<ArgumentException>(() => material.GetShaderMatrixParameter("TIME"));
        var destination = Enumerable.Repeat(-2f, 17).ToArray(); Reject<ArgumentException>(() => material.CopyShaderMatrixParameter("m4", destination));
        Check(destination.All(v => v == -2), "Bad destination remains unchanged.");
        foreach (var (name, source) in new[] { ("m3", M3), ("m4", M4), ("a3", A3), ("a4", A4) })
            for (var i = 0; i < source.Length; i++)
            {
                var invalid = (float[])source.Clone(); invalid[i] = i % 2 == 0 ? float.NaN : float.NegativeInfinity;
                Reject<ArgumentException>(() => material.SetShaderMatrixParameter(name, invalid)); Values(material);
            }
        Action<Resource> failing = _ => throw new ApplicationException("fixture"); material.Changed += failing;
        Reject<ApplicationException>(() => material.SetShaderMatrixParameter("m3", Identity(3))); material.Changed -= failing;
        Check(material.GetShaderMatrixParameter("m3")[0] == 1, "Observer failure retains committed value."); Set(material);
        using var duplicate = (ShaderMaterial)material.Duplicate(true); using var duplicateShader = duplicate.Shader!;
        using var shallow = new ShaderMaterial(); shallow.CopyFromResource(material); Values(duplicate); Values(shallow);
        var old = (PropertyDescriptor<ShaderMaterial, float[]>)descriptors.Single(p => p.Name == "m3"); old.Revert(material);
        Values(duplicate); old.SetValue(material, M3);
        foreach (var code in new[] { Module("Reordered" + language), Widen(Module("Reordered" + language)), Module(language) })
        { shader.SetSPIRV(code); Values(material); old.SetValue(material, M3); }
        shader.SetSPIRV(Module("Changed" + language));
        Check(material.GetShaderMatrixParameter("m3").SequenceEqual(Identity(4)) && material.GetShaderMatrixParameter("a3").SequenceEqual(Identity(3)), "Width and length changes reset incompatible values, including length-one arrays.");
        material.SetShaderMatrixParameter("a3", M3); Check(material.GetShaderMatrixParameter("a3").SequenceEqual(M3), "Length-one matrix arrays use one complete flat value.");
        Check(material.GetShaderMatrixParameter("m4").SequenceEqual(M4) && material.GetShaderMatrixParameter("a4").SequenceEqual(A4), "Compatible siblings survive shape changes.");
        shader.SetSPIRV(Module(language)); Set(material);
        var file = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "electron2d-matrices-" + Guid.NewGuid().ToString("N") + ".e2dres");
        try
        {
            ResourceSaver.Save(material, file);
            var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true };
            start.ArgumentList.Add(typeof(NumericMatrixTests).Assembly.Location); start.Environment["ELECTRON2D_TEST_NUMERIC_MATRICES"] = "archive"; start.Environment["ELECTRON2D_MATRIX_ARCHIVE"] = file;
            using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEnd(); var error = child.StandardError.ReadToEnd(); child.WaitForExit();
            Check(child.ExitCode == 0, "Fresh-process archive: " + output + error);
        }
        finally { File.Delete(file); }
        var result = new float[A4.Length];
        for (var pass = 0; pass < 2; pass++)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 2000; i++) { Set(material); material.CopyShaderMatrixParameter("a4", result); }
            if (pass == 1) Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared matrix writes/copies allocate zero bytes.");
        }
        material.Dispose(); Reject<ObjectDisposedException>(() => material.GetShaderMatrixParameter("m3"));
        Console.WriteLine("Numeric matrices managed/atomic/descriptors/reload/graphs/archive/2000 zero-byte iterations passed: " + language);
    }
    private sealed class Patch : Entity
    {
        internal int Draws;
        protected override void OnDraw() { Draws++; DrawRect(new(0, 0, 24, 16), Colors.White); }
    }
    private static bool Near(Color a, Color b) => MathF.Abs(a.R - b.R) < .01f && MathF.Abs(a.G - b.G) < .01f && MathF.Abs(a.B - b.B) < .01f && MathF.Abs(a.A - b.A) < .01f;
    private static void Native(string profile)
    {
        var language = profile.Contains("hlsl", StringComparison.Ordinal) ? "HLSL" : "GLSL"; var paired = profile.Contains("vertex", StringComparison.Ordinal);
        var fallback = profile == "compatibility"; ProjectSettings.Set(ProjectSettings.RenderingMethod, fallback ? "compatibility" : "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var shader = paired ? Shader.CreateFromSPIRV(Module(language, "vert"), Module(language)) : Shader.CreateFromSPIRV(Module(language));
        using var material = new ShaderMaterial { Shader = shader }; Set(material);
        using var root = new Window { Size = new(80, 32) }; var left = new Patch { Position = new(2, 4), Material = material }; var right = new Patch { Position = new(42, 4) }; root.AddChild(left); root.AddChild(right);
        if (fallback) { Reject<NotSupportedException>(() => Engine.Run(root)); Check(root.IsDisposed, "Fallback failure disposes the scene."); return; }
        RID ownedShader = default, ownedMaterial = default; var frames = 0; var warmLeft = 0; var warmRight = 0; long start = 0, active = 0, idle = 0; var result = new float[A4.Length];
        root.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); ownedShader = RenderingServer.ShaderCreate();
            if (paired) RenderingServer.ShaderSetSPIRV(ownedShader, Module(language, "vert"), Module(language)); else RenderingServer.ShaderSetSPIRV(ownedShader, Module(language));
            ownedMaterial = RenderingServer.MaterialCreate(); RenderingServer.MaterialSetShader(ownedMaterial, ownedShader);
            foreach (var (name, values) in new[] { ("m3", M3), ("m4", M4), ("a3", A3), ("a4", A4) }) RenderingServer.MaterialSetMatrixParam(ownedMaterial, name, values);
            RenderingServer.MaterialSetParam(ownedMaterial, "tail", .875f); RenderingServer.CanvasItemSetMaterial(right.GetCanvasItem(), ownedMaterial);
            Check(RenderingServer.MaterialGetMatrixParam(material.GetRID(), "m3").SequenceEqual(M3), "Borrowed identity reads.");
            Reject<InvalidOperationException>(() => RenderingServer.MaterialSetMatrixParam(material.GetRID(), "m3", M3));
            Reject<ArgumentException>(() => RenderingServer.MaterialSetMatrixParam(ownedMaterial, "a4", [float.NaN]));
        };
        root.Ready += _ => root.Tree!.ProcessFrameStarted += tree =>
        {
            start = GC.GetAllocatedBytesForCurrentThread();
            if (frames is >= 24 and < 88)
            { Set(material); RenderingServer.MaterialSetMatrixParam(ownedMaterial, "a4", A4); RenderingServer.MaterialGetMatrixParam(ownedMaterial, "a4", result); }
        };
        root.Ready += _ => RenderingServer.FramePostDraw += () =>
        {
            var bytes = GC.GetAllocatedBytesForCurrentThread() - start;
            if (frames == 24) { warmLeft = left.Draws; warmRight = right.Draws; }
            if (frames is >= 24 and < 88) active += bytes; if (frames is >= 88 and < 152) idle += bytes;
            if (frames is 0 or 1 or 2 or 3 or 4 or 24 or 151)
            {
                using var image = RenderingServer.Service!.Readback();
                var expected = new Color(frames == 4 ? .46875f : .34375f, .6875f, .5625f, 1);
                Check(Near(image.GetPixel(12, 8), expected) && Near(image.GetPixel(52, 8), new(.34375f, .6875f, .5625f, 1)), "Numeric matrix multiplication pixels in resources and owned RIDs.");
                if (frames == 0) { if (paired) shader.SetSPIRV(Module(language, "vert"), Module("Reordered" + language)); else shader.SetSPIRV(Module("Reordered" + language)); }
                if (frames == 1) { if (paired) shader.SetSPIRV(Module(language, "vert"), Widen(Module("Reordered" + language))); else shader.SetSPIRV(Widen(Module("Reordered" + language))); }
                if (frames == 2) { if (paired) shader.SetSPIRV(Module(language, "vert"), Module(language)); else shader.SetSPIRV(Module(language)); }
                if (frames == 3) { var changed = (float[])M3.Clone(); var array = (float[])A3.Clone(); changed[0] = array[0] = .25f; material.SetShaderMatrixParameter("m3", changed); material.SetShaderMatrixParameter("a3", array); }
                if (frames == 4) Set(material);
            }
            if (++frames == 152)
            {
                Check(active == 0 && idle == 0 && left.Draws == warmLeft && right.Draws == warmRight, $"64 active/64 idle frames: bytes={active}/{idle}, draws={left.Draws}/{right.Draws}, warm={warmLeft}/{warmRight}.");
                RenderingServer.FreeRID(ownedMaterial); RenderingServer.FreeRID(ownedShader); root.Tree!.Quit();
            }
        };
        Engine.Run(root); Check(root.IsDisposed, "Native shutdown disposes the scene.");
        Console.WriteLine($"Numeric matrix native {profile}: {frames} frames, active={active}, idle={idle} bytes.");
    }
    internal static void Run(string profile)
    {
        if (profile == "archive") { using var material = ResourceLoader.Load<ShaderMaterial>(Environment.GetEnvironmentVariable("ELECTRON2D_MATRIX_ARCHIVE")!, cacheMode: ResourceLoader.CacheMode.Ignore)!; Values(material); material.Shader!.Dispose(); return; }
        if (profile == "managed") { Managed("GLSL"); Managed("HLSL"); return; }
        Native(profile);
    }
}
