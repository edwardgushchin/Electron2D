using System.Runtime.InteropServices;
using Electron2D;

internal static class ParticleShaderTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static byte[] Fixture(string language)
    {
        using var input = typeof(ParticleShaderTests).Assembly.GetManifestResourceStream("TestShaders.Particle" + language + ".comp.spv")!;
        using var stream = new MemoryStream(); input.CopyTo(stream); return stream.ToArray();
    }
    internal static void RunManaged()
    {
        Check(!RenderingServer.IsAvailable, "Particle resources require no active renderer.");
        Check(Marshal.SizeOf<ParticleStateLayout>() == 96 && Marshal.SizeOf<ParticleFrameLayout>() == 192, "Particle ABI record sizes.");
        foreach (var language in new[] { "GLSL", "HLSL" })
        {
            var code = Fixture(language);
            using var shader = Shader.CreateFromSPIRV(code, Shader.Mode.Particles);
            Check(shader.GetMode() == Shader.Mode.Particles && shader.GetSPIRV().AsSpan().SequenceEqual(code) && shader.GetSPIRV(RenderingDevice.ShaderStage.Compute).AsSpan().SequenceEqual(code), "Particle mode and both bytecode exports.");
            var properties = shader.GetShaderUniformList();
            Check(properties.Count == 3 && properties.Single(p => p.Name == "Enabled") is PropertyDescriptor<ShaderMaterial, bool> && properties.All(p => p.Name is "Tint" or "Drift" or "Enabled"), "Only typed user parameters are material properties.");
            using var material = new ShaderMaterial { Shader = shader };
            Check(!material.GetShaderParameter<bool>("Enabled"), "Logical boolean defaults to false.");
            material.SetShaderParameter("Enabled", true); material.SetShaderParameter("Drift", new Vector2(8, -4)); material.SetShaderParameter("Tint", new Color(.25f, .5f, .75f, 1));
            Reject<ArgumentException>(() => material.SetShaderParameter("Enabled", 1u));
            Reject<NotSupportedException>(() => material.RequireCanvasState());
            Reject<NotSupportedException>(() => shader.GetSPIRV(RenderingDevice.ShaderStage.Vertex));
            Reject<NotSupportedException>(() => shader.GetSPIRV(RenderingDevice.ShaderStage.Fragment));
            Reject<ArgumentOutOfRangeException>(() => shader.GetSPIRV((RenderingDevice.ShaderStage)123));
            Reject<NotSupportedException>(() => Shader.CreateFromSPIRV(code));
            var events = 0; shader.Changed += _ => events++;
            var badLocalSize = Rewrite(code, 16, i => i.Length == 6 && i[2] == 17, i => i[3] = 32);
            Reject<NotSupportedException>(() => shader.SetSPIRV(badLocalSize, Shader.Mode.Particles));
            var derivativeControl = new byte[code.Length + 8];
            code.AsSpan(0, 20).CopyTo(derivativeControl);
            var capability = MemoryMarshal.Cast<byte, uint>(derivativeControl.AsSpan(20, 8));
            capability[0] = (2u << 16) | 17u; capability[1] = 51;
            code.AsSpan(20).CopyTo(derivativeControl.AsSpan(28));
            Reject<NotSupportedException>(() => shader.SetSPIRV(derivativeControl, Shader.Mode.Particles));
            Check(events == 0 && shader.GetSPIRV().AsSpan().SequenceEqual(code), "Invalid replacement preserves the entire program without events.");
            shader.SetSPIRV(Fixture(language == "GLSL" ? "HLSL" : "GLSL"), Shader.Mode.Particles);
            Check(events == 1 && material.GetShaderParameter<bool>("Enabled") && material.GetShaderParameter<Vector2>("Drift") == new Vector2(8, -4), "Cross-language replacement migrates typed material values.");
            using var duplicate = (Shader)shader.Duplicate();
            Check(duplicate.GetMode() == Shader.Mode.Particles && duplicate.GetSPIRV().AsSpan().SequenceEqual(shader.GetSPIRV()), "Duplication preserves compute domain and authored module.");
            var exported = shader.GetSPIRV(); exported[0] = 0; Check(shader.GetSPIRV()[0] != 0, "Exported module bytes are independent.");
            var archive = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-particle-shader-" + Guid.NewGuid().ToString("N") + ".e2dres");
            try
            {
                ResourceSaver.Save(material, archive);
                using var loaded = ResourceLoader.Load<ShaderMaterial>(archive, ResourceLoader.CacheMode.Ignore);
                Check(loaded.Shader!.GetMode() == Shader.Mode.Particles && loaded.Shader.GetSPIRV().AsSpan().SequenceEqual(shader.GetSPIRV()) && loaded.GetShaderParameter<bool>("Enabled") && loaded.GetShaderParameter<Vector2>("Drift") == new Vector2(8, -4), "Resource graphs store the compute domain, module, and typed material values.");
            }
            finally { System.IO.File.Delete(archive); }
            // Corrupt each independent boundary of the fixed compute ABI without invoking a native pipeline.
            Reject<NotSupportedException>(() => Shader.CreateFromSPIRV(Rewrite(code, 71, i => i.Length == 4 && i[2] == 6 && i[3] == 96, i => i[3] = 80), Shader.Mode.Particles));
            Reject<NotSupportedException>(() => Shader.CreateFromSPIRV(Rewrite(code, 72, i => i.Length == 5 && i[3] == 35 && i[4] == 80, i => i[4] = 76), Shader.Mode.Particles));
            Reject<NotSupportedException>(() => Shader.CreateFromSPIRV(Rewrite(code, 71, i => i.Length == 4 && i[2] == 34 && i[3] == 1, i => i[3] = 3), Shader.Mode.Particles));
            shader.Dispose(); Reject<ObjectDisposedException>(() => shader.GetSPIRV(RenderingDevice.ShaderStage.Compute));
        }
        using var minimal = Shader.CreateFromSPIRV(Fixture("Minimal"), Shader.Mode.Particles);
        Check(minimal.GetShaderUniformList().Count == 0, "A state/frame-only program needs no optional queue or material parameters.");
        Console.WriteLine("Particle HLSL/GLSL ABI, typed booleans, atomic replacement, graph archives, duplication, stage separation and malformed interfaces passed.");
    }
    private delegate bool Select(ReadOnlySpan<uint> instruction);
    private delegate void Change(Span<uint> instruction);
    private static byte[] Rewrite(byte[] code, uint opcode, Select select, Change change)
    {
        var result = (byte[])code.Clone(); var words = MemoryMarshal.Cast<byte, uint>(result.AsSpan()); var changed = false;
        for (var at = 5; at < words.Length;)
        {
            var count = (int)(words[at] >> 16); var instruction = words.Slice(at, count);
            if ((words[at] & 0xffff) == opcode && select(instruction)) { change(instruction); changed = true; }
            at += count;
        }
        Check(changed, "Malformed ABI fixture changed its intended instruction."); return result;
    }
    internal static void RunNative()
    {
        using var device = RenderingServer.CreateLocalRenderingDevice();
        foreach (var language in new[] { "GLSL", "HLSL" })
        {
            using var process = Shader.CreateFromSPIRV(Fixture(language), Shader.Mode.Particles);
            using var module = new RDShaderSPIRV { BytecodeCompute = process.GetSPIRV(RenderingDevice.ShaderStage.Compute) };
            var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader);
            var particles = new ParticleStateLayout[70]; var queue = new ParticleStateLayout[4]; queue[0].State.Y = 3;
            var state = device.StorageBufferCreate((uint)(particles.Length * 96), MemoryMarshal.AsBytes(particles.AsSpan()));
            var subemission = device.StorageBufferCreate((uint)(queue.Length * 96), MemoryMarshal.AsBytes(queue.AsSpan()));
            var frameBuffer = device.UniformBufferCreate(192); var parameterBuffer = device.UniformBufferCreate(32);
            using var storage = new RDUniform { Binding = 0, UniformType = RenderingDevice.UniformType.StorageBuffer }; storage.AddID(state);
            using var sub = new RDUniform { Binding = 1, UniformType = RenderingDevice.UniformType.StorageBuffer }; sub.AddID(subemission);
            using var frameUniform = new RDUniform { Binding = 0, UniformType = RenderingDevice.UniformType.UniformBuffer }; frameUniform.AddID(frameBuffer);
            using var parameters = new RDUniform { Binding = 1, UniformType = RenderingDevice.UniformType.UniformBuffer }; parameters.AddID(parameterBuffer);
            var storageSet = device.UniformSetCreate([storage, sub], shader, 1); var uniformSet = device.UniformSetCreate([frameUniform, parameters], shader, 2);
            var frame = new ParticleFrameLayout { Counts = new(67, 0, 123, 3), Time = new(.25f, 1, 0, 0) };
            var values = new float[] { .25f, .5f, .75f, 1, 8, -4, BitConverter.Int32BitsToSingle(1), 0 };
            device.BufferUpdate(frameBuffer, 0, 192, MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref frame, 1)));
            device.BufferUpdate(parameterBuffer, 0, 32, MemoryMarshal.AsBytes(values.AsSpan()));
            Step(); device.BufferGetData(state, MemoryMarshal.AsBytes(particles.AsSpan())); device.BufferGetData(subemission, MemoryMarshal.AsBytes(queue.AsSpan()));
            Check(particles.Take(67).All(p => p.TransformX.W == 2 && p.TransformY.W == -1 && p.Color == new Vector4(.25f, .5f, .75f, 1) && p.VelocityAge.Z == .25f && p.State.X == 1) && particles.Skip(67).All(p => p.State.X == 0), "Native process dispatch updates active IDs and preserves tail records.");
            Check(queue[0].State.X == 3 && queue[0].State.Y == 3 && queue[0].State.Z == 1 && queue.Skip(1).All(p => p.State.X == 1 && p.TransformX.W == 2) && queue.Skip(1).Select(p => p.Custom.X).Distinct().Count() == 3, "Bounded atomic queue writes unique records and reports overflow without corrupting its header.");
            Step(); device.BufferGetData(state, MemoryMarshal.AsBytes(particles.AsSpan())); device.BufferGetData(subemission, MemoryMarshal.AsBytes(queue.AsSpan()));
            Check(particles[66].TransformX.W == 4 && particles[66].VelocityAge.Z == .5f && queue[0].State.X == 3, "Repeated native steps retain authoritative GPU state and a saturated queue.");
            values[6] = 0; device.BufferUpdate(parameterBuffer, 0, 32, MemoryMarshal.AsBytes(values.AsSpan())); Step(); device.BufferGetData(state, MemoryMarshal.AsBytes(particles.AsSpan()));
            Check(particles[0].TransformX.W == 4, "The logical boolean disables process writes on both authoring paths.");
            values[6] = BitConverter.Int32BitsToSingle(1); device.BufferUpdate(parameterBuffer, 0, 32, MemoryMarshal.AsBytes(values.AsSpan()));
            Array.Clear(queue); device.BufferUpdate(subemission, 0, (uint)(queue.Length * 96), MemoryMarshal.AsBytes(queue.AsSpan())); Step(); device.BufferGetData(subemission, MemoryMarshal.AsBytes(queue.AsSpan()));
            Check(queue[0].State.X == 0 && queue[0].State.Z == 1 && queue.Skip(1).All(p => p.State.X == 0), "Zero-capacity queues preserve all payload slots.");
            Array.Clear(queue); queue[0].State.Y = int.MaxValue;
            device.BufferUpdate(subemission, 0, (uint)(queue.Length * 96), MemoryMarshal.AsBytes(queue.AsSpan())); Step(); device.BufferGetData(subemission, MemoryMarshal.AsBytes(queue.AsSpan()));
            Check(queue[0].State.X == 3 && queue[0].State.Y == int.MaxValue && queue[0].State.Z == 1 && queue.Skip(1).All(p => p.State.X == 1), "Oversized declared capacity is clamped to the actual storage length.");
            queue[0].State.X = -1; device.BufferUpdate(subemission, 0, (uint)(queue.Length * 96), MemoryMarshal.AsBytes(queue.AsSpan())); Step(); device.BufferGetData(subemission, MemoryMarshal.AsBytes(queue.AsSpan()));
            Check(queue[0].State.X == -1 && queue[0].State.Z == 1, "An invalid UINT_MAX reservation count never wraps.");
            for (var i = 0; i < 16; i++) Step();
            var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Step();
            Check(GC.GetAllocatedBytesForCurrentThread() == before, "32 warmed particle compute submission/synchronization steps allocate zero managed owner bytes.");
            var headerless = device.StorageBufferCreate(16, new byte[16]);
            device.FreeRID(storageSet); sub.ClearIDs(); sub.AddID(headerless); storageSet = device.UniformSetCreate([storage, sub], shader, 1);
            Step(); var guard = new byte[16]; device.BufferGetData(headerless, guard);
            Check(guard.All(b => b == 0), "A headerless physical queue returns without any storage write.");
            device.FreeRID(headerless);
            foreach (var rid in new[] { storageSet, uniformSet, pipeline, shader, state, subemission, frameBuffer, parameterBuffer }) device.FreeRID(rid);
            void Step()
            {
                var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, storageSet, 1); device.ComputeListBindUniformSet(list, uniformSet, 2); device.ComputeListDispatch(list, 2, 1, 1); device.ComputeListEnd(); device.Submit(); device.Sync();
            }
        }
        using (var minimal = Shader.CreateFromSPIRV(Fixture("Minimal"), Shader.Mode.Particles))
        using (var module = new RDShaderSPIRV { BytecodeCompute = minimal.GetSPIRV(RenderingDevice.ShaderStage.Compute) })
        {
            var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader);
            var output = new ParticleStateLayout[2]; var state = device.StorageBufferCreate(192, MemoryMarshal.AsBytes(output.AsSpan()));
            var frame = new ParticleFrameLayout { Counts = new(1, 123, 0, 0) };
            var frameBuffer = device.UniformBufferCreate(192, MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref frame, 1)));
            using var storage = new RDUniform { Binding = 0, UniformType = RenderingDevice.UniformType.StorageBuffer }; storage.AddID(state);
            using var uniform = new RDUniform { Binding = 0, UniformType = RenderingDevice.UniformType.UniformBuffer }; uniform.AddID(frameBuffer);
            var storageSet = device.UniformSetCreate([storage], shader, 1); var uniformSet = device.UniformSetCreate([uniform], shader, 2);
            var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, storageSet, 1); device.ComputeListBindUniformSet(list, uniformSet, 2); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd(); device.Submit(); device.Sync();
            device.BufferGetData(state, MemoryMarshal.AsBytes(output.AsSpan()));
            Check(output[0].State.X == 123 && output[1].State.X == 0, "State/frame-only native dispatch requires no queue or material buffer.");
            foreach (var rid in new[] { storageSet, uniformSet, pipeline, shader, state, frameBuffer }) device.FreeRID(rid);
        }
        Console.WriteLine("Particle HLSL/GLSL public Shader -> RenderingDevice execution, retained GPU state, tail bounds and bounded subemission and 32 warmed steps/language=0 managed B passed on " + device.GetDeviceName());
    }
}
