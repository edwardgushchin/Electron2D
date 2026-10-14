using System.Runtime.CompilerServices;
using Electron2D;

public static partial class BackendChecks
{
    private sealed class LifecycleResource : Resource
    {
        internal readonly List<string> Paths = [];
        internal readonly List<string> RawPaths = [];
        internal int Transient = 9;
        internal int Resets;
        internal bool FailReset, FailRaw, FailPath;
        internal Action? ResetHook;
        internal Action<string>? PathHook;
        protected override void OnResetState()
        {
            Resets++; Transient = 0; ResetHook?.Invoke();
            if (FailReset) throw new ArithmeticException("reset");
        }
        protected override void OnPathCacheSet(string path)
        {
            RawPaths.Add(path);
            if (FailRaw) throw new ArithmeticException("raw path");
        }
        protected override void OnResourcePathChanged(string path)
        {
            Paths.Add(path); PathHook?.Invoke(path);
            if (FailPath) throw new ArithmeticException("path");
        }
    }

    private static void ResourceLifecycle(Resource[] resources)
    {
        var prefix = "memory://resource-lifecycle/" + Guid.NewGuid().ToString("N");
        using var first = new LifecycleResource { ResourceName = "stored", ResourceLocalToScene = true, ResourceSceneUniqueID = "stored_id", CanTranslateMessages = false, TranslationDomain = "stored" };
        using var second = new LifecycleResource(); var path = prefix + "/owned";
        var changes = 0; first.Changed += _ => changes++;
        first.ResourcePath = path;
        Check(ResourceLoader.GetCachedRef<Resource>(path) == first && ResourceLoader.HasCached(path), "Registered paths publish exact weak cache ownership");
        first.ResourcePath = path;
        Check(first.Paths.Count == 1 && changes == 0, "Equal registered path assignment is silent and identity changes do not emit content change");
        second.ResourcePath = prefix + "/previous";
        Reject<InvalidOperationException>(() => second.ResourcePath = path);
        Check(first.ResourcePath == path && second.ResourcePath.EndsWith("/previous", StringComparison.Ordinal), "Conflicting path assignment preserves both owners");
        second.PathHook = value => Check(ResourceLoader.GetCachedRef<Resource>(value) == second && first.ResourcePath.Length == 0,
            "Takeover callbacks observe the committed new owner and displaced empty path");
        second.TakeOverPath(path); second.PathHook = null;
        Check(first.Paths.SequenceEqual(new[] { path, "" }) && second.Paths.Last() == path && ResourceLoader.GetCachedRef<Resource>(path) == second,
            "Takeover commits both paths and delivers displaced/new path hooks");
        first.SetPathCache(path); first.SetPathCache(path);
        Check(first.ResourcePath == path && first.RawPaths.Count == 2 && ResourceLoader.GetCachedRef<Resource>(path) == second,
            "Equal raw-path assignments invoke their hooks without taking another owner's cache entry");
        second.SetPathCache(path);
        Check(!ResourceLoader.HasCached(path), "Raw assignment unregisters the caller's prior path without registering a replacement");
        first.ResourcePath = path;
        Check(ResourceLoader.GetCachedRef<Resource>(path) == first, "Promoting an equal unregistered raw path claims ownership");
        first.ResourcePath = prefix + "/Case"; second.ResourcePath = prefix + "/case";
        Check(ResourceLoader.GetCachedRef<Resource>(prefix + "/Case") == first && ResourceLoader.GetCachedRef<Resource>(prefix + "/case") == second,
            "Cache paths are ordinal and case-sensitive");
        foreach (var value in new[] { "", "embedded.e2dres::id", "directory::leaf", "local://asset", "LOCAL://asset", "external.e2dres" })
        { first.SetPathCache(value); Check(first.IsBuiltIn == (value.Length == 0 || value.Contains("::", StringComparison.Ordinal) || value.StartsWith("local://", StringComparison.Ordinal)), "Built-in classification follows visible path storage"); }
        first.TakeOverPath(""); Check(first.ResourcePath.Length == 0 && !ResourceLoader.HasCached(prefix + "/Case"), "Empty takeover clears path ownership");
        var stored = (first.ResourceName, first.ResourceLocalToScene, first.ResourceSceneUniqueID, first.CanTranslateMessages, first.TranslationDomain);
        first.ResetState();
        Check(first.Resets == 1 && first.Transient == 0 && changes == 0 && stored == (first.ResourceName, first.ResourceLocalToScene, first.ResourceSceneUniqueID, first.CanTranslateMessages, first.TranslationDomain),
            "Standalone reset invokes the custom transient hook once without changing stored state or manufacturing Changed");
        first.FailReset = true; Reject<ArithmeticException>(first.ResetState); first.FailReset = false;
        Check(first.Resets == 2 && changes == 0, "A failing reset propagates its error without automatic Changed");
        first.ResetHook = first.EmitChanged; first.ResetState(); first.ResetHook = null;
        Check(changes == 1, "A custom reset may explicitly publish its own content change");
        first.ResourceName = "stored"; first.ResourceName = "stored"; first.EmitChanged();
        Check(changes == 4, "Name assignment publishes after commit even for equal values; explicit Changed remains synchronous");
        Reject<ArgumentNullException>(() => first.ResourceName = null!);
        Reject<ArgumentNullException>(() => first.ResourcePath = null!);
        Reject<ArgumentNullException>(() => first.SetPathCache(null!));
        Reject<ArgumentNullException>(() => first.TakeOverPath(null!));
        Check(changes == 4 && first.ResourceName == "stored", "Invalid null assignments have no state or event effects");
        HookFailures(prefix);
        EventDelivery();
        using (var a = new Resource())
        using (var b = new Resource())
        using (var start = new ManualResetEventSlim())
        {
            var racedPath = prefix + "/race"; Exception? firstError = null, secondError = null;
            var t1 = new Thread(() => { start.Wait(); try { a.ResourcePath = racedPath; } catch (Exception e) { firstError = e; } });
            var t2 = new Thread(() => { start.Wait(); try { b.ResourcePath = racedPath; } catch (Exception e) { secondError = e; } });
            t1.Start(); t2.Start(); start.Set(); t1.Join(); t2.Join();
            Check((firstError is null && secondError is InvalidOperationException || secondError is null && firstError is InvalidOperationException) &&
                ResourceLoader.GetCachedRef<Resource>(racedPath) == (firstError is null ? a : b),
                "Concurrent path claims publish exactly one owner without losing either resource's lifetime");
        }
        var weakPath = prefix + "/weak"; var weak = MakeWeakPath(weakPath); Collect();
        Check(!weak.TryGetTarget(out _) && !ResourceLoader.HasCached(weakPath), "The path registry does not keep an undisposed managed resource alive");
        first.ResourcePath = prefix + "/disposed"; first.Dispose();
        Check(!ResourceLoader.HasCached(prefix + "/disposed"), "Disposal unregisters only owned paths");
        Reject<ObjectDisposedException>(first.ResetState); Reject<ObjectDisposedException>(first.EmitChanged);
        Reject<ObjectDisposedException>(() => first.SetPathCache("")); Reject<ObjectDisposedException>(() => first.TakeOverPath(""));
        Reject<ObjectDisposedException>(() => _ = first.IsBuiltIn); Reject<ObjectDisposedException>(() => first.ResourceName = "late");
        foreach (var resource in resources)
        {
            var oldName = resource.ResourceName; var oldPath = resource.ResourcePath; var oldID = resource.ResourceSceneUniqueID;
            var count = 0; void Changed(Resource sender) { Check(sender == resource && sender.ResourceName == "physical stored", "Physical resource content events observe committed state"); count++; }
            resource.ResourceName = "physical stored"; resource.Changed += Changed;
            try
            {
                resource.ResetState(); resource.SetPathCache(prefix + "/physical::" + resource.InstanceID); resource.TakeOverPath(prefix + "/physical-" + resource.InstanceID);
                Check(count == 0, "Physical family reset/path metadata does not manufacture content events");
                resource.ResourceName = "physical stored"; resource.EmitChanged();
                Check(count == 2 && resource.ResourceSceneUniqueID == oldID && ResourceLoader.GetCachedRef<Resource>(resource.ResourcePath) == resource,
                    "Every physical/base family exposes reset/path/cache/content behavior through the public contract");
            }
            finally { resource.Changed -= Changed; resource.SetPathCache(oldPath); resource.ResourceName = oldName; }
        }
        Console.WriteLine($"Resource lifecycle: {resources.Length} physical/base families, reset/hooks, weak ordinal cache, raw/promotion/takeover/errors and synchronous/reentrant Changed passed.");
    }

    private static void HookFailures(string prefix)
    {
        using var raw = new LifecycleResource { FailRaw = true, FailPath = true };
        Reject<AggregateException>(() => raw.SetPathCache(prefix + "/raw-error"));
        Check(raw.ResourcePath.EndsWith("/raw-error", StringComparison.Ordinal) && raw.RawPaths.Count == 1 && raw.Paths.Count == 1 && !ResourceLoader.HasCached(raw.ResourcePath),
            "Both raw/path hooks run after committed unregistered metadata even when the first hook throws");
        using var old = new LifecycleResource(); using var next = new LifecycleResource(); var path = prefix + "/takeover-error";
        old.ResourcePath = path; old.FailPath = next.FailPath = true;
        Reject<AggregateException>(() => next.TakeOverPath(path));
        Check(old.ResourcePath.Length == 0 && next.ResourcePath == path && ResourceLoader.GetCachedRef<Resource>(path) == next && old.Paths.Last() == "" && next.Paths.Last() == path,
            "Both takeover hooks are attempted and their errors aggregate without rolling back cache ownership");
        using var reentrant = new LifecycleResource(); var first = prefix + "/outer"; var last = prefix + "/inner";
        reentrant.PathHook = value => { if (value == first) reentrant.ResourcePath = last; };
        reentrant.ResourcePath = first;
        Check(reentrant.ResourcePath == last && ResourceLoader.GetCachedRef<Resource>(last) == reentrant && !ResourceLoader.HasCached(first),
            "Reentrant path callbacks keep the final committed registry consistent");
        reentrant.PathHook = null;
    }

    private static void EventDelivery()
    {
        using var source = new Resource(); var order = new List<int>(); var nested = false;
        Action<Resource> removed = _ => order.Add(2);
        source.Changed += sender => { order.Add(1); if (!nested) { nested = true; sender.Changed -= removed; sender.EmitChanged(); } };
        source.Changed += removed; source.EmitChanged();
        Check(order.SequenceEqual(new[] { 1, 1, 2 }), "Reentrant Changed uses the current nested subscription list and the outer captured list");
        using var throwing = new Resource(); var later = 0; throwing.Changed += _ => throw new ArithmeticException(); throwing.Changed += _ => later++;
        Reject<ArithmeticException>(() => throwing.ResourceName = "committed");
        Check(throwing.ResourceName == "committed" && later == 0, "Synchronous delegate failures propagate after state commit and stop later handlers");
        using var worker = new Resource(); var observed = 0; worker.Changed += _ => observed = Environment.CurrentManagedThreadId;
        var thread = new Thread(worker.EmitChanged); thread.Start(); thread.Join();
        Check(observed == thread.ManagedThreadId, "Ordinary synchronous Changed executes on its publishing thread");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<Resource> MakeWeakPath(string path) => new(new Resource { ResourcePath = path });
}
