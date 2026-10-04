using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
namespace Electron2D;

internal sealed class ENetHandle : SafeHandle
{
    internal ENetHandle() : base(0, true) { }
    public override bool IsInvalid => handle == 0;
    internal nint Pointer { get => handle; set => SetHandle(value); }
    protected override bool ReleaseHandle() { ENetNative.Destroy(handle); return true; }
}
[StructLayout(LayoutKind.Sequential)]
internal struct ENetNativeEvent { internal int Type, Peer, Channel; internal uint Data; internal nint Packet; }
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct ENetNativeBuffer { internal byte* Data; internal nuint Length; }
internal static unsafe partial class ENetNative
{
    private const string Library = "Electron2DENet";
    private static readonly object Gate = new();
    private static bool _ready;
    internal static void Prepare()
    {
        if (!OperatingSystem.IsLinux() || IntPtr.Size != 8) throw new PlatformNotSupportedException("ENet currently requires the packaged Linux64 native backend.");
        lock (Gate) { if (_ready) return; Callbacks(&ENetTransport.SendCallback, &ENetTransport.ReceiveCallback, &ENetTransport.WaitCallback, &ENetTransport.ControlCallback); _ready = true; }
    }
    [LibraryImport(Library, EntryPoint = "e2d_enet_callbacks")] private static partial void Callbacks(delegate* unmanaged[Cdecl]<int, uint, ushort, ENetNativeBuffer*, nuint, int> send, delegate* unmanaged[Cdecl]<int, uint*, ushort*, byte*, nuint, int> receive, delegate* unmanaged[Cdecl]<int, uint*, uint, int> wait, delegate* unmanaged[Cdecl]<int, int, int, int> control);
    [LibraryImport(Library, EntryPoint = "e2d_enet_create")] internal static partial nint Create(int socketID, int bound, ushort port, int peers, int channels, uint inBandwidth, uint outBandwidth);
    [LibraryImport(Library, EntryPoint = "e2d_enet_destroy")] internal static partial void Destroy(nint context);
    [LibraryImport(Library, EntryPoint = "e2d_enet_connect")] internal static partial int Connect(ENetHandle context, uint address, ushort port, int channels, uint data);
    [LibraryImport(Library, EntryPoint = "e2d_enet_service")] internal static partial int Service(ENetHandle context, out ENetNativeEvent result, uint timeout, int check);
    [LibraryImport(Library, EntryPoint = "e2d_enet_flush")] internal static partial void Flush(ENetHandle context);
    [LibraryImport(Library, EntryPoint = "e2d_enet_send")] private static partial int Send(ENetHandle context, int peer, byte channel, byte* data, nuint size, uint flags);
    internal static int SendPacket(ENetHandle context, int peer, byte channel, ReadOnlySpan<byte> data, uint flags) { fixed (byte* pointer = data) return Send(context, peer, channel, pointer, (nuint)data.Length, flags); }
    [LibraryImport(Library, EntryPoint = "e2d_enet_packet")] private static partial nuint Packet(nint packet, out byte* data, out uint flags);
    internal static ReadOnlySpan<byte> PacketData(nint packet, out uint flags) { var size = Packet(packet, out var data, out flags); if (size > 16777216) throw new InvalidDataException("ENet packet exceeds its native budget."); return new(data, (int)size); }
    [LibraryImport(Library, EntryPoint = "e2d_enet_release")] internal static partial void ReleasePacket(nint packet);
    [LibraryImport(Library, EntryPoint = "e2d_enet_peer")] internal static partial uint Peer(ENetHandle context, int peer, int operation, uint a, uint b, uint c);
    [LibraryImport(Library, EntryPoint = "e2d_enet_stat")] internal static partial double Statistic(ENetHandle context, int peer, int statistic);
    [LibraryImport(Library, EntryPoint = "e2d_enet_host")] internal static partial uint Host(ENetHandle context, int operation, uint a, uint b);
    [LibraryImport(Library, EntryPoint = "e2d_enet_compress")] internal static partial int Compress(ENetHandle context, int mode);
}
