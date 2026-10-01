using Electron2D;

internal static class AudioLifetimeTests
{
    internal static void VerifyWorkerInitialization()
    {
        Task.Run(() => { using var stream = AudioRuntimeTests.Tone(44100); using var playback = stream.InstantiatePlayback(); playback.Start(); Check(playback.MixAudio(1, 32).Length == 32, "Worker-first resource mixing executes."); }).GetAwaiter().GetResult();
        AudioServer.Instance.BusCount = 1;
        var rejected = Task.Run(() => { try { AudioServer.Instance.AddBus(); return false; } catch (InvalidOperationException) { return true; } }).GetAwaiter().GetResult();
        Check(rejected && AudioServer.Instance.BusCount == 1, "Passive worker mixing does not claim configuration ownership; later foreign mutation rejects.");
    }
    internal static void Run()
    {
        using var stream = new CustomStream(); var root = new Node(); var player = new AudioStreamPlayer { Stream = stream, MaxPolyphony = 2 }; root.AddChild(player); using var tree = new SceneTree(root); player.Play(); var first = player.GetStreamPlayback(); player.Play();
        Check(!first.IsPlaying() || ReferenceEquals(first, player.GetStreamPlayback()), "Monophonic custom streams stop prior voices.");
        player.Seek(.2); Check(player.GetPlaybackPosition() >= .2, "Generic concrete seek starts one cursor.");
        stream.FailMix = true; var failed = false;
        for (var i = 0; i < 20 && !failed; i++) { Thread.Sleep(10); try { tree.ProcessFrame(.01); } catch (Exception) { failed = true; } }
        Check(failed && !player.IsPlaying(), "Native callback failures stop playback and reach the owner frame.");
        stream.FailMix = false; player.Play(); var detached = new ThrowingExit { Stream = stream }; root.AddChild(detached); detached.Play();
        try { root.RemoveChild(detached); } catch (Exception) { }
        var position = detached.GetPlaybackPosition(); Thread.Sleep(30); Check(detached.GetPlaybackPosition() == position, "Exit callbacks failing still pause owned voices.");
        var mutationRejected = Task.Run(() => { try { detached.Stream = null; return false; } catch (InvalidOperationException) { return true; } }).GetAwaiter().GetResult();
        var disposalRejected = Task.Run(() => { try { detached.Dispose(); return false; } catch (InvalidOperationException) { return true; } }).GetAwaiter().GetResult();
        Check(mutationRejected && disposalRejected && !detached.IsDisposed, "Detached registration rejects foreign mutation/disposal before state changes.");
        detached.Dispose(); tree.Dispose(); Check(!stream.IsDisposed, "All player cleanup preserves borrowed streams.");
        Console.WriteLine("Audio custom playback, monophony, callback failure and owner cleanup passed.");
    }
    private sealed class ThrowingExit : AudioStreamPlayer
    {
        protected override void OnExitTree() => throw new InvalidOperationException("Injected exit failure.");
    }
    private sealed class CustomStream : AudioStream
    {
        internal volatile bool FailMix;
        protected override AudioStreamPlayback OnInstantiatePlayback() => new Playback(this);
        private sealed class Playback(CustomStream stream) : AudioStreamPlayback
        {
            private bool _active; private double _position;
            protected override void OnStart(double fromPosition) { _position = Math.Max(0, fromPosition); _active = true; }
            protected override void OnStop() => _active = false;
            protected override bool OnIsPlaying() => _active;
            protected override double OnGetPlaybackPosition() => _position;
            protected override void OnSeek(double time) => _position = Math.Max(0, time);
            protected override int OnMix(Span<Vector2> buffer, float rateScale)
            {
                if (stream.FailMix) throw new InvalidOperationException("Injected mix failure.");
                if (!_active) return 0;
                buffer.Fill(new Vector2(.1f, -.1f)); _position += buffer.Length * rateScale / AudioServer.Instance.GetMixRate(); return buffer.Length;
            }
        }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
