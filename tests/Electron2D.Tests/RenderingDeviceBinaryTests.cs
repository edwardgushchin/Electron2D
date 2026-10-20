using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Electron2D;
using static Electron2D.RenderingDevice;
using static TextureArrayTests;
using IOPath = System.IO.Path;

internal static partial class RenderingDeviceBinaryTests
{
    private static RDShaderSPIRV ComputeModule(string name)
    {
        using var input = typeof(RenderingDeviceBinaryTests).Assembly.GetManifestResourceStream("Electron2D.Compute." + name + ".comp.spv")!;
        using var bytes = new MemoryStream(); input.CopyTo(bytes); return new() { BytecodeCompute = bytes.ToArray() };
    }
    private static RID Set(RenderingDevice device, RID shader, uint set, int binding, UniformType type, RID resource)
    { using var uniform = new RDUniform { Binding = binding, UniformType = type }; uniform.AddID(resource); return device.UniformSetCreate([uniform], shader, set); }
    internal static void Run(string profile)
    {
        var output = Environment.GetEnvironmentVariable("ELECTRON2D_DEVICE_BINARY_OUTPUT");
        var temporary = output is null; output ??= Directory.CreateTempSubdirectory("electron2d-binary-").FullName; Directory.CreateDirectory(output);
        try
        {
            using var device = RenderingServer.CreateLocalRenderingDevice();
            if (profile == "read")
            {
                foreach (var language in new[] { "GLSL", "HLSL" }) { Compute(device, File.ReadAllBytes(IOPath.Combine(output, language + "-compute.bin"))); Graphics(device, File.ReadAllBytes(IOPath.Combine(output, language + "-graphics.bin"))); }
                Specialization(device, File.ReadAllBytes(IOPath.Combine(output, "specialization.bin")));
                Console.WriteLine("Fresh-process binary load executes compute, paired graphics and specialization without the source frontend."); return;
            }
            foreach (var language in new[] { "GLSL", "HLSL" })
            {
                using var compute = ComputeModule(language == "GLSL" ? "LocalCompute" : "LocalComputeHLSL"); using var graphics = RenderingDeviceGraphicsTests.Load("DevicePush" + language);
                var bytes = device.ShaderCompileBinaryFromSPIRV(compute, "Счётчик 😀\0" + language); var draw = device.ShaderCompileBinaryFromSPIRV(graphics, "Draw " + language);
                Check(bytes.SequenceEqual(device.ShaderCompileBinaryFromSPIRV(compute, "Счётчик 😀\0" + language)), "Identical program/name produce deterministic independent binary bytes.");
                File.WriteAllBytes(IOPath.Combine(output, language + "-compute.bin"), bytes); File.WriteAllBytes(IOPath.Combine(output, language + "-graphics.bin"), draw);
                compute.BytecodeCompute = []; compute.Dispose(); graphics.BytecodeVertex = []; graphics.BytecodeFragment = []; graphics.Dispose();
                Compute(device, bytes); Graphics(device, draw); Guards(device, bytes);
            }
            using var symbolic = ComputeModule("LocalSpecialization"); var specialized = device.ShaderCompileBinaryFromSPIRV(symbolic); File.WriteAllBytes(IOPath.Combine(output, "specialization.bin"), specialized); Specialization(device, specialized);
            FreshProcess(output);
            using var closed = RenderingServer.CreateLocalRenderingDevice(); var abandoned = closed.ShaderCreatePlaceholder(); closed.Dispose(); Reject<ObjectDisposedException>(() => closed.ShaderCreatePlaceholder()); Reject<ObjectDisposedException>(() => closed.ShaderCreateFromBytecode(specialized, abandoned)); Reject<ObjectDisposedException>(() => closed.ShaderCompileBinaryFromSPIRV(symbolic));
            Console.WriteLine("Binary program ownership, malformed containers, copied stages, failed-placeholder retry, executable variants and prepared zero owner bytes passed on " + device.GetDeviceName());
        }
        finally { if (temporary) Directory.Delete(output, true); }
    }
    private static void FreshProcess(string output)
    {
        var info = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        if (IOPath.GetFileNameWithoutExtension(Environment.ProcessPath!).Equals("dotnet", StringComparison.OrdinalIgnoreCase)) info.ArgumentList.Add(typeof(RenderingDeviceBinaryTests).Assembly.Location);
        foreach (var key in info.Environment.Keys.Where(k => k.StartsWith("ELECTRON2D_TEST_", StringComparison.Ordinal)).ToArray()) info.Environment.Remove(key);
        info.Environment["ELECTRON2D_TEST_DEVICE_BINARY"] = "read"; info.Environment["ELECTRON2D_DEVICE_BINARY_OUTPUT"] = IOPath.GetFullPath(output); info.Environment["ELECTRON2D_SHADER_IMPORT_TOOL"] = IOPath.Combine(output, "missing-frontend"); info.Environment["PATH"] = "";
        using var child = Process.Start(info)!; var stdout = child.StandardOutput.ReadToEndAsync(); var stderr = child.StandardError.ReadToEndAsync();
        if (!child.WaitForExit(120000)) { child.Kill(true); child.WaitForExit(); throw new InvalidOperationException("Binary consumer timed out."); }
        Task.WaitAll(stdout, stderr); Check(child.ExitCode == 0, "Fresh binary consumer: " + stdout.Result + stderr.Result); Console.Write(stdout.Result);
    }
    private static void Compute(RenderingDevice device, byte[] binary)
    {
        var reserved = device.ShaderCreatePlaceholder(); Reject<ArgumentException>(() => device.ComputePipelineCreate(reserved)); Reject<ArgumentException>(() => device.UniformSetCreate([], reserved, 0)); Reject<ArgumentException>(() => device.ShaderGetVertexInputAttributeMask(reserved));
        var copy = binary.ToArray(); var shader = device.ShaderCreateFromBytecode(copy.AsSpan(), reserved); Check(shader == reserved, "Loading initializes the reserved shader RID in place."); Array.Fill(copy, (byte)0); var pipeline = device.ComputePipelineCreate(shader);
        float[] initial = [1, 2, 3, 4], settings = [2, 4, 3, 0], result = new float[4]; var buffer = device.StorageBufferCreate(16); var uniform = device.UniformBufferCreate(16, MemoryMarshal.AsBytes(settings.AsSpan()));
        var output = Set(device, shader, 3, 7, UniformType.StorageBuffer, buffer); var input = Set(device, shader, 2, 4, UniformType.UniformBuffer, uniform);
        void Work()
        { device.BufferUpdate(buffer, 0, 16, MemoryMarshal.AsBytes(initial.AsSpan())); var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, output, 3); device.ComputeListBindUniformSet(list, input, 2); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd(); device.BufferGetData(buffer, MemoryMarshal.AsBytes(result.AsSpan())); }
        Work(); Check(result.SequenceEqual(new float[] { 5, 7, 9, 11 }), "Binary compute executes copied storage/uniform interfaces after caller bytes are overwritten.");
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); for (var i = 0; i < 16; i++) Work(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Work(); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared binary compute has zero owner managed bytes.");
        device.FreeRID(shader); Check(!device.ComputePipelineIsValid(pipeline) && !device.UniformSetIsValid(output), "Loaded program lifetime remains borrowed by pipeline and sets."); Reject<ArgumentException>(() => device.ShaderCreateFromBytecode(binary, reserved)); device.FreeRID(pipeline); device.FreeRID(input); device.FreeRID(output); device.FreeRID(buffer); device.FreeRID(uniform);
    }
    private static void Specialization(RenderingDevice device, byte[] binary)
    {
        var shader = device.ShaderCreateFromBytecode(binary); using var enabled = new RDPipelineSpecializationConstant<bool> { ConstantID = 1, Value = false }; using var group = new RDPipelineSpecializationConstant<uint> { ConstantID = 5, Value = 4 };
        var normal = device.ComputePipelineCreate(shader); var custom = device.ComputePipelineCreate(shader, [enabled, group]); var buffer = device.StorageBufferCreate(16); var set = Set(device, shader, 0, 0, UniformType.StorageBuffer, buffer); float[] values = new float[4];
        void Work(RID pipeline) { var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, set, 0); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd(); device.BufferGetData(buffer, MemoryMarshal.AsBytes(values.AsSpan())); }
        Work(normal); Check(values[0] == 11, "Binary templates preserve specialization defaults."); Work(custom); Check(values.All(v => v == -1), "Binary templates preserve Boolean values and specialized workgroups.");
        device.FreeRID(custom); Work(normal); Check(values[0] == 11, "Freeing a variant preserves its sibling shader program."); device.FreeRID(shader); Check(!device.ComputePipelineIsValid(normal), "Symbolic binary ownership matches ordinary shader ownership."); device.FreeRID(normal); device.FreeRID(set); device.FreeRID(buffer);
    }
    private static void Guards(RenderingDevice device, byte[] valid)
    {
        var placeholder = device.ShaderCreatePlaceholder(); var buffer = device.StorageBufferCreate(4); using var foreign = RenderingServer.CreateLocalRenderingDevice(); var other = foreign.ShaderCreatePlaceholder();
        Reject<ArgumentException>(() => device.ShaderCreateFromBytecode(valid, other)); Reject<ArgumentException>(() => device.ShaderCreateFromBytecode(valid, buffer)); Reject<InvalidOperationException>(() => Task.Run(() => device.ShaderCreatePlaceholder()).GetAwaiter().GetResult()); Reject<InvalidOperationException>(() => Task.Run(() => device.ShaderCreateFromBytecode(valid)).GetAwaiter().GetResult());
        using var module = ComputeModule("LocalCompute"); Reject<InvalidOperationException>(() => Task.Run(() => device.ShaderCompileBinaryFromSPIRV(module)).GetAwaiter().GetResult());
        var list = device.ComputeListBegin(); Reject<InvalidOperationException>(() => device.ShaderCreatePlaceholder()); Reject<InvalidOperationException>(() => device.ShaderCompileBinaryFromSPIRV(module)); Reject<InvalidOperationException>(() => device.ShaderCreateFromBytecode(valid, placeholder)); device.ComputeListEnd();
        var delivered = 0; device.BufferGetDataAsync(buffer, _ => delivered++); var queued = device.ShaderCompileBinaryFromSPIRV(module); var queuedShader = device.ShaderCreateFromBytecode(queued); Check(delivered == 0, "Binary factories neither submit pending transfers nor deliver readback callbacks."); device.FreeRID(queuedShader); Check(delivered == 1, "Existing explicit resource completion still delivers queued reads.");
        using (var empty = new RDShaderSPIRV()) Reject<ArgumentException>(() => device.ShaderCompileBinaryFromSPIRV(empty));
        Reject<ArgumentNullException>(() => device.ShaderCompileBinaryFromSPIRV(null!)); Reject<ArgumentNullException>(() => device.ShaderCompileBinaryFromSPIRV(module, null!)); Reject<ArgumentException>(() => device.ShaderCompileBinaryFromSPIRV(module, "\ud800")); Reject<ArgumentException>(() => device.ShaderCompileBinaryFromSPIRV(module, new string('é', 2049)));
        var longest = device.ShaderCompileBinaryFromSPIRV(module, new string('é', 2048)); device.FreeRID(device.ShaderCreateFromBytecode(longest)); module.CompileErrorCompute = "failed"; Reject<ArgumentException>(() => device.ShaderCompileBinaryFromSPIRV(module)); module.CompileErrorCompute = ""; module.BytecodeVertex = module.BytecodeCompute; Reject<ArgumentException>(() => device.ShaderCompileBinaryFromSPIRV(module)); module.BytecodeVertex = []; module.Dispose(); Reject<ObjectDisposedException>(() => device.ShaderCompileBinaryFromSPIRV(module));
        void Invalid(byte[] bytes) => Reject<ArgumentException>(() => device.ShaderCreateFromBytecode(bytes, placeholder));
        for (var i = 0; i < 60; i++) Invalid(valid[..i]); var corrupted = valid.ToArray(); corrupted[^1] ^= 1; Invalid(corrupted); corrupted = valid.ToArray(); corrupted[0] ^= 1; Invalid(corrupted); corrupted = valid.ToArray(); BinaryPrimitives.WriteInt32LittleEndian(corrupted.AsSpan(12), int.MaxValue); Invalid(corrupted);
        corrupted = valid.ToArray(); corrupted[8] = 2; Reject<NotSupportedException>(() => device.ShaderCreateFromBytecode(corrupted, placeholder));
        var nameSize = BinaryPrimitives.ReadInt32LittleEndian(valid.AsSpan(52));
        void Mutate(int offset, int value) { var bytes = valid.ToArray(); BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset), value); SHA256.HashData(bytes.AsSpan(48), bytes.AsSpan(16, 32)); Invalid(bytes); }
        foreach (var count in new[] { -1, 0, 2, 3, int.MaxValue }) Mutate(48, count);
        foreach (var length in new[] { -1, 4097, int.MaxValue }) Mutate(52, length);
        Mutate(56 + nameSize, 1); foreach (var length in new[] { -1, 0, 19, 21, 16 * 1024 * 1024 + 4, int.MaxValue }) Mutate(60 + nameSize, length);
        corrupted = valid.ToArray(); corrupted[56] = 255; SHA256.HashData(corrupted.AsSpan(48), corrupted.AsSpan(16, 32)); Invalid(corrupted);
        Mutate(64 + nameSize, 0); // Valid checksum cannot bypass the common module validator.
        corrupted = [.. valid, 0]; BinaryPrimitives.WriteInt32LittleEndian(corrupted.AsSpan(12), corrupted.Length); SHA256.HashData(corrupted.AsSpan(48), corrupted.AsSpan(16, 32)); Invalid(corrupted);
        var shader = device.ShaderCreateFromBytecode(valid, placeholder); Reject<ArgumentException>(() => device.ShaderCreateFromBytecode(valid, shader)); var pipeline = device.ComputePipelineCreate(shader); Check(device.ComputePipelineIsValid(pipeline), "All failed loads leave a reserved identity retryable and completed programs cannot be replaced."); device.FreeRID(pipeline); device.FreeRID(shader); device.FreeRID(buffer); foreign.FreeRID(other);
        var abandoned = device.ShaderCreatePlaceholder(); device.FreeRID(abandoned); Reject<ArgumentException>(() => device.ShaderCreateFromBytecode(valid, abandoned));
    }
}
