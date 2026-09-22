using Electron2D;

internal static partial class RenderingRuntimeTests
{
    internal static void Run()
    {
        var settings = ProjectSettings.Instance;
        var method = settings.Get(ProjectSettings.RenderingMethod);
        var fallback = settings.Get(ProjectSettings.RenderingFallback);
        var limit = Engine.Instance.MaxFPS;
        try
        {
            Engine.Instance.MaxFPS = 60;
            settings.Set(ProjectSettings.RenderingFallback, false);
            VerifyImageDependency();
            VerifyResources();
            VerifyParameters("MaterialHlsl");
            VerifyParameters("MaterialGlsl");
            VerifyTextureResources();
            VerifyCanvasGeometryAllocations();
            if (Environment.GetEnvironmentVariable("SDL_VIDEODRIVER") != "dummy") VerifyTextureCursor();
            foreach (var backend in Environment.GetEnvironmentVariable("SDL_VIDEODRIVER") == "dummy" ? new[] { "compatibility" } : new[] { "compatibility", "gpu" })
            {
                settings.Set(ProjectSettings.RenderingMethod, backend);
                VerifySceneHierarchy(backend);
                VerifyCanvasOrdering(backend);
                VerifyFrame(backend);
                VerifyFrameAllocations(backend);
                VerifyCanvasTexture(backend);
                VerifySprite(backend);
                VerifyCanvasTextureFailures();
                VerifyCanvasHDR(backend);
                VerifyFailure(backend, "SwapHlsl");
                VerifyFailure(backend, "SwapGlsl");
                if (backend == "gpu")
                {
                    VerifyMaterialFrame("MaterialHlsl");
                    VerifyMaterialFrame("MaterialGlsl");
                    VerifyTextureFrame("TextureHlsl");
                    VerifyTextureFrame("TextureGlsl");
                    VerifyCanvasTexture(backend, "CanvasHLSL");
                    VerifyCanvasTexture(backend, "CanvasGLSL");
                    VerifyCanvasUV();
                    VerifySprite(backend, "CanvasGLSL");
                }
                VerifyTextureFailure(backend);
            }
            if (Environment.GetEnvironmentVariable("SDL_VIDEODRIVER") == "dummy")
            {
                settings.Set(ProjectSettings.RenderingMethod, "gpu");
                var rejected = new Window();
                Reject<InvalidOperationException>(() => Engine.Instance.Run(rejected));
                Released(rejected);
                settings.Set(ProjectSettings.RenderingFallback, true);
                VerifyFrame("compatibility");
            }
            Console.WriteLine("Rendering runtime checks passed.");
        }
        finally
        {
            settings.Set(ProjectSettings.RenderingMethod, method);
            settings.Set(ProjectSettings.RenderingFallback, fallback);
            Engine.Instance.MaxFPS = limit;
        }
    }

    private static void VerifyResources()
    {
        using var shader = LoadShader("SwapHlsl");
        Check(shader.GetMode() == Shader.Mode.CanvasItem, "Shaders have the canvas mode.");
        var bytes = shader.GetSPIRV();
        using var binary = Shader.CreateFromSPIRV(bytes);
        bytes[0] = 0;
        Check(binary.GetSPIRV()[0] != 0, "Binary shader input is copied.");
        Reject<ArgumentException>(() => Shader.CreateFromSPIRV(bytes));
        Reject<ArgumentException>(() => Shader.CreateFromSPIRV(new byte[3]));
        var truncated = shader.GetSPIRV();
        Array.Clear(truncated, 20, 4);
        Reject<ArgumentException>(() => Shader.CreateFromSPIRV(truncated));
        var code = shader.GetSPIRV();
        Reject<ArgumentException>(() => shader.SetSPIRV(new byte[3]));
        var oversized = new byte[16 * 1024 * 1024 + 4];
        foreach (Action load in new Action[] {
            () => Shader.CreateFromSPIRV(oversized),
            () => shader.SetSPIRV(oversized),
            () => ShaderCompiler.ValidateInterface(oversized, fragment: true),
        })
        {
            Reject<ArgumentException>(load);
            var before = GC.GetAllocatedBytesForCurrentThread();
            Reject<ArgumentException>(load);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Check(allocated < 65536, $"An oversized shader must be rejected before copying; allocated {allocated} bytes.");
        }
        Check(shader.GetSPIRV().SequenceEqual(code), "Invalid replacement preserves the previous program.");
        using var material = new ShaderMaterial { Shader = shader };
        using var duplicate = (ShaderMaterial)material.Duplicate(true);
        Check(duplicate.Shader is not null && !ReferenceEquals(duplicate.Shader, shader) && duplicate.Shader.GetSPIRV().SequenceEqual(code),
            "Material duplication honors the resource graph policy.");
        duplicate.Shader!.Dispose();
        using var root = new Entity { Material = material, Modulate = new Color(0.5f, 1, 1), SelfModulate = new Color(1, 0.5f, 1), UseParentMaterial = false };
        using var packed = new PackedScene();
        packed.Pack(root);
        using var copy = (Entity)packed.Instantiate();
        Check(copy.Material == material && copy.Modulate == root.Modulate && copy.SelfModulate == root.SelfModulate,
            "PackedScene stores canvas properties and borrows nonlocal materials.");
        Reject<InvalidOperationException>(() => root.DrawRect(new Rect(0, 0, 8, 8), Colors.White));
        root.ZIndex = 4096;
        Reject<ArgumentOutOfRangeException>(() => root.ZIndex = 4097);
    }

    private static void VerifyFrame(string expectedBackend)
    {
        var window = new Window { Title = "Electron2D canvas verification", Size = new Vector2I(128, 96) };
        var red = new CanvasNode { Name = "red", DrawAction = n => n.DrawRect(new Rect(8, 8, 64, 40), Colors.Red) };
        var green = new CanvasNode { Name = "green", ZIndex = 1, DrawAction = n => n.DrawRect(new Rect(24, 16, 32, 24), new Color(0, 1, 0, 0.5f)) };
        var blue = new CanvasNode { Name = "blue", Position = new Vector2(88, 8), Scale = new Vector2(2, 2), DrawAction = n => n.DrawRect(new Rect(0, 0, 8, 8), Colors.Blue) };
        var hidden = new CanvasNode { Name = "hidden", Visible = false, ZIndex = 10, DrawAction = n => n.DrawRect(new Rect(0, 0, 128, 96), Colors.White) };
        var line = new CanvasNode { Name = "line", DrawAction = n => n.DrawLine(new Vector2(8, 64), new Vector2(72, 64), Colors.Yellow, 2) };
        window.AddChild(green); window.AddChild(red); window.AddChild(blue); window.AddChild(hidden); window.AddChild(line);
        var frames = 0;
        Image? first = null;
        var hook = new CanvasNode
        {
            Name = "hook",
            ReadyAction = node =>
            {
                var server = RenderingServer.Instance!;
                Check(server.GetCurrentRenderingMethod() == expectedBackend, "The requested backend is actually active.");
                server.SetDefaultClearColor(Colors.Black);
                Reject<InvalidOperationException>(server.Dispose);
                Reject<InvalidOperationException>(DisplayServer.Instance!.Dispose);
                server.FramePreDraw += () => Reject<InvalidOperationException>(() => Engine.Instance.AdvanceFrame(0));
                server.FramePostDraw += () =>
                {
                    frames++;
                    using var frame = server.Readback();
                    if (frames == 1)
                    {
                        Pixel(frame, 2, 2, Colors.Black);
                        Pixel(frame, 12, 12, Colors.Red);
                        Pixel(frame, 32, 24, new Color(0.5f, 0.5f, 0, 1));
                        Pixel(frame, 94, 14, Colors.Blue);
                        Pixel(frame, 20, 64, Colors.Yellow);
                        Check(red.Draws == 1 && hidden.Draws == 0, "Only visible nodes prepare drawing.");
                        first = (Image)frame.Duplicate();
                        red.Position = new Vector2(4, 0);
                    }
                    else if (frames == 2)
                    {
                        Pixel(frame, 9, 12, Colors.Black);
                        Pixel(frame, 14, 12, Colors.Red);
                        Check(red.Draws == 1, "Changing transform reuses retained local commands.");
                        red.QueueRedraw(); red.QueueRedraw();
                    }
                    else
                    {
                        Check(red.Draws == 2, "Redraw requests coalesce before the next frame.");
                        node.Tree!.Quit(7);
                    }
                };
            }
        };
        window.AddChild(hook);
        try
        {
            Check(Engine.Instance.Run(window) == 7 && frames == 3, "Scene frames submit and quit through Engine.Run.");
            Released(window);
            var output = Environment.GetEnvironmentVariable("ELECTRON2D_RENDER_OUTPUT");
            if (output is not null && first is not null)
            {
                Directory.CreateDirectory(output);
                File.WriteAllBytes(Path.Combine(output, expectedBackend + ".rgba"), first.GetData());
                File.WriteAllText(Path.Combine(output, expectedBackend + ".size"), $"{first.Width} {first.Height}");
            }
        }
        finally { first?.Dispose(); }
    }

    private static void VerifyFrameAllocations(string backend)
    {
        using var image = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8);
        image.Fill(Colors.White);
        using var texture = ImageTexture.CreateFromImage(image);
        using var hlsl = LoadShader("TextureHlsl");
        using var glsl = LoadShader("TextureGlsl");
        using var first = new ShaderMaterial { Shader = hlsl };
        using var second = new ShaderMaterial { Shader = glsl };
        foreach (var material in new[] { first, second })
        {
            material.Shader!.SetDefaultTextureParameter("colorMap", texture);
            material.SetShaderParameter("detailMap", texture);
            material.SetShaderParameter("tint", Colors.White);
        }
        var window = new Window { Size = new Vector2I(96, 80) };
        var sorted = new Entity { YSortEnabled = true };
        var island = new Entity();
        var nested = new Entity { YSortEnabled = true };
        window.AddChild(sorted); sorted.AddChild(island); island.AddChild(nested);
        for (var i = 0; i < 8; i++)
            (i % 2 == 0 ? sorted : nested).AddChild(new CanvasNode
            {
                Name = "canvas" + i,
                Position = new Vector2(i * 8, 0),
                ZIndex = (7 - i) % 3,
                Material = backend == "gpu" ? (i % 3 == 0 ? first : i % 3 == 1 ? second : null) : null,
                DrawAction = n =>
                {
                    n.DrawRect(new Rect(0, 0, 8, 8), Colors.Red);
                    n.DrawTextureRect(texture, new Rect(0, 12, 8, 8), false);
                }
            });
        var frames = 0;
        long before = 0, allocated = 0;
        window.AddChild(new CanvasNode
        {
            ReadyAction = n =>
        {
            var server = RenderingServer.Instance!;
            server.FramePreDraw += () => before = GC.GetAllocatedBytesForCurrentThread();
            server.FramePostDraw += () =>
            {
                var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
                if (++frames > 20) allocated += bytes;
                if (frames == 40) n.Tree!.Quit();
            };
        }
        });
        Engine.Instance.Run(window);
        Released(window);
        Check(frames == 40 && allocated == 0,
            $"Warmed {backend} canvas preparation, sorting and native submission allocated {allocated} bytes in 20 frames.");
    }

    private static void VerifyFailure(string backend, string fixture)
    {
        using var shader = LoadShader(fixture);
        using var material = new ShaderMaterial { Shader = shader };
        using var binaryShader = Shader.CreateFromSPIRV(shader.GetSPIRV());
        material.Shader = binaryShader;
        var window = new Window { Size = new Vector2I(96, 80) };
        window.AddChild(new CanvasNode
        {
            Material = material,
            DrawAction = n => n.DrawRect(new Rect(0, 0, 48, 48), Colors.Red),
            ReadyAction = n =>
        {
            RenderingServer.Instance!.FramePostDraw += () =>
            {
                using var frame = RenderingServer.Instance.Readback();
                Pixel(frame, 16, 16, Colors.Blue);
                n.Tree!.Quit();
            };
        }
        });
        if (backend == "compatibility") Reject<NotSupportedException>(() => Engine.Instance.Run(window));
        else Engine.Instance.Run(window);
        Released(window);
        Check(!shader.IsDisposed && !material.IsDisposed, "Window cleanup does not dispose borrowed shading resources.");
        window = new Window();
        window.AddChild(new CanvasNode { DrawAction = _ => throw new InvalidOperationException("injected draw failure") });
        Reject<InvalidOperationException>(() => Engine.Instance.Run(window));
        Released(window);
    }

    private static void VerifyParameters(string fixture)
    {
        using var shader = LoadShader(fixture);
        using var material = new ShaderMaterial { Shader = shader };
        using var empty = new ShaderMaterial();
        Reject<InvalidOperationException>(() => empty.SetShaderParameter("gain", 1f));
        Check(shader.GetShaderUniformList().Count == 7, "Both languages reflect all seven material parameters.");
        Check(material.GetPropertyList().Count(p => p.Name.StartsWith("shader_parameter/", StringComparison.Ordinal)) == 7,
            "Material property discovery keeps shader uniforms separate from resource properties.");
        Check(material.GetShaderParameter<float>("gain") == 0, "Uniforms are initialized to zero.");
        Initialize(material, Colors.Red);
        Reject<ArgumentException>(() => material.SetShaderParameter("Gain", 1f));
        Reject<ArgumentException>(() => material.SetShaderParameter("gain", 1));
        Reject<ArgumentException>(() => material.SetShaderParameter("mode", 1f));
        Reject<ArgumentException>(() => material.SetShaderParameter("enabled", 1));
        Reject<ArgumentException>(() => material.SetShaderParameter("gain", float.NaN));
        Reject<ArgumentException>(() => material.SetShaderParameter("tint", new Color(1, 1, float.PositiveInfinity, 1)));
        Reject<ArgumentException>(() => material.SetShaderParameter("weights", 1f));
        Reject<ArgumentException>(() => material.SetShaderParameter<float>("weights", new float[] { 1 }.AsSpan()));
        Reject<ArgumentException>(() => material.SetShaderParameter<float>("weights", new float[] { 1, float.NaN }.AsSpan()));
        Reject<ArgumentException>(() => material.SetShaderParameter<double>("weights", new double[] { 1, 2 }.AsSpan()));
        Check(material.GetShaderParameterArray<float>("weights").SequenceEqual(new float[] { 0.25f, 0.75f }), "Invalid writes preserve all array elements.");
        var array = material.GetShaderParameterArray<float>("weights"); array[0] = 42;
        Check(material.GetShaderParameterArray<float>("weights")[0] == 0.25f, "Returned arrays do not alias material storage.");
        var tint = (PropertyDescriptor<ShaderMaterial, Vector4>)shader.GetShaderUniformList().Single(p => p.Name == "tint");
        tint.SetValue(material, new Vector4(0, 1, 0, 1));
        Check(material.GetShaderParameter<Color>("tint") == Colors.Green, "Tooling descriptors and Color aliases use the same parameter storage.");
        using var copy = (ShaderMaterial)material.Duplicate(true);
        using var copiedShader = copy.Shader!;
        material.SetShaderParameter("tint", Colors.Blue);
        material.SetShaderParameter<float>("weights", new float[] { 1, 2 }.AsSpan());
        Check(copy.GetShaderParameter<Color>("tint") == Colors.Green && copy.GetShaderParameterArray<float>("weights")[0] == 0.25f,
            "Material duplication copies uniform values independently.");
        using var reordered = LoadShader("MaterialReordered");
        shader.SetSPIRV(reordered.GetSPIRV());
        Check(material.GetShaderParameter<Color>("tint") == Colors.Blue && material.GetShaderParameterArray<float>("weights")[1] == 2,
            "Reload preserves named values across member offsets and buffer binding changes.");
        material.Shader = copiedShader;
        Check(material.GetShaderParameter<Color>("tint") == Colors.Blue, "Assigning another shader preserves matching named parameter values.");
        using var replaceTarget = new ShaderMaterial { Shader = reordered };
        replaceTarget.CopyFromResource(copy);
        Check(replaceTarget.GetShaderParameter<Color>("tint") == Colors.Green, "CopyFromResource copies the material parameter state.");
        var malformed = shader.GetSPIRV();
        var words = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, uint>(malformed);
        for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
            if ((words[at] & 0xffff) == 71 && (words[at] >> 16) == 4 && words[at + 2] == 34) words[at + 3] = 0;
        Reject<NotSupportedException>(() => shader.SetSPIRV(malformed));
        Check(shader.GetSPIRV().SequenceEqual(reordered.GetSPIRV()), "An invalid descriptor set does not replace the shader.");
        for (var i = 0; i < 2000; i++) material.SetShaderParameter("gain", 1f);
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 2000; i++)
        {
            material.SetShaderParameter("gain", 0.5f);
            material.SetShaderParameter("tint", Colors.Blue);
            _ = material.GetShaderParameter<float>("gain");
            _ = material.GetCanvasState();
        }
        Check(GC.GetAllocatedBytesForCurrentThread() == allocated, "Steady parameter updates and material capture allocate no managed memory.");
        copiedShader.Dispose();
        Reject<ObjectDisposedException>(() => material.GetShaderParameter<float>("gain"));
        material.Shader = null;
        Reject<InvalidOperationException>(() => material.GetShaderParameter<float>("gain"));
        material.Dispose();
        Reject<ObjectDisposedException>(() => material.SetShaderParameter("gain", 1f));
    }

    private static void Initialize(ShaderMaterial material, Color tint)
    {
        material.SetShaderParameter("tint", tint);
        material.SetShaderParameter("gain", 1f);
        material.SetShaderParameter("mode", 0);
        material.SetShaderParameter("offset", Vector2.Zero);
        material.SetShaderParameter("shift", Vector2I.Zero);
        material.SetShaderParameter("enabled", 1u);
        material.SetShaderParameter<float>("weights", new float[] { 0.25f, 0.75f }.AsSpan());
    }

    private static void VerifyMaterialFrame(string fixture)
    {
        using var shader = LoadShader(fixture);
        using var reordered = LoadShader("MaterialReordered");
        using var first = new ShaderMaterial { Shader = shader };
        using var second = new ShaderMaterial { Shader = shader };
        Initialize(first, Colors.Red); Initialize(second, Colors.Green);
        first.SetShaderParameter("offset", new Vector2(10, 0));
        first.SetShaderParameter("shift", new Vector2I(2, 0));
        var window = new Window { Size = new Vector2I(96, 80) };
        var frames = 0;
        var left = new CanvasNode { Name = "left", Material = first, DrawAction = n => n.DrawRect(new Rect(0, 0, 40, 48), Colors.White) };
        var right = new CanvasNode { Name = "right", Material = second, DrawAction = n => n.DrawRect(new Rect(48, 0, 40, 48), Colors.White) };
        left.ReadyAction = n =>
        {
            RenderingServer.Instance!.SetDefaultClearColor(Colors.Black);
            RenderingServer.Instance.FramePostDraw += () =>
            {
                frames++;
                using var frame = RenderingServer.Instance.Readback();
                Pixel(frame, 8, 16, Colors.Black);
                Pixel(frame, 60, 16, Colors.Green);
                Pixel(frame, 16, 16, frames switch { 1 => Colors.Red, 2 => new Color(0, 0, 0.5f, 1), 3 => new Color(0, 0, 0.25f, 1), _ => new Color(0.25f, 0, 0, 1) });
                Check(left.Draws == 1 && right.Draws == 1, "Uniform updates reuse retained geometry.");
                if (frames == 1) { first.SetShaderParameter("tint", Colors.Blue); first.SetShaderParameter("gain", 0.5f); }
                else if (frames == 2)
                {
                    first.SetShaderParameter<float>("weights", new float[] { 0.5f, 0 }.AsSpan());
                    shader.SetSPIRV(reordered.GetSPIRV());
                }
                else if (frames == 3) first.SetShaderParameter("mode", 1);
                else n.Tree!.Quit();
            };
        };
        window.AddChild(left); window.AddChild(right);
        Engine.Instance.Run(window);
        Released(window);
        Check(frames == 4, "Both source languages execute material scalars, arrays, vectors and shader reload on the GPU.");
    }

    private static Shader LoadShader(string name)
    {
        using var input = typeof(RenderingRuntimeTests).Assembly.GetManifestResourceStream("TestShaders." + name + ".spv")!;
        using var memory = new MemoryStream();
        input.CopyTo(memory);
        return Shader.CreateFromSPIRV(memory.ToArray());
    }

    private static void Pixel(Image frame, int x, int y, Color expected)
    {
        var pixel = frame.GetPixel(x, y);
        Check(Math.Abs(pixel.R - expected.R) <= 2f / 255 && Math.Abs(pixel.G - expected.G) <= 2f / 255 &&
              Math.Abs(pixel.B - expected.B) <= 2f / 255 && Math.Abs(pixel.A - expected.A) <= 2f / 255,
            $"Pixel ({x},{y}): expected {expected}, got {pixel}.");
    }
    private static void Released(Window window) => Check(window.IsDisposed && Engine.Instance.MainLoop is null &&
        DisplayServer.Instance is null && RenderingServer.Instance is null, "All scene and graphics ownership is released.");
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
    private sealed class CanvasNode : Entity
    {
        internal Action<CanvasNode>? ReadyAction;
        internal Action<CanvasNode>? DrawAction;
        internal int Draws;
        protected override void OnReady() => ReadyAction?.Invoke(this);
        protected override void OnDraw() { Draws++; DrawAction?.Invoke(this); }
    }
}
