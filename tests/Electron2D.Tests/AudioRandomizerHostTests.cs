using Electron2D;

internal static class AudioRandomizerHostTests
{
    internal static void Run()
    {
        var backend = Environment.GetEnvironmentVariable("ELECTRON2D_AUDIO_RENDERER") == "compatibility" ? "compatibility" : "gpu";
        var settings = ProjectSettings.Service; var original = ProjectSettings.Get(ProjectSettings.RenderingMethod); ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        var fps = Engine.MaxFPS; Engine.MaxFPS = 60;
        try
        {
            for (var run = 0; run < 2; run++)
            {
                using var a = Tone(24000); using var b = Tone(48000); using var pool = new AudioStreamRandomizer { Mode = AudioStreamRandomizer.PlaybackMode.Sequential };
                pool.AddStream(-1, a); pool.AddStream(-1, b);
                var window = new Window { Title = "Electron2D random audio", Size = new Vector2i(240, 120) };
                var player = new AudioStreamPlayer { Stream = pool, Autoplay = true, VolumeDB = -24 }; var scenario = new Scenario(player, pool);
                window.AddChild(player); window.AddChild(scenario);
                if (Engine.Run(window) != 0 || !scenario.Completed || !window.IsDisposed || pool.IsDisposed || a.IsDisposed || b.IsDisposed)
                    throw new InvalidOperationException("Public randomizer host lifecycle failed.");
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { scenario = "audio-randomizer-host", backend, run, firstLength = .5, secondLength = 1, scenario.Position, cleaned = window.IsDisposed }));
            }
        }
        finally { Engine.MaxFPS = fps; ProjectSettings.Set(ProjectSettings.RenderingMethod, original); }
    }
    private static AudioStreamWAV Tone(int frames)
    {
        var bytes = new byte[frames * 2]; for (var i = 0; i < frames; i++) System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(i * 2), (short)(Math.Sin(i * .05) * 16000));
        return new AudioStreamWAV { Data = bytes, SampleFormat = AudioStreamWAV.Format.PCM16, MixRate = 48000 };
    }
    private sealed class Scenario(AudioStreamPlayer player, AudioStreamRandomizer pool) : Node
    {
        private double _elapsed; private bool _second;
        internal bool Completed; internal double Position;
        protected override void OnReady() { ProcessEnabled = true; if (pool.GetLength() != .5) throw new InvalidOperationException("Autoplay selected first child."); }
        protected override void OnProcess(double delta)
        {
            _elapsed += delta;
            if (!_second && _elapsed >= .12)
            {
                player.Stop(); player.Play(); _second = true;
                if (pool.GetLength() != 1) throw new InvalidOperationException("Repeated public Play did not advance the selection.");
            }
            if (_elapsed < .3) return;
            Position = player.GetPlaybackPosition(); Completed = _second && Position > .08 && player.IsPlaying(); Tree!.Quit();
        }
    }
}
