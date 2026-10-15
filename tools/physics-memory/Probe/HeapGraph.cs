using Microsoft.Diagnostics.Runtime;

internal static class HeapGraph
{
    internal static long Calibrate(ClrObject calibration)
    {
        var left = calibration.ReadObjectField("Left"); var weak = calibration.ReadObjectField("Weak");
        var graph = Walk(calibration, []);
        if (left.Address != calibration.ReadObjectField("Right").Address || graph.Count != 3 ||
            graph.Sum(o => checked((long)o.Size)) != checked((long)(calibration.Size + left.Size + weak.Size)))
            throw new InvalidDataException("Heap calibration failed: shared array or weak-target ownership.");
        return graph.Sum(o => checked((long)o.Size));
    }
    internal static List<ClrObject> Walk(ClrObject root, HashSet<ulong> boundaries, HashSet<ulong>? seen = null)
    {
        seen ??= []; var result = new List<ClrObject>(); var pending = new Stack<ClrObject>(); pending.Push(root);
        while (pending.TryPop(out var o))
        {
            if (o.IsNull || boundaries.Contains(o.Address) || !seen.Add(o.Address)) continue;
            if (!o.IsValid || o.IsFree) throw new InvalidDataException("Invalid object in the owned graph.");
            result.Add(o);
            foreach (var next in o.EnumerateReferences(carefully: true, considerDependantHandles: false)) pending.Push(next);
        }
        return result;
    }
}
