namespace Electron2D;

public partial class Node
{
    /// <summary>Queues a typed operation before or after processing of this node's owning group.</summary>
    /// <typeparam name="TNode">The receiver type accepted by the operation.</typeparam>
    /// <param name="operation">The operation receiving this live node.</param>
    /// <remarks>The destination group drains FIFO in each active callback lane; its message mask additionally permits empty-group delivery.
    /// The receiver is borrowed weakly; disposed or collected receivers are skipped. A transferred receiver follows
    /// its current tree owner. Captured delegates and values retain their ordinary caller-owned references.
    /// Messages queued during delivery join the same drain; avoid unbounded self-enqueue.</remarks>
    /// <exception cref="ArgumentNullException">The operation is null.</exception>
    /// <exception cref="ArgumentException">This node does not match the receiver type.</exception>
    /// <exception cref="InvalidOperationException">This node is detached.</exception>
    /// <exception cref="ObjectDisposedException">This node or its owning tree is closed.</exception>
    public void CallDeferredThreadGroup<TNode>(Action<TNode> operation) where TNode : Node
    {
        ValidateThreadMessage<TNode>(operation, nameof(operation));
        RequireMessageTree().EnqueueProcessMessage(this, DeliverCall<TNode>, new CallMessage<TNode>(BorrowIdentity(), operation));
    }

    /// <summary>Queues a typed property assignment in this node's owning processing group.</summary>
    /// <typeparam name="TNode">The receiver type accepted by the setter.</typeparam>
    /// <typeparam name="TValue">The captured property value type.</typeparam>
    /// <param name="setter">The setter receiving this node and the captured value.</param>
    /// <param name="value">The value copied into the pending invocation.</param>
    /// <remarks>Uses the same weak receiver, FIFO, transfer and frame-phase rules as deferred group calls.
    /// Reference values remain borrowed from the caller; delivery invokes the ordinary setter and its guards.</remarks>
    /// <exception cref="ArgumentNullException">The setter is null.</exception>
    /// <exception cref="ArgumentException">This node does not match the receiver type.</exception>
    /// <exception cref="InvalidOperationException">This node is detached.</exception>
    /// <exception cref="ObjectDisposedException">This node or its owning tree is closed.</exception>
    public void SetDeferredThreadGroup<TNode, TValue>(Action<TNode, TValue> setter, TValue value) where TNode : Node
    {
        ValidateThreadMessage<TNode>(setter, nameof(setter));
        RequireMessageTree().EnqueueProcessMessage(this, DeliverSet<TNode, TValue>, new SetMessage<TNode, TValue>(BorrowIdentity(), setter, value));
    }

    /// <summary>Queues a numeric notification in this node's owning processing group.</summary>
    /// <param name="what">The notification identifier.</param>
    /// <remarks>Uses ordinary virtual notification dispatch at the same FIFO boundaries as deferred group calls.</remarks>
    /// <exception cref="InvalidOperationException">This node is detached.</exception>
    /// <exception cref="ObjectDisposedException">This node or its owning tree is closed.</exception>
    public void NotifyDeferredThreadGroup(int what)
    {
        ThrowIfDisposed();
        RequireMessageTree().EnqueueProcessMessage(this, DeliverNotification, new NotificationMessage(BorrowIdentity(), what));
    }

    /// <summary>Invokes a typed operation immediately when accessible, otherwise queues it to this node's owner.</summary>
    /// <typeparam name="TNode">The receiver type accepted by the operation.</typeparam>
    /// <param name="operation">The operation receiving this live node.</param>
    /// <remarks>Attached nodes execute immediately on their tree owner or current processing-group worker. Detached nodes execute on the caller
    /// under their ordinary access policy. Otherwise delivery follows the deferred group rules; this method does not
    /// wait for completion or return an operation result. Immediate failures propagate, queued failures join frame errors.</remarks>
    /// <exception cref="ArgumentNullException">The operation is null.</exception>
    /// <exception cref="ArgumentException">This node does not match the receiver type.</exception>
    /// <exception cref="ObjectDisposedException">This node or its owning tree is closed.</exception>
    /// <exception cref="Exception">An immediate operation fails.</exception>
    public void CallThreadSafe<TNode>(Action<TNode> operation) where TNode : Node
    {
        ValidateThreadMessage<TNode>(operation, nameof(operation));
        if (ProcessingAccessible) operation((TNode)this);
        else RequireMessageTree().EnqueueProcessMessage(this, DeliverCall<TNode>, new CallMessage<TNode>(BorrowIdentity(), operation));
    }

    /// <summary>Assigns a typed property immediately when accessible, otherwise queues it to this node's owner.</summary>
    /// <typeparam name="TNode">The receiver type accepted by the setter.</typeparam>
    /// <typeparam name="TValue">The captured property value type.</typeparam>
    /// <param name="setter">The setter receiving this node and the value.</param>
    /// <param name="value">The assigned or captured value.</param>
    /// <remarks>Uses the same access, transfer and failure rules as thread-safe calls. The ordinary setter owns
    /// validation and notifications; queueing grants no direct off-owner access to scene or backend state.</remarks>
    /// <exception cref="ArgumentNullException">The setter is null.</exception>
    /// <exception cref="ArgumentException">This node does not match the receiver type.</exception>
    /// <exception cref="ObjectDisposedException">This node or its owning tree is closed.</exception>
    /// <exception cref="Exception">An immediate setter fails.</exception>
    public void SetThreadSafe<TNode, TValue>(Action<TNode, TValue> setter, TValue value) where TNode : Node
    {
        ValidateThreadMessage<TNode>(setter, nameof(setter));
        if (ProcessingAccessible) setter((TNode)this, value);
        else RequireMessageTree().EnqueueProcessMessage(this, DeliverSet<TNode, TValue>, new SetMessage<TNode, TValue>(BorrowIdentity(), setter, value));
    }

    /// <summary>Delivers a numeric notification immediately when accessible, otherwise queues it to this node's owner.</summary>
    /// <param name="what">The notification identifier.</param>
    /// <remarks>Uses ordinary virtual notification dispatch and the thread-safe call access and failure rules.</remarks>
    /// <exception cref="ObjectDisposedException">This node or its owning tree is closed.</exception>
    /// <exception cref="Exception">An immediate notification callback fails.</exception>
    public void NotifyThreadSafe(int what)
    {
        ThrowIfDisposed();
        if (ProcessingAccessible) Notify(what);
        else RequireMessageTree().EnqueueProcessMessage(this, DeliverNotification, new NotificationMessage(BorrowIdentity(), what));
    }

    private void ValidateThreadMessage<TNode>(Delegate callback, string parameter) where TNode : Node
    {
        ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(callback, parameter);
        if (this is not TNode) throw new ArgumentException("The node does not match the callback receiver type.", parameter);
    }
    private SceneTree RequireMessageTree() => Tree ?? throw new InvalidOperationException("Deferred group delivery requires an attached node.");
    private readonly record struct CallMessage<TNode>(ObjectIdentity Target, Action<TNode> Operation) where TNode : Node;
    private readonly record struct SetMessage<TNode, TValue>(ObjectIdentity Target, Action<TNode, TValue> Setter, TValue Value) where TNode : Node;
    private readonly record struct NotificationMessage(ObjectIdentity Target, int What);

    private static void DeliverCall<TNode>(CallMessage<TNode> message) where TNode : Node
    {
        if (message.Target.Target is not TNode node) return;
        if (!node.ProcessingAccessible) node.RequireMessageTree().EnqueueProcessMessage(node, DeliverCall<TNode>, message);
        else message.Operation(node);
    }
    private static void DeliverSet<TNode, TValue>(SetMessage<TNode, TValue> message) where TNode : Node
    {
        if (message.Target.Target is not TNode node) return;
        if (!node.ProcessingAccessible) node.RequireMessageTree().EnqueueProcessMessage(node, DeliverSet<TNode, TValue>, message);
        else message.Setter(node, message.Value);
    }
    private static void DeliverNotification(NotificationMessage message)
    {
        if (message.Target.Target is not Node node) return;
        if (!node.ProcessingAccessible) node.RequireMessageTree().EnqueueProcessMessage(node, DeliverNotification, message);
        else node.Notify(message.What);
    }
}
