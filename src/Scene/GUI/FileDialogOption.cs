namespace Electron2D;

/// <summary>An immutable native file-dialog checkbox or choice description.</summary>
public sealed class FileDialogOption
{
    internal readonly string[] Values;
    /// <summary>Creates an option, copying its choice captions.</summary>
    /// <param name="name">Nonempty label and result key.</param>
    /// <param name="values">Nonempty choice captions, or an empty list for a checkbox.</param>
    /// <param name="defaultValueIndex">Initial index, clamped to the choice range or checkbox zero/one.</param>
    /// <exception cref="ArgumentNullException">The name, values or a caption is null.</exception>
    /// <exception cref="ArgumentException">The name or a caption is empty or contains a null character.</exception>
    public FileDialogOption(string name, IReadOnlyList<string> values, int defaultValueIndex = 0)
    {
        ValidateLabel(name); ArgumentNullException.ThrowIfNull(values);
        Values = values.ToArray(); foreach (var value in Values) ValidateLabel(value);
        Name = name; DefaultValueIndex = Math.Clamp(defaultValueIndex, 0, Values.Length == 0 ? 1 : Values.Length - 1);
    }
    private static void ValidateLabel(string label)
    { ArgumentException.ThrowIfNullOrEmpty(label); OS.ValidateNativeMessage(label, nameof(label)); }
    /// <summary>Gets the option's label and result key.</summary>
    /// <value>The copied nonempty label.</value>
    public string Name { get; }
    /// <summary>Gets the clamped initial choice index or checkbox zero/one.</summary>
    /// <value>The initial selection.</value>
    public int DefaultValueIndex { get; }
    /// <summary>Returns an independent copy of the choice captions.</summary>
    /// <returns>Choice captions, or an empty array for a checkbox.</returns>
    public string[] GetValues() => (string[])Values.Clone();
}
