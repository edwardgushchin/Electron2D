using Electron2D;

internal sealed class LiveScriptResource : Resource
{
    internal static readonly PropertyDescriptor<LiveScriptResource, float> GainProperty = new(nameof(Gain), r => r.Gain, (r, v) => r.Gain = v, _ => 1, stored: true);
    internal static readonly PropertyDescriptor<LiveScriptResource, Resource?> LinkProperty = new(nameof(Link), r => r.Link, (r, v) => r.Link = v, _ => null, stored: true);
    public float Gain { get; set; } = 1;
    public Resource? Link { get; set; }
    internal Func<LiveScriptResource, float>? Evaluate;
    internal object? State;
    internal readonly Script? ConstructorSource;
    internal int Binds, Clears, Resets;
    internal bool FailBind, FailClear, FailCopy, Veto;
    internal readonly Exception BindError = new IOException("resource bind"), ClearError = new IOException("resource clear"), CopyError = new IOException("resource copy");
    internal LiveScriptResource() => ConstructorSource = GetScript();
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat([GainProperty, LinkProperty]);
    protected override Resource CreateDuplicateInstance() => new LiveScriptResource();
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicate, Func<Resource?, Resource?> force)
    {
        var copy = (LiveScriptResource)target; copy.Gain = Gain; copy.Link = duplicate(Link);
        if (copy.FailCopy) throw copy.CopyError;
    }
    protected override void OnResetState() => Resets++;
    protected override void OnNotification(int what)
    {
        if (what == NotificationPreDelete && Veto) { Veto = false; CancelDisposal(); }
        base.OnNotification(what);
    }
}
