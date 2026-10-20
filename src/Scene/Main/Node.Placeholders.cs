namespace Electron2D;

public partial class Node
{
    private bool _sceneInstanceLoadPlaceholder;

    /// <summary>Gets whether scene capture stores this instance as a deferred scene path.</summary>
    /// <returns>False initially; true when runtime loading should create an InstancePlaceholder.</returns>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    /// <exception cref="InvalidOperationException">Attached access is outside the owner or current processing group.</exception>
    public bool GetSceneInstanceLoadPlaceholder()
    {
        ThrowIfDisposed(); Tree?.EnsureProcessingAccess(this); return _sceneInstanceLoadPlaceholder;
    }

    /// <summary>Changes the deferred loading policy used when packing a scene instance.</summary>
    /// <param name="loadPlaceholder">Whether to retain only its scene path and pending values.</param>
    /// <remarks>The flag changes future capture, not the live node's type. A deferred instance needs a nonempty
    /// scene file path. Setting it on an ordinary node does not manufacture a scene identity.</remarks>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    /// <exception cref="InvalidOperationException">Authoring is unavailable.</exception>
    public void SetSceneInstanceLoadPlaceholder(bool loadPlaceholder)
    {
        EnsureMutable(); EnsureEditorAuthoringCommit(); _sceneInstanceLoadPlaceholder = loadPlaceholder;
    }

    internal void EnsurePlaceholderAccess() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    internal void EnsurePendingPropertyMutation() { EnsureMutable(); EnsureEditorAuthoringCommit(); }
    internal void NotifyPendingPropertyChanged() => EmitEditorStateChanged();
    internal void CopySceneFileLeasesTo(Node target)
    {
        if (_sceneFileLeases is not null)
            foreach (var lease in _sceneFileLeases) target.AdoptSceneFileLease(ResourceFileOwnership.RetainLease(lease));
    }
    internal void EnsurePlaceholderCreation()
    {
        EnsureMutable(); EnsureChildOrderMutable(); Parent?.EnsureMutable(); Parent?.EnsureChildOrderMutable();
        if (_isEnteringTree || _isMakingReady || _isExitingTree || _hasPendingTreeExit || IsInternalChild)
            throw new InvalidOperationException("A placeholder cannot create a sibling during protected hierarchy delivery.");
    }

    internal void AttachPlaceholderInstance(Node instance, Node parent, int index, bool replace)
    {
        var tree = Tree!; var owner = SceneOwner; List<Exception>? errors = null;
        if (replace)
        {
            try { parent.RemoveChild(this); } catch (Exception error) { CollectException(ref errors, error); }
            if (Parent is not null || IsDisposed)
            {
                CollectException(ref errors, new InvalidOperationException("The placeholder could not leave its parent."));
                ThrowCollected("Placeholder replacement failed.", errors);
            }
        }
        try { parent.AddChild(instance); } catch (Exception error) { CollectException(ref errors, error); }
        if (instance.Parent != parent)
        {
            if (replace && !IsDisposed && Parent is null && Tree is null)
            {
                try { parent.InsertChild(this, parent._internalFrontCount + Math.Min(index, parent.GetChildCount())); Owner = owner; }
                catch (Exception error) { CollectException(ref errors, error); }
            }
            CollectException(ref errors, new InvalidOperationException("The created instance could not enter its parent."));
            ThrowCollected("Placeholder replacement failed.", errors);
        }
        try { parent.MoveChild(instance, Math.Min(index, parent.GetChildCount() - 1)); }
        catch (Exception error) { CollectException(ref errors, error); }
        if (replace && !IsDisposed && Parent is null)
            try { QueueFree(); tree.QueueForDeletion(this, GetQueuedDeletionTicket()); }
            catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Placeholder lifecycle delivery failed after structural progress.", errors);
    }
}
