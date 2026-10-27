using Electron2D;

internal static partial class ScriptTests
{
    private static void SourceChanges()
    {
        var folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-source-change-" + Guid.NewGuid()); Directory.CreateDirectory(folder);
        try
        {
            using var source = ResourceLoader.Load<Script>(ScriptPaintEffect.SourcePath);
            using var template = source.New<ScriptPaintEffect>();
            Check(template.SourceProperties == 0 && template.SourceChanges == 0, "direct C# construction does not synthesize a post-constructor assignment signal");
            var path = System.IO.Path.Combine(folder, "effect.e2dres"); ResourceSaver.Save(template, path, SaverFlags.BundleResources);
            using var target = ResourceLoader.Load<ScriptPaintEffect>(path);
            var identity = target.InstanceID; var before = target.GetScript(); var trace = new List<string>();
            target.PropertyListChanged += self => { Check(self.GetScript() is { IsDisposed: false } && self.InstanceID == identity, "property observers see the committed live source and stable owner"); trace.Add("properties"); };
            target.ScriptChanged += self => { Check(self.GetScript() is { IsDisposed: false }, "script observers see acquired source ownership"); trace.Add("script"); };
            target.Changed += _ => trace.Add("resource");
            using var replaced = ResourceLoader.Load<ScriptPaintEffect>(path, ResourceLoader.CacheMode.ReplaceDeep);
            Check(replaced == target && target.GetScript() != before && trace.SequenceEqual(["properties", "script", "resource"]), "file replacement notifies source identity after ownership commitment and before final resource change");
            trace.Clear(); using var reused = ResourceLoader.Load<ScriptPaintEffect>(path);
            Check(reused == target && trace.Count == 0, "cache reuse does not change or notify source identity");
            before = target.GetScript(); target.CopyFromResource(template);
            Check(target.GetScript() == before && trace.SequenceEqual(["resource"]), "ordinary public data copying retains its source without a script notification");
            trace.Clear(); target.SetBlockSignals(true);
            using var blocked = ResourceLoader.Load<ScriptPaintEffect>(path, ResourceLoader.CacheMode.ReplaceDeep);
            Check(blocked == target && target.GetScript() != before && trace.Count == 0, "blocking suppresses notification but commits the new source");
            target.SetBlockSignals(false);
            var operation = new ApplicationException("source observer"); var final = new ApplicationException("resource observer");
            Action<ElectronObject> failScript = _ => throw operation; Action<Resource> failResource = _ => throw final;
            target.ScriptChanged += failScript; target.Changed += failResource; before = target.GetScript();
            try { ResourceLoader.Load<ScriptPaintEffect>(path, ResourceLoader.CacheMode.ReplaceDeep); throw new InvalidOperationException("Missing observer failure"); }
            catch (AggregateException error) { Check(error.InnerExceptions.Count == 2 && error.InnerExceptions[0] == operation && error.InnerExceptions[1] == final, "script and final resource handler failures retain order and identity"); }
            finally { target.ScriptChanged -= failScript; target.Changed -= failResource; }
            Check(!target.IsDisposed && target.GetScript() != before && target.GetScript() is { IsDisposed: false }, "observer failures preserve committed source ownership");
            trace.Clear(); var current = target.GetScript()!; current.SourceCode += "\n// edited asset";
            Check(trace.Count == 0, "source text edits do not replace the owner association"); current.Dispose();
            Check(trace.Count == 0 && target.GetScript() == current, "explicit source disposal retains borrowed identity without a script signal");

            var retirePath = System.IO.Path.Combine(folder, "retire.e2dres"); ResourceSaver.Save(template, retirePath, SaverFlags.BundleResources);
            using var retiring = ResourceLoader.Load<ScriptPaintEffect>(retirePath); var scripts = 0;
            retiring.PropertyListChanged += self => self.Dispose(); retiring.ScriptChanged += _ => scripts++;
            Reject<ObjectDisposedException>(() => ResourceLoader.Load<ScriptPaintEffect>(retirePath, ResourceLoader.CacheMode.ReplaceDeep));
            Check(retiring.IsDisposed && scripts == 0 && !ResourceLoader.HasCached(retirePath), "retirement during the first notification skips later source delivery and cannot return a retired cached result");

            var remapPath = System.IO.Path.Combine(folder, "remap.e2dres"); ResourceSaver.Save(template, remapPath);
            ResourceLoader.LoadThreadedRequest<ScriptPaintEffect>(remapPath, cacheMode: ResourceLoader.CacheMode.ReplaceDeep);
            using var remapped = ResourceLoader.LoadThreadedGet<ScriptPaintEffect>(remapPath);
            Check(remapped.GetScript() == source && remapped.SourceProperties == 1 && remapped.SourceChanges == 1, "prepared graph publication reports its remapped final source through the ordinary event pipeline");
            var retireRemapPath = System.IO.Path.Combine(folder, "retire-remap.e2dres"); ResourceSaver.Save(template, retireRemapPath);
            ScriptPaintEffect.RetireOnSourceProperties = true;
            try
            {
                ResourceLoader.LoadThreadedRequest<ScriptPaintEffect>(retireRemapPath, cacheMode: ResourceLoader.CacheMode.ReplaceDeep);
                Reject<ObjectDisposedException>(() => ResourceLoader.LoadThreadedGet<ScriptPaintEffect>(retireRemapPath));
                Check(!ResourceLoader.HasCached(retireRemapPath) && !source.IsDisposed, "prepared notification retirement rejects publication and preserves the borrowed cached source");
            }
            finally { ScriptPaintEffect.RetireOnSourceProperties = false; }
            Console.WriteLine("Source publication notifications: equality/order/blocking/errors/retirement and threaded remapping passed.");
        }
        finally { Directory.Delete(folder, true); }
    }
}
