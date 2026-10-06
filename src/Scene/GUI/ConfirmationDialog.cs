namespace Electron2D;

/// <summary>An embedded acceptance dialog with a built-in cancellation button.</summary>
public class ConfirmationDialog : AcceptDialog
{
    private readonly Button _cancel;
    /// <summary>Creates a hidden confirmation dialog with a 200 by 70 minimum and a 200 by 100 client area.</summary>
    public ConfirmationDialog() { Title = "Please Confirm..."; MinSize = new(200, 70); Size = new(200, 100); _cancel = AddCancelButton(""); }
    /// <summary>Gets or sets the owned cancellation button's caption.</summary>
    /// <value>Cancel initially.</value>
    /// <exception cref="ArgumentNullException">The caption is null.</exception>
    public string CancelButtonText { get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _cancel.Text; } set { EnsureMutable(); _cancel.Text = value; } }
    /// <summary>Returns the borrowed built-in cancellation button.</summary>
    /// <returns>The stable cancel button, subject to RemoveButton ownership transfer.</returns>
    public Button GetCancelButton() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _cancel; }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(ConfirmationDialog) ? CreateConfirmation : base.CreateSceneInstanceFactory();
    private static Node CreateConfirmation() => new ConfirmationDialog();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors())
        {
            if (property.Name == nameof(Title)) yield return new PropertyDescriptor<ConfirmationDialog, string>(nameof(Title), d => d.Title, (d, v) => d.Title = v, _ => "Please Confirm...", stored: true);
            else if (property.Name == nameof(MinSize)) yield return new PropertyDescriptor<ConfirmationDialog, Vector2i>(nameof(MinSize), d => d.MinSize, (d, v) => d.MinSize = v, _ => new(200, 70), stored: true);
            else if (property.Name == nameof(Size)) yield return new PropertyDescriptor<ConfirmationDialog, Vector2i>(nameof(Size), d => d.Size, (d, v) => d.Size = v, _ => new(200, 100), stored: true);
            else yield return property;
        }
        yield return new PropertyDescriptor<ConfirmationDialog, string>(nameof(CancelButtonText), d => d.CancelButtonText, (d, v) => d.CancelButtonText = v, _ => "Cancel", stored: true);
    }
}
