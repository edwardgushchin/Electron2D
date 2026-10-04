namespace Electron2D;

/// <summary>Stores a seekable, growable byte stream with copied array boundaries.</summary>
/// <remarks>Clear retains capacity; Duplicate copies bytes and resets position and endian configuration.
/// Resize clamps a cursor beyond the new end, avoiding negative reads. No native socket is required.</remarks>
public class StreamPeerBuffer : StreamPeer
{
    private readonly MemoryStream _data = new();
    /// <summary>Creates an empty buffer.</summary>
    public StreamPeerBuffer() { }
    /// <summary>Gets a caller-owned byte copy, or replaces bytes with a copied array and resets position.</summary><value>Empty initially.</value>
    public byte[] DataArray
    {
        get { CheckStream(); return _data.ToArray(); }
        set { CheckStream(); ArgumentNullException.ThrowIfNull(value); _data.SetLength(0); _data.Position = 0; _data.Write(value); _data.Position = 0; }
    }
    /// <inheritdoc />
    public override int GetAvailableBytes() { CheckStream(); return checked((int)(_data.Length - _data.Position)); }
    /// <summary>Moves within the inclusive interval from zero to GetSize.</summary><param name="position">Byte offset.</param><exception cref="ArgumentOutOfRangeException">The position lies outside the buffer.</exception>
    public void Seek(int position) { CheckStream(); if (position < 0 || position > _data.Length) throw new ArgumentOutOfRangeException(nameof(position)); _data.Position = position; }
    /// <summary>Returns the logical byte length.</summary><returns>Byte count.</returns>
    public int GetSize() { CheckStream(); return checked((int)_data.Length); }
    /// <summary>Returns the cursor byte offset.</summary><returns>Byte offset.</returns>
    public int GetPosition() { CheckStream(); return checked((int)_data.Position); }
    /// <summary>Changes logical length, zero-filling growth and clamping the cursor when shrinking.</summary><param name="size">Nonnegative length.</param>
    public void Resize(int size) { CheckStream(); ArgumentOutOfRangeException.ThrowIfNegative(size); _data.SetLength(size); if (_data.Position > size) _data.Position = size; }
    /// <summary>Clears bytes and cursor while retaining prepared capacity.</summary>
    public void Clear() { CheckStream(); _data.SetLength(0); _data.Position = 0; }
    /// <summary>Copies bytes into a new buffer with default byte order and cursor zero.</summary><returns>A caller-owned buffer.</returns>
    public StreamPeerBuffer Duplicate() { CheckStream(); return new() { DataArray = _data.ToArray() }; }
    /// <inheritdoc />
    protected override int ReadCore(Span<byte> destination, bool block) => _data.Read(destination);
    /// <inheritdoc />
    protected override int WriteCore(ReadOnlySpan<byte> data, bool block) { _data.Write(data); return data.Length; }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) yield return property;
        yield return new PropertyDescriptor<StreamPeerBuffer, byte[]>(nameof(DataArray), s => s.DataArray, (s, v) => s.DataArray = v, _ => Array.Empty<byte>(), stored: true);
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) _data.Dispose(); base.Dispose(disposing); }
}
