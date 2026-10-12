using System.Runtime.ExceptionServices;

namespace Electron2D;

public sealed partial class NoiseTexture
{
    private readonly Action _updateAction, _publishAction;
    private bool _firstGeneration = true, _updateQueued;
    private long _failedVersion = -1;
    private SceneTree? _queuedTree;
    private GenerationWork? _pending;

    internal sealed class GenerationWork(NoiseTexture owner, SceneTree? tree,
        (int Width, int Height, bool MIPMaps, bool Normalize, bool Seamless, bool Invert, bool Normal, float Skirt, float Strength, Noise? Noise, Gradient? Ramp, long Version) input, IDisposable? lease, FastNoiseLite? ownedNoise)
    {
        internal readonly SceneTree? Tree = tree;
        internal readonly (int Width, int Height, bool MIPMaps, bool Normalize, bool Seamless, bool Invert, bool Normal, float Skirt, float Strength, Noise? Noise, Gradient? Ramp, long Version) Input = input;
        private readonly IDisposable? _lease = lease;
        internal void Release() { try { Input.Ramp?.Dispose(); } finally { try { ownedNoise?.Dispose(); } finally { _lease?.Dispose(); } } }
        internal long TaskID;
        internal TexturePixels? Pixels;
        internal ExceptionDispatchInfo? Failure;
        internal void Compute()
        {
            try { Pixels = owner.GeneratePixels(this); }
            catch (Exception error) { Failure = ExceptionDispatchInfo.Capture(error); }
        }
        internal void Run()
        {
            Compute();
            if (Tree is not null) try { Tree.Defer(owner._publishAction); } catch (ObjectDisposedException) { }
        }
    }

    private GenerationWork CaptureGeneration(SceneTree? tree)
    {
        var noise = _noise is FastNoiseLite builtIn ? (FastNoiseLite)builtIn.Duplicate() : null; Gradient? ramp = null;
        try { ramp = _colorRamp is null ? null : (Gradient)_colorRamp.Duplicate(); return new(this, tree, (_width, _height, _generateMipmaps, _normalize, _seamless, _invert, _asNormalMap, _seamlessBlendSkirt, _bumpStrength, noise ?? _noise, ramp, _version), RetainFileResources(), noise); }
        catch { try { ramp?.Dispose(); } finally { noise?.Dispose(); } throw; }
    }
    private void NotifyInvalidation() { if (ResourceLoadGraph.Current is null && !QueueSceneRefresh()) EmitChanged(); }
    private bool QueueSceneRefresh()
    {
        if (ResourceLoadGraph.Current is not null) return false;
        if (Engine.MainLoop is not SceneTree { IsDisposed: false } tree) return false;
        lock (_gate)
        {
            ThrowIfDisposed();
            if (!_dirty || _failedVersion == _version) return true;
            if (_pending is { } work && (ReferenceEquals(work.Tree, tree) || !WorkerThreadPool.IsTaskCompleted(work.TaskID))) return true;
            if (_updateQueued && ReferenceEquals(_queuedTree, tree)) return true;
            _updateQueued = true; _queuedTree = tree;
            try { tree.Defer(_updateAction); } catch { _updateQueued = false; _queuedTree = null; throw; }
            return true;
        }
    }
    private void UpdateTexture()
    {
        GenerationWork? work;
        lock (_gate)
        {
            if (IsDisposed || !_updateQueued || _queuedTree is not { IsDisposed: false } tree || !tree.IsOwnerThread) return;
            _updateQueued = false; _queuedTree = null;
            if (_pending is not null) { work = null; }
            else
            {
                if (!_dirty) return;
                work = CaptureGeneration(tree);
                if (!_firstGeneration && !OperatingSystem.IsBrowser())
                {
                    _pending = work;
                    try { work.TaskID = WorkerThreadPool.AddTask(work.Run, description: "noise texture generation"); }
                    catch { _pending = null; work.Release(); throw; }
                    return;
                }
                _firstGeneration = false;
            }
        }
        if (work is null) PublishCompletion();
        else { work.Compute(); Publish(work); }
    }
    private void PublishCompletion()
    {
        GenerationWork? work;
        lock (_gate)
        {
            if (IsDisposed || _pending is not { } pending) return;
            var tree = Engine.MainLoop as SceneTree ?? pending.Tree;
            if (tree is not { IsDisposed: false } || !tree.IsOwnerThread) return;
            if (!WorkerThreadPool.IsTaskCompleted(pending.TaskID)) { tree.Defer(_publishAction); return; }
            work = pending; _pending = null;
        }
        try { WorkerThreadPool.WaitForTextureTask(work.TaskID); }
        catch { work.Release(); throw; }
        Publish(work);
    }
    private void Publish(GenerationWork work)
    {
        var changed = false;
        try
        {
            lock (_gate)
            {
                if (IsDisposed) return;
                if (work.Input.Version == _version)
                {
                    if (work.Failure is not null) { _failedVersion = _version; work.Failure.Throw(); }
                    _pixels = work.Pixels; if (_pixels is not null) _renderPixels = _pixels;
                    _dirty = false; changed = true;
                }
            }
            if (!changed) QueueSceneRefresh();
            else EmitChanged();
        }
        finally { work.Release(); }
    }
    private void DrainGeneration()
    {
        GenerationWork? work;
        lock (_gate) { work = _pending; _pending = null; _updateQueued = false; _queuedTree = null; }
        if (work is null) return;
        try { WorkerThreadPool.WaitForTextureTask(work.TaskID); }
        finally { work.Release(); }
    }
    private void RefreshPixels()
    {
        if (s_baking?.Contains(this) == true) throw new InvalidOperationException("Noise texture generation is reentrant.");
        if (QueueSceneRefresh()) return;
        lock (_gate) { ThrowIfDisposed(); if (!_dirty && _pending is null) return; }
        DrainGeneration();
        // Cold host reads bound retries when custom sources keep changing during sampling.
        for (var attempt = 0; attempt < 8; attempt++)
        {
            GenerationWork work;
            lock (_gate) { ThrowIfDisposed(); if (!_dirty) return; work = CaptureGeneration(null); _firstGeneration = false; }
            try
            {
                work.Compute();
                lock (_gate)
                {
                    ThrowIfDisposed(); if (work.Input.Version != _version) continue;
                    work.Failure?.Throw(); _pixels = work.Pixels; if (_pixels is not null) _renderPixels = _pixels; _dirty = false; return;
                }
            }
            finally { work.Release(); }
        }
        throw new InvalidOperationException("Noise texture sources changed throughout generation.");
    }
}
