using System.Buffers.Binary;
using Electron2D;

namespace Electron2D.Examples.PhysicsNetwork;

internal ref struct WireWriter(Span<byte> target)
{
    private Span<byte> _data = target;
    internal int Count;
    internal void U32(uint value) { BinaryPrimitives.WriteUInt32LittleEndian(_data[Count..], value); Count += 4; }
    internal void I32(int value) => U32(unchecked((uint)value));
    internal void U64(ulong value) { BinaryPrimitives.WriteUInt64LittleEndian(_data[Count..], value); Count += 8; }
    internal void F32(float value) => U32(BitConverter.SingleToUInt32Bits(value));
    internal void Bool(bool value) => U32(value ? 1u : 0u);
    internal void Vector(Vector2 value) { F32(value.X); F32(value.Y); }
    internal void Bytes(scoped ReadOnlySpan<byte> bytes) { bytes.CopyTo(_data[Count..]); Count += bytes.Length; }
    internal void Header(int kind) { U32(0x31504E45); I32(kind); }
    internal void Spec(ObjectSpec value)
    {
        U64(value.ID); U32(value.Generation); I32((int)value.Kind); I32(value.Owner); U32(value.ControlEpoch);
        Vector(value.Spawn); U64(value.First); U64(value.Second);
    }
    internal void Input(InputCommand value)
    { U64(value.Tick); U64(value.ID); U32(value.Generation); U32(value.Epoch); F32(value.Input.Move); Bool(value.Input.Jump); }
    internal void Event(GameEvent value)
    { U64(value.Sequence); U64(value.Tick); U64(value.ID); U32(value.Generation); U64(value.Other); U32(value.OtherGeneration); I32(value.Kind); }
}
internal ref struct WireReader(ReadOnlySpan<byte> source)
{
    private ReadOnlySpan<byte> _data = source;
    internal int Offset;
    internal int Remaining => _data.Length - Offset;
    internal ReadOnlySpan<byte> Bytes(int count)
    {
        Require(count >= 0 && count <= Remaining); var result = _data.Slice(Offset, count); Offset += count; return result;
    }
    internal uint U32() => BinaryPrimitives.ReadUInt32LittleEndian(Bytes(4));
    internal int I32() => unchecked((int)U32());
    internal ulong U64() => BinaryPrimitives.ReadUInt64LittleEndian(Bytes(8));
    internal float F32() { var value = BitConverter.UInt32BitsToSingle(U32()); Require(float.IsFinite(value)); return value; }
    internal bool Bool() { var value = U32(); Require(value <= 1); return value != 0; }
    internal Vector2 Vector() => new(F32(), F32());
    internal int Header() { Require(U32() == 0x31504E45); return I32(); }
    internal int Count(int maximum, int stride) { var value = I32(); Require(value >= 0 && value <= maximum && value <= Remaining / stride); return value; }
    internal ObjectSpec Spec() => new(U64(), U32(), (ObjectKind)I32(), I32(), U32(), Vector(), U64(), U64());
    internal InputCommand Input()
    {
        var value = new InputCommand(U64(), U64(), U32(), U32(), new(F32(), Bool()));
        Require(value.ID != 0 && value.Generation != 0 && value.Epoch != 0 && Math.Abs(value.Input.Move) <= 1); return value;
    }
    internal GameEvent Event()
    {
        var value = new GameEvent(U64(), U64(), U64(), U32(), U64(), U32(), I32());
        Require(value.Sequence != 0 && value.ID != 0 && value.Generation != 0 && value.Other != 0 && value.OtherGeneration != 0 && value.Kind is 1 or 2); return value;
    }
    internal void End() => Require(Remaining == 0);
    internal static void Require(bool value) { if (!value) throw new InvalidDataException("Malformed physics-game message."); }
}

// Delays, drops, duplicates and reorders application packets before real ENet delivery.
internal sealed class ImpairedLink(SceneMultiplayer multiplayer, bool impaired)
{
    internal const int PacketBytes = 32768;
    private const int Capacity = 128;
    private readonly byte[] _storage = new byte[Capacity * PacketBytes];
    private readonly double[] _due = new double[Capacity];
    private readonly int[] _sizes = new int[Capacity], _peers = new int[Capacity];
    private readonly TransferMode[] _modes = new TransferMode[Capacity];
    private uint _random = 0x371fac53;
    private int _sequence;
    internal long SentBytes, AttemptedBytes;
    internal int Dropped, Duplicated, Delayed, Reordered, Sent;
    private uint Next() { _random ^= _random << 13; _random ^= _random >> 17; _random ^= _random << 5; return _random; }
    internal void Send(int peer, ReadOnlySpan<byte> packet, double now, TransferMode mode = TransferMode.Unreliable)
    {
        if (packet.IsEmpty || packet.Length > PacketBytes) throw new InvalidOperationException("Packet exceeds the prepared network budget.");
        AttemptedBytes += packet.Length; var sequence = ++_sequence;
        if (impaired && sequence % 7 == 0) { Dropped++; return; }
        Queue(peer, packet, now, mode);
        if (impaired && sequence % 11 == 0) { Duplicated++; Queue(peer, packet, now + .025, mode); }
    }
    private void Queue(int peer, ReadOnlySpan<byte> packet, double now, TransferMode mode)
    {
        var slot = Array.FindIndex(_sizes, static size => size == 0);
        if (slot < 0) throw new InvalidOperationException("Network delay queue exhausted its bounded capacity.");
        _due[slot] = now + (impaired ? .02 + Next() % 80 / 1000d : 0);
        _sizes[slot] = packet.Length; _peers[slot] = peer; _modes[slot] = mode;
        packet.CopyTo(_storage.AsSpan(slot * PacketBytes, packet.Length));
        if (impaired) Delayed++;
    }
    internal void Forget(int peer)
    {
        for (var i = 0; i < Capacity; i++) if (_peers[i] == peer) _sizes[i] = 0;
    }
    internal void Flush(double now)
    {
        while (true)
        {
            var slot = -1;
            for (var i = 0; i < Capacity; i++) if (_sizes[i] != 0 && _due[i] <= now && (slot < 0 || _due[i] < _due[slot])) slot = i;
            if (slot < 0) return;
            for (var i = 0; i < slot; i++) if (_sizes[i] != 0 && _due[i] > _due[slot]) { Reordered++; break; }
            var size = _sizes[slot]; _sizes[slot] = 0;
            multiplayer.SendBytes(_storage.AsSpan(slot * PacketBytes, size), _peers[slot], _modes[slot]); SentBytes += size; Sent++;
        }
    }
}
