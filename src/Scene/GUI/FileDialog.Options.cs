using System.Collections.ObjectModel;

namespace Electron2D;

public partial class FileDialog
{
    private sealed class Option { internal string Name = ""; internal string[] Values = []; internal int Default; }
    private readonly List<Option> _options = [];
    private readonly Dictionary<string, FileDialogOptionValue> _selectedOptions = new(StringComparer.Ordinal);
    private bool _optionsDirty = true, _updatingOptions;
    private static int _sharedOwner;
    private static void CheckSharedOwner()
    {
        var current = Environment.CurrentManagedThreadId; var owner = Interlocked.CompareExchange(ref _sharedOwner, current, 0);
        if (owner != 0 && owner != current) throw new InvalidOperationException("Shared file-dialog configuration belongs to its first authoring thread.");
    }
    /// <summary>Gets or sets the number of additional checkbox/choice controls.</summary>
    /// <value>Zero initially; range 0..65536.</value>
    public int OptionCount
    {
        get { CheckFileDialog(); return _options.Count; }
        set { EnsureMutable(); if ((uint)value > 65536) throw new ArgumentOutOfRangeException(nameof(value)); if (value == _options.Count) return; while (_options.Count < value) _options.Add(new()); if (value < _options.Count) _options.RemoveRange(value, _options.Count - value); OptionsChanged(); NotifyPropertyListChanged(); }
    }
    /// <summary>Adds a checkbox when values are empty, or a choice control otherwise.</summary>
    /// <param name="name">The option label/result key.</param><param name="values">Copied choice captions, or empty for a checkbox.</param><param name="defaultValueIndex">Initial index, clamped to the choice or checkbox range.</param>
    public void AddOption(string name, IReadOnlyList<string> values, int defaultValueIndex)
    {
        EnsureMutable(); ValidateOptionName(name); var copy = CopyValues(values); if (_options.Count == 65536) throw new InvalidOperationException("File option capacity exceeded.");
        _options.Add(new() { Name = name, Values = copy, Default = ClampDefault(copy, defaultValueIndex) }); OptionsChanged(); NotifyPropertyListChanged();
    }
    private Option GetOption(int index, bool relative = false) { CheckFileDialog(); if (relative && index < 0) index += _options.Count; if ((uint)index >= _options.Count) throw new ArgumentOutOfRangeException(nameof(index)); return _options[index]; }
    private static void ValidateOptionName(string name) { ArgumentNullException.ThrowIfNull(name); if (name.Contains('\0')) throw new ArgumentException("Option name contains a null character.", nameof(name)); }
    private static string[] CopyValues(IReadOnlyList<string> values) { ArgumentNullException.ThrowIfNull(values); var copy = values.ToArray(); foreach (var value in copy) ValidateOptionName(value); return copy; }
    private static int ClampDefault(string[] values, int value) => Math.Clamp(value, 0, values.Length == 0 ? 1 : values.Length - 1);
    /// <summary>Gets a checkbox/choice's default value index.</summary><param name="option">A nonnegative index.</param><returns>The stored clamped index.</returns>
    public int GetOptionDefault(int option) => GetOption(option).Default;
    /// <summary>Gets the option label/result key.</summary><param name="option">A nonnegative index.</param><returns>The raw option name.</returns>
    public string GetOptionName(int option) => GetOption(option).Name;
    /// <summary>Returns an independent copy of choice captions.</summary><param name="option">A nonnegative index.</param><returns>An empty list for a checkbox.</returns>
    public string[] GetOptionValues(int option) => (string[])GetOption(option).Values.Clone();
    /// <summary>Changes an option's clamped default and rebuilds visible controls.</summary><param name="option">An index, optionally counted from the end.</param><param name="defaultValueIndex">The requested default.</param>
    public void SetOptionDefault(int option, int defaultValueIndex) { EnsureMutable(); var item = GetOption(option, true); item.Default = ClampDefault(item.Values, defaultValueIndex); OptionsChanged(); }
    /// <summary>Changes an option label/result key and rebuilds visible controls.</summary><param name="option">An index, optionally counted from the end.</param><param name="name">The nonnull new key.</param>
    public void SetOptionName(int option, string name) { EnsureMutable(); ValidateOptionName(name); GetOption(option, true).Name = name; OptionsChanged(); }
    /// <summary>Changes copied choice values and clamps the retained default.</summary><param name="option">An index, optionally counted from the end.</param><param name="values">Captions or empty for a checkbox.</param>
    public void SetOptionValues(int option, IReadOnlyList<string> values) { EnsureMutable(); var copy = CopyValues(values); var item = GetOption(option, true); item.Values = copy; item.Default = ClampDefault(copy, item.Default); OptionsChanged(); }
    /// <summary>Returns an owned read-only snapshot of current checkbox/choice results keyed by option name.</summary>
    /// <returns>Typed results, populated when controls are prepared; duplicate keys use the last configured option.</returns>
    public IReadOnlyDictionary<string, FileDialogOptionValue> GetSelectedOptions() { CheckFileDialog(); return new ReadOnlyDictionary<string, FileDialogOptionValue>(new Dictionary<string, FileDialogOptionValue>(_selectedOptions, StringComparer.Ordinal)); }
    private void OptionsChanged() { _optionsDirty = true; if (Visible) RefreshOptions(); }
    private void RefreshOptions()
    {
        if (!_optionsDirty || _updatingOptions || _optionsRow == null || IsDisposed) return;
        _updatingOptions = true;
        try
        {
            for (var pass = 0; pass < 64; pass++)
            {
                _optionsDirty = false;
                while (_optionsRow.GetChildCount() > 0) _optionsRow.GetChild(0).Dispose(); _selectedOptions.Clear();
                foreach (var option in _options.ToArray())
                {
                    if (option.Values.Length == 0)
                    {
                        var checkbox = new CheckBox(option.Name) { Name = "Checkbox" + _optionsRow.GetChildCount(), ButtonPressed = option.Default != 0 }; _optionsRow.AddChild(checkbox);
                        _selectedOptions[option.Name] = new(true, option.Default); checkbox.Toggled += value => { if (_options.Contains(option)) _selectedOptions[option.Name] = new(true, value ? 1 : 0); };
                    }
                    else
                    {
                        var group = new VBoxContainer { Name = "Choice" + _optionsRow.GetChildCount() }; var label = new Label { Name = "Label", Text = option.Name }; group.AddChild(label);
                        var choice = new OptionButton { Name = "Choice" }; foreach (var value in option.Values) choice.AddItem(value); choice.Select(option.Default); group.AddChild(choice); _optionsRow.AddChild(group);
                        _selectedOptions[option.Name] = new(false, option.Default); choice.ItemSelected += index => { if (_options.Contains(option)) _selectedOptions[option.Name] = new(false, index); };
                    }
                }
                _optionsRow.Visible = _options.Count != 0; if (!_optionsDirty) break; if (pass == 63) throw new InvalidOperationException("File options did not settle.");
            }
        }
        finally { _updatingOptions = false; }
    }
    /// <summary>Returns a copied shared favorite-directory list on the authoring thread.</summary><returns>Ordered scoped directory paths normalized with trailing separators.</returns>
    public static IReadOnlyList<string> GetFavoriteList() { CheckSharedOwner(); lock (SharedGate) return Array.AsReadOnly((string[])_sharedFavorites.Clone()); }
    /// <summary>Replaces copied shared favorite directories on the authoring thread.</summary><param name="favorites">Ordered scoped paths.</param>
    public static void SetFavoriteList(IReadOnlyList<string> favorites) { CheckSharedOwner(); var copy = CopyValues(favorites).Select(path => path.EndsWith('/') ? path : path + "/").ToArray(); lock (SharedGate) _sharedFavorites = copy; }
    /// <summary>Returns a copied shared recent-directory list on the authoring thread.</summary><returns>Ordered scoped directory paths normalized with trailing separators.</returns>
    public static IReadOnlyList<string> GetRecentList() { CheckSharedOwner(); lock (SharedGate) return Array.AsReadOnly((string[])_sharedRecents.Clone()); }
    /// <summary>Replaces copied shared recent directories on the authoring thread.</summary><param name="recents">Ordered scoped paths.</param>
    public static void SetRecentList(IReadOnlyList<string> recents) { CheckSharedOwner(); var copy = CopyValues(recents).Select(path => path.EndsWith('/') ? path : path + "/").ToArray(); lock (SharedGate) _sharedRecents = copy; }
    /// <summary>Sets the shared file icon provider used by list layout.</summary><param name="callback">A path-to-borrowed-texture provider, or null to use the theme.</param>
    public static void SetGetIconCallback(Func<string, Texture?>? callback) { CheckSharedOwner(); _iconCallback = callback; }
    /// <summary>Sets the shared thumbnail provider used by thumbnail layout.</summary><param name="callback">A path-to-borrowed-texture provider, or null to use the theme.</param>
    public static void SetGetThumbnailCallback(Func<string, Texture?>? callback) { CheckSharedOwner(); _thumbnailCallback = callback; }
}
