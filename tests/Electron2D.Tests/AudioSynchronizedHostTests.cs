using System.Buffers.Binary;
using Electron2D;

internal static class AudioSynchronizedHostTests
{
    internal static void Run()
    {
        var backend = Environment.GetEnvironmentVariable("ELECTRON2D_AUDIO_RENDERER") == "compatibility" ? "compatibility" : "gpu";
        var settings = ProjectSettings.Service; var old = ProjectSettings.Get(ProjectSettings.RenderingMethod); var fps = Engine.MaxFPS;
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); Engine.MaxFPS = 60;
        try
        {
            using var shortStream = Tone(24000); using var longStream = Tone(48000); using var synchronized = new AudioStreamSynchronized { StreamCount = 2 };
            synchronized.SetSyncStream(0, shortStream); synchronized.SetSyncStream(1, longStream);
            var window = new Window { Title = "Electron2D synchronized audio", Size = new(240, 120) }; var player = new AudioStreamPlayer { Stream = synchronized, Autoplay = true, VolumeDB = -24 }; var scenario = new Scenario(player, synchronized);
            window.AddChild(player); window.AddChild(scenario);
            if (Engine.Run(window) != 0 || !scenario.Completed || !window.IsDisposed || shortStream.IsDisposed || longStream.IsDisposed || synchronized.IsDisposed)
                throw new InvalidOperationException("Public synchronized WAV host lifecycle failed.");
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { scenario = "audio-synchronized-host", backend, shortest = .5, longest = 1, scenario.Frozen, scenario.LongestOnly, cleaned = window.IsDisposed }));
        }
        finally { Engine.MaxFPS = fps; ProjectSettings.Set(ProjectSettings.RenderingMethod, old); }
    }
    private static AudioStreamWAV Tone(int frames)
    {
        var data = new byte[frames * 2]; for (var i = 0; i < frames; i++) BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(i * 2), (short)(Math.Sin(i * .05) * 8000));
        return new AudioStreamWAV { Data = data, SampleFormat = AudioStreamWAV.Format.PCM16, MixRate = 48000 };
    }
    private sealed class Scenario : Node
    {
        private readonly AudioStreamPlayer _player; private readonly AudioStreamSynchronized _stream;
        private double _elapsed, _phaseTime; private int _phase; private bool _finished;
        internal bool Completed, LongestOnly; internal double Frozen;
        internal Scenario(AudioStreamPlayer player, AudioStreamSynchronized stream) { _player = player; _stream = stream; _player.Finished += () => _finished = true; }
        protected override void OnReady() { ProcessEnabled = true; if (_stream.GetLength() != 1 || !_player.IsPlaying() || _player.GetStreamPlayback() is not AudioStreamPlaybackSynchronized) throw new InvalidOperationException("Autoplay prepares the typed synchronized producer."); }
        protected override void OnProcess(double delta)
        {
            _elapsed += delta;
            if (_phase == 0 && _elapsed > .12) { _player.StreamPaused = true; Frozen = _player.GetPlaybackPosition(); _phaseTime = _elapsed; _phase = 1; }
            else if (_phase == 1 && _elapsed - _phaseTime > .12)
            {
                if (_player.GetPlaybackPosition() != Frozen) throw new InvalidOperationException("Paused synchronized cursors moved.");
                _stream.SetSyncStreamVolume(0, -9); _player.StreamPaused = false; _player.Seek(.3); _phase = 2;
            }
            else if (_phase == 2)
            {
                if (_player.GetPlaybackPosition() > .55 && _player.IsPlaying()) LongestOnly = true;
                if (_finished) { if (!LongestOnly || _player.IsPlaying()) throw new InvalidOperationException("Synchronized playback did not outlast its shortest child and finish once."); Completed = true; Tree!.Quit(); }
            }
            if (_elapsed > 5) throw new InvalidOperationException("Synchronized host did not finish.");
        }
    }
}
