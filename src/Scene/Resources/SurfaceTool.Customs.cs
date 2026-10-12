using System.Buffers.Binary;

namespace Electron2D;

public sealed partial class SurfaceTool
{
    private readonly record struct CustomVertex(Vector4 Value, Vector4i Bits = default, int RawFormat = 0);
    /// <summary>Selects the packed format for a numerical custom channel.</summary>
    /// <param name="channelIndex">Zero through three.</param>
    /// <param name="format">One of eight layouts, or Max to disable an uncaptured channel.</param>
    /// <remarks>Requires Begin. Selecting a format does not activate the channel; SetCustom does.
    /// Changing an active format preflights captured and current values, then reencodes them when arrays are produced. Disabling an active
    /// channel after vertices exist rejects before mutation.</remarks>
    public void SetCustomFormat(int channelIndex, MeshCustomFormat format)
    {
        lock (_gate)
        {
            Active(); Channel(channelIndex); if ((uint)format > (uint)MeshCustomFormat.Max) throw new ArgumentOutOfRangeException(nameof(format));
            var mask = MeshCustomCodec.Mask(channelIndex);
            if (format == MeshCustomFormat.Max && _vertices.Count != 0 && (_format & mask) != 0) throw new InvalidOperationException("Cannot disable a captured custom channel.");
            if (format != MeshCustomFormat.Max && (_format & mask) != 0) { Span<byte> record = stackalloc byte[16]; foreach (var vertex in _vertices) WriteCustom(vertex, channelIndex, format, record); WriteCustom(_current, channelIndex, format, record); }
            _customFormats[channelIndex] = format;
            _format &= ~(Mesh.ArrayFormat)(7ul << MeshCustomCodec.Shift(channelIndex));
            if (format == MeshCustomFormat.Max) _format &= ~mask;
            else if ((_format & mask) != 0) _format |= (Mesh.ArrayFormat)((ulong)format << MeshCustomCodec.Shift(channelIndex));
        }
    }
    /// <summary>Returns the selected packed custom format.</summary>
    /// <param name="channelIndex">Zero through three.</param>
    /// <returns>The layout, or Max before selection/after Clear.</returns>
    public MeshCustomFormat GetCustomFormat(int channelIndex) { lock (_gate) { ThrowIfDisposed(); Channel(channelIndex); return _customFormats[channelIndex]; } }
    /// <summary>Assigns the custom components captured by subsequent vertices.</summary>
    /// <param name="channelIndex">Zero through three.</param>
    /// <param name="customColor">Finite raw RGBA components, interpreted by the selected layout.</param>
    /// <remarks>Requires a selected format and first activation before the first vertex. Normalized formats
    /// clamp/truncate; half authoring truncates and flushes small values according to its packed codec.
    /// A value not representable as finite half storage rejects before changing the current value.</remarks>
    public void SetCustom(int channelIndex, Color customColor)
    {
        lock (_gate)
        {
            Active(); Channel(channelIndex); var format = _customFormats[channelIndex]; if (format == MeshCustomFormat.Max) throw new InvalidOperationException("Select a custom format before assigning values.");
            CanActivate(MeshCustomCodec.Mask(channelIndex)); Span<byte> bytes = stackalloc byte[16]; var value = new Vector4(customColor.R, customColor.G, customColor.B, customColor.A); MeshCustomCodec.Write(format, value, bytes);
            _current = WithCustom(_current, channelIndex, new(value)); _format |= MeshCustomCodec.Mask(channelIndex) | (Mesh.ArrayFormat)((ulong)format << MeshCustomCodec.Shift(channelIndex));
        }
    }
    private static Vertex Defaults(Vertex value) => value with { Custom0 = new(new(0, 0, 0, 1)), Custom1 = new(new(0, 0, 0, 1)), Custom2 = new(new(0, 0, 0, 1)), Custom3 = new(new(0, 0, 0, 1)) };
    private static void Channel(int channel) { if ((uint)channel >= 4) throw new ArgumentOutOfRangeException(nameof(channel)); }
    private static CustomVertex CustomValue(Vertex vertex, int channel) => channel switch { 0 => vertex.Custom0, 1 => vertex.Custom1, 2 => vertex.Custom2, 3 => vertex.Custom3, _ => throw new ArgumentOutOfRangeException(nameof(channel)) };
    private static Vertex WithCustom(Vertex vertex, int channel, CustomVertex value) => channel switch { 0 => vertex with { Custom0 = value }, 1 => vertex with { Custom1 = value }, 2 => vertex with { Custom2 = value }, 3 => vertex with { Custom3 = value }, _ => throw new ArgumentOutOfRangeException(nameof(channel)) };
    private void ValidateCustomCapture()
    {
        Span<byte> bytes = stackalloc byte[16];
        for (var channel = 0; channel < 4; channel++) if ((_format & MeshCustomCodec.Mask(channel)) != 0) WriteCustom(_current, channel, _customFormats[channel], bytes);
    }
    private Vertex IndexKey(Vertex vertex)
    {
        Span<byte> bytes = stackalloc byte[16];
        for (var channel = 0; channel < 4; channel++)
            if ((_format & MeshCustomCodec.Mask(channel)) != 0)
            {
                var format = _customFormats[channel]; WriteCustom(vertex, channel, format, bytes); var words = new Vector4i();
                for (var i = 0; i < MeshCustomCodec.Stride(format) / 4; i++) words[i] = BinaryPrimitives.ReadInt32LittleEndian(bytes[(i * 4)..]);
                vertex = WithCustom(vertex, channel, new(CustomValue(vertex, channel).Value, words, (int)format + 1));
            }
        return vertex;
    }
    private static CustomVertex ReadCustom(MeshSurfaceData data, int channel, int vertex)
    {
        if (data.Custom(channel) is not { } custom) return new(new(0, 0, 0, 1));
        var stride = MeshCustomCodec.Stride(custom.Format); var bytes = custom.Data.AsSpan(vertex * stride, stride); var words = new Vector4i();
        for (var i = 0; i < stride / 4; i++) words[i] = BinaryPrimitives.ReadInt32LittleEndian(bytes[(i * 4)..]);
        return new(MeshCustomCodec.Read(custom.Format, bytes), words, (int)custom.Format + 1);
    }
    private static void WriteCustom(Vertex vertex, int channel, MeshCustomFormat format, Span<byte> bytes)
    {
        var value = CustomValue(vertex, channel);
        if (value.RawFormat == (int)format + 1) { for (var i = 0; i < MeshCustomCodec.Stride(format) / 4; i++) BinaryPrimitives.WriteInt32LittleEndian(bytes[(i * 4)..], value.Bits[i]); }
        else MeshCustomCodec.Write(format, value.Value, bytes);
    }
}
