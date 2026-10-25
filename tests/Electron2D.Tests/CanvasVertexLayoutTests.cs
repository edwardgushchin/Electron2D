using System.Runtime.InteropServices;
using Electron2D;

internal static class CanvasVertexLayoutTests
{
    internal static void Run()
    {
        if (Marshal.SizeOf<CanvasVertex>() != 172 ||
            Marshal.OffsetOf<CanvasVertex>("<VertexID>k__BackingField").ToInt32() != 164 ||
            Marshal.OffsetOf<CanvasVertex>("<InstanceID>k__BackingField").ToInt32() != 168)
            throw new InvalidOperationException("Canvas vertex identities require an exact uint2 GPU layout.");
        var vertex = new CanvasVertex(Vector2.Zero, Colors.White);
        if (vertex.VertexID != uint.MaxValue || vertex.InstanceID != uint.MaxValue)
            throw new InvalidOperationException("Ordinary canvas geometry must select native vertex and instance builtins.");
        Console.WriteLine("Canvas vertex layout: exact identity offsets, stride and native builtin defaults passed.");
    }
}
