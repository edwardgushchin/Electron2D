namespace Electron2D;

public sealed partial class ArrayMesh
{
    private const int ArchiveBudget = 64 * 1024 * 1024;
    private byte[] SaveSurfaces()
    {
        lock (Gate)
        {
            ThrowIfDisposed(); long bytes = 8; if (_surfaces.Count > 4096) throw new InvalidOperationException("Mesh archive surface budget exceeded.");
            foreach (var s in _surfaces) { var d = s.Data; if (s.Name.Length > 65536) throw new InvalidOperationException("Mesh archive name budget exceeded."); bytes += 36L + s.Name.Length * 2L + d.Vertices.Length * 8L + d.Colors.Length * 16L + d.UVs.Length * 8L + d.Indices.Length * 4L + d.Bones.Length * 4L + d.Weights.Length * 4L; }
            if (bytes > ArchiveBudget) throw new InvalidOperationException("Mesh archive byte budget exceeded.");
            using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream); writer.Write(0x31534d41u); writer.Write(_surfaces.Count);
            foreach (var s in _surfaces)
            {
                var d = s.Data; writer.Write((int)s.Primitive); writer.Write((ulong)s.Format); writer.Write(s.Name.Length); foreach (var c in s.Name) writer.Write((ushort)c); writer.Write(d.Vertices.Length); writer.Write(d.Colors.Length); writer.Write(d.UVs.Length); writer.Write(d.Indices.Length); writer.Write(d.Bones.Length);
                foreach (var v in d.Vertices) { writer.Write(v.X); writer.Write(v.Y); }
                foreach (var c in d.Colors) { writer.Write(c.R); writer.Write(c.G); writer.Write(c.B); writer.Write(c.A); }
                foreach (var v in d.UVs) { writer.Write(v.X); writer.Write(v.Y); }
                foreach (var i in d.Indices) writer.Write(i); foreach (var i in d.Bones) writer.Write(i); foreach (var w in d.Weights) writer.Write(w);
            }
            return stream.ToArray();
        }
    }
    private void LoadSurfaces(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes); if (bytes.Length > ArchiveBudget) throw new InvalidDataException("Mesh archive byte budget exceeded.");
        var surfaces = new List<Surface>();
        try
        {
            using var stream = new MemoryStream(bytes, writable: false); using var reader = new BinaryReader(stream); if (reader.ReadUInt32() != 0x31534d41u) throw new InvalidDataException("Invalid mesh archive version."); var count = Count(reader, 4096);
            for (var surface = 0; surface < count; surface++)
            {
                var primitive = (PrimitiveType)reader.ReadInt32(); var format = (ArrayFormat)reader.ReadUInt64(); const ArrayFormat allowed = ArrayFormat.Vertex | ArrayFormat.Use2DVertices | ArrayFormat.UseDynamicUpdate | ArrayFormat.Color | ArrayFormat.TexUV | ArrayFormat.Index | ArrayFormat.Bones | ArrayFormat.Weights | ArrayFormat.Use8BoneWeights; if ((format & ~allowed) != 0) throw new InvalidDataException("Unsupported mesh archive channels.");
                var nameLength = Count(reader, 65536); if (stream.Length - stream.Position < nameLength * 2L) throw new InvalidDataException("Truncated mesh name."); var name = new char[nameLength]; for (var i = 0; i < name.Length; i++) name[i] = (char)reader.ReadUInt16();
                var n = Count(reader, ArchiveBudget / 8); var colors = Count(reader, n); var uv = Count(reader, n); var indices = Count(reader, ArchiveBudget / 4); var bones = Count(reader, ArchiveBudget / 8);
                var needed = n * 8L + colors * 16L + uv * 8L + indices * 4L + bones * 8L; if (needed > stream.Length - stream.Position) throw new InvalidDataException("Truncated mesh channels.");
                var data = new MeshSurfaceData { Vertices = new Vector2[n], Colors = new Color[colors], UVs = new Vector2[uv], Indices = new int[indices], Bones = new int[bones], Weights = new float[bones] };
                for (var i = 0; i < n; i++) data.Vertices[i] = new(reader.ReadSingle(), reader.ReadSingle()); for (var i = 0; i < colors; i++) data.Colors[i] = new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()); for (var i = 0; i < uv; i++) data.UVs[i] = new(reader.ReadSingle(), reader.ReadSingle()); for (var i = 0; i < indices; i++) data.Indices[i] = reader.ReadInt32(); for (var i = 0; i < bones; i++) data.Bones[i] = reader.ReadInt32(); for (var i = 0; i < bones; i++) data.Weights[i] = reader.ReadSingle();
                data.Validate(primitive); if (data.SkinSlots != 0 && data.SkinSlots != ((format & ArrayFormat.Use8BoneWeights) != 0 ? 8 : 4)) throw new InvalidDataException("Mesh skin flag mismatch."); var next = new Surface(data, primitive, format & (ArrayFormat.Use2DVertices | ArrayFormat.UseDynamicUpdate | ArrayFormat.Use8BoneWeights)) { Name = new(name) }; if (next.Format != format) throw new InvalidDataException("Mesh channel flags mismatch."); foreach (var weight in data.Weights) if (weight < 0 || weight > 1) throw new InvalidDataException("Packed weights must be normalized unsigned values."); surfaces.Add(next);
            }
            if (stream.Position != stream.Length) throw new InvalidDataException("Trailing mesh archive data.");
        }
        catch (Exception error) when (error is EndOfStreamException or ArgumentException or OverflowException) { throw new InvalidDataException("Malformed mesh archive.", error); }
        lock (Gate) { ThrowIfDisposed(); _surfaces.Clear(); _surfaces.AddRange(surfaces); }
        EmitChanged();
    }
    private static int Count(BinaryReader reader, int maximum) { var value = reader.ReadInt32(); if (value < 0 || value > maximum) throw new InvalidDataException("Invalid mesh archive count."); return value; }
    private Material?[] StoredMaterials
    {
        get { lock (Gate) { ThrowIfDisposed(); var result = new Material?[_surfaces.Count]; for (var i = 0; i < result.Length; i++) result[i] = _surfaces[i].Material; return result; } }
        set { ArgumentNullException.ThrowIfNull(value); lock (Gate) { ThrowIfDisposed(); if (value.Length != _surfaces.Count) throw new InvalidDataException("Mesh material count mismatch."); foreach (var material in value) if (material is { IsDisposed: true }) throw new ObjectDisposedException(nameof(material)); for (var i = 0; i < value.Length; i++) _surfaces[i].Material = value[i]; } EmitChanged(); }
    }
    private static readonly PropertyDescriptor[] SurfaceProperties =
    [
        new PropertyDescriptor<ArrayMesh, byte[]>("_surface_data", m => m.SaveSurfaces(), (m, v) => m.LoadSurfaces(v), _ => [], stored: true),
        new PropertyDescriptor<ArrayMesh, Material?[]>("_surface_materials", m => m.StoredMaterials, (m, v) => m.StoredMaterials = v, _ => [], stored: true)
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(SurfaceProperties);
}
