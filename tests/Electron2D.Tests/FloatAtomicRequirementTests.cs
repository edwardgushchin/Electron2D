using System.Reflection;
using System.Runtime.InteropServices;
using Electron2D;

internal static class FloatAtomicRequirementTests
{
    internal static void Run()
    {
        var precision = typeof(RenderingDevice).Assembly.GetType("Electron2D.ShaderPrecision")!;
        var validate = precision.GetMethod("ReadRequirements", BindingFlags.Static | BindingFlags.NonPublic)!;
        var read = precision.GetMethod("ReadFloatAtomicRequirements", BindingFlags.Static | BindingFlags.NonPublic)!;
        var checks = 0;
        void Check(bool value) { if (!value) throw new InvalidOperationException("Floating atomic requirements differ from their storage/precision contract."); checks++; }
        void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { checks++; return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
        (int Declared, int Mask) Parse(uint storage, uint operation, bool declare = true, bool extension = true, uint width = 32, bool shortInstruction = false, bool resultMismatch = false, bool unused = false, uint capability = 6033)
        {
            var words = new List<uint> { 0x07230203, 0x10000, 0, 100, 0 };
            void Emit(uint op, params uint[] values) { words.Add((uint)(values.Length + 1) << 16 | op); words.AddRange(values); }
            Emit(17, 1); if (declare) Emit(17, capability); if (width == 64) Emit(17, 10);
            if (extension) Emit(10, MemoryMarshal.Cast<byte, uint>("SPV_EXT_shader_atomic_float_add\0"u8).ToArray());
            Emit(22, 1, width); Emit(32, 2, storage, 1); Emit(59, 2, 3);
            if (!unused)
            {
                var count = operation == 227 ? 6 : operation == 228 ? 5 : 7;
                var values = new uint[count - 1]; values[operation == 228 ? 0 : 2] = 3;
                if (operation != 228) values[0] = resultMismatch ? 2u : 1u;
                Emit(operation, shortInstruction ? values[..^1] : values);
            }
            var code = MemoryMarshal.AsBytes(words.ToArray().AsSpan()).ToArray();
            try { validate.Invoke(null, [code]); var result = ((bool, int, bool))read.Invoke(null, [code])!; return ((result.Item1 ? 1 : 0) | (result.Item3 ? 2 : 0), result.Item2); }
            catch (TargetInvocationException error) when (error.InnerException is not null) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
        }
        foreach (var (storage, bit) in new (uint, int)[] { (2, 0), (12, 0), (5349, 0), (4, 2), (11, 4) })
        {
            foreach (var op in new uint[] { 227, 228, 229, 6035 })
            {
                Check(Parse(storage, op) == (1, 1 << (bit + (op == 6035 ? 1 : 0))));
                Reject<ArgumentException>(() => Parse(storage, op, shortInstruction: true));
                if (storage == 11) Reject<NotSupportedException>(() => Parse(storage, op, width: 64, capability: 6034));
                else
                {
                    Check(Parse(storage, op, width: 64, capability: 6034) == (2, 1 << ((storage == 4 ? 8 : 6) + (op == 6035 ? 1 : 0))));
                    Reject<ArgumentException>(() => Parse(storage, op, width: 64, capability: 6034, shortInstruction: true));
                    if (op != 228) Reject<ArgumentException>(() => Parse(storage, op, width: 64, capability: 6034, resultMismatch: true));
                }
                if (op != 228) Reject<ArgumentException>(() => Parse(storage, op, resultMismatch: true));
            }
            Check(Parse(storage, 229, declare: false, extension: false) == (0, 1 << bit));
            Reject<ArgumentException>(() => Parse(storage, 6035, declare: false));
            Reject<ArgumentException>(() => Parse(storage, 6035, extension: false));
            Reject<NotSupportedException>(() => Parse(storage, 5614));
            Reject<NotSupportedException>(() => Parse(storage, 5615));
        }
        foreach (var storage in new uint[] { 0, 1, 3, 6, 7, 9 }) Reject<NotSupportedException>(() => Parse(storage, 6035));
        foreach (var capability in new uint[] { 5404, 6095, 5612, 5613, 5616 }) Reject<NotSupportedException>(() => Parse(12, 6035, capability: capability, unused: true));
        Check(Parse(12, 6035, unused: true) == (1, 0));
        Reject<ArgumentException>(() => Parse(12, 6035, extension: false, unused: true));
        Check(Parse(12, 6035, unused: true, capability: 6034, width: 64) == (2, 0));
        Reject<ArgumentException>(() => Parse(12, 6035, extension: false, unused: true, capability: 6034, width: 64));
        Reject<ArgumentException>(() => Parse(12, 6035, width: 64));
        foreach (var storage in new uint[] { 0, 1, 3, 6, 7, 9, 11 }) Reject<NotSupportedException>(() => Parse(storage, 6035, width: 64, capability: 6034));
        var layout = typeof(RenderingDevice).Assembly.GetType("Electron2D.GPUDeviceFactory+FloatAtomicFeatures")!;
        Check(Marshal.OffsetOf(layout, "Flags").ToInt32() == (IntPtr.Size == 8 ? 16 : 8) && Marshal.SizeOf(layout) == (IntPtr.Size == 8 ? 64 : 56));
        Console.WriteLine($"FP32/FP64 atomic requirement guards PASS {checks}");
    }
}
