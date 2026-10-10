namespace Electron2D;

/// <summary>Owns a bounded, reusable encoding of an authoritative physics state with portable object identities.</summary>
/// <remarks>A snapshot contains no process-local object identifiers or solver types. Use a
/// <see cref="PhysicsSnapshotMap"/> to capture/apply a compatible authored world. Transport authentication,
/// packet ordering and controlling-peer validation belong to the networking caller.</remarks>
public sealed class PhysicsSnapshot
{
    private byte[] _data = [];
    private int _length;
    private readonly int _owner = Environment.CurrentManagedThreadId;
    /// <summary>Creates empty reusable storage with a maximum encoded byte budget.</summary>
    /// <param name="maxBytes">Header size through 64 MiB; no payload storage is allocated until capture or decoding.</param>
    /// <exception cref="ArgumentOutOfRangeException">The budget is outside the supported range.</exception>
    public PhysicsSnapshot(int maxBytes = 64 * 1024 * 1024)
    {
        if (maxBytes < PhysicsSnapshotFormat.HeaderSize || maxBytes > 64 * 1024 * 1024) throw new ArgumentOutOfRangeException(nameof(maxBytes));
        MaxBytes = maxBytes;
    }
    /// <summary>Gets the immutable encoded payload budget.</summary>
    public int MaxBytes { get; }
    /// <summary>Gets the source world's captured simulation tick.</summary>
    /// <exception cref="InvalidOperationException">No complete payload exists or access is off-owner.</exception>
    public ulong Tick { get { var reader = new PhysicsSnapshotReader(Data); return PhysicsSnapshotFormat.Header(ref reader).Tick; } }
    /// <summary>Gets the count of captured collision objects.</summary>
    /// <exception cref="InvalidOperationException">No complete payload exists or access is off-owner.</exception>
    public int ObjectCount { get { var reader = new PhysicsSnapshotReader(Data); return PhysicsSnapshotFormat.Header(ref reader).Objects; } }
    /// <summary>Returns the exact writable span size required by <see cref="WriteTo"/>.</summary>
    /// <returns>The encoded payload size in bytes.</returns>
    /// <exception cref="InvalidOperationException">No complete payload exists or access is off-owner.</exception>
    public int GetEncodedSize() => Data.Length;
    /// <summary>Copies the complete encoding to caller-owned packet storage.</summary>
    /// <param name="destination">A span at least as large as <see cref="GetEncodedSize"/>.</param>
    /// <returns>The number of bytes copied.</returns>
    /// <exception cref="ArgumentException">The destination is too small; it remains unchanged.</exception>
    /// <exception cref="InvalidOperationException">No complete payload exists or access is off-owner.</exception>
    public int WriteTo(Span<byte> destination) { var data = Data; if (destination.Length < data.Length) throw new ArgumentException("Snapshot destination is too small.", nameof(destination)); data.CopyTo(destination); return data.Length; }
    /// <summary>Validates and copies a complete encoded packet into reusable owned storage.</summary>
    /// <param name="source">The exact packet payload, borrowed only for this call.</param>
    /// <remarks>Malformed or over-budget payloads leave the previous complete snapshot unchanged.
    /// Applying to a world performs additional identity, generation and authoring checks before mutation.</remarks>
    /// <exception cref="InvalidDataException">The payload is malformed, unsupported or over budget.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner.</exception>
    public void ReadFrom(ReadOnlySpan<byte> source)
    {
        Check(); if (source.Length > MaxBytes) throw new InvalidDataException("Physics snapshot exceeds its byte budget.");
        PhysicsSnapshotFormat.Validate(source); Reserve(source.Length); source.CopyTo(_data); _length = source.Length;
    }
    private void Check() { if (_owner != Environment.CurrentManagedThreadId) throw new InvalidOperationException("Physics snapshot storage requires its owner thread."); }
    private void Reserve(int bytes) { if (_data.Length < bytes) Array.Resize(ref _data, Math.Min(MaxBytes, Math.Max(bytes, Math.Max(256, _data.Length * 2)))); }
    internal ReadOnlySpan<byte> Data { get { Check(); if (_length == 0) throw new InvalidOperationException("The physics snapshot has no complete capture or payload."); return _data.AsSpan(0, _length); } }
    internal Span<byte> BeginCapture(int bytes)
    {
        Check(); if (bytes > MaxBytes) throw new InvalidOperationException("Captured physics state exceeds the snapshot byte budget.");
        Reserve(bytes); _length = 0; return _data.AsSpan(0, bytes);
    }
    internal void CompleteCapture(int bytes) { PhysicsSnapshotFormat.Validate(_data.AsSpan(0, bytes)); _length = bytes; }
}
