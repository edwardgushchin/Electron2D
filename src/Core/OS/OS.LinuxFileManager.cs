using System.Runtime.InteropServices;
using static Electron2D.GIONative;

namespace Electron2D;

internal static partial class LinuxFileManager
{
    private const string GIO = "libgio-2.0.so.0";
    private const string Service = "org.freedesktop.FileManager1";
    private const int SessionBus = 2;
    private const int AuthenticationClient = 1;
    private const int MessageBusConnection = 8;
    private const int ErrorReply = 3;

    internal static bool TryShow(string path, bool enterDirectory)
    {
        nint address;
        nint error;
        try
        {
            address = g_dbus_address_get_for_bus_sync(SessionBus, 0, out error);
        }
        catch (DllNotFoundException)
        {
            return false;
        }

        if (address == 0)
        {
            if (error != 0)
                g_error_free(error);

            return false;
        }

        nint connection;
        try
        {
            // Own this connection without changing the process's shared bus settings.
            connection = g_dbus_connection_new_for_address_sync(Marshal.PtrToStringUTF8(address)!,
                AuthenticationClient | MessageBusConnection, 0, 0, out error);
        }
        finally
        {
            g_free(address);
        }

        if (connection == 0)
            throw new IOException(ErrorMessage(error, "Cannot connect to the desktop session bus."));

        try
        {
            return Request(connection, path, enterDirectory ? "ShowFolders" : "ShowItems");
        }
        finally
        {
            g_dbus_connection_close_sync(connection, 0, out error);
            if (error != 0)
                g_error_free(error);

            g_object_unref(connection);
        }
    }

    private static bool Request(nint connection, string path, string method)
    {
        var message = g_dbus_message_new_method_call(Service, "/org/freedesktop/FileManager1", Service, method);
        try
        {
            var file = g_file_new_for_path(path);
            try
            {
                var uri = g_file_get_uri(file);
                try
                {
                    g_dbus_message_set_body(message,
                        Tuple([Array("s", [g_variant_new_string(Marshal.PtrToStringUTF8(uri)!)]), g_variant_new_string("")]));
                }
                finally
                {
                    g_free(uri);
                }
            }
            finally
            {
                g_object_unref(file);
            }

            var reply = g_dbus_connection_send_message_with_reply_sync(connection, message, 0, -1, 0, 0, out var error);
            if (reply == 0)
                throw new IOException(ErrorMessage(error, "The desktop file-manager request failed."));

            try
            {
                if (g_dbus_message_get_message_type(reply) != ErrorReply)
                    return true;

                // A service can itself return these names. Only the bus can declare the destination absent.
                if (Marshal.PtrToStringUTF8(g_dbus_message_get_sender(reply)) == "org.freedesktop.DBus"
                    && Marshal.PtrToStringUTF8(g_dbus_message_get_error_name(reply)) is
                        "org.freedesktop.DBus.Error.ServiceUnknown" or "org.freedesktop.DBus.Error.NameHasNoOwner")
                    return false;

                g_dbus_message_to_gerror(reply, out error);
                throw new IOException(ErrorMessage(error, "The desktop file manager rejected the request."));
            }
            finally
            {
                g_object_unref(reply);
            }
        }
        finally
        {
            g_object_unref(message);
        }
    }

    [LibraryImport(GIO)]
    private static partial nint g_dbus_address_get_for_bus_sync(int busType, nint cancellation, out nint error);

    [LibraryImport(GIO, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(NativeUTF8StringMarshaller))]
    private static partial nint g_file_new_for_path(string path);

    [LibraryImport(GIO)]
    private static partial nint g_file_get_uri(nint file);

    [LibraryImport(GIO, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(NativeUTF8StringMarshaller))]
    private static partial nint g_dbus_message_new_method_call(string destination, string path, string iface, string method);

    [LibraryImport(GIO)]
    private static partial void g_dbus_message_set_body(nint message, nint body);

    [LibraryImport(GIO)]
    private static partial nint g_dbus_connection_send_message_with_reply_sync(nint connection, nint message,
        int flags, int timeout, nint serial, nint cancellation, out nint error);

    [LibraryImport(GIO)]
    private static partial int g_dbus_message_get_message_type(nint message);

    [LibraryImport(GIO)]
    private static partial nint g_dbus_message_get_sender(nint message);

    [LibraryImport(GIO)]
    private static partial nint g_dbus_message_get_error_name(nint message);

    [LibraryImport(GIO)]
    private static partial int g_dbus_message_to_gerror(nint message, out nint error);
}
