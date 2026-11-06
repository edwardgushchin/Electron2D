using Electron2D;
using System.Diagnostics;
using IOPath = System.IO.Path;

internal static class TTSChecks
{
    private readonly record struct Notice(DisplayServer.TTSUtteranceEvent Kind, long ID, int Position = -1);
    private static readonly List<Notice> Events = [];
    private static int _checks, _capture, _owner;
    private static string _tools = "", _output = "", _voice = "";
    private const string Phrase = "We see the green leaves in the summer breeze.";
    internal static void Run(string tools, string output)
    {
        _tools = tools; _output = output; _owner = Environment.CurrentManagedThreadId; Directory.CreateDirectory(output);
        var setting = ProjectSettings.Get(ProjectSettings.AudioGeneralTextToSpeech);
        try
        {
            Disabled(); ProjectSettings.Set(ProjectSettings.AudioGeneralTextToSpeech, true);
            using (var display = DisplayServer.Open("Native speech qualification", new(64, 64), hidden: true))
            {
                Check(DisplayServer.HasFeature(DisplayServer.Feature.TextToSpeech), "Actual installed speech producer.");
                var voices = DisplayServer.TTSGetVoices(); Check(voices.Length != 0, "Installed voice list.");
                using (var driver = Start("voices.exe", [IOPath.Combine(output, "voices.txt")])) Finish(driver);
                var native = File.ReadAllLines(IOPath.Combine(output, "voices.txt"));
                Check(voices.Select(value => $"{value.ID}|{value.Name}|{value.Language}").SequenceEqual(native), "Independent SDK token metadata agrees with the public typed snapshot.");
                _voice = voices.FirstOrDefault(value => value.Language.StartsWith("en", StringComparison.Ordinal)).ID;
                if (_voice.Length == 0) _voice = voices[0].ID;
                Check(default(TTSVoice).ID.Length == 0 && default(TTSVoice).Name.Length == 0 && default(TTSVoice).Language.Length == 0, "Default typed descriptor is empty.");
                Check(DisplayServer.TTSGetVoicesForLanguage("").SequenceEqual(voices.Select(value => value.ID)), "Empty language prefix selects all provider IDs in order.");
                var language = voices[0].Language;
                Check(DisplayServer.TTSGetVoicesForLanguage(language).SequenceEqual(voices.Where(value => value.Language.StartsWith(language, StringComparison.Ordinal)).Select(value => value.ID)), "Native language/variant prefix filtering.");
                Check(DisplayServer.TTSGetVoicesForLanguage("__missing_language__").Length == 0, "Unmatched prefix is empty.");
                var id = voices[0].ID; voices[0] = default; Check(DisplayServer.TTSGetVoices()[0].ID == id, "Caller array edits cannot mutate provider metadata.");
                Check(Task.Run(() => { try { DisplayServer.TTSGetVoices(); return false; } catch (InvalidOperationException) { return true; } }).GetAwaiter().GetResult(), "Owner-thread speech access.");
                ProjectSettings.Set(ProjectSettings.AudioGeneralTextToSpeech, false); Check(DisplayServer.HasFeature(DisplayServer.Feature.TextToSpeech), "Setting changes apply to a new display lifetime.");
                Callbacks(); Validation(); QueueAndControl(display); AudioAndBoundaries(); Retire(display);
            }
            using (var display = DisplayServer.Open("Disabled next speech lifetime", new(64, 64), hidden: true)) Check(!DisplayServer.HasFeature(DisplayServer.Feature.TextToSpeech) && DisplayServer.TTSGetVoices().Length == 0, "Next lifetime applies the disabled setting.");
            ProjectSettings.Set(ProjectSettings.AudioGeneralTextToSpeech, true);
            using (var display = DisplayServer.Open("Fresh speech callbacks", new(64, 64), hidden: true))
            {
                var before = Events.Count; DisplayServer.TTSSpeak("", "", utteranceID: 987); DisplayServer.ProcessEvents(); Check(Events.Count == before, "Callbacks never transfer to a new display lifetime.");
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.AudioGeneralTextToSpeech, setting); }
        Console.WriteLine($"TTS: {_checks} public checks; {_capture} independent process-audio captures.");
    }
    internal static void Disabled()
    {
        var setting = ProjectSettings.Get(ProjectSettings.AudioGeneralTextToSpeech);
        try
        {
            ProjectSettings.Set(ProjectSettings.AudioGeneralTextToSpeech, false);
            using var display = DisplayServer.Open("Disabled speech qualification", new(64, 64), hidden: true);
            Check(!DisplayServer.HasFeature(DisplayServer.Feature.TextToSpeech) && DisplayServer.TTSGetVoices().Length == 0 && DisplayServer.TTSGetVoicesForLanguage("").Length == 0, "Disabled speech has no native activation or voices.");
            Check(!DisplayServer.TTSIsSpeaking() && !DisplayServer.TTSIsPaused(), "Disabled state queries.");
            Reject<NotSupportedException>(() => DisplayServer.TTSSpeak("hello", "voice")); Reject<NotSupportedException>(DisplayServer.TTSPause); Reject<NotSupportedException>(DisplayServer.TTSResume); Reject<NotSupportedException>(DisplayServer.TTSStop);
            ProjectSettings.Set(ProjectSettings.AudioGeneralTextToSpeech, true); Check(!DisplayServer.HasFeature(DisplayServer.Feature.TextToSpeech), "An existing disabled display does not activate speech after a setting change.");
        }
        finally { ProjectSettings.Set(ProjectSettings.AudioGeneralTextToSpeech, setting); }
    }
    private static void Callbacks()
    {
        DisplayServer.TTSSetUtteranceCallback(DisplayServer.TTSUtteranceEvent.Started, (long id) => Log(DisplayServer.TTSUtteranceEvent.Started, id));
        DisplayServer.TTSSetUtteranceCallback(DisplayServer.TTSUtteranceEvent.Ended, (long id) => Log(DisplayServer.TTSUtteranceEvent.Ended, id));
        DisplayServer.TTSSetUtteranceCallback(DisplayServer.TTSUtteranceEvent.Canceled, (long id) => Log(DisplayServer.TTSUtteranceEvent.Canceled, id));
        DisplayServer.TTSSetUtteranceCallback(DisplayServer.TTSUtteranceEvent.Boundary, (int at, long id) => Log(DisplayServer.TTSUtteranceEvent.Boundary, id, at));
    }
    private static void Log(DisplayServer.TTSUtteranceEvent kind, long id, int position = -1)
    { if (Environment.CurrentManagedThreadId != _owner) throw new Exception("Speech callback escaped its owner."); Events.Add(new(kind, id, position)); }
    private static bool Seen(DisplayServer.TTSUtteranceEvent kind, long id) => Events.Any(value => value.Kind == kind && value.ID == id);
    private static void Validation()
    {
        Reject<ArgumentNullException>(() => DisplayServer.TTSGetVoicesForLanguage(null!));
        Reject<ArgumentNullException>(() => DisplayServer.TTSSpeak(null!, _voice)); Reject<ArgumentNullException>(() => DisplayServer.TTSSpeak("hello", null!));
        Reject<ArgumentException>(() => DisplayServer.TTSSpeak("bad\0text", _voice)); Reject<ArgumentException>(() => DisplayServer.TTSSpeak("hello", "bad\0voice"));
        Reject<ArgumentException>(() => DisplayServer.TTSSpeak("hello", _voice, pitch: float.NaN)); Reject<ArgumentException>(() => DisplayServer.TTSSpeak("hello", _voice, rate: float.PositiveInfinity));
        Reject<ArgumentException>(() => DisplayServer.TTSSpeak("hello", "__not_installed__"));
        Reject<ArgumentOutOfRangeException>(() => DisplayServer.TTSSetUtteranceCallback((DisplayServer.TTSUtteranceEvent)99, (long _) => { }));
        Reject<ArgumentOutOfRangeException>(() => DisplayServer.TTSSetUtteranceCallback(DisplayServer.TTSUtteranceEvent.Boundary, (long _) => { }));
        Reject<ArgumentOutOfRangeException>(() => DisplayServer.TTSSetUtteranceCallback(DisplayServer.TTSUtteranceEvent.Started, (int _, long _) => { }));
        DisplayServer.TTSSpeak("", "", utteranceID: long.MinValue); Check(!Seen(DisplayServer.TTSUtteranceEvent.Canceled, long.MinValue), "Empty speech is deferred to the owner pump.");
        DisplayServer.ForceProcessAndDropEvents(); Check(Seen(DisplayServer.TTSUtteranceEvent.Canceled, long.MinValue), "Speech cancellation is not dropped as activation input.");
        var previous = 0; var replacement = 0;
        DisplayServer.TTSSetUtteranceCallback(DisplayServer.TTSUtteranceEvent.Canceled, (long _) => previous++);
        DisplayServer.TTSSpeak("", ""); DisplayServer.TTSSetUtteranceCallback(DisplayServer.TTSUtteranceEvent.Canceled, (long _) => replacement++); DisplayServer.ProcessEvents();
        Check(previous == 1 && replacement == 0, "Accepted notices retain the previous delegate after replacement.");
        DisplayServer.TTSSetUtteranceCallback(DisplayServer.TTSUtteranceEvent.Canceled, (Action<long>?)null); DisplayServer.TTSSpeak("", ""); DisplayServer.ProcessEvents(); Check(replacement == 0, "Typed null clears its callback."); Callbacks();
    }
    private static void QueueAndControl(DisplayServer display)
    {
        Events.Clear(); DisplayServer.TTSStop(); DisplayServer.TTSPause(); DisplayServer.TTSPause(); Check(DisplayServer.TTSIsPaused(), "Idempotent idle pause.");
        DisplayServer.TTSSpeak("", "", utteranceID: 1); DisplayServer.ProcessEvents(); Check(DisplayServer.TTSIsPaused() && Seen(DisplayServer.TTSUtteranceEvent.Canceled, 1), "Empty speech cancels without resuming idle pause.");
        DisplayServer.TTSSpeak("First queued sentence.", _voice, volume: 0, rate: 3, utteranceID: 2); Check(!DisplayServer.TTSIsPaused() && DisplayServer.TTSIsSpeaking(), "A nonempty request resumes and is immediately observable as queued.");
        DisplayServer.TTSPause(); PumpFor(.15); Check(!Seen(DisplayServer.TTSUtteranceEvent.Started, 2), "A queued request does not start while paused.");
        DisplayServer.TTSResume(); DisplayServer.TTSResume(); Until(() => Seen(DisplayServer.TTSUtteranceEvent.Ended, 2)); Check(!DisplayServer.TTSIsSpeaking(), "Idempotent resume completes queued native output.");
        Events.Clear(); DisplayServer.TTSSpeak("First sentence.", _voice, volume: 0, rate: 3, utteranceID: long.MaxValue); DisplayServer.TTSSpeak("Second sentence.", _voice, volume: 0, rate: 3, utteranceID: long.MaxValue);
        Until(() => Events.Count(value => value.Kind == DisplayServer.TTSUtteranceEvent.Ended) == 2);
        Check(Events.Where(value => value.Kind != DisplayServer.TTSUtteranceEvent.Boundary).Select(value => value.Kind).SequenceEqual(new[] { DisplayServer.TTSUtteranceEvent.Started, DisplayServer.TTSUtteranceEvent.Ended, DisplayServer.TTSUtteranceEvent.Started, DisplayServer.TTSUtteranceEvent.Ended }), "FIFO requests preserve duplicate signed 64-bit IDs without collapsing events.");
        Events.Clear(); DisplayServer.TTSSpeak(string.Join(' ', Enumerable.Repeat(Phrase, 10)), _voice, volume: 0, utteranceID: 10); Until(() => Seen(DisplayServer.TTSUtteranceEvent.Started, 10));
        DisplayServer.TTSPause(); PumpFor(.4);
        Check(DisplayServer.TTSIsPaused() && !Seen(DisplayServer.TTSUtteranceEvent.Ended, 10), "Active pause is retained while previously queued native notices drain.");
        DisplayServer.TTSSpeak("pending", _voice, volume: 0, utteranceID: 11); Check(!DisplayServer.TTSIsPaused(), "Nonempty append resumes active native pause.");
        DisplayServer.TTSSpeak("pending", _voice, volume: 0, utteranceID: 12); DisplayServer.TTSPause(); DisplayServer.TTSStop(); DisplayServer.ProcessEvents();
        Check(!DisplayServer.TTSIsPaused() && !DisplayServer.TTSIsSpeaking() && Events.Where(value => value.Kind == DisplayServer.TTSUtteranceEvent.Canceled).Select(value => value.ID).SequenceEqual(new long[] { 10, 11, 12 }), "Stop purges active/pending speech, resets pause and cancels each request once.");
        Check(!Seen(DisplayServer.TTSUtteranceEvent.Ended, 10), "Canceled native streams do not become ended requests.");
        Events.Clear(); DisplayServer.TTSSpeak("keep queued", _voice, volume: 0, utteranceID: 20);
        Reject<ArgumentException>(() => DisplayServer.TTSSpeak("invalid", "__not_installed__", interrupt: true)); Check(DisplayServer.TTSIsSpeaking(), "Failed request validation preserves existing queued work.");
        DisplayServer.TTSSpeak("replacement", _voice, volume: 0, rate: 3, utteranceID: 21, interrupt: true); Until(() => Seen(DisplayServer.TTSUtteranceEvent.Ended, 21));
        Check(Seen(DisplayServer.TTSUtteranceEvent.Canceled, 20) && !Seen(DisplayServer.TTSUtteranceEvent.Started, 20), "Interrupt cancels pending work before replacement starts.");
        Events.Clear(); var failures = 0;
        DisplayServer.TTSSetUtteranceCallback(DisplayServer.TTSUtteranceEvent.Started, (long id) => { Log(DisplayServer.TTSUtteranceEvent.Started, id); Reject<InvalidOperationException>(DisplayServer.ProcessEvents); Reject<InvalidOperationException>(() => display.Dispose()); throw new InvalidOperationException("speech callback control"); });
        DisplayServer.TTSSpeak("Required callback continuation.", _voice, volume: 0, rate: 3, utteranceID: 30);
        Until(() => Seen(DisplayServer.TTSUtteranceEvent.Ended, 30), error => { Check(error.Flatten().InnerExceptions.Any(value => value.Message == "speech callback control"), "Callback failure crosses only the managed pump boundary."); failures++; });
        Check(failures == 1 && Seen(DisplayServer.TTSUtteranceEvent.Boundary, 30), "Later word/end notices continue after started-callback failure."); Callbacks();
        Events.Clear(); DisplayServer.TTSSetUtteranceCallback(DisplayServer.TTSUtteranceEvent.Started, (long id) => { Log(DisplayServer.TTSUtteranceEvent.Started, id); DisplayServer.TTSStop(); });
        DisplayServer.TTSSpeak(string.Join(' ', Enumerable.Repeat(Phrase, 10)), _voice, volume: 0, utteranceID: 31); Until(() => Seen(DisplayServer.TTSUtteranceEvent.Canceled, 31));
        Check(!DisplayServer.TTSIsSpeaking(), "A callback can stop its native request safely."); Callbacks();
    }
    private static void AudioAndBoundaries()
    {
        Events.Clear(); var baseline = Capture("Hello &amp; welcome 😀 world.", 100, 1, 1, 100);
        Check(baseline.Peak > 100 && baseline.ActiveFrames > 4410, "Actual public speech produces independently captured process PCM.");
        Check(Events.Where(value => value.Kind == DisplayServer.TTSUtteranceEvent.Boundary && value.ID == 100).Select(value => value.Position).SequenceEqual(new[] { 0, 6, 12, 22 }), "Native UTF16/entity positions map to original-source Unicode-scalar indices.");
        foreach (var sample in new[] { ("One &#x1F600; two &#38; three.", new[] { 0, 14, 18, 24 }), ("One &quot;two&quot;.", new[] { 0, 10 }), ("<emph>One</emph> two.", new[] { 6, 17 }), ("<!-- &amp; -->One two.", new[] { 14, 18 }), ("<![CDATA[One &amp; two]]> three.", new[] { 26 }), ("One\r\ntwo.", new[] { 0, 5 }) })
        {
            const long sampleID = 107; Events.Clear(); DisplayServer.TTSSpeak(sample.Item1, _voice, volume: 0, rate: 3, utteranceID: sampleID); Until(() => Seen(DisplayServer.TTSUtteranceEvent.Ended, sampleID));
            Check(Events.Where(value => value.Kind == DisplayServer.TTSUtteranceEvent.Boundary).Select(value => value.Position).SequenceEqual(sample.Item2), "Native source coordinates for entities/markup/line endings: " + sample.Item1);
        }
        var muted = Capture(Phrase, int.MinValue, 1, 3, 101); Check(muted.Peak <= 1 && muted.ActiveFrames == 0 && Seen(DisplayServer.TTSUtteranceEvent.Ended, 101), "Clamped zero volume stays within one PCM16 conversion LSB while lifecycle events complete.");
        double pausedAt = 0, resumedAt = 0;
        var paused = Capture(() =>
        {
            var clock = Stopwatch.StartNew(); DisplayServer.TTSSpeak(string.Join(' ', Enumerable.Repeat(Phrase, 10)), _voice, volume: 50, utteranceID: 106);
            Until(() => Seen(DisplayServer.TTSUtteranceEvent.Started, 106)); PumpFor(.7);
            DisplayServer.TTSPause(); pausedAt = clock.Elapsed.TotalSeconds; PumpFor(3);
            DisplayServer.TTSResume(); resumedAt = clock.Elapsed.TotalSeconds; PumpFor(.7); DisplayServer.TTSStop(); PumpFor(.2);
        });
        Console.WriteLine($"Pause at {pausedAt:F3}/{resumedAt:F3}; PCM seconds={paused.Samples.Length / 88200.0:F3}; RMS before={RMS(paused.Samples, pausedAt - .5, pausedAt - .1):F3}; paused={RMS(paused.Samples, pausedAt + 1.5, resumedAt - .3):F3}; resumed={RMS(paused.Samples, resumedAt + .2, resumedAt + .6):F3}.");
        Check(RMS(paused.Samples, pausedAt - .5, pausedAt - .1) > 10 && RMS(paused.Samples, pausedAt + 1.5, resumedAt - .3) <= 1 && RMS(paused.Samples, resumedAt + .2, resumedAt + .6) > 10, "Native pause and resume stop and restart actual process audio.");
        var normal = Capture(Phrase, 50, 1, 1, 102); var faster = Capture(Phrase, 50, 1, 10, 103);
        Check(faster.ActiveFrames < normal.ActiveFrames * .8 && faster.Peak > 20, "Mapped native speech rate changes real output duration without a silent out-of-range setter failure.");
        var low = Capture(Phrase, 50, -1, 1, 104); var high = Capture(Phrase, 50, 3, 1, 105);
        var lowPitch = Pitch(low.Samples); var highPitch = Pitch(high.Samples);
        Console.WriteLine($"Speech audio: baseline={baseline.Peak}; muted={muted.Peak}; normal/fast frames={normal.ActiveFrames}/{faster.ActiveFrames}; low/high pitch={lowPitch:F2}/{highPitch:F2} Hz.");
        Check(lowPitch > 0 && highPitch > lowPitch * 1.15, "Clamped pitch settings change the actual voiced output frequency.");
    }
    private sealed record Audio(short[] Samples, int Peak, int ActiveFrames);
    private static Audio Capture(string text, int volume, float pitch, float rate, long id)
        => Capture(() => { DisplayServer.TTSSpeak(text, _voice, volume, pitch, rate, id); Until(() => Seen(DisplayServer.TTSUtteranceEvent.Ended, id)); PumpFor(.12); });
    private static Audio Capture(Action speech)
    {
        var stem = IOPath.Combine(_output, "capture-" + ++_capture + "-" + Guid.NewGuid().ToString("N"));
        using var driver = Start("loopback.exe", [Environment.ProcessId.ToString(), stem]);
        try
        {
            var timer = Stopwatch.StartNew();
            while (!File.Exists(stem + ".ready") && !driver.HasExited && timer.Elapsed.TotalSeconds < 12) Thread.Sleep(10);
            Check(File.Exists(stem + ".ready"), "Independent own-process capture initialized.");
            speech();
        }
        finally { File.WriteAllText(stem + ".stop", ""); Finish(driver); }
        var bytes = File.ReadAllBytes(stem + ".pcm"); var samples = new short[bytes.Length / 2]; Buffer.BlockCopy(bytes, 0, samples, 0, bytes.Length);
        var peak = 0; var first = -1; var last = -1;
        for (var i = 0; i < samples.Length; i++) { var value = Math.Abs((int)samples[i]); peak = Math.Max(peak, value); if (value > 10) { if (first < 0) first = i / 2; last = i / 2; } }
        return new(samples, peak, first < 0 ? 0 : last - first + 1);
    }
    private static double RMS(short[] samples, double start, double end)
    {
        var first = Math.Clamp((int)(start * 88200), 0, samples.Length); var last = Math.Clamp((int)(end * 88200), first, samples.Length);
        if (last == first) return double.PositiveInfinity; double sum = 0;
        for (var i = first; i < last; i++) sum += (double)samples[i] * samples[i];
        return Math.Sqrt(sum / (last - first));
    }
    private static double Pitch(short[] stereo)
    {
        const int size = 1764; var values = new List<double>(); var signal = new double[size];
        for (var start = 0; start + size * 2 <= stereo.Length; start += 4410)
        {
            double energy = 0;
            for (var i = 0; i < size; i++) { signal[i] = (stereo[start + i * 2] + stereo[start + i * 2 + 1]) * .5; energy += signal[i] * signal[i]; }
            if (energy < size * 400) continue;
            var best = 0.0; var lagAt = 0;
            for (var lag = 88; lag <= 630; lag++)
            {
                double sum = 0, a = 0, b = 0;
                for (var i = 0; i < size - lag; i++) { var x = signal[i]; var y = signal[i + lag]; sum += x * y; a += x * x; b += y * y; }
                var score = a * b > 0 ? sum / Math.Sqrt(a * b) : 0;
                if (score > best) { best = score; lagAt = lag; }
            }
            if (best > .8) values.Add(44100.0 / lagAt);
        }
        values.Sort(); return values.Count == 0 ? 0 : values[values.Count / 2];
    }
    private static void Retire(DisplayServer display)
    {
        Events.Clear(); DisplayServer.TTSSpeak(string.Join(' ', Enumerable.Repeat(Phrase, 10)), _voice, volume: 0, utteranceID: 500); Until(() => Seen(DisplayServer.TTSUtteranceEvent.Started, 500));
        display.Dispose(); var count = Events.Count; Thread.Sleep(150); Check(!DisplayServer.IsAvailable && Events.Count == count, "Display retirement stops active synthesis without late callbacks.");
        Reject<InvalidOperationException>(() => DisplayServer.TTSGetVoices());
    }
    private static Process Start(string tool, string[] args)
    {
        var start = new ProcessStartInfo(IOPath.Combine(_tools, tool)) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true };
        foreach (var value in args) start.ArgumentList.Add(value);
        return Process.Start(start) ?? throw new Exception("Native speech inspector did not start.");
    }
    private static void Finish(Process driver)
    { if (!driver.WaitForExit(12000)) { driver.Kill(); throw new Exception("Native speech inspector timed out."); } Check(driver.ExitCode == 0, "Independent native speech inspector completed: " + driver.StandardError.ReadToEnd()); }
    private static void Until(Func<bool> ready, Action<AggregateException>? failure = null)
    {
        var timer = Stopwatch.StartNew();
        while (!ready()) { if (timer.Elapsed.TotalSeconds > 15) throw new Exception("Native speech progress timed out."); try { DisplayServer.ProcessEvents(); } catch (AggregateException error) { if (failure is null) throw; failure(error); } Thread.Sleep(5); }
    }
    private static void PumpFor(double seconds) { var timer = Stopwatch.StartNew(); while (timer.Elapsed.TotalSeconds < seconds) { DisplayServer.ProcessEvents(); Thread.Sleep(5); } }
    private static void Check(bool value, string reason) { _checks++; if (!value) throw new Exception(reason); }
    private static void Reject<T>(Action action) where T : Exception { _checks++; try { action(); } catch (T) { return; } throw new Exception("Expected " + typeof(T).Name); }
}
