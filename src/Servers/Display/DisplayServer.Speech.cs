using System.Globalization;
using System.Text;

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
    /// <remarks>Requires the opening thread. AudioGeneralTextToSpeech is sampled when this display opens.</remarks>
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
    public static bool TTSIsSpeaking() { var owner = RequireService(); var native = owner.GetTTSBackend(); return owner._speechQueue.Count != 0 || native is not null && owner._speechCurrent is not null && native.IsSpeaking; }
    /// <summary>Reports whether the speech producer has been paused.</summary>
    /// <returns>False when speech is disabled, unavailable or not paused.</returns>
    /// <exception cref="InvalidOperationException">The display is absent, retiring or accessed off its opening thread.</exception>
    public static bool TTSIsPaused() { var owner = RequireService(); owner.EnsureTTSOwner(); return owner._speechPaused; }
    /// <summary>Pauses speech until resumed, stopped or given a nonempty new request.</summary>
    /// <remarks>Idempotent, including while idle. Native output pauses at a provider boundary after buffered audio; accepted notices may still arrive. Requires an enabled producer.</remarks>
    /// <exception cref="InvalidOperationException">The display is absent, retiring or accessed off its opening thread.</exception>
    /// <exception cref="IOException">A native speech operation failed.</exception>
    /// <exception cref="NotSupportedException">Speech is disabled or the producer is unavailable.</exception>
    public static void TTSPause() { var owner = RequireService(); owner.EnsureTTSOwner(); var native = owner.RequireTTS(); if (!owner._speechPaused) { native.Pause(); owner._speechPaused = true; } }
    /// <summary>Resumes a paused speech producer.</summary>
    /// <remarks>Idempotent. Queued requests start on the next owner event pump.</remarks>
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
    /// Callbacks run on the owner event pump; native start failures cancel that request and are reported by the pump.</remarks>
    /// <exception cref="ArgumentException">Text/ID contains NUL, a nonempty request has no installed voice, or a floating value is nonfinite.</exception>
    /// <exception cref="ArgumentNullException">Text or voice is null.</exception>
    /// <exception cref="NotSupportedException">Speech is disabled or the native producer is unavailable.</exception>
    /// <exception cref="InvalidOperationException">The display is absent, retiring or accessed off its opening thread.</exception>
    /// <exception cref="IOException">A native speech operation failed.</exception>
    public static void TTSSpeak(string text, string voice, int volume = 50, float pitch = 1, float rate = 1, long utteranceID = 0, bool interrupt = false) => RequireService().SpeakTTS(text, voice, volume, pitch, rate, utteranceID, interrupt);

    private readonly bool _speechEnabled = ProjectSettings.Get(ProjectSettings.AudioGeneralTextToSpeech);
    private WindowsSpeech? _speech;
    private bool _speechUnavailable, _speechPaused;
    private readonly Queue<SpeechRequest> _speechQueue = [];
    private readonly Queue<SpeechNotice> _speechNotices = [];
    private SpeechRequest? _speechCurrent;
    private Action<long>? _speechStarted, _speechEnded, _speechCanceled;
    private Action<int, long>? _speechBoundary;
    private readonly record struct SpeechNotice(TTSUtteranceEvent Kind, long ID, int Position, Delegate Callback);
    private sealed class SpeechRequest(string voice, string xml, int prefix, int[] positions, int positionCount, int volume, float rate, long id)
    {
        internal readonly string Voice = voice, XML = xml;
        internal readonly int Prefix = prefix, PositionCount = positionCount, Volume = volume;
        internal readonly int[] Positions = positions;
        internal readonly float Rate = rate;
        internal readonly long ID = id;
        internal uint Stream;
    }
    private void EnsureTTSOwner() { EnsureOwner(); if (_retiringNativeWindow) throw new InvalidOperationException("The speech owner is retiring."); }
    private WindowsSpeech? GetTTSBackend()
    {
        EnsureTTSOwner(); if (!_speechEnabled || !OperatingSystem.IsWindows() || _speechUnavailable) return null;
        if (_speech is not null) return _speech;
        try { return _speech = new WindowsSpeech(); }
        catch (NotSupportedException) { _speechUnavailable = true; return null; }
    }
    private WindowsSpeech RequireTTS() => GetTTSBackend() ?? throw new NotSupportedException("Enable native text-to-speech for this display on a host with a speech producer.");
    private TTSVoice[] GetTTSVoices() => GetTTSBackend()?.GetVoices() ?? [];
    private void SpeakTTS(string text, string voice, int volume, float pitch, float rate, long id, bool interrupt)
    {
        EnsureTTSOwner(); ArgumentNullException.ThrowIfNull(text); ArgumentNullException.ThrowIfNull(voice);
        if (text.Contains('\0') || voice.Contains('\0')) throw new ArgumentException("Speech text and voice IDs cannot contain NUL.");
        if (!float.IsFinite(pitch) || !float.IsFinite(rate)) throw new ArgumentException("Speech pitch and rate must be finite.");
        var native = RequireTTS(); SpeechRequest? request = null;
        if (text.Length != 0)
        {
            if (!native.GetVoices().Any(value => value.ID == voice)) throw new ArgumentException("The voice is not installed.", nameof(voice));
            request = PrepareTTS(text, voice, Math.Clamp(volume, 0, 100), Math.Clamp(pitch, 0, 2), Math.Clamp(rate, .1f, 10), id);
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
        if (_speechCurrent is { } current) { _speechCurrent = null; NoticeTTS(TTSUtteranceEvent.Canceled, current.ID); }
        while (_speechQueue.TryDequeue(out var pending)) NoticeTTS(TTSUtteranceEvent.Canceled, pending.ID);
        if (_speechPaused) { native.Resume(); _speechPaused = false; }
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
                    if (value.ID == WindowsSpeech.StartedEvent) NoticeTTS(TTSUtteranceEvent.Started, current.ID);
                    else if (value.ID == WindowsSpeech.WordEvent)
                    {
                        var offset = (long)value.Position - current.Prefix;
                        if (offset < 0 || offset >= current.PositionCount) throw new IOException("The speech provider reported a boundary outside its submitted source.");
                        NoticeTTS(TTSUtteranceEvent.Boundary, current.ID, current.Positions[(int)offset]);
                    }
                    else if (value.ID == WindowsSpeech.EndedEvent)
                    {
                        _speechCurrent = null; int result;
                        try { result = native.LastResult; } catch { NoticeTTS(TTSUtteranceEvent.Canceled, current.ID); throw; }
                        if (result < 0) { NoticeTTS(TTSUtteranceEvent.Canceled, current.ID); throw new IOException($"Native speech failed (HRESULT 0x{result:X8}).", result); }
                        NoticeTTS(TTSUtteranceEvent.Ended, current.ID);
                    }
                }
                catch (Exception error) { (failures ??= []).Add(error); }
                finally { WindowsSpeech.ClearEvent(value); }
            }
            var count = _speechQueue.Count;
            for (var i = 0; i < count && !_speechPaused && _speechCurrent is null && !native.IsSpeaking; i++)
            {
                var request = _speechQueue.Dequeue();
                try { request.Stream = native.Speak(request.Voice, request.XML, request.Volume, request.Rate); _speechCurrent = request; }
                catch (Exception error) { NoticeTTS(TTSUtteranceEvent.Canceled, request.ID); (failures ??= []).Add(error); }
            }
        }
        catch (Exception error) { (failures ??= []).Add(error); }
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
    private static SpeechRequest PrepareTTS(string text, string voice, int volume, float pitch, float rate, long id)
    {
        var prefix = "<pitch absmiddle=\"" + ((int)(pitch * 10 - 10)).ToString(CultureInfo.InvariantCulture) + "\">";
        var positions = new int[checked(text.Length + 1)]; var count = 0; var scalar = 0; var literalUntil = 0; var copied = 0;
        StringBuilder? normalized = null;
        for (var i = 0; i < text.Length;)
        {
            if (i >= literalUntil)
            {
                var tail = text.AsSpan(i); var end = tail.StartsWith("<!--") ? "-->" : tail.StartsWith("<![CDATA[") ? "]]>" : tail.StartsWith("<?") ? "?>" : null;
                if (end is not null) { var at = tail.IndexOf(end); literalUntil = at < 0 ? text.Length : i + at + end.Length; }
            }
            if (i >= literalUntil && text[i] == '&' && EntityTTS(text.AsSpan(i), out var length, out var units, out var decoded))
            {
                if (text[i + 1] == '#')
                {
                    // SAPI handles numeric references inconsistently; canonical literals/predefined entities preserve their XML value.
                    normalized ??= new(text.Length); normalized.Append(text.AsSpan(copied, i - copied));
                    normalized.Append(decoded switch { 34 => "&quot;", 38 => "&amp;", 39 => "&apos;", 60 => "&lt;", 62 => "&gt;", _ => new Rune((int)decoded).ToString() });
                    copied = i + length;
                }
                for (var j = 0; j < units; j++) positions[count++] = scalar;
                scalar += length; i += length;
            }
            else
            {
                var width = char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]) ? 2 : 1;
                for (var j = 0; j < width; j++) positions[count++] = scalar;
                scalar++; i += width;
            }
        }
        positions[count++] = scalar;
        if (normalized is not null) normalized.Append(text.AsSpan(copied));
        return new(voice, prefix + (normalized?.ToString() ?? text) + "</pitch>", prefix.Length, positions, count, volume, rate, id);
    }
    private static bool EntityTTS(ReadOnlySpan<char> text, out int length, out int units, out uint decoded)
    {
        length = 0; units = 1; decoded = 0;
        if (text.StartsWith("&amp;")) length = 5;
        else if (text.StartsWith("&lt;") || text.StartsWith("&gt;")) length = 4;
        else if (text.StartsWith("&quot;") || text.StartsWith("&apos;")) length = 6;
        else if (text.Length > 3 && text[1] == '#')
        {
            var index = 2; var hex = text[index] is 'x' or 'X'; if (hex) index++;
            var first = index; uint value = 0; var valid = true;
            for (; index < text.Length; index++)
            {
                var c = text[index]; var digit = c is >= '0' and <= '9' ? c - '0' : hex && c is >= 'a' and <= 'f' ? c - 'a' + 10 : hex && c is >= 'A' and <= 'F' ? c - 'A' + 10 : -1;
                if (digit < 0) break;
                if (value <= 0x10ffff) value = value * (hex ? 16u : 10u) + (uint)digit; else valid = false;
            }
            if (valid && index > first && index < text.Length && text[index] == ';' && value != 0 && Rune.IsValid((int)value)) { length = index + 1; units = value > 0xffff ? 2 : 1; decoded = value; }
        }
        return length != 0;
    }
}
