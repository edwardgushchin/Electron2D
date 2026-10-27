using Electron2D;

internal static partial class ScriptTests
{
    private static void Initialization(Script script)
    {
        using var actor = script.New(() => new ScriptActor(23));
        Check(actor.BaseInitializations == 1 && actor.Initializations == 1 && actor.BaseConstructorSource == script && actor.BaseConstructorIdentity == actor.InstanceID && actor.ConstructorScript == script && actor.Count == 23,
            "compiled base/derived constructors initialize once with typed arguments and stable source/identity");
        using var copy = (ScriptActor)actor.Duplicate();
        using var packed = new PackedScene(); packed.Pack(actor);
        using var scene = (ScriptActor)packed.Instantiate();
        foreach (var restored in new[] { copy, scene })
            Check(restored.BaseInitializations == 1 && restored.Initializations == 1 && restored.ConstructorCount == 3 && restored.Count == 23 &&
                restored.BaseConstructorIdentity == restored.InstanceID && restored.BaseConstructorSource == script,
                "copy/scene construction precedes stored restoration without rerunning project initialization");

        using var required = ResourceLoader.Load<Script>(RequiredScriptActor.SourcePath);
        using var resource = ResourceLoader.Load<Script>(RequiredScriptResource.SourcePath);
        using var rpc = ResourceLoader.Load<Script>(RequiredScriptRPCActor.SourcePath);
        Check(!required.IsAbstract() && required.CanInstantiate() && resource.CanInstantiate() && rpc.CanInstantiate(), "concrete argument-only registrations remain instantiable without claiming a default constructor");
        RequiredScriptActor.Captured = null;
        Reject<InvalidOperationException>(() => required.New<RequiredScriptActor>());
        Check(RequiredScriptActor.Captured is null, "missing default arguments reject before invoking any project constructor");
        Reject<InvalidOperationException>(() => resource.New<RequiredScriptResource>());
        Reject<InvalidOperationException>(() => rpc.New<RequiredScriptRPCActor>());
        using var direct = required.New(() => new RequiredScriptActor(7));
        using var data = resource.New(() => new RequiredScriptResource("typed argument"));
        using var remote = rpc.New(() => new RequiredScriptRPCActor(11));
        Check(direct.Speed == 7 && direct.Initializations == 1 && direct.ConstructorSource == required && direct.ConstructorIdentity == direct.InstanceID &&
            data.Value == "typed argument" && data.Initializations == 1 && data.ConstructorSource == resource && remote.Value == 11 && remote.ConstructorSource == rpc,
            "argument-only Node/Resource/RPC registrations construct through explicit typed expressions");
        Reject<NotSupportedException>(() => direct.Duplicate());
        using var unsupportedScene = new PackedScene();
        Reject<NotSupportedException>(() => unsupportedScene.Pack(direct));
        Reject<NotSupportedException>(() => data.Duplicate());
        Check(!direct.IsDisposed && direct.GetScript() == required && direct.Initializations == 1, "absent reconstruction factories preserve the existing initialized owner");

        var failure = new ApplicationException("typed constructor failure");
        try { required.New(() => new RequiredScriptActor(13, failure)); throw new InvalidOperationException("Missing constructor failure"); }
        catch (ApplicationException actual) { Check(ReferenceEquals(actual, failure), "constructor exception identity propagates without reflective wrapping"); }
        var partial = RequiredScriptActor.Captured!;
        Check(!partial.IsDisposed && partial.Initializations == 1, "failed C# construction does not invoke user cleanup on a partial instance");
        partial.Dispose(); RequiredScriptActor.Captured = null;
        using var after = new RequiredScriptActor(17);
        Check(after.GetScript() is null, "failed constructor invocation closes its source context");
        var folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-init-" + Guid.NewGuid()); Directory.CreateDirectory(folder);
        try
        {
            var path = System.IO.Path.Combine(folder, "required.e2dres"); ResourceSaver.Save(required, path);
            using var restored = ResourceLoader.Load<Script>(path, ResourceLoader.CacheMode.Ignore);
            Reject<InvalidOperationException>(() => restored.New<RequiredScriptActor>());
            using var fresh = restored.New(() => new RequiredScriptActor(19));
            Check(fresh.ConstructorSource == restored && fresh.Speed == 19 && fresh.Initializations == 1, "stored argument-only source keeps explicit typed construction and no default factory");
        }
        finally { RequiredScriptActor.Captured = null; Directory.Delete(folder, true); }
        Console.WriteLine("Compiled initialization: typed/base constructors, required arguments, copy/file order and exact failures passed.");
    }
}
