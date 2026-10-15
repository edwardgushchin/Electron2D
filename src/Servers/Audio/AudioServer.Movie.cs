namespace Electron2D;

public sealed partial class AudioServer
{
    private int _movieRate, _movieChannels;
    internal void PrepareMovie(int rate, int channels)
    {
        Check();
        if (_players.Count != 0 || _native?.HasStandaloneSamples == true)
            throw new InvalidOperationException("Movie recording must start before attaching independent native playback.");
        if (_native is not null) CloseNative(closeInput: false);
        _movieRate = rate; _movieChannels = channels;
        try { EnsureNative(); }
        catch { _movieRate = _movieChannels = 0; throw; }
    }
    internal void ReadMoviePCM(Span<int> output) { Check(); Native.ReadMoviePCM(output); }
}
