namespace Electron2D.Editor;

/// <summary>Owns project switching and shared sessions around the branded editor window.</summary>
internal sealed class EditorApplication : IDisposable
{
    private readonly Font _regular;
    private readonly SystemFont _mono = new() { FontNames = ["monospace"] };
    private readonly Label _message = new("");
    private readonly LineEdit _directory = new() { Name = "ProjectDirectory", PlaceholderText = "Project directory", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, FocusMode = FocusMode.All };
    private readonly List<ProjectWorkspace> _retiring = [];
    private ProjectWorkspace? _workspace;
    private ProjectSession? _session;
    private string? _projectRoot;
    private bool _disposed;
    internal Window Window { get; }
    internal EditorApplication(Texture mark, Texture sparkle, Texture icon, Font semibold, Font regular, string? projectDirectory)
    {
        _regular = regular;
        Window = EditorScene.CreateWindow(mark, sparkle, icon, semibold, regular);
        Window.MinSize = new(1100, 680);
        Window.Ready += _ => Window.Tree!.AutoAcceptQuit = false;
        Window.CloseRequested += () =>
        {
            try { _workspace?.PrepareClose(); Window.Tree!.Quit(); }
            catch (Exception error) { _message.Text = "Cannot close project: " + error.Message; }
        };
        var shell = new VBoxContainer { Name = "ProjectShell", Separation = 8 }; Window.AddChild(shell); shell.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var bar = new HBoxContainer { Name = "ProjectBar", Separation = 8, CustomMinimumSize = new(0, 44) }; shell.AddChild(bar);
        bar.AddChild(new Label("Project")); bar.AddChild(_directory);
        var open = new Button { Name = "OpenProject", Text = "Open", FocusMode = FocusMode.All, CustomMinimumSize = new(80, 36) }; bar.AddChild(open);
        var create = new Button { Name = "CreateProject", Text = "Create here", FocusMode = FocusMode.All, CustomMinimumSize = new(100, 36) }; bar.AddChild(create);
        shell.AddChild(_message); _message.ClipText = true;
        open.Pressed += () => Open(shell, false); create.Pressed += () => Open(shell, true); _directory.TextSubmitted += _ => Open(shell, false);
        if (projectDirectory is not null) { _directory.Text = projectDirectory; Open(shell, false); }
        else _message.Text = "Open a source project, or enter a new directory and choose Create here.";
    }
    private void Open(VBoxContainer shell, bool create)
    {
        try
        {
            _workspace?.PrepareClose();
            var project = create ? SessionProject.Create(_directory.Text, AppContext.BaseDirectory) : SessionProject.Open(_directory.Text);
            if (_projectRoot == project.Root)
            {
                if (_session is { IsFaulted: false }) { _message.Text = project.Name + " is already open."; return; }
                RetireWorkspace();
            }
            var session = project.OpenSession();
            ProjectWorkspace workspace;
            try { workspace = new(project, session, AppContext.BaseDirectory, _regular, _mono); }
            catch { session.Dispose(); throw; }
            RetireWorkspace();
            _session = session; _projectRoot = project.Root; _workspace = workspace; shell.AddChild(workspace); workspace.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            workspace.SizeFlagsVertical = Control.SizeFlags.ExpandFill; workspace.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            if (Window.GetNodeOrNull("Brand") is Control brand) brand.Visible = false;
            _message.Text = project.Name + " · shared scene/code session";
        }
        catch (Exception error) { _message.Text = "Cannot open project: " + error.Message; }
    }
    private void RetireWorkspace()
    {
        var workspace = _workspace; var session = _session; _workspace = null; _session = null; _projectRoot = null;
        try { workspace?.Dispose(); }
        finally { if (workspace is not null) _retiring.Add(workspace); session?.Dispose(); }
    }
    internal Task RetireJobsAsync() => Task.WhenAll(_retiring.Concat(_workspace is null ? [] : new[] { _workspace }).Select(w => w.RetireJobsAsync()));
    public void Dispose()
    {
        if (_disposed) return; _disposed = true;
        try { RetireWorkspace(); }
        finally { try { Window.Dispose(); } finally { _mono.Dispose(); } }
    }
}
