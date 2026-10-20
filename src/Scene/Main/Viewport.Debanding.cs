namespace Electron2D;

public abstract partial class Viewport
{
    private bool _debanding, _debandingAssigned;

    internal void ApplyProjectDebanding()
    {
        if (!_debandingAssigned) _debanding = ProjectSettings.GetWithOverride(ProjectSettings.UseDebanding);
    }

    /// <summary>Gets or sets dithering before HDR canvas output is quantized for the screen.</summary>
    /// <value>False initially. An unassigned root uses ProjectSettings.UseDebanding at startup.</value>
    /// <remarks>The GPU backend applies a stable, zero-centered RGB pattern of one byte quantization step
    /// after the root HDR canvas is encoded for SDR presentation. Alpha, linear viewport textures and movie
    /// capture remain unchanged. In the 2D domain this affects only the root viewport with UseHDR2D enabled;
    /// ordinary byte canvases and offscreen viewports retain their pixels. Compatibility rejects enabled
    /// debanding before drawing. Every valid assignment republishes the authored renderer selection.</remarks>
    /// <exception cref="InvalidOperationException">Mutation violates owner or native-submission rules.</exception>
    /// <exception cref="ObjectDisposedException">The viewport is disposed.</exception>
    public bool UseDebanding
    {
        get { CheckTransformQuery(); return _debanding; }
        set
        {
            EnsureMutable(); RenderingOwner?.EnsureViewportMutation();
            _debanding = value; _debandingAssigned = true; RenderingOwner?.PublishViewportDebanding(this);
        }
    }

    private static readonly PropertyDescriptor[] DebandingProperties =
    [new PropertyDescriptor<Viewport, bool>(nameof(UseDebanding), n => n.UseDebanding, (n, v) => n.UseDebanding = v, _ => false, stored: true)];
}
