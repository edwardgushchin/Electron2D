namespace Electron2D;

/// <summary>Defines a stable compiled subscription between two typed scene endpoints.</summary>
/// <typeparam name="TPublisher">The event owner's Node role.</typeparam>
/// <typeparam name="TSubscriber">The receiving Node role.</typeparam>
/// <typeparam name="TBindings">The portable typed bound payload.</typeparam>
/// <remarks>Obtain bindings through ResourceFileTypes.RegisterSceneConnection. Labels describe compiled
/// code; they never invoke members by name. The same registration is required in each loading process.</remarks>
public sealed class SceneConnectionBinding<TPublisher, TSubscriber, TBindings>
    where TPublisher : Node where TSubscriber : Node
{
    internal SceneConnectionBinding(SceneConnectionSchema<TPublisher, TSubscriber, TBindings> schema) => Schema = schema;
    internal SceneConnectionSchema<TPublisher, TSubscriber, TBindings> Schema { get; }

    /// <summary>Subscribes now and records an authored connection for scene packing.</summary>
    /// <param name="publisher">The live event owner.</param>
    /// <param name="subscriber">The live receiver.</param>
    /// <param name="bindings">A snapshotted typed bound payload.</param>
    /// <returns>The caller-disposable subscription, also cleaned up by either endpoint.</returns>
    /// <remarks>Duplicate calls create distinct subscriptions. Consumed one-shot or disposed tokens are
    /// omitted by Pack. Active tree endpoints require their owner thread. The factory must be static.
    /// A selected edited scene receives EditorStateChanged after retention. If that callback fails, the new
    /// connection is removed without a second authoring event; callback and rollback failures are preserved.</remarks>
    public EventConnection Subscribe(TPublisher publisher, TSubscriber subscriber, TBindings bindings) =>
        Schema.Subscribe(publisher, subscriber, new StoredPropertyValue<TBindings>(StoredPropertyValue<TBindings>.Snapshot(bindings)));
}

internal abstract class SceneConnectionSchema(string id, string signal, string method, int unbinds, bool oneShot, bool deferred)
{
    internal string ID => id;
    internal string Signal => signal;
    internal string Method => method;
    internal int Unbinds => unbinds;
    internal int Flags => 2 | (oneShot ? 4 : 0) | (deferred ? 1 : 0);
    internal abstract Type PublisherType { get; }
    internal abstract Type SubscriberType { get; }
    internal abstract PropertyDescriptor PayloadDescriptor { get; }
    internal abstract EventConnection Subscribe(Node publisher, Node subscriber, StoredPropertyValue bindings);
    internal SceneConnectionRuntime Restore(Node publisher, Node subscriber, StoredPropertyValue bindings)
    {
        if (publisher is MissingNode || subscriber is MissingNode || this is UnresolvedSceneConnectionSchema)
        {
            if (publisher is MissingNode missing) missing.RememberSignal(Signal);
            return publisher.RetainRestoredSceneConnection(this, subscriber, bindings);
        }
        var token = Subscribe(publisher, subscriber, bindings);
        return publisher.CaptureSceneConnections().Single(c => ReferenceEquals(c.Token, token));
    }
    protected void ValidateToken(EventConnection token)
    {
        if (!token.IsConnected || token.OneShot != oneShot || token.Deferred != deferred)
        {
            token.Dispose();
            throw new InvalidOperationException("Subscription factory returned an inactive token or a different delivery policy.");
        }
    }
}

internal sealed class SceneConnectionSchema<TPublisher, TSubscriber, TBindings>(string id, string signal, string method,
    int unbinds, bool oneShot, bool deferred, Func<TPublisher, TSubscriber, TBindings, EventConnection> factory)
    : SceneConnectionSchema(id, signal, method, unbinds, oneShot, deferred)
    where TPublisher : Node where TSubscriber : Node
{
    private static readonly PropertyDescriptor<Node, TBindings> Descriptor = new("Bindings", static _ => default!);
    internal override Type PublisherType => typeof(TPublisher);
    internal override Type SubscriberType => typeof(TSubscriber);
    internal override PropertyDescriptor PayloadDescriptor => Descriptor;
    internal override EventConnection Subscribe(Node publisher, Node subscriber, StoredPropertyValue bindings)
    {
        ArgumentNullException.ThrowIfNull(publisher); ArgumentNullException.ThrowIfNull(subscriber);
        publisher.EnsureConnectionMutation(); subscriber.EnsureConnectionMutation();
        if (publisher is not TPublisher typedPublisher || subscriber is not TSubscriber typedSubscriber || !bindings.TryGetValue<TBindings>(out var payload))
            throw new InvalidDataException("Connection endpoints or payload do not match the registered schema.");
        var token = factory(typedPublisher, typedSubscriber, payload) ?? throw new InvalidOperationException("Subscription factory returned null.");
        try
        {
            ValidateToken(token);
            publisher.EnsureConnectionMutation(); subscriber.EnsureConnectionMutation();
            publisher.RetainSceneConnection(new SceneConnectionRuntime(this, publisher, subscriber, bindings, token));
        }
        catch (Exception error)
        {
            try { token.Dispose(); } catch (Exception cleanup) { throw new AggregateException(error, cleanup); }
            throw;
        }
        return token;
    }
}

internal sealed record SceneConnectionRuntime(SceneConnectionSchema Schema, Node Publisher, Node Subscriber,
    StoredPropertyValue Bindings, EventConnection? Token);
internal sealed record SceneConnectionData(SceneConnectionSchema Schema, int PublisherIndex, int SubscriberIndex, StoredPropertyValue Bindings);

internal sealed class UnresolvedSceneConnectionSchema(string id, string signal, string method, int unbinds, int flags)
    : SceneConnectionSchema(id, signal, method, unbinds, (flags & 4) != 0, (flags & 1) != 0)
{
    internal override Type PublisherType => typeof(Node);
    internal override Type SubscriberType => typeof(Node);
    internal override PropertyDescriptor PayloadDescriptor => throw new InvalidOperationException("The original compiled connection schema is unavailable.");
    internal override EventConnection Subscribe(Node publisher, Node subscriber, StoredPropertyValue bindings) =>
        throw new InvalidOperationException("An unresolved authored connection cannot execute.");
}
