namespace Electron2D;

/// <summary>A typed checkbox or choice result from one file-dialog option.</summary>
public readonly struct FileDialogOptionValue
{
    internal FileDialogOptionValue(bool checkbox, int index) { IsCheckbox = checkbox; SelectedIndex = index; }
    /// <summary>Reports whether this result is a checkbox rather than a choice index.</summary>
    /// <value>The kind of the authoring option.</value>
    public bool IsCheckbox { get; }
    /// <summary>Gets the selected choice index, or zero/one for an unchecked/checked checkbox.</summary>
    /// <value>The committed option result.</value>
    public int SelectedIndex { get; }
    /// <summary>Gets the checkbox value.</summary>
    /// <value>True when the checkbox is checked.</value>
    /// <exception cref="InvalidOperationException">This result belongs to a choice option.</exception>
    public bool Checked => IsCheckbox ? SelectedIndex != 0 : throw new InvalidOperationException("The option is a choice.");
}
