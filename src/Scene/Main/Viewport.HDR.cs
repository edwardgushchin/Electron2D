namespace Electron2D;

public abstract partial class Viewport
{
    private bool _hdr, _hdrAssigned;

    internal void ApplyProjectHDR()
    {
        if (_hdrAssigned) return;
        var value = ProjectSettings.GetWithOverride(ProjectSettings.HDR2D);
        if (_hdr == value) return;
        _hdr = value; TextureSizeChanged();
    }

    /// <summary>Gets or sets linear high-dynamic-range canvas rendering.</summary>
    /// <value>False initially. An unassigned root window uses ProjectSettings.HDR2D at startup.</value>
    /// <remarks>Enabled targets store RGBA half-float values without normalized-color clamping. Native sampling,
    /// composition and readback retain linear values; screen presentation applies the output transfer function.
    /// Changes replace native targets at the next submission, preserving the logical texture RID and expiring
    /// borrowed device storage. Valid assignments republish renderer overrides. Compatibility rejects enabled HDR.</remarks>
    /// <exception cref="InvalidOperationException">Mutation violates owner or native-submission rules.</exception>
    /// <exception cref="ObjectDisposedException">The viewport is disposed.</exception>
    /// <exception cref="AggregateException">A texture observer fails after publication; every view is still notified.</exception>
    public bool UseHDR2D
    {
        get { CheckTransformQuery(); return _hdr; }
        set
        {
            EnsureMutable(); RenderingOwner?.EnsureViewportMutation();
            var previous = RenderingOwner?.ViewportHDR(this) ?? _hdr;
            _hdr = value; _hdrAssigned = true; RenderingOwner?.PublishViewportHDR(this);
            var current = RenderingOwner?.ViewportHDR(this) ?? (_hdr || this is Window { HDROutputRequested: true });
            if (previous != current) TextureSizeChanged();
        }
    }

    private static readonly PropertyDescriptor[] HDRProperties =
    [new PropertyDescriptor<Viewport, bool>(nameof(UseHDR2D), n => n.UseHDR2D, (n, v) => n.UseHDR2D = v, _ => false, stored: true)];
}
