namespace Electron2D;

internal static class MeshFrameCodec
{
    internal static Vector3 FaceNormal(Vector2 a, Vector2 b, Vector2 c, bool flip = false)
    {
        var cross = ((double)b.X - a.X) * ((double)c.Y - a.Y) - ((double)b.Y - a.Y) * ((double)c.X - a.X);
        return new(0, 0, (flip ? 1 : -1) * Math.Sign(cross));
    }
    internal static void SynthesizeTangents(MeshSurfaceData data)
    {
        var normal = data.Normals.Length != 0;
        if (normal && data.Tangents.Length == 0)
        {
            data.Tangents = new Vector4[data.VertexCount];
            for (var i = 0; i < data.Tangents.Length; i++)
            {
                var n = Unit(data.Normals[i]);
                var t = Unit(new Vector3(n.Z, -n.X, n.Y).Cross(n));
                data.Tangents[i] = new(t.X, t.Y, t.Z, 1);
            }
        }
    }
    internal static uint[] Capture(MeshSurfaceData data)
    {
        SynthesizeTangents(data); var normal = data.Normals.Length != 0;
        var tangent = data.Tangents.Length != 0;
        var stride = (normal ? 1 : 0) + (tangent ? 1 : 0);
        if (stride == 0) return [];
        var words = new uint[checked(data.VertexCount * stride)];
        for (var i = 0; i < data.VertexCount; i++)
        {
            var offset = i * stride;
            if (normal) { var word = PackNormal(data.Normals[i]); words[offset++] = word; data.Normals[i] = Normal(word); }
            if (tangent) { var word = PackTangent(data.Tangents[i]); words[offset] = word; data.Tangents[i] = Tangent(word); }
        }
        return words;
    }
    internal static void RestoreWord(MeshSurfaceData data, int index, uint word)
    {
        var normal = data.Normals.Length != 0;
        var stride = (normal ? 1 : 0) + (data.Tangents.Length != 0 ? 1 : 0);
        var vertex = index / stride;
        if (normal && index % stride == 0) data.Normals[vertex] = Normal(word);
        else data.Tangents[vertex] = Tangent(word);
    }
    internal static uint PackNormal(Vector3 value) => Pack(Encode(value));
    internal static uint PackTangent(Vector4 value)
    {
        var oct = Encode(new(value.X, value.Y, value.Z));
        var y = Math.Max(oct.Y, 1f / 32767f) * .5f + .5f;
        var word = Pack(new(oct.X, value.W >= 0 ? y : 1 - y));
        return word == 0xffff0000u ? 0xffffffffu : word;
    }
    internal static Vector3 Normal(uint word) => Vector3.OctahedronDecode(Unpack(word));
    internal static Vector4 Tangent(uint word)
    {
        var oct = Unpack(word); var y = oct.Y * 2 - 1;
        var direction = Vector3.OctahedronDecode(new(oct.X, Math.Abs(y)));
        return new(direction.X, direction.Y, direction.Z, y >= 0 ? 1 : -1);
    }
    private static uint Pack(Vector2 oct) => (uint)(ushort)Math.Clamp(oct.X * 65535f, 0, 65535) | (uint)(ushort)Math.Clamp(oct.Y * 65535f, 0, 65535) << 16;
    private static Vector2 Unpack(uint word) => new((word & 65535u) / 65535f, (word >> 16) / 65535f);
    private static Vector2 Encode(Vector3 value)
    {
        // Zero has no direction; keep a deterministic positive Z code instead of undefined NaN conversion.
        if (value == Vector3.Zero) return new(.5f, .5f);
        var sum = Math.Abs(value.X) + Math.Abs(value.Y) + Math.Abs(value.Z);
        if (!float.IsFinite(sum) || sum == 0) value = Unit(value);
        return value.OctahedronEncode();
    }
    internal static Vector3 Unit(Vector3 value)
    {
        return Unit(value.X, value.Y, value.Z);
    }
    internal static Vector3 Unit(double x, double y, double z)
    {
        var length = Math.Sqrt(x * x + y * y + z * z);
        return length == 0 ? default : new((float)(x / length), (float)(y / length), (float)(z / length));
    }
}
