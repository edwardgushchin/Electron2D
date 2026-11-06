namespace Electron2D;

internal readonly record struct NativeSpeechEvent(DisplayServer.TTSUtteranceEvent Kind, uint Stream, long Position = 0);

internal interface INativeSpeech : IDisposable
{
    bool IsAvailable { get; }
    bool IsSpeaking { get; }
    int LastResult { get; }
    TTSVoice[] GetVoices();
    NativeSpeechText Prepare(string text, float pitch, string language);
    uint Speak(string voice, NativeSpeechText text, int volume, float pitch, float rate);
    bool TryGetEvent(out NativeSpeechEvent value);
    void Pause();
    void Resume();
    void Purge();
}
