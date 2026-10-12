namespace Electron2D;

public partial class AnimationMixer
{
    private string[]? _fileLibraryNames;
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory()
    {
        if (GetType() != typeof(AnimationMixer)) return base.CreateSceneInstanceFactory();
        return CreateFileMixer;
    }
    private static Node CreateFileMixer() => new AnimationMixer();
    internal override void ValidateSceneInstance()
    {
        base.ValidateSceneInstance();
        if (_fileLibraryNames is not null) throw new InvalidDataException("Animation library table is incomplete.");
        foreach (var item in _libraries.Values) ObjectDisposedException.ThrowIf(item.Library.IsDisposed, item.Library);
    }
    private IEnumerable<PropertyDescriptor> GetFileLibraryProperties()
    {
        yield return new PropertyDescriptor<AnimationMixer, string[]>("LibraryNames", n => n.GetFileLibraryNames(), (n, v) => n.SetFileLibraryNames(v), stored: true);
        foreach (var name in _fileLibraryNames ?? GetAnimationLibraryList())
        {
            var key = name;
            yield return new PropertyDescriptor<AnimationMixer, AnimationLibrary>($"Library[{name}]", n => n.GetAnimationLibrary(key), (n, v) => n.SetFileLibrary(key, v), stored: true);
        }
    }
    private string[] GetFileLibraryNames()
    {
        var names = GetAnimationLibraryList();
        if (names.Length > 4096) throw new InvalidDataException("Animation library count exceeds file budget.");
        return names;
    }
    private void SetFileLibraryNames(string[] names)
    {
        EnsureAnimationMutable(); ArgumentNullException.ThrowIfNull(names);
        if (names.Length > 4096 || names.Any(n => !AnimationLibrary.ValidName(n, true)) || names.Distinct(StringComparer.Ordinal).Count() != names.Length)
            throw new InvalidDataException("Invalid animation library namespaces.");
        foreach (var item in _libraries.Values) item.Library.Changed -= item.Changed;
        _libraries.Clear(); _animationIndexDirty = true;
        _fileLibraryNames = names.Length == 0 ? null : (string[])names.Clone();
        InvalidateBindings(); NotifyPropertyListChanged();
    }
    private void SetFileLibrary(string name, AnimationLibrary library)
    {
        AddAnimationLibrary(name, library);
        if (_fileLibraryNames is not null && _libraries.Count == _fileLibraryNames.Length) _fileLibraryNames = null;
    }
}
