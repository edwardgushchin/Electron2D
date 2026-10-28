using System.Runtime.InteropServices;

namespace Electron2D;

internal static class LinuxPortalDBus
{
    internal const string Library = "libdbus-1.so.3", Destination = "org.freedesktop.portal.Desktop", DesktopPath = "/org/freedesktop/portal/desktop";
    internal const int StringType = 's', PathType = 'o', UInt32Type = 'u', BooleanType = 'b', ArrayType = 'a', StructType = 'r', VariantType = 'v', DictEntryType = 'e';

    internal static uint QueryVersion(string portal)
    {
        nint connection = 0;
        try
        {
            Check(ThreadsInitDefault()); connection = BusGet(0, 0);
            return connection == 0 ? 0 : QueryVersion(connection, portal, out _);
        }
        catch (DllNotFoundException) { return 0; }
        catch (EntryPointNotFoundException) { return 0; }
        catch (InvalidOperationException) { return 0; }
        finally { if (connection != 0) ConnectionUnref(connection); }
    }

    internal static uint QueryVersion(nint connection, string portal, out string owner)
    {
        owner = "";
        var message = NewMethodCall(Destination, DesktopPath, "org.freedesktop.DBus.Properties", "Get");
        if (message == 0) throw new OutOfMemoryException();
        try
        {
            InitAppend(message, out var args); AppendString(ref args, portal); AppendString(ref args, "version");
            var reply = SendWithReplyAndBlock(connection, message, 250, 0);
            if (reply == 0) return 0;
            try
            {
                if (MessageType(reply) != 2 || Init(reply, out var value) == 0 || GetArgType(ref value) != VariantType) return 0;
                Recurse(ref value, out var version); if (GetArgType(ref version) != UInt32Type) return 0;
                GetBasic(ref version, out uint result); owner = Marshal.PtrToStringUTF8(MessageSender(reply)) ?? ""; return result;
            }
            finally { MessageUnref(reply); }
        }
        finally { MessageUnref(message); }
    }

    internal static void Check(int result) { if (result == 0) throw new OutOfMemoryException(); }
    internal static void AppendString(ref MessageIter iter, string value)
    {
        var text = Marshal.StringToCoTaskMemUTF8(value);
        try { Check(AppendBasicPointer(ref iter, StringType, ref text)); }
        finally { Marshal.FreeCoTaskMem(text); }
    }
    internal static void AppendByte(ref MessageIter iter, byte value) => Check(AppendBasicByte(ref iter, 'y', ref value));
    internal static void AppendUInt32(ref MessageIter iter, uint value, int type = UInt32Type) => Check(AppendBasicUInt32(ref iter, type, ref value));
    internal static MessageIter Open(ref MessageIter iter, int type, string? signature)
    { Check(OpenContainer(ref iter, type, signature, out var child)); return child; }
    internal static void Close(ref MessageIter iter, ref MessageIter child) => Check(CloseContainer(ref iter, ref child));
    internal static void Require(ref MessageIter iter, int type)
    { if (GetArgType(ref iter) != type) throw new InvalidOperationException("The portal returned an unexpected value type."); }
    internal static string ReadString(ref MessageIter iter, int type = StringType)
    { Require(ref iter, type); GetBasicPointer(ref iter, out var value); return Marshal.PtrToStringUTF8(value) ?? throw new InvalidOperationException("The portal returned a null string."); }
    internal static uint ReadUInt32(ref MessageIter iter)
    { Require(ref iter, UInt32Type); GetBasic(ref iter, out uint value); return value; }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MessageIter
    {
        public nint Pointer1, Pointer2;
        public int Dummy3, Dummy4, Dummy5, Dummy6, Dummy7, Dummy8, Dummy9, Dummy10, Dummy11, Pad1;
        public nint Pad2, Pad3;
    }

    [DllImport(Library, EntryPoint = "dbus_threads_init_default")] internal static extern int ThreadsInitDefault();
    [DllImport(Library, EntryPoint = "dbus_bus_get")] internal static extern nint BusGet(int bus, nint error);
    [DllImport(Library, EntryPoint = "dbus_bus_get_private")] internal static extern nint BusGetPrivate(int bus, nint error);
    [DllImport(Library, EntryPoint = "dbus_bus_get_unique_name")] internal static extern nint BusGetUniqueName(nint connection);
    [DllImport(Library, EntryPoint = "dbus_bus_add_match")] internal static extern void BusAddMatch(nint connection, [MarshalAs(UnmanagedType.LPUTF8Str)] string rule, nint error);
    [DllImport(Library, EntryPoint = "dbus_connection_set_exit_on_disconnect")] internal static extern void SetExitOnDisconnect(nint connection, int enabled);
    [DllImport(Library, EntryPoint = "dbus_connection_close")] internal static extern void ConnectionClose(nint connection);
    [DllImport(Library, EntryPoint = "dbus_connection_unref")] internal static extern void ConnectionUnref(nint connection);
    [DllImport(Library, EntryPoint = "dbus_connection_read_write")] internal static extern int ReadWrite(nint connection, int timeout);
    [DllImport(Library, EntryPoint = "dbus_connection_pop_message")] internal static extern nint PopMessage(nint connection);
    [DllImport(Library, EntryPoint = "dbus_message_new_method_call")] internal static extern nint NewMethodCall([MarshalAs(UnmanagedType.LPUTF8Str)] string destination, [MarshalAs(UnmanagedType.LPUTF8Str)] string path, [MarshalAs(UnmanagedType.LPUTF8Str)] string iface, [MarshalAs(UnmanagedType.LPUTF8Str)] string method);
    [DllImport(Library, EntryPoint = "dbus_message_unref")] internal static extern void MessageUnref(nint message);
    [DllImport(Library, EntryPoint = "dbus_message_get_type")] internal static extern int MessageType(nint message);
    [DllImport(Library, EntryPoint = "dbus_message_get_sender")] internal static extern nint MessageSender(nint message);
    [DllImport(Library, EntryPoint = "dbus_message_get_path")] internal static extern nint MessagePath(nint message);
    [DllImport(Library, EntryPoint = "dbus_message_is_signal")] internal static extern int IsSignal(nint message, [MarshalAs(UnmanagedType.LPUTF8Str)] string iface, [MarshalAs(UnmanagedType.LPUTF8Str)] string name);
    [DllImport(Library, EntryPoint = "dbus_message_iter_init_append")] internal static extern void InitAppend(nint message, out MessageIter iter);
    [DllImport(Library, EntryPoint = "dbus_message_iter_append_basic")] private static extern int AppendBasicPointer(ref MessageIter iter, int type, ref nint value);
    [DllImport(Library, EntryPoint = "dbus_message_iter_append_basic")] private static extern int AppendBasicByte(ref MessageIter iter, int type, ref byte value);
    [DllImport(Library, EntryPoint = "dbus_message_iter_append_basic")] private static extern int AppendBasicUInt32(ref MessageIter iter, int type, ref uint value);
    [DllImport(Library, EntryPoint = "dbus_message_iter_open_container")] private static extern int OpenContainer(ref MessageIter iter, int type, [MarshalAs(UnmanagedType.LPUTF8Str)] string? signature, out MessageIter child);
    [DllImport(Library, EntryPoint = "dbus_message_iter_close_container")] private static extern int CloseContainer(ref MessageIter iter, ref MessageIter child);
    [DllImport(Library, EntryPoint = "dbus_connection_send_with_reply_and_block")] internal static extern nint SendWithReplyAndBlock(nint connection, nint message, int timeoutMs, nint error);
    [DllImport(Library, EntryPoint = "dbus_connection_send")] internal static extern int Send(nint connection, nint message, nint serial);
    [DllImport(Library, EntryPoint = "dbus_message_iter_init")] internal static extern int Init(nint message, out MessageIter iter);
    [DllImport(Library, EntryPoint = "dbus_message_iter_get_arg_type")] internal static extern int GetArgType(ref MessageIter iter);
    [DllImport(Library, EntryPoint = "dbus_message_iter_next")] internal static extern int Next(ref MessageIter iter);
    [DllImport(Library, EntryPoint = "dbus_message_iter_recurse")] internal static extern void Recurse(ref MessageIter iter, out MessageIter child);
    [DllImport(Library, EntryPoint = "dbus_message_iter_get_basic")] private static extern void GetBasicPointer(ref MessageIter iter, out nint value);
    [DllImport(Library, EntryPoint = "dbus_message_iter_get_basic")] internal static extern void GetBasic(ref MessageIter iter, out uint value);
}
