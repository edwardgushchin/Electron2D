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
        Arrays<string>();
        Arrays<float>();
        Arrays<int>();
        Arrays<byte>();
        Arrays<Vector2>();
        Arrays<Color>();
        Arrays<int[]>();
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
        s.Put32(v is null ? -1 : v.Length); if (v is not null) foreach (var item in v) ResourceFileTypes.Codec<T>().Write(s, item);
    }
, s =>
{
    var count = s.Get32(); if (count == -1) return null!; if (count is < 0 or > 1048576) throw new InvalidDataException("Array exceeds file budget."); var value = new T[count]; for (var i = 0; i < count; i++) value[i] = ResourceFileTypes.Codec<T>().Read(s); return value;
}
);
}
