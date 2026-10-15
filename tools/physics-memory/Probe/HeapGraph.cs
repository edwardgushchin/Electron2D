using Microsoft.Diagnostics.Runtime;

internal static class HeapGraph
{
    internal static object CPUWorldPool(ClrRuntime runtime, HashSet<ulong> selectedOwners)
    {
        var module = runtime.EnumerateModules().Single(m => Path.GetFileName(m.Name) == "Electron2D.dll");
        var type = module.GetTypeByName("Box2D.NET.B2Worlds");
        var field = type?.StaticFields.Single(f => f.Name == "b2_worlds");
        var domain = runtime.AppDomains.Single();
        var initialized = field is not null && field.IsInitialized(domain);
        var pool = initialized ? field!.ReadObject(domain) : default;
        if (initialized && pool.IsNull) throw new InvalidDataException("Initialized CPU world pool is null.");
        var slots = pool.IsNull ? [] : Enumerable.Range(0, pool.AsArray().Length).Select(i => pool.AsArray().GetObjectValue(i)).ToArray();
        var active = slots.Where(o => o.ReadField<bool>("inUse")).ToArray();
        // Active numerical worlds borrow callbacks and engine owners; only their object shells belong here.
        var objects = Walk(pool, active.Select(o => o.Address).ToHashSet());
        if (objects.Any(o => o.Type?.Name is "Box2D.NET.B2SolverBlock[]" or "Box2D.NET.B2GraphColor[]"))
            throw new InvalidDataException("An idle static CPU world slot retains retired solver scratch.");
        objects.AddRange(active);
        var bytes = objects.Sum(o => checked((long)o.Size));
        var shared = objects.Where(o => selectedOwners.Contains(o.Address)).Sum(o => checked((long)o.Size));
        return new
        {
            Initialized = initialized,
            Slots = slots.Length,
            ActiveSlots = active.Length,
            IdleSlots = slots.Length - active.Length,
            Objects = objects.Count,
            ManagedObjectBytes = bytes,
            SharedWithSelectedOwnersBytes = shared,
            AdditionalManagedObjectBytes = bytes - shared,
            Types = objects.GroupBy(o => o.Type!.Name!).Select(g => new { Type = g.Key, Count = g.Count(), Bytes = g.Sum(o => checked((long)o.Size)) })
                .OrderByDescending(x => x.Bytes).ToArray()
        };
    }

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
