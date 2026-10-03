using System.Buffers.Binary;
using Electron2D;

internal static class AudioRuntimeTests
{
    internal static void Run()
    {
        VerifyPCM(); VerifyNative(); Console.WriteLine("Audio PCM and native streaming smoke checks passed.");
    }
    private static void VerifyPCM()
    {
        using var stream = Tone(44100); Check(stream.GetLength() == 1, "PCM duration matches sample metadata.");
        using var playback = stream.InstantiatePlayback(); playback.Start(); var frames = playback.MixAudio(1, 128); Check(frames.Length == 128 && frames.Any(v => MathF.Abs(v.X) > .1f), "Concrete PCM playback produces real samples.");
        using var copy = (AudioStreamWAV)stream.Duplicate(); Check(copy.Data.SequenceEqual(stream.Data), "Resource duplication copies encoded state.");
    }
    private static void VerifyNative()
    {
        using var stream = Tone(48000); var root = new Node(); var player = new AudioStreamPlayer { Stream = stream }; root.AddChild(player);
        using var tree = new SceneTree(root); player.Play();
        var server = AudioServer.Instance; var native = server.Native; native.PrepareCapture(48000);
        for (var i = 0; i < 40; i++) { Thread.Sleep(10); tree.ProcessFrame(.01); }
        var captured = native.CapturedPCM(); Check(captured.Any(value => MathF.Abs(value) > .1f), "Actual mixed native output contains the source waveform.");
        Check(native.MixPasses > 10 && player.GetPlaybackPosition() > .2, "Real native audio quanta advance the streamed voice.");
        var original = stream.Data; var setterMixBytes = native.MixManagedBytes; stream.Data = new byte[original.Length]; Thread.Sleep(30); native.PrepareCapture(4800); Thread.Sleep(30); Check(native.CapturedPCM().All(value => value == 0), "Live prepared resource replacement reaches native silence."); stream.Data = original; Thread.Sleep(30); Check(native.MixManagedBytes == setterMixBytes, "Live data changes decode on the setter thread without native mix managed allocations.");
        player.StreamPaused = true; Check(!player.IsPlaying() && player.HasStreamPlayback(), "Paused voice remains available without reporting active playing."); var paused = player.GetPlaybackPosition(); Thread.Sleep(40); Check(player.GetPlaybackPosition() == paused, "Native pause preserves the source cursor."); player.StreamPaused = false;
        var managed = native.MixManagedBytes; var allocations = FAudioContext.AllocationCalls;
        Thread.Sleep(100); Check(native.MixManagedBytes == managed, $"Warmed audio callbacks allocated {native.MixManagedBytes - managed} managed bytes.");
        Check(FAudioContext.AllocationCalls == allocations, $"Warmed native mix allocated {FAudioContext.AllocationCalls - allocations} times.");
        player.Stop(); Check(!player.HasStreamPlayback() && player.GetPlaybackPosition() == 0, "Stopped slots remain internal.");
        player.PitchScale = 2; player.Play(); Thread.Sleep(100); var fast = player.GetPlaybackPosition(); Check(fast is > .15 and < .4, "Playback time follows resampled pitch.");
        root.RemoveChild(player); var detached = player.GetPlaybackPosition(); Thread.Sleep(40); Check(player.GetPlaybackPosition() == detached, "Detached playback pauses and retains cursor."); root.AddChild(player); Thread.Sleep(40); Check(player.GetPlaybackPosition() > detached, "Reentry resumes the retained voice.");
        sourceLoop();
        player.Stop(); server.Lock(); server.Lock(); try { tree.Dispose(); server.CloseNative(); } finally { server.Unlock(); server.Unlock(); }
        var rejected = false; try { server.Dispose(); } catch (InvalidOperationException) { rejected = true; }
        Check(rejected && !server.IsDisposed, "Borrowed singleton disposal preserves lifetime.");
        void sourceLoop()
        {
            player.Stop(); stream.LoopBegin = 2400; stream.LoopEnd = 7200; stream.Loop = AudioLoopMode.Forward; player.Play(.05); Thread.Sleep(120); Check(player.GetPlaybackPosition() is >= .05 and <= .153, "Player position follows loop cursor.");
        }
    }
    internal static AudioStreamWAV Tone(int rate)
    {
        var data = new byte[rate * 2]; for (var i = 0; i < rate; i++) BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(i * 2), (short)(Math.Sin(2 * Math.PI * 440 * i / rate) * 12000));
        return new AudioStreamWAV { Data = data, SampleFormat = AudioStreamWAV.Format.PCM16, MixRate = rate };
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
