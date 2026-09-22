using System.Runtime.InteropServices;

namespace Electron2D;

public sealed partial class DisplayServer
{
    private const uint GtkApplicationStylePriority = 600;

    [LibraryImport("libgtk-3.so.0", EntryPoint = "gtk_init_check")]
    private static partial int GtkInitCheck(nint argc, nint argv);

    [LibraryImport("libgtk-3.so.0", EntryPoint = "gtk_css_provider_new")]
    private static partial nint GtkCssProviderNew();

    [LibraryImport("libgtk-3.so.0", EntryPoint = "gtk_css_provider_load_from_data", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int GtkCssProviderLoadFromData(nint provider, string css, nint length, nint error);

    [LibraryImport("libgdk-3.so.0", EntryPoint = "gdk_screen_get_default")]
    private static partial nint GdkScreenGetDefault();

    [LibraryImport("libgtk-3.so.0", EntryPoint = "gtk_style_context_add_provider_for_screen")]
    private static partial void GtkStyleContextAddProviderForScreen(nint screen, nint provider, uint priority);

    [LibraryImport("libgtk-3.so.0", EntryPoint = "gtk_style_context_remove_provider_for_screen")]
    private static partial void GtkStyleContextRemoveProviderForScreen(nint screen, nint provider);

    [LibraryImport("libgobject-2.0.so.0", EntryPoint = "g_object_unref")]
    private static partial void GObjectUnref(nint instance);

    private static (nint GtkScreen, nint GtkProvider) InstallGtkTitlebarStyle()
    {
        nint provider = 0;
        try
        {
            if (GtkInitCheck(0, 0) == 0)
                return default;
            var screen = GdkScreenGetDefault();
            if (screen == 0)
                return default;
            provider = GtkCssProviderNew();
            if (provider == 0)
                return default;

            // Some themes put a one-pixel border below the titlebar. The decoration plugin renders
            // its background but not that border, so the border area must inherit the background.
            if (GtkCssProviderLoadFromData(provider,
                    "headerbar.default-decoration { background-clip: border-box; }", -1, 0) == 0)
                return default;
            GtkStyleContextAddProviderForScreen(screen, provider, GtkApplicationStylePriority);
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

    private static void RemoveGtkTitlebarStyle(nint screen, nint provider)
    {
        if (provider == 0)
            return;
        GtkStyleContextRemoveProviderForScreen(screen, provider);
        GObjectUnref(provider);
    }
}
