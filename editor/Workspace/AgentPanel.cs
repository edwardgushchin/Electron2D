using System.Text;

namespace Electron2D.Editor;

/// <summary>Owner-thread conversation/control surface; executor tasks never touch native controls.</summary>
internal sealed class AgentPanel : VBoxContainer
{
    private const string Actor = "editor-agent";
    private readonly ProjectSession _session;
    private readonly SessionClient _human;
    private readonly SessionProject _project;
    private readonly ISessionAgentExecutor _executor;
    private readonly SessionOperationsHost? _host;
    private readonly Label _status = new("Disconnected") { Name = "AgentStatus", ClipText = true };
    private readonly Label _context = new("") { Name = "AgentContext", ClipText = true };
    private readonly TextEdit _conversation = new() { Name = "AgentConversation", Editable = false, WrapMode = TextEdit.LineWrappingMode.Boundary, CustomMinimumSize = new(240, 180), SizeFlagsVertical = SizeFlags.ExpandFill, ClipContents = true };
    private readonly TextEdit _task = new() { Name = "AgentTask", CustomMinimumSize = new(240, 110), ClipContents = true, WrapMode = TextEdit.LineWrappingMode.Boundary, FocusMode = FocusMode.All };
    private readonly CheckBox _publish = new() { Name = "AgentAllowSave", Text = "Allow agent to save files" };
    private readonly CheckBox _discard = new() { Name = "AgentAllowDiscard", Text = "Allow agent to reload disk" };
    private readonly Button _connect, _send, _stop, _undo;
    private readonly StringBuilder _text = new();
    private Task _pending = Task.CompletedTask, _retirement = Task.CompletedTask;
    private string? _capability;
    private bool _retired, _message;
    internal AgentPanel(SessionProject project, ProjectSession session, SessionClient human, string sdk, Func<ISessionAgentExecutor>? factory = null)
    {
        Name = "Agent"; Separation = 8; SizeFlagsHorizontal = SizeFlagsVertical = SizeFlags.ExpandFill;
        _session = session; _human = human; _project = project; _executor = factory?.Invoke() ?? new CodexAgentExecutor();
        _conversation.AddThemeColorOverride("font_readonly_color", Color.FromHTML("#F9F3EE"));
        AddChild(_status); AddChild(_context); AddChild(_conversation);
        AddChild(_publish); AddChild(_discard);
        _publish.TooltipText = "Selected before connection: permits publishing shared documents to disk.";
        _discard.TooltipText = "Selected before connection: permits discarding unsaved documents by reloading disk.";
        AddChild(new Label("Task · current selection is attached")); AddChild(_task);
        var actions = new HBoxContainer { Separation = 6 }; AddChild(actions);
        _connect = Button(actions, "Connect", "AgentConnect", ConnectOrDisconnect);
        _send = Button(actions, "Send", "AgentSend", Submit);
        _stop = Button(actions, "Stop", "AgentStop", Stop);
        _undo = Button(this, "Undo last agent change", "AgentUndo", Undo);
        try { _host = session.StartOperations(project, sdk); }
        catch (Exception error) { Append("Unavailable", error.Message); _status.Text = "Operations unavailable"; }
        Append("Profile", "Codex CLI 0.163.0 · existing login · shared operations. Save and reload are disabled unless selected before connecting. Captures and job diagnostics are in Inspector.");
    }
    internal ISessionAgentExecutor Executor => _executor;
    internal Task Retirement => _retirement;
    private static Button Button(Node parent, string text, string name, Action action)
    {
        var button = new Button { Name = name, Text = text, FocusMode = FocusMode.All, CustomMinimumSize = new(0, 32) };
        button.Pressed += action; parent.AddChild(button); return button;
    }
    private void Append(string speaker, string text)
    {
        if (speaker == "Agent" && _message) _text.Append(text);
        else { if (_text.Length > 0) _text.Append('\n'); _text.Append(speaker).Append(": ").Append(text); }
        _message = speaker == "Agent";
        if (_text.Length > 65536) { var remove = _text.Length - 57344; if (char.IsLowSurrogate(_text[remove])) remove++; _text.Remove(0, remove); }
        _conversation.Text = _text.ToString(); _conversation.ClearUndoHistory();
        _conversation.SetCaretLine(Math.Max(0, _conversation.GetLineCount() - 1));
    }
    private void Begin(Func<Task> action)
    {
        if (!_pending.IsCompleted || _retired) return;
        try { _pending = action(); }
        catch (Exception error) { Append("Error", error.Message); }
    }
    private void ConnectOrDisconnect()
    {
        if (_executor.IsConnected || _executor.Status == SessionAgentStatus.Connecting) { Disconnect(); return; }
        Begin(() =>
        {
            if (_host is null) throw new InvalidOperationException("Local operations are unavailable.");
            if (_capability is not null) _host.RevokeClient(_capability);
            _capability = _host.GrantClient(Actor, _publish.ButtonPressed, _discard.ButtonPressed);
            var capability = _capability;
            return Reconnect(capability);
        });
    }
    private async Task Reconnect(string capability)
    {
        await _executor.DisconnectAsync().ConfigureAwait(false);
        await _executor.ConnectAsync(_project.Root, Actor, capability).ConfigureAwait(false);
    }
    private void Disconnect()
    {
        if (_capability is not null) { _host?.RevokeClient(_capability); _capability = null; }
        // Disconnect cancels setup even when the connection task has not completed yet.
        _pending = DisconnectAndObserve(_pending);
    }
    private async Task DisconnectAndObserve(Task previous)
    {
        await _executor.DisconnectAsync().ConfigureAwait(false);
        try { await previous.ConfigureAwait(false); } catch (Exception) { /* The owner reports the explicit disconnect. */ }
    }
    private void Submit()
    {
        var task = _task.Text;
        if (string.IsNullOrWhiteSpace(task)) return;
        Begin(() =>
        {
            if (task.Length > 14000) throw new ArgumentOutOfRangeException(nameof(task), "Keep the task within 14000 UTF-16 characters.");
            var scene = _human.InspectScene(); var code = _human.InspectCode(); var selection = _human.InspectSelection();
            Append("You", task); _task.Text = ""; _task.ClearUndoHistory();
            if (_capability is null) throw new InvalidOperationException("Reconnect the executor before submitting a new task.");
            _host!.ResumeClientWork(_capability);
            return _executor.SubmitAsync(task + $"\n\nObserved editor context (refresh with operations): scene {scene.ID} r{scene.Revision}, code {code.ID} r{code.Revision}, selection {selection.NodeID} r{selection.Revision}. These are unsaved shared documents; no save/reload is implied.");
        });
    }
    private void Stop()
    {
        if (_capability is not null) _host?.CancelClientWork(_capability);
        _pending = InterruptAndObserve(_pending);
    }
    private async Task InterruptAndObserve(Task previous)
    {
        try { await _executor.InterruptAsync().ConfigureAwait(false); }
        finally { try { await previous.ConfigureAwait(false); } catch (Exception) { /* Submission failures are already explicit executor events. */ } }
    }
    private void Undo()
    {
        try
        {
            var operation = _human.InspectHistory().LastOrDefault(o => o.IsApplied && o.Origin.StartsWith("external:" + Actor + ":", StringComparison.Ordinal));
            if (operation is null) return;
            var result = _human.Undo(operation.ID); Append("History", result.Kind + " · " + operation.Label);
        }
        catch (Exception error) { Append("Conflict", error.Message); }
    }
    internal void Poll()
    {
        if (_retired) return;
        if (_executor.Status == SessionAgentStatus.Failed && _capability is not null)
        {
            _host?.CancelClientWork(_capability);
            if (!_executor.IsConnected) { _host?.RevokeClient(_capability); _capability = null; _pending = DisconnectAndObserve(_pending); }
        }
        _host?.Pump();
        if (_host is not null) foreach (var item in _host.DrainActivity()) Append(item.Succeeded ? "Activity" : "Conflict", (item.Succeeded ? "Finished · electron2d/" : item.ErrorCode + " · electron2d/") + item.Method + " · " + item.Origin);
        if (_pending.IsFaulted) { Append("Error", _pending.Exception!.GetBaseException().Message); _pending = Task.CompletedTask; }
        else if (_pending.IsCanceled) { Append("Status", "Request cancelled; inspect shared history for committed changes."); _pending = Task.CompletedTask; }
        foreach (var item in _executor.DrainEvents()) Append(item.Kind == SessionAgentEventKind.Message ? "Agent" : item.Kind.ToString(), item.Text);
        var status = _executor.Status; var connected = _executor.IsConnected; var busy = !_pending.IsCompleted;
        _status.Text = _executor.Name + " · " + status + (_executor.Model.Length > 0 ? " · " + _executor.Model : ""); _status.TooltipText = _status.Text;
        var selection = _human.InspectSelection();
        _context.Text = selection.NodeID == Guid.Empty ? "No selection" : "Selected · " + _human.InspectScene().Nodes.Single(n => n.ID == selection.NodeID).Name;
        _connect.Text = connected || status == SessionAgentStatus.Connecting ? "Disconnect" : "Connect";
        _connect.Disabled = _host is null || busy && status != SessionAgentStatus.Connecting;
        _send.Disabled = busy || !connected || status is not (SessionAgentStatus.Ready or SessionAgentStatus.Failed);
        _stop.Disabled = status != SessionAgentStatus.Running;
        _publish.Disabled = _discard.Disabled = connected || busy;
        _undo.Disabled = busy || !_human.InspectHistory().Any(o => o.IsApplied && o.Origin.StartsWith("external:" + Actor + ":", StringComparison.Ordinal));
    }
    internal void Retire()
    {
        if (_retired) return; _retired = true;
        if (_capability is not null) _host?.RevokeClient(_capability);
        _host?.Dispose(); _retirement = FinishRetirement(_pending);
    }
    private async Task FinishRetirement(Task pending)
    {
        await _executor.DisposeAsync().ConfigureAwait(false);
        try { await pending.ConfigureAwait(false); } catch (Exception) { /* Closing explicitly cancels pending setup/submission. */ }
        if (_host is not null) await _host.Completion.ConfigureAwait(false);
    }
    protected override void Dispose(bool disposing) { if (disposing) Retire(); base.Dispose(disposing); }
}
