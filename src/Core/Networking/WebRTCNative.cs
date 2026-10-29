using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
namespace Electron2D;

internal sealed class WebRTCPeerHandle : SafeHandle
{
    internal WebRTCPeerHandle(nint value) : base(0, true) => SetHandle(value);
    public override bool IsInvalid => handle == 0;
    protected override bool ReleaseHandle() { WebRTCNative.Destroy(handle); return true; }
}
internal sealed class WebRTCChannelHandle : SafeHandle
{
    internal WebRTCChannelHandle(nint value) : base(0, true) => SetHandle(value);
    public override bool IsInvalid => handle == 0;
    protected override bool ReleaseHandle() { WebRTCNative.DestroyChannel(handle); return true; }
}
internal static unsafe partial class WebRTCNative
{
    private const string Library = NativeLibraries.WebRTCLibrary;
    internal static void Check(int result) { if (result < 0) throw new IOException(Message()); }
    internal static string Message() => Marshal.PtrToStringUTF8(Error()) ?? "Native WebRTC failure.";
    internal static nint Require(nint value) { if (value == 0) throw new IOException(Message()); return value; }
    internal static void CheckError() { if (*(byte*)Error() != 0) throw new IOException(Message()); }
    [LibraryImport(Library, EntryPoint = "e2d_rtc_error"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] internal static partial nint Error();
    internal static nint Create(nint* urls, nint* users, nint* passwords, int count)
    {
        TLSNative.CheckBackend();
        try
        {
            if (TLSVersion() != 0x40100000) throw new PlatformNotSupportedException("WebRTC requires the matching OpenSSL 4.1.0 native libraries.");
        }
        catch (EntryPointNotFoundException error) { throw new PlatformNotSupportedException("WebRTC requires the matching native transport bundle.", error); }
        return CreateNative(urls, users, passwords, count);
    }
    [LibraryImport(Library, EntryPoint = "e2d_rtc_tls_version"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial ulong TLSVersion();
    [LibraryImport(Library, EntryPoint = "e2d_rtc_create"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] private static partial nint CreateNative(nint* urls, nint* users, nint* passwords, int count);
    [LibraryImport(Library, EntryPoint = "e2d_rtc_destroy"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] internal static partial void Destroy(nint peer);
    [LibraryImport(Library, EntryPoint = "e2d_rtc_close"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] internal static partial int Close(WebRTCPeerHandle peer);
    [LibraryImport(Library, EntryPoint = "e2d_rtc_state"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] internal static partial int State(WebRTCPeerHandle peer, int kind);
    [LibraryImport(Library, EntryPoint = "e2d_rtc_offer"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] internal static partial int Offer(WebRTCPeerHandle peer);
    [LibraryImport(Library, EntryPoint = "e2d_rtc_local", StringMarshalling = StringMarshalling.Utf8), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] internal static partial int Local(WebRTCPeerHandle peer, string type, string sdp);
    [LibraryImport(Library, EntryPoint = "e2d_rtc_remote", StringMarshalling = StringMarshalling.Utf8), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] internal static partial int Remote(WebRTCPeerHandle peer, string type, string sdp);
    [LibraryImport(Library, EntryPoint = "e2d_rtc_candidate", StringMarshalling = StringMarshalling.Utf8), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] internal static partial int Candidate(WebRTCPeerHandle peer, string mid, string candidate);
    [LibraryImport(Library, EntryPoint = "e2d_rtc_event"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] internal static partial nint Event(WebRTCPeerHandle peer);
    [LibraryImport(Library, EntryPoint = "e2d_rtc_event_kind"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] internal static partial int EventKind(nint value);
    [LibraryImport(Library, EntryPoint = "e2d_rtc_event_string"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] internal static partial nint EventString(nint value, int part);
    [LibraryImport(Library, EntryPoint = "e2d_rtc_event_channel"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] internal static partial nint EventChannel(nint value);
    [LibraryImport(Library, EntryPoint = "e2d_rtc_event_release"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] internal static partial void ReleaseEvent(nint value);
    [LibraryImport(Library, EntryPoint = "e2d_rtc_channel_create", StringMarshalling = StringMarshalling.Utf8), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] internal static partial nint CreateChannel(WebRTCPeerHandle peer, string label, string protocol, int ordered, int negotiated, int id, int lifetime, int retransmits);
    [LibraryImport(Library, EntryPoint = "e2d_rtc_channel_destroy"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] internal static partial void DestroyChannel(nint value);
    [LibraryImport(Library, EntryPoint = "e2d_rtc_channel_close"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] internal static partial int CloseChannel(WebRTCChannelHandle value);
    [LibraryImport(Library, EntryPoint = "e2d_rtc_channel_poll"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] internal static partial int PollChannel(WebRTCChannelHandle value);
    [LibraryImport(Library, EntryPoint = "e2d_rtc_channel_info"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] internal static partial int ChannelInfo(WebRTCChannelHandle value, int kind);
    [LibraryImport(Library, EntryPoint = "e2d_rtc_channel_string"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] internal static partial nint ChannelString(WebRTCChannelHandle value, int kind);
    [LibraryImport(Library, EntryPoint = "e2d_rtc_channel_size"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] internal static partial int ChannelSize(WebRTCChannelHandle value);
    [LibraryImport(Library, EntryPoint = "e2d_rtc_channel_read"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] internal static partial int Read(WebRTCChannelHandle value, byte* target, int capacity, out int text);
    [LibraryImport(Library, EntryPoint = "e2d_rtc_channel_send"), UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])] internal static partial int Send(WebRTCChannelHandle value, byte* source, int length, int text);
}
