namespace Electron2D;

public sealed partial class RenderingServer
{
    internal static void DrawPhysicsShape(RID rid, Shape shape, Color color)
    {
        var item = RequireService().CommandTarget(rid);
        using var scope = item.StartServerDrawing();
        shape.DrawToCanvas(item, color);
    }
}
