namespace Electron2D;

public abstract partial class Viewport
{
    private bool _mouseInViewport;
    private bool _handleInputLocally = true, _guiDisableInput;
    /// <summary>Gets or sets whether handled input belongs to this viewport instead of its containing window or topmost viewport.</summary>
    /// <value>True initially. SubViewportContainer sets false on direct children.</value>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">This viewport is disposed.</exception>
    public bool HandleInputLocally { get { ThrowIfDisposed(); return _handleInputLocally; } set { EnsureMutable(); _handleInputLocally = value; } }
    /// <summary>Gets or sets whether this viewport ignores input dispatch.</summary>
    /// <value>False initially. Disabling clears hover and mouse capture; focus remains stored.</value>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">This viewport is disposed.</exception>
    public bool GUIDisableInput { get { ThrowIfDisposed(); return _guiDisableInput; } set { EnsureMutable(); if (_guiDisableInput == value) return; _guiDisableInput = value; if (value) Tree?.DisableGUIViewport(this); } }
    /// <summary>Returns this viewport's currently hovered control.</summary>
    /// <returns>A borrowed control, or null when detached or outside its canvas.</returns>
    /// <exception cref="InvalidOperationException">The attached scene is queried off-owner.</exception>
    /// <exception cref="ObjectDisposedException">This viewport is disposed.</exception>
    public Control? GUIGetHoveredControl() { ThrowIfDisposed(); return Tree?.GetGUIHoveredControl(this); }
    /// <summary>Notifies this viewport that its pointer entered the displayed area.</summary>
    /// <remarks>Equal entry notifications are silent. The notification records entry even while detached; embedded containers also refresh hover at transformed positions.</remarks>
    /// <exception cref="InvalidOperationException">The scene is accessed off-owner.</exception>
    /// <exception cref="ObjectDisposedException">This viewport is disposed.</exception>
    public void NotifyMouseEntered() { EnsureMutable(); if (!_mouseInViewport) DispatchNotification(Node.NotificationVPMouseEnter); }
    /// <summary>Notifies this viewport that its pointer left, clearing embedded hover and tooltips.</summary>
    /// <exception cref="InvalidOperationException">The scene is accessed off-owner.</exception>
    /// <exception cref="ObjectDisposedException">This viewport is disposed.</exception>
    public void NotifyMouseExited() { EnsureMutable(); if (_mouseInViewport && !GUIDisableInput && IsInsideTree) { try { Tree!.ExitGUIViewport(this); } finally { DispatchNotification(Node.NotificationVPMouseExit); } } }
    private static readonly PropertyDescriptor[] GUIProperties =
    [new PropertyDescriptor<Viewport, bool>(nameof(HandleInputLocally), n => n.HandleInputLocally, (n,v) => n.HandleInputLocally=v, _ => true, stored:true),
     new PropertyDescriptor<Viewport, bool>(nameof(GUIDisableInput), n => n.GUIDisableInput, (n,v) => n.GUIDisableInput=v, _ => false, stored:true)];
}
