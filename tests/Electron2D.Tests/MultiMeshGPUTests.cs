using System.Runtime.InteropServices;
using Electron2D;
using static TextureArrayTests;

internal static partial class MultiMeshGPUTests
{
    private sealed class Draw : Entity
    {
        internal RID Instances;
        protected override void OnDraw() { if (Instances.IsValid()) DrawMultiMesh(Instances); }
    }
    internal static void Run(string profile)
    {
        if (profile == "compatibility") { Compatibility(); return; }
        if (profile.StartsWith("procedural", StringComparison.Ordinal)) { ProceduralMeshTests.RunNative(profile, true); return; }
        if (profile.StartsWith("vertex", StringComparison.Ordinal)) { CanvasVertexProgramTests.RunNative(profile, true); return; }
        Oracles(); Commands(); Commands("HLSL", true); Commands("GLSL", modern: true); Interpolation(); Sessions();
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new() { Vertices = [Vector2.Zero, new(20, 0), new(20, 20), new(0, 20)], Indices = [0, 1, 2, 0, 2, 3] }, lods: new Dictionary<float, int[]> { [2] = [0, 1, 3] });
        using var window = new Window { Size = new(128, 80) }; var draw = new Draw(); window.AddChild(draw);
        var frame = 0; long start = 0, active = 0, idle = 0; RID instances = default, storage = default, commands = default, shader = default, pipeline = default, parameters = default, set = default, uniforms = default; RenderingDevice device = null!; var settings = new float[4];
        void Dispatch(float phase)
        {
            settings[0] = phase; device.BufferUpdate(parameters, 0, 16, MemoryMarshal.AsBytes(settings.AsSpan()));
            var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, set, 0); device.ComputeListBindUniformSet(list, uniforms, 1); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd();
        }
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); device = RenderingServer.GetRenderingDevice()!; Contracts(device);
            instances = RenderingServer.MultiMeshCreate(); RenderingServer.MultiMeshAllocateData(instances, 2, true, true, true); RenderingServer.MultiMeshSetMesh(instances, mesh.GetRID()); RenderingServer.MultiMeshSetCustomAABB(instances, new(0, 0, 128, 80));
            storage = RenderingServer.MultiMeshGetBufferRDRID(instances); commands = RenderingServer.MultiMeshGetCommandBufferRDRID(instances); Check(storage.IsValid() && commands.IsValid(), "Real retained GPU buffers exist."); Check(RenderingServer.MultiMeshGetBufferRDRID(instances) == storage && RenderingServer.MultiMeshGetCommandBufferRDRID(instances) == commands, "Prepared device identities remain stable.");
            Reject<InvalidOperationException>(() => device.FreeRID(storage));
            using var module = new RDShaderSPIRV(); using var input = typeof(MultiMeshGPUTests).Assembly.GetManifestResourceStream("Electron2D.Compute.MultiMeshGPU.comp.spv")!; using var bytes = new MemoryStream(); input.CopyTo(bytes); module.BytecodeCompute = bytes.ToArray(); shader = device.ShaderCreateFromSPIRV(module); pipeline = device.ComputePipelineCreate(shader); parameters = device.UniformBufferCreate(16);
            using var a = new RDUniform { Binding = 0, UniformType = RenderingDevice.UniformType.StorageBuffer }; a.AddID(storage); using var b = new RDUniform { Binding = 1, UniformType = RenderingDevice.UniformType.StorageBuffer }; b.AddID(commands); using var p = new RDUniform { Binding = 0, UniformType = RenderingDevice.UniformType.UniformBuffer }; p.AddID(parameters); set = device.UniformSetCreate([a, b], shader, 0); uniforms = device.UniformSetCreate([p], shader, 1);
            Dispatch(0); draw.Instances = instances; draw.QueueRedraw();
            window.Tree!.ProcessFrameStarted += _ => { start = GC.GetAllocatedBytesForCurrentThread(); if (frame is >= 16 and < 80) Dispatch(frame & 1); };
            RenderingServer.FramePostDraw += () =>
            {
                if (frame is >= 16 and < 80) active += GC.GetAllocatedBytesForCurrentThread() - start; if (frame is >= 80 and < 144) idle += GC.GetAllocatedBytesForCurrentThread() - start;
                frame++;
                if (frame <= 2)
                {
                    using var pixels = window.GetTexture().GetImage()!; var color = pixels.GetPixel(frame == 1 ? 16 : 20, 20); var second = pixels.GetPixel(52, 20);
                    Check(frame == 1 ? color.R > .9f && color.G < .1f && second.R < .1f && second.G < .1f : color.G > .9f && color.R < .1f && second.G > .9f, $"GPU-generated poses/color/indirect count are visible on frame {frame}: {color}, {second}.");
                    var lodPixel = pixels.GetPixel(frame == 1 ? 20 : 24, frame == 1 ? 24 : 28); Check(frame == 1 ? lodPixel.R < .1f && lodPixel.G < .1f : lodPixel.G > .9f, "GPU-written scale selects per-instance LOD geometry.");
                    Check(RenderingServer.MultiMeshInstanceGetTransform2D(instances, 0) == default, "GPU writes preserve authored CPU snapshots.");
                    if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } path) { Directory.CreateDirectory(path); pixels.SavePNG(System.IO.Path.Combine(path, $"gpu-multimesh-{frame}.png")); }
                    if (frame == 1) Dispatch(1);
                }
                if (frame == 144)
                {
                    Check(active == 0 && idle == 0, $"GPU instance complete managed frames active={active}, idle={idle}."); RenderingServer.FreeRID(instances); Check(!device.UniformSetIsValid(set), "Expired instance storage invalidates device borrowers."); foreach (var rid in new[] { storage, commands, set, uniforms, parameters, pipeline, shader }) device.FreeRID(rid); window.Tree!.Quit();
                }
            };
        };
        Check(Engine.Run(window) == 0 && frame == 144, "GPU-generated indirect instance execution completed."); Console.WriteLine($"GPU instance pixels and native indirect count; 64 active={active}, 64 idle={idle}.");
    }
}
