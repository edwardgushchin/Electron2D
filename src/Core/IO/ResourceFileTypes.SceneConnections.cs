namespace Electron2D;

public static partial class ResourceFileTypes
{
    /// <summary>Registers a stable compiled subscription factory for authored scene connections.</summary>
    /// <typeparam name="TPublisher">The event owner's role.</typeparam>
    /// <typeparam name="TSubscriber">The receiving role.</typeparam>
    /// <typeparam name="TBindings">The portable bound payload, using a value codec or Resource graph schema.</typeparam>
    /// <param name="id">An immutable portable ID, at most 256 characters.</param>
    /// <param name="signal">Diagnostic event label; never used for member invocation.</param>
    /// <param name="method">Diagnostic receiver label; never used for member invocation.</param>
    /// <param name="factory">A static factory using typed event accessors and returning an active owned token.</param>
    /// <param name="unbinds">Number of event arguments intentionally omitted by the compiled adapter.</param>
    /// <param name="oneShot">The delivery policy implemented by the factory's token.</param>
    /// <param name="deferred">Whether the factory's token uses a scheduler.</param>
    /// <returns>The typed authoring binding.</returns>
    /// <remarks>Register the same ID/schema in each saving and loading process. Argument adaptation is compiled
    /// C# code, not dynamic binding. Duplicate IDs are rejected. Native duplicate subscriptions remain distinct.</remarks>
    /// <exception cref="ArgumentException">Labels/ID are invalid or the factory retains a target.</exception>
    /// <exception cref="InvalidOperationException">The ID is already registered.</exception>
    public static SceneConnectionBinding<TPublisher, TSubscriber, TBindings> RegisterSceneConnection<TPublisher, TSubscriber, TBindings>(
        string id, string signal, string method, Func<TPublisher, TSubscriber, TBindings, EventConnection> factory,
        int unbinds = 0, bool oneShot = false, bool deferred = false) where TPublisher : Node where TSubscriber : Node
    {
        ArgumentNullException.ThrowIfNull(factory);
        if (typeof(Node).IsAssignableFrom(typeof(TBindings))) throw new ArgumentException("Node payloads require explicit endpoint roles.");
        foreach (var label in new[] { id, signal, method })
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(label);
            if (label.Length > 256 || label.Any(char.IsControl)) throw new ArgumentException("Connection identity is not a portable token.");
        }
        if (factory.Target is not null || factory.GetInvocationList().Length != 1) throw new ArgumentException("Connection factories must be static.", nameof(factory));
        ArgumentOutOfRangeException.ThrowIfNegative(unbinds);
        var schema = new SceneConnectionSchema<TPublisher, TSubscriber, TBindings>(id, signal, method, unbinds, oneShot, deferred, factory);
        lock (Gate)
            if (!ResourceLoader.Runtime.FileTypes.SceneConnections.TryAdd(id, schema)) throw new InvalidOperationException("Connection ID already registered.");
        return new(schema);
    }
    internal static SceneConnectionSchema? FindSceneConnection(string id)
    {
        lock (Gate) return ResourceLoader.Runtime.FileTypes.SceneConnections.GetValueOrDefault(id);
    }
}
