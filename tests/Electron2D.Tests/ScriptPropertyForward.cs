using Electron2D;

internal sealed class ScriptPropertyForward : ScriptPropertyBase
{
    internal new static readonly string SourcePath = Source();
    internal static readonly PropertyDescriptor<RigidBody, int> Amount = new("ScriptValue", b => Get(b).Value, (b, v) => SetValue(b, v), _ => 3, (_, v) => v >= 0, stored: true) { Hint = PropertyHint.Range, HintString = "0,100,1" };
    internal static readonly PropertyDescriptor<RigidBody, Node?> Target = new("ScriptTarget", b => Get(b).Target, (b, v) => { Guard(b); Get(b).Target = v; }, _ => null, stored: true);
    internal static void InstallBody(RigidBody owner) => ScriptPropertyTests.InstallBody(owner, 1);
    internal static void ClearBody(RigidBody owner) => Clear(owner);
    private static string Source([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
}
