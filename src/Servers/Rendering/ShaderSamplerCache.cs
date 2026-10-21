using SDL3;

namespace Electron2D;

internal sealed class ShaderSamplerCache(RenderHandle device, string driver, bool anisotropyAvailable = true) : IDisposable
{
    private readonly Dictionary<SamplerConfiguration, RenderHandle> _samplers = [];
    private HashSet<SamplerConfiguration> _used = [], _previous = [];

    internal nint Get(SamplerConfiguration configuration)
    {
        if (!_samplers.TryGetValue(configuration, out var sampler))
        {
            var info = configuration.Native(driver, anisotropyAvailable);
            sampler = new(SDL.CreateGPUSampler(device.DangerousGetHandle(), in info),
                h => SDL.ReleaseGPUSampler(device.DangerousGetHandle(), h), device);
            _samplers.Add(configuration, sampler);
        }
        _used.Add(configuration);
        return sampler.DangerousGetHandle();
    }

    internal void AdvanceEpoch()
    {
        foreach (var entry in _samplers)
            if (!_used.Contains(entry.Key) && !_previous.Contains(entry.Key))
            {
                entry.Value.Dispose(); _samplers.Remove(entry.Key);
            }
        (_used, _previous) = (_previous, _used); _used.Clear();
    }

    public void Dispose()
    {
        foreach (var sampler in _samplers.Values) sampler.Dispose();
        _samplers.Clear(); _used.Clear(); _previous.Clear();
    }
}
