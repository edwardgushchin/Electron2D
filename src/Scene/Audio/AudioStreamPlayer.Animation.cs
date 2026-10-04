namespace Electron2D;

public partial class AudioStreamPlayer
{
    internal FAudioStreamVoice PrepareAnimationVoice(AudioStreamPlayback playback)
    {
        EnsureMutable(); if (!IsInsideTree) throw new InvalidOperationException("Audio tracks require an attached receiver.");
        var voice = CreateVoice(playback); _voices.Add(voice); _ages.Add(0); return voice;
    }
    internal bool HasAnimationVoice(FAudioStreamVoice voice) => !IsDisposed && _voices.Contains(voice);
    internal void StartAnimationVoice(AudioStream stream, FAudioStreamVoice voice)
    {
        EnsureMutable(); if (!HasAnimationVoice(voice)) throw new InvalidOperationException("Audio receiver storage changed; rebuild animation caches.");
        _audioOperation = true;
        try
        {
            if (!ReferenceEquals(_stream, stream)) { foreach (var other in _voices) if (!ReferenceEquals(other, voice)) StopVoice(other); _parameters.Clear(); _stream = stream; }
            if (!voice.Playing) { _ages[_voices.IndexOf(voice)] = checked(++_sequence); voice.Play(0); }
            PauseVoice(voice, StreamPaused || !CanProcess());
        }
        finally { _audioOperation = false; }
    }
    internal void ClearAnimationSource(AudioStream stream) { EnsureMutable(); if (ReferenceEquals(_stream, stream)) { _stream = null; _parameters.Clear(); } }
    internal void StopAnimationVoice(FAudioStreamVoice voice) { EnsureMutable(); if (HasAnimationVoice(voice)) StopVoice(voice); }
    internal void ReleaseAnimationVoice(FAudioStreamVoice voice)
    {
        EnsureMutable(); var index = _voices.IndexOf(voice); if (index < 0) return;
        _voices.RemoveAt(index); _ages.RemoveAt(index); voice.Dispose(); voice.Playback.Dispose();
    }
}
