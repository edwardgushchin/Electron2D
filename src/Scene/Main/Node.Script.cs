namespace Electron2D;

public partial class Node
{
    private bool _scriptDisposalPrepared;
    internal NodeScriptBinding? ScriptCallbacks { get; private set; }

    /// <summary>Installs a fresh typed callback set during this owner's registered live Script Bind.</summary>
    /// <typeparam name="TNode">Compatible receiver type.</typeparam><param name="callbacks">Single-use callbacks and their state-retirement function.</param>
    /// <remarks>Uses existing tree, frame, input, picking and integration dispatch. Successful installation transfers the
    /// callback targets' retirement to this binding; project Clear precedes automatic slot removal and Retired.
    /// Ready enables present frame/input slots before its callback. Installation after readiness enables them without
    /// rerunning Ready. Later explicit switch changes remain authoritative; retirement does not reset those switches.
    /// No callbacks run from this method. It rejects foreign, reentrant or flowed-worker installation.</remarks>
    /// <exception cref="ArgumentNullException">The set is null.</exception>
    /// <exception cref="ArgumentException">This owner is incompatible with the receiver role.</exception>
    /// <exception cref="InvalidOperationException">No matching Bind is active, a set is already installed, the set was used or mutation is unavailable.</exception>
    /// <exception cref="ObjectDisposedException">The owner is disposed.</exception>
    public void SetScriptCallbacks<TNode>(NodeScriptCallbacks<TNode> callbacks) where TNode : Node
    {
        ArgumentNullException.ThrowIfNull(callbacks); EnsureScriptCallbackInstallation(); ValidateMutation();
        if (this is not TNode owner) throw new ArgumentException("The callback receiver role is incompatible with this node.", nameof(callbacks));
        if (ScriptCallbacks is not null) throw new InvalidOperationException("This binding already has a callback set.");
        callbacks.Issue(); var binding = new NodeScriptBinding(); callbacks.Populate(binding, owner);
        ScriptCallbacks = binding; BoundScriptNotification = binding.Notification;
        if (IsNodeReady) EnableScriptCallbacks();
    }

    private void EnableScriptCallbacks()
    {
        var callbacks = ScriptCallbacks; if (callbacks is null) return;
        if (callbacks.Process is not null) ProcessEnabled = true;
        if (callbacks.PhysicsProcess is not null) PhysicsProcessEnabled = true;
        if (callbacks.Input is not null) InputEnabled = true;
        if (callbacks.ShortcutInput is not null) ShortcutInputEnabled = true;
        if (callbacks.UnhandledKeyInput is not null) UnhandledKeyInputEnabled = true;
        if (callbacks.UnhandledInput is not null) UnhandledInputEnabled = true;
    }

    internal Exception? RetireScriptCallbacks()
    {
        var callbacks = ScriptCallbacks; ScriptCallbacks = null; BoundScriptNotification = null;
        try { callbacks?.Retired?.Invoke(); return null; }
        catch (Exception error) { return error; }
    }

    internal void PrepareScriptCallbackDisposal()
    {
        List<Exception>? errors = null; PrepareNodeDisposal(ref errors);
        ThrowCollected("Script owner tree detachment failed.", errors);
    }

    private void PrepareNodeDisposal(ref List<Exception>? errors)
    {
        try { DisposeSceneConnections(); } catch (Exception error) { (errors ??= []).Add(error); }
        ReleaseOwnedNodes(); SetSceneOwner(null); Interlocked.Or(ref _queuedForDeletion, 4);
        try
        {
            if (Parent is not null) Parent.RemoveChildCore(this);
            else Tree?.DetachSubtree(this);
        }
        catch (Exception error) { CollectException(ref errors, error); }
        _scriptDisposalPrepared = true;
    }
}
