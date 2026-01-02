using System.Reflection;

namespace Electron2D;

internal static class ProcessingOverrideCache
{
    private static readonly Dictionary<Type, (bool ProcessOverridden, bool PhysicsOverridden)> Cache = new();

    internal static (bool ProcessOverridden, bool PhysicsOverridden) Get(Type type)
    {
        lock (Cache)
        {
            if (Cache.TryGetValue(type, out var v))
                return v;

            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

            var process = IsOverridden(type, "Process", Flags, typeof(float));
            var physics = IsOverridden(type, "PhysicsProcess", Flags, typeof(float));

            v = (process, physics);
            Cache[type] = v;
            return v;
        }
    }

    private static bool IsOverridden(Type type, string name, BindingFlags flags, params Type[] args)
    {
        var m = type.GetMethod(name, flags, binder: null, types: args, modifiers: null);
        if (m is null) return false;

        // Если не переопределён — DeclaringType будет Node (унаследованная реализация).
        return m.DeclaringType != typeof(Node);
    }
}