using System.Runtime.CompilerServices;
using Electron2D;

internal class ObjectCallbackCode
{
    internal static readonly string SourcePath = Source();
    internal static readonly Exception Error = new IOException("object callback"), ClearError = new IOException("object clear"), RetiredError = new IOException("object retired");
    internal static bool FailBind, FailClear, FailRetired, FailValidation, FailDiscovery, Collision;
    internal static ElectronObjectScriptCallbacks<ElectronObject>? NextCallbacks;
    internal sealed class State
    {
        internal int Value = 3, Notifications, Discoveries, Validations, Clears, Retired, Setups, SetupValue;
        internal bool Cancel, Hide = true, AllowRevert = true;
        internal Resource? Controller;
        internal Node? Scene;
        internal readonly PropertyDescriptor MetadataValue = new PropertyDescriptor<ElectronObject, int>("LiveValue", o => Get(o).Value, SetValue, _ => 3, (_, v) => v >= 0, stored: true) { Hint = PropertyHint.Range, HintString = "0,100" };
    }
    private static readonly ConditionalWeakTable<ElectronObject, State> States = new();
    internal static readonly List<State> Created = [];
    internal static State Get(ElectronObject owner) => States.GetValue(owner, static _ => throw new InvalidOperationException("Retired object state."));
    internal static State Install(ElectronObject owner) { var s = new State(); States.Add(owner, s); Created.Add(s); return s; }
    internal static readonly PropertyDescriptor<ElectronObject, int> Value = new("LiveValue", o => Get(o).Value, SetValue, _ => 3, (_, v) => v >= 0, stored: true);
    internal static readonly PropertyDescriptor<ElectronObject, Resource?> Controller = new("LiveController", o => Get(o).Controller, (o, v) => { Guard(o); Get(o).Controller = v; }, _ => null, stored: true);
    private static readonly PropertyDescriptor<ElectronObject, int> NativeCollision = new(nameof(ElectronObject.InstanceID), o => Get(o).Value, SetValue, _ => 3, stored: true);
    internal static void Guard(ElectronObject owner) { if (owner is Node n) n.ProcessPriority = n.ProcessPriority; else if (owner is Resource r) r.ResourceName = r.ResourceName; else owner.SetBlockSignals(owner.IsBlockingSignals()); }
    private static void SetValue(ElectronObject owner, int value) { Guard(owner); Get(owner).Value = value; }
    internal static IEnumerable<PropertyDescriptor> Discover(ElectronObject owner) { if (FailDiscovery) throw Error; Get(owner).Discoveries++; return Collision ? [Value, Controller, NativeCollision] : [Value, Controller]; }
    internal static PropertyDescriptor? Validate(ElectronObject owner, PropertyDescriptor property)
    {
        if (FailValidation) throw Error;
        var state = Get(owner); state.Validations++;
        if (Collision && property.Name == nameof(ElectronObject.InstanceID) && property.ValueType == typeof(ulong)) return null;
        return property == Value ? state.MetadataValue : property.Name == nameof(ElectronObject.TranslationDomain) && state.Hide ? null : property;
    }
    internal static bool CanRevert(ElectronObject owner, PropertyDescriptor property) => Get(owner).AllowRevert && property.CanRevert(owner);
    internal static void Notify(ElectronObject owner, int what, bool reversed)
    {
        var state = Get(owner); state.Notifications++;
        if (what == ElectronObject.NotificationPreDelete && state.Cancel) { if (!reversed) throw Error; owner.CancelDisposal(); }
    }
    internal static void Retire(State state) { state.Retired++; if (FailRetired) throw RetiredError; }
    internal static void Bind(ElectronObject owner)
    {
        var state = Install(owner);
        owner.SetScriptCallbacks(NextCallbacks ?? new ElectronObjectScriptCallbacks<ElectronObject>
        {
            Notification = Notify,
            PropertyDescriptors = Discover,
            ValidateProperty = Validate,
            CanRevertProperty = CanRevert,
            StringRepresentation = _ => "object-code",
            Retired = _ => Retire(state)
        });
        if (FailBind) throw Error;
    }
    internal static void Clear(ElectronObject owner) { if (States.TryGetValue(owner, out var state)) { state.Clears++; States.Remove(owner); } if (FailClear) throw ClearError; }
    private static string Source([CallerFilePath] string path = "") => path;
}
