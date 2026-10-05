using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Electron2D;

internal static class NativeChecks
{
    internal static void Run()
    {
        VerifyTLS();
        var pcm = new byte[48000 * 2];
        for (var frame = 0; frame < pcm.Length / 2; frame++)
            BinaryPrimitives.WriteInt16LittleEndian(pcm.AsSpan(frame * 2), (short)(6000 * Math.Sin(2 * Math.PI * 440 * frame / 48000)));
        using var stream = new AudioStreamWAV { Data = pcm, MixRate = 48000 };
        VerifySynchronizedGain(stream);
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
                    throw new InvalidOperationException("Native WOFF2 shaping or dictionary wrapping failed.");
                using var host = new ENetConnection();
                host.CreateHostBound("127.0.0.1", 0);
                if (host.GetLocalPort() <= 0) throw new InvalidOperationException("Native ENet binding failed.");
                AudioServer.AddBusEffect(0, capture);
                try
                {
                    player.Play();
                    var watch = Stopwatch.StartNew();
                    var nonzero = false;
                    while (watch.Elapsed < TimeSpan.FromSeconds(5) && !nonzero)
                    {
                        Thread.Sleep(20);
                        tree.ProcessFrame(0.02);
                        foreach (var sample in capture.GetBuffer(capture.GetFramesAvailable()))
                        {
                            if (!sample.IsFinite()) throw new InvalidOperationException("Native PCM contains a nonfinite sample.");
                            nonzero |= Math.Abs(sample.X) > 0.001f || Math.Abs(sample.Y) > 0.001f;
                        }
                    }
                    var latency = AudioServer.GetOutputLatency();
                    if (!nonzero || player.GetPlaybackPosition() <= 0 || !double.IsFinite(latency) || latency <= 0)
                        throw new InvalidOperationException($"Native audio PCM/output bridge failed: nonzero={nonzero}, pushed={capture.GetPushedFrames()}, position={player.GetPlaybackPosition()}, latency={AudioServer.GetOutputLatency()}.");
                    player.Stop();
                }
                finally { AudioServer.RemoveBusEffect(0, 0); }
            }
            finally { Engine.Stop(); }
        }
        Console.WriteLine("Native WOFF2/text boundaries, ENet, TLS, synchronized gain, audio PCM and repeated lifecycle checks passed.");
    }

    private static void VerifySynchronizedGain(AudioStreamWAV stream)
    {
        using var synchronized = new AudioStreamSynchronized { StreamCount = 1 };
        synchronized.SetSyncStream(0, stream);
        using var reference = stream.InstantiatePlayback();
        using var playback = synchronized.InstantiatePlayback();
        reference.Start(); playback.Start();
        foreach (var volume in new[] { -6f, -12f, 0f, float.NegativeInfinity })
        {
            synchronized.SetSyncStreamVolume(0, volume);
            var expected = reference.MixAudio(1, 128);
            var actual = playback.MixAudio(1, 128);
            var gain = (float)Mathf.DBToLinear(volume);
            if (expected.Length != 128 || actual.Length != 128)
                throw new InvalidOperationException("Synchronized audio did not supply its requested frames.");
            for (var frame = 0; frame < actual.Length; frame++)
                if (!actual[frame].IsFinite() || Math.Abs(actual[frame].X - expected[frame].X * gain) > 0.00001f ||
                    Math.Abs(actual[frame].Y - expected[frame].Y * gain) > 0.00001f)
                    throw new InvalidOperationException("Atomic synchronized audio gain failed.");
        }
    }

    private static void VerifyTLS()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
        var names = new SubjectAlternativeNameBuilder();
        names.AddDnsName("localhost"); names.AddIpAddress(IPAddress.Loopback);
        request.CertificateExtensions.Add(names.Build());
        using var identity = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow.AddHours(1));
        using var key = new CryptoKey(); key.LoadFromString(rsa.ExportRSAPrivateKeyPem());
        using var certificate = new Electron2D.X509Certificate(); certificate.LoadFromString(identity.ExportCertificatePem());
        using var serverOptions = TLSOptions.Server(key, certificate);
        foreach (var expected in new[] { TLSStatus.Connected, TLSStatus.ErrorHostnameMismatch, TLSStatus.Error })
        {
            using var listener = new TCPServer(); listener.Listen(0, "127.0.0.1");
            using var socket = new StreamPeerTCP(); socket.ConnectToHost("127.0.0.1", listener.GetLocalPort());
            Wait(() => { socket.Poll(); return socket.GetStatus() == StreamSocketStatus.Connected && listener.IsConnectionAvailable(); });
            using var accepted = listener.TakeConnection()!;
            using var clientOptions = TLSOptions.Client(expected == TLSStatus.Error ? null : certificate);
            using var server = new StreamPeerTLS(); using var client = new StreamPeerTLS();
            server.AcceptStream(accepted, serverOptions);
            client.ConnectToStream(socket, expected == TLSStatus.ErrorHostnameMismatch ? "wrong.invalid" : "localhost", clientOptions);
            Wait(() =>
            {
                try { client.Poll(); server.Poll(); }
                catch (AuthenticationException) when (expected != TLSStatus.Connected) { }
                return client.GetStatus() != TLSStatus.Handshaking && (expected != TLSStatus.Connected || server.GetStatus() == TLSStatus.Connected);
            });
            if (client.GetStatus() != expected) throw new InvalidOperationException("Native TLS trust/name validation failed.");
            if (expected != TLSStatus.Connected) continue;
            var sent = new byte[] { 1, 2, 3, 4 }; var received = new byte[sent.Length];
            client.PutData(sent);
            Wait(() => { client.Poll(); server.Poll(); return server.GetAvailableBytes() >= sent.Length; });
            server.GetData(received);
            if (!sent.AsSpan().SequenceEqual(received)) throw new InvalidOperationException("Native TLS record transfer failed.");
            client.DisconnectFromStream();
            Wait(() => { server.Poll(); return server.GetStatus() == TLSStatus.Disconnected; });
        }
        Console.WriteLine("Native TLS records, custom trust, hostname rejection and OS-trust rejection passed.");
    }

    private static void Wait(Func<bool> complete)
    {
        var watch = Stopwatch.StartNew();
        while (!complete())
        {
            if (watch.Elapsed > TimeSpan.FromSeconds(5)) throw new TimeoutException("Native operation did not complete.");
            Thread.Sleep(1);
        }
    }
}
