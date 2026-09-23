namespace Electron2D;

public sealed partial class SceneTree
{
    private readonly List<Node> _retiredScenes = [];
    private Node? _currentScene;
    private Node? _pendingScene;
    private bool _sceneChangeQueued;
    private bool _sceneChangePreparing;

    /// <summary>Gets or selects the active scene child of <see cref="Root"/>.</summary>
    /// <value>The selected direct child, or null when no scene is selected.</value>
    /// <remarks>Assigning a child changes only the selected reference. Use <see cref="ChangeSceneToNode"/> to replace a scene.</remarks>
    /// <exception cref="ArgumentException">The assigned node is not a direct child of <see cref="Root"/>.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The tree or assigned node is disposed.</exception>
    public Node? CurrentScene
    {
        get { EnsureOwnerThread(); EnsureAcceptingWork(); return _currentScene; }
        set
        {
            EnsureOwnerThread(); EnsureAcceptingWork();
            if (value is not null)
            {
                ObjectDisposedException.ThrowIf(value.IsDisposed, value);
                if (!ReferenceEquals(value.Parent, Root))
                    throw new ArgumentException("The current scene must be a direct child of the tree root.", nameof(value));
            }
            _currentScene = value;
        }
    }

    /// <summary>Occurs after a pending scene has entered the tree successfully.</summary>
    /// <remarks>The argument is this tree. The selected scene is available through <see cref="CurrentScene"/>.</remarks>
    public event Action<SceneTree>? SceneChanged;

    /// <summary>Replaces the current scene with a detached node at the next deferred safe point.</summary>
    /// <param name="node">The live, parentless scene root. The tree takes ownership after acceptance.</param>
    /// <remarks>The old scene exits immediately and is disposed before the new scene enters. A later request before the
    /// safe point supersedes the pending scene. A callback failure after detachment does not cancel an accepted change.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The new scene is attached, queued for deletion, or cannot enter this root.</exception>
    /// <exception cref="ObjectDisposedException">The tree or scene node is disposed.</exception>
    /// <exception cref="Exception">A callback during old-scene removal fails after the replacement is accepted.</exception>
    public void ChangeSceneToNode(Node node)
    {
        ThrowIfDisposed(); EnsureOwnerThread(); EnsureAcceptingWork();
        ArgumentNullException.ThrowIfNull(node);
        ObjectDisposedException.ThrowIf(node.IsDisposed, node);
        if (!_constructionComplete || _sceneChangePreparing)
            throw new InvalidOperationException("A scene change cannot be started during scene activation or another scene change.");
        if (node.Parent is not null || node.Tree is not null || node.IsQueuedForDeletion ||
            ReferenceEquals(node, _pendingScene) || _retiredScenes.Contains(node))
            throw new InvalidOperationException("The new scene must be a detached node not already owned by this tree.");
        if (node is Viewport)
            throw new NotSupportedException("A child viewport requires multiwindow or offscreen rendering support.");
        node.EnsureSceneActivationAvailable();
        if (Root.Children.Any(child => !ReferenceEquals(child, _currentScene) && StringComparer.Ordinal.Equals(child.Name, node.Name)))
            throw new InvalidOperationException("A child with this scene name already exists under the tree root.");

        Exception? removalError = null;
        _sceneChangePreparing = true;
        try
        {
            if (_currentScene is { } previous)
            {
                _retiredScenes.Add(previous);
                try { Root.RemoveChild(previous); }
                catch (Exception error)
                {
                    if (ReferenceEquals(previous.Parent, Root))
                    {
                        _retiredScenes.Remove(previous);
                        throw;
                    }
                    removalError = error;
                }
            }

            EnsureAcceptingWork();
            if (_pendingScene is { } superseded) _retiredScenes.Add(superseded);
            _pendingScene = node;
            if (!_sceneChangeQueued)
            {
                Defer(FlushSceneChange);
                _sceneChangeQueued = true;
            }
        }
        finally { _sceneChangePreparing = false; }

        if (removalError is not null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(removalError).Throw();
    }

    /// <summary>Instantiates a packed scene and schedules it to replace the current scene.</summary>
    /// <param name="packedScene">The reusable in-memory scene template.</param>
    /// <remarks>Instantiation completes before the old scene is removed. An empty or failing template leaves the current scene intact.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="packedScene"/> is null.</exception>
    /// <exception cref="Exception">Instantiation or the subsequent scene change fails.</exception>
    public void ChangeSceneToPacked(PackedScene packedScene)
    {
        ThrowIfDisposed(); EnsureOwnerThread(); EnsureAcceptingWork();
        ArgumentNullException.ThrowIfNull(packedScene);
        var node = packedScene.Instantiate();
        try { ChangeSceneToNode(node); }
        catch (Exception changeError)
        {
            if (!ReferenceEquals(_pendingScene, node) && !ReferenceEquals(_currentScene, node) && !_retiredScenes.Contains(node))
            {
                try { node.Dispose(); }
                catch (Exception cleanupError) { throw new AggregateException(changeError, cleanupError); }
            }
            throw;
        }
    }

    /// <summary>Immediately disposes the selected current scene, leaving other root children intact.</summary>
    /// <remarks>A pending scene change remains scheduled.</remarks>
    /// <exception cref="InvalidOperationException">The caller is not the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The tree is disposed.</exception>
    /// <exception cref="Exception">A disposal callback fails after the scene is removed.</exception>
    public void UnloadCurrentScene()
    {
        ThrowIfDisposed(); EnsureOwnerThread(); EnsureAcceptingWork();
        _currentScene?.Dispose();
    }

    private void FlushSceneChange()
    {
        _sceneChangeQueued = false;
        var pending = _pendingScene;
        _pendingScene = null;
        var retired = _retiredScenes.ToArray();
        _retiredScenes.Clear();
        List<Exception>? errors = null;

        _sceneChangePreparing = true;
        try
        {
            foreach (var scene in retired)
            {
                try { scene.Dispose(); }
                catch (Exception error) { CollectException(ref errors, error); }
            }

            if (pending is not null && !pending.IsDisposed)
            {
                _currentScene = pending;
                try { Root.AddChild(pending); }
                catch (Exception error)
                {
                    CollectException(ref errors, error);
                    if (!ReferenceEquals(pending.Parent, Root) || !ReferenceEquals(pending.Tree, this))
                    {
                        _currentScene = null;
                        try { pending.Dispose(); }
                        catch (Exception cleanupError) { CollectException(ref errors, cleanupError); }
                    }
                }
            }
        }
        finally { _sceneChangePreparing = false; }

        if (pending is not null && ReferenceEquals(_currentScene, pending) && ReferenceEquals(pending.Tree, this) && pending.IsNodeReady)
        {
            try { SceneChanged?.Invoke(this); }
            catch (Exception error) { CollectException(ref errors, error); }
        }
        ThrowCollected("One or more scene-change callbacks failed.", errors);
    }

    private void DisposePendingScenes(ref List<Exception>? errors)
    {
        var pending = _pendingScene;
        _pendingScene = null;
        _sceneChangeQueued = false;
        foreach (var scene in _retiredScenes)
        {
            try { scene.Dispose(); }
            catch (Exception error) { CollectException(ref errors, error); }
        }
        _retiredScenes.Clear();
        if (pending is not null)
        {
            try { pending.Dispose(); }
            catch (Exception error) { CollectException(ref errors, error); }
        }
    }
}
