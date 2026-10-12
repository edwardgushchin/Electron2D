namespace Electron2D;

public sealed partial class ImporterMesh
{
    private const int ArchiveBudget = 64 * 1024 * 1024;
    private static long DataSize(MeshSurfaceData d) => 68L + d.Vertices.LongLength * 8 + d.Normals.LongLength * 12 + d.Tangents.LongLength * 16 + d.Colors.LongLength * 16 + d.UVs.LongLength * 8 + d.UV2s.LongLength * 8 + d.Indices.LongLength * 4 + d.Bones.LongLength * 4 + d.Weights.LongLength * 4 + Enumerable.Range(0, 4).Sum(c => d.Custom(c)?.Data.LongLength ?? 0);
    private byte[] SaveData()
    {
        lock (_gate)
        {
            ThrowIfDisposed(); if (_names.Count > 4096 || _surfaces.Count > 4096) throw new InvalidOperationException("Importer archive count budget exceeded.");
            long bytes = 12; foreach (var name in _names) { CheckName(name); bytes += 4 + name.Length * 2L; }
            foreach (var s in _surfaces) { if (s.LODs.Length > 4096) throw new InvalidOperationException("Importer level count budget exceeded."); CheckName(s.Name); bytes += 20 + s.Name.Length * 2L + DataSize(s.Data); foreach (var shape in s.Shapes) bytes += DataSize(shape); foreach (var lod in s.LODs) bytes += 8 + lod.Data.Indices.LongLength * 4; }
            if (bytes > ArchiveBudget) throw new InvalidOperationException("Importer archive byte budget exceeded.");
            using var stream = new MemoryStream((int)bytes); using var writer = new BinaryWriter(stream); writer.Write(0x31504d49u); writer.Write(_names.Count); writer.Write(_surfaces.Count); foreach (var name in _names) WriteName(writer, name);
            foreach (var s in _surfaces)
            {
                writer.Write((int)s.Primitive); writer.Write((ulong)s.Flags); WriteName(writer, s.Name); WriteData(writer, s.Data); foreach (var shape in s.Shapes) WriteData(writer, shape); writer.Write(s.LODs.Length); foreach (var lod in s.LODs) { writer.Write(lod.Error); writer.Write(lod.Data.Indices.Length); foreach (var index in lod.Data.Indices) writer.Write(index); }
            }
            return stream.ToArray();
        }
    }
    private static void CheckName(string name) { if (name.Length > 65536) throw new InvalidOperationException("Importer name budget exceeded."); }
    private static void WriteName(BinaryWriter w, string name) { w.Write(name.Length); foreach (var c in name) w.Write((ushort)c); }
    private static string ReadName(BinaryReader r)
    {
        var length = Count(r, 65536); if (length * 2L > r.BaseStream.Length - r.BaseStream.Position) throw new InvalidDataException("Truncated importer name."); var chars = new char[length]; for (var i = 0; i < length; i++) chars[i] = (char)r.ReadUInt16(); return new(chars);
    }
    private static void WriteData(BinaryWriter w, MeshSurfaceData d)
    {
        foreach (var length in new[] { d.Vertices.Length, d.Normals.Length, d.Tangents.Length, d.Colors.Length, d.UVs.Length, d.UV2s.Length, d.Indices.Length, d.Bones.Length, d.Weights.Length }) w.Write(length);
        for (var c = 0; c < 4; c++) { var channel = d.Custom(c); w.Write(channel is null ? -1 : (int)channel.Format); w.Write(channel?.Data.Length ?? 0); }
        foreach (var v in d.Vertices) { w.Write(v.X); w.Write(v.Y); }
        foreach (var v in d.Normals) { w.Write(v.X); w.Write(v.Y); w.Write(v.Z); }
        foreach (var v in d.Tangents) { w.Write(v.X); w.Write(v.Y); w.Write(v.Z); w.Write(v.W); }
        foreach (var v in d.Colors) { w.Write(v.R); w.Write(v.G); w.Write(v.B); w.Write(v.A); }
        foreach (var v in d.UVs) { w.Write(v.X); w.Write(v.Y); }
        foreach (var v in d.UV2s) { w.Write(v.X); w.Write(v.Y); }
        foreach (var v in d.Indices) w.Write(v); foreach (var v in d.Bones) w.Write(v); foreach (var v in d.Weights) w.Write(v); for (var c = 0; c < 4; c++) if (d.Custom(c) is { } channel) w.Write(channel.Data);
    }
    private static MeshSurfaceData ReadData(BinaryReader r)
    {
        var n = Count(r, ArchiveBudget / 8); var normals = Count(r, n); var tangents = Count(r, n); var colors = Count(r, n); var uv = Count(r, n); var uv2 = Count(r, n); var indices = Count(r, ArchiveBudget / 4); var bones = Count(r, ArchiveBudget / 4); var weights = Count(r, bones);
        var formats = new int[4]; var sizes = new int[4];
        for (var c = 0; c < 4; c++) { formats[c] = r.ReadInt32(); sizes[c] = Count(r, ArchiveBudget); if (formats[c] < -1 || formats[c] >= 8 || formats[c] == -1 && sizes[c] != 0 || formats[c] >= 0 && sizes[c] != n * (long)MeshCustomCodec.Stride((MeshCustomFormat)formats[c])) throw new InvalidDataException("Invalid importer custom schema."); }
        var needed = n * 8L + normals * 12L + tangents * 16L + colors * 16L + uv * 8L + uv2 * 8L + indices * 4L + bones * 4L + weights * 4L + sizes.Sum(x => (long)x); if (needed > r.BaseStream.Length - r.BaseStream.Position) throw new InvalidDataException("Truncated importer arrays.");
        var d = new MeshSurfaceData { Vertices = new Vector2[n], Normals = new Vector3[normals], Tangents = new Vector4[tangents], Colors = new Color[colors], UVs = new Vector2[uv], UV2s = new Vector2[uv2], Indices = new int[indices], Bones = new int[bones], Weights = new float[weights] };
        for (var i = 0; i < n; i++) d.Vertices[i] = new(r.ReadSingle(), r.ReadSingle()); for (var i = 0; i < normals; i++) d.Normals[i] = new(r.ReadSingle(), r.ReadSingle(), r.ReadSingle()); for (var i = 0; i < tangents; i++) d.Tangents[i] = new(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle()); for (var i = 0; i < colors; i++) d.Colors[i] = new(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle()); for (var i = 0; i < uv; i++) d.UVs[i] = new(r.ReadSingle(), r.ReadSingle()); for (var i = 0; i < uv2; i++) d.UV2s[i] = new(r.ReadSingle(), r.ReadSingle()); for (var i = 0; i < indices; i++) d.Indices[i] = r.ReadInt32(); for (var i = 0; i < bones; i++) d.Bones[i] = r.ReadInt32(); for (var i = 0; i < weights; i++) d.Weights[i] = r.ReadSingle(); for (var c = 0; c < 4; c++) if (formats[c] >= 0) d.SetCustom(c, new() { Format = (MeshCustomFormat)formats[c], Data = r.ReadBytes(sizes[c]) });
        return d;
    }
    private static int Count(BinaryReader r, int maximum) { var value = r.ReadInt32(); if (value < 0 || value > maximum) throw new InvalidDataException("Importer count budget exceeded."); return value; }
    private void LoadData(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes); if (bytes.Length > ArchiveBudget) throw new InvalidDataException("Importer archive byte budget exceeded.");
        using var prepared = new ImporterMesh();
        try
        {
            using var stream = new MemoryStream(bytes, false); using var r = new BinaryReader(stream); if (r.ReadUInt32() != 0x31504d49u) throw new InvalidDataException("Invalid importer archive version."); var names = Count(r, 4096); var surfaces = Count(r, 4096); for (var i = 0; i < names; i++) prepared.AddBlendShape(ReadName(r));
            for (var i = 0; i < surfaces; i++)
            {
                var primitive = (Mesh.PrimitiveType)r.ReadInt32(); var flags = (Mesh.ArrayFormat)r.ReadUInt64(); var name = ReadName(r); var data = ReadData(r); var shapes = new MeshSurfaceData[names]; for (var j = 0; j < names; j++) shapes[j] = ReadData(r); var count = Count(r, 4096); var levels = new Dictionary<float, int[]>(count); var previous = 0f;
                for (var j = 0; j < count; j++) { var error = r.ReadSingle(); var length = Count(r, ArchiveBudget / 4); if (!float.IsFinite(error) || error <= previous || length * 4L > stream.Length - stream.Position) throw new InvalidDataException("Invalid importer level schema."); var order = new int[length]; for (var k = 0; k < length; k++) order[k] = r.ReadInt32(); levels.Add(error, order); previous = error; }
                prepared.AddSurface(primitive, data, shapes, levels, name: name, flags: flags);
            }
            if (stream.Position != stream.Length) throw new InvalidDataException("Trailing importer archive data.");
        }
        catch (Exception error) when (error is ArgumentException or EndOfStreamException or OverflowException or NotSupportedException) { throw new InvalidDataException("Malformed importer archive.", error); }
        lock (_gate) { Mutable(); _surfaces.EnsureCapacity(prepared._surfaces.Count); _names.EnsureCapacity(prepared._names.Count); _surfaces.Clear(); _surfaces.AddRange(prepared._surfaces); _names.Clear(); _names.AddRange(prepared._names); Invalidate(); }
        EmitChanged();
    }
    private Material?[] Materials
    {
        get { lock (_gate) { ThrowIfDisposed(); return _surfaces.Select(s => s.Material).ToArray(); } }
        set { ArgumentNullException.ThrowIfNull(value); lock (_gate) { Mutable(); if (value.Length != _surfaces.Count) throw new InvalidDataException("Importer material count mismatch."); foreach (var m in value) if (m is { IsDisposed: true }) throw new ObjectDisposedException(nameof(Material)); for (var i = 0; i < value.Length; i++) _surfaces[i].Material = value[i]; Invalidate(); } EmitChanged(); }
    }
    private static readonly PropertyDescriptor[] Properties =
    [
        new PropertyDescriptor<ImporterMesh, BlendShapeMode>(nameof(BlendShapeMode), r => r.BlendShapeMode, (r, v) => r.BlendShapeMode = v, _ => BlendShapeMode.Normalized, stored: true),
        new PropertyDescriptor<ImporterMesh, byte[]>("_data", r => r.SaveData(), (r, v) => r.LoadData(v), _ => [], stored: true),
        new PropertyDescriptor<ImporterMesh, Material?[]>("_materials", r => r.Materials, (r, v) => r.Materials = v, _ => [], stored: true)
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
}
