using System.Runtime.ExceptionServices;

namespace Electron2D;

public sealed partial class ResourceLoader
{
    /// <summary>Reports the lifetime of an explicitly requested background resource load.</summary>
    public enum ThreadLoadStatus
    {
        /// <summary>No request exists, or its final result has already been collected.</summary>
        InvalidResource,
        /// <summary>Preparation or owner publication is still pending.</summary>
        InProgress,
        /// <summary>Preparation, publication or cooperative cancellation failed.</summary>
        Failed,
        /// <summary>The resource is published and ready for collection.</summary>
        Loaded
    }
    private sealed class ThreadRequest
    {
        internal readonly string Path;
        internal readonly Type Type;
        internal readonly CacheMode Mode;
        internal readonly bool Parallel;
        internal readonly CancellationToken Cancellation;
        internal readonly SceneTree? Tree;
        internal readonly Action Publish;
        internal ResourceLoadGraph? Graph;
        internal Resource? Result;
        internal ExceptionDispatchInfo? Failure;
        internal long TaskID;
        internal int Consumers = 1, Getters, PublisherThreadID;
        internal float Progress;
        internal bool Prepared, Retired, Publishing;
        internal ThreadLoadStatus Status = ThreadLoadStatus.InProgress;
        internal ThreadRequest(string path, Type type, CacheMode mode, bool parallel, CancellationToken cancellation, SceneTree? tree)
        {
            Path = path; Type = type; Mode = mode; Parallel = parallel; Cancellation = cancellation; Tree = tree; Publish = () => PublishRequest(this, false);
        }
    }
    private readonly object _threadGate = new();
    private readonly Dictionary<string, ThreadRequest> _threadRequests = new(StringComparer.Ordinal);

    /// <summary>Requests a typed resource graph on background workers, retaining its result until collected.</summary>
    /// <typeparam name="TResource">Requested resource type or compatible base type.</typeparam>
    /// <param name="path">Ordinary, res://, user:// or registered UID path.</param>
    /// <param name="useSubThreads">Allows independent external dependency preparation on multiple workers.</param>
    /// <param name="cacheMode">Root and external dependency cache policy.</param>
    /// <param name="cancellationToken">Cooperative cancellation between file stages and before publication.</param>
    /// <param name="publicationTree">Optional owner tree; defaults to the active engine scene tree.</param>
    /// <remarks>Every accepted request requires a matching get, including failures. Duplicate identical requests
    /// share preparation; conflicting options reject. Format hooks/factories must be thread-safe and prepare
    /// independent data. Existing cache objects and change callbacks are updated on the publication owner.</remarks>
    /// <exception cref="ArgumentException">Path or cache mode is invalid.</exception>
    /// <exception cref="InvalidOperationException">Options conflict with a pending request, or request capacity is exhausted.</exception>
    /// <exception cref="OperationCanceledException">Cancellation was requested before submission.</exception>
    /// <exception cref="PlatformNotSupportedException">The runtime cannot start background workers.</exception>
    public static void LoadThreadedRequest<TResource>(string path, bool useSubThreads = false, CacheMode cacheMode = CacheMode.Reuse, CancellationToken cancellationToken = default, SceneTree? publicationTree = null) where TResource : Resource
    {
        CheckFilePath(path); if ((uint)cacheMode > 4) throw new ArgumentOutOfRangeException(nameof(cacheMode)); cancellationToken.ThrowIfCancellationRequested();
        path = ResourceUID.EnsurePath(path); publicationTree ??= Engine.MainLoop as SceneTree;
        if (publicationTree is not null) { ObjectDisposedException.ThrowIf(publicationTree.IsDisposed, publicationTree); }
        lock (Runtime._threadGate)
        {
            if (Runtime._threadRequests.TryGetValue(path, out var existing))
            {
                if (existing.Type != typeof(TResource) || existing.Mode != cacheMode || existing.Parallel != useSubThreads || existing.Cancellation != cancellationToken || !ReferenceEquals(existing.Tree, publicationTree)) throw new InvalidOperationException("A pending resource request has different options.");
                existing.Consumers = checked(existing.Consumers + 1); return;
            }
            if (Runtime._threadRequests.Count >= 128) throw new InvalidOperationException("At most 128 resource requests may retain uncollected results.");
            var request = new ThreadRequest(path, typeof(TResource), cacheMode, useSubThreads, cancellationToken, publicationTree);
            Runtime._threadRequests.Add(path, request);
            try { request.TaskID = WorkerThreadPool.AddTask(() => PrepareRequest(request), description: "resource load"); }
            catch { Runtime._threadRequests.Remove(path); throw; }
        }
    }
    private static void PrepareRequest(ThreadRequest request)
    {
        ResourceLoadGraph? graph = null;
        try
        {
            graph = new(LoaderSnapshot(), request.Parallel, request.Cancellation);
            lock (Runtime._threadGate) request.Graph = graph;
            graph.Prepare(request.Path, request.Mode, request.Type);
        }
        catch (Exception error)
        {
            try { graph?.DisposeUnused(); } catch (Exception cleanup) { error = new AggregateException(error, cleanup); }
            lock (Runtime._threadGate) { request.Failure = ExceptionDispatchInfo.Capture(error); request.Graph = null; }
        }
        lock (Runtime._threadGate) { request.Prepared = true; Monitor.PulseAll(Runtime._threadGate); }
        if (request.Tree is { } tree)
        {
            try { tree.Defer(request.Publish); }
            catch (Exception error) { FailPublication(request, error); }
        }
    }
    private static void FailPublication(ThreadRequest request, Exception error)
    {
        try { request.Graph?.DisposeUnused(); } catch (Exception cleanup) { error = new AggregateException(error, cleanup); }
        lock (Runtime._threadGate) { request.Graph = null; request.Failure ??= ExceptionDispatchInfo.Capture(error); request.Status = ThreadLoadStatus.Failed; request.Progress = 1; Monitor.PulseAll(Runtime._threadGate); }
    }
    private static void PublishRequest(ThreadRequest request, bool blocking)
    {
        lock (Runtime._threadGate)
        {
            if (request.Status != ThreadLoadStatus.InProgress && request.Retired || request.Publishing || !request.Prepared) return;
            if (request.Failure is null && request.Tree is { IsDisposed: false } tree && !tree.IsOwnerThread)
            {
                if (blocking) throw new InvalidOperationException("An unpublished resource result requires its scene publication owner.");
                return;
            }
            if (!request.Retired && !WorkerThreadPool.IsTaskCompleted(request.TaskID))
            {
                if (request.Tree is { IsDisposed: false } pendingTree && !blocking)
                {
                    try { pendingTree.Defer(request.Publish); }
                    catch (Exception error) { request.Failure ??= ExceptionDispatchInfo.Capture(error); }
                }
                return;
            }
            request.Publishing = true;
            request.PublisherThreadID = Environment.CurrentManagedThreadId;
        }
        var acquired = false;
        try
        {
            if (!request.Retired)
            {
                lock (Runtime._threadGate) request.Retired = true;
                WorkerThreadPool.WaitForLoadTask(request.TaskID);
            }
            if (request.Tree?.IsDisposed == true) throw new ObjectDisposedException(nameof(SceneTree));
            if (blocking) { Monitor.Enter(LoadGate); acquired = true; }
            else acquired = Monitor.TryEnter(LoadGate);
            if (!acquired) { request.Tree?.Defer(request.Publish); return; }
            request.Failure?.Throw(); request.Cancellation.ThrowIfCancellationRequested();
            var result = request.Graph!.Commit();
            if (!request.Type.IsInstanceOfType(result) || result.IsDisposed) throw new InvalidDataException("Published resource does not match its requested type.");
            lock (Runtime._threadGate) { request.Result = result; request.Graph = null; request.Status = ThreadLoadStatus.Loaded; request.Progress = 1; }
        }
        catch (Exception error) { FailPublication(request, error); }
        finally
        {
            if (acquired) Monitor.Exit(LoadGate);
            lock (Runtime._threadGate) { request.Publishing = false; request.PublisherThreadID = 0; Monitor.PulseAll(Runtime._threadGate); }
        }
    }
    /// <summary>Returns a pending request's status without returning or releasing its result.</summary>
    /// <param name="path">The request's exact path or registered UID path.</param><returns>InvalidResource when no request remains.</returns>
    /// <remarks>The publication owner may perform ready cache publication; other threads only inspect status.</remarks>
    public static ThreadLoadStatus LoadThreadedGetStatus(string path) => LoadThreadedGetStatus(path, out _);
    /// <summary>Returns request status and monotonic progress from zero through one.</summary>
    /// <param name="path">Request path.</param><param name="progress">Completed-stage ratio; zero for an absent request, one for terminal status.</param><returns>The request lifetime status.</returns>
    public static ThreadLoadStatus LoadThreadedGetStatus(string path, out float progress)
    {
        CheckPath(path); path = ResourceUID.EnsurePath(path); ThreadRequest? request;
        lock (Runtime._threadGate) Runtime._threadRequests.TryGetValue(path, out request);
        if (request is null) { progress = 0; return ThreadLoadStatus.InvalidResource; }
        PublishRequest(request, false);
        lock (Runtime._threadGate)
        {
            request.Progress = Math.Max(request.Progress, request.Graph?.Progress ?? 0);
            progress = request.Progress; return request.Status;
        }
    }
    /// <summary>Waits for a requested resource, releases one matching request and returns its published result.</summary>
    /// <param name="path">Pending request path.</param><returns>A caller-owned live resource, possibly the existing cache identity.</returns>
    /// <remarks>Throws the captured load/cancellation failure after cleanup. An unpublished scene result requires its owner.</remarks>
    /// <exception cref="ArgumentException">No request remains.</exception>
    /// <exception cref="InvalidOperationException">The current load or scene-owner requirements make waiting unsafe.</exception>
    public static Resource LoadThreadedGet(string path) => LoadThreadedGet<Resource>(path);
    /// <summary>Waits for and collects one matching result with a concrete type check.</summary>
    /// <typeparam name="TResource">Required assignable result type.</typeparam><param name="path">Pending request path.</param><returns>The caller-owned resource.</returns>
    /// <exception cref="ArgumentException">No request remains or the requested type is incompatible.</exception>
    /// <exception cref="InvalidOperationException">A pending load callback or scene owner makes waiting unsafe.</exception>
    public static TResource LoadThreadedGet<TResource>(string path) where TResource : Resource
    {
        CheckPath(path); path = ResourceUID.EnsurePath(path); ThreadRequest request;
        lock (Runtime._threadGate)
        {
            if (!Runtime._threadRequests.TryGetValue(path, out request!)) throw new ArgumentException("No matching threaded resource request remains.", nameof(path));
            if (!typeof(TResource).IsAssignableFrom(request.Type)) throw new ArgumentException("The requested result type is incompatible.", nameof(TResource));
            if (request.Publishing && request.PublisherThreadID == Environment.CurrentManagedThreadId) throw new InvalidOperationException("A publication callback cannot collect its active request.");
            if (ResourceLoadGraph.Current is not null && request.Status == ThreadLoadStatus.InProgress) throw new InvalidOperationException("A load callback cannot consume an unpublished threaded request.");
            if (request.Tree is { } tree && !tree.IsOwnerThread && request.Status == ThreadLoadStatus.InProgress) throw new InvalidOperationException("An unpublished result requires its scene publication owner.");
            if (request.Getters >= request.Consumers) throw new ArgumentException("Every matching request already has a collector.", nameof(path));
            request.Getters++;
        }
        var consumed = false;
        try
        {
            try { if (!request.Retired) WorkerThreadPool.WaitForLoadTask(request.TaskID, retire: false); }
            catch (ArgumentException) when (request.Retired) { }
            PublishRequest(request, true);
            lock (Runtime._threadGate) while (request.Publishing) Monitor.Wait(Runtime._threadGate);
            consumed = true; request.Failure?.Throw();
            var result = request.Result ?? throw new InvalidDataException("The load did not publish a resource.");
            ObjectDisposedException.ThrowIf(result.IsDisposed, result); return (TResource)result;
        }
        finally
        {
            lock (Runtime._threadGate)
            {
                request.Getters--;
                if (consumed && --request.Consumers == 0) Runtime._threadRequests.Remove(path);
            }
        }
    }
}
