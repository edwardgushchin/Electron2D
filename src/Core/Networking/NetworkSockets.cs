using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
namespace Electron2D;

internal static partial class NetworkSockets
{
    internal static void ConfigureListener(Socket socket)
    {
        if (!OperatingSystem.IsLinux()) { socket.ExclusiveAddressUse = true; return; }
        // Linux SO_REUSEADDR permits restart after FIN without enabling SO_REUSEPORT listener sharing.
        var retained = false;
        try { socket.SafeHandle.DangerousAddRef(ref retained); var value = 1; if (SetOption(socket.Handle.ToInt32(), 1, 2, in value, 4) != 0) throw new SocketException(Marshal.GetLastPInvokeError()); }
        finally { if (retained) socket.SafeHandle.DangerousRelease(); }
    }
    [LibraryImport("libc", EntryPoint = "setsockopt", SetLastError = true)]
    private static partial int SetOption(int socket, int level, int option, in int value, int size);
    internal static IPAddress BindAddress(string address)
    {
        ArgumentNullException.ThrowIfNull(address);
        if (address == "*") return Socket.OSSupportsIPv6 ? IPAddress.IPv6Any : IPAddress.Any;
        if (!IPAddress.TryParse(address, out var ip)) throw new ArgumentException("A bind address must be an IP literal or wildcard.", nameof(address));
        return ip;
    }
    internal static IPAddress Resolve(string host)
    {
        ArgumentException.ThrowIfNullOrEmpty(host);
        if (IPAddress.TryParse(host, out var ip)) return ip;
        var addresses = Dns.GetHostAddresses(host);
        if (addresses.Length == 0) throw new SocketException((int)SocketError.HostNotFound);
        return addresses[0];
    }
    internal static void Port(int port, bool remote = false) { if (port < (remote ? 1 : 0) || port > 65535) throw new ArgumentOutOfRangeException(nameof(port)); }
    internal static Socket Create(AddressFamily family, SocketType type)
    {
        if (OperatingSystem.IsBrowser()) throw new PlatformNotSupportedException("Raw sockets require a native host.");
        var socket = new Socket(family, type, family == AddressFamily.Unix ? ProtocolType.Unspecified : type == SocketType.Stream ? ProtocolType.Tcp : ProtocolType.Udp);
        try { socket.Blocking = false; if (family == AddressFamily.InterNetworkV6) socket.DualMode = true; return socket; } catch { socket.Dispose(); throw; }
    }
    internal static IPEndPoint Endpoint(Socket socket, IPAddress address, int port)
    {
        if (socket.AddressFamily == AddressFamily.InterNetworkV6 && address.AddressFamily == AddressFamily.InterNetwork) address = address.MapToIPv6();
        return new(address, port);
    }
    internal static string Host(IPAddress address) => (address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address).ToString();
    internal static bool Busy(SocketError error) => error is SocketError.WouldBlock or SocketError.InProgress or SocketError.AlreadyInProgress;
}
