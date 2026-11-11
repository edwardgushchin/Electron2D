using System.Runtime.InteropServices;

namespace Electron2D;

internal static unsafe partial class LinuxPortalDBus
{
    internal const string Library = "libdbus-1.so.3", Destination = "org.freedesktop.portal.Desktop", DesktopPath = "/org/freedesktop/portal/desktop";
    internal const int StringType = 's', PathType = 'o', UInt32Type = 'u', DoubleType = 'd', BooleanType = 'b', ArrayType = 'a', StructType = 'r', VariantType = 'v', DictEntryType = 'e';

    internal static int ReadAppearancePreference(string key) => ReadAccessibilityPreference(key, appearance: true);
    internal static int ReadScreenReader() => ReadAccessibilityPreference("ScreenReaderEnabled", appearance: false);

    private static int ReadAccessibilityPreference(string key, bool appearance)
    {
        const string settings = "org.freedesktop.portal.Settings";
        nint connection = 0;
        try
        {
            Check(ThreadsInitDefault());
            connection = BusGetPrivate(0, 0);
            if (connection == 0)
                return -1;
            SetExitOnDisconnect(connection, 0);

            if (!appearance)
            {
                var enabled = ReadATSPIStatus(connection, "IsEnabled");
                return enabled == 1 ? ReadATSPIStatus(connection, key) : enabled;
            }

            var version = QueryVersion(connection, settings, out var owner);
            if (version == 0 || owner.Length == 0)
                return -1;
            var legacyRead = version == 1;
            var message = NewMethodCall(owner, DesktopPath, settings, legacyRead ? "Read" : "ReadOne");
            return ReadPreferenceValue(connection, message, "org.freedesktop.appearance", key, UInt32Type, legacyRead);
        }
        catch (Exception error) when (error is DllNotFoundException or EntryPointNotFoundException)
        {
            return -1;
        }
        finally
        {
            if (connection != 0)
            {
                ConnectionClose(connection);
                ConnectionUnref(connection);
            }
        }
    }

    private static int ReadATSPIStatus(nint connection, string key)
    {
        var message = NewMethodCall("org.a11y.Bus", "/org/a11y/bus", "org.freedesktop.DBus.Properties", "Get");
        return ReadPreferenceValue(connection, message, "org.a11y.Status", key, BooleanType);
    }

    private static int ReadPreferenceValue(nint connection, nint message, string group, string key, int type, bool legacyRead = false)
    {
        if (message == 0)
            throw new OutOfMemoryException();
        nint reply = 0;
        try
        {
            InitAppend(message, out var args);
            AppendString(ref args, group);
            AppendString(ref args, key);
            reply = SendWithReplyAndBlock(connection, message, 250, 0);
            if (reply == 0 || MessageType(reply) != 2 || Init(reply, out var result) == 0 || GetArgType(ref result) != VariantType)
                return -1;
            Recurse(ref result, out var value);
            var valueType = GetArgType(ref value);
            if ((legacyRead && valueType == VariantType) || (type == BooleanType && valueType == StructType))
            {
                Recurse(ref value, out var inner);
                value = inner;
                valueType = GetArgType(ref value);
            }

            if (valueType != type)
                return -1;
            GetBasic(ref value, out uint preference);
            return preference == 1 ? 1 : 0;
        }
        finally
        {
            if (reply != 0) MessageUnref(reply);
            MessageUnref(message);
        }
    }

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
        var text = (nint)NativeUTF8StringMarshaller.Allocate(value);
        try { Check(AppendBasicPointer(ref iter, StringType, ref text)); }
        finally { NativeAllocation.Free((void*)text); }
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

    internal static double ReadDouble(ref MessageIter iter)
    { Require(ref iter, DoubleType); GetBasicDouble(ref iter, out var value); return value; }

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
    [LibraryImport(Library, EntryPoint = "dbus_bus_add_match", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(NativeUTF8StringMarshaller))] internal static partial void BusAddMatch(nint connection, string rule, nint error);
    [DllImport(Library, EntryPoint = "dbus_connection_set_exit_on_disconnect")] internal static extern void SetExitOnDisconnect(nint connection, int enabled);
    [DllImport(Library, EntryPoint = "dbus_connection_close")] internal static extern void ConnectionClose(nint connection);
    [DllImport(Library, EntryPoint = "dbus_connection_unref")] internal static extern void ConnectionUnref(nint connection);
    [DllImport(Library, EntryPoint = "dbus_connection_read_write")] internal static extern int ReadWrite(nint connection, int timeout);
    [DllImport(Library, EntryPoint = "dbus_connection_pop_message")] internal static extern nint PopMessage(nint connection);
    [LibraryImport(Library, EntryPoint = "dbus_message_new_method_call", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(NativeUTF8StringMarshaller))] internal static partial nint NewMethodCall(string destination, string path, string iface, string method);
    [DllImport(Library, EntryPoint = "dbus_message_unref")] internal static extern void MessageUnref(nint message);
    [DllImport(Library, EntryPoint = "dbus_message_get_type")] internal static extern int MessageType(nint message);
    [DllImport(Library, EntryPoint = "dbus_message_get_sender")] internal static extern nint MessageSender(nint message);
    [DllImport(Library, EntryPoint = "dbus_message_get_path")] internal static extern nint MessagePath(nint message);
    [LibraryImport(Library, EntryPoint = "dbus_message_is_signal", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(NativeUTF8StringMarshaller))] internal static partial int IsSignal(nint message, string iface, string name);
    [DllImport(Library, EntryPoint = "dbus_message_iter_init_append")] internal static extern void InitAppend(nint message, out MessageIter iter);
    [DllImport(Library, EntryPoint = "dbus_message_iter_append_basic")] private static extern int AppendBasicPointer(ref MessageIter iter, int type, ref nint value);
    [DllImport(Library, EntryPoint = "dbus_message_iter_append_basic")] private static extern int AppendBasicByte(ref MessageIter iter, int type, ref byte value);
    [DllImport(Library, EntryPoint = "dbus_message_iter_append_basic")] private static extern int AppendBasicUInt32(ref MessageIter iter, int type, ref uint value);
    [LibraryImport(Library, EntryPoint = "dbus_message_iter_open_container", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(NativeUTF8StringMarshaller))] private static partial int OpenContainer(ref MessageIter iter, int type, string? signature, out MessageIter child);
    [DllImport(Library, EntryPoint = "dbus_message_iter_close_container")] private static extern int CloseContainer(ref MessageIter iter, ref MessageIter child);
    [DllImport(Library, EntryPoint = "dbus_connection_send_with_reply_and_block")] internal static extern nint SendWithReplyAndBlock(nint connection, nint message, int timeoutMs, nint error);
    [DllImport(Library, EntryPoint = "dbus_connection_send")] internal static extern int Send(nint connection, nint message, nint serial);
    [DllImport(Library, EntryPoint = "dbus_message_iter_init")] internal static extern int Init(nint message, out MessageIter iter);
    [DllImport(Library, EntryPoint = "dbus_message_iter_get_arg_type")] internal static extern int GetArgType(ref MessageIter iter);
    [DllImport(Library, EntryPoint = "dbus_message_iter_next")] internal static extern int Next(ref MessageIter iter);
    [DllImport(Library, EntryPoint = "dbus_message_iter_recurse")] internal static extern void Recurse(ref MessageIter iter, out MessageIter child);
    [DllImport(Library, EntryPoint = "dbus_message_iter_get_basic")] private static extern void GetBasicPointer(ref MessageIter iter, out nint value);
    [DllImport(Library, EntryPoint = "dbus_message_iter_get_basic")] private static extern void GetBasicDouble(ref MessageIter iter, out double value);
    [DllImport(Library, EntryPoint = "dbus_message_iter_get_basic")] internal static extern void GetBasic(ref MessageIter iter, out uint value);
}
