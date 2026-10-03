namespace Electron2D;

/// <summary>Stores named borrowed animation resources and forwards their change notifications.</summary>
/// <remarks>Names are ordinal and exclude slash, colon, comma and opening bracket. Replacement emits removed
/// then added. Observers run synchronously after committed mutation. Disposal detaches subscriptions without
/// disposing animations. Access follows the scene owner thread; resource copies preserve aliases.</remarks>
public sealed class AnimationLibrary : Resource
{
    private readonly Dictionary<string, (Animation Animation, Action<Resource> Changed)> _animations = new(StringComparer.Ordinal);
    /// <summary>Occurs after a name is added.</summary>
    public event Action<string>? AnimationAdded;
    /// <summary>Occurs when a contained animation emits Changed.</summary>
    public event Action<string>? AnimationChanged;
    /// <summary>Occurs after a name is removed.</summary>
    public event Action<string>? AnimationRemoved;
    /// <summary>Occurs after a name is changed.</summary>
    public event Action<string, string>? AnimationRenamed;
    /// <summary>Adds or replaces an animation; invalid names or resources throw before mutation.</summary>
    /// <param name="name">The ordinal animation, library or marker name; empty names are accepted only where explicitly documented.</param>
    /// <param name="animation">The live borrowed animation resource.</param>
    public void AddAnimation(string name, Animation animation)
    {
        ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(animation); ObjectDisposedException.ThrowIf(animation.IsDisposed, animation);
        if (!ValidName(name, false)) throw new ArgumentException("Invalid animation name.", nameof(name));
        Action<Resource> changed = _ => { AnimationChanged?.Invoke(name); };
        var replaced = _animations.Remove(name, out var prior);
        if (replaced) prior.Animation.Changed -= prior.Changed;
        _animations[name] = (animation, changed); animation.Changed += changed;
        EmitChanged(); NotifyPropertyListChanged(); if (replaced) AnimationRemoved?.Invoke(name);
        if (!IsDisposed && _animations.TryGetValue(name, out var current) && current.Changed == changed) AnimationAdded?.Invoke(name);
    }
    /// <summary>Returns a borrowed animation, or throws for a missing name.</summary>
    /// <param name="name">The ordinal animation, library or marker name; empty names are accepted only where explicitly documented.</param>
    public Animation GetAnimation(string name) { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(name); return _animations[name].Animation; }
    /// <summary>Tests exact name membership.</summary>
    /// <param name="name">The exact ordinal name.</param>
    public bool HasAnimation(string name) { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(name); return _animations.ContainsKey(name); }
    /// <summary>Returns an independent ordinal-sorted name array.</summary>
    public string[] GetAnimationList() { ThrowIfDisposed(); var names = _animations.Keys.ToArray(); Array.Sort(names, StringComparer.Ordinal); return names; }
    /// <summary>Returns the animation count.</summary>
    public int GetAnimationListSize() { ThrowIfDisposed(); return _animations.Count; }
    /// <summary>Removes a name and detaches its resource observer.</summary>
    /// <param name="name">The exact ordinal name.</param>
    public void RemoveAnimation(string name) { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(name); if (!_animations.Remove(name, out var item)) throw new KeyNotFoundException(name); item.Animation.Changed -= item.Changed; NotifyPropertyListChanged(); EmitChanged(); AnimationRemoved?.Invoke(name); }
    /// <summary>Renames an animation, rejecting invalid or occupied names before mutation.</summary>
    /// <param name="newName">An unoccupied valid replacement name.</param>
    /// <param name="name">The exact ordinal name.</param>
    public void RenameAnimation(string name, string newName)
    {
        ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(name);
        if (!ValidName(newName, false) || _animations.ContainsKey(newName)) throw new ArgumentException("Invalid or occupied animation name.", nameof(newName));
        var prior = _animations[name]; Action<Resource> changed = _ => { AnimationChanged?.Invoke(newName); };
        _animations.Remove(name); prior.Animation.Changed -= prior.Changed; _animations.Add(newName, (prior.Animation, changed)); prior.Animation.Changed += changed;
        EmitChanged(); AnimationRenamed?.Invoke(name, newName);
    }
    internal static bool ValidName(string? name, bool allowEmpty) => name is not null && (allowEmpty || name.Length != 0) && name.AsSpan().IndexOfAny("/:,[") < 0;
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AnimationLibrary();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicate, Func<Resource?, Resource?> force)
    { var copy = (AnimationLibrary)target; copy.Detach(); foreach (var (name, entry) in _animations) copy.AddAnimation(name, deep ? (Animation)duplicate(entry.Animation)! : entry.Animation); }
    private void Detach() { foreach (var entry in _animations.Values) entry.Animation.Changed -= entry.Changed; _animations.Clear(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) { Detach(); AnimationAdded = null; AnimationChanged = null; AnimationRemoved = null; AnimationRenamed = null; } base.Dispose(disposing); }
}
