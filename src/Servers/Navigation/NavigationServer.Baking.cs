using System.Runtime.ExceptionServices;

namespace Electron2D;

public sealed partial class NavigationServer
{
    private sealed class BakeWork(NavigationPolygon target, NavigationBakeSettings settings, Vector2[][] outlines, NavigationSourceGeometrySnapshot source, Action? callback, SceneTree? tree)
    {
        internal readonly NavigationPolygon Target = target;
        internal readonly NavigationBakeSettings Settings = settings;
        internal readonly Vector2[][] Outlines = outlines;
        internal readonly NavigationSourceGeometrySnapshot Source = source;
        internal readonly Action? Callback = callback;
        internal readonly SceneTree? Tree = tree;
        internal Action Publish = null!;
        internal NavigationPolygonData? Result;
        internal ExceptionDispatchInfo? Failure;
        internal long TaskID;
        internal bool Prepared, Publishing, Synchronous;
    }
    private readonly Dictionary<NavigationPolygon, BakeWork> _bakes = new(ReferenceEqualityComparer.Instance);
    private readonly BakeWork?[] _bakePump = new BakeWork?[128];
    private bool _pumpingBakes;
    private BakeWork BeginBake(NavigationPolygon target, NavigationMeshSourceGeometryData source, Action? callback, SceneTree? tree)
    {
        ArgumentNullException.ThrowIfNull(target); ArgumentNullException.ThrowIfNull(source);
        var input = target.CaptureBake(); var data = source.Snapshot();
        if (tree is not null) ObjectDisposedException.ThrowIf(tree.IsDisposed, tree);
        lock (_gate)
        {
            if (_bakes.ContainsKey(target)) throw new InvalidOperationException("This navigation polygon already has an active bake.");
            if (_bakes.Count >= 128) throw new InvalidOperationException("Navigation bake capacity is exhausted.");
            var work = new BakeWork(target, input.Settings, input.Outlines, data, callback, tree); work.Publish = () => PublishBake(work); _bakes.Add(target, work); return work;
        }
    }
    /// <summary>Bakes one coherent source snapshot and publishes complete planar geometry on the caller.</summary>
    /// <param name="navigationPolygon">Live target with outlines/settings.</param><param name="sourceGeometryData">Live copied source contours.</param><param name="callback">Optional completion callback after successful publication.</param>
    /// <remarks>Preparation failures preserve prior geometry. A completion/Changed exception may follow committed data.</remarks>
    /// <exception cref="InvalidOperationException">A bake is already active for this target.</exception>
    public static void BakeFromSourceGeometryData(NavigationPolygon navigationPolygon, NavigationMeshSourceGeometryData sourceGeometryData, Action? callback = null)
    {
        var work = Shared.BeginBake(navigationPolygon, sourceGeometryData, callback, null); work.Synchronous = true;
        try { var result = NavigationBaker.Bake(work.Settings, work.Outlines, work.Source); lock (Shared._gate) Shared._bakes.Remove(navigationPolygon); if (result is not null) navigationPolygon.PublishBake(result); callback?.Invoke(); }
        finally { lock (Shared._gate) if (Shared._bakes.TryGetValue(navigationPolygon, out var current) && ReferenceEquals(current, work)) Shared._bakes.Remove(navigationPolygon); }
    }
    /// <summary>Prepares navigation geometry on a worker and publishes it on its scene or standalone completion owner.</summary>
    /// <param name="navigationPolygon">Live target.</param><param name="sourceGeometryData">Source snapshot retained independently of later edits/disposal.</param><param name="callback">Owner completion callback.</param><param name="publicationTree">Explicit owner; defaults to the active engine scene.</param>
    /// <remarks>SceneTree.Defer publishes automatically. Standalone callers pump completion through IsBakingNavigationPolygon or Synchronize. Worker failures surface on that owner, and every task is retired.</remarks>
    public static void BakeFromSourceGeometryDataAsync(NavigationPolygon navigationPolygon, NavigationMeshSourceGeometryData sourceGeometryData, Action? callback = null, SceneTree? publicationTree = null)
    {
        publicationTree ??= Engine.MainLoop as SceneTree; var work = Shared.BeginBake(navigationPolygon, sourceGeometryData, callback, publicationTree);
        lock (Shared._gate)
        {
            try { work.TaskID = WorkerThreadPool.AddTask(() => PrepareBake(work), highPriority: true, description: "navigation bake"); }
            catch { Shared._bakes.Remove(navigationPolygon); throw; }
        }
    }
    private static void PrepareBake(BakeWork work)
    {
        NavigationPolygonData? result = null; ExceptionDispatchInfo? failure = null;
        try { result = NavigationBaker.Bake(work.Settings, work.Outlines, work.Source); }
        catch (Exception error) { failure = ExceptionDispatchInfo.Capture(error); }
        lock (Shared._gate) { work.Result = result; work.Failure = failure; work.Prepared = true; }
        if (work.Tree is { } tree) try { tree.Defer(work.Publish); } catch (Exception error) { lock (Shared._gate) work.Failure ??= ExceptionDispatchInfo.Capture(error); }
    }
    private static void PublishBake(BakeWork work)
    {
        lock (Shared._gate)
        {
            if (work.Synchronous || !work.Prepared || work.Publishing || !Shared._bakes.TryGetValue(work.Target, out var current) || !ReferenceEquals(current, work)) return;
            if (work.Tree is { IsDisposed: false } tree && !tree.IsOwnerThread) return;
            if (!WorkerThreadPool.IsTaskCompleted(work.TaskID))
            { if (work.Tree is { IsDisposed: false } pending) try { pending.Defer(work.Publish); } catch (Exception error) { work.Failure ??= ExceptionDispatchInfo.Capture(error); } return; }
            work.Publishing = true;
        }
        try
        {
            WorkerThreadPool.WaitForLoadTask(work.TaskID);
            lock (Shared._gate) Shared._bakes.Remove(work.Target);
            if (work.Tree?.IsDisposed == true) throw new ObjectDisposedException(nameof(SceneTree));
            ObjectDisposedException.ThrowIf(work.Target.IsDisposed, work.Target);
            work.Failure?.Throw(); if (work.Result is { } result) work.Target.PublishBake(result); work.Callback?.Invoke();
        }
        finally
        { lock (Shared._gate) { if (Shared._bakes.TryGetValue(work.Target, out var current) && ReferenceEquals(current, work)) Shared._bakes.Remove(work.Target); work.Publishing = false; } }
    }
    /// <summary>Reports pending preparation/publication, pumping ready results when called by their publication owner.</summary>
    /// <param name="navigationPolygon">Target identity.</param><returns>False when no bake remains.</returns>
    public static bool IsBakingNavigationPolygon(NavigationPolygon navigationPolygon)
    {
        ArgumentNullException.ThrowIfNull(navigationPolygon); BakeWork? work;
        lock (Shared._gate) Shared._bakes.TryGetValue(navigationPolygon, out work);
        if (work is null) return false; PublishBake(work);
        lock (Shared._gate) return Shared._bakes.ContainsKey(navigationPolygon);
    }
    private void PumpBakes()
    {
        var count = 0;
        lock (_gate) { if (_bakes.Count == 0 || _pumpingBakes) return; _pumpingBakes = true; foreach (var work in _bakes.Values) _bakePump[count++] = work; }
        List<Exception>? errors = null;
        try { for (var i = 0; i < count; i++) try { PublishBake(_bakePump[i]!); } catch (Exception error) { (errors ??= []).Add(error); } }
        finally { lock (_gate) { Array.Clear(_bakePump, 0, count); _pumpingBakes = false; } }
        if (errors is not null) throw new AggregateException(errors);
    }
}
