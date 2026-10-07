using Electron2D;

internal static class RenderingProgramTests
{
    private sealed class Paint : Entity
    {
        internal int Draws;
        protected override void OnDraw() { Draws++; DrawRect(new(40, 4, 12, 12), Colors.White); }
    }
    private static byte[] Code(string name)
    {
        using var stream = typeof(RenderingProgramTests).Assembly.GetManifestResourceStream("TestShaders." + name + ".spv")!;
        using var memory = new MemoryStream(); stream.CopyTo(memory); return memory.ToArray();
    }
    internal static void Run()
    {
        using var shader = Shader.CreateFromSPIRV(Code("MaterialHlsl")); using var material = new ShaderMaterial { Shader = shader };
        var sr = shader.GetRID(); var mr = material.GetRID();
        Check(sr.IsValid() && mr.IsValid() && sr != mr && shader.GetRID() == sr && material.GetRID() == mr, "Stable distinct program identities before startup.");
        using var duplicate = (ShaderMaterial)material.Duplicate(); Check(duplicate.GetRID() != mr, "Duplicates own independent identities.");
        material.SetShaderParameter<float>("weights", [.25f, .75f]); Span<float> values = stackalloc float[2]; material.CopyShaderParameterArray("weights", values);
        Check(values[0] == .25f && values[1] == .75f, "Caller storage receives typed array elements.");
        var unchanged = new[] { 9f }; Reject<ArgumentException>(() => material.CopyShaderParameterArray("weights", unchanged.AsSpan())); Check(unchanged[0] == 9, "Rejected destination remains unchanged.");
        for (var i = 0; i < 64; i++) material.CopyShaderParameterArray("weights", values);
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 2000; i++) material.CopyShaderParameterArray("weights", values);
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "2000 warmed array copies allocate zero managed bytes.");
        shader.Dispose(); material.Dispose(); Check(RenderingProgramRegistry<Shader>.ResolveOrNull(sr) is null && RenderingProgramRegistry<Material>.ResolveOrNull(mr) is null, "Disposal removes borrowed program identities.");
        Reject<ObjectDisposedException>(() => shader.GetRID()); Reject<ObjectDisposedException>(() => material.GetRID()); Reject<InvalidOperationException>(() => RenderingServer.ShaderCreate());
        Console.WriteLine("Program identities and 2000 allocation-free typed array copies passed.");
    }
    internal static void RunHost()
    {
        var backend = Environment.GetEnvironmentVariable("ELECTRON2D_RENDER_BACKEND") ?? "gpu";
        var fixture = Environment.GetEnvironmentVariable("ELECTRON2D_SHADER_FIXTURE") ?? "Hlsl";
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); Engine.MaxFPS = 60;
        using var authored = new CanvasItemMaterial(); using var blend = new CanvasItemMaterial { BlendMode = BlendMode.Sub };
        using var borrowed = Shader.CreateFromSPIRV(Code("Material" + fixture)); using var borrowedMaterial = new ShaderMaterial { Shader = borrowed };
        using var pixels = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); pixels.Fill(Colors.Red); using var red = ImageTexture.CreateFromImage(pixels); pixels.Fill(Colors.Blue); using var blue = ImageTexture.CreateFromImage(pixels);
        var window = new Window { Size = new(80, 40) }; var source = new Paint { Material = authored }; window.AddChild(source);
        RID shader = default, material = default, item = default, textureShader = default, textureMaterial = default, textureItem = default, cleanupShader = default, cleanupMaterial = default;
        var submissionChecks = 0; var phase = -4; var preparedDraws = 0; long warmBytes = 0;
        using var submissionTexture = new SubmissionTexture(pixels, () => { Reject<InvalidOperationException>(() => RenderingServer.MaterialSetParam(material, "gain", 2f)); Reject<InvalidOperationException>(() => RenderingServer.FreeRID(material)); submissionChecks++; });
        window.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Requested backend is active."); RenderingServer.SetDefaultClearColor(Colors.Black);
            shader = RenderingServer.ShaderCreate(); material = RenderingServer.MaterialCreate(); item = RenderingServer.CanvasItemCreate(); RenderingServer.CanvasItemSetParent(item, source.GetCanvas()); RenderingServer.CanvasItemAddRect(item, new(4, 4, 12, 12), Colors.White);
            Check(RenderingServer.ShaderGetSPIRV(shader).Length > 0, "Fresh shader contains the built-in program.");
            RenderingServer.ShaderSetSPIRV(shader, borrowed.GetSPIRV()); RenderingServer.MaterialSetShader(material, shader); Initialize(material, Colors.Red);
            Check(RenderingServer.ShaderGetSPIRV(borrowed.GetRID()).SequenceEqual(borrowed.GetSPIRV()), "Borrowed shader reads are executable.");
            borrowedMaterial.SetShaderParameter("gain", .75f); Check(RenderingServer.MaterialGetParam<float>(borrowedMaterial.GetRID(), "gain") == .75f, "Borrowed material reads are executable.");
            Check(RenderingServer.GetShaderParameterList(shader).Count == 7 && RenderingServer.MaterialGetParam<Color>(material, "tint") == Colors.Red, "Reflected catalog and typed values.");
            var bytes = RenderingServer.ShaderGetSPIRV(shader); bytes[0] = 0; Check(RenderingServer.ShaderGetSPIRV(shader)[0] != 0, "Shader code getters copy storage.");
            RenderingServer.ShaderSetPathHint(shader, "res://program-test");
            try { RenderingServer.ShaderSetSPIRV(shader, new byte[4]); throw new InvalidOperationException("Invalid module accepted."); } catch (ArgumentException error) { Check(error.Message.Contains("res://program-test"), "Path hint annotates validation failure."); }
            Check(RenderingServer.ShaderGetSPIRV(shader).SequenceEqual(borrowed.GetSPIRV()), "Rejected code preserves program.");
            Reject<InvalidOperationException>(() => RenderingServer.ShaderSetSPIRV(borrowed.GetRID(), borrowed.GetSPIRV())); Reject<InvalidOperationException>(() => RenderingServer.MaterialSetShader(borrowedMaterial.GetRID(), shader)); Reject<InvalidOperationException>(() => RenderingServer.FreeRID(borrowed.GetRID())); Reject<InvalidOperationException>(() => RenderingServer.FreeRID(authored.GetRID()));
            Reject<ArgumentException>(() => RenderingServer.MaterialSetShader(material, red.GetRID())); Reject<ArgumentException>(() => RenderingServer.MaterialSetParam(material, "gain", float.NaN)); Reject<ArgumentException>(() => RenderingServer.MaterialSetParam<float>(material, "weights", [.5f])); Reject<ArgumentException>(() => RenderingServer.MaterialGetParam<int>(material, "gain"));
            var copied = RenderingServer.MaterialGetParamArray<float>(material, "weights"); copied[0] = 9; Check(RenderingServer.MaterialGetParamArray<float>(material, "weights")[0] == .25f, "Array reads copy storage.");
            Reject<InvalidOperationException>(() => Task.Run(() => RenderingServer.MaterialSetParam(material, "gain", 1f)).GetAwaiter().GetResult());
            if (backend == "gpu")
            {
                var parent = RenderingServer.CanvasItemCreate(); RenderingServer.CanvasItemSetParent(parent, source.GetCanvas()); RenderingServer.CanvasItemSetParent(item, parent); RenderingServer.CanvasItemSetMaterial(parent, material); RenderingServer.CanvasItemSetUseParentMaterial(item, true);
                textureShader = RenderingServer.ShaderCreate(); RenderingServer.ShaderSetSPIRV(textureShader, Code("Texture" + fixture)); textureMaterial = RenderingServer.MaterialCreate(); RenderingServer.MaterialSetShader(textureMaterial, textureShader);
                RenderingServer.ShaderSetDefaultTextureParameter(textureShader, "colorMap", red.GetRID()); RenderingServer.ShaderSetDefaultTextureParameter(textureShader, "detailMap", submissionTexture.GetRID()); RenderingServer.MaterialSetParam(textureMaterial, "tint", Colors.White); RenderingServer.MaterialSetParam(textureMaterial, "lod", 0f); RenderingServer.MaterialSetParam(textureMaterial, "detailAmount", 0f);
                Check(RenderingServer.ShaderGetDefaultTextureParameter(textureShader, "colorMap") == red.GetRID() && !RenderingServer.MaterialGetParam(textureMaterial, "colorMap").IsValid(), "Default and explicit texture identities remain distinct.");
                Reject<ArgumentOutOfRangeException>(() => RenderingServer.ShaderSetDefaultTextureParameter(textureShader, "colorMap", blue.GetRID(), 1));
                textureItem = RenderingServer.CanvasItemCreate(); RenderingServer.CanvasItemSetParent(textureItem, source.GetCanvas()); RenderingServer.CanvasItemAddRect(textureItem, new(24, 4, 12, 12), Colors.White); RenderingServer.CanvasItemSetMaterial(textureItem, textureMaterial);
            }
            RenderingServer.CanvasItemSetMaterial(source.GetCanvasItem(), blend.GetRID()); Check(ReferenceEquals(source.Material, authored), "Server material override preserves source getter.");
            cleanupShader = RenderingServer.ShaderCreate(); cleanupMaterial = RenderingServer.MaterialCreate();
            RenderingServer.FramePostDraw += () =>
            {
                using var image = window.GetTexture().GetImage()!;
                Pixel(image, 8, 8, backend == "compatibility" || phase >= 4 ? Colors.White : phase <= 0 ? Colors.Red : phase == 1 ? Colors.Blue : new(.5f, 0, 0));
                Pixel(image, 44, 8, phase <= 0 ? new Color(0, 0, 0, 0) : Colors.White); if (phase == 0) preparedDraws = source.Draws; if (phase >= 0) Check(source.Draws == preparedDraws, $"Source material republishing preserves retained geometry: phase {phase}, draws {source.Draws}.");
                if (backend == "gpu" && phase < 5) Pixel(image, 28, 8, phase == 1 ? Colors.Blue : Colors.Red);
                switch (phase)
                {
                    case 0: RenderingServer.MaterialSetParam(material, "tint", Colors.Blue); if (backend == "gpu") RenderingServer.MaterialSetParam(textureMaterial, "colorMap", blue.GetRID()); source.Material = authored; Check(!source.ServerState!.MaterialAssigned, "Source setter republishes matching field."); break;
                    case 1: RenderingServer.ShaderSetSPIRV(shader, Code("MaterialReordered")); RenderingServer.MaterialSetParam(material, "mode", 1); RenderingServer.MaterialSetParam(material, "gain", .5f); if (backend == "gpu") RenderingServer.MaterialSetParam(textureMaterial, "colorMap", default(RID)); break;
                    case 2:
                        if (Environment.GetEnvironmentVariable("ELECTRON2D_PROGRAM_SNAPSHOT") is { } snapshot) image.SavePNG(snapshot);
                        Span<float> values = stackalloc float[2];
                        for (var i = 0; i < 64; i++) { RenderingServer.MaterialSetParam(material, "gain", .5f); RenderingServer.MaterialGetParam(material, "weights", values); }
                        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 2000; i++) { RenderingServer.MaterialSetParam(material, "gain", .5f); RenderingServer.MaterialGetParam(material, "weights", values); }
                        warmBytes = GC.GetAllocatedBytesForCurrentThread() - before;
                        Check(warmBytes == 0 && values[0] == .25f && values[1] == .75f, "2000 warm RID updates and array reads allocate zero managed bytes."); break;
                    case 3: RenderingServer.FreeRID(shader); Reject<ArgumentException>(() => RenderingServer.ShaderGetSPIRV(shader)); Reject<ArgumentException>(() => RenderingServer.FreeRID(shader)); Reject<InvalidOperationException>(() => RenderingServer.MaterialGetParam<float>(material, "gain")); break;
                    case 4: RenderingServer.FreeRID(material); if (backend == "gpu") { RenderingServer.FreeRID(textureMaterial); RenderingServer.FreeRID(textureShader); } break;
                    case 5: window.Tree!.Quit(); break;
                }
                phase++;
            };
        };
        Engine.Run(window); Check(phase == 6 && window.IsDisposed && Engine.MainLoop is null && RenderingProgramRegistry<Shader>.ResolveOrNull(cleanupShader) is null && RenderingProgramRegistry<Material>.ResolveOrNull(cleanupMaterial) is null, "Renderer teardown releases remaining owned programs.");
        Check(backend != "gpu" || submissionChecks > 0, "Native texture capture rejected material mutation and free.");
        Console.WriteLine($"Program RID pixels/reload/ownership passed: {backend}/{fixture}; {phase} phases; 2000 warm updates/reads, {warmBytes} bytes.");
        if (backend == "compatibility")
        {
            var rejected = new Window { Size = new(40, 40) }; rejected.Ready += _ => { var s = RenderingServer.ShaderCreate(); RenderingServer.ShaderSetSPIRV(s, Code("Material" + fixture)); var m = RenderingServer.MaterialCreate(); RenderingServer.MaterialSetShader(m, s); Initialize(m, Colors.Red); var n = RenderingServer.CanvasItemCreate(); var c = RenderingServer.CanvasCreate(); RenderingServer.ViewportAttachCanvas(rejected.GetViewportRID(), c); RenderingServer.CanvasItemSetParent(n, c); RenderingServer.CanvasItemAddRect(n, new(0, 0, 8, 8), Colors.White); RenderingServer.CanvasItemSetMaterial(n, m); };
            Reject<NotSupportedException>(() => Engine.Run(rejected)); Check(rejected.IsDisposed && Engine.MainLoop is null, "Compatibility shader rejection cleans up ownership.");
        }
    }
    private sealed class SubmissionTexture(Image image, Action capture) : Texture
    {
        public override int GetWidth() => image.Width;
        public override int GetHeight() => image.Height;
        public override Image GetImage() { capture(); return TexturePixels.FromImage(image).CopyImage(); }
    }
    private static void Initialize(RID material, Color tint)
    {
        RenderingServer.MaterialSetParam(material, "tint", tint); RenderingServer.MaterialSetParam(material, "gain", 1f); RenderingServer.MaterialSetParam(material, "mode", 0); RenderingServer.MaterialSetParam(material, "offset", Vector2.Zero); RenderingServer.MaterialSetParam(material, "shift", Vector2i.Zero); RenderingServer.MaterialSetParam(material, "enabled", 1u); RenderingServer.MaterialSetParam<float>(material, "weights", [.25f, .75f]);
    }
    private static void Pixel(Image image, int x, int y, Color expected) { var actual = image.GetPixel(x, y); Check(Math.Abs(actual.R - expected.R) < .01f && Math.Abs(actual.G - expected.G) < .01f && Math.Abs(actual.B - expected.B) < .01f && Math.Abs(actual.A - expected.A) < .01f, $"Pixel {x},{y}: expected {expected}, got {actual}."); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
