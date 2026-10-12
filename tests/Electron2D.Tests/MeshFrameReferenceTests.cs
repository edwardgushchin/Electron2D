using System.Runtime.InteropServices;
using Electron2D;
using static TextureArrayTests;

internal static class MeshFrameReferenceTests
{
    internal static unsafe void Run()
    {
        var path = Environment.GetEnvironmentVariable("ELECTRON2D_MIKK_REFERENCE"); if (path is null) return;
        var library = NativeLibrary.Load(path);
        try
        {
            var generate = (delegate* unmanaged[Cdecl]<int, int, float*, float*, float*, int*, float*, int>)NativeLibrary.GetExport(library, "e2d_mikk_generate");
            var random = new Random(124577);
            for (var sample = 0; sample < 100; sample++)
            {
                var data = SurfaceToolTests.Grid(4); data.Normals = new Vector3[data.Vertices.Length]; data.UVs = new Vector2[data.Vertices.Length];
                for (var i = 0; i < data.Vertices.Length; i++) { data.Normals[i] = new(0, 0, sample % 2 == 0 ? -1 : 1); data.UVs[i] = sample < 2 ? data.Vertices[i] : new((float)random.NextDouble() * 4 - 2, (float)random.NextDouble() * 4 - 2); }
                var positions = new float[data.Vertices.Length * 2]; var normals = new float[data.Vertices.Length * 3]; var uvs = new float[data.Vertices.Length * 2]; var expected = new float[data.Vertices.Length * 4];
                for (var i = 0; i < data.Vertices.Length; i++) { positions[i * 2] = data.Vertices[i].X; positions[i * 2 + 1] = data.Vertices[i].Y; normals[i * 3 + 2] = data.Normals[i].Z; uvs[i * 2] = data.UVs[i].X; uvs[i * 2 + 1] = data.UVs[i].Y; }
                fixed (float* p = positions, n = normals, uv = uvs, result = expected) fixed (int* indices = data.Indices) Check(generate(data.Vertices.Length, data.Indices.Length, p, n, uv, indices, result) != 0, "Native reference generation succeeds.");
                using var tool = new SurfaceTool(); tool.CreateFromArrays(data); tool.GenerateTangents(); var actual = tool.CommitToArrays().Tangents;
                for (var i = 0; i < actual.Length; i++)
                {
                    var tangent = new Vector4(expected[i * 4], expected[i * 4 + 1], expected[i * 4 + 2], expected[i * 4 + 3]);
                    Check(actual[i].DistanceTo(tangent) < .0002f, "MikkTSpace numerical parity for indexed UV seams, mirrored orientation and both normal directions.");
                }
            }
            Console.WriteLine("MikkTSpace C reference parity: 100 indexed geometry/UV fixtures passed.");
        }
        finally { NativeLibrary.Free(library); }
    }
}
