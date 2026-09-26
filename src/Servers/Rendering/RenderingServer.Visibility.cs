namespace Electron2D;

public sealed partial class RenderingServer
{
    private readonly List<(VisibleOnScreenNotifier Node, ulong Generation)> _screenChanges = [];

    private void AppendScreenCanvas(CanvasItem node, Transform transform, Rect2i? clip, Vector2i pixels)
    {
        var first = _vertices.Count;
        node.AppendCanvas(_vertices, _batches, transform, CanvasTime, clip);
        if (node is not VisibleOnScreenNotifier notifier || notifier.ScreenCandidate || notifier.InheritedModulate.A < 0.007f) return;
        var local = new Rect2().Merge(notifier.Rect.Abs());
        var bounds = transform * local;
        for (var index = first; index < _vertices.Count; index++) bounds = bounds.Expand(_vertices[index].Position);
        if (!bounds.IsFinite()) throw new InvalidOperationException("Transformed screen bounds exceed the finite canvas range.");
        var viewport = new Rect2(0, 0, pixels.X, pixels.Y);
        if (clip is { } clipping) viewport = viewport.Intersection(new(clipping.Position, clipping.Size));
        notifier.ScreenCandidate = bounds.Intersects(viewport, includeBorders: true);
    }

    private void DispatchScreenVisibility(SceneTree tree)
    {
        _screenChanges.Clear();
        foreach (var node in _nodes)
            if (node is VisibleOnScreenNotifier notifier && !notifier.IsDisposed && ReferenceEquals(notifier.Tree, tree) && notifier.CommitScreenState())
                _screenChanges.Add((notifier, notifier.ScreenGeneration));
        List<Exception>? errors = null;
        try
        {
            foreach (var change in _screenChanges)
                if (!change.Node.IsDisposed && ReferenceEquals(change.Node.Tree, tree) && change.Node.ScreenGeneration == change.Generation)
                    try { change.Node.RaiseScreenChange(); }
                    catch (Exception error) { (errors ??= []).Add(error); }
        }
        finally { _screenChanges.Clear(); }
        if (errors is not null) throw new AggregateException("Canvas screen visibility callbacks failed.", errors);
    }
}
