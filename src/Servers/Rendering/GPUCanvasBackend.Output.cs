using SDL3;

namespace Electron2D;

internal sealed unsafe partial class GPUCanvasBackend
{
    internal static Func<nint, (bool HDR, float White, float Headroom)>? OutputForTesting;
    private SDL.GPUSwapchainComposition _outputComposition;
    private CanvasOutput _output;
    private bool _linearOutputAvailable, _pqOutputAvailable;
    private readonly Dictionary<SDL.GPUTextureFormat, RenderHandle> _outputPipelines = [];
    private RenderHandle? _linearPresentationPipeline;

    private CanvasOutput ConfigureOutput(nint window, bool requested, float reference, float maximum,
        DisplayServer.VSyncMode vSync, ref SDL.GPUSwapchainComposition composition)
    {
        var properties = SDL.GetWindowProperties(window);
        var native = OutputForTesting?.Invoke(window) ??
            (SDL.GetBooleanProperty(properties, SDL.Props.WindowHDREnabledBoolean, false),
             SDL.GetFloatProperty(properties, SDL.Props.WindowSDRWhiteLevelFloat, 1),
             SDL.GetFloatProperty(properties, SDL.Props.WindowHDRHeadroomFloat, 1));
        var claimed = window != _window || _windowClaimed;
        var linear = claimed ? SDL.WindowSupportsGPUSwapchainComposition(Device, window, SDL.GPUSwapchainComposition.HDRExtendedLinear) : _linearOutputAvailable;
        var pq = claimed ? SDL.WindowSupportsGPUSwapchainComposition(Device, window, SDL.GPUSwapchainComposition.HDR10ST2084) : _pqOutputAvailable;
        if (window == _window && claimed) { _linearOutputAvailable = linear; _pqOutputAvailable = pq; }
        var supported = claimed && (linear || pq) && native.Item1 && float.IsFinite(native.Item2) && native.Item2 > 0 &&
            float.IsFinite(native.Item3) && native.Item3 > 1 && float.IsFinite(80 * native.Item2 * native.Item3);
        var enabled = requested && supported;
        var next = enabled ? linear ? SDL.GPUSwapchainComposition.HDRExtendedLinear : SDL.GPUSwapchainComposition.HDR10ST2084 : SDL.GPUSwapchainComposition.SDR;
        var result = new CanvasOutput(linear || pq, supported, enabled,
            enabled ? reference < 0 ? 80 * native.Item2 : reference : 0,
            enabled ? maximum < 0 ? 80 * native.Item2 * native.Item3 : maximum : 0);
        if (enabled && !float.IsFinite(result.MaxLinear)) throw new ArgumentOutOfRangeException(nameof(reference), "Output luminance ratio must be finite.");
        if (next != composition && claimed) ApplyVSync(window, vSync, next);
        composition = next;
        return result;
    }

    internal override CanvasOutput ConfigureOutput(bool requested, float reference, float maximum)
        => _output = ConfigureOutput(_window, requested, reference, maximum, _vSync, ref _outputComposition);

    internal CanvasOutput ConfigureOutput(ChildPresentation child, bool requested, float reference, float maximum)
        => child.Output = ConfigureOutput(child.Window, requested, reference, maximum, child.VSync, ref child.Composition);

    private RenderHandle OutputPipeline(SDL.GPUTextureFormat format)
    {
        if (_outputPipelines.TryGetValue(format, out var pipeline)) return pipeline;
        pipeline = CreatePipeline(BuiltInShaders.OutputFragment, BlendMode.Mix, overwrite: true, format: format);
        _outputPipelines.Add(format, pipeline); return pipeline;
    }

    private void DisposeOutput()
    {
        foreach (var pipeline in _outputPipelines.Values) pipeline.Dispose();
        _outputPipelines.Clear(); _linearPresentationPipeline?.Dispose();
    }
}
