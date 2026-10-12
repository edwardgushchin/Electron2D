namespace Electron2D;

public sealed partial class AnimationLibrary
{
    private string[]? _fileNames;
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) yield return property;
        yield return new PropertyDescriptor<AnimationLibrary, string[]>("AnimationNames", a => a.GetAnimationList(), (a, v) => a.SetFileNames(v), stored: true);
        foreach (var name in _fileNames ?? GetAnimationList())
        {
            var key = name;
            yield return new PropertyDescriptor<AnimationLibrary, Animation>($"Animation[{name}]", a => a.GetAnimation(key), (a, v) => a.SetFileAnimation(key, v), stored: true);
        }
    }
    private void SetFileNames(string[] names)
    {
        ArgumentNullException.ThrowIfNull(names);
        if (names.Length > 65520 || names.Any(n => !ValidName(n, false)) || names.Distinct(StringComparer.Ordinal).Count() != names.Length)
            throw new InvalidDataException("Invalid animation library names.");
        Detach(); _fileNames = names.Length == 0 ? null : (string[])names.Clone();
        EmitChanged(); NotifyPropertyListChanged();
    }
    private void SetFileAnimation(string name, Animation animation)
    {
        AddAnimation(name, animation);
        if (_fileNames is not null && _animations.Count == _fileNames.Length) _fileNames = null;
    }
}
