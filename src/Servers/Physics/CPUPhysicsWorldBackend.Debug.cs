using static Box2D.NET.B2Worlds;

namespace Electron2D;

internal sealed partial class CPUPhysicsWorldBackend
{
    internal override int ReadDebugContacts(Span<Vector2> output)
    {
        var count = 0;
        var world = b2GetWorldFromId(WorldID);
        foreach (var color in world.constraintGraph.colors)
            foreach (var contact in color.contactSims.data.AsSpan(0, color.contactSims.count))
                for (var i = 0; i < contact.manifold.pointCount; i++)
                {
                    ref readonly var point = ref contact.manifold.points[i];
                    if (point.separation >= 0) continue;
                    var half = point.separation * .5f * contact.manifold.normal;
                    Add(ToScene(point.point - half), output, ref count);
                    if (count == output.Length) return count;
                    Add(ToScene(point.point + half), output, ref count);
                    if (count == output.Length) return count;
                }
        return count;
    }
    private static void Add(Vector2 point, Span<Vector2> output, ref int count)
    {
        if (count == output.Length) return;
        if (!point.IsFinite()) throw new InvalidOperationException("Physics contact diagnostics contain a nonfinite point.");
        output[count++] = point;
    }
}
