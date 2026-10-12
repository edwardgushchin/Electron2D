using System.Buffers.Binary;

namespace Electron2D;

/// <summary>Selects the packed layout of one numerical mesh custom channel.</summary>
public enum MeshCustomFormat
{
    /// <summary>Four unsigned normalized eight-bit components.</summary>
    RGBA8UNORM = 0,
    /// <summary>Four signed normalized eight-bit components.</summary>
    RGBA8SNORM = 1,
    /// <summary>Two little-endian binary16 components.</summary>
    RGHalf = 2,
    /// <summary>Four little-endian binary16 components.</summary>
    RGBAHalf = 3,
    /// <summary>One little-endian float32 component.</summary>
    RFloat = 4,
    /// <summary>Two little-endian float32 components.</summary>
    RGFloat = 5,
    /// <summary>Three little-endian float32 components.</summary>
    RGBFloat = 6,
    /// <summary>Four little-endian float32 components.</summary>
    RGBAFloat = 7,
    /// <summary>No selected authoring channel; not a storage format.</summary>
    Max = 8
}

/// <summary>Supplies copied packed numerical values for one mesh custom channel.</summary>
/// <remarks>Mesh import copies this object and its bytes. Surface queries return independent copies.
/// The format determines a four, eight, twelve or sixteen-byte vertex record.</remarks>
public sealed class MeshCustomData
{
    /// <summary>Creates an empty unsigned normalized channel payload.</summary>
    public MeshCustomData() { }
    /// <summary>Gets or sets the packed layout.</summary>
    /// <value>RGBA8UNORM initially; Max is not accepted as stored geometry.</value>
    public MeshCustomFormat Format { get; set; }
    /// <summary>Gets or sets contiguous little-endian packed records.</summary>
    /// <value>Empty initially; consumed data must contain one complete finite record per vertex.</value>
    public byte[] Data { get; set; } = [];
    internal Vector4[] Values = [];
    internal MeshCustomData Copy() { ArgumentNullException.ThrowIfNull(Data); return new() { Format = Format, Data = (byte[])Data.Clone() }; }
    internal void Validate(int count)
    {
        ArgumentNullException.ThrowIfNull(Data); var stride = MeshCustomCodec.Stride(Format);
        if (Data.LongLength != (long)count * stride) throw new ArgumentException("Custom data requires one packed record per vertex.");
        for (var i = 0; i < count; i++) if (!MeshCustomCodec.Read(Format, Data.AsSpan(i * stride)).IsFinite()) throw new ArgumentException("Custom records must decode to finite components.");
    }
    internal void Prepare(int count)
    {
        Validate(count); Values = new Vector4[count]; for (var i = 0; i < count; i++) Refresh(i);
    }
    internal void Refresh(int index)
    {
        var value = MeshCustomCodec.Read(Format, Data.AsSpan(index * MeshCustomCodec.Stride(Format)));
        if (Format == MeshCustomFormat.RGBA8SNORM) value = new(Math.Max(-1, value.X), Math.Max(-1, value.Y), Math.Max(-1, value.Z), Math.Max(-1, value.W));
        Values[index] = value;
    }
}

internal static class MeshCustomCodec
{
    internal static Mesh.ArrayFormat Mask(int channel) => (Mesh.ArrayFormat)(64ul << channel);
    internal static int Shift(int channel) => 13 + channel * 3;
    internal const Mesh.ArrayFormat FormatMask = (Mesh.ArrayFormat)(0xffful << 13);
    internal static Mesh.ArrayFormat Channels(MeshSurfaceData data)
    {
        var format = Mesh.ArrayFormat.None;
        for (var i = 0; i < 4; i++) if (data.Custom(i) is { } channel) format |= Mask(i) | (Mesh.ArrayFormat)((ulong)channel.Format << Shift(i));
        return format;
    }
    internal static int AttributeStride(Mesh.ArrayFormat format)
    {
        var uvSize = (format & Mesh.ArrayFormat.CompressAttributes) != 0 ? 4 : 8;
        var bytes = ((format & Mesh.ArrayFormat.Color) != 0 ? 4 : 0) + ((format & Mesh.ArrayFormat.TexUV) != 0 ? uvSize : 0) + ((format & Mesh.ArrayFormat.TexUV2) != 0 ? uvSize : 0);
        for (var i = 0; i < 4; i++) if ((format & Mask(i)) != 0) bytes += Stride((MeshCustomFormat)((ulong)format >> Shift(i) & 7));
        return bytes;
    }
    internal static int Stride(MeshCustomFormat format) => format switch
    {
        MeshCustomFormat.RGBA8UNORM or MeshCustomFormat.RGBA8SNORM or MeshCustomFormat.RGHalf or MeshCustomFormat.RFloat => 4,
        MeshCustomFormat.RGBAHalf or MeshCustomFormat.RGFloat => 8,
        MeshCustomFormat.RGBFloat => 12,
        MeshCustomFormat.RGBAFloat => 16,
        _ => throw new ArgumentOutOfRangeException(nameof(format))
    };
    internal static Vector4 Read(MeshCustomFormat format, ReadOnlySpan<byte> bytes)
    {
        _ = Stride(format); var value = new Vector4(0, 0, 0, 1);
        var components = format switch { MeshCustomFormat.RGHalf or MeshCustomFormat.RGFloat => 2, MeshCustomFormat.RFloat => 1, MeshCustomFormat.RGBFloat => 3, _ => 4 };
        for (var i = 0; i < components; i++) value[i] = format switch
        {
            MeshCustomFormat.RGBA8UNORM => bytes[i] / 255f,
            MeshCustomFormat.RGBA8SNORM => unchecked((sbyte)bytes[i]) / 127f,
            MeshCustomFormat.RGHalf or MeshCustomFormat.RGBAHalf => (float)BitConverter.UInt16BitsToHalf(BinaryPrimitives.ReadUInt16LittleEndian(bytes[(i * 2)..])),
            _ => BinaryPrimitives.ReadSingleLittleEndian(bytes[(i * 4)..])
        };
        return value;
    }
    internal static void Write(MeshCustomFormat format, Vector4 value, Span<byte> bytes)
    {
        if (!value.IsFinite()) throw new ArgumentException("Custom authoring values must be finite.", nameof(value));
        _ = Stride(format); var components = format switch { MeshCustomFormat.RGHalf or MeshCustomFormat.RGFloat => 2, MeshCustomFormat.RFloat => 1, MeshCustomFormat.RGBFloat => 3, _ => 4 };
        for (var i = 0; i < components; i++)
        {
            var component = value[i];
            if (format == MeshCustomFormat.RGBA8UNORM) bytes[i] = (byte)Math.Clamp(component * 255d, 0, 255);
            else if (format == MeshCustomFormat.RGBA8SNORM) bytes[i] = unchecked((byte)(sbyte)Math.Clamp(component * 127d, -128, 127));
            else if (format is MeshCustomFormat.RGHalf or MeshCustomFormat.RGBAHalf)
            {
                var word = HalfWord(component); if ((word & 0x7c00) == 0x7c00) throw new ArgumentException("Custom half components must remain finite.", nameof(value));
                BinaryPrimitives.WriteUInt16LittleEndian(bytes[(i * 2)..], word);
            }
            else BinaryPrimitives.WriteSingleLittleEndian(bytes[(i * 4)..], component);
        }
    }
    private static ushort HalfWord(float value)
    {
        var bits = BitConverter.SingleToUInt32Bits(value); var exponent = bits & 0x7f800000u;
        if (exponent <= 0x38000000u) return 0;
        if (exponent >= 0x47800000u) return (ushort)((bits >> 16 & 0x8000u) | 0x7c00u);
        return (ushort)((bits >> 16 & 0x8000u) | (exponent - 0x38000000u) >> 13 | (bits & 0x7fffffu) >> 13);
    }
}
