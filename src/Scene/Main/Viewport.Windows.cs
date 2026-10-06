namespace Electron2D;

public abstract partial class Viewport
{
    private bool _embedSubwindows;
    internal readonly List<Window> EmbeddedWindows = [];
    /// <summary>Gets or sets whether child windows are composed into this viewport.</summary>
    /// <value>False initially. Configure before attaching visible windows.</value>
    /// <remarks>Independent native child windows are unavailable. Changing the embedding policy while windows are attached is rejected.</remarks>
    /// <exception cref="InvalidOperationException">An attached child window requires the current policy or mutation is off-owner.</exception>
    public bool GUIEmbedSubwindows
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _embedSubwindows; }
        set { EnsureMutable(); if (_embedSubwindows == value) return; if (EmbeddedWindows.Count != 0) throw new InvalidOperationException("Detach child windows before changing their embedding policy."); _embedSubwindows = value; }
    }
    /// <summary>Returns the visible embedded child windows in back-to-front order.</summary>
    /// <returns>A new array of borrowed window identities.</returns>
    public Window[] GetEmbeddedSubwindows() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return EmbeddedWindows.Where(w => !w.IsDisposed && w.Visible).ToArray(); }
    private static readonly PropertyDescriptor[] EmbeddedProperties =
    [new PropertyDescriptor<Viewport, bool>(nameof(GUIEmbedSubwindows), n => n.GUIEmbedSubwindows, (n, v) => n.GUIEmbedSubwindows = v, _ => false, stored: true)];
}
