using System.Diagnostics;
using System.Runtime.InteropServices;
using Electron2D;
using static TextureArrayTests;

internal static class CanvasVertexProgramTests
{
    internal static void RunManaged()
    {
        var shared = new ShaderUniform("gain", typeof(float), 0, 16, 4, 0, 4, false, 0, false, 0);
        var frame = new ShaderUniform("size", typeof(Vector2), 0, 0, 8, 0, 8, false, 0, false, 0);
        var vertexShared = shared with { Buffer = 1, Offset = 4 };
        var fragment = new ShaderProgram([], [32], new() { [shared.Name] = shared }, [new("map", 0)]);
        var vertex = new ShaderProgram([], [16, 16], new() { [frame.Name] = frame, [shared.Name] = vertexShared }, [new("map", 0), new("vertexMap", 1)]);
        var program = ShaderProgram.WithVertex(fragment, vertex);
        Check(program.Uniforms.Count == 1 && program.Descriptors.Count == 3 && program.Vertex == vertex && program.Fragment == fragment, "Shared stage parameters have one logical descriptor.");
        Check(program.FragmentBufferCount == 1 && program.FragmentTextureCount == 1 && program.BufferSizes.SequenceEqual(new[] { 32, 16 }), "Frame storage remains renderer-owned and stage buffer ranges stay distinct.");
        Check(program.VertexTextureIndices.SequenceEqual(new[] { 0, 1 }), "Stage texture slots map to shared logical resources.");
        var state = new MaterialState(new object(), program, null);
        var expected = .375f; MemoryMarshal.Write(state.Buffers[0].AsSpan(16), in expected); state.SynchronizeSharedUniforms();
        Check(MemoryMarshal.Read<float>(state.Buffers[1].AsSpan(4)) == expected, "A shared value reaches a different stage binding and offset.");
        var previous = new MaterialState(new object(), fragment, null); previous.UniformOverrides.Add(shared.Name); MemoryMarshal.Write(previous.Buffers[0].AsSpan(16), in expected);
        var migrated = new MaterialState(new object(), program, previous); migrated.SynchronizeSharedUniforms();
        Check(MemoryMarshal.Read<float>(migrated.Buffers[1].AsSpan(4)) == expected, "Reload from fragment-only state retains shared values.");
        var badFrame = new ShaderProgram([], [16], new() { [frame.Name] = frame }, timeUniform: shared with { Name = "TIME", Offset = 8 });
        Reject<NotSupportedException>(() => ShaderProgram.WithVertex(fragment, badFrame));
        var wrong = new ShaderProgram([], [16, 16], new() { [frame.Name] = frame, [shared.Name] = vertexShared with { Type = typeof(int) } });
        Reject<ArgumentException>(() => ShaderProgram.WithVertex(fragment, wrong));
        var conflict = new ShaderProgram([], [16], new() { [frame.Name] = frame }, [new("gain", 0)]);
        Reject<ArgumentException>(() => ShaderProgram.WithVertex(fragment, conflict));
        var shape = new ShaderProgram([], [16], new() { [frame.Name] = frame }, [new("map", 0, IsArray: true)]);
        Reject<ArgumentException>(() => ShaderProgram.WithVertex(fragment, shape));
        for (var i = 0; i < 8; i++) state.SynchronizeSharedUniforms();
        var bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 1024; i++) state.SynchronizeSharedUniforms();
        Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "Prepared shared-stage propagation allocates no managed bytes.");
        var vertexCode = Module("CanvasUserVertexGLSL.vert.spv"); var fragmentCode = Module("CanvasUserVertexGLSL.frag.spv");
        using var shader = Shader.CreateFromSPIRV(vertexCode, fragmentCode);
        Check(shader.GetShaderUniformList().Count == 3 && shader.GetSPIRV(RenderingDevice.ShaderStage.Vertex).SequenceEqual(vertexCode), "Real GLSL stages expose one shared parameter namespace and copied bytecode.");
        if (Environment.GetEnvironmentVariable("ELECTRON2D_VERTEX_DUMP") is { } dumpFolder)
        {
            Directory.CreateDirectory(dumpFolder);
            for (var i = 0; i < 4; i++) File.WriteAllBytes(System.IO.Path.Combine(dumpFolder, $"vertex-{i}.spv"), shader.GetProgram().VertexCode((i & 1) != 0, (i & 2) != 0)!);
        }
        using var material = new ShaderMaterial { Shader = shader }; material.SetShaderParameter("gain", .5f); material.SetShaderParameter("offset", new Vector2(16, 0));
        Check(material.GetShaderParameter<float>("gain") == .5f && material.GetShaderParameter<Vector2>("offset") == new Vector2(16, 0), "Vertex-only and shared public parameters execute.");
        var changed = 0; shader.Changed += _ => changed++;
        Reject<ArgumentException>(() => shader.SetSPIRV(vertexCode, new byte[20]));
        Check(changed == 0 && shader.GetSPIRV(RenderingDevice.ShaderStage.Vertex).SequenceEqual(vertexCode), "Failed replacement preserves both stages.");
        shader.SetSPIRV(vertexCode, fragmentCode); Check(changed == 1 && material.GetShaderParameter<float>("gain") == .5f, "Pair reload publishes once and retains material values.");
        shader.SetBlockSignals(true); shader.SetSPIRV(vertexCode, fragmentCode);
        Check(changed == 1 && material.GetShaderParameter<float>("gain") == .5f && material.GetShaderParameter<Vector2>("offset") == new Vector2(16, 0), "Suppressed reload still replaces both stages and migrates typed material values.");
        shader.SetBlockSignals(false); Check(changed == 1, "Unblocking a paired shader does not replay its suppressed reload.");
        Reject<ArgumentOutOfRangeException>(() => shader.GetSPIRV((RenderingDevice.ShaderStage)int.MaxValue));
        Reject<NotSupportedException>(() => shader.GetSPIRV(RenderingDevice.ShaderStage.Compute));
        var exported = shader.GetSPIRV(RenderingDevice.ShaderStage.Vertex); exported[0] = 0;
        Check(shader.GetSPIRV(RenderingDevice.ShaderStage.Vertex)[0] != 0, "Stage exports never expose program storage.");
        using var duplicate = (Shader)shader.Duplicate();
        Check(duplicate.GetSPIRV(RenderingDevice.ShaderStage.Vertex).SequenceEqual(vertexCode), "Resource duplication preserves the paired vertex stage.");
        var storage = shader.GetPropertyList().OfType<PropertyDescriptor<Shader, byte[]>>().Single(property => property.Name == "SPIRV");
        var packed = storage.GetValue(shader);
        Reject<InvalidDataException>(() => storage.SetValue(shader, packed[..12]));
        var bad = (byte[])packed.Clone(); bad[4] = 2; Reject<InvalidDataException>(() => storage.SetValue(shader, bad));
        bad = (byte[])packed.Clone(); System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(8), int.MaxValue);
        Reject<InvalidDataException>(() => storage.SetValue(shader, bad));
        Check(shader.GetSPIRV(RenderingDevice.ShaderStage.Vertex).SequenceEqual(vertexCode), "Malformed stored pair headers preserve the live program.");
        storage.SetValue(shader, fragmentCode); Check(shader.GetSPIRV(RenderingDevice.ShaderStage.Vertex).Length == 0, "Legacy fragment storage selects built-in vertices.");
        storage.SetValue(shader, packed);
        var folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "canvas-stages-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            var path = System.IO.Path.Combine(folder, "pair.e2dres"); ResourceSaver.Save(shader, path);
            using var restored = ResourceLoader.Load<Shader>(path, ResourceLoader.CacheMode.Ignore);
            Check(restored.GetSPIRV(RenderingDevice.ShaderStage.Vertex).SequenceEqual(vertexCode) && restored.GetSPIRV().SequenceEqual(fragmentCode), "Stored shader code restores both stages atomically.");
            var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
            start.ArgumentList.Add(typeof(CanvasVertexProgramTests).Assembly.Location); start.ArgumentList.Add("--canvas-vertex-archive"); start.ArgumentList.Add(path); start.Environment.Remove("ELECTRON2D_TEST_CANVAS_VERTEX");
            using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var errors = child.StandardError.ReadToEndAsync();
            if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Fresh paired shader archive deadline."); }
            Check(child.ExitCode == 0, "Fresh paired shader archive: " + output.GetAwaiter().GetResult() + errors.GetAwaiter().GetResult());
        }
        finally { Directory.Delete(folder, recursive: true); }
        Console.WriteLine("Canvas vertex stage material merge, migration and guards passed.");
    }

    internal static void RunNative(string profile = "gpu", bool gpuBuffers = false)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var language = profile.Contains("hlsl", StringComparison.Ordinal) ? "HLSL" : "GLSL";
        var hdr = profile.Contains("hdr", StringComparison.Ordinal); var instanced = profile.Contains("instanced", StringComparison.Ordinal); var lit = profile.Contains("lit", StringComparison.Ordinal);
        var glyph = profile.Contains("glyph", StringComparison.Ordinal);
        var resources = profile.Contains("resources", StringComparison.Ordinal) || glyph;
        var fixture = resources ? "CanvasUserResources" : "CanvasUserVertex" + language;
        using var shader = Shader.CreateFromSPIRV(Module(fixture + ".vert.spv"), Module((glyph ? "CanvasUserVertexGLSL" : fixture) + ".frag.spv"));
        using var textureImage = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); textureImage.Fill(Colors.White);
        using var texture = ImageTexture.CreateFromImage(textureImage);
        using var material = new ShaderMaterial { Shader = shader }; material.SetShaderParameter("gain", 1f); material.SetShaderParameter("offset", new Vector2(16, 0));
        if (resources) material.SetShaderParameter("vertexMap", texture);
        var window = new Window { Size = new(96, 64), UseHDR2D = hdr, MSAA2D = profile.Contains("msaa", StringComparison.Ordinal) ? Viewport.MSAA.Samples4X : Viewport.MSAA.Disabled };
        using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, MeshTests.Quad(Colors.White));
        using var multi = new MultiMesh { Mesh = mesh, UseColors = true, UseCustomData = true, InstanceCount = 2 };
        multi.SetInstanceTransform2D(0, new Transform(0, new Vector2(8, 8))); multi.SetInstanceTransform2D(1, new Transform(0, new Vector2(48, 8)));
        multi.SetInstanceColor(0, Colors.Red); multi.SetInstanceColor(1, Colors.Blue);
        CanvasItem item = instanced ? new MultiMeshInstance { MultiMesh = multi, Material = material } : new Box(glyph ? texture : null, profile.Contains("lcd", StringComparison.Ordinal)) { Position = new(8, 8), Material = material };
        if (resources) { item.SetInstanceShaderParameter<Vector2>("displacement", new Vector2(4, 0)); item.SetInstanceShaderParameter<float>("intensity", 1f); }
        window.AddChild(item);
        if (lit) { window.AddChild(new CanvasModulate { Color = new(.25f, .25f, .25f, 1) }); window.AddChild(new DirectionalLight { Color = Colors.White, Energy = .25f }); }
        var frame = 0; long before = 0, activeBytes = 0, idleBytes = 0;
        var reloadEvents = 0; shader.Changed += _ => reloadEvents++;
        window.Ready += _ =>
        {
            if (gpuBuffers) { RenderingServer.MultiMeshGetBufferRDRID(multi.GetRID()); window.SnapVerticesToPixel = profile.Contains("snap", StringComparison.Ordinal); }
            Check(RenderingServer.GetCurrentRenderingMethod() == "gpu", "Native GPU without fallback."); RenderingServer.SetDefaultClearColor(Colors.Black);
            var owned = RenderingServer.ShaderCreate(); RenderingServer.ShaderSetSPIRV(owned, shader.GetSPIRV(RenderingDevice.ShaderStage.Vertex), shader.GetSPIRV());
            Check(RenderingServer.ShaderGetSPIRV(owned, RenderingDevice.ShaderStage.Vertex).SequenceEqual(shader.GetSPIRV(RenderingDevice.ShaderStage.Vertex)), "Owned shader stages use the public server family."); RenderingServer.FreeRID(owned);
            window.Tree!.ProcessFrameStarted += _ =>
            {
                before = GC.GetAllocatedBytesForCurrentThread();
                if (frame is >= 16 and < 80) material.SetShaderParameter("offset", new Vector2((frame & 1) * 16, 16));
            };
            RenderingServer.FramePostDraw += () =>
            {
                if (frame is >= 16 and < 80) activeBytes += GC.GetAllocatedBytesForCurrentThread() - before;
                if (frame is >= 80 and < 144) idleBytes += GC.GetAllocatedBytesForCurrentThread() - before;
                frame++;
                if (frame == 144) { Check(activeBytes == 0 && idleBytes == 0, $"Prepared vertex frame allocations: active={activeBytes}, idle={idleBytes}."); window.Tree!.Quit(); }
                if (frame > 2) return;
                using var image = window.GetTexture().GetImage()!;
                var shiftedX = frame == 1; var x = (shiftedX ? 28 : 12) + (resources ? 8 : 0); var y = (shiftedX ? 12 : 28) + (resources ? 4 : 0);
                var expected = lit ? (hdr ? new Color(.25f, .25f, .25f, 1).SRGBToLinear().R : .25f) + .25f : 1;
                Check(MathF.Abs(image.GetPixel(x, y).R - expected) < .03f && image.GetPixel(12, 12).R < .02f, $"Authored vertex pixels {profile}: {image.GetPixel(x, y)}, expected R={expected}.");
                if (instanced) Check(MathF.Abs(image.GetPixel((shiftedX ? 68 : 52) + (resources ? 8 : 0), y).B - expected) < .03f, "Hardware instances preserve transforms and per-instance colors.");
                if (frame == 1)
                {
                    shader.SetBlockSignals(true); material.SetBlockSignals(true);
                    shader.SetSPIRV(shader.GetSPIRV(RenderingDevice.ShaderStage.Vertex), shader.GetSPIRV());
                    material.SetShaderParameter("offset", new Vector2(0, 16));
                    Check(reloadEvents == 0, "Suppressed shader reload retains executable native material migration.");
                }
            };
        };
        try { Check(Engine.Run(window) == 0 && frame == 144, "Vertex shader native frames completed."); }
        finally { if (!window.IsDisposed) window.Dispose(); }
        Console.WriteLine($"Canvas custom vertex stage native pixels passed: {profile}; 64 active={activeBytes} B, 64 idle={idleBytes} B.");
    }
    internal static void RunArchive(string path)
    {
        using var shader = ResourceLoader.Load<Shader>(path, ResourceLoader.CacheMode.Ignore);
        Check(shader.GetSPIRV(RenderingDevice.ShaderStage.Vertex).SequenceEqual(Module("CanvasUserVertexGLSL.vert.spv")) && shader.GetSPIRV().SequenceEqual(Module("CanvasUserVertexGLSL.frag.spv")), "Fresh resource factory restores both authored stages.");
    }

    private sealed class Box(Texture? glyph = null, bool lcd = false) : Entity
    {
        protected override void OnDraw()
        {
            if (glyph is null) DrawRect(new(0, 0, 16, 16), Colors.White);
            else if (lcd) DrawLCDTextureRectRegion(glyph, new(0, 0, 16, 16), new(0, 0, 1, 1));
            else DrawMSDFTextureRectRegion(glyph, new(0, 0, 16, 16), new(0, 0, 1, 1));
        }
    }
    private static byte[] Module(string name)
    { using var source = typeof(CanvasVertexProgramTests).Assembly.GetManifestResourceStream("TestShaders." + name)!; using var bytes = new MemoryStream(); source.CopyTo(bytes); return bytes.ToArray(); }

}
