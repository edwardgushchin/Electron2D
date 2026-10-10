namespace Electron2D.Examples.AsyncGallery;

internal sealed class Gallery(string path) : Entity
{
    private readonly CancellationTokenSource _cancellation = new();
    private bool _pending;
    private float _progress, _pulse;
    internal bool Loaded { get; private set; }
    internal int Frames { get; private set; }
    protected override void OnReady()
    {
        ProcessEnabled = true;
        ResourceLoader.LoadThreadedRequest<PackedScene>(path, useSubThreads: true, cancellationToken: _cancellation.Token, publicationTree: Tree); _pending = true;
    }
    protected override void OnProcess(double delta)
    {
        Frames++; _pulse += (float)delta;
        if (_pending)
        {
            var status = ResourceLoader.LoadThreadedGetStatus(path, out _progress);
            if (status is ResourceLoader.ThreadLoadStatus.Loaded or ResourceLoader.ThreadLoadStatus.Failed)
            {
                _pending = false;
                using var scene = ResourceLoader.LoadThreadedGet<PackedScene>(path);
                AddChild(scene.Instantiate()); Loaded = true;
            }
        }
        QueueRedraw();
    }
    protected override void OnDraw()
    {
        DrawRect(new(4, 48, 56, 5), new(.1f, .1f, .1f)); DrawRect(new(4, 48, 56 * _progress, 5), Colors.Green);
        DrawRect(new(4 + (int)(_pulse * 24) % 56, 58, 2, 2), Colors.White);
    }
    protected override void Dispose(bool disposing)
    {
        try
        {
            if (_pending)
            {
                _cancellation.Cancel(); _pending = false;
                try { ResourceLoader.LoadThreadedGet(path).Dispose(); } catch (OperationCanceledException) { } catch (ObjectDisposedException) { }
            }
        }
        finally { _cancellation.Dispose(); base.Dispose(disposing); }
    }
}
