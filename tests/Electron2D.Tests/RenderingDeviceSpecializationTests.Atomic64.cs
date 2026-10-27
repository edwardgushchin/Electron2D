using System.Reflection;
using System.Runtime.InteropServices;
using Electron2D;
using static TextureArrayTests;

internal static partial class RenderingDeviceSpecializationTests
{
    private static void Atomic64Requirements()
    {
        var precision = typeof(RenderingDevice).Assembly.GetType("Electron2D.ShaderPrecision")!;
        var read = precision.GetMethod("ReadRequirements", BindingFlags.Static | BindingFlags.NonPublic)!;
        var atomic = precision.GetMethod("ReadAtomicRequirements", BindingFlags.Static | BindingFlags.NonPublic)!;
        (bool Declared, bool Buffer, bool Shared) Parse(uint storage, uint operation, bool declare = true, uint pointerOperation = 59, bool wide = true, bool shortInstruction = false)
        {
            var words = new List<uint> { 0x07230203, 0x10000, 0, 100, 0 };
            void Emit(uint op, params uint[] args) { words.Add((uint)(args.Length + 1) << 16 | op); words.AddRange(args); }
            Emit(17, declare ? 12u : 11u); Emit(21, 1, wide ? 64u : 32u, 0); Emit(32, 2, storage, 1); Emit(pointerOperation, 2, 3);
            var count = operation is 227 or 232 or 233 ? 6 : operation == 228 ? 5 : operation is 230 or 231 ? 9 : 7;
            var operands = new uint[count - 1]; operands[operation == 228 ? 0 : 2] = 3;
            Emit(operation, shortInstruction ? operands[..^1] : operands);
            var code = MemoryMarshal.AsBytes(words.ToArray().AsSpan()).ToArray();
            try { read.Invoke(null, [code]); return ((bool, bool, bool))atomic.Invoke(null, [code])!; }
            catch (TargetInvocationException error) when (error.InnerException is not null) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
        }
        for (uint op = 227; op <= 242; op++)
        {
            Check(Parse(2, op) == (true, true, false) && Parse(12, op) == (true, true, false), "Both buffer encodings classify every integer atomic opcode.");
            Check(Parse(4, op) == (true, false, true), "Workgroup atomics require their independent feature.");
            Reject<ArgumentException>(() => Parse(12, op, false));
            Reject<ArgumentException>(() => Parse(12, op, shortInstruction: true));
        }
        foreach (var op in new uint[] { 55, 57, 59, 60, 61, 65, 66, 67, 70, 83, 120, 169, 245 })
            Check(Parse(12, 234, pointerOperation: op) == (true, true, false), "Pointer-producing instructions retain atomic storage identity.");
        foreach (var storage in new uint[] { 0, 1, 3, 6, 7, 9, 11, 5349 }) Reject<NotSupportedException>(() => Parse(storage, 234));
        Check(Parse(11, 234, wide: false) == (true, false, false), "32-bit image atomics do not require a 64-bit storage adapter.");
        var layout = typeof(RenderingDevice).Assembly.GetType("Electron2D.GPUDeviceFactory+Vulkan12Features")!;
        Check(Marshal.OffsetOf(layout, "Flags").ToInt32() == (IntPtr.Size == 8 ? 16 : 8) && Marshal.SizeOf(layout) == (IntPtr.Size == 8 ? 208 : 196), "Vulkan 1.2 feature header and 47 flags match the host ABI.");
        Console.WriteLine("PASS all 16 integer atomic opcodes, independent storage classes, implicit Int64, pointer producers, malformed operands and Vulkan feature ABI.");
    }
}
