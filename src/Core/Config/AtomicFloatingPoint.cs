using System.Runtime.CompilerServices;

namespace Electron2D;

// Integer atomics avoid the floating-point Volatile ABI defect on 32-bit Android Mono.
internal static class AtomicFloatingPoint
{
    internal static float Read(ref float location) =>
        BitConverter.Int32BitsToSingle(Volatile.Read(ref Unsafe.As<float, int>(ref location)));

    internal static void Write(ref float location, float value) =>
        Volatile.Write(ref Unsafe.As<float, int>(ref location), BitConverter.SingleToInt32Bits(value));

    internal static double Read(ref double location) =>
        BitConverter.Int64BitsToDouble(Interlocked.Read(ref Unsafe.As<double, long>(ref location)));

    internal static void Write(ref double location, double value) =>
        Interlocked.Exchange(ref Unsafe.As<double, long>(ref location), BitConverter.DoubleToInt64Bits(value));
}
