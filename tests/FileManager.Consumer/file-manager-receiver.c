#include <gio/gio.h>
#include <stdio.h>

#define FILE_MANAGER "org.freedesktop.FileManager1"

static const char introspection_xml[] =
    "<node>"
    "  <interface name='" FILE_MANAGER "'>"
    "    <method name='ShowFolders'>"
    "      <arg type='as' direction='in'/>"
    "      <arg type='s' direction='in'/>"
    "    </method>"
    "    <method name='ShowItems'>"
    "      <arg type='as' direction='in'/>"
    "      <arg type='s' direction='in'/>"
    "    </method>"
    "  </interface>"
    "</node>";

typedef struct {
    GMainLoop *loop;
    GDBusNodeInfo *node;
    gboolean reject;
} Receiver;

static void receive(GDBusConnection *connection, const gchar *sender,
    const gchar *path, const gchar *interface, const gchar *method,
    GVariant *parameters, GDBusMethodInvocation *invocation, gpointer user_data)
{
    Receiver *receiver = user_data;
    GVariant *uris;
    const gchar *startup_id;

    g_variant_get(parameters, "(@as&s)", &uris, &startup_id);
    g_print("%s\t%zu", method, g_variant_n_children(uris));
    for (gsize index = 0; index < g_variant_n_children(uris); index++) {
        const gchar *uri;
        g_variant_get_child(uris, index, "&s", &uri);
        g_print("\t%s", uri);
    }
    g_print("\t%s\n", startup_id);
    fflush(stdout);
    g_variant_unref(uris);

    if (receiver->reject) {
        g_dbus_method_invocation_return_dbus_error(invocation,
            "org.freedesktop.DBus.Error.ServiceUnknown", "Probe rejected selection");
    } else {
        g_dbus_method_invocation_return_value(invocation, NULL);
    }
}

static void register_object(GDBusConnection *connection, const gchar *name, gpointer user_data)
{
    Receiver *receiver = user_data;
    static const GDBusInterfaceVTable vtable = { .method_call = receive };
    g_autoptr(GError) error = NULL;

    if (!g_dbus_connection_register_object(connection, "/org/freedesktop/FileManager1",
        receiver->node->interfaces[0], &vtable, receiver, NULL, &error)) {
        g_error("%s", error->message);
    }
}

static void ready(GDBusConnection *connection, const gchar *name, gpointer user_data)
{
    g_print("ready\n");
    fflush(stdout);
}

static void lost_name(GDBusConnection *connection, const gchar *name, gpointer user_data)
{
    Receiver *receiver = user_data;
    g_printerr("Could not own %s\n", name);
    g_main_loop_quit(receiver->loop);
}

int main(int argc, char *argv[])
{
    if (argc != 2 || (g_strcmp0(argv[1], "accept") != 0 && g_strcmp0(argv[1], "reject") != 0)) {
        g_printerr("Expected accept or reject\n");
        return 2;
    }

    g_autoptr(GError) error = NULL;
    g_autoptr(GDBusNodeInfo) node = g_dbus_node_info_new_for_xml(introspection_xml, &error);
    if (!node) {
        g_printerr("%s\n", error->message);
        return 2;
    }

    g_autoptr(GMainLoop) loop = g_main_loop_new(NULL, FALSE);
    Receiver receiver = { loop, node, g_strcmp0(argv[1], "reject") == 0 };
    guint owner = g_bus_own_name(G_BUS_TYPE_SESSION, FILE_MANAGER, G_BUS_NAME_OWNER_FLAGS_NONE,
        register_object, ready, lost_name, &receiver, NULL);
    g_main_loop_run(loop);
    g_bus_unown_name(owner);
    return 2;
}
