namespace Electron2D;

public partial class Window
{
    private bool _hdrOutputRequested, _hdrOutputAssigned;
    private EventPublisher<Action<float>> _publisherOutputMaxLinearValueChanged;

    /// <summary>Gets or requests native HDR presentation independently of current display support.</summary>
    /// <value>False initially. Unassigned roots read ProjectSettings.HDROutputRequested at startup.
    /// Embedded windows follow their containing window while embedded.</value>
    /// <remarks>Requests force effective linear half-float canvas storage. The authored UseHDR2D value and independent
    /// SubViewports are preserved. Unsupported native displays retain the request and present SDR. Compatibility
    /// rejects HDR. Native mode failures preserve state; observer failures propagate after commitment.</remarks>
    /// <exception cref="InvalidOperationException">Mutation violates owner or native submission rules.</exception>
    public bool HDROutputRequested
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return ContainingOutputWindow is { } host ? host.HDROutputRequested : _hdrOutputRequested; }
        set
        {
            EnsureMutable(); RenderingOwner?.EnsureNativeWindowMutation();
            try
            {
                if (_display is { } display) display.SetHDROutputRequestCore(value);
                else CommitHDROutputRequest(value);
            }
            finally { if (_hdrOutputRequested == value) _hdrOutputAssigned = true; }
        }
    }

    private Window? ContainingOutputWindow => Embedder?.GetWindow();

    internal void ApplyProjectHDROutput()
    {
        if (!_hdrOutputAssigned) CommitHDROutputRequest(ProjectSettings.GetWithOverride(ProjectSettings.HDROutputRequested));
    }

    /// <summary>Reads the maximum linear color value of this window's active native output.</summary>
    /// <returns>The HDR luminance ratio, or one for SDR or an inactive window.</returns>
    /// <remarks>Embedded windows observe their containing native output. This read neither creates a device nor synchronizes GPU work.</remarks>
    public float GetOutputMaxLinearValue()
    {
        ThrowIfDisposed(); Tree?.EnsureOwnerThread();
        return ContainingOutputWindow is { } host ? host.GetOutputMaxLinearValue() : _display?.OutputMaxLinear ?? 1;
    }

    /// <summary>Occurs after the output value commits and notification 1013 reaches this window's scene.</summary>
    public event Action<float>? OutputMaxLinearValueChanged
    { add => _publisherOutputMaxLinearValueChanged.Add(value); remove => _publisherOutputMaxLinearValueChanged.Remove(value); }

    internal void CommitHDROutputRequest(bool requested)
    {
        if (_hdrOutputRequested == requested) return;
        _hdrOutputRequested = requested;
        NotifyHDRTargets(this);
    }

    private static void NotifyHDRTargets(Node node)
    {
        List<Exception>? errors = null;
        if (node is Window view)
            try { view.TextureSizeChanged(); } catch (Exception error) { CollectException(ref errors, error); }
        if (!node.IsDisposed) foreach (var child in node.GetChildren(includeInternal: true))
            {
                if (child.IsDisposed || !ReferenceEquals(child.Parent, node) || child is Window { NativeDisplay: not null }) continue;
                try { NotifyHDRTargets(child); } catch (Exception error) { CollectException(ref errors, error); }
            }
        ThrowCollected("HDR target observers failed.", errors);
    }

    private void HandleOutputChanged(float value)
    {
        if (IsDisposed || GetOutputMaxLinearValue() != value) return;
        List<Exception>? errors = null;
        try { PropagateWindowNotification(NotificationWmOutputMaxLinearValueChanged); } catch (Exception error) { CollectException(ref errors, error); }
        try
        {
            var handlers = !IsDisposed && GetOutputMaxLinearValue() == value ? BeginEventEmission(_publisherOutputMaxLinearValueChanged.Handlers) : null;
            if (handlers is not null) foreach (var handler in Delegate.EnumerateInvocationList(handlers))
                    try { handler(value); } catch (Exception error) { CollectException(ref errors, error); }
        }
        catch (Exception error) { CollectException(ref errors, error); }
        try { if (!IsDisposed && GetOutputMaxLinearValue() == value) NotifyEmbeddedOutputs(this, value); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("HDR output observers failed.", errors);
    }

    private static void NotifyEmbeddedOutputs(Node node, float value)
    {
        List<Exception>? errors = null;
        foreach (var child in node.GetChildren(includeInternal: true))
        {
            if (child.IsDisposed || child is Window { NativeDisplay: not null }) continue;
            try
            {
                if (child is Window window) { if (window.ContainingOutputWindow is not null) window.HandleOutputChanged(value); }
                else NotifyEmbeddedOutputs(child, value);
            }
            catch (Exception error) { CollectException(ref errors, error); }
        }
        ThrowCollected("Embedded HDR output observers failed.", errors);
    }

    private static readonly PropertyDescriptor[] OutputProperties =
    [new PropertyDescriptor<Window, bool>(nameof(HDROutputRequested), w => w.HDROutputRequested, (w, v) => w.HDROutputRequested = v, _ => false, stored: true)];
}
