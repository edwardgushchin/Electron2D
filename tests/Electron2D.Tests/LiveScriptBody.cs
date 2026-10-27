using Electron2D;

internal sealed class LiveScriptBody : RigidBody
{
    internal static readonly string SourcePath = Source();
    internal static readonly PropertyDescriptor<LiveScriptBody, int> CounterProperty = new(nameof(Counter), b => b.Counter, (b, v) => b.Counter = v, _ => 0, stored: true);
    public int Counter { get; set; }
    internal Action<LiveScriptBody, double>? Step;
    internal IDisposable? State;
    internal readonly Script? ConstructorSource;
    internal int Binds, Clears;
    internal bool Veto, FailBind, FailClear;
    internal ManualResetEventSlim? BindEntered, ContinueBind;
    internal readonly Exception BindError = new IOException("bind failed"), ClearError = new IOException("clear failed");
    internal LiveScriptBody() { ConstructorSource = GetScript(); }
    protected override void OnPhysicsProcess(double delta) => Step?.Invoke(this, delta);
    protected override void OnNotification(int what)
    {
        if (what == NotificationPreDelete && Veto) { Veto = false; CancelDisposal(); }
        base.OnNotification(what);
    }
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Append(CounterProperty);
    private static Node CreateScene() => new LiveScriptBody();
    protected override Func<Node> CreateSceneInstanceFactory() => CreateScene;
    private static string Source([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
}
