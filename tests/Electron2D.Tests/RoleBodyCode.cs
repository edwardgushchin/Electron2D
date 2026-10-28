using System.Runtime.CompilerServices;
using Electron2D;

internal sealed class RoleBodyCode
{
    internal static readonly string SourcePath = Source();
    internal static readonly Exception Error = new IOException("node role query");
    internal sealed class State
    {
        internal int Speed = 5, Queries, FocusQueries, Clears, Retired;
        internal bool Fail;
        internal RID Focus;
        internal Resource? Controller;
        internal string[] Warnings = ["role warning"], AccessibilityWarnings = ["role accessibility"];
    }
    private static readonly ConditionalWeakTable<RigidBody, State> States = new();
    internal static State Get(RigidBody owner) => States.GetValue(owner, static _ => throw new InvalidOperationException("No body hook state."));
    internal static readonly PropertyDescriptor<RigidBody, int> Speed = new("RoleSpeed", r => Get(r).Speed, (r, v) => { r.ProcessPriority = r.ProcessPriority; Get(r).Speed = v; }, _ => 5, stored: true);
    internal static readonly PropertyDescriptor<RigidBody, Resource?> Controller = new("RoleController", r => Get(r).Controller, (r, v) => { r.ProcessPriority = r.ProcessPriority; Get(r).Controller = v; }, _ => null, stored: true);
    internal static void Bind(RigidBody owner)
    {
        var state = new State(); States.Add(owner, state);
        owner.SetScriptCallbacks(new RigidBodyScriptCallbacks<RigidBody>
        {
            GetConfigurationWarnings = _ => { state.Queries++; if (state.Fail) throw Error; return state.Warnings; },
            GetAccessibilityConfigurationWarnings = _ => { state.Queries++; if (state.Fail) throw Error; return state.AccessibilityWarnings; },
            GetFocusedAccessibilityElement = _ => { state.FocusQueries++; if (state.Fail) throw Error; return state.Focus; },
            PhysicsProcess = (body, _) => body.LinearVelocity = new(state.Speed, 0),
            Retired = _ => state.Retired++
        });
    }
    internal static void Clear(RigidBody owner) { if (States.TryGetValue(owner, out var state)) { state.Clears++; States.Remove(owner); } }
    private static string Source([CallerFilePath] string path = "") => path;
}
