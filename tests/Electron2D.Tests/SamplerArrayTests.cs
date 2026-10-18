using System.Runtime.InteropServices;
using System.Diagnostics;
using Electron2D;
using static TextureArrayTests;

internal static partial class SamplerArrayTests
{
    private static byte[] Module(string language, string stem = "SamplerBindings", string stage = "frag")
    {
        using var stream = typeof(SamplerArrayTests).Assembly.GetManifestResourceStream($"TestShaders.{stem}{language}.{stage}.spv")!;
        using var bytes = new MemoryStream(); stream.CopyTo(bytes); return bytes.ToArray();
    }
    private static ImageTexture Solid(Color color)
    {
        using var image = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); image.Fill(color); return ImageTexture.CreateFromImage(image);
    }
    private static byte[] Modern(byte[] code)
    {
        var module = new MaterialLightingProgram.Module(code); module.Header[1] = 0x10500;
        var entry = module.Instructions.Single(i => MaterialLightingProgram.Op(i) == 15); var end = MaterialLightingProgram.StringEnd(entry, 3);
        var variables = module.Instructions.Where(i => MaterialLightingProgram.Op(i) == 59 && i[3] != 7).Select(i => i[2]);
        module.Instructions[module.Instructions.IndexOf(entry)] = MaterialLightingProgram.Instruction(15, [.. entry.AsSpan(1, end - 1).ToArray(), .. entry.AsSpan(end).ToArray().Concat(variables).Distinct()]);
        return MemoryMarshal.AsBytes(module.Header.Concat(module.Instructions.SelectMany(i => i)).ToArray().AsSpan()).ToArray();
    }
    private static byte[] Length(byte[] code, int length)
    {
        var module = new MaterialLightingProgram.Module(code); var type = module.Instructions.First(i => MaterialLightingProgram.Op(i) == 28 && module.Instructions.Any(t => MaterialLightingProgram.Op(t) is 25 or 27 && t[1] == i[2]));
        var constant = module.Instructions.Single(i => MaterialLightingProgram.Op(i) == 43 && i[2] == type[3]); constant[3] = (uint)length;
        return MemoryMarshal.AsBytes(module.Header.Concat(module.Instructions.SelectMany(i => i)).ToArray().AsSpan()).ToArray();
    }
    internal static void Run()
    {
        using var red = Solid(Colors.Red); using var blue = Solid(Colors.Blue);
        foreach (var language in new[] { "GLSL", "HLSL" })
        {
            var original = Module(language); using var shader = Shader.CreateFromSPIRV(original); var program = shader.GetProgram();
            Check(program.Textures.Length == 2 && program.Textures.All(t => t.Name == "colorMaps" && t.ArrayLength == 2), "Physical elements retain one logical descriptor shape.");
            Check(shader.GetShaderUniformList().Count == 2 && shader.GetShaderUniformList().Single(p => p.Name == "colorMaps") is PropertyDescriptor<ShaderMaterial, Texture?[]>, "One typed resource-array descriptor.");
            Check(shader.GetSPIRV().SequenceEqual(original) && !program.NativeCode.SequenceEqual(original), "Original bytecode remains separate from native lowering.");
            if (Environment.GetEnvironmentVariable("ELECTRON2D_SAMPLER_DUMP") is { } path) File.WriteAllBytes(path + "." + language + ".spv", program.NativeCode);
            var changed = 0; shader.Changed += _ => changed++;
            shader.SetDefaultTextureParameter("colorMaps", red); shader.SetDefaultTextureParameter("colorMaps", blue, 1);
            Check(changed == 2 && ReferenceEquals(shader.GetDefaultTextureParameter("colorMaps"), red) && ReferenceEquals(shader.GetDefaultTextureParameter("colorMaps", 1), blue), "Indexed defaults and post-commit signals.");
            Reject<ArgumentOutOfRangeException>(() => shader.SetDefaultTextureParameter("colorMaps", red, -1));
            Reject<ArgumentOutOfRangeException>(() => shader.GetDefaultTextureParameter("colorMaps", 2));
            Reject<ArgumentException>(() => shader.GetDefaultLayeredTextureParameter("colorMaps", 0));
            using var material = new ShaderMaterial { Shader = shader }; Texture?[] values = [null, red]; material.SetShaderTextureParameterArray("colorMaps", values);
            var events = 0; material.Changed += _ => events++;
            values[1] = blue; Check(ReferenceEquals(material.GetShaderTextureParameterArray("colorMaps")[1], red), "Set copies references out of caller storage.");
            var copy = material.GetShaderTextureParameterArray("colorMaps"); copy[1] = blue;
            Check(ReferenceEquals(material.GetShaderTextureParameterArray("colorMaps")[1], red), "Get copies storage and borrows resources.");
            Reject<ArgumentException>(() => material.SetShaderParameter("colorMaps", red)); Reject<ArgumentException>(() => material.SetShaderParameter("colorMaps", null)); Reject<ArgumentException>(() => material.SetShaderLayeredParameter("colorMaps", null)); Reject<ArgumentException>(() => material.GetShaderParameter("colorMaps"));
            Reject<ArgumentException>(() => material.SetShaderTextureParameterArray("colorMaps", new Texture?[1]));
            using var disposed = Solid(Colors.White); disposed.Dispose(); Reject<ObjectDisposedException>(() => material.SetShaderTextureParameterArray("colorMaps", new Texture?[] { blue, disposed }));
            Check(events == 0 && ReferenceEquals(material.GetShaderTextureParameterArray("colorMaps")[1], red), "Invalid tail preserves every existing override and signal count.");
            Resource?[] effective = new Resource?[2]; material.GetCanvasState()!.CopyTextures(effective);
            Check(ReferenceEquals(effective[0], red) && ReferenceEquals(effective[1], red), "Null override resolves its own element default.");
            shader.SetDefaultTextureParameter("colorMaps", null, 1); material.SetShaderTextureParameterArray("colorMaps", new Texture?[2]);
            Reject<InvalidOperationException>(() => material.GetCanvasState()!.CopyTextures(effective)); shader.SetDefaultTextureParameter("colorMaps", red, 1);
            material.SetShaderTextureParameterArray("colorMaps", new Texture?[] { red, null });
            using var deep = (ShaderMaterial)material.Duplicate(true); var deepImage = deep.GetShaderTextureParameterArray("colorMaps")[0]!;
            Check(!ReferenceEquals(deepImage, red) && ReferenceEquals(deepImage, deep.Shader!.GetDefaultTextureParameter("colorMaps", 1)), "Deep graph duplication preserves default/override aliases.");
            deepImage.Dispose(); deep.Shader!.Dispose();
            var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-sampler-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
            try
            {
                var file = System.IO.Path.Combine(directory, "material.e2dres"); ResourceSaver.Save(material, file); CheckArchive(file);
                using var restored = ResourceLoader.Load<ShaderMaterial>(file, cacheMode: ResourceLoader.CacheMode.Ignore)!;
                var image = restored.GetShaderTextureParameterArray("colorMaps")[0]!;
                Check(ReferenceEquals(image, restored.Shader!.GetDefaultTextureParameter("colorMaps", 1)) && restored.Shader.GetSPIRV().SequenceEqual(original), "Typed archives preserve arrays, resource aliases and original bytecode.");
                image.Dispose(); restored.Shader.Dispose();
            }
            finally { Directory.Delete(directory, true); }
            shader.SetSPIRV(original); Check(ReferenceEquals(material.GetShaderTextureParameterArray("colorMaps")[0], red), "Compatible reload retains each override.");
            shader.SetSPIRV(Length(original, 1)); Check(material.GetShaderTextureParameterArray("colorMaps").Length == 1 && material.GetShaderTextureParameterArray("colorMaps")[0] is null && shader.GetDefaultTextureParameter("colorMaps") is null, "Length-one arrays stay arrays and incompatible lengths drop the complete value.");
            Reject<ArgumentException>(() => material.SetShaderParameter("colorMaps", red)); shader.SetSPIRV(original);
            var destination = new Texture?[2]; values[0] = red; values[1] = blue;
            for (var i = 0; i < 32; i++) { material.SetShaderTextureParameterArray("colorMaps", values); material.CopyShaderTextureParameterArray("colorMaps", destination); }
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 1024; i++) { material.SetShaderTextureParameterArray("colorMaps", values); material.CopyShaderTextureParameterArray("colorMaps", destination); }
            Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared typed resource array updates/copies allocate zero managed bytes.");
        }
        Callbacks();
        var separate = Module("GLSL", "SamplerPairs");
        using (var paired = Shader.CreateFromSPIRV(separate))
        {
            Check(paired.GetProgram().Textures.Length == 4, "A reusable image/sampler helper accepts different correctly paired array bindings.");
            var module = new MaterialLightingProgram.Module(separate);
            var calls = module.Instructions.Where(i => MaterialLightingProgram.Op(i) == 57).ToArray();
            Check(calls.Length == 2 && calls.All(i => i.Length == 6), "Pair fixture exercises reusable opaque helper parameters.");
            calls[1][5] = calls[0][5];
            var crossed = MemoryMarshal.AsBytes(module.Header.Concat(module.Instructions.SelectMany(i => i)).ToArray().AsSpan()).ToArray();
            Reject<NotSupportedException>(() => Shader.CreateFromSPIRV(crossed));
            if (Environment.GetEnvironmentVariable("ELECTRON2D_SAMPLER_DUMP") is { } path) File.WriteAllBytes(path + ".pairs.spv", paired.GetProgram().NativeCode);
        }
        foreach (var language in new[] { "GLSL", "HLSL" })
        {
            using var full = Shader.CreateFromSPIRV(Length(Module(language), 16));
            Check(full.GetProgram().Textures.Length == 16 && ShaderCompiler.SamplerCount(full.GetProgram().NativeCode) == 16, "Sixteen descriptors reach native reflection.");
            Reject<NotSupportedException>(() => Shader.CreateFromSPIRV(Length(Module(language), 17)));
            if (Environment.GetEnvironmentVariable("ELECTRON2D_SAMPLER_DUMP") is { } path) File.WriteAllBytes(path + ".full-" + language + ".spv", full.GetProgram().NativeCode);
            using var pair = Shader.CreateFromSPIRV(Modern(Module(language, "SamplerVertex", "vert")), Modern(Module(language)));
            if (Environment.GetEnvironmentVariable("ELECTRON2D_SAMPLER_DUMP") is { } folder)
                for (var i = 0; i < 8; i++) File.WriteAllBytes(folder + ".vertex-" + language + i + ".spv", pair.GetProgram().VertexCode((i & 1) != 0, (i & 2) != 0, (i & 4) != 0)!);
        }
        using (var image = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8))
        using (var layered = new TextureArray())
        using (var shader = Shader.CreateFromSPIRV(Module("GLSL", "SamplerOperations")))
        using (var material = new ShaderMaterial { Shader = shader })
        {
            layered.CreateFromImages([image]);
            shader.SetDefaultLayeredTextureParameter("layers", layered, 1);
            material.SetShaderLayeredParameterArray("layers", new TextureLayered?[] { layered, null });
            using var duplicate = (ShaderMaterial)material.Duplicate(true);
            var copy = duplicate.GetShaderLayeredParameterArray("layers")[0]!;
            Check(!ReferenceEquals(copy, layered) && ReferenceEquals(copy, duplicate.Shader!.GetDefaultLayeredTextureParameter("layers", 1)), "Layered descriptor duplication preserves graph aliases.");
            copy.Dispose(); duplicate.Shader!.Dispose();
            var file = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-layered-sampler-" + Guid.NewGuid().ToString("N") + ".e2dres");
            try { ResourceSaver.Save(material, file); CheckArchive(file); } finally { File.Delete(file); }
        }
        foreach (var language in new[] { "GLSL", "HLSL" })
            foreach (var stem in new[] { "SamplerOperations", "SamplerScalar" })
            {
                using var shader = Shader.CreateFromSPIRV(Module(language, stem));
                if (Environment.GetEnvironmentVariable("ELECTRON2D_SAMPLER_DUMP") is { } path) File.WriteAllBytes(path + "." + stem + language + ".spv", shader.GetProgram().NativeCode);
            }
        Console.WriteLine("Sampler arrays: reflection, helpers, typed/default arrays, atomic rejection, copy/graph/archive, reload/length-one and warm access passed.");
    }

    private static void CheckArchive(string file)
    {
        var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(typeof(SamplerArrayTests).Assembly.Location);
        start.Environment["ELECTRON2D_TEST_SAMPLER_ARRAYS"] = "archive"; start.Environment["ELECTRON2D_SAMPLER_ARCHIVE"] = file;
        using var child = Process.Start(start)!;
        var output = child.StandardOutput.ReadToEnd(); var error = child.StandardError.ReadToEnd(); child.WaitForExit();
        Check(child.ExitCode == 0, "Fresh-process sampler archive failed: " + output + error);
    }
    private static void ReadArchive()
    {
        using var material = ResourceLoader.Load<ShaderMaterial>(Environment.GetEnvironmentVariable("ELECTRON2D_SAMPLER_ARCHIVE")!, cacheMode: ResourceLoader.CacheMode.Ignore)!;
        var shader = material.Shader!; var ordinary = shader.GetShaderUniformList().Any(p => p.Name == "colorMaps");
        Resource image = ordinary ? material.GetShaderTextureParameterArray("colorMaps")[0]! : material.GetShaderLayeredParameterArray("layers")[0]!;
        var fallback = ordinary ? (Resource?)shader.GetDefaultTextureParameter("colorMaps", 1) : shader.GetDefaultLayeredTextureParameter("layers", 1);
        Check(ReferenceEquals(image, fallback) && !image.IsDisposed, "Fresh-process ordinary/layered descriptor aliases and concrete factories.");
        image.Dispose(); shader.Dispose();
    }

    private sealed class Patch : Entity
    {
        internal int Draws;
        protected override void OnDraw() { Draws++; DrawRect(new(0, 0, 40, 24), Colors.White); }
    }
    internal static void RunNative(string profile)
    {
        if (profile == "archive") { ReadArchive(); return; }
        if (profile.StartsWith("vertex", StringComparison.Ordinal)) { Stages(profile); return; }
        if (profile.StartsWith("operations", StringComparison.Ordinal)) { Operations(profile.Contains("hlsl", StringComparison.Ordinal) ? "HLSL" : "GLSL"); return; }
        var backend = profile == "compatibility" ? profile : "gpu"; var language = profile.Contains("hlsl", StringComparison.Ordinal) ? "HLSL" : "GLSL";
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var red = Solid(Colors.Red); using var blue = Solid(Colors.Blue); using var shader = Shader.CreateFromSPIRV(profile.Contains("capacity", StringComparison.Ordinal) ? Length(Module(language), 16) : profile.Contains("modern", StringComparison.Ordinal) ? Modern(Module(language)) : Module(language));
        shader.SetDefaultTextureParameter("colorMaps", red); shader.SetDefaultTextureParameter("colorMaps", blue, 1);
        using var material = new ShaderMaterial { Shader = shader }; Texture?[] overrides = Enumerable.Range(0, shader.GetProgram().Textures.Length).Select(i => (Texture?)(i % 2 == 0 ? red : blue)).ToArray(); material.SetShaderTextureParameterArray("colorMaps", overrides);
        using var root = new Window { Size = new(96, 32) }; var left = new Patch { Material = material, Position = new(0, 4) }; var right = new Patch { Position = new(48, 4) }; root.AddChild(left); root.AddChild(right);
        var frames = 0; var warmLeft = 0; var warmRight = 0; long start = 0, active = 0, idle = 0; RID ownedShader = default, ownedMaterial = default; RID[] identities = overrides.Select(t => t!.GetRID()).ToArray(); var result = new RID[identities.Length];
        if (backend == "compatibility")
        {
            Reject<NotSupportedException>(() => Engine.Run(root)); Check(root.IsDisposed && RenderingServer.Service is null, "Compatibility rejects descriptor arrays before drawing and cleans up."); return;
        }
        root.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); ownedShader = RenderingServer.ShaderCreate(); RenderingServer.ShaderSetSPIRV(ownedShader, shader.GetSPIRV());
            RenderingServer.ShaderSetDefaultTextureParameter(ownedShader, "colorMaps", red.GetRID(), 0); RenderingServer.ShaderSetDefaultTextureParameter(ownedShader, "colorMaps", blue.GetRID(), 1);
            Check(RenderingServer.ShaderGetDefaultTextureParameter(ownedShader, "colorMaps", 1) == blue.GetRID(), "Indexed server defaults.");
            ownedMaterial = RenderingServer.MaterialCreate(); RenderingServer.MaterialSetShader(ownedMaterial, ownedShader); RenderingServer.MaterialSetParam(ownedMaterial, "colorMaps", identities);
            Check(RenderingServer.MaterialGetParamTextureArray(ownedMaterial, "colorMaps").SequenceEqual(identities), "Copied RID array access.");
            Reject<ArgumentException>(() => RenderingServer.MaterialSetParam(ownedMaterial, "colorMaps", new RID[] { red.GetRID(), RID.Allocate() }));
            Check(RenderingServer.MaterialGetParamTextureArray(ownedMaterial, "colorMaps").SequenceEqual(identities), "Invalid RID tail preserves every override.");
            RenderingServer.CanvasItemSetMaterial(right.GetCanvasItem(), ownedMaterial);
            root.Tree!.ProcessFrameStarted += _ =>
            {
                start = GC.GetAllocatedBytesForCurrentThread();
                if (frames < 88)
                {
                    material.SetShaderTextureParameterArray("colorMaps", overrides); material.SetShaderParameter("selected", frames % 2 == 0 ? 0 : overrides.Length - 1);
                    RenderingServer.MaterialSetParam(ownedMaterial, "colorMaps", identities); RenderingServer.MaterialSetParam(ownedMaterial, "selected", frames % 2 == 0 ? 0 : overrides.Length - 1); RenderingServer.MaterialGetParam(ownedMaterial, "colorMaps", result);
                }
            };
            RenderingServer.FramePostDraw += () =>
            {
                var bytes = GC.GetAllocatedBytesForCurrentThread() - start;
                if (frames == 23) { warmLeft = left.Draws; warmRight = right.Draws; }
                if (frames is >= 24 and < 88) active += bytes;
                if (frames >= 112) idle += bytes;
                if (frames < 2 || frames == 175)
                {
                    using var image = root.GetTexture().GetImage()!; var expected = frames % 2 == 0 && frames < 88 ? Colors.Red : Colors.Blue;
                    foreach (var x in new[] { 16, 64 })
                    {
                        var pixel = image.GetPixel(x * image.Width / root.Size.X, 16 * image.Height / root.Size.Y);
                        Check(Math.Abs(pixel.R - expected.R) < .01 && Math.Abs(pixel.G - expected.G) < .01 && Math.Abs(pixel.B - expected.B) < .01, $"Native {language} array pixel {x}: {pixel} vs {expected}.");
                    }
                }
                if (++frames != 176) return;
                Check(active == 0 && idle == 0 && left.Draws == warmLeft && right.Draws == warmRight && warmLeft > 0 && warmRight > 0, $"Prepared arrays and native frames allocate {active}/{idle} managed bytes; draw captures {left.Draws}/{right.Draws}.");
                RenderingServer.FreeRID(ownedMaterial); RenderingServer.FreeRID(ownedShader); root.Tree!.Quit();
            };
        };
        Engine.Run(root); Check(root.IsDisposed && RenderingServer.Service is null && frames == 176, "Native descriptor array lifecycle.");
        Console.WriteLine($"Sampler arrays {language}: live material/server pixels and native binding; 64 active={active}, 64 idle={idle} managed bytes.");
    }
}
