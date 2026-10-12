namespace Electron2D;

public static partial class ResourceFileTypes
{
    /// <summary>Registers a stable compiled receiver and callback binding for file-backed animation method keys.</summary>
    /// <typeparam name="TOwner">The exact Node receiver role shared by the method track.</typeparam>
    /// <typeparam name="TArguments">The exact portable payload type, with a codec or resource-reference schema.</typeparam>
    /// <param name="id">An immutable portable ID, at most 256 characters.</param>
    /// <param name="callback">The typed callback used by authored keys.</param>
    /// <remarks>Register the same callback/schema before saving and in each loading process. IDs select
    /// compiled code; file data never invokes reflected members or restores delegates. Key names remain
    /// independent diagnostic data. An empty method track still needs a registered receiver binding.
    /// Registration is allocating setup; payload resources remain governed by the existing file graph.</remarks>
    /// <exception cref="ArgumentException">The ID is blank, too long or contains control characters.</exception>
    /// <exception cref="ArgumentNullException">The callback is null.</exception>
    /// <exception cref="InvalidOperationException">The ID or callback/receiver/payload binding is already registered.</exception>
    public static void RegisterAnimationMethod<TOwner, TArguments>(string id, Action<TOwner, TArguments> callback) where TOwner : Node
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id); ArgumentNullException.ThrowIfNull(callback);
        if (id.Length > 256 || id.Any(char.IsControl)) throw new ArgumentException("Method binding ID is not a portable token.", nameof(id));
        var binding = new AnimationFileMethodBinding<TOwner, TArguments>(id, callback);
        lock (Gate)
        {
            var bindings = ResourceLoader.Runtime.FileTypes.AnimationMethods;
            if (bindings.ContainsKey(id) || bindings.Values.Any(b => b.SameBinding(binding)))
                throw new InvalidOperationException("Animation method binding or ID is already registered.");
            bindings.Add(id, binding);
        }
    }
    internal static string AnimationMethodReceiverID<TOwner>() where TOwner : Node
    {
        lock (Gate)
            foreach (var binding in ResourceLoader.Runtime.FileTypes.AnimationMethods.Values)
                if (binding.ReceiverType == typeof(TOwner)) return binding.ID;
        throw new NotSupportedException("Register an animation callback for this receiver before saving its method tracks.");
    }
    internal static string AnimationMethodBindingID<TOwner>(AnimationMethodKey<TOwner> key) where TOwner : Node
    {
        lock (Gate)
            foreach (var binding in ResourceLoader.Runtime.FileTypes.AnimationMethods.Values)
                if (binding.Matches(key)) return binding.ID;
        throw new NotSupportedException("Register the exact animation method callback and payload before saving.");
    }
    internal static AnimationFileMethodBinding AnimationMethodBinding(string id)
    {
        if (id is null || id.Length == 0 || id.Length > 256) throw new InvalidDataException("Invalid animation method binding ID.");
        lock (Gate) return ResourceLoader.Runtime.FileTypes.AnimationMethods.GetValueOrDefault(id)
            ?? throw new InvalidDataException("Animation method binding is not registered.");
    }
}
internal abstract class AnimationFileMethodBinding(string id, Type receiverType, Type argumentType, Delegate callback)
{
    internal string ID => id;
    internal Type ReceiverType => receiverType;
    internal bool SameBinding(AnimationFileMethodBinding other) => receiverType == other.ReceiverType && argumentType == other.ArgumentType && Equals(callback, other.Callback);
    private Type ArgumentType => argumentType;
    private Delegate Callback => callback;
    internal abstract bool Matches<T>(AnimationMethodKey<T> key) where T : Node;
    internal abstract AnimationTrack CreateTrack();
    internal abstract AnimationMethodKey<T> CreateKey<T>() where T : Node;
}
internal sealed class AnimationFileMethodBinding<TOwner, TArguments>(string id, Action<TOwner, TArguments> callback)
    : AnimationFileMethodBinding(id, typeof(TOwner), typeof(TArguments), callback) where TOwner : Node
{
    internal override bool Matches<T>(AnimationMethodKey<T> key) => key is AnimationMethodKey<TOwner, TArguments> typed && Equals(typed.Callback, callback);
    internal override AnimationTrack CreateTrack() => new AnimationMethodTrack<TOwner>();
    internal override AnimationMethodKey<T> CreateKey<T>()
    {
        if (typeof(T) != typeof(TOwner)) throw new InvalidDataException("Animation method receiver does not match its track.");
        return (AnimationMethodKey<T>)(object)new AnimationMethodKey<TOwner, TArguments>(ID, callback, default!);
    }
}
