using System.Buffers.Binary;

namespace Electron2D;

public partial class TileMapLayer
{
    /// <summary>Gets or sets a copied little-endian tile array: version zero followed by 12-byte cell records.</summary>
    /// <remarks>Coordinates wrap to signed 16-bit values on serialization. Each record contains X, Y,
    /// source identity, atlas X, atlas Y and alternative identity as six 16-bit fields.
    /// Malformed input is rejected before authored cells change; an empty array clears the layer.</remarks>
    public byte[] TileMapData
    {
        get
        {
            ThrowIfDisposed(); if (_cells.Count == 0) return [];
            var bytes = new byte[checked(2 + _cells.Count * 12)]; var offset = 2;
            foreach (var pair in _cells.OrderBy(p => p.Key.X).ThenBy(p => p.Key.Y))
            {
                var row = bytes.AsSpan(offset, 12); var c = pair.Value;
                BinaryPrimitives.WriteInt16LittleEndian(row, unchecked((short)pair.Key.X)); BinaryPrimitives.WriteInt16LittleEndian(row[2..], unchecked((short)pair.Key.Y));
                BinaryPrimitives.WriteUInt16LittleEndian(row[4..], (ushort)c.SourceID); BinaryPrimitives.WriteUInt16LittleEndian(row[6..], (ushort)c.Atlas.X);
                BinaryPrimitives.WriteUInt16LittleEndian(row[8..], (ushort)c.Atlas.Y); BinaryPrimitives.WriteUInt16LittleEndian(row[10..], (ushort)c.Alternative); offset += 12;
            }
            return bytes;
        }
        set
        {
            EnsureMutable(); ArgumentNullException.ThrowIfNull(value);
            if (value.Length == 0) { Clear(); return; }
            if (value.Length < 2 || (value.Length - 2) % 12 != 0 || BinaryPrimitives.ReadUInt16LittleEndian(value) != 0) throw new ArgumentException("Invalid tile array format.", nameof(value));
            var restored = new Dictionary<Vector2i, Cell>();
            for (var i = 2; i < value.Length; i += 12)
            {
                var row = value.AsSpan(i, 12); var coords = new Vector2i(BinaryPrimitives.ReadInt16LittleEndian(row), BinaryPrimitives.ReadInt16LittleEndian(row[2..]));
                var source = BinaryPrimitives.ReadUInt16LittleEndian(row[4..]); var atlas = new Vector2i(BinaryPrimitives.ReadUInt16LittleEndian(row[6..]), BinaryPrimitives.ReadUInt16LittleEndian(row[8..]));
                var alternative = BinaryPrimitives.ReadUInt16LittleEndian(row[10..]);
                if (source == ushort.MaxValue || atlas.X == ushort.MaxValue || atlas.Y == ushort.MaxValue || alternative == ushort.MaxValue) restored.Remove(coords);
                else restored[coords] = new(source, atlas, alternative);
            }
            foreach (var coords in _cells.Keys) _dirtyCells.Add(coords);
            _cells.Clear(); foreach (var pair in restored) _cells.Add(pair.Key, pair.Value); MarkAll();
        }
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(TileProperties);
    private static readonly PropertyDescriptor[] TileProperties =
    [
        new PropertyDescriptor<TileMapLayer, TileSet?>(nameof(TileSet), n => n.TileSet, (n,v) => n.TileSet = v, _ => null, stored: true),
        new PropertyDescriptor<TileMapLayer, bool>(nameof(Enabled), n => n.Enabled, (n,v) => n.Enabled = v, _ => true, stored: true),
        new PropertyDescriptor<TileMapLayer, bool>(nameof(CollisionEnabled), n => n.CollisionEnabled, (n,v) => n.CollisionEnabled = v, _ => true, stored: true),
        new PropertyDescriptor<TileMapLayer, bool>(nameof(UseKinematicBodies), n => n.UseKinematicBodies, (n,v) => n.UseKinematicBodies = v, _ => false, stored: true),
        new PropertyDescriptor<TileMapLayer, int>(nameof(PhysicsQuadrantSize), n => n.PhysicsQuadrantSize, (n,v) => n.PhysicsQuadrantSize = v, _ => 16, stored: true),
        new PropertyDescriptor<TileMapLayer, DebugVisibilityMode>(nameof(CollisionVisibilityMode), n => n.CollisionVisibilityMode, (n,v) => n.CollisionVisibilityMode = v, _ => DebugVisibilityMode.Default, stored: true),
        new PropertyDescriptor<TileMapLayer, byte[]>(nameof(TileMapData), n => n.TileMapData, (n,v) => n.TileMapData = v, _ => [], stored: true)
    ];
}
