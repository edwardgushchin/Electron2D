using System.Buffers.Binary;
using Electron2D;

internal static class AudioSpeakerTests
{
    internal static void Run()
    {
        var data = new byte[48000 * 4]; for (var i = 0; i < 48000; i++) { BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(i * 4), 8192); BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(i * 4 + 2), -4096); }
        using var stream = new AudioStreamWAV { Data = data, SampleFormat = AudioStreamWAV.Format.PCM16, Stereo = true, MixRate = 48000 };
        var root = new Node(); var player = new AudioStreamPlayer { Stream = stream, VolumeLinear = .5f }; root.AddChild(player); using var tree = new SceneTree(root); player.Play(); var server = AudioServer.Service; var native = server.Native; var channels = native.Channels;
        Check(channels is 2 or 4 or 6 or 8 && AudioServer.GetBusChannels(0) == channels / 2, "Real native stereo-pair layout.");
        foreach (var target in Enum.GetValues<AudioStreamPlayer.MixTarget>())
        {
            player.MixTargetMode = target; Thread.Sleep(30); native.PrepareCapture(channels * 2400); Thread.Sleep(30); var pcm = native.CapturedPCM(); Check(pcm.Length > channels * 480, "Real output quanta available.");
            for (var c = 0; c < channels; c++)
            {
                var expected = c < 2 && (channels == 2 || target != AudioStreamPlayer.MixTarget.Center) || c >= 4 && target == AudioStreamPlayer.MixTarget.Surround ? (c % 2 == 0 ? 4096f : -2048f) / 32767 : c == 2 && target != AudioStreamPlayer.MixTarget.Stereo ? 4096f / 32767 : c == 3 && target != AudioStreamPlayer.MixTarget.Stereo ? -4096f / 32767 : 0;
                var actual = pcm[c]; Check(MathF.Abs(actual - expected) < .00001f, $"Actual native {channels}-channel {target} output: channel {c}, {actual} vs {expected}.");
            }
        }
        Console.WriteLine($"Audio actual native {channels}-channel stereo/center/surround PCM passed; physical speakers unverified.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
