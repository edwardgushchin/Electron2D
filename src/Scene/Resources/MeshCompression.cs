using System.Buffers.Binary;

namespace Electron2D;

internal sealed class MeshCompression
{
    internal readonly Vector4 UVScale;
    internal readonly byte[] UVs;
    internal readonly byte[] UV2s;
    internal readonly byte[] Frames;
    private bool Scaled => UVScale.X >= .00001f || UVScale.Y >= .00001f || UVScale.Z >= .00001f || UVScale.W >= .00001f;
    internal MeshCompression(MeshSurfaceData data, Vector4? scale = null, byte[]? uvs = null, byte[]? uv2s = null, byte[]? frames = null)
    {
        MeshFrameCodec.SynthesizeTangents(data);
        if (data.Tangents.Length != 0 && data.Normals.Length == 0) throw new ArgumentException("Compressed tangents require normals.");
        UVScale = scale ?? Scale(data);
        if (!UVScale.IsFinite() || UVScale.X < 0 || UVScale.Y < 0 || UVScale.Z < 0 || UVScale.W < 0) throw new ArgumentException("Compressed UV scale must remain finite and nonnegative.");
        UVs = uvs ?? new byte[checked(data.UVs.Length * 4)]; UV2s = uv2s ?? new byte[checked(data.UV2s.Length * 4)]; Frames = frames ?? new byte[checked(data.Normals.Length * 6)];
        if (UVs.Length != data.UVs.Length * 4L || UV2s.Length != data.UV2s.Length * 4L || Frames.Length != data.Normals.Length * 6L) throw new ArgumentException("Compressed channel counts differ.");
        for (var i = 0; i < data.UVs.Length; i++) { if (uvs is null) WriteUV(data.UVs[i], false, UVs.AsSpan(i * 4)); data.UVs[i] = ReadUV(UVs.AsSpan(i * 4), false); }
        for (var i = 0; i < data.UV2s.Length; i++) { if (uv2s is null) WriteUV(data.UV2s[i], true, UV2s.AsSpan(i * 4)); data.UV2s[i] = ReadUV(UV2s.AsSpan(i * 4), true); }
        for (var i = 0; i < data.Normals.Length; i++) { if (frames is null) WriteFrame(data.Normals[i], data.Tangents[i], Frames.AsSpan(i * 6)); RestoreFrame(data, i); }
    }
    internal MeshCompression Copy(MeshSurfaceData data) => new(data, UVScale, (byte[])UVs.Clone(), (byte[])UV2s.Clone(), (byte[])Frames.Clone());
    private static Vector4 Scale(MeshSurfaceData data)
    {
        Vector4 maximum = default; var unit = true;
        foreach (var uv in data.UVs) { unit &= uv.X >= 0 && uv.X <= 1 && uv.Y >= 0 && uv.Y <= 1; maximum.X = Math.Max(maximum.X, Math.Abs(uv.X)); maximum.Y = Math.Max(maximum.Y, Math.Abs(uv.Y)); }
        foreach (var uv in data.UV2s) { unit &= uv.X >= 0 && uv.X <= 1 && uv.Y >= 0 && uv.Y <= 1; maximum.Z = Math.Max(maximum.Z, Math.Abs(uv.X)); maximum.W = Math.Max(maximum.W, Math.Abs(uv.Y)); }
        return unit ? default : maximum * 2;
    }
    private void WriteUV(Vector2 value, bool secondary, Span<byte> target)
    {
        if (Scaled)
        {
            var scale = secondary ? new Vector2(UVScale.Z, UVScale.W) : new Vector2(UVScale.X, UVScale.Y);
            value = new(scale.X == 0 ? .5f : value.X / scale.X + .5f, scale.Y == 0 ? .5f : value.Y / scale.Y + .5f);
        }
        BinaryPrimitives.WriteUInt16LittleEndian(target, UNORM(value.X)); BinaryPrimitives.WriteUInt16LittleEndian(target[2..], UNORM(value.Y));
    }
    internal Vector2 ReadUV(ReadOnlySpan<byte> source, bool secondary)
    {
        var value = new Vector2(BinaryPrimitives.ReadUInt16LittleEndian(source) / 65535f, BinaryPrimitives.ReadUInt16LittleEndian(source[2..]) / 65535f);
        if (Scaled) { var scale = secondary ? new Vector2(UVScale.Z, UVScale.W) : new Vector2(UVScale.X, UVScale.Y); value = (value - new Vector2(.5f, .5f)) * scale; }
        return value;
    }
    private static ushort UNORM(float value) => (ushort)Math.Clamp(value * 65535f, 0, 65535);
    internal static void WriteFrame(Vector3 normal, Vector4 tangent, Span<byte> target)
    {
        var n = MeshFrameCodec.Unit(normal); var t = MeshFrameCodec.Unit(new Vector3(tangent.X, tangent.Y, tangent.Z)); var b = MeshFrameCodec.Unit(n.Cross(t));
        AxisAngle(t, b, n, out var axis, out var angle);
        var packedAngle = tangent.W < 0 ? Math.Clamp((1 - angle / MathF.PI) * .5f, 0, .49999f) : Math.Clamp(angle / MathF.PI * .5f + .5f, .500008f, 1);
        BinaryPrimitives.WriteUInt32LittleEndian(target, MeshFrameCodec.PackNormal(axis)); BinaryPrimitives.WriteUInt16LittleEndian(target[4..], UNORM(packedAngle));
    }
    internal void RestoreFrame(MeshSurfaceData data, int index)
    {
        ReadFrame(Frames.AsSpan(index * 6), out data.Normals[index], out data.Tangents[index]);
    }
    internal static void ReadFrame(ReadOnlySpan<byte> source, out Vector3 normal, out Vector4 tangent)
    {
        var axis = MeshFrameCodec.Normal(BinaryPrimitives.ReadUInt32LittleEndian(source)); var encoded = BinaryPrimitives.ReadUInt16LittleEndian(source[4..]) / 65535f;
        var angle = Math.Abs(encoded * 2 - 1) * MathF.PI; var cosine = MathF.Cos(angle); var sine = MathF.Sin(angle); var one = 1 - cosine;
        var row = new Vector3(axis.X * axis.X + cosine * (1 - axis.X * axis.X), axis.X * axis.Y * one - axis.Z * sine, axis.X * axis.Z * one + axis.Y * sine);
        normal = new(axis.X * axis.Z * one - axis.Y * sine, axis.Y * axis.Z * one + axis.X * sine, axis.Z * axis.Z + cosine * (1 - axis.Z * axis.Z));
        tangent = new(row.X, row.Y, row.Z, encoded > .5f ? 1 : -1);
    }
    private static void AxisAngle(Vector3 x, Vector3 y, Vector3 z, out Vector3 axis, out float angle)
    {
        const float epsilon = .00001f;
        if (Math.Abs(x.Y - y.X) < epsilon && Math.Abs(x.Z - z.X) < epsilon && Math.Abs(y.Z - z.Y) < epsilon)
        {
            if (Math.Abs(x.Y) < epsilon && Math.Abs(x.Z) < epsilon && Math.Abs(y.X) < epsilon && Math.Abs(y.Z) < epsilon && Math.Abs(z.X) < epsilon && Math.Abs(z.Y) < epsilon && Math.Abs(x.X + y.Y + z.Z - 3) < 3 * epsilon) { axis = new(0, 1, 0); angle = 0; return; }
            var xx = (x.X + 1) * .5f; var yy = (y.Y + 1) * .5f; var zz = (z.Z + 1) * .5f; var xy = (x.Y + y.X) * .25f; var xz = (x.Z + z.X) * .25f; var yz = (y.Z + z.Y) * .25f;
            if (xx > yy && xx > zz) { var root = MathF.Sqrt(xx); axis = xx < epsilon ? new(0, MathF.Sqrt(.5f), MathF.Sqrt(.5f)) : new(root, xy / root, xz / root); }
            else if (yy > zz) { var root = MathF.Sqrt(yy); axis = yy < epsilon ? new(MathF.Sqrt(.5f), 0, MathF.Sqrt(.5f)) : new(xy / root, root, yz / root); }
            else { var root = MathF.Sqrt(zz); axis = zz < epsilon ? new(MathF.Sqrt(.5f), MathF.Sqrt(.5f), 0) : new(xz / root, yz / root, root); }
            angle = MathF.PI; return;
        }
        var difference = new Vector3(z.Y - y.Z, x.Z - z.X, y.X - x.Y); var length = Math.Sqrt((double)difference.X * difference.X + (double)difference.Y * difference.Y + (double)difference.Z * difference.Z);
        if (Math.Abs(length) < epsilon) length = 1;
        axis = new((float)(difference.X / length), (float)(difference.Y / length), (float)(difference.Z / length)); angle = MathF.Acos(Math.Clamp((x.X + y.Y + z.Z - 1) * .5f, -1, 1));
    }
}
