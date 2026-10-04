namespace Electron2D;

public partial class SceneTree
{
    private MultiplayerAPI? _defaultMultiplayer;
    private bool _ownsDefaultMultiplayer;
    private readonly Dictionary<string, MultiplayerAPI> _customMultiplayer = new(StringComparer.Ordinal);
    private readonly List<MultiplayerAPI> _multiplayerSnapshot = [];
    private bool _multiplayerPoll = true;
    /// <summary>Gets or sets automatic process-frame multiplayer polling.</summary><value>True initially; polling occurs after the process-frame event and before node callbacks, including while paused.</value>
    public bool MultiplayerPoll { get { ThrowIfDisposed(); EnsureOwnerThread(); return _multiplayerPoll; } set { ThrowIfDisposed(); EnsureOwnerThread(); _multiplayerPoll = value; } }
    private void InitializeMultiplayer()
    {
        var api = MultiplayerAPI.CreateDefaultInterface(); try { api.Attach(this, Root.GetPath()); _defaultMultiplayer = api; _ownsDefaultMultiplayer = true; } catch { api.Dispose(); throw; }
    }
    /// <summary>Gets a branch-specific override or the tree's default multiplayer interface.</summary><param name="forPath">Absolute branch path; empty selects default. The most specific ancestor override wins.</param><returns>A borrowed live interface.</returns>
    public MultiplayerAPI GetMultiplayer(string forPath = "")
    {
        ThrowIfDisposed(); EnsureOwnerThread(); ArgumentNullException.ThrowIfNull(forPath); if (forPath.Length == 0) return _defaultMultiplayer ?? throw new InvalidOperationException("Scene multiplayer is not initialized.");
        if (!forPath.StartsWith('/')) throw new ArgumentException("Multiplayer branch path must be absolute.", nameof(forPath)); MultiplayerAPI? selected = null; var length = -1;
        foreach (var entry in _customMultiplayer) if (forPath.StartsWith(entry.Key, StringComparison.Ordinal) && (forPath.Length == entry.Key.Length || forPath[entry.Key.Length] == '/') && entry.Key.Length > length) { selected = entry.Value; length = entry.Key.Length; }
        return selected ?? _defaultMultiplayer ?? throw new InvalidOperationException("Scene multiplayer is not initialized.");
    }
    internal MultiplayerAPI GetMultiplayer(Node node)
    {
        ThrowIfDisposed(); EnsureOwnerThread(); if (_customMultiplayer.Count == 0) return _defaultMultiplayer!;
        // Branch selection is cold-cached by hierarchy version below; queries avoid repeated path allocation.
        if (_multiplayerNodes.TryGetValue(node, out var result)) return result;
        result = GetMultiplayer(node.GetPath()); _multiplayerNodes[node] = result; return result;
    }
    private readonly Dictionary<Node, MultiplayerAPI> _multiplayerNodes = [];
    private void InvalidateMultiplayerPaths() { _multiplayerNodes.Clear(); if (_defaultMultiplayer is SceneMultiplayer scene) scene.InvalidatePaths(); foreach (var custom in _customMultiplayer.Values) if (custom is SceneMultiplayer branch) branch.InvalidatePaths(); }
    /// <summary>Assigns a borrowed interface to the default or an existing absolute scene branch.</summary><param name="multiplayer">Live owner-thread interface; null removes a custom branch or creates a new default.</param><param name="rootPath">Empty selects default; an absolute existing node path selects a branch.</param><remarks>One interface can belong to only one tree/branch. Replacement commits the new interface, detaches the old one and rebinds producers despite cleanup failure. Only tree-created defaults are disposed by the tree. Removing a custom mapping is allowed after its branch leaves.</remarks>
    public void SetMultiplayer(MultiplayerAPI? multiplayer, string rootPath = "")
    {
        ThrowIfDisposed(); EnsureOwnerThread(); EnsureAcceptingWork(); ArgumentNullException.ThrowIfNull(rootPath); List<Exception>? errors = null;
        if (rootPath.Length > 0)
        {
            if (!rootPath.StartsWith('/')) throw new ArgumentException("Multiplayer branch path must be absolute.", nameof(rootPath)); if (multiplayer is not null) Root.GetNode(rootPath);
            if (_customMultiplayer.TryGetValue(rootPath, out var current) && ReferenceEquals(current, multiplayer)) return;
            current?.ValidateDetachment(); multiplayer?.Attach(this, rootPath); if (multiplayer is null) _customMultiplayer.Remove(rootPath); else _customMultiplayer[rootPath] = multiplayer; _multiplayerNodes.Clear(); if (current is not null) try { current.Detach(); } catch (Exception error) { CollectException(ref errors, error); }
        }
        else
        {
            if (ReferenceEquals(multiplayer, _defaultMultiplayer)) return; _defaultMultiplayer?.ValidateDetachment(); var owned = multiplayer is null; multiplayer ??= MultiplayerAPI.CreateDefaultInterface();
            try { multiplayer.Attach(this, Root.GetPath()); } catch { if (owned) multiplayer.Dispose(); throw; }
            var old = _defaultMultiplayer; var dispose = _ownsDefaultMultiplayer; _defaultMultiplayer = multiplayer; _ownsDefaultMultiplayer = owned; _multiplayerNodes.Clear(); try { old?.Detach(); } catch (Exception error) { CollectException(ref errors, error); }
            if (dispose) try { old?.Dispose(); } catch (Exception error) { CollectException(ref errors, error); }
        }
        _multiplayerNodes.Clear();
        foreach (var node in Root.EnumerateDepthFirst().ToArray()) { if (node.IsDisposed || !ReferenceEquals(node.Tree, this)) continue; try { if (node is MultiplayerSpawner spawner) spawner.Rebind(); else if (node is MultiplayerSynchronizer sync) sync.Rebind(); } catch (Exception error) { CollectException(ref errors, error); } }
        ThrowCollected("Multiplayer replacement cleanup/configuration failed after committing the new interface.", errors);
    }
    private void PollMultiplayer(ref List<Exception>? errors)
    {
        if (!_multiplayerPoll) return; _multiplayerSnapshot.Clear(); if (_defaultMultiplayer is { } api) _multiplayerSnapshot.Add(api); foreach (var custom in _customMultiplayer.Values) _multiplayerSnapshot.Add(custom);
        for (var i = 0; i < _multiplayerSnapshot.Count; i++) { var current = _multiplayerSnapshot[i]; if (!ReferenceEquals(current.AttachedTree, this)) continue; try { current.Poll(); } catch (Exception e) { CollectException(ref errors, e); } }
        _multiplayerSnapshot.Clear();
    }
    private void FinalizeMultiplayer(ref List<Exception>? errors)
    {
        foreach (var custom in _customMultiplayer.Values) try { custom.Detach(); } catch (Exception e) { CollectException(ref errors, e); }
        _customMultiplayer.Clear();
        var current = _defaultMultiplayer; _defaultMultiplayer = null; try { current?.Detach(); } catch (Exception e) { CollectException(ref errors, e); }
        if (_ownsDefaultMultiplayer) try { current?.Dispose(); } catch (Exception e) { CollectException(ref errors, e); }
        _multiplayerNodes.Clear(); _multiplayerSnapshot.Clear();
    }
}
