using Electron2D;

[GlobalClass]
internal sealed class ScriptPlainObject : ScriptPlainBase
{
    internal static new readonly string SourcePath = Source();
    internal static readonly PropertyDescriptor<ScriptPlainObject, int> CountProperty = new(nameof(Count), n => n.Count, (n, v) => n.Count = v, _ => 3, stored: true);
    public const int Limit = 12;
    public int Count { get; set; } = 3;
    public event Action<ScriptPlainObject>? Pulse;
    internal readonly Script? ConstructorSource;
    internal bool VetoDisposal;
    internal int Cleanups;
    public ScriptPlainObject() { ConstructorSource = GetScript(); }
    public void Ping() { Count++; BeginEventEmission(Pulse)?.Invoke(this); }
    protected override void OnNotification(int what)
    {
        if (what == 7001) Ping();
        if (what == NotificationPreDelete && VetoDisposal) { VetoDisposal = false; CancelDisposal(); }
        base.OnNotification(what);
    }
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Append(CountProperty);
    protected override void Dispose(bool disposing) { Cleanups++; Pulse = null; base.Dispose(disposing); }
    private static string Source([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
}
