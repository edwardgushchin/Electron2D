using System.Buffers.Binary;
using System.Diagnostics;
using Electron2D;

internal static class BrowserNativeChecks
{
    internal static async Task RunAsync()
    {
        var owner = Environment.CurrentManagedThreadId;
        var pcm = new byte[48000 * 2];
        for (var frame = 0; frame < pcm.Length / 2; frame++)
            BinaryPrimitives.WriteInt16LittleEndian(pcm.AsSpan(frame * 2), (short)(6000 * Math.Sin(2 * Math.PI * 440 * frame / 48000)));
        using var stream = new AudioStreamWAV { Data = pcm, MixRate = 48000 };
        for (var cycle = 0; cycle < 2; cycle++)
        {
            var root = new Node();
            var player = new AudioStreamPlayer { Stream = stream };
            root.AddChild(player);
            using var tree = new SceneTree(root);
            using var capture = new AudioEffectCapture { BufferLength = 0.5f };
            Engine.Start(tree);
            try
            {
                var font = ThemeDB.FallbackFont ?? throw new InvalidOperationException("Missing default font.");
                var single = font.GetStringSize("ffi AV");
                var wrapped = font.GetMultilineStringSize("\u0e20\u0e32\u0e29\u0e32\u0e44\u0e17\u0e22\u0e20\u0e32\u0e29\u0e32\u0e44\u0e17\u0e22", width: 32);
                if (single.X <= 0 || single.Y <= 0 || wrapped.Y <= single.Y)
                    throw new InvalidOperationException("Browser WOFF2 shaping or dictionary wrapping failed.");
                AudioServer.AddBusEffect(0, capture);
                try
                {
                    player.Play();
                    var watch = Stopwatch.StartNew();
                    var nonzero = false;
                    while (watch.Elapsed < TimeSpan.FromSeconds(5) && !nonzero)
                    {
                        await Task.Delay(20);
                        if (Environment.CurrentManagedThreadId != owner)
                            throw new InvalidOperationException("Browser media checks left their execution thread.");
                        tree.ProcessFrame(0.02);
                        foreach (var sample in capture.GetBuffer(capture.GetFramesAvailable()))
                        {
                            if (!sample.IsFinite()) throw new InvalidOperationException("Browser PCM contains a nonfinite sample.");
                            nonzero |= Math.Abs(sample.X) > 0.001f || Math.Abs(sample.Y) > 0.001f;
                        }
                    }
                    var latency = AudioServer.GetOutputLatency();
                    if (!nonzero || player.GetPlaybackPosition() <= 0 || !double.IsFinite(latency) || latency <= 0)
                        throw new InvalidOperationException($"Browser FAudio PCM/output failed: nonzero={nonzero}, position={player.GetPlaybackPosition()}, latency={latency}.");
                    player.Stop();
                }
                finally { AudioServer.RemoveBusEffect(0, 0); }
            }
            finally { Engine.Stop(); }
        }
        Console.WriteLine("Browser native WOFF2/dictionary text, FAudio PCM/output and repeated lifecycle checks passed.");
    }

}
