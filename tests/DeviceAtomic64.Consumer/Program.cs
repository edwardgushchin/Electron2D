using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

try { Atomic64.Run(args.Length == 0 ? "all" : args[0]); return 0; }
catch (Exception error) { Console.Error.WriteLine(error); return 1; }

internal static partial class Atomic64
{
    internal static void Run(string mode)
    {
        using var device = RenderingServer.CreateLocalRenderingDevice();
        Console.WriteLine("Native atomic device: " + device.GetDeviceName());
        var buffer = mode is "all" or "buffer" or "unavailable-shared";
        var shared = mode is "all" or "shared" or "unavailable-buffer";
        if (buffer) { Execute(device, "buffer.comp.glsl"); Execute(device, "buffer.comp.glsl", true); Execute(device, "buffer.comp.hlsl"); }
        if (shared) { Execute(device, "shared.comp.glsl"); Execute(device, "shared.comp.glsl", true); }
        if (mode == "all") Mixed(device);
        if (mode.StartsWith("unavailable", StringComparison.Ordinal))
        {
            foreach (var name in new[] { "buffer.comp.glsl", "shared.comp.glsl", "mixed.comp.glsl" })
            {
                if (name == "buffer.comp.glsl" && buffer || name == "shared.comp.glsl" && shared) continue;
                using var code = Code(name);
                Reject<NotSupportedException>(() => device.ShaderCreateFromSPIRV(code));
                Reject<NotSupportedException>(() => device.ShaderCompileBinaryFromSPIRV(code));
            }
            Console.WriteLine("PASS independent native atomic feature refusal before shader/binary publication.");
        }
        Check(buffer || shared || mode is "unavailable-both" or "unavailable-api", "Known native profile.");
    }
    private static RDShaderSPIRV Code(string name) => new() { BytecodeCompute = File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", name + ".spv")) };
    private static void Execute(RenderingDevice device, string name, bool raw = false)
    {
        using var code = Code(name); if (raw) code.BytecodeCompute = CounterOpcodes(code.BytecodeCompute); var shader = device.ShaderCreateFromBytecode(device.ShaderCompileBinaryFromSPIRV(code));
        var hlsl = name.EndsWith("hlsl", StringComparison.Ordinal);
        using var extra = new RDPipelineSpecializationConstant<ulong> { ConstantID = 0, Value = 19 };
        var pipeline = hlsl ? device.ComputePipelineCreate(shader) : device.ComputePipelineCreate(shader, [extra]); extra.Dispose();
        var cells = device.StorageBufferCreate(16); var output = device.StorageBufferCreate(1024);
        using var state = new RDUniform { Binding = 0, UniformType = UniformType.StorageBuffer }; state.AddID(cells);
        using var results = new RDUniform { Binding = 1, UniformType = UniformType.StorageBuffer }; results.AddID(output);
        var set = device.UniformSetCreate([state, results], shader, 0);
        ulong[] push = [1UL << 48]; var values = new ulong[128];
        void Dispatch()
        {
            push[0] = 1UL << 48;
            var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, set, 0);
            device.ComputeListSetPushConstant(list, MemoryMarshal.AsBytes(push.AsSpan()), 8); push[0] = 0;
            device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd(); device.BufferGetData(output, MemoryMarshal.AsBytes(values.AsSpan()));
        }
        Dispatch();
        var start = (1UL << 48) + (hlsl ? 0UL : 19UL);
        Check(values[15] == start + 64, "All 64 contending invocations increment exactly once above 32 bits.");
        var old = values.AsSpan(64, 64).ToArray(); Array.Sort(old);
        for (var i = 0; i < old.Length; i++) Check(old[i] == start + (ulong)i, "Atomic returns form an exact nonduplicated counter sequence.");
        if (hlsl) Check(values[0] == start + 64 && values[1] == start + 71, "HLSL compare/exchange preserves both words.");
        else
        {
            const ulong original = 0xfedcba9800000007;
            var and = original & 0xffff0000ffff0000; var or = and | 0x0000111100001111; var xor = or ^ 0x8000000000000001;
            ulong[] expected = [unchecked((ulong)-1099511627776L), unchecked((ulong)-1099511627781L), unchecked((ulong)-1125899906842624L), 1125899906842624, unchecked((ulong)-7L), unchecked((ulong)-7L), unchecked((ulong)-17L), original, and, or, xor, Math.Min(xor, 0x8000000000000000), 0xf000000000000000, ulong.MaxValue, 0];
            Check(values[16] == start && values[17] == start, "Atomic store/load and decrement retain exact high words.");
            for (var i = 0; i < expected.Length; i++) Check(values[i] == expected[i], "Signed/unsigned atomic arithmetic, min/max, bitwise, exchange, failed/successful CAS and rollover preserve exact bits.");
        }
        for (var i = 0; i < 16; i++) Dispatch(); var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 16; i++) Dispatch();
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared atomic dispatch/readback allocates zero owner-thread managed bytes.");
        Check(values[15] == start + 64, "Repeated dispatches observe initialization and barriers.");
        Console.WriteLine("PASS " + name + (raw ? " raw increment/decrement/subtract and implicit Int64" : "") + ": exact 64-bit atomics, 64 concurrent returns, copied specialization/push, binary roundtrip, repeated barriers and zero owner bytes.");
    }
    private static void Mixed(RenderingDevice device)
    {
        using var code = Code("mixed.comp.glsl"); var shader = device.ShaderCreateFromSPIRV(code); var pipeline = device.ComputePipelineCreate(shader); var buffer = device.StorageBufferCreate(24);
        using var uniform = new RDUniform { Binding = 0, UniformType = UniformType.StorageBuffer }; uniform.AddID(buffer); var set = device.UniformSetCreate([uniform], shader, 0);
        var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, set, 0); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd();
        Check(MemoryMarshal.Cast<byte, ulong>(device.BufferGetData(buffer)).SequenceEqual(new ulong[] { 64, 64, 64 }), "One kernel requires and executes both independent atomic storage classes.");
        Console.WriteLine("PASS mixed buffer/shared atomic requirements and native results.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
