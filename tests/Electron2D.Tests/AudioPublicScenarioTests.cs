using Electron2D;

// This consumer scenario uses only public runtime APIs; PCM instrumentation lives in AudioRuntimeTests.
internal static class AudioPublicScenarioTests
{
    internal static void Run()
    {
        var backend = Environment.GetEnvironmentVariable("ELECTRON2D_AUDIO_RENDERER") == "compatibility" ? "compatibility" : "gpu";
        var settings = ProjectSettings.Service; var method = ProjectSettings.Get(ProjectSettings.RenderingMethod); ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        var previous = Engine.MaxFPS; Engine.MaxFPS = 60;
        try
        {
            for (var run = 0; run < 2; run++)
            {
                using var stream = AudioRuntimeTests.Tone(48000);
                var window = new Window { Title = "Electron2D audio scenario", Size = new Vector2i(240, 120) };
                var player = new AudioStreamPlayer { Stream = stream, Autoplay = true, VolumeDB = -24 };
                var scenario = new Scenario(player); window.AddChild(player); window.AddChild(scenario);
                var result = Engine.Run(window);
                if (result != 0 || !window.IsDisposed || !player.IsDisposed || stream.IsDisposed || !scenario.Advanced)
                    throw new InvalidOperationException("Public audio host lifecycle failed.");
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { scenario = "audio-host", renderer = backend, run, driver = scenario.Driver, position = scenario.Position, cleaned = window.IsDisposed }));
            }
        }
        finally { Engine.MaxFPS = previous; ProjectSettings.Set(ProjectSettings.RenderingMethod, method); }
    }
    private sealed class Scenario(AudioStreamPlayer player) : Node
    {
        private double _elapsed;
        internal bool Advanced;
        internal string Driver = "";
        internal double Position;
        protected override void OnReady() { ProcessEnabled = true; Driver = AudioServer.GetDriverName(); }
        protected override void OnProcess(double delta)
        {
            _elapsed += delta;
            if (_elapsed < .25) return;
            Position = player.GetPlaybackPosition(); Advanced = Position > .1 && player.IsPlaying(); Tree!.Quit();
        }
    }
}
