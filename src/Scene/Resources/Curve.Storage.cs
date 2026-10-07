using CurvePoint = (Electron2D.Vector2 Position, float Left, float Right, Electron2D.Curve.TangentMode LeftMode, Electron2D.Curve.TangentMode RightMode);

namespace Electron2D;

public sealed partial class Curve
{
    private static readonly PropertyDescriptor CurveStorage = new PropertyDescriptor<Curve, byte[]>("_curve_data", c => c.SaveCurve(), (c, v) => c.LoadCurve(v), _ => Convert.FromHexString("45435631000000000000803f000000000000803f6400000000000000"), stored: true);
    private byte[] SaveCurve()
    {
        lock (_gate)
        {
            ThrowIfDisposed(); if (_points.Count > 65536) throw new InvalidOperationException("Curve archive exceeds the point budget.");
            using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream);
            writer.Write(0x31564345u); writer.Write(_minDomain); writer.Write(_maxDomain); writer.Write(_minValue); writer.Write(_maxValue); writer.Write(_bakeResolution); writer.Write(_points.Count);
            foreach (var p in _points) { writer.Write(p.Position.X); writer.Write(p.Position.Y); writer.Write(p.Left); writer.Write(p.Right); writer.Write((int)p.LeftMode); writer.Write((int)p.RightMode); }
            return stream.ToArray();
        }
    }
    private void LoadCurve(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes); if (bytes.Length < 28 || bytes.Length > 28 + 65536 * 24) throw new InvalidDataException("Invalid curve archive size.");
        using var stream = new MemoryStream(bytes, false); using var reader = new BinaryReader(stream);
        if (reader.ReadUInt32() != 0x31564345u) throw new InvalidDataException("Unsupported curve archive.");
        var minX = reader.ReadSingle(); var maxX = reader.ReadSingle(); var minY = reader.ReadSingle(); var maxY = reader.ReadSingle(); var resolution = reader.ReadInt32(); var count = reader.ReadInt32();
        if (!float.IsFinite(minX) || !float.IsFinite(maxX) || !float.IsFinite(minY) || !float.IsFinite(maxY) || !float.IsFinite(maxX - minX) || !float.IsFinite(maxY - minY) || maxX <= minX || maxY <= minY || resolution < 1 || resolution > 1000 || count < 0 || count > 65536 || bytes.Length != 28 + count * 24) throw new InvalidDataException("Invalid curve archive bounds.");
        var points = new CurvePoint[count]; var last = float.NegativeInfinity;
        for (var i = 0; i < count; i++)
        {
            var p = (Position: new Vector2(reader.ReadSingle(), reader.ReadSingle()), Left: reader.ReadSingle(), Right: reader.ReadSingle(), LeftMode: (TangentMode)reader.ReadInt32(), RightMode: (TangentMode)reader.ReadInt32());
            if (!p.Position.IsFinite() || p.Position.X < last || p.LeftMode < TangentMode.Free || p.LeftMode >= TangentMode.Count || p.RightMode < TangentMode.Free || p.RightMode >= TangentMode.Count || p.LeftMode == TangentMode.Free && !float.IsFinite(p.Left) || p.RightMode == TangentMode.Free && !float.IsFinite(p.Right)) throw new InvalidDataException("Invalid curve archive point.");
            last = p.Position.X; points[i] = p;
        }
        lock (_gate) { ThrowIfDisposed(); _points.Clear(); _points.AddRange(points); _minDomain = minX; _maxDomain = maxX; _minValue = minY; _maxValue = maxY; _bakeResolution = resolution; _baked = []; _dirty = true; }
        EmitChanged();
    }
}
