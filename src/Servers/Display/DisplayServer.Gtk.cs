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
