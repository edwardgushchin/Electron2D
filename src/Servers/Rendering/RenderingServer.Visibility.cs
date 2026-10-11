namespace Electron2D;

public sealed partial class RenderingServer
{
    private readonly List<(VisibleOnScreenNotifier Node, ulong Generation)> _screenChanges = [];

    private void AppendScreenCanvas(CanvasItem node, Transform transform, Rect2i? clip, Vector2i pixels)
    {
        var first = _vertices.Count;
        var batchStart = _batches.Count;
        var mode = (node.CanvasMaterial as CanvasItemMaterial)?.LightMode ?? LightMode.Normal;
        var layer = 0; node.RenderCanvas()?.View(_viewport, out _, out layer, out _);
        var illumination = _lighting.Affects(node.RenderLightMask, node.RenderZ, layer) || mode != LightMode.Normal;
        // A command boundary prevents items with different masks from merging into one lighting batch.
        var boundary = illumination || _batches.Count != 0 && _batches[^1].Lighting is not null;
        if (boundary) _batches.Add(new(0, 0, null, Operation: CanvasOperation.MaskFinish));
        node.AppendCanvas(_vertices, _batches, transform, CanvasTime, clip, pixels, _viewport, illumination);
        if (boundary) _batches.RemoveAt(batchStart);
        for (var i = batchStart; i < _batches.Count; i++)
        {
            var batch = _batches[i];
            var channels = (batch.Texture is AtlasTexture atlas ? atlas.RenderingTexture : batch.Texture) as CanvasTexture;
            var filter = channels?.TextureFilter is { } ownFilter && ownFilter != TextureFilter.ParentNode ? ownFilter : batch.Filter;
            var repeat = channels?.TextureRepeat is { } ownRepeat && ownRepeat != TextureRepeat.ParentNode ? ownRepeat : batch.Repeat;
            var shaded = illumination && mode != LightMode.Unshaded;
            _batches[i] = batch with { Filter = filter, Repeat = repeat, MaxAnisotropy = filter >= TextureFilter.NearestWithMipmapsAnisotropic ? 1 << (int)_viewport.AnisotropicFilteringLevel : 1, Lighting = shaded ? new(node.RenderCanvas()?.Modulate ?? Colors.White, node.RenderLightMask, node.RenderZ, layer, mode) : null };
        }
        if (node is not VisibleOnScreenNotifier notifier || notifier.ScreenCandidate || notifier.RenderInheritedModulate.A < 0.007f) return;
        var local = new Rect2().Merge(notifier.Rect.Abs());
        var bounds = transform * local;
        for (var index = first; index < _vertices.Count; index++) bounds = bounds.Expand(_vertices[index].Position);
        if (!bounds.IsFinite()) throw new InvalidOperationException("Transformed screen bounds exceed the finite canvas range.");
        var viewport = new Rect2(0, 0, pixels.X, pixels.Y);
        if (!node.MayIgnoreClip && clip is { } clipping) viewport = viewport.Intersection(new(clipping.Position, clipping.Size));
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
