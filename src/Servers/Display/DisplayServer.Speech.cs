namespace Electron2D;

public sealed partial class DisplayServer
{
    /// <summary>Identifies a native speech utterance event.</summary>
    public enum TTSUtteranceEvent
    {
        /// <summary>Native speech output has started.</summary>
        Started = 0,
        /// <summary>Native speech output has finished.</summary>
        Ended = 1,
        /// <summary>The request was empty, stopped, interrupted or failed.</summary>
        Canceled = 2,
        /// <summary>A provider text boundary was reached.</summary>
        Boundary = 3,
    }
    /// <summary>Gets an independent snapshot of installed native speech voices.</summary>
    /// <returns>Voice IDs, names and language codes; empty when speech is disabled or unavailable.</returns>
    /// <remarks>Requires the opening thread. AudioGeneralTextToSpeech is sampled when this display opens.
    /// Windows uses system speech; Linux/FreeBSD require an available Speech Dispatcher service.
    /// Its voice catalog loads once per producer; reopen the display to refresh host catalog changes.</remarks>
    /// <exception cref="InvalidOperationException">The display is absent, retiring or accessed off its opening thread.</exception>
    /// <exception cref="IOException">A native speech operation failed.</exception>
    public static TTSVoice[] TTSGetVoices() => RequireService().GetTTSVoices();
    /// <summary>Gets voice identifiers whose native language starts with the specified prefix.</summary>
    /// <param name="language">An ordinal, case-sensitive prefix; empty matches every voice.</param>
    /// <returns>An independent ID array in native enumeration order.</returns>
    /// <exception cref="ArgumentNullException">The prefix is null.</exception>
    /// <exception cref="InvalidOperationException">The display is absent, retiring or accessed off its opening thread.</exception>
    /// <exception cref="IOException">A native speech operation failed.</exception>
    public static string[] TTSGetVoicesForLanguage(string language)
    {
        ArgumentNullException.ThrowIfNull(language); return RequireService().GetTTSVoices().Where(voice => voice.Language.StartsWith(language, StringComparison.Ordinal)).Select(voice => voice.ID).ToArray();
    }
    /// <summary>Reports active synthesis/playback or pending speech requests.</summary>
    /// <returns>False when speech is disabled, unavailable or idle.</returns>
    /// <exception cref="InvalidOperationException">The display is absent, retiring or accessed off its opening thread.</exception>
    /// <exception cref="IOException">A native speech operation failed.</exception>
    public static bool TTSIsSpeaking() { var owner = RequireService(); var native = owner.GetTTSBackend(); return native is not null && (owner._speechQueue.Count != 0 || owner._speechCurrent is not null && native.IsSpeaking); }
    /// <summary>Reports whether the speech producer has been paused.</summary>
    /// <returns>False when speech is disabled, unavailable or not paused.</returns>
    /// <exception cref="InvalidOperationException">The display is absent, retiring or accessed off its opening thread.</exception>
    public static bool TTSIsPaused() { var owner = RequireService(); owner.EnsureTTSOwner(); return owner._speech?.IsAvailable == true && owner._speechPaused; }
    /// <summary>Pauses speech until resumed, stopped or given a nonempty new request.</summary>
    /// <remarks>Idempotent, including while idle. Native output pauses at a provider boundary after buffered audio; accepted notices may still arrive. Requires an enabled producer.</remarks>
    /// <exception cref="InvalidOperationException">The display is absent, retiring or accessed off its opening thread.</exception>
    /// <exception cref="IOException">A native speech operation failed.</exception>
    /// <exception cref="NotSupportedException">Speech is disabled or the producer is unavailable.</exception>
    public static void TTSPause() { var owner = RequireService(); owner.EnsureTTSOwner(); var native = owner.RequireTTS(); if (!owner._speechPaused) { native.Pause(); owner._speechPaused = true; } }
    /// <summary>Resumes a paused speech producer.</summary>
    /// <remarks>Idempotent. Queued requests start on the next owner event pump.
    /// A native resume may repeat a preceding source boundary.</remarks>
    /// <exception cref="InvalidOperationException">The display is absent, retiring or accessed off its opening thread.</exception>
    /// <exception cref="IOException">A native speech operation failed.</exception>
    /// <exception cref="NotSupportedException">Speech is disabled or the producer is unavailable.</exception>
    public static void TTSResume() { var owner = RequireService(); owner.EnsureTTSOwner(); var native = owner.RequireTTS(); if (owner._speechPaused) { native.Resume(); owner._speechPaused = false; } }
    /// <summary>Stops native speech and cancels every active or queued request.</summary>
    /// <remarks>Canceled callbacks run through ProcessEvents; this operation resets pause after native purge.</remarks>
    /// <exception cref="InvalidOperationException">The display is absent, retiring or accessed off its opening thread.</exception>
    /// <exception cref="IOException">A native speech operation failed.</exception>
    /// <exception cref="NotSupportedException">Speech is disabled or the producer is unavailable.</exception>
    public static void TTSStop() => RequireService().StopTTS();
    /// <summary>Replaces a Started, Ended or Canceled callback for subsequently accepted notices.</summary>
    /// <param name="eventType">One of the three events with an utterance-ID argument.</param>
    /// <param name="callback">Borrowed callback receiving the signed 64-bit application ID, or null to clear.</param>
    /// <exception cref="ArgumentOutOfRangeException">The event is undefined or requires the boundary overload.</exception>
    /// <exception cref="InvalidOperationException">The display is absent, retiring or accessed off its opening thread.</exception>
    public static void TTSSetUtteranceCallback(TTSUtteranceEvent eventType, Action<long>? callback)
    {
        var owner = RequireService(); owner.EnsureTTSOwner();
        switch (eventType) { case TTSUtteranceEvent.Started: owner._speechStarted = callback; break; case TTSUtteranceEvent.Ended: owner._speechEnded = callback; break; case TTSUtteranceEvent.Canceled: owner._speechCanceled = callback; break; default: throw new ArgumentOutOfRangeException(nameof(eventType)); }
    }
    /// <summary>Replaces the Boundary callback for subsequently accepted notices.</summary>
    /// <param name="eventType">Boundary; other events require the utterance-ID overload.</param>
    /// <param name="callback">Borrowed original-source Unicode-scalar position and application-ID callback, or null.</param>
    /// <exception cref="ArgumentOutOfRangeException">The event is not Boundary.</exception>
    /// <exception cref="InvalidOperationException">The display is absent, retiring or accessed off its opening thread.</exception>
    public static void TTSSetUtteranceCallback(TTSUtteranceEvent eventType, Action<int, long>? callback)
    {
        var owner = RequireService(); owner.EnsureTTSOwner(); if (eventType != TTSUtteranceEvent.Boundary) throw new ArgumentOutOfRangeException(nameof(eventType)); owner._speechBoundary = callback;
    }
    /// <summary>Queues native speech, optionally interrupting all previous requests.</summary>
    /// <param name="text">Nonnull text/host-supported speech markup without NUL. Empty text emits Canceled.</param>
    /// <param name="voice">An installed voice ID; unused for empty text.</param>
    /// <param name="volume">Volume clamped to 0..100.</param><param name="pitch">Finite pitch clamped to 0..2.</param>
    /// <param name="rate">Finite relative rate clamped to 0.1..10 and mapped to the provider's native capacity.</param>
    /// <param name="utteranceID">Opaque application ID, including zero, negatives and duplicates.</param>
    /// <param name="interrupt">Cancels active/pending speech before this request.</param>
    /// <remarks>Nonempty requests resume pause. Output and word granularity depend on the native voice.
    /// Windows reports native word starts; Speech Dispatcher uses source word-end marks and sentence separators.
    /// Callbacks run on the owner event pump; native start failures cancel that request and are reported by the pump.</remarks>
    /// <exception cref="ArgumentException">Text/ID contains NUL, a nonempty request has no installed voice, or a floating value is nonfinite.</exception>
    /// <exception cref="ArgumentNullException">Text or voice is null.</exception>
    /// <exception cref="NotSupportedException">Speech is disabled or the native producer is unavailable.</exception>
    /// <exception cref="InvalidOperationException">The display is absent, retiring or accessed off its opening thread.</exception>
    /// <exception cref="IOException">A native speech operation failed.</exception>
    public static void TTSSpeak(string text, string voice, int volume = 50, float pitch = 1, float rate = 1, long utteranceID = 0, bool interrupt = false) => RequireService().SpeakTTS(text, voice, volume, pitch, rate, utteranceID, interrupt);

    private readonly bool _speechEnabled = ProjectSettings.Get(ProjectSettings.AudioGeneralTextToSpeech);
    private INativeSpeech? _speech;
    private bool _speechUnavailable, _speechPaused;
    private readonly Queue<SpeechRequest> _speechQueue = [];
    private readonly Queue<SpeechNotice> _speechNotices = [];
    private SpeechRequest? _speechCurrent;
    private Action<long>? _speechStarted, _speechEnded, _speechCanceled;
    private Action<int, long>? _speechBoundary;
    private readonly record struct SpeechNotice(TTSUtteranceEvent Kind, long ID, int Position, Delegate Callback);
    private sealed class SpeechRequest(string voice, NativeSpeechText text, int volume, float pitch, float rate, long id)
    {
        internal readonly string Voice = voice;
        internal readonly NativeSpeechText Text = text;
        internal readonly int Volume = volume;
        internal readonly float Pitch = pitch, Rate = rate;
        internal readonly long ID = id;
        internal uint Stream;
    }
    private void EnsureTTSOwner() { EnsureOwner(); if (_retiringNativeWindow) throw new InvalidOperationException("The speech owner is retiring."); }
    private INativeSpeech? GetTTSBackend()
    {
        EnsureTTSOwner(); if (!_speechEnabled || !(OperatingSystem.IsWindows() || OperatingSystem.IsLinux() && !OperatingSystem.IsAndroid() || OperatingSystem.IsFreeBSD()) || _speechUnavailable) return null;
        if (_speech is not null) return _speech.IsAvailable ? _speech : null;
        try { return _speech = OperatingSystem.IsWindows() ? new WindowsSpeech() : new SpeechDispatcherSpeech(); }
        catch (NotSupportedException) { _speechUnavailable = true; return null; }
    }
    private INativeSpeech RequireTTS() => GetTTSBackend() ?? throw new NotSupportedException("Enable native text-to-speech for this display on a host with a speech producer.");
    private TTSVoice[] GetTTSVoices() => GetTTSBackend()?.GetVoices() ?? [];
    private void SpeakTTS(string text, string voice, int volume, float pitch, float rate, long id, bool interrupt)
    {
        EnsureTTSOwner(); ArgumentNullException.ThrowIfNull(text); ArgumentNullException.ThrowIfNull(voice);
        if (text.Contains('\0') || voice.Contains('\0')) throw new ArgumentException("Speech text and voice IDs cannot contain NUL.");
        if (!float.IsFinite(pitch) || !float.IsFinite(rate)) throw new ArgumentException("Speech pitch and rate must be finite.");
        var native = RequireTTS(); SpeechRequest? request = null;
        if (text.Length != 0)
        {
            var selected = native.GetVoices().FirstOrDefault(value => value.ID == voice);
            if (selected.ID.Length == 0) throw new ArgumentException("The voice is not installed.", nameof(voice));
            pitch = Math.Clamp(pitch, 0, 2);
            request = new(voice, native.Prepare(text, pitch, selected.Language), Math.Clamp(volume, 0, 100), pitch, Math.Clamp(rate, .1f, 10), id);
            EnsureTTSOwner();
            if (!ReferenceEquals(native, _speech) || !native.IsAvailable) throw new IOException("The speech producer retired during text preparation.");
            _speechQueue.EnsureCapacity(_speechQueue.Count + 1);
        }
        _speechNotices.EnsureCapacity(_speechNotices.Count + _speechQueue.Count + (_speechCurrent is null ? 1 : 2));
        if (interrupt) StopTTS();
        if (request is null) { NoticeTTS(TTSUtteranceEvent.Canceled, id); return; }
        if (_speechPaused) { native.Resume(); _speechPaused = false; }
        _speechQueue.Enqueue(request);
    }
    private void StopTTS()
    {
        EnsureTTSOwner(); var native = RequireTTS();
        _speechNotices.EnsureCapacity(_speechNotices.Count + _speechQueue.Count + (_speechCurrent is null ? 0 : 1));
        native.Purge();
        CancelTTSRequests();
        if (_speechPaused) { native.Resume(); _speechPaused = false; }
    }
    private void CancelTTSRequests()
    {
        _speechNotices.EnsureCapacity(_speechNotices.Count + _speechQueue.Count + (_speechCurrent is null ? 0 : 1));
        if (_speechCurrent is { } current) { _speechCurrent = null; NoticeTTS(TTSUtteranceEvent.Canceled, current.ID); }
        while (_speechQueue.TryDequeue(out var pending)) NoticeTTS(TTSUtteranceEvent.Canceled, pending.ID);
    }
    private void NoticeTTS(TTSUtteranceEvent kind, long id, int position = 0)
    {
        Delegate? callback = kind switch { TTSUtteranceEvent.Started => _speechStarted, TTSUtteranceEvent.Ended => _speechEnded, TTSUtteranceEvent.Canceled => _speechCanceled, _ => _speechBoundary };
        if (callback is not null) _speechNotices.Enqueue(new(kind, id, position, callback));
    }
    private void PumpTTS(ref List<Exception>? failures)
    {
        var native = _speech; if (native is null) return;
        _speechNotices.EnsureCapacity(_speechNotices.Count + 256);
        try
        {
            for (var i = 0; i < 256 && native.TryGetEvent(out var value); i++)
            {
                try
                {
                    if (_speechCurrent is not { } current || value.Stream != current.Stream) continue;
                    if (value.Kind == TTSUtteranceEvent.Started) NoticeTTS(TTSUtteranceEvent.Started, current.ID);
                    else if (value.Kind == TTSUtteranceEvent.Boundary)
                    {
                        NoticeTTS(TTSUtteranceEvent.Boundary, current.ID, current.Text.SourcePosition(value.Position));
                    }
                    else if (value.Kind == TTSUtteranceEvent.Canceled) { _speechCurrent = null; NoticeTTS(TTSUtteranceEvent.Canceled, current.ID); }
                    else if (value.Kind == TTSUtteranceEvent.Ended)
                    {
                        _speechCurrent = null; int result;
                        try { result = native.LastResult; } catch { NoticeTTS(TTSUtteranceEvent.Canceled, current.ID); throw; }
                        if (result < 0) { NoticeTTS(TTSUtteranceEvent.Canceled, current.ID); throw new IOException($"Native speech failed (HRESULT 0x{result:X8}).", result); }
                        NoticeTTS(TTSUtteranceEvent.Ended, current.ID);
                    }
                }
                catch (Exception error) { (failures ??= []).Add(error); }
            }
            var count = _speechQueue.Count;
            for (var i = 0; i < count && native.IsAvailable && !_speechPaused && _speechCurrent is null && !native.IsSpeaking; i++)
            {
                var request = _speechQueue.Dequeue();
                try { request.Stream = native.Speak(request.Voice, request.Text, request.Volume, request.Pitch, request.Rate); _speechCurrent = request; }
                catch (Exception error) { NoticeTTS(TTSUtteranceEvent.Canceled, request.ID); (failures ??= []).Add(error); }
            }
        }
        catch (Exception error) { (failures ??= []).Add(error); }
        finally { if (!native.IsAvailable) { CancelTTSRequests(); _speechPaused = false; } }
    }
    private void DrainTTS(ref List<Exception>? failures)
    {
        var count = _speechNotices.Count;
        for (var i = 0; i < count && _speechNotices.TryDequeue(out var notice); i++)
        {
            try { if (notice.Kind == TTSUtteranceEvent.Boundary) ((Action<int, long>)notice.Callback)(notice.Position, notice.ID); else ((Action<long>)notice.Callback)(notice.ID); }
            catch (Exception error) { (failures ??= []).Add(error); }
        }
    }
    private void RetireTTS()
    {
        var native = _speech; _speech = null;
        try { native?.Purge(); }
        finally
        {
            try { native?.Dispose(); }
            finally { _speechCurrent = null; _speechQueue.Clear(); _speechNotices.Clear(); _speechStarted = _speechEnded = _speechCanceled = null; _speechBoundary = null; _speechPaused = false; }
        }
    }
}
