using System.Runtime.CompilerServices;
using Electron2D;

internal sealed class RoleResourceCode
{
    internal static readonly string SourcePath = Source();
    internal static readonly Exception Error = new IOException("resource role hook");
    internal static Resource? LastReset;
    internal sealed class State
    {
        internal int Value = 5, Transient = 44, Resets, Paths, RIDCalls, ValueAtReset, Clears, Retired;
        internal RID Identity;
        internal string Path = "";
        internal bool FailReset, FailPath, FailRID;
    }
    private static readonly ConditionalWeakTable<Resource, State> States = new();
    internal static State Get(Resource owner) => States.GetValue(owner, static _ => throw new InvalidOperationException("No resource hook state."));
    internal static readonly PropertyDescriptor<Resource, int> Value = new("RoleValue", r => Get(r).Value, (r, v) => { r.ResourceName = r.ResourceName; Get(r).Value = v; }, _ => 5, stored: true);
    internal static void Bind(Resource owner)
    {
        var state = new State(); States.Add(owner, state);
        owner.SetScriptCallbacks(new ResourceScriptCallbacks<Resource>
        {
            GetRID = _ => { state.RIDCalls++; if (state.FailRID) throw Error; return state.Identity; },
            ResetState = resource => { LastReset = resource; state.Resets++; state.ValueAtReset = state.Value; if (resource is RoleHookTests.NativeResource native && !native.ResetVisited) throw Error; if (state.FailReset) throw Error; state.Transient = 0; },
            SetPathCache = (resource, path) => { state.Paths++; state.Path = path; if (resource.ResourcePath != path || resource is RoleHookTests.NativeResource native && native.PathVisited != path) throw Error; if (state.FailPath) throw Error; },
            Retired = _ => state.Retired++
        });
    }
    internal static void Clear(Resource owner) { if (States.TryGetValue(owner, out var state)) { state.Clears++; States.Remove(owner); } }
    private static string Source([CallerFilePath] string path = "") => path;
}
