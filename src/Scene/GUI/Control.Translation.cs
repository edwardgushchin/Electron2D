namespace Electron2D;

public partial class Control
{
    private string _translationContext = string.Empty;

    /// <summary>Gets or sets the default context used by this control's automatic message translation.</summary>
    /// <value>An empty string initially. The value is local to this control and is stored in scene files.</value>
    /// <remarks>A null or empty context passed to <see cref="Node.Atr"/> or <see cref="Node.AtrN"/> uses this value;
    /// a nonempty argument overrides it. Explicit <see cref="ElectronObject.Tr"/> and <see cref="ElectronObject.TrN"/>
    /// calls keep their supplied context. Setting this property does not invalidate cached captions or send a
    /// translation-change notification; set it before assigning text, or explicitly notify the control afterward.</remarks>
    /// <exception cref="ArgumentNullException">The assigned value is null.</exception>
    /// <exception cref="InvalidOperationException">An attached access is off the scene owner thread, or a mutation occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public string TranslationContext
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _translationContext; }
        set { EnsureMutable(); ArgumentNullException.ThrowIfNull(value); _translationContext = value; }
    }

    internal override string? ResolveAutomaticTranslationContext(string? context)
        => string.IsNullOrEmpty(context) ? _translationContext : context;
}
