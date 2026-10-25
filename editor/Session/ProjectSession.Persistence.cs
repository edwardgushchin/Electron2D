namespace Electron2D.Editor;

public sealed partial class ProjectSession
{
    private static Node Load(string path)
    {
        SessionStorage.Read(path, SessionStorage.MaxSceneBytes);
        using var packed = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep);
        return packed.Instantiate();
    }

    private static Node Decode(SessionStorage storage, byte[] bytes, string scenePath, NodeState[]? nodes = null)
    {
        SessionStorage.AtomicWrite(storage.DecodePath, bytes);
        var root = Load(storage.DecodePath);
        try
        {
            root.SceneFilePath = scenePath;
            if (nodes is not null)
            {
                var map = Map(root, nodes, checkPose: false);
                // Exact authoring poses are overlays: runtime stored component reconstruction can round floats.
                foreach (var node in nodes) if (node.Transform is { } pose) ((Entity)map[node.ID]).Transform = pose;
            }
            return root;
        }
        catch { root.Dispose(); throw; }
    }

    private static byte[] Encode(SessionStorage storage, Node root)
    {
        using var packed = new PackedScene(); packed.Pack(root);
        ResourceSaver.Save(packed, storage.EncodePath, SaverFlags.BundleResources);
        return SessionStorage.Read(storage.EncodePath, SessionStorage.MaxSceneBytes);
    }

    private static Node[] Flatten(Node root)
    {
        var result = new List<Node>(); Visit(root); return result.ToArray();
        void Visit(Node node)
        {
            if (result.Count >= 4096) throw new InvalidDataException("A session scene supports at most 4096 nodes.");
            result.Add(node); foreach (var child in node.GetChildren()) Visit(child);
        }
    }

    private static NodeState[] Catalog(Node root, Dictionary<Node, Guid>? identities, ulong revision, NodeState[]? previous = null)
    {
        var nodes = Flatten(root);
        identities ??= nodes.ToDictionary(n => n, _ => Guid.NewGuid());
        if (nodes.Any(n => !identities.ContainsKey(n))) throw new InvalidDataException("Reconstruction changed the authored scene topology.");
        return nodes.Select(n => new NodeState(identities[n], n.Parent is { } parent ? identities[parent] : Guid.Empty,
            n.Name, n.GetType().FullName!, previous?.SingleOrDefault(p => p.ID == identities[n])?.Revision ?? revision,
            ReferenceEquals(n, root) || ReferenceEquals(n.Owner, root), n is Entity entity ? entity.Transform : null, n.GetIndex())).ToArray();
    }

    private static void ValidateCatalog(Node root, NodeState[] nodes) => Map(root, nodes);
    private static Dictionary<Guid, Node> Map(Node root, NodeState[] nodes, bool checkPose = true)
    {
        var live = Flatten(root);
        if (live.Length != nodes.Length) throw new InvalidDataException("Recovery scene and identity catalog differ.");
        var map = new Dictionary<Guid, Node>();
        for (var i = 0; i < nodes.Length; i++)
        {
            var n = nodes[i]; var node = live[i];
            if (node.GetType().FullName != n.TypeName || node.Name != n.Name ||
                checkPose && node is Entity entity && entity.Transform != n.Transform ||
                node.Parent is { } parent && (n.ParentID == Guid.Empty || !map.TryGetValue(n.ParentID, out var mapped) || parent != mapped) ||
                node.Parent is null && n.ParentID != Guid.Empty ||
                n.CanEdit != (node == root || node.Owner == root) || n.Index != node.GetIndex())
                throw new InvalidDataException("Recovery no longer reconstructs the catalog's exact typed identity/pose/hierarchy.");
            map.Add(n.ID, node);
        }
        return map;
    }

    internal SessionResult Save(string origin, SessionDocumentKind kind, ulong expectedRevision)
    {
        Begin();
        try
        {
            Expected(kind, expectedRevision);
            if (External(kind)) throw Conflict(Guid.Empty, expectedRevision, expectedRevision, kind == SessionDocumentKind.Code);
            var path = PathFor(kind); var diagnostics = new List<SessionDiagnostic>();
            // Recovery already contains both live buffers. One file is published at a time, never a fake multi-file transaction.
            _state = _storage.Persist(_state);
            var beforeHash = kind == SessionDocumentKind.Scene ? _state.SceneFileHash : _state.CodeFileHash;
            Exception? failure = null;
            try
            {
                if (kind == SessionDocumentKind.Code) SessionStorage.AtomicWrite(path, SessionStorage.CodeBytes(_state.Code, _state.BOM));
                else
                {
                    using var root = Decode(_storage, _state.Scene, path, _state.Nodes); ValidateCatalog(root, _state.Nodes);
                    ResourceSaver.SaveScene(root, path, SaverFlags.BundleResources);
                }
            }
            catch (Exception error) { failure = error; }
            string? afterHash;
            try { afterHash = SessionStorage.FileHash(path, kind == SessionDocumentKind.Scene ? SessionStorage.MaxSceneBytes : SessionStorage.MaxCodeBytes); }
            catch (Exception error)
            {
                _faulted = true;
                diagnostics.Add(new("publication-observation-failed-reopen-required", error.Message));
                if (failure is not null) diagnostics.Add(new("save-failed-publication-uncertain", failure.Message));
                return Result(Guid.Empty, SessionChangeKind.Saved, origin, failure is null, diagnostics);
            }
            var written = failure is null || afterHash != beforeHash && afterHash is not null;
            if (!written) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure!).Throw();
            if (failure is not null) diagnostics.Add(new("save-callback-failed-after-write", failure.Message));
            try
            {
                if (kind == SessionDocumentKind.Code)
                    _state = _state with { SavedCode = _state.Code, SavedBOM = _state.BOM, CodeFileHash = afterHash! };
                else
                {
                    // RESET/pre-save projection may differ from the unchanged authoring buffer.
                    var uid = ResourceSaver.GetResourceIDForPath(_storage.EncodePath, generate: true);
                    // Normalize only the private copy's file identity; never reassign the project file UID.
                    SessionStorage.AtomicWrite(_storage.EncodePath, SessionStorage.Read(path, SessionStorage.MaxSceneBytes));
                    ResourceSaver.SetUID(_storage.EncodePath, uid);
                    var saved = SessionStorage.Read(_storage.EncodePath, SessionStorage.MaxSceneBytes);
                    _state = _state with { SavedSceneHash = SessionStorage.Hash(saved), SceneFileHash = afterHash! };
                }
                _state = _storage.Persist(_state);
            }
            catch (Exception error)
            {
                // The file may already be durable. Retain the previous recovery and stop mutations until reopen.
                _faulted = true; diagnostics.Add(new("reopen-required-after-write", error.Message));
            }
            return Result(Guid.Empty, SessionChangeKind.Saved, origin, true, diagnostics);
        }
        finally { _busy = false; }
    }

    internal SessionResult Refresh(string origin, SessionDocumentKind kind, ulong expectedRevision, SessionExternalResolution resolution)
    {
        Begin();
        try
        {
            Expected(kind, expectedRevision);
            var dirty = kind == SessionDocumentKind.Scene ? SessionStorage.Hash(_state.Scene) != _state.SavedSceneHash : _state.Code != _state.SavedCode || _state.BOM != _state.SavedBOM;
            if (!Enum.IsDefined(resolution)) throw new ArgumentOutOfRangeException(nameof(resolution));
            if (dirty && resolution == SessionExternalResolution.Reject) throw Conflict(Guid.Empty, expectedRevision, expectedRevision, kind == SessionDocumentKind.Code);
            if (!External(kind) && !dirty) return Result(Guid.Empty, SessionChangeKind.Refreshed, origin, false);
            var path = PathFor(kind); var revision = checked(_state.Clock + 1); DocumentState next;
            if (kind == SessionDocumentKind.Scene)
            {
                var hash = SessionStorage.Hash(SessionStorage.Read(path, SessionStorage.MaxSceneBytes));
                using var root = Load(path); root.SceneFilePath = path;
                var bytes = Encode(_storage, root);
                next = _state with { Clock = revision, SceneRevision = revision, Scene = resolution == SessionExternalResolution.KeepLive ? _state.Scene : bytes, Nodes = resolution == SessionExternalResolution.KeepLive ? _state.Nodes : Catalog(root, null, revision), SceneFileHash = hash, SavedSceneHash = SessionStorage.Hash(bytes), History = _state.History.Where(h => h.Tokens.Count == 0).ToArray() };
            }
            else
            {
                var data = SessionStorage.Read(path, SessionStorage.MaxCodeBytes); var (text, bom) = SessionStorage.DecodeCode(data);
                next = _state with { Clock = revision, CodeRevision = revision, Code = resolution == SessionExternalResolution.KeepLive ? _state.Code : text, SavedCode = text, SavedBOM = bom, BOM = resolution == SessionExternalResolution.KeepLive ? _state.BOM : bom, CodeFileHash = SessionStorage.Hash(data), History = _state.History.Where(h => h.CodeToken == 0).ToArray() };
            }
            _state = _storage.Persist(next); RetireHistory();
            return Result(Guid.Empty, SessionChangeKind.Refreshed, origin, false);
        }
        finally { _busy = false; }
    }
}
