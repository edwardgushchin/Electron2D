namespace Electron2D.Editor;

/// <summary>Owns one existing typed scene and UTF-8 C# buffer without a window or scene activation.</summary>
/// <remarks>All calls and disposal use the creating thread. A retained OS file lease excludes another owner.
/// Clients are trusted in-process capabilities; network discovery/authentication belong to the later transport.
/// Edits publish durable recovery before memory. Each Save publishes one file; recovery preserves other unsaved
/// documents across interrupted multi-file workflows. Runtime factories, reconstruction and save callbacks are
/// trusted code, not a sandbox. No SceneTree, Ready, process loop or renderer is started.</remarks>
public sealed partial class ProjectSession : IDisposable
{
    private readonly SessionStorage _storage;
    private readonly int _thread = Environment.CurrentManagedThreadId;
    private readonly Dictionary<Guid, UndoRedo> _history = [];
    private DocumentState _state;
    private bool _busy, _disposed, _faulted;
    private Action? _prepared;

    private ProjectSession(SessionStorage storage, DocumentState state)
    {
        _storage = storage; _state = state;
        try { foreach (var group in state.History) AddHistory(group); }
        catch { foreach (var history in _history.Values) history.Dispose(); throw; }
    }

    /// <summary>Acquires the sole owner lease, opening or recovering the same two project-relative documents.</summary>
    /// <param name="projectRoot">Existing project directory; symbolic-link paths are rejected.</param>
    /// <param name="scenePath">Existing typed scene, relative to the root.</param>
    /// <param name="codePath">Existing strict UTF-8 C# file, relative to the root.</param>
    /// <returns>The caller-owned session, retaining a project-wide writer lease.</returns>
    /// <exception cref="ArgumentException">Paths escape the project, overlap, or do not identify C# source.</exception>
    /// <exception cref="IOException">Another owner holds the project, a path is unsafe, or persistence fails.</exception>
    /// <exception cref="InvalidDataException">A file/recovery record is invalid, oversized or belongs to different documents.</exception>
    public static ProjectSession Open(string projectRoot, string scenePath, string codePath)
    {
        var storage = new SessionStorage(projectRoot);
        try
        {
            var scene = storage.Resolve(scenePath); var code = storage.Resolve(codePath);
            scenePath = System.IO.Path.GetRelativePath(storage.Root, scene); codePath = System.IO.Path.GetRelativePath(storage.Root, code);
            if (scene == code || !code.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Choose distinct scene and C# files.");
            var state = storage.ReadState();
            if (state is not null)
            {
                if (state.ScenePath != scenePath || state.CodePath != codePath) throw new InvalidDataException("This project's session owns a different document pair.");
                // The encode file retains its own UID, independently of the project's scene UID.
                SessionStorage.AtomicWrite(storage.EncodePath, state.Scene);
                using var root = Decode(storage, state.Scene, scene, state.Nodes);
                ValidateCatalog(root, state.Nodes);
            }
            else
            {
                var sceneHash = SessionStorage.Hash(SessionStorage.Read(scene, SessionStorage.MaxSceneBytes));
                var codeBytes = SessionStorage.Read(code, SessionStorage.MaxCodeBytes);
                var (text, bom) = SessionStorage.DecodeCode(codeBytes);
                using var root = Load(scene); root.SceneFilePath = scene;
                var bytes = Encode(storage, root);
                var nodes = Catalog(root, null, 1);
                state = new DocumentState
                {
                    ScenePath = scenePath,
                    CodePath = codePath,
                    Scene = bytes,
                    Nodes = nodes,
                    SceneFileHash = sceneHash,
                    SavedSceneHash = SessionStorage.Hash(bytes),
                    CodeFileHash = SessionStorage.Hash(codeBytes),
                    Code = text,
                    SavedCode = text,
                    BOM = bom,
                    SavedBOM = bom
                };
                state = storage.Persist(state);
            }
            return new ProjectSession(storage, state);
        }
        catch { storage.Dispose(); throw; }
    }

    /// <summary>Gets the canonical project root.</summary>
    /// <value>The absolute directory protected by the writer lease.</value>
    public string ProjectRoot { get { Check(); return _storage.Root; } }

    /// <summary>Publishes one committed transition, in registration order; observer failures become result diagnostics.</summary>
    public event Action<SessionResult>? Changed;

    /// <summary>Creates a local operation client with bounded attribution and shared live buffers.</summary>
    /// <param name="origin">Nonblank attribution, at most 128 characters.</param>
    /// <returns>A disposable capability using the same live state as every other client.</returns>
    /// <exception cref="ArgumentException">Attribution is blank or exceeds the bound.</exception>
    /// <exception cref="InvalidOperationException">Called outside the owner thread or recovery is required.</exception>
    /// <exception cref="ObjectDisposedException">The owner was disposed.</exception>
    public SessionClient Connect(string origin)
    {
        Check(); ArgumentException.ThrowIfNullOrWhiteSpace(origin);
        if (origin.Length > 128) throw new ArgumentOutOfRangeException(nameof(origin));
        return new SessionClient(this, origin);
    }

    internal SessionScene Scene()
    {
        Check();
        return new(_state.SceneID, _state.ScenePath, _state.SceneRevision,
            SessionStorage.Hash(_state.Scene) != _state.SavedSceneHash, External(SessionDocumentKind.Scene),
            Array.AsReadOnly(_state.Nodes.Select(n => new SessionNode(n.ID, n.ParentID, n.Name, n.TypeName, n.Revision, n.CanEdit, n.Transform)).ToArray()));
    }

    internal SessionCode Code()
    {
        Check(); return new(_state.CodeID, _state.CodePath, _state.CodeRevision, _state.Code != _state.SavedCode || _state.BOM != _state.SavedBOM,
            External(SessionDocumentKind.Code), _state.Code);
    }

    internal IReadOnlyList<SessionOperation> InspectHistory()
    {
        Check(); return Array.AsReadOnly(_state.History.Select(h => new SessionOperation(h.ID, h.Label, h.Origin, h.Applied)).ToArray());
    }

    private string PathFor(SessionDocumentKind kind) => _storage.Resolve(kind switch
    { SessionDocumentKind.Scene => _state.ScenePath, SessionDocumentKind.Code => _state.CodePath, _ => throw new ArgumentOutOfRangeException(nameof(kind)) });
    private bool External(SessionDocumentKind kind)
    {
        try
        {
            return SessionStorage.FileHash(PathFor(kind), kind == SessionDocumentKind.Scene ? SessionStorage.MaxSceneBytes : SessionStorage.MaxCodeBytes)
                != (kind == SessionDocumentKind.Scene ? _state.SceneFileHash : _state.CodeFileHash);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            // An unreadable/oversize external file must not hide the recoverable live buffer.
            return true;
        }
    }

    private void Check()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_thread != Environment.CurrentManagedThreadId) throw new InvalidOperationException("Use the session owner thread.");
        if (_faulted) throw new InvalidOperationException("Persistence recovery is required; dispose and reopen the session.");
    }

    private void Begin()
    {
        Check(); if (_busy) throw new InvalidOperationException("Session mutations cannot reenter a transition."); _busy = true;
    }

    private SessionConflictException Conflict(Guid objectID, ulong expected, ulong actual, bool code = false) =>
        new(code ? _state.CodeID : _state.SceneID, objectID, expected, actual, "The affected document/object changed; inspect its current revision before retrying.");

    private void Expected(SessionDocumentKind kind, ulong revision)
    {
        var actual = kind switch { SessionDocumentKind.Scene => _state.SceneRevision, SessionDocumentKind.Code => _state.CodeRevision, _ => throw new ArgumentOutOfRangeException(nameof(kind)) };
        if (revision != actual) throw Conflict(Guid.Empty, revision, actual, kind == SessionDocumentKind.Code);
    }

    internal SessionResult Apply(string origin, string label, IReadOnlyList<SessionEdit> edits)
    {
        Begin();
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(label); ArgumentNullException.ThrowIfNull(edits);
            if (label.Length > 128 || edits.Count is < 1 or > 64) throw new ArgumentException("A group needs 1–64 edits and a label of at most 128 characters.");
            var requests = edits.ToArray(); // Never execute a client-owned collection during staging.
            var touched = new HashSet<Guid>(); var hierarchy = false; var code = _state.Code;
            foreach (var edit in requests)
            {
                if (edit is SessionCodeEdit source)
                {
                    if (source.DocumentID != _state.CodeID) throw new ArgumentException("Unknown code document.");
                    Expected(SessionDocumentKind.Code, source.ExpectedRevision); SessionStorage.CodeBytes(source.Text, _state.BOM); code = source.Text;
                    continue;
                }
                var id = edit switch { SessionTransformEdit e => e.NodeID, SessionRenameEdit e => e.NodeID, SessionReparentEdit e => e.NodeID, _ => throw new ArgumentException("Unknown edit kind.") };
                var node = _state.Nodes.SingleOrDefault(n => n.ID == id) ?? throw new ArgumentException("Unknown node identity.");
                if (!node.CanEdit) throw new NotSupportedException("This slice edits only the root and its directly owned nodes.");
                switch (edit)
                {
                    case SessionTransformEdit e:
                        if (node.Revision != e.ExpectedRevision) throw Conflict(id, e.ExpectedRevision, node.Revision);
                        if (node.Transform is null) throw new ArgumentException("The object is not an Entity.");
                        if (!e.Value.IsFinite()) throw new ArgumentOutOfRangeException(nameof(edits), "Transforms must be finite.");
                        break;
                    case SessionRenameEdit e:
                        if (node.Revision != e.ExpectedRevision) throw Conflict(id, e.ExpectedRevision, node.Revision);
                        ValidateName(e.Name); break;
                    case SessionReparentEdit e:
                        Expected(SessionDocumentKind.Scene, e.ExpectedSceneRevision); hierarchy = true;
                        if (node.ParentID == Guid.Empty || !_state.Nodes.Any(n => n.ID == e.ParentID && n.CanEdit)) throw new ArgumentException("Choose a nonroot node and editable parent.");
                        break;
                }
                touched.Add(id);
            }
            var nodes = _state.Nodes; var bytes = _state.Scene;
            if (touched.Count > 0)
            {
                using var root = Decode(_storage, bytes, PathFor(SessionDocumentKind.Scene), _state.Nodes);
                var map = Map(root, nodes);
                foreach (var edit in requests)
                {
                    switch (edit)
                    {
                        case SessionTransformEdit e: ((Entity)map[e.NodeID]).Transform = e.Value; break;
                        case SessionRenameEdit e:
                            if (map[e.NodeID].Parent?.GetChildren().Any(n => n != map[e.NodeID] && n.Name == e.Name) == true) throw new ArgumentException("Sibling names must be unique.");
                            map[e.NodeID].Name = e.Name; break;
                        case SessionReparentEdit e:
                            var node = map[e.NodeID]; var parent = map[e.ParentID];
                            if (parent.GetChildren().Any(n => n != node && n.Name == node.Name)) throw new ArgumentException("Sibling names must be unique.");
                            node.Reparent(parent, e.KeepGlobalTransform); node.Owner = root; break;
                    }
                }
                nodes = Catalog(root, map.ToDictionary(p => p.Value, p => p.Key), _state.Clock, _state.Nodes);
                bytes = Encode(_storage, root);
            }
            var before = _state.Nodes.Where(n => touched.Contains(n.ID)).ToArray();
            var after = nodes.Where(n => touched.Contains(n.ID)).ToArray();
            if (bytes.AsSpan().SequenceEqual(_state.Scene) && code == _state.Code) return Result(Guid.Empty, SessionChangeKind.Applied, origin, false);
            var revision = checked(_state.Clock + 1);
            var sceneChanged = !bytes.AsSpan().SequenceEqual(_state.Scene); var codeChanged = code != _state.Code;
            // Hierarchy groups guard the whole scene; property groups allow independent object/code edits.
            var affected = hierarchy ? nodes.Select(n => n.ID).ToHashSet() : touched;
            nodes = nodes.Select(n => affected.Contains(n.ID) && sceneChanged ? n with { Revision = revision } : n).ToArray();
            var group = new HistoryState(Guid.NewGuid(), label, origin, before, after, hierarchy,
                codeChanged ? _state.Code : null, codeChanged ? code : null,
                sceneChanged ? nodes.Where(n => affected.Contains(n.ID)).ToDictionary(n => n.ID, n => n.Revision) : [],
                codeChanged ? revision : 0, hierarchy && sceneChanged ? revision : 0, true);
            var next = _state with
            {
                Clock = revision,
                Scene = bytes,
                Nodes = nodes,
                Code = code,
                SceneRevision = sceneChanged ? revision : _state.SceneRevision,
                CodeRevision = codeChanged ? revision : _state.CodeRevision,
                History = [.. _state.History.TakeLast(31), group]
            };
            using var pending = PrepareHistory(group);
            _state = _storage.Persist(next);
            _history.Add(group.ID, pending.Take()); RetireHistory();
            return Result(group.ID, SessionChangeKind.Applied, origin, false);
        }
        finally { _busy = false; }
    }

    private static void ValidateName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Length > 256 || name.AsSpan().IndexOfAny(".:@/\"%\\") >= 0) throw new ArgumentException("Use a bounded literal node name without path/reserved characters.", nameof(name));
    }

    internal SessionResult History(string origin, Guid id, bool redo)
    {
        Begin();
        try
        {
            var group = _state.History.SingleOrDefault(h => h.ID == id) ?? throw new ArgumentException("Unknown or retired operation.", nameof(id));
            if (group.Applied == redo) throw new InvalidOperationException(redo ? "The operation is already applied." : "The operation is already undone.");
            foreach (var (nodeID, token) in group.Tokens)
            {
                var current = _state.Nodes.SingleOrDefault(n => n.ID == nodeID);
                if (current?.Revision != token) throw Conflict(nodeID, token, current?.Revision ?? 0);
            }
            if (group.SceneToken != 0) Expected(SessionDocumentKind.Scene, group.SceneToken);
            if (group.CodeToken != 0) Expected(SessionDocumentKind.Code, group.CodeToken);
            var targets = redo ? group.After : group.Before;
            var nodes = _state.Nodes; var bytes = _state.Scene;
            if (group.Tokens.Count > 0)
            {
                using var root = Decode(_storage, bytes, PathFor(SessionDocumentKind.Scene), _state.Nodes);
                var map = Map(root, nodes);
                foreach (var target in targets)
                    if (map[target.ID].Name != target.Name) map[target.ID].Name = "SessionRestore" + Guid.NewGuid().ToString("N");
                if (group.Hierarchy)
                {
                    // Restore moved nodes in target ancestor order; a fresh hierarchy token prevents overlap.
                    foreach (var target in targets.OrderBy(n => Depth(n.ID, targets, nodes)))
                    {
                        var node = map[target.ID];
                        if (target.ParentID != Guid.Empty && node.Parent != map[target.ParentID]) { node.Reparent(map[target.ParentID], false); node.Owner = root; }
                    }
                }
                foreach (var target in targets)
                {
                    var node = map[target.ID];
                    if (node.Name != target.Name) node.Name = target.Name;
                    if (target.Transform is { } pose) ((Entity)node).Transform = pose;
                    if (node.Name != target.Name) throw new InvalidOperationException("History conflicts with a sibling name.");
                    if (group.Hierarchy && node.Parent is { } parent) parent.MoveChild(node, target.Index);
                }
                nodes = Catalog(root, map.ToDictionary(p => p.Value, p => p.Key), _state.Clock, _state.Nodes);
                bytes = Encode(_storage, root);
            }
            var revision = checked(_state.Clock + 1);
            nodes = nodes.Select(n => group.Tokens.ContainsKey(n.ID) ? n with { Revision = revision } : n).ToArray();
            var updated = group with { Applied = redo, Tokens = group.Tokens.Keys.ToDictionary(k => k, _ => revision), CodeToken = group.CodeToken == 0 ? 0 : revision, SceneToken = group.SceneToken == 0 ? 0 : revision };
            var next = _state with
            {
                Clock = revision,
                Scene = bytes,
                Nodes = nodes,
                Code = (redo ? group.AfterCode : group.BeforeCode) ?? _state.Code,
                SceneRevision = group.Tokens.Count > 0 ? revision : _state.SceneRevision,
                CodeRevision = group.CodeToken != 0 ? revision : _state.CodeRevision,
                History = _state.History.Select(h => h.ID == id ? updated : h).ToArray()
            };
            next = _storage.Persist(next);
            _prepared = () => _state = next;
            try { if (redo) _history[id].Redo(); else _history[id].Undo(); }
            finally { _prepared = null; }
            RetireHistory();
            return Result(id, redo ? SessionChangeKind.Redone : SessionChangeKind.Undone, origin, false);
        }
        finally { _busy = false; }
    }

    private static int Depth(Guid id, NodeState[] overrides, NodeState[] nodes)
    {
        var depth = 0;
        while (id != Guid.Empty)
        {
            if (++depth > nodes.Length) throw new InvalidDataException("Cyclic history hierarchy.");
            id = (overrides.SingleOrDefault(n => n.ID == id) ?? nodes.Single(n => n.ID == id)).ParentID;
        }
        return depth;
    }

    private HistoryLease PrepareHistory(HistoryState group)
    {
        var history = new UndoRedo();
        try
        {
            history.CreateAction(group.Label); history.AddDoMethod(PublishPrepared); history.AddUndoMethod(PublishPrepared); history.CommitAction(false);
            if (!group.Applied) history.Undo();
            return new HistoryLease(history);
        }
        catch { history.Dispose(); throw; }
    }
    private void PublishPrepared() => _prepared?.Invoke();
    private void AddHistory(HistoryState group) { using var lease = PrepareHistory(group); _history.Add(group.ID, lease.Take()); }
    private sealed class HistoryLease(UndoRedo history) : IDisposable
    {
        private UndoRedo? _value = history;
        internal UndoRedo Take() { var value = _value!; _value = null; return value; }
        public void Dispose() => _value?.Dispose();
    }
    private void RetireHistory()
    {
        foreach (var id in _history.Keys.Where(id => !_state.History.Any(h => h.ID == id)).ToArray()) { _history[id].Dispose(); _history.Remove(id); }
    }

    private SessionResult Result(Guid id, SessionChangeKind kind, string origin, bool written, List<SessionDiagnostic>? diagnostics = null)
    {
        diagnostics ??= [];
        var observation = new SessionResult(id, kind, origin, _state.SceneRevision, _state.CodeRevision, written, Array.AsReadOnly(diagnostics.ToArray()));
        foreach (var observer in Changed?.GetInvocationList() ?? [])
        {
            try { ((Action<SessionResult>)observer)(observation); }
            catch (Exception error) { diagnostics.Add(new("observer-failed", error.Message)); }
        }
        return observation with { Diagnostics = Array.AsReadOnly(diagnostics.ToArray()) };
    }

    /// <summary>Releases the owner lease; committed unsaved documents and history remain recoverable.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        if (_thread != Environment.CurrentManagedThreadId || _busy) throw new InvalidOperationException("Dispose on the idle session owner thread.");
        _disposed = true; Changed = null;
        try { foreach (var history in _history.Values) history.Dispose(); }
        finally { _storage.Dispose(); }
    }
}
