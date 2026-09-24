namespace Electron2D;

public partial class Node
{
    /// <summary>Replaces this node in its parent while moving its children to a detached replacement.</summary>
    /// <param name="node">The live parentless replacement.</param>
    /// <param name="keepGroups">Whether to copy this node's group memberships and persistence flags.</param>
    /// <remarks>This node remains alive and detached. The replacement keeps its existing children, then receives this
    /// node's children in order. Owner references to this node become references to the replacement. An active
    /// SceneTree root cannot be replaced because its identity is stable. An active CurrentScene or EditedSceneRoot
    /// selection clears when this node exits; callers may select the replacement afterward. Existing typed event
    /// subscriptions remain with the node they target. Callback failures are collected after structural progress.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="node"/> is this node.</exception>
    /// <exception cref="InvalidOperationException">The replacement is attached, names conflict, the active root is
    /// selected, a protected lifecycle is running, or the caller is off the scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">Either node is disposed.</exception>
    /// <exception cref="AggregateException">One or more structural or lifecycle callbacks failed after mutation began.</exception>
    public void ReplaceBy(Node node, bool keepGroups = false)
    {
        EnsureMutable();
        ArgumentNullException.ThrowIfNull(node);
        if (ReferenceEquals(node, this)) throw new ArgumentException("A node cannot replace itself.", nameof(node));
        node.EnsureMutable();
        if (node.Parent is not null || node.Tree is not null)
            throw new InvalidOperationException("A replacement must be detached and parentless.");
        if (Parent is null && Tree is not null)
            throw new InvalidOperationException("An active SceneTree root has stable identity.");
        if (_isEnteringTree || _isMakingReady || _isExitingTree ||
            node._isEnteringTree || node._isMakingReady || node._isExitingTree)
            throw new InvalidOperationException("Replacement is unavailable during tree lifecycle delivery.");

        var parent = Parent;
        parent?.EnsureMutable();
        parent?.EnsureChildOrderMutable();
        EnsureChildOrderMutable();
        node.EnsureChildOrderMutable();
        if (node is Viewport && parent is not null)
            throw new NotSupportedException("Child viewports require multiwindow or offscreen rendering support.");
        if (parent is not null && parent._children.Any(sibling => !ReferenceEquals(sibling, this) && sibling.Name == node.Name))
            throw new InvalidOperationException("The replacement name conflicts with a sibling.");
        if (_children.Any(child => node._children.Any(existing => existing.Name == child.Name)))
            throw new InvalidOperationException("A moved child name conflicts with a replacement child.");

        var owner = _owner;
        var owned = EnumerateDepthFirst()
            .Where(descendant => !ReferenceEquals(descendant, this) && descendant._owner is not null)
            .Select(descendant => (Node: descendant, Owner: descendant._owner!))
            .ToArray();
        var index = GetIndex();
        if (keepGroups)
            foreach (var (group, persistent) in _groups)
                node.AddToGroup(group, persistent);

        List<Exception>? errors = null;
        if (parent is not null)
        {
            try { parent.RemoveChildCore(this); }
            catch (Exception error) { CollectException(ref errors, error); }
            if (IsDisposed)
            {
                CollectException(ref errors, new ObjectDisposedException(GetType().FullName));
                ThrowCollected("The original node was disposed during replacement.", errors);
            }
            if (Parent is not null)
            {
                ThrowCollected("The original node could not leave its parent.", errors);
                throw new InvalidOperationException("The original node could not leave its parent.");
            }

            try { parent.InsertChild(node, index); }
            catch (Exception error) { CollectException(ref errors, error); }
            if (!ReferenceEquals(node.Parent, parent))
            {
                try { parent.InsertChild(this, index); }
                catch (Exception error) { CollectException(ref errors, error); }
                ThrowCollected("The replacement could not enter the parent.", errors);
                throw new InvalidOperationException("The replacement could not enter the parent.");
            }
        }

        try { ReplacingBy?.Invoke(node); }
        catch (Exception error) { CollectException(ref errors, error); }
        if (IsDisposed || node.IsDisposed)
        {
            CollectException(ref errors, new ObjectDisposedException(IsDisposed ? GetType().FullName : node.GetType().FullName));
            ThrowCollected("A node was disposed during replacement.", errors);
        }
        if (Parent is not null || (parent is not null && !ReferenceEquals(node.Parent, parent)))
        {
            CollectException(ref errors, new InvalidOperationException("A replacement callback changed the replacement topology."));
            ThrowCollected("A replacement callback changed the replacement topology.", errors);
        }

        foreach (var child in _children.ToArray())
        {
            if (!ReferenceEquals(child.Parent, this)) continue;
            try { RemoveChildCore(child); }
            catch (Exception error) { CollectException(ref errors, error); }
            if (child.Parent is not null)
            {
                CollectException(ref errors, new InvalidOperationException($"Child '{child.Name}' could not leave the original node."));
                continue;
            }
            try { node.AddChild(child); }
            catch (Exception error)
            {
                CollectException(ref errors, error);
                if (child.Parent is null)
                    try { AddChild(child); }
                    catch (Exception restoreError) { CollectException(ref errors, restoreError); }
            }
        }

        if (ReferenceEquals(node.Parent, parent) || parent is null)
        {
            node.Owner = owner;
            foreach (var (descendant, previousOwner) in owned)
                if (node.IsAncestorOf(descendant) &&
                    (ReferenceEquals(previousOwner, this) || previousOwner.IsAncestorOf(descendant)))
                    descendant.Owner = ReferenceEquals(previousOwner, this) ? node : previousOwner;
                else if (IsAncestorOf(descendant) && previousOwner.IsAncestorOf(descendant))
                    descendant.Owner = previousOwner;
            node._sceneFilePath = _sceneFilePath;
            if (_ownedSceneResources is { Count: > 0 } resources)
            {
                foreach (var resource in resources) resource.ReassignLocalScene(this, node);
                node._ownedSceneResources ??= [];
                node._ownedSceneResources.AddRange(resources);
                _ownedSceneResources = null;
            }
        }

        ThrowCollected("One or more node-replacement callbacks failed.", errors);
    }
}
