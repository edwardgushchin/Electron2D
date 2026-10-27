using System.Runtime.ExceptionServices;

namespace Electron2D;

public sealed partial class DisplayServer
{
    private CanvasBackend? _outputBackend;
    private GPUCanvasBackend.ChildPresentation? _outputChild;
    private bool _hdrOutputRequested;
    private float _hdrReference = -1, _hdrMaximum = -1;
    private CanvasOutput _output;
    private ulong _outputRevision;
    private EventPublisher<Action<float>> _publisherOutputChanged;
    internal event Action<float>? OutputChangedCore { add => _publisherOutputChanged.Add(value); remove => _publisherOutputChanged.Remove(value); }

    private static DisplayServer OutputWindow(int windowID)
    {
        var display = RequireService().ResolveWindow(windowID);
        display.ValidateWindowEventAccess(); return display;
    }

    /// <summary>Reads whether native HDR output was requested for this window.</summary>
    /// <param name="windowID">Active native window identity; zero selects main.</param><returns>The retained request, independently of support or active output.</returns>
    public static bool WindowIsHDROutputRequested(int windowID = MainWindowId) => OutputWindow(windowID)._hdrOutputRequested;
    /// <summary>Reads whether the current renderer, native surface and display support HDR.</summary>
    /// <param name="windowID">Active native window identity; zero selects main.</param><returns>The latest committed capability.</returns>
    public static bool WindowIsHDROutputSupported(int windowID = MainWindowId) => OutputWindow(windowID)._output.Supported;
    /// <summary>Reads whether the selected window currently presents HDR.</summary>
    /// <param name="windowID">Active native window identity; zero selects main.</param><returns>True only for a requested, supported and selected HDR swapchain.</returns>
    public static bool WindowIsHDROutputEnabled(int windowID = MainWindowId) => OutputWindow(windowID)._output.Enabled;
    /// <summary>Requests HDR presentation, retaining the request while the native display is unavailable.</summary>
    /// <param name="enable">Requested HDR policy.</param><param name="windowID">Active native window identity; zero selects main.</param>
    /// <remarks>Requires the display owner outside native submission. The Window canvas uses linear half-float
    /// storage while requested; independent SubViewports retain their own policy. Native capability events can
    /// change the active mode. Native failures preserve prior state; observer failures propagate after commitment.</remarks>
    /// <exception cref="NotSupportedException">The compatibility renderer cannot present HDR.</exception>
    public static void WindowRequestHDROutput(bool enable, int windowID = MainWindowId) => OutputWindow(windowID).ChangeOutput(enable, null, null);
    /// <summary>Reads the authored HDR reference white luminance in nits.</summary>
    /// <param name="windowID">Active native window identity; zero selects main.</param><returns>A positive override, or a negative automatic policy; initially minus one.</returns>
    public static float WindowGetHDROutputReferenceLuminance(int windowID = MainWindowId) => OutputWindow(windowID)._hdrReference;
    /// <summary>Reads the authored HDR maximum luminance in nits.</summary>
    /// <param name="windowID">Active native window identity; zero selects main.</param><returns>A positive override, or a negative automatic policy; initially minus one.</returns>
    public static float WindowGetHDROutputMaxLuminance(int windowID = MainWindowId) => OutputWindow(windowID)._hdrMaximum;
    /// <summary>Reads current proportional HDR reference white luminance in nits.</summary>
    /// <param name="windowID">Active native window identity; zero selects main.</param><returns>The applied native or authored value, or zero with SDR output.</returns>
    public static float WindowGetHDROutputCurrentReferenceLuminance(int windowID = MainWindowId) => OutputWindow(windowID)._output.Reference;
    /// <summary>Reads current proportional HDR maximum luminance in nits.</summary>
    /// <param name="windowID">Active native window identity; zero selects main.</param><returns>The applied native or authored value, or zero with SDR output.</returns>
    public static float WindowGetHDROutputCurrentMaxLuminance(int windowID = MainWindowId) => OutputWindow(windowID)._output.Maximum;
    /// <summary>Reads the maximum linear color component of the active output.</summary>
    /// <param name="windowID">Active native window identity; zero selects main.</param><returns>Maximum/reference for HDR, otherwise one.</returns>
    public static float WindowGetOutputMaxLinearValue(int windowID = MainWindowId) => OutputWindow(windowID)._output.MaxLinear;
    /// <summary>Selects a positive HDR reference white in nits, or a negative automatic policy.</summary>
    /// <param name="referenceLuminance">Finite nonzero reference white policy.</param><param name="windowID">Active native window identity; zero selects main.</param>
    /// <exception cref="ArgumentOutOfRangeException">The value or resulting luminance ratio is invalid.</exception>
    public static void WindowSetHDROutputReferenceLuminance(float referenceLuminance, int windowID = MainWindowId)
        => OutputWindow(windowID).ChangeOutput(null, ValidateLuminance(referenceLuminance), null);
    /// <summary>Selects a positive HDR peak in nits, or a negative automatic policy.</summary>
    /// <param name="maxLuminance">Finite nonzero maximum luminance policy.</param><param name="windowID">Active native window identity; zero selects main.</param>
    /// <exception cref="ArgumentOutOfRangeException">The value or resulting luminance ratio is invalid.</exception>
    public static void WindowSetHDROutputMaxLuminance(float maxLuminance, int windowID = MainWindowId)
        => OutputWindow(windowID).ChangeOutput(null, null, ValidateLuminance(maxLuminance));

    private static float ValidateLuminance(float value)
    {
        if (!float.IsFinite(value) || value == 0) throw new ArgumentOutOfRangeException(nameof(value));
        return value;
    }
    internal void AttachOutputPolicy(CanvasBackend backend, GPUCanvasBackend.ChildPresentation? child = null)
    {
        _outputBackend = backend; _outputChild = child;
        try { RefreshOutput(); } catch { _outputBackend = null; _outputChild = null; throw; }
    }
    internal void RefreshOutput() => ChangeOutput(null, null, null);
    internal void SetHDROutputRequestCore(bool requested) => ChangeOutput(requested, null, null);
    internal float OutputMaxLinear => _output.MaxLinear;

    private void ChangeOutput(bool? requested, float? reference, float? maximum)
    {
        ValidateWindowEventAccess(); RenderingServer.Service?.EnsureNativeWindowMutation();
        var nextRequest = requested ?? _hdrOutputRequested;
        var nextReference = reference ?? _hdrReference; var nextMaximum = maximum ?? _hdrMaximum;
        var next = _outputChild is { } child ? ((GPUCanvasBackend)_outputBackend!).ConfigureOutput(child, nextRequest, nextReference, nextMaximum)
            : _outputBackend?.ConfigureOutput(nextRequest, nextReference, nextMaximum) ?? default;
        if (nextRequest == _hdrOutputRequested && nextReference == _hdrReference && nextMaximum == _hdrMaximum && next == _output) return;
        var previous = _output.MaxLinear;
        _hdrOutputRequested = nextRequest; _hdrReference = nextReference; _hdrMaximum = nextMaximum; _output = next;
        var revision = ++_outputRevision;
        RenderingServer.NotifyRenderMutation();
        Exception? failure = null;
        try { SceneWindow?.CommitHDROutputRequest(nextRequest); } catch (Exception error) { failure = error; }
        try
        {
            if (previous != next.MaxLinear)
                PublishOutput(next.MaxLinear, revision);
        }
        catch (Exception error) { if (failure is not null) throw new AggregateException(failure, error); throw; }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private void PublishOutput(float value, ulong revision)
    {
        var publisher = _windowEvents;
        var local = BeginEventEmission(_publisherOutputChanged.Handlers); var scoped = publisher?.OutputHandlers;
        Exception? failure = null;
        try { local?.Invoke(value); } catch (Exception error) { failure = error; }
        try { if (revision == _outputRevision && publisher is { Available: true }) scoped?.Invoke(value); }
        catch (Exception error) { if (failure is not null) throw new AggregateException(failure, error); throw; }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
