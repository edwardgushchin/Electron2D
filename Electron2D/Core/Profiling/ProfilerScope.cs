namespace Electron2D;

public readonly struct ProfilerScope : IDisposable
{
    private readonly ProfilerSystem? _sys;
    private readonly ProfilerSampleId _id;

    internal ProfilerScope(ProfilerSystem sys, ProfilerSampleId id)
    {
        _sys = sys;
        _id = id;
    }

    public void Dispose()
    {
        _sys?.EndSample(_id);
    }
}