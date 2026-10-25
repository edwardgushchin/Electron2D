namespace Electron2D;

internal static class CanvasSurfaceProgram
{
    private const string Prefix = "Electron2D:canvas_surface:";
    private static readonly int[] Widths = [3, 3, 1, 0, 3, 2];
    internal static byte[] Annotate(byte[] code, CanvasStageSource source) => CanvasStageProgram.Annotate(code, source, Prefix, Widths);
    internal static CanvasStageEntry? Read(byte[] code) => CanvasStageProgram.Read(code, Prefix, Widths);
    internal static byte[] RemoveMarker(byte[] code, CanvasStageEntry? entry) => CanvasStageProgram.RemoveMarker(code, entry, Prefix);
}
