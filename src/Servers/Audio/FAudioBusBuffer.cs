namespace Electron2D;

internal sealed class FAudioBusBuffer(int channels, int frames, FAudioBusEffect.Activity activity)
{
    private readonly Vector2[][] _pcm = Enumerable.Range(0, channels / 2).Select(_ => new Vector2[frames]).ToArray();
    internal FAudioBusBuffer? Send;
    internal readonly FAudioBusEffect.Activity Activity = activity;
    internal void Write(ReadOnlySpan<float> pcm)
    {
        for (var pair = 0; pair < _pcm.Length; pair++)
            for (var i = 0; i < pcm.Length / channels; i++) _pcm[pair][i] = new(pcm[i * channels + pair * 2], pcm[i * channels + pair * 2 + 1]);
    }
    internal void AddSend(ReadOnlySpan<float> pcm)
    {
        if (Send is null) return;
        for (var pair = 0; pair < _pcm.Length; pair++)
            if (Activity.Active[pair])
                for (var i = 0; i < pcm.Length / channels; i++) Send._pcm[pair][i] += new Vector2(pcm[i * channels + pair * 2], pcm[i * channels + pair * 2 + 1]);
    }
    internal ReadOnlySpan<Vector2> Read(int pair, int count)
    {
        if (!Activity.WasUsed(pair)) { Activity.Use(pair); _pcm[pair].AsSpan().Clear(); }
        return _pcm[pair].AsSpan(0, count);
    }
}
