using System.Runtime.InteropServices;
using static Electron2D.LinuxPortalDBus;

namespace Electron2D;

internal sealed class LinuxPortalRequest : IDisposable
{
    private const string RequestInterface = "org.freedesktop.portal.Request";
    private const string BusInterface = "org.freedesktop.DBus";
    private const string RequestPrefix = DesktopPath + "/request/";

    private readonly string _portal;
    private readonly string _token = "electron2d_" + Guid.NewGuid().ToString("N");
    private nint _connection;
    private string _owner = "", _path = "";
    private bool _completed;

    internal LinuxPortalRequest(string portal, uint requiredVersion)
    {
        _portal = portal;
        try
        {
            Check(ThreadsInitDefault());
            _connection = BusGetPrivate(0, 0);
            if (_connection == 0)
                throw new NotSupportedException("The session bus is unavailable.");
            SetExitOnDisconnect(_connection, 0);
            if (QueryVersion(_connection, portal, out _owner) < requiredVersion || _owner.Length == 0)
                throw new NotSupportedException($"The {portal} portal is unavailable or too old.");

            var sender = Marshal.PtrToStringUTF8(BusGetUniqueName(_connection));
            if (sender is not { Length: > 1 } || sender[0] != ':')
                throw new InvalidOperationException("The portal connection has no sender identity.");
            _path = RequestPrefix + sender[1..].Replace('.', '_') + "/" + _token;

            // Subscribe before launch so a response arriving before its method reply is retained.
            BusAddMatch(_connection, $"type='signal',sender='{_owner}',interface='{RequestInterface}',member='Response'", 0);
            BusAddMatch(_connection, $"type='signal',sender='{BusInterface}',interface='{BusInterface}',member='NameOwnerChanged',arg0='{_owner}'", 0);
        }
        catch { Dispose(); throw; }
    }

    internal void Launch(string method, Action<nint, string> writeArguments, int timeoutMs = 5000)
    {
        var message = NewMethodCall(_owner, DesktopPath, _portal, method);
        if (message == 0) throw new OutOfMemoryException();
        try
        {
            writeArguments(message, _token);
            var reply = SendWithReplyAndBlock(_connection, message, timeoutMs, 0);
            if (reply == 0) throw new InvalidOperationException($"The {_portal} portal could not start {method}.");
            try
            {
                if (MessageType(reply) != 2 || Init(reply, out var value) == 0)
                    throw new InvalidOperationException("The portal returned no request.");
                var path = ReadString(ref value, PathType);
                if (!path.StartsWith(RequestPrefix, StringComparison.Ordinal) || Next(ref value) != 0)
                    throw new InvalidOperationException("The portal returned an invalid request path.");
                _path = path;
            }
            finally { MessageUnref(reply); }
        }
        finally { MessageUnref(message); }
    }

    // Transfers the matching response to the caller for decoding and unref.
    internal nint TakeResponse()
    {
        var connected = ReadWrite(_connection, 0) != 0;
        nint message;
        while ((message = PopMessage(_connection)) != 0)
        {
            var sender = Marshal.PtrToStringUTF8(MessageSender(message));
            if (sender == _owner && IsSignal(message, RequestInterface, "Response") != 0 &&
                Marshal.PtrToStringUTF8(MessagePath(message)) == _path)
            {
                _completed = true;
                return message;
            }

            try
            {
                if (sender == BusInterface && IsSignal(message, BusInterface, "NameOwnerChanged") != 0 &&
                    Init(message, out var changed) != 0 && ReadString(ref changed) == _owner)
                {
                    Next(ref changed);
                    _ = ReadString(ref changed);
                    Next(ref changed);
                    if (ReadString(ref changed).Length == 0)
                        throw new InvalidOperationException("The desktop portal disconnected.");
                }
            }
            finally { MessageUnref(message); }
        }

        if (!connected)
            throw new InvalidOperationException("The portal session bus disconnected.");
        return 0;
    }

    public void Dispose()
    {
        if (_connection == 0) return;
        if (!_completed && _path.Length != 0)
        {
            var message = NewMethodCall(_owner, _path, RequestInterface, "Close");
            if (message != 0)
            {
                try { Send(_connection, message, 0); ReadWrite(_connection, 0); }
                finally { MessageUnref(message); }
            }
        }
        ConnectionClose(_connection);
        ConnectionUnref(_connection);
        _connection = 0;
    }
}
