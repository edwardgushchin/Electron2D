using System.Runtime.InteropServices;
using System.Globalization;

namespace Electron2D;

internal sealed partial record ShaderUniform
{
    internal uint[]? DefaultWords { get; init; }
    internal int DefaultMatrixWidth => Type == typeof(Transform) ? 2 : MatrixWidth;
    internal int DefaultWordWidth => DefaultMatrixWidth != 0 ? DefaultMatrixWidth * DefaultMatrixWidth : ElementSize / 4;
    internal string DefaultTypeCode => (DefaultMatrixWidth != 0 ? "m" : BooleanWidth != 0 ? "b" :
        Type == typeof(float) || Type == typeof(Vector2) || Type == typeof(Vector3) || Type == typeof(Vector4) ? "f" : Unsigned ? "u" : "i") +
        (DefaultMatrixWidth != 0 ? DefaultMatrixWidth : BooleanWidth != 0 ? BooleanWidth : DefaultWordWidth).ToString(CultureInfo.InvariantCulture);
    internal bool HasSameDefault(ReadOnlySpan<uint> other)
    {
        if (DefaultWords is not { } first || first.Length != other.Length) return false;
        var floating = DefaultTypeCode[0] is 'f' or 'm';
        for (var i = 0; i < first.Length; i++)
            if (first[i] != other[i] && !(floating && ((first[i] | other[i]) & 0x7fffffff) == 0)) return false;
        return true;
    }
    internal int StorageSize => checked((Count - 1) * Stride + ElementSize);

    internal void InitializeDefault(Span<byte> target)
    {
        target.Clear();
        for (var i = 0; i < Count; i++) WriteDefaultElement(target.Slice(i * Stride, ElementSize), i);
    }
    internal void WriteDefaultElement(Span<byte> target, int index)
    {
        if (DefaultWords is { } words)
        {
            var element = words.AsSpan(index * DefaultWordWidth, DefaultWordWidth);
            if (DefaultMatrixWidth > 2) WriteMatrix(target, MemoryMarshal.Cast<uint, float>(element));
            else if (Type == typeof(Transform))
            {
                var values = MemoryMarshal.Cast<uint, float>(element);
                Write(target, new Transform(new(values[0], values[1]), new(values[2], values[3]), Vector2.Zero));
            }
            else MemoryMarshal.AsBytes(element).CopyTo(target);
        }
        else if (Type == typeof(Transform)) Write(target, Transform.Identity);
        else if (MatrixWidth > 2)
        {
            Span<float> identity = stackalloc float[16]; identity.Clear();
            for (var axis = 0; axis < MatrixWidth; axis++) identity[axis * (MatrixWidth + 1)] = 1;
            WriteMatrix(target, identity);
        }
    }
    internal T ReadDefault<T>(int index = 0) where T : unmanaged
    {
        Span<byte> bytes = stackalloc byte[ElementSize]; bytes.Clear(); WriteDefaultElement(bytes, index); return Read<T>(bytes);
    }
    private T[] DefaultArray<T>() where T : unmanaged
    {
        var result = new T[Count];
        for (var i = 0; i < result.Length; i++) result[i] = ReadDefault<T>(i);
        return result;
    }
}
