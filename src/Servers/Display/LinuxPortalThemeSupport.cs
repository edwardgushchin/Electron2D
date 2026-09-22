using System.Runtime.InteropServices;

namespace Electron2D;

internal static class LinuxPortalThemeSupport
{
    private const string Library = "libdbus-1.so.3";
    private const int SessionBus = 0;
    private const int StringType = 's';
    private const int VariantType = 'v';
    private const int UInt32Type = 'u';

    internal static bool Query()
    {
        try
        {
            return Probe();
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }

    private static bool Probe()
    {
        var connection = DBusBusGet(SessionBus, 0);
        if (connection == 0)
            return false;
        try
        {
            var message = DBusMessageNewMethodCall("org.freedesktop.portal.Desktop",
                "/org/freedesktop/portal/desktop", "org.freedesktop.DBus.Properties", "Get");
            if (message == 0)
                return false;
            try
            {
                DBusMessageIterInitAppend(message, out var args);
                var interfaceName = Marshal.StringToCoTaskMemUTF8("org.freedesktop.portal.Settings");
                var propertyName = Marshal.StringToCoTaskMemUTF8("version");
                try
                {
                    if (DBusMessageIterAppendBasic(ref args, StringType, ref interfaceName) == 0 ||
                        DBusMessageIterAppendBasic(ref args, StringType, ref propertyName) == 0)
                        return false;
                }
                finally
                {
                    Marshal.FreeCoTaskMem(interfaceName);
                    Marshal.FreeCoTaskMem(propertyName);
                }

                var reply = DBusConnectionSendWithReplyAndBlock(connection, message, 250, 0);
                if (reply == 0)
                    return false;
                try
                {
                    if (DBusMessageIterInit(reply, out var value) == 0 ||
                        DBusMessageIterGetArgType(ref value) != VariantType)
                        return false;
                    DBusMessageIterRecurse(ref value, out var versionValue);
                    if (DBusMessageIterGetArgType(ref versionValue) != UInt32Type)
                        return false;
                    DBusMessageIterGetBasic(ref versionValue, out uint version);
                    return version >= 1;
                }
                finally
                {
                    DBusMessageUnref(reply);
                }
            }
            finally
            {
                DBusMessageUnref(message);
            }
        }
        finally
        {
            DBusConnectionUnref(connection);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MessageIter
    {
        public nint Pointer1, Pointer2;
        public int Dummy3, Dummy4, Dummy5, Dummy6, Dummy7, Dummy8, Dummy9, Dummy10, Dummy11, Pad1;
        public nint Pad2, Pad3;
    }

    [DllImport(Library, EntryPoint = "dbus_bus_get")]
    private static extern nint DBusBusGet(int bus, nint error);

    [DllImport(Library, EntryPoint = "dbus_connection_unref")]
    private static extern void DBusConnectionUnref(nint connection);

    [DllImport(Library, EntryPoint = "dbus_message_new_method_call")]
    private static extern nint DBusMessageNewMethodCall(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string destination,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string path,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string interfaceName,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string method);

    [DllImport(Library, EntryPoint = "dbus_message_unref")]
    private static extern void DBusMessageUnref(nint message);

    [DllImport(Library, EntryPoint = "dbus_message_iter_init_append")]
    private static extern void DBusMessageIterInitAppend(nint message, out MessageIter iter);

    [DllImport(Library, EntryPoint = "dbus_message_iter_append_basic")]
    private static extern int DBusMessageIterAppendBasic(ref MessageIter iter, int type, ref nint value);

    [DllImport(Library, EntryPoint = "dbus_connection_send_with_reply_and_block")]
    private static extern nint DBusConnectionSendWithReplyAndBlock(nint connection, nint message, int timeoutMs, nint error);

    [DllImport(Library, EntryPoint = "dbus_message_iter_init")]
    private static extern int DBusMessageIterInit(nint message, out MessageIter iter);

    [DllImport(Library, EntryPoint = "dbus_message_iter_get_arg_type")]
    private static extern int DBusMessageIterGetArgType(ref MessageIter iter);

    [DllImport(Library, EntryPoint = "dbus_message_iter_recurse")]
    private static extern void DBusMessageIterRecurse(ref MessageIter iter, out MessageIter sub);

    [DllImport(Library, EntryPoint = "dbus_message_iter_get_basic")]
    private static extern void DBusMessageIterGetBasic(ref MessageIter iter, out uint value);
}
