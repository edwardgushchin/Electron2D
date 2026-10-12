namespace Electron2D;

public static partial class ResourceFileTypes
{
    /// <summary>Registers a stable typed property binding for file-backed animation value and scalar Bézier tracks.</summary>
    /// <typeparam name="TOwner">The exact node receiver role.</typeparam>
    /// <typeparam name="TValue">The property's exact value type, with a portable codec or resource-reference schema.</typeparam>
    /// <param name="id">An immutable portable binding ID, at most 256 characters.</param>
    /// <param name="property">The writable compiled descriptor used when authoring tracks.</param>
    /// <param name="interpolate">The authoring track's custom interpolator, or null for the built-in profile.</param>
    /// <remarks>Register the same descriptor and interpolator before saving and in each loading process.
    /// File data selects this compiled binding; it never resolves CLR members or stores delegates.
    /// Float and double descriptors also restore scalar Bézier tracks. Registration is allocating setup.</remarks>
    /// <exception cref="ArgumentException">The ID is invalid or the descriptor is read-only.</exception>
    /// <exception cref="InvalidOperationException">The ID or descriptor/interpolator pair is already registered.</exception>
    public static void RegisterAnimationProperty<TOwner, TValue>(string id, PropertyDescriptor<TOwner, TValue> property,
        Func<TValue, TValue, double, TValue>? interpolate = null) where TOwner : Node
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id); ArgumentNullException.ThrowIfNull(property);
        if (id.Length > 256 || id.Any(char.IsControl)) throw new ArgumentException("Binding ID is not a portable token.", nameof(id));
        if (property.IsReadOnly) throw new ArgumentException("Animation requires a writable property.", nameof(property));
        var binding = new AnimationFileBinding<TOwner, TValue>(id, property, interpolate ?? TweenValue<TValue>.Interpolate);
        lock (Gate)
        {
            var bindings = ResourceLoader.Runtime.FileTypes.AnimationBindings;
            if (bindings.ContainsKey(id) || bindings.Values.Any(b => b.Matches(property, binding.Interpolator)))
                throw new InvalidOperationException("Animation binding or ID is already registered.");
            bindings.Add(id, binding);
        }
    }
    internal static string AnimationBindingID(AnimationTrack track)
    {
        lock (Gate)
            foreach (var binding in ResourceLoader.Runtime.FileTypes.AnimationBindings.Values)
                if (binding.Matches(track.FileProperty, track.FileInterpolator)) return binding.ID;
        throw new NotSupportedException("Register the exact animation property and interpolator before saving.");
    }
    internal static AnimationTrack CreateAnimationFileTrack(string schema)
    {
        if (schema is null || schema.Length < 3 || schema.Length > 258 || schema[1] != ':' || schema[0] is not ('0' or '6'))
            throw new InvalidDataException("Invalid animation track schema.");
        AnimationFileBinding binding;
        lock (Gate) binding = ResourceLoader.Runtime.FileTypes.AnimationBindings.GetValueOrDefault(schema[2..])
            ?? throw new InvalidDataException("Animation property binding is not registered.");
        return binding.Create(schema[0] == '0' ? Animation.TrackType.Value : Animation.TrackType.Bezier);
    }
}

internal abstract class AnimationFileBinding(string id, PropertyDescriptor property, Delegate? interpolator)
{
    internal string ID => id;
    internal Delegate? Interpolator => interpolator;
    internal bool Matches(PropertyDescriptor? candidate, Delegate? curve) => ReferenceEquals(property, candidate) && Equals(interpolator, curve);
    internal abstract AnimationTrack Create(Animation.TrackType kind);
}
internal sealed class AnimationFileBinding<TOwner, T>(string id, PropertyDescriptor<TOwner, T> property, Func<T, T, double, T>? interpolate)
    : AnimationFileBinding(id, property, interpolate) where TOwner : Node
{
    internal override AnimationTrack Create(Animation.TrackType kind) => kind switch
    {
        Animation.TrackType.Value => new AnimationValueTrack<TOwner, T>(property, interpolate),
        Animation.TrackType.Bezier when (typeof(T) == typeof(float) || typeof(T) == typeof(double)) && interpolate == TweenValue<T>.Interpolate => new AnimationBezierTrack<TOwner, T>(property),
        _ => throw new InvalidDataException("Registered property cannot restore this animation track kind.")
    };
}
