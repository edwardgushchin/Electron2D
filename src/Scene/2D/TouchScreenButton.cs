namespace Electron2D;

/// <summary>Displays a scene-space button driven by one touch contact and an optional named input action.</summary>
/// <remarks>Textures, bitmap and shape are borrowed. Shape and bitmap regions form a union; the normal-texture rectangle
/// is the fallback without either mask. Passby pressing follows one contact entering/leaving that region.
/// Attached access uses the scene owner; runtime contact/action state is not stored.</remarks>
public partial class TouchScreenButton : Entity
{
    private Texture? _normal, _pressedTexture;
    private BitMap? _bitmask;
    private Shape? _shape;
    private readonly RectangleShape _pixel = new() { Size = Vector2.One };
    private readonly VirtualActionBinding _binding;
    private bool IsEdited => Tree?.EditedSceneRoot is { } root && (ReferenceEquals(root, this) || root.IsAncestorOf(this));
    private int? _finger;
    private ulong _revision;
    private string _action = "";
    private bool _passby, _shapeCentered = true, _shapeVisible = true;
    private TouchButtonVisibility _visibility;
    private readonly List<Texture> _resident = [];
    /// <summary>Creates an unpressed touch button with no visual or action resource.</summary>
    public TouchScreenButton() { _binding = new(this, 0); }
    private void CheckTouch() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    private static void Live(Resource? resource) { if (resource is { IsDisposed: true }) throw new ObjectDisposedException(nameof(resource)); }
    private bool DeviceVisible => _visibility == TouchButtonVisibility.Always || DisplayServer.Service?.IsTouchscreenAvailableCore() == true;
    /// <summary>Gets or assigns the borrowed normal texture.</summary>
    /// <value>Null initially; it also supplies the fallback hit rectangle and shape center.</value>
    public Texture? TextureNormal { get { CheckTouch(); return _normal; } set => SetTexture(ref _normal, value); }
    /// <summary>Gets or assigns the borrowed pressed texture.</summary>
    /// <value>Null initially; normal texture is used while pressed when this is absent.</value>
    public Texture? TexturePressed { get { CheckTouch(); return _pressedTexture; } set => SetTexture(ref _pressedTexture, value); }
    private void SetTexture(ref Texture? target, Texture? value)
    {
        EnsureMutable(); Live(value); if (ReferenceEquals(target, value)) return;
        if (target is not null) target.Changed -= ResourceChanged; target = value;
        if (target is not null) target.Changed += ResourceChanged;
        ReleaseTextures(); RetainTextures(); QueueRedraw();
    }
    private void ResourceChanged(Resource _) { if (!IsDisposed) InvalidateCanvas(); }
    private void RetainTextures()
    {
        if (!IsInsideTree) return;
        foreach (var texture in new[] { _normal, _pressedTexture }) if (texture is { IsDisposed: false } && !_resident.Contains(texture)) { texture.AcquireRendererCacheResidency(); _resident.Add(texture); }
    }
    private void ReleaseTextures() { foreach (var texture in _resident) texture.ReleaseRendererCacheResidency(); _resident.Clear(); }
    /// <summary>Gets or assigns a borrowed bitmap hit region.</summary>
    /// <value>Null initially. Set pixels combine with the shape region.</value>
    public BitMap? Bitmask { get { CheckTouch(); return _bitmask; } set { EnsureMutable(); Live(value); _bitmask = value; } }
    /// <summary>Gets or assigns borrowed local collision geometry used as a hit region.</summary>
    /// <value>Null initially. The owned unit pixel query shape remains internal.</value>
    public Shape? Shape
    {
        get { CheckTouch(); return _shape; }
        set { EnsureMutable(); Live(value); if (ReferenceEquals(_shape, value)) return; if (_shape is not null) _shape.Changed -= ResourceChanged; _shape = value; if (_shape is not null) _shape.Changed += ResourceChanged; QueueRedraw(); }
    }
    /// <summary>Gets or sets whether the shape origin is centered on the normal texture.</summary>
    /// <value>True initially; absent normal texture leaves the origin at zero.</value>
    public bool ShapeCentered { get { CheckTouch(); return _shapeCentered; } set { EnsureMutable(); _shapeCentered = value; QueueRedraw(); } }
    /// <summary>Gets or sets whether shape outlines are eligible for collision-debug drawing.</summary>
    /// <value>True initially. This does not enable collision debugging itself.</value>
    public bool ShapeVisible { get { CheckTouch(); return _shapeVisible; } set { EnsureMutable(); _shapeVisible = value; QueueRedraw(); } }
    /// <summary>Gets or sets whether dragging into/out of the hit region presses/releases this button.</summary>
    /// <value>False initially; a normal press remains active until that contact releases.</value>
    public bool PassbyPress { get { CheckTouch(); return _passby; } set { EnsureMutable(); _passby = value; } }
    /// <summary>Gets or sets the device availability policy.</summary>
    /// <value>Always initially. TouchscreenOnly disables both input and decoration on unavailable hosts.</value>
    public TouchButtonVisibility VisibilityMode
    {
        get { CheckTouch(); return _visibility; }
        set { EnsureMutable(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); _visibility = value; try { if (!DeviceVisible) Release(); } finally { QueueRedraw(); } }
    }
    /// <summary>Gets or sets the named input action.</summary>
    /// <value>Empty initially. Unknown names do not publish state and appear in configuration warnings.</value>
    public string Action
    {
        get { CheckTouch(); return _action; }
        set
        {
            EnsureMutable(); ArgumentNullException.ThrowIfNull(value); if (_action == value) return;
            var previous = _binding.Action; _binding.Release(); _action = value; if (_finger is not null) _binding.Set(value, 1); var revision = ++_revision; List<Exception>? errors = null;
            try { if (previous.Length != 0) Tree?.QueueVirtualAction(this, previous, false); } catch (Exception error) { CollectException(ref errors, error); }
            try { if (revision == _revision && _finger is not null && _binding.Action.Length != 0) Tree?.QueueVirtualAction(this, _binding.Action, true); } catch (Exception error) { CollectException(ref errors, error); }
            ThrowCollected("Touch action rebinding callbacks failed.", errors);
        }
    }
    /// <summary>Occurs after a contact and its action contribution are committed.</summary>
    public event Action? Pressed;
    /// <summary>Occurs after a live contact is released, including lifecycle cancellation.</summary>
    public event Action? Released;
    /// <summary>Tests whether this button currently owns a touch contact.</summary>
    /// <returns>True while one signed contact index is active.</returns>
    public bool IsPressed() { CheckTouch(); return _finger is not null; }
    private bool Hit(Vector2 position)
    {
        var point = GetGlobalTransformWithCanvas().AffineInverse() * position;
        if (!point.IsFinite()) throw new ArgumentOutOfRangeException(nameof(position));
        if (_shape is { } shape)
        {
            var center = _shapeCentered && _normal is { } normal ? normal.GetSize() * .5f : Vector2.Zero;
            if (shape.Collide(new(0, center), _pixel, new(0, point + new Vector2(.5f, .5f)))) return true;
        }
        if (_bitmask is { } mask && new Rect2(Vector2.Zero, mask.GetSize()).HasPoint(point) && mask.GetBit((int)point.X, (int)point.Y)) return true;
        return _shape is null && _bitmask is null && _normal is { } texture && new Rect2(Vector2.Zero, texture.GetSize()).HasPoint(point);
    }
    private void Press(int finger)
    {
        if (_finger is not null || !InputEnabled || GetViewport()?.GUIDisableInput == true || IsEdited || !IsInsideTree || !IsVisibleInTree || !DeviceVisible) return; _finger = finger; SetInternalProcessing(true, false); _revision++; _binding.Set(_action, 1); List<Exception>? errors = null;
        try { if (_binding.Action.Length != 0) Tree?.QueueVirtualAction(this, _binding.Action, true); } catch (Exception error) { CollectException(ref errors, error); }
        try { if (!IsDisposed && _finger == finger) BeginEventEmission(Pressed)?.Invoke(); } catch (Exception error) { CollectException(ref errors, error); }
        try { if (!IsDisposed) QueueRedraw(); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Touch press callbacks failed.", errors);
    }
    private void Release(bool exiting = false)
    {
        if (_finger is null) { _binding.Release(); return; }
        _finger = null; SetInternalProcessing(false, false); _revision++; var action = _binding.Action; _binding.Release(); List<Exception>? errors = null;
        try { if (!exiting && action.Length != 0) Tree?.QueueVirtualAction(this, action, false); } catch (Exception error) { CollectException(ref errors, error); }
        try { if (!exiting && !IsDisposed) BeginEventEmission(Released)?.Invoke(); } catch (Exception error) { CollectException(ref errors, error); }
        try { if (!exiting && !IsDisposed) QueueRedraw(); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Touch release callbacks failed.", errors);
    }
    /// <inheritdoc />
    protected override void OnInput(InputEvent input)
    {
        if (IsEdited) { Release(exiting: true); return; }
        if (!IsInsideTree || !IsVisibleInTree || !DeviceVisible) return;
        if (input is InputEventScreenTouch touch)
        {
            if ((!touch.Pressed || touch.Canceled) && _finger == touch.Index) { Release(); return; }
            if (touch.Pressed && !touch.Canceled && (_finger is null || _finger == touch.Index))
            { var hit = Hit(touch.Position); if (hit && _finger is null) Press(touch.Index); else if (_passby && !hit && _finger is not null) Release(); }
        }
        else if (_passby && input is InputEventScreenDrag drag && (_finger is null || _finger == drag.Index))
        { var hit = Hit(drag.Position); if (hit && _finger is null) Press(drag.Index); else if (!hit && _finger is not null) Release(); }
    }
    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        List<Exception>? errors = null; try { base.OnNotification(what); } catch (Exception error) { CollectException(ref errors, error); }
        if (!IsDisposed) try
            {
                if (what == NotificationEnterTree) { InputEnabled = true; RetainTextures(); }
                else if (what == NotificationInternalProcess && _finger is not null && (!InputEnabled || GetViewport()?.GUIDisableInput == true || IsEdited)) Release(exiting: IsEdited);
                else if (what == NotificationExitTree) { Release(exiting: true); ReleaseTextures(); }
                else if (what is NotificationPaused or NotificationDisabled or NotificationWmWindowFocusOut or NotificationApplicationFocusOut || what == NotificationVisibilityChanged && !IsVisibleInTree) Release();
                else if (what == NotificationDraw && IsInsideTree && (IsEdited || DeviceVisible))
                {
                    var texture = _finger is not null ? _pressedTexture ?? _normal : _normal; if (texture is not null) DrawTexture(texture, Vector2.Zero);
                    if (_shapeVisible && Tree is { } tree && (IsEdited || tree.DebugCollisionsHint) && _shape is { } shape)
                    {
                        var center = _shapeCentered && _normal is { } normal ? normal.GetSize() * .5f : Vector2.Zero;
                        DrawSetTransform(center); try { shape.DrawToCanvas(this, tree.DebugCollisionsColor); } finally { DrawSetTransformMatrix(Transform.Identity); }
                    }
                }
            }
            catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Touch button notification callbacks failed.", errors);
    }
    /// <inheritdoc />
    public override string[] GetConfigurationWarnings()
    { var warnings = base.GetConfigurationWarnings(); return _action.Length != 0 && !InputMap.HasAction(_action) ? [.. warnings, "The touch-button input action is not registered."] : warnings; }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        _binding.Release(); _finger = null; ReleaseTextures(); if (_normal is not null) _normal.Changed -= ResourceChanged; if (_pressedTexture is not null) _pressedTexture.Changed -= ResourceChanged; if (_shape is not null) _shape.Changed -= ResourceChanged;
        try { base.Dispose(disposing); } finally { _pixel.Dispose(); Pressed = null; Released = null; _normal = _pressedTexture = null; _shape = null; _bitmask = null; }
    }
}
