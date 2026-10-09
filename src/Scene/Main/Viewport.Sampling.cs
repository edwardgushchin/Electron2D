namespace Electron2D;

public abstract partial class Viewport
{
    /// <summary>Selects the viewport default canvas filtering.</summary>
    public enum DefaultCanvasItemTextureFilter
    {
        /// <summary>Nearest base-level texels.</summary>
        Nearest = 0,
        /// <summary>Linear base-level texels; the default.</summary>
        Linear = 1,
        /// <summary>Linear texels with mip levels.</summary>
        LinearWithMipmaps = 2,
        /// <summary>Nearest texels with mip levels.</summary>
        NearestWithMipmaps = 3,
        /// <summary>Inherits the direct canvas or viewport parent, otherwise linear.</summary>
        ParentNode = 4,
        /// <summary>Sentinel; not a valid choice.</summary>
        Max = 5,
    }

    /// <summary>Selects the viewport default canvas addressing.</summary>
    public enum DefaultCanvasItemTextureRepeat
    {
        /// <summary>Clamps to texture edges; the default.</summary>
        Disabled = 0,
        /// <summary>Repeats the texture.</summary>
        Enabled = 1,
        /// <summary>Reflects alternate repeated tiles.</summary>
        Mirror = 2,
        /// <summary>Inherits the direct canvas or viewport parent, otherwise disabled.</summary>
        ParentNode = 3,
        /// <summary>Sentinel; not a valid choice.</summary>
        Max = 4,
    }

    /// <summary>Sets the maximum anisotropy for canvas filters that request it.</summary>
    public enum AnisotropicFiltering
    {
        /// <summary>Disables anisotropy.</summary>
        Disabled = 0,
        /// <summary>At most two samples.</summary>
        Anisotropy2X = 1,
        /// <summary>At most four samples; the default.</summary>
        Anisotropy4X = 2,
        /// <summary>At most eight samples.</summary>
        Anisotropy8X = 3,
        /// <summary>At most sixteen samples.</summary>
        Anisotropy16X = 4,
        /// <summary>Sentinel; not a valid choice.</summary>
        Max = 5,
    }

    private DefaultCanvasItemTextureFilter _canvasFilter = DefaultCanvasItemTextureFilter.Linear;
    private DefaultCanvasItemTextureRepeat _canvasRepeat;
    private AnisotropicFiltering _anisotropy = (AnisotropicFiltering)ProjectSettings.GetWithOverride(ProjectSettings.AnisotropicFilteringLevel);
    private TextureFilter _filterCache = TextureFilter.Linear;
    private TextureRepeat _repeatCache = TextureRepeat.Disabled;
    private static readonly PropertyDescriptor[] ViewportSamplingProperties =
    [
        new PropertyDescriptor<Viewport, DefaultCanvasItemTextureFilter>(nameof(CanvasItemDefaultTextureFilter), n => n.CanvasItemDefaultTextureFilter, (n, v) => n.CanvasItemDefaultTextureFilter = v, _ => DefaultCanvasItemTextureFilter.Linear, stored: true),
        new PropertyDescriptor<Viewport, DefaultCanvasItemTextureRepeat>(nameof(CanvasItemDefaultTextureRepeat), n => n.CanvasItemDefaultTextureRepeat, (n, v) => n.CanvasItemDefaultTextureRepeat = v, _ => DefaultCanvasItemTextureRepeat.Disabled, stored: true),
        new PropertyDescriptor<Viewport, AnisotropicFiltering>(nameof(AnisotropicFilteringLevel), n => n.AnisotropicFilteringLevel, (n, v) => n.AnisotropicFilteringLevel = v, _ => (AnisotropicFiltering)ProjectSettings.GetWithOverride(ProjectSettings.AnisotropicFilteringLevel), stored: true),
    ];

    /// <summary>Gets or sets filtering used when no canvas ancestor selects an explicit filter.</summary>
    /// <value>Linear by default. ParentNode on a root also resolves to linear.</value>
    /// <remarks>Changes affect the next frame and request redraw through direct inheriting canvas children.
    /// Mipmap modes require GPU rendering; the software compatibility driver rejects linear filtering.
    /// Detached settings become effective on tree entry.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The enum value is invalid or Max.</exception>
    /// <exception cref="InvalidOperationException">An attached viewport is mutated off-owner or during capture.</exception>
    /// <exception cref="ObjectDisposedException">The viewport is disposed.</exception>
    public DefaultCanvasItemTextureFilter CanvasItemDefaultTextureFilter
    {
        get { ThrowIfDisposed(); return _canvasFilter; }
        set
        {
            EnsureMutable();
            if ((uint)value >= (uint)DefaultCanvasItemTextureFilter.Max) throw new ArgumentOutOfRangeException(nameof(value));
            if (_canvasFilter == value) return;
            _canvasFilter = value; UpdateTextureSampling(filter: true);
        }
    }

    /// <summary>Gets or sets addressing used when no canvas ancestor selects explicit repeat behavior.</summary>
    /// <value>Disabled by default. ParentNode on a root also resolves to disabled.</value>
    /// <remarks>Changes affect the next frame and request redraw through direct inheriting canvas children.
    /// Mirror requires GPU rendering; unsupported fallback wrapping fails explicitly.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The enum value is invalid or Max.</exception>
    /// <exception cref="InvalidOperationException">An attached viewport is mutated off-owner or during capture.</exception>
    /// <exception cref="ObjectDisposedException">The viewport is disposed.</exception>
    public DefaultCanvasItemTextureRepeat CanvasItemDefaultTextureRepeat
    {
        get { ThrowIfDisposed(); return _canvasRepeat; }
        set
        {
            EnsureMutable();
            if ((uint)value >= (uint)DefaultCanvasItemTextureRepeat.Max) throw new ArgumentOutOfRangeException(nameof(value));
            if (_canvasRepeat == value) return;
            _canvasRepeat = value; UpdateTextureSampling(filter: false);
        }
    }

    /// <summary>Gets or sets the anisotropy limit used by anisotropic canvas filters.</summary>
    /// <value>Initialized from ProjectSettings.AnisotropicFilteringLevel (four samples by default).</value>
    /// <remarks>Only filters explicitly requesting anisotropy use this limit; Disabled retains ordinary mip filtering.
    /// Changes affect the next submitted frame. Native precision depends on the GPU driver.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The enum value is invalid or Max.</exception>
    /// <exception cref="InvalidOperationException">An attached viewport is mutated off-owner or during capture.</exception>
    /// <exception cref="ObjectDisposedException">The viewport is disposed.</exception>
    public AnisotropicFiltering AnisotropicFilteringLevel
    {
        get { ThrowIfDisposed(); return _anisotropy; }
        set
        {
            EnsureMutable();
            if ((uint)value >= (uint)AnisotropicFiltering.Max) throw new ArgumentOutOfRangeException(nameof(value));
            _anisotropy = value;
        }
    }

    internal TextureFilter TextureFilterInTree
    {
        get
        {
            if (IsInsideTree) _filterCache = _canvasFilter switch
            {
                DefaultCanvasItemTextureFilter.Nearest => TextureFilter.Nearest,
                DefaultCanvasItemTextureFilter.LinearWithMipmaps => TextureFilter.LinearWithMipmaps,
                DefaultCanvasItemTextureFilter.NearestWithMipmaps => TextureFilter.NearestWithMipmaps,
                DefaultCanvasItemTextureFilter.ParentNode => Parent switch
                {
                    CanvasItem item when item.TextureFilterInTree != TextureFilter.ParentNode => item.TextureFilterInTree,
                    Viewport viewport => viewport.TextureFilterInTree,
                    _ => TextureFilter.Linear,
                },
                _ => TextureFilter.Linear,
            };
            return _filterCache;
        }
    }

    internal TextureRepeat TextureRepeatInTree
    {
        get
        {
            if (IsInsideTree) _repeatCache = _canvasRepeat switch
            {
                DefaultCanvasItemTextureRepeat.Enabled => TextureRepeat.Enabled,
                DefaultCanvasItemTextureRepeat.Mirror => TextureRepeat.Mirror,
                DefaultCanvasItemTextureRepeat.ParentNode => Parent switch
                {
                    CanvasItem item when item.TextureRepeatInTree != TextureRepeat.ParentNode => item.TextureRepeatInTree,
                    Viewport viewport => viewport.TextureRepeatInTree,
                    _ => TextureRepeat.Disabled,
                },
                _ => TextureRepeat.Disabled,
            };
            return _repeatCache;
        }
    }

    internal void UpdateTextureSampling(bool filter)
    {
        if (!IsInsideTree) return;
        if (filter) _ = TextureFilterInTree; else _ = TextureRepeatInTree;
        CanvasItem.PropagateTextureSampling(this, filter);
    }

    internal override void OnTreeMembershipChanged(bool entering)
    {
        if (!entering)
        {
            List<Exception>? errors = null;
            try { Tree?.ReleaseGUIViewport(this); } catch (Exception error) { CollectException(ref errors, error); }
            try { base.OnTreeMembershipChanged(false); } catch (Exception error) { CollectException(ref errors, error); }
            ThrowCollected("Viewport detachment callbacks failed.", errors); return;
        }
        Tree!.RegisterGUIViewport(this);
        base.OnTreeMembershipChanged(true);
        UpdateTextureSampling(filter: true); UpdateTextureSampling(filter: false);
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(GUIProperties).Concat(EmbeddedProperties).Concat(TargetProperties).Concat(WorldProperties).Concat(ViewportSamplingProperties).Concat(CanvasRenderingProperties).Concat(CanvasTransformProperties).Concat(ViewportDragProperties).Concat(ViewportAudioProperties).Concat(PickingProperties);
}
