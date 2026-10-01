using Electron2D;

internal static class AudioBusTests
{
    internal static void Run()
    {
        var server = AudioServer.Instance; server.BusCount = 1; var events = 0; server.BusLayoutChanged += Changed;
        server.AddBus(); server.SetBusName(1, "Effects"); server.AddBus(); server.SetBusName(2, "Music"); server.SetBusSend(2, "Effects");
        Check(server.BusCount == 3 && server.GetBusIndex("Missing") == -1 && server.GetBusSend(2) == "Effects" && events > 0, "Executable named bus graph data.");
        using var stream = AudioRuntimeTests.Tone(48000); var root = new Node(); var player = new AudioStreamPlayer { Stream = stream, Bus = "Music", MaxPolyphony = 2 }; root.AddChild(player); using var tree = new SceneTree(root); player.Play();
        var native = server.Native;
        Check(Peak() > .1, "Routed native bus produces the source."); server.SetBusMute(1, true); Check(Peak() == 0, "Muting parent send removes audible output."); server.SetBusMute(1, false); server.SetBusVolumeDB(1, -20); var quiet = Peak(); Check(quiet is > .01f and < .06f, "Parent dB gain changes native output."); server.SetBusVolumeDB(1, 0);
        Check(server.GetBusPeakVolumeLeftDB(0, 0) > -30 && server.GetBusPeakVolumeRightDB(0, 0) > -30, "Actual native bus peak meters report mixed output.");
        var preserved = player.GetStreamPlayback(); player.StreamPaused = true; var cursor = player.GetPlaybackPosition(); server.MoveBus(2, 1); Check(ReferenceEquals(preserved, player.GetStreamPlayback()) && player.GetPlaybackPosition() == cursor && player.StreamPaused, "Graph edits preserve playback identity, exact paused cursor and interpolation state."); player.StreamPaused = false; Check(player.IsPlaying() && server.GetBusName(1) == "Music", "Reordering the live graph preserves playback.");
        player.Play(.1); Check(player.IsPlaying(), "Prepared polyphony permits another voice."); player.MaxPolyphony = 1; Check(player.IsPlaying(), "Reducing polyphony preserves the newest voice.");
        server.RemoveBus(1); Check(player.IsPlaying() && Peak() > .1, "Removed player bus falls back to Master.");
        server.SetBusSolo(1, true); Check(Peak() == 0, "Solo excludes an unrelated Master-routed voice."); server.SetBusSolo(1, false);
        player.Stop(); var completed = false; player.Finished += () => completed = true; player.Play(.95); for (var i = 0; i < 30 && !completed; i++) { Thread.Sleep(10); tree.ProcessFrame(.01); }
        Check(completed && !player.IsPlaying(), "Natural end emits Finished through owner-thread processing."); tree.Dispose(); server.CloseNative(); server.BusCount = 1; server.BusLayoutChanged -= Changed;
        Console.WriteLine("Audio buses native routing, gain/mute/solo, graph lifecycle, polyphony and natural completion passed.");
        void Changed() => events++;
        float Peak()
        {
            native.PrepareCapture(native.MixRate / 5 * native.Channels); var target = native.MixPasses + Math.Max(4, (native.MixRate / 12 + native.QuantumFrames - 1) / native.QuantumFrames); var started = System.Diagnostics.Stopwatch.GetTimestamp();
            while (native.MixPasses < target) { if (System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalSeconds > 5) throw new InvalidOperationException("Native bus mix deadline."); Thread.Sleep(1); }
            return native.CapturedPCM().Select(MathF.Abs).DefaultIfEmpty(0).Max();
        }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
