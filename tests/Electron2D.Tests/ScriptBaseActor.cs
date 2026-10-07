using Electron2D;
[Tool]
internal abstract class ScriptBaseActor : Node
{
    internal static readonly string SourcePath = Source();
    internal static readonly PropertyDescriptor<ScriptBaseActor, int> PowerProperty = new(nameof(Power), n => n.Power, (n, v) => n.Power = v, _ => 7, stored: true);
    public int Power { get; set; } = 7;
    public const string Category = "Actor";
    public virtual int Twice(int value) => value * 2;
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Append(PowerProperty);
    internal static string Source([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
}
