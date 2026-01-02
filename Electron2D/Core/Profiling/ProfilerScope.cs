using System;

namespace Electron2D;

/// <summary>
/// Scope-объект для замера CPU-сэмпла профайлера (используется через <c>using</c>).
/// </summary>
public readonly struct ProfilerScope : IDisposable
{
    private readonly ProfilerSystem? _system;
    private readonly ProfilerSampleId _sampleId;

    internal ProfilerScope(ProfilerSystem system, ProfilerSampleId sampleId)
    {
        _system = system;
        _sampleId = sampleId;
    }

    public void Dispose()
    {
        _system?.EndSample(_sampleId);
    }
}