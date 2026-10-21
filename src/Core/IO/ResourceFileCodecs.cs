namespace Electron2D;

internal static class ResourceFileCodecs
{
    internal static void Prepare()
    {
        Add<bool>((s, v) => s.PutU8(v ? (byte)1 : (byte)0), s => s.GetU8() switch
        {
            0 => false,
            1 => true,
            _ => throw new InvalidDataException("Invalid boolean.")
        }
);
        Add<byte>((s, v) => s.PutU8(v), s => s.GetU8());
        Add<sbyte>((s, v) => s.Put8(v), s => s.Get8());
        Add<short>((s, v) => s.Put16(v), s => s.Get16());
        Add<ushort>((s, v) => s.PutU16(v), s => s.GetU16());
        Add<int>((s, v) => s.Put32(v), s => s.Get32());
        Add<uint>((s, v) => s.PutU32(v), s => s.GetU32());
        Add<long>((s, v) => s.Put64(v), s => s.Get64());
        Add<ulong>((s, v) => s.PutU64(v), s => s.GetU64());
        Add<float>((s, v) => s.PutFloat(v), s => s.GetFloat());
        Add<double>((s, v) => s.PutDouble(v), s => s.GetDouble());
        Add<string>((s, v) =>
        {
            s.PutU8(v is null ? (byte)0 : (byte)1); if (v is not null) ResourceArchiveStrings.Write(s, v);
        }
, s => s.GetU8() switch
{
    0 => null!,
    1 => ResourceArchiveStrings.Read(s),
    _ => throw new InvalidDataException("Invalid string marker.")
}
);
        Add<AnimationBezierKey>((s, v) => { s.PutDouble(v.Value); s.PutFloat(v.InHandle.X); s.PutFloat(v.InHandle.Y); s.PutFloat(v.OutHandle.X); s.PutFloat(v.OutHandle.Y); }, s => new(s.GetDouble(), new(s.GetFloat(), s.GetFloat()), new(s.GetFloat(), s.GetFloat())));
        Add<Vector2>((s, v) =>
        {
            s.PutFloat(v.X); s.PutFloat(v.Y);
        }
, s => new(s.GetFloat(), s.GetFloat()));
        Add<Vector2i>((s, v) =>
        {
            s.Put32(v.X); s.Put32(v.Y);
        }
, s => new(s.Get32(), s.Get32()));
        Add<Vector3>((s, v) =>
        {
            s.PutFloat(v.X); s.PutFloat(v.Y); s.PutFloat(v.Z);
        }
, s => new(s.GetFloat(), s.GetFloat(), s.GetFloat()));
        Add<Vector3i>((s, v) =>
        {
            s.Put32(v.X); s.Put32(v.Y); s.Put32(v.Z);
        }
, s => new(s.Get32(), s.Get32(), s.Get32()));
        Add<Vector4>((s, v) =>
        {
            s.PutFloat(v.X); s.PutFloat(v.Y); s.PutFloat(v.Z); s.PutFloat(v.W);
        }
, s => new(s.GetFloat(), s.GetFloat(), s.GetFloat(), s.GetFloat()));
        Add<Vector4i>((s, v) =>
        {
            s.Put32(v.X); s.Put32(v.Y); s.Put32(v.Z); s.Put32(v.W);
        }
, s => new(s.Get32(), s.Get32(), s.Get32(), s.Get32()));
        Add<Color>((s, v) =>
        {
            s.PutFloat(v.R); s.PutFloat(v.G); s.PutFloat(v.B); s.PutFloat(v.A);
        }
, s => new(s.GetFloat(), s.GetFloat(), s.GetFloat(), s.GetFloat()));
        Add<Rect2>((s, v) =>
        {
            ResourceFileTypes.Codec<Vector2>().Write(s, v.Position); ResourceFileTypes.Codec<Vector2>().Write(s, v.Size);
        }
, s => new(ResourceFileTypes.Codec<Vector2>().Read(s), ResourceFileTypes.Codec<Vector2>().Read(s)));
        Add<Rect2i>((s, v) =>
        {
            ResourceFileTypes.Codec<Vector2i>().Write(s, v.Position); ResourceFileTypes.Codec<Vector2i>().Write(s, v.Size);
        }
, s => new(ResourceFileTypes.Codec<Vector2i>().Read(s), ResourceFileTypes.Codec<Vector2i>().Read(s)));
        Add<Transform>((s, v) =>
        {
            ResourceFileTypes.Codec<Vector2>().Write(s, v.X); ResourceFileTypes.Codec<Vector2>().Write(s, v.Y); ResourceFileTypes.Codec<Vector2>().Write(s, v.Origin);
        }
, s => new(ResourceFileTypes.Codec<Vector2>().Read(s), ResourceFileTypes.Codec<Vector2>().Read(s), ResourceFileTypes.Codec<Vector2>().Read(s)));
        Add<System.Numerics.Matrix4x4>((s, v) =>
        {
            for (var row = 0; row < 4; row++) for (var column = 0; column < 4; column++) s.PutFloat(v[row, column]);
        }, s =>
        {
            var value = new System.Numerics.Matrix4x4();
            for (var row = 0; row < 4; row++) for (var column = 0; column < 4; column++) value[row, column] = s.GetFloat();
            return value;
        });
        Add<Dictionary<string, int>>((s, v) =>
        {
            s.Put32(v.Count); foreach (var pair in v.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                ResourceArchiveStrings.Write(s, pair.Key); s.Put32(pair.Value);
            }
        }
, s =>
{
    var count = s.Get32(); if (count is < 0 or > 65536) throw new InvalidDataException("Dictionary exceeds file budget."); var result = new Dictionary<string, int>(StringComparer.Ordinal); for (var i = 0; i < count; i++) if (!result.TryAdd(ResourceArchiveStrings.Read(s), s.Get32())) throw new InvalidDataException("Duplicate dictionary key."); return result;
}
);
        Add<Dictionary<string, string>>((stream, map) => { if (map.Count > 65536) throw new InvalidDataException("Dictionary exceeds file budget."); stream.Put32(map.Count); foreach (var pair in map.OrderBy(p => p.Key, StringComparer.Ordinal)) { ResourceArchiveStrings.Write(stream, pair.Key); ResourceArchiveStrings.Write(stream, pair.Value); } }, stream => { var count = stream.Get32(); if (count < 0 || count > 65536) throw new InvalidDataException("Invalid dictionary count."); var map = new Dictionary<string, string>(StringComparer.Ordinal); for (var i = 0; i < count; i++) if (!map.TryAdd(ResourceArchiveStrings.Read(stream), ResourceArchiveStrings.Read(stream))) throw new InvalidDataException("Duplicate dictionary key."); return map; });
        Add<Dictionary<Color, Color>>((stream, map) =>
        {
            if (map.Count > 65536) throw new InvalidDataException("Paint map exceeds file budget.");
            stream.Put32(map.Count);
            foreach (var pair in map.OrderBy(p => p.Key.R).ThenBy(p => p.Key.G).ThenBy(p => p.Key.B).ThenBy(p => p.Key.A))
            { ResourceFileTypes.Codec<Color>().Write(stream, pair.Key); ResourceFileTypes.Codec<Color>().Write(stream, pair.Value); }
        }, stream =>
        {
            var count = stream.Get32(); if (count < 0 || count > 65536) throw new InvalidDataException("Invalid paint map count.");
            var result = new Dictionary<Color, Color>();
            for (var i = 0; i < count; i++)
            {
                var key = ResourceFileTypes.Codec<Color>().Read(stream); var value = ResourceFileTypes.Codec<Color>().Read(stream);
                if (!key.IsFinite() || !value.IsFinite() || !result.TryAdd(key, value)) throw new InvalidDataException("Invalid or duplicate paint map color.");
            }
            return result;
        });
        Add<Dictionary<string, Color>>((s, v) =>
        {
            if (v.Count > 65536) throw new InvalidDataException("Dictionary exceeds file budget.");
            s.Put32(v.Count); foreach (var pair in v.OrderBy(p => p.Key, StringComparer.Ordinal)) { ResourceArchiveStrings.Write(s, pair.Key); ResourceFileTypes.Codec<Color>().Write(s, pair.Value); }
        }, s =>
        {
            var count = s.Get32(); if (count is < 0 or > 65536) throw new InvalidDataException("Dictionary exceeds file budget.");
            var result = new Dictionary<string, Color>(StringComparer.Ordinal); for (var i = 0; i < count; i++) if (!result.TryAdd(ResourceArchiveStrings.Read(s), ResourceFileTypes.Codec<Color>().Read(s))) throw new InvalidDataException("Duplicate dictionary key."); return result;
        });
        Nullable<bool>();
        Nullable<int>();
        Nullable<float>();
        Nullable<double>();
        Nullable<Color>();
        Nullable<Vector2>();
        Nullable<uint>();
        Nullable<Vector3>();
        Nullable<Vector4>();
        Nullable<Rect2>();
        Nullable<Vector2i>();
        Nullable<Vector3i>();
        Nullable<Vector4i>();
        Nullable<Rect2i>();
        Nullable<Transform>();
        Nullable<System.Numerics.Matrix4x4>();
        Add<GraphConnection>((s, v) => { ResourceArchiveStrings.Write(s, v.FromNode); s.Put32(v.FromPort); ResourceArchiveStrings.Write(s, v.ToNode); s.Put32(v.ToPort); s.PutU8(v.KeepAlive ? (byte)1 : (byte)0); }, s => { var from = ResourceArchiveStrings.Read(s); var fp = s.Get32(); var to = ResourceArchiveStrings.Read(s); var tp = s.Get32(); var alive = s.GetU8(); if (alive > 1 || fp < 0 || tp < 0 || from.Length == 0 || to.Length == 0) throw new InvalidDataException("Invalid graph connection."); return new(from, fp, to, tp, alive == 1); });
        Arrays<GraphConnection>();
        Add<Dictionary<int, string>>((s, v) => { s.Put32(v.Count); foreach (var pair in v.OrderBy(p => p.Key)) { s.Put32(pair.Key); ResourceArchiveStrings.Write(s, pair.Value); } }, s => { var count = s.Get32(); if (count < 0 || count > 1048576) throw new InvalidDataException("Graph type-name count exceeds file budget."); var result = new Dictionary<int, string>(); for (var i = 0; i < count; i++) if (!result.TryAdd(s.Get32(), ResourceArchiveStrings.Read(s))) throw new InvalidDataException("Duplicate graph type identifier."); return result; });
        Add<Dictionary<uint, float>>((s, v) => { s.Put32(v.Count); foreach (var pair in v.OrderBy(p => p.Key)) { s.PutU32(pair.Key); s.PutFloat(pair.Value); } }, s => { var count = s.Get32(); if (count < 0 || count > 65536) throw new InvalidDataException("Variation coordinate count exceeds file budget."); var result = new Dictionary<uint, float>(); for (var i = 0; i < count; i++) { var tag = s.GetU32(); var value = s.GetFloat(); if (!float.IsFinite(value) || !result.TryAdd(tag, value)) throw new InvalidDataException("Invalid variation coordinate."); } return result; });
        Arrays<string>();
        Arrays<float>();
        Arrays<double>();
        Arrays<int>();
        Arrays<byte>();
        Arrays<Vector2>();
        Arrays<Vector2i>();
        Arrays<Color>();
        Arrays<int[]>();
        Arrays<bool>();
        Arrays<uint>();
        Arrays<Vector3>();
        Arrays<Vector4>();
        Arrays<Rect2>();
        Arrays<Vector3i>();
        Arrays<Vector4i>();
        Arrays<Transform>();
        Arrays<System.Numerics.Matrix4x4>();
    }
    private static void Add<T>(Action<StreamPeerBuffer, T> write, Func<StreamPeerBuffer, T> read) => ResourceFileTypes.RegisterValueCodec(write, read);
    private static void Nullable<T>() where T : struct => Add<T?>((s, v) =>
    {
        s.PutU8(v.HasValue ? (byte)1 : (byte)0); if (v.HasValue) ResourceFileTypes.Codec<T>().Write(s, v.Value);
    }
, s => s.GetU8() switch
{
    0 => null,
    1 => ResourceFileTypes.Codec<T>().Read(s),
    _ => throw new InvalidDataException("Invalid nullable marker.")
}
);
    private static void Arrays<T>() => Add<T[]>((s, v) =>
    {
        if (v is { Length: > 0 } && v.Length > (typeof(T) == typeof(byte) ? 16777216 : 1048576)) throw new InvalidDataException("Array exceeds file budget."); s.Put32(v is null ? -1 : v.Length); if (v is not null) foreach (var item in v) ResourceFileTypes.Codec<T>().Write(s, item);
    }
, s =>
{
    var count = s.Get32(); if (count == -1) return null!; if (count < 0 || count > (typeof(T) == typeof(byte) ? 16777216 : 1048576)) throw new InvalidDataException("Array exceeds file budget."); if (typeof(T) == typeof(byte) && count > s.GetAvailableBytes()) throw new InvalidDataException("Truncated byte array."); var value = new T[count]; for (var i = 0; i < count; i++) value[i] = ResourceFileTypes.Codec<T>().Read(s); return value;
}
);
}
