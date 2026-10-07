using Electron2D;
[GlobalClass]
internal sealed class ScriptActor : ScriptBaseActor
{
    internal static new readonly string SourcePath = Source();
    internal static readonly PropertyDescriptor<ScriptActor, int> CountProperty = new(nameof(Count), n => n.Count, (n, v) => n.Count = v, _ => 3, (n, v) => v >= 0, stored: true);
    internal static readonly PropertyDescriptor<ScriptActor, Node?> PeerProperty = new(nameof(Peer), n => n.Peer, (n, v) => n.Peer = v, _ => null, stored: true);
    public const int Limit = 12;
    public int Count { get; set; } = 3;
    public Node? Peer { get; set; }
    public event Action<int>? Pulse;
    internal int ReadyCalls, Frames;
    internal bool FailReady;
    public ScriptActor() { ProcessEnabled = true; }
    public ScriptActor(int count) : this() { Count = count; }
    public void Ping() => Pulse?.Invoke(Count);
    protected override void OnReady() { ReadyCalls++; if (FailReady) throw new InvalidOperationException("script activation"); }
    protected override void OnProcess(double delta) { Frames++; }
    private static Node CreateActor() => new ScriptActor();
    protected override Func<Node> CreateSceneInstanceFactory() => CreateActor;
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat([CountProperty, PeerProperty]);
    internal static new string Source([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
}
