using System.Runtime.InteropServices;

namespace Electron2D;

internal static unsafe partial class GIONative
{
    private const string GIO = "libgio-2.0.so.0", GLib = "libglib-2.0.so.0", GObject = "libgobject-2.0.so.0";
    [StructLayout(LayoutKind.Sequential)]
    internal struct VTable
    {
        internal nint Method, GetProperty, SetProperty;
        internal nint Pad0, Pad1, Pad2, Pad3, Pad4, Pad5, Pad6, Pad7;
    }
    [StructLayout(LayoutKind.Sequential)] internal struct Error { internal uint Domain; internal int Code; internal nint Message; }
    [LibraryImport(GLib)] internal static partial nint g_main_context_new();
    [LibraryImport(GLib)] internal static partial void g_main_context_unref(nint context);
    [LibraryImport(GLib)] internal static partial void g_main_context_push_thread_default(nint context);
    [LibraryImport(GLib)] internal static partial void g_main_context_pop_thread_default(nint context);
    [LibraryImport(GLib)] internal static partial int g_main_context_pending(nint context);
    [LibraryImport(GLib)] internal static partial int g_main_context_iteration(nint context, int mayBlock);
    [LibraryImport(GLib)] internal static partial void g_error_free(nint error);
    [LibraryImport(GIO, StringMarshalling = StringMarshalling.Utf8)] internal static partial nint g_dbus_error_new_for_dbus_error(string name, string message);
    internal static void SetError(nint error, string message, string name = "org.freedesktop.DBus.Error.Failed") { if (error != 0) *(nint*)error = g_dbus_error_new_for_dbus_error(name, message); }
    [LibraryImport(GObject)] internal static partial void g_object_unref(nint value);
    [LibraryImport(GIO)] internal static partial nint g_bus_get_sync(int busType, nint cancellation, out nint error);
    [LibraryImport(GIO, StringMarshalling = StringMarshalling.Utf8)] internal static partial nint g_dbus_connection_new_for_address_sync(string address, int flags, nint observer, nint cancellation, out nint error);
    [LibraryImport(GIO)] internal static partial nint g_dbus_connection_get_unique_name(nint connection);
    [LibraryImport(GIO)] internal static partial int g_dbus_connection_is_closed(nint connection);
    [LibraryImport(GIO)] internal static partial void g_dbus_connection_set_exit_on_close(nint connection, int enabled);
    [LibraryImport(GIO)] internal static partial int g_dbus_connection_close_sync(nint connection, nint cancellation, out nint error);
    [LibraryImport(GIO, StringMarshalling = StringMarshalling.Utf8)] internal static partial nint g_dbus_connection_call_sync(nint connection, string destination, string path, string iface, string method, nint parameters, nint replyType, int flags, int timeout, nint cancellation, out nint error);
    [LibraryImport(GIO, StringMarshalling = StringMarshalling.Utf8)] internal static partial void g_dbus_connection_call(nint connection, string destination, string path, string iface, string method, nint parameters, nint replyType, int flags, int timeout, nint cancellation, nint callback, nint data);
    [LibraryImport(GIO)] internal static partial nint g_dbus_connection_call_finish(nint connection, nint result, out nint error);
    [LibraryImport(GIO, StringMarshalling = StringMarshalling.Utf8)] internal static partial nint g_dbus_node_info_new_for_xml(string xml, out nint error);
    [LibraryImport(GIO)] internal static partial void g_dbus_node_info_unref(nint info);
    [LibraryImport(GIO, StringMarshalling = StringMarshalling.Utf8)] internal static partial nint g_dbus_node_info_lookup_interface(nint info, string name);
    [LibraryImport(GIO, StringMarshalling = StringMarshalling.Utf8)] internal static partial uint g_dbus_connection_register_object(nint connection, string path, nint iface, nint vtable, nint data, nint destroy, out nint error);
    [LibraryImport(GIO)] internal static partial int g_dbus_connection_unregister_object(nint connection, uint id);
    [LibraryImport(GIO)] internal static partial void g_dbus_method_invocation_return_value(nint invocation, nint value);
    [LibraryImport(GIO, StringMarshalling = StringMarshalling.Utf8)] internal static partial void g_dbus_method_invocation_return_dbus_error(nint invocation, string name, string message);
    [LibraryImport(GIO, StringMarshalling = StringMarshalling.Utf8)] internal static partial int g_dbus_connection_emit_signal(nint connection, nint destination, string path, string iface, string name, nint parameters, out nint error);
    [LibraryImport(GLib, StringMarshalling = StringMarshalling.Utf8)] internal static partial nint g_variant_type_new(string signature);
    [LibraryImport(GLib)] internal static partial void g_variant_type_free(nint type);
    [LibraryImport(GLib)] internal static partial nint g_variant_new_tuple(nint* values, nuint count);
    [LibraryImport(GLib)] internal static partial nint g_variant_new_array(nint type, nint* values, nuint count);
    [LibraryImport(GLib)] internal static partial nint g_variant_new_dict_entry(nint key, nint value);
    [LibraryImport(GLib)] internal static partial nint g_variant_new_variant(nint value);
    [LibraryImport(GLib, StringMarshalling = StringMarshalling.Utf8)] internal static partial nint g_variant_new_string(string value);
    [LibraryImport(GLib, EntryPoint = "g_variant_new_string")] internal static partial nint g_variant_new_string(byte* value);
    [LibraryImport(GLib, StringMarshalling = StringMarshalling.Utf8)] internal static partial nint g_variant_new_object_path(string value);
    [LibraryImport(GLib)] internal static partial nint g_variant_new_int16(short value);
    [LibraryImport(GLib)] internal static partial nint g_variant_new_int32(int value);
    [LibraryImport(GLib)] internal static partial nint g_variant_new_uint32(uint value);
    [LibraryImport(GLib)] internal static partial nint g_variant_new_boolean(int value);
    [LibraryImport(GLib)] internal static partial nint g_variant_new_double(double value);
    [LibraryImport(GLib)] internal static partial nint g_variant_get_child_value(nint value, nuint index);
    [LibraryImport(GLib)] internal static partial nint g_variant_get_string(nint value, out nuint length);
    [LibraryImport(GLib)] internal static partial double g_variant_get_double(nint value);
    [LibraryImport(GLib)] internal static partial int g_variant_get_int32(nint value);
    [LibraryImport(GLib)] internal static partial uint g_variant_get_uint32(nint value);
    [LibraryImport(GLib)] internal static partial int g_variant_get_boolean(nint value);
    [LibraryImport(GLib)] internal static partial nint g_variant_get_variant(nint value);
    [LibraryImport(GLib)] internal static partial nint g_variant_ref_sink(nint value);
    [LibraryImport(GLib)] internal static partial void g_variant_unref(nint value);
    [LibraryImport(GLib)] internal static partial nint g_variant_builder_new(nint type);
    [LibraryImport(GLib)] internal static partial void g_variant_builder_add_value(nint builder, nint value);
    [LibraryImport(GLib)] internal static partial nint g_variant_builder_end(nint builder);
    [LibraryImport(GLib)] internal static partial void g_variant_builder_unref(nint builder);

    internal static nint Tuple(ReadOnlySpan<nint> values) { fixed (nint* p = values) return g_variant_new_tuple(p, (nuint)values.Length); }
    internal static nint Array(string itemType, ReadOnlySpan<nint> values)
    {
        var type = g_variant_type_new(itemType);
        try { fixed (nint* p = values) return g_variant_new_array(type, p, (nuint)values.Length); }
        finally { g_variant_type_free(type); }
    }
    internal static nint Builder(string typeName) { var type = g_variant_type_new(typeName); try { return g_variant_builder_new(type); } finally { g_variant_type_free(type); } }
    internal static nint Ref(string bus, string path) => Tuple([g_variant_new_string(bus), g_variant_new_object_path(path)]);
    internal static string String(nint value) => Marshal.PtrToStringUTF8(g_variant_get_string(value, out _)) ?? "";
    internal static int IntArg(nint args, nuint index) { var v = g_variant_get_child_value(args, index); try { return g_variant_get_int32(v); } finally { g_variant_unref(v); } }
    internal static uint UIntArg(nint args, nuint index) { var v = g_variant_get_child_value(args, index); try { return g_variant_get_uint32(v); } finally { g_variant_unref(v); } }
    internal static string StringArg(nint args, nuint index) { var v = g_variant_get_child_value(args, index); try { return String(v); } finally { g_variant_unref(v); } }
    internal static string ErrorMessage(nint error)
    {
        if (error == 0) return "Native accessibility operation failed.";
        try { return Marshal.PtrToStringUTF8(((Error*)error)->Message) ?? "Native accessibility operation failed."; }
        finally { g_error_free(error); }
    }
}
