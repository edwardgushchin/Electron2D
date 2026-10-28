using System.Runtime.CompilerServices;
using Electron2D;

internal class ScriptPropertyBase
{
    internal static readonly string SourcePath = Source();
    internal sealed class State
    {
        internal int Value, Mode, Clears, Retired;
        internal Resource? Payload;
        internal float[] Values = [];
        internal Node? Target;
    }
    private static readonly ConditionalWeakTable<ElectronObject, State> States = new();
    internal static State Get(ElectronObject owner) => States.GetValue(owner, static _ => throw new InvalidOperationException("No property binding state."));
    internal static readonly PropertyDescriptor<ElectronObject, int> Value = new("ScriptValue", o => Get(o).Value, SetValue, _ => 1, (_, v) => v >= 0, stored: true);
    internal static readonly PropertyDescriptor<ElectronObject, Resource?> Payload = new("ScriptPayload", o => Get(o).Payload, (o, v) => { Guard(o); Get(o).Payload = v; }, _ => null, stored: true);
    internal static readonly PropertyDescriptor<ElectronObject, float[]> Values = new("ScriptValues", o => Get(o).Values, (o, v) => { Guard(o); Get(o).Values = v; }, _ => [], stored: true);
    internal static void Guard(ElectronObject owner)
    {
        if (owner is Node node) node.ProcessPriority = node.ProcessPriority;
        else if (owner is Resource resource) resource.ResourceName = resource.ResourceName;
        else owner.SetBlockSignals(owner.IsBlockingSignals());
    }
    internal static void SetValue(ElectronObject owner, int value)
    { Guard(owner); Get(owner).Value = value; if (owner is Resource resource) resource.EmitChanged(); }
    internal static State Install(ElectronObject owner, int mode)
    { var state = new State { Value = mode == 0 ? 1 : 10 * mode, Mode = mode }; States.Add(owner, state); return state; }
    internal static void Install(ElectronObject owner) => Install(owner, 0);
    internal static void Clear(ElectronObject owner) { if (States.TryGetValue(owner, out var state)) { state.Clears++; States.Remove(owner); } if (ScriptPropertyTests.FailClear) throw ScriptPropertyTests.ClearError; }
    private static string Source([CallerFilePath] string path = "") => path;
}
