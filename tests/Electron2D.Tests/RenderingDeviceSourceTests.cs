using System.Runtime.InteropServices;
using Electron2D;
using static TextureArrayTests;
using static Electron2D.RenderingDevice;
using IOPath = System.IO.Path;

internal static class RenderingDeviceSourceTests
{
    private static string Source(string name)
    { using var input = typeof(RenderingDeviceSourceTests).Assembly.GetManifestResourceStream("TestSources." + name)!; using var reader = new StreamReader(input); return reader.ReadToEnd(); }
    internal static void Run()
    {
        using var device = RenderingServer.CreateLocalRenderingDevice();
        Description(device); Failures(device); Cache(device);
        foreach (var language in new[] { ShaderLanguage.GLSL, ShaderLanguage.HLSL }) { Compute(device, language); Graphics(device, language); Includes(device, language); }
        Canvas();
        Console.WriteLine("Shader source authoring: both languages, stage diagnostics, bounded cache/bypass, changed includes, native compute/graphics/canvas and prepared zero-allocation recording passed on " + device.GetDeviceName());
    }
    private static void Description(RenderingDevice device)
    {
        using var source = new RDShaderSource(); Check(source.Language == ShaderLanguage.GLSL && source.SourceCompute == "" && source.SourceVertex == "" && source.SourceFragment == "", "Source descriptions preserve language/empty stage defaults.");
        source.SetStageSource(ShaderStage.Compute, "abc"); Check(source.GetStageSource(ShaderStage.Compute) == "abc", "Stage accessors share property state.");
        var property = (PropertyDescriptor<RDShaderSource, string>)source.GetPropertyList().Single(p => p.Name == "SourceCompute"); Check(property.GetValue(source) == "abc" && source.PropertyCanRevert(property), "Typed metadata exposes stage source."); source.RevertProperty(property); Check(source.SourceCompute == "", "Stage source reverts to empty.");
        using var empty = device.ShaderCompileSPIRVFromSource(source); Check(empty.BytecodeCompute.Length == 0 && empty.CompileErrorCompute == "", "Empty descriptions produce empty independent artifacts without compilation.");
        Reject<ArgumentOutOfRangeException>(() => source.GetStageSource((ShaderStage)2)); Reject<ArgumentOutOfRangeException>(() => source.SetStageSource(ShaderStage.Max, "a")); Reject<ArgumentNullException>(() => source.SourceCompute = null!); Reject<ArgumentNullException>(() => device.ShaderCompileSPIRVFromSource(null!));
        source.Language = (ShaderLanguage)99; Reject<ArgumentOutOfRangeException>(() => device.ShaderCompileSPIRVFromSource(source)); source.Language = ShaderLanguage.GLSL; source.SourceCompute = "x\0y"; Reject<ArgumentException>(() => device.ShaderCompileSPIRVFromSource(source)); source.SourceCompute = new string('x', 1_048_577); Reject<ArgumentException>(() => device.ShaderCompileSPIRVFromSource(source));
        source.Dispose(); Reject<ObjectDisposedException>(() => device.ShaderCompileSPIRVFromSource(source)); using var live = new RDShaderSource(); Reject<InvalidOperationException>(() => Task.Run(() => device.ShaderCompileSPIRVFromSource(live)).GetAwaiter().GetResult());
    }
    private static void Failures(RenderingDevice device)
    {
        var tool = Environment.GetEnvironmentVariable("ELECTRON2D_SHADER_IMPORT_TOOL")!;
        try
        {
            Environment.SetEnvironmentVariable("ELECTRON2D_SHADER_IMPORT_TOOL", tool + ".missing"); using var source = new RDShaderSource { SourceCompute = "a" }; Reject<NotSupportedException>(() => device.ShaderCompileSPIRVFromSource(source));
        }
        finally { Environment.SetEnvironmentVariable("ELECTRON2D_SHADER_IMPORT_TOOL", tool); }
        foreach (var language in new[] { ShaderLanguage.GLSL, ShaderLanguage.HLSL })
        {
            using var source = new RDShaderSource { Language = language, SourceVertex = Source("DevicePush" + language + ".vert." + (language == ShaderLanguage.GLSL ? "glsl" : "hlsl")), SourceFragment = "this is invalid source\n" };
            using var result = device.ShaderCompileSPIRVFromSource(source, false); Check(result.BytecodeVertex.Length > 20 && result.CompileErrorVertex == "" && result.BytecodeFragment.Length == 0 && result.CompileErrorFragment.Contains("Fragment", StringComparison.Ordinal) && result.CompileErrorFragment.Contains("fragment.", StringComparison.Ordinal), "A failed stage preserves its compiler source/stage location and successful sibling: " + language + " vertex=" + result.CompileErrorVertex + " fragment=" + result.CompileErrorFragment); Reject<ArgumentException>(() => device.ShaderCreateFromSPIRV(result));
        }
    }
    private static void Cache(RenderingDevice device)
    {
        if (!OperatingSystem.IsLinux()) return;
        var original = Environment.GetEnvironmentVariable("ELECTRON2D_SHADER_IMPORT_TOOL")!; var directory = Directory.CreateTempSubdirectory("electron2d-source-cache-test-").FullName; var wrapper = IOPath.Combine(directory, "frontend"); var log = IOPath.Combine(directory, "calls");
        static string Quote(string value) => "'" + value.Replace("'", "'\\''", StringComparison.Ordinal) + "'";
        File.WriteAllText(wrapper, "#!/bin/sh\nprintf 'call\\n' >> " + Quote(log) + "\nexec " + Quote(original) + " \"$@\"\n"); File.SetUnixFileMode(wrapper, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        try
        {
            Environment.SetEnvironmentVariable("ELECTRON2D_SHADER_IMPORT_TOOL", wrapper); using var source = new RDShaderSource { SourceCompute = Source("LocalCompute.comp.glsl") };
            using var first = device.ShaderCompileSPIRVFromSource(source); Check(first.CompileErrorCompute == "", first.CompileErrorCompute); var bytes = first.BytecodeCompute; first.BytecodeCompute = [];
            using var second = device.ShaderCompileSPIRVFromSource(source); Check(second.BytecodeCompute.SequenceEqual(bytes) && File.ReadAllLines(log).Length == 1, "Cache returns independent copied artifacts without executing the frontend again.");
            using var bypass = device.ShaderCompileSPIRVFromSource(source, false); Check(bypass.BytecodeCompute.SequenceEqual(bytes) && File.ReadAllLines(log).Length == 2, "Explicit cache bypass executes the frontend.");
            File.AppendAllText(wrapper, "# changed frontend content\n"); using var changed = device.ShaderCompileSPIRVFromSource(source); Check(changed.CompileErrorCompute == "" && File.ReadAllLines(log).Length == 3, "Compiler footprint changes invalidate cached modules.");
            var retained = device.GetType().GetField("_sourceCache", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            for (var i = 0; i < 17; i++) { source.SourceCompute = Source("LocalCompute.comp.glsl") + "\n// cache entry " + i; using var value = device.ShaderCompileSPIRVFromSource(source); Check(value.CompileErrorCompute == "", value.CompileErrorCompute); }
            Check(((System.Collections.IDictionary)retained.GetValue(device)!).Count <= 16, "Cold module cache bounds its retained entry count.");
            var noisy = IOPath.Combine(directory, "noisy"); File.WriteAllText(noisy, "#!/bin/sh\nyes diagnostic | head -c 1100000\nexit 1\n"); File.SetUnixFileMode(noisy, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute); Environment.SetEnvironmentVariable("ELECTRON2D_SHADER_IMPORT_TOOL", noisy); using var noise = device.ShaderCompileSPIRVFromSource(source); Check(noise.CompileErrorCompute.Length is > 1_000_000 and < 1_049_000, "Compiler diagnostics drain full pipes while retaining bounded text.");
            var slow = IOPath.Combine(directory, "slow"); File.WriteAllText(slow, "#!/bin/sh\nsleep 10\n"); File.SetUnixFileMode(slow, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            var compile = typeof(RenderingDevice).GetMethod("CompileSource", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!; var beforeTemporary = Directory.GetDirectories(IOPath.GetTempPath(), "electron2d-source-*").ToHashSet(); var watch = System.Diagnostics.Stopwatch.StartNew();
            try { compile.Invoke(null, [slow, ShaderLanguage.GLSL, ShaderStage.Compute, "a", directory, 30]); throw new InvalidOperationException("Expected compiler timeout."); }
            catch (System.Reflection.TargetInvocationException error) when (error.InnerException is TimeoutException) { }
            Check(watch.Elapsed.TotalSeconds < 5 && Directory.GetDirectories(IOPath.GetTempPath(), "electron2d-source-*").ToHashSet().SetEquals(beforeTemporary), "Compiler timeout kills its process tree and cleans staging files.");
            var bad = IOPath.Combine(directory, "bad"); File.WriteAllText(bad, "#!/bin/sh\nprintf 'not a module' > \"$3\"\n"); File.SetUnixFileMode(bad, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute); Environment.SetEnvironmentVariable("ELECTRON2D_SHADER_IMPORT_TOOL", bad); using var invalid = device.ShaderCompileSPIRVFromSource(source); Check(invalid.BytecodeCompute.Length == 0 && invalid.CompileErrorCompute.Contains("SPIR-V", StringComparison.Ordinal), "A malformed frontend artifact is reported and never cached.");
        }
        finally { Environment.SetEnvironmentVariable("ELECTRON2D_SHADER_IMPORT_TOOL", original); Directory.Delete(directory, true); }
    }
    private static void Compute(RenderingDevice device, ShaderLanguage language)
    {
        using var source = new RDShaderSource { Language = language, SourceCompute = Source(language == ShaderLanguage.GLSL ? "LocalCompute.comp.glsl" : "LocalComputeHLSL.comp.hlsl") }; using var module = device.ShaderCompileSPIRVFromSource(source); Check(module.CompileErrorCompute == "", module.CompileErrorCompute); var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader);
        float[] input = [1, 2, 3, 4]; var data = device.StorageBufferCreate(16, MemoryMarshal.AsBytes(input.AsSpan())); float[] settings = [2, 4, 3, 0]; var uniform = device.UniformBufferCreate(16, MemoryMarshal.AsBytes(settings.AsSpan()));
        using var storage = new RDUniform { Binding = 7, UniformType = UniformType.StorageBuffer }; storage.AddID(data); var dataSet = device.UniformSetCreate([storage], shader, 3); using var description = new RDUniform { Binding = 4, UniformType = UniformType.UniformBuffer }; description.AddID(uniform); var set = device.UniformSetCreate([description], shader, 2); float[] result = new float[4];
        var completedReads = 0; device.BufferGetDataAsync(data, _ => completedReads++); var activeList = device.ComputeListBegin();
        using (var duringList = device.ShaderCompileSPIRVFromSource(source)) Check(duringList.CompileErrorCompute == "" && completedReads == 0, "Authoring during recording neither completes GPU work nor invokes readback callbacks.");
        device.ComputeListEnd(); device.Submit(); device.Sync(); Check(completedReads == 1, "Explicit completion still delivers the queued readback.");
        source.SourceCompute = "invalid"; source.Dispose(); module.BytecodeCompute = []; module.Dispose();
        void Work()
        { device.BufferUpdate(data, 0, 16, MemoryMarshal.AsBytes(input.AsSpan())); var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, dataSet, 3); device.ComputeListBindUniformSet(list, set, 2); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd(); device.BufferGetData(data, MemoryMarshal.AsBytes(result.AsSpan())); }
        Work(); Check(result.SequenceEqual(new float[] { 5, 7, 9, 11 }), language + " source compilation reaches executable buffer/descriptor computation.");
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); for (var i = 0; i < 16; i++) Work(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Work(); var allocated = GC.GetAllocatedBytesForCurrentThread() - before; Check(allocated == 0, "Prepared source-produced compute recording allocates zero bytes: " + allocated); Console.WriteLine(language + " source compute: 32 cycles, " + allocated + " managed bytes.");
        using var specialized = new RDShaderSource { SourceCompute = Source("LocalSpecialization.comp.glsl") }; using var specializedModule = device.ShaderCompileSPIRVFromSource(specialized); Check(specializedModule.CompileErrorCompute == "", specializedModule.CompileErrorCompute); Check(device.ComputePipelineIsValid(device.ComputePipelineCreate(device.ShaderCreateFromSPIRV(specializedModule))), "Source-produced specialization templates retain the native pipeline path.");
    }
    private static RID Graphics(RenderingDevice device, ShaderLanguage language)
    {
        var suffix = language == ShaderLanguage.GLSL ? "glsl" : "hlsl"; using var source = new RDShaderSource { Language = language, SourceVertex = Source("DevicePush" + language + ".vert." + suffix), SourceFragment = Source("DevicePush" + language + ".frag." + suffix) }; using var module = device.ShaderCompileSPIRVFromSource(source); Check(module.CompileErrorVertex == "" && module.CompileErrorFragment == "", module.CompileErrorVertex + module.CompileErrorFragment); var shader = device.ShaderCreateFromSPIRV(module);
        using var format = new RDTextureFormat { Width = 32, Height = 16, Format = DataFormat.R8G8B8A8UNORM, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom | TextureUsageBits.Sampling }; using var view = new RDTextureView(); var image = device.TextureCreate(format, view); var target = device.FramebufferCreate([image]); using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] }; var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend);
        float[] ones = [1, 1, 1, 1]; var uniform = device.UniformBufferCreate(16, MemoryMarshal.AsBytes(ones.AsSpan())); using var description = new RDUniform { Binding = 0, UniformType = UniformType.UniformBuffer }; description.AddID(uniform); var set = device.UniformSetCreate([description], shader, 0); float[] payload = [1, 0, 0, 1, -.5f, 0, 0, 0]; var pixels = new byte[2048];
        var list = device.DrawListBegin(target, DrawFlags.ClearColorAll, [Colors.Black]); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 0); device.DrawListSetPushConstant(list, MemoryMarshal.AsBytes(payload.AsSpan()), 32); device.DrawListDraw(list, false, 1, 3); payload[0] = 0; payload[2] = 1; payload[4] = .5f; device.DrawListSetPushConstant(list, MemoryMarshal.AsBytes(payload.AsSpan()), 32); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd(); device.TextureGetData(image, 0, pixels.AsSpan());
        var left = (8 * 32 + 8) * 4; var right = (8 * 32 + 24) * 4; Check(pixels[left] == 255 && pixels[left + 2] == 0 && pixels[right] == 0 && pixels[right + 2] == 255, language + " source-produced vertex/fragment programs preserve push bytes, descriptors and pixels.");
        var directory = Environment.GetEnvironmentVariable("ELECTRON2D_DEVICE_SOURCE_OUTPUT"); if (directory is not null) { Directory.CreateDirectory(directory); using var proof = Electron2D.Image.CreateFromData(32, 16, false, Electron2D.Image.Format.Rgba8, pixels); proof.SavePNG(IOPath.Combine(directory, language + "-source.png")); File.WriteAllBytes(IOPath.Combine(directory, language + "-vertex.spv"), module.BytecodeVertex); File.WriteAllBytes(IOPath.Combine(directory, language + "-fragment.spv"), module.BytecodeFragment); }
        void Work()
        { var id = device.DrawListBegin(target); device.DrawListBindRenderPipeline(id, pipeline); device.DrawListBindUniformSet(id, set, 0); device.DrawListSetPushConstant(id, MemoryMarshal.AsBytes(payload.AsSpan()), 32); device.DrawListDraw(id, false, 1, 3); device.DrawListEnd(); device.TextureGetData(image, 0, pixels.AsSpan()); }
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); for (var i = 0; i < 16; i++) Work(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Work(); var allocated = GC.GetAllocatedBytesForCurrentThread() - before; Check(allocated == 0, "Prepared source-produced graphics allocates zero bytes: " + allocated); Console.WriteLine(language + " source graphics: 32 cycles, " + allocated + " managed bytes.");
        list = device.DrawListBegin(target); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 0); payload[0] = 1; payload[2] = 0; payload[4] = -.5f; device.DrawListSetPushConstant(list, MemoryMarshal.AsBytes(payload.AsSpan()), 32); device.DrawListDraw(list, false, 1, 3); payload[0] = 0; payload[2] = 1; payload[4] = .5f; device.DrawListSetPushConstant(list, MemoryMarshal.AsBytes(payload.AsSpan()), 32); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd(); return image;
    }
    private static float IncludedValue(RenderingDevice device, RDShaderSPIRV module)
    {
        var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader); float[] initial = [1, 0, 0, 0]; var buffer = device.StorageBufferCreate(16, MemoryMarshal.AsBytes(initial.AsSpan())); float[] values = [2, 1, 0, 0]; var uniform = device.UniformBufferCreate(16, MemoryMarshal.AsBytes(values.AsSpan()));
        using var storage = new RDUniform { Binding = 7, UniformType = UniformType.StorageBuffer }; storage.AddID(buffer); using var input = new RDUniform { Binding = 4, UniformType = UniformType.UniformBuffer }; input.AddID(uniform); var dataSet = device.UniformSetCreate([storage], shader, 3); var inputSet = device.UniformSetCreate([input], shader, 2);
        var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, dataSet, 3); device.ComputeListBindUniformSet(list, inputSet, 2); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd(); device.BufferGetData(buffer, MemoryMarshal.AsBytes(initial.AsSpan())); device.FreeRID(pipeline); device.FreeRID(shader); return initial[0];
    }
    private static void Includes(RenderingDevice device, ShaderLanguage language)
    {
        var projectRoot = ProjectSettings.GlobalizePath("res://"); var directory = IOPath.Combine(projectRoot, "bin", "source-include-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory); var file = IOPath.Combine(directory, "value.inc"); var includeName = IOPath.GetRelativePath(projectRoot, file).Replace(IOPath.DirectorySeparatorChar, '/');
        try
        {
            var sourceText = Source(language == ShaderLanguage.GLSL ? "LocalCompute.comp.glsl" : "LocalComputeHLSL.comp.hlsl").Replace("+ settings.z", "+ ADDEND", StringComparison.Ordinal);
            sourceText = language == ShaderLanguage.GLSL ? sourceText.Replace("#version 450", "#version 450\n#extension GL_GOOGLE_include_directive : require\n#include \"" + includeName + "\"", StringComparison.Ordinal) : "#include \"" + includeName + "\"\n" + sourceText;
            using var source = new RDShaderSource { Language = language, SourceCompute = sourceText }; File.WriteAllText(file, "#define ADDEND 3.0\n"); using var first = device.ShaderCompileSPIRVFromSource(source); Check(first.CompileErrorCompute == "", first.CompileErrorCompute); Check(IncludedValue(device, first) == 5, "Initial include produces its actual GPU arithmetic."); var timestamp = File.GetLastWriteTimeUtc(file); File.WriteAllText(file, "#define ADDEND 5.0\n"); File.SetLastWriteTimeUtc(file, timestamp); using var changed = device.ShaderCompileSPIRVFromSource(source); Check(changed.CompileErrorCompute == "" && !first.BytecodeCompute.SequenceEqual(changed.BytecodeCompute), language + " include-content changes with unchanged timestamp are recompiled."); Check(IncludedValue(device, changed) == 7, "Changed include produces updated GPU arithmetic."); File.WriteAllText(file, "invalid include text\n"); using var failed = device.ShaderCompileSPIRVFromSource(source); Check(failed.BytecodeCompute.Length == 0 && failed.CompileErrorCompute.Length != 0 && first.BytecodeCompute.Length > 20, "A failed include import preserves earlier returned artifacts.");
        }
        finally { Directory.Delete(directory, true); }
    }
    private sealed class Patch(Texture texture) : Entity { protected override void OnDraw() => DrawTextureRect(texture, new(0, 0, 32, 16), false); }
    private static void Canvas()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); using var texture = new TextureRD(); using var root = new Window { Size = new(40, 24) }; var frames = 0;
        root.Ready += _ => { RenderingServer.SetDefaultClearColor(Colors.Black); texture.TextureRDRID = Graphics(RenderingServer.GetRenderingDevice()!, ShaderLanguage.GLSL); root.AddChild(new Patch(texture)); RenderingServer.FramePostDraw += Captured; };
        void Captured() { if (++frames < 3) return; RenderingServer.FramePostDraw -= Captured; using var image = root.GetTexture().GetImage()!; Check(image.GetPixel(8, 8).R > .99f && image.GetPixel(24, 8).B > .99f, "Source-produced graphics reaches real retained canvas pixels."); var directory = Environment.GetEnvironmentVariable("ELECTRON2D_DEVICE_SOURCE_OUTPUT"); if (directory is not null) image.SavePNG(IOPath.Combine(directory, "canvas-source.png")); root.Tree!.Quit(); }
        Engine.Run(root);
    }
}
