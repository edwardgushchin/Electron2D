using System.Diagnostics;
using System.Runtime.InteropServices;
using Electron2D;
using Kind = Electron2D.RenderingServer.GlobalShaderParameterType;

internal static class ShaderGlobalParametersTests
{
    private static readonly float[] Matrix3Value = [1, 0, 0, 0, 1, 0, .25f, 0, 1];
    private static readonly float[] Matrix4Value = [1, 0, 0, .125f, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1];
    internal static void RunManaged()
    {
        var globals = new ShaderGlobalParameters(); globals.Add("gain", Kind.Float, .5f);
        globals.SetOverride<float>("gain", .75f); globals.Set("gain", .25f); Check(globals.Get<float>("gain") == .25f, "Base reads ignore an active override.");
        Reject<ArgumentException>(() => globals.Add("gain", Kind.Float, .1f));
        Reject<ArgumentException>(() => globals.Add("bad", Kind.UInt, -1));
        Reject<ArgumentException>(() => globals.Add("TIME", Kind.Float, 1f));
        Reject<ArgumentException>(() => globals.Set("gain", float.NaN));
        Reject<ArgumentException>(() => globals.Get<int>("gain"));
        Check(globals.GetType("absent") == Kind.Max && globals.Get<float>("gain") == .25f, "Rejected edits preserve declared state.");
        globals.AddMatrix("matrix3", Kind.Mat3, Matrix3Value); globals.AddMatrix("matrix4", Kind.Mat4, Matrix4Value);
        var exported = globals.GetMatrix("matrix3"); exported[0] = 9; Check(globals.GetMatrix("matrix3")[0] == 1, "Matrix getters copy base storage.");
        Span<float> wrong = stackalloc float[8]; wrong.Fill(7); RejectMatrixDestination(globals, wrong); Check(wrong[0] == 7, "Rejected matrix copies preserve destination.");
        var invalid = (float[])Matrix4Value.Clone(); invalid[15] = float.PositiveInfinity; Reject<ArgumentException>(() => globals.SetMatrix("matrix4", invalid));
        var code = Module("GLSL", "frag"); using var shader = Shader.CreateFromSPIRV(Module("GLSL", "vert"), code);
        var program = shader.GetProgram(); Check(program.Uniforms.Count == 0 && program.GlobalUniforms.Count == 23 && shader.GetShaderUniformList().Count == 0, "Globals never become material descriptors.");
        using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); image.Fill(Colors.White); using var texture = ImageTexture.CreateFromImage(image);
        using var array = new TextureArray(); array.CreateFromImages([image]);
        using var duplicate = (Shader)shader.Duplicate();
        Check(duplicate.GetProgram().GlobalUniforms.Count == 23 && duplicate.GetShaderUniformList().Count == 0, "Duplication retains global roles without material aliases.");
        var folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "global-shader-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            var path = System.IO.Path.Combine(folder, "global.e2dres"); ResourceSaver.Save(shader, path);
            var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
            start.ArgumentList.Add(typeof(ShaderGlobalParametersTests).Assembly.Location); start.ArgumentList.Add("--global-shader-archive"); start.ArgumentList.Add(path); start.Environment.Remove("ELECTRON2D_TEST_SHADER_GLOBALS");
            using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var errors = child.StandardError.ReadToEndAsync();
            if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Fresh global shader archive deadline."); }
            Check(child.ExitCode == 0, "Fresh global shader archive: " + output.GetAwaiter().GetResult() + errors.GetAwaiter().GetResult());
        }
        finally { Directory.Delete(folder, true); }
        Populate(globals, texture, array); globals.Validate(program);
        var buffers = program.BufferSizes.Select(n => new byte[n]).ToArray(); globals.Write(program, buffers);
        var matrix = program.GlobalUniforms["affine"]; var data = MemoryMarshal.Cast<byte, float>(buffers[matrix.Buffer].AsSpan(matrix.Offset, matrix.ElementSize));
        Check(matrix.RowMajor && data[2] == 24 && data[matrix.MatrixStride / 4 + 1] == 1 && data[2 * matrix.MatrixStride / 4 + 2] == 1, "Full planar origin reaches a row-major homogeneous matrix.");
        var gain = program.GlobalUniforms["gain"]; Check(gain.Read<float>(buffers[gain.Buffer].AsSpan(gain.Offset, gain.ElementSize)) == .75f, "Rendering uses the override while reads keep the base.");
        Check(ReferenceEquals(globals.Resolve(program.Textures[0]), texture), "Global sampled resources retain borrowed identity.");
        globals.SetMatrix("matrix3", Matrix3Value, true);
        for (var i = 0; i < 64; i++) { globals.Write(program, buffers); globals.SetMatrix("matrix3", Matrix3Value, true); }
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1024; i++) { globals.Set("gain", .5f); globals.SetOverride<float>("gain", (i & 1) == 0 ? .75f : null); globals.SetMatrix("matrix3", (i & 1) == 0 ? Matrix3Value : [], true); globals.Write(program, buffers); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "1024 warmed value/override/matrix writes allocate zero owner managed bytes.");
        globals.Remove("gain"); Reject<ArgumentException>(() => globals.Validate(program)); globals.Remove("gain"); Check(!texture.IsDisposed, "Removing globals does not dispose borrowed images.");
        Console.WriteLine("Global shader base/override/type/copy/layout/ownership and 1024 zero-byte updates passed.");
    }
    private static void RejectMatrixDestination(ShaderGlobalParameters globals, Span<float> destination)
    { try { globals.GetMatrix("matrix3", destination); } catch (ArgumentException) { return; } throw new Exception("Invalid matrix destination accepted."); }

    private static void Populate(ShaderGlobalParameters g, Texture texture, TextureLayered array)
    {
        g.Add("enabled", Kind.Bool, true); g.Add("mask2", Kind.BVec2, 3); g.Add("mask3", Kind.BVec3, 5); g.Add("mask4", Kind.BVec4, 15);
        g.Add("signedValue", Kind.Int, -7); g.Add("signed2", Kind.IVec2, new Vector2i(-2, 3)); g.Add("signed3", Kind.IVec3, new Vector3i(-4, 5, -6)); g.Add("signed4", Kind.IVec4, new Vector4i(1, 2, 3, 4));
        g.Add("rectangleI", Kind.Rect2i, new Rect2i(5, 6, 7, 8)); g.Add("unsignedValue", Kind.UInt, uint.MaxValue - 1);
        g.Add("unsigned2", Kind.UVec2, new Vector2i(-1, int.MinValue)); g.Add("unsigned3", Kind.UVec3, new Vector3i(1, 2, int.MinValue)); g.Add("unsigned4", Kind.UVec4, new Vector4i(1, 2, 3, 123));
        g.Add("offset", Kind.Vec2, new Vector2(16, 0)); g.Add("rgb", Kind.Vec3, new Vector3(.5f, .25f, .125f)); g.Add("wide", Kind.Vec4, Vector4.One); g.Add("tint", Kind.Color, new Color(1, .5f, .25f, 1));
        g.Add("rectangle", Kind.Rect2, new Rect2(5, 6, 7, 8)); g.Add("basis", Kind.Mat2, Transform.Identity); g.Add("affine", Kind.Transform2D, new Transform(0, new Vector2(24, 0)));
        g.AddTexture("map", texture, false); g.AddTexture("layers", array, true);
    }
    internal static void PopulatePublic(Texture texture, TextureLayered array)
    {
        var seed = new ShaderGlobalParameters(); seed.Add("gain", Kind.Float, .5f); seed.AddMatrix("matrix3", Kind.Mat3, Matrix3Value); seed.AddMatrix("matrix4", Kind.Mat4, Matrix4Value); Populate(seed, texture, array);
        RenderingServer.GlobalShaderParameterAdd("gain", Kind.Float, .5f);
        RenderingServer.GlobalShaderParameterAdd("matrix3", Kind.Mat3, Matrix3Value); RenderingServer.GlobalShaderParameterAdd("matrix4", Kind.Mat4, Matrix4Value);
        foreach (var name in seed.Names())
        {
            var type = seed.GetType(name);
            switch (type)
            {
                case Kind.Bool: RenderingServer.GlobalShaderParameterAdd(name, type, seed.Get<bool>(name)); break;
                case Kind.BVec2 or Kind.BVec3 or Kind.BVec4 or Kind.Int: RenderingServer.GlobalShaderParameterAdd(name, type, seed.Get<int>(name)); break;
                case Kind.UInt: RenderingServer.GlobalShaderParameterAdd(name, type, seed.Get<uint>(name)); break;
                case Kind.IVec2 or Kind.UVec2: RenderingServer.GlobalShaderParameterAdd(name, type, seed.Get<Vector2i>(name)); break;
                case Kind.IVec3 or Kind.UVec3: RenderingServer.GlobalShaderParameterAdd(name, type, seed.Get<Vector3i>(name)); break;
                case Kind.IVec4 or Kind.UVec4: RenderingServer.GlobalShaderParameterAdd(name, type, seed.Get<Vector4i>(name)); break;
                case Kind.Rect2i: RenderingServer.GlobalShaderParameterAdd(name, type, seed.Get<Rect2i>(name)); break;
                case Kind.Vec2: RenderingServer.GlobalShaderParameterAdd(name, type, seed.Get<Vector2>(name)); break;
                case Kind.Vec3: RenderingServer.GlobalShaderParameterAdd(name, type, seed.Get<Vector3>(name)); break;
                case Kind.Vec4: RenderingServer.GlobalShaderParameterAdd(name, type, seed.Get<Vector4>(name)); break;
                case Kind.Color: RenderingServer.GlobalShaderParameterAdd(name, type, seed.Get<Color>(name)); break;
                case Kind.Rect2: RenderingServer.GlobalShaderParameterAdd(name, type, seed.Get<Rect2>(name)); break;
                case Kind.Mat2 or Kind.Transform2D: RenderingServer.GlobalShaderParameterAdd(name, type, seed.Get<Transform>(name)); break;
                case Kind.Sampler2D: RenderingServer.GlobalShaderParameterAdd(name, texture); break;
                case Kind.Sampler2DArray: RenderingServer.GlobalShaderParameterAdd(name, array); break;
            }
        }
    }
    internal static void RunNative(string profile)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var language = profile.Contains("hlsl", StringComparison.Ordinal) ? "HLSL" : "GLSL";
        using var shader = Shader.CreateFromSPIRV(Module(language, "vert"), Module(language, "frag"));
        using var first = new ShaderMaterial { Shader = shader }; using var second = new ShaderMaterial { Shader = shader };
        using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); image.Fill(Colors.White);
        using var texture = ImageTexture.CreateFromImage(image); using var array = new TextureArray(); array.CreateFromImages([image]);
        image.Fill(Colors.Red); using var red = ImageTexture.CreateFromImage(image); using var redArray = new TextureArray(); redArray.CreateFromImages([image]);
        image.Fill(Colors.Black); using var black = ImageTexture.CreateFromImage(image);
        var window = new Window { Size = new(96, 48), UseHDR2D = profile.Contains("hdr", StringComparison.Ordinal), MSAA2D = profile.Contains("msaa", StringComparison.Ordinal) ? Viewport.MSAA.Samples4X : Viewport.MSAA.Disabled };
        window.AddChild(new Box { Position = new(8, 8), Material = first }); window.AddChild(new Box { Position = new(48, 8), Material = second });
        var frame = 0; long before = 0, active = 0, idle = 0;
        window.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingMethod() == "gpu", "Requested programmable renderer is effective."); PopulatePublic(texture, array); RenderingServer.SetDefaultClearColor(Colors.Black);
            Check(RenderingServer.GlobalShaderParameterGetList().Length == 25 && RenderingServer.GlobalShaderParameterGetType("absent") == Kind.Max, "Public registry lists all numeric and sampled roles.");
            Reject<InvalidOperationException>(() => Task.Run(() => RenderingServer.GlobalShaderParameterSet("gain", 1f)).GetAwaiter().GetResult());
            Reject<ArgumentException>(() => RenderingServer.GlobalShaderParameterSet("gain", float.NaN));
            window.Tree!.ProcessFrameStarted += _ =>
            {
                before = GC.GetAllocatedBytesForCurrentThread();
                if (frame is >= 16 and < 80) { RenderingServer.GlobalShaderParameterSet("gain", .5f); RenderingServer.GlobalShaderParameterSetOverride<float>("gain", (frame & 1) == 0 ? .75f : null); RenderingServer.GlobalShaderParameterSetMatrixOverride("matrix3", (frame & 1) == 0 ? Matrix3Value : []); }
            };
            RenderingServer.FramePostDraw += () =>
            {
                if (frame is >= 16 and < 80) active += GC.GetAllocatedBytesForCurrentThread() - before;
                if (frame is >= 80 and < 144) idle += GC.GetAllocatedBytesForCurrentThread() - before;
                frame++;
                if (frame == 144) { Check(active == 0 && idle == 0, $"Global frame allocations: active={active}, idle={idle}."); window.Tree!.Quit(); }
                if (frame > 10) return;
                var gain = frame is 2 or 3 ? .75f : frame >= 4 ? .25f : .5f;
                using var pixels = window.GetTexture().GetImage()!;
                var x = 8 + 24 + (int)(16 * gain) + 4;
                Check(pixels.GetPixel(8 + (int)(16 * gain) + 4, 12).R < .01f, "Full planar origin moves pixels beyond the old geometry.");
                foreach (var px in new[] { x, x + 40 })
                {
                    var color = pixels.GetPixel(px, 12); var expected = frame == 7 ? 0 : gain * gain; var colored = frame is 5 or 6 or 8;
                    Check(MathF.Abs(color.R - expected) < .01f && MathF.Abs(color.G - expected * (colored ? 0 : .5f)) < .01f && MathF.Abs(color.B - expected * (colored ? 0 : .25f)) < .01f, $"Global {profile} frame {frame} pixel {px}: {color}, expected {expected}.");
                }
                if (frame == 1) RenderingServer.GlobalShaderParameterSetOverride<float>("gain", .75f);
                if (frame == 2) { RenderingServer.GlobalShaderParameterSet("gain", .25f); Check(RenderingServer.GlobalShaderParameterGet<float>("gain") == .25f, "Base updates remain readable under overrides."); }
                if (frame == 3) RenderingServer.GlobalShaderParameterSetOverride<float>("gain", null);
                if (frame == 4)
                {
                    RenderingServer.GlobalShaderParameterSetOverride("map", red);
                    Check(ReferenceEquals(RenderingServer.GlobalShaderParameterGetTexture("map"), texture), "Sampled base reads ignore overrides.");
                }
                if (frame == 5) RenderingServer.GlobalShaderParameterSet("map", black);
                if (frame == 6) RenderingServer.GlobalShaderParameterSetOverride("map", (Texture?)null);
                if (frame == 7) { RenderingServer.GlobalShaderParameterSet("map", texture); RenderingServer.GlobalShaderParameterSetOverride("layers", redArray); }
                if (frame == 8) { RenderingServer.GlobalShaderParameterSetOverride("layers", (TextureLayered?)null); Check(ReferenceEquals(RenderingServer.GlobalShaderParameterGetLayeredTexture("layers"), array), "Array overrides restore borrowed base storage."); }
                if (frame == 9) shader.SetSPIRV(shader.GetSPIRV(RenderingDevice.ShaderStage.Vertex), shader.GetSPIRV());

            };
        };
        try { Check(Engine.Run(window) == 0 && frame == 144, "Global shader native lifecycle completed."); }
        finally { if (!window.IsDisposed) window.Dispose(); }
        Check(!texture.IsDisposed && !array.IsDisposed && !RenderingServer.IsAvailable, "Session retirement retains borrowed resources.");
        Reject<InvalidOperationException>(() => RenderingServer.GlobalShaderParameterGet<float>("gain"));
        Console.WriteLine($"Global shader {profile} pixels, 25 typed roles, shared stages/materials and 64 active={active} B/64 idle={idle} B passed.");
    }
    internal static void RunArchive(string path)
    {
        using var restored = ResourceLoader.Load<Shader>(path, ResourceLoader.CacheMode.Ignore);
        Check(!RenderingServer.IsAvailable && restored.GetProgram().GlobalUniforms.Count == 23 && restored.GetProgram().Vertex is not null && restored.GetShaderUniformList().Count == 0, "A fresh process restores paired global declarations independently of the renderer registry.");
    }
    private sealed class Box : Entity { protected override void OnDraw() => DrawRect(new(0, 0, 16, 16), Colors.White); }
    internal static byte[] Module(string language, string stage)
    { using var stream = typeof(ShaderGlobalParametersTests).Assembly.GetManifestResourceStream($"TestShaders.GlobalParameters{language}.{stage}.spv")!; using var output = new MemoryStream(); stream.CopyTo(output); return output.ToArray(); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new Exception("Expected " + typeof(T).Name); }
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
}
