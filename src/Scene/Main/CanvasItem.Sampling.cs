namespace Electron2D;

/// <summary>Selects how canvas textures are filtered.</summary>
public enum TextureFilter
{
    /// <summary>Inherits the direct canvas parent, or the containing viewport default.</summary>
    ParentNode = 0,
    /// <summary>Samples the nearest base-level texel.</summary>
    Nearest = 1,
    /// <summary>Interpolates neighboring base-level texels.</summary>
    Linear = 2,
    /// <summary>Uses nearest texels with mip levels for minification.</summary>
    NearestWithMipmaps = 3,
    /// <summary>Uses linear texel filtering with mip levels for minification.</summary>
    LinearWithMipmaps = 4,
    /// <summary>Uses nearest texels, mip levels and viewport-controlled anisotropy.</summary>
    NearestWithMipmapsAnisotropic = 5,
    /// <summary>Uses linear texel filtering, mip levels and viewport-controlled anisotropy.</summary>
    LinearWithMipmapsAnisotropic = 6,
    /// <summary>Sentinel; not a valid filtering choice.</summary>
    Max = 7,
}

/// <summary>Selects addressing outside the texture's normalized rectangle.</summary>
public enum TextureRepeat
{
    /// <summary>Inherits the direct canvas parent, or the containing viewport default.</summary>
    ParentNode = 0,
    /// <summary>Clamps sampling to the texture edge.</summary>
    Disabled = 1,
    /// <summary>Repeats the texture.</summary>
    Enabled = 2,
    /// <summary>Repeats, reflecting alternate tiles.</summary>
    Mirror = 3,
    /// <summary>Sentinel; not a valid repeat choice.</summary>
    Max = 4,
}

public abstract partial class CanvasItem
{
    private TextureFilter _textureFilter;
    private TextureRepeat _textureRepeat;
    private TextureFilter _textureFilterCache = TextureFilter.Linear;
    private TextureRepeat _textureRepeatCache = TextureRepeat.Disabled;
    private static readonly PropertyDescriptor[] SamplingProperties =
    [
        new PropertyDescriptor<CanvasItem, TextureFilter>(nameof(TextureFilter), n => n.TextureFilter, (n, v) => n.TextureFilter = v, _ => TextureFilter.ParentNode, stored: true),
        new PropertyDescriptor<CanvasItem, TextureRepeat>(nameof(TextureRepeat), n => n.TextureRepeat, (n, v) => n.TextureRepeat = v, _ => TextureRepeat.ParentNode, stored: true),
    ];

    /// <summary>Gets or sets filtering for this item's built-in texture sampler.</summary>
    /// <value>ParentNode by default. Neutral parents and TopLevel end canvas inheritance.</value>
    /// <remarks>An actual change requests redraw for attached inheriting canvas descendants, then raises PropertyListChanged
    /// on this item. Detached changes take effect on tree entry. Mipmaps and anisotropy require GPU rendering;
    /// the software compatibility driver also rejects linear filtering. Images without mipmaps use their sole level. Arbitrary material texture parameters retain their own sampler contract.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The enum value is invalid or Max.</exception>
    /// <exception cref="InvalidOperationException">An attached item is mutated off the owner thread or during capture.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    /// <exception cref="Exception">A property-list subscriber throws after the change is committed.</exception>
    public TextureFilter TextureFilter
    {
        get { ThrowIfDisposed(); return _textureFilter; }
        set
        {
            EnsureMutable();
            if ((uint)value >= (uint)TextureFilter.Max) throw new ArgumentOutOfRangeException(nameof(value));
            if (_textureFilter == value) return;
            _textureFilter = value;
            UpdateTextureSampling(filter: true);
            NotifyPropertyListChanged();
        }
    }

    /// <summary>Gets or sets addressing for this item's built-in texture sampler.</summary>
    /// <value>ParentNode by default. Tiled drawing commands override this with Enabled.</value>
    /// <remarks>An actual change requests redraw for attached inheriting canvas descendants, then raises PropertyListChanged.
    /// Detached changes take effect on tree entry. Mirror requires GPU rendering; unsupported fallback wrapping fails explicitly.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The enum value is invalid or Max.</exception>
    /// <exception cref="InvalidOperationException">An attached item is mutated off the owner thread or during capture.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    /// <exception cref="Exception">A property-list subscriber throws after the change is committed.</exception>
    public TextureRepeat TextureRepeat
    {
        get { ThrowIfDisposed(); return _textureRepeat; }
        set
        {
            EnsureMutable();
            if ((uint)value >= (uint)TextureRepeat.Max) throw new ArgumentOutOfRangeException(nameof(value));
            if (_textureRepeat == value) return;
            _textureRepeat = value;
            UpdateTextureSampling(filter: false);
            NotifyPropertyListChanged();
        }
    }

    internal TextureFilter TextureFilterInTree
    {
        get
        {
            if (IsInsideTree) _textureFilterCache = _textureFilter == TextureFilter.ParentNode
                ? GetParentItem()?.TextureFilterInTree ?? TextureFilter.ParentNode : _textureFilter;
            return _textureFilterCache;
        }
    }

    internal TextureRepeat TextureRepeatInTree
    {
        get
        {
            if (IsInsideTree) _textureRepeatCache = _textureRepeat == TextureRepeat.ParentNode
                ? GetParentItem()?.TextureRepeatInTree ?? TextureRepeat.ParentNode : _textureRepeat;
            return _textureRepeatCache;
        }
    }

    internal void UpdateTextureSampling(bool filter)
    {
        if (!IsInsideTree) return;
        if (filter) _ = TextureFilterInTree; else _ = TextureRepeatInTree;
        QueueRedraw();
        PropagateTextureSampling(this, filter);
    }

    internal static void PropagateTextureSampling(Node parent, bool filter)
    {
        foreach (var child in parent.Children)
        {
            if (child is CanvasItem item && (filter ? item._textureFilter == TextureFilter.ParentNode : item._textureRepeat == TextureRepeat.ParentNode))
                item.UpdateTextureSampling(filter);
            else if (child is Viewport viewport && (filter
                ? viewport.CanvasItemDefaultTextureFilter == Viewport.DefaultCanvasItemTextureFilter.ParentNode
                : viewport.CanvasItemDefaultTextureRepeat == Viewport.DefaultCanvasItemTextureRepeat.ParentNode))
                viewport.UpdateTextureSampling(filter);
        }
    }
}
