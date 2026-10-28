using System.Runtime.InteropServices;

namespace Electron2D;

public sealed partial class DisplayServer
{
    private const uint GtkApplicationStylePriority = 600;

    [LibraryImport("libgtk-3.so.0", EntryPoint = "gtk_init_check")]
    private static partial int GTKInitCheck(nint argc, nint argv);

    [LibraryImport("libgtk-3.so.0", EntryPoint = "gtk_css_provider_new")]
    private static partial nint GTKCSSProviderNew();

    [LibraryImport("libgtk-3.so.0", EntryPoint = "gtk_css_provider_load_from_data", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int GTKCSSProviderLoadFromData(nint provider, string css, nint length, nint error);

    [LibraryImport("libgdk-3.so.0", EntryPoint = "gdk_screen_get_default")]
    private static partial nint GDKScreenGetDefault();

    [LibraryImport("libgtk-3.so.0", EntryPoint = "gtk_style_context_add_provider_for_screen")]
    private static partial void GTKStyleContextAddProviderForScreen(nint screen, nint provider, uint priority);

    [LibraryImport("libgtk-3.so.0", EntryPoint = "gtk_style_context_remove_provider_for_screen")]
    private static partial void GTKStyleContextRemoveProviderForScreen(nint screen, nint provider);

    [LibraryImport("libgobject-2.0.so.0", EntryPoint = "g_object_unref")]
    private static partial void GObjectUnref(nint instance);

    [LibraryImport("libgio-2.0.so.0", EntryPoint = "g_bus_get_sync")]
    private static partial nint GIOBusGetSync(int type, nint cancellable, nint error);
    [LibraryImport("libgio-2.0.so.0", EntryPoint = "g_dbus_connection_get_exit_on_close")]
    private static partial int GIOGetExitOnClose(nint connection);
    [LibraryImport("libgio-2.0.so.0", EntryPoint = "g_dbus_connection_set_exit_on_close")]
    private static partial void GIOSetExitOnClose(nint connection, int enabled);
    [LibraryImport("libgio-2.0.so.0", EntryPoint = "g_dbus_connection_is_closed")]
    private static partial int GIOIsClosed(nint connection);

    private static (nint Connection, int ExitOnClose) RetainGTKSessionBus()
    {
        nint connection = 0;
        try
        {
            connection = GIOBusGetSync(2, 0, 0);
            if (connection == 0) return default;
            var previous = GIOGetExitOnClose(connection);
            // The decoration plugin pumps this shared connection; bus loss must not terminate the host.
            GIOSetExitOnClose(connection, 0);
            var retained = (connection, previous); connection = 0; return retained;
        }
        catch (DllNotFoundException) { return default; }
        catch (EntryPointNotFoundException) { return default; }
        finally { if (connection != 0) GObjectUnref(connection); }
    }

    private static void ReleaseGTKSessionBus((nint Connection, int ExitOnClose) retained)
    {
        if (retained.Connection == 0) return;
        try { if (GIOIsClosed(retained.Connection) == 0) GIOSetExitOnClose(retained.Connection, retained.ExitOnClose); }
        finally { GObjectUnref(retained.Connection); }
    }

    private static (nint GtkScreen, nint GtkProvider) InstallGTKTitlebarStyle()
    {
        nint provider = 0;
        try
        {
            if (GTKInitCheck(0, 0) == 0)
                return default;
            var screen = GDKScreenGetDefault();
            if (screen == 0)
                return default;
            provider = GTKCSSProviderNew();
            if (provider == 0)
                return default;

            // Some themes put a one-pixel border below the titlebar. The decoration plugin renders
            // its background but not that border, so the border area must inherit the background.
            if (GTKCSSProviderLoadFromData(provider,
                    "headerbar.default-decoration { background-clip: border-box; }", -1, 0) == 0)
                return default;
            GTKStyleContextAddProviderForScreen(screen, provider, GtkApplicationStylePriority);
            var installed = (screen, provider);
            provider = 0;
            return installed;
        }
        catch (DllNotFoundException)
        {
            return default;
        }
        catch (EntryPointNotFoundException)
        {
            return default;
        }
        finally
        {
            if (provider != 0)
                GObjectUnref(provider);
        }
    }

    private static void RemoveGTKTitlebarStyle(nint screen, nint provider)
    {
        if (provider == 0)
            return;
        GTKStyleContextRemoveProviderForScreen(screen, provider);
        GObjectUnref(provider);
    }
}
