namespace Electron2D.Editor;

/// <summary>Binds the first project tree, transform inspector, source and preview to the shared owner.</summary>
internal sealed class ProjectWorkspace : Control
{
    private readonly SessionProject _project;
    private readonly ProjectSession _session;
    private readonly SessionClient _human;
    private SessionCodeBuffer? _codeBuffer;
    private string _boundText = "";
    private readonly string _sdk;
    private AgentPanel? _agent;
    private readonly Tree _nodes = new() { Name = "SceneTree", ClipContents = true, SizeFlagsVertical = SizeFlags.ExpandFill, CustomMinimumSize = new(180, 160), FocusMode = FocusMode.All };
    private readonly SessionCodeEditor _code = new() { Name = "Code", ClipContents = true, GuttersDrawLineNumbers = true, LineFolding = true, SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill, CustomMinimumSize = new(300, 180), FocusMode = FocusMode.All };
    private readonly TextEdit _sessionCode = new() { Name = "CurrentSessionCode", ClipContents = true, Editable = false, SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill, CustomMinimumSize = new(280, 180) };
    private readonly VBoxContainer _conflictPane = new() { Name = "SourceConflict", Visible = false, SizeFlagsHorizontal = SizeFlags.ExpandFill };
    private readonly SpinBox _x = Coordinate("PositionX"), _y = Coordinate("PositionY");
    private readonly Label _selection = new("Select an Entity"), _sceneStatus = new(""), _codeStatus = new(""), _feedback = new("");
    private readonly Label _previewStatus = new("Authored scene"), _jobStatus = new("No snapshot job") { Name = "JobStatus" }, _captureStatus = new("No completed capture") { Name = "CaptureStatus" };
    private readonly OptionButton _history = new() { Name = "History", ClipText = true, CustomMinimumSize = new(220, 32), FocusMode = FocusMode.All };
    private readonly OptionButton _mode = new() { Name = "RunMode", ClipText = true, CustomMinimumSize = new(156, 32), FocusMode = FocusMode.All };
    private readonly SpinBox _frames = new() { Name = "Frames", MinValue = 1, MaxValue = 1000000, Step = 1, Value = 60, CustomMinimumSize = new(90, 32) };
    private readonly SubViewport _viewport = new() { Name = "SceneViewport", Size = new(320, 180), RenderTargetUpdateMode = ViewportUpdateMode.Always };
    private readonly TextureRect _capture = new() { Name = "Capture", ExpandMode = TextureRectExpandMode.IgnoreSize, StretchMode = TextureStretchMode.KeepAspectCentered, TextureFilter = TextureFilter.Nearest, CustomMinimumSize = new(180, 110), SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
    private readonly List<SessionJob> _jobs = [];
    private readonly List<Resource> _resources = [];
    private SessionScene _scene;
    private SessionCode _source;
    private SessionOperation[] _operations = [];
    private readonly Dictionary<Guid, TreeItem> _treeItems = [];
    private Guid _selected;
    private bool _binding, _refresh = true, _draft;
    private double _poll;
    private Node? _instance;
    private SessionJob? _job, _capturedJob, _observedJob;
    private ulong _previewRevision;
    private readonly HSplitContainer _body = new(), _main = new();
    private int _layoutPhase;
    private ImageTexture? _captureImage;
    private Button _build = null!, _run = null!, _cancel = null!, _retry = null!, _discard = null!;

    internal ProjectWorkspace(SessionProject project, ProjectSession session, string sdk, Font regular, Font mono, Func<ISessionAgentExecutor>? executorFactory = null)
    {
        Name = "Workspace"; ClipContents = true; _project = project; _session = session; _sdk = sdk; _human = session.Connect("human");
        _scene = _human.InspectScene(); _source = _human.InspectCode();
        var selection = _human.InspectSelection();
        if (selection.NodeID == Guid.Empty) _human.SelectNode(_scene.Nodes.FirstOrDefault(n => n.CanEdit && n.Transform.HasValue)?.ID ?? _scene.Nodes[0].ID, _scene.Revision, selection.Revision);
        try
        {
            var theme = new Theme { DefaultFont = regular, DefaultFontSize = 14 }; _resources.Add(theme); Theme = theme;
            _code.AddThemeFontOverride("font", mono); _sessionCode.AddThemeFontOverride("font", mono);
            _code.AddThemeFontSizeOverride("font_size", 14); _sessionCode.AddThemeFontSizeOverride("font_size", 14);
            var layout = new VBoxContainer { Name = "Layout", Separation = 10 }; AddChild(layout); layout.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            var toolbar = new HBoxContainer { Name = "Toolbar", Separation = 8, CustomMinimumSize = new(0, 40) }; layout.AddChild(toolbar);
            toolbar.AddChild(new Label(project.Name) { SizeFlagsHorizontal = SizeFlags.ExpandFill, ClipText = true });
            AddButton(toolbar, "Save scene", "SaveScene", () => Save(SessionDocumentKind.Scene));
            AddButton(toolbar, "Save code", "SaveCode", () => Save(SessionDocumentKind.Code));
            toolbar.AddChild(_history);
            AddButton(toolbar, "Undo", "Undo", () => PlayHistory(false, true)); AddButton(toolbar, "Redo", "Redo", () => PlayHistory(true, true));
            var body = _body; body.Name = "Body"; body.SizeFlagsVertical = body.SizeFlagsHorizontal = SizeFlags.ExpandFill; layout.AddChild(body);
            var tree = Column("Scene"); tree.AddChild(_sceneStatus); tree.AddChild(_nodes);
            var sceneReconcile = new HBoxContainer(); tree.AddChild(sceneReconcile);
            AddButton(sceneReconcile, "Keep live", "KeepScene", () => Reconcile(SessionDocumentKind.Scene, SessionExternalResolution.KeepLive));
            AddButton(sceneReconcile, "Reload disk", "ReloadScene", () => Reconcile(SessionDocumentKind.Scene, SessionExternalResolution.UseDisk));
            body.AddChild(Panel(tree));
            var main = _main; main.Name = "Main"; main.SizeFlagsHorizontal = main.SizeFlagsVertical = SizeFlags.ExpandFill; body.AddChild(main);
            var center = new VSplitContainer { Name = "Center", SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
            main.AddChild(center);
            var authored = Column("Authored scene"); authored.AddChild(_previewStatus);
            var preview = new SubViewportContainer { Name = "Preview", Stretch = true, SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill, CustomMinimumSize = new(300, 180) };
            preview.AddChild(_viewport); authored.AddChild(preview); center.AddChild(Panel(authored));
            var source = Column(project.CodeFile); source.AddChild(_codeStatus);
            var sourceActions = new HBoxContainer(); source.AddChild(sourceActions);
            AddButton(sourceActions, "Keep live", "KeepCode", () => Reconcile(SessionDocumentKind.Code, SessionExternalResolution.KeepLive));
            AddButton(sourceActions, "Reload disk", "ReloadCode", () => Reconcile(SessionDocumentKind.Code, SessionExternalResolution.UseDisk));
            _retry = AddButton(sourceActions, "Apply reviewed draft", "ApplyDraft", ApplyDraft); _discard = AddButton(sourceActions, "Discard draft", "DiscardDraft", DiscardDraft);
            _retry.Visible = _discard.Visible = false;
            var sources = new HSplitContainer { SizeFlagsVertical = SizeFlags.ExpandFill }; source.AddChild(sources); sources.AddChild(_code);
            _conflictPane.AddChild(new Label("Current shared session — review before replacing")); _conflictPane.AddChild(_sessionCode); sources.AddChild(_conflictPane);
            center.AddChild(Panel(source));
            var inspector = Column("Inspector"); inspector.CustomMinimumSize = new(240, 0); inspector.AddChild(_selection);
            inspector.AddChild(new Label("Position X")); inspector.AddChild(_x); inspector.AddChild(new Label("Position Y")); inspector.AddChild(_y);
            inspector.AddChild(new Label("Snapshot execution"));
            _mode.AddItem("Headless", 0); _mode.AddItem("GPU capture", 1); _mode.AddItem("Compatibility capture", 2); _mode.Select(1); inspector.AddChild(_mode);
            inspector.AddChild(new Label("Frame budget")); inspector.AddChild(_frames);
            var jobs = new HBoxContainer(); inspector.AddChild(jobs);
            _build = AddButton(jobs, "Build", "Build", () => StartJob(SessionJobKind.Build)); _run = AddButton(jobs, "Run snapshot", "Run", () => StartJob(SessionJobKind.Run));
            _cancel = AddButton(inspector, "Cancel job", "Cancel", () => _job?.Cancel());
            inspector.AddChild(_jobStatus); inspector.AddChild(_captureStatus); inspector.AddChild(_capture);
            AddButton(inspector, "Clear job results", "ClearResults", ClearResults);
            var tabs = new TabContainer { Name = "InspectorTabs", CustomMinimumSize = new(280, 0), SizeFlagsVertical = SizeFlags.ExpandFill };
            var inspectorPanel = Panel(inspector); inspectorPanel.Name = "Inspector"; tabs.AddChild(inspectorPanel);
            _agent = new(project, session, _human, sdk, executorFactory); var agentPanel = Panel(_agent); agentPanel.Name = "Agent"; tabs.AddChild(agentPanel);
            main.AddChild(tabs);
            _feedback.Name = "Feedback"; _feedback.ClipText = true; _feedback.CustomMinimumSize = new(0, 28); layout.AddChild(_feedback);
            _nodes.ItemSelected += SelectNode; _nodes.NothingSelected += SelectNode;
            _codeBuffer = _human.BindCodeBuffer(ReadPendingSource, RejectSource);
            _code.TextChanged += EditSource; _code.HistoryRequested = redo => PlayHistory(redo, false);
            _x.ValueChanged += value => EditPosition(value, true); _y.ValueChanged += value => EditPosition(value, false);
            _session.Changed += Changed;
            foreach (var label in new[] { _selection, _sceneStatus, _codeStatus, _previewStatus, _jobStatus, _captureStatus }) label.ClipText = true;
            RefreshViews();
        }
        catch (Exception error) { try { Dispose(); } catch (Exception cleanup) { throw new AggregateException(error, cleanup); } throw; }
    }

    internal void PrepareClose()
    {
        if (_draft) throw new InvalidOperationException("Apply or discard the visible source draft before closing this project.");
        if (_session.IsFaulted)
        {
            if (ReadPendingSource() is not null) throw new InvalidOperationException("Copy the visible unapplied source before reopening for persistence recovery.");
            return;
        }
        _codeBuffer!.Flush();
    }
    private void Changed(SessionResult result)
    {
        _refresh = true;
        // A source token and its visible buffer advance together before another native input can arrive.
        if (!_draft) BindSource(_human.InspectCode());
        _scene = _human.InspectScene(); _selected = _human.InspectSelection().NodeID; BindInspector();
    }
    private void BindSource(SessionCode source)
    {
        _source = source;
        var visible = SourceTextProjection.Normalize(source.Text);
        if (_code.Text == visible) { _boundText = visible; return; }
        var line = _code.GetCaretLine(); var column = _code.GetCaretColumn(); _binding = true;
        try { _code.Text = visible; _boundText = _code.Text; _code.SetCaretLine(line); _code.SetCaretColumn(column); }
        finally { _binding = false; }
    }
    private SessionCodeEdit? ReadPendingSource()
    {
        if (_binding || _draft || _code.Text == _boundText) return null;
        return new(_source.ID, _source.Revision, SourceTextProjection.Apply(_source.Text, _boundText, _code.Text));
    }
    private void RejectSource(SessionCodeEdit edit, Exception error)
    {
        _draft = true; ShowError(error); _refresh = true;
    }
    private void EditSource()
    {
        if (_binding || _draft) return;
        try { _codeBuffer!.Flush(); }
        catch (Exception error) { _draft = true; ShowError(error); }
        _refresh = true;
    }
    private void ApplyDraft()
    {
        Guard(() => { var current = _human.InspectCode(); Report(_human.Apply("Apply reviewed source draft", new SessionCodeEdit(current.ID, current.Revision, SourceTextProjection.Apply(_source.Text, _boundText, _code.Text)))); _draft = false; BindSource(_human.InspectCode()); });
    }
    private void DiscardDraft()
    {
        _draft = false;
        if (_session.IsFaulted) { _binding = true; try { _code.Text = _boundText; } finally { _binding = false; } }
        else Guard(() => BindSource(_human.InspectCode()));
        _refresh = true;
    }
    private void SelectNode()
    {
        if (_binding) return;
        Guard(() => { var selection = _human.InspectSelection(); _human.SelectNode(_nodes.GetSelected()?.GetMetadata<Guid>(0) ?? Guid.Empty, _human.InspectScene().Revision, selection.Revision); });
    }
    private void BindInspector()
    {
        var node = _scene.Nodes.SingleOrDefault(n => n.ID == _selected); _binding = true;
        try
        {
            _selection.Text = node is null ? "Select an Entity" : node.Name + " · r" + node.Revision;
            _x.Editable = _y.Editable = node is { CanEdit: true, Transform: not null };
            if (node?.Transform is { } pose) { _x.Value = pose.Origin.X; _y.Value = pose.Origin.Y; }
        }
        finally { _binding = false; }
    }
    private void EditPosition(double value, bool horizontal)
    {
        if (_binding) return;
        Guard(() =>
        {
            var node = _scene.Nodes.Single(n => n.ID == _selected); var pose = node.Transform!.Value;
            if (horizontal) pose.Origin.X = checked((float)value); else pose.Origin.Y = checked((float)value);
            Report(_human.Apply("Move " + node.Name, new SessionTransformEdit(node.ID, node.Revision, pose)));
        });
    }
    private void Save(SessionDocumentKind kind) => Guard(() =>
    {
        _codeBuffer!.Flush();
        if (kind == SessionDocumentKind.Code && _draft) throw new InvalidOperationException("Apply or discard the visible draft before saving source.");
        Report(_human.Save(kind, kind == SessionDocumentKind.Scene ? _scene.Revision : _source.Revision));
    });
    private void Reconcile(SessionDocumentKind kind, SessionExternalResolution resolution) => Guard(() => Report(_human.Refresh(kind, kind == SessionDocumentKind.Scene ? _scene.Revision : _source.Revision, resolution)));
    private void PlayHistory(bool redo, bool selected)
    {
        Guard(() =>
        {
            var current = _human.InspectHistory();
            var selectedID = selected && _history.Selected >= 0 && _history.Selected < _operations.Length ? _operations[_history.Selected].ID : Guid.Empty;
            var operation = selectedID == Guid.Empty ? current.LastOrDefault(o => o.IsApplied != redo) : current.SingleOrDefault(o => o.ID == selectedID);
            if (operation is null) return;
            Report(redo ? _human.Redo(operation.ID) : _human.Undo(operation.ID));
        });
    }
    private void RefreshViews()
    {
        var scene = _human.InspectScene(); var code = _human.InspectCode();
        var changed = _previewRevision != scene.Revision; _scene = scene; _selected = _human.InspectSelection().NodeID;
        _sceneStatus.Text = Persistence(scene.IsDirty, scene.HasExternalChange, scene.Revision);
        _codeStatus.Text = _draft ? "Unapplied draft — review the current session" : Persistence(code.IsDirty, code.HasExternalChange, code.Revision);
        if (_draft) _sessionCode.Text = SourceTextProjection.Normalize(code.Text); else BindSource(code);
        _retry.Visible = _discard.Visible = _conflictPane.Visible = _draft;
        _binding = true;
        try
        {
            if (changed)
            {
                var collapsed = _treeItems.Where(p => p.Value.Collapsed).Select(p => p.Key).ToHashSet();
                _nodes.Clear(); _treeItems.Clear(); var map = _treeItems;
                foreach (var node in scene.Nodes)
                {
                    var item = _nodes.CreateItem(node.ParentID == Guid.Empty ? null : map[node.ParentID]); item.SetText(0, node.Name); item.SetMetadata(0, node.ID); map[node.ID] = item; item.Collapsed = collapsed.Contains(node.ID);
                }
                try
                {
                    var next = _human.CreatePreview(scene.Revision); _instance?.Dispose(); _instance = next; _viewport.AddChild(next);
                    if (Tree is { } treeOwner) treeOwner.EditedSceneRoot = next;
                    _previewStatus.Text = "Authored scene · r" + scene.Revision;
                }
                catch (NotSupportedException error) { _instance?.Dispose(); _instance = null; _previewStatus.Text = error.Message; }
                _previewRevision = scene.Revision;
            }
            _nodes.DeselectAll(); if (_treeItems.TryGetValue(_selected, out var selected)) selected.Select(0);
            var selectedID = _history.Selected >= 0 && _history.Selected < _operations.Length ? _operations[_history.Selected].ID : Guid.Empty;
            _operations = _human.InspectHistory().ToArray(); _history.Clear();
            foreach (var operation in _operations) _history.AddItem((operation.IsApplied ? "Applied · " : "Undone · ") + operation.Origin + " · " + operation.Label);
            if (_operations.Length > 0) { var index = Array.FindIndex(_operations, o => o.ID == selectedID); _history.Select(index < 0 ? _operations.Length - 1 : index); }
        }
        finally { _binding = false; }
        BindInspector(); _refresh = false;
    }
    protected override void OnNotification(int what)
    {
        var reversed = IsNotificationReversed;
        if (!reversed) base.OnNotification(what);
        if (what == NotificationPostEnterTree && _instance is { IsInsideTree: true } && Tree is { } owner)
            owner.EditedSceneRoot = _instance;
        if (reversed) base.OnNotification(what);
    }

    protected override void OnProcess(double delta)
    {
        base.OnProcess(delta); _poll += delta;
        if (_session.IsFaulted)
        {
            _code.Editable = _x.Editable = _y.Editable = false; _build.Disabled = _run.Disabled = true;
            _retry.Visible = false; _discard.Visible = _draft; _feedback.Text = "Persistence recovery required — copy any unapplied source, then reopen the project.";
            _agent?.Retire(); return;
        }
        Guard(() => _agent?.Poll());
        Guard(() => { var jobs = _human.InspectJobs(); foreach (var shared in jobs) if (!_jobs.Contains(shared)) _jobs.Add(shared); _job = jobs.LastOrDefault(); });
        if (_layoutPhase == 0 && _body.Size.X > 800) { _body.SplitOffset = (int)(220 - _body.Size.X / 2); _layoutPhase++; }
        else if (_layoutPhase == 1) { _main.SplitOffset = (int)(_main.Size.X / 2 - 280); _layoutPhase++; }
        if (_refresh || _poll > 1) { _poll = 0; Guard(RefreshViews); }
        _build.Disabled = _run.Disabled = _draft || _job is { Completion.IsCompleted: false };
        _cancel.Disabled = _job is not { Completion.IsCompleted: false };
        if (_job is { } job)
        {
            Guard(() => _jobStatus.Text = job.Stage + (job.IsStale ? " · stale input" : ""));
            var running = !job.Completion.IsCompleted;
            if (!running && job != _observedJob)
            {
                var result = job.Completion.GetAwaiter().GetResult();
                _feedback.Text = result.Status == SessionJobStatus.Completed ? "Snapshot completed; artifacts retain their captured input identity." : string.Join(" | ", result.Diagnostics.Select(d => d.Code + ": " + d.Message));
                _feedback.TooltipText = _feedback.Text;
                var image = result.Artifacts.LastOrDefault(a => a.Kind == "image");
                if (image is not null) Guard(() => { var next = ResourceLoader.Load<ImageTexture>(image.Path, ResourceLoader.CacheMode.Ignore); _capture.Texture = next; _captureImage?.Dispose(); _captureImage = next; _capturedJob = job; });
                _observedJob = job;
            }
        }
        if (_capturedJob is { } captured) Guard(() => _captureStatus.Text = "Snapshot " + captured.SnapshotID.ToString("N")[..8] + " · " + captured.Status + (captured.IsStale ? " · stale" : " · current"));
    }
    private void StartJob(SessionJobKind kind)
    {
        Guard(() =>
        {
            if (_job is { Completion.IsCompleted: false }) return;
            _codeBuffer!.Flush();
            if (_draft) throw new InvalidOperationException("Apply or discard the visible draft before building a snapshot.");
            var mode = _mode.Selected; var steps = checked((int)_frames.Value);
            var options = new SessionJobOptions(kind, _project.ProjectFile, _sdk, mode == 0 ? SessionRunMode.Headless : SessionRunMode.Rendered,
                Steps: steps, Backend: mode == 2 ? "compatibility" : "gpu", CaptureSteps: mode == 0 || kind == SessionJobKind.Build ? [] : [steps]);
            _job = _human.StartJob(options, _scene.Revision, _source.Revision); _jobs.Add(_job); _feedback.Text = "Snapshot queued; authoring remains available.";
        });
    }
    private void ClearResults()
    {
        Guard(() =>
        {
            if (_jobs.Any(j => !j.Completion.IsCompleted)) throw new InvalidOperationException("Cancel or finish the active job before clearing results.");
            _capture.Texture = null; _captureImage?.Dispose(); _captureImage = null;
            foreach (var job in _jobs) { job.DisposeAsync().AsTask().GetAwaiter().GetResult(); job.RemoveResult(); }
            _jobs.Clear(); _job = _capturedJob = _observedJob = null; _jobStatus.Text = "No snapshot job"; _captureStatus.Text = "No completed capture";
        });
    }
    internal Task RetireJobsAsync() => Task.WhenAll(_jobs.Select(j => j.DisposeAsync().AsTask()).Append(_agent?.Retirement ?? Task.CompletedTask));
    private void Report(SessionResult result) { _feedback.AddThemeColorOverride("font_color", Color.FromHTML("#F9F3EE")); _feedback.Text = result.Diagnostics.Count > 0 ? string.Join(" | ", result.Diagnostics.Select(d => d.Message)) : result.Kind + " · " + result.Origin; _refresh = true; }
    private void Guard(Action action) { try { action(); } catch (Exception error) { ShowError(error); } }
    private void ShowError(Exception error) { _feedback.Text = "Error: " + error.Message; _feedback.TooltipText = _feedback.Text; _feedback.AddThemeColorOverride("font_color", Color.FromHTML("#FFB4C6")); _refresh = true; }
    private static string Persistence(bool dirty, bool external, ulong revision) => (dirty ? "Unsaved" : "Saved") + (external ? " · disk conflict" : "") + " · r" + revision;
    private static SpinBox Coordinate(string name) => new() { Name = name, MinValue = -1000000, MaxValue = 1000000, AllowGreater = true, AllowLesser = true, Step = .1, CustomMinimumSize = new(140, 32) };
    private static VBoxContainer Column(string title) { var box = new VBoxContainer { Separation = 8, SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill }; box.AddChild(new Label(title)); return box; }
    private Control Panel(Control child)
    {
        var panel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        var style = new StyleBoxFlat { BGColor = Color.FromHTML("#2D2235"), BorderColor = Color.FromHTML("#5D4968") }; style.SetContentMarginAll(12); style.SetBorderWidthAll(1);
        _resources.Add(style); panel.AddThemeStyleBoxOverride("panel", style); panel.AddChild(child); return panel;
    }
    private static Button AddButton(Node parent, string text, string name, Action action)
    {
        var button = new Button { Name = name, Text = text, FocusMode = FocusMode.All, CustomMinimumSize = new(0, 32) }; button.Pressed += action; parent.AddChild(button); return button;
    }
    protected override void Dispose(bool disposing)
    {
        if (!disposing) { base.Dispose(false); return; }
        var errors = new List<Exception>();
        void Cleanup(Action action) { try { action(); } catch (Exception error) { errors.Add(error); } }
        _session.Changed -= Changed;
        Cleanup(() => _agent?.Retire());
        if (!_draft && !_session.IsFaulted) Cleanup(() => _codeBuffer?.Flush());
        foreach (var job in _jobs) Cleanup(job.Cancel);
        Cleanup(_human.Dispose);
        Cleanup(() => base.Dispose(true));
        Cleanup(() => _captureImage?.Dispose());
        foreach (var resource in _resources) Cleanup(resource.Dispose);
        if (errors.Count > 0) throw new AggregateException(errors);
    }
}

/// <summary>Routes editor history shortcuts through shared operations before ordinary text editing.</summary>
internal sealed class SessionCodeEditor : CodeEdit
{
    internal Action<bool>? HistoryRequested;
    internal SessionCodeEditor()
    {
        // shortcut: source context menus await session-owned commands; toolbar and keyboard use shared history now.
        ContextMenuEnabled = false;
    }
    protected override void OnGUIInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventKey { Pressed: true, Echo: false } key && (key.ControlPressed || key.MetaPressed) && key.Keycode is Key.Z or Key.Y)
        { HistoryRequested?.Invoke(key.Keycode == Key.Y || key.ShiftPressed); AcceptEvent(); return; }
        base.OnGUIInput(inputEvent);
    }
}
