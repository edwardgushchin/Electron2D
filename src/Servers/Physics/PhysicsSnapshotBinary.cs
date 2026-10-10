using System.Buffers.Binary;

namespace Electron2D;

internal ref struct PhysicsSnapshotWriter(Span<byte> destination)
{
    private Span<byte> _destination = destination;
    internal int Position { get; private set; }
    internal void U32(uint value) { if (!_destination.IsEmpty) BinaryPrimitives.WriteUInt32LittleEndian(_destination[Position..], value); Position = checked(Position + 4); }
    internal void I32(int value) => U32(unchecked((uint)value));
    internal void U64(ulong value) { if (!_destination.IsEmpty) BinaryPrimitives.WriteUInt64LittleEndian(_destination[Position..], value); Position = checked(Position + 8); }
    internal void F32(float value) => U32(BitConverter.SingleToUInt32Bits(value));
    internal void Bool(bool value) => U32(value ? 1u : 0u);
    internal void Vector(Vector2 value) { F32(value.X); F32(value.Y); }
    internal void Pose(Transform value) { Vector(value.X); Vector(value.Y); Vector(value.Origin); }
}

internal ref struct PhysicsSnapshotReader(ReadOnlySpan<byte> source)
{
    private ReadOnlySpan<byte> _source = source;
    internal int Position { get; private set; }
    internal int Remaining => _source.Length - Position;
    internal uint U32() { Need(4); var value = BinaryPrimitives.ReadUInt32LittleEndian(_source[Position..]); Position += 4; return value; }
    internal int I32() => unchecked((int)U32());
    internal ulong U64() { Need(8); var value = BinaryPrimitives.ReadUInt64LittleEndian(_source[Position..]); Position += 8; return value; }
    internal float F32() { var value = BitConverter.UInt32BitsToSingle(U32()); Require(float.IsFinite(value)); return value; }
    internal bool Bool() { var value = U32(); Require(value <= 1); return value != 0; }
    internal Vector2 Vector() => new(F32(), F32());
    internal Transform Pose() => new(Vector(), Vector(), Vector());
    internal int Count(int minimumStride) { var count = I32(); Require(count >= 0 && count <= Remaining / minimumStride); return count; }
    internal void Need(int bytes) => Require(bytes >= 0 && bytes <= Remaining);
    internal void End() => Require(Remaining == 0);
    internal static void Require(bool condition) { if (!condition) throw new InvalidDataException("Invalid physics snapshot encoding."); }
}

// Schema equality detects incompatible authoring. It is not transport authentication or a signature.
internal struct PhysicsSnapshotSchema
{
    private ulong _first = 14695981039346656037UL, _second = 7809847782465536322UL;
    public PhysicsSnapshotSchema() { }
    internal readonly (ulong, ulong) Value => (_first, _second);
    internal void Add(ulong value)
    {
        for (var i = 0; i < 8; i++) { var b = (byte)value; _first = unchecked((_first ^ b) * 1099511628211UL); _second = unchecked((_second ^ b) * 14029467366897019727UL); value >>= 8; }
    }
    internal void Add(uint value) => Add((ulong)value);
    internal void Add(int value) => Add(unchecked((uint)value));
    internal void Add(bool value) => Add(value ? 1u : 0u);
    internal void Add(float value) => Add(value == 0 ? 0u : BitConverter.SingleToUInt32Bits(value));
    internal void Add(Vector2 value) { Add(value.X); Add(value.Y); }
    internal void Add(Transform value) { Add(value.X); Add(value.Y); Add(value.Origin); }
    internal void Add(string? value) { Add(value?.Length ?? -1); if (value is not null) foreach (var c in value) Add((uint)c); }
}
