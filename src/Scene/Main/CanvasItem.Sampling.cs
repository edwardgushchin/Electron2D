namespace Electron2D;

public abstract partial class CanvasItem
{
    /// <summary>Selects how canvas textures are filtered.</summary>
    public enum TextureFilterEnum
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
    public enum TextureRepeatEnum
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

    private TextureFilterEnum _textureFilter;
    private TextureRepeatEnum _textureRepeat;
    private TextureFilterEnum _textureFilterCache = TextureFilterEnum.Linear;
    private TextureRepeatEnum _textureRepeatCache = TextureRepeatEnum.Disabled;
    private static readonly PropertyDescriptor[] SamplingProperties =
    [
        new PropertyDescriptor<CanvasItem, TextureFilterEnum>(nameof(TextureFilter), n => n.TextureFilter, (n, v) => n.TextureFilter = v, _ => TextureFilterEnum.ParentNode, stored: true),
        new PropertyDescriptor<CanvasItem, TextureRepeatEnum>(nameof(TextureRepeat), n => n.TextureRepeat, (n, v) => n.TextureRepeat = v, _ => TextureRepeatEnum.ParentNode, stored: true),
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
    public TextureFilterEnum TextureFilter
    {
        get { ThrowIfDisposed(); return _textureFilter; }
        set
        {
            EnsureMutable();
            if ((uint)value >= (uint)TextureFilterEnum.Max) throw new ArgumentOutOfRangeException(nameof(value));
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
    public TextureRepeatEnum TextureRepeat
    {
        get { ThrowIfDisposed(); return _textureRepeat; }
        set
        {
            EnsureMutable();
            if ((uint)value >= (uint)TextureRepeatEnum.Max) throw new ArgumentOutOfRangeException(nameof(value));
            if (_textureRepeat == value) return;
            _textureRepeat = value;
            UpdateTextureSampling(filter: false);
            NotifyPropertyListChanged();
        }
    }

    internal TextureFilterEnum TextureFilterInTree
    {
        get
        {
            if (IsInsideTree) _textureFilterCache = _textureFilter == TextureFilterEnum.ParentNode
                ? GetParentItem()?.TextureFilterInTree ?? TextureFilterEnum.ParentNode : _textureFilter;
            return _textureFilterCache;
        }
    }

    internal TextureRepeatEnum TextureRepeatInTree
    {
        get
        {
            if (IsInsideTree) _textureRepeatCache = _textureRepeat == TextureRepeatEnum.ParentNode
                ? GetParentItem()?.TextureRepeatInTree ?? TextureRepeatEnum.ParentNode : _textureRepeat;
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
            if (child is CanvasItem item && (filter ? item._textureFilter == TextureFilterEnum.ParentNode : item._textureRepeat == TextureRepeatEnum.ParentNode))
                item.UpdateTextureSampling(filter);
            else if (child is Viewport viewport && (filter
                ? viewport.CanvasItemDefaultTextureFilter == Viewport.DefaultCanvasItemTextureFilter.ParentNode
                : viewport.CanvasItemDefaultTextureRepeat == Viewport.DefaultCanvasItemTextureRepeat.ParentNode))
                viewport.UpdateTextureSampling(filter);
        }
    }
}
