using Electron2D;
using System.Diagnostics;

internal static partial class TTSChecks
{
    private static void LinuxAudioAndBoundaries()
    {
        Events.Clear(); var baseline = Capture("Hello &amp; welcome 😀 world.", 100, 1, 1, 100);
        Check(baseline.Peak > 100 && baseline.ActiveFrames > 4410, "Actual system speech produces independently captured private-sink PCM.");
        Check(Events.Where(value => value.Kind == DisplayServer.TTSUtteranceEvent.Boundary && value.ID == 100).Select(value => value.Position).SequenceEqual(new[] { 5, 19, 21, 27 }), "Native word-end marks retain original entity/emoji scalar positions.");
        foreach (var sample in new[] { ("One &#x1F600; two &#38; three.", new[] { 3, 13, 17, 29 }), ("One &quot;two&quot;.", new[] { 3, 13 }), ("<emph>One</emph> two.", new[] { 9, 20 }), ("<!-- &amp; -->One two.", new[] { 17, 21 }), ("<![CDATA[One &amp; two]]> three.", new[] { 31 }), ("One\r\ntwo.", new[] { 3, 8 }) })
        {
            const long sampleID = 107; Events.Clear(); DisplayServer.TTSSpeak(sample.Item1, _voice, volume: 0, rate: 3, utteranceID: sampleID); Until(() => Seen(DisplayServer.TTSUtteranceEvent.Ended, sampleID));
            Check(Events.Where(value => value.Kind == DisplayServer.TTSUtteranceEvent.Boundary).Select(value => value.Position).SequenceEqual(sample.Item2), "Native source-coordinate marks for entities/markup/newlines: " + sample.Item1);
        }
        Events.Clear(); DisplayServer.TTSSpeak("One\n.\n..two <mark name=\"authored\"/> three.", _voice, volume: 0, rate: 3, utteranceID: 108); Until(() => Seen(DisplayServer.TTSUtteranceEvent.Ended, 108));
        Check(Events.Where(value => value.Kind == DisplayServer.TTSUtteranceEvent.Boundary).Select(value => value.Position).SequenceEqual(new[] { 3, 11, 41 }), "SSIP dot stuffing retains text and authored marks cannot forge engine boundaries.");
        var muted = Capture(Phrase, int.MinValue, 1, 3, 101); Check(muted.Peak <= 1 && muted.ActiveFrames == 0 && Seen(DisplayServer.TTSUtteranceEvent.Ended, 101), "Clamped zero volume silences actual native output.");
        double pausedAt = 0, resumedAt = 0;
        var paused = Capture(() =>
        {
            var clock = Stopwatch.StartNew(); DisplayServer.TTSSpeak(string.Join(' ', Enumerable.Repeat(Phrase, 10)), _voice, volume: 50, utteranceID: 106);
            Until(() => Seen(DisplayServer.TTSUtteranceEvent.Started, 106)); PumpFor(.7);
            DisplayServer.TTSPause(); pausedAt = clock.Elapsed.TotalSeconds; PumpFor(5);
            DisplayServer.TTSResume(); resumedAt = clock.Elapsed.TotalSeconds; PumpFor(3); DisplayServer.TTSStop(); PumpFor(.2);
        });
        Console.WriteLine($"Linux pause at {pausedAt:F3}/{resumedAt:F3}; RMS before={RMS(paused.Samples, pausedAt - .5, pausedAt - .1):F3}; paused={RMS(paused.Samples, pausedAt + 3, resumedAt - .3):F3}; resumed={RMS(paused.Samples, resumedAt + 2.2, resumedAt + 2.8):F3}.");
        Check(RMS(paused.Samples, pausedAt - .5, pausedAt - .1) > 10 && RMS(paused.Samples, pausedAt + 3, resumedAt - .3) <= 1 && RMS(paused.Samples, resumedAt + 2.2, resumedAt + 2.8) > 10, "Native pause/resume stop and restart actual private-sink output.");
        var normal = Capture(Phrase, 50, 1, 1, 102); var faster = Capture(Phrase, 50, 1, 10, 103);
        Check(faster.ActiveFrames < normal.ActiveFrames * .8 && faster.Peak > 20, "Native saturated rate changes real output duration.");
        var low = Capture(Phrase, 50, -1, 1, 104); var high = Capture(Phrase, 50, 3, 1, 105);
        var lowPitch = Pitch(low.Samples); var highPitch = Pitch(high.Samples);
        Console.WriteLine($"Linux speech audio: baseline={baseline.Peak}; muted={muted.Peak}; normal/fast frames={normal.ActiveFrames}/{faster.ActiveFrames}; low/high pitch={lowPitch:F2}/{highPitch:F2} Hz.");
        Check(lowPitch > 0 && highPitch > lowPitch * 1.15, "Clamped native pitch changes measured voiced frequency.");
    }
    private static void HotDispatch()
    {
        Events.Clear(); Events.EnsureCapacity(16384);
        DisplayServer.TTSSpeak(string.Join(' ', Enumerable.Repeat(Phrase, 100)), _voice, volume: 0, utteranceID: 900);
        Until(() => Seen(DisplayServer.TTSUtteranceEvent.Started, 900)); PumpFor(1);
        var beforeEvents = Events.Count; var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var index = 0; index < 256; index++) { DisplayServer.ProcessEvents(); Thread.Sleep(5); }
        var allBytes = GC.GetTotalAllocatedBytes(true) - all; var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - owner;
        Console.WriteLine($"Prepared active speech dispatch bytes: owner={ownerBytes}; all={allBytes}.");
        Check(Events.Count > beforeEvents && ownerBytes == 0 && allBytes == 0, "Prepared active native notice delivery allocates zero managed bytes and executes real boundaries.");
        DisplayServer.TTSStop(); PumpFor(.3);
        owner = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 256; index++) DisplayServer.ProcessEvents();
        Check(GC.GetAllocatedBytesForCurrentThread() == owner, "Prepared idle speech pumping allocates zero owner bytes.");
    }
}
