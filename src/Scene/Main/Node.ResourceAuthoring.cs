namespace Electron2D;

public partial class Node
{
    internal void EnsureIndexedMutation() => EnsureMutable();
    /// <summary>Resolves a node path and its successive resource-valued tooling properties.</summary>
    /// <param name="path">A node path followed by optional colon-separated property names; an omitted node path addresses this node.</param>
    /// <returns>The borrowed node, last borrowed resource, and remaining colon-prefixed property subpath. A missing node/property returns a null node; a previously resolved resource is retained in the result.</returns>
    /// <remarks>Resolution stops at the first existing non-resource or null property. Remaining components are returned without validation. Property names use the exposed typed descriptor schema, including custom derived nodes and resources; no reflection or universal value container is used.</remarks>
    /// <exception cref="ArgumentException">The path is blank or contains an empty property component.</exception>
    /// <exception cref="ArgumentNullException">The path is null.</exception>
    /// <exception cref="ObjectDisposedException">This node or a resolved resource is disposed.</exception>
    /// <exception cref="InvalidOperationException">An attached query runs off-owner or exposes an invalid property schema.</exception>
    /// <exception cref="Exception">A property getter or schema callback fails.</exception>
    public (Node? Node, Resource? Resource, string RemainingPath) GetNodeAndResource(string path)
    {
        ThrowIfDisposed(); Tree?.EnsureOwnerThread(); ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (path.EndsWith(':')) path = path[..^1];
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var separator = path.IndexOf(':');
        if (separator < 0) return (GetNodeOrNull(path), null, "");
        var names = path[(separator + 1)..].Split(':');
        if (names.Any(static name => name.Length == 0)) throw new ArgumentException("Resource property components cannot be empty.", nameof(path));
        var node = GetNodeOrNull(separator == 0 ? "." : path[..separator]);
        if (node is null) return (null, null, "");
        Resource? resource = null;
        ElectronObject owner = node;
        for (var index = 0; index < names.Length; index++)
        {
            var property = owner.GetPropertyList().FirstOrDefault(property => StringComparer.Ordinal.Equals(property.Name, names[index]));
            if (property is null) return (null, resource, "");
            var next = property.ReadResource(owner);
            if (next is null) return (node, resource, ":" + string.Join(':', names, index, names.Length - index));
            if (next.IsDisposed) throw new ObjectDisposedException(next.GetType().FullName);
            resource = next; owner = next;
        }
        return (node, resource, "");
    }
    /// <summary>Reports whether node and resource-property traversal succeeds.</summary>
    /// <param name="path">A node path with optional colon-separated property components.</param>
    /// <returns>True when the node exists and every traversed property exists. The first non-resource or null property and its remaining components count as a successful remainder.</returns>
    /// <exception cref="ArgumentException">The path is invalid.</exception>
    /// <exception cref="ArgumentNullException">The path is null.</exception>
    /// <exception cref="ObjectDisposedException">This node or a resolved resource is disposed.</exception>
    /// <exception cref="InvalidOperationException">An attached query runs off-owner or exposes an invalid schema.</exception>
    /// <exception cref="Exception">A property getter or schema callback fails.</exception>
    public bool HasNodeAndResource(string path) => GetNodeAndResource(path).Node is not null;

    /// <summary>Invokes a typed operation on this node and all descendants, including internal children.</summary>
    /// <param name="operation">The synchronous operation.</param><param name="parentFirst">Whether each parent executes before its descendants; false visits children first.</param>
    /// <remarks>Direct child insertion/removal/reordering and node disposal are blocked while that node is traversed. Properties may change. Callback failures do not skip remaining nodes and are combined afterward.</remarks>
    /// <exception cref="ArgumentNullException">The operation is null.</exception>
    /// <exception cref="InvalidOperationException">An attached call runs off-owner.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposed.</exception>
    /// <exception cref="AggregateException">Callbacks fail after traversal continues.</exception>
    public void PropagateCall(Action<Node> operation, bool parentFirst = false) => PropagateCall<Node>(operation, parentFirst);
    /// <summary>Invokes a typed operation on matching nodes throughout this subtree.</summary>
    /// <typeparam name="TNode">The node role receiving the operation; other node roles are traversed without invocation.</typeparam>
    /// <param name="operation">The synchronous typed operation.</param><param name="parentFirst">Whether parents precede descendants.</param>
    /// <remarks>Includes internal children, blocks traversed child-list mutation, and combines failures after visiting remaining nodes.</remarks>
    /// <exception cref="ArgumentNullException">The operation is null.</exception><exception cref="InvalidOperationException">The attached call is off-owner.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposed.</exception><exception cref="AggregateException">One or more callbacks fail.</exception>
    public void PropagateCall<TNode>(Action<TNode> operation, bool parentFirst = false) where TNode : Node
    { ArgumentNullException.ThrowIfNull(operation); PropagateOperation(operation, static (node, callback) => { if (node is TNode typed) callback(typed); }, parentFirst); }
    /// <summary>Invokes a typed operation and argument value on matching nodes throughout this subtree.</summary>
    /// <typeparam name="TNode">The matching node role.</typeparam><typeparam name="TArgument">The argument type, which may be a typed tuple.</typeparam>
    /// <param name="operation">The synchronous typed operation.</param><param name="argument">The argument passed to every matching node.</param><param name="parentFirst">Whether parents precede descendants.</param>
    /// <remarks>The argument is borrowed, and each invocation receives its ordinary C# value/reference semantics. Includes internal children and combines callback failures after traversal.</remarks>
    /// <exception cref="ArgumentNullException">The operation is null.</exception><exception cref="InvalidOperationException">The attached call is off-owner.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposed.</exception><exception cref="AggregateException">One or more callbacks fail.</exception>
    public void PropagateCall<TNode, TArgument>(Action<TNode, TArgument> operation, TArgument argument, bool parentFirst = false) where TNode : Node
    { ArgumentNullException.ThrowIfNull(operation); PropagateOperation((operation, argument), static (node, state) => { if (node is TNode typed) state.operation(typed, state.argument); }, parentFirst); }
    private void PropagateOperation<TState>(TState state, Action<Node, TState> invoke, bool parentFirst)
    {
        ThrowIfDisposed(); Tree?.EnsureOwnerThread(); List<Exception>? errors = null;
        _notificationPropagationDepth++;
        try
        {
            if (parentFirst) try { invoke(this, state); } catch (Exception error) { CollectException(ref errors, error); }
            foreach (var child in _children)
                try { child.PropagateOperation(state, invoke, parentFirst); } catch (Exception error) { CollectException(ref errors, error); }
            if (!parentFirst) try { invoke(this, state); } catch (Exception error) { CollectException(ref errors, error); }
        }
        finally { _notificationPropagationDepth--; }
        ThrowCollected("One or more propagated operation callbacks failed.", errors);
    }
}
