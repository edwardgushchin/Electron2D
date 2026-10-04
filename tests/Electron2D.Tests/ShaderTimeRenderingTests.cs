using System.Runtime.InteropServices;
using Electron2D;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyShaderTimeContract(string fixture)
    {
        using var shader = LoadShader(fixture);
        using var material = new ShaderMaterial { Shader = shader };
        Check(shader.GetShaderUniformList().Select(p => p.Name).Order().SequenceEqual(new[] { "gain", "time", "tint" }), "TIME is reserved; lowercase time remains a material parameter.");
        Check(material.GetPropertyList().Count(p => p.Name.StartsWith("shader_parameter/", StringComparison.Ordinal)) == 3, "TIME is not a stored material property.");
        Reject<ArgumentException>(() => material.SetShaderParameter("TIME", 2f));
        Reject<ArgumentException>(() => material.GetShaderParameter<float>("TIME"));
        Reject<ArgumentException>(() => material.SetShaderParameter<float>("TIME", new float[] { 1 }.AsSpan()));
        Reject<ArgumentException>(() => material.GetShaderParameterArray<float>("TIME"));
        Reject<ArgumentException>(() => material.SetShaderParameter("TIME", (Texture?)null));
        Reject<ArgumentException>(() => shader.SetDefaultTextureParameter("TIME", null));
        material.SetShaderParameter("time", .5f);
        var state = material.GetCanvasState()!; var clock = state.Program.TimeUniform!; var value = 42f;
        MemoryMarshal.Write(state.Buffers[clock.Buffer].AsSpan(clock.Offset), in value);
        using var copy = (ShaderMaterial)material.Duplicate();
        var copied = copy.GetCanvasState()!;
        Check(MemoryMarshal.Read<float>(copied.Buffers[clock.Buffer].AsSpan(clock.Offset)) == 0 && copy.GetShaderParameter<float>("time") == .5f, "Duplication copies user values, not the last uploaded frame time.");
        using var reordered = LoadShader("TimeReordered");
        shader.SetSPIRV(reordered.GetSPIRV());
        state = material.GetCanvasState()!; clock = state.Program.TimeUniform!;
        Check(clock.Buffer == 1 && clock.Offset == 0 && material.GetShaderParameter<float>("time") == .5f, "Reload resolves TIME in its new binding while preserving user parameters.");
        using var integer = LoadShader("MaterialHlsl");
        var malformed = integer.GetSPIRV(); var words = MemoryMarshal.Cast<byte, uint>(malformed.AsSpan()); var renamed = false;
        for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
            if ((words[at] & 0xffff) == 6 && (words[at] >> 16) == 5 && words[at + 3] == 0x65646f6d)
            { words[at + 3] = 0x454d4954; renamed = true; }
        Check(renamed, "Malformed TIME fixture renames an actual integer member.");
        Reject<NotSupportedException>(() => Shader.CreateFromSPIRV(malformed));
        Reject<NotSupportedException>(() => shader.SetSPIRV(malformed));
        Check(shader.GetSPIRV().SequenceEqual(reordered.GetSPIRV()), "Rejected TIME replacement preserves the previous program.");
        using var only = LoadShader("TimeOnly");
        Check(only.GetShaderUniformList().Count == 0 && only.GetProgram().BufferSizes.Length == 1, "TIME-only programs still retain their upload buffer.");
        Console.WriteLine($"Shader TIME reflection, guards, copies and reload passed: {fixture}.");
    }

    private static void VerifyShaderTimeFrame(string backend, string fixture)
    {
        var settings = ProjectSettings.Service; var oldPeriod = ProjectSettings.Get(ProjectSettings.RenderingTimeRolloverSeconds); var oldScale = Engine.TimeScale;
        using var shader = LoadShader(fixture); using var reordered = LoadShader("TimeReordered"); using var only = LoadShader("TimeOnly");
        using var material = new ShaderMaterial { Shader = shader };
        material.SetShaderParameter("tint", Colors.White); material.SetShaderParameter("gain", .25f); material.SetShaderParameter("time", .5f);
        using var copy = (ShaderMaterial)material.Duplicate(); copy.SetShaderParameter("gain", .75f);
        var nodes = new[] { new CanvasNode { Name = "first", Material = material }, new CanvasNode { Name = "copy", Material = copy, Position = new(16, 0) }, new CanvasNode { Name = "shared", Material = material, Position = new(32, 0) } };
        var window = new Window { Size = new(64, 32) }; foreach (var node in nodes) { node.DrawAction = n => n.DrawRect(new(0, 0, 12, 12), Colors.White); window.AddChild(node); }
        var frames = 0; var previous = 0d; var before = 0L; var allocated = 0L; var changed = 0;
        try
        {
            Engine.TimeScale = 1; ProjectSettings.Set(ProjectSettings.RenderingTimeRolloverSeconds, 3600d);
            window.Ready += _ =>
            {
                var server = RenderingServer.Service!; RenderingServer.SetDefaultClearColor(Colors.Black);
                Check(server.CanvasTime == 0, "A restarted renderer has a fresh shader clock.");
                RenderingServer.FramePreDraw += () => before = GC.GetAllocatedBytesForCurrentThread();
                RenderingServer.FramePostDraw += () =>
                {
                    var bytes = GC.GetAllocatedBytesForCurrentThread() - before; if (++frames > 20) allocated += bytes;
                    var time = server.CanvasTime; var phase = (float)time * 4 % 1;
                    using var frame = server.Readback();
                    Pixel(frame, 4, 4, new(phase, frames == 8 ? 0 : .25f, frames == 8 ? 0 : .5f, 1));
                    Pixel(frame, 20, 4, new(phase, .75f, .5f, 1)); Pixel(frame, 36, 4, frame.GetPixel(4, 4));
                    if (time != previous) changed++;
                    if (frames == 2) Check(time == previous, "TimeScale zero freezes shader TIME.");
                    if (frames == 3) Check(window.Tree!.Paused && time > previous, "Paused scenes still advance shader TIME.");
                    if (frames == 4) Check(time < .05, "Live base rollover reaches shader TIME.");
                    if (frames == 5) Check(time < .09, "Live feature rollover reaches shader TIME.");
                    previous = time;
                    if (frames == 1) Engine.TimeScale = 0;
                    if (frames == 2) { Engine.TimeScale = 2; window.Tree!.Paused = true; }
                    if (frames == 3) { Engine.TimeScale = 1; window.Tree!.Paused = false; ProjectSettings.Set(ProjectSettings.RenderingTimeRolloverSeconds, .05); }
                    if (frames == 4) { ProjectSettings.AddCustomFeature("shader-time-test"); ProjectSettings.SetFeatureOverride(ProjectSettings.RenderingTimeRolloverSeconds, "shader-time-test", .09); }
                    if (frames == 5) shader.SetSPIRV(reordered.GetSPIRV());
                    if (frames == 6) { ProjectSettings.ClearFeatureOverride(ProjectSettings.RenderingTimeRolloverSeconds, "shader-time-test"); ProjectSettings.RemoveCustomFeature("shader-time-test"); ProjectSettings.Set(ProjectSettings.RenderingTimeRolloverSeconds, 3600d); }
                    if (frames == 7) material.Shader = only;
                    if (frames == 8) { material.Shader = shader; material.SetShaderParameter("tint", Colors.White); material.SetShaderParameter("gain", .25f); material.SetShaderParameter("time", .5f); }
                    if (frames == 40) { Check(changed > 30 && allocated == 0 && nodes.All(n => n.Draws == 1), "TIME updates without rerecording or warm allocations."); window.Tree!.Quit(); }
                };
            };
            if (backend == "compatibility") Reject<NotSupportedException>(() => Engine.Run(window));
            else Engine.Run(window);
            Released(window);
            Console.WriteLine(backend == "compatibility" ? $"Shader TIME fallback rejection and cleanup passed: {fixture}." :
                $"Shader TIME pixels and lifetime passed: {backend}/{fixture}, {frames} frames, {allocated} warm bytes.");
        }
        finally
        {
            ProjectSettings.ClearFeatureOverride(ProjectSettings.RenderingTimeRolloverSeconds, "shader-time-test"); ProjectSettings.RemoveCustomFeature("shader-time-test");
            ProjectSettings.Set(ProjectSettings.RenderingTimeRolloverSeconds, oldPeriod); Engine.TimeScale = oldScale;
        }
    }

    private static void VerifyShaderTimeOverflow()
    {
        var settings = ProjectSettings.Service; var oldPeriod = ProjectSettings.Get(ProjectSettings.RenderingTimeRolloverSeconds); var oldScale = Engine.TimeScale;
        using var shader = LoadShader("TimeOnly"); using var material = new ShaderMaterial { Shader = shader };
        var window = new Window(); var node = new CanvasNode { Material = material, DrawAction = n => n.DrawRect(new(0, 0, 12, 12), Colors.White) }; window.AddChild(node);
        try
        {
            Engine.TimeScale = 1e100; ProjectSettings.Set(ProjectSettings.RenderingTimeRolloverSeconds, double.MaxValue);
            try { Engine.Run(window); throw new Exception("Shader TIME overflow must fail before submission."); }
            catch (InvalidOperationException error) { Check(error.Message.Contains("float32", StringComparison.Ordinal), "Overflow reaches shader TIME validation."); }
            Released(window);
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingTimeRolloverSeconds, oldPeriod); Engine.TimeScale = oldScale; }
    }
}
