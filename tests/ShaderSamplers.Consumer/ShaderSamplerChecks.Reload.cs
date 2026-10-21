using Electron2D;
using Path = System.IO.Path;

internal static partial class ShaderSamplerChecks
{
    private static void ReloadAndCapacity()
    {
        using var shader = Load("CanvasGLSL"); using var state = new RDSamplerState { RepeatU = RenderingDevice.SamplerRepeatMode.Repeat };
        shader.SetTextureSamplerState("pixels", state); using var hlsl = Load("CanvasHLSL"); shader.SetSPIRV(hlsl.GetSPIRV());
        using var compatible = shader.GetTextureSamplerState("pixels")!; Check(compatible.RepeatU == state.RepeatU, "Cross-language compatible program replacement preserves sampler metadata.");
        Reject<ArgumentException>(() => shader.SetSPIRV(new byte[20]));
        using var retained = shader.GetTextureSamplerState("pixels")!; Check(retained.RepeatU == state.RepeatU, "Rejected program preserves sampler metadata.");
        using var array = Load("ArrayGLSL"); shader.SetSPIRV(array.GetSPIRV()); Check(shader.GetTextureSamplerState("pixels") is null, "Image shape replacement retires old sampler metadata.");
        shader.SetTextureSamplerState("pixels", state); using var integer = Load("IntegerGLSL"); shader.SetSPIRV(integer.GetSPIRV()); Check(shader.GetTextureSamplerState("pixels") is null, "Image scalar replacement retires metadata.");
        var vertex = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Shaders", "CapacityGLSL.vert.glsl.spv"));
        using var fragment = Load("CapacityGLSL"); using var capacity = Shader.CreateFromSPIRV(vertex, fragment.GetSPIRV());
        for (var i = 0; i < 16; i++) { state.MaxLOD = 100 + i; capacity.SetTextureSamplerState("v" + i, state); state.MaxLOD = 200 + i; capacity.SetTextureSamplerState("f" + i, state); }
        using var copy = (Shader)capacity.Duplicate();
        var folder = Directory.CreateTempSubdirectory("electron2d-sampler-capacity-").FullName;
        try
        {
            var path = Path.Combine(folder, "capacity.e2dres"); ResourceSaver.Save(copy, path);
            using var loaded = ResourceLoader.Load<Shader>(path, ResourceLoader.CacheMode.IgnoreDeep);
            for (var i = 0; i < 16; i++) { using var v = loaded.GetTextureSamplerState("v" + i)!; using var f = loaded.GetTextureSamplerState("f" + i)!; Check(v.MaxLOD == 100 + i && f.MaxLOD == 200 + i, "All 32 merged stage sampler settings persist."); }
        }
        finally { Directory.Delete(folder, true); }
        using var builtin = new Shader();
        var program = (PropertyDescriptor<Shader, byte[]>)builtin.GetPropertyList().Single(p => p.Name == "SPIRV");
        var marker = program.GetValue(builtin); Check(marker.Length == 8, "Built-in persistence uses an identity rather than the private module interface.");
        program.SetValue(builtin, builtin.GetSPIRV()); Check(builtin.GetMode() == Shader.Mode.CanvasItem, "Current legacy raw built-in archives remain readable.");
        Reject<Exception>(() => program.SetValue(builtin, marker.AsSpan(0, 7).ToArray()));
        var before = shader.GetTextureSamplerState("pixels"); before?.Dispose();
        shader.Changed += _ => throw new ApplicationException("sampler observer");
        Reject<ApplicationException>(() => shader.SetTextureSamplerState("pixels", state));
        using var committed = shader.GetTextureSamplerState("pixels")!; Check(committed.MaxLOD == state.MaxLOD, "Observer failure preserves committed sampler state.");
        Console.WriteLine("Sampler reload, scalar/shape retirement, 32 merged bindings, built-in archive and observer failures passed.");
    }
    private static void Compatibility()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "compatibility"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var shader = new Shader(); using var state = new RDSamplerState(); shader.SetTextureSamplerState("TEXTURE", state);
        using var material = new ShaderMaterial { Shader = shader }; using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); image.Fill(Colors.White);
        using var texture = ImageTexture.CreateFromImage(image); using var root = new Window { Size = new(48, 48) }; root.AddChild(new Quad(texture, 16) { Material = material });
        try { Engine.Run(root); throw new InvalidOperationException("Compatibility unexpectedly executed a shader material."); }
        catch (NotSupportedException) { }
        Check(root.IsDisposed && !RenderingServer.IsAvailable, "Unsupported compatibility shader execution cleans up.");
        using var retained = shader.GetTextureSamplerState("TEXTURE"); Check(retained is not null, "Backend rejection preserves authored metadata.");
        Console.WriteLine("Compatibility sampler configuration preserves authoring and explicitly rejects shader execution.");
    }
}
