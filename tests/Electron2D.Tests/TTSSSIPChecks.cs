using Electron2D;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

internal static class TTSSSIPChecks
{
    private static int _checks;
    private static void Check(bool value, string reason) { _checks++; if (!value) throw new Exception(reason); }
    internal static void Run()
    {
        if (!(OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD())) throw new NotSupportedException("This profile requires the native SSIP platform.");
        var previousAddress = Environment.GetEnvironmentVariable("SPEECHD_ADDRESS");
        var previousSetting = ProjectSettings.Get(ProjectSettings.AudioGeneralTextToSpeech);
        try
        {
            ProjectSettings.Set(ProjectSettings.AudioGeneralTextToSpeech, true);
            using (var peer = new Peer(success: 299, manualStart: true))
            using (var display = DisplayServer.Open("Fragmented speech protocol", new(64, 64), hidden: true))
            {
                Check(DisplayServer.TTSGetVoices().Single().Language == "en-US_none", "Fragmented native metadata is a typed snapshot.");
                DisplayServer.TTSSpeak("retained", "Voice"); var invalidText = false;
                try { DisplayServer.TTSSpeak("\ud800", "Voice", interrupt: true); } catch (ArgumentException) { invalidText = true; }
                Check(invalidText && DisplayServer.TTSIsSpeaking(), "Malformed UTF16 rejects before interrupting previously queued work.");
                DisplayServer.TTSStop(); DisplayServer.ProcessEvents();
                var events = new List<(string Kind, long ID, int At)>();
                DisplayServer.TTSSetUtteranceCallback(DisplayServer.TTSUtteranceEvent.Started, id => events.Add(("start", id, 0)));
                DisplayServer.TTSSetUtteranceCallback(DisplayServer.TTSUtteranceEvent.Canceled, id => events.Add(("cancel", id, 0)));
                DisplayServer.TTSSetUtteranceCallback(DisplayServer.TTSUtteranceEvent.Boundary, (at, id) => events.Add(("mark", id, at)));
                DisplayServer.TTSSpeak("One\n.\n..two <mark name=\"authored\"/> three.", "Voice", utteranceID: long.MinValue);
                DisplayServer.ProcessEvents();
                Check(peer.Text.Contains("\r\n..<break/>\r\n...two", StringComparison.Ordinal), "SSIP success groups are accepted and data is dot-stuffed without losing punctuation.");
                peer.Send("701-1\r\n"); DisplayServer.ProcessEvents();
                Check(events.Count == 0, "A partial native notice never executes early.");
                peer.Send("701-1\r\n701 BEGIN\r\n");
                var snapshot = DisplayServer.TTSGetVoices(); snapshot[0] = default;
                Check(DisplayServer.TTSGetVoices()[0].ID == "Voice" && peer.VoiceQueries == 1, "Producer catalog loads once while public snapshots remain independent.");
                DisplayServer.TTSPause(); Check(DisplayServer.TTSIsPaused(), "Cold controls can finish an already partial notification.");
                var mark = Regex.Match(peer.Text, "name=\"(e2d_[a-f0-9]+_3)\"").Groups[1].Value;
                Check(mark.Length != 0, "Prepared speech has a private source mark.");
                peer.Send("700-1\r\n700-1\r\n700-authored\r\n700 MARK\r\n700-1\r\n700-1\r\n700-" + mark + "\r\n700 MARK\r\n703-1\r\n703-1\r\n703 CANCELED\r\n");
                Pump(() => events.Count >= 3);
                Check(events.SequenceEqual(new[] { ("start", long.MinValue, 0), ("mark", long.MinValue, 3), ("cancel", long.MinValue, 0) }), "Owner delivery ignores authored marks and retains unsolicited cancellation and full IDs.");
                events.Clear(); DisplayServer.TTSSetUtteranceCallback(DisplayServer.TTSUtteranceEvent.Started, (Action<long>?)null); DisplayServer.TTSSpeak("active", "Voice", utteranceID: 2); DisplayServer.ProcessEvents();
                peer.Send("701-2\r\n701-1\r\n701 BEGIN\r\n"); DisplayServer.TTSSpeak("pending", "Voice", utteranceID: 3); DisplayServer.TTSPause(); peer.Disconnect();
                FailurePump(); DisplayServer.ProcessEvents();
                Check(events.SequenceEqual(new[] { ("cancel", 2L, 0), ("cancel", 3L, 0) }), "EOF cancels paused active and pending work exactly once.");
                Check(!DisplayServer.HasFeature(DisplayServer.Feature.TextToSpeech) && !DisplayServer.TTSIsSpeaking() && !DisplayServer.TTSIsPaused() && DisplayServer.TTSGetVoices().Length == 0, "A broken producer becomes unavailable and clears pause.");
            }
            foreach (var code in new[] { 301, 401, 501 })
            {
                using var peer = new Peer(rejection: code); using var display = DisplayServer.Open("Refused native speech", new(64, 64), hidden: true);
                var canceled = new List<long>(); var ended = new List<long>();
                DisplayServer.TTSSetUtteranceCallback(DisplayServer.TTSUtteranceEvent.Canceled, canceled.Add);
                DisplayServer.TTSSetUtteranceCallback(DisplayServer.TTSUtteranceEvent.Ended, ended.Add);
                DisplayServer.TTSSpeak("refused", "Voice", utteranceID: 101); DisplayServer.TTSSpeak("later", "Voice", utteranceID: 102);
                FailurePump(); Check(canceled.SequenceEqual(new long[] { 101 }) && DisplayServer.HasFeature(DisplayServer.Feature.TextToSpeech), "A refused operation cancels only that request and leaves its healthy connection usable.");
                peer.Send("701-2\r\n701-1\r\n701 BEGIN\r\n702-2\r\n702-1\r\n702 END\r\n"); Pump(() => ended.Count != 0);
                Check(ended.SequenceEqual(new long[] { 102 }) && canceled.Count == 1, "Later native speech completes after a server/client/syntax refusal.");
            }
            foreach (var corrupt in new[] { "701-1\r\n702 END\r\n", "700-1\r\n700 MARK\r\n", "706 UNKNOWN\r\n", new string('x', 65536), "200 UNSOLICITED\r\n", "600 INVALID\r\n" })
                Fault(Encoding.UTF8.GetBytes(corrupt));
            Fault([55, 48, 49, 45, 0xff, 13, 10]);
            using (var peer = new Peer(manualStart: true))
            using (var display = DisplayServer.Open("Expired partial speech notice", new(64, 64), hidden: true))
            {
                var canceled = 0; DisplayServer.TTSSetUtteranceCallback(DisplayServer.TTSUtteranceEvent.Canceled, (long _) => canceled++);
                DisplayServer.TTSSpeak("active", "Voice"); DisplayServer.ProcessEvents(); DisplayServer.TTSSpeak("pending", "Voice");
                peer.Send("701-1\r\n"); DisplayServer.ProcessEvents(); Thread.Sleep(10100); FailurePump();
                Check(canceled == 2 && !DisplayServer.HasFeature(DisplayServer.Feature.TextToSpeech), "An incomplete notice expires across owner pumps instead of retaining requests forever.");
            }
            using (var peer = new Peer("Voice\rINJECT"))
            using (var display = DisplayServer.Open("Invalid speech metadata", new(64, 64), hidden: true))
            {
                var failed = false; try { DisplayServer.TTSGetVoices(); } catch (IOException) { failed = true; }
                Check(failed && !DisplayServer.HasFeature(DisplayServer.Feature.TextToSpeech), "Control characters in native identifiers cannot inject commands.");
            }
            Environment.SetEnvironmentVariable("SPEECHD_ADDRESS", "inet_socket:127.0.0.2:1");
            using (var display = DisplayServer.Open("Absent remote speech", new(64, 64), hidden: true)) Check(!DisplayServer.HasFeature(DisplayServer.Feature.TextToSpeech), "An absent remote endpoint does not autospawn a local daemon.");
            Environment.SetEnvironmentVariable("SPEECHD_ADDRESS", "unsupported");
            using (var display = DisplayServer.Open("Invalid speech endpoint", new(64, 64), hidden: true)) Check(DisplayServer.TTSGetVoices().Length == 0, "Unsupported endpoint is explicitly unavailable.");
        }
        finally { Environment.SetEnvironmentVariable("SPEECHD_ADDRESS", previousAddress); ProjectSettings.Set(ProjectSettings.AudioGeneralTextToSpeech, previousSetting); }
        Console.WriteLine($"SSIP: {_checks} public protocol/failure checks; synthetic peer, no audio claim.");
    }
    private static void Fault(byte[] bytes)
    {
        using var peer = new Peer(); using var display = DisplayServer.Open("Corrupt speech protocol", new(64, 64), hidden: true);
        var canceled = 0; DisplayServer.TTSSetUtteranceCallback(DisplayServer.TTSUtteranceEvent.Canceled, (long _) => canceled++);
        DisplayServer.TTSSpeak("active", "Voice"); DisplayServer.ProcessEvents(); DisplayServer.TTSSpeak("pending", "Voice");
        Console.WriteLine($"SSIP fault control: bytes={bytes.Length}; header={Convert.ToHexString(bytes.AsSpan(0, Math.Min(bytes.Length, 8)))}.");
        peer.Send(bytes); FailurePump(); DisplayServer.ProcessEvents();
        Check(canceled == 2 && !DisplayServer.HasFeature(DisplayServer.Feature.TextToSpeech), "Malformed native frames report failure and retire every request once.");
    }
    private static void Pump(Func<bool> ready)
    {
        var deadline = Environment.TickCount64 + 3000;
        while (!ready()) { if (Environment.TickCount64 > deadline) throw new Exception("Synthetic speech progress timed out."); DisplayServer.ProcessEvents(); Thread.Sleep(5); }
    }
    private static void FailurePump()
    {
        var failed = false;
        Pump(() => { try { DisplayServer.ProcessEvents(); } catch (AggregateException error) { Check(error.Flatten().InnerExceptions.Any(value => value is IOException), "Protocol failure crosses the managed pump boundary."); failed = true; } return failed; });
    }
    private sealed class Peer : IDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly Thread _thread;
        private readonly object _gate = new();
        private TcpClient? _client;
        private readonly string _voice;
        private readonly int _rejection, _success;
        private readonly bool _manualStart;
        internal string Text = "";
        internal int VoiceQueries;
        internal Peer(string voice = "Voice", int rejection = 0, int success = 225, bool manualStart = false)
        {
            _voice = voice; _rejection = rejection; _success = success; _manualStart = manualStart; _listener.Start();
            Environment.SetEnvironmentVariable("SPEECHD_ADDRESS", "inet_socket:127.0.0.1:" + ((IPEndPoint)_listener.LocalEndpoint).Port);
            _thread = new Thread(Serve) { IsBackground = true }; _thread.Start();
        }
        private void Serve()
        {
            try
            {
                _client = _listener.AcceptTcpClient(); _client.NoDelay = true;
                using var reader = new StreamReader(_client.GetStream(), new UTF8Encoding(false, true)); uint id = 0, active = 0;
                while (reader.ReadLine() is { } line)
                {
                    if (line == "LIST SYNTHESIS_VOICES") { VoiceQueries++; Send("249-" + _voice); Send("\ten-US\tnone\r\n249 OK\r\n"); }
                    else if (line == "SPEAK")
                    {
                        Send((_success == 225 ? 230 : _success) + " RECEIVING\r\n"); var text = new StringBuilder();
                        while (reader.ReadLine() is { } part && part != ".") text.Append(part).Append("\r\n");
                        Text = text.ToString(); id++;
                        if (id == 1 && _rejection != 0) Send(_rejection + " REFUSED\r\n"); else { active = id; Send(_success + "-" + id + "\r\n" + _success + " QUEUED\r\n"); if (!_manualStart) { Thread.Sleep(20); Send($"701-{active}\r\n701-1\r\n701 BEGIN\r\n"); } }
                    }
                    else if (line is "PAUSE SELF" or "CANCEL SELF" or "RESUME SELF")
                    {
                        Send("200 OK\r\n"); Thread.Sleep(20);
                        if (active != 0)
                        {
                            var code = line == "PAUSE SELF" ? 704 : line == "RESUME SELF" ? 705 : 703;
                            Send($"{code}-{active}\r\n{code}-1\r\n{code} CONTROL FINISHED\r\n");
                            if (code == 703) active = 0;
                        }
                    }
                    else Send("200 OK\r\n");
                }
            }
            catch (Exception error) when (error is IOException or SocketException or ObjectDisposedException) { }
        }
        internal void Send(string value) => Send(Encoding.UTF8.GetBytes(value));
        internal void Send(byte[] value) { lock (_gate) _client!.GetStream().Write(value); }
        internal void Disconnect() { lock (_gate) _client?.Dispose(); }
        public void Dispose() { Disconnect(); _listener.Stop(); if (!_thread.Join(2000)) throw new Exception("Synthetic speech peer did not retire."); }
    }
}
