namespace Electron2D;

public sealed partial class ArrayMesh
{
    private const int ArchiveBudget = 64 * 1024 * 1024;
    internal byte[] SaveSurfaces()
    {
        lock (Gate)
        {
            ThrowIfDisposed(); long bytes = 12 + _blendShapeNames.Sum(name => 4L + name.Length * 2L); if (_surfaces.Count > 4096) throw new InvalidOperationException("Mesh archive surface budget exceeded.");
            foreach (var s in _surfaces) { var d = s.Data; if (s.Name.Length > 65536) throw new InvalidOperationException("Mesh archive name budget exceeded."); bytes += 80L + s.Name.Length * 2L + d.Vertices.Length * 8L + d.Colors.Length * 16L + d.UVs.Length * 8L + d.UV2s.Length * 8L + d.Indices.Length * 4L + d.Bones.Length * 4L + d.Weights.Length * 4L + s.FrameWords.LongLength * 4L; for (var channel = 0; channel < 4; channel++) bytes += d.Custom(channel)?.Data.LongLength ?? 0; bytes += 12; foreach (var lod in s.LODs) bytes += 8L + lod.Data.Indices.LongLength * 4; foreach (var shape in s.BlendShapes) bytes += shape.Vertices.LongLength * 8 + shape.Normals.LongLength * 12 + shape.Tangents.LongLength * 16; }
            if (bytes > ArchiveBudget) throw new InvalidOperationException("Mesh archive byte budget exceeded.");
            using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream); writer.Write(0x36534d41u); writer.Write(_surfaces.Count); writer.Write(_blendShapeNames.Count); foreach (var name in _blendShapeNames) { writer.Write(name.Length); foreach (var c in name) writer.Write((ushort)c); }
            foreach (var s in _surfaces)
            {
                var d = s.Data; writer.Write((int)s.Primitive); writer.Write((ulong)s.Format); writer.Write(s.Name.Length); foreach (var c in s.Name) writer.Write((ushort)c); writer.Write(d.Vertices.Length); writer.Write(d.Colors.Length); writer.Write(d.UVs.Length); writer.Write(d.UV2s.Length); writer.Write(d.Indices.Length); writer.Write(d.Bones.Length); writer.Write(d.Normals.Length); writer.Write(d.Tangents.Length); for (var channel = 0; channel < 4; channel++) { var custom = d.Custom(channel); writer.Write(custom is null ? -1 : (int)custom.Format); writer.Write(custom?.Data.Length ?? 0); }
                writer.Write((ulong)s.BlendMask); writer.Write(s.LODs.Length);
                foreach (var v in d.Vertices) { writer.Write(v.X); writer.Write(v.Y); }
                foreach (var c in d.Colors) { writer.Write(c.R); writer.Write(c.G); writer.Write(c.B); writer.Write(c.A); }
                foreach (var v in d.UVs) { writer.Write(v.X); writer.Write(v.Y); }
                foreach (var v in d.UV2s) { writer.Write(v.X); writer.Write(v.Y); }
                foreach (var i in d.Indices) writer.Write(i); foreach (var i in d.Bones) writer.Write(i); foreach (var w in d.Weights) writer.Write(w); foreach (var word in s.FrameWords) writer.Write(word); for (var channel = 0; channel < 4; channel++) if (d.Custom(channel) is { } custom) writer.Write(custom.Data);
                foreach (var shape in s.BlendShapes) { foreach (var v in shape.Vertices) { writer.Write(v.X); writer.Write(v.Y); } foreach (var v in shape.Normals) { writer.Write(v.X); writer.Write(v.Y); writer.Write(v.Z); } foreach (var v in shape.Tangents) { writer.Write(v.X); writer.Write(v.Y); writer.Write(v.Z); writer.Write(v.W); } }
                foreach (var lod in s.LODs) { writer.Write(lod.Error); writer.Write(lod.Data.Indices.Length); foreach (var index in lod.Data.Indices) writer.Write(index); }
            }
            return stream.ToArray();
        }
    }
    internal void LoadSurfaces(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes); if (bytes.Length > ArchiveBudget) throw new InvalidDataException("Mesh archive byte budget exceeded.");
        var surfaces = new List<Surface>(); var names = new List<string>();
        try
        {
            using var stream = new MemoryStream(bytes, writable: false); using var reader = new BinaryReader(stream); var version = reader.ReadUInt32(); if (version is not (0x31534d41u or 0x32534d41u or 0x33534d41u or 0x34534d41u or 0x35534d41u or 0x36534d41u)) throw new InvalidDataException("Invalid mesh archive version."); var count = Count(reader, 4096);
            if (version >= 0x35534d41u) { var shapeCount = Count(reader, 4096); for (var shape = 0; shape < shapeCount; shape++) { var length = Count(reader, 65536); if (stream.Length - stream.Position < length * 2L) throw new InvalidDataException("Truncated blend shape name."); var chars = new char[length]; for (var c = 0; c < length; c++) chars[c] = (char)reader.ReadUInt16(); var name = new string(chars); if (names.Contains(name, StringComparer.Ordinal)) throw new InvalidDataException("Duplicate blend shape names."); names.Add(name); } }
            for (var surface = 0; surface < count; surface++)
            {
                var primitive = (PrimitiveType)reader.ReadInt32(); var format = (ArrayFormat)reader.ReadUInt64(); const ArrayFormat allowed = ArrayFormat.Custom0 | ArrayFormat.Custom1 | ArrayFormat.Custom2 | ArrayFormat.Custom3 | MeshCustomCodec.FormatMask | ArrayFormat.Vertex | ArrayFormat.Normal | ArrayFormat.Tangent | ArrayFormat.Use2DVertices | ArrayFormat.UseDynamicUpdate | ArrayFormat.Color | ArrayFormat.TexUV | ArrayFormat.TexUV2 | ArrayFormat.Index | ArrayFormat.Bones | ArrayFormat.Weights | ArrayFormat.Use8BoneWeights; if ((format & ~allowed) != 0) throw new InvalidDataException("Unsupported mesh archive channels.");
                var nameLength = Count(reader, 65536); if (stream.Length - stream.Position < nameLength * 2L) throw new InvalidDataException("Truncated mesh name."); var name = new char[nameLength]; for (var i = 0; i < name.Length; i++) name[i] = (char)reader.ReadUInt16();
                var n = Count(reader, ArchiveBudget / 8); var colors = Count(reader, n); var uv = Count(reader, n); var uv2 = version >= 0x32534d41u ? Count(reader, n) : 0; var indices = Count(reader, ArchiveBudget / 4); var bones = Count(reader, ArchiveBudget / 8); var normals = version >= 0x33534d41u ? Count(reader, n) : 0; var tangents = version >= 0x33534d41u ? Count(reader, n) : 0;
                var customFormats = new int[4]; Array.Fill(customFormats, -1); var customLengths = new int[4];
                if (version >= 0x34534d41u)
                    for (var channel = 0; channel < 4; channel++)
                    {
                        customFormats[channel] = reader.ReadInt32(); customLengths[channel] = Count(reader, ArchiveBudget);
                        if (customFormats[channel] < -1 || customFormats[channel] >= 8 || customFormats[channel] == -1 && customLengths[channel] != 0 || customFormats[channel] >= 0 && customLengths[channel] != n * (long)MeshCustomCodec.Stride((MeshCustomFormat)customFormats[channel])) throw new InvalidDataException("Invalid custom channel schema.");
                    }
                var blendMask = version >= 0x35534d41u ? (ArrayFormat)reader.ReadUInt64() : format & ArrayFormat.BlendShapeMask;
                if ((blendMask & ~ArrayFormat.BlendShapeMask) != 0 || (blendMask & ArrayFormat.Vertex) == 0 || (blendMask & ~format) != 0 || (blendMask & ArrayFormat.Normal) != (format & ArrayFormat.Normal) || (format & ArrayFormat.Tangent) != 0 && (format & ArrayFormat.Normal) == 0 && (blendMask & ArrayFormat.Tangent) == 0) throw new InvalidDataException("Invalid blend shape channel mask.");
                var lodCount = version >= 0x36534d41u ? Count(reader, 4096) : 0;
                var blendBytes = names.Count * (long)n * (8 + ((blendMask & ArrayFormat.Normal) != 0 ? 12 : 0) + ((blendMask & ArrayFormat.Tangent) != 0 ? 16 : 0));
                var needed = lodCount * 8L + blendBytes + n * 8L + colors * 16L + uv * 8L + uv2 * 8L + indices * 4L + bones * 8L + normals * 4L + tangents * 4L + customLengths.Sum(x => (long)x); if (needed > stream.Length - stream.Position) throw new InvalidDataException("Truncated mesh channels.");
                var data = new MeshSurfaceData { Vertices = new Vector2[n], Colors = new Color[colors], UVs = new Vector2[uv], UV2s = new Vector2[uv2], Indices = new int[indices], Bones = new int[bones], Weights = new float[bones], Normals = new Vector3[normals], Tangents = new Vector4[tangents] };
                for (var i = 0; i < n; i++) data.Vertices[i] = new(reader.ReadSingle(), reader.ReadSingle()); for (var i = 0; i < colors; i++) data.Colors[i] = new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()); for (var i = 0; i < uv; i++) data.UVs[i] = new(reader.ReadSingle(), reader.ReadSingle()); for (var i = 0; i < uv2; i++) data.UV2s[i] = new(reader.ReadSingle(), reader.ReadSingle()); for (var i = 0; i < indices; i++) data.Indices[i] = reader.ReadInt32(); for (var i = 0; i < bones; i++) data.Bones[i] = reader.ReadInt32(); for (var i = 0; i < bones; i++) data.Weights[i] = reader.ReadSingle();
                data.Validate(primitive);
                var words = new uint[checked(normals + tangents)]; for (var i = 0; i < words.Length; i++) { words[i] = reader.ReadUInt32(); MeshFrameCodec.RestoreWord(data, i, words[i]); }
                for (var channel = 0; channel < 4; channel++) if (customFormats[channel] >= 0) data.SetCustom(channel, new() { Format = (MeshCustomFormat)customFormats[channel], Data = reader.ReadBytes(customLengths[channel]) });
                data.Validate(primitive);
                var shapes = new MeshSurfaceData[names.Count]; for (var shape = 0; shape < shapes.Length; shape++) { var pose = new MeshSurfaceData { Vertices = new Vector2[n], Normals = (blendMask & ArrayFormat.Normal) != 0 ? new Vector3[n] : [], Tangents = (blendMask & ArrayFormat.Tangent) != 0 ? new Vector4[n] : [] }; for (var i = 0; i < n; i++) pose.Vertices[i] = new(reader.ReadSingle(), reader.ReadSingle()); for (var i = 0; i < pose.Normals.Length; i++) pose.Normals[i] = new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()); for (var i = 0; i < pose.Tangents.Length; i++) pose.Tangents[i] = new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()); ValidateBlendShape(pose, data, blendMask); shapes[shape] = pose; }
                var lods = new Dictionary<float, int[]>(lodCount);
                float previousError = 0;
                for (var lod = 0; lod < lodCount; lod++)
                {
                    var error = reader.ReadSingle(); var length = Count(reader, ArchiveBudget / 4);
                    if (!float.IsFinite(error) || error <= previousError || length == 0 || length >= indices || length * 4L > stream.Length - stream.Position) throw new InvalidDataException("Invalid LOD archive schema.");
                    var order = new int[length]; for (var i = 0; i < length; i++) order[i] = reader.ReadInt32(); lods.Add(error, order); previousError = error;
                }
                if (data.SkinSlots != 0 && data.SkinSlots != ((format & ArrayFormat.Use8BoneWeights) != 0 ? 8 : 4)) throw new InvalidDataException("Mesh skin flag mismatch."); var next = new Surface(data, primitive, format & (ArrayFormat.Use2DVertices | ArrayFormat.UseDynamicUpdate | ArrayFormat.Use8BoneWeights), words, shapes, blendMask, lods) { Name = new(name) }; if (next.Format != format) throw new InvalidDataException("Mesh channel flags mismatch."); foreach (var weight in data.Weights) if (weight < 0 || weight > 1) throw new InvalidDataException("Packed weights must be normalized unsigned values."); surfaces.Add(next);
            }
            if (stream.Position != stream.Length) throw new InvalidDataException("Trailing mesh archive data.");
        }
        catch (Exception error) when (error is EndOfStreamException or ArgumentException or OverflowException) { throw new InvalidDataException("Malformed mesh archive.", error); }
        lock (Gate) { ThrowIfDisposed(); _surfaces.Clear(); _surfaces.AddRange(surfaces); _blendShapeNames.Clear(); _blendShapeNames.AddRange(names); }
        EmitChanged();
    }
    private static int Count(BinaryReader reader, int maximum) { var value = reader.ReadInt32(); if (value < 0 || value > maximum) throw new InvalidDataException("Invalid mesh archive count."); return value; }
    internal Material?[] StoredMaterials
    {
        get { lock (Gate) { ThrowIfDisposed(); var result = new Material?[_surfaces.Count]; for (var i = 0; i < result.Length; i++) result[i] = _surfaces[i].Material; return result; } }
        set { ArgumentNullException.ThrowIfNull(value); lock (Gate) { ThrowIfDisposed(); if (value.Length != _surfaces.Count) throw new InvalidDataException("Mesh material count mismatch."); foreach (var material in value) if (material is { IsDisposed: true }) throw new ObjectDisposedException(nameof(material)); for (var i = 0; i < value.Length; i++) _surfaces[i].Material = value[i]; } EmitChanged(); }
    }
    private static readonly PropertyDescriptor[] SurfaceProperties =
    [
        new PropertyDescriptor<ArrayMesh, BlendShapeMode>(nameof(BlendShapeMode), m => m.BlendShapeMode, (m, v) => m.BlendShapeMode = v, _ => BlendShapeMode.Relative, stored: true),
        new PropertyDescriptor<ArrayMesh, byte[]>("_surface_data", m => m.SaveSurfaces(), (m, v) => m.LoadSurfaces(v), _ => [], stored: true),
        new PropertyDescriptor<ArrayMesh, Material?[]>("_surface_materials", m => m.StoredMaterials, (m, v) => m.StoredMaterials = v, _ => [], stored: true)
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(SurfaceProperties);
}
