namespace Electron2D;

internal static class CanvasLightProgram
{
    private const string Prefix = "Electron2D:canvas_light:";
    private static readonly int[] Widths = [4, 4, 3, 1, 0, 3, 3, 3, 4, 4];
    internal static byte[] Annotate(byte[] code, CanvasStageSource source) => CanvasStageProgram.Annotate(code, source, Prefix, Widths);
    internal static CanvasStageEntry? Read(byte[] code) => CanvasStageProgram.Read(code, Prefix, Widths);
    internal static byte[] RemoveMarker(byte[] code, CanvasStageEntry? entry) => CanvasStageProgram.RemoveMarker(code, entry, Prefix);
}
