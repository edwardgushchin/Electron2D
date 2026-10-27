using Electron2D;

internal abstract class ScriptPlainBase : ElectronObject
{
    internal static readonly string SourcePath = Source();
    internal static readonly PropertyDescriptor<ScriptPlainBase, int> PowerProperty = new(nameof(Power), n => n.Power, (n, v) => n.Power = v, _ => 7, stored: true);
    public int Power { get; set; } = 7;
    public const string Category = "Plain";
    internal readonly Script? BaseConstructorSource;
    protected ScriptPlainBase() { BaseConstructorSource = GetScript(); }
    public int Twice(int value) => value * 2;
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Append(PowerProperty);
    private static string Source([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
}
