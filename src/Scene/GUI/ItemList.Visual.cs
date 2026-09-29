namespace Electron2D;

public partial class ItemList
{
    private void WatchIcon(Texture? icon)
    {
        if (icon is null) return;
        if (_watchedIcons.TryGetValue(icon, out var uses)) { _watchedIcons[icon] = uses + 1; return; }
        _watchedIcons.Add(icon, 1);
        icon.Changed += IconChanged; icon.Disposed += IconDisposed;
    }

    private void UnwatchIcon(Texture? icon)
    {
        if (icon is null) return;
        var uses = _watchedIcons[icon];
        if (uses > 1) { _watchedIcons[icon] = uses - 1; return; }
        _watchedIcons.Remove(icon); icon.Changed -= IconChanged; icon.Disposed -= IconDisposed;
    }

    private void IconChanged(Resource _) { if (!IsDisposed) InvalidateListLayout(); }

    private void IconDisposed(ElectronObject resource)
    {
        if (IsDisposed) return;
        foreach (var item in _items)
            if (ReferenceEquals(item.Icon, resource)) item.Icon = null;
        var icon = (Texture)resource;
        icon.Changed -= IconChanged; icon.Disposed -= IconDisposed;
        _watchedIcons.Remove(icon);
        InvalidateListLayout();
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        foreach (var icon in _watchedIcons.Keys) { icon.Changed -= IconChanged; icon.Disposed -= IconDisposed; }
        _watchedIcons.Clear();
        base.Dispose(disposing);
    }

    /// <summary>Gets or sets whether item icons appear above or beside text.</summary>
    /// <value>Left initially.</value>
    public IconMode IconDisplayMode
    {
        get { ThrowIfDisposed(); return _iconMode; }
        set { EnsureMutable(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); if (_iconMode == value) return; _iconMode = value; InvalidateListLayout(); }
    }

    /// <summary>Gets or sets a fixed icon allocation when both components are positive.</summary>
    /// <value>Zero in both axes initially, selecting each icon's intrinsic size.</value>
    public Vector2i FixedIconSize
    {
        get { ThrowIfDisposed(); return _fixedIconSize; }
        set { EnsureMutable(); if (_fixedIconSize == value) return; _fixedIconSize = value; InvalidateListLayout(); }
    }

    /// <summary>Gets or sets the finite scale applied to item icons.</summary>
    /// <value>One initially.</value>
    /// <exception cref="ArgumentOutOfRangeException">The scale is nonfinite.</exception>
    public float IconScale
    {
        get { ThrowIfDisposed(); return _iconScale; }
        set { EnsureMutable(); if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); if (_iconScale == value) return; _iconScale = value; InvalidateListLayout(); }
    }

    /// <summary>Gets the borrowed icon of one item.</summary>
    /// <param name="index">A valid zero-based item index.</param>
    /// <returns>The icon, or null.</returns>
    public Texture? GetItemIcon(int index) { ThrowIfDisposed(); return GetItem(index).Icon; }

    /// <summary>Changes one item's borrowed icon.</summary>
    /// <param name="index">An index; negative values count from the end.</param>
    /// <param name="icon">A live borrowed icon, or null.</param>
    public void SetItemIcon(int index, Texture? icon)
    {
        EnsureMutable(); if (icon?.IsDisposed == true) throw new ObjectDisposedException(nameof(icon));
        var item = GetItem(index, negative: true);
        if (ReferenceEquals(item.Icon, icon)) return;
        UnwatchIcon(item.Icon); item.Icon = icon; WatchIcon(icon); InvalidateListLayout();
    }

    /// <summary>Gets whether an item's icon exchanges its axes while drawing.</summary>
    /// <param name="index">A valid zero-based item index.</param>
    /// <returns>False initially.</returns>
    public bool IsItemIconTransposed(int index) { ThrowIfDisposed(); return GetItem(index).IconTransposed; }

    /// <summary>Changes whether an item's icon exchanges its axes while drawing.</summary>
    /// <param name="index">An index; negative values count from the end.</param>
    /// <param name="transposed">The new orientation.</param>
    public void SetItemIconTransposed(int index, bool transposed)
    {
        EnsureMutable(); var item = GetItem(index, negative: true);
        if (item.IconTransposed == transposed) return; item.IconTransposed = transposed; InvalidateListLayout();
    }

    /// <summary>Gets an item's source icon rectangle.</summary>
    /// <param name="index">A valid zero-based item index.</param>
    /// <returns>Zero by default, selecting the full texture.</returns>
    public Rect2 GetItemIconRegion(int index) { ThrowIfDisposed(); return GetItem(index).IconRegion; }

    /// <summary>Changes the source icon rectangle.</summary>
    /// <param name="index">An index; negative values count from the end.</param>
    /// <param name="region">A finite source region; a zero-size axis selects the full texture.</param>
    public void SetItemIconRegion(int index, Rect2 region)
    {
        EnsureMutable(); if (!region.IsFinite()) throw new ArgumentException("The icon region must be finite.", nameof(region));
        var item = GetItem(index, negative: true);
        if (item.IconRegion == region) return; item.IconRegion = region; InvalidateListLayout();
    }

    /// <summary>Gets the color multiplied into one item's icon.</summary>
    /// <param name="index">A valid zero-based item index.</param>
    /// <returns>White initially.</returns>
    public Color GetItemIconModulate(int index) { ThrowIfDisposed(); return GetItem(index).IconModulate; }

    /// <summary>Changes one item's icon modulation color.</summary>
    /// <param name="index">An index; negative values count from the end.</param>
    /// <param name="color">A finite color multiplier.</param>
    public void SetItemIconModulate(int index, Color color)
    {
        EnsureMutable(); if (!color.IsFinite()) throw new ArgumentException("The icon color must be finite.", nameof(color));
        var item = GetItem(index, negative: true); if (item.IconModulate == color) return; item.IconModulate = color; QueueRedraw();
    }

    /// <summary>Gets one item's custom background color.</summary>
    /// <param name="index">A valid zero-based item index.</param>
    /// <returns>Transparent initially.</returns>
    public Color GetItemCustomBGColor(int index) { ThrowIfDisposed(); return GetItem(index).CustomBG; }

    /// <summary>Changes one item's custom background color.</summary>
    /// <param name="index">An index; negative values count from the end.</param>
    /// <param name="color">A finite color; transparent disables the overlay.</param>
    public void SetItemCustomBGColor(int index, Color color)
    {
        EnsureMutable(); if (!color.IsFinite()) throw new ArgumentException("The background color must be finite.", nameof(color));
        var item = GetItem(index, negative: true); if (item.CustomBG == color) return; item.CustomBG = color; QueueRedraw();
    }

    /// <summary>Gets one item's custom foreground text color.</summary>
    /// <param name="index">A valid zero-based item index.</param>
    /// <returns>Transparent initially, selecting the theme color.</returns>
    public Color GetItemCustomFGColor(int index) { ThrowIfDisposed(); return GetItem(index).CustomFG; }

    /// <summary>Changes one item's custom foreground text color.</summary>
    /// <param name="index">An index; negative values count from the end.</param>
    /// <param name="color">A finite color; zero selects the theme color.</param>
    public void SetItemCustomFGColor(int index, Color color)
    {
        EnsureMutable(); if (!color.IsFinite()) throw new ArgumentException("The text color must be finite.", nameof(color));
        var item = GetItem(index, negative: true); if (item.CustomFG == color) return; item.CustomFG = color; QueueRedraw();
    }

    private Vector2 IconSize(Item item)
    {
        if (item.Icon is null) return Vector2.Zero;
        var size = item.IconRegion.Size.X == 0 || item.IconRegion.Size.Y == 0 ? item.Icon.GetSize() : item.IconRegion.Size.Abs();
        if (item.IconTransposed) size = new(size.Y, size.X);
        if (_fixedIconSize.X > 0 && _fixedIconSize.Y > 0) size = _fixedIconSize;
        return size * _iconScale;
    }

    private Vector2 FittedIconSize(Item item, Vector2 allocation)
    {
        if (_fixedIconSize.X <= 0 || _fixedIconSize.Y <= 0) return allocation;
        var source = item.IconRegion.Size.X == 0 || item.IconRegion.Size.Y == 0
            ? item.Icon!.GetSize() : item.IconRegion.Size.Abs();
        if (item.IconTransposed) source = new(source.Y, source.X);
        if (source.X <= 0 || source.Y <= 0 || allocation.X <= 0 || allocation.Y <= 0) return Vector2.Zero;
        return source * Math.Min(allocation.X / source.X, allocation.Y / source.Y);
    }
}
