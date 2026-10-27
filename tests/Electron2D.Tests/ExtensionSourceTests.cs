using System.Runtime.CompilerServices;
using Electron2D;

internal static class ExtensionSourceTests
{
    private const string Address = "test:source-native-extension";
    private static int _version = 1;
    internal static void Run()
    {
        var sourcePath = SourcePath();
        ExtensionManager.LoadExtensionFromFunction(Address, Open);
        var module = ExtensionManager.GetExtension(Address)!;
        try
        {
            Script.RegisterNode<ExtensionSourceActor>("test:extension-source-actor", sourcePath, CreateActor, ExtensionSourceActor.Own);
            ResourceFileTypes.RegisterResource<ExtensionDataChild>("test:extension-data-child", CreateData);
            using var script = ResourceLoader.Load<Script>(sourcePath);
            Check(script.CanInstantiate() && script.GetInstanceBaseType() == typeof(ExtensionNativeNode), "Script metadata preserves its native extension shell role.");
            Check(script.GetScriptPropertyList().Length == 1 && !script.HasScriptMethod(nameof(ExtensionNativeNode.NativeMethod)), "Native shell declarations are not script declarations.");
            ExtensionSourceActor.ConstructorHook = () => Reject<InvalidOperationException>(() => ExtensionManager.UnloadExtension(Address));
            using var actor = script.New<ExtensionSourceActor>(); ExtensionSourceActor.ConstructorHook = null;
            Check(actor.SourceSeen && actor.Generation == 1 && actor.GetScript() == script, "Source construction identity and inherited extension binding coexist.");
            actor.NativeValue = 17; actor.OwnValue = 23;
            using var duplicate = (ExtensionSourceActor)actor.Duplicate();
            using var stripped = (ExtensionNativeNode)actor.Duplicate(Node.DuplicateFlags.Groups);
            Check(stripped.GetType() == typeof(ExtensionNativeNode) && stripped.GetScript() is null && stripped.Generation == 1 && stripped.NativeValue == 17,
                "Removing Scripts preserves the constructible native extension shell and its values.");
            using var data = new ExtensionDataChild { Value = 31 };
            using var dataCopy = (ExtensionDataChild)data.Duplicate();
            Check(dataCopy.Generation == 1 && dataCopy.Value == 31, "Resource duplication binds registered ancestors before copying actual state.");
            _version = 2; ExtensionManager.ReloadExtension(Address);
            Check(actor.Generation == 2 && duplicate.Generation == 2 && stripped.Generation == 2 && dataCopy.Generation == 2 && actor.NativeValue == 17 && duplicate.OwnValue == 23 && dataCopy.Value == 31,
                "Script, stripped node and resource descendants restore together through one native module generation.");
            Check(actor.Notifications == 1 && actor.GetScript() == script, "Extension notification preserves the independent Script association.");
            using var veto = module.New<ExtensionNativeNode>("test:extension-native-node"); veto.Veto = true; veto.Dispose();
            Check(!veto.IsDisposed && veto.Generation == 2 && veto.Clears == 0, "A pre-delete veto preserves the active module binding.");
            veto.Veto = false; veto.Dispose(); Check(veto.IsDisposed && veto.Generation == 0 && veto.Clears == 1, "Committed disposal clears once before native owner cleanup.");
            using var foreign = module.New<ExtensionNativeNode>("test:extension-native-node");
            Exception? error = null; var worker = new Thread(() => { try { foreign.Dispose(); } catch (Exception failure) { error = failure; } }); worker.Start(); worker.Join();
            Check(error is InvalidOperationException && !foreign.IsDisposed && foreign.Generation == 2, "Foreign bound-instance disposal rejects before pre-delete.");
            var failing = module.New<ExtensionNativeNode>("test:extension-native-node"); failing.FailClear = true;
            Check(ReferenceEquals(Reject<InvalidOperationException>(failing.Dispose), ExtensionNativeNode.Expected) && failing.IsDisposed && failing.Generation == 0,
                "Clear callback errors do not skip committed owner cleanup.");
            ExtensionManager.UnloadExtension(Address);
            Check(!script.CanInstantiate(), "A Script cannot instantiate a missing native extension base.");
            Reject<InvalidOperationException>(() => script.New<ExtensionSourceActor>());
        }
        finally
        {
            ExtensionSourceActor.ConstructorHook = null;
            if (ExtensionManager.IsExtensionLoaded(Address)) ExtensionManager.UnloadExtension(Address);
            module.Dispose();
        }
        Console.WriteLine("Extension source/native inheritance, factory families and disposal passed.");
    }
    private static string SourcePath([CallerFilePath] string path = "") => path;
    private static ExtensionSourceActor CreateActor() => new();
    private static ExtensionNativeNode CreateNative() => new();
    private static ExtensionDataChild CreateData() => new();
    private static ExtensionData CreateNativeData() => new();
    private static ExtensionInitialization? Open(Extension module)
    {
        var version = _version;
        return new(ExtensionInitializationLevel.Scene, level =>
        {
            if (level != ExtensionInitializationLevel.Scene) return;
            module.RegisterClass<ExtensionNativeNode>("test:extension-native-node", CreateNative,
                node => { node.Generation = version; node.NativeValue = 0; }, node => { node.Clears++; node.Generation = 0; if (node.FailClear) throw ExtensionNativeNode.Expected; });
            module.RegisterClass<ExtensionData>("test:extension-native-data", CreateNativeData,
                data => { data.Generation = version; data.Value = 0; }, data => data.Generation = 0);
        }, _ => { });
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static T Reject<T>(Action action) where T : Exception { try { action(); } catch (T error) { return error; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
internal class ExtensionNativeNode : Node
{
    internal int NativeValue, Generation, Notifications, Clears;
    internal bool Veto, FailClear;
    internal static readonly InvalidOperationException Expected = new("Expected native-shell cleanup error");
    private static readonly PropertyDescriptor<ExtensionNativeNode, int> Native = new("NativeValue", n => n.NativeValue, (n, v) => n.NativeValue = v, _ => 0, stored: true);
    public int NativeMethod() => Generation;
    protected override Func<Node> CreateSceneInstanceFactory() => Create;
    private static Node Create() => new ExtensionNativeNode();
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() { foreach (var p in base.GetPropertyDescriptors()) yield return p; yield return Native; }
    protected override void OnNotification(int what) { base.OnNotification(what); if (what == NotificationPreDelete && Veto) CancelDisposal(); if (what == NotificationExtensionReloaded) Notifications++; }
}
internal sealed class ExtensionSourceActor : ExtensionNativeNode
{
    internal static Action? ConstructorHook;
    internal readonly bool SourceSeen;
    internal int OwnValue;
    internal static readonly PropertyDescriptor<ExtensionSourceActor, int> Own = new("OwnValue", n => n.OwnValue, (n, v) => n.OwnValue = v, _ => 0, stored: true);
    internal ExtensionSourceActor() { SourceSeen = GetScript() is not null; ConstructorHook?.Invoke(); }
    protected override Func<Node> CreateSceneInstanceFactory() => Create;
    private static Node Create() => new ExtensionSourceActor();
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() { foreach (var p in base.GetPropertyDescriptors()) yield return p; yield return Own; }
}
internal class ExtensionData : Resource
{
    internal int Value, Generation;
    private static readonly PropertyDescriptor<ExtensionData, int> Field = new("Value", d => d.Value, (d, v) => d.Value = v, _ => 0, stored: true);
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() { foreach (var p in base.GetPropertyDescriptors()) yield return p; yield return Field; }
    protected override Resource CreateDuplicateInstance() => new ExtensionData();
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicate, Func<Resource?, Resource?> forceDuplicate) => ((ExtensionData)target).Value = Value;
}
internal sealed class ExtensionDataChild : ExtensionData
{
    protected override Resource CreateDuplicateInstance() => new ExtensionDataChild();
}
