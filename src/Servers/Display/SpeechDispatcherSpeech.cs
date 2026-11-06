using System.Buffers.Text;
using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using System.Text;

namespace Electron2D;

internal sealed class SpeechDispatcherSpeech : INativeSpeech
{
    private static readonly UTF8Encoding UTF8 = new(false, true);
    private readonly Socket _socket;
    private readonly Queue<NativeSpeechEvent> _notifications = new(4096);
    private readonly byte[] _buffer = new byte[65536];
    private int _start, _end, _replyCode, _replyBytes, _eventFields, _eventPosition;
    private uint _eventID;
    private long _frameDeadline;
    private bool _ownMark;
    private List<string>? _replyParts;
    private IOException? _fault;
    private NativeSpeechEvent? _faultEvent;
    private bool _disposed, _paused, _started;
    private int _controlAcknowledgment;
    private uint _active;
    private NativeSpeechText? _text;
    private TTSVoice[]? _voices;
    private readonly record struct Reply(int Code, string[] Parts, string Reason, NativeSpeechEvent? Notification = null);

    internal SpeechDispatcherSpeech()
    {
        var address = Address();
        _socket = Connect(address);
        try
        {
            var user = new string(Environment.UserName.Select(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' ? c : '_').ToArray());
            Command("SET SELF CLIENT_NAME " + (user.Length == 0 ? "user" : user) + ":Electron2D:tts");
            Command("SET SELF NOTIFICATION ALL ON");
            Command("SET SELF PRIORITY TEXT");
            Command("SET SELF SSML_MODE ON");
            Command("SET SELF PAUSE_CONTEXT 0");
        }
        catch { _socket.Dispose(); throw; }
    }

    private static (string Kind, string Host, int Port) Address()
    {
        var address = Environment.GetEnvironmentVariable("SPEECHD_ADDRESS");
        if (address is null or "unix_socket")
        {
            var directory = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
            if (string.IsNullOrEmpty(directory)) directory = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
            if (string.IsNullOrEmpty(directory)) directory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache");
            return ("unix_socket", System.IO.Path.Combine(directory, "speech-dispatcher", "speechd.sock"), 0);
        }
        if (address.StartsWith("unix_socket:", StringComparison.Ordinal) && address.Length > 12) return ("unix_socket", address[12..], 0);
        var fields = address.Split(':');
        if (fields[0] == "inet_socket" && fields.Length <= 3)
        {
            var host = fields.Length > 1 ? fields[1] : "127.0.0.1"; var port = 6560;
            if (host.Length == 0 || fields.Length == 3 && (!int.TryParse(fields[2], NumberStyles.None, CultureInfo.InvariantCulture, out port) || port is <= 0 or > 65535)) throw new NotSupportedException("The Speech Dispatcher inet address is invalid.");
            return ("inet_socket", host, port);
        }
        throw new NotSupportedException("The configured Speech Dispatcher address is unsupported.");
    }
    private static Socket Connect((string Kind, string Host, int Port) address)
    {
        try { return Open(address); }
        catch (Exception first) when (first is SocketException or IOException or OperationCanceledException)
        {
            if (address.Kind == "inet_socket" && address.Host is not ("127.0.0.1" or "localhost")) throw new NotSupportedException("The configured remote speech service is unavailable.", first);
            try
            {
                var start = new ProcessStartInfo("speech-dispatcher") { UseShellExecute = false, CreateNoWindow = true };
                foreach (var value in new[] { "--spawn", "--communication-method", address.Kind, address.Kind == "unix_socket" ? "--socket-path" : "--port", address.Kind == "unix_socket" ? address.Host : address.Port.ToString(CultureInfo.InvariantCulture) }) start.ArgumentList.Add(value);
                using var process = Process.Start(start);
                if (process is null || !process.WaitForExit(10000))
                {
                    if (process is not null && !process.HasExited) { process.Kill(); process.WaitForExit(10000); }
                    throw new NotSupportedException("Speech Dispatcher autospawn did not complete.");
                }
                if (process.ExitCode != 0) throw new NotSupportedException("Speech Dispatcher autospawn is disabled or unavailable.");
                return Open(address);
            }
            catch (Exception error) when (error is System.ComponentModel.Win32Exception or SocketException or IOException or OperationCanceledException or NotSupportedException or InvalidOperationException)
            { throw new NotSupportedException("The system Speech Dispatcher service is unavailable.", error); }
        }
    }
    private static Socket Open((string Kind, string Host, int Port) address)
    {
        var socket = address.Kind == "unix_socket" ? new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified) : new Socket(SocketType.Stream, ProtocolType.Tcp);
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            if (address.Kind == "unix_socket") socket.ConnectAsync(new UnixDomainSocketEndPoint(address.Host), timeout.Token).AsTask().GetAwaiter().GetResult();
            else socket.ConnectAsync(address.Host, address.Port, timeout.Token).AsTask().GetAwaiter().GetResult();
            socket.ReceiveTimeout = 10000; socket.SendTimeout = 10000; return socket;
        }
        catch { socket.Dispose(); throw; }
    }

    public bool IsAvailable => !_disposed && _fault is null;
    public bool IsSpeaking => _active != 0;
    public int LastResult { get { if (_fault is not null) throw _fault; return 0; } }
    public TTSVoice[] GetVoices()
    {
        if (_voices is not null) return (TTSVoice[])_voices.Clone();
        var reply = Command("LIST SYNTHESIS_VOICES"); var voices = new TTSVoice[reply.Parts.Length];
        for (var i = 0; i < voices.Length; i++)
        {
            var fields = reply.Parts[i].Split('\t');
            if (fields.Length != 3 || fields[0].Length == 0 || fields.Any(value => value.IndexOfAny(['\0', '\r', '\n']) >= 0)) throw Break("The speech service returned invalid voice metadata.");
            voices[i] = new(fields[0], fields[0], fields[1] + "_" + fields[2]);
        }
        _voices = voices; return (TTSVoice[])voices.Clone();
    }
    public NativeSpeechText Prepare(string text, float pitch, string language)
    {
        var prepared = NativeSpeechText.SSIP(text, language);
        _ = UTF8.GetByteCount(prepared.Content);
        return prepared;
    }
    public uint Speak(string voice, NativeSpeechText text, int volume, float pitch, float rate)
    {
        Command("SET SELF SYNTHESIS_VOICE " + voice);
        Command("SET SELF VOLUME " + (volume * 2 - 100).ToString(CultureInfo.InvariantCulture));
        Command("SET SELF PITCH " + ((int)((pitch - 1) * 100)).ToString(CultureInfo.InvariantCulture));
        var nativeRate = rate > 1 ? Math.Log(Math.Min(rate, 2.5f)) / Math.Log(2.5) * 100 : rate < 1 ? Math.Log(Math.Max(rate, .5f)) / Math.Log(.5) * -100 : 0;
        Command("SET SELF RATE " + Math.Clamp((int)nativeRate, -100, 100).ToString(CultureInfo.InvariantCulture));
        Command("SPEAK");
        Reply queued;
        try
        {
            var content = text.Content.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
            content = content.Replace("\n.", "\n..", StringComparison.Ordinal);
            if (content.StartsWith('.')) content = "." + content;
            Write(content.Replace("\n", "\r\n", StringComparison.Ordinal) + "\r\n.\r\n");
            queued = Response();
        }
        catch (Exception error) when (error is IOException or SocketException or DecoderFallbackException) { throw Break("The speech service failed while accepting text.", error); }
        if (queued.Code is < 200 or >= 300 || queued.Parts.Length != 1 || !uint.TryParse(queued.Parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id == 0)
        {
            if (queued.Code is >= 300 and < 600) { Command("CANCEL SELF"); throw new IOException("The speech service rejected the utterance: " + queued.Reason); }
            throw Break("The speech service returned no valid utterance identity.");
        }
        _text = text; _active = id; _started = false; return id;
    }
    public void Pause()
    {
        if (_active == 0 || _paused) return;
        if (!_started) { _controlAcknowledgment = 0; AwaitControl(701); }
        if (_active == 0) return;
        _paused = true; _controlAcknowledgment = 0;
        try { Command("PAUSE SELF"); AwaitControl(704); }
        catch { _paused = false; throw; }
    }
    public void Resume()
    {
        if (!_paused) return;
        _controlAcknowledgment = 0; Command("RESUME SELF"); AwaitControl(705); _paused = false;
    }
    public void Purge()
    {
        if (IsAvailable)
        {
            // A retained paused message must re-enter playback before its terminal cancellation can be observed.
            Resume(); Command("CANCEL SELF"); AwaitControl(0);
        }
        _active = 0; _text = null; _started = false;
    }
    private void AwaitControl(int notificationCode)
    {
        try
        {
            var deadline = Environment.TickCount64 + 10000;
            while (_active != 0 && (notificationCode == 0 || _controlAcknowledgment != notificationCode))
            {
                if (!TryRead(true, out var reply, deadline)) continue;
                if (reply.Code < 700) throw new IOException("The speech service returned an unsolicited control reply.");
                QueueNotification(reply);
            }
        }
        catch (Exception error) when (error is IOException or SocketException or DecoderFallbackException)
        { throw Break("The speech service failed to finish its control transition.", error); }
    }
    private void QueueNotification(Reply reply)
    {
        if (reply.Notification is not { } notification) return;
        if (_notifications.Count == 4096) throw new IOException("The speech service notification queue exceeds its prepared capacity.");
        _notifications.Enqueue(notification);
    }

    public bool TryGetEvent(out NativeSpeechEvent value)
    {
        value = default;
        if (_faultEvent is { } terminal) { _faultEvent = null; value = terminal; return true; }
        if (_notifications.TryDequeue(out value)) return true;
        if (!IsAvailable) return false;
        try
        {
            for (var i = 0; i < 256; i++)
            {
                if (!TryRead(false, out var reply)) return false;
                if (reply.Code < 700) throw Break("The speech service returned an unsolicited control reply.");
                if (reply.Notification is not { } notification) continue;
                value = notification; return true;
            }
            return false;
        }
        catch (Exception error) when (error is IOException or SocketException or DecoderFallbackException)
        {
            var failure = Break("The speech service connection failed.", error);
            if (_faultEvent is { } canceled) { _faultEvent = null; value = canceled; return true; }
            throw failure;
        }
    }
    private Reply Command(string command)
    {
        ObjectDisposedException.ThrowIf(_disposed, this); if (_fault is not null) throw _fault;
        Reply reply;
        try { Write(command + "\r\n"); reply = Response(); }
        catch (Exception error) when (error is IOException or SocketException or DecoderFallbackException) { throw Break("The speech service control connection failed.", error); }
        if (reply.Code is < 200 or >= 300) throw new IOException("The speech service refused a command: " + reply.Reason);
        return reply;
    }
    private Reply Response()
    {
        var deadline = Environment.TickCount64 + 10000;
        while (true)
        {
            if (!TryRead(true, out var reply, deadline)) continue;
            if (reply.Code < 700) return reply;
            QueueNotification(reply);
        }
    }
    private void Write(string text)
    {
        var bytes = UTF8.GetBytes(text); var sent = 0;
        while (sent < bytes.Length)
        { var count = _socket.Send(bytes.AsSpan(sent), SocketFlags.None); if (count == 0) throw new IOException("The speech service stopped accepting data."); sent += count; }
    }
    private bool TryRead(bool wait, out Reply reply, long deadline = long.MaxValue)
    {
        reply = default;
        while (true)
        {
            var now = Environment.TickCount64;
            if ((_start < _end || _replyCode != 0) && _frameDeadline == 0) _frameDeadline = now + 10000;
            if (wait && now > deadline || _frameDeadline != 0 && now > _frameDeadline) throw new IOException("The speech service response timed out.");
            var newline = _buffer.AsSpan(_start, _end - _start).IndexOf((byte)'\n');
            if (newline >= 0)
            {
                var line = _buffer.AsSpan(_start, newline); if (line.Length != 0 && line[^1] == '\r') line = line[..^1];
                _start += newline + 1;
                if (_start == _end) _start = _end = 0;
                if (line.Length < 4 || line[3] is not ((byte)'-' or (byte)' ') || !Utf8Parser.TryParse(line[..3], out int code, out var digits) || digits != 3 || code is < 100 or >= 600 and < 700 or > 999 || line.IndexOf((byte)0) >= 0) throw new IOException("The speech service reply framing is invalid.");
                if (_replyCode == 0) { _replyCode = code; _replyBytes = 0; _eventFields = 0; _ownMark = false; }
                if (code != _replyCode || (long)_replyBytes + line.Length > 64 * 1024 * 1024) throw new IOException("The speech service reply exceeds its valid frame.");
                _replyBytes += line.Length; var data = line[4..];
                if (code >= 700)
                {
                    if (code > 705) throw new IOException("The speech service returned an unknown notification.");
                    _ = UTF8.GetCharCount(data);
                    if (line[3] == '-')
                    {
                        if (_eventFields == 0)
                        { if (!Utf8Parser.TryParse(data, out _eventID, out var used) || used != data.Length || _eventID == 0) throw new IOException("The speech service notification identity is invalid."); }
                        else if (_eventFields == 1)
                        { if (!Utf8Parser.TryParse(data, out uint _, out var used) || used != data.Length) throw new IOException("The speech service notification client is invalid."); }
                        else if (_eventFields == 2 && code == 700)
                        {
                            if (_eventID == _active && _text is not null && data.StartsWith(_text.MarkPrefixBytes))
                            {
                                var suffix = data[_text.MarkPrefixBytes.Length..];
                                if (!Utf8Parser.TryParse(suffix, out _eventPosition, out var used) || used != suffix.Length || _eventPosition < 0) throw new IOException("The speech service source mark is invalid.");
                                _ownMark = true;
                            }
                        }
                        else throw new IOException("The speech service notification has excess fields.");
                        _eventFields++; continue;
                    }
                    if (_eventFields != (code == 700 ? 3 : 2)) throw new IOException("The speech service notification is incomplete.");
                    NativeSpeechEvent? notification = code switch
                    {
                        700 when _ownMark => new(DisplayServer.TTSUtteranceEvent.Boundary, _eventID, _eventPosition),
                        701 => new(DisplayServer.TTSUtteranceEvent.Started, _eventID),
                        702 => new(DisplayServer.TTSUtteranceEvent.Ended, _eventID),
                        703 => new(DisplayServer.TTSUtteranceEvent.Canceled, _eventID),
                        _ => null
                    };
                    if (_eventID == _active && code is 701 or 704 or 705) _controlAcknowledgment = code;
                    if (_eventID == _active && code == 701) _started = true;
                    if (_eventID == _active && code is 702 or 703) { _active = 0; _text = null; _started = false; }
                    reply = new(code, Array.Empty<string>(), "", notification); _replyCode = 0; _frameDeadline = 0; return true;
                }
                _replyParts ??= [];
                if (line[3] == '-') { _replyParts.Add(UTF8.GetString(data)); continue; }
                reply = new(code, _replyParts.ToArray(), UTF8.GetString(data)); _replyParts = null; _replyCode = 0; _frameDeadline = 0; return true;
            }
            if (_end - _start >= 65536) throw new IOException("The speech service reply line exceeds its budget.");
            if (!wait && _socket.Available == 0)
            { if (_socket.Poll(0, SelectMode.SelectRead)) throw new IOException("The speech service closed the connection."); return false; }
            if (_start != 0) { _buffer.AsSpan(_start, _end - _start).CopyTo(_buffer); _end -= _start; _start = 0; }
            if (_end == _buffer.Length)
                throw new IOException("The speech service reply line exceeds its budget.");
            var count = _socket.Receive(_buffer.AsSpan(_end), SocketFlags.None);
            if (count == 0) throw new IOException("The speech service closed the connection.");
            _end += count;
        }
    }
    private IOException Break(string message, Exception? error = null)
    {
        if (_fault is not null) return _fault;
        _fault = new IOException(message, error);
        if (_active != 0) _faultEvent = new(DisplayServer.TTSUtteranceEvent.Ended, _active);
        _active = 0; _text = null; _voices = null; _paused = _started = false; _controlAcknowledgment = 0; _notifications.Clear(); _replyParts = null; _socket.Dispose(); return _fault;
    }
    public void Dispose() { if (_disposed) return; _disposed = true; _active = 0; _text = null; _voices = null; _paused = _started = false; _controlAcknowledgment = 0; _notifications.Clear(); _faultEvent = null; _socket.Dispose(); }
}
